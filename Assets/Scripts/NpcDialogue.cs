using UnityEngine;

// NPC 와 대화하기 위한 IInteractable 진입점.
//
// 설계 의도:
// - PlayerInteraction Raycast 가 IInteractable 로 감지 → Space 키에 Interact() 호출.
// - 대사 풀 자체는 DialogueData(SO) 가 보관, 실제 선택 규칙은 DialogueService 가 담당.
// - NpcController 가 같은 GameObject 에 붙어 있으면 NpcProfile 을 자동 참조 → T/F 톤 분기에 사용.
// - UI 가 아직 없는 스프린트 단계이므로 출력은 Debug.Log 로만. 추후 DialogueBubbleUI 가 생기면
//   이 파일의 ShowLine() 한 곳만 수정해 화면 말풍선으로 전환할 수 있다 (Docs/06 NFR 유지보수성).
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
    public int LastVillageResponseDay => _lastVillageResponseDay;

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

    public void Interact(GameObject interactor)
    {
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
            string toneLine = ResolveTopicLine(DialogueTopic.Economy, warnIfMissing: false);
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
    }

    public string GetInteractPrompt()
    {
        string name = ResolveProfile() is NpcProfile p && !string.IsNullOrEmpty(p.npcName) ? p.npcName : gameObject.name;

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

        return $"[{name}] {interactPrompt}";
    }

    // -------- 공개 API (다른 시스템에서 호출) --------

    /// <summary>topic 에 해당하는 라인 하나를 꺼내 Debug.Log (추후 UI) 로 출력한다.</summary>
    public void SpeakTopic(DialogueTopic topic)
    {
        string line = ResolveTopicLine(topic, warnIfMissing: true);

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
            FriendshipService.Instance.AddDialoguePoints(friendshipId);
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

    private static string RequestItemName(ResidentRequestState request)
    {
        return request.Item != null && !string.IsNullOrWhiteSpace(request.Item.itemName)
            ? request.Item.itemName
            : "재료";
    }

    // -------- 출력 래퍼 (추후 UI 교체 지점) --------

    // §3 DialogueUI + FriendshipUI 연동
    private void ShowLine(string line, DialogueTopic topic)
    {
        if (DialogueUI.instance != null)
            DialogueUI.instance.Show(DisplayName, line);
        else
            Debug.Log($"💬 [{DisplayName}] ({topic}) \"{line}\"");

        if (!string.IsNullOrEmpty(friendshipId))
            FriendshipUI.instance?.ShowForNpc(friendshipId);
    }

    private string DisplayName
    {
        get
        {
            var p = ResolveProfile();
            return p != null && !string.IsNullOrEmpty(p.npcName) ? p.npcName : gameObject.name;
        }
    }
}
