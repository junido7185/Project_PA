#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// ═══════════════════════════════════════════════════════════════════════════
//  PA_DevConsole — Project P.A. 통합 개발 도구
//  메뉴: P.A. System > ⚡ Dev Console (priority = 0)
//
//  설계 의도 (Docs/06_개발_로드맵_및_명세.md, 기능_명세서.md):
//   • 1인 개발자의 빠른 반복(iteration) 을 위한 "버튼 한 번 = 완성" 도구.
//   • 흩어져 있던 5개 빌더 (DataBootstrapper / DataCreator / SceneAutoBuilder /
//     UIBuilder / SceneValidator) 를 단일 EditorWindow 로 통합.
//   • 중복 생성 방지: 빌드 전 자동 청소(Clean) → 멱등(idempotent) 동작.
//   • 자동 수리: 입력 안 먹힘·타임스케일 멈춤·EventSystem 누락 등 흔한
//     "왜 안 되지?" 를 한 클릭으로 진단·복구.
//   • 실시간 진단 패널: 현재 씬 상태를 색상 칩으로 한눈에.
//
//  개발자가 알아야 할 단축키:
//   F5     — 진단 새로고침
//   Ctrl+R — 모두 재구축 (Clean + Build All + Wire + Validate)
// ═══════════════════════════════════════════════════════════════════════════
public class PA_DevConsole : EditorWindow
{
    // ── 메뉴 진입 ─────────────────────────────────────────────────────────────
    [MenuItem("P.A. System/⚡ Dev Console", priority = 0)]
    public static void Open()
    {
        var w = GetWindow<PA_DevConsole>("⚡ P.A. Dev Console");
        w.minSize = new Vector2(440, 600);
        w.Show();
    }

    // ── 상태 ──────────────────────────────────────────────────────────────────
    Vector2 _scroll;
    DiagnosticReport _report;
    string _lastActionLog = "";
    bool _showAdvanced;
    bool _showCleanup;
    bool _autoRefresh = true;
    double _nextRefresh;

    // ──────────────────────────────────────────────────────────────────────────
    //  GUI
    // ──────────────────────────────────────────────────────────────────────────
    void OnEnable()
    {
        _report = Diagnose();
        EditorApplication.update += AutoRefresh;
    }
    void OnDisable() => EditorApplication.update -= AutoRefresh;

