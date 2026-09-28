using UnityEngine;

// Two approved item bindings, shared by direct input and the existing world save path.
public static class WorldPlaceableKitCatalog
{
    public const string HubId = "pa-settlement-hub";
    public const string ShopId = "world-b01-market-stall-001";
    public const string HubItemPath = "Items/Item_ManagementHubKit";
    public const string ShopItemPath = "Items/Blueprints/Blueprint_B01_MarketStall";
    public static readonly WorldBuildingPlacementDefinition Hub = new WorldBuildingPlacementDefinition(
        "PA_SETTLEMENT_HUB_T0", "DepartureTutorial/Settlement/Hub",
        new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up, Vector2Int.one }, new Vector2Int(0, -1));
    public static readonly WorldBuildingPlacementDefinition Shop = new WorldBuildingPlacementDefinition(
        "B01_MARKET_STALL", "Buildings/Building_B01_MarketStall",
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, 2), new Vector2Int(0, 2), new Vector2Int(1, 2) }, new Vector2Int(0, -1));

    public static readonly string[] PersistedIds =
        { WorldBuildingPlacementService.PrototypeInstanceId, HubId, ShopId };

    public static bool TryDefinition(string id, out WorldBuildingPlacementDefinition definition)
    {
        definition = id == HubId ? Hub : id == ShopId ? Shop :
            id == WorldBuildingPlacementService.PrototypeInstanceId ? WorldBuildingPlacementService.StorageShedDefinition : null;
        return definition != null;
    }

    public static bool TryKit(Item item, out string id, out WorldBuildingPlacementDefinition definition)
    {
        id = null; definition = null;
        if (item == null || item.toolType != ToolType.Building || item.buildingToBuild == null) return false;
        if (item == Resources.Load<Item>(HubItemPath)) id = HubId;
        else if (item == Resources.Load<Item>(ShopItemPath)) id = ShopId;
        return TryDefinition(id, out definition) &&
            item.buildingToBuild == Resources.Load<BuildingData>(definition.BuildingResourcePath);
    }
}
