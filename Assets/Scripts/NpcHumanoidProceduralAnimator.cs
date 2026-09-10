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
        ApplyPose();
    }

    void ApplyPose()
    {
        EnsurePoseArrays();
        Array.Copy(_basePose.muscles, _pose.muscles, _basePose.muscles.Length);
        _pose.bodyPosition = _basePose.bodyPosition;
        _pose.bodyRotation = _basePose.bodyRotation;

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
        AddMuscle(_leftArmDownUp, armRestDrop * _leftArmDropSign);
        AddMuscle(_rightArmDownUp, armRestDrop * _rightArmDropSign);
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

        SetBasePose();
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
