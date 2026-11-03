using UnityEngine;

// NPC 와 대화하기 위한 IInteractable 진입점.
//
// 설계 의도:
// - PlayerInteraction Raycast 가 IInteractable 로 감지 → Space 키에 Interact() 호출.
// - 인사·잡담·구매 반응은 ResidentContextDialogue(Assets/Scripts/Dialogue)가 실제 상황(정착 진행·시간·가방·
//   동행 일과·있는 곳·친밀도)에서 고르고, 같은 날 다시 말을 걸면 다음 이야기로 이어 간다.
//   역할 표가 없는 주제나 조합 실패 시에만 DialogueData(SO) 풀 + DialogueService 로 폴백한다.
// - NpcController 가 같은 GameObject 에 붙어 있으면 NpcProfile 을 자동 참조 → T/F 톤 분기에 사용.
// - 화면 이름은 ResidentNames 가 푼다(미라 · 농부). profile.npcName("Farmer_01")은 내부 키라 그대로 둔다.
// - 출력은 ShowLine() 한 곳(DialogueUI, 없으면 Debug.Log)으로 모은다 (Docs/06 NFR 유지보수성).
//
// 결정론:
// - NpcController 의 결정론 RNG 를 재사용하지 않는다. 대사 선택은 게임 상태(저장 대상)에 영향이 없기 때문에
//   새로운 System.Random() 을 그때그때 생성해도 문제없다 (DialogueService 가 자동 처리).
[RequireComponent(typeof(Collider))]
public class NpcDialogue : MonoBehaviour, IInteractable
{
    [Header("대사 소스")]
    [Tooltip("이 NPC 가 사용할 대사 풀. 공용 에셋을 드래그하거나 전용 에셋을 만들어 연결한다.")]
    public DialogueData dialogueData;

    [Header("프로필 (선택)")]
    [Tooltip("비워두면 같은 오브젝트의 NpcController.profile 을 자동 참조한다.")]
    public NpcProfile overrideProfile;

    [Header("상호작용 프롬프트")]
    [Tooltip("PlayerInteraction 에 표시될 안내 문구. 기본: '대화하기'")]
    public string interactPrompt = "대화하기";

    [Header("친밀도 ID")]
    [Tooltip("FriendshipService 의 키. 비워 두면 친밀도 포인트가 집계되지 않는다.\n" +
             "같은 프리팹을 여러 번 스폰할 때 각자의 친밀도를 원한다면 인스턴스마다 고유 ID를 지정한다.")]
    public string friendshipId = "";

    [Header("주민 요청")]
    [Tooltip("전문 주민의 당일 재료 요청을 완료했을 때 지급할 친밀도. 코인/아이템 보상은 지급하지 않습니다.")]
    [Min(0)] public int requestFriendshipReward = 4;

    // 직전에 출력한 대사를 기억해 연속 호출 시 동일 라인을 회피하는 최소한의 반복 억제.
    private string _lastLine;
    private string _seenRequestId;
    private int _seenRequestDay = -1;
    private int _lastVillageResponseDay = -1;

    public string LastLine => _lastLine ?? string.Empty;
    public event System.Action<NpcDialogue, GameObject> GreetingInteracted;
    public int LastVillageResponseDay => _lastVillageResponseDay;

    // 이 주민과의 대화 기억(오늘 한 이야기·최근 문장·개인 이야기 진도). 세션 한정, 저장하지 않는다.
    readonly ResidentConversationMemory _conversation = new ResidentConversationMemory();
    public ResidentConversationMemory Conversation => _conversation;
    GameObject _lastInteractor;

    // 대화는 빈손으로만 시작된다. 들고 다가온 물건을 잠시 기억해 "그 설계도요…" 같은 말을 할 수 있게 한다.
    public void NoteHeldItem(Item held)
    {
        if (held == null) return;
        _conversation.NotedHeld = held;
        _conversation.NotedHeldAt = Time.unscaledTime;
    }

    // 런타임 NpcController 캐시 (결정론 RNG 시드 재사용)
    private NpcController _cachedController;
    private bool _controllerProbed;

    public struct ResidentRequestState
    {
        public RecipeData Recipe { get; }
        public Item Item { get; }
        public int RequiredCount { get; }
        public int OwnedCount { get; }
        public string ActivityId { get; }
        public bool CompletedToday { get; }
        public bool CanDeliver => !CompletedToday && OwnedCount >= RequiredCount;

        public ResidentRequestState(RecipeData recipe, Item item, int requiredCount,
            int ownedCount, string activityId, bool completedToday)
        {
            Recipe = recipe;
            Item = item;
            RequiredCount = requiredCount;
            OwnedCount = ownedCount;
            ActivityId = activityId;
            CompletedToday = completedToday;
        }
    }

