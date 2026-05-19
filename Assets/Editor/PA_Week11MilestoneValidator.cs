#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

// 🎯 PA_Week11MilestoneValidator — 9~12주차 마일스톤 종합 점검기
//
// 목적:
//   13주차 (JSON 저장/로드 데모) 진입 전, 9~12주차에 걸쳐 구현된 모든 시스템이
//   서로 정상적으로 연결되어 플레이 가능한 상태인지 한 화면에서 검증한다.
//
// 메뉴:
//   P.A. System / Week 11 / Build + Validate Milestone
//   P.A. System / Week 11 / Validate Only
//
// PASS / WARN / FAIL 분류:
//   - PASS : 13주차로 넘어갈 준비 완료
//   - WARN : 동작은 하지만 보완 필요 — 개발일지에 사유 기록
//   - FAIL : 13주차 진입 차단 — 즉시 수리 권장
public static class PA_Week11MilestoneValidator
{
    public enum Level { Pass, Warn, Fail }

    public class Item
    {
        public string  Category;
        public string  Label;
        public Level   Level;
        public string  Detail;
    }

    // ── 메뉴 진입점 ──────────────────────────────────────────────────────
    public static void BuildAndValidate()
    {
        if (!EditorUtility.DisplayDialog(
                "🎯 Week 11 Milestone — Build + Validate",
                "다음을 한 번에 진행합니다:\n\n" +
                "  1. PA_DataCreator.CreateAll\n" +
                "  2. PA_ContentAutoIntegrator.BuildAll\n" +
                "  3. PA_MapLayoutBuilder.Build\n" +
                "  4. PA_UIBuilder.BuildUISystem\n" +
                "  5. PA_VerticalSlice.ValidateAll\n" +
                "  6. Week 11 종합 검증\n\n" +
                "약 6~10초.",
                "시작", "취소")) return;

        try
        {
            EditorApplication.LockReloadAssemblies();
            Debug.Log("🎯 [Week11] ─── 시작 ───");

            PA_DataCreator.CreateAll();
            PA_ContentAutoIntegrator.BuildAll(showDialog: false);
            PA_MapLayoutBuilder.Build(showDialog: false);
            PA_UIBuilder.BuildUISystem();

            var sliceReport = PA_VerticalSlice.ValidateAll();
            Debug.Log("🚀 [Week11→VerticalSlice]\n" + string.Join("\n", sliceReport));

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            ValidateAndReport();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [Week11] 빌드 실패: {e}");
            EditorUtility.DisplayDialog("❌ Week 11 실패", e.Message, "확인");
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    public static void ValidateOnly() => ValidateAndReport();

    // ── 종합 검증 + 다이얼로그 ──────────────────────────────────────────
    public static void ValidateAndReport()
    {
        var items = ValidateAll();
        int pass = items.Count(i => i.Level == Level.Pass);
        int warn = items.Count(i => i.Level == Level.Warn);
        int fail = items.Count(i => i.Level == Level.Fail);

        // 콘솔 — 카테고리별 그룹
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🎯 [Week11] 검증 결과 — PASS {pass} / WARN {warn} / FAIL {fail}");
        foreach (var grp in items.GroupBy(i => i.Category))
        {
            sb.AppendLine($"\n── {grp.Key} ──");
            foreach (var i in grp)
                sb.AppendLine($"  {Mark(i.Level)} {i.Label}: {i.Detail}");
        }
        if (fail > 0) Debug.LogError(sb.ToString());
        else if (warn > 0) Debug.LogWarning(sb.ToString());
        else Debug.Log(sb.ToString());

        // 다이얼로그 — 요약
        string summary =
            $"PASS: {pass}\nWARN: {warn}\nFAIL: {fail}\n\n" +
            (fail == 0 ? "✅ 13주차 (JSON 저장/로드 데모) 진입 가능."
                       : "❌ FAIL 항목을 먼저 수리하세요. Console 에 상세 리포트가 있습니다.");
        EditorUtility.DisplayDialog("🎯 Week 11 Milestone", summary, "확인");
    }

    // ── 11개 카테고리 × N개 검사 ────────────────────────────────────────
    public static List<Item> ValidateAll()
    {
        var list = new List<Item>();

        ValidateInteractAndPrompt(list);
        ValidateDialogue(list);
        ValidateSmartphoneApps(list);
        ValidateScriptableAssets(list);
        ValidateSingletons(list);
        ValidateShop(list);
        ValidateCrafting(list);
        ValidateBuildLoop(list);
        ValidateSceneStructure(list);
        ValidateNavMesh(list);
        ValidateSaveLoadPreflight(list);

        return list;
    }

    // ── ① 상호작용 + 프롬프트 (§3) ──────────────────────────────────────
    static void ValidateInteractAndPrompt(List<Item> list)
    {
        const string CAT = "①상호작용/프롬프트";
        var prompt = UnityEngine.Object.FindFirstObjectByType<InteractPromptUI>();
        list.Add(prompt != null
            ? Pass(CAT, "InteractPromptUI", "씬 연결됨")
            : Fail(CAT, "InteractPromptUI", "씬에 없음 — Space 프롬프트 표시 불가"));

        if (prompt != null)
        {
            // [Space] 텍스트가 표시되는지: PromptText 자식 또는 TextMeshProUGUI 컴포넌트
            var hasText = prompt.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) != null
                       || prompt.GetComponentInChildren<UnityEngine.UI.Text>(true) != null;
            list.Add(hasText
                ? Pass(CAT, "Prompt 텍스트 컴포넌트", "TMP/Text 발견")
                : Warn(CAT, "Prompt 텍스트 컴포넌트", "Text/TMP 미발견 — 표시 안 될 수 있음"));
        }
    }

    // ── ② Dialogue (§4) ────────────────────────────────────────────────
    static void ValidateDialogue(List<Item> list)
    {
        const string CAT = "②대화/말풍선";
        var dlgUI = UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
        list.Add(dlgUI != null
            ? Pass(CAT, "DialogueUI", "씬 연결됨")
            : Fail(CAT, "DialogueUI", "씬에 없음"));

        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int withDialogue = 0, dataLinked = 0, withBubble = 0;
        foreach (var n in npcs)
        {
            var d = n.GetComponent<NpcDialogue>();
            if (d != null) { withDialogue++; if (d.dialogueData != null) dataLinked++; }
            if (n.GetComponentInChildren<NpcBubbleUI>(true) != null) withBubble++;
        }
        list.Add(npcs.Length == 0
            ? Warn(CAT, "NpcController 수", "0 — NPC 미배치")
            : (withDialogue == npcs.Length
                ? Pass(CAT, "NpcDialogue 부착", $"{withDialogue}/{npcs.Length}")
                : Fail(CAT, "NpcDialogue 부착", $"{withDialogue}/{npcs.Length}")));
        list.Add(npcs.Length == 0
            ? Warn(CAT, "DialogueData 연결", "NPC 없음")
            : (dataLinked == npcs.Length
                ? Pass(CAT, "DialogueData 연결", $"{dataLinked}/{npcs.Length}")
                : Warn(CAT, "DialogueData 연결", $"{dataLinked}/{npcs.Length} — 누락 NPC 있음")));
        list.Add(npcs.Length == 0
            ? Warn(CAT, "NpcBubbleUI 부착", "NPC 없음")
            : (withBubble == npcs.Length
                ? Pass(CAT, "NpcBubbleUI 부착", $"{withBubble}/{npcs.Length}")
                : Warn(CAT, "NpcBubbleUI 부착", $"{withBubble}/{npcs.Length}")));
    }

    // ── ③ Smartphone 4탭 (§5) ───────────────────────────────────────────
    static void ValidateSmartphoneApps(List<Item> list)
    {
        const string CAT = "③Smartphone 4탭";
        var phone = UnityEngine.Object.FindFirstObjectByType<SmartphoneUI>();
        list.Add(phone != null
            ? Pass(CAT, "SmartphoneUI", "씬 연결됨")
            : Fail(CAT, "SmartphoneUI", "씬에 없음"));
        if (phone == null) return;

        list.Add(phone.homeScreen != null
            ? Pass(CAT, "homeScreen 참조", phone.homeScreen.name)
            : Warn(CAT, "homeScreen 참조", "null"));

        int pCount = phone.tabPanels?.Count(p => p != null) ?? 0;
        int bCount = phone.tabButtons?.Count(b => b != null) ?? 0;
        list.Add(pCount == 4 ? Pass(CAT, "tabPanels", "4/4") : Fail(CAT, "tabPanels", $"{pCount}/4"));
        list.Add(bCount == 4 ? Pass(CAT, "tabButtons", "4/4") : Fail(CAT, "tabButtons", $"{bCount}/4"));

        // 각 패널에 해당 앱 컴포넌트 부착
        var hiringPanel = GameObject.Find("HiringPanel");
        var auditPanel  = GameObject.Find("AuditPanel");
        var feedPanel   = GameObject.Find("FeedPanel");
        var settPanel   = GameObject.Find("SettingsPanel");
        list.Add(hiringPanel != null && hiringPanel.GetComponentInChildren<HiringUI>(true) != null
            ? Pass(CAT, "HiringPanel ⊃ HiringUI", "OK")
            : Fail(CAT, "HiringPanel ⊃ HiringUI", "패널 또는 컴포넌트 누락"));
        list.Add(auditPanel  != null && auditPanel.GetComponentInChildren<AuditResultUI>(true) != null
            ? Pass(CAT, "AuditPanel ⊃ AuditResultUI", "OK")
            : Warn(CAT, "AuditPanel ⊃ AuditResultUI", "누락"));
        list.Add(feedPanel   != null && feedPanel.GetComponentInChildren<FeedUI>(true) != null
            ? Pass(CAT, "FeedPanel ⊃ FeedUI", "OK")
            : Warn(CAT, "FeedPanel ⊃ FeedUI", "누락"));
        list.Add(settPanel   != null && settPanel.GetComponentInChildren<SettingsUI>(true) != null
            ? Pass(CAT, "SettingsPanel ⊃ SettingsUI", "OK")
            : Warn(CAT, "SettingsPanel ⊃ SettingsUI", "누락"));
    }

    // ── ④ ScriptableObject 에셋 최소치 ──────────────────────────────────
    static void ValidateScriptableAssets(List<Item> list)
    {
        const string CAT = "④SO 에셋 최소치";
        int items     = CountAssets("t:Item",             "Assets/Resources/Items")
                      + CountAssets("t:Item",             "Assets/ScriptableObjects/Items");
        int recipes   = CountAssets("t:RecipeData",       "Assets/Resources/Recipes")
                      + CountAssets("t:RecipeData",       "Assets/ScriptableObjects/Recipes");
        int candidates= CountAssets("t:NpcCandidateData", "Assets/Resources/Candidates");
        int dialogues = CountAssets("t:DialogueData",     "Assets/Resources/Dialogues");
        int buildings = CountAssets("t:BuildingData",     "Assets/Resources/Buildings");

        list.Add(items     >= 8 ? Pass(CAT, "Item 자산",      $"{items}/8+")     : Fail(CAT, "Item 자산", $"{items}/8 부족"));
        list.Add(recipes   >= 5 ? Pass(CAT, "Recipe 자산",    $"{recipes}/5+")   : Warn(CAT, "Recipe 자산", $"{recipes}/5 부족"));
        list.Add(candidates>= 4 ? Pass(CAT, "Candidate 자산", $"{candidates}/4+"): Warn(CAT, "Candidate 자산", $"{candidates}/4 부족"));
        list.Add(dialogues >= 4 ? Pass(CAT, "Dialogue 자산",  $"{dialogues}/4+") : Warn(CAT, "Dialogue 자산", $"{dialogues}/4 부족"));
        list.Add(buildings >=12 ? Pass(CAT, "BuildingData",   $"{buildings}/12") : Fail(CAT, "BuildingData", $"{buildings}/12 부족"));
    }

    // ── ⑤ 핵심 싱글톤 + 중복 검증 ───────────────────────────────────────
    static void ValidateSingletons(List<Item> list)
    {
        const string CAT = "⑤싱글톤";
        // 초기화 순서 (Docs/04 §02): EconomyService(-100), TierService(-90), GameClock(-80), ItemRegistry(-70)
        ValidateSingle<EconomyService>(list, CAT, true);
        ValidateSingle<TierService>(list, CAT, true);
        ValidateSingle<GameClock>(list, CAT, true);
        ValidateSingle<ItemRegistry>(list, CAT, true);
        ValidateSingle<HiringService>(list, CAT, true);
        ValidateSingle<FriendshipService>(list, CAT, true);
        ValidateSingle<SaveManager>(list, CAT, true);
        ValidateSingle<PlayerInputHandler>(list, CAT, true);
        ValidateSingle<AuditService>(list, CAT, true);
        ValidateSingle<GridService>(list, CAT, true);
        ValidateSingle<BuildingRegistry>(list, CAT, true);
        ValidateSingle<BuildManager>(list, CAT, true);

        // EventSystem 단일성
        var ess = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        list.Add(ess.Length == 1
            ? Pass(CAT, "EventSystem", "단일")
            : (ess.Length == 0 ? Fail(CAT, "EventSystem", "없음") : Fail(CAT, "EventSystem", $"중복 {ess.Length}")));

        // ItemRegistry.allItems 연결
        var reg = UnityEngine.Object.FindFirstObjectByType<ItemRegistry>();
        int regCount = reg != null ? (reg.allItems?.Count ?? 0) : 0;
        list.Add(regCount >= 8
            ? Pass(CAT, "ItemRegistry.allItems", $"{regCount}")
            : Fail(CAT, "ItemRegistry.allItems", $"{regCount} — 8 이상 권장"));

        // SaveManager.allBuildingTypes 연결
        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        int btCount = save != null ? (save.allBuildingTypes?.Count ?? 0) : 0;
        list.Add(btCount >= 12
            ? Pass(CAT, "Save.allBuildingTypes", $"{btCount}/12")
            : Fail(CAT, "Save.allBuildingTypes", $"{btCount}/12 부족"));

        // TierService 정의 자산 연결 검사
        var tier = UnityEngine.Object.FindFirstObjectByType<TierService>();
        int tierDefs = 0;
        if (tier != null)
        {
            // tierDefinitions 필드를 리플렉션으로 안전 조회 (이름 변동 대응)
            var f = typeof(TierService).GetField("tierDefinitions",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (f != null)
            {
                if (f.GetValue(tier) is System.Collections.IList l) tierDefs = l.Count;
            }
        }
        list.Add(tier == null
            ? Warn(CAT, "TierService 정의", "TierService 없음")
            : (tierDefs >= 2
                ? Pass(CAT, "TierService 정의", $"{tierDefs}개 등록")
                : Warn(CAT, "TierService 정의", $"{tierDefs}개 — 부족할 수 있음")));
    }

    // ── ⑥ Shop / ShopSlot ───────────────────────────────────────────────
    static void ValidateShop(List<Item> list)
    {
        const string CAT = "⑥Shop/ShopSlot";
        var shops = UnityEngine.Object.FindObjectsByType<Shop>(FindObjectsSortMode.None);
        list.Add(shops.Length >= 1
            ? (shops.Length == 1 ? Pass(CAT, "Shop 수", "1") : Warn(CAT, "Shop 수", $"중복 {shops.Length}"))
            : Fail(CAT, "Shop 수", "0"));

        var taggedShops = GameObject.FindGameObjectsWithTag("Shop");
        list.Add(taggedShops.Length >= 1
            ? Pass(CAT, "Tag=Shop", $"{taggedShops.Length}개")
            : Fail(CAT, "Tag=Shop", "0 — Shop 자동 평가 실패 가능"));

        var slots = UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None);
        list.Add(slots.Length >= 4
            ? Pass(CAT, "ShopSlot 수", $"{slots.Length}/4+")
            : Fail(CAT, "ShopSlot 수", $"{slots.Length}/4 부족"));

        // managedSlots 연결도 확인
        if (shops.Length > 0)
        {
            var s = shops[0];
            int managed = s.managedSlots?.Count ?? 0;
            list.Add(managed >= 4
                ? Pass(CAT, "Shop.managedSlots", $"{managed}")
                : Warn(CAT, "Shop.managedSlots", $"{managed} — managed 등록 필요"));
        }
    }

    // ── ⑦ Crafting / Workbench ─────────────────────────────────────────
    static void ValidateCrafting(List<Item> list)
    {
        const string CAT = "⑦제작/작업대";
        var craftUI = UnityEngine.Object.FindFirstObjectByType<CraftingUI>();
        list.Add(craftUI != null
            ? Pass(CAT, "CraftingUI", "씬 연결")
            : Fail(CAT, "CraftingUI", "씬에 없음"));

        var benches = UnityEngine.Object.FindObjectsByType<Workbench>(FindObjectsSortMode.None);
        var types = benches.Select(b => b.workbenchType).Distinct().ToList();
        list.Add(types.Count >= 4
            ? Pass(CAT, "Workbench 타입", $"{types.Count}/4 ({string.Join(",", types)})")
            : Warn(CAT, "Workbench 타입", $"{types.Count}/4 — Basic/Kitchen/Forge/Sewing 4종 권장"));
    }

    // ── ⑧ 건설 루프 (BuildManager + GridService + BuildingData) ────────
    static void ValidateBuildLoop(List<Item> list)
    {
        const string CAT = "⑧건설 루프";
        // BuildManager / BuildingRegistry / GridService 는 ⑤ 에서 검사
        // 여기는 데이터 연결 검사

        // 모든 Building Item (Blueprint) 의 buildingToBuild + prefab 연결 확인
        var blueprintGuids = AssetDatabase.IsValidFolder("Assets/Resources/Items/Blueprints")
            ? AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources/Items/Blueprints" })
            : new string[0];

        int total = blueprintGuids.Length;
        int linked = 0, unlinkedNames = 0;
        var brokenList = new List<string>();
        foreach (var g in blueprintGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var item = AssetDatabase.LoadAssetAtPath<global::Item>(path);
            if (item == null) continue;
            bool ok = item.buildingToBuild != null && item.buildingToBuild.prefab != null;
            if (ok) linked++;
            else { unlinkedNames++; brokenList.Add(item.name); }
        }
        string brokenJoin = string.Join(",", brokenList.Take(3));
        list.Add(total == 0
            ? Warn(CAT, "Blueprint 아이템", "0 — 건설 메뉴 비어있음")
            : (unlinkedNames == 0
                ? Pass(CAT, "Blueprint→BuildingData→prefab", $"{linked}/{total}")
                : Fail(CAT, "Blueprint→BuildingData→prefab", $"{linked}/{total}, 누락 {unlinkedNames} ({brokenJoin}...)")));
    }

    // ── ⑨ 씬 구조 (ScreenFader + BuildingEntrance + 맵 루트) ──────────
    static void ValidateSceneStructure(List<Item> list)
    {
        const string CAT = "⑨씬 구조";
        var fader = UnityEngine.Object.FindFirstObjectByType<ScreenFader>();
        list.Add(fader != null
            ? Pass(CAT, "ScreenFader", "씬 연결")
            : Warn(CAT, "ScreenFader", "씬에 없음 — 화면 전환 페이드 미동작"));

        var entrances = UnityEngine.Object.FindObjectsByType<BuildingEntrance>(FindObjectsSortMode.None);
        list.Add(entrances.Length >= 1
            ? Pass(CAT, "BuildingEntrance", $"{entrances.Length}개")
            : Warn(CAT, "BuildingEntrance", "0 — 실내 진입 미설정"));

        // [PA_MapRoot] 단일성
        var roots = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t != null && t.name == "[PA_MapRoot]" && t.parent == null).ToList();
        list.Add(roots.Count == 1
            ? Pass(CAT, "[PA_MapRoot]", "단일")
            : (roots.Count == 0 ? Warn(CAT, "[PA_MapRoot]", "없음 — Map/Build Layout 권장") : Fail(CAT, "[PA_MapRoot]", $"중복 {roots.Count}")));

        // [WorldBuildings] 잔재 — MapLayoutBuilder 가 정리해야 함
        var legacy = GameObject.Find("[WorldBuildings]");
        list.Add(legacy == null
            ? Pass(CAT, "[WorldBuildings] 잔재", "없음")
            : Warn(CAT, "[WorldBuildings] 잔재", "남아있음 — MapLayoutBuilder 가 다음 빌드 시 정리"));
    }

