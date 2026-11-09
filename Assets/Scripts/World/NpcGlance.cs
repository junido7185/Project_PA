using UnityEngine;

// 레퍼런스 연구 K-02(ACNH 생활감, ReferenceResearch/pa-acnh-npc-life): 하던 일을 유지한 채 지나가는 플레이어를 '짧게 알아보는' 표현.
// 몸·이동·작업은 건드리지 않고 목·머리만 돌린다(표현 전용, 생산·대화 권위 없음). 대화 중에는 상대를 계속 본다.
// 몸 자세(절차 애니메이터·도구 자세)가 정해진 뒤에 덧붙도록 늦게 실행한다.
[DefaultExecutionOrder(300)]
public sealed class NpcGlance : MonoBehaviour
{
    const float MaxYaw = 65f, MaxPitch = 20f, Ease = 5f;
    Transform _neck, _head;
    Transform _follow;
    Vector3 _offset;
    float _weight, _until;
    bool _hold;

    public float Weight => _weight;

    // 지금부터 seconds 동안 follow(+offset)를 따라 본다(hold=true면 Release까지 계속).
    public void Look(Transform follow, Vector3 offset, float seconds, bool hold = false)
    {
        _follow = follow; _offset = offset; _hold = hold;
        _until = Mathf.Max(_until, Time.time + seconds);
    }

    public void Release() { _hold = false; _until = 0f; }

    void LateUpdate()
    {
        if (_head == null && !Resolve()) return;
        bool active = _hold || Time.time < _until;
        _weight = Mathf.MoveTowards(_weight, active ? 1f : 0f, Time.deltaTime * Ease * (active ? 1f : .7f));
        if (_weight <= .001f || _follow == null) return;
        // 몸 정면 기준으로 목표 방향을 좌우·위아래 한계 안에서 나눠 목(40%)과 머리(60%)에 준다.
        Vector3 to = _follow.position + _offset - _head.position;
        Vector3 local = Quaternion.Inverse(transform.rotation) * to;
        float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -MaxYaw, MaxYaw);
        float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -MaxPitch, MaxPitch);
        Apply(_neck, yaw * .4f, pitch * .4f);
        Apply(_head, yaw * .6f, pitch * .6f);
    }

    void Apply(Transform bone, float yaw, float pitch)
    {
        if (bone == null) return;
        Quaternion turn = Quaternion.AngleAxis(yaw * _weight, transform.up) * Quaternion.AngleAxis(pitch * _weight, transform.right);
        bone.rotation = turn * bone.rotation;
    }

    bool Resolve()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return false;
        _neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        _head = animator.GetBoneTransform(HumanBodyBones.Head);
        return _head != null;
    }
}
