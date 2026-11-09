using System.Linq;
using UnityEngine;

// 2026-11-09 너굴 리스트 연구 H5 중 "러그 교체": 가게 실내 생활 공간 러그(LivingRug·RugInner·RugStripeA/B)를 러그 아이템으로 바꾼다.
// 배치 칸(칸당 하나) 규칙은 그대로 — 새 물건을 놓는 것이 아니라 깔려 있는 러그의 무늬만 바꾼다. 바꾸면 깔려 있던 러그(기본 제외)는 가방으로 돌아온다.
// 저장: SaveData.shopRugStyle 한 줄(추가 필드 — 옛 저장은 비어 있어 기본 러그).
public sealed class ShopRugSwap : MonoBehaviour
{
    public sealed class Style
    {
        public string Key, Name; public int ItemId; public Color Inner, Base, Stripe;
    }

    public static readonly Style[] Styles =
    {
        new Style { Key = "seaweed", Name = "해초 러그", ItemId = 2060, Inner = new Color(.42f, .55f, .2f), Base = new Color(.27f, .37f, .14f), Stripe = new Color(.93f, .86f, .5f) }, // 전시 다듬기: 기본 청록과 구분되게 올리브 초록
        new Style { Key = "coral", Name = "산호 러그", ItemId = 2061, Inner = new Color(.93f, .64f, .57f), Base = new Color(.72f, .43f, .39f), Stripe = new Color(.99f, .91f, .83f) },
    };
    static readonly Color DefaultInner = new Color(.36f, .58f, .5f), DefaultBase = new Color(.28f, .48f, .40f), DefaultStripe = new Color(.88f, .78f, .52f);

    public static string Current { get; private set; } = "";
    public static string Pending; // SaveManager가 이어하기 때 넣는다(실내가 생기면 적용)
    public static int Swaps { get; private set; }
    public static ShopRugSwap Instance { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = ""; Pending = null; Swaps = 0; Instance = null; }

    Renderer _inner, _base;
    Renderer[] _stripes = new Renderer[0];
    ShopRugSpot _spot;
    float _nextBind;

    void Awake() { Instance = this; }

    public static Style ForItem(Item item) => item == null ? null : Styles.FirstOrDefault(s => s.ItemId == item.id);
    public static Style ForKey(string key) => Styles.FirstOrDefault(s => s.Key == key);
    public Transform Spot => _spot != null ? _spot.transform : null;

    void Update()
    {
        if (_spot == null)
        {
            if (Time.unscaledTime < _nextBind) return;
            _nextBind = Time.unscaledTime + .5f;
            Bind();
            if (_spot == null) return;
        }
        if (Pending != null) { Apply(Pending); Pending = null; }
        // 러그를 들고 있을 때만 [E] 대상이 된다(다른 실내 상호작용을 가리지 않게).
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        bool holding = player != null && ForItem(EquipmentSystem.CurrentHeld(player)) != null;
        if (holding) Follow(player.transform);
        if (_spot.enabled != holding) _spot.enabled = holding;
    }

    // r204: 러그 한가운데의 고정 지점은 러그 위 탁자·소파에 가려 [E] 대상이 되지 못했다 →
    // 들고 있는 동안 지점을 '플레이어 바로 앞의 러그 위'로 옮긴다(러그 밖으로는 나가지 않게 러그 칸 안으로 자른다).
    void Follow(Transform player)
    {
        var rug = _base.transform;
        Vector3 local = rug.InverseTransformPoint(player.position + Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized * .8f);
        bool near = Mathf.Abs(local.x) < .5f + 1.6f / Mathf.Max(.1f, rug.lossyScale.x) && Mathf.Abs(local.z) < .5f + 1.6f / Mathf.Max(.1f, rug.lossyScale.z);
        local.x = Mathf.Clamp(local.x, -.45f, .45f); local.z = Mathf.Clamp(local.z, -.45f, .45f); local.y = 0f;
        Vector3 at = rug.TransformPoint(local) + Vector3.up * .3f;
        _spot.transform.position = near ? at : rug.position + Vector3.up * .3f;
    }

    public static int InteriorCount => FindObjectsByType<DemoShopInterior>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

    void Bind()
    {
        // 운영 중인 가게의 실내를 먼저 쓴다(여러 실내가 있으면 엉뚱한 곳에 묶이지 않게).
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null) interior = FindFirstObjectByType<DemoShopInterior>(FindObjectsInactive.Include);
        if (interior == null || interior.interior == null) return;
        var parts = interior.interior.GetComponentsInChildren<Renderer>(true);
        _base = parts.FirstOrDefault(r => r.name == "LivingRug");
        _inner = parts.FirstOrDefault(r => r.name == "RugInner");
        _stripes = parts.Where(r => r.name == "RugStripeA" || r.name == "RugStripeB").ToArray();
        if (_base == null) return;
        var go = new GameObject("RugSwapSpot");
        go.transform.SetParent(_base.transform.parent, false);
        // r202: 실내가 꺼져 있을 때(밖에서 Bind) 렌더러 bounds는 비어 원점이 나왔다 → 러그 Transform 위치(RoomBox 가운데)를 쓴다.
        go.transform.position = _base.transform.position + Vector3.up * .3f;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(.8f, .6f, .8f);
        _spot = go.AddComponent<ShopRugSpot>();
        _spot.enabled = false;
        Apply(Current);
    }

    public void Apply(string key)
    {
        var style = ForKey(key);
        Paint(_inner, style != null ? style.Inner : DefaultInner);
        Paint(_base, style != null ? style.Base : DefaultBase);
        foreach (var stripe in _stripes) Paint(stripe, style != null ? style.Stripe : DefaultStripe);
        Current = style != null ? style.Key : "";
    }

    static void Paint(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        var material = renderer.material; // 이 러그만(공용 재질은 통로 러너도 쓴다)
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    // 러그 아이템으로 바꾸기: 든 러그 1개를 쓰고, 깔려 있던 러그(기본 제외)는 가방으로.
    public bool TrySwap(GameObject interactor, out string message)
    {
        message = "";
        var inventory = interactor != null ? interactor.GetComponent<Inventory>() : null;
        var held = EquipmentSystem.CurrentHeld(interactor);
        var style = ForItem(held);
        if (inventory == null || style == null) { message = "러그를 들고 바꿀 수 있어요."; return false; }
        if (style.Key == Current) { message = $"이미 {style.Name}가 깔려 있어요."; return false; }
        var previous = ForKey(Current);
        inventory.RemoveItems(held, 1);
        if (previous != null)
        {
            var back = ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(previous.ItemId, null) : null;
            if (back != null && !inventory.TryReceiveToHotbar(back, 1)) inventory.AddInstance(new ItemInstance(back, 1));
        }
        inventory.RefreshAllUI();
        Apply(style.Key);
        Swaps++;
        message = previous != null ? $"{style.Name}로 바꿨어요. {previous.Name}는 가방에 넣었어요." : $"{style.Name}를 깔았어요.";
        return true;
    }
}

public sealed class ShopRugSpot : MonoBehaviour, IInteractable
{
    public string GetInteractPrompt()
    {
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        var style = ShopRugSwap.ForItem(player != null ? EquipmentSystem.CurrentHeld(player) : null);
        return style != null ? $"러그 바꾸기 · {style.Name}" : "러그";
    }

    public void Interact(GameObject interactor)
    {
        if (ShopRugSwap.Instance == null) return;
        bool ok = ShopRugSwap.Instance.TrySwap(interactor, out string message);
        if (ok) GatherFeedback.Pickup();
        FirstDayWorldPresentation.Toast(message, false);
    }
}
