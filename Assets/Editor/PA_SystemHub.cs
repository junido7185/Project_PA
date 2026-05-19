#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Single entry point for Project P.A. editor tooling.
// Default actions are non-destructive: they never rebuild Content/Map/UI or overwrite imported models.
public class PA_SystemHub : EditorWindow
{
    Vector2 _scroll;
    bool _showAdvanced;

    [MenuItem("P.A. System/Project P.A. Hub", priority = -100)]
    public static void Open()
    {
        var window = GetWindow<PA_SystemHub>("Project P.A. Hub");
        window.minSize = new Vector2(460f, 540f);
        window.Show();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        DrawSafeActions();
        DrawReviewActions();
        DrawAdvancedActions();

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        var title = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleLeft
        };

        EditorGUILayout.LabelField("Project P.A. 통합 허브", title, GUILayout.Height(30));
        EditorGUILayout.HelpBox(
            "기본 버튼은 기존 씬, Tripo3D 모델, 텍스처, 맵 배치를 다시 생성하지 않습니다.\n" +
            "전체 재빌드/맵 재배치/UI 재생성은 아래 고급 섹션에만 모아두었습니다.",
            MessageType.Info);
    }

    void DrawSafeActions()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("기본 작업", EditorStyles.boldLabel);

        if (GUILayout.Button("첫날 프로토타입 안전 적용", GUILayout.Height(38)))
            PA_PlayableDayBuilder.BuildSafeFromHub();

        // 🎬 시연 전용 별도 씬 (MainGame.unity 와 분리) — 사용자 작업 보호
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.45f, 0.78f, 1.00f);
        if (GUILayout.Button("🎬 시연 씬 새로 만들기 (Prototype_FirstDay.unity)", GUILayout.Height(34)))
            PA_PrototypeSceneCreator.CreateNew();
        GUI.backgroundColor = prev;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎬 시연 씬 열기", GUILayout.Height(26)))
            PA_PrototypeSceneCreator.OpenExisting();
        if (GUILayout.Button("🎬 시연 씬 재구축", GUILayout.Height(26)))
            PA_PrototypeSceneCreator.Rebuild();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Safe Repair Current Scene", GUILayout.Height(34)))
            PA_SafeSceneRepair.RepairCurrentScene(includeNavMeshBake: true, showDialog: true);

        if (GUILayout.Button("첫날 프로토타입 검증", GUILayout.Height(30)))
            PA_PlayableDayBuilder.ValidateAndReport();

        if (GUILayout.Button("기본 NPC 중복만 수리", GUILayout.Height(30)))
        {
            int removed = PA_NpcDuplicateGuard.RepairGeneratedNpcDuplicates();
            EditorUtility.DisplayDialog("NPC 중복 수리", $"자동 생성 기본 NPC 중복 {removed}개를 정리했습니다.", "확인");
        }
    }

    void DrawReviewActions()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("진단 / 리뷰", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Dev Console 열기", GUILayout.Height(28)))
            PA_DevConsole.Open();
        if (GUILayout.Button("구현 체크리스트", GUILayout.Height(28)))
            PA_FeatureChecker.Open();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("에러 트래커", GUILayout.Height(28)))
            PA_ErrorTracker.Open();
        if (GUILayout.Button("씬 검증", GUILayout.Height(28)))
            PA_SceneValidator.Validate();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Week 11 마일스톤 검증", GUILayout.Height(28)))
            PA_Week11MilestoneValidator.ValidateAndReport();
    }

    void DrawAdvancedActions()
    {
        EditorGUILayout.Space(10);
        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 / 재생성 작업 (주의)", true);
        if (!_showAdvanced) return;

        EditorGUILayout.HelpBox(
            "아래 버튼은 씬 오브젝트, UI, 맵 배치, 건물 프리팹/머티리얼을 다시 만들 수 있습니다. " +
            "이미 손으로 적용한 모델링이나 배치가 있다면 누르기 전에 씬을 저장하거나 Git diff를 확인하세요.",
            MessageType.Warning);

        if (GUILayout.Button("데이터 에셋 생성/보정", GUILayout.Height(28)))
            PA_DataCreator.CreateAll();

        if (GUILayout.Button("폰트 자동 설정", GUILayout.Height(28)))
            PA_TMPFontFixer.Apply();

        if (ConfirmButton("UI 시스템 재생성", "PA_UIRoot/EventSystem/스마트폰 UI를 다시 만들 수 있습니다. 계속할까요?"))
            PA_UIBuilder.BuildUISystem();

        if (ConfirmButton("건물 콘텐츠 재생성", "건물 프리팹/머티리얼/텍스처 연결을 다시 생성합니다. 계속할까요?"))
            PA_ContentAutoIntegrator.BuildAll(showDialog: true);

        if (ConfirmButton("맵 레이아웃 재배치", "건물/마커 위치를 레이아웃 기준으로 다시 배치합니다. 계속할까요?"))
            PA_MapLayoutBuilder.Build(showDialog: true);

        if (ConfirmButton("전체 씬 재구축", "가장 위험한 작업입니다. 기존 씬 배치가 덮어써질 수 있습니다. 계속할까요?"))
            PA_SceneAutoBuilder.BuildFullScene();

        if (ConfirmButton("Vertical Slice + Demo Seed 재구축", "기존 모델링/씬 배치가 바뀔 수 있습니다. 계속할까요?"))
            PA_VerticalSlice.Build();

        if (ConfirmButton("첫날 프로토타입 + 기본 씬 재구축", "기존 씬/맵/UI를 다시 만든 뒤 첫날 프로토타입을 얹습니다. 계속할까요?"))
            PA_PlayableDayBuilder.BuildPlayableDayDemo(showDialog: false, rebuildBaseScene: true);
    }

    bool ConfirmButton(string label, string message)
    {
        if (!GUILayout.Button(label, GUILayout.Height(28))) return false;
        return EditorUtility.DisplayDialog("주의", message, "실행", "취소");
    }
}
#endif