    // -------- IInteractable --------

    // 플레이어가 [E]로 건 대화만 대화 구도(카메라 접근·서로 바라보기·이름표 하트)를 쓴다.
    // 손님 구매 반응(NpcController → SpeakTopic) 같은 혼잣말은 같은 대화창에 뜨되 카메라를 움직이지 않는다(2026-11-03 seat-r4).
    bool _playerTalk;

    public void Interact(GameObject interactor)
    {
        _playerTalk = true;
        try { InteractInner(interactor); }
        finally { _playerTalk = false; }
    }

    void InteractInner(GameObject interactor)
    {
        _lastInteractor = interactor;
        if (IsResidentRequestWindow() && TryGetResidentRequest(out ResidentRequestState request))
        {
            if (request.CompletedToday)
            {
                ShowLine("오늘 필요한 재료는 이미 받았어요. 고마워요!", DialogueTopic.Economy);
                return;
            }

            if (request.CanDeliver)
            {
                TryDeliverResidentRequest(interactor);
                return;
            }

            MarkRequestSeen(request.ActivityId);
            // 앞말은 역할다운 요청 이유(ResidentContextDialogue), 없으면 기존 Economy 풀.
            string toneLine = ResidentContextDialogue.RequestIntro(this, request.Item)
                ?? ResolveTopicLine(DialogueTopic.Economy, warnIfMissing: false);
            string requestLine = $"요청: {RequestItemName(request)} x{request.RequiredCount} · 보유 {request.OwnedCount}/{request.RequiredCount}\n낮에 재료를 준비한 뒤 다시 이야기하세요.";
            string combinedLine = string.IsNullOrWhiteSpace(toneLine)
                ? requestLine
                : $"{toneLine}\n{requestLine}";
            _lastLine = combinedLine;
            ShowLine(combinedLine, DialogueTopic.Economy);
            GrantDailyDialoguePoints();
            return;
        }

        // The existing resident request remains authoritative and is evaluated
        // first. Residents without an active request can explain the exact sale
        // that changed their role area on the following day.
        if (TryShowVillageResponse())
        {
            GrantDailyDialoguePoints();
            return;
        }

        SpeakTopic(DialogueTopic.Greeting);
        GrantDailyDialoguePoints();
        if (!string.IsNullOrEmpty(_lastLine)) GreetingInteracted?.Invoke(this, interactor);
    }

    public string GetInteractPrompt()
    {
        NoteHeldItem(Inventory.instance != null ? EquipmentSystem.CurrentHeld(Inventory.instance.gameObject) : null);
        string name = ShortName;

        if (IsResidentRequestWindow() && TryGetResidentRequest(out ResidentRequestState request))
        {
            string itemName = RequestItemName(request);
            if (request.CompletedToday)
                return $"[{name}] 오늘 도움 완료";
            if (request.CanDeliver)
                return $"[{name}] {itemName} {request.OwnedCount}/{request.RequiredCount} · 건네기";
            if (HasSeenRequest(request.ActivityId))
                return $"[{name}] {itemName} {request.OwnedCount}/{request.RequiredCount}";
            return $"[{name}] 요청 확인 · {itemName} {request.OwnedCount}/{request.RequiredCount}";
        }

        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (_lastVillageResponseDay != day &&
            VillageCultureVisualController.Instance != null &&
            VillageCultureVisualController.Instance.TryBuildResidentResponse(gameObject, out _))
            return $"[{name}] 어제 판매로 달라진 마을 이야기";

        return TalkPrompt(name);
    }

    // 기본/동행 문구("대화하기", "미라 · TEMP와 대화")는 이름과 조사를 맞춰 "로건과 대화하기"로 보인다.
    // 그 밖의 직접 지정 문구("인사하기")는 기존 형식 "[보리] 인사하기"를 유지한다.
    string TalkPrompt(string name)
    {
        string prompt = string.IsNullOrWhiteSpace(interactPrompt) ? string.Empty : interactPrompt.Trim();
        if (prompt.Length == 0 || prompt == "대화하기" || prompt == "대화" || prompt.Contains("TEMP") ||
            prompt.EndsWith("와 대화") || prompt.EndsWith("과 대화"))
            return $"{ResidentNames.WithJosa(name, "와")} 대화하기";
        return $"[{name}] {prompt}";
    }

    // -------- 공개 API (다른 시스템에서 호출) --------

    /// <summary>topic 에 해당하는 라인 하나를 골라 DialogueUI 로 출력한다. 상황 대사가 있으면 그것을 우선한다.</summary>
    public void SpeakTopic(DialogueTopic topic)
    {
        string line = ResolveContextLine(topic) ?? ResolveTopicLine(topic, warnIfMissing: true);

        if (string.IsNullOrEmpty(line))
        {
            Debug.Log($"💬 {DisplayName}: ... (대사 풀 비어 있음: {topic})");
            return;
        }

        _lastLine = line;
        ShowLine(line, topic);
    }

