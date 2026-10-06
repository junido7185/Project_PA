using System;
using UnityEngine;

// Opening Canon v2 §23: references to locally imported art, never gameplay ownership.
public sealed class FirstDayStudioAssets : ScriptableObject
{
    public GameObject player, boat, dock, chestClosed, chestOpen, fruit, axe, pickaxe, rod, net, fish, butterfly;
    public GameObject[] trees, rocks, grasses, flowers, bushes;
    public Item[] supplies;
    public GameObject blueprint, phone, workbench, wood;
    // P6: 판재(Plank)는 전용 모델이 없어 열매 모델로 보였다. 로컬 나무 판(FoodKit cutting board)을 판재 표시로 쓴다.
    public GameObject plank;
    public static FirstDayStudioAssets Load() => Resources.Load<FirstDayStudioAssets>("FirstDayStudio/Assets");
    public GameObject ModelFor(Item item)
    {
        if (item == null) return null;
        switch (item.toolType)
        {
            case ToolType.Axe: return axe;
            case ToolType.Pickaxe: return pickaxe;
            case ToolType.FishingRod: return rod;
            case ToolType.Net: return net;
        }
        if (item.id == 2010) return phone;
        if (item.id == 2014) return workbench;
        if (item.id == 2012 || item.id == 2013) return blueprint;
        if (item.itemName == "Wood") return wood;
        if (item.id == 7 && plank != null) return plank;
        if (item.itemName == "Ore") return rocks[0];
        if (item.itemName == "Fish") return fish;
        if (item.itemName == "Butterfly") return butterfly;
        return item.model != null ? item.model : fruit;
    }
    public static void ScaleVisual(GameObject visual, float height)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        visual.transform.localScale *= height / Mathf.Max(.001f, b.size.y);
    }
    public static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float height, float yaw = 0)
    {
        if (prefab == null) throw new InvalidOperationException("First Day local art reference is missing.");
        // Imported model roots may contain the authored axis conversion (for example -90 X).
        // Apply placement yaw on top instead of replacing that conversion and laying trees flat.
        var go = Instantiate(prefab, parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * prefab.transform.localRotation;
        ScaleVisual(go, height);
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            go.transform.position += Vector3.up * (position.y - b.min.y);
        }
        return go;
    }
}
