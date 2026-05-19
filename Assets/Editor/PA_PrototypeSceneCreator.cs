#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 🎬 PA_PrototypeSceneCreator — 시연용 별도 씬 생성기
//
// 설계 의도:
// - 사용자의 작업 씬 `Assets/Scenes/MainGame.unity` 는 절대 건드리지 않는다.
// - "Project P.A. — 첫 번째 개척 임무" 시나리오를 시연할 수 있는 깨끗한 씬을
//   `Assets/Scenes/Prototype_FirstDay.unity` 로 따로 만든다.
// - PlayableDayBuilder 의 안전 빌드 경로 (`BuildSafeFromHub`) 와 달리 이 도구는
//   "신규 씬에서만" 동작하므로 SceneAutoBuilder / MapLayoutBuilder / UIBuilder 를
//   모두 풀로 호출해 처음부터 깔끔하게 구성한다.
// - 매번 같은 결과가 나오게 멱등성 유지 — Rebuild 시 기존 파일을 덮어쓰지만
//   현재 열려 있는 작업 씬은 저장 후 분리한다.
//
// 메뉴:
//   P.A. System / Week 12 / 🎬 시연 씬 / 새로 만들기 (Prototype_FirstDay.unity)
//   P.A. System / Week 12 / 🎬 시연 씬 / 열기
//   P.A. System / Week 12 / 🎬 시연 씬 / 재구축
public static class PA_PrototypeSceneCreator
{
    public const string SCENE_PATH = "Assets/Scenes/Prototype_FirstDay.unity";
    public const string SCENE_NAME = "Prototype_FirstDay";
    public const string MAIN_SCENE_PATH = "Assets/Scenes/MainGame.unity"; // 안전 가드

    // ── 메뉴 ────────────────────────────────────────────────────────────
    [MenuItem("P.A. System/Week 12/🎬 시연 씬/새로 만들기 (Prototype_FirstDay.unity)", priority = -3)]
    public static void CreateNew()
    {
        if (File.Exists(SCENE_PATH))
        {
            if (!EditorUtility.DisplayDialog(
                    "시연 씬 이미 존재",
                    $"{SCENE_PATH} 가 이미 있습니다.\n덮어쓰고 새로 만들까요?\n\n" +
                    "기존 시연 씬의 수동 편집은 사라집니다.\n작업 씬(MainGame.unity)은 영향 없음.",
                    "덮어쓰기", "취소")) return;
        }
        CreateOrRebuild();
    }

    [MenuItem("P.A. System/Week 12/🎬 시연 씬/열기", priority = -2)]
    public static void OpenExisting()
    {
        if (!File.Exists(SCENE_PATH))
        {
            if (EditorUtility.DisplayDialog(
                    "시연 씬 없음",
                    $"{SCENE_PATH} 가 없습니다.\n지금 새로 만들까요?",
                    "만들기", "취소"))
            {
                CreateOrRebuild();
            }
            return;
        }

        if (!SaveCurrentSceneIfDirty()) return;
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
    }

