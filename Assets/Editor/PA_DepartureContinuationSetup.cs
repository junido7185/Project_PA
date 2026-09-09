using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Only creates the continuation configuration. Never rebuilds the P0 scene or assets.
public static class PA_DepartureContinuationSetup
{
    public const string PrefabPath = "Assets/Resources/DepartureTutorial/DepartureContinuation.prefab";

    [MenuItem("Project PA/Presentation/Configure Departure Voyage")]
    public static void ConfigureVoyage()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var voyage = root.GetComponent<DepartureVoyagePresentation>() ?? root.AddComponent<DepartureVoyagePresentation>();
            const string intake = "Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_";
            voyage.boatPrefab = Require<GameObject>("Assets/Resources/DepartureTutorial/Visuals/PA_DepartureBoat.prefab");
            voyage.treePrefab = Require<GameObject>(intake + "ULTIMATENATURE_COMMONTREE_1.prefab");
            voyage.birchPrefab = Require<GameObject>(intake + "ULTIMATENATURE_BIRCHTREE_1.prefab");
            voyage.rockPrefab = Require<GameObject>(intake + "ULTIMATENATURE_ROCK_1.prefab");
            voyage.dockPrefab = Require<GameObject>(intake + "CUTEFISH_DOCK_LONG_NOROPE.prefab");
            voyage.cratePrefab = Require<GameObject>(intake + "CUBEWORLDKIT_CHEST_CLOSED.prefab");
            voyage.waterMaterial = Require<Material>("Assets/Resources/DepartureTutorial/Materials/PA_WaterLight.mat");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        Debug.Log("[VS-P2] CONFIGURED existing boat, nature and grid presentation.");
    }

    [MenuItem("Project PA/Presentation/Configure Departure Continuation")]
    public static void Configure()
    {
        var go = new GameObject("DepartureContinuation");
        try
        {
            var selection = go.AddComponent<DepartureCompanionSelection>();
            selection.font = Require<TMP_FontAsset>("Assets/Fonts/Jalnan2_SDF.asset");
            // Names and descriptions remain explicitly TEMP proposals from CONTENT_CANON_BIBLE §6.3–6.5.
            // MBTI comes from the existing role profile; zero axes remain unknown.
            selection.candidates = new[]
            {
                Make("Lumberjack", "아를로 · TEMP", "벌목꾼", "목재", "원목 계열", "목수", "말을 아끼며 곁에서 돕는 동료", "C-03", new Color(.49f,.59f,.31f)),
                Make("Miner", "로건 · TEMP", "광부", "채광", "돌 / 기초 광석", "대장장이", "신중하게 확인하고 약속을 지킴", "C-04", new Color(.46f,.54f,.59f)),
                Make("Farmer", "미라 · TEMP", "농부", "농업", "기초 작물", "요리사 / 가공업자", "내일 필요한 양을 미리 살핌", "C-02", new Color(.83f,.59f,.24f))
            };
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        Debug.Log("[VS-P1] CONFIGURED existing profiles and character models.");
    }

    static DepartureCompanionSelection.Candidate Make(string key, string name, string job, string field, string goods,
        string future, string personality, string model, Color accent) => new DepartureCompanionSelection.Candidate
    {
        id = key + "_01", displayName = name, profession = job, production = field,
        firstGoods = goods, future = future, personality = personality, accent = accent,
        profile = Require<NpcProfile>("Assets/Resources/NPCs/Profile_" + key + ".asset"),
        model = Require<GameObject>("Assets/Art/Character/" + model + ".fbx")
    };

    public static T Require<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing continuation asset: " + path);

    [MenuItem("Project PA/Presentation/Play Companion Selection (Development Entry)")]
    public static void PlaySelection()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("PA.Companions.DevelopmentEntry", true);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        EditorApplication.EnterPlaymode();
    }
}
