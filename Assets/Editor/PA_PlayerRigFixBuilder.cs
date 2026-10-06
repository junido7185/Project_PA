using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// C-01 다리 리그 보정 데이터 생성. 원본 C-01.fbx와 임포트 설정은 바꾸지 않는다.
// 측정(2026-09-29): 휴머노이드 무릎 L_Calf가 원시 높이 0.077(엉덩이 0.374, 발 0.042), 정강이 메시(0.09~0.21)는 L_ThighTwist02를 따른다.
// 보정: 무릎을 엉덩이-발 중간으로 옮기고, 무릎 아래 허벅지 트위스트 가중치를 정강이(L_CalfTwist01, L_Calf 자식)로 옮긴 뒤 아바타를 다시 만든다.
public static class PA_PlayerRigFixBuilder
{
    const string AssetPath = "Assets/Resources/PlayerLocomotion/C01_RigFix.asset";
    const float KneeBlend = 0.02f; // 원시 단위(런타임 약 3cm)

    [MenuItem("Project PA/Art/Player Motion Style Lab/Build C-01 Leg Rig Fix")]
    public static void Build()
    {
        var prefab = FirstDayStudioAssets.Load().player;
        string fbxPath = AssetDatabase.GetAssetPath(prefab);
        var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var model = (GameObject)Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(model, scene);
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            var byName = model.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            var source = skin.sharedMesh;

            float bindError = 0f;
            for (int i = 0; i < skin.bones.Length; i++)
            {
                Matrix4x4 expected = skin.bones[i].worldToLocalMatrix * skin.transform.localToWorldMatrix;
                for (int e = 0; e < 16; e++) bindError = Mathf.Max(bindError, Mathf.Abs(expected[e] - source.bindposes[i][e]));
            }
            if (bindError > 1e-3f) { Debug.LogError("[RigFix] prefab default pose is not the bind pose: " + bindError); return; }

            var toWorld = skin.transform.localToWorldMatrix;
            var vertices = source.vertices;
            var weights = source.boneWeights;
            var boneIndex = new Dictionary<string, int>();
            for (int i = 0; i < skin.bones.Length; i++) boneIndex[skin.bones[i].name] = i;
            var changed = new List<string>();
            var report = new List<string>();

            foreach (string side in new[] { "L_", "R_" })
            {
                Transform thigh = byName[side + "Thigh"], calf = byName[side + "Calf"], foot = byName[side + "Foot"];
                Vector3 knee = Vector3.Lerp(foot.position, thigh.position, 0.5f);
                // 자식은 세계 위치를 유지하고 무릎(L_Calf)만 옮긴다.
                var children = new List<(Transform t, Vector3 p, Quaternion r)>();
                foreach (Transform c in calf) children.Add((c, c.position, c.rotation));
                report.Add(side + "Calf y " + calf.position.y.ToString("F3") + " -> " + knee.y.ToString("F3"));
                calf.position = knee;
                foreach (var c in children) c.t.SetPositionAndRotation(c.p, c.r);
                changed.Add(calf.name);
                changed.AddRange(children.Select(c => c.t.name));

                int shin = boneIndex[side + "CalfTwist01"];
                var thighBones = new HashSet<int> { boneIndex[side + "ThighTwist01"], boneIndex[side + "ThighTwist02"] };
                int moved = 0;
                for (int v = 0; v < vertices.Length; v++)
                {
                    float y = toWorld.MultiplyPoint3x4(vertices[v]).y;
                    float t = Mathf.InverseLerp(knee.y - KneeBlend, knee.y + KneeBlend, y);
                    float keep = t * t * (3f - 2f * t);
                    if (keep >= 1f) continue;
                    var influences = Influences(weights[v]);
                    float transfer = 0f;
                    foreach (int b in influences.Keys.ToList())
                        if (thighBones.Contains(b)) { transfer += influences[b] * (1f - keep); influences[b] *= keep; }
                    if (transfer <= 0f) continue;
                    influences[shin] = (influences.TryGetValue(shin, out float w) ? w : 0f) + transfer;
                    weights[v] = Pack(influences);
                    moved++;
                }
                report.Add(side + "vertices moved to shin=" + moved);
            }

            var mesh = Object.Instantiate(source);
            mesh.name = source.name + "_LegFix";
            mesh.boneWeights = weights;

            var human = importer.humanDescription;
            var skeleton = human.skeleton;
            for (int i = 0; i < skeleton.Length; i++)
                if (changed.Contains(skeleton[i].name)) skeleton[i].position = byName[skeleton[i].name].localPosition;
            human.skeleton = skeleton;
            var avatar = AvatarBuilder.BuildHumanAvatar(model, human);
            avatar.name = "C-01Avatar_LegFix";
            if (!avatar.isValid || !avatar.isHuman) { Debug.LogError("[RigFix] avatar invalid"); return; }

            var fix = AssetDatabase.LoadAssetAtPath<PlayerRigFix>(AssetPath);
            if (fix == null)
            {
                fix = ScriptableObject.CreateInstance<PlayerRigFix>();
                AssetDatabase.CreateAsset(fix, AssetPath);
            }
            else
            {
                foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath)) AssetDatabase.RemoveObjectFromAsset(sub);
            }
            AssetDatabase.AddObjectToAsset(mesh, fix);
            AssetDatabase.AddObjectToAsset(avatar, fix);
            fix.sourceAvatar = model.GetComponentInChildren<Animator>().avatar.name;
            fix.mesh = mesh;
            fix.avatar = avatar;
            var toModel = model.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            fix.soleLocalY = source.vertices.Min(v => toModel.MultiplyPoint3x4(v).y);
            // 발 뼈 가중치가 큰 정점 중 바닥 1.2cm 띠의 앞·뒤 끝 = 발끝·뒤꿈치. 발 뼈 로컬(바인드포즈)로 저장.
            Vector3 forward = model.transform.forward;
            (Vector3 heel, Vector3 toe) Sole(string footName)
            {
                int index = boneIndex[footName];
                var points = Enumerable.Range(0, vertices.Length).Where(v => Influences(source.boneWeights[v]).TryGetValue(index, out float w) && w > 0.5f).ToList();
                float bottom = points.Min(v => toWorld.MultiplyPoint3x4(vertices[v]).y);
                var sole = points.Where(v => toWorld.MultiplyPoint3x4(vertices[v]).y < bottom + 0.012f).ToList();
                int toeVertex = sole.OrderByDescending(v => Vector3.Dot(toWorld.MultiplyPoint3x4(vertices[v]), forward)).First();
                int heelVertex = sole.OrderBy(v => Vector3.Dot(toWorld.MultiplyPoint3x4(vertices[v]), forward)).First();
                var bind = source.bindposes[index];
                return (bind.MultiplyPoint3x4(vertices[heelVertex]), bind.MultiplyPoint3x4(vertices[toeVertex]));
            }
            (fix.leftHeel, fix.leftToe) = Sole("L_Foot");
            (fix.rightHeel, fix.rightToe) = Sole("R_Foot");
            fix.bones = changed.ToArray();
            fix.localPositions = changed.Select(n => byName[n].localPosition).ToArray();
            EditorUtility.SetDirty(fix);
            AssetDatabase.SaveAssetIfDirty(fix);
            Debug.Log("[RigFix] " + string.Join("; ", report) + " bones=" + string.Join(",", changed));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Dictionary<int, float> Influences(BoneWeight w)
    {
        var d = new Dictionary<int, float>();
        void Add(int i, float x) { if (x > 0f) d[i] = (d.TryGetValue(i, out float o) ? o : 0f) + x; }
        Add(w.boneIndex0, w.weight0); Add(w.boneIndex1, w.weight1); Add(w.boneIndex2, w.weight2); Add(w.boneIndex3, w.weight3);
        return d;
    }

    static BoneWeight Pack(Dictionary<int, float> d)
    {
        var top = d.Where(kv => kv.Value > 1e-4f).OrderByDescending(kv => kv.Value).Take(4).ToArray();
        float sum = top.Sum(kv => kv.Value);
        var w = new BoneWeight();
        if (top.Length > 0) { w.boneIndex0 = top[0].Key; w.weight0 = top[0].Value / sum; }
        if (top.Length > 1) { w.boneIndex1 = top[1].Key; w.weight1 = top[1].Value / sum; }
        if (top.Length > 2) { w.boneIndex2 = top[2].Key; w.weight2 = top[2].Value / sum; }
        if (top.Length > 3) { w.boneIndex3 = top[3].Key; w.weight3 = top[3].Value / sum; }
        return w;
    }
}
