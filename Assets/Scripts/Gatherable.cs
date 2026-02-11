using UnityEngine;

public class Gatherable : MonoBehaviour, IInteractable
{
    public Item dropItem; // ⭐ ItemData -> Item
    public ToolType requiredTool;

    public void Interact(GameObject interactor)
    {
        Item heldItem = Inventory.instance.GetSelectedItem();

        if (requiredTool != ToolType.None)
        {
            if (heldItem == null || heldItem.toolType != requiredTool)
            {
                Debug.Log($"🚫 {requiredTool}이(가) 필요합니다.");
                return; 
            }
        }

        Animator playerAnim = interactor.GetComponentInChildren<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("DoChop");

        Harvest(); 
    }

    public string GetInteractPrompt() => $"{dropItem.itemName} 채집하기";
    
    public void Harvest()
    {
        if (dropItem != null)
        {
            Inventory.instance.AddItem(dropItem);
        }
        Destroy(gameObject);
    }
}