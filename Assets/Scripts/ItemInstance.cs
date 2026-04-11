using System;
using UnityEngine;

// 인벤토리/진열대에 실제 존재하는 "스택"의 동적 상태.
// Docs/02 "ItemInstance" 설계 반영 — 스택 단위 Guid를 가지며,
// quality/currentPrice 등 원형(ItemData = Item.cs)과 분리된 런타임 상태를 보관한다.
// MBTI 기반 가격 책정(DisplayPrice) 및 가공 품질 보정의 자리가 된다.
[Serializable]
public class ItemInstance
{
    public string instanceId;   // 스택 식별자 (Guid)
    public Item data;           // ScriptableObject 원형 참조
    public int count;
    public float quality = 1f;  // 가공 품질 보정 계수 (1.0 = 기본)

    // 사용자가 책정한 진열 가격. 0 이하이면 data.basePrice를 사용한다.
    // (JsonUtility가 Nullable<int>을 직렬화하지 못하므로 sentinel=0을 사용)
    public int currentPrice = 0;

    public ItemInstance(Item data, int count = 1)
    {
        this.instanceId = Guid.NewGuid().ToString();
        this.data = data;
        this.count = count;
    }

    // 시스템이 사용하는 유효 가격 — 사용자 책정가가 없으면 basePrice 사용.
    public int EffectivePrice => currentPrice > 0 ? currentPrice : (data != null ? data.basePrice : 0);

    // 같은 원형이고 가격/품질이 일치할 때만 스택 병합을 허용한다.
    // (가격이 다르면 사실상 "다른 상품"이므로 스택을 나눠야 함)
    public bool CanStackWith(ItemInstance other)
    {
        if (other == null || data == null || other.data == null) return false;
        if (data != other.data) return false;
        if (!Mathf.Approximately(quality, other.quality)) return false;
        if (currentPrice != other.currentPrice) return false;
        return true;
    }
}
