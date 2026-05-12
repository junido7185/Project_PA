#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 🚀 PA_VerticalSlice — "버튼 한 번 = 플레이 가능" 통합 빌더
//
// 설계 의도 (Docs/06 개발 로드맵):
// - PA_DevConsole 의 Action_FullRebuild 가 9단계를 돌리고 있지만, 그 후 빈 인벤토리/돈 상태로
//   시작되어 게임 루프 검증에 5~10분이 더 걸린다.
// - 이 클래스는 "9단계 빌드 + Demo Seed" 를 한 번에 묶어서, 클릭 한 번으로 5분 안에 다음을 시연:
//     플레이어 이동 → 인벤토리/핫바 → 진열 → 가격 책정 → NPC 방문 → 구매 → 매출 → 티어 → 폰 → 작업대 → 창고
// - 기존 빌더(SceneAutoBuilder/UIBuilder/ContentAutoIntegrator) 와 모두 호환되며,
//   중복 생성 없이 멱등(idempotent) 동작. 이미 만들어진 객체는 재활용한다.
//
// 메뉴 위치:
//   P.A. System / 🚀 Build Vertical Slice + Demo Seed   (priority -10, 최상단)
//   P.A. System / 🌱 Demo Seed Only                      (PA_DemoSeed 가 등록)
public static class PA_VerticalSlice
{
    [MenuItem("P.A. System/🚀 Build Vertical Slice + Demo Seed", priority = -10)]
    public static void Build()
    {
        bool ok = EditorUtility.DisplayDialog(
            "🚀 수직 슬라이스 빌드 + Demo Seed",
            "다음을 한 번에 진행합니다:\n\n" +
            "  1. 데이터 부트스트랩 (Tier/Item/Recipe/NpcProfile)\n" +
            "  2. 누락 데이터 보완 (Item·Recipe·Candidate·Dialogue)\n" +
            "  3. 씬 자동 빌드 (Ground/Services/Player/Camera/UI/Shop/Workbench/NPC)\n" +
            "  4. 빌딩 콘텐츠 통합 (B01~B12 Prefab/BuildingData/Blueprint)\n" +
            "  5. UI 자동 구축 (Canvas/Hotbar/Inventory/Smartphone)\n" +
            "  6. 한글 폰트 강제 적용 (Jalnan2)\n" +
            "  7. 자동 수리 (Singleton/EventSystem/NavMesh/...)\n" +
            "  8. 씬 검증 (콘솔)\n" +
            "  🌱 Demo Seed (50000G + Hotbar/Inventory + Shop 진열)\n\n" +
            "약 8~12초. 기존 객체는 재활용(중복 생성 없음).",
            "시작", "취소");
        if (!ok) return;

        try
        {
            EditorApplication.LockReloadAssemblies();
            Debug.Log("🚀 [VerticalSlice] ─── 시작 ───");

            // 1. Data Bootstrap (PA_DataBootstrapper.BootstrapAll 은 internal/private static 일 수 있어 리플렉션)
            InvokeOptional("PA_DataBootstrapper", "BootstrapAll");
            Debug.Log("✅ [VerticalSlice] 1/8 Data Bootstrap");

            // 2. Missing Data
            PA_DataCreator.CreateAll();
            Debug.Log("✅ [VerticalSlice] 2/8 Missing Data");

            // 3. Scene Auto Build
            PA_SceneAutoBuilder.BuildFullScene();
            Debug.Log("✅ [VerticalSlice] 3/8 Scene Auto Build");

            // 4. Building Content (B01~B12)
            PA_ContentAutoIntegrator.BuildAll(showDialog: false);
            Debug.Log("✅ [VerticalSlice] 4/8 B01~B12 Content");

            // 4.5 Map Layout (Docs/08 §마을 레이아웃) — 멱등
            PA_MapLayoutBuilder.Build(showDialog: false);
            Debug.Log("✅ [VerticalSlice] 4.5/8 Map Layout");

            // 5. UI System
            PA_UIBuilder.BuildUISystem();
            Debug.Log("✅ [VerticalSlice] 5/8 UI System");

            // 6. Korean Font (Jalnan2)
            PA_TMPFontFixer.Apply();
            Debug.Log("✅ [VerticalSlice] 6/8 Font Fix");

            // 7. Auto-fix (NavMesh Bake 포함)
            int fixedCount = MiniAutoFix();
            Debug.Log($"✅ [VerticalSlice] 7/8 Auto-Fix ({fixedCount}건)");

            // 8. Validate
            PA_SceneValidator.Validate();
            Debug.Log("✅ [VerticalSlice] 8/8 Validate");

            // 🌱 Demo Seed
            int seeded = PA_DemoSeed.Apply();
            Debug.Log($"🌱 [VerticalSlice] Demo Seed ({seeded}건)");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "✅ 수직 슬라이스 완료",
                $"빌드 + Demo Seed 완료.\n  자동수리: {fixedCount}건\n  Seed: {seeded}건\n\n" +
                "▶ Play 버튼을 누르고 다음을 확인:\n" +
                "  • WASD 이동, I 인벤토리, P 폰, ESC 일시정지\n" +
                "  • 진열대 앞 Space → 가격 책정 UI\n" +
                "  • NPC 가 자동으로 와서 구매\n" +
                "  • 작업대 앞 Space → 제작 UI\n" +
                "  • 창고 앞 Space → 수납 UI",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [VerticalSlice] 실패: {e}");
            EditorUtility.DisplayDialog("❌ 실패", e.Message + "\n\nConsole 스택 확인", "확인");
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    // PA_DevConsole.AutoFixAll 과 같은 효과의 경량 버전 — public 만 호출.
    // (AutoFixAll 은 인스턴스 메서드라 직접 부르려면 EditorWindow 인스턴스가 필요)
    static int MiniAutoFix()
    {
        int n = 0;

        // TimeScale
        if (!Mathf.Approximately(Time.timeScale, 1f))
        {
            Time.timeScale = 1f; n++;
        }

        // EventSystem 단일화
        var ess = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsSortMode.None);
        for (int i = 1; i < ess.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(ess[i].gameObject); n++;
        }
        if (ess.Length == 0)
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null) go.AddComponent(moduleType);
            else go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            n++;
        }

