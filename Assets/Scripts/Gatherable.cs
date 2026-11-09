using UnityEngine;

public class Gatherable : MonoBehaviour, IInteractable
{
    public Item dropItem; // ⭐ ItemData -> Item
    public ToolType requiredTool;

    // D4 표시 전용 바인딩. 비어 있으면 렌더러를 가진 첫 자식을 흔든다(루트에 렌더러가 있는 기존 자원은 기존 스케일 반응).
    // 루트·상호작용 트리거·타격 수·보상·저장은 이 값과 무관하다. 그루터기는 표시만 하며 자원 권한이 없다.
    [SerializeField] Transform treeVisual;
    [SerializeField] GameObject stumpVisual;

    // DIRECT-GATHERING-01A: explicit generated-resource mode; legacy behavior stays intact.
    WorldPersistenceService _directPersistence;
    WorldGenerationResult _directWorld;
    Item _directReward;
    string _directSpawnKey;
    int _directHits;
    bool _directDepleted, _contactPending, _felledPendingClaim;
    float _directHitAt = -10f;
    Vector3 _directBaseScale;
    string _directFeedback;
    public bool IsDirectWorld => _directWorld != null;
    public string DirectSpawnKey => _directSpawnKey;
    public int DirectRemainingHits => _directHits;
    public bool DirectDepleted => _directDepleted;

    Transform _visual;
    Vector3 _visualRestPosition, _visualRestScale = Vector3.one, _pivotLocal, _hitAxis = Vector3.right;
    Quaternion _visualRestRotation;
    bool _visualPosed, _falling;
    Coroutine _fallRoutine;
    const float HitSwayDuration = .45f, FallDuration = .6f, LandHold = .3f, SinkDuration = .35f;
    public Transform PresentationVisual => _visual;
    public GameObject StumpVisual => stumpVisual;
    public bool PresentationFalling => _falling;

    // 빌더/어댑터가 씬 연출 대상을 런타임에 지정할 때 쓴다. null 인자는 기존(직렬화) 값을 유지한다.
    public void ConfigurePresentation(Transform visual, GameObject stump = null)
    {
        if (_falling) EndFall();
        ResetVisualPose();
        if (visual != null) treeVisual = visual;
        if (stump != null) stumpVisual = stump;
        _visual = null;
        ResolveVisual();
        if (IsDirectWorld) ApplyDirectVisibility();
    }

    public void ConfigureDirectWorld(WorldPersistenceService persistence,
        WorldResourceSpawnRecord spawn, Item reward, int hits = 3)
    {
        if (persistence == null || persistence.ActiveGeneratedWorld == null || spawn.Kind != WorldResourceKind.Timber || reward == null)
            throw new System.ArgumentException("Invalid direct Timber binding.");
        bool registered = false;
        foreach (var candidate in persistence.ActiveGeneratedWorld.ResourceSpawns)
            if (candidate.SpawnKey == spawn.SpawnKey && candidate.Kind == spawn.Kind && candidate.Coordinate == spawn.Coordinate)
                registered = true;
        if (!registered) throw new System.ArgumentException("Direct resource spawn is not registered.");
        if (_falling) EndFall();
        _directPersistence = persistence;
        _directWorld = persistence.ActiveGeneratedWorld;
        _directSpawnKey = spawn.SpawnKey;
        _directReward = reward;
        _directHits = Mathf.Clamp(hits, 2, 3);
        _directBaseScale = transform.localScale;
        _directFeedback = "도끼로 나무 채집";
        ResolveVisual();
        ResourcePresentation.BrightenCanopy(_visual);
        RefreshDirectState();
    }

    bool RefreshDirectState()
    {
        if (_directPersistence == null || !ReferenceEquals(_directWorld, _directPersistence.ActiveGeneratedWorld))
            return false;
        var states = _directPersistence.CaptureState(transform.position, null).resourceStates;
        var state = states?.Find(entry => entry != null && entry.spawnKey == _directSpawnKey);
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        // 베어 낸 뒤 드롭을 줍기 전까지는 세션에서만 사라진 상태다(저장에는 줍기 후 확정).
        _directDepleted = _felledPendingClaim || state != null && state.consumed && day < state.respawnDay;
        ApplyDirectVisibility();
        return !_directDepleted;
    }

