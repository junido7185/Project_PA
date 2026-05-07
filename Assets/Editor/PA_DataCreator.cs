using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// §7 PA DataCreator — 기능_명세서.md 미생성 에셋 일괄 보완.
// 메뉴: P.A. System > Create Missing Data Assets
//
// PA_SceneAutoBuilder / PA_DataBootstrapper 와 충돌 없이 보완:
//   • Item     id 9~15 (구운감자·생선구이·목제가구·철제도구·의류·호미·씨앗)
//   • Recipe   5종 (구운감자·생선구이·목제가구·철제도구·의류)
//   • NpcCandidateData  8종 → Assets/Resources/Candidates/
//   • DialogueData      8종 → Assets/Resources/Dialogues/
// 이미 에셋이 존재하면 스킵 (idempotent).
public static class PA_DataCreator
{
    const string ITEMS  = "Assets/Resources/Items";
    const string RECS   = "Assets/Resources/Recipes";
    const string NPCS   = "Assets/Resources/NPCs";
    const string CANDS  = "Assets/Resources/Candidates";
    const string DLGS   = "Assets/Resources/Dialogues";

    [MenuItem("P.A. System/Create Missing Data Assets", priority = 2)]
    public static void CreateAll()
    {
        EnsureDir("Assets/Resources", "Candidates");
        EnsureDir("Assets/Resources", "Dialogues");

        int n = 0;
        n += CreateMissingItems();
        n += CreateMissingRecipes();
        n += CreateCandidates();
        n += CreateDialogues();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("PA DataCreator",
            $"완료! {n}개 에셋 생성됨.\n\n" +
            "다음 단계:\n" +
            "① HiringService Inspector > availableCandidates에 Candidates/ 전체 드래그\n" +
            "② 각 NpcDialogue Inspector > dialogueData에 Dialogues/ 에셋 연결\n" +
            "③ NpcCandidateData > spawnPrefab 연결 (NPC 프리팹 준비 후)\n" +
            "④ ItemRegistry > allItems에 Items/ 하위 전체 드래그",
            "확인");
    }

