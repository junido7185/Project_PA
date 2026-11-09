using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-01 사용자 피드백 "딩컴 키 포즈 이동(PA_DK_*)은 로봇처럼 기계적이고 생동감이 없다".
// 공식으로 만든 키 포즈 대신 실제 배우 모캡(반다이남코 리서치 모션 데이터셋, CC BY-NC 4.0 — 비상업 전시용, 출처 표기)을 쓴다.
// 아이(childish)·명랑(happy)·젊은(youthful) 스타일 걷기/달리기 BVH를 BVH 골격용 임시 휴머노이드 아바타로 읽어
// Unity 머슬 공간으로 리타깃하고, 한 걸음 주기를 골라 이음매 없는 루프 클립(PA_MC_*)으로 굽는다.
// 배치: Unity -batchmode -executeMethod PA_MocapLocomotion.BatchBuild (후보 클립·검토 시트·보고서)
public static class PA_MocapLocomotion
{
    public const string SourceFolder = "Assets/Art/External/BandaiNamcoResearch/Motion";
    public const string Folder = "Assets/Art/Animation/PlayerLocomotionMocap";
    const string CalibrationTake = "dataset-1_bow_childish_001";
    const string FingerSource = "Assets/Art/Animation/PlayerMotionStyle/PA_DK_Run.anim";
    const string ReportName = "mocap-report.json";
    static string Output => Environment.GetEnvironmentVariable("PA_LOCO_OUT") ?? "Logs/ParallelProduction/Feedback-20261101/mocap";

    static readonly (string bvh, string human)[] HumanMap =
    {
        ("Hips", "Hips"), ("Spine", "Spine"), ("Chest", "Chest"), ("Neck", "Neck"), ("Head", "Head"),
        ("Shoulder_L", "LeftShoulder"), ("UpperArm_L", "LeftUpperArm"), ("LowerArm_L", "LeftLowerArm"), ("Hand_L", "LeftHand"),
        ("Shoulder_R", "RightShoulder"), ("UpperArm_R", "RightUpperArm"), ("LowerArm_R", "RightLowerArm"), ("Hand_R", "RightHand"),
        ("UpperLeg_L", "LeftUpperLeg"), ("LowerLeg_L", "LeftLowerLeg"), ("Foot_L", "LeftFoot"), ("Toes_L", "LeftToes"),
        ("UpperLeg_R", "RightUpperLeg"), ("LowerLeg_R", "RightLowerLeg"), ("Foot_R", "RightFoot"), ("Toes_R", "RightToes"),
    };

    // 곡선 속성명에 Part가 들어 있으면 적용(좌우 공통). 새 값 = 평균×Mean + Add + Amp×(값 − 평균).
    internal sealed class Rule { public string Part; public float Amp = 1f, Mean = 1f, Add; }

    internal sealed class Take
    {
        public string File, Name;
        public int Start = -1, Cycles = 1;
        public bool Idle;
        public readonly List<Rule> Rules = new List<Rule>();
        public Take(string file, string name) { File = file; Name = name; }
        public Take R(string part, float amp, float mean = 1f, float add = 0f)
        { Rules.Add(new Rule { Part = part, Amp = amp, Mean = mean, Add = add }); return this; }
        // 머리가 큰 C-01은 걸음마다 상체가 좌우로 기우는 동작이 머리 끝에서 크게 증폭돼 "달랑거림"으로 보인다
        // (2026-11-02 사용자 피드백, 기본 달리기 머리 좌우 9.7cm). 척추·가슴·윗가슴 좌우 기울임과 목·머리 기울임/돌림을 줄이고,
        // 끄덕임은 흔들림만 줄인다. 다리·팔은 그대로.
        // 몸(골반) 좌우 롤과 좌우 이동이 머리 좌우 흔들림의 대부분이다(상체 머슬만 줄였을 때 9.2→9.3cm로 변화 없음).
        public float BodyRoll = 1f, BodySway = 1f;
        public Take SteadyHead()
        {
            BodyRoll = .3f; BodySway = .45f;
            return R("Spine Left-Right", .35f, .5f).R("Chest Left-Right", .3f, .5f)
                .R("Tilt Left-Right", .2f, .4f).R("Turn Left-Right", .25f, .4f).R("Nod Down-Up", .5f);
        }
    }

    internal static List<Take> Candidates() => new List<Take>
    {
        new Take("dataset-1_walk_childish_002", "PA_MC_Walk_Childish"),
        new Take("dataset-1_walk_happy_002", "PA_MC_Walk_Happy"),
        new Take("dataset-2_walk_youthful_001", "PA_MC_Walk_Youthful"),
        new Take("dataset-2_walk_active_001", "PA_MC_Walk_Active"),
        new Take("dataset-1_run_childish_001", "PA_MC_Run_Childish"),
        new Take("dataset-1_run_happy_001", "PA_MC_Run_Happy"),
        new Take("dataset-1_dash_happy_001", "PA_MC_Dash_Happy"),
        new Take("dataset-1_dash_childish_001", "PA_MC_Dash_Childish"),
        new Take("dataset-2_run_youthful_002", "PA_MC_Run_Youthful"),
        new Take("dataset-2_run_active_001", "PA_MC_Run_Active"),
        // C-01은 다리가 짧아 같은 관절 각도로는 보폭이 작다(게임 기본 이동 4.2m/s). 엉덩이 앞뒤 스윙만 넓혀 보폭을 키운 변형.
        new Take("dataset-1_run_childish_001", "PA_MC_Run_Childish_S").R("Upper Leg Front-Back", 1.3f),
        new Take("dataset-1_dash_childish_001", "PA_MC_Dash_Childish_S").R("Upper Leg Front-Back", 1.25f),
        new Take(CalibrationTake, "PA_MC_Idle") { Idle = true },
    };

