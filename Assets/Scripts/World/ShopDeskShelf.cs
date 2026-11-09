using System.Linq;
using UnityEngine;

// 2026-11-09 너굴 리스트 연구 H5 "탁상 위 작은 물건": 가게 실내 책상(윗면 .75m) 오른쪽 뒤 빈자리에 해변·땅속 발견물 하나를 올려 둔다.
// 배치 칸 시스템은 건드리지 않는다 — 책상 위 고정 자리 하나. 올리면 가방에서 하나 빠지고, 다른 발견물을 들고 [E]면 바꾸며(전 것은 가방으로), 빈손 [E]면 가방으로.
// 저장: SaveData.shopDeskItem 한 줄(추가 필드, 0 = 빈 책상 — 옛 저장은 0).
// 가게에 올려 두면 저녁 손님이 알아본다(DecorThemeSets.RemarksInShop).
public sealed class ShopDeskShelf : MonoBehaviour
{
    static readonly Vector3 SetAt = new Vector3(-1.56f, .75f, 1.62f); // CozyDressing 기준(책은 왼쪽, 머그는 오른쪽 앞)
    const float ShownSize = .3f;

    public static int Current { get; private set; }
    public static int? Pending; // SaveManager가 이어하기 때 넣는다
    public static int Sets { get; private set; }
    public static ShopDeskShelf Instance { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = 0; Pending = null; Sets = 0; Instance = null; }

    Transform _desk;
    GameObject _shown;
    ShopDeskSpot _spot;
    float _nextBind;

    void Awake() { Instance = this; }
    public Transform Spot => _spot != null ? _spot.transform : null;
    public GameObject Shown => _shown;

    // 책상에 올릴 수 있는 것: 도감 해변·땅속 섹션의 발견물(조개·유리·화석·광물 등 손바닥만 한 것).
    public static bool Accepts(Item item)
    {
        if (item == null) return false;
        foreach (var (section, ids) in DiscoveryLog.Sections)
            if ((section == "해변" || section == "땅속") && System.Array.IndexOf(ids, item.id) >= 0) return true;
        return false;
    }

    public static Item CurrentItem => Current != 0 && ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(Current, null) : null;

    void Update()
    {
        if (_spot == null)
        {
            if (Time.unscaledTime < _nextBind) return;
            _nextBind = Time.unscaledTime + .5f;
            Bind();
            if (_spot == null) return;
        }
        if (Pending.HasValue) { Show(Pending.Value); Pending = null; }
        // 발견물을 들었을 때(올리기·바꾸기) 또는 올려져 있고 빈손일 때(내리기)만 [E] 대상이 된다.
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        var held = player != null ? EquipmentSystem.CurrentHeld(player) : null;
        bool active = player != null && (Accepts(held) && held.id != Current || held == null && Current != 0);
        if (_spot.enabled != active) _spot.enabled = active;
    }

    void Bind()
    {
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null) interior = FindFirstObjectByType<DemoShopInterior>(FindObjectsInactive.Include);
        if (interior == null || interior.interior == null) return;
        _desk = interior.interior.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CozyDressing");
        if (_desk == null) return;
        var go = new GameObject("DeskShelfSpot");
        go.transform.SetParent(_desk, false);
        go.transform.localPosition = new Vector3(-1.8f, .85f, 1.45f); // 책상 위
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(.9f, .5f, .9f);
        _spot = go.AddComponent<ShopDeskSpot>();
        _spot.enabled = false;
        Show(Current);
    }

    void Show(int id)
    {
        if (_shown != null) { Destroy(_shown); _shown = null; }
        Current = id;
        if (id == 0 || _desk == null) return;
        var item = CurrentItem;
        if (item == null || item.model == null) return;
        // desk-r210: 높이로 맞추면 납작한 불가사리가 1.9m가 됐다 → 가장 긴 변을 .3m(r211 .26은 램프 갓에 반쯤 가려 오른쪽으로 옮김)로 맞추고 책상 윗면에 앉힌다.
        _shown = Instantiate(item.model, _desk, false);
        _shown.name = "DeskFind_" + id;
        _shown.transform.rotation = Quaternion.Euler(0, _desk.eulerAngles.y + 200f, 0) * item.model.transform.localRotation;
        _shown.transform.position = _desk.TransformPoint(SetAt);
        var renderers = _shown.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            _shown.transform.localScale *= ShownSize / Mathf.Max(.001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            Vector3 target = _desk.TransformPoint(SetAt);
            _shown.transform.position += new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);
        }
        foreach (var c in _shown.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var b in _shown.GetComponentsInChildren<Rigidbody>(true)) b.isKinematic = true;
    }

    static string N(Item item, string particle) { string name = ItemDisplayName.For(item); return name + ResidentNames.Josa(name, particle); }

    public bool TryUse(GameObject interactor, out string message)
    {
        message = "";
        var inventory = interactor != null ? interactor.GetComponent<Inventory>() : null;
        if (inventory == null) return false;
        var held = EquipmentSystem.CurrentHeld(interactor);
        var previous = CurrentItem;
        if (held == null)
        {
            if (previous == null) return false;
            if (!inventory.TryReceiveToHotbar(previous, 1) && !inventory.AddInstance(new ItemInstance(previous, 1)))
            { message = "가방이 가득 차서 내릴 수 없어요."; return false; }
            inventory.RefreshAllUI();
            Show(0);
            message = $"{N(previous, "를")} 가방에 넣었어요.";
            return true;
        }
        if (!Accepts(held)) { message = "해변·땅속에서 찾은 것을 올릴 수 있어요."; return false; }
        if (held.id == Current) { message = "이미 올려 두었어요."; return false; }
        inventory.RemoveItems(held, 1);
        if (previous != null && !inventory.TryReceiveToHotbar(previous, 1)) inventory.AddInstance(new ItemInstance(previous, 1));
        inventory.RefreshAllUI();
        Show(held.id);
        Sets++;
        message = previous != null ? $"{N(held, "로")} 바꿨어요. {N(previous, "는")} 가방에 넣었어요." : $"책상 위에 {N(held, "를")} 올려 두었어요.";
        return true;
    }
}

public sealed class ShopDeskSpot : MonoBehaviour, IInteractable, IPrecedesPlacement
{
    public string GetInteractPrompt()
    {
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        var held = player != null ? EquipmentSystem.CurrentHeld(player) : null;
        if (held != null) return $"책상에 올리기 · {ItemDisplayName.For(held)}";
        var current = ShopDeskShelf.CurrentItem;
        return current != null ? $"책상에서 내리기 · {ItemDisplayName.For(current)}" : "책상";
    }

    public void Interact(GameObject interactor)
    {
        if (ShopDeskShelf.Instance == null) return;
        bool ok = ShopDeskShelf.Instance.TryUse(interactor, out string message);
        if (ok) GatherFeedback.Pickup();
        if (message != "") FirstDayWorldPresentation.Toast(message, false);
    }
}
