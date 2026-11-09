using System;
using System.Linq;
using UnityEngine;

public enum DemoPlaceableKind { ShopBase, ResidentTent, Workbench, DisplayStand, Furniture }

[Serializable]
public sealed class DemoToolUpgrade
{
    public RecipeData recipe;
    public Item tool, starterTool;
    public DemoSpecialization root;
    public NpcSpecialty specialty;
    public ProductionData production;
    public string companionPrefix;
    public float npcEfficiency = 1.5f;
    public int playerWorkStrength = 2;
}

[Serializable]
public sealed class DemoPlaceableEntry
{
    public Item item;
    public string key, buildingResource;
    public DemoPlaceableKind kind;
    public Vector2Int size = Vector2Int.one;
    public Vector2Int[] footprint;
    public Vector2Int entrance = Vector2Int.down;
    public WorldPlaceableSurface surfaces;
    public bool canMove = true;
    public WorldBuildingPlacementDefinition Definition
    {
        get
        {
            var cells = footprint != null && footprint.Length > 0 ? footprint :
                Enumerable.Range(0, size.x * size.y).Select(i => new Vector2Int(i % size.x, i / size.x)).ToArray();
            return new WorldBuildingPlacementDefinition(key, buildingResource, cells,
                entrance, surfaces, canMove);
        }
    }
}

// Item and placement data, not a placement/inventory authority. Session-only demo ids
// deliberately stay out of the legacy world persistence catalog.
public sealed class DemoPlaceableCatalog : ScriptableObject
{
    public DemoPlaceableEntry[] entries;
    public DemoToolUpgrade[] upgrades;

    // Cached to avoid Resources.Load on every frame (GetInteractPrompt, RecipeUnlocked).
    static DemoPlaceableCatalog _cache;
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => _cache = null;

    public static DemoPlaceableCatalog Load()
    {
        if (_cache == null) _cache = Resources.Load<DemoPlaceableCatalog>("DemoStructure/Catalog");
        return _cache;
    }

    public DemoPlaceableEntry Find(Item item) => entries?.FirstOrDefault(e => e.item == item);
    public DemoPlaceableEntry Find(DemoPlaceableKind kind) => entries?.FirstOrDefault(e => e.kind == kind);
    public DemoToolUpgrade Upgrade(Item item) => upgrades?.FirstOrDefault(u => u.tool == item);
    // 전문 분야 잠금: 동행 도구 업그레이드의 root(None이면 누구나)와 도구 단계 표의 철 단계 조건(ToolPower)을 함께 본다.
    public static bool RecipeUnlocked(RecipeData recipe)
    {
        var root = RequiredRoot(recipe);
        return root == DemoSpecialization.None || DemoSettlementController.Instance?.SelectedRoot == root;
    }

    public static DemoSpecialization RequiredRoot(RecipeData recipe)
    {
        if (recipe == null) return DemoSpecialization.None;
        var upgrade = Load()?.upgrades?.FirstOrDefault(u => u.recipe == recipe);
        if (upgrade != null && upgrade.root != DemoSpecialization.None) return upgrade.root;
        return ToolPower.RequiredRoot(recipe.outputItem);
    }
}