    // ── ⑩ NavMesh ──────────────────────────────────────────────────────
    static void ValidateNavMesh(List<Item> list)
    {
        const string CAT = "⑩NavMesh";
        var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        var ground = GameObject.Find("Ground");
        bool hasSurface = surfaceType != null && ground != null && ground.GetComponent(surfaceType) != null;
        list.Add(hasSurface
            ? Pass(CAT, "NavMeshSurface on Ground", "OK")
            : Warn(CAT, "NavMeshSurface on Ground", surfaceType == null ? "AI Navigation 패키지 없음" : "Ground 미연결"));

        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int withAgent = npcs.Count(n => n != null && n.GetComponent<NavMeshAgent>() != null);
        int onMesh = npcs.Count(PA_SafeSceneRepair.IsNpcNavMeshReady);
        list.Add(npcs.Length == 0
            ? Warn(CAT, "NPC 부착률", "NPC 없음")
            : (withAgent == npcs.Length ? Pass(CAT, "NPC NavMeshAgent", $"{withAgent}/{npcs.Length}") : Fail(CAT, "NPC NavMeshAgent", $"{withAgent}/{npcs.Length}")));
        list.Add(npcs.Length == 0
            ? Warn(CAT, "NPC onMesh", "NPC 없음")
            : (onMesh == npcs.Length ? Pass(CAT, "NPC onMesh", $"{onMesh}/{npcs.Length}") : Warn(CAT, "NPC onMesh", $"{onMesh}/{npcs.Length} — Bake 후 자동 보정")));
    }

