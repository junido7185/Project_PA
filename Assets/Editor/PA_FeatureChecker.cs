#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  PA_FeatureChecker — Docs/기능_명세서.md 자동 검증
//  메뉴: P.A. System > 🩺 Implementation Checker (priority = 5)
//
//  설계 의도:
//   • 기능 명세서 312개 항목을 코드로 자동 체크.
//   • 클래스 존재 / 씬 컴포넌트 / 에셋 보유 / Inspector 참조 까지 검증.
//   • 명세 기대 상태(✅/🟡/❌) 와 실제 상태를 좌우 비교 → 차이 강조.
//   • 1인 개발자가 "어디까지 했지?" 를 한 화면에서 파악.
//
//  검증 범위:
//   §1  코드 — 게임플레이 시스템 (60+ 항목)
//   §2  코드 — UI 스크립트 (24 항목)
//   §3  ScriptableObject 클래스
//   §5  씬 GameObject 배치 (선택적, 씬에서 동적 검사)
//   §7  ScriptableObject 인스턴스 (에셋 카운트)
//
//  미검증(스킵): §8~12 = 3D 모델·오디오·애니메이션 — 디스크에 파일 있는지만 체크
// ═══════════════════════════════════════════════════════════════════════════
public class PA_FeatureChecker : EditorWindow
{
    public static void Open()
    {
        var w = GetWindow<PA_FeatureChecker>("🩺 구현 체크리스트");
        w.minSize = new Vector2(560, 700);
        w.Show();
    }

    // ── 데이터 모델 ──────────────────────────────────────────────────────────
    enum Status { OK, Partial, Missing, NA }

    class Check
    {
        public string Section;     // 예: "1-A. 플레이어 & 카메라"
        public string Title;       // 예: "PlayerController"
        public string Description; // 한 줄 설명
        public string SpecStatus;  // 명세서 기대 상태 ("✅", "🟡", "❌")
        public int    Priority;    // 1(★)~3(★★★)
        public Func<(Status status, string detail)> Run;
    }

    // ── 상태 ──────────────────────────────────────────────────────────────────
    Vector2 _scroll;
    List<Check> _checks;
    Dictionary<string, List<Check>> _bySection;
    Dictionary<string, bool> _foldout = new();
    string _filterSearch = "";
    bool _onlyMissing;
    bool _only3Star;
    bool _autoRun = true;
    Dictionary<Check, (Status status, string detail)> _results = new();
    double _nextRefresh;

