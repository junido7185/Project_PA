using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// INTEGRATION-01: 기존 출항 표현과 Demo256 구성을 잇는 세션 한정 연결부.
// 거래/재고/저장 권위는 Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md §1 유지.
[DisallowMultipleComponent]
public sealed class DemoRouteController : MonoBehaviour
{
    public const string WorldScene = "WorldSandbox";
    static bool _arrivalPending;
    static bool _continuePending;
    static string _continueError;
    public static DepartureCompanionSelection.Candidate[] SelectedCompanions { get; private set; } = Array.Empty<DepartureCompanionSelection.Candidate>();
    public static string[] SelectedCompanionIds => SelectedCompanions.Select(c => c.id).ToArray();
    public static GameObject TransitionOverlay;
    public static void CarryCompanions(DepartureCompanionSelection selection)
    { SelectedCompanions = selection.ConfirmedIds.Select(id => selection.candidates.First(c => c.id == id)).ToArray(); }
    public static string ConsumeContinueError()
    { string error = _continueError; _continueError = null; return error; }

    public static bool PrepareContinue(string[] companionIds, out string reason)
    {
        reason = string.Empty;
        var ids = companionIds ?? Array.Empty<string>();
        if (ids.Length > 0)
        {
            var source = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation")
                ?.GetComponent<DepartureCompanionSelection>();
            if (source?.candidates == null || ids.Length != 2 || ids.Distinct().Count() != 2 ||
                ids.Any(id => !source.candidates.Any(candidate => candidate.id == id)))
            { reason = "저장된 동행자를 현재 후보에 대응할 수 없습니다."; return false; }
            SelectedCompanions = ids.Select(id => source.candidates.First(candidate => candidate.id == id)).ToArray();
        }
        else SelectedCompanions = Array.Empty<DepartureCompanionSelection.Candidate>();
        _continuePending = true;
        _arrivalPending = true;
        return true;
    }
    public bool FirstDemoSaleCompleted { get; private set; }
    public event Action DemoSucceeded;
    public bool IsPlayable { get; private set; }
    WorldAlphaPlayableController _alpha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        _arrivalPending = false;
        _continuePending = false;
        _continueError = null;
        SelectedCompanions = Array.Empty<DepartureCompanionSelection.Candidate>();
        TransitionOverlay = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == DepartureTutorialController.SceneName)
        {
            // sceneLoaded는 Awake 이후/인증 Start 이전이다. 기존 seed 보존과 새 교육을 분리한다.
            Inventory.instance?.ResetForFreshOpeningSession();
            new GameObject("DemoRoute_ArrivalBridge").AddComponent<DemoRouteController>();
        }
        else if (scene.name == WorldScene && _arrivalPending)
        {
            _arrivalPending = false;
            var grid = FindFirstObjectByType<WorldGridService>();
            if (grid == null) throw new InvalidOperationException("Demo256 scene has no existing world grid.");
            // AfterSceneLoad bootstrap는 첫 씬에서만 호출되므로 전환 시 동일 구성을 명시 연결한다.
            grid.gameObject.AddComponent<DemoRouteController>();
            if (grid.GetComponent<WorldGameplayAdapterService>() == null)
                grid.gameObject.AddComponent<WorldGameplayAdapterService>();
            if (grid.GetComponent<WorldAlphaPlayableController>() == null)
                grid.gameObject.AddComponent<WorldAlphaPlayableController>();
        }
    }

    IEnumerator Start()
    {
        if (gameObject.scene.name == DepartureTutorialController.SceneName)
        {
            DepartureVoyagePresentation voyage;
            while ((voyage = FindFirstObjectByType<DepartureVoyagePresentation>()) == null) yield return null;
            // 작은 도착 섬에서는 P3를 시작하지 않고 기존 항해 페이드 완료를 기다린다.
            var settlement = voyage.GetComponent<FirstIslandSettlementController>();
            if (settlement != null) settlement.enabled = false;
            var production = voyage.GetComponent<FirstProductionController>();
            if (production != null) production.enabled = false;
            while (!voyage.ArrivalFadeFinished) yield return null;
            if (!Application.CanStreamedLevelBeLoaded(WorldScene))
                throw new InvalidOperationException("WorldSandbox must be enabled in Build Settings for the demo route.");
            _arrivalPending = true;
            // 남은 실습 사과/선택/손 표현은 섬의 실제 보급품으로 전달하지 않는다.
            Inventory.instance?.ResetForFreshOpeningSession();
            Debug.Log("[INTEGRATION-01] Opening arrival -> Demo256");
            yield return SceneManager.LoadSceneAsync(WorldScene, LoadSceneMode.Single);
            yield break;
        }

        _alpha = GetComponent<WorldAlphaPlayableController>();
        while (_alpha == null || !_alpha.IsReady)
        {
            _alpha = GetComponent<WorldAlphaPlayableController>();
            yield return null;
        }
        if (_continuePending)
        {
            _continuePending = false;
            var load = _alpha.Adapter.RuntimeSaveManager.TryLoadGameAsync();
            while (!load.IsCompleted) yield return null;
            if (load.IsFaulted || !load.Result)
            {
                _continueError = load.IsFaulted ? load.Exception?.GetBaseException().Message :
                    _alpha.Adapter.RuntimeSaveManager.LastLoadError;
                if (string.IsNullOrEmpty(_continueError)) _continueError = "저장 데이터를 복원하지 못했습니다.";
                SceneManager.LoadScene("Prototype_FirstDay", LoadSceneMode.Single);
                yield break;
            }
        }
        else if (!_alpha.BeginNewGame()) throw new InvalidOperationException("Demo256 playable clock could not start.");
        _alpha.SetDevelopmentOverlayVisible(false);
        IsPlayable = true;
        SalesLogManager.OnSaleRecorded -= OnSale;
        SalesLogManager.OnSaleRecorded += OnSale;
        // The world controller has already bound the gameplay camera. End the
        // bridge by ownership, not by WaitForEndOfFrame (which can stall in Editor).
        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled || gameplayCamera.targetTexture != null)
            throw new InvalidOperationException("Demo256 gameplay camera is not rendering to the screen.");
        if (TransitionOverlay != null)
        {
            TransitionOverlay.SetActive(false);
            Destroy(TransitionOverlay);
            TransitionOverlay = null;
        }
        Debug.Log("[FIRST-DAY] Demo256 harbor ready; supply awaits E; exact companions=" + string.Join(",",SelectedCompanionIds));
    }

    void OnDestroy() => SalesLogManager.OnSaleRecorded -= OnSale;

    void OnSale(SaleRecord sale)
    {
        if (!IsPlayable || FirstDemoSaleCompleted || sale == null || sale.price <= 0) return;
        DemoSettlementController settlement = DemoSettlementController.Instance;
        if (settlement != null && settlement.OperatingShop != null)
        {
            if (!settlement.OperatingShop.Slots.Any(slot => slot != null && slot.IsClaimed &&
                    !slot.IsEmpty && slot.currentItem.data.itemName == sale.itemName)) return;
            CompleteFirstDemoSale();
            return;
        }

        // 이전 INTEGRATION-01 Hub+B01 직접 경로도 계속 지원한다.
        var placement = _alpha.Buildings;
        if (!placement.TryGetPlacement(WorldPlaceableKitCatalog.HubId, out _) ||
            !placement.TryGetPlacement(WorldPlaceableKitCatalog.ShopId, out var shop) ||
            _alpha.Adapter.RuntimeShop == null || _alpha.Adapter.RuntimeShop.gameObject != shop.GameObject) return;
        // RecordSale는 결제 후 슬롯 비우기 전에 발생한다. 실제 배치 상점의 청구된 상품만 관찰한다.
        if (!_alpha.Adapter.RuntimeShopSlots.Any(s => s != null && s.IsClaimed &&
                !s.IsEmpty && s.currentItem.data.itemName == sale.itemName)) return;
        CompleteFirstDemoSale();
    }

    void CompleteFirstDemoSale()
    {
        FirstDemoSaleCompleted = true;
        Debug.Log("[INTEGRATION-01] DEMO_SUCCESS first real placed-shop sale");
        DemoSucceeded?.Invoke();
    }
}