    // 고갈 판정은 즉시 콜라이더를 끈다. 렌더러만 쓰러지는 연출이 끝날 때까지 남는다.
    void ApplyDirectVisibility()
    {
        bool visible = !_directDepleted || _falling;
        if (_directDepleted && stumpVisual == null) CreateDirectStump();
        foreach (var renderer in GetComponentsInChildren<Renderer>()) if (!IsStump(renderer)) renderer.enabled = visible;
        foreach (var collider in GetComponentsInChildren<Collider>()) if (!IsStump(collider)) collider.enabled = !_directDepleted;
        if (_visual != null && !_visual.IsChildOf(transform))
        {
            foreach (var renderer in _visual.GetComponentsInChildren<Renderer>()) if (!IsStump(renderer)) renderer.enabled = visible;
            foreach (var collider in _visual.GetComponentsInChildren<Collider>()) if (!IsStump(collider)) collider.enabled = !_directDepleted;
        }
        if (stumpVisual != null && !transform.IsChildOf(stumpVisual.transform) && stumpVisual.activeSelf != _directDepleted)
            stumpVisual.SetActive(_directDepleted);
    }

    bool IsStump(Component component) => stumpVisual != null && component.transform.IsChildOf(stumpVisual.transform);

    // D4: 벤 나무 자리에 로컬 TreeStump 모델을 남긴다(충돌·NavMesh·저장 없음, 표시 전용). 저장된 소진 상태로
    // 다시 들어와도 같은 자리에 생긴다. 회전은 spawn key로 정해 매번 같다.
    void CreateDirectStump()
    {
        if (!IsDirectWorld) return;
        var assets = FirstDayStudioAssets.Load();
        if (assets == null || assets.stump == null) return;
        Vector3 root = _visual != null ? ResourcePresentation.ToWorld(_visual, _pivotLocal) : transform.position;
        float yaw = (_directSpawnKey != null ? (_directSpawnKey.GetHashCode() & 0xFFFF) : 0) % 360;
        stumpVisual = FirstDayStudioAssets.Place(assets.stump, transform, root, .45f, yaw);
        stumpVisual.name = "DirectStump";
        foreach (var collider in stumpVisual.GetComponentsInChildren<Collider>()) Destroy(collider);
    }

    void ResolveVisual()
    {
        if (_visual != null) return;
        Transform stump = stumpVisual != null ? stumpVisual.transform : null;
        _visual = treeVisual != null ? treeVisual
            : GetComponent<Renderer>() == null ? ResourcePresentation.FindChildVisual(transform, stump) : null;
        // 루트(트리거)나 그 조상은 연출 대상으로 쓰지 않는다.
        if (_visual == null || transform.IsChildOf(_visual) || stump != null && _visual.IsChildOf(stump)) { _visual = null; return; }
        _visualRestPosition = _visual.localPosition;
        _visualRestRotation = _visual.localRotation;
        _visualRestScale = _visual.localScale;
        _pivotLocal = ResourcePresentation.RootPivotLocal(_visual);
    }

    void PoseVisual(float angle)
    {
        _visual.localPosition = _visualRestPosition;
        _visual.localRotation = _visualRestRotation;
        _visual.RotateAround(ResourcePresentation.ToWorld(_visual, _pivotLocal), _hitAxis, angle);
        _visualPosed = true;
    }

    void ResetVisualPose()
    {
        if (_visual != null && _visualPosed)
        {
            _visual.localPosition = _visualRestPosition;
            _visual.localRotation = _visualRestRotation;
            _visual.localScale = _visualRestScale;
        }
        _visualPosed = false;
    }

    // 쓰러짐은 표시 전용이다. 고갈·보상·저장 판정은 시작 전에 이미 확정되어 있다.
    System.Collections.IEnumerator FallThenHide()
    {
        float lie = FallAngle();
        MeasureCrown(Vector3.Cross(_hitAxis, Vector3.up));
        for (float t = 0f; t < FallDuration; t += Time.deltaTime)
        {
            float k = t / FallDuration;
            PoseFalling(lie * k * k);
            yield return null;
        }
        // 땅에 닿은 순간 작은 튕김 뒤 잠깐 누워 있어야 쓰러짐이 읽힌다.
        for (float t = 0f; t < LandHold; t += Time.deltaTime)
        {
            PoseFalling(lie - 5f * Mathf.Sin(Mathf.Clamp01(t / .16f) * Mathf.PI));
            yield return null;
        }
        // 누운 나무가 한 프레임에 사라지지 않게 뿌리 쪽으로 줄어들며 꺼진다(D4 critic: 끝이 급함).
        for (float t = 0f; t < SinkDuration; t += Time.deltaTime)
        {
            float k = t / SinkDuration;
            PoseFalling(lie, Mathf.Lerp(1f, .15f, k * k));
            yield return null;
        }
        _fallRoutine = null;
        EndFall();
    }