    // 제품 클립(PA_MC_Idle/Walk/Run/Sprint). 루프 시작 = 왼발이 가장 앞선 순간이라 블렌드 트리 정규화 시간에서 발 위상이 맞는다.
    // 2026-11-02 사용자 "플레이어 모션이 바보 같다": 이전 달리기는 아이(childish) 연기 대시에 엉덩이 스윙 ×1.45라 과장된 연기가 그대로 보였다.
    // 모든 이동을 배우의 기본(normal) 스타일로 바꾸고, 짧은 다리 보폭 보정은 과장이 보이지 않는 범위(×1.1~1.25)로 줄인다.
    internal static List<Take> Product() => new List<Take>
    {
        new Take("dataset-1_bow_normal_001", "PA_MC_Idle") { Idle = true },
        new Take("dataset-2_walk_normal_001", "PA_MC_Walk").R("Upper Leg Front-Back", 1.1f).SteadyHead(),
        new Take("dataset-2_run_normal_001", "PA_MC_Run").R("Upper Leg Front-Back", 1.2f).SteadyHead(),
        new Take("dataset-2_run_normal_002", "PA_MC_Sprint").R("Upper Leg Front-Back", 1.25f).R("Lower Leg Stretch", 1.1f).SteadyHead(),
    };

    [Serializable] sealed class Entry { public string name, source; public int startFrame, endFrame; public float length, poseMismatch, takeSpeed, naturalSpeed, footDrift, hipsMin, hipsMax, headSwayCm, neckSwayCm; }

