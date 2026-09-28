using System.Collections;
using UnityEngine;

// 낮 광질의 플레이어 행동 계층.
// 일일 완료/저장/보상 상태는 기존 DaytimeStockPrepPoint가 계속 소유한다.
public class MiningSpot : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.25f)] float strikeDuration = 1.1f;

    DaytimeStockPrepPoint _stockPoint;
    Coroutine _miningRoutine;
    Transform _pickaxeVisual;
    bool _isMining;
    string _lastFeedback = "공용 곡괭이로 광맥을 캘 수 있어요.";

    public bool IsMining => _isMining;
    public string LastFeedback => _lastFeedback;

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
        CancelMining();
    }


    // DIRECT-GATHERING-01A: explicit generated-resource mode; legacy behavior stays intact.
    WorldPersistenceService _directPersistence;
    WorldGenerationResult _directWorld;
    Item _directReward;
    string _directSpawnKey;
    int _directHits;
    bool _directDepleted, _directRewardPending;
    float _directHitAt = -10f;
    Vector3 _directBaseScale;
    string _directFeedback;
    public bool IsDirectWorld => _directWorld != null;
    public string DirectSpawnKey => _directSpawnKey;
    public int DirectRemainingHits => _directHits;
    public bool DirectDepleted => _directDepleted;

    public void ConfigureDirectWorld(WorldPersistenceService persistence,
        WorldResourceSpawnRecord spawn, Item reward, int hits = 3)
    {
        if (persistence == null || persistence.ActiveGeneratedWorld == null || spawn.Kind != WorldResourceKind.Stone || reward == null)
            throw new System.ArgumentException("Invalid direct Stone binding.");
        bool registered = false;
        foreach (var candidate in persistence.ActiveGeneratedWorld.ResourceSpawns)
            if (candidate.SpawnKey == spawn.SpawnKey && candidate.Kind == spawn.Kind && candidate.Coordinate == spawn.Coordinate)
                registered = true;
        if (!registered) throw new System.ArgumentException("Direct resource spawn is not registered.");
        _directPersistence = persistence;
        _directWorld = persistence.ActiveGeneratedWorld;
        _directSpawnKey = spawn.SpawnKey;
        _directReward = reward;
        _directHits = Mathf.Clamp(hits, 2, 4);
        _directBaseScale = transform.localScale;
        _directFeedback = "곡괭이로 광석 채집";
        RefreshDirectState();
    }

    bool RefreshDirectState()
    {
        if (_directPersistence == null || !ReferenceEquals(_directWorld, _directPersistence.ActiveGeneratedWorld))
            return false;
        var states = _directPersistence.CaptureState(transform.position, null).resourceStates;
        var state = states?.Find(entry => entry != null && entry.spawnKey == _directSpawnKey);
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        _directDepleted = state != null && state.consumed && day < state.respawnDay;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = !_directDepleted;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = !_directDepleted;
        return !_directDepleted;
    }

    string DirectPrompt => _directDepleted ? "채집 완료" : _directFeedback + $" · 남은 타격 {_directHits}";

    void InteractDirect(GameObject interactor)
    {
        if (_directRewardPending || !RefreshDirectState() || interactor == null) return;
        var inventory = interactor.GetComponent<Inventory>();
        Vector3 delta = interactor.transform.position - transform.position;
        if (inventory == null || inventory != Inventory.instance ||
            Vector3.ProjectOnPlane(delta, Vector3.up).magnitude > 2.05f || Mathf.Abs(delta.y) > 2f) return;
        if (EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Pickaxe)
        {
            _directFeedback = "곡괭이를 손에 들어주세요";
            return;
        }
        if (Time.time - _directHitAt < .25f) return;
        int strength = DemoPlaceableCatalog.Load()?.Upgrade(EquipmentSystem.CurrentHeld(interactor))?.playerWorkStrength ?? 1;
        strength = Mathf.Max(1, strength);
        if (_directHits > strength)
        {
            _directHits -= strength;
            _directHitAt = Time.time;
            _directFeedback = "곡괭이 타격";
            return;
        }
        // AddInstance accepts the whole reward or changes nothing. Keep the final hit on failure.
        _directRewardPending = true;
        bool added = inventory.AddInstance(new ItemInstance(_directReward, 1));
        if (!added)
        {
            _directRewardPending = false;
            _directFeedback = "Inventory full - free a slot and try again";
            return;
        }
        _directHits = 0;
        _directDepleted = true;
        // Persist depletion only after successful reward; direct resources do not auto-regrow.
        if (!_directPersistence.SetResourceState(_directSpawnKey, true, int.MaxValue))
            Debug.LogError("Direct resource persistence rejected a previously validated spawn: " + _directSpawnKey);
        _directFeedback = "Ore +1 - gathered";
        FirstDayWorldPresentation.Acquired(_directReward, 1, transform.position);
        RefreshDirectState();
        _directRewardPending = false;
    }

    void Update()
    {
        if (!IsDirectWorld) return;
        float elapsed = Time.time - _directHitAt;
        transform.localScale = _directBaseScale * (1f + (elapsed < .22f ? .1f * Mathf.Sin(elapsed / .22f * Mathf.PI) : 0f));
    }

    public void Interact(GameObject interactor)
    {
        if (IsDirectWorld) { InteractDirect(interactor); return; }
        if (_isMining)
            return;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
        {
            _lastFeedback = "지금은 광질할 수 없어요.";
            Debug.LogWarning("[MiningSpot] Mining requires a day-prep stock point and loop controller.");
            return;
        }

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
        {
            // 기존 루프가 잘못된 페이즈/당일 완료 사유를 HUD에도 표시한다.
            loop.TryCollectDayPrepStock(_stockPoint, interactor);
            _lastFeedback = loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "오늘 광질은 마쳤어요."
                : "광질은 낮 준비 시간에 할 수 있어요.";
            return;
        }

        _isMining = true;
        _lastFeedback = "공용 곡괭이로 광맥을 두드리는 중...";
        ResolvePickaxeVisual();
        _miningRoutine = StartCoroutine(StrikeAndCollect(interactor));
    }

    public string GetInteractPrompt()
    {
        if (IsDirectWorld) return DirectPrompt;
        if (_isMining)
            return _lastFeedback;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
            return "광산 채굴지: 이용 불가";

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
            return loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "광산 채굴지: 오늘 채굴 완료"
                : "광산 채굴지: 낮에 다시 오기";

        return "광산 채굴지: 광맥 두드리기";
    }

    IEnumerator StrikeAndCollect(GameObject interactor)
    {
        float elapsed = 0f;
        bool revealedOre = false;

        while (elapsed < strikeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / strikeDuration);
            AnimatePickaxe(progress);

            if (!revealedOre && progress >= 0.48f)
            {
                revealedOre = true;
                _lastFeedback = "돌 틈에서 광석이 보여요...";
            }

            yield return null;
        }

        _miningRoutine = null;
        ResetPickaxeVisual();
        CompleteMining(interactor);
    }

    bool CompleteMining(GameObject interactor)
    {
        _isMining = false;

        var loop = DayNightShopLoopController.Instance;
        bool collected = loop != null
            && _stockPoint != null
            && loop.TryCollectDayPrepStock(_stockPoint, interactor);

        _lastFeedback = collected
            ? "광석을 캐서 재고 가방에 담았어요!"
            : "이번에는 광질을 마치지 못했어요.";
        return collected;
    }

    void ResolvePickaxeVisual()
    {
        if (_pickaxeVisual != null) return;
        var go = GameObject.Find("PA_Mining_Pickaxe");
        if (go != null)
            _pickaxeVisual = go.transform;
    }

    void AnimatePickaxe(float progress)
    {
        ResolvePickaxeVisual();
        if (_pickaxeVisual == null) return;

        // 두 번의 부드러운 타격. 실제 채굴 결과와 무관한 표현 계층이다.
        float swing = Mathf.Sin(progress * Mathf.PI * 4f);
        float angle = -24f + Mathf.Max(0f, swing) * 46f;
        _pickaxeVisual.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void ResetPickaxeVisual()
    {
        if (_pickaxeVisual != null)
            _pickaxeVisual.localRotation = Quaternion.Euler(0f, 0f, -24f);
    }

    void CancelMining()
    {
        if (_miningRoutine != null)
        {
            StopCoroutine(_miningRoutine);
            _miningRoutine = null;
        }

        _isMining = false;
        ResetPickaxeVisual();
    }

    public void PrepareForStateRestore()
    {
        CancelMining();
    }

#if UNITY_EDITOR
    // 에디터 스모크는 벽시계 대기 없이 동일한 완료 경로를 호출한다.
    public bool CompleteMiningForValidation(GameObject interactor)
    {
        if (!_isMining)
            return false;

        CancelMining();
        return CompleteMining(interactor);
    }
#endif
}
