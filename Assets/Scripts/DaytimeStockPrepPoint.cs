using UnityEngine;

public class DaytimeStockPrepPoint : MonoBehaviour, IInteractable
{
    public string activityId = "garden-basket";
    public string itemResourcePath = "Items/Item_Carrot";
    public int grantCount = 2;
    public string displayName = "Day Stock Basket";

    Renderer[] _renderers;
    PrototypeWorldLabel _label;
    bool _collected;

    void Awake()
    {
        CacheVisuals();
        RefreshVisual();
    }

    public void Configure(string id, string resourcePath, int count, string label)
    {
        activityId = string.IsNullOrWhiteSpace(id) ? activityId : id;
        itemResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? itemResourcePath : resourcePath;
        grantCount = Mathf.Max(1, count);
        displayName = string.IsNullOrWhiteSpace(label) ? displayName : label;
        CacheVisuals();
        RefreshVisual();
    }

    public void Configure(string resourcePath, int count, string label)
    {
        Configure(activityId, resourcePath, count, label);
    }

    public void SetCollected(bool collected)
    {
        _collected = collected;
        RefreshVisual();
    }

    public void Interact(GameObject interactor)
    {
        if (DayNightShopLoopController.Instance == null)
        {
            Debug.LogWarning("[DaytimeStockPrep] DayNightShopLoopController is missing.");
            return;
        }

        DayNightShopLoopController.Instance.TryCollectDayPrepStock(this, interactor);
    }

    public string GetInteractPrompt()
    {
        if (DayNightShopLoopController.Instance == null)
            return $"{displayName}: unavailable";

        if (_collected)
            return $"{displayName}: collected today";

        if (!DayNightShopLoopController.Instance.IsDayPrepPointAvailable(this))
            return $"{displayName}: day prep only";

        return $"{displayName}: prepare shop stock";
    }

    void CacheVisuals()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);

        if (_label == null)
        {
            var labelTransform = transform.Find("Label");
            GameObject labelGo = labelTransform != null ? labelTransform.gameObject : new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            _label = labelGo.GetComponent<PrototypeWorldLabel>() ?? labelGo.AddComponent<PrototypeWorldLabel>();
        }
    }

    void RefreshVisual()
    {
        CacheVisuals();

        Color color = _collected
            ? new Color(0.48f, 0.54f, 0.50f, 1f)
            : new Color(0.95f, 0.68f, 0.34f, 1f);

        if (_renderers != null)
        {
            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer.GetComponent<TMPro.TextMeshPro>() != null) continue;
                if (renderer.material != null)
                    renderer.material.color = color;
            }
        }

        if (_label != null)
        {
            string status = _collected ? "ready" : "prep stock";
            _label.Set($"{displayName}\n{status}", _collected ? new Color(0.70f, 0.80f, 0.72f) : new Color(1f, 0.95f, 0.72f), 1.3f);
        }
    }
}