        // ContentAutoIntegrator 의 Repair (멱등) — Building 참조 재연결
        n += PA_ContentAutoIntegrator.RepairGeneratedContent(showDialog: false);
        n += PA_MapLayoutBuilder.Build(showDialog: false);

        // NavMesh Bake (PA_DevConsole 패턴 그대로 — 리플렉션)
        n += TryBakeNavMesh();

        return n;
    }

    static int TryBakeNavMesh()
    {
        try
        {
            var ground = GameObject.Find("Ground");
            if (ground == null) return 0;

            var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null) return 0;

            var surface = ground.GetComponent(surfaceType);
            int added = 0;
            if (surface == null)
            {
                surface = ground.AddComponent(surfaceType); added = 1;
            }

            var bakeMethod = surfaceType.GetMethod("BuildNavMesh");
            if (bakeMethod == null) return added;

            bakeMethod.Invoke(surface, null);
            EditorUtility.SetDirty(ground);
            return added + 1;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VerticalSlice] NavMesh Bake 실패: {e.Message}");
            return 0;
        }
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

    // ──────────────────────────────────────────────────────────────────────
    //  검증 — 수직 슬라이스 핵심 항목 (DevConsole 진단을 보강)
    // ──────────────────────────────────────────────────────────────────────
    [MenuItem("P.A. System/🔎 Validate Vertical Slice", priority = -8)]
    public static void ValidateMenu()
    {
        var report = ValidateAll();
        var msg = string.Join("\n", report);
        Debug.Log("🔎 [VerticalSlice] 검증 결과:\n" + msg);
        EditorUtility.DisplayDialog("🔎 수직 슬라이스 검증", msg, "확인");
    }

    public static System.Collections.Generic.List<string> ValidateAll()
    {
        var list = new System.Collections.Generic.List<string>();

        // 1) 필수 싱글톤
        list.Add(CheckSingle<EconomyService>());
        list.Add(CheckSingle<TierService>());
        list.Add(CheckSingle<GameClock>());
        list.Add(CheckSingle<ItemRegistry>());
        list.Add(CheckSingle<HiringService>());
        list.Add(CheckSingle<SaveManager>());
        list.Add(CheckSingle<PlayerInputHandler>());

        // 2) Shop / ShopSlot
        var shops = UnityEngine.Object.FindObjectsByType<Shop>(FindObjectsSortMode.None);
        if (shops.Length == 0) list.Add("❌ Shop: 씬에 없음");
        else
        {
            int totalSlots = shops.Sum(s => s.GetComponentsInChildren<ShopSlot>(true).Length);
            list.Add($"✅ Shop: {shops.Length}개, ShopSlot {totalSlots}개");
        }

        // 3) Workbench 4종
        var benches = UnityEngine.Object.FindObjectsByType<Workbench>(FindObjectsSortMode.None);
        var types = benches.Select(b => b.workbenchType).Distinct().ToList();
        string benchMark = types.Count >= 1 ? "✅" : "⚠";
        list.Add($"{benchMark} Workbench: {benches.Length}개 ({string.Join(",", types)})");

        // 4) SaveManager.allBuildingTypes
        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        int btCount = save != null ? (save.allBuildingTypes?.Count ?? 0) : 0;
        string btMark = btCount >= 12 ? "✅" : "⚠";
        list.Add($"{btMark} SaveManager.allBuildingTypes: {btCount}/12");

        // 5) ItemRegistry.allItems
        var reg = UnityEngine.Object.FindFirstObjectByType<ItemRegistry>();
        int itemCount = reg != null ? (reg.allItems?.Count ?? 0) : 0;
        string itemMark = itemCount >= 8 ? "✅" : "⚠";
        list.Add($"{itemMark} ItemRegistry.allItems: {itemCount}");

        // 6) NPC NavMeshAgent 위 배치
        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int onMesh = 0, withAgent = 0;
        foreach (var n in npcs)
        {
            var agent = n.GetComponent<NavMeshAgent>();
            if (agent != null) withAgent++;
            if (agent != null && agent.isOnNavMesh) onMesh++;
        }
        string npcMark = (npcs.Length > 0 && onMesh == npcs.Length) ? "✅" : "⚠";
        list.Add($"{npcMark} NPC: {npcs.Length}개 (Agent {withAgent}, onMesh {onMesh})");

        // 7) UI 핵심 컴포넌트
        int npcDuplicates = PA_NpcDuplicateGuard.CountGeneratedBaseNpcDuplicates();
        list.Add(npcDuplicates == 0
            ? "??NPC generated duplicates: 0"
            : $"??NPC generated duplicates: {npcDuplicates}");

        list.Add(CheckSingle<HotbarUI>());
        list.Add(CheckSingle<InventoryUI>());
        list.Add(CheckSingle<SmartphoneUI>());
        list.Add(CheckSingle<MoneyHUD>());
        list.Add(CheckSingle<DialogueUI>());

        return list;
    }

    static string CheckSingle<T>() where T : UnityEngine.Object
    {
        var arr = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (arr.Length == 0)  return $"❌ {typeof(T).Name}: 없음";
        if (arr.Length == 1)  return $"✅ {typeof(T).Name}";
        return $"⚠ {typeof(T).Name}: 중복 {arr.Length}";
    }
}
#endif