    // 2026-11-02 사용자 "머리가 좌우로 달랑거려서 그런 것 같다": 머리가 큰 C-01에서 머리 좌우 흔들림을 잰다.
    // 한 루프 동안 머리 위치의 좌우 성분(엉덩이 좌우 축 기준) 최대-최소(cm). head = 엉덩이 기준(몸통 흔들림 포함), neck = 가슴 기준(목·머리만).
    // 좌우 축은 루프 평균 엉덩이 축으로 고정한다(달리기 골반 비틀림으로 축이 돌면 앞숙임이 좌우 흔들림으로 잘못 잡힌다).
    // neck 자리에는 머리 뼈의 좌우 기울기(롤) 최대-최소(도)를 돌려준다.
    static (float head, float neck) HeadSway(AnimationClip clip)
    {
        using var stage = new PA_PlayerMotionStyleLab.Stage(16, 16, 1.75f, null, false);
        var a = stage.Animator;
        Transform head = a.GetBoneTransform(HumanBodyBones.Head), hips = a.GetBoneTransform(HumanBodyBones.Hips);
        Transform l = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg), r = a.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (head == null || hips == null || l == null || r == null) return (0f, 0f);
        const int Steps = 120;
        var offsets = new Vector3[Steps + 1];
        var ups = new Vector3[Steps + 1];
        Vector3 axis = Vector3.zero;
        for (int s = 0; s <= Steps; s++)
        {
            stage.Sample(clip, clip.length * s / Steps);
            axis += Flat(r.position - l.position).normalized;
            offsets[s] = head.position - hips.position;
            ups[s] = head.up;
        }
        Vector3 right = axis.normalized;
        float hMin = float.MaxValue, hMax = float.MinValue, rMin = float.MaxValue, rMax = float.MinValue;
        for (int s = 0; s <= Steps; s++)
        {
            float h = Vector3.Dot(offsets[s], right);
            float roll = Mathf.Atan2(Vector3.Dot(ups[s], right), Vector3.Dot(ups[s], Vector3.up)) * Mathf.Rad2Deg;
            hMin = Mathf.Min(hMin, h); hMax = Mathf.Max(hMax, h); rMin = Mathf.Min(rMin, roll); rMax = Mathf.Max(rMax, roll);
        }
        return ((hMax - hMin) * 100f, rMax - rMin);
    }
    [Serializable] sealed class Report { public List<Entry> clips = new List<Entry>(); public string notes; }

    [MenuItem("Project PA/Art/Player Locomotion (Mocap)/Build Candidate Clips + Review")]
    public static void Build() => BuildTakes(Candidates(), true);

    public static void BatchBuild() => RunBatch(() => Build());

    [MenuItem("Project PA/Art/Player Locomotion (Mocap)/Build Product Clips + Apply")]
    public static void BuildProduct()
    {
        BuildTakes(Product(), true);
        ApplyToProductController();
    }

    public static void BatchBuildProduct() => RunBatch(BuildProduct);

    // 폴더의 모든 PA_MC_* 클립의 머리 좌우 흔들림만 잰다(다시 굽지 않음).
    public static void BatchMeasureHeadSway() => RunBatch(() =>
    {
        // 비교 기준: 머리·상체 감쇠 전 기본 스타일 클립(후보 이름으로만 굽고 제품 컨트롤러에는 연결하지 않음).
        BuildTakes(new List<Take>
        {
            new Take("dataset-2_walk_normal_001", "PA_MC_Walk_Normal").R("Upper Leg Front-Back", 1.1f),
            new Take("dataset-2_run_normal_001", "PA_MC_Run_Normal").R("Upper Leg Front-Back", 1.2f),
        }, false);
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { Folder }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            var (head, neck) = HeadSway(clip);
            Debug.Log($"[Mocap] sway {clip.name} head={head:F1}cm roll={neck:F1}deg");
        }
    });

    const string ProductControllerPath = "Assets/Resources/PlayerLocomotion/PlayerLocomotion.controller";

    // 블렌드 임계값 = 게임 속도(PlayerLocomotionAnimator.Walk/Jog/SprintNaturalSpeed). 발 고정은 재생 배율 = 속도 / 보폭 속도(보고서 naturalSpeed)로 맞춘다.
    public static void ApplyToProductController()
    {
        AnimationClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + n + ".anim") ?? throw new InvalidOperationException("missing " + n);
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ProductControllerPath);
        var machine = controller.layers[0].stateMachine;
        var tree = (UnityEditor.Animations.BlendTree)machine.states.First(st => st.state.name == "Locomotion").state.motion;
        var children = tree.children;
        if (children.Length != 4) throw new InvalidOperationException("Locomotion 블렌드 자식이 4개가 아닙니다: " + children.Length);
        tree.useAutomaticThresholds = false;
        children[0].motion = Clip("PA_MC_Idle"); children[0].threshold = 0f;
        children[1].motion = Clip("PA_MC_Walk"); children[1].threshold = PlayerLocomotionAnimator.WalkNaturalSpeed;
        children[2].motion = Clip("PA_MC_Run"); children[2].threshold = PlayerLocomotionAnimator.JogNaturalSpeed;
        children[3].motion = Clip("PA_MC_Sprint"); children[3].threshold = PlayerLocomotionAnimator.SprintNaturalSpeed;
        for (int i = 0; i < children.Length; i++) children[i].timeScale = 1f;
        tree.children = children;
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("[Mocap] product controller " + string.Join(", ", tree.children.Select(c => c.motion.name + "@" + c.threshold.ToString("F2", CultureInfo.InvariantCulture))));
    }

    static void RunBatch(Action action)
    {
        int code = 0;
        try { action(); }
        catch (Exception e) { Debug.LogError("[Mocap] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    internal static List<AnimationClip> BuildTakes(List<Take> takes, bool review)
    {
        Directory.CreateDirectory(Output);
        var calibration = Bvh.Load(Path.Combine(SourceFolder, CalibrationTake + ".bvh"));
        var fingers = FingerValues();
        var report = new Report();
        var built = new List<AnimationClip>();
        using (var rig = new Rig(calibration))
        {
            var avatar = BuildAvatar(rig, calibration, out var facing);
            try
            {
                foreach (var take in takes)
                {
                    var bvh = Bvh.Load(Path.Combine(SourceFolder, take.File + ".bvh"));
                    if (bvh.Names.Count != calibration.Names.Count || !bvh.Names.SequenceEqual(calibration.Names))
                        throw new InvalidOperationException(take.File + " 골격이 보정 테이크와 다릅니다.");
                    Entry entry;
                    var clip = take.Idle ? BakeIdle(rig, avatar, bvh, take, facing, fingers, out entry) : Bake(rig, avatar, bvh, take, fingers, out entry);
                    clip = SaveClip(clip);
                    built.Add(clip);
                    var m = PA_PlayerLocomotionRebuild.Measure(clip, !take.Idle);
                    entry.naturalSpeed = m.naturalSpeed; entry.footDrift = m.footDrift; entry.hipsMin = m.hipsMin; entry.hipsMax = m.hipsMax;
                    (entry.headSwayCm, entry.neckSwayCm) = HeadSway(clip);
                    report.clips.Add(entry);
                }
            }
            finally { Object.DestroyImmediate(avatar); }
        }
        AssetDatabase.SaveAssets();
        if (review)
        {
            foreach (var clip in built) PA_PlayerLocomotionRebuild.RenderReview(clip);
            foreach (var clip in built) PA_PlayerLocomotionRebuild.RenderLoopFrames(clip, Mathf.Max(1.6f, clip.length * 3f), 30);
        }
        report.notes = "PA_MC_*: 반다이남코 리서치 모캡(CC BY-NC 4.0) 리타깃. takeSpeed=배우 원본 속도(m/s), naturalSpeed=C-01 1.75m 스테이지에서 지지발이 미끄러지지 않는 속도.";
        File.WriteAllText(Path.Combine(Output, ReportName), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        Debug.Log("[Mocap] built " + string.Join(", ", report.clips.Select(c =>
            $"{c.name} f{c.startFrame}-{c.endFrame} len={c.length:F2} take={c.takeSpeed:F2} v={c.naturalSpeed:F2} drift={c.footDrift:F2} mis={c.poseMismatch:F3} headSway={c.headSwayCm:F1}cm headRoll={c.neckSwayCm:F1}deg")));
        return built;
    }

    // ---------- BVH ----------
    internal sealed class Bvh
    {
        public readonly List<string> Names = new List<string>();
        public readonly List<int> Parents = new List<int>();
        public readonly List<Vector3> Offsets = new List<Vector3>();
        public readonly List<string[]> Channels = new List<string[]>();
        public float[][] Frames;
        public float FrameTime;
        public int Index(string name) => Names.IndexOf(name);

        public static Bvh Load(string path)
        {
            var b = new Bvh();
            string[] t = File.ReadAllText(path).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            int i = 0, current = -1;
            bool endSite = false;
            var stack = new Stack<int>();
            while (t[i] != "MOTION")
            {
                string token = t[i++];
                switch (token)
                {
                    case "ROOT":
                    case "JOINT":
                        b.Names.Add(t[i++]);
                        b.Parents.Add(stack.Count > 0 ? stack.Peek() : -1);
                        b.Offsets.Add(Vector3.zero);
                        b.Channels.Add(new string[0]);
                        current = b.Names.Count - 1;
                        endSite = false;
                        break;
                    case "End": i++; endSite = true; break;
                    case "{": stack.Push(endSite ? -2 : current); break;
                    case "}": if (stack.Pop() == -2) endSite = false; break;
                    case "OFFSET":
                        var o = new Vector3(F(t[i]), F(t[i + 1]), F(t[i + 2]));
                        i += 3;
                        if (!endSite) b.Offsets[current] = o;
                        break;
                    case "CHANNELS":
                        int n = int.Parse(t[i++], CultureInfo.InvariantCulture);
                        b.Channels[current] = t.Skip(i).Take(n).ToArray();
                        i += n;
                        break;
                }
            }
            i++; // MOTION
            if (t[i] != "Frames:") throw new FormatException(path + " Frames: 누락");
            int frames = int.Parse(t[i + 1], CultureInfo.InvariantCulture);
            if (t[i + 2] != "Frame" || t[i + 3] != "Time:") throw new FormatException(path + " Frame Time: 누락");
            b.FrameTime = F(t[i + 4]);
            i += 5;
            int width = b.Channels.Sum(c => c.Length);
            b.Frames = new float[frames][];
            for (int f = 0; f < frames; f++)
            {
                var row = new float[width];
                for (int c = 0; c < width; c++) row[c] = F(t[i++]);
                b.Frames[f] = row;
            }
            return b;
        }

        static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    // BVH(오른손 좌표, cm)를 Unity(왼손, m)로: x를 뒤집으면 X축 회전은 그대로, Y·Z축 회전은 부호가 바뀐다.
    internal sealed class Rig : IDisposable
    {
        public readonly GameObject Root;
        public readonly Transform[] Joints;
        readonly Bvh _bvh;
        readonly int[] _column;

        public Rig(Bvh bvh)
        {
            _bvh = bvh;
            Root = new GameObject("BvhRig") { hideFlags = HideFlags.HideAndDontSave };
            Joints = new Transform[bvh.Names.Count];
            _column = new int[bvh.Names.Count];
            int col = 0;
            for (int j = 0; j < Joints.Length; j++)
            {
                var go = new GameObject(bvh.Names[j]) { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(bvh.Parents[j] < 0 ? Root.transform : Joints[bvh.Parents[j]], false);
                go.transform.localPosition = ToUnity(bvh.Offsets[j]);
                Joints[j] = go.transform;
                _column[j] = col;
                col += bvh.Channels[j].Length;
            }
        }

        public Transform this[string name] => Joints[_bvh.Index(name)];

        // 같은 골격의 다른 테이크 프레임도 적용한다(채널 배치가 같음을 호출자가 확인).
        public void Apply(Bvh take, int frame, Quaternion align)
        {
            var row = take.Frames[frame];
            for (int j = 0; j < Joints.Length; j++)
            {
                Vector3 p = take.Offsets[j];
                var q = Quaternion.identity;
                var channels = take.Channels[j];
                for (int c = 0; c < channels.Length; c++)
                {
                    float v = row[_column[j] + c];
                    switch (channels[c])
                    {
                        case "Xposition": p.x = v; break;
                        case "Yposition": p.y = v; break;
                        case "Zposition": p.z = v; break;
                        case "Xrotation": q *= Quaternion.AngleAxis(v, Vector3.right); break;
                        case "Yrotation": q *= Quaternion.AngleAxis(-v, Vector3.up); break;
                        case "Zrotation": q *= Quaternion.AngleAxis(-v, Vector3.forward); break;
                    }
                }
                Vector3 pos = ToUnity(p);
                if (take.Parents[j] < 0) { pos = align * pos; q = align * q; }
                Joints[j].localPosition = pos;
                Joints[j].localRotation = q;
            }
        }

        static Vector3 ToUnity(Vector3 v) => new Vector3(-v.x, v.y, v.z) * 0.01f;

        public void Dispose() => Object.DestroyImmediate(Root);
    }

    // 보정 테이크(인사 직전 선 자세) 0프레임을 정면(+Z)으로 돌린 뒤, 팔·다리만 최소 회전으로 펴서 T 포즈를 만든다.
    // 척추·머리·발목·손목은 배우의 자연스러운 선 자세 그대로라, 머슬 값은 "편히 선 자세에서 벗어난 양"이 된다.
    static Avatar BuildAvatar(Rig rig, Bvh calibration, out Quaternion facing)
    {
        rig.Apply(calibration, 0, Quaternion.identity);
        Vector3 right = rig["UpperLeg_R"].position - rig["UpperLeg_L"].position;
        right.y = 0f;
        Vector3 forward = Vector3.Cross(right.normalized, Vector3.up);
        facing = YawTo(forward);
        rig.Apply(calibration, 0, facing);
        Vector3 shift = rig["Hips"].position;
        shift.y = 0f;
        rig.Joints[0].position -= shift;
        foreach (string s in new[] { "L", "R" })
        {
            var foot = rig["Foot_" + s];
            Quaternion footWorld = foot.rotation;
            Point(rig["UpperLeg_" + s], rig["LowerLeg_" + s].position, Vector3.down);
            Point(rig["LowerLeg_" + s], foot.position, Vector3.down);
            foot.rotation = footWorld;
            Vector3 toe = rig["Toes_" + s].position - foot.position;
            toe.y = 0f;
            foot.rotation = Quaternion.FromToRotation(toe.normalized, Vector3.forward) * foot.rotation;
            Vector3 side = s == "L" ? Vector3.left : Vector3.right;
            Point(rig["UpperArm_" + s], rig["LowerArm_" + s].position, side);
            Point(rig["LowerArm_" + s], rig["Hand_" + s].position, side);
        }
        var description = new HumanDescription
        {
            human = HumanMap.Select(m => new HumanBone { boneName = m.bvh, humanName = m.human, limit = new HumanLimit { useDefaultValues = true } }).ToArray(),
            skeleton = rig.Root.GetComponentsInChildren<Transform>(true)
                .Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray(),
            upperArmTwist = .5f, lowerArmTwist = .5f, upperLegTwist = .5f, lowerLegTwist = .5f,
            armStretch = .05f, legStretch = .05f, feetSpacing = 0f, hasTranslationDoF = false,
        };
        var avatar = AvatarBuilder.BuildHumanAvatar(rig.Root, description);
        if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("BVH 휴머노이드 아바타 생성 실패");
        avatar.name = "BvhAvatar";
        avatar.hideFlags = HideFlags.HideAndDontSave;
        return avatar;
    }

    static void Point(Transform bone, Vector3 child, Vector3 direction)
    {
        Vector3 current = (child - bone.position).normalized;
        bone.rotation = Quaternion.FromToRotation(current, direction) * bone.rotation;
    }

    // ---------- 추출·주기 선택 ----------
    sealed class Frame { public float[] Muscles; public Vector3 Body; public Quaternion Rotation; public Vector3 Hips, FootL, FootR; }

    static Frame[] Extract(Rig rig, Avatar avatar, Bvh take, Quaternion align)
    {
        var frames = new Frame[take.Frames.Length];
        var pose = new HumanPose();
        using (var handler = new HumanPoseHandler(avatar, rig.Root.transform))
        {
            for (int f = 0; f < frames.Length; f++)
            {
                rig.Apply(take, f, align);
                handler.GetHumanPose(ref pose);
                frames[f] = new Frame
                {
                    Muscles = (float[])pose.muscles.Clone(), Body = pose.bodyPosition, Rotation = pose.bodyRotation,
                    Hips = rig["Hips"].position, FootL = rig["Foot_L"].position, FootR = rig["Foot_R"].position,
                };
            }
        }
        return frames;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    // 수평 방향을 +Z로 돌리는 요 회전만 쓴다(FromToRotation은 정반대 방향일 때 임의 축으로 180° 돌려 몸이 뒤집힌다).
    static Quaternion YawTo(Vector3 direction) => Quaternion.AngleAxis(Vector3.SignedAngle(Flat(direction), Vector3.forward, Vector3.up), Vector3.up);

    // 왼발이 오른발보다 가장 앞선 순간(진행 방향 기준 극대)을 주기 경계로 쓴다. 속도가 안정된 구간(최대의 85% 이상)에서
    // 이웃 극대 쌍 중 끝 프레임 자세가 시작 자세와 가장 닮은(머슬 차이 최소) 한 주기를 고른다.
    static (int a, int b, float mismatch) PickCycle(Frame[] frames, float dt, Take take)
    {
        int n = frames.Length;
        var speed = new float[n];
        for (int f = 1; f < n; f++) speed[f] = Flat(frames[f].Hips - frames[f - 1].Hips).magnitude / dt;
        speed[0] = speed[Math.Min(1, n - 1)];
        var smooth = new float[n];
        for (int f = 0; f < n; f++)
        {
            float sum = 0f; int count = 0;
            for (int k = Math.Max(0, f - 2); k <= Math.Min(n - 1, f + 2); k++) { sum += speed[k]; count++; }
            smooth[f] = sum / count;
        }
        float max = smooth.Max();
        var rel = frames.Select(fr => fr.FootL.z - fr.FootR.z).ToArray();
        var peaks = new List<int>();
        for (int f = 1; f < n - 1; f++)
            if (rel[f] > rel[f - 1] && rel[f] >= rel[f + 1] && rel[f] > .08f && smooth[f] > .7f * max) peaks.Add(f);
        float Mismatch(int a, int b) => frames[a].Muscles.Zip(frames[b].Muscles, (x, y) => (x - y) * (x - y)).Sum() / frames[a].Muscles.Length;
        if (take.Start >= 0)
        {
            int startPeak = peaks.Where(p => p >= take.Start).DefaultIfEmpty(take.Start).First();
            int idx = peaks.IndexOf(startPeak);
            if (idx < 0 || idx + take.Cycles >= peaks.Count) throw new InvalidOperationException(take.File + " 지정 시작 뒤 주기가 부족합니다.");
            return Refine(startPeak, peaks[idx + take.Cycles]);
        }
        // 걸음 안에서도 골반 속도가 출렁이므로 프레임별이 아니라 주기 평균 속도로 안정 구간(최고 주기 평균의 90% 이상)을 고른다.
        var cycles = new List<(int a, int b, float v)>();
        for (int i = 0; i + take.Cycles < peaks.Count; i++)
        {
            int a = peaks[i], b = peaks[i + take.Cycles];
            float period = (b - a) * dt / take.Cycles;
            if (period < .35f || period > 1.6f) continue;
            cycles.Add((a, b, Flat(frames[b].Hips - frames[a].Hips).magnitude / ((b - a) * dt)));
        }
        float top = cycles.Count > 0 ? cycles.Max(c => c.v) : 0f;
        (int, int, float) best = (-1, -1, float.MaxValue);
        foreach (var c in cycles.Where(c => c.v >= .9f * top))
        {
            var r = Refine(c.a, c.b);
            if (r.Item3 < best.Item3) best = r;
        }
        if (best.Item1 < 0) throw new InvalidOperationException(take.File + " 안정 주기를 찾지 못했습니다. peaks=" + string.Join(",", peaks));
        return best;

        (int, int, float) Refine(int a, int b)
        {
            (int, int, float) r = (a, b, Mismatch(a, b));
            for (int d = -2; d <= 2; d++)
            {
                int e = b + d;
                if (e <= a + 4 || e >= n) continue;
                float m = Mismatch(a, e);
                if (m < r.Item3) r = (a, e, m);
            }
            return r;
        }
    }

    // ---------- 굽기 ----------
    static AnimationClip Bake(Rig rig, Avatar avatar, Bvh take, Take spec, Dictionary<string, float> fingers, out Entry entry)
    {
        float dt = take.FrameTime;
        // 1차: 전체 이동 방향으로 정렬해 주기를 찾고, 2차: 고른 주기의 실제 진행 방향으로 다시 정렬한다.
        rig.Apply(take, 0, Quaternion.identity);
        Vector3 first = rig["Hips"].position;
        rig.Apply(take, take.Frames.Length - 1, Quaternion.identity);
        Vector3 last = rig["Hips"].position;
        var align = YawTo(last - first);
        var frames = Extract(rig, avatar, take, align);
        var (a, b, mismatch) = PickCycle(frames, dt, spec);
        Vector3 travel = Flat(frames[b].Hips - frames[a].Hips);
        align = YawTo(travel) * align;
        frames = Extract(rig, avatar, take, align);
        int n = b - a;
        float length = n * dt;
        entry = new Entry
        {
            name = spec.Name, source = spec.File, startFrame = a, endFrame = b, length = length, poseMismatch = mismatch,
            takeSpeed = Flat(frames[b].Hips - frames[a].Hips).magnitude / length,
        };

        var clip = new AnimationClip { name = spec.Name, frameRate = 30 };
        string[] muscleNames = HumanTrait.MuscleName;
        for (int m = 0; m < muscleNames.Length; m++)
        {
            string muscle = muscleNames[m];
            if (fingers.TryGetValue(muscle, out float finger))
            {
                clip.SetCurve("", typeof(Animator), muscle, AnimationCurve.Constant(0f, length, finger));
                continue;
            }
            var values = new float[n + 1];
            for (int k = 0; k <= n; k++) values[k] = frames[a + k].Muscles[m];
            LoopFix(values);
            var rule = spec.Rules.FirstOrDefault(r => muscle.Contains(r.Part));
            if (rule != null) Restyle(values, rule);
            clip.SetCurve("", typeof(Animator), muscle, LoopCurve(values, dt));
        }

        // 몸통 방향: 주기 평균 요를 0(정면)으로, 끝 회전을 시작 회전에 맞도록 주기에 걸쳐 나눠 보정한다.
        var rotations = new Quaternion[n + 1];
        var mean = Vector4.zero;
        for (int k = 0; k <= n; k++)
        {
            var q = frames[a + k].Rotation;
            if (k > 0 && Quaternion.Dot(q, rotations[k - 1]) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            rotations[k] = q;
            mean += new Vector4(q.x, q.y, q.z, q.w);
        }
        var meanQ = new Quaternion(mean.x, mean.y, mean.z, mean.w).normalized;
        var unyaw = Quaternion.Euler(0f, -meanQ.eulerAngles.y, 0f);
        var drift = Quaternion.Inverse((unyaw * rotations[n]) * Quaternion.Inverse(unyaw * rotations[0]));
        var body = new Vector3[n + 1];
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n;
            rotations[k] = Quaternion.Slerp(Quaternion.identity, drift, t) * (unyaw * rotations[k]);
            if (k > 0 && Quaternion.Dot(rotations[k], rotations[k - 1]) < 0f) rotations[k] = new Quaternion(-rotations[k].x, -rotations[k].y, -rotations[k].z, -rotations[k].w);
            body[k] = unyaw * frames[a + k].Body;
        }
        if (spec.BodyRoll < .999f)
        {
            // 루프 평균 자세 기준으로 앞축(z) 롤만 줄인다. 요(골반 비틀림)·피치(앞숙임)는 그대로.
            var sum = Vector4.zero;
            foreach (var q in rotations) sum += new Vector4(q.x, q.y, q.z, q.w);
            var meanBody = new Quaternion(sum.x, sum.y, sum.z, sum.w).normalized;
            float Signed(float angle) => angle > 180f ? angle - 360f : angle;
            for (int k = 0; k <= n; k++)
            {
                Vector3 e = (Quaternion.Inverse(meanBody) * rotations[k]).eulerAngles;
                rotations[k] = meanBody * Quaternion.Euler(Signed(e.x), Signed(e.y), Signed(e.z) * spec.BodyRoll);
                if (k > 0 && Quaternion.Dot(rotations[k], rotations[k - 1]) < 0f) rotations[k] = new Quaternion(-rotations[k].x, -rotations[k].y, -rotations[k].z, -rotations[k].w);
            }
        }
        string[] axes = { "x", "y", "z", "w" };
        for (int c = 0; c < 4; c++)
        {
            var values = rotations.Select(q => q[c]).ToArray();
            clip.SetCurve("", typeof(Animator), "RootQ." + axes[c], LoopCurve(values, dt));
        }
        for (int c = 0; c < 3; c++)
        {
            var values = body.Select(p => p[c]).ToArray();
            LoopFix(values);
            if (c != 1) { float avg = values.Take(n).Average(); for (int k = 0; k <= n; k++) values[k] -= avg; }
            if (c == 0) for (int k = 0; k <= n; k++) values[k] *= spec.BodySway; // 좌우 몸 이동(걸음마다 체중 옮김)
            clip.SetCurve("", typeof(Animator), "RootT." + axes[c], LoopCurve(values, dt));
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlendOrientation = true; settings.keepOriginalOrientation = false;
        settings.loopBlendPositionY = true; settings.keepOriginalPositionY = true; settings.heightFromFeet = false;
        settings.loopBlendPositionXZ = true; settings.keepOriginalPositionXZ = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        PA_PlayerLocomotionRebuild.Ground(clip);
        return clip;
    }

    // 서 있기: 보정 테이크 첫 3프레임(인사 직전 편히 선 자세) 평균 + 숨쉬기·머리·어깨의 작은 움직임(4초 루프).
    // 하체·몸 위치는 고정해 발이 미끄러지지 않게 하고, 주기를 서로 다르게(1·2·3회/루프) 겹쳐 기계적인 반복감을 줄인다.
    const float IdleLength = 4f;
    static AnimationClip BakeIdle(Rig rig, Avatar avatar, Bvh take, Take spec, Quaternion facing, Dictionary<string, float> fingers, out Entry entry)
    {
        var frames = Extract(rig, avatar, take, facing).Take(3).ToArray();
        entry = new Entry { name = spec.Name, source = spec.File, startFrame = 0, endFrame = frames.Length - 1, length = IdleLength };
        var clip = new AnimationClip { name = spec.Name, frameRate = 30 };
        string[] muscleNames = HumanTrait.MuscleName;
        float Wave(float p, float cycles, float phase) => Mathf.Sin(2f * Mathf.PI * (cycles * p + phase));
        var motion = new Dictionary<string, Func<float, float>>
        {
            ["Chest Front-Back"] = p => .022f * Wave(p, 2f, 0f),
            ["Spine Front-Back"] = p => .010f * Wave(p, 2f, .05f),
            ["Spine Left-Right"] = p => .012f * Wave(p, 1f, .2f),
            ["Chest Twist Left-Right"] = p => .010f * Wave(p, 1f, .55f),
            ["Left Shoulder Down-Up"] = p => .035f * Wave(p, 2f, .08f),
            ["Right Shoulder Down-Up"] = p => .035f * Wave(p, 2f, .08f),
            ["Left Arm Down-Up"] = p => .012f * Wave(p, 2f, .12f),
            ["Right Arm Down-Up"] = p => .012f * Wave(p, 2f, .12f),
            ["Left Arm Front-Back"] = p => .015f * Wave(p, 1f, .3f),
            ["Right Arm Front-Back"] = p => -.012f * Wave(p, 1f, .35f),
            ["Left Forearm Stretch"] = p => .010f * Wave(p, 3f, .1f),
            ["Right Forearm Stretch"] = p => .010f * Wave(p, 3f, .4f),
            ["Neck Nod Down-Up"] = p => .012f * Wave(p, 2f, .3f),
            ["Head Nod Down-Up"] = p => .015f * Wave(p, 1f, .7f),
            ["Head Turn Left-Right"] = p => .035f * Wave(p, 1f, .15f),
            ["Head Tilt Left-Right"] = p => .020f * Wave(p, 1f, .45f),
        };
        const int Keys = 120;
        float dt = IdleLength / Keys;
        for (int m = 0; m < muscleNames.Length; m++)
        {
            string muscle = muscleNames[m];
            if (fingers.TryGetValue(muscle, out float finger))
            {
                clip.SetCurve("", typeof(Animator), muscle, AnimationCurve.Constant(0f, IdleLength, finger));
                continue;
            }
            float baseValue = frames.Average(f => f.Muscles[m]);
            var values = new float[Keys + 1];
            motion.TryGetValue(muscle, out var add);
            for (int k = 0; k <= Keys; k++) values[k] = baseValue + (add != null ? add(k / (float)Keys) : 0f);
            clip.SetCurve("", typeof(Animator), muscle, LoopCurve(values, dt));
        }
        var q = frames[0].Rotation;
        var unyaw = Quaternion.Euler(0f, -q.eulerAngles.y, 0f);
        q = unyaw * q;
        Vector3 body = unyaw * frames[0].Body;
        string[] axes = { "x", "y", "z", "w" };
        for (int c = 0; c < 4; c++) clip.SetCurve("", typeof(Animator), "RootQ." + axes[c], AnimationCurve.Constant(0f, IdleLength, q[c]));
        clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Constant(0f, IdleLength, 0f));
        clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Constant(0f, IdleLength, 0f));
        var y = new float[Keys + 1];
        for (int k = 0; k <= Keys; k++) y[k] = body.y + .002f * Wave(k / (float)Keys, 2f, .02f);
        clip.SetCurve("", typeof(Animator), "RootT.y", LoopCurve(y, dt));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlendOrientation = true; settings.keepOriginalOrientation = false;
        settings.loopBlendPositionY = true; settings.keepOriginalPositionY = true; settings.heightFromFeet = false;
        settings.loopBlendPositionXZ = true; settings.keepOriginalPositionXZ = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        PA_PlayerLocomotionRebuild.Ground(clip);
        return clip;
    }

    // 끝 값이 시작 값과 같아지도록 차이를 주기 전체에 선형으로 나눈다(모캡 한 주기의 미세한 어긋남 제거).
    static void LoopFix(float[] v)
    {
        int n = v.Length - 1;
        float d = v[n] - v[0];
        for (int k = 0; k <= n; k++) v[k] -= d * k / n;
    }

    static void Restyle(float[] v, Rule rule)
    {
        int n = v.Length - 1;
        float mean = v.Take(n).Average();
        float target = mean * rule.Mean + rule.Add;
        for (int k = 0; k <= n; k++) v[k] = target + rule.Amp * (v[k] - mean);
    }

    // 30fps 키, 기울기는 루프를 감싼 중앙 차분(끝 키 = 시작 키라 이음매에서 속도도 이어진다).
    static AnimationCurve LoopCurve(float[] v, float dt)
    {
        int n = v.Length - 1;
        var keys = new Keyframe[n + 1];
        for (int k = 0; k <= n; k++)
        {
            float prev = k == 0 ? v[n - 1] : v[k - 1];
            float next = k == n ? v[1] : v[k + 1];
            float slope = (next - prev) / (2f * dt);
            keys[k] = new Keyframe(k * dt, v[k], slope, slope);
        }
        return new AnimationCurve(keys);
    }

    // BVH에는 손가락이 없으므로 기존 클립의 살짝 쥔 손가락 값을 상수로 쓴다.
    static Dictionary<string, float> FingerValues()
    {
        var result = new Dictionary<string, float>();
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(FingerSource);
        string[] parts = { "Thumb", "Index", "Middle", "Ring", "Little" };
        foreach (string muscle in HumanTrait.MuscleName)
        {
            if (!parts.Any(muscle.Contains)) continue;
            float value = 0f;
            if (source != null)
            {
                var curve = AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle));
                if (curve != null) value = curve.Evaluate(0f);
            }
            result[muscle] = value;
        }
        return result;
    }

    static AnimationClip SaveClip(AnimationClip clip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Animation")) AssetDatabase.CreateFolder("Assets/Art", "Animation");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art/Animation", "PlayerLocomotionMocap");
        string path = Folder + "/" + clip.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing);
        AssetDatabase.SaveAssetIfDirty(existing);
        Object.DestroyImmediate(clip);
        return existing;
    }
}
