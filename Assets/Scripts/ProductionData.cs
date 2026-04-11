using UnityEngine;

// 생산형 NPC 가 만들어 내는 품목의 명세 ScriptableObject.
//
// 사용법:
//   Assets > Create > P.A. System > Production Data 로 에셋을 만든다.
//   ProducerNpcController.productionData 에 드래그 드롭한다.
//
// 설계 의도:
//   코드 한 줄 건드리지 않고 생산 품목/속도/가격을 Inspector 에서 조정할 수 있다.
//   농부(밀/감자), 광부(철/금), 벌목꾼(원목/수지) 등 직업마다 별도 에셋을 만들면 된다.
[CreateAssetMenu(fileName = "New Production Data", menuName = "P.A. System/Production Data")]
public class ProductionData : ScriptableObject
{
    [Header("생산 아이템")]
    [Tooltip("이 에셋을 가진 NPC 가 생산하는 아이템")]
    public Item producedItem;

    [Header("생산 파라미터")]
    [Tooltip("기본 1사이클 소요 시간(초). NpcProfile.workEfficiency·traitEI 로 보정된다.\n" +
             "실제 간격 = baseInterval ÷ (workEfficiency × (1 + 0.3 × (-traitEI)))\n" +
             "  I형(traitEI=-1) → 30% 빠름 / E형(+1) → 30% 느림")]
    public float baseProductionInterval = 30f;

    [Tooltip("한 사이클에 생산되는 기본 개수.\n" +
             "실제 개수 = max(1, round(amount × workEfficiency × (1 + 0.2 × (-traitJP))))\n" +
             "  J형(traitJP=-1) → 20% 더 생산 / P형(+1) → 20% 덜 생산")]
    public int baseProductionAmount = 1;

    [Header("납품 파라미터")]
    [Tooltip("NPC 인벤토리에 이 개수 이상 쌓이면 납품 이동을 시작한다")]
    public int deliveryThreshold = 3;

    [Tooltip("NPC 인벤토리 최대 보관 개수. 이를 초과하면 생산을 멈춘다.")]
    public int maxInventoryCount = 10;

    [Header("납품 단가")]
    [Tooltip("플레이어에게 납품할 때 아이템 1개당 플레이어 지갑에서 차감되는 금액.\n" +
             "0이면 producedItem.basePrice 를 그대로 사용한다.")]
    public int overrideDeliveryPrice = 0;

    /// <summary>실제 납품 단가 (override가 0이면 item.basePrice 폴백).</summary>
    public int EffectiveDeliveryPrice =>
        overrideDeliveryPrice > 0 ? overrideDeliveryPrice :
        (producedItem != null ? producedItem.basePrice : 0);
}
