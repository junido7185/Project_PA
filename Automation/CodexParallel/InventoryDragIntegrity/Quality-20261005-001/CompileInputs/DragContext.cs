public static class DragContext
{
    public static Item draggedItem;
    public static ItemInstance draggedInstance;
    public static int draggedCount;
    public static int fromSlotIndex;
    public static SlotOwner fromOwner;

    public static void Clear()
    {
        draggedItem = null;
        draggedInstance = null;
        draggedCount = 0;
        fromSlotIndex = -1;
    }
}
