using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// CONTENT-001 / Campaign A01: 실제 이웃 인사와 가방의 판매 재고만 관찰한다.
// 거래·친밀도·퀘스트 보상을 별도로 실행하지 않는 WorldSandbox 전용 연결이다.
[DisallowMultipleComponent]
public sealed class CampaignOpeningController : MonoBehaviour
{
    public const string BoriId = "bori";
    public const float PlayCameraSize = 14f;
    WorldAlphaPlayableController _alpha;
    CampaignProgressSaveData _progress = new CampaignProgressSaveData();
    GameObject _bori;
    NpcDialogue _dialogue;

    public bool HasStarted => _progress.started;
    public bool HasGreetedBori => _progress.boriGreeted;
    public bool HasSecuredStock => _progress.firstStockItemId >= 0;
    public bool OpeningComplete => HasStarted && HasGreetedBori && HasSecuredStock;
    public GameObject Bori => _bori;
    public string Objective => !HasGreetedBori
        ? "가게 옆 이웃 보리에게 다가가 Space로 인사하세요."
        : !HasSecuredStock
            ? "오늘 밤 보리가 고를 물건을 준비하세요. 숲 채집터에서 당근을 모을 수 있어요."
            : "첫 물건을 마련했어요. 잡화점 판매대에 진열해 보세요.";

    public bool Begin(WorldAlphaPlayableController alpha)
    {
        _alpha = alpha;
        if (!EnsureBori()) return false;
        _progress.started = true;
        _bori.SetActive(true);
        ApplyCampaignCamera();
        EnsureFirstNight().Begin(this);
        ObserveInventory();
        return true;
    }

    bool EnsureBori()
    {
        if (_bori != null) return true;
        WorldGameplayAdapterService adapter = _alpha != null ? _alpha.Adapter : null;
        GameObject prefab = Resources.Load<GameObject>("Residents/Resident_Bori");
        if (adapter?.RuntimeShop == null || prefab == null) return false;
        // 상점과 시작점 사이의 NavMesh 위에 배치한다. 기존 역할 주민을 바꾸지 않는다.
        Vector3 shop = adapter.RuntimeShop.transform.position;
        Vector3 direction = adapter.PlayerRoot.transform.position - shop;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        Vector3 preferred = shop + direction.normalized * 5f;
        if (!NavMesh.SamplePosition(preferred, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return false;
        _bori = Instantiate(prefab, hit.position, Quaternion.LookRotation(direction.normalized), transform);
        _bori.name = "Campaign_Bori";
        _dialogue = _bori.GetComponent<NpcDialogue>();
        _dialogue.GreetingInteracted += OnGreeting;
        _bori.GetComponent<NpcController>().shopLocation = adapter.RuntimeShop.transform;
        var labelRoot = new GameObject("BoriName");
        labelRoot.transform.SetParent(_bori.transform, false);
        labelRoot.transform.localPosition = Vector3.up * 2.15f;
        labelRoot.AddComponent<PrototypeWorldLabel>().Set("보리 · 이웃", new Color(1f, 0.87f, 0.55f), 2.5f);
        return true;
    }

    void OnGreeting(NpcDialogue speaker, GameObject interactor)
    {
        if (!HasStarted || speaker != _dialogue || speaker.friendshipId != BoriId ||
            interactor != _alpha?.Adapter?.PlayerRoot) return;
        _progress.boriGreeted = true;
        ObserveInventory();
    }

    void Update()
    {
        if (!HasStarted) return;
        ObserveInventory();
    }

    void ObserveInventory()
    {
        if (HasSecuredStock || Inventory.instance == null) return;
        Inventory inventory = Inventory.instance;
        InventorySlot stock = inventory.slots?.FirstOrDefault(IsSaleStock) ??
                              inventory.hotbar?.slots?.FirstOrDefault(IsSaleStock);
        if (stock == null) return;
        _progress.firstStockItemId = stock.item.id;
        _progress.firstStockDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    }

    static bool IsSaleStock(InventorySlot slot) => slot != null && !slot.IsEmpty &&
        slot.item.basePrice > 0 && slot.item.buildingToBuild == null && slot.item.toolType == ToolType.None;

    public CampaignProgressSaveData Capture()
    {
        if (HasStarted) ObserveInventory();
        return new CampaignProgressSaveData
        {
            started = _progress.started,
            boriGreeted = _progress.boriGreeted,
            firstStockItemId = _progress.firstStockItemId,
            firstStockDay = _progress.firstStockDay,
            firstNight = GetComponent<CampaignFirstShopNightController>()?.Capture()
        };
    }

    public bool Restore(WorldAlphaPlayableController alpha, CampaignProgressSaveData saved)
    {
        _alpha = alpha;
        _progress = saved == null ? new CampaignProgressSaveData() : new CampaignProgressSaveData
        {
            started = saved.started,
            boriGreeted = saved.started && saved.boriGreeted,
            firstStockItemId = saved.started ? saved.firstStockItemId : -1,
            firstStockDay = saved.started ? saved.firstStockDay : 0
        };
        if (!HasStarted)
        {
            if (_bori != null) _bori.SetActive(false);
            GetComponent<CampaignFirstShopNightController>()?.Restore(this, null);
            return true;
        }
        if (!EnsureBori()) return false;
        _bori.SetActive(true);
        ApplyCampaignCamera();
        EnsureFirstNight().Restore(this, saved?.firstNight);
        return true;
    }

    CampaignFirstShopNightController EnsureFirstNight() =>
        GetComponent<CampaignFirstShopNightController>() ?? gameObject.AddComponent<CampaignFirstShopNightController>();

    void ApplyCampaignCamera()
    {
        // 실제 대화 거리에서 이웃의 외형을 알아볼 수 있는 새 캠페인 배율.
        if (Camera.main != null && Camera.main.orthographic) Camera.main.orthographicSize = PlayCameraSize;
    }

    void OnDestroy()
    {
        if (_dialogue != null) _dialogue.GreetingInteracted -= OnGreeting;
    }
}
