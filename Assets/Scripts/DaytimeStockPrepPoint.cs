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
    public bool IsCollected => _collected;
    public bool PhysicalFruit { get; private set; }
    public bool FruitDropped { get; private set; }
    public void ConfigurePhysicalFruit()
    {
        PhysicalFruit = true;
        if (_label != null) _label.gameObject.SetActive(false);
    }
    System.Collections.IEnumerator ShakeFruit()
    {
        FruitDropped = true;
        Quaternion rest = transform.rotation;
        for (float t = 0; t < .55f; t += Time.deltaTime)
        { transform.rotation = rest * Quaternion.Euler(0, 0, Mathf.Sin(t * 40) * (1 - t / .55f) * 3); yield return null; }
        transform.rotation = rest;
        Item fruit = Resources.Load<Item>(itemResourcePath);
        var assets = FirstDayStudioAssets.Load();
        for (int i = 0; i < grantCount; i++)
        {
            float angle = (i - 1) * .9f;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
            var drop = Instantiate(assets.fruit, transform.position + direction * .9f + Vector3.up * 2.3f, Quaternion.identity);
            FirstDayStudioAssets.ScaleVisual(drop, .22f);
            var sphere = drop.AddComponent<SphereCollider>(); sphere.radius = .12f / drop.transform.lossyScale.x;
            var body = drop.AddComponent<Rigidbody>(); body.mass = .15f; body.linearDamping = 1.5f; body.angularDamping = 2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(direction * .7f, ForceMode.VelocityChange);
            var pickup = drop.AddComponent<InventoryFramework.PickupItem>();
            pickup.item = fruit; pickup.amount = 1; pickup.contextual = true;
        }
    }

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
        if (PhysicalFruit)
        { if (!FruitDropped && EquipmentSystem.CurrentHeld(interactor) == null) StartCoroutine(ShakeFruit()); return; }
        if (DayNightShopLoopController.Instance == null)
        {
            Debug.LogWarning("[DaytimeStockPrep] DayNightShopLoopController is missing.");
            return;
        }

        DayNightShopLoopController.Instance.TryCollectDayPrepStock(this, interactor);
    }

    public string GetInteractPrompt()
    {
        if (PhysicalFruit) return FruitDropped ? "떨어진 사과를 주워보세요" : "사과나무 흔들기";
        if (DayNightShopLoopController.Instance == null)
            return $"{displayName}: 이용 불가";

        if (_collected)
            return $"{displayName}: 오늘 준비 완료";

        if (!DayNightShopLoopController.Instance.IsDayPrepPointAvailable(this))
            return $"{displayName}: 낮에만 채집 가능";

        return $"{displayName}: 재고 준비하기";
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
        if (PhysicalFruit) return;
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
            // v2 — 플로팅 텍스트 최소화: 한 줄 + 소형 (디버그 느낌 제거).
            string status = _collected ? "오늘 완료" : "채집 가능";
            _label.Set($"{displayName} · {status}", _collected ? new Color(0.70f, 0.80f, 0.72f) : new Color(1f, 0.95f, 0.72f), 0.95f);
        }
    }
}
