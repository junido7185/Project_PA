using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Task 086 — 상품 진열 코너는 ShopSlot/배치의 읽기 전용 파생 상태다.
// 재고, 가격, 구매, 판매 기록, 마을 변화 점수와 저장은 기존 소유자가 계속 담당한다.
[DefaultExecutionOrder(90)]
public class MerchandisingCornerController : MonoBehaviour
{
    public sealed class CornerSnapshot
    {
        public ItemCategory Category { get; }
        public string CornerId { get; }
        public IReadOnlyList<string> PlacementIds { get; }
        public IReadOnlyList<Vector2Int> FootprintCells { get; }
        public int SlotCount => PlacementIds.Count;
        public Vector3 WorldCenter { get; }

        internal CornerSnapshot(ItemCategory category, string cornerId, string[] placementIds,
            Vector2Int[] footprintCells, Vector3 worldCenter)
        {
            Category = category;
            CornerId = cornerId;
            PlacementIds = placementIds;
            FootprintCells = footprintCells;
            WorldCenter = worldCenter;
        }
    }

    sealed class ShelfEntry
    {
        public ShopSlot slot;
        public string placementId;
        public ItemCategory category;
        public List<Vector2Int> footprintCells;
    }

    public static MerchandisingCornerController Instance { get; private set; }

    [Header("Derived Corner Presentation")]
    [Min(0.05f)] public float refreshInterval = 0.25f;
    [Min(0.2f)] public float labelHeight = 1.35f;

    public bool IsReady { get; private set; }
    public IReadOnlyList<CornerSnapshot> CurrentCorners => _cornerView;
    public int CornerCount => _corners.Count;
    public int WorldLabelCount => _labelObjects.Count(label => label != null && label.activeSelf);

