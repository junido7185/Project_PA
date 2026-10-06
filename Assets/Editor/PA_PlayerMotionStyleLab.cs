using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// TASK 01-C 모션 재작업: 딩컴 레퍼런스 기준 맨손 대기/달리기 스타일 샘플.
// 키 포즈(휴머노이드 머슬 값)를 코드 데이터로 두고 클립을 생성한다. 제품 컨트롤러는 바꾸지 않는다.
public static class PA_PlayerMotionStyleLab
{
    public const string Folder = "Assets/Art/Animation/PlayerMotionStyle";
    const string LabOutput = "Logs/VisualQA/Task01C-Locomotion/StyleRework-20260929/lab";

    // ---------- 미리보기 렌더 ----------
    // 편집 모드의 SkinnedMeshRenderer는 Camera.Render 직전 뼈 변경을 스킨에 반영하지 않으므로 BakeMesh로 그린다.
    sealed class Stage : IDisposable
    {
        public Scene Scene;
        // 바인드 자세(발바닥이 지면에 평평하게 닿은 원본 자세)의 발 뼈 높이·회전. 접지 목표와 발 각도 기준.
        public float BindFootY;
        public Quaternion BindFootRotation;
        public Vector3 BindToeAxis;
        public float SoleBottomY = float.MaxValue;
        public readonly List<(Transform foot, Vector3 local)> SolePoints = new List<(Transform, Vector3)>();
        public float LowestSole() => SolePoints.Min(s => (s.foot.position + s.foot.rotation * s.local).y);
        // 바인드 자세 메시의 가장 낮은 정점 높이와, 마지막 Sample의 스킨 메시 최저 정점 높이. 접지는 실제 메시 기준으로 맞춘다
        // (바닥 두 점 기준은 발이 기울 때 메시보다 최대 1.5cm 낮게 잡힌다).
        public float BindMeshBottomY;
        public float LowestMesh()
        {
            float low = float.MaxValue;
            foreach (var v in _baked.vertices) low = Mathf.Min(low, (_skin.transform.position + _skin.transform.rotation * v).y);
            return low;
        }
        // 발끝 방향의 수평 대비 기울기(도). +는 발끝이 위.
        public float ToePitch(Transform foot) => Mathf.Asin(Mathf.Clamp((foot.rotation * BindToeAxis).normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        public GameObject Model;
        public Animator Animator;
        public Camera Camera;
        readonly RenderTexture _rt;
        readonly Texture2D _read;
        readonly SkinnedMeshRenderer _skin;
        readonly MeshFilter _bakedFilter;
        readonly Mesh _baked = new Mesh();
        public readonly int Width, Height;

        public Stage(int width, int height, float modelHeight = 1.75f, GameObject prefab = null, bool rigFix = true)
        {
            Width = width; Height = height;
            Scene = EditorSceneManager.NewPreviewScene();
            Model = FirstDayStudioAssets.Place(prefab != null ? prefab : FirstDayStudioAssets.Load().player, null, Vector3.zero, modelHeight);
            SceneManager.MoveGameObjectToScene(Model, Scene);
            Animator = Model.GetComponentInChildren<Animator>();
            if (rigFix) PlayerRigFix.TryApply(Animator);
            Animator.applyRootMotion = false;
            _skin = Model.GetComponentInChildren<SkinnedMeshRenderer>();
            _skin.enabled = false;
            var bindFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            BindFootY = bindFoot.position.y;
            BindFootRotation = bindFoot.rotation;
            // 바인드에서 몸 앞쪽을 가장 잘 가리키는 발 뼈 로컬 축 = 발끝 방향.
            Vector3 fwd = Model.transform.forward;
            BindToeAxis = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back }
                .OrderByDescending(a => Vector3.Dot(bindFoot.rotation * a, fwd)).First();
            // 신발 바닥의 뒤꿈치·발끝 점(바인드 메시에서 발 뼈 가중치가 큰 가장 낮은 정점들의 앞뒤 끝)을 발 뼈 로컬로 저장한다.
            var mesh = _skin.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var toWorld = _skin.transform.localToWorldMatrix;
            foreach (var side in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var foot = Animator.GetBoneTransform(side);
                int index = Array.IndexOf(_skin.bones, foot);
                var points = new List<Vector3>();
                for (int v = 0; v < vertices.Length; v++)
                {
                    var w = weights[v];
                    float fw = (w.boneIndex0 == index ? w.weight0 : 0) + (w.boneIndex1 == index ? w.weight1 : 0) + (w.boneIndex2 == index ? w.weight2 : 0) + (w.boneIndex3 == index ? w.weight3 : 0);
                    if (fw > 0.5f) points.Add(toWorld.MultiplyPoint3x4(vertices[v]));
                }
                float bottom = points.Min(p => p.y);
                SoleBottomY = Mathf.Min(SoleBottomY, bottom);
                var sole = points.Where(p => p.y < bottom + 0.012f).ToList();
                Vector3 toe = sole.OrderByDescending(p => Vector3.Dot(p, fwd)).First();
                Vector3 heel = sole.OrderBy(p => Vector3.Dot(p, fwd)).First();
                SolePoints.Add((foot, Quaternion.Inverse(foot.rotation) * (toe - foot.position)));
                SolePoints.Add((foot, Quaternion.Inverse(foot.rotation) * (heel - foot.position)));
            }
            _skin.BakeMesh(_baked, true);
            BindMeshBottomY = LowestMesh();
            var baked = new GameObject("Baked");
            SceneManager.MoveGameObjectToScene(baked, Scene);
            _bakedFilter = baked.AddComponent<MeshFilter>();
            var materials = _skin.sharedMaterials;
            if (materials.Length == 0 || materials[0] == null)
                materials = new[] { new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.55f, 0.6f, 0.7f) } };
            baked.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(ground, Scene);
            ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ground.transform.localScale = Vector3.one * 8f;
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.93f, 0.86f, 0.62f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            var light = new GameObject("Light").AddComponent<Light>();
            SceneManager.MoveGameObjectToScene(light.gameObject, Scene);
            light.type = LightType.Directional; light.intensity = 1.3f; light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            Camera = new GameObject("Camera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(Camera.gameObject, Scene);
            Camera.enabled = false; Camera.scene = Scene; Camera.fieldOfView = 30f; Camera.nearClipPlane = 0.05f;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(0.78f, 0.86f, 0.9f);
            _rt = new RenderTexture(width, height, 24); Camera.targetTexture = _rt;
            _read = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        public void Sample(AnimationClip clip, float time)
        {
            clip.SampleAnimation(Animator.gameObject, time);
            _skin.BakeMesh(_baked, true);
            _bakedFilter.sharedMesh = _baked;
            _bakedFilter.transform.SetPositionAndRotation(_skin.transform.position, _skin.transform.rotation);
        }

        public Color[] Render(Vector3 offset, Vector3 lookAtLocal)
        {
            Vector3 target = Animator.transform.position + lookAtLocal;
            Camera.transform.position = target + offset;
            Camera.transform.LookAt(target);
            Camera.Render();
            RenderTexture.active = _rt;
            _read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _read.Apply();
            RenderTexture.active = null;
            return _read.GetPixels();
        }

        public void Dispose()
        {
            _rt.Release();
            Object.DestroyImmediate(_read);
            Object.DestroyImmediate(_baked);
            EditorSceneManager.ClosePreviewScene(Scene);
        }
    }

