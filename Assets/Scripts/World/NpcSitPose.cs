using UnityEngine;

// 손님이 상점 의자에 앉아 쉬는 자세(2026-11-03, 가게 꾸밈 의자의 실제 쓰임). 표현 전용: 이동·구매·저장 권위 없음.
// 절차 애니메이터가 정한 몸 위에 허벅지를 앞으로 들고 정강이를 내린 뒤, 골반이 좌석 위에 오도록 모델만 옮긴다(루트·NavMeshAgent는 의자 앞에 둔다).
[DefaultExecutionOrder(290)]
public sealed class NpcSitPose : MonoBehaviour
{
    const float ThighLift = 84f, KneeBend = 86f, Ease = 4f, HipAboveSeat = .07f;
    Animator _animator;
    Transform _hips, _lUpper, _lLower, _rUpper, _rLower, _model;
    Vector3 _modelBase, _seat;
    float _weight;
    bool _sitting, _applied;

    public bool Sitting => _sitting;
    public float Weight => _weight;

    // seatPoint: 좌석 윗면 가운데(월드). 루트는 이미 좌석 정면을 보게 돌려 둔다.
    public bool Sit(Vector3 seatPoint)
    {
        if (_hips == null && !Resolve()) return false;
        if (!_applied) _modelBase = _model.localPosition; // 앉기 직전 모델 자리(다른 표현 코드가 옮겨 둔 값 포함)
        _seat = seatPoint; _sitting = true;
        return true;
    }

    public void Stand() => _sitting = false;

    void LateUpdate()
    {
        if (_hips == null) return;
        _weight = Mathf.MoveTowards(_weight, _sitting ? 1f : 0f, Time.deltaTime * Ease);
        // 앉아 있는 동안에만 모델 자리를 직접 다룬다. 다 일어서면 원래 자리로 한 번 돌려 두고 손을 뗀다.
        if (_applied) { _model.localPosition = _modelBase; _applied = false; }
        if (_weight <= .001f) return;
        _applied = true;
        Vector3 right = transform.right;
        Bend(_lUpper, -ThighLift, right); Bend(_rUpper, -ThighLift, right);
        Bend(_lLower, KneeBend, right); Bend(_rLower, KneeBend, right);
        // 골반을 좌석 위(살짝 뒤)로: 다리를 접은 뒤의 골반 위치에서 목표까지 모델만 옮긴다.
        Vector3 target = _seat + Vector3.up * HipAboveSeat - transform.forward * .05f;
        _model.position += (target - _hips.position) * Smooth(_weight);
    }

    void Bend(Transform bone, float angle, Vector3 axis)
    {
        if (bone == null) return;
        bone.rotation = Quaternion.AngleAxis(angle * Smooth(_weight), axis) * bone.rotation;
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    bool Resolve()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null || !_animator.isHuman) return false;
        _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
        _lUpper = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        _lLower = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        _rUpper = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        _rLower = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        _model = _animator.transform;
        if (_model == transform) return false; // 루트에 Animator가 있으면 모델만 따로 옮길 수 없다.
        return _hips != null;
    }
}
