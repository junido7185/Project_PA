using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-02 사용자 "NPC가 거꾸로 걸어 다닌다", "광부 모자를 쓴 NPC 매칭이 잘못됐다" 진단.
// 캐릭터 모델(C-01~C-09)마다 바인드 자세와 이동 클립(PA_MC_Walk) 자세의 몸 정면을 잰다.
// 표시 모델을 루트 아래 yaw 0(플레이어·관광객)과 yaw 180(동행 FirstDayWorldPresentation 배치)으로 두고,
// 루트 +Z(NavMeshAgent가 돌리는 진행 방향)와 몸 정면 사이 각도를 보고한다. 0°면 앞으로, 180°면 뒷걸음이다.
// 정면·측면·뒷면 초상을 같이 남겨 모자·복장 판독 근거로 쓴다.
// 배치: Unity -batchmode -executeMethod PA_CharacterFacingAudit.Batch (출력: PA_AUDIT_OUT 또는 아래 기본 폴더)
public static class PA_CharacterFacingAudit
{
    static string Output => Environment.GetEnvironmentVariable("PA_AUDIT_OUT") ?? "Logs/ParallelProduction/Feedback-20261102/character-audit";
    const string WalkClip = "Assets/Art/Animation/PlayerLocomotionMocap/PA_MC_Walk.anim";

    public static void Batch()
    {
        int code = 0;
        try { Run(); }
        catch (Exception e) { Debug.LogError("[FacingAudit] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    [MenuItem("Project PA/Diagnostics/Character Facing Audit")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClip);
        var lines = new List<string> { "각도 = 루트 +Z(진행 방향)에서 몸 정면까지(도). 0 = 앞으로 걷기, ±180 = 뒷걸음." };
        for (int i = 1; i <= 9; i++)
        {
            string path = $"Assets/Art/Character/C-0{i}.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { lines.Add(path + " 없음"); continue; }
            lines.Add(Measure(prefab, clip, 0f, $"C-0{i} 표시 yaw 0  "));
            lines.Add(Measure(prefab, clip, 180f, $"C-0{i} 표시 yaw 180"));
            Portrait(prefab, $"C-0{i}");
        }
        File.WriteAllLines(Path.Combine(Output, "facing.txt"), lines, new UTF8Encoding(false));
        foreach (var line in lines) Debug.Log("[FacingAudit] " + line);
    }

    static string Measure(GameObject prefab, AnimationClip clip, float yaw, string label)
    {
        var root = new GameObject("FacingAuditRoot");
        try
        {
            var visual = FirstDayStudioAssets.Place(prefab, root.transform, Vector3.zero, 1.75f, yaw);
            var animator = visual.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.avatar != null && a.avatar.isHuman);
            if (animator == null) return label + " 휴머노이드 Animator 없음";
            animator.Rebind();
            animator.Update(0f);
            float bind = BodyYaw(animator, root.transform);
            string sampled = "클립 없음";
            if (clip != null)
            {
                clip.SampleAnimation(animator.gameObject, clip.length * .25f);
                sampled = $"{BodyYaw(animator, root.transform):F0}";
            }
            return $"{label} 바인드 자세 {bind:F0} · 걷기 클립 {sampled}";
        }
        finally { Object.DestroyImmediate(root); }
    }

    // 몸 정면 = 좌→우 엉덩이 축과 위쪽의 외적(Unity 왼손 좌표: Cross(right, up) = forward).
    static Vector3 BodyForward(Animator animator)
    {
        var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var right = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (left == null || right == null) return Vector3.forward;
        Vector3 across = right.position - left.position;
        across.y = 0f;
        return Vector3.Cross(across.normalized, Vector3.up);
    }

    static float BodyYaw(Animator animator, Transform root) =>
        Vector3.SignedAngle(Vector3.ProjectOnPlane(root.forward, Vector3.up), BodyForward(animator), Vector3.up);

    const int ViewWidth = 384, ViewHeight = 512;

    // 몸 정면 기준 정면 · 3/4 · 측면 · 뒷면을 한 장에 렌더한다(동일 카메라·조명).
    static void Portrait(GameObject prefab, string name)
    {
        var preview = new PreviewRenderUtility();
        try
        {
            var clone = Object.Instantiate(prefab);
            foreach (var behaviour in clone.GetComponentsInChildren<Behaviour>(true))
                if (!(behaviour is Animator)) behaviour.enabled = false;
            var animator = clone.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.avatar != null && a.avatar.isHuman);
            if (animator != null) { animator.Rebind(); animator.Update(0f); }
            preview.AddSingleGO(clone);
            var renderers = clone.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            Vector3 front = animator != null ? BodyForward(animator) : Vector3.forward;

            var camera = preview.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.86f, .88f, .84f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographicSize = bounds.size.y * .56f;
            float distance = Mathf.Max(3f, bounds.size.magnitude * 2f);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = distance * 3f;
            preview.ambientColor = new Color(.42f, .44f, .46f);
            preview.lights[0].type = LightType.Directional; preview.lights[0].intensity = 1.4f; preview.lights[0].color = new Color(1f, .93f, .82f);
            preview.lights[1].type = LightType.Directional; preview.lights[1].intensity = .8f; preview.lights[1].color = new Color(.8f, .9f, 1f);

            float[] angles = { 0f, 40f, 90f, 180f };
            var sheet = new Texture2D(ViewWidth * angles.Length, ViewHeight, TextureFormat.RGB24, false, false);
            for (int v = 0; v < angles.Length; v++)
            {
                Vector3 toCamera = Quaternion.Euler(0f, angles[v], 0f) * front;
                camera.transform.position = bounds.center + toCamera * distance;
                camera.transform.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
                preview.lights[0].transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, 35f, 0f) * -toCamera + Vector3.down * .7f);
                preview.lights[1].transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, -120f, 0f) * -toCamera + Vector3.down * .3f);
                float pixelsPerPoint = Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint);
                preview.BeginPreview(new Rect(0, 0, ViewWidth / pixelsPerPoint, ViewHeight / pixelsPerPoint), GUIStyle.none);
                preview.Render(true, true);
                var rendered = (RenderTexture)preview.EndPreview();
                var previous = RenderTexture.active;
                RenderTexture.active = rendered;
                sheet.ReadPixels(new Rect(0, 0, ViewWidth, ViewHeight), ViewWidth * v, 0);
                RenderTexture.active = previous;
                if (v == 0 && QualitySettings.activeColorSpace == ColorSpace.Linear && !rendered.sRGB) sheet.name = "linear";
            }
            if (sheet.name == "linear")
            {
                var colors = sheet.GetPixels();
                for (int i = 0; i < colors.Length; i++) colors[i] = colors[i].gamma;
                sheet.SetPixels(colors);
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + "_views.png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }
        finally { preview.Cleanup(); }
    }
}
