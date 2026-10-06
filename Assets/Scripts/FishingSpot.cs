using System.Collections;
using UnityEngine;

// Player-facing fishing action layered on the existing daily stock-prep state.
public class FishingSpot : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.2f)] float castDuration = 1.25f;

    DaytimeStockPrepPoint _stockPoint;
    Coroutine _castRoutine;
    bool _isFishing;
    string _lastFeedback = "낚싯대를 드리울 수 있어요.";

    public bool IsFishing => _isFishing;
    public string LastFeedback => _lastFeedback;

    bool _directPlayerDemo, _biteReady, _claiming;
    GameObject _castingPlayer;
    public bool IsDirectPlayerDemo => _directPlayerDemo;
    public bool BiteReady => _biteReady;

    // User direction 2026-10-05: bite -> reel control -> dry-land flop -> tool capture.
    // 보상은 기존 일일 낚시 활동(DayNightShopLoopController.TryCollectDayPrepStock)만 준다.
    public enum DemoPhase { Idle, Waiting, Bite, Hooked, Reeling, Landed }
    public DemoPhase Phase { get; private set; }
    // P9 플레이어 동작 표현용(읽기 전용): 지금 줄을 던져 둔 플레이어와 마지막으로 당긴 시각.
    public static GameObject ActiveDirectCaster { get; private set; }
    public static float LastDirectPullAt { get; private set; } = -10f;
    public GameObject Bobber => _bobber;
    public Vector3 WaterTarget => _waterTarget;
    const float BiteWindow = 1f;
    GameObject _bobber, _fishShadow;
    Vector3 _waterTarget, _bobberRest;
    float _biteEndsAt, _phaseStartedAt;
    WorldGridService _grid;
    FirstDayFishingMinigame _reel;
    public FirstDayLandedFish LandedFish { get; private set; }
    public Item SelectedFish { get; private set; }
    string _selectedResource;
    static readonly string[] Species = { "Items/Item_Fish", "Items/Item_Fish_RedSnapper", "Items/Item_Fish_YellowTang" };

    public void ConfigureDirectPlayerDemo(WorldGridService grid = null)
    {
        if (_directPlayerDemo) return;
        CancelCast();
        _directPlayerDemo = true;
        _grid = grid != null ? grid : FindFirstObjectByType<WorldGridService>();
        SnapToShore();
    }

    // 낚시터를 실제 물과 맞닿은 가장 가까운 땅 칸으로 옮기고, 그 앞 물 위에 물고기 그림자를 둔다(식별 가능한 낚시 기회).
    void SnapToShore()
    {
        if (_grid == null || !_grid.WorldToCell(transform.position, out var origin)) return;
        var definition = _grid.Definition;
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        // 고정 3인칭 카메라에서 찌·입질이 화면 아래(프롬프트/핫바 뒤)로 가지 않게,
        // 물이 카메라 반대쪽이나 옆에 있는 물가를 가까운 순서보다 우선한다(거리 페널티로 제한).
        Vector3 away = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up) : Vector3.forward;
        away = away.sqrMagnitude > .01f ? away.normalized : Vector3.forward;
        float bestScore = float.MaxValue;
        Vector2Int bestLand = default, bestStep = default;
        for (int dx = -24; dx <= 24; dx++)
            for (int dz = -24; dz <= 24; dz++)
            {
                var land = origin + new Vector2Int(dx, dz);
                if (!_grid.TryGetCell(land, out var landCell) || !landCell.IsWalkable) continue;
                foreach (var step in around)
                {
                    if (!_grid.TryGetCell(land + step, out var water) || !water.HasWater) continue;
                    float facing = Vector3.Dot(new Vector3(step.x, 0f, step.y), away); // 1=멀어지는 쪽, -1=카메라 쪽
                    float score = Mathf.Sqrt(dx * dx + dz * dz) + (facing > .5f ? 0f : facing > -.5f ? 4f : 18f);
                    if (score < bestScore) { bestScore = score; bestLand = land; bestStep = step; }
                }
            }
        if (bestScore == float.MaxValue) return;
        if (!_grid.CellToWorld(bestLand, out var landPosition) || !_grid.CellToWorld(bestLand + bestStep, out var waterPosition) ||
            !_grid.TryGetCell(bestLand + bestStep, out var waterCell)) return;
        // 물가 바로 앞에 낚시터 표식을 두고, 물 쪽 0.6칸 앞에 찌가 떨어진다.
        Vector3 edge = Vector3.Lerp(landPosition, waterPosition, .45f);
        transform.position = new Vector3(edge.x, landPosition.y, edge.z);
        _waterTarget = waterPosition + new Vector3(bestStep.x, 0f, bestStep.y) * definition.CellSize * .6f;
        _waterTarget.y = WorldChunkMeshBuilder.OpeningWaterHeight(definition,waterCell) + .02f;
        SpawnFishShadow();
    }

    void SpawnFishShadow()
    {
        var assets = FirstDayStudioAssets.Load();
        if (assets == null || assets.fish == null || _fishShadow != null) return;
        _fishShadow = FirstDayStudioAssets.Place(assets.fish, transform, _waterTarget, .22f);
        _fishShadow.name = "FishingSpot_FishShadow";
        foreach (var collider in _fishShadow.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(.10f, .16f, .20f, 1f));
        block.SetColor("_Color", new Color(.10f, .16f, .20f, 1f));
        foreach (var renderer in _fishShadow.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
    }

    bool IsPlayerInReach(GameObject player)
    {
        if (player == null || player.GetComponent<Inventory>() != Inventory.instance || Inventory.instance == null)
            return false;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
        return Mathf.Abs(transform.position.y - player.transform.position.y) < 2f &&
            delta.sqrMagnitude <= 2.05f * 2.05f && delta.sqrMagnitude > .0025f &&
            Vector3.Dot(player.transform.forward, delta.normalized) >= .75f;
    }

    bool DemoAvailable(out DayNightShopLoopController loop)
    {
        loop = DayNightShopLoopController.Instance;
        return loop != null && _stockPoint != null && loop.IsDayPrepPointAvailable(_stockPoint);
    }

    void InteractDirect(GameObject player)
    {
        if (_claiming || !isActiveAndEnabled || !IsPlayerInReach(player)) return;
        if ((PlayerInputHandler.Instance?.FirstDayControls ?? false) && EquipmentSystem.CurrentHeld(player)?.toolType != ToolType.FishingRod) return;
        if (!DemoAvailable(out var loop))
        {
            CancelCast();
            _lastFeedback = "오늘 이 낚시터의 물고기는 다 낚았어요.";
            return;
        }
        switch (Phase)
        {
            case DemoPhase.Idle:
                StartDirectCast(player);
                return;
            case DemoPhase.Waiting:
                CancelCast();
                _lastFeedback = "너무 빨리 당겼어요. 다시 던져 보세요.";
                FirstDayWorldPresentation.Toast(_lastFeedback, false);
                return;
            case DemoPhase.Bite:
                if (player != _castingPlayer) return;
                LastDirectPullAt = Time.time;
                BeginReeling();
                return;
            default:
                return;
        }
    }

    void StartDirectCast(GameObject player)
    {
        if (_waterTarget == Vector3.zero) { _lastFeedback = "물가를 바라보고 던져 주세요."; return; }
        _castingPlayer = player;
        _selectedResource = Species[Random.Range(0, Species.Length)];
        SelectedFish = Resources.Load<Item>(_selectedResource);
        if (SelectedFish == null) { _selectedResource = Species[0]; SelectedFish = FishItem; }
        ActiveDirectCaster = player;
        _isFishing = true;
        _biteReady = false;
        Phase = DemoPhase.Waiting;
        _phaseStartedAt = Time.time;
        _lastFeedback = "입질을 기다리는 중… 찌가 쑥 들어가면 당기세요.";
        // Display-only float; existing fishing phases, timings and rewards remain authoritative.
        if (_bobber == null) _bobber = FirstDayFishingFloat.Create(this);
        if (_bobber != null) { _bobber.SetActive(true); _bobberRest = _bobber.transform.position; }
        GatherFeedback.Splash();
        _castRoutine = StartCoroutine(WaitForDirectBite());
    }

    IEnumerator WaitForDirectBite()
    {
        yield return new WaitForSeconds(Random.Range(1.6f, 3.2f));
        _castRoutine = null;
        if (Phase != DemoPhase.Waiting) yield break;
        Phase = DemoPhase.Bite;
        _biteReady = true;
        _biteEndsAt = Time.time + BiteWindow;
        _lastFeedback = "지금 당기기!";
        GatherFeedback.Bite();
    }

    void BeginReeling()
    {
        Phase = DemoPhase.Reeling; _biteReady = false;
        if (_reel == null) _reel = gameObject.AddComponent<FirstDayFishingMinigame>();
        _reel.Open(SelectedFish, CompleteReeling);
    }

    void CompleteReeling(bool caught)
    {
        if (Phase != DemoPhase.Reeling) return;
        if (!caught || _castingPlayer == null || !DemoAvailable(out _) || !TryFindDryLanding(out var dry))
        {
            CancelCast(); _lastFeedback = "물고기가 달아났어요. 다시 던져 보세요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
            return;
        }
        var assets = FirstDayStudioAssets.Load();
        GameObject model = SelectedFish != null && SelectedFish.model != null ? SelectedFish.model : assets != null ? assets.fish : null;
        LandedFish = FirstDayLandedFish.Create(this, SelectedFish, model, _waterTarget, dry);
        if (LandedFish == null) { CancelCast(); _lastFeedback = "물고기 모델을 표시할 수 없어요."; return; }
        Phase = DemoPhase.Landed; _isFishing = false;
        if (_bobber != null) _bobber.SetActive(false);
        if (ActiveDirectCaster == _castingPlayer) ActiveDirectCaster = null;
        if (_fishShadow != null) _fishShadow.SetActive(false);
        ToolDurability.ConsumeSuccess(_castingPlayer.GetComponent<Inventory>(), _castingPlayer.GetComponent<Inventory>().GetSelectedInstance());
        _lastFeedback = ItemDisplayName.For(SelectedFish) + "를 끌어올렸어요! 도끼나 곡괭이로 포획하세요.";
        FirstDayWorldPresentation.Toast(_lastFeedback, true);
    }

    bool TryFindDryLanding(out Vector3 position)
    {
        position = Vector3.zero;
        if (_grid == null || _castingPlayer == null || !_grid.WorldToCell(_castingPlayer.transform.position, out var origin)) return false;
        Vector3 desired = _castingPlayer.transform.position + _castingPlayer.transform.forward * .9f + _castingPlayer.transform.right * .65f;
        float best = float.MaxValue;
        for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
        {
            var cell = origin + new Vector2Int(x, z);
            if (!_grid.TryGetCell(cell, out var data) || data.HasWater || !data.IsWalkable ||
                !_grid.CellToWorld(cell, out var centre) || Mathf.Abs(centre.y - _castingPlayer.transform.position.y) > 1.2f) continue;
            float inset = _grid.Definition.CellSize * .5f - .45f;
            var candidate = new Vector3(Mathf.Clamp(desired.x, centre.x - inset, centre.x + inset), centre.y,
                Mathf.Clamp(desired.z, centre.z - inset, centre.z + inset));
            float score = (candidate - desired).sqrMagnitude;
            if (score >= best) continue;
            best = score; position = candidate;
        }
        return best < float.MaxValue;
    }

    public bool TryClaimLandedFish(FirstDayLandedFish fish, GameObject player)
    {
        if (_claiming || Phase != DemoPhase.Landed || fish == null || fish != LandedFish || !fish.ReadyToClaim ||
            player == null || player.GetComponent<Inventory>() != Inventory.instance || !DemoAvailable(out var loop)) return false;
        // 기존 일일 낚시 활동이 Inventory 우선 지급과 오늘 완료 기록을 소유한다.
        _claiming = true;
        bool caught;
        int before = Inventory.instance.CountItems(SelectedFish);
        // Activity id stays unchanged: old saves cannot collect the same shore activity again.
        _stockPoint.itemResourcePath = _selectedResource; _stockPoint.grantCount = 1;
        try { caught = loop.TryCollectDayPrepStock(_stockPoint, player); }
        finally { _claiming = false; }
        if (!caught)
        {
            _lastFeedback = "가방이 가득 찼어요. 포획한 물고기는 여기서 기다려요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
            return false;
        }
        DemoHotbarPreference.Prefer(SelectedFish);
        FirstDayWorldPresentation.Acquired(SelectedFish, Inventory.instance.CountItems(SelectedFish) - before, fish.transform.position);
        GatherFeedback.Catch();
        CancelCast();
        _lastFeedback = "물고기를 포획했어요!";
        if (_fishShadow != null) _fishShadow.SetActive(false);
        return true;
    }

    static Item FishItem => Resources.Load<Item>("Items/Item_Fish");

    void Update()
    {
        if (!_directPlayerDemo) return;
        if (_fishShadow != null && _fishShadow.activeSelf)
        {
            float t = Time.time * .6f;
            _fishShadow.transform.position = _waterTarget + new Vector3(Mathf.Cos(t) * .8f, -.01f, Mathf.Sin(t) * .8f);
            _fishShadow.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t))) * Quaternion.Euler(0f, 90f, 0f);
        }
        if (Phase == DemoPhase.Idle || Phase == DemoPhase.Landed) return;
        // 낚싯대를 내려놓거나(X·도구 변경) 자리를 떠나면 줄을 거둔다.
        if (_castingPlayer == null || !IsPlayerInReach(_castingPlayer) ||
            EquipmentSystem.CurrentHeld(_castingPlayer)?.toolType != ToolType.FishingRod)
        {
            CancelCast();
            _lastFeedback = "낚싯대를 거뒀어요.";
            return;
        }
        if (_bobber != null)
        {
            float bob = Phase == DemoPhase.Waiting ? Mathf.Sin((Time.time - _phaseStartedAt) * 3f) * .03f : -.12f;
            _bobber.transform.position = _bobberRest + Vector3.up * bob;
        }
        if (Phase == DemoPhase.Bite && Time.time > _biteEndsAt)
        {
            CancelCast();
            _lastFeedback = "물고기가 달아났어요. 다시 던져 보세요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
        }
    }

    public void Configure(DaytimeStockPrepPoint stockPoint)
    {
        _stockPoint = stockPoint;
    }

    void Awake()
    {
        if (_stockPoint == null)
            _stockPoint = GetComponentInParent<DaytimeStockPrepPoint>();
    }

    void OnDisable()
    {
        CancelCast();
    }

    public void Interact(GameObject interactor)
    {
        if (_directPlayerDemo) { InteractDirect(interactor); return; }
        if (_isFishing)
            return;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
        {
            _lastFeedback = "지금은 낚시할 수 없어요.";
            Debug.LogWarning("[FishingSpot] Fishing requires a day-prep stock point and loop controller.");
            return;
        }

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
        {
            // Reuse the loop HUD explanation for the wrong phase or a completed catch.
            loop.TryCollectDayPrepStock(_stockPoint, interactor);
            _lastFeedback = loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "오늘 낚시는 마쳤어요."
                : "낚시는 낮 준비 시간에 할 수 있어요.";
            return;
        }

        _isFishing = true;
        _lastFeedback = "낚싯대를 드리웠어요. 찌를 기다리는 중...";
        _castRoutine = StartCoroutine(CastAndCatch(interactor));
    }

    public string GetInteractPrompt()
    {
        if (_directPlayerDemo)
        {
            if (Phase != DemoPhase.Idle) return _lastFeedback;
            return DemoAvailable(out _) ? "낚시 · 낚싯대 던지기" : "낚시 · 오늘 낚시 완료";
        }
        if (_isFishing)
            return _lastFeedback;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
            return "해변 낚시터: 이용 불가";

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
            return loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "해변 낚시터: 오늘 낚시 완료"
                : "해변 낚시터: 낮에 다시 오기";

        return "해변 낚시터: 낚싯대 드리우기";
    }

    IEnumerator CastAndCatch(GameObject interactor)
    {
        float firstWait = Mathf.Min(0.35f, castDuration * 0.35f);
        yield return new WaitForSeconds(firstWait);

        _lastFeedback = "찌가 흔들려요...";
        yield return new WaitForSeconds(Mathf.Max(0.05f, castDuration - firstWait));

        _castRoutine = null;
        CompleteCatch(interactor);
    }

    bool CompleteCatch(GameObject interactor)
    {
        _isFishing = false;

        var loop = DayNightShopLoopController.Instance;
        bool caught = loop != null
            && _stockPoint != null
            && loop.TryCollectDayPrepStock(_stockPoint, interactor);

        _lastFeedback = caught
            ? "물고기를 낚아 재고 가방에 담았어요!"
            : "이번에는 낚시를 마치지 못했어요.";
        return caught;
    }

    void CancelCast()
    {
        if (_reel != null) _reel.CloseSilently();
        if (LandedFish != null) { Destroy(LandedFish.gameObject); LandedFish = null; }
        _biteReady = false;
        if (ActiveDirectCaster == _castingPlayer) ActiveDirectCaster = null;
        _castingPlayer = null;
        Phase = DemoPhase.Idle;
        if (_bobber != null) _bobber.SetActive(false);
        if (_castRoutine != null)
        {
            StopCoroutine(_castRoutine);
            _castRoutine = null;
        }

        _isFishing = false;
    }

    public void PrepareForStateRestore()
    {
        CancelCast();
    }

#if UNITY_EDITOR
    // Editor smoke tests use the same completion path without waiting on wall-clock time.
    public bool CompleteCatchForValidation(GameObject interactor)
    {
        if (_directPlayerDemo) return false;
        if (!_isFishing)
            return false;

        CancelCast();
        return CompleteCatch(interactor);
    }
#endif
}