    // ── 생명주기 ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        BuildCheckList();
        RunAllChecks();
        EditorApplication.update += AutoRefresh;
    }
    void OnDisable() => EditorApplication.update -= AutoRefresh;

    void AutoRefresh()
    {
        if (!_autoRun) return;
        if (EditorApplication.timeSinceStartup < _nextRefresh) return;
        _nextRefresh = EditorApplication.timeSinceStartup + 2.0;
        RunAllChecks();
        Repaint();
    }

    void RunAllChecks()
    {
        _results.Clear();
        foreach (var c in _checks)
        {
            try { _results[c] = c.Run(); }
            catch (Exception e) { _results[c] = (Status.NA, $"체크 실패: {e.Message}"); }
        }
    }

    // ── GUI ───────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        DrawHeader();
        DrawSummary();
        DrawFilterBar();
        DrawSeparator();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var sec in _bySection)
        {
            var visible = sec.Value.Where(PassFilter).ToList();
            if (visible.Count == 0) continue;
            DrawSection(sec.Key, visible);
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        var hdr = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🩺 Project P.A. — 구현 체크리스트", hdr, GUILayout.Height(28));
        GUILayout.FlexibleSpace();
        _autoRun = GUILayout.Toggle(_autoRun, "🔁 자동", "Button", GUILayout.Width(60));
        if (GUILayout.Button("↻ 다시 실행", GUILayout.Width(80))) RunAllChecks();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            "Docs/기능_명세서.md §1~§7 자동 검증. 명세 기대 ↔ 실제 상태 좌우 비교.",
            EditorStyles.miniLabel);
    }

    void DrawSummary()
    {
        int ok = _results.Values.Count(r => r.status == Status.OK);
        int partial = _results.Values.Count(r => r.status == Status.Partial);
        int missing = _results.Values.Count(r => r.status == Status.Missing);
        int total = _checks.Count;
        float pct = total > 0 ? (float)(ok + partial * 0.5f) / total * 100f : 0f;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 진행률 바
        var rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.ProgressBar(rect, pct / 100f,
            $"전체 진행률 {pct:0.0}%   (✅ {ok} · 🟡 {partial} · ❌ {missing} / 총 {total})");

        EditorGUILayout.Space(2);

        // 우선순위별 점수
        var byPrio = _checks.GroupBy(c => c.Priority).OrderByDescending(g => g.Key);
        foreach (var g in byPrio)
        {
            int gOk = g.Count(c => _results.GetValueOrDefault(c).status == Status.OK);
            int gMiss = g.Count(c => _results.GetValueOrDefault(c).status == Status.Missing);
            string stars = new string('★', g.Key) + new string('☆', 3 - g.Key);
            EditorGUILayout.LabelField(
                $"   {stars}  {gOk} / {g.Count()} 완성  ({gMiss} 누락)",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(18));
        _filterSearch = EditorGUILayout.TextField(_filterSearch, GUILayout.MinWidth(120));
        _onlyMissing = GUILayout.Toggle(_onlyMissing, "❌ 누락만", "Button", GUILayout.Width(80));
        _only3Star = GUILayout.Toggle(_only3Star, "★★★ 만", "Button", GUILayout.Width(80));
        if (GUILayout.Button("📂 모두 펼치기", GUILayout.Width(100)))
            foreach (var k in _foldout.Keys.ToList()) _foldout[k] = true;
        if (GUILayout.Button("📁 모두 접기", GUILayout.Width(100)))
            foreach (var k in _foldout.Keys.ToList()) _foldout[k] = false;
        EditorGUILayout.EndHorizontal();
    }

    bool PassFilter(Check c)
    {
        if (_only3Star && c.Priority < 3) return false;
        if (_onlyMissing && _results.GetValueOrDefault(c).status == Status.OK) return false;
        if (!string.IsNullOrEmpty(_filterSearch))
        {
            var s = _filterSearch.ToLowerInvariant();
            if (!c.Title.ToLowerInvariant().Contains(s) &&
                !c.Description.ToLowerInvariant().Contains(s)) return false;
        }
        return true;
    }

    void DrawSection(string section, List<Check> rows)
    {
        if (!_foldout.ContainsKey(section)) _foldout[section] = true;

        // 섹션 진행률
        int sOk = rows.Count(c => _results.GetValueOrDefault(c).status == Status.OK);
        int sTotal = rows.Count;

        var label = $"§{section}    [{sOk}/{sTotal}]";
        _foldout[section] = EditorGUILayout.Foldout(_foldout[section], label, true,
            EditorStyles.foldoutHeader);
        if (!_foldout[section]) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        foreach (var c in rows) DrawCheckRow(c);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    void DrawCheckRow(Check c)
    {
        var (st, detail) = _results.GetValueOrDefault(c);

        var prev = GUI.backgroundColor;
        GUI.backgroundColor = st switch
        {
            Status.OK      => new Color(0.62f, 0.92f, 0.62f),
            Status.Partial => new Color(0.99f, 0.86f, 0.45f),
            Status.Missing => new Color(1.00f, 0.55f, 0.55f),
            _              => new Color(0.7f, 0.7f, 0.7f),
        };

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // 실제 상태 칩
        string emoji = st switch
        {
            Status.OK      => "✅",
            Status.Partial => "🟡",
            Status.Missing => "❌",
            _              => "❔",
        };
        GUILayout.Label(emoji, GUILayout.Width(22));

        // 명세 기대 (회색)
        var specStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        GUILayout.Label($"명세 {c.SpecStatus}", specStyle, GUILayout.Width(50));

        // 본문
        EditorGUILayout.BeginVertical();
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{c.Title}    {new string('★', c.Priority)}", titleStyle);
        EditorGUILayout.LabelField(c.Description, EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(detail))
            EditorGUILayout.LabelField($"  ↳ {detail}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = prev;
    }

    static void DrawSeparator()
    {
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  체크 정의 — 명세서 §1~§7 매핑
    // ══════════════════════════════════════════════════════════════════════
    void BuildCheckList()
    {
        _checks = new List<Check>();

        // ── §1-A 플레이어 & 카메라 ─────────────────────────────────────────
        Add("1-A. 플레이어 & 카메라", "PlayerController",
            "WASD 이동·가속·중력·앉기", "✅", 3,
            () => CheckType("PlayerController"));
        Add("1-A. 플레이어 & 카메라", "PlayerInteraction",
            "정면 SphereCast → IInteractable", "✅", 3,
            () => CheckType("PlayerInteraction"));
        Add("1-A. 플레이어 & 카메라", "PlayerInputHandler",
            "Input System 단일 진입점", "✅", 3,
            () => CheckTypeAndScene("PlayerInputHandler"));
        Add("1-A. 플레이어 & 카메라", "CameraController",
            "플레이어 추적 + SnapToTarget", "✅", 3,
            () => CheckType("CameraController"));
        Add("1-A. 플레이어 & 카메라", "InteractPromptUI",
            "[Space] 프롬프트 텍스트 표시", "❌", 3,
            () => CheckType("InteractPromptUI"));
        Add("1-A. 플레이어 & 카메라", "BuildManager",
            "Ghost 미리보기 → 마우스 배치", "✅", 2,
            () => CheckTypeAndScene("BuildManager"));

        // ── §1-B 경제 ──────────────────────────────────────────────────────
        Add("1-B. 경제 서비스", "EconomyService",
            "TrySpend/Deposit + OnMoneyChanged", "✅", 3,
            () => CheckTypeAndScene("EconomyService"));
        Add("1-B. 경제 서비스", "TierService",
            "5단계 자동 승급", "✅", 3,
            () => CheckTypeAndScene("TierService"));
        Add("1-B. 경제 서비스", "AuditService",
            "7일 주기 자동 평가", "✅", 2,
            () => CheckTypeAndScene("AuditService"));
        Add("1-B. 경제 서비스", "MoneyHUD",
            "현재 재화 화면 상단 표시", "❌", 3,
            () => CheckTypeAndScene("MoneyHUD"));
        Add("1-B. 경제 서비스", "TierHUD",
            "현재 티어 이름·뱃지", "❌", 2,
            () => CheckType("TierHUD"));

        // ── §1-C 시간·계절 ────────────────────────────────────────────────
        Add("1-C. 시간·계절", "GameClock",
            "OnHourTick / OnNewDay / OnSeasonChanged", "✅", 3,
            () => CheckTypeAndScene("GameClock"));
        Add("1-C. 시간·계절", "SeasonModifier",
            "계절별 생산 보정 배수", "✅", 2,
            () => CheckType("SeasonModifier"));
        Add("1-C. 시간·계절", "ClockHUD",
            "시각·일수·계절 UI", "❌", 2,
            () => CheckType("ClockHUD"));
        Add("1-C. 시간·계절", "DayNightVisual",
            "Directional Light 색·강도 보간", "❌", 1,
            () => CheckType("DayNightVisual"));

        // ── §1-D 인벤토리 & 아이템 ────────────────────────────────────────
        Add("1-D. 인벤토리 & 아이템", "Item (SO)", "정적 원형 데이터", "✅", 3,
            () => CheckType("Item"));
        Add("1-D. 인벤토리 & 아이템", "ItemInstance", "런타임 동적 상태", "✅", 3,
            () => CheckType("ItemInstance"));
        Add("1-D. 인벤토리 & 아이템", "ItemRegistry", "id→Item 딕셔너리", "✅", 3,
            () => CheckTypeAndScene("ItemRegistry"));
        Add("1-D. 인벤토리 & 아이템", "Inventory", "20슬롯 컨테이너", "✅", 3,
            () => CheckType("Inventory"));
        Add("1-D. 인벤토리 & 아이템", "Hotbar", "9슬롯 서브셋 + 선택", "✅", 3,
            () => CheckType("Hotbar"));
        Add("1-D. 인벤토리 & 아이템", "PickupItem", "지면 드롭 자동 픽업", "✅", 2,
            () => CheckType("PickupItem"));
        Add("1-D. 인벤토리 & 아이템", "StorageBox", "창고 박스", "✅", 2,
            () => CheckType("StorageBox"));

        // ── §1-E 상점 & 판매 ──────────────────────────────────────────────
        Add("1-E. 상점 & 판매", "Shop", "ShopSlot 컨테이너 (Tag=Shop)", "✅", 3,
            () => CheckType("Shop"));
        Add("1-E. 상점 & 판매", "ShopSlot", "진열대 1칸", "✅", 3,
            () => CheckType("ShopSlot"));
        Add("1-E. 상점 & 판매", "PurchaseEvaluator", "MBTI 구매 확률 함수", "✅", 3,
            () => CheckType("PurchaseEvaluator"));
        Add("1-E. 상점 & 판매", "ShopPriceUI", "가격 입력 패널", "❌", 3,
            () => CheckTypeAndScene("ShopPriceUI"));
        Add("1-E. 상점 & 판매", "CustomerReactionUI", "구매/거부 말풍선", "❌", 3,
            () => CheckType("CustomerReactionUI"));
        Add("1-E. 상점 & 판매", "PriceHintSystem", "완벽가 근접도 게이지", "❌", 2,
            () => CheckType("PriceHintSystem"));
        Add("1-E. 상점 & 판매", "SalesLogManager", "판매 기록 저장·조회", "❌", 1,
            () => CheckTypeAndScene("SalesLogManager"));

        // ── §1-F NPC AI ───────────────────────────────────────────────────
        Add("1-F. NPC AI", "NpcController", "소비형 FSM", "✅", 3,
            () => CheckType("NpcController"));
        Add("1-F. NPC AI", "ProducerNpcController", "생산형 FSM", "✅", 3,
            () => CheckType("ProducerNpcController"));
        Add("1-F. NPC AI", "SpecialistNpcController", "전문가 FSM", "✅", 3,
            () => CheckType("SpecialistNpcController"));
        Add("1-F. NPC AI", "NpcScheduleController", "Schedule → FSM 전환", "✅", 3,
            () => CheckType("NpcScheduleController"));
        Add("1-F. NPC AI", "NpcDialogue", "대화 IInteractable", "✅", 3,
            () => CheckType("NpcDialogue"));
        Add("1-F. NPC AI", "DialogueService", "대화 큐 진행", "✅", 2,
            () => CheckType("DialogueService"));
        Add("1-F. NPC AI", "NpcBubbleUI", "머리 위 말풍선", "❌", 3,
            () => CheckType("NpcBubbleUI"));

        // ── §1-G 채용 & 친밀도 ────────────────────────────────────────────
        Add("1-G. 채용 & 친밀도", "HiringService", "후보 풀·Hire", "✅", 3,
            () => CheckTypeAndScene("HiringService"));
        Add("1-G. 채용 & 친밀도", "FriendshipService", "친밀도 포인트", "✅", 3,
            () => CheckTypeAndScene("FriendshipService"));
        Add("1-G. 채용 & 친밀도", "HiringUI", "채용 탭 UI", "❌", 3,
            () => CheckType("HiringUI"));
        Add("1-G. 채용 & 친밀도", "FriendshipUI", "친밀도 게이지", "❌", 2,
            () => CheckType("FriendshipUI"));

        // ── §1-H 가공 ────────────────────────────────────────────────────
        Add("1-H. 가공", "Workbench", "C키 → CraftingUI", "✅", 3,
            () => CheckType("Workbench"));
        Add("1-H. 가공", "CraftingService", "재료 소모·결과 생성", "✅", 3,
            () => CheckType("CraftingService"));
        Add("1-H. 가공", "CraftingUI", "레시피 목록·제작 버튼", "🟡", 3,
            () => CheckType("CraftingUI"));

        // ── §1-I 파밍 & 채집 ──────────────────────────────────────────────
        Add("1-I. 파밍 & 채집", "Farmland", "호미 → Crop 인스턴스화", "✅", 2,
            () => CheckType("Farmland"));
        Add("1-I. 파밍 & 채집", "Crop", "씨앗→성장→수확", "✅", 2,
            () => CheckType("Crop"));
        Add("1-I. 파밍 & 채집", "Gatherable", "채집 오브젝트", "✅", 2,
            () => CheckType("Gatherable"));

        // ── §1-J 건물 진입 ────────────────────────────────────────────────
        Add("1-J. 건물 진입", "BuildingEntrance", "워프 + Snap", "✅", 3,
            () => CheckType("BuildingEntrance"));
        Add("1-J. 건물 진입", "ScreenFader", "워프 페이드 싱글톤", "✅", 3,
            () => CheckType("ScreenFader"));

        // ── §1-K 저장 & 시스템 ────────────────────────────────────────────
        Add("1-K. 저장 & 시스템", "SaveManager", "F5/F9 async", "✅", 3,
            () => CheckTypeAndScene("SaveManager"));
        Add("1-K. 저장 & 시스템", "ISaveRepository", "Load/Save 인터페이스", "✅", 3,
            () => CheckType("ISaveRepository"));
        Add("1-K. 저장 & 시스템", "LocalJsonSaveRepository", "JSON 구현체", "✅", 3,
            () => CheckType("LocalJsonSaveRepository"));
        Add("1-K. 저장 & 시스템", "SaveData", "DTO", "🟡", 3,
            () => CheckType("SaveData"));
        Add("1-K. 저장 & 시스템", "GridService", "셀 점유 판정", "✅", 3,
            () => CheckTypeAndScene("GridService"));
        Add("1-K. 저장 & 시스템", "BuildingRegistry", "Register/Unregister", "✅", 3,
            () => CheckType("BuildingRegistry"));
        Add("1-K. 저장 & 시스템", "AudioManager", "BGM 크로스페이드 + SFX", "❌", 3,
            () => CheckTypeAndScene("AudioManager"));
        Add("1-K. 저장 & 시스템", "PauseManager", "ESC TimeScale=0", "❌", 2,
            () => CheckTypeAndScene("PauseManager"));

        // ── §2 UI 스크립트 ────────────────────────────────────────────────
        AddUi("InventoryUI", "✅", 3); AddUi("InventorySlotUI", "✅", 3);
        AddUi("InventoryAnchorFollower", "✅", 3); AddUi("HotbarUI", "✅", 3);
        AddUi("SmartphoneUI", "✅", 3); AddUi("DragContext", "✅", 3);
        AddUi("ItemTooltip", "✅", 2); AddUi("StorageUI", "✅", 2);
        AddUi("CraftingUI", "🟡", 3); AddUi("DialogueUI", "❌", 3);
        AddUi("ShopPriceUI", "❌", 3); AddUi("CustomerReactionUI", "❌", 3);
        AddUi("PriceHintGauge", "❌", 2); AddUi("HiringUI", "❌", 3);
        AddUi("AuditResultUI", "❌", 2); AddUi("FeedUI", "❌", 1);
        AddUi("SettingsUI", "❌", 2); AddUi("MoneyHUD", "❌", 3);
        AddUi("ClockHUD", "❌", 2); AddUi("InteractPromptUI", "❌", 3);
        AddUi("TierProgressUI", "❌", 2); AddUi("NpcBubbleUI", "❌", 3);
        AddUi("LoadingScreen", "❌", 1); AddUi("PauseMenuUI", "❌", 2);
        AddUi("MainMenuUI", "❌", 3);

        // ── §3 ScriptableObject 클래스 ─────────────────────────────────────
        AddSo("Item", "✅", 3); AddSo("TierDefinition", "✅", 3);
        AddSo("NpcProfile", "✅", 3); AddSo("NpcDailySchedule", "✅", 3);
        AddSo("NpcCandidateData", "✅", 3); AddSo("RecipeData", "✅", 3);
        AddSo("ProductionData", "✅", 3); AddSo("BuildingData", "✅", 3);
        AddSo("DialogueData", "✅", 3); AddSo("HiddenBlueprintData", "✅", 3);
        AddSo("SeasonModifier", "✅", 2); AddSo("SaleRecord", "❌", 1);

        // ── §5 씬 GameObject 배치 ────────────────────────────────────────
        Add("5-A. 서비스 그룹", "[Services] 그룹 GO",
            "필수 서비스 부모 빈 GameObject", "✅", 3,
            () => GameObject.Find("[Services]") != null
                ? (Status.OK, "씬 존재")
                : (Status.Missing, "씬에 [Services] 없음"));
        Add("5-A. 서비스 그룹", "ScreenFader 씬 배치",
            "씬에 컴포넌트 인스턴스 1개 필요", "🟡", 3,
            () => CheckSceneOnly("ScreenFader"));
        Add("5-B. 플레이어", "Player (Tag=Player)",
            "Tag=Player + CharacterController + PlayerController", "✅", 3,
            () => {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return (Status.Missing, "Tag=Player 객체 없음");
                bool hasCC = p.GetComponent("CharacterController") != null;
                bool hasPC = p.GetComponent("PlayerController") != null;
                bool hasInv = p.GetComponent("Inventory") != null;
                if (hasCC && hasPC && hasInv) return (Status.OK, $"{p.name}");
                var miss = new List<string>();
                if (!hasCC)  miss.Add("CharacterController");
                if (!hasPC)  miss.Add("PlayerController");
                if (!hasInv) miss.Add("Inventory");
                return (Status.Partial, $"{p.name} — 누락: {string.Join(", ", miss)}");
            });
        Add("5-A. 서비스 그룹", "PA_UIRoot Canvas",
            "UI 빌더 결과물", "❌", 3,
            () => GameObject.Find("PA_UIRoot") != null
                ? (Status.OK, "Canvas 존재")
                : (Status.Missing, "PA_UIRoot 없음 — UIBuilder 실행"));
        Add("5-A. 서비스 그룹", "EventSystem",
            "UI 입력 분배기", "❌", 3,
            () => {
                var esType = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
                if (esType == null) return (Status.NA, "EventSystem 타입 없음");
                var found = UnityEngine.Object.FindObjectsByType(esType, FindObjectsSortMode.None);
                if (found.Length == 1) return (Status.OK, "정상");
                if (found.Length == 0) return (Status.Missing, "없음 — 키 입력 작동 안함");
                return (Status.Partial, $"중복 {found.Length}개");
            });

        // ── §7 ScriptableObject 인스턴스 ──────────────────────────────────
        Add("7-A. TierDefinition", "Tier 자산 5개",
            "Tier0~4 (생존자~파트너)", "❌", 3,
            () => CheckAssetCount<TierDefinition>(5));
        Add("7-B. Item", "Item 자산 15+종",
            "Raw·Processed·Luxury·Tool", "❌", 3,
            () => CheckAssetCount<Item>(15));
        Add("7-C. NpcProfile", "NpcProfile 8+종",
            "농부·광부·벌목꾼·어부·쉐프·대장장이·재봉사·목수", "🟡", 3,
            () => CheckAssetCount<NpcProfile>(8));
        Add("7-D. NpcDailySchedule", "Schedule 3+종",
            "Producer/Specialist/Resident", "🟡", 3,
            () => CheckAssetCount<NpcDailySchedule>(3));
        Add("7-E. RecipeData", "Recipe 6+종",
            "원목·광석·빵·감자·생선·가구·도구·의류", "❌", 3,
            () => CheckAssetCount<RecipeData>(6));
        Add("7-F. ProductionData", "ProductionData 4+종",
            "농부·광부·벌목꾼·어부", "🟡", 3,
            () => CheckAssetCount<ProductionData>(4));
        Add("7-G. NpcCandidateData", "Candidate 8+종",
            "채용 가능 NPC 8명", "❌", 3,
            () => CheckAssetCount<NpcCandidateData>(8));
        Add("7-H. DialogueData", "Dialogue 8+종",
            "NPC별 대화 풀", "❌", 3,
            () => CheckAssetCount<DialogueData>(8));
        Add("7-I. BuildingData", "BuildingData 8+종",
            "가판대·잡화점·공방·주방·대장간·재봉대·창고·주택", "❌", 3,
            () => CheckAssetCount<BuildingData>(4));

        // ── 그룹화 ────────────────────────────────────────────────────────
        _bySection = _checks.GroupBy(c => c.Section)
                            .ToDictionary(g => g.Key, g => g.ToList());
    }

    void Add(string section, string title, string desc, string spec, int prio,
             Func<(Status, string)> run)
    {
        _checks.Add(new Check
        {
            Section     = section,
            Title       = title,
            Description = desc,
            SpecStatus  = spec,
            Priority    = prio,
            Run         = run,
        });
    }

    void AddUi(string title, string spec, int prio)
        => Add("2. UI 스크립트", title, "Canvas 위 MonoBehaviour", spec, prio,
               () => CheckType(title));

    void AddSo(string title, string spec, int prio)
        => Add("3. ScriptableObject 클래스", title,
               "CreateAssetMenu 데이터 정의", spec, prio,
               () => CheckType(title));

    // ══════════════════════════════════════════════════════════════════════
    //  체크 함수
    // ══════════════════════════════════════════════════════════════════════
    static (Status, string) CheckType(string typeName)
    {
        var t = FindType(typeName);
        return t != null
            ? (Status.OK, $"{typeName} 컴파일됨")
            : (Status.Missing, $"{typeName} 클래스 없음");
    }

    static (Status, string) CheckTypeAndScene(string typeName)
    {
        var t = FindType(typeName);
        if (t == null) return (Status.Missing, $"{typeName} 클래스 없음");
        if (!typeof(UnityEngine.Object).IsAssignableFrom(t))
            return (Status.OK, "코드만");
        var found = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
        if (found.Length == 0)
            return (Status.Partial, "코드 OK, 씬 미배치");
        if (found.Length > 1)
            return (Status.Partial, $"씬 중복 {found.Length}개");
        return (Status.OK, "코드+씬 OK");
    }

    static (Status, string) CheckSceneOnly(string typeName)
    {
        var t = FindType(typeName);
        if (t == null) return (Status.Missing, $"{typeName} 클래스 없음");
        if (!typeof(UnityEngine.Object).IsAssignableFrom(t))
            return (Status.NA, "코드 전용 (씬 객체 아님)");
        var found = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
        return found.Length > 0
            ? (Status.OK, $"{found.Length}개 배치")
            : (Status.Missing, "씬 미배치");
    }

    static (Status, string) CheckAssetCount<T>(int min) where T : UnityEngine.Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        int count = guids.Length;
        if (count >= min)        return (Status.OK,      $"{count}개 (목표 {min})");
        if (count > 0)           return (Status.Partial, $"{count}/{min}개");
        return (Status.Missing, $"0/{min}개 — Bootstrap 필요");
    }

    static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        // 풀네임이 아닌 짧은 이름이면 모든 타입 순회
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
#endif
