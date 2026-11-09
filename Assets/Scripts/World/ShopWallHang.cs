using System.Linq;
using UnityEngine;

// 2026-11-09 너굴 리스트 연구 H5 "벽걸이(액자·기념패)": 가게 실내 뒷벽(책상과 창문 사이)의 빈 벽에 도감 기념패(2051)를 건다.
// 배치 칸 시스템은 건드리지 않는다 — 고정된 벽 자리 하나에 걸거나 떼기만 한다. 걸면 가방에서 하나 빠지고, 빈손으로 [E] 하면 다시 가방으로.
// 저장: SaveData.shopWallHang 한 줄(추가 필드, "" / "plaque" — 옛 저장은 비어 있어 빈 벽).
// 가게에 걸려 있으면 저녁 손님이 기념패를 알아본다(DecorThemeSets.RemarksInShop).
public sealed class ShopWallHang : MonoBehaviour
{
    public const string PlaqueKey = "plaque";
    public const int PlaqueId = 2051;
    static readonly Vector3 HangAt = new Vector3(-.85f, 1.2f, 2.49f); // CozyDressing 기준(뒷벽 z 2.52, 기념패 판 중심이 바닥 위 약 1.8m)
    static readonly string[] StandParts = { "PlaqueFoot", "PlaqueLegL", "PlaqueLegR" };

    public static string Current { get; private set; } = "";
    public static string Pending; // SaveManager가 이어하기 때 넣는다
    public static int Hangs { get; private set; }
    public static ShopWallHang Instance { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = ""; Pending = null; Hangs = 0; Instance = null; }

    Transform _wall;
    GameObject _shown;
    ShopWallSpot _spot;
    float _nextBind;

    void Awake() { Instance = this; }
    public Transform Spot => _spot != null ? _spot.transform : null;
    public GameObject Shown => _shown;
    public static bool IsPlaque(Item item) => item != null && item.id == PlaqueId;

    void Update()
    {
        if (_spot == null)
        {
            if (Time.unscaledTime < _nextBind) return;
            _nextBind = Time.unscaledTime + .5f;
            Bind();
            if (_spot == null) return;
        }
        if (Pending != null) { Show(Pending); Pending = null; }
        // 기념패를 들었을 때(걸기) 또는 걸려 있고 빈손일 때(떼기)만 [E] 대상이 된다.
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        var held = player != null ? EquipmentSystem.CurrentHeld(player) : null;
        bool active = player != null && (IsPlaque(held) && Current == "" || held == null && Current != "");
        if (_spot.enabled != active) _spot.enabled = active;
    }

    void Bind()
    {
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null) interior = FindFirstObjectByType<DemoShopInterior>(FindObjectsInactive.Include);
        if (interior == null || interior.interior == null) return;
        _wall = interior.interior.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CozyDressing");
        if (_wall == null) return;
        var go = new GameObject("WallHangSpot");
        go.transform.SetParent(_wall, false);
        go.transform.localPosition = new Vector3(HangAt.x, 1f, 2.15f); // 벽 바로 앞 허리 높이
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(1.1f, 1.4f, .5f);
        _spot = go.AddComponent<ShopWallSpot>();
        _spot.enabled = false;
        Show(Current);
    }

    void Show(string key)
    {
        if (_shown != null) { Destroy(_shown); _shown = null; }
        Current = key == PlaqueKey ? PlaqueKey : "";
        if (Current == "" || _wall == null) return;
        var item = Resources.Load<Item>("Items/Item_CollectionPlaque");
        if (item == null || item.model == null) return;
        _shown = Instantiate(item.model, _wall, false);
        _shown.name = "WallPlaque";
        _shown.transform.localPosition = HangAt;
        _shown.transform.localRotation = Quaternion.identity; // 판 앞면(-z)이 방 안쪽
        _shown.transform.localScale = Vector3.one * 1.15f;
        foreach (var t in _shown.GetComponentsInChildren<Transform>(true))
            if (System.Array.IndexOf(StandParts, t.name) >= 0) t.gameObject.SetActive(false);
        foreach (var c in _shown.GetComponentsInChildren<Collider>(true)) c.enabled = false;
    }

    public bool TryToggle(GameObject interactor, out string message)
    {
        message = "";
        var inventory = interactor != null ? interactor.GetComponent<Inventory>() : null;
        if (inventory == null) return false;
        var held = EquipmentSystem.CurrentHeld(interactor);
        if (Current == "")
        {
            if (!IsPlaque(held)) { message = "도감 기념패를 들고 걸 수 있어요."; return false; }
            inventory.RemoveItems(held, 1);
            inventory.RefreshAllUI();
            Show(PlaqueKey);
            Hangs++;
            message = "도감 기념패를 벽에 걸었어요.";
            return true;
        }
        if (held != null) { message = "빈손으로 떼어 낼 수 있어요."; return false; }
        var plaque = Resources.Load<Item>("Items/Item_CollectionPlaque");
        if (plaque == null) return false;
        if (!inventory.TryReceiveToHotbar(plaque, 1) && !inventory.AddInstance(new ItemInstance(plaque, 1)))
        { message = "가방이 가득 차서 뗄 수 없어요."; return false; }
        inventory.RefreshAllUI();
        Show("");
        message = "도감 기념패를 떼어 가방에 넣었어요.";
        return true;
    }
}

// 배치 미리보기(바닥에 놓기)보다 먼저 [E]를 받는 가게 고정 자리(벽 걸이·책상 위).
public interface IPrecedesPlacement { }

public sealed class ShopWallSpot : MonoBehaviour, IInteractable, IPrecedesPlacement
{
    public string GetInteractPrompt() => ShopWallHang.Current == "" ? "벽에 걸기 · 도감 기념패" : "벽에서 떼기 · 도감 기념패";

    public void Interact(GameObject interactor)
    {
        if (ShopWallHang.Instance == null) return;
        bool ok = ShopWallHang.Instance.TryToggle(interactor, out string message);
        if (ok) GatherFeedback.Pickup();
        FirstDayWorldPresentation.Toast(message, false);
    }
}