    // r2 verifier: 넓은 침엽수 수관(반지름 ~1.7m)이 84°에서 땅 아래로 ~1.6m 들어가 눕자마자 사라져 보였다.
    // r5 verifier: 통째로 들어 올리면 줄기 밑동이 그루터기 위 머리 높이에 떠 있었다. 넓은 수관은 가지가 먼저
    // 땅에 닿으므로 덜 눕히고(수관 폭/높이로 84°→~65°), 들어 올림은 0.45m까지만 허용했다(표시 전용).
    // r11: 그 들어 올림은 측면에서 밑동이 그루터기 위에 떠 보여 제거했다(아래 PoseFalling).

    float FallAngle()
    {
        if (!ResourcePresentation.TryGetBounds(_visual, out var rest) || rest.size.y < .1f) return 84f;
        float spread = Mathf.Max(rest.extents.x, rest.extents.z) / rest.size.y;
        return Mathf.Lerp(84f, 64f, Mathf.InverseLerp(.2f, .38f, spread));
    }

    // r11 (r9 side fixture: the 0.45 m lift left the cut base floating above the stump). The base stays on
    // the stump; the crown-width angle above limits how deep the branches overlap the ground while lying.
    // r12 (r11 independent review: the lying crown's lower tiers still cut deep into the ground). Branches that
    // reach the ground fold in toward the trunk: from first crown contact the visual is squeezed along the fall
    // direction just enough to keep its submesh corners near the ground (at most to CrownFoldMin). Squeezing
    // the whole crown radially read as a thin skewer from the play camera (r12p), so only the axis facing the
    // ground folds; the tree turns up to 45 degrees about its trunk while falling to line that local axis up
    // with the fall. Base pin, lie angle, stump, reward and save are unchanged.
    const float CrownFoldMin = .6f, CrownGroundAllow = .3f;
    readonly System.Collections.Generic.List<Vector2> _crownCorners = new System.Collections.Generic.List<Vector2>();
    Vector3 _foldAxes = new Vector3(1f, 0f, 0f), _pivotInVisual;
    float _fallTwist;

