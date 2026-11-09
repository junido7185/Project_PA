using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-31 사용자 피드백 "기본 idle·걷기 자세가 부자연스럽고 모동숲·딩컴 같은 조작감이 없다".
// 키 포즈로 만든 PA_Style 클립 대신 Quaternius UAL1(CC0, 모캡 기반) 클립의 타이밍·무게 이동을 그대로 쓰고,
// 저등신 C-01에 과한 부분(팔 펌핑·앞숙임·머리 끄덕임·상하 반동)만 머슬 진폭/평균을 줄인 PA_Loco_* 클립을 굽는다.
// 다리 머슬은 바꾸지 않아 발 접지 타이밍을 보존하고, 클립마다 지지발이 미끄러지지 않는 실제 이동 속도를 잰다.
// 배치: Unity -batchmode -executeMethod PA_PlayerLocomotionRebuild.BatchBuild (검토 시트·프레임·보고서 생성)
//       Unity -batchmode -executeMethod PA_PlayerLocomotionRebuild.BatchApply (제품 컨트롤러 연결)
public static class PA_PlayerLocomotionRebuild
{
    const string SourceFbx = "Assets/Art/External/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx";
    public const string Folder = "Assets/Art/Animation/PlayerLocomotionUAL";
    const string ProductControllerPath = "Assets/Resources/PlayerLocomotion/PlayerLocomotion.controller";
    const string ReportName = "locomotion-report.json";
    static string Output => Environment.GetEnvironmentVariable("PA_LOCO_OUT") ?? "Logs/ParallelProduction/Feedback-20261031/locomotion";

    // 곡선 속성명에 Part가 들어 있으면 적용(좌우 공통). 새 값 = 평균×Mean + Add + Amp×(값 − 평균).
    sealed class Rule { public string Part; public float Amp = 1f, Mean = 1f, Add; }

    sealed class Style
    {
        public string Source, Name;
        public bool Loop = true;
        public float RootBob = 1f, PitchAmp = 1f, PitchMean = 1f, RollAmp = 1f;
        public readonly List<Rule> Rules = new List<Rule>();
        public Style R(string part, float amp, float mean = 1f, float add = 0f)
        { Rules.Add(new Rule { Part = part, Amp = amp, Mean = mean, Add = add }); return this; }
    }

    // 저등신 보정: 큰 머리가 끄덕이며 흔들리지 않게(모동숲·딩컴처럼 머리는 수평 유지), 팔은 몸 옆에서 가볍게,
    // 상체는 거의 세운다. 다리(Upper Leg/Lower Leg/Foot)는 원본 그대로.
    static List<Style> Styles() => new List<Style>
    {
        new Style { Source = "UAL1_Idle_Loop", Name = "PA_Loco_Idle" }
            .R("Head Nod", .7f).R("Neck Nod", .7f),
        new Style { Source = "UAL1_Walk_Loop", Name = "PA_Loco_Walk", RootBob = .8f, PitchAmp = .7f, PitchMean = .6f }
            .R("Arm Front-Back", .9f).R("Head Nod", .5f, .6f).R("Neck Nod", .5f, .6f)
            .R("Spine Front-Back", .8f, .6f).R("Chest Front-Back", .8f, .6f),
        new Style { Source = "UAL1_Jog_Fwd_Loop", Name = "PA_Loco_Jog", RootBob = .6f, PitchAmp = .6f, PitchMean = .45f, RollAmp = .7f }
            .R("Arm Front-Back", .62f).R("Forearm Stretch", .75f).R("Arm Down-Up", .7f, .6f).R("Shoulder Down-Up", .6f, .6f)
            .R("Spine Front-Back", .65f, .4f).R("Chest Front-Back", .65f, .4f)
            .R("Twist Left-Right", .75f).R("Spine Left-Right", .7f).R("Chest Left-Right", .7f)
            .R("Head Nod", .4f, .35f).R("Neck Nod", .4f, .35f).R("Head Turn", .6f).R("Neck Turn", .6f),
        new Style { Source = "UAL1_Sprint_Loop", Name = "PA_Loco_Sprint", RootBob = .7f, PitchAmp = .7f, PitchMean = .6f, RollAmp = .75f }
            .R("Arm Front-Back", .72f).R("Forearm Stretch", .8f).R("Arm Down-Up", .75f, .7f).R("Shoulder Down-Up", .7f, .7f)
            .R("Spine Front-Back", .75f, .6f).R("Chest Front-Back", .75f, .6f).R("Twist Left-Right", .8f)
            .R("Head Nod", .45f, .45f).R("Neck Nod", .45f, .45f),
        new Style { Source = "UAL1_Jump_Start", Name = "PA_Loco_JumpStart", Loop = false }.R("Arm Front-Back", .85f).R("Head Nod", .6f),
        new Style { Source = "UAL1_Jump_Loop", Name = "PA_Loco_Airborne" }.R("Arm Front-Back", .85f).R("Head Nod", .6f),
        new Style { Source = "UAL1_Jump_Land", Name = "PA_Loco_Land", Loop = false }.R("Arm Front-Back", .85f).R("Head Nod", .6f),
    };