    static void SaveSheet(string file, List<Color[]> tiles, int columns, int w, int h)
    {
        int rows = Mathf.CeilToInt(tiles.Count / (float)columns);
        var sheet = new Texture2D(w * columns, h * rows, TextureFormat.RGB24, false);
        for (int i = 0; i < tiles.Count; i++)
            sheet.SetPixels(i % columns * w, (rows - 1 - i / columns) * h, w, h, tiles[i]);
        sheet.Apply();
        Directory.CreateDirectory(LabOutput);
        File.WriteAllBytes(Path.Combine(LabOutput, file), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
    }

    static AnimationClip ConstantClip(Dictionary<string, float> muscles, float rootY, float pitch)
    {
        var clip = new AnimationClip();
        foreach (var kv in muscles)
            clip.SetCurve("", typeof(Animator), kv.Key, AnimationCurve.Constant(0, 1, kv.Value));
        Quaternion q = Quaternion.Euler(pitch, 0, 0);
        clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Constant(0, 1, 0));
        clip.SetCurve("", typeof(Animator), "RootT.y", AnimationCurve.Constant(0, 1, rootY));
        clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Constant(0, 1, 0));
        clip.SetCurve("", typeof(Animator), "RootQ.x", AnimationCurve.Constant(0, 1, q.x));
        clip.SetCurve("", typeof(Animator), "RootQ.y", AnimationCurve.Constant(0, 1, q.y));
        clip.SetCurve("", typeof(Animator), "RootQ.z", AnimationCurve.Constant(0, 1, q.z));
        clip.SetCurve("", typeof(Animator), "RootQ.w", AnimationCurve.Constant(0, 1, q.w));
        return clip;
    }

    // 머슬 부호 확인: 각 열은 한 머슬, 행은 -1.5/-0.5/+0.5/+1.5. 측면(캐릭터는 +z를 향해 오른쪽을 본다).
    [MenuItem("Project PA/Art/Player Motion Style Lab/Calibrate Muscle Signs")]
    public static void CalibrateMuscleSigns()
    {
        string[] tests =
        {
            "Left Upper Leg Front-Back", "Left Lower Leg Stretch", "Left Foot Up-Down",
            "Left Arm Down-Up", "Left Arm Front-Back", "Left Forearm Stretch",
            "Spine Front-Back", "Chest Front-Back", "UpperChest Front-Back", "Head Nod Down-Up"
        };
        float[] values = { -1.5f, -0.5f, 0.5f, 1.5f };
        var tiles = new List<Color[]>();
        using var stage = new Stage(200, 260);
        foreach (float v in values)
            foreach (string muscle in tests)
            {
                var m = NeutralMuscles();
                m[muscle] = v;
                var clip = ConstantClip(m, 1f, 0f);
                stage.Sample(clip, 0.5f);
                tiles.Add(stage.Render(new Vector3(3.2f, 0.2f, 0), Vector3.up * 0.8f));
                Object.DestroyImmediate(clip);
            }
        SaveSheet("calibrate-muscle-signs.png", tiles, tests.Length, 200, 260);
        Debug.Log("[MotionStyleLab] columns=" + string.Join(" | ", tests) + " rows=" + string.Join(",", values));
    }

    // 모든 머슬 0에서 팔만 내린 기준 자세. 애니메이션 대상이 아닌 머슬은 0으로 둔다.
    static Dictionary<string, float> NeutralMuscles()
    {
        var m = new Dictionary<string, float>();
        foreach (string name in HumanTrait.MuscleName) m[name] = 0f;
        m["Left Arm Down-Up"] = -0.6f; m["Right Arm Down-Up"] = -0.6f;
        return m;
    }

    // ---------- 키 포즈 데이터 ----------
    // C-01 부호(보정 시트 확인): 허벅지 Front-Back -=앞, 무릎 Stretch -=굽힘, 팔 Front-Back -=앞, 팔 Down-Up +=위,
    // 전완 Stretch -=굽힘, Spine/Chest Front-Back +=앞숙임, Head Nod +=위.
    sealed class Track
    {
        public string Muscle;
        public float Shift;
        public float[] Phase, Value;
    }

    sealed class MotionDef
    {
        public string Name;
        public float Length;
        public bool Loop = true;
        // 지지 동작(대기·착지)은 무릎이 굽는 만큼 몸이 내려가야 하므로 프레임마다 가장 낮은 발을 접지한다. 달리기·공중은 전체 한 번만 맞춰 체공을 살린다.
        public bool GroundPerFrame;
        // 달리기 체공: 각 발의 지지 구간(0~StanceEnd, 0.5~0.5+StanceEnd) 밖에서 몸을 FlightLift(정규화 단위)만큼 사인형으로 띄운다.
        public float FlightLift, StanceEnd = 0.5f;
        public readonly Dictionary<string, float> Static = new Dictionary<string, float>();
        public readonly List<Track> Tracks = new List<Track>();
        public Track RootY, Pitch;
    }