    [MenuItem("P.A. System/Week 12/🎬 시연 씬/재구축 (Rebuild)", priority = -1)]
    public static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog(
                "시연 씬 재구축",
                $"{SCENE_PATH} 를 처음부터 다시 만듭니다.\n현재 시연 씬의 수동 편집은 사라집니다.\n\n" +
                "(작업 씬 MainGame.unity 는 영향 없음)",
                "재구축", "취소")) return;
        CreateOrRebuild();
    }

    // ── 메인 빌드 ────────────────────────────────────────────────────────
    static void CreateOrRebuild()
    {
        try
        {
            EditorApplication.LockReloadAssemblies();
            Debug.Log("🎬 [Prototype] ─── 시연 씬 빌드 시작 ───");

            // 1. 작업 씬이 dirty 면 저장 (사용자 변경 보호)
            if (!SaveCurrentSceneIfDirty())
            {
                Debug.Log("🎬 [Prototype] 사용자 취소 — 빌드 중단");
                return;
            }

            // 1.5. MainGame.unity 가 열려 있으면 경고만 (덮어쓰지 않으니 안전)
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == MAIN_SCENE_PATH)
            {
                Debug.Log("🎬 [Prototype] MainGame.unity 가 열려 있었음 — 시연 씬으로 전환");
            }

            // 2. 폴더 보장
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // 3. 신규 씬 — 빈 캔버스 (Sky+Light 만)
            var newScene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            // 4. 파일로 1차 저장 (이름 부여)
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            Debug.Log($"🎬 [Prototype] 빈 씬 생성 → {SCENE_PATH}");

            // 5. 풀 빌드 체인 — 새 씬이므로 모든 빌더가 깨끗하게 동작
            //    (각 빌더는 자체 멱등성을 보장하지만 신규 씬은 충돌 가능성 0)
            InvokeOptional("PA_DataBootstrapper", "BootstrapAll");
            Debug.Log("✅ [Prototype] 1/8 Data Bootstrap");

            PA_DataCreator.CreateAll();
            Debug.Log("✅ [Prototype] 2/8 Data Creator");

            PA_SceneAutoBuilder.BuildFullScene();
            Debug.Log("✅ [Prototype] 3/8 Scene Auto Build");

            PA_ContentAutoIntegrator.BuildAll(showDialog: false);
            Debug.Log("✅ [Prototype] 4/8 Building Content");

            PA_MapLayoutBuilder.Build(showDialog: false);
            Debug.Log("✅ [Prototype] 5/8 Map Layout");

            PA_UIBuilder.BuildUISystem();
            Debug.Log("✅ [Prototype] 6/8 UI System");

            PA_TMPFontFixer.Apply();
            Debug.Log("✅ [Prototype] 7/8 Korean Font");

            // 6. 첫날 시나리오 보정 + 보급/소품 + 보리 + NavMesh
            //    rebuildBaseScene=false 로 호출 (이미 위 5단계가 다 했음)
            PA_PlayableDayBuilder.BuildPlayableDayDemo(showDialog: false, rebuildBaseScene: false);
            Debug.Log("✅ [Prototype] 8/8 PlayableDay (보리/보급/시나리오 컨트롤러)");

            // 7. 씬에 식별용 마커 추가 — 작업 중 헷갈리지 않도록
            EnsureSceneMarker();

            // 8. 최종 저장
            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureSceneInBuildSettings(SCENE_PATH);

            Debug.Log($"🎬 [Prototype] ✅ 완료 — {SCENE_PATH}");
            EditorUtility.DisplayDialog(
                "🎬 시연 씬 빌드 완료",
                $"{SCENE_PATH}\n\n" +
                "현재 이 씬이 열려 있습니다.\n" +
                "▶ Play 버튼 → 개척자 등록 → 시나리오 진행\n\n" +
                "작업 씬 MainGame.unity 는 영향 없음.",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [Prototype] 빌드 실패: {e}");
            EditorUtility.DisplayDialog("❌ 시연 씬 빌드 실패", e.Message, "확인");
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────
    // 현재 열린 씬이 dirty 면 사용자에게 저장 여부 묻기 (취소 가능).
    // 안 묻고 강제로 버리지 않는다 — 사용자 작업 보호.
    static bool SaveCurrentSceneIfDirty()
    {
        var current = EditorSceneManager.GetActiveScene();
        if (!current.IsValid()) return true;
        if (!current.isDirty) return true;

        int choice = EditorUtility.DisplayDialogComplex(
            "작업 씬 저장",
            $"현재 열린 씬 '{current.name}' 에 저장되지 않은 변경이 있습니다.\n" +
            "어떻게 처리할까요?",
            "저장 후 진행", "저장 안 하고 진행", "취소");

        switch (choice)
        {
            case 0: // Save
                if (string.IsNullOrEmpty(current.path))
                {
                    // 이름 없는 씬 — 사용자가 직접 저장하게 안내
                    EditorUtility.DisplayDialog("저장 필요",
                        "이름 없는 씬은 먼저 File > Save 로 저장해주세요.", "확인");
                    return false;
                }
                EditorSceneManager.SaveScene(current);
                return true;
            case 1: // Don't save, proceed
                return true;
            default: // Cancel
                return false;
        }
    }

    // 씬 루트에 [SceneInfo_Prototype] 빈 GameObject 를 두어 Hierarchy 에서
    // "지금 시연 씬에 있구나" 를 즉시 알아볼 수 있게 한다.
    static void EnsureSceneMarker()
    {
        const string MARKER = "[SceneInfo_Prototype_FirstDay]";
        var existing = GameObject.Find(MARKER);
        if (existing == null)
        {
            var go = new GameObject(MARKER);
            go.transform.position = Vector3.zero;
            // 메모용 — 다른 시스템에는 영향 0
        }
    }

    // Build Settings 에 자동 추가 — Play 모드 진입 시 씬 인식
    static void EnsureSceneInBuildSettings(string scenePath)
    {
        var current = EditorBuildSettings.scenes;
        foreach (var s in current)
            if (s.path == scenePath) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(current);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"🎬 [Prototype] Build Settings 에 {scenePath} 추가");
    }

    static void InvokeOptional(string typeName, string methodName)
    {
        var t = Type.GetType(typeName);
        if (t == null) return;
        var m = t.GetMethod(methodName,
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        m?.Invoke(null, null);
    }
}
#endif
