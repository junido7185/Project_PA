using System.Collections;
using UnityEngine;

// 낮 광질의 플레이어 행동 계층.
// 일일 완료/저장/보상 상태는 기존 DaytimeStockPrepPoint가 계속 소유한다.
public class MiningSpot : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.25f)] float strikeDuration = 1.1f;
    // D4 표시 전용 바인딩(직접 채집 바위). 비어 있으면 렌더러를 가진 첫 자식을 쓴다(루트에 렌더러가 있으면 기존 스케일 반응).
    // 타격 수·보상·저장·콜라이더 판정은 이 값과 무관하다.
    [SerializeField] Transform rockVisual;

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
        if (_crumbling) EndCrumble();
        else if (_visual != null) PoseStaged(_stageScale);
    }


    // DIRECT-GATHERING-01A: explicit generated-resource mode; legacy behavior stays intact.
    WorldPersistenceService _directPersistence;
    WorldGenerationResult _directWorld;
    Item _directReward;
    string _directSpawnKey;
    int _directHits;
    bool _directDepleted, _contactPending, _minedPendingClaim;
    float _directHitAt = -10f;
    Vector3 _directBaseScale;
    string _directFeedback;
    public bool IsDirectWorld => _directWorld != null;
    public string DirectSpawnKey => _directSpawnKey;
    public int DirectRemainingHits => _directHits;
    public bool DirectDepleted => _directDepleted;

    Transform _visual;
    Vector3 _visualRestPosition, _visualRestScale, _pivotLocal, _hitDirection = Vector3.forward, _openSide = Vector3.back;
    float _visualRadius = .5f, _stageScale = 1f;
    int _directMaxHits;
    bool _visualPosed, _crumbling;
    Coroutine _crumbleRoutine;
    MaterialPropertyBlock _crackBlock;
    readonly System.Collections.Generic.List<GameObject> _crackDebris = new System.Collections.Generic.List<GameObject>();
    const float HitJoltDuration = .22f, CrumbleDuration = .35f;
    public Transform PresentationVisual => _visual;
    public bool PresentationCrumbling => _crumbling;
    // 0 = 온전, 1 = 마지막 타격 직전. 남은 타격 수에서만 계산하는 표시 값이다(저장하지 않음).
    public float DirectCrackStage => _directMaxHits > 0 ? 1f - _directHits / (float)_directMaxHits : 0f;
    public int CrackDebrisCount => _crackDebris.Count;

    // 빌더/어댑터가 씬 연출 대상을 런타임에 지정할 때 쓴다. null은 기존(직렬화) 값을 유지한다.
    public void ConfigurePresentation(Transform visual)
    {
        if (_crumbling) EndCrumble();
        ApplyCrackStage(0f);
        if (visual != null) rockVisual = visual;
        _visual = null;
        ResolveVisual();
        if (!IsDirectWorld) return;
        ApplyCrackStage(_directDepleted ? 0f : DirectCrackStage);
        ApplyDirectVisibility();
    }

    // 단단함(TOOL_PROGRESSION.md): 1 일반 바위(구리 광석), 2 철 광맥(구리 곡괭이 이상). 곡괭이 힘이 모자라면 튕긴다.
    public int DirectHardness => _directHardness;
    int _directHardness = 1;

    public void ConfigureDirectWorld(WorldPersistenceService persistence,
        WorldResourceSpawnRecord spawn, Item reward, int hits = 3, int hardness = 1)
    {
        _directHardness = Mathf.Clamp(hardness, 1, 3);
        if (persistence == null || persistence.ActiveGeneratedWorld == null || spawn.Kind != WorldResourceKind.Stone || reward == null)
            throw new System.ArgumentException("Invalid direct Stone binding.");
        bool registered = false;
        foreach (var candidate in persistence.ActiveGeneratedWorld.ResourceSpawns)
            if (candidate.SpawnKey == spawn.SpawnKey && candidate.Kind == spawn.Kind && candidate.Coordinate == spawn.Coordinate)
                registered = true;
        if (!registered) throw new System.ArgumentException("Direct resource spawn is not registered.");
        if (_crumbling) EndCrumble();
        _directPersistence = persistence;
        _directWorld = persistence.ActiveGeneratedWorld;
        _directSpawnKey = spawn.SpawnKey;
        _directReward = reward;
        _directHits = Mathf.Clamp(hits, 2, 4);
        _directMaxHits = _directHits;
        _directBaseScale = transform.localScale;
        _directFeedback = "곡괭이로 광석 채집";
        ResolveVisual();
        ClearCrackDebris();
        ApplyCrackStage(0f);
        RefreshDirectState();
    }

    bool RefreshDirectState()
    {
        if (_directPersistence == null || !ReferenceEquals(_directWorld, _directPersistence.ActiveGeneratedWorld))
            return false;
        var states = _directPersistence.CaptureState(transform.position, null).resourceStates;
        var state = states?.Find(entry => entry != null && entry.spawnKey == _directSpawnKey);
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        // 캔 뒤 드롭을 줍기 전까지는 세션에서만 사라진 상태다(저장에는 줍기 후 확정).
        _directDepleted = _minedPendingClaim || state != null && state.consumed && day < state.respawnDay;
        ApplyDirectVisibility();
        return !_directDepleted;
    }

    // 고갈 판정은 즉시 콜라이더를 끈다. 렌더러만 부서지는 연출이 끝날 때까지 남는다.
    void ApplyDirectVisibility()
    {
        bool visible = !_directDepleted || _crumbling;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = !_directDepleted;
        if (_visual != null && !_visual.IsChildOf(transform))
        {
            foreach (var renderer in _visual.GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
            foreach (var collider in _visual.GetComponentsInChildren<Collider>()) collider.enabled = !_directDepleted;
        }
    }

    void ResolveVisual()
    {
        if (_visual != null) return;
        _visual = rockVisual != null ? rockVisual
            : GetComponent<Renderer>() == null ? ResourcePresentation.FindChildVisual(transform) : null;
        // 루트(트리거)나 그 조상은 연출 대상으로 쓰지 않는다.
        if (_visual == null || transform.IsChildOf(_visual)) { _visual = null; return; }
        _visualRestPosition = _visual.localPosition;
        _visualRestScale = _visual.localScale;
        _stageScale = 1f;
        _pivotLocal = ResourcePresentation.RootPivotLocal(_visual);
        if (ResourcePresentation.TryGetBounds(_visual, out var bounds))
            _visualRadius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), .2f, 1.5f);
    }

    // 바닥 중심을 고정한 채 크기만 바꾼다(바위가 떠 보이지 않게).
    void PoseStaged(float scale)
    {
        _visual.localScale = _visualRestScale * scale;
        _visual.localPosition = _pivotLocal + (_visualRestPosition - _pivotLocal) * scale;
    }

    // 단계별 균열: 기존 재질 색을 조금씩 어둡게(MaterialPropertyBlock, 새 재질 없음), 크기를 조금씩 줄인다.
    void ApplyCrackStage(float stage)
    {
        if (_visual == null) return;
        _stageScale = 1f - .1f * stage;
        PoseStaged(_stageScale);
        _visualPosed = false;
        _crackBlock ??= new MaterialPropertyBlock();
        float shade = Mathf.Lerp(1f, .6f, stage);
        foreach (var renderer in _visual.GetComponentsInChildren<Renderer>(true))
        {
            if (stage <= 0f && !renderer.HasPropertyBlock()) continue; // 칠한 적 없는 렌더러에는 빈 블록도 붙이지 않는다.
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                _crackBlock.Clear();
                var material = materials[i];
                string property = material == null ? null
                    : material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (stage > 0f && property != null)
                {
                    Color color = material.GetColor(property);
                    _crackBlock.SetColor(property, new Color(color.r * shade, color.g * shade, color.b * shade, color.a));
                }
                renderer.SetPropertyBlock(_crackBlock, i);
            }
        }
    }

    // 중간 타격마다 기존 로컬 바위 모델의 작은 조각을 카메라에 보이는 옆쪽 발치에 남긴다(충돌 없음, 표시 전용).
    void SpawnCrackDebris()
    {
        var assets = FirstDayStudioAssets.Load();
        if (_visual == null || assets == null || assets.rocks == null || assets.rocks.Length == 0) return;
        Vector3 pivot = ResourcePresentation.ToWorld(_visual, _pivotLocal);
        for (int i = 0; i < 2; i++)
        {
            var model = assets.rocks[Random.Range(0, assets.rocks.Length)];
            if (model == null) continue;
            Vector3 side = Quaternion.Euler(0f, Random.Range(-40f, 40f), 0f) * _openSide;
            var piece = FirstDayStudioAssets.Place(model, transform, pivot + side * _visualRadius * Random.Range(.85f, 1.15f),
                Random.Range(.1f, .17f), Random.Range(0f, 360f));
            piece.name = "CrackDebris";
            // 루트 콜라이더 토글이 조각 콜라이더를 다시 켜지 않도록 제거한다.
            foreach (var collider in piece.GetComponentsInChildren<Collider>()) Destroy(collider);
            _crackDebris.Add(piece);
        }
    }

    void ClearCrackDebris()
    {
        foreach (var piece in _crackDebris) if (piece != null) Destroy(piece);
        _crackDebris.Clear();
    }

    // 부서짐은 표시 전용이다. 고갈·보상·저장 판정은 시작 전에 이미 확정되어 있다.
    IEnumerator CrumbleThenHide()
    {
        float from = _stageScale;
        ApplyCrackStage(1f);
        for (float t = 0f; t < CrumbleDuration; t += Time.deltaTime)
        {
            float k = t / CrumbleDuration;
            PoseStaged(Mathf.Lerp(from, .25f, k * k));
            _visual.position += _hitDirection * (Mathf.Sin(t * 70f) * .03f * (1f - k));
            yield return null;
        }
        _crumbleRoutine = null;
        EndCrumble();
    }

    void EndCrumble()
    {
        if (_crumbleRoutine != null) StopCoroutine(_crumbleRoutine);
        _crumbleRoutine = null;
        _crumbling = false;
        ClearCrackDebris();
        ApplyCrackStage(0f);
        ApplyDirectVisibility();
    }

    public const int DirectDropAmount = 3;
    string DirectPrompt => _directDepleted ? "채집 완료" :
        $"{(_directHardness <= 1 ? "기본 곡괭이 가능" : ToolPower.PickaxeFor(_directHardness) + " 이상")} · {ItemDisplayName.For(_directReward)}×{DirectDropAmount} · 남은 {_directHits}회";

    void InteractDirect(GameObject interactor)
    {
        if (_contactPending || !RefreshDirectState() || interactor == null) return;
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
        StartCoroutine(DirectContact(interactor, inventory));
    }

    // 스윙이 바위에 닿는 순간 타격이 성립한다. 그 사이의 연타·도구 교체는 타격/보상을 만들지 않는다.
    System.Collections.IEnumerator DirectContact(GameObject interactor, Inventory inventory)
    {
        _contactPending = true;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _contactPending = false;
        Item held = interactor != null ? EquipmentSystem.CurrentHeld(interactor) : null;
        if (interactor == null || _directDepleted || held?.toolType != ToolType.Pickaxe) yield break;
        // 곡괭이 힘(ToolPower)이 바위 단단함보다 낮으면 튕긴다(내구도·바위 손상 없음). 아니면 (힘 − 단단함 + 1)만큼 깎는다.
        int power = Mathf.Max(1, ToolPower.For(held));
        if (power < _directHardness)
        {
            _directHitAt = Time.time;
            _directFeedback = $"너무 단단해요 · {ToolPower.PickaxeFor(_directHardness)} 이상이 필요해요";
            interactor.GetComponent<EquipmentSystem>()?.FailedUse(_directFeedback, true);
            GatherFeedback.Hit(GatherFeedback.VisibleContact(transform.position, transform.position.y + .55f, .4f, interactor.transform.position), true, .6f);
            yield break;
        }
        int strength = power - _directHardness + 1;
        _directHitAt = Time.time;
        Vector3 toward = Vector3.ProjectOnPlane(transform.position - interactor.transform.position, Vector3.up);
        _hitDirection = toward.sqrMagnitude > .01f ? toward.normalized : Vector3.forward;
        // 기본 추적 카메라에서 플레이어 머리에 가리지 않게 바위 윗면, 화면 가로로 플레이어 반대쪽에 맞는다(표시 전용).
        _openSide = GatherFeedback.OpenSide(transform.position, interactor.transform.position);
        Vector3 contact = _visual != null && ResourcePresentation.TryGetBounds(_visual, out var rock)
            ? GatherFeedback.VisibleContact(rock.center, rock.max.y, _visualRadius, interactor.transform.position)
            : GatherFeedback.VisibleContact(transform.position, transform.position.y + .55f, .4f, interactor.transform.position);
        GatherFeedback.Hit(contact, true);
        if (_directHits > strength)
        {
            _directHits -= strength;
            _directFeedback = "곡괭이 타격";
            ApplyCrackStage(DirectCrackStage);
            SpawnCrackDebris();
            yield break;
        }
        // 성공한 작업 결과: 바위가 부서지고 Ore가 바닥에 떨어진다. 도구 내구도는 이 결과에만 줄어든다.
        _directHits = 0;
        _minedPendingClaim = true;
        _crumbling = _visual != null;
        RefreshDirectState();
        if (_crumbling) _crumbleRoutine = StartCoroutine(CrumbleThenHide());
        _directFeedback = "바위를 캤어요";
        ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        GatherFeedback.SpawnDrop(_directReward, transform.position, interactor.transform.position, ClaimDirectDrop, DirectDropAmount);
    }

    void ClaimDirectDrop()
    {
        _minedPendingClaim = false;
        // Persist depletion only after the dropped reward is in the bag; direct resources do not auto-regrow.
        if (_directPersistence == null || !_directPersistence.SetResourceState(_directSpawnKey, true, int.MaxValue))
            Debug.LogError("Direct resource persistence rejected a previously validated spawn: " + _directSpawnKey);
        RefreshDirectState();
    }

    void Update()
    {
        if (!IsDirectWorld) return;
        float elapsed = Time.time - _directHitAt;
        if (_visual == null)
        {
            transform.localScale = _directBaseScale * (1f + (elapsed < .22f ? .1f * Mathf.Sin(elapsed / .22f * Mathf.PI) : 0f));
            return;
        }
        if (_crumbling) return;
        // 맞은 방향으로 짧게 밀렸다 돌아오며 살짝 눌린다. 루트와 상호작용 트리거는 움직이지 않는다.
        if (elapsed < HitJoltDuration)
        {
            float k = elapsed / HitJoltDuration;
            PoseStaged(_stageScale * (1f - .06f * Mathf.Sin(k * Mathf.PI)));
            _visual.position += _hitDirection * (Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * .05f);
            _visualPosed = true;
        }
        else if (_visualPosed)
        {
            PoseStaged(_stageScale);
            _visualPosed = false;
        }
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
