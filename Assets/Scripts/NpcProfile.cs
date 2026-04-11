using UnityEngine;

// NPC의 "정체성" 정적 데이터. ScriptableObject로 관리하여 코드 수정 없이
// 새로운 MBTI 조합 NPC를 추가할 수 있다. (NFR-02 확장성 요구사항 반영)
//
// 설계 의도 (Docs/03 매핑):
// - MBTI 16종을 개별 enum으로 관리하지 않고, 4축을 [-1, +1] 연속값으로 저장한다.
//   이렇게 하면 "완전한 E"와 "살짝 E 성향" 같은 미세 조정이 가능하고,
//   AI 연산 시 가중치 곱셈으로 깔끔하게 처리된다.
// - MVP에서는 EI/SN 두 축만 실제 AI 연산에 쓰이고, TF/JP는 대사 톤 분기 용도로 사용한다.
//   데이터 구조는 4축 전부 보관하므로 나중에 연산 대상 확장 시 ScriptableObject 에셋을 재작성할 필요 없다.
// - 파생 가중치(workEfficiency 등)는 기획서 Docs/03 테이블에 명시된 변수들이며,
//   MBTI 축에서 자동 계산하지 않고 디자이너가 개별 NPC별로 조정할 수 있도록 명시 필드로 둔다.
[CreateAssetMenu(fileName = "New NPC Profile", menuName = "P.A. System/NPC Profile")]
public class NpcProfile : ScriptableObject
{
    [Header("기본 정보")]
    public string npcName = "Unnamed";
    [TextArea] public string bio;

    [Header("MBTI 4축 (-1 ~ +1)")]
    [Tooltip("-1 = 완전 I, +1 = 완전 E. 광장 체류/쇼핑 확률에 반영")]
    [Range(-1f, 1f)] public float traitEI = 0f;

    [Tooltip("-1 = 감각(S, 실용재 선호), +1 = 직관(N, 사치품 선호)")]
    [Range(-1f, 1f)] public float traitSN = 0f;

    [Tooltip("-1 = 사고(T, 논리적 대사), +1 = 감정(F, 감성적 대사). MVP에서는 대사 톤에만 영향")]
    [Range(-1f, 1f)] public float traitTF = 0f;

    [Tooltip("-1 = 판단(J, 일과 엄격), +1 = 인식(P, 돌발 행동). MVP에서는 FSM drift 에만 영향")]
    [Range(-1f, 1f)] public float traitJP = 0f;

    [Header("파생 행동 가중치")]
    [Tooltip("작업 상태에 있을 때의 생산 효율 계수. 1.0 = 기본")]
    public float workEfficiency = 1f;

    [Tooltip("광장/사교 공간을 선호하는 정도. 0~1")]
    [Range(0f, 1f)] public float socialWeight = 0.5f;

    [Tooltip("주거지 체류 시간 비율. 0~1 (1 = 거의 집에만)")]
    [Range(0f, 1f)] public float homeStayBias = 0.3f;

    [Header("소비 가중치 (Docs/03 2.1절)")]
    [Tooltip("실용재(도구/식재료) 선호 가중치. S 성향이 높이는 계수")]
    public float utilityConsumption = 1f;

    [Tooltip("사치품(가구/장식) 선호 가중치. N 성향이 높이는 계수")]
    public float luxuryConsumption = 1f;

    [Tooltip("가격 변화에 대한 저항(민감도). 높을수록 비싸면 금방 구매 포기")]
    public float priceSensitivity = 1f;
}
