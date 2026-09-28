using System;
using UnityEngine;

[Flags]
public enum WorldPlaceableSurface
{
    None = 0, Settlement = 1, ShopFloor = 2, Living = 4, Utility = 8
}

// Authored functional areas only. The existing placement service evaluates these together
// with grid occupancy, natural obstacles and critical navigation routes.
public sealed class WorldPlaceableZone : MonoBehaviour
{
    public WorldPlaceableSurface surface;
    public Bounds localBounds;
    public int priority;
    public bool Contains(Vector3 position) => isActiveAndEnabled &&
        localBounds.Contains(transform.InverseTransformPoint(position));
}
