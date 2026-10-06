using UnityEngine;

// 이동 권위는 PlayerController/CharacterController에 두고, 실제 이동 결과만 Animator 파라미터로 전달한다.
[DisallowMultipleComponent]
public sealed class PlayerLocomotionAnimator : MonoBehaviour
{
    public const string ControllerResource = "PlayerLocomotion/PlayerLocomotion";

    // 블렌드 임계값(게임 속도, m/s): 걷기 입력 4.2는 Jog, Shift 6.72는 Sprint가 담당한다.
    public const float WalkNaturalSpeed = 0.78f;
    public const float JogNaturalSpeed = 4.29f;
    public const float SprintNaturalSpeed = 6.60f;

    // 각 클립이 다리 보정 C-01에서 지지발을 고정한 채 이동하는 속도(m/s). 재생 배율 = 실제 속도 / 이 값.
    // PA_Style_Run 3.54, PA_Style_Sprint 5.06: 게임 Animator에서 제자리·재생 배율 1로 재생해 지면에 닿은 메시(최저 10정점)의 뒤쪽 속도를 잰 값.
    // 편집기 SampleAnimation 측정은 약 1.3~1.4배 크게 나와 쓰지 않는다. 걷기 구간도 Run 클립을 쓴다.
    // 2026-09-29 기본 이동 보폭을 늘려 반복을 늦춤(사용자 피드백). 발이 맞는 값은 4.6이지만, 사용자 선택으로 애니메이션만 더 늦춰
    // 6.0을 쓴다(4.2m/s에서 재생 배율 약 0.7, 지지발 약 1.2m/s 미끄러짐 감수).
    [SerializeField] float walkStrideSpeed = 6.0f;
    [SerializeField] float jogStrideSpeed = 6.0f;
    [SerializeField] float sprintStrideSpeed = 5.06f;

    static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
    static readonly int PlaybackId = Animator.StringToHash("LocomotionPlayback");
    static readonly int GroundedId = Animator.StringToHash("Grounded");
    static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
    static readonly int JumpId = Animator.StringToHash("Jump");

    PlayerController _player;
    CharacterController _body;
    EquipmentSystem _equipment;
    Animator _animator;
    Transform _rightUpperArm, _rightLowerArm, _rightHand, _leftUpperArm, _leftLowerArm, _leftHand, _chest;
    float _airArms, _toolWeight;
    GameObject _measuredVisual;
    float _gripLocalY, _supportLocalY, _supportGripWeight;
    Vector3 _rightFingerAxis, _leftFingerAxis, _rightPalmAxis, _leftPalmAxis;
    bool _tipAtOrigin;
    readonly RaycastHit[] _groundHits = new RaycastHit[8];
    Vector3 _lastPosition;
    float _speed, _airTime, _visualBaseY, _groundGap;
    int _lastJumpCount;

    public float MoveSpeed => _speed;
    public bool Grounded { get; private set; } = true;
    public Animator Animator => _animator;
    public bool SupportingTool { get; private set; }
    public float SupportGripError { get; private set; } = float.PositiveInfinity;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _body = GetComponent<CharacterController>();
        _equipment = GetComponent<EquipmentSystem>();

        // NPC용 절차 애니메이터는 controller를 비우고 LateUpdate에서 뼈를 덮어쓰므로 플레이어에서는 끈다.
        var procedural = GetComponent<NpcHumanoidProceduralAnimator>();
        if (procedural != null) procedural.enabled = false;