    [Serializable] internal sealed class ClipReport { public string name, source; public float length, naturalSpeed, hipsMin, hipsMax, footDrift; public int stanceSamples; }
    [Serializable] sealed class Report { public List<ClipReport> clips = new List<ClipReport>(); public string notes; }

    [MenuItem("Project PA/Art/Player Locomotion (UAL)/Build Clips + Review")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        var sources = AssetDatabase.LoadAllAssetsAtPath(SourceFbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        var report = new Report();
        var built = new List<AnimationClip>();
        foreach (var style in Styles())
        {
            if (!sources.TryGetValue(style.Source, out var source)) { Debug.LogError("[LocoRebuild] missing source clip " + style.Source); continue; }
            var clip = SaveClip(BuildStyled(style, source));
            built.Add(clip);
            var entry = Measure(clip, style.Loop);
            entry.source = style.Source;
            report.clips.Add(entry);
        }
        AssetDatabase.SaveAssets();
        foreach (var clip in built) RenderReview(clip);
        foreach (var clip in built.Where(c => c.isLooping)) RenderLoopFrames(clip, 2.4f, 25);
        report.notes = "naturalSpeed = 지지발이 미끄러지지 않는 이동 속도(m/s, 1.75m 표시 스케일). 재생 배율 = 게임 속도 / naturalSpeed.";
        File.WriteAllText(Path.Combine(Output, ReportName), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        Debug.Log("[LocoRebuild] built " + string.Join(", ", report.clips.Select(c => $"{c.name} v={c.naturalSpeed:F2} len={c.length:F2}")));
    }

    public static void BatchBuild() => RunBatch(Build);
    public static void BatchApply() => RunBatch(ApplyToProductController);
    public static void BatchBuildDinkum() => RunBatch(BuildDinkum);
    public static void BatchApplyDinkum() => RunBatch(ApplyDinkumToProductController);

    // ---------- 2026-10-31 2차: 딩컴 키 포즈 이동(PA_DK_*) ----------
    // 사용자 판정 "UAL 모캡 이동은 껄렁한 건달" → 딩컴 트레일러 관찰 기반 키 포즈 클립(PA_PlayerMotionStyleLab.GaitDef)으로 교체.
    const string DinkumReportName = "dinkum-report.json";
    public const float DkWalkThreshold = 1.5f;

    [MenuItem("Project PA/Art/Player Locomotion (UAL)/Build Dinkum Clips + Review")]
    public static void BuildDinkum()
    {
        Directory.CreateDirectory(Output);
        var report = new Report();
        var clips = PA_PlayerMotionStyleLab.BuildDinkumClips();
        foreach (var clip in clips)
        {
            var entry = Measure(clip, clip.isLooping && clip.name != "PA_DK_Idle");
            entry.source = "PA_PlayerMotionStyleLab.GaitDef";
            report.clips.Add(entry);
        }
        foreach (var clip in clips) RenderReview(clip);
        foreach (var clip in clips) RenderLoopFrames(clip, Mathf.Max(1.2f, clip.length * 3f), 30);
        report.notes = "PA_DK_*: 딩컴 키 포즈. naturalSpeed는 설계 속도(Walk 1.5, Run 4.2, Sprint 6.72)와 비교한다.";
        File.WriteAllText(Path.Combine(Output, DinkumReportName), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        Debug.Log("[LocoRebuild] dinkum " + string.Join(", ", report.clips.Select(c => $"{c.name} v={c.naturalSpeed:F2} len={c.length:F2} hips={c.hipsMin:F3}..{c.hipsMax:F3}")));
    }

    [MenuItem("Project PA/Art/Player Locomotion (UAL)/Apply Dinkum To Product Controller")]
    public static void ApplyDinkumToProductController()
    {
        AnimationClip Clip(string folder, string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/" + n + ".anim")
            ?? throw new InvalidOperationException("missing " + n);
        string lab = PA_PlayerMotionStyleLab.Folder;
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ProductControllerPath);
        var machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState State(string n) => machine.states.First(st => st.state.name == n).state;
        var tree = (UnityEditor.Animations.BlendTree)State("Locomotion").motion;
        var children = tree.children;
        if (children.Length != 4) throw new InvalidOperationException("Locomotion 블렌드 자식이 4개가 아닙니다: " + children.Length);
        tree.useAutomaticThresholds = false;
        children[0].motion = Clip(lab, "PA_DK_Idle"); children[0].threshold = 0f;
        children[1].motion = Clip(lab, "PA_DK_Walk"); children[1].threshold = DkWalkThreshold;
        children[2].motion = Clip(lab, "PA_DK_Run"); children[2].threshold = JogThreshold;
        children[3].motion = Clip(lab, "PA_DK_Sprint"); children[3].threshold = SprintThreshold;
        for (int i = 0; i < children.Length; i++) children[i].timeScale = 1f;
        tree.children = children;
        // 점프는 같은 굽힌 팔·곧은 상체로 만든 기존 PA_Style 점프 3종.
        State("JumpStart").motion = Clip(lab, "PA_Style_JumpStart");
        State("Airborne").motion = Clip(lab, "PA_Style_Airborne");
        State("Land").motion = Clip(lab, "PA_Style_Land");
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("[LocoRebuild] product controller (dinkum) " + string.Join(", ", tree.children.Select(c =>
            c.motion.name + "@" + c.threshold.ToString("F2", CultureInfo.InvariantCulture))));
    }

    static void RunBatch(Action action)
    {
        int code = 0;
        try { action(); }
        catch (Exception e) { Debug.LogError("[LocoRebuild] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // ---------- 스타일 굽기 ----------
    static AnimationClip BuildStyled(Style s, AnimationClip src)
    {
        var clip = new AnimationClip { name = s.Name, frameRate = 60 };
        float length = src.length;
        foreach (var binding in AnimationUtility.GetCurveBindings(src))
        {
            var curve = AnimationUtility.GetEditorCurve(src, binding);
            if (curve == null || binding.propertyName.StartsWith("RootQ.")) continue;
            string p = binding.propertyName;
            if (p == "RootT.y") curve = Scale(curve, length, s.RootBob, 1f, 0f);
            else
            {
                var rule = s.Rules.FirstOrDefault(r => p.Contains(r.Part));
                if (rule != null) curve = Scale(curve, length, rule.Amp, rule.Mean, rule.Add);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
        WriteRootRotation(clip, src, s);
        var settings = AnimationUtility.GetAnimationClipSettings(src);
        // 생성 .anim은 Feet 높이 기준이 런타임에서 FBX처럼 보정되지 않으므로 Original 기준 + 실측 접지(Ground)로 맞춘다(MotionStyleLab과 같은 이유).
        settings.loopTime = s.Loop;
        settings.heightFromFeet = false;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        Ground(clip);
        return clip;
    }

    static AnimationCurve Scale(AnimationCurve curve, float length, float amp, float meanScale, float add)
    {
        if (Mathf.Approximately(amp, 1f) && Mathf.Approximately(meanScale, 1f) && Mathf.Approximately(add, 0f)) return curve;
        const int N = 240;
        double sum = 0;
        for (int i = 0; i < N; i++) sum += curve.Evaluate(length * (i + .5f) / N);
        float mean = (float)(sum / N);
        float target = mean * meanScale + add;
        var keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].value = target + amp * (keys[i].value - mean);
            keys[i].inTangent *= amp;
            keys[i].outTangent *= amp;
        }
        return new AnimationCurve(keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode };
    }

    // 몸통(RootQ) 기울기: 평균 자세의 앞숙임을 줄이고, 주기 중 흔들림(피치·롤) 진폭을 줄인다. 요(방향)는 그대로.
    static void WriteRootRotation(AnimationClip clip, AnimationClip src, Style s)
    {
        AnimationCurve Get(string c) => AnimationUtility.GetEditorCurve(src, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ." + c));
        AnimationCurve qx = Get("x"), qy = Get("y"), qz = Get("z"), qw = Get("w");
        if (qx == null || qy == null || qz == null || qw == null) return;
        float length = src.length;
        int count = Mathf.Max(2, Mathf.RoundToInt(length * 60f));
        var samples = new Quaternion[count + 1];
        Vector4 acc = Vector4.zero;
        for (int k = 0; k <= count; k++)
        {
            float t = length * k / count;
            var q = new Quaternion(qx.Evaluate(t), qy.Evaluate(t), qz.Evaluate(t), qw.Evaluate(t));
            if (k > 0 && Quaternion.Dot(q, samples[k - 1]) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            samples[k] = Normalize(q);
            acc += new Vector4(samples[k].x, samples[k].y, samples[k].z, samples[k].w);
        }
        Quaternion mean = Normalize(new Quaternion(acc.x, acc.y, acc.z, acc.w));
        // UAL1 클립의 평균 몸 방향은 C-01 바인드 정면의 반대(요 약 180°)다. 기존 PA_Style 클립(요 0, 게임에서 정면 확인)과 같게
        // 평균 요를 0으로 맞추고, 요를 뺀 기울기(피치·롤)만 줄인다. 주기 중 요 흔들림(골반 회전)은 그대로 둔다.
        Quaternion yaw = Quaternion.Euler(0f, mean.eulerAngles.y, 0f);
        Vector3 tilt = Signed((Quaternion.Inverse(yaw) * mean).eulerAngles);
        Quaternion newMean = Quaternion.Euler(tilt.x * s.PitchMean, 0f, tilt.z * s.PitchMean);
        var keys = new[] { new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>(), new List<Keyframe>() };
        Quaternion previous = Quaternion.identity;
        for (int k = 0; k <= count; k++)
        {
            Vector3 d = Signed((Quaternion.Inverse(mean) * samples[k]).eulerAngles);
            var q = newMean * Quaternion.Euler(d.x * s.PitchAmp, d.y, d.z * s.RollAmp);
            if (k > 0 && Quaternion.Dot(q, previous) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            previous = q;
            float t = length * k / count;
            keys[0].Add(new Keyframe(t, q.x)); keys[1].Add(new Keyframe(t, q.y)); keys[2].Add(new Keyframe(t, q.z)); keys[3].Add(new Keyframe(t, q.w));
        }
        string[] names = { "x", "y", "z", "w" };
        for (int c = 0; c < 4; c++)
        {
            var curve = new AnimationCurve(keys[c].ToArray());
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ." + names[c]), curve);
        }
    }

    static Quaternion Normalize(Quaternion q)
    {
        float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return m < 1e-6f ? Quaternion.identity : new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
    }

    static Vector3 Signed(Vector3 e) => new Vector3(Mathf.DeltaAngle(0f, e.x), Mathf.DeltaAngle(0f, e.y), Mathf.DeltaAngle(0f, e.z));

    // 주기 중 스킨 메시 최저 정점 = 바인드 신발 바닥이 되도록 RootT.y를 일정량 옮긴다(상대 반동은 유지, 2회 반복).
    internal static void Ground(AnimationClip clip)
    {
        using var stage = new PA_PlayerMotionStyleLab.Stage(16, 16, 1.75f, null, false);
        float perUnit = stage.Animator.humanScale * stage.Animator.transform.lossyScale.y;
        var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
        for (int pass = 0; pass < 2; pass++)
        {
            float lowest = float.MaxValue;
            for (int i = 0; i < 48; i++) { stage.Sample(clip, clip.length * i / 48f); lowest = Mathf.Min(lowest, stage.LowestMesh()); }
            float shift = (stage.BindMeshBottomY - lowest) / perUnit;
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) return;
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].value += shift;
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }

    static AnimationClip SaveClip(AnimationClip clip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Animation")) AssetDatabase.CreateFolder("Assets/Art", "Animation");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art/Animation", "PlayerLocomotionUAL");
        string path = Folder + "/" + clip.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing);
        AssetDatabase.SaveAssetIfDirty(existing);
        Object.DestroyImmediate(clip);
        return existing;
    }

    // ---------- 측정 ----------
    // 지지발: 각 발 뼈가 주기 최저 높이 +2.5cm 안이고 반대 발보다 낮은 프레임. 그 수평 속도 중앙값 = 미끄러지지 않는 이동 속도.
    // footDrift: 지지 구간 수평 속도의 사분위 범위(작을수록 발이 한 점에 고정된다).
    internal static ClipReport Measure(AnimationClip clip, bool loop)
    {
        var r = new ClipReport { name = clip.name, length = clip.length };
        using var stage = new PA_PlayerMotionStyleLab.Stage(16, 16, 1.75f, null, false);
        var feet = new[] { stage.Animator.GetBoneTransform(HumanBodyBones.LeftFoot), stage.Animator.GetBoneTransform(HumanBodyBones.RightFoot) };
        var hips = stage.Animator.GetBoneTransform(HumanBodyBones.Hips);
        const int Steps = 160;
        var pos = new Vector3[2, Steps + 1];
        r.hipsMin = float.MaxValue; r.hipsMax = float.MinValue;
        for (int s = 0; s <= Steps; s++)
        {
            stage.Sample(clip, clip.length * s / Steps);
            for (int f = 0; f < 2; f++) pos[f, s] = feet[f].position;
            r.hipsMin = Mathf.Min(r.hipsMin, hips.position.y);
            r.hipsMax = Mathf.Max(r.hipsMax, hips.position.y);
        }
        if (!loop) return r;
        // 지지 구간(그 발이 더 낮고 최저 +2.5cm 안)의 뒤쪽 변위 합 / 지지 시간. 발뒤꿈치→발끝 구름으로 순간 속도가 출렁이므로
        // 중앙값보다 변위 평균이 실제 미끄럼 없는 속도에 맞다. 방향은 몸 정면(+z) 기준 뒤쪽.
        float dt = clip.length / Steps;
        float back = 0f, time = 0f;
        var speeds = new List<float>();
        for (int f = 0; f < 2; f++)
        {
            float min = float.MaxValue;
            for (int s = 0; s <= Steps; s++) min = Mathf.Min(min, pos[f, s].y);
            for (int s = 1; s <= Steps; s++)
            {
                bool low = pos[f, s].y < min + .025f && pos[f, s - 1].y < min + .025f && pos[f, s].y <= pos[1 - f, s].y + .01f;
                if (!low) continue;
                float dz = -(pos[f, s].z - pos[f, s - 1].z);
                back += dz; time += dt;
                speeds.Add(dz / dt);
            }
        }
        speeds.Sort();
        r.stanceSamples = speeds.Count;
        if (time > 0f)
        {
            r.naturalSpeed = back / time;
            r.footDrift = speeds[speeds.Count * 3 / 4] - speeds[speeds.Count / 4];
        }
        return r;
    }

    // ---------- 검토 렌더 ----------
    static readonly Vector3 GameView = new Vector3(0, 13.61f, -11.31f).normalized * 3.6f;

    internal static void RenderReview(AnimationClip clip)
    {
        const int W = 220, H = 280, Phases = 8;
        var tiles = new List<Color[]>();
        using var stage = new PA_PlayerMotionStyleLab.Stage(W, H, 1.75f, null, false);
        Vector3[] views = { new Vector3(2.7f, .2f, 0), new Vector3(0, .2f, 2.7f), GameView };
        foreach (var view in views)
            for (int i = 0; i < Phases; i++)
            {
                stage.Sample(clip, clip.length * i / Phases);
                tiles.Add(stage.Render(view, Vector3.up * .62f));
            }
        SaveSheet(clip.name + "-review.png", tiles, Phases, W, H);
    }

    // 제자리 재생 프레임(측면 | 게임 카메라). ffmpeg로 영상·접촉 시트를 만들어 리듬과 상하 움직임을 판정한다.
    internal static void RenderLoopFrames(AnimationClip clip, float seconds, int fps)
    {
        string dir = Path.Combine(Output, "frames_" + clip.name);
        Directory.CreateDirectory(dir);
        foreach (string old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
        using var stage = new PA_PlayerMotionStyleLab.Stage(360, 420, 1.75f, null, false);
        var pair = new Texture2D(720, 420, TextureFormat.RGB24, false);
        int count = Mathf.RoundToInt(seconds * fps);
        for (int i = 0; i < count; i++)
        {
            stage.Sample(clip, Mathf.Repeat(i / (float)fps, clip.length));
            pair.SetPixels(0, 0, 360, 420, stage.Render(new Vector3(2.4f, .15f, 0), Vector3.up * .62f));
            pair.SetPixels(360, 0, 360, 420, stage.Render(GameView * 1.1f, Vector3.up * .62f));
            pair.Apply();
            File.WriteAllBytes(Path.Combine(dir, i.ToString("0000") + ".png"), pair.EncodeToPNG());
        }
        Object.DestroyImmediate(pair);
    }

    static void SaveSheet(string file, List<Color[]> tiles, int columns, int w, int h)
    {
        int rows = Mathf.CeilToInt(tiles.Count / (float)columns);
        var sheet = new Texture2D(w * columns, h * rows, TextureFormat.RGB24, false);
        for (int i = 0; i < tiles.Count; i++)
            sheet.SetPixels(i % columns * w, (rows - 1 - i / columns) * h, w, h, tiles[i]);
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(Output, file), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
    }

    // ---------- 제품 연결 ----------
    // Locomotion 블렌드 임계값 = 게임 속도(PlayerLocomotionAnimator와 같은 값): Idle 0 · Walk · Jog(기본 이동 4.2) · Sprint(Shift 6.72).
    // 발 고정은 PlayerLocomotionAnimator가 재생 배율 = 실제 속도 / 클립 자연 속도(보고서 값)로 맞춘다. 점프 3상태도 새 클립으로 바꾼다.
    public const float WalkThreshold = 0.6f, JogThreshold = 4.2f, SprintThreshold = 6.72f;
    [MenuItem("Project PA/Art/Player Locomotion (UAL)/Apply To Product Controller")]
    public static void ApplyToProductController()
    {
        string reportPath = Path.Combine(Output, ReportName);
        if (!File.Exists(reportPath)) throw new InvalidOperationException("먼저 Build를 실행해 " + reportPath + "를 만드세요.");
        var report = JsonUtility.FromJson<Report>(File.ReadAllText(reportPath));
        float Speed(string n) => report.clips.First(c => c.name == n).naturalSpeed;
        AnimationClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + n + ".anim")
            ?? throw new InvalidOperationException("missing " + n);
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ProductControllerPath);
        var machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState State(string n) => machine.states.First(st => st.state.name == n).state;
        var tree = (UnityEditor.Animations.BlendTree)State("Locomotion").motion;
        var children = tree.children;
        if (children.Length != 4) throw new InvalidOperationException("Locomotion 블렌드 자식이 4개가 아닙니다: " + children.Length);
        float walk = Speed("PA_Loco_Walk"), jog = Speed("PA_Loco_Jog"), sprint = Speed("PA_Loco_Sprint");
        if (!(walk > .2f && jog > walk && sprint > jog)) throw new InvalidOperationException($"측정 속도 순서 이상 walk={walk} jog={jog} sprint={sprint}");
        tree.useAutomaticThresholds = false;
        children[0].motion = Clip("PA_Loco_Idle"); children[0].threshold = 0f;
        children[1].motion = Clip("PA_Loco_Walk"); children[1].threshold = WalkThreshold;
        children[2].motion = Clip("PA_Loco_Jog"); children[2].threshold = JogThreshold;
        children[3].motion = Clip("PA_Loco_Sprint"); children[3].threshold = SprintThreshold;
        for (int i = 0; i < children.Length; i++) children[i].timeScale = 1f;
        tree.children = children;
        State("JumpStart").motion = Clip("PA_Loco_JumpStart");
        State("Airborne").motion = Clip("PA_Loco_Airborne");
        State("Land").motion = Clip("PA_Loco_Land");
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("[LocoRebuild] product controller thresholds " + string.Join(", ", tree.children.Select(c =>
            c.motion.name + "@" + c.threshold.ToString("F2", CultureInfo.InvariantCulture))) +
            $" · natural walk={walk:F2} jog={jog:F2} sprint={sprint:F2}");
    }
}
