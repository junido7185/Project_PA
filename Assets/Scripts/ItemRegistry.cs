using UnityEngine;
using System.Collections.Generic;

// 역할:
// - 씬 내 유일한 Item(ScriptableObject) 도감 싱글톤.
// - DefaultExecutionOrder(-70) — EconomyService(-100) · TierService(-90) · GameClock(-80) 이후,
//   나머지 MonoBehaviour 보다 먼저 초기화된다.
// - SaveManager 가 SlotSaveData.itemId / itemName 으로 Item 원형을 복원할 때 사용한다.
//
// 씬 설정:
//   빈 GameObject 에 이 컴포넌트를 붙이고, allItems 에 프로젝트 내 모든 Item 에셋을 등록한다.
[DefaultExecutionOrder(-70)]
public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance { get; private set; }

    [Tooltip("게임 내 모든 Item ScriptableObject 를 여기에 등록한다.")]
    public List<Item> allItems = new List<Item>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // id 우선, 이름 폴백으로 Item 원형을 반환한다.
    // id == 0 이거나 id 중복 등록된 경우에도 itemName 으로 보완한다.
    public Item Find(int id, string itemName)
    {
        // 1. id 로 검색 (0은 미할당 sentinel 이므로 스킵)
        if (id != 0)
        {
            foreach (var item in allItems)
                if (item != null && item.id == id) return item;
        }

        // 2. 이름 폴백
        if (!string.IsNullOrEmpty(itemName))
        {
            foreach (var item in allItems)
                if (item != null && item.itemName == itemName) return item;
        }

        return null;
    }
}
