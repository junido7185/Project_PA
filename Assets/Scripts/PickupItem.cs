using UnityEngine;

namespace InventoryFramework
{
    public class PickupItem : MonoBehaviour, IInteractable
    {
        public Item item;
        public int amount;
        public bool contextual;
        // 직접 채집 드롭: 실제로 가방에 들어간 뒤에만 자원 소모를 확정한다(줍기 전 저장/이어하기 유실 방지).
        public System.Action Claimed;
        bool _claimed;
        public void Interact(GameObject interactor)
        {
            if (!contextual || _claimed || interactor == null || EquipmentSystem.CurrentHeld(interactor) != null) return;
            var inventory = interactor.GetComponent<Inventory>();
            // 빈손 줍기는 선택된 빈 칸에 들어가면 바로 손에 들려 다음 물건을 못 줍는다(2026-11-03 beach-r8: 두 번째 조개 실패).
            // 선택된 칸이 비어 있었으면 받은 물건을 다른 빈 칸으로 옮겨 손을 비운 채 이어 줍게 한다.
            var hotbarSlots = inventory != null && inventory.hotbar != null ? inventory.hotbar.slots : null;
            int selected = inventory != null ? inventory.selectedHotbarIndex : -1;
            bool selectedWasEmpty = hotbarSlots != null && selected >= 0 && selected < hotbarSlots.Count && hotbarSlots[selected].IsEmpty;
            if (inventory == null || !inventory.TryReceiveToHotbar(item, amount))
            { FirstDayWorldPresentation.Toast("가방이 가득 찼어요. 자리를 비워주세요."); return; }
            if (selectedWasEmpty && !hotbarSlots[selected].IsEmpty)
            {
                var other = hotbarSlots.Find(s => s.IsEmpty);
                if (other != null) { other.SetInstance(hotbarSlots[selected].instance); hotbarSlots[selected].Clear(); inventory.RefreshAllUI(); }
            }
            _claimed = true;
            GatherFeedback.Pickup();
            Claimed?.Invoke();
            FirstDayWorldPresentation.Acquired(item, amount, transform.position);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        public string GetInteractPrompt() => item != null ? ItemDisplayName.For(item) + (amount > 1 ? " ×" + amount : "") + " 줍기" : "줍기";

        void OnCollisionEnter(Collision collision)
        {
            if (contextual) return;
            Debug.Log(collision.gameObject);

            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<ItemPickupHandler>().PickupItem(item, amount);
                Destroy(this.gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (contextual) return;
            if (other.gameObject.CompareTag("Player"))
            {
                other.gameObject.GetComponent<ItemPickupHandler>().PickupItem(item, amount);
                Destroy(this.gameObject);
            }
        }
    }
}

