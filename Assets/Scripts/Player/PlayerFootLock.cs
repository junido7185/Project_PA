using UnityEngine;

// 지지발 고정: 클립의 지지 구간 속도가 완전히 일정하지 않아 남는 발 미끄러짐을 없앤다.
// 애니메이션이 뼈에 적용된 뒤(LateUpdate) 신발 바닥의 뒤꿈치·발끝 중 지면에 닿은 점을 세계 위치에 고정하고,
// 허벅지·무릎 두 뼈 IK로 발목을 옮겨 그 점이 제자리에 있게 한다. 발이 굴러 닿는 점이 바뀌면 새 점으로 넘기고,
// 발이 들리거나 애니메이션 발과 너무 멀어지면 놓는다. 생성 클립에는 IK 목표 곡선이 없어 Animator IK 대신 뼈를 직접 다룬다.
public sealed class PlayerFootLock
{
    const float ContactHeight = 0.02f;    // 지면 위 이 높이 안의 바닥 점은 닿음
    const float ReleaseDistance = 0.14f;  // 고정 발목과 애니메이션 발목의 수평 거리가 이보다 크면 놓음
    const float BlendRate = 30f;

    sealed class Leg
    {
        public Transform Thigh, Knee, Foot;
        public Vector3[] Sole; // 발 뼈 로컬: 0 뒤꿈치, 1 발끝
        public int Anchor = -1;
        public Vector3 AnchorWorld;
        public float Weight;
    }

    readonly Leg[] _legs;

    public PlayerFootLock(Animator animator, PlayerRigFix fix)
    {
        _legs = new[]
        {
            new Leg { Thigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), Knee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), Foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot), Sole = new[] { fix.leftHeel, fix.leftToe } },
            new Leg { Thigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg), Knee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg), Foot = animator.GetBoneTransform(HumanBodyBones.RightFoot), Sole = new[] { fix.rightHeel, fix.rightToe } }
        };
    }

    public void Apply(bool active, float groundY, float dt)
    {
        dt = Mathf.Max(dt, 0.0001f);
        foreach (var leg in _legs)
        {
            if (leg.Thigh == null || leg.Knee == null || leg.Foot == null) continue;
            var toWorld = leg.Foot.localToWorldMatrix;
            Vector3 heel = toWorld.MultiplyPoint3x4(leg.Sole[0]), toe = toWorld.MultiplyPoint3x4(leg.Sole[1]);
            int lowest = heel.y <= toe.y ? 0 : 1;
            Vector3 contact = lowest == 0 ? heel : toe;
            bool touching = active && contact.y < groundY + ContactHeight;
            if (!touching) leg.Anchor = -1;
            else if (leg.Anchor < 0) { leg.Anchor = lowest; leg.AnchorWorld = contact; }
            else if (leg.Anchor != lowest)
            {
                // 뒤꿈치→발끝으로 굴러 닿는 점이 바뀌면 새 점의 현재(고정 보정 전) 위치가 아니라, 고정된 발에서의 위치로 넘긴다.
                Vector3 fromAnchor = (lowest == 0 ? heel : toe) - (leg.Anchor == 0 ? heel : toe);
                leg.AnchorWorld += new Vector3(fromAnchor.x, 0f, fromAnchor.z);
                leg.Anchor = lowest;
            }
            Vector3 target = leg.Foot.position;
            if (leg.Anchor >= 0)
            {
                Vector3 anchorNow = leg.Anchor == 0 ? heel : toe;
                Vector3 shift = leg.AnchorWorld - anchorNow; shift.y = 0f;
                if (shift.magnitude > ReleaseDistance) { leg.Anchor = -1; shift = Vector3.zero; }
                target += shift;
            }
            leg.Weight = Mathf.MoveTowards(leg.Weight, leg.Anchor >= 0 ? 1f : 0f, BlendRate * dt);
            if (leg.Weight <= 0f) continue;
            SolveTwoBone(leg, Vector3.Lerp(leg.Foot.position, target, leg.Weight));
        }
    }

    static void SolveTwoBone(Leg leg, Vector3 target)
    {
        Quaternion footRotation = leg.Foot.rotation;
        Vector3 a = leg.Thigh.position, b = leg.Knee.position, c = leg.Foot.position;
        float upper = (b - a).magnitude, lower = (c - b).magnitude;
        Vector3 toTarget = target - a;
        float distance = Mathf.Clamp(toTarget.magnitude, 0.01f, upper + lower - 0.001f);
        Vector3 axis = toTarget.normalized;
        // 무릎이 굽는 방향(현재 애니메이션의 무릎 위치)을 유지한다.
        Vector3 ac = c - a;
        Vector3 pole = (b - a) - Vector3.Project(b - a, ac.sqrMagnitude > 1e-6f ? ac : axis);
        pole = Vector3.ProjectOnPlane(pole, axis);
        if (pole.sqrMagnitude < 1e-8f) pole = Vector3.ProjectOnPlane(leg.Thigh.parent.forward, axis);
        pole.Normalize();
        float cosA = Mathf.Clamp((upper * upper + distance * distance - lower * lower) / (2f * upper * distance), -1f, 1f);
        float sinA = Mathf.Sqrt(1f - cosA * cosA);
        Vector3 knee = a + axis * (upper * cosA) + pole * (upper * sinA);
        leg.Thigh.rotation = Quaternion.FromToRotation(b - a, knee - a) * leg.Thigh.rotation;
        leg.Knee.rotation = Quaternion.FromToRotation(leg.Foot.position - leg.Knee.position, target - leg.Knee.position) * leg.Knee.rotation;
        leg.Foot.rotation = footRotation;
    }
}
