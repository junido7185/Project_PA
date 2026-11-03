using UnityEngine;

// 2026-11-02 사용자 "NPC가 도구 들고 다니는 것도 플레이어처럼 자연스러워야": 동행 손 도구를 플레이어와 같은 키 자세
// (PlayerLocomotionAnimator.CarryHeavy / AxeSwing / PickaxeSwing)로 들고 휘두른다. 이동 클립(Animator) 뒤 LateUpdate에서
// 오른팔 두 뼈를 키 방향으로 돌리고, 손잡이(모델 +Y) 방향·날(모델 +X) 방향·쥔 자리를 손에 맞춘다.
// 휘두르는 동안에는 왼손이 자루 아래쪽을 받친다. 휘두르기 시작 시각은 Swing()으로 받는다(표현 전용, 작업 판정은 생산자 FSM 몫).
[DisallowMultipleComponent]
public sealed class NpcHeldToolPose : MonoBehaviour
{
    public GameObject Tool { get; private set; }
    public ToolType Type { get; private set; }
    public float ActionStartedAt { get; private set; } = -100f;
    public bool Swinging => Time.time - ActionStartedAt < GatherFeedback.HeavyToolReturnDelay;

    Animator _animator;
    Transform _rUpper, _rLower, _rHand, _lUpper, _lLower, _lHand, _chest;
    Vector3 _rFinger, _rPalm, _lFinger, _lPalm;
    float _gripLocalY, _supportLocalY, _weight, _support;

    public void Hold(GameObject tool, ToolType type)
    {
        Tool = tool;
        Type = tool != null ? type : ToolType.None;
        if (tool != null) MeasureGrip(tool);
    }

    public void Swing() => ActionStartedAt = Time.time;

    // 플레이어 운반 키(CarryHeavy)는 뒤에서 보는 카메라 기준이라, 앞에서 보이는 NPC는 곡괭이 머리가 얼굴을 가렸다(npc-r2 0060).
    // NPC는 손을 오른쪽 엉덩이 앞에 두고 자루를 오른쪽 어깨 바깥으로 세워 든다. 휘두르기 중간 키는 플레이어와 같다.
    static readonly PlayerLocomotionAnimator.ArmKey CarrySide = new PlayerLocomotionAnimator.ArmKey(0f,
        new Vector3(.3f, -1f, .05f), new Vector3(.25f, -.35f, .9f), new Vector3(.55f, .75f, .35f));
    static readonly PlayerLocomotionAnimator.ArmKey[] AxeKeys = WithCarry(PlayerLocomotionAnimator.AxeSwing);
    static readonly PlayerLocomotionAnimator.ArmKey[] PickaxeKeys = WithCarry(PlayerLocomotionAnimator.PickaxeSwing);

    static PlayerLocomotionAnimator.ArmKey[] WithCarry(PlayerLocomotionAnimator.ArmKey[] keys)
    {
        var copy = (PlayerLocomotionAnimator.ArmKey[])keys.Clone();
        copy[0] = new PlayerLocomotionAnimator.ArmKey(copy[0].t, CarrySide.upper, CarrySide.fore, CarrySide.tool);
        int last = copy.Length - 1;
        copy[last] = new PlayerLocomotionAnimator.ArmKey(copy[last].t, CarrySide.upper, CarrySide.fore, CarrySide.tool);
        return copy;
    }

    bool Bind()
    {
        if (_animator != null && _rHand != null) return true;
        foreach (var candidate in GetComponentsInChildren<Animator>(true))
            if (candidate.avatar != null && candidate.avatar.isHuman && candidate.avatar.isValid) { _animator = candidate; break; }
        if (_animator == null) return false;
        _rUpper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rLower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        _lUpper = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _lLower = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _lHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
        _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
        if (_rUpper == null || _rLower == null || _rHand == null) { _rHand = null; return false; }
        // 바인드 자세에서 손가락·손바닥 축(손 뼈 로컬)을 잡는다. 아래 AlignGripHand가 자루를 감싸 쥐게 쓴다.
        HandAxes(_rLower, _rHand, out _rFinger, out _rPalm);
        HandAxes(_lLower, _lHand, out _lFinger, out _lPalm);
        return true;
    }

    void HandAxes(Transform lower, Transform hand, out Vector3 finger, out Vector3 palm)
    {
        finger = Vector3.forward; palm = Vector3.up;
        if (lower == null || hand == null) return;
        Vector3 direction = (hand.position - lower.position).normalized;
        Vector3 normal = Vector3.ProjectOnPlane(transform.up, direction);
        if (normal.sqrMagnitude < .001f) normal = Vector3.ProjectOnPlane(transform.forward, direction);
        finger = hand.InverseTransformDirection(direction);
        palm = hand.InverseTransformDirection(normal.normalized);
    }

