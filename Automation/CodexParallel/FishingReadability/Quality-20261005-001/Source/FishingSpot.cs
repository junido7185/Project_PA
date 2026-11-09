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

    // P4 — 기본 낚시: 던지기 → 기다림 → 입질(짧은 창) → 당기기. 너무 빨리/놓침/취소 뒤 다시 던질 수 있다.
    // 보상은 기존 일일 낚시 활동(DayNightShopLoopController.TryCollectDayPrepStock)만 준다.
    public enum DemoPhase { Idle, Waiting, Bite, Hooked }
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
        _waterTarget.y = definition.WorldOrigin.y + waterCell.WaterSurfaceLevel * definition.ElevationStep + .02f;
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
            default:
                if (player != _castingPlayer) return;
                LastDirectPullAt = Time.time;
                TryLandFish(player, loop);
                return;
        }
    }

    void StartDirectCast(GameObject player)
    {
        if (_waterTarget == Vector3.zero) { _lastFeedback = "물가를 바라보고 던져 주세요."; return; }
        _castingPlayer = player;
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

    void TryLandFish(GameObject player, DayNightShopLoopController loop)
    {
        // 기존 일일 낚시 활동이 Inventory 우선 지급과 오늘 완료 기록을 소유한다.
        _claiming = true;
        bool caught;
        int before = Inventory.instance != null ? Inventory.instance.CountItems(FishItem) : 0;
        try { caught = loop.TryCollectDayPrepStock(_stockPoint, player); }
        finally { _claiming = false; }
        if (!caught)
        {
            // 가방이 가득 차도 물고기는 줄에 걸린 채로 남는다(유실 없음).
            Phase = DemoPhase.Hooked;
            _biteReady = true;
            _lastFeedback = "가방이 가득 찼어요. 자리를 비우고 끌어올리세요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
            return;
        }
        var inventory = player.GetComponent<Inventory>();
        ToolDurability.ConsumeSuccess(inventory, inventory != null ? inventory.GetSelectedInstance() : null);
        DemoHotbarPreference.Prefer(FishItem); // P8: 낚은 물고기도 바로 들고 진열할 수 있게 핫바 우선
        Vector3 from = _bobber != null ? _bobber.transform.position : _waterTarget;
        int gained = Inventory.instance != null ? Inventory.instance.CountItems(FishItem) - before : 0;
        GatherFeedback.Splash();
        FirstDayWorldPresentation.Acquired(FishItem, Mathf.Max(1, gained), from);
        CancelCast();
        _lastFeedback = "물고기를 낚았어요!";
        if (_fishShadow != null) _fishShadow.SetActive(false);
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
        if (Phase == DemoPhase.Idle) return;
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