    // Rest-pose submesh bound corners on the falling side, as (height above root, reach toward the fall).
    void MeasureCrown(Vector3 fall)
    {
        _crownCorners.Clear();
        PoseVisual(0f);
        Vector3 root = ResourcePresentation.ToWorld(_visual, _pivotLocal);
        _pivotInVisual = _visual.InverseTransformPoint(root);
        // The local axis that points up at rest is the trunk; of the other two, fold the one closest to the fall.
        Vector3[] axes = { _visual.right, _visual.up, _visual.forward };
        int trunk = 0;
        for (int i = 1; i < 3; i++) if (Mathf.Abs(axes[i].y) > Mathf.Abs(axes[trunk].y)) trunk = i;
        _fallTwist = 180f;
        for (int i = 0; i < 3; i++)
        {
            if (i == trunk) continue;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float twist = Vector3.SignedAngle(Vector3.ProjectOnPlane(axes[i] * sign, Vector3.up), fall, Vector3.up);
                if (Mathf.Abs(twist) >= Mathf.Abs(_fallTwist)) continue;
                _fallTwist = twist;
                _foldAxes = i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
            }
        }
        _visual.RotateAround(root, Vector3.up, _fallTwist);
        foreach (var filter in _visual.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh;
            var renderer = filter.GetComponent<Renderer>();
            if (mesh == null || renderer == null || !renderer.enabled) continue;
            var matrix = filter.transform.localToWorldMatrix;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var b = mesh.GetSubMesh(sub).bounds;
                for (int c = 0; c < 8; c++)
                {
                    var corner = matrix.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3(c & 1, (c >> 1) & 1, (c >> 2) & 1) * 2f - Vector3.one));
                    float reach = Vector3.Dot(corner - root, fall);
                    if (reach > 0f) _crownCorners.Add(new Vector2(corner.y - root.y, reach));
                }
            }
        }
    }

    // A corner at height h and reach u ends at h·cos(a) - fold·u·sin(a) relative to the root.
    float CrownFold(float angle)
    {
        float s = Mathf.Sin(angle * Mathf.Deg2Rad), c = Mathf.Cos(angle * Mathf.Deg2Rad), fold = 1f;
        if (s <= 0f) return 1f;
        foreach (var corner in _crownCorners)
            fold = Mathf.Min(fold, (corner.x * c + CrownGroundAllow) / (corner.y * s));
        return Mathf.Clamp(fold, CrownFoldMin, 1f);
    }

    void PoseFalling(float angle, float shrink = 1f)
    {
        float fold = CrownFold(angle);
        _visual.localPosition = _visualRestPosition;
        _visual.localRotation = _visualRestRotation;
        _visual.localScale = Vector3.Scale(_visualRestScale, (Vector3.one - _foldAxes * (1f - fold)) * shrink);
        // Fold and shrink around the root so the cut base stays on the stump; the twist is done before contact.
        Vector3 root = ResourcePresentation.ToWorld(_visual, _pivotLocal);
        _visual.RotateAround(root, Vector3.up, _fallTwist * Mathf.SmoothStep(0f, 1f, angle / 35f));
        _visual.position += root - _visual.TransformPoint(_pivotInVisual);
        _visual.RotateAround(root, _hitAxis, angle);
        _visualPosed = true;
    }

    void EndFall()
    {
        if (_fallRoutine != null) StopCoroutine(_fallRoutine);
        _fallRoutine = null;
        _falling = false;
        ResetVisualPose();
        ApplyDirectVisibility();
    }

    void OnDisable()
    {
        if (_falling) EndFall();
        else ResetVisualPose();
    }

    public const int DirectDropAmount = 4;
    string DirectPrompt => _directDepleted ? "채집 완료" : $"기본 도끼 가능 · {ItemDisplayName.For(_directReward)}×{DirectDropAmount} · 남은 {_directHits}회";

    void InteractDirect(GameObject interactor)
    {
        if (_contactPending || !RefreshDirectState() || interactor == null) return;
        var inventory = interactor.GetComponent<Inventory>();
        Vector3 delta = interactor.transform.position - transform.position;
        if (inventory == null || inventory != Inventory.instance ||
            Vector3.ProjectOnPlane(delta, Vector3.up).magnitude > 2.05f || Mathf.Abs(delta.y) > 2f) return;
        if (EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Axe)
        {
            _directFeedback = "도끼를 손에 들어주세요";
            return;
        }
        if (Time.time - _directHitAt < .25f) return;
        StartCoroutine(DirectContact(interactor, inventory));
    }

    // 스윙이 나무에 닿는 순간 타격이 성립한다. 그 사이의 연타·도구 교체는 타격/보상을 만들지 않는다.
    System.Collections.IEnumerator DirectContact(GameObject interactor, Inventory inventory)
    {
        _contactPending = true;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _contactPending = false;
        if (interactor == null || _directDepleted || EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Axe) yield break;
        _directHitAt = Time.time;
        // 나무는 맞은 반대쪽(플레이어에서 먼 쪽)으로 흔들리고 쓰러진다.
        Vector3 away = Vector3.ProjectOnPlane(transform.position - interactor.transform.position, Vector3.up);
        _hitAxis = Vector3.Cross(Vector3.up, away.sqrMagnitude > .01f ? away.normalized : Vector3.forward);
        // 기본 추적 카메라에서 플레이어 몸에 가리지 않게 줄기 축(뿌리 위 1m), 화면 가로로 플레이어 반대쪽에 맞는다(표시 전용).
        Vector3 trunk = _visual != null ? ResourcePresentation.ToWorld(_visual, _pivotLocal) : transform.position;
        Vector3 contact = GatherFeedback.VisibleContact(trunk, trunk.y + 1.0f, .5f, interactor.transform.position);
        GatherFeedback.Hit(contact, false);
        // 도끼 힘(ToolPower: 기본 1·구리 2·철 3)만큼 깎는다(TOOL_PROGRESSION.md).
        int power = Mathf.Max(1, ToolPower.For(EquipmentSystem.CurrentHeld(interactor)));
        if (_directHits > power)
        {
            _directHits -= power;
            _directFeedback = "도끼 타격";
            yield break;
        }
        // 성공한 작업 결과: 나무가 쓰러지고 Wood가 바닥에 떨어진다. 도구 내구도는 이 결과에만 줄어든다.
        _directHits = 0;
        _felledPendingClaim = true;
        // D4 critic: 플레이어 반대(=카메라 반대)로 쓰러지면 줄기가 가려진다. 화면 가로의 열린 쪽으로 눕힌다.
        Vector3 fall = GatherFeedback.OpenSide(trunk, interactor.transform.position);
        _hitAxis = Vector3.Cross(Vector3.up, fall);
        _falling = _visual != null;
        RefreshDirectState();
        if (_falling) _fallRoutine = StartCoroutine(FallThenHide());
        _directFeedback = "나무를 베었어요";
        ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        GatherFeedback.SpawnDrop(_directReward, transform.position, interactor.transform.position, ClaimDirectDrop, DirectDropAmount);
    }

    void ClaimDirectDrop()
    {
        _felledPendingClaim = false;
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
        if (_falling) return;
        // 뿌리 피벗 기준의 감쇠 흔들림. 루트와 상호작용 트리거는 움직이지 않는다.
        if (elapsed < HitSwayDuration) PoseVisual(Mathf.Sin(elapsed * 28f) * Mathf.Exp(-elapsed * 7f) * 4f);
        else if (_visualPosed) ResetVisualPose();
    }

    public void Interact(GameObject interactor)
    {
        if (IsDirectWorld) { InteractDirect(interactor); return; }
        Item heldItem = EquipmentSystem.CurrentHeld(interactor);

        if (requiredTool != ToolType.None)
        {
            if (heldItem == null || heldItem.toolType != requiredTool)
            {
                Debug.Log($"🚫 {requiredTool}이(가) 필요합니다.");
                return;
            }
        }

        Animator playerAnim = interactor.GetComponentInChildren<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("DoChop");

        Harvest();
    }

    public string GetInteractPrompt() => IsDirectWorld ? DirectPrompt : $"{dropItem.itemName} 채집하기";

    public void Harvest()
    {
        if (IsDirectWorld) return; // Do not bypass direct-mode hit and reward checks.
        if (dropItem != null)
        {
            Inventory.instance.AddItem(dropItem);
        }
        Destroy(gameObject);
    }
}

