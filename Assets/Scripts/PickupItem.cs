using UnityEngine;

namespace InventoryFramework
{
    public class PickupItem : MonoBehaviour, IInteractable
    {
        public Item item;
        public int amount;
        public bool contextual;
        bool _claimed;
        public void Interact(GameObject interactor)
        {
            if (!contextual || _claimed || interactor == null || EquipmentSystem.CurrentHeld(interactor) != null) return;
            var inventory = interactor.GetComponent<Inventory>();
            if (inventory == null || !inventory.TryReceiveToHotbar(item, amount))
            { FirstDayWorldPresentation.Toast("가방이 가득 찼어요. 자리를 비워주세요."); return; }
            _claimed = true;
            FirstDayWorldPresentation.Acquired(item, amount, transform.position);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        public string GetInteractPrompt() => item != null ? item.itemName + " 줍기" : "줍기";

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