    readonly List<CornerSnapshot> _corners = new List<CornerSnapshot>();
    readonly List<GameObject> _labelObjects = new List<GameObject>();
    ReadOnlyCollection<CornerSnapshot> _cornerView;
    Transform _labelRoot;
    float _nextRefresh;
    string _lastSignature = string.Empty;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cornerView = _corners.AsReadOnly();
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 180; i++)
        {
            if (ShopCustomizationController.Instance != null && ShopCustomizationController.Instance.IsReady)
                break;
            yield return null;
        }

        IsReady = ShopCustomizationController.Instance != null && ShopCustomizationController.Instance.IsReady;
        if (!IsReady)
        {
            Debug.LogWarning("[MerchandisingCorner] ShopCustomizationController가 준비되지 않아 비활성 상태로 남습니다.");
            yield break;
        }

        RefreshNow();
        Debug.Log($"[MerchandisingCorner] Ready: {BuildCompactCornerSummary()}");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsReady || Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        RefreshInternal(force: false);
    }

    public int RefreshNow()
    {
        if (ShopCustomizationController.Instance == null || !ShopCustomizationController.Instance.IsReady)
            return _corners.Count;

        IsReady = true;
        RefreshInternal(force: true);
        return _corners.Count;
    }

    public bool TryGetFirstCorner(ItemCategory category, out CornerSnapshot corner)
    {
        corner = _corners.FirstOrDefault(candidate => candidate.Category == category);
        return corner != null;
    }

    public string BuildCornerSummary()
    {
        if (_corners.Count == 0)
            return "진열 코너: 같은 분류 상품을 나란히 2칸 이상 진열하세요.";

        return "진열 코너: " + string.Join(" · ", _corners.Select(corner =>
            $"{GetCategoryDisplayName(corner.Category)} {corner.SlotCount}칸"));
    }

    public string BuildCompactCornerSummary()
    {
        if (_corners.Count == 0) return "코너: 같은 분류 2칸+";

        string[] visible = _corners.Take(2)
            .Select(corner => $"{GetCategoryDisplayName(corner.Category)} {corner.SlotCount}")
            .ToArray();
        string remainder = _corners.Count > visible.Length ? $" +{_corners.Count - visible.Length}" : string.Empty;
        return $"코너: {string.Join(" · ", visible)}{remainder}";
    }

    public static string GetCategoryDisplayName(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "원재료",
            ItemCategory.Processed => "가공품",
            ItemCategory.Utility => "실용품",
            ItemCategory.Luxury => "고급품",
            _ => "상품"
        };
    }

    void RefreshInternal(bool force)
    {
        ShopCustomizationController customization = ShopCustomizationController.Instance;
        List<ShelfEntry> entries = CollectEligibleShelves(customization);
        string signature = BuildSignature(entries);
        if (!force && string.Equals(signature, _lastSignature, StringComparison.Ordinal)) return;

        _lastSignature = signature;
        RebuildCorners(entries, customization);
        RebuildWorldLabels();
        customization.RefreshCornerPresentation();
        Debug.Log($"[MerchandisingCorner] {BuildCompactCornerSummary()}");
    }

    static List<ShelfEntry> CollectEligibleShelves(ShopCustomizationController customization)
    {
        var byPlacement = new Dictionary<string, ShelfEntry>(StringComparer.Ordinal);
        var invalidMixedPlacements = new HashSet<string>(StringComparer.Ordinal);
        var footprintBuffer = new List<Vector2Int>();

        ShopSlot[] slots = FindObjectsByType<ShopSlot>(FindObjectsSortMode.None);
        foreach (ShopSlot slot in slots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy || slot.IsEmpty
                || slot.currentItem == null || slot.currentItem.data == null) continue;

            Item item = slot.currentItem.data;
            if (item.category == ItemCategory.Tool || item.toolType != ToolType.None) continue;

            if (!customization.TryGetShopSlotPlacementSnapshot(slot, footprintBuffer,
                    out string placementId, out bool recovered)
                || recovered || string.IsNullOrWhiteSpace(placementId) || footprintBuffer.Count == 0
                || invalidMixedPlacements.Contains(placementId)) continue;

            if (byPlacement.TryGetValue(placementId, out ShelfEntry existing))
            {
                // 미래의 다중 슬롯 가구가 서로 다른 분류를 담으면 sub-slot footprint 전에는
                // 가구 하나를 코너 두 칸처럼 오인하지 않는다.
                if (existing.category != item.category)
                {
                    byPlacement.Remove(placementId);
                    invalidMixedPlacements.Add(placementId);
                }
                continue;
            }

            byPlacement.Add(placementId, new ShelfEntry
            {
                slot = slot,
                placementId = placementId,
                category = item.category,
                footprintCells = footprintBuffer.Distinct().OrderBy(cell => cell.x).ThenBy(cell => cell.y).ToList()
            });
        }

        return byPlacement.Values.OrderBy(entry => entry.category).ThenBy(entry => entry.placementId).ToList();
    }

    static string BuildSignature(List<ShelfEntry> entries)
    {
        var builder = new StringBuilder(entries.Count * 32);
        foreach (ShelfEntry entry in entries)
        {
            builder.Append(entry.placementId).Append('|').Append((int)entry.category).Append('|');
            foreach (Vector2Int cell in entry.footprintCells)
                builder.Append(cell.x).Append(',').Append(cell.y).Append(';');
            builder.Append('#');
        }
        return builder.ToString();
    }

    void RebuildCorners(List<ShelfEntry> entries, ShopCustomizationController customization)
    {
        _corners.Clear();
        foreach (IGrouping<ItemCategory, ShelfEntry> categoryGroup in entries.GroupBy(entry => entry.category))
        {
            List<ShelfEntry> group = categoryGroup.OrderBy(entry => entry.placementId).ToList();
            var visited = new bool[group.Count];
            for (int start = 0; start < group.Count; start++)
            {
                if (visited[start]) continue;
                var component = new List<ShelfEntry>();
                var pending = new Queue<int>();
                pending.Enqueue(start);
                visited[start] = true;

                while (pending.Count > 0)
                {
                    int current = pending.Dequeue();
                    component.Add(group[current]);
                    for (int next = 0; next < group.Count; next++)
                    {
                        if (visited[next] || !AreEdgeAdjacent(group[current], group[next])) continue;
                        visited[next] = true;
                        pending.Enqueue(next);
                    }
                }

                if (component.Count < 2) continue;
                AddCorner(categoryGroup.Key, component, customization);
            }
        }

        _corners.Sort((a, b) =>
        {
            int category = a.Category.CompareTo(b.Category);
            return category != 0 ? category : string.CompareOrdinal(a.CornerId, b.CornerId);
        });
    }

    static bool AreEdgeAdjacent(ShelfEntry a, ShelfEntry b)
    {
        foreach (Vector2Int cellA in a.footprintCells)
            foreach (Vector2Int cellB in b.footprintCells)
                if (Mathf.Abs(cellA.x - cellB.x) + Mathf.Abs(cellA.y - cellB.y) == 1)
                    return true;
        return false;
    }

    void AddCorner(ItemCategory category, List<ShelfEntry> component,
        ShopCustomizationController customization)
    {
        string[] placementIds = component.Select(entry => entry.placementId)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Vector2Int[] cells = component.SelectMany(entry => entry.footprintCells).Distinct()
            .OrderBy(cell => cell.x).ThenBy(cell => cell.y).ToArray();
        string cornerId = $"corner:{category}:{string.Join("+", placementIds)}";

        Vector3 center = Vector3.zero;
        foreach (Vector2Int cell in cells)
            center += GridService.Instance.ZoneCellToWorld(customization.ZoneId, cell, labelHeight);
        center /= Mathf.Max(1, cells.Length);

        _corners.Add(new CornerSnapshot(category, cornerId, placementIds, cells, center));
    }

    void RebuildWorldLabels()
    {
        EnsureLabelRoot();
        for (int i = 0; i < _corners.Count; i++)
        {
            CornerSnapshot corner = _corners[i];
            GameObject labelObject = EnsureLabelObject(i);
            labelObject.name = $"CornerLabel_{corner.Category}_{i + 1}";
            labelObject.transform.position = corner.WorldCenter;
            labelObject.SetActive(true);

            PrototypeWorldLabel label = labelObject.GetComponent<PrototypeWorldLabel>();
            label.Set($"{GetCategoryDisplayName(corner.Category)} 코너 · {corner.SlotCount}칸",
                GetCategoryColor(corner.Category), 1.18f);
            TextMeshPro text = labelObject.GetComponent<TextMeshPro>();
            if (text != null)
            {
                text.fontStyle = FontStyles.Bold;
                text.sortingOrder = 18;
            }
        }

        for (int i = _corners.Count; i < _labelObjects.Count; i++)
            if (_labelObjects[i] != null) _labelObjects[i].SetActive(false);
    }

    void EnsureLabelRoot()
    {
        if (_labelRoot != null) return;
        var root = new GameObject("PA_MerchandisingCornerLabels");
        root.transform.SetParent(transform, false);
        _labelRoot = root.transform;
    }

    GameObject EnsureLabelObject(int index)
    {
        while (_labelObjects.Count <= index)
        {
            var label = new GameObject($"CornerLabel_{_labelObjects.Count + 1}");
            label.transform.SetParent(_labelRoot, false);
            label.AddComponent<PrototypeWorldLabel>();
            _labelObjects.Add(label);
        }
        return _labelObjects[index];
    }

    static Color GetCategoryColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => new Color(0.63f, 0.88f, 0.48f),
            ItemCategory.Processed => new Color(1f, 0.72f, 0.44f),
            ItemCategory.Utility => new Color(0.86f, 0.68f, 0.46f),
            ItemCategory.Luxury => new Color(0.82f, 0.74f, 1f),
            _ => new Color(1f, 0.90f, 0.64f)
        };
    }
}
