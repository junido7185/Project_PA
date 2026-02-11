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

    [Header("건설/농사 설정")]
    public BuildingData buildingToBuild; // 건설할 건물 데이터
    public GameObject cropPrefab;        // 심을 작물 프리팹
}