using System.Collections.Generic;
using UnityEngine;

// C-01 자동 리그는 휴머노이드 무릎(L/R_Calf)이 발목 높이에 있고 정강이 메시가 허벅지 트위스트 뼈를 따라가
// 어떤 클립이든 다리가 발목 위에서 접힌다. 편집 도구(PA_PlayerRigFixBuilder)가 만든 무릎 위치·다리 가중치·아바타를
// 플레이어 인스턴스에만 적용한다. 원본 FBX·씬·NPC는 바꾸지 않는다.
public sealed class PlayerRigFix : ScriptableObject
{
    public const string Resource = "PlayerLocomotion/C01_RigFix";

    public string sourceAvatar;
    public Avatar avatar;
    public Mesh mesh;
    public string[] bones;
    public Vector3[] localPositions;
    // 바인드 메시 가장 낮은 정점(신발 바닥)의 모델 로컬 높이(스케일 전). 런타임 정점 읽기 없이 표시 높이를 맞추는 데 쓴다.
    public float soleLocalY;
    // 신발 바닥 뒤꿈치·발끝 점(각 발 뼈 로컬). 지지발 고정이 실제 닿는 점을 붙잡는 데 쓴다.
    public Vector3 leftHeel, leftToe, rightHeel, rightToe;

    public static PlayerRigFix Load() => Resources.Load<PlayerRigFix>(Resource);

    public static bool TryApply(Animator animator) => TryApply(animator, out _);

    public static bool TryApply(Animator animator, out float soleLocalY)
    {
        soleLocalY = 0f;
        var fix = Resources.Load<PlayerRigFix>(Resource);
        if (fix == null || animator == null || animator.avatar == null || animator.avatar.name != fix.sourceAvatar) return false;
        soleLocalY = fix.soleLocalY;
        var skin = animator.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skin == null || skin.sharedMesh == null || skin.sharedMesh.vertexCount != fix.mesh.vertexCount) return false;
        var byName = new Dictionary<string, Transform>();
        foreach (var t in animator.GetComponentsInChildren<Transform>(true)) byName[t.name] = t;
        // 뼈 위치는 애니메이션에서 바뀌지 않으므로 현재 자세와 무관하게 로컬 위치만 교체한다.
        for (int i = 0; i < fix.bones.Length; i++)
            if (byName.TryGetValue(fix.bones[i], out var bone)) bone.localPosition = fix.localPositions[i];
        skin.sharedMesh = fix.mesh;
        animator.avatar = fix.avatar;
        return true;
    }
}
