using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu: P.A. System > Create Missing Data Assets
// Idempotent data creator/updater for the early Project P.A. economy dataset.
public static class PA_DataCreator
{
    const string ITEMS = "Assets/Resources/Items";
    const string RECS = "Assets/Resources/Recipes";
    const string NPCS = "Assets/Resources/NPCs";
    const string CANDS = "Assets/Resources/Candidates";
    const string DLGS = "Assets/Resources/Dialogues";

    [MenuItem("P.A. System/Create Missing Data Assets", priority = 2)]
    public static void CreateAll()
    {
        EnsureDir("Assets", "Resources");
        EnsureDir("Assets/Resources", "Items");
        EnsureDir("Assets/Resources", "Recipes");
        EnsureDir("Assets/Resources", "Candidates");
        EnsureDir("Assets/Resources", "Dialogues");

        int changed = 0;
        changed += UpsertBalancedItems();
        AssetDatabase.SaveAssets();
        changed += UpsertBalancedRecipes();
        changed += CreateCandidates();
        changed += CreateDialogues();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "PA DataCreator",
            $"완료: {changed}개 데이터 에셋 생성/갱신\n\n아이템 15종, 레시피 8종 기준 밸런스를 적용했습니다.",
            "확인");
    }

    static int UpsertBalancedItems()
    {
        var specs = new[]
        {
            new ItemSpec("Item_Wheat",          1,  "Wheat",       10,  ItemCategory.Raw,       ToolType.None, 99, 0, "Basic crop used for early cooking."),
            new ItemSpec("Item_Carrot",         2,  "Carrot",      12,  ItemCategory.Raw,       ToolType.None, 99, 0, "Reliable spring crop and cooking ingredient."),
            new ItemSpec("Item_Wood",           3,  "Wood",        8,   ItemCategory.Raw,       ToolType.None, 99, 0, "Basic construction and crafting material."),
            new ItemSpec("Item_Ore",            4,  "Ore",         15,  ItemCategory.Raw,       ToolType.None, 99, 0, "Raw mineral used at the forge."),
            new ItemSpec("Item_BreadLoaf",      5,  "BreadLoaf",   34,  ItemCategory.Processed, ToolType.None, 99, 0, "Starter processed food with a steady margin."),
            new ItemSpec("Item_IronBar",        6,  "IronBar",     42,  ItemCategory.Processed, ToolType.None, 99, 0, "Refined metal with strong utility demand."),
            new ItemSpec("Item_Plank",          7,  "Plank",       24,  ItemCategory.Processed, ToolType.None, 99, 0, "Processed wood for furniture and tools."),
            new ItemSpec("Item_Fish",           8,  "Fish",        18,  ItemCategory.Raw,       ToolType.None, 99, 0, "Fresh fish for cooking."),
            new ItemSpec("Item_09_BakedPotato", 9,  "구운 감자",    28,  ItemCategory.Processed, ToolType.None, 32, 0, "초반 조리 상품. 재료 대비 회전율이 좋다."),
            new ItemSpec("Item_10_GrilledFish", 10, "생선구이",     52,  ItemCategory.Processed, ToolType.None, 32, 0, "단일 재료 고부가가치 조리 상품."),
            new ItemSpec("Item_11_Furniture",   11, "목제 가구",    185, ItemCategory.Luxury,    ToolType.None, 8,  2, "중반부 해금용 고마진 목공 상품."),
            new ItemSpec("Item_12_ToolSet",     12, "철제 도구",    150, ItemCategory.Utility,   ToolType.None, 8,  1, "실용 성향 NPC가 선호하는 중간 티어 상품."),
            new ItemSpec("Item_13_Clothes",     13, "의류",         165, ItemCategory.Luxury,    ToolType.None, 8,  2, "감성 성향 NPC가 반응하기 좋은 재단 상품."),
            new ItemSpec("Item_14_Hoe",         14, "호미",         0,   ItemCategory.Tool,      ToolType.Hoe,  1,  0, "밭을 갈 때 쓰는 기본 도구."),
            new ItemSpec("Item_15_Seed",        15, "씨앗",         20,  ItemCategory.Utility,   ToolType.Seed, 64, 0, "작물 재배를 시작하는 기본 소모품."),
        };

        int changed = 0;
        foreach (var spec in specs)
        {
            string path = $"{ITEMS}/{spec.AssetName}.asset";
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);
            bool created = false;
            if (item == null)
            {
                item = FindItemById(spec.Id);
            }
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<Item>();
                AssetDatabase.CreateAsset(item, path);
                created = true;
            }

            bool dirty = ApplyItemSpec(item, spec);
            if (dirty || created)
            {
                EditorUtility.SetDirty(item);
                changed++;
            }
        }

        Debug.Log($"[PA DataCreator] Balanced items upserted: {changed}");
        return changed;
    }

    static int UpsertBalancedRecipes()
    {
        var specs = new[]
        {
            new RecipeSpec("Recipe_Bread",        "BreadLoaf",       5,  1, 1.16f, 0.35f, 0, WorkbenchType.Kitchen,        "3 Wheat -> 1 BreadLoaf",     new[] { new IngredientSpec(1, 3) }),
            new RecipeSpec("Recipe_IronBar",      "IronBar",         6,  1, 1.18f, 0.25f, 0, WorkbenchType.Forge,          "2 Ore -> 1 IronBar",         new[] { new IngredientSpec(4, 2) }),
            new RecipeSpec("Recipe_Plank",        "Plank",           7,  1, 1.12f, 0.22f, 0, WorkbenchType.BasicWorkbench, "2 Wood -> 1 Plank",          new[] { new IngredientSpec(3, 2) }),
            new RecipeSpec("Recipe_BakedPotato",  "구운 감자 굽기",   9,  2, 1.08f, 0.45f, 0, WorkbenchType.Kitchen,        "Carrot-based starter dish.", new[] { new IngredientSpec(2, 2) }),
            new RecipeSpec("Recipe_GrilledFish",  "생선 굽기",        10, 1, 1.26f, 0.50f, 0, WorkbenchType.Kitchen,        "Single fish premium dish.",  new[] { new IngredientSpec(8, 1) }),
            new RecipeSpec("Recipe_Furniture",    "목제 가구 제작",   11, 1, 1.35f, 0.35f, 2, WorkbenchType.BasicWorkbench, "Tier 2 luxury woodcraft.",   new[] { new IngredientSpec(7, 3) }),
            new RecipeSpec("Recipe_ToolSet",      "철제 도구 제작",   12, 1, 1.32f, 0.30f, 1, WorkbenchType.Forge,          "Tier 1 utility craft.",      new[] { new IngredientSpec(7, 1), new IngredientSpec(6, 2) }),
            new RecipeSpec("Recipe_Clothes",      "의류 제작",        13, 1, 1.28f, 0.45f, 2, WorkbenchType.SewingTable,    "Tier 2 tailoring product.",  new[] { new IngredientSpec(1, 2), new IngredientSpec(7, 1) }),
        };

        int changed = 0;
        foreach (var spec in specs)
        {
            string path = $"{RECS}/{spec.AssetName}.asset";
            RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            bool created = false;
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(recipe, path);
                created = true;
            }

            bool dirty = ApplyRecipeSpec(recipe, spec);
            if (dirty || created)
            {
                EditorUtility.SetDirty(recipe);
                changed++;
            }
        }

        Debug.Log($"[PA DataCreator] Balanced recipes upserted: {changed}");
        return changed;
    }

    static bool ApplyItemSpec(Item item, ItemSpec spec)
    {
        bool dirty = false;
        dirty |= SetValue(ref item.id, spec.Id);
        dirty |= SetValue(ref item.itemName, spec.ItemName);
        dirty |= SetValue(ref item.basePrice, spec.BasePrice);
        dirty |= SetValue(ref item.category, spec.Category);
        dirty |= SetValue(ref item.toolType, spec.ToolType);
        dirty |= SetValue(ref item.maxStack, spec.MaxStack);
        dirty |= SetValue(ref item.requiredTier, spec.RequiredTier);
        dirty |= SetValue(ref item.description, spec.Description);
        return dirty;
    }

    static bool ApplyRecipeSpec(RecipeData recipe, RecipeSpec spec)
    {
        bool dirty = false;
        dirty |= SetValue(ref recipe.recipeName, spec.RecipeName);
        dirty |= SetValue(ref recipe.description, spec.Description);
        dirty |= SetValue(ref recipe.outputCount, spec.OutputCount);
        dirty |= SetValue(ref recipe.baseOutputQuality, spec.BaseOutputQuality);
        dirty |= SetValue(ref recipe.ingredientQualityWeight, spec.IngredientQualityWeight);
        dirty |= SetValue(ref recipe.requiredTier, spec.RequiredTier);
        dirty |= SetValue(ref recipe.requiredWorkbench, spec.RequiredWorkbench);

        Item output = FindItemById(spec.OutputItemId);
        dirty |= SetValue(ref recipe.outputItem, output);

        if (!IngredientsMatch(recipe.ingredients, spec.Ingredients))
        {
            recipe.ingredients = new List<RecipeIngredient>();
            foreach (var ingredient in spec.Ingredients)
            {
                Item item = FindItemById(ingredient.ItemId);
                if (item == null)
                {
                    Debug.LogWarning($"[PA DataCreator] Missing ingredient item id {ingredient.ItemId} for {spec.AssetName}");
                    continue;
                }
                recipe.ingredients.Add(new RecipeIngredient { item = item, count = ingredient.Count });
            }
            dirty = true;
        }

        if (recipe.inputItem != null || recipe.inputCount != 0)
        {
            recipe.inputItem = null;
            recipe.inputCount = 0;
            dirty = true;
        }

        return dirty;
    }

    static int CreateCandidates()
    {
        var defs = new[]
        {
            new CandidateSpec("Candidate_Farmer",     NpcSpecialty.Farmer,     300, "Profile_Farmer",     "마을 농부",   "작물을 안정적으로 공급하는 초반 생산자."),
            new CandidateSpec("Candidate_Miner",      NpcSpecialty.Miner,      400, "Profile_Miner",      "마을 광부",   "광석과 금속 경제의 출발점."),
            new CandidateSpec("Candidate_Lumberjack", NpcSpecialty.Lumberjack, 350, "Profile_Lumberjack", "마을 벌목꾼", "목재와 판자 생산을 돕는다."),
            new CandidateSpec("Candidate_Fisher",     NpcSpecialty.Fisher,     300, "Profile_Fisher",     "마을 어부",   "생선 조리 상품의 원천."),
            new CandidateSpec("Candidate_Chef",       NpcSpecialty.Chef,       600, "Profile_Chef",       "마을 셰프",   "주방 가공의 핵심 NPC."),
            new CandidateSpec("Candidate_Blacksmith", NpcSpecialty.Blacksmith, 700, "Profile_Blacksmith", "마을 대장장이", "단조와 도구 제작을 맡는다."),
            new CandidateSpec("Candidate_Tailor",     NpcSpecialty.Tailor,     650, "Profile_Tailor",     "마을 재단사", "의류 제작을 담당한다."),
            new CandidateSpec("Candidate_Carpenter",  NpcSpecialty.Carpenter,  600, "Profile_Carpenter",  "마을 목수",   "가구 제작과 목공 성장의 중심."),
        };

        int created = 0;
        foreach (var spec in defs)
        {
            string path = $"{CANDS}/{spec.AssetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<NpcCandidateData>(path) != null) continue;

            var candidate = ScriptableObject.CreateInstance<NpcCandidateData>();
            candidate.specialty = spec.Specialty;
            candidate.hireCost = spec.HireCost;
            candidate.displayName = spec.DisplayName;
            candidate.bio = spec.Bio;
            candidate.profile = AssetDatabase.LoadAssetAtPath<NpcProfile>($"{NPCS}/{spec.ProfileAssetName}.asset");
            AssetDatabase.CreateAsset(candidate, path);
            created++;
        }

        Debug.Log($"[PA DataCreator] Candidate assets created: {created}");
        return created;
    }

    static int CreateDialogues()
    {
        var defs = new[]
        {
            new DialogueSpec("Dialogue_Farmer",     "농부 기본 대사",     "오늘 흙 상태가 좋네요.",         "좋은 수확이 기대돼요."),
            new DialogueSpec("Dialogue_Miner",      "광부 기본 대사",     "좋은 광맥은 준비된 사람에게 보여요.", "반짝이는 광석을 찾았어요."),
            new DialogueSpec("Dialogue_Lumberjack", "벌목꾼 기본 대사",   "나무결을 보면 쓸모가 보여요.",   "숲 냄새가 참 좋네요."),
            new DialogueSpec("Dialogue_Fisher",     "어부 기본 대사",     "물살이 바뀌면 물고기도 움직여요.", "오늘은 파도가 부드러워요."),
            new DialogueSpec("Dialogue_Chef",       "셰프 기본 대사",     "좋은 재료가 좋은 요리를 만들죠.", "새 메뉴가 떠올랐어요."),
            new DialogueSpec("Dialogue_Blacksmith", "대장장이 기본 대사", "쇠는 정확한 온도를 좋아해요.",   "좋은 도구는 손에 착 감겨요."),
            new DialogueSpec("Dialogue_Tailor",     "재단사 기본 대사",   "옷감은 방향이 중요해요.",       "어울리는 색을 찾아볼까요?"),
            new DialogueSpec("Dialogue_Carpenter",  "목수 기본 대사",     "목재는 결을 따라 다뤄야 해요.",   "튼튼한 가구를 만들어볼게요."),
        };

        int created = 0;
        foreach (var spec in defs)
        {
            string path = $"{DLGS}/{spec.AssetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<DialogueData>(path) != null) continue;

            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            dialogue.label = spec.Label;
            dialogue.topics = new List<TopicDialoguePool>
            {
                new TopicDialoguePool
                {
                    topic = DialogueTopic.Greeting,
                    thinkingLines = new List<string> { spec.ThinkingLine },
                    feelingLines = new List<string> { spec.FeelingLine },
                }
            };
            AssetDatabase.CreateAsset(dialogue, path);
            created++;
        }

        Debug.Log($"[PA DataCreator] Dialogue assets created: {created}");
        return created;
    }

    static bool IngredientsMatch(List<RecipeIngredient> current, IngredientSpec[] specs)
    {
        if (current == null || current.Count != specs.Length) return false;

        for (int i = 0; i < specs.Length; i++)
        {
            RecipeIngredient ingredient = current[i];
            if (ingredient == null || ingredient.item == null) return false;
            if (ingredient.item.id != specs[i].ItemId) return false;
            if (ingredient.count != specs[i].Count) return false;
        }

        return true;
    }

    static Item FindItemById(int id)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Item", new[] { ITEMS }))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.id == id) return item;
        }
        return null;
    }

    static void EnsureDir(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    static bool SetValue<T>(ref T current, T value)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return false;
        current = value;
        return true;
    }

    struct ItemSpec
    {
        public readonly string AssetName;
        public readonly int Id;
        public readonly string ItemName;
        public readonly int BasePrice;
        public readonly ItemCategory Category;
        public readonly ToolType ToolType;
        public readonly int MaxStack;
        public readonly int RequiredTier;
        public readonly string Description;

        public ItemSpec(string assetName, int id, string itemName, int basePrice, ItemCategory category, ToolType toolType, int maxStack, int requiredTier, string description)
        {
            AssetName = assetName;
            Id = id;
            ItemName = itemName;
            BasePrice = basePrice;
            Category = category;
            ToolType = toolType;
            MaxStack = maxStack;
            RequiredTier = requiredTier;
            Description = description;
        }
    }

    struct RecipeSpec
    {
        public readonly string AssetName;
        public readonly string RecipeName;
        public readonly int OutputItemId;
        public readonly int OutputCount;
        public readonly float BaseOutputQuality;
        public readonly float IngredientQualityWeight;
        public readonly int RequiredTier;
        public readonly WorkbenchType RequiredWorkbench;
        public readonly string Description;
        public readonly IngredientSpec[] Ingredients;

        public RecipeSpec(string assetName, string recipeName, int outputItemId, int outputCount, float baseOutputQuality, float ingredientQualityWeight, int requiredTier, WorkbenchType requiredWorkbench, string description, IngredientSpec[] ingredients)
        {
            AssetName = assetName;
            RecipeName = recipeName;
            OutputItemId = outputItemId;
            OutputCount = outputCount;
            BaseOutputQuality = baseOutputQuality;
            IngredientQualityWeight = ingredientQualityWeight;
            RequiredTier = requiredTier;
            RequiredWorkbench = requiredWorkbench;
            Description = description;
            Ingredients = ingredients;
        }
    }

    struct IngredientSpec
    {
        public readonly int ItemId;
        public readonly int Count;

        public IngredientSpec(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    struct CandidateSpec
    {
        public readonly string AssetName;
        public readonly NpcSpecialty Specialty;
        public readonly int HireCost;
        public readonly string ProfileAssetName;
        public readonly string DisplayName;
        public readonly string Bio;

        public CandidateSpec(string assetName, NpcSpecialty specialty, int hireCost, string profileAssetName, string displayName, string bio)
        {
            AssetName = assetName;
            Specialty = specialty;
            HireCost = hireCost;
            ProfileAssetName = profileAssetName;
            DisplayName = displayName;
            Bio = bio;
        }
    }

    struct DialogueSpec
    {
        public readonly string AssetName;
        public readonly string Label;
        public readonly string ThinkingLine;
        public readonly string FeelingLine;

        public DialogueSpec(string assetName, string label, string thinkingLine, string feelingLine)
        {
            AssetName = assetName;
            Label = label;
            ThinkingLine = thinkingLine;
            FeelingLine = feelingLine;
        }
    }
}
