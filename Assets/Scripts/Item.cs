using UnityEngine;

// 도구 타입 정의 (기존 ItemData에 있던 것)
public enum ToolType
{
    None,       // 도구 아님
    Axe,        // 도끼
    Pickaxe,    // 곡괭이
    Weapon,     // 무기
    Building,   // 건설
    Hoe,        // 괭이
    Seed        // 씨앗
}

// 경제 시스템용 카테고리. Docs/03의 MBTI 소비 패턴에 사용된다.
// - Raw: 원자재 (목재/광석/작물/생선). 가공 전 단계, 중립적.
// - Processed: 가공품 (음식/가구/의류). N 성향 소폭 선호.
// - Utility: 실용재 (소모품 도구/씨앗/비료). S 성향 강 선호.
// - Luxury: 사치품 (장식/예술/명품). N 성향 강 선호.
// - Tool: 플레이어 작업용 도구 — 상점에서 판매 불가.
public enum ItemCategory
{
    Raw,
    Processed,
    Utility,
    Luxury,
    Tool
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("기본 설정 (Framework)")]
    public int id;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int maxStack = 1;
    public GameObject model; // 드롭되거나 장착될 때 보일 모델

    // 👇 [통합된 기존 데이터]
    [Header("게임플레이 설정 (My Game)")]
    public int basePrice = 10;                // 판매 가격
    public ToolType toolType = ToolType.None; // 도구 타입

    [Header("경제 분류 (MBTI 소비 분기)")]
    [Tooltip("NPC가 이 아이템을 어떤 성향으로 판단할지 결정한다")]
    public ItemCategory category = ItemCategory.Raw;

    [Header("해금 조건 (Tier System)")]
    [Tooltip("이 아이템을 ShopSlot에 진열하려면 필요한 최소 티어 (0=무제한, 1=지점장 ...)")]
    public int requiredTier = 0;

    [Header("건설/농사 설정")]
    public BuildingData buildingToBuild; // 건설할 건물 데이터
    public GameObject cropPrefab;        // 심을 작물 프리팹
}