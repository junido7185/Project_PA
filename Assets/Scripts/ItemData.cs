using UnityEngine;

// 도구 종류를 정의하는 열거형 (Enum)
public enum ToolType
{
    None,       // 도구 아님 (재료 등)
    Axe,        // 도끼 (나무용)
    Pickaxe,    // 곡괭이 (광석용)
    Weapon      // 무기 (전투용)
}

// 이 스크립트는 게임 오브젝트에 붙이는 게 아니라,
// 프로젝트 창에서 '데이터 파일'로 만들어낼 수 있게 합니다.
[CreateAssetMenu(fileName = "New Item", menuName = "P.A. System/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;       // 아이템 이름 (예: 통나무)
    public Sprite icon;           // 아이템 아이콘 (이미지)
    public int basePrice;         // 기본 판매 가격
    public bool isStackable;      // 겹쳐지는 아이템인가? (장비는 false, 재료는 true)
    public ToolType toolType = ToolType.None; // 이 아이템의 도구 타입
    
    [TextArea]
    public string description;    // 아이템 설명
}