    // ── ⑪ Save/Load 13주차 프리플라이트 ────────────────────────────────
    static void ValidateSaveLoadPreflight(List<Item> list)
    {
        const string CAT = "⑪Save/Load 프리플라이트";

        // F5/F9 → SaveManager 연결 (런타임 이벤트라 정적 분석은 한계 — 존재만 확인)
        var input = UnityEngine.Object.FindFirstObjectByType<PlayerInputHandler>();
        var save  = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        list.Add(input != null && save != null
            ? Pass(CAT, "F5/F9 → SaveManager 경로", "PlayerInputHandler + SaveManager 모두 존재")
            : Fail(CAT, "F5/F9 → SaveManager 경로", $"input={input!=null}, save={save!=null}"));

        // SaveData.version 상수 검증 (CurrentSaveVersion 은 private — JSON 검사로 우회)
        // SaveManager.cs 에서 const private 한 값이라 직접 못 봐도 SaveData 의 default version 으로 확인
        var dummy = new SaveData();
        // SaveData.version 기본값은 0 — 실제 SaveGame 은 CurrentSaveVersion 으로 강제 세팅
        // 그래서 여기서는 SaveManager 의 SaveGame 이 호출됐을 때 v6 가 쓰여졌는지를 파일로 검사
        string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(savePath))
        {
            try
            {
                var json = File.ReadAllText(savePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                list.Add(data.version == 6
                    ? Pass(CAT, "SaveData.version", $"v{data.version} (현재 스키마)")
                    : Warn(CAT, "SaveData.version", $"v{data.version} — v6 마이그레이션 대상"));
            }
            catch { list.Add(Warn(CAT, "SaveData.version", "JSON 파싱 실패")); }
        }
        else
        {
            list.Add(Warn(CAT, "SaveData.version", "savegame.json 없음 — F5 한 번 누르면 생성"));
        }

        // FriendshipRecord.lastDialogueDay 필드 존재 (v4 추가)
        bool hasFR = typeof(FriendshipRecord).GetField("lastDialogueDay") != null;
        list.Add(hasFR
            ? Pass(CAT, "FriendshipRecord.lastDialogueDay", "v4 필드 존재")
            : Fail(CAT, "FriendshipRecord.lastDialogueDay", "필드 누락 — v4 스키마 손상"));

        // HiredNpcRecord transform/FSM 필드 존재 (v4 추가)
        var hiredType = typeof(HiredNpcRecord);
        bool hasHasTransform = hiredType.GetField("hasTransform") != null;
        bool hasFsm = hiredType.GetField("activeFsm") != null;
        list.Add(hasHasTransform && hasFsm
            ? Pass(CAT, "HiredNpcRecord transform/FSM", "v4 필드 존재")
            : Fail(CAT, "HiredNpcRecord transform/FSM", $"hasTransform={hasHasTransform}, activeFsm={hasFsm}"));

        // ShopSlotSaveData 필드 존재 (v5 추가)
        bool hasShopSlotSave = typeof(SaveData).GetField("shopSlots") != null
            && typeof(ShopSlotSaveData).GetField("displayPrice") != null
            && typeof(ShopSlotSaveData).GetField("itemId") != null;
        list.Add(hasShopSlotSave
            ? Pass(CAT, "ShopSlotSaveData", "v5 필드 존재")
            : Fail(CAT, "ShopSlotSaveData", "필드 누락 — v5 진열대 저장 손상"));

        bool hasFirstDayProfile = typeof(SaveData).GetField("playerName") != null
            && typeof(SaveData).GetField("selectedMapId") != null
            && typeof(SaveData).GetField("firstDayPrototypeStage") != null;
        list.Add(hasFirstDayProfile
            ? Pass(CAT, "FirstDay profile", "v6 필드 존재")
            : Fail(CAT, "FirstDay profile", "필드 누락 — v6 프로토타입 저장 손상"));

        // ISaveRepository + LocalJsonSaveRepository 타입 존재
        var iRepo = Type.GetType("ISaveRepository") ?? AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .FirstOrDefault(t => t.Name == "ISaveRepository");
        var localRepo = Type.GetType("LocalJsonSaveRepository") ?? AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .FirstOrDefault(t => t.Name == "LocalJsonSaveRepository");
        list.Add(iRepo != null && localRepo != null
            ? Pass(CAT, "ISaveRepository + LocalJsonSaveRepository", "타입 존재")
            : Fail(CAT, "ISaveRepository + LocalJsonSaveRepository", $"iRepo={iRepo!=null}, localRepo={localRepo!=null}"));

        // SaveManager.LoadGameAsync 안에서 GameClock.ForceSet 이 마이그레이션 분기 외에 호출되는지
        // 정적 검사 — SaveManager.cs 텍스트를 읽어 단순 패턴 확인
        try
        {
            var smPath = "Assets/Scripts/SaveManager.cs";
            if (File.Exists(smPath))
            {
                var src = File.ReadAllText(smPath);
                bool gameClockOutsideMigration = src.Contains("GameClock.Instance.ForceSet")
                    && !System.Text.RegularExpressions.Regex.IsMatch(src,
                        @"if\s*\(\s*data\.version\s*<\s*CurrentSaveVersion\s*\)\s*\{[^}]*GameClock\.Instance\.ForceSet",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                list.Add(gameClockOutsideMigration
                    ? Pass(CAT, "GameClock.ForceSet on Load", "마이그레이션 외 호출 (v6 세이브도 시간 복구됨)")
                    : Fail(CAT, "GameClock.ForceSet on Load", "마이그레이션 분기 안에서만 호출 — v6 시간 손실"));
            }
        }
        catch { /* 정적 분석 실패는 무시 */ }
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────
    static void ValidateSingle<T>(List<Item> list, string cat, bool errorIfMissing) where T : UnityEngine.Object
    {
        var arr = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (arr.Length == 1) list.Add(Pass(cat, typeof(T).Name, "단일"));
        else if (arr.Length == 0) list.Add(errorIfMissing ? Fail(cat, typeof(T).Name, "없음") : Warn(cat, typeof(T).Name, "없음"));
        else list.Add(Fail(cat, typeof(T).Name, $"중복 {arr.Length}"));
    }

    static int CountAssets(string filter, string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return 0;
        return AssetDatabase.FindAssets(filter, new[] { folder }).Length;
    }

    static Item Pass(string cat, string label, string detail) => new Item { Category = cat, Label = label, Level = Level.Pass, Detail = detail };
    static Item Warn(string cat, string label, string detail) => new Item { Category = cat, Label = label, Level = Level.Warn, Detail = detail };
    static Item Fail(string cat, string label, string detail) => new Item { Category = cat, Label = label, Level = Level.Fail, Detail = detail };

    static string Mark(Level l) => l switch { Level.Pass => "✅", Level.Warn => "⚠", Level.Fail => "❌", _ => "?" };
}
#endif
