using System;
using System.Linq;
using UnityEngine;

// Runtime foot correction for the Tripo/Mixamo humanoid player.
// It keeps the walk clip looping normally while gently flattening feet against
// the ground so imported foot-bone offsets do not make the character walk on
// heels with toes pointing upward.
[DefaultExecutionOrder(40)]
public class PlayerFootIkStabilizer : MonoBehaviour
{
    [Header("Foot IK")]
    [Range(0f, 1f)] public float maxWeight = 0.85f;
    [Range(0f, 1f)] public float idleWeight = 0.25f;
    public float raycastHeight = 0.85f;
    public float raycastDistance = 1.5f;
    public float footHeightOffset = 0.035f;
    public float footPitchOffset = 0f;

    Animator _animator;
    Transform _ownerRoot;
    bool _hasSpeed;
    bool _hasIsSitting;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _ownerRoot = transform.root;

        if (_animator == null) return;

        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        _hasSpeed = _animator.parameters.Any(p => p.type == AnimatorControllerParameterType.Float && p.name == "Speed");
        _hasIsSitting = _animator.parameters.Any(p => p.type == AnimatorControllerParameterType.Bool && p.name == "IsSitting");
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null || !_animator.isHuman) return;
        if (_hasIsSitting && _animator.GetBool("IsSitting"))
        {
            ClearFoot(AvatarIKGoal.LeftFoot);
            ClearFoot(AvatarIKGoal.RightFoot);
            return;
        }

        float speed = _hasSpeed ? Mathf.Clamp01(_animator.GetFloat("Speed")) : 1f;
        float weight = Mathf.Lerp(idleWeight, maxWeight, Mathf.InverseLerp(0.08f, 0.45f, speed));

        ApplyFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, weight);
        ApplyFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, weight);
    }

    void ApplyFoot(AvatarIKGoal goal, HumanBodyBones footBone, float weight)
    {
        Transform foot = _animator.GetBoneTransform(footBone);
        if (foot == null)
        {
            ClearFoot(goal);
            return;
        }

        Vector3 origin = foot.position + Vector3.up * raycastHeight;
        if (!RaycastGround(origin, out RaycastHit hit))
        {
            ClearFoot(goal);
            return;
        }

        Vector3 targetPosition = hit.point + hit.normal * footHeightOffset;
        Vector3 forward = Vector3.ProjectOnPlane(_ownerRoot.forward, hit.normal);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, hit.normal)
                                    * Quaternion.Euler(footPitchOffset, 0f, 0f);

        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);
        _animator.SetIKPosition(goal, targetPosition);
        _animator.SetIKRotation(goal, targetRotation);
    }

    bool RaycastGround(Vector3 origin, out RaycastHit selected)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastDistance, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (_ownerRoot != null && hit.collider.transform.IsChildOf(_ownerRoot)) continue;
            selected = hit;
            return true;
        }

        selected = default;
        return false;
    }

    void ClearFoot(AvatarIKGoal goal)
    {
        _animator.SetIKPositionWeight(goal, 0f);
        _animator.SetIKRotationWeight(goal, 0f);
    }
}