    Vector3 Local(Vector3 v) => (transform.right * v.x + transform.up * v.y + transform.forward * v.z).normalized;

    void LateUpdate()
    {
        bool heavy = Tool != null && (Type == ToolType.Axe || Type == ToolType.Pickaxe);
        _weight = Mathf.MoveTowards(_weight, heavy ? 1f : 0f, Time.deltaTime * 6f);
        if (_weight <= .001f || Tool == null || !Bind()) return;
        float action = Time.time - ActionStartedAt;
        var keys = Type == ToolType.Axe ? AxeKeys : PickaxeKeys;
        // 휘두르기 밖에서는 첫/끝 키(CarrySide: 오른쪽 엉덩이 앞에서 자루를 어깨 바깥으로 세워 든 자세).
        var pose = PlayerLocomotionAnimator.Sample(keys, action);
        float w = _weight;
        if (_chest != null && Mathf.Abs(pose.chest) > .01f)
            _chest.rotation = Quaternion.AngleAxis(pose.chest * w, transform.right) * _chest.rotation;
        PlayerLocomotionAnimator.Aim(_rUpper, _rLower, Local(pose.upper), w);
        PlayerLocomotionAnimator.Aim(_rLower, _rHand, Local(pose.fore), w);

        var visual = Tool.transform;
        Vector3 handle = Vector3.Slerp(visual.up, Local(pose.tool), w).normalized;
        Vector3 blade = Vector3.ProjectOnPlane(transform.forward, handle);
        if (blade.sqrMagnitude < 1e-4f) blade = Vector3.ProjectOnPlane(transform.up, handle);
        visual.rotation = Quaternion.LookRotation(Vector3.Cross(blade.normalized, handle), handle);
        visual.position = _rHand.position - visual.up * (_gripLocalY * visual.lossyScale.y);
        SupportGrip(action, visual);
    }

    // 휘두르는 동안 오른손 쥔 자리를 몸 앞에 두고 왼손이 자루 아래쪽을 받친다(플레이어 ApplySupportGrip과 같은 규칙).
    void SupportGrip(float action, Transform visual)
    {
        if (_lUpper == null || _lLower == null || _lHand == null) return;
        bool wanted = action >= 0f && action < GatherFeedback.HeavyToolReturnDelay + .02f;
        _support = Mathf.MoveTowards(_support, wanted ? 1f : 0f, Time.deltaTime * 12f);
        if (_support <= .001f) return;
        Quaternion toolRotation = visual.rotation;
        Vector3 separation = visual.up * ((_supportLocalY - _gripLocalY) * visual.lossyScale.y);
        Vector3 target = _rHand.position;
        target += transform.forward * Mathf.Max(0f, .28f - Vector3.Dot(target - transform.position, transform.forward));
        float rightReach = (Vector3.Distance(_rUpper.position, _rLower.position) + Vector3.Distance(_rLower.position, _rHand.position)) * .97f;
        float leftReach = (Vector3.Distance(_lUpper.position, _lLower.position) + Vector3.Distance(_lLower.position, _lHand.position)) * .97f;
        for (int i = 0; i < 4; i++)
        {
            target = _lUpper.position + Vector3.ClampMagnitude(target + separation - _lUpper.position, leftReach) - separation;
            target = _rUpper.position + Vector3.ClampMagnitude(target - _rUpper.position, rightReach);
        }
        PlayerLocomotionAnimator.SolveArm(_rUpper, _rLower, _rHand, target, transform.right, _support);
        PlayerLocomotionAnimator.AlignGripHand(_rHand, _rFinger, _rPalm, visual.up, -transform.right, _support);
        visual.rotation = toolRotation;
        visual.position = _rHand.position - visual.up * (_gripLocalY * visual.lossyScale.y);
        Vector3 support = visual.TransformPoint(new Vector3(0f, _supportLocalY, 0f));
        PlayerLocomotionAnimator.SolveArm(_lUpper, _lLower, _lHand, support, -transform.right, _support);
        PlayerLocomotionAnimator.AlignGripHand(_lHand, _lFinger, _lPalm, visual.up, transform.right, _support);
    }

    // 손잡이 쪽 끝(모델 -Y)에서 12% 안쪽을 쥐고, 32% 자리를 받친다. 메시 bounds를 도구 로컬로 옮겨 잰다.
    void MeasureGrip(GameObject tool)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var filter in tool.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float y = tool.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)).y;
                min = Mathf.Min(min, y); max = Mathf.Max(max, y);
            }
        }
        if (min > max) { min = 0f; max = 0f; }
        float length = max - min;
        _gripLocalY = min + length * .12f;
        _supportLocalY = min + length * .32f;
    }
}