        foreach (var candidate in GetComponentsInChildren<Animator>(true))
            if (candidate.avatar != null && candidate.avatar.isHuman && candidate.avatar.isValid) { _animator = candidate; break; }
        var controller = Resources.Load<RuntimeAnimatorController>(ControllerResource);
        if (_animator == null || controller == null)
        {
            Debug.LogError("[PlayerLocomotion] Humanoid Animator 또는 PlayerLocomotion controller를 찾지 못했습니다.");
            enabled = false;
            return;
        }
        _animator.enabled = true;
        _animator.applyRootMotion = false;
        // 런타임 리그 보정(PlayerRigFix)·발 고정(PlayerFootLock) 실험은 끈다: 시각 판정 전 근거가 없고 기본 자세가 변형됐다.
        // 원인은 C-01 원본 리그이므로 원본 모델 수정으로 해결한다(Docs 개발일지 2026-09-29 참고).
        _animator.runtimeAnimatorController = controller;
        _animator.Rebind();
        // 표시 모델은 CharacterController 바닥과 지면 사이의 실제 간격만큼 내린다(LateUpdate).
        _visualBaseY = SoleAlignedBaseY(BindSoleLocalY());
        _groundGap = _body != null ? _body.skinWidth : 0f;
        ApplyVisualGrounding();
        _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rightLowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
        _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        _leftUpperArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _leftLowerArm = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
        BindHandAxes(_rightLowerArm, _rightHand, out _rightFingerAxis, out _rightPalmAxis);
        BindHandAxes(_leftLowerArm, _leftHand, out _leftFingerAxis, out _leftPalmAxis);
        _lastPosition = transform.position;
        _lastJumpCount = _player != null ? _player.JumpCount : 0;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_animator == null || dt <= 0f) return;

        bool controllable = _player != null && _player.enabled && _body != null && _body.enabled;
        Vector3 delta = transform.position - _lastPosition;
        _lastPosition = transform.position;
        delta.y = 0f;
        float measured = controllable ? delta.magnitude / dt : 0f;
        // 포즈 이동·워프는 달리기 속도로 보지 않는다.
        if (measured > SprintNaturalSpeed * 2f) measured = 0f;
        _speed = Mathf.Lerp(_speed, measured, 1f - Mathf.Exp(-18f * dt));
        if (_speed < 0.02f && measured == 0f) _speed = 0f;

        _animator.SetFloat(MoveSpeedId, _speed);
        _animator.SetFloat(PlaybackId, _speed < WalkNaturalSpeed ? 1f : Mathf.Clamp(_speed / StrideSpeedAt(_speed), 0.6f, 1.4f));

        // 경사·작은 턱의 한두 프레임 isGrounded 끊김은 낙하로 보지 않는다.
        // 아래 지면이 stepOffset보다 멀면 실제 단차 낙하이므로 발이 떨어진 프레임부터 공중으로 본다.
        bool airborne = controllable && !_body.isGrounded;
        _airTime = airborne ? _airTime + dt : 0f;
        int jumps = _player != null ? _player.JumpCount : 0;
        if (jumps != _lastJumpCount)
        {
            _lastJumpCount = jumps;
            _animator.SetTrigger(JumpId);
        }
        // 한 번 낙하로 판정되면 지면에 가까워져도 실제 접지 전까지 공중을 유지한다.
        Grounded = !airborne || (Grounded && _airTime < 0.18f && (_player == null || _player.VerticalSpeed <= 0.5f) &&
                                 GroundDistance(_body.stepOffset) <= _body.stepOffset);
        _animator.SetBool(GroundedId, Grounded);
        _animator.SetFloat(VerticalSpeedId, _player != null ? _player.VerticalSpeed : 0f);
    }

    // 블렌드 트리와 같은 구간 가중치로 두 클립의 보폭 속도를 보간한다.
    float StrideSpeedAt(float speed)
    {
        if (speed <= JogNaturalSpeed)
            return Mathf.Lerp(walkStrideSpeed, jogStrideSpeed, Mathf.InverseLerp(WalkNaturalSpeed, JogNaturalSpeed, speed));
        return Mathf.Lerp(jogStrideSpeed, sprintStrideSpeed, Mathf.InverseLerp(JogNaturalSpeed, SprintNaturalSpeed, speed));
    }

    // 스폰/워프 직후 CC는 skin 안쪽(예: 0.04m)에 멈춰 있고 첫 수평 이동 뒤에야 skinWidth 높이로 정렬된다.
    // 고정 skinWidth 대신 실측 간격을 쓰되 기존 보정량(skinWidth)을 넘지 않게 해 경사·단차 가장자리에서 더 내려가지 않는다.
    void ApplyVisualGrounding()
    {
        if (_animator.transform == transform || _body == null) return;
        if (_body.enabled)
            _groundGap = Mathf.Clamp(GroundDistance(_body.skinWidth * 2f), 0f, _body.skinWidth);
        Vector3 local = _animator.transform.localPosition;
        local.y = _visualBaseY - _groundGap / _animator.transform.parent.lossyScale.y;
        _animator.transform.localPosition = local;
    }

    // 배치 코드는 렌더러 bounds로 모델을 올려 실제 신발 바닥보다 약 16cm 높게 둔다. 바인드 메시의 가장 낮은 정점(신발 바닥)이
    // 플레이어 루트 높이에 오도록 표시 모델 기준 높이를 다시 정한다. 스타일 클립은 바인드 신발 바닥 높이에 지지발을 맞춘다.
    float SoleAlignedBaseY(float soleLocalY)
    {
        var model = _animator.transform;
        if (model == transform || model.parent == null) return model.localPosition.y;
        // 모델이 플레이어 루트의 자식이든 중첩이든 세계 높이 차이로 맞춘다.
        float current = model.position.y - transform.position.y;
        float desired = -soleLocalY * model.lossyScale.y;
        return model.localPosition.y + (desired - current) / model.parent.lossyScale.y;
    }

    // 바인드 메시의 가장 낮은 점(신발 바닥)의 표시 모델 로컬 높이. 메시 정점은 읽기 불가라 메시 bounds 모서리로 구한다.
    // 스킨 노드는 애니메이션되지 않으므로 자세와 무관하다.
    float BindSoleLocalY()
    {
        var skin = _animator.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skin == null || skin.sharedMesh == null) return 0f;
        var model = _animator.transform;
        Bounds b = skin.sharedMesh.bounds;
        float lowest = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            lowest = Mathf.Min(lowest, model.InverseTransformPoint(skin.transform.TransformPoint(corner)).y);
        }
        return lowest;
    }

    // CharacterController 캡슐 바닥에서 바로 아래 지면까지의 거리. range 안에 지면이 없으면 무한대.
    float GroundDistance(float range)
    {
        Vector3 bottom = transform.TransformPoint(_body.center) - Vector3.up * (_body.height * 0.5f * transform.lossyScale.y);
        float distance = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(bottom + Vector3.up * 0.1f, Vector3.down, _groundHits, 0.1f + range, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!_groundHits[i].collider.transform.IsChildOf(transform))
                distance = Mathf.Min(distance, _groundHits[i].distance - 0.1f);
        return distance;
    }

    // P9 — 손 장착·도구 동작과 공중 팔 자세. Animator 자세 위에 오른팔 두 뼈를 목표 방향으로 돌리고 도구 방향을 직접 정한다.
    // 키 방향은 캐릭터 기준(x 오른쪽, y 위, z 앞). 접촉 순간 0.28초는 GatherFeedback.ContactDelay와 같다.
    struct ArmKey
    {
        public float t, chest; public Vector3 upper, fore, tool;
        public ArmKey(float t, Vector3 upper, Vector3 fore, Vector3 tool, float chest = 0f) { this.t = t; this.upper = upper; this.fore = fore; this.tool = tool; this.chest = chest; }
    }

    static readonly ArmKey CarryHeavy = new ArmKey(0f, new Vector3(.15f, -1f, .1f), new Vector3(.1f, -.45f, .9f), new Vector3(.3f, .65f, .7f));
    static readonly ArmKey[] AxeSwing =
    {
        CarryHeavy,
        new ArmKey(.16f, new Vector3(.25f, .85f, -.25f), new Vector3(.05f, .75f, -.65f), new Vector3(0f, .25f, -1f), -6f),
        new ArmKey(.28f, new Vector3(.1f, -.15f, 1f), new Vector3(0f, -.5f, 1f), new Vector3(0f, -.35f, 1f), 12f),
        new ArmKey(.40f, new Vector3(.1f, -.45f, .9f), new Vector3(0f, -.75f, .65f), new Vector3(0f, -.65f, .75f), 8f),
        new ArmKey(.56f, CarryHeavy.upper, CarryHeavy.fore, CarryHeavy.tool)
    };
    static readonly ArmKey[] PickaxeSwing =
    {
        CarryHeavy,
        new ArmKey(.16f, new Vector3(.25f, .9f, -.2f), new Vector3(.05f, .8f, -.6f), new Vector3(0f, .3f, -1f), -6f),
        new ArmKey(.28f, new Vector3(.1f, -.3f, 1f), new Vector3(0f, -.75f, .8f), new Vector3(0f, -.7f, .7f), 16f),
        new ArmKey(.40f, new Vector3(.1f, -.55f, .85f), new Vector3(0f, -.85f, .55f), new Vector3(0f, -.8f, .55f), 10f),
        new ArmKey(.56f, CarryHeavy.upper, CarryHeavy.fore, CarryHeavy.tool)
    };
    static readonly ArmKey CarryNet = new ArmKey(0f, new Vector3(.15f, -1f, .1f), new Vector3(.1f, -.1f, 1f), new Vector3(.05f, .9f, .45f));
    static readonly ArmKey[] NetSwing =
    {
        CarryNet,
        new ArmKey(.14f, new Vector3(.85f, .2f, -.35f), new Vector3(.75f, .35f, -.4f), new Vector3(.7f, .55f, -.45f)),
        new ArmKey(.28f, new Vector3(.05f, -.1f, 1f), new Vector3(-.35f, -.1f, 1f), new Vector3(-.25f, -.45f, 1f), 6f),
        new ArmKey(.42f, new Vector3(-.3f, -.3f, .9f), new Vector3(-.7f, -.2f, .7f), new Vector3(-.8f, -.2f, .6f), 4f),
        new ArmKey(.58f, CarryNet.upper, CarryNet.fore, CarryNet.tool)
    };
    static readonly ArmKey CarryRod = new ArmKey(0f, new Vector3(.12f, -1f, .15f), new Vector3(.05f, -.1f, 1f), new Vector3(0f, .75f, .65f));
    static readonly ArmKey RodWait = new ArmKey(0f, new Vector3(.1f, -.35f, .9f), new Vector3(0f, -.05f, 1f), new Vector3(0f, .35f, 1f));
    static readonly ArmKey[] RodCast =
    {
        CarryRod,
        new ArmKey(.16f, new Vector3(.2f, .7f, -.2f), new Vector3(.05f, .85f, -.4f), new Vector3(0f, .85f, -.5f), -5f),
        new ArmKey(.32f, new Vector3(.1f, -.05f, 1f), new Vector3(0f, .15f, 1f), new Vector3(0f, .4f, 1f), 8f),
        new ArmKey(.50f, RodWait.upper, RodWait.fore, RodWait.tool)
    };
    static readonly ArmKey[] RodPull =
    {
        RodWait,
        new ArmKey(.14f, new Vector3(.15f, -.1f, .95f), new Vector3(0f, .65f, .75f), new Vector3(0f, .98f, .2f), -8f),
        new ArmKey(.45f, CarryRod.upper, CarryRod.fore, CarryRod.tool)
    };

    void LateUpdate()
    {
        if (_animator == null) return;
        ApplyVisualGrounding();
        ApplyAirArms();
        ApplyToolPose();
    }

    Vector3 Local(Vector3 v) => (transform.right * v.x + transform.up * v.y + transform.forward * v.z).normalized;

    // 뼈가 자식 뼈를 가리키는 방향을 목표 방향으로 돌린다(기존 자세 위에 가중치로 덮는다).
    static void Aim(Transform bone, Transform child, Vector3 direction, float weight)
    {
        if (bone == null || child == null || weight <= 0f) return;
        Vector3 current = child.position - bone.position;
        if (current.sqrMagnitude < 1e-6f) return;
        Quaternion delta = Quaternion.FromToRotation(current.normalized, direction);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Clamp01(weight)) * bone.rotation;
    }

    // 점프 시작·공중·착지에서는 팔을 몸 옆으로 내린다(앞으로 곧게 뻗은 클립 팔을 덮는다).
    void ApplyAirArms()
    {
        var state = _animator.GetCurrentAnimatorStateInfo(0);
        bool airPose = !Grounded || state.IsName("JumpStart") || state.IsName("Land");
        _airArms = Mathf.MoveTowards(_airArms, airPose ? 1f : 0f, Time.deltaTime * (airPose ? 7f : 4f));
        if (_airArms <= .001f) return;
        float w = _airArms * .85f;
        Aim(_leftUpperArm, _leftLowerArm, Local(new Vector3(-.38f, -.85f, .18f)), w);
        Aim(_leftLowerArm, _leftHand, Local(new Vector3(-.15f, -.55f, .65f)), w);
        if (HeldTool() != ToolType.None) return;
        Aim(_rightUpperArm, _rightLowerArm, Local(new Vector3(.38f, -.85f, .18f)), w);
        Aim(_rightLowerArm, _rightHand, Local(new Vector3(.15f, -.55f, .65f)), w);
    }

    ToolType HeldTool()
    {
        var item = _equipment != null ? _equipment.HeldItem : null;
        return item != null && _equipment.HeldVisual != null ? item.toolType : ToolType.None;
    }

    void ApplyToolPose()
    {
        ToolType tool = HeldTool();
        bool handled = tool == ToolType.Axe || tool == ToolType.Pickaxe || tool == ToolType.Net || tool == ToolType.FishingRod;
        _toolWeight = Mathf.MoveTowards(_toolWeight, handled ? 1f : 0f, Time.deltaTime * 6f);
        SupportingTool = false;
        SupportGripError = float.PositiveInfinity;
        if (!handled || _rightUpperArm == null || _rightLowerArm == null || _rightHand == null)
        { _supportGripWeight = 0f; return; }
        float action = Time.time - _equipment.ActionStartedAt;
        ArmKey pose;
        switch (tool)
        {
            case ToolType.Axe: pose = Sample(AxeSwing, action); break;
            case ToolType.Pickaxe: pose = Sample(PickaxeSwing, action); break;
            case ToolType.Net: pose = Sample(NetSwing, action); break;
            default:
                bool casting = FishingSpot.ActiveDirectCaster == gameObject;
                float pulled = Time.time - FishingSpot.LastDirectPullAt;
                pose = pulled >= 0f && pulled < .45f ? Sample(RodPull, pulled)
                    : action >= 0f && action < .5f ? Sample(RodCast, action)
                    : casting ? RodWait : CarryRod;
                break;
        }
        float w = _toolWeight;
        // Failed contact rebounds briefly instead of following through as a successful chop.
        // Air swings retain the normal arc. The gameplay component owns the failure observation.
        float failed = Time.time - _equipment.FailedContactAt;
        if (tool != ToolType.FishingRod && failed >= 0 && failed < .24f)
        {
            float recoil = Mathf.Sin(failed / .24f * Mathf.PI);
            pose.upper = Vector3.Slerp(pose.upper, new Vector3(.2f, .35f, .75f), recoil * .35f);
            pose.fore = Vector3.Slerp(pose.fore, new Vector3(.05f, .5f, .7f), recoil * .55f);
            pose.tool = Vector3.Slerp(pose.tool, new Vector3(.1f, .85f, .45f), recoil * .45f);
            pose.chest -= 6f * recoil;
        }
        if (_chest != null && Mathf.Abs(pose.chest) > .01f)
            _chest.rotation = Quaternion.AngleAxis(pose.chest * w, transform.right) * _chest.rotation;
        Aim(_rightUpperArm, _rightLowerArm, Local(pose.upper), w);
        Aim(_rightLowerArm, _rightHand, Local(pose.fore), w);
        // 도구: 손잡이(모델 +Y)를 키 방향으로, 날/그물면(모델 +X)은 앞쪽으로 두고 손에 쥔 자리(모델 원점 근처)를 손에 맞춘다.
        var visual = _equipment.HeldVisual.transform;
        MeasureGrip(visual.gameObject, tool);
        Vector3 currentTip = _tipAtOrigin ? -visual.up : visual.up;
        Vector3 handle = Vector3.Slerp(currentTip, Local(pose.tool), w).normalized;
        Vector3 across = tool == ToolType.Net || tool == ToolType.FishingRod ? transform.right : transform.forward;
        Vector3 blade = Vector3.ProjectOnPlane(across, handle);
        if (blade.sqrMagnitude < 1e-4f) blade = Vector3.ProjectOnPlane(transform.up, handle);
        // 낚싯대는 모델 원점이 초리(끝) 쪽이고 +Y가 손잡이 쪽이라 뒤집어 쥔다.
        Vector3 modelUp = _tipAtOrigin ? -handle : handle;
        visual.rotation = Quaternion.LookRotation(Vector3.Cross(blade.normalized, modelUp), modelUp);
        visual.position = _rightHand.position - visual.up * (_gripLocalY * visual.lossyScale.y);
        ApplySupportGrip(tool, action, visual);
    }

    // Support the shaft during heavy-tool use and casting; carry/net/jump retain the authored free arm.
    // Both targets stay within the existing arm lengths. No root, rig scale or gameplay timing changes.
    void ApplySupportGrip(ToolType tool, float action, Transform visual)
    {
        if (_leftUpperArm == null || _leftLowerArm == null || _leftHand == null) return;
        bool heavy = tool == ToolType.Axe || tool == ToolType.Pickaxe;
        bool rod = tool == ToolType.FishingRod;
        float pull = Time.time - FishingSpot.LastDirectPullAt;
        bool wanted = Grounded && (heavy && action >= 0f && action < .58f || rod &&
            (FishingSpot.ActiveDirectCaster == gameObject || action >= 0f && action < .5f || pull >= 0f && pull < .45f));
        _supportGripWeight = Mathf.MoveTowards(_supportGripWeight, wanted ? 1f : 0f, Time.deltaTime * 12f);
        if (_supportGripWeight <= .001f || !heavy && !rod) return;

        Quaternion toolRotation = visual.rotation;
        Vector3 separation = visual.up * ((_supportLocalY - _gripLocalY) * visual.lossyScale.y);
        Vector3 target = _rightHand.position;
        // Keep the shared grip in front of the body, particularly during the rod backswing.
        target += transform.forward * Mathf.Max(0f, .28f - Vector3.Dot(target - transform.position, transform.forward));
        if (rod) target.y = Mathf.Min(target.y, _rightUpperArm.position.y + .35f);
        float rightReach = (Vector3.Distance(_rightUpperArm.position, _rightLowerArm.position) +
                            Vector3.Distance(_rightLowerArm.position, _rightHand.position)) * .97f;
        float leftReach = (Vector3.Distance(_leftUpperArm.position, _leftLowerArm.position) +
                           Vector3.Distance(_leftLowerArm.position, _leftHand.position)) * .97f;
        // Fold the primary arm toward the shared reachable region rather than stretching the support arm.
        for (int i = 0; i < 4; i++)
        {
            target = _leftUpperArm.position + Vector3.ClampMagnitude(target + separation - _leftUpperArm.position, leftReach) - separation;
            target = _rightUpperArm.position + Vector3.ClampMagnitude(target - _rightUpperArm.position, rightReach);
        }
        SolveArm(_rightUpperArm, _rightLowerArm, _rightHand, target, transform.right, _supportGripWeight);
        AlignGripHand(_rightHand, _rightFingerAxis, _rightPalmAxis, visual.up * (_tipAtOrigin ? -1f : 1f), -transform.right, _supportGripWeight);
        visual.rotation = toolRotation;
        visual.position = _rightHand.position - visual.up * (_gripLocalY * visual.lossyScale.y);
        Vector3 support = visual.TransformPoint(new Vector3(0f, _supportLocalY, 0f));
        SolveArm(_leftUpperArm, _leftLowerArm, _leftHand, support, -transform.right, _supportGripWeight);
        AlignGripHand(_leftHand, _leftFingerAxis, _leftPalmAxis, visual.up * (_tipAtOrigin ? -1f : 1f), transform.right, _supportGripWeight);
        SupportingTool = wanted && _supportGripWeight > .85f;
        SupportGripError = Vector3.Distance(_leftHand.position, support);
    }

    void BindHandAxes(Transform lower, Transform hand, out Vector3 finger, out Vector3 palm)
    {
        finger = Vector3.forward; palm = Vector3.up;
        if (lower == null || hand == null) return;
        Vector3 direction = (hand.position - lower.position).normalized;
        Vector3 normal = Vector3.ProjectOnPlane(transform.up, direction);
        if (normal.sqrMagnitude < .001f) normal = Vector3.ProjectOnPlane(transform.forward, direction);
        finger = hand.InverseTransformDirection(direction);
        palm = hand.InverseTransformDirection(normal.normalized);
    }

    static void AlignGripHand(Transform hand, Vector3 fingerAxis, Vector3 palmAxis, Vector3 shaft, Vector3 inward, float weight)
    {
        Vector3 normal = Vector3.ProjectOnPlane(inward, shaft).normalized;
        if (normal.sqrMagnitude < .001f) return;
        Quaternion localBasis = Quaternion.LookRotation(fingerAxis, palmAxis);
        Quaternion target = Quaternion.LookRotation(shaft, normal) * Quaternion.Inverse(localBasis);
        hand.rotation = Quaternion.Slerp(hand.rotation, target, weight);
    }

    static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 fallbackPole, float weight)
    {
        // Keep the authored local wrist pose so it follows the solved forearm instead of bending back to the old world angle.
        Vector3 a = upper.position, b = lower.position, c = hand.position;
        target = Vector3.Lerp(c, target, weight);
        float first = Vector3.Distance(a, b), second = Vector3.Distance(b, c);
        if (first < .001f || second < .001f) return;
        Vector3 toTarget = target - a;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(first - second) + .001f, first + second - .001f);
        Vector3 axis = toTarget.normalized;
        if (axis.sqrMagnitude < .5f) return;
        Vector3 pole = Vector3.ProjectOnPlane(b - a, axis);
        if (pole.sqrMagnitude < .00001f) pole = Vector3.ProjectOnPlane(fallbackPole, axis);
        pole.Normalize();
        float cos = Mathf.Clamp((first * first + distance * distance - second * second) / (2f * first * distance), -1f, 1f);
        Vector3 elbow = a + axis * (first * cos) + pole * (first * Mathf.Sqrt(1f - cos * cos));
        upper.rotation = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
    }

    // 손에 쥘 자리(모델 로컬 Y). 메시 bounds를 모델 공간으로 옮겨 손잡이 쪽 끝에서 12% 안쪽을 쥔다.
    void MeasureGrip(GameObject visual, ToolType tool)
    {
        if (_measuredVisual == visual) return;
        _measuredVisual = visual;
        float min = float.MaxValue, max = float.MinValue;
        foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float y = visual.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)).y;
                min = Mathf.Min(min, y); max = Mathf.Max(max, y);
            }
        }
        if (min > max) { min = 0f; max = 0f; }
        _tipAtOrigin = tool == ToolType.FishingRod;
        float length = max - min;
        _gripLocalY = _tipAtOrigin ? max - length * .12f : min + length * .12f;
        _supportLocalY = _tipAtOrigin ? max - length * .32f : min + length * .32f;
    }

    // 키 사이를 부드럽게(smoothstep) 잇는다. 범위를 벗어나면 첫/끝 자세.
    static ArmKey Sample(ArmKey[] keys, float t)
    {
        if (t <= keys[0].t || t >= keys[keys.Length - 1].t) return t >= keys[keys.Length - 1].t ? keys[keys.Length - 1] : keys[0];
        for (int i = 1; i < keys.Length; i++)
        {
            if (t > keys[i].t) continue;
            var a = keys[i - 1]; var b = keys[i];
            float k = Mathf.SmoothStep(0f, 1f, (t - a.t) / Mathf.Max(.0001f, b.t - a.t));
            return new ArmKey(t, Vector3.Slerp(a.upper.normalized, b.upper.normalized, k), Vector3.Slerp(a.fore.normalized, b.fore.normalized, k),
                Vector3.Slerp(a.tool.normalized, b.tool.normalized, k), Mathf.Lerp(a.chest, b.chest, k));
        }
        return keys[keys.Length - 1];
    }
}
