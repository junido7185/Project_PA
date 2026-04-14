using UnityEngine;

// 고용 후보 NPC 의 정적 데이터 — ScriptableObject.
// Docs/05 §2 "NPC 채용" 에서 설명한 "후보 풀 → 선택 고용" 흐름의 데이터 단위.
//
// 역할:
// - HiringService 가 보관하는 후보 리스트의 한 항목.
// - 고용 비용 / 필요 티어 / 스폰할 프리팹 / MBTI 프로필 / 스페셜티 를 한 묶음으로 보유한다.
// - 고용이 확정되면 HiringService 가 spawnPrefab 을 Instantiate 한 뒤
//   필요하면 NpcController / ProducerNpcController / SpecialistNpcController 의 profile 을 덮어쓴다.
//
// 사용법:
//   Assets > Create > P.A. System > NPC Candidate Data 로 에셋을 만들고
//   HiringService.availableCandidates 리스트에 드래그한다.
//   같은 스페셜티의 프로필/프리팹 변형을 여러 개 등록해 "채용 후보" 가 여러 명 보이게 할 수 있다.
[CreateAssetMenu(fileName = "New NPC Candidate", menuName = "P.A. System/NPC Candidate Data")]
public class NpcCandidateData : ScriptableObject
{
    [Header("후보 프로필")]
    [Tooltip("고용 후보의 정체성 (MBTI·가중치). NpcController/Producer/Specialist 의 profile 로 주입된다.")]
    public NpcProfile profile;

    [Tooltip("이 후보가 가진 전문 분야. 고용 후 SpecialistNpcController 가 작업대 매칭에 사용한다.")]
    public NpcSpecialty specialty = NpcSpecialty.None;

    [Header("고용 조건")]
    [Tooltip("고용에 필요한 일회성 비용 (Gold). EconomyService.TrySpend 로 차감된다.")]
    public int hireCost = 500;

    [Tooltip("이 후보를 표시하기 위한 최소 티어. TierService.IsUnlocked 가 false 면 후보 목록에서 제외된다.")]
    public int requiredTier = 0;

    [Header("스폰")]
    [Tooltip("고용 확정 시 Instantiate 할 NPC 프리팹. NpcController / ProducerNpcController / SpecialistNpcController 중 적합한 것이 붙어 있어야 한다.")]
    public GameObject spawnPrefab;

    [Header("UI/로그 표시")]
    [Tooltip("후보 목록·로그에 표시될 이름. 비어 있으면 profile.npcName 을 사용.")]
    public string displayName = "";

    [TextArea]
    [Tooltip("후보 소개문 — UI 카드 본문에 표시.")]
    public string bio = "";

    /// <summary>UI/로그에 표시할 이름 (displayName 우선, 없으면 profile.npcName, 없으면 에셋명).</summary>
    public string ResolveDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        if (profile != null && !string.IsNullOrEmpty(profile.npcName)) return profile.npcName;
        return name;
    }
}