    void AutoRefresh()
    {
        if (!_autoRefresh) return;
        if (EditorApplication.timeSinceStartup < _nextRefresh) return;
        _nextRefresh = EditorApplication.timeSinceStartup + 1.0; // 1초마다
        _report = Diagnose();
        Repaint();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawTitleBar();
        DrawDiagnostics();
        EditorGUILayout.Space(8);
        DrawOneClickActions();
        EditorGUILayout.Space(8);
        DrawIndividualBuilders();
        EditorGUILayout.Space(8);
        DrawAutoFix();
        EditorGUILayout.Space(8);
        DrawCleanupSection();
        EditorGUILayout.Space(8);
        DrawAdvancedSection();
        EditorGUILayout.Space(8);
        DrawFooter();

        EditorGUILayout.EndScrollView();

        // 단축키
        var e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.F5) { _report = Diagnose(); Repaint(); e.Use(); }
            if (e.control && e.keyCode == KeyCode.R) { Action_FullRebuild(); e.Use(); }
        }
    }

    // ── 타이틀바 ──────────────────────────────────────────────────────────────
    void DrawTitleBar()
    {
        var hdr = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⚡ Project P.A. — Dev Console", hdr, GUILayout.Height(28));
        GUILayout.FlexibleSpace();
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "🔁 자동", "Button", GUILayout.Width(60));
        if (GUILayout.Button("↻ 새로고침", GUILayout.Width(80)))
        { _report = Diagnose(); Repaint(); }
        EditorGUILayout.EndHorizontal();

        // 자매 윈도우 열기 버튼
        EditorGUILayout.BeginHorizontal();
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        if (GUILayout.Button("🩺 구현 체크리스트", GUILayout.Height(26)))
            PA_FeatureChecker.Open();
        GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f);
        if (GUILayout.Button("🐛 에러 트래커", GUILayout.Height(26)))
            PA_ErrorTracker.Open();
        GUI.backgroundColor = new Color(0.95f, 0.85f, 1.0f);
        if (GUILayout.Button("🔤 한글 폰트", GUILayout.Height(26)))
            PA_TMPFontFixer.Apply();
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Scene: {SceneManager.GetActiveScene().name}    " +
            $"Mode: {(EditorApplication.isPlayingOrWillChangePlaymode ? "▶ Play" : "■ Edit")}    " +
            $"TimeScale: {Time.timeScale:0.00}",
            EditorStyles.miniLabel);
        DrawSeparator();
    }

    // ── 진단 패널 ─────────────────────────────────────────────────────────────
    void DrawDiagnostics()
    {
        EditorGUILayout.LabelField("📋 씬 상태 진단", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawStatusGrid(_report.Items);

        // 종합 점수
        int total = _report.Items.Count;
        int ok    = _report.Items.Count(i => i.Status == ItemStatus.OK);
        int warn  = _report.Items.Count(i => i.Status == ItemStatus.Warning);
        int err   = _report.Items.Count(i => i.Status == ItemStatus.Error);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            $"✅ {ok}    ⚠️ {warn}    ❌ {err}    (총 {total})",
            EditorStyles.miniBoldLabel);

        if (err > 0)
            EditorGUILayout.HelpBox(
                "❌ 오류 항목이 있습니다. 아래 [🩹 자동 수리] 버튼을 누르면 대부분 해결됩니다.",
                MessageType.Error);
        else if (warn > 0)
            EditorGUILayout.HelpBox(
                "⚠️ 경고 항목이 있습니다. 동작은 하지만 일부 기능이 누락될 수 있습니다.",
                MessageType.Warning);
        else
            EditorGUILayout.HelpBox(
                "✅ 씬이 완벽한 상태입니다. Play 버튼을 눌러 테스트하세요!",
                MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    void DrawStatusGrid(List<DiagItem> items)
    {
        const int cols = 2;
        for (int i = 0; i < items.Count; i += cols)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                int idx = i + c;
                if (idx >= items.Count) { GUILayout.FlexibleSpace(); continue; }
                DrawStatusChip(items[idx]);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawStatusChip(DiagItem item)
    {
        string emoji = item.Status switch
        {
            ItemStatus.OK      => "✅",
            ItemStatus.Warning => "⚠️",
            ItemStatus.Error   => "❌",
            _ => "❔"
        };

        var prevColor = GUI.backgroundColor;
        GUI.backgroundColor = item.Status switch
        {
            ItemStatus.OK      => new Color(0.62f, 0.92f, 0.62f),
            ItemStatus.Warning => new Color(0.99f, 0.86f, 0.45f),
            ItemStatus.Error   => new Color(1.00f, 0.55f, 0.55f),
            _ => Color.gray
        };

        var content = new GUIContent($"{emoji} {item.Label}", item.Detail);
        GUILayout.Box(content, GUILayout.Height(22), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = prevColor;
    }

    // ── 원클릭 액션 (큰 버튼) ─────────────────────────────────────────────────
    void DrawOneClickActions()
    {
        EditorGUILayout.LabelField("🚀 원클릭 작업 (가장 자주 쓰는 버튼)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 큰 메인 버튼
        var bigBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            fixedHeight = 42,
        };

        GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
        if (GUILayout.Button("🛠  모두 자동 구축 (Clean → Data → Scene → UI → Wire → Validate)", bigBtn))
            Action_FullRebuild();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            "⌨ Ctrl+R 단축키. 처음이거나 씬이 망가졌을 때 이 버튼 하나만 누르면 됩니다.",
            MessageType.None);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1.00f, 0.85f, 0.45f);
        if (GUILayout.Button("🩹 자동 수리\n(키 안먹힘·타임스톱·중복 등)", GUILayout.Height(48)))
            Action_AutoFix();
        GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        if (GUILayout.Button("🔍 씬 검증\n(에러만 콘솔에)", GUILayout.Height(48)))
            Action_Validate();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ── 개별 빌더 (세분화) ─────────────────────────────────────────────────────
    void DrawIndividualBuilders()
    {
        EditorGUILayout.LabelField("🧱 개별 빌드 단계 (필요할 때만)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawBuilderRow("1️⃣ 데이터 자산 부트스트랩",
            "Tier/Item/Recipe/NpcProfile (ScriptableObjects/) 일괄 생성",
            () => SafeRun(PA_DataBootstrapper_BootstrapAll));
        DrawBuilderRow("2️⃣ 누락 데이터 보완",
            "Item id 9~15, Recipe 5종, Candidate 8종, Dialogue 8종 (Resources/)",
            () => SafeRun(PA_DataCreator.CreateAll));
        DrawBuilderRow("3️⃣ 씬 자동 빌드",
            "Ground/Services/Shop/Workbench/NPC 12명 자동 배치",
            () => SafeRun(PA_SceneAutoBuilder.BuildFullScene));
        DrawBuilderRow("4️⃣ UI 자동 구축",
            "Canvas/Hotbar/Inventory/Smartphone/Tooltip 재구성",
            () => SafeRun(PA_UIBuilder.BuildUISystem));

        EditorGUILayout.EndVertical();
    }

    void DrawBuilderRow(string title, string desc, Action onClick)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        if (GUILayout.Button("실행", GUILayout.Width(60), GUILayout.Height(32)))
            onClick();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }

    // ── 자동 수리 (상세) ──────────────────────────────────────────────────────
    void DrawAutoFix()
    {
        EditorGUILayout.LabelField("🩹 자동 수리 (개별)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⏰ TimeScale=1 강제", GUILayout.Height(28)))
        { Time.timeScale = 1f; Log("⏰ Time.timeScale 1로 복구"); _report = Diagnose(); }
        if (GUILayout.Button("🎮 EventSystem 재생성", GUILayout.Height(28)))
            Action_RebuildEventSystem();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎯 PlayerInputHandler 보장", GUILayout.Height(28)))
            Action_EnsurePlayerInput();
        if (GUILayout.Button("🚫 중복 싱글톤 제거", GUILayout.Height(28)))
            Action_RemoveDuplicateSingletons();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔗 Inspector 참조 다시 잇기", GUILayout.Height(28)))
            Action_RewireReferences();
        if (GUILayout.Button("🧭 NavMesh 자동 Bake", GUILayout.Height(28)))
            Action_BakeNavMesh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("👥 NPC 크기/리그 보정", GUILayout.Height(28)))
            Action_FixNpcPresentation();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔤 Jalnan2 폰트 재적용", GUILayout.Height(28)))
        { PA_TMPFontFixer.Apply(); Log("🔤 Jalnan2 폰트 재적용"); _report = Diagnose(); }
        if (GUILayout.Button("🎨 슬롯 베이지 강화", GUILayout.Height(28)))
        {
            int n = BrightenSlotPrefab();
            Log($"🎨 슬롯 프리팹 베이지 적용: {n}개");
            _report = Diagnose();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ── 씬 청소 ───────────────────────────────────────────────────────────────
    void DrawCleanupSection()
    {
        _showCleanup = EditorGUILayout.Foldout(_showCleanup,
            "🧹 씬 청소 — 중복 제거·초기화 (조심스러운 작업)", true, EditorStyles.foldoutHeader);
        if (!_showCleanup) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox(
            "씬에서 P.A.가 만든 객체만 골라 삭제합니다. " +
            "나머지(카메라·라이트·플레이어 등) 는 손대지 않습니다.",
            MessageType.Warning);

        if (GUILayout.Button("🧹 P.A. 생성 객체 모두 삭제 (재빌드 전 청소)", GUILayout.Height(30)))
            Action_CleanScene();
        if (GUILayout.Button("🧹 UI 만 삭제 (PA_UIRoot)", GUILayout.Height(28)))
            Action_CleanUI();
        if (GUILayout.Button("🧹 NPC 만 삭제 ([NPCs])", GUILayout.Height(28)))
            Action_CleanGroup("[NPCs]");

        EditorGUILayout.EndVertical();
    }

    // ── 고급 ──────────────────────────────────────────────────────────────────
    void DrawAdvancedSection()
    {
        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced,
            "⚙ 고급 — 로그·디버그", true, EditorStyles.foldoutHeader);
        if (!_showAdvanced) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("최근 액션 로그", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(_lastActionLog,
            EditorStyles.textArea, GUILayout.MinHeight(80));
        if (GUILayout.Button("로그 비우기")) _lastActionLog = "";
        EditorGUILayout.EndVertical();
    }

    void DrawFooter()
    {
        DrawSeparator();
        EditorGUILayout.LabelField(
            "Docs/06 개발_로드맵 · 기능_명세서.md 기준  |  단축키: F5 새로고침 · Ctrl+R 재구축",
            EditorStyles.centeredGreyMiniLabel);
    }

    static void DrawSeparator()
    {
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  진단 (Diagnose)
    // ══════════════════════════════════════════════════════════════════════
    public class DiagItem
    {
        public string Label;
        public string Detail;
        public ItemStatus Status;
    }
    public enum ItemStatus { OK, Warning, Error }
    public class DiagnosticReport { public List<DiagItem> Items = new(); }

    static DiagnosticReport Diagnose()
    {
        var rep = new DiagnosticReport();

        // ── 필수 싱글톤 ────────────────────────────────────────────────────
        var required = new (Type t, string label)[]
        {
            (typeof(EconomyService),     "EconomyService"),
            (typeof(TierService),        "TierService"),
            (typeof(GameClock),          "GameClock"),
            (typeof(ItemRegistry),       "ItemRegistry"),
            (typeof(FriendshipService),  "FriendshipService"),
            (typeof(HiringService),      "HiringService"),
            (typeof(GridService),        "GridService"),
            (typeof(PlayerInputHandler), "PlayerInputHandler"),
            (typeof(AuditService),       "AuditService"),
            (typeof(SaveManager),        "SaveManager"),
        };
        foreach (var (t, label) in required)
        {
            var objs = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
            if (objs.Length == 0)
                rep.Items.Add(new DiagItem { Label = label, Detail = "씬에 없음", Status = ItemStatus.Error });
            else if (objs.Length > 1)
                rep.Items.Add(new DiagItem { Label = label, Detail = $"중복 {objs.Length}개", Status = ItemStatus.Error });
            else
                rep.Items.Add(new DiagItem { Label = label, Detail = "정상", Status = ItemStatus.OK });
        }

        // ── EventSystem ───────────────────────────────────────────────────
        var events = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        rep.Items.Add(new DiagItem
        {
            Label  = "EventSystem",
            Detail = events.Length == 1 ? "정상" : (events.Length == 0 ? "없음" : $"중복 {events.Length}개"),
            Status = events.Length == 1 ? ItemStatus.OK : ItemStatus.Error,
        });

        // ── PA_UIRoot ──────────────────────────────────────────────────────
        var uiRoot = GameObject.Find("PA_UIRoot");
        rep.Items.Add(new DiagItem
        {
            Label  = "PA_UIRoot (Canvas)",
            Detail = uiRoot != null ? "있음" : "없음 — UI 버튼 작동 안함",
            Status = uiRoot != null ? ItemStatus.OK : ItemStatus.Error,
        });

        // ── Player ─────────────────────────────────────────────────────────
        var player = GameObject.FindWithTag("Player");
        rep.Items.Add(new DiagItem
        {
            Label  = "Player(태그)",
            Detail = player != null ? player.name : "Player 태그 객체 없음",
            Status = player != null ? ItemStatus.OK : ItemStatus.Warning,
        });

        // ── TimeScale ──────────────────────────────────────────────────────
        rep.Items.Add(new DiagItem
        {
            Label  = "Time.timeScale",
            Detail = $"{Time.timeScale:0.00}",
            Status = Mathf.Approximately(Time.timeScale, 1f) ? ItemStatus.OK : ItemStatus.Error,
        });

        // ── 데이터 에셋 ────────────────────────────────────────────────────
        // ⚠ AssetDatabase.FindAssets 는 존재하지 않는 폴더를 받으면 경고를 출력한다.
        //   FindAssetsSafe 헬퍼로 폴더 미존재 시 0 을 반환해 콘솔 스팸 방지.
        int itemCount = FindAssetsSafe("t:Item",             "Assets/Resources/Items")
                      + FindAssetsSafe("t:Item",             "Assets/ScriptableObjects/Items");
        int recipeCnt = FindAssetsSafe("t:RecipeData",       "Assets/Resources/Recipes")
                      + FindAssetsSafe("t:RecipeData",       "Assets/ScriptableObjects/Recipes");
        int candCnt   = FindAssetsSafe("t:NpcCandidateData", "Assets/Resources/Candidates");

        rep.Items.Add(new DiagItem
        {
            Label  = "Item 자산",
            Detail = $"{itemCount}개",
            Status = itemCount >= 8 ? ItemStatus.OK : (itemCount > 0 ? ItemStatus.Warning : ItemStatus.Error),
        });
        rep.Items.Add(new DiagItem
        {
            Label  = "Recipe 자산",
            Detail = $"{recipeCnt}개",
            Status = recipeCnt >= 3 ? ItemStatus.OK : (recipeCnt > 0 ? ItemStatus.Warning : ItemStatus.Error),
        });
        rep.Items.Add(new DiagItem
        {
            Label  = "Candidate 자산",
            Detail = $"{candCnt}개",
            Status = candCnt >= 4 ? ItemStatus.OK : (candCnt > 0 ? ItemStatus.Warning : ItemStatus.Error),
        });

        // ── HiringService.availableCandidates ──────────────────────────────
        var hiring = UnityEngine.Object.FindFirstObjectByType<HiringService>();
        if (hiring != null)
        {
            int cc = hiring.availableCandidates?.Count ?? 0;
            rep.Items.Add(new DiagItem
            {
                Label  = "Hiring.availableCandidates",
                Detail = $"{cc}개 연결",
                Status = cc > 0 ? ItemStatus.OK : ItemStatus.Warning,
            });
        }

        // ── ItemRegistry.allItems ─────────────────────────────────────────
        var reg = UnityEngine.Object.FindFirstObjectByType<ItemRegistry>();
        if (reg != null)
        {
            int ic = reg.allItems?.Count ?? 0;
            rep.Items.Add(new DiagItem
            {
                Label  = "ItemRegistry.allItems",
                Detail = $"{ic}개 연결",
                Status = ic > 0 ? ItemStatus.OK : ItemStatus.Warning,
            });
        }

        var phone = UnityEngine.Object.FindFirstObjectByType<SmartphoneUI>();
        int wiredPanels = phone != null && phone.tabPanels != null ? phone.tabPanels.Count(p => p != null) : 0;
        int wiredButtons = phone != null && phone.tabButtons != null ? phone.tabButtons.Count(b => b != null) : 0;
        rep.Items.Add(new DiagItem
        {
            Label = "Smartphone Apps",
            Detail = phone == null ? "SmartphoneUI 없음" : $"패널 {wiredPanels}/4, 버튼 {wiredButtons}/4",
            Status = phone != null && phone.homeScreen != null && wiredPanels == 4 && wiredButtons == 4
                ? ItemStatus.OK
                : ItemStatus.Warning,
        });

        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int npcVisuals = npcs.Count(n => n != null && n.GetComponentInChildren<SkinnedMeshRenderer>(true) != null);
        rep.Items.Add(new DiagItem
        {
            Label = "NPC Visual Assets",
            Detail = $"{npcVisuals}/{npcs.Length} 모델 연결",
            Status = npcs.Length == 0 ? ItemStatus.Warning :
                npcVisuals == npcs.Length ? ItemStatus.OK : ItemStatus.Warning,
        });

        var avatarStats = GetNpcAvatarStats();
        rep.Items.Add(new DiagItem
        {
            Label = "NPC Avatar/Rig",
            Detail = $"{avatarStats.valid}/{avatarStats.total} Humanoid valid",
            Status = avatarStats.total > 0 && avatarStats.valid == avatarStats.total
                ? ItemStatus.OK
                : ItemStatus.Warning,
        });

        var navSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        var ground = GameObject.Find("Ground");
        bool hasNavSurface = ground != null && navSurfaceType != null && ground.GetComponent(navSurfaceType) != null;
        rep.Items.Add(new DiagItem
        {
            Label = "NavMeshSurface",
            Detail = navSurfaceType == null ? "AI Navigation 패키지 없음" : hasNavSurface ? "Ground 연결됨" : "Ground 연결 필요",
            Status = navSurfaceType != null && hasNavSurface ? ItemStatus.OK : ItemStatus.Warning,
        });

        return rep;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  액션 핸들러
    // ══════════════════════════════════════════════════════════════════════

    // ── 한방 재구축 ───────────────────────────────────────────────────────
    void Action_FullRebuild()
    {
        if (!EditorUtility.DisplayDialog("⚡ 모두 자동 구축 (v3)",
            "다음 순서로 일괄 진행합니다:\n\n" +
            "  1. 씬 청소 (P.A. 생성 객체 삭제)\n" +
            "  2. 데이터 부트스트랩\n" +
            "  3. 누락 데이터 보완\n" +
            "  4. 씬 자동 빌드\n" +
            "  5. UI 자동 구축\n" +
            "  6. 🔤 Jalnan2 한글 폰트 적용\n" +
            "  7. 자동 수리 (TimeScale·EventSystem·슬롯 색상…)\n" +
            "  8. 씬 검증\n\n" +
            "약 5~8초 소요. 계속하시겠습니까?",
            "진행", "취소")) return;

        try
        {
            ScriptableSilencer.Begin(); // 빌더 다이얼로그 자동 OK 모드
            Log("─── 🛠 모두 자동 구축 시작 ───");

            CleanScene();
            Log("✅ 1/7 씬 청소");

            PA_DataBootstrapper_BootstrapAll();
            Log("✅ 2/7 데이터 부트스트랩");

            PA_DataCreator.CreateAll();
            Log("✅ 3/7 누락 데이터 보완");

            PA_SceneAutoBuilder.BuildFullScene();
            Log("✅ 4/7 씬 빌드");

            PA_UIBuilder.BuildUISystem();
            Log("✅ 5/8 UI 빌드");

            // ⚠ v3 신규 단계: Jalnan2 폰트를 모든 신규 TMP_Text 에 즉시 강제 적용
            //   (UI 빌드 직후 → 한글 깨짐 방지)
            PA_TMPFontFixer.Apply();
            Log("✅ 6/8 한글 폰트 적용");

            AutoFixAll();
            Log("✅ 7/8 자동 수리");

            PA_SceneValidator.Validate();
            Log("✅ 8/8 씬 검증");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            _report = Diagnose();
            EditorUtility.DisplayDialog("완료",
                "모두 자동 구축 완료!\n\n" +
                "다음으로:\n" +
                "  • 자동 수리 단계에서 NavMesh Bake 까지 함께 시도됨\n" +
                "  • Play 버튼 누르고 WASD/I/P 키 테스트\n",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 모두 자동 구축 실패: {e}");
            Log($"❌ 실패: {e.Message}");
            EditorUtility.DisplayDialog("실패",
                $"{e.Message}\n\nConsole 에 스택 트레이스가 남았습니다.", "확인");
        }
        finally
        {
            ScriptableSilencer.End();
        }
    }

    // ── 자동 수리 ─────────────────────────────────────────────────────────
    void Action_AutoFix()
    {
        try
        {
            Log("─── 🩹 자동 수리 시작 ───");
            int fixedCount = AutoFixAll();
            _report = Diagnose();
            EditorUtility.DisplayDialog("자동 수리 완료",
                $"{fixedCount}개 항목을 수리했습니다.\n\nConsole 로그를 확인하세요.", "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 자동 수리 실패: {e}");
            Log($"❌ 실패: {e.Message}");
        }
    }

    int AutoFixAll()
    {
        int n = 0;

        // 1) TimeScale 강제 1
        if (!Mathf.Approximately(Time.timeScale, 1f))
        {
            Time.timeScale = 1f;
            Debug.Log("[PA AutoFix] ⏰ Time.timeScale 1.0 으로 복구");
            Log("⏰ TimeScale 복구"); n++;
        }

        // 2) EventSystem 단일화
        var events = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (events.Length == 0)
        {
            CreateEventSystem();
            Debug.Log("[PA AutoFix] 🎮 EventSystem 새로 생성");
            Log("🎮 EventSystem 생성"); n++;
        }
        else if (events.Length > 1)
        {
            for (int i = 1; i < events.Length; i++)
                UnityEngine.Object.DestroyImmediate(events[i].gameObject);
            Debug.Log($"[PA AutoFix] 🚫 EventSystem 중복 {events.Length - 1}개 제거");
            Log($"🚫 EventSystem 중복 {events.Length - 1}개 제거"); n++;
        }

        // 3) PlayerInputHandler 보장 (씬 키 입력 핵심!)
        var inputHandlers = UnityEngine.Object.FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
        if (inputHandlers.Length == 0)
        {
            EnsurePlayerInputHandler();
            Debug.Log("[PA AutoFix] 🎯 PlayerInputHandler [Services] 에 추가");
            Log("🎯 PlayerInputHandler 추가"); n++;
        }
        else if (inputHandlers.Length > 1)
        {
            for (int i = 1; i < inputHandlers.Length; i++)
                UnityEngine.Object.DestroyImmediate(inputHandlers[i]);
            Debug.Log($"[PA AutoFix] 🚫 PlayerInputHandler 중복 {inputHandlers.Length - 1}개 제거");
            Log($"🚫 PlayerInputHandler 중복 제거"); n++;
        }

        // 4) 기타 싱글톤 중복 제거
        n += RemoveDuplicateSingletons(silent: true);
        n += RemoveDuplicateNamedSceneObjects("[Services]", "PA_UIRoot", "AudioManager", "PA_RuntimeUI", "ShopPriceUI");

        // 5) HiringService.availableCandidates 자동 채움
        var hiring = UnityEngine.Object.FindFirstObjectByType<HiringService>();
        if (hiring != null && (hiring.availableCandidates == null || hiring.availableCandidates.Count == 0))
        {
            var cands = LoadAllAssets<NpcCandidateData>("Assets/Resources/Candidates");
            if (cands.Count > 0)
            {
                hiring.availableCandidates = cands;
                EditorUtility.SetDirty(hiring);
                Debug.Log($"[PA AutoFix] 🤝 HiringService.availableCandidates {cands.Count}개 자동 연결");
                Log($"🤝 Candidates {cands.Count}개 연결"); n++;
            }
        }

        // 6) ItemRegistry.allItems 자동 채움
        var reg = UnityEngine.Object.FindFirstObjectByType<ItemRegistry>();
        if (reg != null && (reg.allItems == null || reg.allItems.Count == 0))
        {
            var items = LoadAllAssets<Item>("Assets/Resources/Items");
            items.AddRange(LoadAllAssets<Item>("Assets/ScriptableObjects/Items"));
            if (items.Count > 0)
            {
                reg.allItems = items.Distinct().ToList();
                EditorUtility.SetDirty(reg);
                Debug.Log($"[PA AutoFix] 📦 ItemRegistry.allItems {reg.allItems.Count}개 자동 연결");
                Log($"📦 Items {reg.allItems.Count}개 연결"); n++;
            }
        }

        // 6.5) UI_Slot 프리팹 베이지 강화 (스프라이트 미발견 시 어두워 보이는 문제 해결)
        n += BrightenSlotPrefab();

        // 7) NpcDialogue 누락 dialogueData 자동 매칭
        var dialogues = UnityEngine.Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None);
        var dlgAssets = LoadAllAssets<DialogueData>("Assets/Resources/Dialogues");
        foreach (var nd in dialogues)
        {
            if (nd.dialogueData != null) continue;
            // GameObject 이름에서 NPC 종류 추출 (예: NPC_Farmer → Farmer)
            var key = nd.gameObject.name.Replace("NPC_", "");
            var match = dlgAssets.FirstOrDefault(d => d.name.Contains(key));
            if (match != null)
            {
                nd.dialogueData = match;
                EditorUtility.SetDirty(nd);
                Debug.Log($"[PA AutoFix] 💬 {nd.gameObject.name}.dialogueData ← {match.name}");
                n++;
            }
        }

        n += EnsureEditorRuntimeServices();
        n += EnsureSmartphoneAppWiring();
        n += EnsureNpcModelImportSettings();
        n += EnsureNpcAssetsAndHooks();
        n += EnsurePlayerEditorHooks();

        if (TryBakeNavMesh(showDialogs: false, log: Log, out bool navSurfaceAdded) && navSurfaceAdded)
            n++;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[PA AutoFix] 총 {n}개 수정");
        return n;
    }

    // ── 개별 자동 수리 ────────────────────────────────────────────────────
    void Action_RebuildEventSystem()
    {
        foreach (var es in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(es.gameObject);
        CreateEventSystem();
        Log("🎮 EventSystem 재생성");
        _report = Diagnose();
    }

    void Action_EnsurePlayerInput()
    {
        EnsurePlayerInputHandler();
        Log("🎯 PlayerInputHandler 보장");
        _report = Diagnose();
    }

    void Action_RemoveDuplicateSingletons()
    {
        int n = RemoveDuplicateSingletons(silent: false);
        Log($"🚫 중복 싱글톤 {n}개 제거");
        _report = Diagnose();
    }

    void Action_RewireReferences()
    {
        // UI 자동 빌더의 WireReferences 부분만 다시 호출하기 위해 전체 빌더 재실행은 부담.
        // 따라서 자동 수리 내부에서 동일 작업 수행 (allItems, candidates, dialogues 자동 연결).
        AutoFixAll();
        Log("🔗 참조 자동 연결");
    }

    // NavMesh 자동 Bake — Ground 의 NavMeshSurface 를 리플렉션으로 호출
    void Action_FixNpcPresentation()
    {
        int n = 0;
        n += EnsureNpcModelImportSettings();
        n += EnsureNpcAssetsAndHooks();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Log($"NPC 크기/리그 보정: {n}개");
        _report = Diagnose();
    }

    void Action_BakeNavMesh()
    {
        try
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                EditorUtility.DisplayDialog("NavMesh Bake 실패",
                    "씬에 Ground 객체가 없습니다.\nDev Console > 모두 자동 구축 먼저 실행하세요.", "확인");
                return;
            }

            // AI Navigation 패키지의 NavMeshSurface 타입 (리플렉션)
            var surfaceType =
                Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                EditorUtility.DisplayDialog("AI Navigation 패키지 없음",
                    "Window > Package Manager > Unity Registry > AI Navigation 설치 후 재시도.",
                    "확인");
                return;
            }

            var surface = ground.GetComponent(surfaceType);
            if (surface == null)
            {
                surface = ground.AddComponent(surfaceType);
                Debug.Log("[PA DevConsole] 🧭 Ground 에 NavMeshSurface 추가");
            }

            // BuildNavMesh() 메서드 리플렉션 호출
            var bakeMethod = surfaceType.GetMethod("BuildNavMesh");
            if (bakeMethod == null)
            {
                Action_NavMeshHelp();
                return;
            }
            bakeMethod.Invoke(surface, null);

            Log("🧭 NavMesh Bake 완료");
            EditorUtility.DisplayDialog("NavMesh Bake 완료",
                "NPC 들이 이제 이동할 수 있습니다.\n▶ Play 모드에서 확인하세요.", "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ NavMesh Bake 실패: {e}");
            Action_NavMeshHelp();
        }
    }

    void Action_NavMeshHelp()
    {
        EditorUtility.DisplayDialog("NavMesh Bake",
            "NPC 이동을 위한 NavMesh 굽기:\n\n" +
            "  1. Hierarchy 에서 'Ground' 선택\n" +
            "  2. Inspector → NavMeshSurface 컴포넌트\n" +
            "  3. [Bake] 버튼 클릭\n\n" +
            "Bake 후에야 NPC 가 이동합니다.\n" +
            "(Window → AI → Navigation 창 사용도 가능)",
            "확인");
    }

    void Action_Validate()
    {
        PA_SceneValidator.Validate();
        Log("🔍 씬 검증 — 콘솔 확인");
        _report = Diagnose();
    }

    // ── 청소 ──────────────────────────────────────────────────────────────
    void Action_CleanScene()
    {
        if (!EditorUtility.DisplayDialog("씬 청소",
            "P.A.가 만든 객체 모두 삭제:\n\n" +
            "[Services], [Workbenches], [Markers], [NPCs], Shop, Ground, PA_UIRoot, EventSystem\n\n" +
            "Player·Camera·Light 등 그 외 객체는 보존합니다.",
            "삭제", "취소")) return;
        CleanScene();
        Log("🧹 씬 청소 완료");
        _report = Diagnose();
    }

    void Action_CleanUI()
    {
        var ui = GameObject.Find("PA_UIRoot");
        if (ui != null) UnityEngine.Object.DestroyImmediate(ui);
        Log("🧹 PA_UIRoot 삭제");
        _report = Diagnose();
    }

    void Action_CleanGroup(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        Log($"🧹 '{name}' 삭제");
        _report = Diagnose();
    }

    static void CleanScene()
    {
        string[] knownNames =
        {
            "[Services]", "[Workbenches]", "[Markers]", "[NPCs]",
            "Shop", "Ground", "PA_UIRoot",
        };
        foreach (var n in knownNames)
        {
            var go = GameObject.Find(n);
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
        // EventSystem 도 제거 (UI 빌더가 다시 만든다)
        foreach (var es in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(es.gameObject);
        // ShopPriceUI / MoneyHUD 같은 독립 객체도 제거
        var shopUI = UnityEngine.Object.FindFirstObjectByType<ShopPriceUI>();
        if (shopUI != null) UnityEngine.Object.DestroyImmediate(shopUI.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════════════════

    static int EnsureEditorRuntimeServices()
    {
        int changed = 0;
        var services = EnsureRootObject("[Services]");

        changed += EnsureSingleSceneComponent<EconomyService>(services);
        changed += EnsureSingleSceneComponent<TierService>(services);
        changed += EnsureSingleSceneComponent<GameClock>(services);
        changed += EnsureSingleSceneComponent<ItemRegistry>(services);
        changed += EnsureSingleSceneComponent<FriendshipService>(services);
        changed += EnsureSingleSceneComponent<HiringService>(services);
        changed += EnsureSingleSceneComponent<GridService>(services);
        changed += EnsureSingleSceneComponent<PlayerInputHandler>(services);
        changed += EnsureSingleSceneComponent<AuditService>(services);
        changed += EnsureSingleSceneComponent<SalesLogManager>(services);
        changed += EnsureSingleSceneComponent<SaveManager>(services);
        changed += EnsureSingleSceneComponent<BuildingRegistry>(services);
        changed += EnsureSingleSceneComponent<BuildManager>(services);
        changed += EnsureSingleSceneComponent<GameManager>(services);
        changed += EnsureSingleSceneComponent<ScreenFader>(services);

        var existingAudio = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        var audioRoot = existingAudio != null ? existingAudio.gameObject : EnsureRootObject("AudioManager");
        changed += EnsureSingleSceneComponent<AudioManager>(audioRoot);

        var uiRoot = GameObject.Find("PA_UIRoot");
        if (uiRoot != null)
        {
            var runtimeUi = GameObject.Find("PA_RuntimeUI");
            if (runtimeUi == null)
            {
                runtimeUi = new GameObject("PA_RuntimeUI");
                runtimeUi.transform.SetParent(uiRoot.transform, false);
                changed++;
            }

            changed += EnsureSingleSceneComponent<InteractPromptUI>(runtimeUi);
            changed += EnsureSingleSceneComponent<DialogueUI>(runtimeUi);
            changed += EnsureSingleSceneComponent<FriendshipUI>(runtimeUi);
            changed += EnsureSingleSceneComponent<ClockHUD>(runtimeUi);
            changed += EnsureSingleSceneComponent<CraftingUI>(runtimeUi);
            changed += EnsureSingleSceneComponent<PauseManager>(runtimeUi);
            changed += EnsureSingleSceneComponent<MoneyHUD>(runtimeUi);
        }

        var existingShopPrice = UnityEngine.Object.FindFirstObjectByType<ShopPriceUI>();
        var shopPriceRoot = existingShopPrice != null
            ? existingShopPrice.gameObject
            : (GameObject.Find("ShopPriceUI") ?? new GameObject("ShopPriceUI"));
        changed += EnsureSingleSceneComponent<ShopPriceUI>(shopPriceRoot);

        return changed;
    }

    static int EnsureSmartphoneAppWiring()
    {
        int changed = 0;

        changed += EnsurePanelApp<AuditResultUI>("AuditPanel");
        changed += EnsurePanelApp<HiringUI>("HiringPanel");
        changed += EnsurePanelApp<FeedUI>("FeedPanel");
        changed += EnsurePanelApp<SettingsUI>("SettingsPanel");

        var phone = UnityEngine.Object.FindFirstObjectByType<SmartphoneUI>();
        if (phone == null) return changed;

        var home = FindDescendant(phone.transform, "HomeScreen");
        if (home != null && phone.homeScreen != home.gameObject)
        {
            phone.homeScreen = home.gameObject;
            EditorUtility.SetDirty(phone);
            changed++;
        }

        string[] panelNames = { "AuditPanel", "HiringPanel", "FeedPanel", "SettingsPanel" };
        var panels = new GameObject[panelNames.Length];
        var buttons = new UnityEngine.UI.Button[panelNames.Length];
        bool needsPanelWrite = phone.tabPanels == null || phone.tabPanels.Length != panelNames.Length;
        bool needsButtonWrite = phone.tabButtons == null || phone.tabButtons.Length != panelNames.Length;

        for (int i = 0; i < panelNames.Length; i++)
        {
            panels[i] = FindDescendant(phone.transform, panelNames[i])?.gameObject;
            var tile = FindDescendant(phone.transform, panelNames[i] + "_Tile");
            buttons[i] = tile != null ? tile.GetComponentInChildren<UnityEngine.UI.Button>(true) : null;

            if (!needsPanelWrite && panels[i] != phone.tabPanels[i]) needsPanelWrite = true;
            if (!needsButtonWrite && buttons[i] != phone.tabButtons[i]) needsButtonWrite = true;
        }

        if (needsPanelWrite)
        {
            phone.tabPanels = panels;
            EditorUtility.SetDirty(phone);
            changed++;
        }

        if (needsButtonWrite)
        {
            phone.tabButtons = buttons;
            EditorUtility.SetDirty(phone);
            changed++;
        }

        return changed;
    }

    static int EnsurePanelApp<T>(string panelName) where T : Component
    {
        var panel = GameObject.Find(panelName);
        if (panel == null) return 0;

        int changed = 0;
        var apps = panel.GetComponents<T>();
        if (apps.Length == 0)
        {
            panel.AddComponent<T>();
            changed++;
        }
        else if (apps.Length > 1)
        {
            for (int i = 1; i < apps.Length; i++)
                UnityEngine.Object.DestroyImmediate(apps[i]);
            changed += apps.Length - 1;
        }

        if (panelName == "AuditPanel")
        {
            var placeholder = panel.transform.Find("Placeholder");
            if (placeholder != null)
            {
                UnityEngine.Object.DestroyImmediate(placeholder.gameObject);
                changed++;
            }
        }

        return changed;
    }

    static int EnsureNpcModelImportSettings()
    {
        int changed = 0;
        foreach (string path in GetNpcModelPaths())
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (importer.optimizeGameObjects)
            {
                importer.optimizeGameObjects = false;
                dirty = true;
            }

            if (importer.importCameras)
            {
                importer.importCameras = false;
                dirty = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        return changed;
    }

    static (int total, int valid) GetNpcAvatarStats()
    {
        int total = 0;
        int valid = 0;

        foreach (string path in GetNpcModelPaths())
        {
            if (!File.Exists(path)) continue;
            total++;

            var avatar = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Avatar>()
                .FirstOrDefault(a => a != null);
            if (avatar != null && avatar.isHuman && avatar.isValid)
                valid++;
        }

        return (total, valid);
    }

    static string[] GetNpcModelPaths()
    {
        return new[]
        {
            "Assets/Art/Character/C-02.fbx",
            "Assets/Art/Character/C-03.fbx",
            "Assets/Art/Character/C-04.fbx",
            "Assets/Art/Character/C-05.fbx",
            "Assets/Art/Character/C-06.fbx",
            "Assets/Art/Character/C-07.fbx",
            "Assets/Art/Character/C-08.fbx",
            "Assets/Art/Character/C-09.fbx",
        };
    }

    static int EnsureNpcAssetsAndHooks()
    {
        int changed = 0;
        var dialogues = LoadAllAssets<DialogueData>("Assets/Resources/Dialogues");
        var profiles = LoadAllAssets<NpcProfile>("Assets/ScriptableObjects/NPCs");

        foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc.profile == null)
            {
                var profile = MatchProfile(profiles, npc.gameObject.name);
                if (profile != null)
                {
                    npc.profile = profile;
                    EditorUtility.SetDirty(npc);
                    changed++;
                }
            }

            var dialogue = npc.GetComponent<NpcDialogue>();
            if (dialogue == null)
            {
                dialogue = npc.gameObject.AddComponent<NpcDialogue>();
                changed++;
            }

            if (dialogue.dialogueData == null)
            {
                dialogue.dialogueData = MatchDialogue(dialogues, npc);
                if (dialogue.dialogueData != null)
                {
                    EditorUtility.SetDirty(dialogue);
                    changed++;
                }
            }

            if (string.IsNullOrEmpty(dialogue.friendshipId))
            {
                dialogue.friendshipId = ResolveFriendshipId(npc);
                EditorUtility.SetDirty(dialogue);
                changed++;
            }

            if (npc.GetComponentInChildren<NpcBubbleUI>(true) == null)
            {
                var bubble = new GameObject("NpcBubbleUI", typeof(RectTransform), typeof(Canvas));
                bubble.transform.SetParent(npc.transform, false);
                bubble.AddComponent<NpcBubbleUI>();
                changed++;
            }

            foreach (var bubble in npc.GetComponentsInChildren<NpcBubbleUI>(true))
            {
                Vector3 targetOffset = new Vector3(0f, 4.0f, 0f);
                if (bubble.offset != targetOffset || bubble.transform.localPosition != targetOffset)
                {
                    bubble.offset = targetOffset;
                    bubble.transform.localPosition = targetOffset;
                    EditorUtility.SetDirty(bubble);
                    changed++;
                }
            }

            changed += EnsureSingleComponentOnObject<NpcPresentationNormalizer>(npc.gameObject);
            var normalizer = npc.GetComponent<NpcPresentationNormalizer>();
            if (normalizer != null)
            {
                if (normalizer.animationMode != NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural)
                {
                    normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
                    EditorUtility.SetDirty(normalizer);
                    changed++;
                }

                if (normalizer.animatorController != null)
                {
                    normalizer.animatorController = null;
                    EditorUtility.SetDirty(normalizer);
                    changed++;
                }
            }

            changed += EnsureNpcModelVisual(npc);
            if (NpcPresentationNormalizer.Normalize(npc.gameObject))
            {
                EditorUtility.SetDirty(npc.gameObject);
                if (normalizer != null)
                    EditorUtility.SetDirty(normalizer);
                foreach (var animator in npc.GetComponentsInChildren<Animator>(true))
                    EditorUtility.SetDirty(animator);
                foreach (var procedural in npc.GetComponentsInChildren<NpcHumanoidProceduralAnimator>(true))
                    EditorUtility.SetDirty(procedural);
                changed++;
            }
        }

        return changed;
    }

    static int EnsureNpcModelVisual(NpcController npc)
    {
        var existing = FindDescendant(npc.transform, "CharacterVisual");
        int changed = 0;

        if (existing == null && npc.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
        {
            string modelPath = ResolveNpcModelPath(npc);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                var inst = PrefabUtility.InstantiatePrefab(prefab, npc.transform) as GameObject;
                if (inst == null) inst = UnityEngine.Object.Instantiate(prefab, npc.transform);
                inst.name = "CharacterVisual";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one * NpcPresentationNormalizer.VisualScale;
                existing = inst.transform;
                changed++;
            }
        }

        if (existing != null)
        {
            var primitive = npc.transform.Find("Visual");
            if (primitive != null && primitive.gameObject.activeSelf)
            {
                primitive.gameObject.SetActive(false);
                changed++;
            }
        }

        return changed;
    }

    static int EnsurePlayerEditorHooks()
    {
        int changed = 0;
        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player == null) return 0;

        changed += EnsureSingleComponentOnObject<EquipmentSystem>(player);

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
            changed += EnsureSingleComponentOnObject<PlayerFootIkStabilizer>(animator.gameObject);

        var buildManager = UnityEngine.Object.FindFirstObjectByType<BuildManager>();
        if (buildManager != null && buildManager.placementAnchor != player.transform)
        {
            buildManager.placementAnchor = player.transform;
            EditorUtility.SetDirty(buildManager);
            changed++;
        }

        return changed;
    }

    static int EnsureSingleSceneComponent<T>(GameObject preferredHost) where T : Component
    {
        var all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (all.Length == 0)
        {
            preferredHost.AddComponent<T>();
            EditorUtility.SetDirty(preferredHost);
            return 1;
        }

        T keep = all.FirstOrDefault(c => c.gameObject == preferredHost) ?? all[0];
        int removed = 0;
        foreach (var comp in all)
        {
            if (comp == keep) continue;
            UnityEngine.Object.DestroyImmediate(comp);
            removed++;
        }

        return removed;
    }

    static int EnsureSingleComponentOnObject<T>(GameObject host) where T : Component
    {
        var comps = host.GetComponents<T>();
        if (comps.Length == 0)
        {
            host.AddComponent<T>();
            EditorUtility.SetDirty(host);
            return 1;
        }

        int removed = 0;
        for (int i = 1; i < comps.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(comps[i]);
            removed++;
        }

        return removed;
    }

    static int RemoveDuplicateNamedSceneObjects(params string[] names)
    {
        int removed = 0;
        var activeScene = SceneManager.GetActiveScene();

        foreach (var name in names)
        {
            var matches = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t != null && t.name == name && t.gameObject.scene == activeScene)
                .Select(t => t.gameObject)
                .Distinct()
                .ToList();

            if (matches.Count <= 1) continue;

            var keep = matches.FirstOrDefault(HasRoleComponent) ?? matches[0];
            foreach (var go in matches)
            {
                if (go == keep) continue;
                UnityEngine.Object.DestroyImmediate(go);
                removed++;
            }
        }

        return removed;
    }

    static bool HasRoleComponent(GameObject go)
    {
        return go.GetComponent<EventSystem>() != null
            || go.GetComponent<Canvas>() != null
            || go.GetComponent<EconomyService>() != null
            || go.GetComponent<AudioManager>() != null
            || go.GetComponent<ShopPriceUI>() != null;
    }

    static GameObject EnsureRootObject(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go : new GameObject(name);
    }

    static NpcProfile MatchProfile(List<NpcProfile> profiles, string objectName)
    {
        string key = CleanNpcKey(objectName);
        return profiles.FirstOrDefault(p =>
            p != null &&
            (CleanNpcKey(p.name).Contains(key) ||
             (!string.IsNullOrEmpty(p.npcName) && CleanNpcKey(p.npcName).Contains(key))));
    }

    static DialogueData MatchDialogue(List<DialogueData> dialogues, NpcController npc)
    {
        string key = CleanNpcKey(npc.profile != null ? npc.profile.name : npc.gameObject.name);
        return dialogues.FirstOrDefault(d => d != null && CleanNpcKey(d.name).Contains(key));
    }

    static string ResolveFriendshipId(NpcController npc)
    {
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.name))
            return npc.profile.name;
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.npcName))
            return npc.profile.npcName;
        return npc.gameObject.name;
    }

    static string ResolveNpcModelPath(NpcController npc)
    {
        string key = CleanNpcKey(
            (npc.profile != null ? npc.profile.name + " " + npc.profile.npcName : "") + " " + npc.gameObject.name);

        if (key.Contains("lumber")) return "Assets/Art/Character/C-03.fbx";
        if (key.Contains("miner")) return "Assets/Art/Character/C-04.fbx";
        if (key.Contains("fisher")) return "Assets/Art/Character/C-05.fbx";
        if (key.Contains("chef")) return "Assets/Art/Character/C-06.fbx";
        if (key.Contains("blacksmith")) return "Assets/Art/Character/C-07.fbx";
        if (key.Contains("tailor")) return "Assets/Art/Character/C-08.fbx";
        if (key.Contains("carpenter")) return "Assets/Art/Character/C-09.fbx";
        return "Assets/Art/Character/C-02.fbx";
    }

    static string CleanNpcKey(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.ToLowerInvariant()
            .Replace("npc_", "")
            .Replace("profile_", "")
            .Replace("dialogue_", "")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
    }

    static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindDescendant(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    static bool TryBakeNavMesh(bool showDialogs, Action<string> log, out bool surfaceAdded)
    {
        surfaceAdded = false;
        try
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                if (showDialogs)
                    EditorUtility.DisplayDialog("NavMesh Bake 실패", "씬에 Ground 객체가 없습니다.", "확인");
                return false;
            }

            var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                if (showDialogs)
                    EditorUtility.DisplayDialog("AI Navigation 패키지 없음",
                        "Package Manager 에서 AI Navigation 패키지를 설치해야 자동 Bake 가 가능합니다.", "확인");
                return false;
            }

            var surface = ground.GetComponent(surfaceType);
            if (surface == null)
            {
                surface = ground.AddComponent(surfaceType);
                surfaceAdded = true;
            }

            var bakeMethod = surfaceType.GetMethod("BuildNavMesh");
            if (bakeMethod == null) return false;

            bakeMethod.Invoke(surface, null);
            EditorUtility.SetDirty(ground);
            log?.Invoke("NavMesh baked");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PA DevConsole] NavMesh bake failed: {e}");
            return false;
        }
    }

    static int RemoveDuplicateSingletons(bool silent)
    {
        Type[] singletonTypes = {
            typeof(EconomyService), typeof(TierService), typeof(GameClock),
            typeof(ItemRegistry), typeof(FriendshipService), typeof(HiringService),
            typeof(GridService), typeof(AuditService), typeof(SaveManager),
            typeof(BuildManager), typeof(BuildingRegistry), typeof(SalesLogManager),
            typeof(GameManager), typeof(AudioManager), typeof(ScreenFader),
            typeof(SmartphoneUI), typeof(InventoryUI), typeof(HotbarUI),
            typeof(PauseManager), typeof(MoneyHUD), typeof(ShopPriceUI),
            typeof(DialogueUI), typeof(ClockHUD), typeof(CraftingUI),
        };

        int total = 0;
        foreach (var t in singletonTypes)
        {
            var objs = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
            for (int i = 1; i < objs.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(objs[i]);
                total++;
                if (!silent) Debug.Log($"[PA AutoFix] 🚫 {t.Name} 중복 인스턴스 제거");
            }
        }
        return total;
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
        // Input System UI 모듈 — 패키지 미설치 시 컴파일 가능하도록 리플렉션
        var moduleType = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null)
            go.AddComponent(moduleType);
        else
            go.AddComponent<StandaloneInputModule>();
    }

    static void EnsurePlayerInputHandler()
    {
        if (UnityEngine.Object.FindFirstObjectByType<PlayerInputHandler>() != null) return;

        // [Services] 가 있으면 거기에, 없으면 새 GameObject
        var services = GameObject.Find("[Services]");
        if (services == null)
        {
            services = new GameObject("[Services]");
            Debug.Log("[PA AutoFix] [Services] 새로 생성");
        }
        services.AddComponent<PlayerInputHandler>();
    }

    // UI_Slot 프리팹의 Image 알파/컬러를 동물의 숲 베이지로 강화.
    // 핫바 BG 가 비쳐서 어두워 보이는 시각 버그 해결 (스프라이트 미존재 시 fallback).
    static int BrightenSlotPrefab()
    {
        const string SLOT_PATH = "Assets/Prefabs/UI_Slot.prefab";
        if (!System.IO.File.Exists(SLOT_PATH)) return 0;

        var root = PrefabUtility.LoadPrefabContents(SLOT_PATH);
        if (root == null) return 0;

        var inventorySlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_inventory_slot.png");
        bool changed = false;
        foreach (var img in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            // 슬롯 본체만 (Icon/CountText 자식은 건드리지 않음)
            if (img.gameObject.name != "UI_Slot") continue;
            // 알파 1.0 + 베이지 #EFE4D0 강제
            if (inventorySlotSprite != null && img.sprite != inventorySlotSprite)
            {
                img.sprite = inventorySlotSprite;
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = true;
                changed = true;
            }

            var target = inventorySlotSprite != null
                ? Color.white
                : new Color(0.937f, 0.894f, 0.816f, 1f);
            if (img.color != target)
            {
                img.color = target;
                changed = true;
            }
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, SLOT_PATH);
            Debug.Log("[PA AutoFix] 🎨 UI_Slot 프리팹 베이지 컬러 적용");
        }
        PrefabUtility.UnloadPrefabContents(root);
        return changed ? 1 : 0;
    }

    static List<T> LoadAllAssets<T>(string folder) where T : UnityEngine.Object
    {
        var list = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return list;
        foreach (var g in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) list.Add(a);
        }
        return list;
    }

    // 폴더 미존재 시 AssetDatabase.FindAssets 가 콘솔 경고를 출력하는 문제 방지.
    // IsValidFolder 사전 체크로 0 을 반환한다.
    static int FindAssetsSafe(string filter, string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return 0;
        return AssetDatabase.FindAssets(filter, new[] { folder }).Length;
    }

    // PA_DataBootstrapper 의 BootstrapAll() 은 private static 이므로 리플렉션 호출
    static void PA_DataBootstrapper_BootstrapAll()
    {
        var t = Type.GetType("PA_DataBootstrapper");
        var m = t?.GetMethod("BootstrapAll",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        m?.Invoke(null, null);
    }

    static void SafeRun(Action a)
    {
        try { a?.Invoke(); }
        catch (Exception e) { Debug.LogError($"[PA DevConsole] 액션 실패: {e}"); }
    }

    void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _lastActionLog = line + "\n" + _lastActionLog;
        if (_lastActionLog.Length > 2000)
            _lastActionLog = _lastActionLog.Substring(0, 2000);
    }

    // 빌더들이 띄우는 확인 다이얼로그를 자동 OK 처리할 수 없으므로
    // 표식만 남기고 사용자가 [모두 자동 구축] 시 다이얼로그가 떠도 [진행] 누르도록 안내.
    // (Unity Editor 다이얼로그를 진정으로 무시하려면 reflection 으로 ShowDialog 가
    //  걸리는데, 사용자 흐름을 끊지 않기 위해 기본적으로 활성화하지 않음)
    static class ScriptableSilencer
    {
        public static void Begin() { /* future: hook EditorUtility.DisplayDialog */ }
        public static void End()   { }
    }
}
#endif
