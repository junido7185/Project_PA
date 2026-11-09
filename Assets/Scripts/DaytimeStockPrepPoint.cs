using UnityEngine;

public class DaytimeStockPrepPoint : MonoBehaviour, IInteractable
{
    public string activityId = "garden-basket";
    public string itemResourcePath = "Items/Item_Carrot";
    public int grantCount = 2;
    public string displayName = "Day Stock Basket";

    // D4 표시 전용 바인딩. 이 지점(트리거 앵커)은 움직이지 않고 보이는 나무만 흔든다.
    // 비어 있으면 렌더러를 가진 첫 자식을 쓴다. 매달린 열매 위치가 있으면 그 자리에서 실제 열매가 떨어진다.
    [SerializeField] Transform shakeVisual;
    [SerializeField] Transform[] fruitHangPoints;

    Renderer[] _renderers;
    PrototypeWorldLabel _label;
    bool _collected;
    bool _shaking, _fruitSpawned;
    Transform _shakeTarget;
    Vector3 _shakeRestPosition;
    Quaternion _shakeRestRotation;
    const float ShakeDuration = .65f, FruitReleaseAt = .18f;
    public bool IsCollected => _collected;
    public bool PhysicalFruit { get; private set; }
    public bool FruitDropped { get; private set; }
    public Transform ShakeVisual => shakeVisual;
    public bool IsShaking => _shaking;

    // null 인자는 기존(직렬화) 값을 유지한다. 인자 없는 기존 호출도 그대로 동작한다.
    public void ConfigurePhysicalFruit(Transform visual = null, Transform[] hangPoints = null)
    {
        PhysicalFruit = true;
        if (visual != null) shakeVisual = visual;
        if (hangPoints != null && hangPoints.Length > 0) fruitHangPoints = hangPoints;
        if (shakeVisual == null || transform.IsChildOf(shakeVisual))
            shakeVisual = ResourcePresentation.FindChildVisual(transform, _label != null ? _label.transform : null);
        if (_label != null) _label.gameObject.SetActive(false);
    }

    // 뿌리 피벗 기준의 감쇠 흔들림(플레이어 반대쪽으로 먼저). 흔들림이 가장 클 때 열매가 떨어진다.
    System.Collections.IEnumerator ShakeFruit(Vector3 from)
    {
        FruitDropped = true;
        _shaking = true;
        _fruitSpawned = false;
        _shakeTarget = shakeVisual;
        Vector3 pivot = default, axis = Vector3.right;
        if (_shakeTarget != null)
        {
            _shakeRestPosition = _shakeTarget.localPosition;
            _shakeRestRotation = _shakeTarget.localRotation;
            pivot = ResourcePresentation.ToWorld(_shakeTarget, ResourcePresentation.RootPivotLocal(_shakeTarget));
            Vector3 away = Vector3.ProjectOnPlane(transform.position - from, Vector3.up);
            axis = Vector3.Cross(Vector3.up, away.sqrMagnitude > .01f ? away.normalized : Vector3.forward);
        }
        for (float t = 0; t < ShakeDuration; t += Time.deltaTime)
        {
            if (_shakeTarget != null)
            {
                float k = 1f - t / ShakeDuration;
                _shakeTarget.localPosition = _shakeRestPosition;
                _shakeTarget.localRotation = _shakeRestRotation;
                _shakeTarget.RotateAround(pivot, axis, Mathf.Sin(t * 22f) * k * k * 5f);
            }
            if (!_fruitSpawned && t >= FruitReleaseAt) DropFruit();
            yield return null;
        }
        EndShake();
        if (!_fruitSpawned) DropFruit();
    }

    void EndShake()
    {
        if (_shakeTarget != null)
        {
            _shakeTarget.localPosition = _shakeRestPosition;
            _shakeTarget.localRotation = _shakeRestRotation;
        }
        _shakeTarget = null;
        _shaking = false;
    }

    void OnDisable()
    {
        if (!_shaking) return;
        EndShake();
        // 열매가 생기기 전에 끊겼다면 다시 흔들 수 있게 되돌린다(보상 수는 그대로).
        if (!_fruitSpawned) FruitDropped = false;
    }

    void DropFruit()
    {
        _fruitSpawned = true;
        Item fruit = Resources.Load<Item>(itemResourcePath);
        var assets = FirstDayStudioAssets.Load();
        for (int i = 0; i < grantCount; i++)
        {
            float angle = (i - 1) * .9f;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
            Vector3 start = transform.position + direction * .9f + Vector3.up * 2.3f;
            Transform hang = fruitHangPoints != null && i < fruitHangPoints.Length ? fruitHangPoints[i] : null;
            if (hang != null)
            {
                start = hang.position;
                Vector3 outward = Vector3.ProjectOnPlane(start - transform.position, Vector3.up);
                if (outward.sqrMagnitude > .01f) direction = outward.normalized;
                // 매달린 장식 열매는 실제 드롭으로 바뀐다(표시 전용 숨김, 나무·앵커 자체는 숨기지 않는다).
                if (!transform.IsChildOf(hang) && (shakeVisual == null || !shakeVisual.IsChildOf(hang)))
                    hang.gameObject.SetActive(false);
            }
            var drop = Instantiate(assets.fruit, start, Quaternion.identity);
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
        { if (!FruitDropped && EquipmentSystem.CurrentHeld(interactor) == null) StartCoroutine(ShakeFruit(interactor != null ? interactor.transform.position : transform.position)); return; }
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
                if (shakeVisual != null && renderer.transform.IsChildOf(shakeVisual)) continue; // 연결된 나무 모델은 상태색으로 칠하지 않는다.
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
