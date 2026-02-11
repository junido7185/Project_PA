[System.Serializable] // Inspector에서 보이게 추가
public class InventorySlot
{
    public Item item;
    public int count;
    public bool IsEmpty => item == null;
}