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

    // 직전에 출력한 대사를 기억해 연속 호출 시 동일 라인을 회피하는 최소한의 반복 억제.
    private string _lastLine;

    // 런타임 NpcController 캐시 (결정론 RNG 시드 재사용)
    private NpcController _cachedController;
    private bool _controllerProbed;

    // -------- IInteractable --------

    public void Interact(GameObject interactor)
    {
        SpeakTopic(DialogueTopic.Greeting);

        // 대화 성공 시 친밀도 가산 (ID 가 설정되어 있을 때만)
        if (!string.IsNullOrEmpty(friendshipId) && FriendshipService.Instance != null)
            FriendshipService.Instance.AddDialoguePoints(friendshipId);
    }

    public string GetInteractPrompt()
    {
        string name = ResolveProfile() is NpcProfile p && !string.IsNullOrEmpty(p.npcName) ? p.npcName : gameObject.name;
        return $"[{name}] {interactPrompt}";
    }

    // -------- 공개 API (다른 시스템에서 호출) --------

    /// <summary>topic 에 해당하는 라인 하나를 꺼내 Debug.Log (추후 UI) 로 출력한다.</summary>
    public void SpeakTopic(DialogueTopic topic)
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"💬 {gameObject.name}: DialogueData 가 비어 있습니다.");
            return;
        }

        NpcProfile profile = ResolveProfile();
        string line = DialogueService.GetLineFor(profile, dialogueData, topic);

        // 직전과 동일하면 한 번 더 뽑아서 반복 회피 (최소한의 UX 보정)
        if (!string.IsNullOrEmpty(line) && line == _lastLine)
        {
            string retry = DialogueService.GetLineFor(profile, dialogueData, topic);
            if (!string.IsNullOrEmpty(retry)) line = retry;
        }

        if (string.IsNullOrEmpty(line))
        {
            Debug.Log($"💬 {DisplayName}: ... (대사 풀 비어 있음: {topic})");
            return;
        }

        _lastLine = line;
        ShowLine(line, topic);
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