    // ── Item id 9~15 (기존 1~8 미포함) ─────────────────────────────────────────
    static int CreateMissingItems()
    {
        var defs = new[]
        {
            (9,  "Item_09_BakedPotato", "구운 감자", 45,  ItemCategory.Processed, ToolType.None, 32, 0,
             "감자를 구워 만든 간식. 소박하지만 맛있다."),
            (10, "Item_10_GrilledFish", "생선구이",  70,  ItemCategory.Processed, ToolType.None, 32, 0,
             "불에 구운 생선. 진귀한 향이 난다."),
            (11, "Item_11_Furniture",   "목제 가구", 200, ItemCategory.Luxury,    ToolType.None, 8,  2,
             "목재로 정성껏 만든 가구. N 성향 NPC가 선호한다."),
            (12, "Item_12_ToolSet",     "철제 도구", 150, ItemCategory.Utility,   ToolType.None, 8,  1,
             "철괴로 만든 도구 세트. S 성향 NPC가 선호한다."),
            (13, "Item_13_Clothes",     "의류",      180, ItemCategory.Luxury,    ToolType.None, 8,  2,
             "손수 만든 옷. 품질에 따라 가격 차이가 크다."),
            (14, "Item_14_Hoe",         "호미",      0,   ItemCategory.Tool,      ToolType.Hoe,  1,  0,
             "밭을 갈 때 사용하는 도구."),
            (15, "Item_15_Seed",        "씨앗",      20,  ItemCategory.Utility,   ToolType.Seed, 64, 0,
             "심으면 작물이 자란다."),
        };

        int n = 0;
        foreach (var (id, fn, nm, price, cat, tool, stack, tier, desc) in defs)
        {
            string path = $"{ITEMS}/{fn}.asset";
            if (AssetDatabase.LoadAssetAtPath<Item>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<Item>();
            so.id = id; so.itemName = nm; so.basePrice = price;
            so.category = cat; so.toolType = tool; so.maxStack = stack;
            so.requiredTier = tier; so.description = desc;
            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA DataCreator] Item: {n}개 생성");
        return n;
    }

    // ── RecipeData 5종 (기존 Bread/IronBar/Plank 외) ────────────────────────────
    static int CreateMissingRecipes()
    {
        var specs = new[]
        {
            new RSpec("Recipe_BakedPotato",  "구운 감자 굽기",   9,  2, 1.1f,  WorkbenchType.Kitchen,
                new[]{("Carrot",2)},
                "감자 2개를 오븐에서 구운 간식."),
            new RSpec("Recipe_GrilledFish",  "생선 굽기",        10, 1, 1.3f,  WorkbenchType.Kitchen,
                new[]{("Fish",1)},
                "생선 1마리를 불에 구운 요리."),
            new RSpec("Recipe_Furniture",    "목제 가구 제작",   11, 1, 1.4f,  WorkbenchType.BasicWorkbench,
                new[]{("Plank",3)},
                "목재 3개로 만든 가구."),
            new RSpec("Recipe_ToolSet",      "철제 도구 제작",   12, 1, 1.5f,  WorkbenchType.Forge,
                new[]{("Plank",1),("IronBar",2)},
                "목재+철괴로 만든 도구 세트."),
            new RSpec("Recipe_Clothes",      "의류 제작",        13, 1, 1.35f, WorkbenchType.SewingTable,
                new[]{("Plank",1),("Wheat",2)},
                "목재+밀로 만든 의류."),
        };

        int n = 0;
        foreach (var s in specs)
        {
            string path = $"{RECS}/{s.fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeData>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<RecipeData>();
            so.recipeName = s.name; so.description = s.desc;
            so.outputCount = s.outCount; so.baseOutputQuality = s.quality;
            so.requiredWorkbench = s.workbench; so.requiredTier = 0;
            so.outputItem = FindItemById(s.outId);
            so.ingredients = new List<RecipeIngredient>();
            foreach (var (ingName, cnt) in s.ingredients)
                so.ingredients.Add(new RecipeIngredient { item = FindItemByName(ingName), count = cnt });

            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA DataCreator] RecipeData: {n}개 생성");
        return n;
    }

    // ── NpcCandidateData 8종 ────────────────────────────────────────────────────
    static int CreateCandidates()
    {
        var defs = new[]
        {
            ("Candidate_Farmer",     NpcSpecialty.Farmer,     300, "Profile_Farmer",
             "마을 농부", "부지런히 밭을 일구는 농부. 봄과 여름에 생산량이 오른다."),
            ("Candidate_Miner",      NpcSpecialty.Miner,      400, "Profile_Miner",
             "마을 광부", "깊은 광산에서 귀한 광석을 캐낸다. 겨울에도 묵묵히 일한다."),
            ("Candidate_Lumberjack", NpcSpecialty.Lumberjack, 350, "Profile_Lumberjack",
             "마을 벌목꾼", "숲에서 원목을 공급한다. 가을 수확이 특히 풍성하다."),
            ("Candidate_Fisher",     NpcSpecialty.Fisher,     300, "Profile_Fisher",
             "마을 어부", "새벽부터 낚시터를 지킨다. 여름에 가장 활발하다."),
            ("Candidate_Chef",       NpcSpecialty.Chef,       600, "Profile_Chef",
             "마을 쉐프", "주방에서 맛있는 음식을 만든다. NPC들에게 인기가 높다."),
            ("Candidate_Blacksmith", NpcSpecialty.Blacksmith, 700, "Profile_Blacksmith",
             "마을 대장장이", "대장간에서 강철 도구를 만든다. 품질을 최우선시한다."),
            ("Candidate_Tailor",     NpcSpecialty.Tailor,     650, "Profile_Tailor",
             "마을 재봉사", "재봉대에서 아름다운 의류를 제작한다. N 성향 NPC가 선호한다."),
            ("Candidate_Carpenter",  NpcSpecialty.Carpenter,  600, "Profile_Carpenter",
             "마을 목수", "공방에서 목재 가구를 만든다. 품질 좋은 목재를 선호한다."),
        };

        int n = 0;
        foreach (var (fn, sp, cost, profFile, nm, bio) in defs)
        {
            string path = $"{CANDS}/{fn}.asset";
            if (AssetDatabase.LoadAssetAtPath<NpcCandidateData>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<NpcCandidateData>();
            so.specialty = sp; so.hireCost = cost;
            so.displayName = nm; so.bio = bio;
            so.profile = AssetDatabase.LoadAssetAtPath<NpcProfile>($"{NPCS}/{profFile}.asset");
            // spawnPrefab: null — NPC 프리팹 준비 후 Inspector에서 연결
            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA DataCreator] NpcCandidateData: {n}개 생성");
        return n;
    }

    // ── DialogueData 8종 ────────────────────────────────────────────────────────
    static int CreateDialogues()
    {
        var defs = new[]
        {
            ("Dialogue_Farmer", "농부 기본 대사",
             new[]{"날씨가 좋군. 수확량이 좋을 것 같다.","계획대로만 되면 이번 철엔 흑자다."},
             new[]{"오늘도 좋은 하루네요! 밭이 싱그러워서 기분이 좋아요.","이 계절엔 어떤 작물이 잘 자랄까요?"}),
            ("Dialogue_Miner", "광부 기본 대사",
             new[]{"광맥이 예상보다 깊더군. 분석이 필요해.","채굴 효율을 높이려면 더 나은 도구가 필요하다."},
             new[]{"오늘도 반짝이는 광석을 찾았어요! 운이 좋은 날이에요.","광산 깊은 곳은 무섭지만 보석을 찾으면 다 잊혀요."}),
            ("Dialogue_Lumberjack", "벌목꾼 기본 대사",
             new[]{"이 숲은 자원이 풍부하다. 계획적으로 벌목해야 해.","오늘은 단단한 원목 위주로 가자."},
             new[]{"숲이 좋아서 이 일을 한답니다. 나무 냄새가 참 좋아요.","가을 단풍 속에서 일하면 기분이 최고예요!"}),
            ("Dialogue_Fisher", "어부 기본 대사",
             new[]{"조류를 분석하면 어군 위치를 알 수 있다.","오늘 조건으로는 북쪽 해역이 유리하겠어."},
             new[]{"바다 냄새 너무 좋아요! 오늘도 파도가 예쁘네요.","낚시는 기다리는 여유가 있어서 좋아요. 느긋하게요."}),
            ("Dialogue_Chef", "쉐프 기본 대사",
             new[]{"최고의 요리는 최상의 재료에서 나온다. 품질이 핵심이야.","이번 레시피는 효율성을 높인 버전이야."},
             new[]{"맛있는 음식은 모두를 행복하게 해요! 함께 드실래요?","새로운 레시피를 생각하면 설레어요!"}),
            ("Dialogue_Blacksmith", "대장장이 기본 대사",
             new[]{"쇠는 정확한 온도에서 두드려야 한다. 원칙이 전부야.","이번 철괴 품질이 마음에 드는군."},
             new[]{"쨍그랑! 이 소리가 제일 좋아요. 멋진 도구가 완성될 때 뿌듯해요.","좋은 도구는 사람의 마음을 설레게 해요."}),
            ("Dialogue_Tailor", "재봉사 기본 대사",
             new[]{"직물의 조직을 분석하면 내구성이 보인다.","이 소재는 특정 온도에서 가장 잘 늘어난다."},
             new[]{"이 원단 너무 예쁘지 않나요? 만지는 것만으로도 행복해요.","당신에게 어울리는 옷을 만들어드리고 싶어요!"}),
            ("Dialogue_Carpenter", "목수 기본 대사",
             new[]{"목재의 결을 따라 다듬어야 단단한 가구가 된다.","이번 의자는 하중 분산 설계를 적용했다."},
             new[]{"나무는 살아있는 것 같아요. 다듬을 때 이야기를 들어요.","완성된 가구를 손님이 기뻐하면 그게 제일 보람차요!"}),
        };

        int n = 0;
        foreach (var (fn, label, thinkLines, feelLines) in defs)
        {
            string path = $"{DLGS}/{fn}.asset";
            if (AssetDatabase.LoadAssetAtPath<DialogueData>(path) != null) continue;

            var so = ScriptableObject.CreateInstance<DialogueData>();
            so.label = label;
            var pool = new TopicDialoguePool
            {
                topic        = DialogueTopic.Greeting,
                thinkingLines = new List<string>(thinkLines),
                feelingLines  = new List<string>(feelLines),
            };
            so.topics = new List<TopicDialoguePool> { pool };
            AssetDatabase.CreateAsset(so, path);
            n++;
        }
        Debug.Log($"[PA DataCreator] DialogueData: {n}개 생성");
        return n;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────────

    static Item FindItemById(int id)
    {
        foreach (var g in AssetDatabase.FindAssets("t:Item", new[]{ITEMS}))
        {
            var it = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g));
            if (it != null && it.id == id) return it;
        }
        return null;
    }

    static Item FindItemByName(string itemName)
    {
        foreach (var g in AssetDatabase.FindAssets("t:Item", new[]{ITEMS}))
        {
            var it = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g));
            if (it != null && it.itemName == itemName) return it;
        }
        return null;
    }

    static void EnsureDir(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    class RSpec
    {
        public string fileName, name, desc;
        public int outId, outCount;
        public float quality;
        public WorkbenchType workbench;
        public (string name, int count)[] ingredients;
        public RSpec(string fn, string nm, int oid, int oc, float q, WorkbenchType wb,
                     (string,int)[] ing, string d)
        { fileName=fn; name=nm; outId=oid; outCount=oc; quality=q; workbench=wb; ingredients=ing; desc=d; }
    }
}
