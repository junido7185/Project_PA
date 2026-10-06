using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class NpcHumanoidProceduralAnimator : MonoBehaviour
{
    [Header("Blend")]
    public bool autoApplyCharacterPreset = true;
    public bool autoCalibrateDirections = true;
    public string resolvedProfile = "Default";
    public float idleFrequency = 0.55f;
    public float walkFrequency = 1.55f;
    public float blendResponsiveness = 8f;
    [Tooltip("World-space distance covered by one complete procedural gait cycle.")]
    public float nominalStrideLength = 1.15f;
    public float CurrentPlanarSpeed { get; private set; }
    public float CurrentCadence { get; private set; }

    [Header("Idle")]
    [Range(0f, 0.15f)] public float idleBreath = 0.035f;
    [Range(0f, 0.08f)] public float idleHeadNod = 0.012f;

    [Header("Natural Arms")]
    [Range(0f, 1f)] public float armRestDrop = 0.78f;
    [Range(0f, 0.4f)] public float elbowRestBend = 0.18f;
    [Range(0f, 0.2f)] public float shoulderRelax = 0.06f;
    [Range(0f, 0.2f)] public float wristRelax = 0.035f;
    [Range(0f, 0.2f)] public float elbowWalkPump = 0.045f;

    [Header("Walk")]
    [Range(0f, 0.6f)] public float legSwing = 0.24f;
    [Range(0f, 0.7f)] public float kneeBend = 0.34f;
    [Range(0f, 0.3f)] public float footLift = 0.11f;
    [Range(0f, 0.45f)] public float armSwing = 0.18f;
    [Range(0f, 0.2f)] public float torsoSway = 0.045f;
    public float kneeBendSign = -1f;
    public float footLiftSign = -1f;

    Animator _animator;
    NavMeshAgent _agent;
    PlayerController _player;
    FirstDayStepTraversal _stepTraversal;
    HumanPoseHandler _handler;
    HumanPose _basePose;
    HumanPose _pose;
    bool _ready;
    float _cycle;
    float _walkBlend;

    int _spineFrontBack;
    int _spineLeftRight;
    int _chestFrontBack;
    int _chestLeftRight;
    int _neckNod;
    int _headNod;
    int _leftUpperLegFrontBack;
    int _rightUpperLegFrontBack;
    int _leftLowerLegStretch;
    int _rightLowerLegStretch;
    int _leftFootUpDown;
    int _rightFootUpDown;
    int _leftShoulderDownUp;
    int _rightShoulderDownUp;
    int _leftShoulderFrontBack;
    int _rightShoulderFrontBack;
    int _leftArmDownUp;
    int _rightArmDownUp;
    int _leftArmFrontBack;
    int _rightArmFrontBack;
    int _leftArmTwist;
    int _rightArmTwist;
    int _leftForearmStretch;
    int _rightForearmStretch;
    int _leftHandDownUp;
    int _rightHandDownUp;
    int _leftHandInOut;
    int _rightHandInOut;

    float _leftArmDropSign = 1f;
    float _rightArmDropSign = 1f;
    float _leftShoulderDropSign = 1f;
    float _rightShoulderDropSign = 1f;
    float _leftElbowBendSign = -1f;
    float _rightElbowBendSign = -1f;
    float _leftWristRelaxSign = -1f;
    float _rightWristRelaxSign = -1f;
    float _leftKneeBendSign = -1f;
    float _rightKneeBendSign = -1f;
    float _leftFootLiftSign = -1f;
    float _rightFootLiftSign = -1f;

    // FirstDay 플레이어 전용 보행 상태. NPC와 Golden 씬 경로는 사용하지 않는다.
    bool _playerMode;
    bool _playerCalibrated;
    CharacterController _body;
    Transform _hipsBone, _leftFootBone, _rightFootBone;
    float _baseFootHeight;
    float _legReach = 0.45f;
    float _legForwardSign = 1f;
    float _armForwardSign = 1f;
    float _spineForwardSign = 1f;
    float _spineRollSign = 1f;
    float _gaitPhase, _gaitWeight, _stanceFraction = 0.5f;
    float _airWeight, _airTime, _peakFall;
    float _landTimer = 1f, _landStrength;
    float _hipOffset, _lastYaw, _turnLean, _lastSpeed, _accelLean, _idleTime;
    bool _yawInitialized;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        RefreshAvatar();
    }

    void OnEnable()
    {
        _agent = GetComponent<NavMeshAgent>();
        RefreshAvatar();
    }

    void OnDisable()
    {
        ResetToBasePose();
        DisposeHandler();
    }

    void OnDestroy()
    {
        DisposeHandler();
    }

    public bool RefreshAvatar()
    {
        Animator nextAnimator = FindValidHumanoidAnimator();
        if (nextAnimator == null)
        {
            _ready = false;
            DisposeHandler();
            return false;
        }

        bool needsNewHandler = _handler == null || _animator != nextAnimator;
        _animator = nextAnimator;
        _animator.applyRootMotion = false;
        _animator.runtimeAnimatorController = null;
        if (!_animator.enabled)
            _animator.enabled = true;

        if (needsNewHandler)
        {
            DisposeHandler();
            _handler = new HumanPoseHandler(_animator.avatar, _animator.transform);
        }

        EnsurePoseArrays();
        _animator.Rebind();
        _animator.Update(0f);
        _handler.GetHumanPose(ref _basePose);
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;

        CacheMuscles();
        ApplyCharacterPreset();
        CalibrateNaturalPoseSigns();
        _ready = true;
        return true;
    }

    void LateUpdate()
    {
        if (!_ready && !RefreshAvatar())
            return;

        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        float speed = 0f;
        if (_agent != null)
        {
            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
        }
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_player != null) speed = _player.enabled ? _player.ActualPlanarSpeed : 0f;
        CurrentPlanarSpeed = speed;

        float targetBlend = Mathf.InverseLerp(0.05f, 1.25f, speed);
        _walkBlend = Mathf.MoveTowards(_walkBlend, targetBlend, Time.deltaTime * blendResponsiveness);

        float safeStride = Mathf.Max(0.45f, nominalStrideLength);
        float profileCadenceScale = Mathf.Clamp(walkFrequency / 1.55f, 0.8f, 1.2f);
        float distanceCadence = Mathf.Clamp(speed / safeStride * profileCadenceScale, 0.7f, 2.5f);
        CurrentCadence = Mathf.Lerp(idleFrequency, distanceCadence, _walkBlend);
        _cycle += Time.deltaTime * CurrentCadence;

        // 원본 rig/절차 pose를 유지하며 기본 idle을 작은 호흡으로 제한한다.
        if (gameObject.scene.name == DepartureTutorialController.SceneName)
        { idleBreath = .008f; idleHeadNod = .004f; elbowRestBend = .10f; if (_player == null) armRestDrop = 1f; }
        _playerMode = _player != null && _playerCalibrated && PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls;
        ApplyPose();
    }

    void ApplyPose()
    {
        EnsurePoseArrays();
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;

        if (_playerMode)
        {
            ApplyPlayerLocomotion(Time.deltaTime);
            ApplyPlayerHeldAndAction();
            _handler.SetHumanPose(ref _pose);
            ApplyPlayerGrounding(Time.deltaTime);
            return;
        }

        float idlePhase = _cycle * Mathf.PI * 2f;
        float walkPhase = _cycle * Mathf.PI * 2f;
        float idle = Mathf.Sin(idlePhase);
        float stride = Mathf.Sin(walkPhase);
        float leftKnee = Mathf.Max(0f, Mathf.Sin(walkPhase + Mathf.PI * 0.5f));
        float rightKnee = Mathf.Max(0f, Mathf.Sin(walkPhase - Mathf.PI * 0.5f));
        float leftArmPump = Mathf.Max(0f, -stride);
        float rightArmPump = Mathf.Max(0f, stride);

        AddMuscle(_spineFrontBack, idle * idleBreath * (1f - _walkBlend) - 0.02f * _walkBlend);
        AddMuscle(_chestFrontBack, idle * idleBreath * 0.5f * (1f - _walkBlend));
        AddMuscle(_neckNod, idle * idleHeadNod * (1f - _walkBlend));
        AddMuscle(_headNod, idle * idleHeadNod * 0.65f * (1f - _walkBlend));

        AddMuscle(_spineLeftRight, -stride * torsoSway * _walkBlend);
        AddMuscle(_chestLeftRight, stride * torsoSway * 0.45f * _walkBlend);

        AddMuscle(_leftShoulderDownUp, shoulderRelax * _leftShoulderDropSign);
        AddMuscle(_rightShoulderDownUp, shoulderRelax * _rightShoulderDropSign);
        AddMuscle(_leftShoulderFrontBack, -0.03f + -stride * 0.025f * _walkBlend);
        AddMuscle(_rightShoulderFrontBack, -0.03f + stride * 0.025f * _walkBlend);
        // P10: 바인드(T) 자세에 더하면 팔이 수평 근처에 머문다(데모 동행·손님의 '팔 벌림').
        // 데모 월드에서는 팔 내림 근육을 내림 방향 절대값으로 둔다. 다른 씬(골든 등)의 표현은 그대로다.
        if (FirstDayWorldPresentation.Instance != null)
        {
            SetMuscle(_leftArmDownUp, armRestDrop * _leftArmDropSign);
            SetMuscle(_rightArmDownUp, armRestDrop * _rightArmDropSign);
        }
        else
        {
            AddMuscle(_leftArmDownUp, armRestDrop * _leftArmDropSign);
            AddMuscle(_rightArmDownUp, armRestDrop * _rightArmDropSign);
        }
        AddMuscle(_leftArmTwist, -0.035f);
        AddMuscle(_rightArmTwist, 0.035f);
        AddMuscle(_leftForearmStretch, (elbowRestBend + leftArmPump * elbowWalkPump * _walkBlend) * _leftElbowBendSign);
        AddMuscle(_rightForearmStretch, (elbowRestBend + rightArmPump * elbowWalkPump * _walkBlend) * _rightElbowBendSign);
        AddMuscle(_leftHandDownUp, wristRelax * _leftWristRelaxSign + stride * 0.012f * _walkBlend);
        AddMuscle(_rightHandDownUp, wristRelax * _rightWristRelaxSign - stride * 0.012f * _walkBlend);
        AddMuscle(_leftHandInOut, 0.025f);
        AddMuscle(_rightHandInOut, -0.025f);

        AddMuscle(_leftUpperLegFrontBack, stride * legSwing * _walkBlend);
        AddMuscle(_rightUpperLegFrontBack, -stride * legSwing * _walkBlend);
        AddMuscle(_leftLowerLegStretch, leftKnee * kneeBend * _leftKneeBendSign * _walkBlend);
        AddMuscle(_rightLowerLegStretch, rightKnee * kneeBend * _rightKneeBendSign * _walkBlend);
        AddMuscle(_leftFootUpDown, leftKnee * footLift * _leftFootLiftSign * _walkBlend);
        AddMuscle(_rightFootUpDown, rightKnee * footLift * _rightFootLiftSign * _walkBlend);

        AddMuscle(_leftArmFrontBack, -stride * armSwing * _walkBlend);
        AddMuscle(_rightArmFrontBack, stride * armSwing * _walkBlend);

        if (_player != null && PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls)
        {
            if (_player.IsAirborne)
            {
                AddMuscle(_leftUpperLegFrontBack, .22f); AddMuscle(_rightUpperLegFrontBack, .12f);
                AddMuscle(_leftLowerLegStretch, .3f * _leftKneeBendSign);
                AddMuscle(_rightLowerLegStretch, .45f * _rightKneeBendSign);
                AddMuscle(_leftArmFrontBack, .14f); AddMuscle(_rightArmFrontBack, .14f);
            }
            var equipment = _player.GetComponent<EquipmentSystem>();
            if (equipment != null && equipment.HeldItem != null)
            { AddMuscle(_rightForearmStretch, .28f * _rightElbowBendSign); AddMuscle(_rightArmFrontBack, .14f); }
            float action = equipment != null ? Time.time - equipment.ActionStartedAt : 10;
            if (action < .5f)
            {
                float swing = Mathf.Sin(action / .5f * Mathf.PI);
                AddMuscle(_rightArmFrontBack, swing * .65f);
                AddMuscle(_chestFrontBack, swing * .12f);
            }
        }
        if (_player == null)
        {
            if (_stepTraversal == null) _stepTraversal=GetComponent<FirstDayStepTraversal>();
            if (_stepTraversal != null && _stepTraversal.IsJumping)
            {
                float tuck=Mathf.Sin(_stepTraversal.Progress*Mathf.PI);
                SetMuscle(_leftUpperLegFrontBack,.18f*tuck);
                SetMuscle(_rightUpperLegFrontBack,.24f*tuck);
                SetMuscle(_leftLowerLegStretch,.32f*tuck*_leftKneeBendSign);
                SetMuscle(_rightLowerLegStretch,.40f*tuck*_rightKneeBendSign);
                AddMuscle(_leftArmFrontBack,.10f*tuck);
                AddMuscle(_rightArmFrontBack,.10f*tuck);
            }
        }
        _handler.SetHumanPose(ref _pose);
    }

    void ResetToBasePose()
    {
        if (!_ready || _handler == null || _basePose.muscles == null)
            return;

        EnsurePoseArrays();
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;
        _handler.SetHumanPose(ref _pose);
    }

    Animator FindValidHumanoidAnimator()
    {
        foreach (var candidate in GetComponentsInChildren<Animator>(true))
        {
            if (candidate == null) continue;
            if (candidate.avatar == null) continue;
            if (!candidate.avatar.isHuman || !candidate.avatar.isValid) continue;
            return candidate;
        }

        return null;
    }

    void CacheMuscles()
    {
        _spineFrontBack = FindMuscle("spine", "front", "back");
        _spineLeftRight = FindMuscle("spine", "left", "right");
        _chestFrontBack = FindMuscle("chest", "front", "back");
        _chestLeftRight = FindMuscle("chest", "left", "right");
        _neckNod = FindMuscle("neck", "nod");
        _headNod = FindMuscle("head", "nod");
        _leftUpperLegFrontBack = FindMuscle("left", "upper", "leg", "front", "back");
        _rightUpperLegFrontBack = FindMuscle("right", "upper", "leg", "front", "back");
        _leftLowerLegStretch = FindMuscle("left", "lower", "leg", "stretch");
        _rightLowerLegStretch = FindMuscle("right", "lower", "leg", "stretch");
        _leftFootUpDown = FindMuscle("left", "foot", "up", "down");
        _rightFootUpDown = FindMuscle("right", "foot", "up", "down");
        _leftShoulderDownUp = FindMuscle("left", "shoulder", "down", "up");
        _rightShoulderDownUp = FindMuscle("right", "shoulder", "down", "up");
        _leftShoulderFrontBack = FindMuscle("left", "shoulder", "front", "back");
        _rightShoulderFrontBack = FindMuscle("right", "shoulder", "front", "back");
        _leftArmDownUp = FindMuscle("left", "arm", "down", "up");
        _rightArmDownUp = FindMuscle("right", "arm", "down", "up");
        _leftArmFrontBack = FindMuscle("left", "arm", "front", "back");
        _rightArmFrontBack = FindMuscle("right", "arm", "front", "back");
        _leftArmTwist = FindMuscle("left", "arm", "twist");
        _rightArmTwist = FindMuscle("right", "arm", "twist");
        _leftForearmStretch = FindMuscle("left", "forearm", "stretch");
        _rightForearmStretch = FindMuscle("right", "forearm", "stretch");
        _leftHandDownUp = FindMuscle("left", "hand", "down", "up");
        _rightHandDownUp = FindMuscle("right", "hand", "down", "up");
        _leftHandInOut = FindMuscle("left", "hand", "in", "out");
        _rightHandInOut = FindMuscle("right", "hand", "in", "out");
    }

    void ApplyCharacterPreset()
    {
        if (!autoApplyCharacterPreset)
            return;

        string key = ResolveCharacterKey();
        resolvedProfile = key;

        idleFrequency = 0.55f;
        walkFrequency = 1.55f;
        blendResponsiveness = 8f;
        nominalStrideLength = 1.15f;
        idleBreath = 0.035f;
        idleHeadNod = 0.012f;
        armRestDrop = 0.78f;
        elbowRestBend = 0.18f;
        shoulderRelax = 0.06f;
        wristRelax = 0.035f;
        elbowWalkPump = 0.045f;
        legSwing = 0.24f;
        kneeBend = 0.34f;
        footLift = 0.11f;
        armSwing = 0.18f;
        torsoSway = 0.045f;

        switch (key)
        {
            case "Farmer":
                walkFrequency = 1.48f;
                legSwing = 0.21f;
                kneeBend = 0.28f;
                footLift = 0.08f;
                armSwing = 0.15f;
                armRestDrop = 0.82f;
                elbowRestBend = 0.2f;
                torsoSway = 0.035f;
                break;
            case "Lumberjack":
                walkFrequency = 1.35f;
                legSwing = 0.27f;
                kneeBend = 0.38f;
                footLift = 0.1f;
                armSwing = 0.24f;
                armRestDrop = 0.8f;
                elbowRestBend = 0.23f;
                torsoSway = 0.06f;
                break;
            case "Miner":
                walkFrequency = 1.38f;
                legSwing = 0.23f;
                kneeBend = 0.36f;
                footLift = 0.09f;
                armSwing = 0.18f;
                armRestDrop = 0.84f;
                elbowRestBend = 0.25f;
                torsoSway = 0.052f;
                break;
            case "Fisher":
                walkFrequency = 1.5f;
                legSwing = 0.22f;
                kneeBend = 0.3f;
                footLift = 0.1f;
                armSwing = 0.17f;
                armRestDrop = 0.79f;
                elbowRestBend = 0.17f;
                torsoSway = 0.04f;
                break;
            case "Chef":
                walkFrequency = 1.6f;
                legSwing = 0.2f;
                kneeBend = 0.27f;
                footLift = 0.09f;
                armSwing = 0.14f;
                armRestDrop = 0.76f;
                elbowRestBend = 0.2f;
                torsoSway = 0.032f;
                break;
            case "Blacksmith":
                walkFrequency = 1.32f;
                legSwing = 0.25f;
                kneeBend = 0.4f;
                footLift = 0.09f;
                armSwing = 0.2f;
                armRestDrop = 0.86f;
                elbowRestBend = 0.27f;
                torsoSway = 0.06f;
                break;
            case "Tailor":
                walkFrequency = 1.66f;
                legSwing = 0.19f;
                kneeBend = 0.26f;
                footLift = 0.08f;
                armSwing = 0.13f;
                armRestDrop = 0.74f;
                elbowRestBend = 0.16f;
                torsoSway = 0.03f;
                break;
            case "Carpenter":
                walkFrequency = 1.45f;
                legSwing = 0.24f;
                kneeBend = 0.34f;
                footLift = 0.1f;
                armSwing = 0.19f;
                armRestDrop = 0.82f;
                elbowRestBend = 0.22f;
                torsoSway = 0.05f;
                break;
        }
    }

    void CalibrateNaturalPoseSigns()
    {
        _leftArmDropSign = PickHandLoweringSign(_leftArmDownUp, HumanBodyBones.LeftHand);
        _rightArmDropSign = PickHandLoweringSign(_rightArmDownUp, HumanBodyBones.RightHand);
        _leftShoulderDropSign = PickHandLoweringSign(_leftShoulderDownUp, HumanBodyBones.LeftHand);
        _rightShoulderDropSign = PickHandLoweringSign(_rightShoulderDownUp, HumanBodyBones.RightHand);
        _leftElbowBendSign = PickElbowBendSign(_leftForearmStretch, HumanBodyBones.LeftHand, HumanBodyBones.LeftUpperArm);
        _rightElbowBendSign = PickElbowBendSign(_rightForearmStretch, HumanBodyBones.RightHand, HumanBodyBones.RightUpperArm);
        _leftWristRelaxSign = PickHandLoweringSign(_leftHandDownUp, HumanBodyBones.LeftHand);
        _rightWristRelaxSign = PickHandLoweringSign(_rightHandDownUp, HumanBodyBones.RightHand);

        if (autoCalibrateDirections)
        {
            _leftKneeBendSign = PickElbowBendSign(_leftLowerLegStretch, HumanBodyBones.LeftFoot, HumanBodyBones.LeftUpperLeg);
            _rightKneeBendSign = PickElbowBendSign(_rightLowerLegStretch, HumanBodyBones.RightFoot, HumanBodyBones.RightUpperLeg);
            _leftFootLiftSign = PickToeLiftSign(_leftFootUpDown, HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot);
            _rightFootLiftSign = PickToeLiftSign(_rightFootUpDown, HumanBodyBones.RightToes, HumanBodyBones.RightFoot);
        }
        else
        {
            _leftKneeBendSign = Mathf.Sign(kneeBendSign);
            _rightKneeBendSign = Mathf.Sign(kneeBendSign);
            _leftFootLiftSign = Mathf.Sign(footLiftSign);
            _rightFootLiftSign = Mathf.Sign(footLiftSign);
        }

        if (GetComponent<PlayerController>() != null)
            CalibratePlayerGait();

        SetBasePose();
    }

    // 리그마다 근육 부호와 다리 길이가 다르므로 실제 뼈 이동을 측정해 보폭을 맞춘다.
    void CalibratePlayerGait()
    {
        _playerCalibrated = false;
        _hipsBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
        _leftFootBone = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _rightFootBone = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
        if (_hipsBone == null || _leftFootBone == null || _rightFootBone == null || leftHand == null || head == null
            || _leftUpperLegFrontBack < 0 || _rightUpperLegFrontBack < 0 || _leftArmFrontBack < 0 || _spineFrontBack < 0)
            return;

        SetBasePose();
        Transform leftHip = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightHip = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftHip == null || rightHip == null)
            return;
        Vector3 right = Vector3.ProjectOnPlane(rightHip.position - leftHip.position, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.5f)
            return;
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        _baseFootHeight = Mathf.Min(_leftFootBone.position.y, _rightFootBone.position.y) - transform.position.y;
        float footBase = Vector3.Dot(_leftFootBone.position - transform.position, forward);
        float handBase = Vector3.Dot(leftHand.position - transform.position, forward);
        float headForwardBase = Vector3.Dot(head.position - transform.position, forward);
        float headRightBase = Vector3.Dot(head.position - transform.position, right);

        SetProbePose(_leftUpperLegFrontBack, 0.3f);
        float footFront = Vector3.Dot(_leftFootBone.position - transform.position, forward) - footBase;
        SetProbePose(_leftUpperLegFrontBack, -0.3f);
        float footBack = Vector3.Dot(_leftFootBone.position - transform.position, forward) - footBase;
        _legForwardSign = footFront >= footBack ? 1f : -1f;
        _legReach = Mathf.Clamp((Mathf.Abs(footFront) + Mathf.Abs(footBack)) / 0.6f, 0.15f, 1.5f);

        SetProbePose(_leftArmFrontBack, 0.3f);
        _armForwardSign = Vector3.Dot(leftHand.position - transform.position, forward) - handBase >= 0f ? 1f : -1f;
        SetProbePose(_spineFrontBack, 0.3f);
        _spineForwardSign = Vector3.Dot(head.position - transform.position, forward) - headForwardBase >= 0f ? 1f : -1f;
        if (_spineLeftRight >= 0)
        {
            SetProbePose(_spineLeftRight, 0.3f);
            _spineRollSign = Vector3.Dot(head.position - transform.position, right) - headRightBase >= 0f ? 1f : -1f;
        }
        _playerCalibrated = true;
    }

    void AddLayer(int index, float value)
    {
        if (index < 0 || _pose.muscles == null || index >= _pose.muscles.Length)
            return;
        _pose.muscles[index] = Mathf.Clamp(_pose.muscles[index] + value, -1f, 1f);
    }

    static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // phase 0..stance: 발이 앞→뒤로 몸 속도에 맞춰 이동(지지), 이후: 무릎을 들며 앞으로 복귀(공중 스윙).
    void LegCycle(float phase, float stance, out float forward, out float lift, out float load)
    {
        phase = Mathf.Repeat(phase, 1f);
        if (phase < stance)
        {
            float s = phase / stance;
            forward = 1f - 2f * s;
            lift = 0f;
            load = Mathf.Sin(s * Mathf.PI);
        }
        else
        {
            float s = (phase - stance) / (1f - stance);
            forward = -1f + 2f * Smooth01(s);
            lift = Mathf.Sin(s * Mathf.PI);
            load = 0f;
        }
    }

    void ApplyPlayerLocomotion(float dt)
    {
        dt = Mathf.Max(dt, 0.0001f);
        float speed = CurrentPlanarSpeed;
        float walkSpeed = Mathf.Max(0.5f, _player.moveSpeed);
        float runT = Mathf.InverseLerp(walkSpeed, walkSpeed * 1.6f, speed);
        float moveT = Mathf.Clamp01(speed / walkSpeed);

        // 경사/단차의 한두 프레임 isGrounded 끊김은 점프 자세로 보지 않는다.
        if (_body == null) _body = GetComponent<CharacterController>();
        bool airborne = _player.enabled && _body != null && _body.enabled && _player.IsAirborne;
        float verticalSpeed = _player.VerticalSpeed;
        if (airborne)
        {
            _airTime += dt;
            _peakFall = Mathf.Min(_peakFall, verticalSpeed);
        }
        else
        {
            if (_airTime > 0.2f)
            {
                _landTimer = 0f;
                _landStrength = Mathf.Clamp(-_peakFall / 6.5f, 0.35f, 1f);
            }
            _airTime = 0f;
            _peakFall = 0f;
        }
        bool jumping = airborne && (_airTime > 0.08f || verticalSpeed > 1f);
        _airWeight = Mathf.MoveTowards(_airWeight, jumping ? 1f : 0f, dt / (jumping ? 0.09f : 0.07f));
        _landTimer += dt;
        float landT = _landTimer / 0.26f;
        float landWeight = landT >= 1f ? 0f : _landStrength * (landT < 0.3f ? Smooth01(landT / 0.3f) : 1f - Smooth01((landT - 0.3f) / 0.7f));

        float yaw = transform.eulerAngles.y;
        if (!_yawInitialized) { _lastYaw = yaw; _yawInitialized = true; }
        float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
        _lastYaw = yaw;
        float targetGait = Smooth01(Mathf.InverseLerp(0.05f, 0.7f, speed)) * (1f - _airWeight);
        _gaitWeight = Mathf.MoveTowards(_gaitWeight, targetGait, dt * 7f);
        _turnLean = Mathf.Lerp(_turnLean, Mathf.Clamp(yawRate / 540f, -1f, 1f) * _gaitWeight, 1f - Mathf.Exp(-10f * dt));
        _accelLean = Mathf.Lerp(_accelLean, Mathf.Clamp((speed - _lastSpeed) / dt / 30f, -1f, 1f), 1f - Mathf.Exp(-8f * dt));
        _lastSpeed = speed;
        _idleTime += dt;

        // 지지 구간에서 발이 이동한 거리 = 몸이 이동한 거리가 되도록 보폭·주기를 정한다.
        float cadence = Mathf.Lerp(Mathf.Lerp(1.9f, 2.8f, moveT), 3.3f, runT);
        float amplitude = Mathf.Lerp(0.46f, 0.6f, runT);
        float forwardLimit = Mathf.Min(amplitude, 0.4f);
        float travel = _legReach * 0.5f * (amplitude + forwardLimit) * 2f;
        float stance = speed > 0.05f ? travel * cadence / speed : 0.62f;
        if (stance > 0.62f)
        {
            float scale = 0.62f / stance;
            amplitude *= scale; forwardLimit *= scale; stance = 0.62f;
        }
        _stanceFraction = Mathf.Clamp(stance, 0.22f, 0.62f);
        _gaitPhase += cadence * dt * Mathf.Max(_gaitWeight, speed > 0.05f ? 1f : 0f);
        CurrentCadence = cadence * _gaitWeight;

        LegCycle(_gaitPhase, _stanceFraction, out float lf, out float lLift, out float lLoad);
        LegCycle(_gaitPhase + 0.5f, _stanceFraction, out float rf, out float rLift, out float rLoad);
        float g = _gaitWeight;
        float kneeSwing = Mathf.Lerp(0.5f, 0.85f, runT);
        float kneeLoad = Mathf.Lerp(0.12f, 0.2f, runT);
        float leftLeg = (lf >= 0f ? lf * forwardLimit : lf * amplitude) * g;
        float rightLeg = (rf >= 0f ? rf * forwardLimit : rf * amplitude) * g;
        float leftKnee = (lLift * kneeSwing + lLoad * kneeLoad) * g;
        float rightKnee = (rLift * kneeSwing + rLoad * kneeLoad) * g;

        float idle = (1f - g) * (1f - _airWeight);
        float breath = Mathf.Sin(_idleTime * Mathf.PI * 2f * 0.28f);
        float sway = Mathf.Sin(_idleTime * Mathf.PI * 2f * 0.11f);
        leftKnee += 0.07f * idle;
        rightKnee += 0.07f * idle;

        // 점프: 상승 중 다리를 모으고, 하강 중 착지를 준비한다.
        float rise = Mathf.InverseLerp(-3f, 3f, verticalSpeed);
        float air = _airWeight;
        leftLeg = Mathf.Lerp(leftLeg, Mathf.Lerp(0.12f, 0.34f, rise), air);
        rightLeg = Mathf.Lerp(rightLeg, Mathf.Lerp(-0.04f, 0.14f, rise), air);
        leftKnee = Mathf.Lerp(leftKnee, Mathf.Lerp(0.22f, 0.62f, rise), air);
        rightKnee = Mathf.Lerp(rightKnee, Mathf.Lerp(0.16f, 0.4f, rise), air);

        // 착지: 무릎을 굽혀 충격을 받고 골반 보정이 몸을 낮춘다.
        leftKnee += 0.55f * landWeight;
        rightKnee += 0.55f * landWeight;
        leftLeg += 0.18f * landWeight;
        rightLeg += 0.18f * landWeight;

        AddLayer(_leftUpperLegFrontBack, leftLeg * _legForwardSign);
        AddLayer(_rightUpperLegFrontBack, rightLeg * _legForwardSign);
        AddLayer(_leftLowerLegStretch, leftKnee * _leftKneeBendSign);
        AddLayer(_rightLowerLegStretch, rightKnee * _rightKneeBendSign);
        AddLayer(_leftFootUpDown, (lLift * 0.12f * g + 0.1f * air) * _leftFootLiftSign);
        AddLayer(_rightFootUpDown, (rLift * 0.12f * g + 0.1f * air) * _rightFootLiftSign);

        // 팔: 반대쪽 다리와 함께 흔들고, 빠를수록 팔꿈치를 더 접는다. 정지 시 몸통 옆으로 내린다.
        // 달리기 팔은 앞으로 크게, 뒤로는 작게 흔들어야 어깨 높이로 뒤로 뻗지 않는다.
        float armSwingAmp = Mathf.Lerp(0.26f, 0.4f, runT) * g;
        float leftArmSwing = rf * armSwingAmp * (rf < 0f ? 0.55f : 1f);
        float rightArmSwing = lf * armSwingAmp * (lf < 0f ? 0.55f : 1f);
        float armDrop = 1.05f - 0.3f * air * rise - 0.15f * air * (1f - rise);
        float elbow = 0.2f + Mathf.Lerp(0.14f, 0.42f, runT) * g + 0.12f * air + 0.1f * landWeight;
        AddLayer(_leftArmDownUp, armDrop * _leftArmDropSign);
        AddLayer(_rightArmDownUp, armDrop * _rightArmDropSign);
        AddLayer(_leftShoulderDownUp, (0.07f - 0.04f * air) * _leftShoulderDropSign);
        AddLayer(_rightShoulderDownUp, (0.07f - 0.04f * air) * _rightShoulderDropSign);
        AddLayer(_leftArmFrontBack, (leftArmSwing + 0.05f + 0.12f * air + 0.08f * landWeight) * _armForwardSign);
        AddLayer(_rightArmFrontBack, (rightArmSwing + 0.05f + 0.12f * air + 0.08f * landWeight) * _armForwardSign);
        AddLayer(_leftForearmStretch, elbow * _leftElbowBendSign);
        AddLayer(_rightForearmStretch, elbow * _rightElbowBendSign);
        AddLayer(_leftArmTwist, -0.035f);
        AddLayer(_rightArmTwist, 0.035f);
        AddLayer(_leftHandDownUp, wristRelax * _leftWristRelaxSign);
        AddLayer(_rightHandDownUp, wristRelax * _rightWristRelaxSign);
        AddLayer(_leftHandInOut, 0.025f);
        AddLayer(_rightHandInOut, -0.025f);

        // 몸통: 속도·가속만큼 앞으로, 회전 방향으로 기울이고 대기 중에는 호흡과 체중 이동만 남긴다.
        float lean = (0.05f + 0.07f * runT) * g + 0.06f * _accelLean * g + 0.1f * landWeight + 0.03f * air;
        AddLayer(_spineFrontBack, (lean + breath * 0.012f * idle) * _spineForwardSign);
        AddLayer(_chestFrontBack, (breath * 0.03f * idle + 0.02f * g) * _spineForwardSign);
        AddLayer(_spineLeftRight, (_turnLean * 0.08f + sway * 0.018f * idle) * _spineRollSign);
        AddLayer(_neckNod, (-lean * 0.35f + breath * 0.008f * idle) * _spineForwardSign);
        AddLayer(_headNod, -lean * 0.25f * _spineForwardSign);
    }

    // 손 장착/도구 동작은 다음 작업 범위이므로 기존 덮어쓰기 규칙을 그대로 유지한다.
    void ApplyPlayerHeldAndAction()
    {
        var equipment = _player.GetComponent<EquipmentSystem>();
        if (equipment != null && equipment.HeldItem != null)
        { AddMuscle(_rightForearmStretch, .28f * _rightElbowBendSign); AddMuscle(_rightArmFrontBack, .14f); }
        float action = equipment != null ? Time.time - equipment.ActionStartedAt : 10;
        if (action < .5f)
        {
            float swing = Mathf.Sin(action / .5f * Mathf.PI);
            AddMuscle(_rightArmFrontBack, swing * .65f);
            AddMuscle(_chestFrontBack, swing * .12f);
        }
    }

    // 지지발이 기본 자세의 발 높이에 오도록 골반을 내린다. 공중에서는 보정하지 않는다.
    void ApplyPlayerGrounding(float dt)
    {
        float rootY = transform.position.y;
        float leftY = _leftFootBone.position.y - rootY;
        float rightY = _rightFootBone.position.y - rootY;
        LegCycle(_gaitPhase, _stanceFraction, out _, out float lLift, out _);
        LegCycle(_gaitPhase + 0.5f, _stanceFraction, out _, out float rLift, out _);
        float contactY = Mathf.Min(leftY, rightY);
        bool flight = lLift > 0f && rLift > 0f;
        float contactWeight = Mathf.Lerp(1f, flight ? 0.45f : 1f, _gaitWeight) * (1f - _airWeight);
        float target = Mathf.Clamp(-(contactY - _baseFootHeight) * contactWeight, -0.3f, 0.08f);
        _hipOffset = Mathf.Lerp(_hipOffset, target, 1f - Mathf.Exp(-35f * Mathf.Max(dt, 0.0001f)));
        _hipsBone.position += Vector3.up * _hipOffset;
    }

    float PickHandLoweringSign(int muscle, HumanBodyBones handBone)
    {
        if (muscle < 0 || _animator == null)
            return 1f;

        Transform hand = _animator.GetBoneTransform(handBone);
        if (hand == null)
            return 1f;

        float positive = EvaluateBoneY(muscle, 0.45f, hand);
        float negative = EvaluateBoneY(muscle, -0.45f, hand);
        return positive <= negative ? 1f : -1f;
    }

    float PickElbowBendSign(int muscle, HumanBodyBones handBone, HumanBodyBones upperArmBone)
    {
        if (muscle < 0 || _animator == null)
            return -1f;

        Transform hand = _animator.GetBoneTransform(handBone);
        Transform upperArm = _animator.GetBoneTransform(upperArmBone);
        if (hand == null || upperArm == null)
            return -1f;

        float positive = EvaluateBoneDistance(muscle, 0.35f, hand, upperArm);
        float negative = EvaluateBoneDistance(muscle, -0.35f, hand, upperArm);
        return positive <= negative ? 1f : -1f;
    }

    float PickToeLiftSign(int muscle, HumanBodyBones toeBone, HumanBodyBones fallbackBone)
    {
        if (muscle < 0 || _animator == null)
            return 1f;

        Transform bone = _animator.GetBoneTransform(toeBone);
        if (bone == null)
            bone = _animator.GetBoneTransform(fallbackBone);
        if (bone == null)
            return 1f;

        float positive = EvaluateBoneY(muscle, 0.25f, bone);
        float negative = EvaluateBoneY(muscle, -0.25f, bone);
        return positive >= negative ? 1f : -1f;
    }

    float EvaluateBoneY(int muscle, float value, Transform bone)
    {
        SetProbePose(muscle, value);
        float y = bone.position.y;
        SetBasePose();
        return y;
    }

    float EvaluateBoneDistance(int muscle, float value, Transform a, Transform b)
    {
        SetProbePose(muscle, value);
        float distance = Vector3.Distance(a.position, b.position);
        SetBasePose();
        return distance;
    }

    void SetProbePose(int muscle, float value)
    {
        EnsurePoseArrays();
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;
        if (muscle >= 0 && muscle < _pose.muscles.Length)
        {
            float min = HumanTrait.GetMuscleDefaultMin(muscle);
            float max = HumanTrait.GetMuscleDefaultMax(muscle);
            _pose.muscles[muscle] = Mathf.Clamp(_basePose.muscles[muscle] + value, min, max);
        }

        _handler.SetHumanPose(ref _pose);
    }

    void SetBasePose()
    {
        EnsurePoseArrays();
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;
        _handler.SetHumanPose(ref _pose);
    }

    string ResolveCharacterKey()
    {
        string text = CollectHierarchyNames(transform).ToLowerInvariant();

        if (text.Contains("lumber") || text.Contains("c-03")) return "Lumberjack";
        if (text.Contains("miner") || text.Contains("c-04")) return "Miner";
        if (text.Contains("fisher") || text.Contains("c-05")) return "Fisher";
        if (text.Contains("chef") || text.Contains("c-06")) return "Chef";
        if (text.Contains("blacksmith") || text.Contains("c-07")) return "Blacksmith";
        if (text.Contains("tailor") || text.Contains("c-08")) return "Tailor";
        if (text.Contains("carpenter") || text.Contains("c-09")) return "Carpenter";
        if (text.Contains("farmer") || text.Contains("c-02")) return "Farmer";
        return "Default";
    }

    string CollectHierarchyNames(Transform root)
    {
        if (root == null) return "";

        string names = root.name;
        for (int i = 0; i < root.childCount; i++)
            names += " " + CollectHierarchyNames(root.GetChild(i));
        return names;
    }

    int FindMuscle(params string[] tokens)
    {
        string[] names = HumanTrait.MuscleName;
        for (int i = 0; i < names.Length; i++)
        {
            string name = NormalizeName(names[i]);
            bool match = true;
            foreach (string token in tokens)
            {
                if (!name.Contains(NormalizeName(token)))
                {
                    match = false;
                    break;
                }
            }

            if (match) return i;
        }

        return -1;
    }

    void SetMuscle(int index, float value)
    {
        if (index < 0 || _pose.muscles == null || index >= _pose.muscles.Length) return;
        _pose.muscles[index] = Mathf.Clamp(value, -1f, 1f);
    }

    void AddMuscle(int index, float value)
    {
        if (index < 0 || _pose.muscles == null || index >= _pose.muscles.Length)
            return;

        float min = HumanTrait.GetMuscleDefaultMin(index);
        float max = HumanTrait.GetMuscleDefaultMax(index);
        _pose.muscles[index] = Mathf.Clamp(_basePose.muscles[index] + value, min, max);
    }

    void EnsurePoseArrays()
    {
        int count = HumanTrait.MuscleCount;
        if (_basePose.muscles == null || _basePose.muscles.Length != count)
            _basePose.muscles = new float[count];
        if (_pose.muscles == null || _pose.muscles.Length != count)
            _pose.muscles = new float[count];
    }

    void DisposeHandler()
    {
        if (_handler == null) return;
        _handler.Dispose();
        _handler = null;
    }

    static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? ""
            : value.ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ");
    }
}