    // 전문 주민의 실제 담당 레시피에서 오늘 요청할 첫 유효 재료를 파생한다.
    public bool TryGetResidentRequest(out ResidentRequestState state)
    {
        state = default;

        SpecialistNpcController specialist = GetComponent<SpecialistNpcController>();
        if (specialist == null || specialist.assignedRecipes == null)
            return false;

        WorkbenchType expectedWorkbench = NpcSpecialtyMapping.GetWorkbenchType(specialist.specialty);
        if (expectedWorkbench == WorkbenchType.None)
            return false;

        foreach (RecipeData recipe in specialist.assignedRecipes)
        {
            if (recipe == null || recipe.outputItem == null || recipe.ingredients == null)
                continue;
            if (recipe.requiredWorkbench != expectedWorkbench)
                continue;
            if (TierService.Instance != null && !TierService.Instance.IsUnlocked(recipe.requiredTier))
                continue;
            if (FriendshipService.Instance != null && !FriendshipService.Instance.IsRecipeUnlocked(recipe))
                continue;

            RecipeIngredient ingredient = null;
            foreach (RecipeIngredient candidate in recipe.ingredients)
            {
                if (candidate != null && candidate.item != null && candidate.count > 0)
                {
                    ingredient = candidate;
                    break;
                }
            }

            if (ingredient == null)
                continue;

            int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            string residentId = ResolveResidentId();
            string activityId = $"resident-request:{day}:{residentId}:{recipe.name}:{ingredient.item.id}";
            int ownedCount = Inventory.instance != null ? Inventory.instance.CountItems(ingredient.item) : 0;
            bool completed = DayNightShopLoopController.Instance != null
                && DayNightShopLoopController.Instance.IsDailyActivityCompleted(activityId);

            state = new ResidentRequestState(recipe, ingredient.item, ingredient.count,
                ownedCount, activityId, completed);
            return true;
        }

        return false;
    }

    // 준비된 재료를 정확히 한 번 차감하고 기존 당일 활동 저장 경로에 완료를 기록한다.
    public bool TryDeliverResidentRequest(GameObject interactor)
    {
        if (!IsResidentRequestWindow()
            || !TryGetResidentRequest(out ResidentRequestState request)
            || request.CompletedToday
            || Inventory.instance == null
            || !Inventory.instance.HasItems(request.Item, request.RequiredCount))
            return false;

        Inventory.instance.RemoveItems(request.Item, request.RequiredCount);
        if (!DayNightShopLoopController.Instance.TryCompleteDailyActivity(request.ActivityId))
        {
            Debug.LogError($"[ResidentRequest] {request.ActivityId} 재료 차감 후 당일 완료 기록에 실패했습니다.");
            return false;
        }

        MarkRequestSeen(request.ActivityId);
        bool rewarded = !string.IsNullOrEmpty(friendshipId)
            && FriendshipService.Instance != null
            && requestFriendshipReward > 0;
        if (rewarded)
            FriendshipService.Instance.AddPoints(friendshipId, requestFriendshipReward, "주민 재료 요청");

        string rewardText = rewarded ? $" · 친밀도 +{requestFriendshipReward}" : "";
        string line = $"{RequestItemName(request)} x{request.RequiredCount} 전달 완료! 오늘 도움을 기억할게요.{rewardText}";
        _lastLine = line;
        ShowLine(line, DialogueTopic.Economy);
        Debug.Log($"[ResidentRequest] 완료: {request.ActivityId}{rewardText}");
        return true;
    }

    /// <summary>현재 참조 중인 NpcProfile 을 반환한다 (override → NpcController → null).</summary>
    public NpcProfile ResolveProfile()
    {
        if (overrideProfile != null) return overrideProfile;

        if (!_controllerProbed)
        {
            _cachedController = GetComponent<NpcController>();
            _controllerProbed = true;
        }
        return _cachedController != null ? _cachedController.profile : null;
    }

