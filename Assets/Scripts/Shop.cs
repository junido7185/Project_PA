using System.Collections.Generic;
using UnityEngine;

// 상점 건물. 역할 2가지:
// 1. 자식 ShopSlot 들의 컨테이너 — NPC 가 GetAvailableSlots() 로 진열된 물건 목록을 조회한다.
// 2. TierService.OnTierAdvanced 를 구독해 상점 단계(슬롯 수)를 자동으로 진화시킨다 (Docs/05 §3).
//
// 슬롯 활성화 전략:
//   managedSlots 에 씬에 배치한 ShopSlot 을 최대 슬롯 수만큼 등록해 둔다.
//   TierDefinition.shopSlotCount 에 따라 앞에서부터 순서대로 활성화된다.
//   ShopSlot 은 Awake 에서 _slots 에 자기 자신을 등록하므로,
//   모든 ShopSlot 을 씬에서 활성 상태로 시작시키면 등록이 완료된다.
//   비활성 슬롯은 GetAvailableSlots() 의 activeSelf 필터로 NPC 에게 노출되지 않는다.
public class Shop : MonoBehaviour, IInteractable
{
    // 자식 ShopSlot 들이 Awake 시점에 자기 자신을 여기에 등록한다.
    private readonly List<ShopSlot> _slots = new List<ShopSlot>();
    public IReadOnlyList<ShopSlot> Slots => _slots;

    [Header("티어 기반 슬롯 관리 (Docs/05 §3)")]
    [Tooltip("씬에 배치한 ShopSlot 오브젝트를 활성화할 순서대로 나열한다.\n" +
             "모든 슬롯을 씬에서 활성 상태로 시작시켜 자동 등록을 허용한다.\n" +
             "티어 진화 시 앞에서부터 shopSlotCount 개만큼 켜지고 나머지는 꺼진다.")]
    public List<ShopSlot> managedSlots = new List<ShopSlot>();

    void Start()
    {
        if (TierService.Instance != null)
            TierService.Instance.OnTierAdvanced += OnTierAdvanced;

        // 씬 로드 직후 현재 티어 즉시 반영
        ApplyTierSlots();
    }

    void OnDestroy()
    {
        if (TierService.Instance != null)
            TierService.Instance.OnTierAdvanced -= OnTierAdvanced;
    }

    private void OnTierAdvanced(int _oldTier, int newTier) => ApplyTierSlots();

    // TierService 에서 얻은 유효 슬롯 수만큼 managedSlots 의 앞부분을 활성화한다.
    // shopSlotCount=0 이면 이전 티어 값 캐스케이드 (GetEffectiveShopSlotCount 처리).
    private void ApplyTierSlots()
    {
        if (managedSlots == null || managedSlots.Count == 0) return;
        if (TierService.Instance == null) return;

        int tier       = TierService.Instance.CurrentTier;
        int slotCount  = TierService.Instance.GetEffectiveShopSlotCount(tier);
        if (slotCount <= 0) return;  // TierDefinition 에 shopSlotCount 미설정 — 변화 없음

        for (int i = 0; i < managedSlots.Count; i++)
        {
            if (managedSlots[i] == null) continue;
            managedSlots[i].gameObject.SetActive(i < slotCount);
        }

        // 단계 이름 — 현재 또는 가장 최근 tier 에서 설정된 shopStageName 을 사용
        string stageName = ResolveStageName(tier);
        Debug.Log($"🏪 상점 진화: [{stageName}] 슬롯 {slotCount}개 활성화 (Tier {tier})");
    }

    // tier 이하에서 설정된 가장 최근 shopStageName 을 반환한다.
    private string ResolveStageName(int tier)
    {
        for (int t = tier; t >= 0; t--)
        {
            TierDefinition def = TierService.Instance.GetDefinition(t);
            if (def != null && !string.IsNullOrEmpty(def.shopStageName))
                return def.shopStageName;
        }
        return $"Tier {tier}";
    }

    public void RegisterSlot(ShopSlot slot)
    {
        if (slot != null && !_slots.Contains(slot)) _slots.Add(slot);
    }

    public void UnregisterSlot(ShopSlot slot)
    {
        _slots.Remove(slot);
    }

    // NPC 가 둘러볼 수 있는 "물건이 진열된" 슬롯만 반환한다.
    // 비활성(SetActive false) 슬롯은 현재 티어에서 잠긴 슬롯이므로 제외한다.
    public List<ShopSlot> GetAvailableSlots()
    {
        var result = new List<ShopSlot>();
        foreach (var s in _slots)
        {
            if (s != null && !s.IsEmpty && s.gameObject.activeSelf) result.Add(s);
        }
        return result;
    }

    // -------- 플레이어 직접 상호작용 (디버그 일괄 판매) --------

    public void Interact(GameObject interactor)
    {
        Debug.Log("🏪 상점 주인: 어서오세요!");
        SellAllItems();
    }

    public string GetInteractPrompt()
    {
        return "모두 판매하기 (디버그)";
    }

    public void SellAllItems()
    {
        Inventory inventory = Inventory.instance;
        int totalEarnings = 0;
        int soldCount = 0;

        soldCount += SellFromSlots(inventory.slots, ref totalEarnings);
        if (inventory.hotbar != null)
        {
            soldCount += SellFromSlots(inventory.hotbar.slots, ref totalEarnings);
        }

        if (soldCount > 0)
        {
            if (EconomyService.Instance != null)
            {
                EconomyService.Instance.Deposit(totalEarnings, "Shop.SellAllItems (디버그)");
            }
            Debug.Log($"💵 정산 완료! {soldCount}개 판매, 수익: {totalEarnings}G");
            inventory.RefreshAllUI();
        }
        else
        {
            Debug.Log("🚫 판매할 수 있는 아이템(자원)이 없습니다.");
        }
    }

    private int SellFromSlots(List<InventorySlot> slots, ref int earnings)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item.toolType == ToolType.None && slot.item.category != ItemCategory.Tool)
            {
                // ItemInstance 의 EffectivePrice 를 사용해 향후 가격/품질 보정을 자동 반영한다.
                earnings += slot.instance.EffectivePrice * slot.count;
                count += slot.count;
                slot.Clear();
            }
        }
        return count;
    }
}
