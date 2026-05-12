#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// P.A. 씬 전체 자동 빌더.
// 메뉴: P.A. System > Build Full Scene
//
// 실행 순서:
//   1. Resources 폴더 구조 생성
//   2. ScriptableObject 생성 (Item / TierDef / NpcProfile / Schedule / Production / Recipe)
//   3. "Shop" 태그 등록
//   4. 씬 오브젝트 배치 (Ground / Services / Shop / Workbench / Markers / NPCs)
//   5. Inspector 참조 자동 연결
//
// NavMesh Bake 는 수동으로 수행해야 합니다:
//   Hierarchy → Ground 선택 → Inspector → NavMeshSurface → Bake 버튼
public static class PA_SceneAutoBuilder
{
    // ── 경로 상수 ─────────────────────────────────────────────────────────────
    const string RES     = "Assets/Resources";
    const string TIERS   = "Assets/Resources/Tiers";
    const string ITEMS   = "Assets/Resources/Items";
    const string NPCS    = "Assets/Resources/NPCs";
    const string SCHEDS  = "Assets/Resources/Schedules";
    const string PRODS   = "Assets/Resources/Production";
    const string RECIPES = "Assets/Resources/Recipes";

    // ── 메뉴 엔트리 ───────────────────────────────────────────────────────────
    [MenuItem("P.A. System/Build Full Scene", priority = 1)]
    public static void BuildFullScene()
    {
        if (!EditorUtility.DisplayDialog("P.A. 씬 자동 빌드",
            "현재 씬에 Project P.A. 전체 오브젝트를 자동으로 생성합니다.\n\n" +
            "• ScriptableObject (Item / TierDef / NpcProfile 등) 자동 생성\n" +
            "• 씬 오브젝트 (Services / Shop / Workbench / NPC 등) 자동 배치\n" +
            "• Inspector 참조 자동 연결\n\n" +
            "이미 배치된 오브젝트가 있으면 중복 생성될 수 있습니다.",
            "빌드 시작", "취소"))
            return;

        try
        {
            Prog("폴더 생성 중...", 0.00f);
            CreateFolders();

            Prog("Item SO 생성 중...", 0.10f);
            var items = CreateItems();

            Prog("TierDefinition SO 생성 중...", 0.20f);
            var tiers = CreateTierDefs();

            Prog("NpcProfile SO 생성 중...", 0.30f);
            var profiles = CreateProfiles();

            Prog("NpcDailySchedule SO 생성 중...", 0.40f);
            var schedules = CreateSchedules();

            Prog("ProductionData SO 생성 중...", 0.50f);
            var productions = CreateProductions(items);

            Prog("RecipeData SO 생성 중...", 0.60f);
            var recipes = CreateRecipes(items);

            Prog("\"Shop\" 태그 등록 중...", 0.65f);
            EnsureTag("Shop");

            Prog("씬 오브젝트 생성 중...", 0.70f);
            BuildSceneObjects(tiers, profiles, schedules, productions, recipes);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("완료!",
                "씬 자동 빌드 완료!\n\n" +
                "마지막으로 NavMesh를 직접 Bake 해주세요:\n" +
                "  Hierarchy → Ground 선택\n" +
                "  Inspector → NavMeshSurface 컴포넌트 → Bake 버튼\n\n" +
                "Console에서 생성 로그를 확인하세요.",
                "확인");

            Debug.Log("✅ PA_SceneAutoBuilder: 씬 빌드 완료!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ PA_SceneAutoBuilder 오류: {e}");
            EditorUtility.DisplayDialog("빌드 오류",
                $"{e.Message}\n\nConsole에서 상세 스택 트레이스를 확인하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void Prog(string msg, float t) =>
        EditorUtility.DisplayProgressBar("P.A. 씬 빌더", msg, t);

    // ──────────────────────────────────────────────────────────────────────────
    // 1. 폴더 구조
    // ──────────────────────────────────────────────────────────────────────────
    static void CreateFolders()
    {
        EnsureDir("Assets", "Resources");
        EnsureDir(RES, "Tiers");
        EnsureDir(RES, "Items");
        EnsureDir(RES, "NPCs");
        EnsureDir(RES, "Schedules");
        EnsureDir(RES, "Production");
        EnsureDir(RES, "Recipes");
        EnsureDir("Assets", "Editor");
        Debug.Log("  📁 폴더 구조 생성 완료");
    }

    static void EnsureDir(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Item ScriptableObject
    //    id는 절대 변경 금지 (SaveData 직렬화 키)
    // ──────────────────────────────────────────────────────────────────────────
    static Item[] CreateItems()
    {
        var defs = new (int id, string name, int price, ItemCategory cat)[]
        {
            (1, "Wheat",     10, ItemCategory.Raw),
            (2, "Carrot",    12, ItemCategory.Raw),
            (3, "Wood",       8, ItemCategory.Raw),
            (4, "Ore",       15, ItemCategory.Raw),
            (5, "BreadLoaf", 30, ItemCategory.Processed),
            (6, "IronBar",   40, ItemCategory.Processed),
            (7, "Plank",     25, ItemCategory.Processed),
            (8, "Fish",      18, ItemCategory.Raw),
        };

        var result = new Item[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            var item = LoadOrCreate<Item>($"{ITEMS}/Item_{d.name}.asset");
            using var so = new SerializedObject(item);
            so.FindProperty("id").intValue             = d.id;
            so.FindProperty("itemName").stringValue    = d.name;
            so.FindProperty("basePrice").intValue      = d.price;
            so.FindProperty("category").enumValueIndex = (int)d.cat;
            so.FindProperty("maxStack").intValue       = 99;
            so.ApplyModifiedPropertiesWithoutUndo();
            result[i] = item;
        }
        Debug.Log($"  📦 Item {result.Length}개 생성 완료");
        return result;
    }

    static Item FindItem(Item[] items, string name) =>
        Array.Find(items, x => x != null && x.itemName == name);

    // ──────────────────────────────────────────────────────────────────────────
    // 3. TierDefinition ScriptableObject  (Tier 0~4)
    // ──────────────────────────────────────────────────────────────────────────
    static TierDefinition[] CreateTierDefs()
    {
        var defs = new (int t, string name, int slots, string stage, long rev, int rep, bool manual)[]
        {
            (0, "생존자",   4,  "가판대",  0,      0, false),
            (1, "지점장",   8,  "잡화점",  10000,  0, false),
            (2, "점주",     16, "마트",    100000, 0, false),
            (3, "지역장",   32, "백화점",  0,      3, false),
            (4, "본사임원", 64, "그룹몰",  0,      0, true),
        };

        var result = new TierDefinition[5];
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            var td = LoadOrCreate<TierDefinition>($"{TIERS}/Tier{d.t}.asset");
            using var so = new SerializedObject(td);
            so.FindProperty("tier").intValue                      = d.t;
            so.FindProperty("tierName").stringValue               = d.name;
            so.FindProperty("shopSlotCount").intValue             = d.slots;
            so.FindProperty("shopStageName").stringValue          = d.stage;
            so.FindProperty("requiredCumulativeRevenue").longValue = d.rev;
            so.FindProperty("requiredReputation").intValue         = d.rep;
            so.FindProperty("requiresManualApproval").boolValue    = d.manual;
            so.ApplyModifiedPropertiesWithoutUndo();
            result[i] = td;
        }
        Debug.Log($"  🏆 TierDefinition {result.Length}개 생성 완료");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. NpcProfile ScriptableObject
    //    traitEI/SN/TF/JP : -1(극단) ~ +1(극단)
    // ──────────────────────────────────────────────────────────────────────────
    static Dictionary<string, NpcProfile> CreateProfiles()
    {
        var defs = new (string key, string name, float ei, float sn, float tf, float jp, float eff, float soc)[]
        {
            ("Farmer",     "Farmer_01",    -0.3f,  0.5f,  0.0f, -0.5f, 1.2f, 0.4f),
            ("Miner",      "Miner_01",     -0.5f,  0.3f, -0.2f, -0.7f, 1.3f, 0.3f),
            ("Lumberjack", "Lumberjack_01", 0.0f,  0.4f,  0.1f, -0.3f, 1.1f, 0.5f),
            ("Chef",       "Chef_01",       0.2f,  0.0f,  0.5f, -0.4f, 1.2f, 0.6f),
            ("Blacksmith", "Blacksmith_01",-0.4f,  0.6f, -0.3f, -0.6f, 1.4f, 0.3f),
            ("Tailor",     "Tailor_01",     0.1f, -0.2f,  0.7f,  0.2f, 1.1f, 0.6f),
            ("Carpenter",  "Carpenter_01", -0.2f,  0.3f, -0.1f, -0.3f, 1.2f, 0.5f),
            ("Fisher",     "Fisher_01",     0.1f,  0.6f,  0.2f,  0.3f, 1.1f, 0.5f),
        };

        var result = new Dictionary<string, NpcProfile>();
        foreach (var d in defs)
        {
            var p = LoadOrCreate<NpcProfile>($"{NPCS}/Profile_{d.key}.asset");
            using var so = new SerializedObject(p);
            so.FindProperty("npcName").stringValue       = d.name;
            so.FindProperty("traitEI").floatValue        = d.ei;
            so.FindProperty("traitSN").floatValue        = d.sn;
            so.FindProperty("traitTF").floatValue        = d.tf;
            so.FindProperty("traitJP").floatValue        = d.jp;
            so.FindProperty("workEfficiency").floatValue = d.eff;
            so.FindProperty("socialWeight").floatValue   = d.soc;
            so.ApplyModifiedPropertiesWithoutUndo();
            result[d.key] = p;
        }
        Debug.Log($"  👤 NpcProfile {result.Count}개 생성 완료");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 5. NpcDailySchedule ScriptableObject
    //    NpcDailySchedule 내부의 private ContextMenu 메서드를 리플렉션으로 호출해
    //    기본 시간대 데이터를 자동으로 채운다.
    // ──────────────────────────────────────────────────────────────────────────
    static Dictionary<string, NpcDailySchedule> CreateSchedules()
    {
        // key → NpcDailySchedule 내 private 메서드 이름 (ContextMenu 어트리뷰트 붙어있음)
        var map = new Dictionary<string, string>
        {
            ["Producer"]   = "ApplyFarmerDefaults",
            ["Specialist"] = "ApplyProfessionalDefaults",
            ["Resident"]   = "ApplyResidentDefaults",
        };

        var result = new Dictionary<string, NpcDailySchedule>();
        foreach (var kv in map)
        {
            var sched = LoadOrCreate<NpcDailySchedule>($"{SCHEDS}/Schedule_{kv.Key}.asset");
            var method = typeof(NpcDailySchedule).GetMethod(
                kv.Value, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(sched, null);
            else
                Debug.LogWarning($"⚠️ NpcDailySchedule.{kv.Value} 리플렉션 실패 — 수동 설정 필요");
            EditorUtility.SetDirty(sched);
            result[kv.Key] = sched;
        }
        Debug.Log($"  📅 NpcDailySchedule {result.Count}개 생성 완료");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 6. ProductionData ScriptableObject
    // ──────────────────────────────────────────────────────────────────────────
    static Dictionary<string, ProductionData> CreateProductions(Item[] items)
    {
        // (key, itemName, interval, amount, threshold, maxInv, price)
        var defs = new (string key, string itemName, float interval, int amount, int threshold, int maxInv, int price)[]
        {
            ("Wheat", "Wheat", 10f, 5, 15, 40, 10),
            ("Ore",   "Ore",   15f, 3, 10, 30, 15),
            ("Wood",  "Wood",  12f, 4, 12, 35,  8),
            ("Fish",  "Fish",  13f, 3, 10, 30, 18),
        };

        var result = new Dictionary<string, ProductionData>();
        foreach (var d in defs)
        {
            var pd = LoadOrCreate<ProductionData>($"{PRODS}/Production_{d.key}.asset");
            using var so = new SerializedObject(pd);
            so.FindProperty("producedItem").objectReferenceValue  = FindItem(items, d.itemName);
            so.FindProperty("baseProductionInterval").floatValue  = d.interval;
            so.FindProperty("baseProductionAmount").intValue      = d.amount;
            so.FindProperty("deliveryThreshold").intValue         = d.threshold;
            so.FindProperty("maxInventoryCount").intValue         = d.maxInv;
            so.FindProperty("overrideDeliveryPrice").intValue     = d.price;
            so.ApplyModifiedPropertiesWithoutUndo();
            result[d.key] = pd;
        }
        Debug.Log($"  🌾 ProductionData {result.Count}개 생성 완료");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 7. RecipeData ScriptableObject
    //    ingredients 리스트는 공개 필드이므로 직접 할당
    // ──────────────────────────────────────────────────────────────────────────
    static Dictionary<string, RecipeData> CreateRecipes(Item[] items)
    {
        var defs = new (string key, string name, string ingItem, int ingCount, string outItem, WorkbenchType wb)[]
        {
            ("Bread",   "BreadLoaf", "Wheat", 3, "BreadLoaf", WorkbenchType.Kitchen),
            ("IronBar", "IronBar",   "Ore",   2, "IronBar",   WorkbenchType.Forge),
            ("Plank",   "Plank",     "Wood",  2, "Plank",     WorkbenchType.BasicWorkbench),
        };

        var result = new Dictionary<string, RecipeData>();
        foreach (var d in defs)
        {
            var rd = LoadOrCreate<RecipeData>($"{RECIPES}/Recipe_{d.key}.asset");
            rd.recipeName        = d.name;
            rd.outputItem        = FindItem(items, d.outItem);
            rd.outputCount       = 1;
            rd.requiredWorkbench = d.wb;
            rd.requiredTier      = 0;
            rd.baseOutputQuality = 1.2f;
            rd.ingredients       = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = FindItem(items, d.ingItem), count = d.ingCount }
            };
            EditorUtility.SetDirty(rd);
            result[d.key] = rd;
        }
        Debug.Log($"  📜 RecipeData {result.Count}개 생성 완료");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 8. "Shop" 태그 등록  (TagManager.asset 직접 수정)
    // ──────────────────────────────────────────────────────────────────────────
    static void EnsureTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var tagMgr = new SerializedObject(assets[0]);
        var tags   = tagMgr.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagMgr.ApplyModifiedProperties();
        Debug.Log($"  🏷 Tag \"{tag}\" 등록 완료");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 9. 씬 오브젝트 총괄
    // ──────────────────────────────────────────────────────────────────────────
    static void BuildSceneObjects(
        TierDefinition[] tiers,
        Dictionary<string, NpcProfile> profiles,
        Dictionary<string, NpcDailySchedule> schedules,
        Dictionary<string, ProductionData> productions,
        Dictionary<string, RecipeData> recipes)
    {
        BuildGround();
        BuildServices(tiers);
        BuildShop();
        var workbenches = BuildWorkbenches();
        var markers     = BuildMarkers();
        BuildNPCs(profiles, schedules, productions, recipes, workbenches, markers);
        Debug.Log("  🏗 씬 오브젝트 생성 완료");
    }

    // ── Ground (Plane + NavMeshSurface) ──────────────────────────────────────
    static void BuildGround()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Ground";
        go.transform.position   = Vector3.zero;
        go.transform.localScale = new Vector3(10f, 1f, 10f);

        // AI Navigation 패키지의 NavMeshSurface — 리플렉션으로 추가해 패키지 없어도 컴파일 OK
        var surfaceType =
            Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation") ??
            Type.GetType("UnityEngine.AI.NavMeshSurface");

        if (surfaceType != null)
        {
            go.AddComponent(surfaceType);
            Debug.Log("  🗺 NavMeshSurface 추가 완료 — Inspector에서 Bake 버튼을 눌러주세요!");
        }
        else
        {
            Debug.LogWarning(
                "⚠️ AI Navigation 패키지 미설치 — NavMeshSurface를 수동으로 추가하세요.\n" +
                "Window → Package Manager → Unity Registry → AI Navigation → Install");
        }

        Debug.Log("  🌍 Ground (100×100m Plane) 생성 완료");
    }

    // ── [Services] ───────────────────────────────────────────────────────────
    static void BuildServices(TierDefinition[] tiers)
    {
        var go = new GameObject("[Services]");

        // EconomyService — _money : private SerializeField → SerializedObject 필요
        var econ = go.AddComponent<EconomyService>();
        SetPrivateIntField(econ, "_money", 500);

        // TierService — tierDefinitions : private SerializeField → SerializedObject 필요
        var tierSvc = go.AddComponent<TierService>();
        {
            using var so = new SerializedObject(tierSvc);
            var listProp = so.FindProperty("tierDefinitions");
            if (listProp != null)
            {
                listProp.arraySize = tiers.Length;
                for (int i = 0; i < tiers.Length; i++)
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = tiers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // GameClock — public 필드
        var clock = go.AddComponent<GameClock>();
        clock.secondsPerGameHour = 10f;   // 테스트 속도 (실 플레이 시 60f 로 변경)
        clock.startHour          = 7f;
        clock.daysPerSeason      = 7;
        EditorUtility.SetDirty(clock);

        // GridService — public 필드
        var grid = go.AddComponent<GridService>();
        grid.cellSize = 2f;
        EditorUtility.SetDirty(grid);

        // BuildManager — public 필드
        var bm = go.AddComponent<BuildManager>();
        bm.gridSize      = 2f;
        bm.buildDistance = 2f;
        EditorUtility.SetDirty(bm);

        // AuditService — public 필드
        var audit = go.AddComponent<AuditService>();
        audit.auditIntervalDays = 3;
        EditorUtility.SetDirty(audit);

        // 나머지 서비스: 기본값 OK, 컴파일 오류 있으면 경고 후 건너뜀
        TryAdd<BuildingRegistry>(go);
        TryAdd<SaveManager>(go);
        TryAdd<HiringService>(go);
        TryAdd<FriendshipService>(go);
        TryAdd<PlayerInputHandler>(go);
        // CraftingService 는 static class — MonoBehaviour 가 아니므로 컴포넌트 추가 불필요

        Debug.Log("  ⚙️ [Services] 생성 완료 (12개 컴포넌트)");
    }

    // ── Shop + ShopSlot ───────────────────────────────────────────────────────
    static void BuildShop()
    {
        var shopGO = new GameObject("Shop");
        shopGO.transform.position = new Vector3(0f, 0f, 8f);
        shopGO.tag = "Shop";

        // 시각화 큐브 (플레이스홀더)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "ShopVisual";
        visual.transform.SetParent(shopGO.transform);
        visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        visual.transform.localScale    = new Vector3(5f, 3f, 4f);

        var slotParent = new GameObject("SlotParent");
        slotParent.transform.SetParent(shopGO.transform);
        slotParent.transform.localPosition = Vector3.zero;

        // ShopSlot 4개 (Tier0 기준)
        var slots = new ShopSlot[4];
        float[] xs = { -1.5f, -0.5f, 0.5f, 1.5f };
        for (int i = 0; i < 4; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Slot_{i}";
            cube.transform.SetParent(slotParent.transform);
            cube.transform.localPosition = new Vector3(xs[i], 0f, 3.5f);
            cube.transform.localScale    = new Vector3(0.9f, 0.1f, 0.9f);
            slots[i] = cube.AddComponent<ShopSlot>();
        }

        // Shop 컴포넌트 + managedSlots 연결 (public List — 직접 할당)
        var shopComp = shopGO.AddComponent<Shop>();
        shopComp.managedSlots = new List<ShopSlot>(slots);
        EditorUtility.SetDirty(shopComp);

        Debug.Log("  🏪 Shop + ShopSlot×4 생성 완료");
    }

    // ── Workbench 4종 ─────────────────────────────────────────────────────────
    static Dictionary<WorkbenchType, Workbench> BuildWorkbenches()
    {
        var parent = new GameObject("[Workbenches]");
        var result = new Dictionary<WorkbenchType, Workbench>();

        var defs = new (string name, WorkbenchType type, Vector3 pos)[]
        {
            ("Workbench_Kitchen",     WorkbenchType.Kitchen,        new Vector3(10f, 0.5f,  0f)),
            ("Workbench_Forge",       WorkbenchType.Forge,          new Vector3(10f, 0.5f,  5f)),
            ("Workbench_SewingTable", WorkbenchType.SewingTable,    new Vector3(10f, 0.5f, -5f)),
            ("Workbench_Basic",       WorkbenchType.BasicWorkbench, new Vector3(10f, 0.5f,-10f)),
        };

        foreach (var d in defs)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = d.name;
            cube.transform.SetParent(parent.transform);
            cube.transform.position   = d.pos;
            cube.transform.localScale = new Vector3(2f, 1f, 2f);

            var wb = cube.AddComponent<Workbench>();
            wb.workbenchType = d.type;
            wb.displayName   = d.name.Replace("Workbench_", "");
            EditorUtility.SetDirty(wb);
            result[d.type] = wb;
        }

        Debug.Log($"  🔨 Workbench {defs.Length}개 생성 완료");
        return result;
    }

    // ── 위치 마커 (WorkSpot / HomePoint) ──────────────────────────────────────
    static Dictionary<string, Transform> BuildMarkers()
    {
        var parent = new GameObject("[Markers]");
        var result = new Dictionary<string, Transform>();

        var defs = new (string key, Vector3 pos)[]
        {
            ("WorkSpot_Farmer",      new Vector3(-15f, 0f,  10f)),
            ("WorkSpot_Miner",       new Vector3( 20f, 0f,  15f)),
            ("WorkSpot_Lumberjack",  new Vector3(-20f, 0f, -10f)),
            ("HomePoint_Farmer",     new Vector3(-10f, 0f,  -5f)),
            ("HomePoint_Miner",      new Vector3( 15f, 0f,  -5f)),
            ("HomePoint_Lumberjack", new Vector3(-15f, 0f,  -8f)),
            ("HomePoint_Chef",       new Vector3(  5f, 0f,  -8f)),
            ("HomePoint_Blacksmith", new Vector3( 10f, 0f,  -8f)),
            ("HomePoint_Tailor",     new Vector3(  3f, 0f, -10f)),
            ("HomePoint_Carpenter",  new Vector3(  7f, 0f, -10f)),
            ("WorkSpot_Fisher",      new Vector3(  0f, 0f,  25f)),
            ("HomePoint_Fisher",     new Vector3(  0f, 0f,  -8f)),
        };

        foreach (var d in defs)
        {
            var go = new GameObject(d.key);
            go.transform.SetParent(parent.transform);
            go.transform.position = d.pos;
            result[d.key] = go.transform;
        }

        Debug.Log($"  📍 위치 마커 {defs.Length}개 생성 완료");
        return result;
    }

    // ── NPC 전체 ─────────────────────────────────────────────────────────────
    static void BuildNPCs(
        Dictionary<string, NpcProfile>       profiles,
        Dictionary<string, NpcDailySchedule> schedules,
        Dictionary<string, ProductionData>   productions,
        Dictionary<string, RecipeData>       recipes,
        Dictionary<WorkbenchType, Workbench> workbenches,
        Dictionary<string, Transform>        markers)
    {
        PA_NpcDuplicateGuard.PrepareForSceneAutoBuild();
        var parent = new GameObject("[NPCs]");

        // 생산형 NPC (Farmer / Miner / Lumberjack)
        var producerDefs = new (string key, Vector3 pos, NpcSpecialty spec, string prod, string ws, string hp)[]
        {
            ("Farmer",     new Vector3(-7f, 0f, 0f), NpcSpecialty.Farmer,
                "Wheat", "WorkSpot_Farmer",     "HomePoint_Farmer"),
            ("Miner",      new Vector3(-4f, 0f, 0f), NpcSpecialty.Miner,
                "Ore",   "WorkSpot_Miner",      "HomePoint_Miner"),
            ("Lumberjack", new Vector3(-1f, 0f, 0f), NpcSpecialty.Lumberjack,
                "Wood",  "WorkSpot_Lumberjack", "HomePoint_Lumberjack"),
            ("Fisher",     new Vector3( 2f, 0f, 0f), NpcSpecialty.Fisher,
                "Fish",  "WorkSpot_Fisher",     "HomePoint_Fisher"),
        };

        foreach (var d in producerDefs)
        {
            CreateProducerNpc(
                $"NPC_{d.key}", d.pos, parent.transform,
                profiles.SafeGet(d.key),
                schedules.SafeGet("Producer"),
                productions.SafeGet(d.prod),
                d.spec,
                markers.SafeGet(d.ws),
                markers.SafeGet(d.hp));
        }

        // 전문가 NPC (Chef / Blacksmith / Tailor / Carpenter)
        var specialistDefs = new (string key, Vector3 pos, NpcSpecialty spec, WorkbenchType wb, string recipe, string hp)[]
        {
            ("Chef",       new Vector3( 5f, 0f, 0f), NpcSpecialty.Chef,
                WorkbenchType.Kitchen,        "Bread",   "HomePoint_Chef"),
            ("Blacksmith", new Vector3( 8f, 0f, 0f), NpcSpecialty.Blacksmith,
                WorkbenchType.Forge,          "IronBar", "HomePoint_Blacksmith"),
            ("Tailor",     new Vector3(11f, 0f, 0f), NpcSpecialty.Tailor,
                WorkbenchType.SewingTable,    "Bread",   "HomePoint_Tailor"),
            ("Carpenter",  new Vector3(14f, 0f, 0f), NpcSpecialty.Carpenter,
                WorkbenchType.BasicWorkbench, "Plank",   "HomePoint_Carpenter"),
        };

        foreach (var d in specialistDefs)
        {
            CreateSpecialistNpc(
                $"NPC_{d.key}", d.pos, parent.transform,
                profiles.SafeGet(d.key),
                schedules.SafeGet("Specialist"),
                d.spec,
                workbenches.SafeGet(d.wb),
                recipes.SafeGet(d.recipe),
                markers.SafeGet(d.hp));
        }

        Debug.Log($"  🤖 NPC {producerDefs.Length + specialistDefs.Length}개 생성 완료 (생산형 {producerDefs.Length}, 전문가 {specialistDefs.Length})");
    }

    // ── 생산형 NPC 1개 ────────────────────────────────────────────────────────
    static void CreateProducerNpc(
        string      goName,    Vector3    pos,      Transform      parent,
        NpcProfile  profile,   NpcDailySchedule schedule,
        ProductionData prodData, NpcSpecialty specialty,
        Transform   workSpot,  Transform  homePoint)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        AddNpcBase(go);

        var consumer = go.AddComponent<NpcController>();
        consumer.profile              = profile;
        consumer.idleTickInterval     = 3f;
        consumer.wanderRadius         = 8f;
        consumer.maxSlotsPerVisit     = 3;
        consumer.browseDurationAtSlot = 1.5f;
        consumer.slotArriveDistance   = 1.2f;
        consumer.shopArriveDistance   = 1.5f;
        EditorUtility.SetDirty(consumer);

        var producer = go.AddComponent<ProducerNpcController>();
        producer.profile             = profile;
        producer.productionData      = prodData;
        producer.specialty           = specialty;
        producer.workSpot            = workSpot;
        producer.idleTickInterval    = 5f;
        producer.baseWorkProbability = 0.4f;
        producer.arriveDistance      = 1.5f;
        EditorUtility.SetDirty(producer);

        var sched = go.AddComponent<NpcScheduleController>();
        sched.profile            = profile;
        sched.scheduleData       = schedule;
        sched.consumerController = consumer;
        sched.producerController = producer;
        sched.homePoint          = homePoint;
        EditorUtility.SetDirty(sched);
    }

    // ── 전문가 NPC 1개 ────────────────────────────────────────────────────────
    static void CreateSpecialistNpc(
        string      goName,    Vector3    pos,      Transform  parent,
        NpcProfile  profile,   NpcDailySchedule schedule,
        NpcSpecialty specialty, Workbench workbench,
        RecipeData  recipe,    Transform homePoint)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        AddNpcBase(go);

        var consumer = go.AddComponent<NpcController>();
        consumer.profile              = profile;
        consumer.idleTickInterval     = 3f;
        consumer.wanderRadius         = 8f;
        consumer.maxSlotsPerVisit     = 3;
        consumer.browseDurationAtSlot = 1.5f;
        consumer.slotArriveDistance   = 1.2f;
        consumer.shopArriveDistance   = 1.5f;
        EditorUtility.SetDirty(consumer);

        var specialist = go.AddComponent<SpecialistNpcController>();
        specialist.profile             = profile;
        specialist.specialty           = specialty;
        specialist.targetWorkbench     = workbench;
        specialist.idleTickInterval    = 5f;
        specialist.baseWorkProbability = 0.5f;
        specialist.baseCraftInterval   = 20f;
        specialist.arriveDistance      = 1.5f;
        if (recipe != null)
            specialist.assignedRecipes = new List<RecipeData> { recipe };
        EditorUtility.SetDirty(specialist);

        var sched = go.AddComponent<NpcScheduleController>();
        sched.profile               = profile;
        sched.scheduleData          = schedule;
        sched.consumerController    = consumer;
        sched.specialistController  = specialist;
        sched.homePoint             = homePoint;
        EditorUtility.SetDirty(sched);
    }

    // ── NPC 공통 베이스 컴포넌트 ──────────────────────────────────────────────
    //    NavMeshAgent + CapsuleCollider + 시각화 Capsule
    static void AddNpcBase(GameObject go)
    {
        // 시각화용 Capsule 자식 (Collider는 삭제해 루트 Collider와 충돌 방지)
        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.name = "Visual";
        cap.transform.SetParent(go.transform);
        cap.transform.localPosition = new Vector3(0f, NpcPresentationNormalizer.ColliderHeight * 0.5f, 0f);
        cap.transform.localScale    = Vector3.one * (NpcPresentationNormalizer.ColliderHeight * 0.5f);
        UnityEngine.Object.DestroyImmediate(cap.GetComponent<CapsuleCollider>());

        // NavMeshAgent (NPC 이동 필수)
        var agent = go.AddComponent<NavMeshAgent>();
        agent.speed            = 2f;
        agent.angularSpeed     = 240f;
        agent.stoppingDistance = 0.2f;
        agent.height           = NpcPresentationNormalizer.AgentHeight;
        agent.radius           = NpcPresentationNormalizer.AgentRadius;
        agent.baseOffset       = 0f;

        // CapsuleCollider (루트 — 상호작용 감지)
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, NpcPresentationNormalizer.ColliderHeight * 0.5f, 0f);
        col.height = NpcPresentationNormalizer.ColliderHeight;
        col.radius = NpcPresentationNormalizer.ColliderRadius;

        var normalizer = go.AddComponent<NpcPresentationNormalizer>();
        normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
        normalizer.animatorController = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공통 유틸리티
    // ──────────────────────────────────────────────────────────────────────────

    // 에셋이 이미 있으면 로드, 없으면 생성
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var inst = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(inst, path);
        return inst;
    }

    // private [SerializeField] int 필드를 SerializedObject 로 설정
    static void SetPrivateIntField(UnityEngine.Object target, string fieldName, int value)
    {
        using var so   = new SerializedObject(target);
        var       prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"⚠️ {target.GetType().Name}.{fieldName} 필드를 찾지 못했습니다.");
        }
    }

    // 컴파일 오류 있는 컴포넌트 추가 시 예외 발생 → 경고 후 건너뜀
    static T TryAdd<T>(GameObject go) where T : Component
    {
        try   { return go.AddComponent<T>(); }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ {typeof(T).Name} 추가 실패: {e.Message}");
            return null;
        }
    }

    // Dictionary 키 없으면 null 반환 (KeyNotFoundException 방지)
    static TValue SafeGet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        where TValue : class
    {
        dict.TryGetValue(key, out var val);
        return val;
    }
}
#endif
