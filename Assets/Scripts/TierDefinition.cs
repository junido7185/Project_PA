using UnityEngine;

// 각 등급의 명세를 담는 ScriptableObject.
//
// 사용법:
//   Assets > Create > P.A. System > Tier Definition 으로 에셋 5개를 만든다.
//   tier 필드를 0~4로 설정하고, TierService.tierDefinitions 리스트에 순서대로 등록한다.
//
// 승급 조건 (이 tier 값의 등급으로 '올라오기 위해' 필요한 조건):
//   - Tier 0 : 조건 없음 (시작 등급)
//   - Tier 1 : requiredCumulativeRevenue = 10000  (초기 대출 상환 기준)
//   - Tier 2 : requiredCumulativeRevenue = 100000 (매출 10만 G)
//   - Tier 3 : requiredReputation = 3             (마을 평판 Lv.3)
//   - Tier 4 : requiresManualApproval = true      (본사 감사 통과 이벤트)
[CreateAssetMenu(fileName = "TierDef_Tier0", menuName = "P.A. System/Tier Definition")]
public class TierDefinition : ScriptableObject
{
    [Header("등급 정보")]
    [Tooltip("0~4. TierService 의 리스트 검색 키로 사용된다.")]
    public int tier = 0;

    [Tooltip("화면에 표시될 등급 명칭 (예: 생존자, 지점장)")]
    public string tierName = "생존자";

    [TextArea]
    public string description;

    [Header("승급 조건 (이 티어로 올라오기 위해 충족해야 할 조건)")]
    [Tooltip("누적 총매출 기준. 0이면 매출 조건 없음.")]
    public long requiredCumulativeRevenue = 0;

    [Tooltip("마을 평판 최솟값. 0이면 평판 조건 없음.")]
    public int requiredReputation = 0;

    [Tooltip("true면 자동 승급 불가 — TierService.TryManualAdvance() 로만 승급됨 (본사 감사 등 이벤트 전용).")]
    public bool requiresManualApproval = false;

    [Header("해금 내용 (UI 표시용)")]
    [TextArea]
    [Tooltip("이 등급에 도달했을 때 해금되는 건물/기능 설명")]
    public string unlockDescription;
}