    static Track Keys(string muscle, float shift, params float[] phaseValue)
    {
        int n = phaseValue.Length / 2;
        var t = new Track { Muscle = muscle, Shift = shift, Phase = new float[n], Value = new float[n] };
        for (int i = 0; i < n; i++) { t.Phase[i] = phaseValue[i * 2]; t.Value[i] = phaseValue[i * 2 + 1]; }
        return t;
    }

    // 왼쪽은 위상 그대로, 오른쪽은 반 주기 뒤. 팔은 같은 쪽 다리와 반대 위상을 주려면 armShift=0.5.
    static void Both(MotionDef d, string part, float leftShift, params float[] phaseValue)
    {
        d.Tracks.Add(Keys("Left " + part, leftShift, phaseValue));
        d.Tracks.Add(Keys("Right " + part, leftShift + 0.5f, phaseValue));
    }

    static void BothStatic(MotionDef d, string part, float value)
    {
        d.Static["Left " + part] = value;
        d.Static["Right " + part] = value;
    }

    // 딩컴 레퍼런스(공식 트레일러 5DKXglQAdNc 22.1~23.4s, 플레이 영상 rxWrM7h4LT4 168.9~170.8s):
    // 한 걸음 약 0.13s의 빠르고 짧은 보폭, 곧은 상체, 팔꿈치 약 90도로 주먹을 허리 앞에 둔 작은 스윙, 무릎 들기와 뒤꿈치 차올림.
    // sprint: Shift 속도용. 같은 스타일에서 걸음 빈도와 보폭만 키우고 몸을 조금 더 숙인다(같은 클립 고속 재생은 발이 미끄러진다).
    // 보정 리그에서 무릎 값 1.25≈폄, 0.85≈32도, 0.3≈76도, -0.2≈116도(측정). 지지 키는 "무릎 값 + 엉덩이 관절 대비 발 앞뒤 거리(m)"로 주고
    // 허벅지 값은 SolveThigh가 푼다. 지지발이 접지(+ahead)에서 밀기(-behind)까지 지지 시간 동안 움직인 거리 = 발이 미끄러지지 않는 이동 속도.
    static MotionDef RunDef(bool sprint = false)
    {
        var d = new MotionDef { Name = sprint ? "PA_Style_Sprint" : "PA_Style_Run", Length = sprint ? 0.24f : 0.28f, GroundPerFrame = true };
        // 지지 범위: 발 각도 보정 후 신발 바닥 접점 기준 실측(0.32/-0.33 → 5.41m/s, 0.37/-0.38 → 8.30m/s)을 게임 속도 4.2/6.72에 맞춰 비례 축소.
        // 2026-09-29 LegFix 리그·낮은 스윙 재작업 후 게임 실측에서 지지발이 이동 방향으로 약 0.9m/s 끌려(보폭 부족) 걸음 빈도를 유지한 채 지지 거리를 늘렸다
        // (기본 ×1.28, Shift ×1.15). 재생 배율을 올리는 방식은 다리가 더 빨라져 쓰지 않는다.
        // 사용자 피드백(기본 이동 반복이 너무 빠름): 이동 속도 4.2m/s는 유지하고 한 걸음을 약 30% 늘려 반복을 약 25% 늦춘다
        // (지지 거리 ×1.12, 지지 비율 0.44→0.38로 짧은 체공 추가). Shift는 그대로.
        float ahead = sprint ? 0.35f : 0.36f, behind = sprint ? -0.36f : -0.37f, stanceEnd = sprint ? 0.40f : 0.38f;
        d.StanceEnd = stanceEnd;
        d.FlightLift = sprint ? 0.035f : 0.018f;
        // 지지 구간: 등간격 5키에서 발 위치를 접지(+ahead)→밀기(behind)로 선형 이동시켜 지지발이 일정 속도로 뒤로 가게 한다(미끄러짐 방지).
        // 무릎은 접지 0.95 → 중간 0.65(약 50도) → 밀기 1.0, 발끝은 +8(뒤꿈치 접지) → 0(평평) → -35(발끝 밀기).
        // 이어서 0.58 뒤꿈치 차올림 → 0.76 무릎 들기 → 0.9 뻗기.
        // 2026-09-29 재작업(사용자 FAIL: 빠른 다리 구르기·높은 무릎·굳은 상체). 딩컴 플레이 rxWrM7h4LT4 169.35~169.8s 측면 60fps 실측:
        // 걸음 주기는 우리와 같은 약 0.12s지만 진폭이 작다(뒤꿈치는 정강이 중간 높이까지만, 앞다리는 낮게 앞으로 뻗음, 발끝 까치발 없음).
        // 그래서 지지 거리(=속도)는 유지하고 스윙 진폭·발끝 밀기를 줄였다.
        var ph = new List<float>(); var knee = new List<float>(); var thigh = new List<float>(); var toe = new List<float>();
        float[] stanceKnee = { 1.0f, 0.85f, 0.75f, 0.85f, 1.05f };
        float[] stanceToe = { 6f, 2f, 0f, -8f, -22f };
        for (int k = 0; k < 5; k++)
        {
            float s = k / 4f;
            ph.Add(stanceEnd * s);
            knee.Add(stanceKnee[k]);
            thigh.Add(SolveThigh(stanceKnee[k], Mathf.Lerp(ahead, behind, s)));
            toe.Add(stanceToe[k]);
        }
        // 스윙: 0.58 뒤꿈치가 정강이 중간까지(무릎 약 70도, Shift 약 85도) → 0.76 발이 낮게 앞으로 → 0.9 거의 편 채 접지 준비.
        ph.AddRange(new[] { 0.58f, 0.76f, 0.9f });
        // Shift는 1차(무릎 0.2/0.3)가 기본 이동과 거의 같아 보여, 뒤꿈치를 무릎 높이 근처까지(약 95도)·앞다리를 더 앞으로 뻗게 했다.
        knee.AddRange(new[] { sprint ? 0.0f : 0.35f, sprint ? 0.3f : 0.45f, sprint ? 0.8f : 0.85f });
        thigh.AddRange(new[] { sprint ? 0.55f : 0.4f, sprint ? -0.55f : -0.35f, sprint ? -0.42f : -0.28f });
        toe.AddRange(new[] { -25f, -8f, 4f });
        var thighKeys = new List<float>(); var kneeKeys = new List<float>(); var footKeys = new List<float>();
        for (int i = 0; i < ph.Count; i++)
        {
            thighKeys.Add(ph[i]); thighKeys.Add(thigh[i]);
            kneeKeys.Add(ph[i]); kneeKeys.Add(knee[i]);
            footKeys.Add(ph[i]); footKeys.Add(SolveFoot(thigh[i], knee[i], toe[i]));
        }
        Both(d, "Upper Leg Front-Back", 0f, thighKeys.ToArray());
        Both(d, "Lower Leg Stretch", 0f, kneeKeys.ToArray());
        Both(d, "Foot Up-Down", 0f, footKeys.ToArray());
        BothStatic(d, "Upper Leg In-Out", 0.05f);
        // 팔: 반대쪽 다리와 같은 위상. 이전의 "전완을 앞으로 수평"(좀비 팔)을 버리고, 몸통 옆으로 내린 팔을 팔꿈치 약 40도(Shift 약 60도)로
        // 가볍게 굽혀 앞뒤로 흔든다(1차 0.4/0.6은 손이 허리 앞으로 나와 0.65/0.85로 폄). 앞으로 갈 때 조금 더 굽힌다. 손바닥은 몸 쪽(비틀기 0).
        Both(d, "Arm Front-Back", 0.5f, 0f, sprint ? -0.35f : -0.25f, 0.5f, sprint ? 0.4f : 0.3f);
        Both(d, "Forearm Stretch", 0.5f, 0f, sprint ? 0.25f : 0.65f, 0.5f, sprint ? 0.5f : 0.85f);
        BothStatic(d, "Arm Down-Up", -0.62f);
        BothStatic(d, "Shoulder Down-Up", -0.15f);
        BothStatic(d, "Hand Down-Up", 0f);
        BothStatic(d, "Forearm Twist In-Out", 0f);
        // 상체: 살짝 앞, 걸음마다 지지발 쪽으로 가볍게 기울고 가슴은 다리와 반대로 비튼다. 머리는 반대로 조금 돌려 시선을 유지한다.
        d.Static["Spine Front-Back"] = sprint ? 0.16f : 0.06f;
        d.Static["Chest Front-Back"] = 0.02f;
        d.Static["UpperChest Front-Back"] = 0f;
        d.Static["Head Nod Down-Up"] = sprint ? 0.08f : 0.04f;
        d.Tracks.Add(Keys("Chest Twist Left-Right", 0f, 0f, 0.1f, 0.5f, -0.1f));
        d.Tracks.Add(Keys("Spine Left-Right", 0f, 0.1f, 0.035f, 0.35f, 0f, 0.6f, -0.035f, 0.85f, 0f));
        d.Tracks.Add(Keys("Head Turn Left-Right", 0f, 0f, -0.06f, 0.5f, 0.06f));
        d.Tracks.Add(Keys("Head Tilt Left-Right", 0f, 0.1f, -0.03f, 0.6f, 0.03f));
        // 골반 상하: 각 발의 중간 지지(0.125/0.625 부근)에서 낮고 밀어낸 뒤 높다.
        d.RootY = new Track
        {
            Muscle = "RootT.y",
            Phase = new[] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f },
            Value = new[] { 0.935f, 0.905f, 0.935f, 0.955f, 0.935f, 0.905f, 0.935f, 0.955f }
        };
        d.Pitch = Keys("Pitch", 0f, 0f, sprint ? 7f : 4f);
        return d;
    }

    static MotionDef IdleDef()
    {
        var d = new MotionDef { Name = "PA_Style_Idle", Length = 2.4f, GroundPerFrame = true };
        float idleThigh = SolveThigh(IdleKnee, 0f);
        BothStatic(d, "Upper Leg Front-Back", idleThigh);
        BothStatic(d, "Upper Leg In-Out", 0.1f);
        BothStatic(d, "Lower Leg Stretch", IdleKnee);
        BothStatic(d, "Foot Up-Down", SolveFoot(idleThigh, IdleKnee, 0f));
        Both(d, "Arm Down-Up", 0f, 0f, -0.58f, 0.5f, -0.55f);
        BothStatic(d, "Arm Front-Back", 0.05f);
        Both(d, "Forearm Stretch", 0f, 0f, 0.8f, 0.5f, 0.75f);
        BothStatic(d, "Shoulder Down-Up", -0.2f);
        BothStatic(d, "Hand Down-Up", 0f);
        d.Tracks.Add(Keys("Chest Front-Back", 0f, 0f, 0.0f, 0.5f, -0.04f));
        d.Static["Spine Front-Back"] = 0f;
        d.Static["UpperChest Front-Back"] = 0f;
        d.Static["Head Nod Down-Up"] = 0.05f;
        d.RootY = Keys("RootT.y", 0f, 0f, 0.95f, 0.5f, 0.945f);
        d.Pitch = Keys("Pitch", 0f, 0f, 0f);
        return d;
    }

    // 점프 3종: 달리기와 같은 팔꿈치 굽힌 팔·곧은 상체. 도약은 이륙 순간(다리 폄)에서 시작해 무릎을 모으고,
    // 공중은 다리를 조금씩 교차하며, 착지는 접지 순간 → 짧은 웅크림(무릎 약 60도) → 대기 자세로 돌아온다.
    static void JumpUpperBody(MotionDef d)
    {
        BothStatic(d, "Shoulder Down-Up", -0.15f);
        BothStatic(d, "Hand Down-Up", 0.2f);
        BothStatic(d, "Forearm Twist In-Out", -0.8f);
        BothStatic(d, "Upper Leg In-Out", 0.08f);
        d.Static["Chest Front-Back"] = 0f;
        d.Static["UpperChest Front-Back"] = 0f;
        d.Static["Head Nod Down-Up"] = 0.05f;
    }

    static MotionDef JumpStartDef()
    {
        var d = new MotionDef { Name = "PA_Style_JumpStart", Length = 0.5f, Loop = false };
        JumpUpperBody(d);
        float takeoff = SolveThigh(1.2f, -0.06f);
        float takeoffFoot = SolveFoot(takeoff, 1.2f, -40f);
        d.Tracks.Add(Keys("Left Upper Leg Front-Back", 0f, 0f, takeoff, 0.3f, -0.35f, 1f, -0.35f));
        d.Tracks.Add(Keys("Right Upper Leg Front-Back", 0f, 0f, takeoff, 0.3f, 0.05f, 1f, 0.05f));
        d.Tracks.Add(Keys("Left Lower Leg Stretch", 0f, 0f, 1.2f, 0.3f, -0.5f, 1f, -0.5f));
        d.Tracks.Add(Keys("Right Lower Leg Stretch", 0f, 0f, 1.2f, 0.3f, -0.2f, 1f, -0.2f));
        d.Tracks.Add(Keys("Left Foot Up-Down", 0f, 0f, takeoffFoot, 0.3f, SolveFoot(-0.35f, -0.5f, -20f), 1f, SolveFoot(-0.35f, -0.5f, -20f)));
        d.Tracks.Add(Keys("Right Foot Up-Down", 0f, 0f, takeoffFoot, 0.3f, SolveFoot(0.05f, -0.2f, -20f), 1f, SolveFoot(0.05f, -0.2f, -20f)));
        Both(d, "Arm Down-Up", 0f, 0f, -0.3f, 0.3f, -0.35f, 1f, -0.35f);
        Both(d, "Arm Front-Back", 0f, 0f, -0.35f, 0.3f, 0.0f, 1f, 0.0f);
        Both(d, "Forearm Stretch", 0f, 0f, 0.2f, 0.3f, 0.0f, 1f, 0.0f);
        d.Tracks.Add(Keys("Spine Front-Back", 0f, 0f, 0.0f, 0.3f, 0.08f, 1f, 0.08f));
        d.RootY = Keys("RootT.y", 0f, 0f, 0.95f);
        d.Pitch = Keys("Pitch", 0f, 0f, 2f, 1f, 0f);
        FixRightShift(d);
        return d;
    }

    static MotionDef AirborneDef()
    {
        var d = new MotionDef { Name = "PA_Style_Airborne", Length = 0.8f };
        JumpUpperBody(d);
        Both(d, "Upper Leg Front-Back", 0f, 0f, -0.2f, 0.5f, 0.1f);
        Both(d, "Lower Leg Stretch", 0f, 0f, -0.2f, 0.5f, 0.2f);
        Both(d, "Foot Up-Down", 0f, 0f, SolveFoot(-0.2f, -0.2f, -25f), 0.5f, SolveFoot(0.1f, 0.2f, -25f));
        Both(d, "Arm Down-Up", 0f, 0f, -0.3f, 0.5f, -0.25f);
        BothStatic(d, "Arm Front-Back", 0.05f);
        BothStatic(d, "Forearm Stretch", 0f);
        d.Static["Spine Front-Back"] = 0.05f;
        d.RootY = Keys("RootT.y", 0f, 0f, 0.95f);
        d.Pitch = Keys("Pitch", 0f, 0f, 2f);
        return d;
    }

    static MotionDef LandDef()
    {
        var d = new MotionDef { Name = "PA_Style_Land", Length = 0.45f, Loop = false, GroundPerFrame = true };
        JumpUpperBody(d);
        // 접지(무릎 약 18도, 발 조금 앞) → 웅크림(무릎 약 70도, 발은 엉덩이 아래) → 대기 자세. 팔 끝 값도 대기와 같다(-0.58, 전완 0.8).
        float contact = SolveThigh(1.0f, 0.04f), crouch = SolveThigh(0.35f, 0f), stand = SolveThigh(IdleKnee, 0f);
        Both(d, "Upper Leg Front-Back", 0f, 0f, contact, 0.3f, crouch, 1f, stand);
        Both(d, "Lower Leg Stretch", 0f, 0f, 1.0f, 0.3f, 0.35f, 1f, IdleKnee);
        Both(d, "Foot Up-Down", 0f, 0f, SolveFoot(contact, 1.0f, -5f), 0.3f, SolveFoot(crouch, 0.35f, 0f), 1f, SolveFoot(stand, IdleKnee, 0f));
        Both(d, "Arm Down-Up", 0f, 0f, -0.3f, 0.3f, -0.45f, 1f, -0.58f);
        Both(d, "Arm Front-Back", 0f, 0f, 0f, 0.3f, -0.1f, 1f, 0.05f);
        Both(d, "Forearm Stretch", 0f, 0f, 0f, 0.3f, 0.1f, 1f, 0.8f);
        d.Tracks.Add(Keys("Spine Front-Back", 0f, 0f, 0.1f, 0.3f, 0.25f, 1f, 0f));
        d.RootY = Keys("RootT.y", 0f, 0f, 0.95f);
        d.Pitch = Keys("Pitch", 0f, 0f, 0f, 0.3f, 4f, 1f, 0f);
        return d;
    }

    const float IdleKnee = 1.15f; // 약 7도
    static Stage _solverStage;

    // 무릎 값과 원하는 "엉덩이 관절 대비 발의 앞(+)/뒤(-) 거리(m, 런타임 1.75m 스케일)"에 맞는 허벅지 값을 이분법으로 찾는다.
    static float SolveThigh(float knee, float ahead)
    {
        _solverStage ??= new Stage(8, 8);
        var animator = _solverStage.Animator;
        var hip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        float Ahead(float thigh)
        {
            var m = NeutralMuscles();
            m["Left Upper Leg Front-Back"] = thigh;
            m["Left Lower Leg Stretch"] = knee;
            var clip = ConstantClip(m, 1f, 0f);
            clip.SampleAnimation(animator.gameObject, 0.5f);
            Object.DestroyImmediate(clip);
            return Vector3.Dot(foot.position - hip.position, animator.transform.forward);
        }
        float lo = -0.6f, hi = 1.5f; // 허벅지 값이 작을수록 발이 앞(단조 구간)
        for (int i = 0; i < 24; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (Ahead(mid) > ahead) lo = mid; else hi = mid;
        }
        return (lo + hi) * 0.5f;
    }

    // 허벅지·무릎이 주어졌을 때 발끝 기울기(도, +는 발끝 위, 0은 바닥과 평행)가 목표가 되는 Foot Up-Down 값.
    static float SolveFoot(float thigh, float knee, float pitch)
    {
        _solverStage ??= new Stage(8, 8);
        var animator = _solverStage.Animator;
        var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        float Pitch(float value)
        {
            var m = NeutralMuscles();
            m["Left Upper Leg Front-Back"] = thigh;
            m["Left Lower Leg Stretch"] = knee;
            m["Left Foot Up-Down"] = value;
            var clip = ConstantClip(m, 1f, 0f);
            clip.SampleAnimation(animator.gameObject, 0.5f);
            Object.DestroyImmediate(clip);
            return _solverStage.ToePitch(foot);
        }
        float lo = -1.5f, hi = 1.5f;
        bool increasing = Pitch(hi) > Pitch(lo);
        for (int i = 0; i < 24; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if ((Pitch(mid) < pitch) == increasing) lo = mid; else hi = mid;
        }
        return (lo + hi) * 0.5f;
    }

    // 비반복 클립에서 Both()가 준 오른쪽 0.5 위상 이동을 없앤다(좌우 같은 시점).
    static void FixRightShift(MotionDef d)
    {
        foreach (var t in d.Tracks) t.Shift = 0f;
    }

    // 주기형 Catmull-Rom. 키 위상은 [0,1) 오름차순.
    // 비반복 클립(도약·착지)용 Catmull-Rom. 키 위상은 0과 1을 포함한다.
    static float Clamped(Track t, float p)
    {
        int n = t.Phase.Length;
        if (n == 1) return t.Value[0];
        p = Mathf.Clamp01(p);
        int i = 0;
        while (i < n - 2 && p > t.Phase[i + 1]) i++;
        float s = Mathf.InverseLerp(t.Phase[i], t.Phase[i + 1], p);
        float v0 = t.Value[Mathf.Max(i - 1, 0)], v1 = t.Value[i], v2 = t.Value[i + 1], v3 = t.Value[Mathf.Min(i + 2, n - 1)];
        return 0.5f * (2f * v1 + (-v0 + v2) * s + (2f * v0 - 5f * v1 + 4f * v2 - v3) * s * s + (-v0 + 3f * v1 - 3f * v2 + v3) * s * s * s);
    }

    static float Evaluate(MotionDef d, Track t, float p) => d.Loop ? Periodic(t, p) : Clamped(t, p);

    static float Periodic(Track t, float p)
    {
        int n = t.Phase.Length;
        if (n == 1) return t.Value[0];
        p = Mathf.Repeat(p - t.Shift, 1f);
        int i = n - 1;
        for (int k = 0; k < n; k++) if (t.Phase[k] > p) { i = k - 1; break; }
        if (i < 0) i = n - 1;
        int j = (i + 1) % n;
        float p0 = t.Phase[i], p1 = t.Phase[j];
        if (p1 <= p0) p1 += 1f;
        float pp = p < p0 ? p + 1f : p;
        float s = (pp - p0) / (p1 - p0);
        float v0 = t.Value[(i - 1 + n) % n], v1 = t.Value[i], v2 = t.Value[j], v3 = t.Value[(j + 1) % n];
        return 0.5f * (2f * v1 + (-v0 + v2) * s + (2f * v0 - 5f * v1 + 4f * v2 - v3) * s * s + (-v0 + 3f * v1 - 3f * v2 + v3) * s * s * s);
    }

    static AnimationCurve Sampled(float length, Func<float, float> f)
    {
        const int Fps = 60;
        int count = Mathf.Max(2, Mathf.RoundToInt(length * Fps));
        var keys = new Keyframe[count + 1];
        float dt = length / count, h = 0.25f / count;
        for (int k = 0; k <= count; k++)
        {
            float time = k * dt, p = time / length;
            float slope = (f(p + h) - f(p - h)) / (2f * h * length);
            keys[k] = new Keyframe(time, f(p), slope, slope);
        }
        return new AnimationCurve(keys);
    }

    static AnimationClip BuildClip(MotionDef d)
    {
        var clip = new AnimationClip { name = d.Name, frameRate = 60 };
        var animated = new HashSet<string>(d.Tracks.Select(t => t.Muscle));
        foreach (string muscle in HumanTrait.MuscleName)
        {
            if (animated.Contains(muscle)) continue;
            float v = d.Static.TryGetValue(muscle, out float s) ? s : 0f;
            clip.SetCurve("", typeof(Animator), muscle, AnimationCurve.Constant(0, d.Length, v));
        }
        foreach (var t in d.Tracks)
        {
            var track = t;
            clip.SetCurve("", typeof(Animator), track.Muscle, Sampled(d.Length, p => Evaluate(d, track, p)));
        }
        clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Constant(0, d.Length, 0));
        clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Constant(0, d.Length, 0));
        clip.SetCurve("", typeof(Animator), "RootT.y", Sampled(d.Length, p => Evaluate(d, d.RootY, p)));
        Func<float, Quaternion> q = p => Quaternion.Euler(Evaluate(d, d.Pitch, p), 0, 0);
        clip.SetCurve("", typeof(Animator), "RootQ.x", Sampled(d.Length, p => q(p).x));
        clip.SetCurve("", typeof(Animator), "RootQ.y", Sampled(d.Length, p => q(p).y));
        clip.SetCurve("", typeof(Animator), "RootQ.z", Sampled(d.Length, p => q(p).z));
        clip.SetCurve("", typeof(Animator), "RootQ.w", Sampled(d.Length, p => q(p).w));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        // 회전·Y·XZ를 포즈에 굽힌다. 생성한 .anim의 Feet 높이 기준은 런타임에서 FBX 임포트처럼 보정되지 않아(몸이 약 0.6m 가라앉음)
        // Original 기준을 쓰고 RootT.y를 GroundClip에서 실측으로 맞춘다.
        settings.loopTime = d.Loop;
        settings.loopBlendOrientation = true; settings.keepOriginalOrientation = false;
        settings.loopBlendPositionY = true; settings.keepOriginalPositionY = true; settings.heightFromFeet = false;
        settings.loopBlendPositionXZ = true; settings.keepOriginalPositionXZ = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        GroundClip(clip, d);
        return clip;
    }

    // 주기 중 가장 낮은 발 뼈 높이를 기존 UAL Idle(FBX Feet 기준, 발바닥 접지 확인됨)의 발 뼈 높이에 맞춘다.
    static void GroundClip(AnimationClip clip, MotionDef d)
    {
        bool perFrame = d.GroundPerFrame;
        using var stage = new Stage(16, 16);
        var feet = new[] { stage.Animator.GetBoneTransform(HumanBodyBones.LeftFoot), stage.Animator.GetBoneTransform(HumanBodyBones.RightFoot) };
        float target = stage.BindMeshBottomY;
        float perUnit = stage.Animator.humanScale * stage.Animator.transform.lossyScale.y;
        var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        var keys = curve.keys;
        float Lowest(float time) { stage.Sample(clip, time); return stage.LowestMesh(); }
        if (perFrame)
        {
            // RootT.y 변화와 세계 높이 변화의 배율이 정확히 1이 아니어서(1회 보정 후에도 최대 2.6cm 남음) 곡선을 적용·재측정하며 3번 맞춘다.
            for (int pass = 0; pass < 3; pass++)
            {
                var shifts = keys.Select(k => (target - Lowest(k.time)) / perUnit).ToArray();
                for (int i = 0; i < keys.Length; i++) keys[i].value += shifts[i];
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            for (int i = 0; i < keys.Length; i++)
            {
                float q = Mathf.Repeat(keys[i].time / clip.length, 0.5f);
                if (d.FlightLift > 0f && q > d.StanceEnd)
                    keys[i].value += d.FlightLift * Mathf.Sin(Mathf.PI * (q - d.StanceEnd) / (0.5f - d.StanceEnd));
            }
            for (int i = 0; i < keys.Length; i++)
            {
                int a = Mathf.Max(i - 1, 0), b = Mathf.Min(i + 1, keys.Length - 1);
                float slope = b == a ? 0f : (keys[b].value - keys[a].value) / (keys[b].time - keys[a].time);
                keys[i].inTangent = keys[i].outTangent = slope;
            }
        }
        else
        {
            float lowest = float.MaxValue;
            for (int s = 0; s < 60; s++) lowest = Mathf.Min(lowest, Lowest(clip.length * s / 60f));
            float shift = (target - lowest) / perUnit;
            for (int i = 0; i < keys.Length; i++) keys[i].value += shift;
        }
        curve.keys = keys;
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    static AnimationClip SaveClip(AnimationClip clip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Animation")) AssetDatabase.CreateFolder("Assets/Art", "Animation");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art/Animation", "PlayerMotionStyle");
        string path = Folder + "/" + clip.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing);
        AssetDatabase.SaveAssetIfDirty(existing);
        Object.DestroyImmediate(clip);
        return existing;
    }

    public const string SampleControllerPath = Folder + "/PlayerLocomotion_StyleSample.controller";
    const string ProductControllerPath = "Assets/Resources/PlayerLocomotion/PlayerLocomotion.controller";

    [MenuItem("Project PA/Art/Player Motion Style Lab/Build Style Clips")]
    public static void BuildStyleClips()
    {
        try { BuildStyleClipsCore(); }
        finally { _solverStage?.Dispose(); _solverStage = null; }
    }

    static void BuildStyleClipsCore()
    {
        var idle = SaveClip(BuildClip(IdleDef()));
        var run = SaveClip(BuildClip(RunDef()));
        var sprint = SaveClip(BuildClip(RunDef(true)));
        var jumpStart = SaveClip(BuildClip(JumpStartDef()));
        var airborne = SaveClip(BuildClip(AirborneDef()));
        var land = SaveClip(BuildClip(LandDef()));
        foreach (var clip in new[] { idle, run, sprint, jumpStart, airborne, land }) AssetDatabase.SaveAssetIfDirty(clip);
        Debug.Log("[MotionStyleLab] runNaturalSpeed=" + MeasureStanceSpeed(run).ToString("F2") +
                  " sprintNaturalSpeed=" + MeasureStanceSpeed(sprint).ToString("F2") + "m/s");
    }

    // 제품 컨트롤러(Resources/PlayerLocomotion)에 스타일 클립을 연결한다. 상태·전이 조건은 유지하고
    // 도약 클립이 이륙 순간에서 시작하므로 Any→JumpStart 오프셋을 0, 착지 재생 속도를 1로 둔다.
    [MenuItem("Project PA/Art/Player Motion Style Lab/Apply Style Clips To Product Controller")]
    public static void ApplyToProductController()
    {
        AnimationClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + n + ".anim");
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ProductControllerPath);
        var machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState State(string n) => machine.states.First(s => s.state.name == n).state;
        var tree = (UnityEditor.Animations.BlendTree)State("Locomotion").motion;
        var children = tree.children;
        children[0].motion = Clip("PA_Style_Idle");
        children[1].motion = Clip("PA_Style_Run");
        children[2].motion = Clip("PA_Style_Run");
        children[3].motion = Clip("PA_Style_Sprint");
        tree.children = children;
        State("JumpStart").motion = Clip("PA_Style_JumpStart");
        State("Airborne").motion = Clip("PA_Style_Airborne");
        State("Land").motion = Clip("PA_Style_Land");
        State("Land").speed = 1f;
        foreach (var t in machine.anyStateTransitions)
            if (t.destinationState != null && t.destinationState.name == "JumpStart") t.offset = 0f;
        // 지지발 고정(PlayerFootLock)은 LateUpdate 두 뼈 IK라 Animator IK Pass가 필요 없다.
        var layers = controller.layers;
        layers[0].iKPass = false;
        controller.layers = layers;
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("[MotionStyleLab] product controller now uses " + string.Join(",", tree.children.Select(c => c.motion.name)) + " + style jump");
    }

    // 제품 컨트롤러 사본에서 Locomotion 블렌드의 모션만 스타일 클립으로 바꾼다. 점프 상태·전이는 그대로(점프는 샘플 확인 후 작업).
    [MenuItem("Project PA/Art/Player Motion Style Lab/Build Sample Controller")]
    public static void BuildSampleController()
    {
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/PA_Style_Idle.anim");
        var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/PA_Style_Run.anim");
        var sprint = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/PA_Style_Sprint.anim");
        if (idle == null || run == null || sprint == null) { Debug.LogError("[MotionStyleLab] build clips first"); return; }
        // AssetDatabase.CopyAsset은 이 에디터에서 전역 저장을 일으켜(Play 중 채워진 TMP 폰트까지 저장) 쓰지 않는다.
        if (AssetDatabase.LoadAssetAtPath<Object>(SampleControllerPath) == null)
        {
            File.Copy(ProductControllerPath, SampleControllerPath);
            AssetDatabase.ImportAsset(SampleControllerPath);
        }
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(SampleControllerPath);
        var locomotion = controller.layers[0].stateMachine.states.First(s => s.state.name == "Locomotion").state;
        var tree = (UnityEditor.Animations.BlendTree)locomotion.motion;
        var children = tree.children;
        // 임계값(0 / 0.78 / 4.29 / 6.6)은 PlayerLocomotionAnimator의 속도 구간과 같다.
        children[0].motion = idle;
        children[1].motion = run;
        children[2].motion = run;
        children[3].motion = sprint;
        tree.children = children;
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("[MotionStyleLab] sample controller " + SampleControllerPath + " thresholds=" +
                  string.Join(",", tree.children.Select(c => c.threshold + ":" + c.motion.name)));
    }

    // 지지발(낮은 발)의 뒤로 가는 속도 중앙값 = 발이 미끄러지지 않는 이동 속도(런타임 1.75m 스케일).
    public static float MeasureStanceSpeed(AnimationClip clip)
    {
        // 굴러가는 발은 발목이 아니라 지면에 닿은 신발 바닥 점이 멈춰야 한다. 각 바닥 점이 지면(바인드 바닥 +5mm) 안에 있는 동안의 뒤쪽 속도 중앙값.
        using var stage = new Stage(16, 16);
        const int Steps = 120;
        int count = stage.SolePoints.Count;
        var y = new float[count, Steps + 1];
        var z = new float[count, Steps + 1];
        for (int s = 0; s <= Steps; s++)
        {
            stage.Sample(clip, clip.length * s / Steps);
            for (int i = 0; i < count; i++)
            {
                var (foot, local) = stage.SolePoints[i];
                Vector3 p = foot.position + foot.rotation * local;
                y[i, s] = p.y; z[i, s] = p.z;
            }
        }
        var speeds = new List<float>();
        float dt = clip.length / Steps;
        for (int i = 0; i < count; i++)
            for (int s = 1; s <= Steps; s++)
                if (y[i, s] < stage.SoleBottomY + 0.005f && y[i, s - 1] < stage.SoleBottomY + 0.005f) speeds.Add(-(z[i, s] - z[i, s - 1]) / dt);
        speeds.Sort();
        return speeds.Count == 0 ? 0f : speeds[speeds.Count / 2];
    }

    // 제자리 재생 프레임열(측면/3/4 정면 나란히). ffmpeg로 영상화해 리듬·상하 움직임을 판정한다.
    public static void RenderLoopFrames(string clipName, float seconds, int fps, float playback)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + clipName + ".anim");
        string dir = Path.Combine(LabOutput, "frames_" + clipName);
        Directory.CreateDirectory(dir);
        foreach (string old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
        using var stage = new Stage(360, 420);
        var pair = new Texture2D(720, 420, TextureFormat.RGB24, false);
        int count = Mathf.RoundToInt(seconds * fps);
        for (int i = 0; i < count; i++)
        {
            stage.Sample(clip, Mathf.Repeat(i / (float)fps * playback, clip.length));
            pair.SetPixels(0, 0, 360, 420, stage.Render(new Vector3(2.4f, 0.15f, 0), Vector3.up * 0.62f));
            pair.SetPixels(360, 0, 360, 420, stage.Render(new Vector3(1.4f, 0.35f, 1.9f), Vector3.up * 0.62f));
            pair.Apply();
            File.WriteAllBytes(Path.Combine(dir, i.ToString("0000") + ".png"), pair.EncodeToPNG());
        }
        Object.DestroyImmediate(pair);
    }

    // 리그 비교: 같은 클립을 원본 Mannequin(기준 캐릭터), C-01 원본 리그, C-01 다리 보정 리그에 같은 위상으로 재생한다(측면 3행, 정면 3행).
    public static void RenderRigComparison(string clipName, string fbxPath, int phases = 8)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().First(c => c.name == clipName);
        var mannequin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/External/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx");
        var tiles = new List<Color[]>();
        Vector3[] views = { new Vector3(2.7f, 0.2f, 0), new Vector3(0, 0.2f, 2.7f) };
        foreach (var view in views)
            foreach (var (prefab, fix) in new (GameObject, bool)[] { (mannequin, false), (null, false), (null, true) })
            {
                using var stage = new Stage(220, 280, 1.75f, prefab, fix);
                for (int i = 0; i < phases; i++)
                {
                    stage.Sample(clip, clip.length * i / phases);
                    tiles.Add(stage.Render(view, Vector3.up * 0.62f));
                }
            }
        SaveSheet("rig-compare-" + clipName + ".png", tiles, phases, 220, 280);
    }

    // 검토 시트: 측면 8위상 / 정면 8위상 / 16m 카메라 각도(확대) 8위상.
    [MenuItem("Project PA/Art/Player Motion Style Lab/Render Review Sheets")]
    public static void RenderReviewSheets()
    {
        foreach (string path in new[] { "PA_Style_Run", "PA_Style_Sprint", "PA_Style_Idle", "PA_Style_JumpStart", "PA_Style_Airborne", "PA_Style_Land" }.Select(n => Folder + "/" + n + ".anim"))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { Debug.LogError("[MotionStyleLab] missing " + path); continue; }
            var tiles = new List<Color[]>();
            using var stage = new Stage(220, 280);
            Vector3 gameOffset = new Vector3(0, 13.61f, -11.31f).normalized;
            Vector3[] views = { new Vector3(2.7f, 0.2f, 0), new Vector3(0, 0.2f, 2.7f), gameOffset * 3.4f };
            foreach (var view in views)
                for (int i = 0; i < 8; i++)
                {
                    stage.Sample(clip, clip.length * i / 8f);
                    tiles.Add(stage.Render(view, Vector3.up * 0.62f));
                }
            SaveSheet(clip.name + "-review.png", tiles, 8, 220, 280);
        }
    }
}
