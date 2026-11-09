using System.Collections.Generic;
using UnityEngine;

// 손에 든 물건 자세 층(2026-10-31 피드백: 물건을 들어도 팔은 차렷이고 물건은 몸 옆 공중에 떠 있었다).
// 이동 Animator와 PlayerLocomotionAnimator(도구 동작)가 끝난 뒤 LateUpdate에서 팔·손 뼈와 든 물건 위치만 덮는다.
// - 작은 물건·물고기: 오른 아래팔을 들어 몸 앞 오른쪽에서 쥔다. 통나무·판재·설계도·키트: 두 손으로 가슴 앞에 든다.
// - 도끼·곡괭이·잠자리채·낚싯대: 기존 동작 자세는 그대로 두고, 동작이 없을 때만 걸음에 맞춘 작은 흔들림을 더한다.
// - 장착·교체·집어넣기는 직전 팔 자세에서 짧게 이어 붙이고, 물건은 작게 나타나고 작게 사라진다.
// 이동 클립이나 다른 뼈 보정에 기대지 않고 실제 어깨·발 위치로 몸 기준을 잡는다.
// 실행 순서: Animator 평가 → 기본 순서(0) LateUpdate(PlayerLocomotionAnimator·EquipmentSystem) → 이 층(200).
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class PlayerCarryPose : MonoBehaviour
{
    public enum CarryStyle { None, Tool, LooseTool, Small, Fish, TwoHand }

    [SerializeField] float blendTime = .18f;
    [SerializeField] float switchFadeTime = .18f;
    [Tooltip("어깨 반폭 대비 배·가슴 앞면 거리. C-01 메시 실측 약 0.9")]
    [SerializeField] float bodyFrontRatio = .9f;
    [Tooltip("아래팔 대비 손(벙어리 손) 길이. C-01 메시 실측 약 0.85")]
    [SerializeField] float handLengthRatio = .8f;
    [Tooltip("어깨선의 좌우 비틀림을 들기 기준이 따라가는 비율")]
    [SerializeField] float twistFollow = .5f;
    [Tooltip("물고기 머리를 앞에서 바깥(오른쪽)으로 돌리는 각도")]
    [SerializeField] float fishYaw = 40f;
    [SerializeField] bool toolIdleSway = true;

    struct Released { public Transform visual; public Vector3 scale; public float at; }

    EquipmentSystem _equipment;
    Animator _animator;
    Transform _rUpper, _rLower, _rHand, _lUpper, _lLower, _lHand, _lFoot, _rFoot;
    readonly Transform[] _bones = new Transform[6];
    readonly Quaternion[] _pose = new Quaternion[6], _fadeFrom = new Quaternion[6];
    readonly List<Released> _released = new List<Released>();
    float _armLen, _foreLen, _shoulderHalf, _legLen, _nextRigSearch;
    float _right, _left, _toolIdle, _appear, _suspendUntil, _heldSince, _fadeStart, _stride, _strideMean;
    bool _hasPose, _fading;
    GameObject _visual;
    Vector3 _baseScale, _itemOffset, _itemHalf, _centerLocal, _centerFromWrists;
    Quaternion _authored = Quaternion.identity, _itemRot = Quaternion.identity, _frame = Quaternion.identity;
    Vector3 _origin, _rTarget, _rPole, _lTarget, _lPole;

    // 이동 담당이 조회하는 상태. CarryWeight는 이 층이 지금 팔을 덮는 정도(0~1)다.
    public CarryStyle Style { get; private set; }
    public bool IsCarrying => Style == CarryStyle.Small || Style == CarryStyle.Fish || Style == CarryStyle.TwoHand;
    public bool IsTwoHanded => Style == CarryStyle.TwoHand;
    public float RightArmWeight => _right;
    public float LeftArmWeight => _left;
    public float CarryWeight => Mathf.Max(_right, _left);
    // 다른 동작이 팔을 직접 쓰는 동안 true로 두면 들기 자세가 빠진다. 물건은 계속 손을 따라간다.
    public bool Suspended { get; set; }
    public void SuspendFor(float seconds) => _suspendUntil = Mathf.Max(_suspendUntil, Time.time + seconds);

    public static CarryStyle StyleFor(Item item)
    {
        if (item == null) return CarryStyle.None;
        switch (item.toolType)
        {
            case ToolType.Axe: case ToolType.Pickaxe: case ToolType.Net: case ToolType.FishingRod: case ToolType.Shovel: return CarryStyle.Tool;
            case ToolType.Hoe: case ToolType.Weapon: return CarryStyle.LooseTool;
            case ToolType.Building: return CarryStyle.TwoHand;
        }
        // 판재·가구·관리 키트·설계도·작업대 키트·진열대·의자는 두 손, 생선(8)과 낚시 어종(3000번대)은 물고기 자세.
        switch (item.id)
        {
            case 7: case 11: case 2005: case 2012: case 2013: case 2014: case 2020: case 2021: return CarryStyle.TwoHand;
            case 8: return CarryStyle.Fish;
        }
        if (item.itemName == "Wood") return CarryStyle.TwoHand;
        if (item.itemName == "Fish" || item.id >= 3000 && item.id < 3100) return CarryStyle.Fish;
        return CarryStyle.Small;
    }
    public static bool IsCarried(Item item)
    {
        var style = StyleFor(item);
        return style == CarryStyle.Small || style == CarryStyle.Fish || style == CarryStyle.TwoHand;
    }
    // 든 물건의 가장 긴 변(m). 높은 고정 카메라에서도 읽히는 크기.
    public static float HeldSize(Item item)
    {
        var style = StyleFor(item);
        return style == CarryStyle.TwoHand ? .44f : style == CarryStyle.Fish ? .4f : .34f;
    }

    // 장착이 바뀐 이전 모델은 손에 붙은 채 짧게 줄어들며 사라진다.
    public void Release(GameObject visual)
    {
        if (visual == null) return;
        visual.name = "Released_" + visual.name;
        _released.Add(new Released { visual = visual.transform, scale = visual.transform.localScale, at = Time.time });
    }

    void OnDisable()
    {
        foreach (var released in _released) if (released.visual != null) Destroy(released.visual.gameObject);
        _released.Clear();
        _hasPose = _fading = false;
        _right = _left = _toolIdle = 0f;
    }

    void LateUpdate()
    {
        UpdateReleased();
        if (!EnsureRig()) return;
        float dt = Time.deltaTime;
        var visual = _equipment != null ? _equipment.HeldVisual : null;
        if (!ReferenceEquals(visual, _visual)) BeginHeld(visual);
        if (Style != CarryStyle.None && _visual == null) Style = CarryStyle.None;

        bool suspended = Suspended || Time.time < _suspendUntil;
        bool rightWasOff = _right <= .001f, leftWasOff = _left <= .001f;
        float rate = dt / Mathf.Max(.01f, blendTime);
        _right = Mathf.MoveTowards(_right, !suspended && (IsCarrying || Style == CarryStyle.LooseTool) ? 1f : 0f, rate);
        _left = Mathf.MoveTowards(_left, !suspended && IsTwoHanded ? 1f : 0f, rate);
        _appear = Mathf.MoveTowards(_appear, 1f, rate);
        bool busy = ToolBusy();
        bool idleTool = toolIdleSway && Style == CarryStyle.Tool && !suspended && !busy;
        _toolIdle = Mathf.MoveTowards(_toolIdle, idleTool ? 1f : 0f, dt / (idleTool ? .25f : .08f));

        BuildFrame();
        float stride = Stride(dt);
        if (IsCarrying || Style == CarryStyle.LooseTool)
        {
            Targets(stride, Offer(), out var rWrist, out var rPole, out var lWrist, out var lPole);
            float follow = 1f - Mathf.Exp(-dt / .05f);
            _rTarget = rightWasOff ? rWrist : Vector3.Lerp(_rTarget, rWrist, follow);
            _rPole = rightWasOff ? rPole : Vector3.Lerp(_rPole, rPole, follow);
            _lTarget = leftWasOff ? lWrist : Vector3.Lerp(_lTarget, lWrist, follow);
            _lPole = leftWasOff ? lPole : Vector3.Lerp(_lPole, lPole, follow);
        }
        if (_right > .001f) SolveArm(_rUpper, _rLower, _rHand, World(_rTarget), _frame * _rPole, Ease(_right));
        if (_left > .001f) SolveArm(_lUpper, _lLower, _lHand, World(_lTarget), _frame * _lPole, Ease(_left));
        // 도구 대기·걷기: 기존 도구 자세 위에서 왼발이 앞일 때 오른팔이 앞으로 오게 위팔만 조금 흔든다(도구는 손의 자식이라 함께 움직인다).
        if (_toolIdle > .001f)
            _rUpper.rotation = Quaternion.AngleAxis(-stride * 16f * Ease(_toolIdle), _frame * Vector3.right) * _rUpper.rotation;
        ApplySwitchFade();
        PlaceHeld();
        for (int i = 0; i < _bones.Length; i++) _pose[i] = _bones[i].localRotation;
        _hasPose = true;
    }

    bool EnsureRig()
    {
        if (_animator != null && _rHand != null && _lHand != null) return true;
        if (Time.time < _nextRigSearch) return false;
        _nextRigSearch = Time.time + 1f;
        if (_equipment == null) _equipment = GetComponent<EquipmentSystem>();
        _animator = null;
        foreach (var candidate in GetComponentsInChildren<Animator>(true))
            if (candidate.avatar != null && candidate.avatar.isHuman && candidate.avatar.isValid) { _animator = candidate; break; }
        if (_animator == null) return false;
        _rUpper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rLower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        _lUpper = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _lLower = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _lHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
        _lFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _rFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (_rUpper == null || _rLower == null || _rHand == null || _lUpper == null || _lLower == null || _lHand == null)
        { _animator = null; return false; }
        _bones[0] = _rUpper; _bones[1] = _rLower; _bones[2] = _rHand; _bones[3] = _lUpper; _bones[4] = _lLower; _bones[5] = _lHand;
        // 길이는 자세와 무관하다. 목표는 모두 실제 어깨 반폭·팔 길이의 비율로 정해 리그 크기가 바뀌어도 맞는다.
        _foreLen = Vector3.Distance(_rLower.position, _rHand.position);
        _armLen = Vector3.Distance(_rUpper.position, _rLower.position) + _foreLen;
        _shoulderHalf = Mathf.Max(.05f, Vector3.Distance(_lUpper.position, _rUpper.position) * .5f);
        var hip = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        _legLen = hip != null && _lFoot != null ? Mathf.Max(.1f, Vector3.Distance(hip.position, _lFoot.position)) : _armLen;
        _hasPose = false;
        return true;
    }

    void BeginHeld(GameObject visual)
    {
        // 직전 최종 팔 자세에서 이어 붙인다(도구→물건, 물건→도구, 집어넣기 모두).
        if (_hasPose) { System.Array.Copy(_pose, _fadeFrom, _pose.Length); _fadeStart = Time.time; _fading = true; }
        _visual = visual;
        _heldSince = Time.time;
        _appear = 0f;
        Style = visual != null && _equipment != null ? StyleFor(_equipment.HeldItem) : CarryStyle.None;
        if (visual == null) return;
        _baseScale = visual.transform.localScale;
        _authored = _equipment.HeldAuthoredRotation;
        Bounds bounds = _equipment.HeldBounds;
        Vector3 size = bounds.size.sqrMagnitude > 1e-6f ? bounds.size : Vector3.one * .3f;
        _itemOffset = bounds.center;
        _itemRot = ItemRotation(Style, size);
        Vector3 e = size * .5f;
        _itemHalf = Abs(_itemRot * Vector3.right) * e.x + Abs(_itemRot * Vector3.up) * e.y + Abs(_itemRot * Vector3.forward) * e.z;
    }

    // 원래 모델 방향(바닥에 놓인 방향) 위에서 몸 기준으로 돌리는 회전. size는 원래 방향의 크기.
    Quaternion ItemRotation(CarryStyle style, Vector3 size)
    {
        float max = Mathf.Max(size.x, Mathf.Max(size.y, size.z)), min = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        bool standingSheet = min < max * .3f && Mathf.Min(size.x, size.z) <= min + 1e-4f && size.y > min * 2f;
        // CuteFish 모델은 +Z가 머리, +Y가 등(원본 FBX 실측). 긴 축이 X인 모델은 먼저 Z로 돌린다. 머리를 조금 든다.
        if (style == CarryStyle.Fish) return Quaternion.Euler(0f, (size.x > size.z ? -90f : 0f) + fishYaw, 0f) * Quaternion.Euler(-10f, 0f, 0f);
        if (style == CarryStyle.TwoHand)
        {
            // 세워진 통나무처럼 위아래로 긴 덩어리는 눕혀 긴 축을 좌우로 둔다.
            if (!standingSheet && size.y > 1.6f * Mathf.Max(size.x, size.z)) return Quaternion.Euler(0f, 0f, 90f);
            // 가장 긴 수평 축을 좌우로 둔다. 세운 얇은 판(설계도)은 윗변을 몸쪽으로 20° 눕혀 높은 카메라에 면이 보이게 한다.
            Quaternion yaw = Quaternion.Euler(0f, size.z > size.x ? 90f : 0f, 0f);
            return standingSheet ? Quaternion.Euler(-20f, 0f, 0f) * yaw : yaw;
        }
        // 작은 판(스마트폰)은 화면이 앞을 보게 세운 뒤 윗변을 몸쪽으로 25° 눕힌다. 그 밖의 작은 물건은 원래 방향.
        if (standingSheet) return Quaternion.Euler(-25f, 0f, 0f) * Quaternion.Euler(0f, size.x < size.z ? 90f : 0f, 0f);
        return Quaternion.identity;
    }

    // 몸 기준: 원점은 두 어깨 관절의 가운데(걸음·호흡의 위아래 흔들림 포함), 방향은 플레이어 정면에 어깨선 비틀림 일부를 더한다.
    void BuildFrame()
    {
        Vector3 up = transform.up, right = transform.right;
        Vector3 line = Vector3.ProjectOnPlane(_rUpper.position - _lUpper.position, up);
        if (line.sqrMagnitude > 1e-6f) right = Vector3.Slerp(right, line.normalized, twistFollow);
        _frame = Quaternion.LookRotation(Vector3.Cross(right, up), up);
        _origin = (_rUpper.position + _lUpper.position) * .5f;
    }

    Vector3 World(Vector3 local) => _origin + _frame * local;

    // 걸음 위상: 왼발이 앞이면 +. 정지 자세의 발 엇갈림은 느린 평균으로 빼서 서 있을 때는 0에 가깝다.
    float Stride(float dt)
    {
        if (_lFoot == null || _rFoot == null || dt <= 0f) return _stride;
        float raw = Vector3.Dot(_lFoot.position - _rFoot.position, transform.forward) / _legLen;
        _strideMean = Mathf.Lerp(_strideMean, raw, 1f - Mathf.Exp(-dt / .6f));
        _stride = Mathf.Clamp(raw - _strideMean, -.7f, .7f);
        return _stride;
    }

    // 들고 있는 동안 상호작용(PlayAction)하면 물건을 0.4초 동안 살짝 내민다.
    float Offer()
    {
        if (_equipment == null) return 0f;
        float t = Time.time - _equipment.ActionStartedAt;
        return t >= 0f && t < .4f && t < Time.time - _heldSince ? Mathf.Sin(t / .4f * Mathf.PI) : 0f;
    }

    bool ToolBusy()
    {
        if (_equipment == null) return false;
        float action = Time.time - _equipment.ActionStartedAt, failed = Time.time - _equipment.FailedContactAt;
        float pull = Time.time - FishingSpot.LastDirectPullAt;
        return action >= 0f && action < GatherFeedback.HeavyToolReuseDelay + .05f || failed >= 0f && failed < .3f ||
               pull >= 0f && pull < .5f || FishingSpot.ActiveDirectCaster == gameObject;
    }

    // 손목 목표와 팔꿈치 방향(몸 기준 x 오른쪽, y 위, z 앞). 손은 어깨 높이 아래·가슴 앞면 바깥에 두어 큰 머리와 몸통을 피한다.
    void Targets(float stride, float offer, out Vector3 rWrist, out Vector3 rPole, out Vector3 lWrist, out Vector3 lPole)
    {
        float w = _shoulderHalf, a = _armLen, front = _shoulderHalf * bodyFrontRatio, hand = _foreLen * handLengthRatio;
        Vector3 push = new Vector3(0f, a * .06f, a * .12f) * offer;
        lWrist = _lTarget; lPole = new Vector3(-.7f, -1f, -.45f);
        switch (Style)
        {
            case CarryStyle.TwoHand:
            {
                // 물건 뒷면이 배 앞에 닿지 않게 두께만큼 앞으로, 손목은 양옆 면 가운데 조금 아래, 손바닥이 앞쪽 절반을 감싼다.
                Vector3 h = _itemHalf;
                _centerLocal = new Vector3(0f, -a * .5f, front + h.z + a * .08f) + push;
                float x = Mathf.Max(h.x + hand * .2f, w * .8f);
                rWrist = new Vector3(x, _centerLocal.y - h.y * .2f, _centerLocal.z - hand * .45f);
                lWrist = new Vector3(-x, rWrist.y, rWrist.z);
                _centerFromWrists = _centerLocal - (rWrist + lWrist) * .5f;
                // 걸음마다 물건을 좌우로 살짝 기울인다.
                Quaternion roll = Quaternion.AngleAxis(stride * 5f, Vector3.forward);
                rWrist = _centerLocal + roll * (rWrist - _centerLocal);
                lWrist = _centerLocal + roll * (lWrist - _centerLocal);
                rPole = new Vector3(.7f, -1f, -.45f);
                return;
            }
            case CarryStyle.LooseTool:
                // 전용 동작이 없는 도구(괭이 등): 팔꿈치를 굽혀 허리 앞에서 쥔다.
                rWrist = new Vector3(w * 1.15f, -a * .62f, front * .8f + a * (.12f + stride * .25f));
                rPole = new Vector3(.4f, -1f, -.5f);
                return;
            default:
                // 작은 물건·물고기: 위팔은 내리고 아래팔을 앞으로 들어 몸 앞 오른쪽에서 쥔다. 걸음에 맞춰 앞뒤로 조금 흔든다.
                rWrist = new Vector3(w * 1.2f, -a * .42f, front + a * (.18f + stride * .14f)) + push;
                rPole = new Vector3(.45f, -1f, -.35f);
                return;
        }
    }

    // 장착이 바뀐 직후에는 직전 최종 자세에서 현재 결과로 이어 붙인다(뼈 로컬 회전 보간).
    void ApplySwitchFade()
    {
        if (!_fading) return;
        float k = (Time.time - _fadeStart) / Mathf.Max(.01f, switchFadeTime);
        if (k >= 1f) { _fading = false; return; }
        k = Ease(k);
        for (int i = 0; i < _bones.Length; i++) _bones[i].localRotation = Quaternion.Slerp(_fadeFrom[i], _bones[i].localRotation, k);
    }

    // 물건을 최종 손 위치에 붙인다. 방향은 몸 기준이라 손목 뼈 회전과 무관하게 읽히는 방향을 유지한다.
    void PlaceHeld()
    {
        if (_visual == null || !IsCarrying) return;
        var visual = _visual.transform;
        float grow = Ease(_appear), scale = Mathf.Lerp(.5f, 1f, grow);
        visual.localScale = _baseScale * scale;
        Quaternion pose;
        Vector3 center;
        if (IsTwoHanded)
        {
            // 두 손 사이에 끼우고 손목 선의 기울기를 따른다. 처음 나타날 때는 들기 자리에서 커지며 올라오는 손에 잡힌다.
            Vector3 across = _rHand.position - _lHand.position;
            Quaternion tilt = across.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(_frame * Vector3.right, across.normalized) : Quaternion.identity;
            pose = tilt * _frame * _itemRot;
            Vector3 held = (_rHand.position + _lHand.position) * .5f + _frame * _centerFromWrists;
            center = Vector3.Lerp(World(_centerLocal), held, grow);
        }
        else
        {
            // 손바닥(손목에서 손 길이 절반 앞) 위에 올린다. 물고기는 몸통을 쥔다.
            Vector3 along = _rHand.position - _rLower.position;
            Vector3 grip = _rHand.position + along.normalized * (_foreLen * handLengthRatio * .5f);
            pose = Quaternion.AngleAxis(-_stride * 8f, _frame * Vector3.right) * _frame * _itemRot;
            center = grip + transform.up * (_itemHalf.y * scale * (Style == CarryStyle.Fish ? .1f : .5f));
        }
        visual.rotation = pose * _authored;
        visual.position = center - pose * (_itemOffset * scale);
    }

    void UpdateReleased()
    {
        for (int i = _released.Count - 1; i >= 0; i--)
        {
            var released = _released[i];
            float k = (Time.time - released.at) / .14f;
            if (released.visual != null && k < 1f) { released.visual.localScale = released.scale * (1f - Ease(k)); continue; }
            if (released.visual != null) Destroy(released.visual.gameObject);
            _released.RemoveAt(i);
        }
    }

    // 두 뼈 팔 IK. 팔꿈치는 pole 방향으로 굽히고, 결과는 원래 로컬 회전에서 weight만큼 보간한다(손목 로컬 자세는 유지).
    static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole, float weight)
    {
        Quaternion upperFrom = upper.localRotation, lowerFrom = lower.localRotation;
        Vector3 a = upper.position, b = lower.position;
        float first = Vector3.Distance(a, b), second = Vector3.Distance(b, hand.position);
        if (first < .001f || second < .001f) return;
        Vector3 toTarget = target - a;
        if (toTarget.sqrMagnitude < 1e-6f) return;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(first - second) + .001f, (first + second) * .995f);
        Vector3 axis = toTarget.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(pole, axis);
        if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(b - a, axis);
        if (bend.sqrMagnitude < 1e-6f) return;
        bend.Normalize();
        float cos = Mathf.Clamp((first * first + distance * distance - second * second) / (2f * first * distance), -1f, 1f);
        Vector3 elbow = a + axis * (first * cos) + bend * (first * Mathf.Sqrt(1f - cos * cos));
        upper.rotation = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, a + axis * distance - lower.position) * lower.rotation;
        if (weight >= .999f) return;
        upper.localRotation = Quaternion.Slerp(upperFrom, upper.localRotation, weight);
        lower.localRotation = Quaternion.Slerp(lowerFrom, lower.localRotation, weight);
    }

    static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