// D4: 자원 표시 연출 공용 도우미(Gatherable/MiningSpot/DaytimeStockPrepPoint). 판정·보상·저장 권한은 없다.
static class ResourcePresentation
{
    // 렌더러(월드 라벨 TMP 제외)를 가진 첫 직계 자식. 제외 대상(그루터기 등)은 건너뛴다.
    public static Transform FindChildVisual(Transform root, Transform exclude = null)
    {
        foreach (Transform child in root)
        {
            if (exclude != null && child.IsChildOf(exclude) || child.GetComponentInChildren<PrototypeWorldLabel>(true) != null) continue;
            foreach (var renderer in child.GetComponentsInChildren<Renderer>())
                if (renderer.GetComponent<TMPro.TextMeshPro>() == null) return child;
        }
        return null;
    }

    public static bool TryGetBounds(Transform visual, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer.GetComponent<TMPro.TextMeshPro>() != null) continue;
            if (any) bounds.Encapsulate(renderer.bounds); else { bounds = renderer.bounds; any = true; }
        }
        return any;
    }

    // 렌더러 바닥 중심(뿌리)을 부모 공간 좌표로 저장한다. 렌더러가 보이지 않으면 모델 자체 피벗을 쓴다.
    public static Vector3 RootPivotLocal(Transform visual)
    {
        Vector3 world = TryGetBounds(visual, out var b) ? new Vector3(b.center.x, b.min.y, b.center.z) : visual.position;
        return visual.parent != null ? visual.parent.InverseTransformPoint(world) : world;
    }

    public static Vector3 ToWorld(Transform visual, Vector3 parentLocal) =>
        visual.parent != null ? visual.parent.TransformPoint(parentLocal) : parentLocal;

    static readonly System.Collections.Generic.Dictionary<Material, Material> _brightCanopy =
        new System.Collections.Generic.Dictionary<Material, Material>();

    // 직접 채집 나무는 어댑터가 따로 만들어 FirstDayWorldPresentation.ApplyFoliageLook의 밝은 수관을 받지 못해
    // 장식 나무 옆에서 검게 보였다(D4 motion review). 같은 tint의 공유 런타임 복사본을 쓴다. 줄기·재질 자산은 그대로다.
    public static void BrightenCanopy(Transform visual)
    {
        if (visual == null) return;
        foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var m = materials[i];
                if (m == null || m.name.EndsWith("_PACanopy") || !(m.name.StartsWith("Green") || m.name.StartsWith("DarkGreen"))) continue;
                if (!_brightCanopy.TryGetValue(m, out var bright) || bright == null)
                {
                    bright = new Material(m) { name = m.name + "_PACanopy", hideFlags = HideFlags.DontSave };
                    var tint = m.name.StartsWith("Dark") ? new Color(.40f, .58f, .22f) : new Color(.55f, .72f, .28f);
                    if (bright.HasProperty("_BaseColor")) bright.SetColor("_BaseColor", tint);
                    if (bright.HasProperty("_Color")) bright.SetColor("_Color", tint);
                    _brightCanopy[m] = bright;
                }
                materials[i] = bright; changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }
}