    // 인사·잡담은 상황 조합기, 구매/보류 직후는 실제 상품·가격 반응. 조합 실패나 예외는 기존 풀로 폴백한다.
    private string ResolveContextLine(DialogueTopic topic)
    {
        try
        {
            switch (topic)
            {
                case DialogueTopic.Greeting:
                case DialogueTopic.SmallTalk:
                case DialogueTopic.Idle:
                    return ResidentContextDialogue.TryComposeGreeting(this, _lastInteractor, out string talk) ? talk : null;
                case DialogueTopic.ShopBought:
                case DialogueTopic.ShopTooExpensive:
                    return ResidentContextDialogue.TryComposeShopReaction(this, topic, out string reaction) ? reaction : null;
                default:
                    return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
            return null;
        }
    }

    private string ResolveTopicLine(DialogueTopic topic, bool warnIfMissing)
    {
        if (dialogueData == null)
        {
            if (warnIfMissing)
                Debug.LogWarning($"💬 {gameObject.name}: DialogueData가 비어 있습니다.");
            return null;
        }

        NpcProfile profile = ResolveProfile();
        string line = DialogueService.GetLineFor(profile, dialogueData, topic);
        if (!string.IsNullOrEmpty(line) && line == _lastLine)
        {
            string retry = DialogueService.GetLineFor(profile, dialogueData, topic);
            if (!string.IsNullOrEmpty(retry)) line = retry;
        }

        return line;
    }

    private bool IsResidentRequestWindow()
    {
        return DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.DayPreparation;
    }

    private string ResolveResidentId()
    {
        if (!string.IsNullOrWhiteSpace(friendshipId))
            return friendshipId.Trim();

        NpcProfile profile = ResolveProfile();
        if (profile != null && !string.IsNullOrWhiteSpace(profile.name))
            return profile.name.Trim();

        return gameObject.name;
    }

    private void GrantDailyDialoguePoints()
    {
        if (!string.IsNullOrEmpty(friendshipId) && FriendshipService.Instance != null)
        {
            FriendshipService.Instance.AddDialoguePoints(friendshipId);
            if (DialogueUI.TalkingWith(transform)) DialogueUI.instance.RefreshHearts(friendshipId);
        }
    }

    private bool TryShowVillageResponse()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (_lastVillageResponseDay == day || VillageCultureVisualController.Instance == null ||
            !VillageCultureVisualController.Instance.TryBuildResidentResponse(gameObject,
                out string response))
            return false;

        _lastVillageResponseDay = day;
        _lastLine = response;
        ShowLine(response, DialogueTopic.Economy);
        return true;
    }

    private void MarkRequestSeen(string activityId)
    {
        _seenRequestId = activityId;
        _seenRequestDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    }

    private bool HasSeenRequest(string activityId)
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        return _seenRequestDay == day && _seenRequestId == activityId;
    }

    // 화면용 재료 이름. 영문 키 이름(Wheat/Wood)은 데모 표시 이름으로, 한글 이름은 그대로(저장 키는 item.id).
    private static string RequestItemName(ResidentRequestState request)
    {
        string name = request.Item != null ? ResidentNames.ItemName(request.Item) : null;
        return !string.IsNullOrWhiteSpace(name) ? name : "재료";
    }

    // 플레이어 행동(선물 등)에 대한 대답: [E] 대화와 같은 대화 구도·이름표 하트로 보인다.
    public void SayToPlayer(string line)
    {
        _playerTalk = true;
        try { _lastLine = line; ShowLine(line, DialogueTopic.Greeting); }
        finally { _playerTalk = false; }
    }

    // -------- 출력 래퍼 (추후 UI 교체 지점) --------

    // §3 DialogueUI + FriendshipUI 연동
    private void ShowLine(string line, DialogueTopic topic)
    {
        if (DialogueUI.instance != null)
            DialogueUI.instance.Show(DisplayName, line, _playerTalk ? transform : null, _playerTalk ? friendshipId : null);
        else
            Debug.Log($"💬 [{DisplayName}] ({topic}) \"{line}\"");

        // 테마 대화창은 이름표에 하트로 친밀도를 보인다(옛 친밀도 막대는 겹치지 않게 띄우지 않는다).
        if (!string.IsNullOrEmpty(friendshipId) && !PAUiTheme.Active)
            FriendshipUI.instance?.ShowForNpc(friendshipId);
    }

    // 대화창 이름표("미라 · 농부")와 프롬프트용 짧은 이름("미라"). 내부 ID(Farmer_01)는 화면에 내보내지 않는다.
    // 프롬프트는 매 프레임 묻기 때문에 같은 프로필·같은 동행 구성에서는 한 번 푼 이름을 재사용한다.
    public string DisplayName { get { RefreshNames(); return _displayName; } }
    public string ShortName { get { RefreshNames(); return _shortName; } }
    string _displayName, _shortName;
    NpcProfile _namedProfile;
    bool _namedWithRoutine;

    void RefreshNames()
    {
        NpcProfile profile = ResolveProfile();
        bool routine = GetComponent<DemoCompanionRoutine>() != null;
        if (_shortName != null && profile == _namedProfile && routine == _namedWithRoutine) return;
        _namedProfile = profile;
        _namedWithRoutine = routine;
        _shortName = ResidentNames.ShortName(gameObject, profile, dialogueData);
        _displayName = ResidentNames.Display(gameObject, profile, dialogueData);
    }
}
