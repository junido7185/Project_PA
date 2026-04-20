using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// §7. 에셋 — ScriptableObject 인스턴스 일괄 생성 (기능_명세서.md 우선순위 1번)
//
// 메뉴: P.A. System > Bootstrap All Data Assets
//
// 생성 목록:
//   Assets/ScriptableObjects/Tiers/    — TierDefinition × 5 (Tier0~Tier4)
//   Assets/ScriptableObjects/Items/    — Item × 15종 (Raw 5 / Processed 5 / Product 3 / Tool 2)
//   Assets/ScriptableObjects/Recipes/  — RecipeData × 6
//   Assets/ScriptableObjects/NpcProfiles/ — NpcProfile × 5 (기본 MBTI 샘플)
//
// 이미 존재하는 에셋은 덮어쓰지 않는다 (idempotent).
// 아이콘·참조 연결은 수동으로 Inspector에서 추가한다.
public static class PA_DataBootstrapper
{
    const string ROOT = "Assets/ScriptableObjects";

    // ─────────────────────────────────────────────────────────
    [MenuItem("P.A. System/Bootstrap All Data Assets")]
    static void BootstrapAll()
    {
        int created = 0;

        EnsureFolder(ROOT);
        EnsureFolder(ROOT + "/Tiers");
        EnsureFolder(ROOT + "/Items");
        EnsureFolder(ROOT + "/Recipes");
        EnsureFolder(ROOT + "/NpcProfiles");

        created += CreateTiers();
        created += CreateItems();
        created += CreateRecipes();
        created += CreateNpcProfiles();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "P.A. Data Bootstrap",
            $"완료! {created}개 에셋 생성됨.\n\n" +
            "다음 단계:\n" +
            "① TierService > Tier Definitions 리스트에 Tier0~4 에셋 드래그\n" +
            "② ItemRegistry > All Items 에 Items/ 하위 전체 드래그\n" +
            "③ 각 RecipeData > ingredients Item 참조 연결\n" +
            "④ 각 ShopSlot > displayItem 연결 테스트",
            "확인");
    }

    // ─── Tier Definitions ────────────────────────────────────
    static int CreateTiers()
    {
        int n = 0;

        // (tier, tierName, revenue, reputation, manualApproval, shopCount, stageName, unlock, description)
        var defs = new (int t, string name, long rev, int rep, bool manual, int slots, string stage, string unlock, string desc)[]
        {
            (0, "생존자",   0,      0, false, 4,  "가판대",
             "가판대 × 4슬롯 운영 가능.\n원목·광석·작물 등 원자재 판매 허용.",
             "초기 등급. 본사 파이오니아 프로그램 참가자."),

            (1, "지점장",   10_000, 0, false, 0,  "",
             "잡화점 건물 허용. 전문가 NPC 1명 채용 가능.\n(슬롯 수는 Tier0 유지 — Tier2에서 확장)",
             "초기 대출 10,000G 상환 기준으로 자동 승급."),

            (2, "매니저",   100_000,0, false, 8,  "잡화점",
             "잡화점 × 8슬롯 확장.\n가공품(음식·가구) 판매 허용. 전문가 NPC 3명 채용 가능.",
             "누적 매출 100,000G 달성 시 자동 승급."),

            (3, "본부장",   0,      3, false, 12, "마트",
             "마트 × 12슬롯. 사치품·의류 판매 허용.\n특수 레시피 전문가 NPC 해금.",
             "마을 평판 Lv.3 이상일 때 자동 승급."),

            (4, "총괄이사", 0,      0, true,  20, "백화점",
             "백화점 × 20슬롯 풀오픈.\n모든 NPC 채용 가능. 최종 등급.",
             "본사 감사 통과 이벤트 (수동 승급 전용)."),
        };

        foreach (var d in defs)
        {
            string path = $"{ROOT}/Tiers/TierDef_Tier{d.t}.asset";
            if (AssetDatabase.LoadAssetAtPath<TierDefinition>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<TierDefinition>();
            so.tier                     = d.t;
            so.tierName                 = d.name;
            so.description              = d.desc;
            so.requiredCumulativeRevenue= d.rev;
            so.requiredReputation       = d.rep;
            so.requiresManualApproval   = d.manual;
            so.shopSlotCount            = d.slots;
            so.shopStageName            = d.stage;
            so.unlockDescription        = d.unlock;

            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA Bootstrap] TierDefinition: {n}개 생성");
        return n;
    }

    // ─── Items ───────────────────────────────────────────────
    static int CreateItems()
    {
        int n = 0;

        // (id, name, price, category, maxStack, toolType, reqTier, description)
        var items = new (int id, string name, int price, ItemCategory cat, int stack, ToolType tool, int tier, string desc)[]
        {
            // ─ Raw 원자재 ─
            (1,  "원목",       50,  ItemCategory.Raw,       64, ToolType.None,    0, "베어낸 통나무. 목재로 가공할 수 있다."),
            (2,  "광석",       80,  ItemCategory.Raw,       64, ToolType.None,    0, "채굴한 원석. 철괴로 가공할 수 있다."),
            (3,  "밀",         30,  ItemCategory.Raw,       64, ToolType.None,    0, "수확한 밀. 빵으로 가공할 수 있다."),
            (4,  "감자",       25,  ItemCategory.Raw,       64, ToolType.None,    0, "수확한 감자. 구워먹으면 든든하다."),
            (5,  "생선",       60,  ItemCategory.Raw,       64, ToolType.None,    0, "낚시로 잡은 생선. 구우면 맛이 배가된다."),

            // ─ Processed 가공품 ─
            (6,  "목재",      120,  ItemCategory.Processed, 32, ToolType.None,    0, "원목을 가공한 판재. 가구·건설 재료."),
            (7,  "철괴",      180,  ItemCategory.Processed, 32, ToolType.None,    0, "광석을 제련한 철. 도구·건물 재료."),
            (8,  "빵",         90,  ItemCategory.Processed, 32, ToolType.None,    0, "밀로 구운 빵. NPC들이 좋아한다."),
            (9,  "구운 감자",  75,  ItemCategory.Processed, 32, ToolType.None,    0, "감자를 구워 만든 간식."),
            (10, "생선구이",  150,  ItemCategory.Processed, 32, ToolType.None,    0, "불에 구운 생선. 진귀한 향이 난다."),

            // ─ Luxury·Utility 완제품 ─
            (11, "목제 가구",  500,  ItemCategory.Luxury,   8,  ToolType.None,    2, "목재로 만든 가구. N 성향 NPC가 선호한다."),
            (12, "철제 도구",  400,  ItemCategory.Utility,  8,  ToolType.None,    1, "철괴로 만든 도구 세트. S 성향 NPC가 선호한다."),
            (13, "의류",       350,  ItemCategory.Luxury,   8,  ToolType.None,    2, "손수 만든 옷. 품질에 따라 가격 차이가 크다."),

            // ─ Tool 플레이어 전용 도구 ─
            (14, "호미",       200,  ItemCategory.Tool,     1,  ToolType.Hoe,     0, "밭을 갈 때 사용하는 도구. 판매 불가."),
            (15, "씨앗",        20,  ItemCategory.Utility,  64, ToolType.Seed,    0, "심으면 작물이 자란다."),
        };

        foreach (var d in items)
        {
            string path = $"{ROOT}/Items/Item_{d.id:D2}_{SanitizeName(d.name)}.asset";
            if (AssetDatabase.LoadAssetAtPath<Item>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<Item>();
            so.id          = d.id;
            so.itemName    = d.name;
            so.description = d.desc;
            so.basePrice   = d.price;
            so.category    = d.cat;
            so.maxStack    = d.stack;
            so.toolType    = d.tool;
            so.requiredTier= d.tier;

            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA Bootstrap] Item: {n}개 생성");
        return n;
    }

    // ─── Recipes ─────────────────────────────────────────────
    static int CreateRecipes()
    {
        int n = 0;

        // 레시피는 Item 참조가 필요하므로 먼저 Item 에셋을 로드한다.
        // 에셋이 없으면 null로 두고 경고 — 수동으로 연결한다.
        var recipes = new[]
        {
            new RecipeSpec
            {
                fileName    = "Recipe_원목가공",
                recipeName  = "원목 가공",
                description = "원목 2개로 목재 1개를 제작한다. 가장 기본적인 가공 레시피.",
                outputId    = 6,  // 목재
                outputCount = 1,
                baseQuality = 1.2f,
                qualityWeight = 0.3f,
                reqTier     = 0,
                workbench   = WorkbenchType.BasicWorkbench,
                ingredients = new[] { (1, 2) }  // 원목×2
            },
            new RecipeSpec
            {
                fileName    = "Recipe_광석제련",
                recipeName  = "광석 제련",
                description = "광석 3개를 용광로에서 제련해 철괴 1개를 만든다.",
                outputId    = 7,  // 철괴
                outputCount = 1,
                baseQuality = 1.3f,
                qualityWeight = 0.3f,
                reqTier     = 1,
                workbench   = WorkbenchType.Forge,
                ingredients = new[] { (2, 3) }  // 광석×3
            },
            new RecipeSpec
            {
                fileName    = "Recipe_빵굽기",
                recipeName  = "빵 굽기",
                description = "밀 3개로 빵 2개를 만든다. NPC들에게 인기 있다.",
                outputId    = 8,  // 빵
                outputCount = 2,
                baseQuality = 1.2f,
                qualityWeight = 0.25f,
                reqTier     = 0,
                workbench   = WorkbenchType.Kitchen,
                ingredients = new[] { (3, 3) }  // 밀×3
            },
            new RecipeSpec
            {
                fileName    = "Recipe_구운감자",
                recipeName  = "구운 감자",
                description = "감자 2개를 구워 구운 감자 2개를 만든다.",
                outputId    = 9,  // 구운 감자
                outputCount = 2,
                baseQuality = 1.1f,
                qualityWeight = 0.2f,
                reqTier     = 0,
                workbench   = WorkbenchType.Kitchen,
                ingredients = new[] { (4, 2) }  // 감자×2
            },
            new RecipeSpec
            {
                fileName    = "Recipe_생선구이",
                recipeName  = "생선 굽기",
                description = "생선 1마리를 구워 생선구이 1개를 만든다. 품질 상승폭이 크다.",
                outputId    = 10,  // 생선구이
                outputCount = 1,
                baseQuality = 1.3f,
                qualityWeight = 0.4f,
                reqTier     = 0,
                workbench   = WorkbenchType.Kitchen,
                ingredients = new[] { (5, 1) }  // 생선×1
            },
            new RecipeSpec
            {
                fileName    = "Recipe_철제도구",
                recipeName  = "철제 도구 제작",
                description = "목재 1개와 철괴 2개로 철제 도구를 만든다. S 성향 NPC가 선호.",
                outputId    = 12,  // 철제 도구
                outputCount = 1,
                baseQuality = 1.5f,
                qualityWeight = 0.35f,
                reqTier     = 1,
                workbench   = WorkbenchType.BasicWorkbench,
                ingredients = new[] { (6, 1), (7, 2) }  // 목재×1, 철괴×2
            },
        };

        foreach (var spec in recipes)
        {
            string path = $"{ROOT}/Recipes/{spec.fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeData>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<RecipeData>();
            so.recipeName          = spec.recipeName;
            so.description         = spec.description;
            so.outputCount         = spec.outputCount;
            so.baseOutputQuality   = spec.baseQuality;
            so.ingredientQualityWeight = spec.qualityWeight;
            so.requiredTier        = spec.reqTier;
            so.requiredWorkbench   = spec.workbench;
            so.ingredients         = new List<RecipeIngredient>();

            // outputItem 연결
            so.outputItem = FindItemById(spec.outputId);
            if (so.outputItem == null)
                Debug.LogWarning($"[PA Bootstrap] Recipe '{spec.recipeName}' — outputItem(id={spec.outputId}) 못 찾음. 수동 연결 필요.");

            // 재료 연결
            foreach (var (itemId, count) in spec.ingredients)
            {
                var ingredient = new RecipeIngredient
                {
                    item  = FindItemById(itemId),
                    count = count
                };
                if (ingredient.item == null)
                    Debug.LogWarning($"[PA Bootstrap] Recipe '{spec.recipeName}' — ingredient(id={itemId}) 못 찾음. 수동 연결 필요.");
                so.ingredients.Add(ingredient);
            }

            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA Bootstrap] RecipeData: {n}개 생성");
        return n;
    }

    // ─── NPC Profiles (5종 MBTI 샘플) ───────────────────────
    static int CreateNpcProfiles()
    {
        int n = 0;

        // (fileName, name, EI, SN, TF, JP, workEff, social, homeStay, utility, luxury, priceSens, bio)
        var profiles = new[]
        {
            // 소비형 NPC: 실용적 구매자 (ISTJ 성향)
            ("NpcProfile_Mira",   "미라",   -0.6f, -0.5f, -0.4f, -0.6f, 1.1f, 0.3f, 0.4f, 1.4f, 0.7f, 1.2f,
             "마을 창고지기. 실용적이고 꼼꼼하다. 특가 도구는 절대 놓치지 않는다."),
            // 소비형 NPC: 감성 소비자 (ENFP 성향)
            ("NpcProfile_Jun",    "준",      0.7f,  0.6f,  0.7f,  0.5f, 0.8f, 0.8f, 0.1f, 0.6f, 1.5f, 0.7f,
             "마을 예술가. 독특하고 아름다운 것에 지갑을 연다. 가격에 관대한 편."),
            // 생산형 NPC: 나무꾼 (ISTP 성향)
            ("NpcProfile_Arlo",   "아를로",  -0.5f, -0.3f, -0.6f,  0.4f, 1.3f, 0.2f, 0.3f, 1.2f, 0.8f, 1.0f,
             "마을 나무꾼. 말이 적고 근면하다. 계절 변화에 민감하게 반응한다."),
            // 생산형 NPC: 어부 (ISFP 성향)
            ("NpcProfile_Tara",   "타라",   -0.4f, -0.2f,  0.5f,  0.3f, 1.2f, 0.4f, 0.3f, 1.0f, 1.1f, 0.9f,
             "마을 어부. 감성적이고 자연을 사랑한다. 생선 요리에 특화되어 있다."),
            // 전문가 NPC: 대장장이 (ENTJ 성향)
            ("NpcProfile_Felix",  "펠릭스",  0.5f,  0.3f, -0.7f, -0.7f, 1.4f, 0.5f, 0.2f, 1.1f, 0.9f, 1.3f,
             "마을 대장장이. 효율과 품질을 최우선시한다. 수주량이 많아 바쁘다."),
        };

        foreach (var (fileName, name, ei, sn, tf, jp, weff, soc, home, util, lux, sens, bio) in profiles)
        {
            string path = $"{ROOT}/NpcProfiles/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<NpcProfile>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<NpcProfile>();
            so.npcName         = name;
            so.bio             = bio;
            so.traitEI         = ei;
            so.traitSN         = sn;
            so.traitTF         = tf;
            so.traitJP         = jp;
            so.workEfficiency  = weff;
            so.socialWeight    = soc;
            so.homeStayBias    = home;
            so.utilityConsumption = util;
            so.luxuryConsumption  = lux;
            so.priceSensitivity   = sens;

            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA Bootstrap] NpcProfile: {n}개 생성");
        return n;
    }

    // ─── 헬퍼 ────────────────────────────────────────────────

    // 이미 생성된 Item SO 에셋을 id 로 찾는다.
    static Item FindItemById(int id)
    {
        string[] guids = AssetDatabase.FindAssets("t:Item", new[] { ROOT + "/Items" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
            if (item != null && item.id == id) return item;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string folder = path.Substring(lastSlash + 1);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    static string SanitizeName(string s)
        => s.Replace(" ", "_").Replace("/", "-");

    // 재료 스펙 내부 클래스
    class RecipeSpec
    {
        public string       fileName;
        public string       recipeName;
        public string       description;
        public int          outputId;
        public int          outputCount;
        public float        baseQuality;
        public float        qualityWeight;
        public int          reqTier;
        public WorkbenchType workbench;
        public (int id, int count)[] ingredients;
    }
}
