#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PA_CraftingRecipeCardValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string WorkbenchPrefabPath = "Assets/Prefabs/Buildings/B05_Workbench.prefab";
    const string ActiveKey = "PA.CraftingRecipeCards.Active";
    const string RanKey = "PA.CraftingRecipeCards.Ran";
    const string ErrorKey = "PA.CraftingRecipeCards.Error";
    const string StageKey = "PA.CraftingRecipeCards.Stage";
    const string WaitFramesKey = "PA.CraftingRecipeCards.WaitFrames";
    const string WoodBeforeKey = "PA.CraftingRecipeCards.WoodBefore";
    const string PlankBeforeKey = "PA.CraftingRecipeCards.PlankBefore";
    const string FeedbackBeforeKey = "PA.CraftingRecipeCards.FeedbackBefore";

    static List<ItemInstance> _inventorySnapshot;
    static List<ItemInstance> _hotbarSnapshot;
    static GameObject _validationWorkbenchObject;
    static double _runtimeDeadline;

    static PA_CraftingRecipeCardValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        RegisterCallbacks();
        if (SessionState.GetBool(RanKey, false) &&
            !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Finish;
        }
    }

    [MenuItem("Project PA/Validation/Run Crafting Recipe Card Validation")]
    public static void RunValidation()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Failed to open {ScenePath}");

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(ErrorKey, false);
        SessionState.SetInt(StageKey, 0);
        SessionState.SetInt(WaitFramesKey, 0);
        _inventorySnapshot = null;
        _hotbarSnapshot = null;
        _validationWorkbenchObject = null;
        _runtimeDeadline = EditorApplication.timeSinceStartup + 30d;
        RegisterCallbacks();
        Debug.Log("[Crafting Recipe Cards] Entering D3D11 Play Mode validation");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _runtimeDeadline = EditorApplication.timeSinceStartup + 30d;
            SessionState.SetInt(StageKey, 10);
            SessionState.SetInt(WaitFramesKey, 0);
        }
        else if (state == PlayModeStateChange.EnteredEditMode &&
                 SessionState.GetBool(RanKey, false))
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;

        int waitFrames = SessionState.GetInt(WaitFramesKey, 0);
        if (waitFrames < 2)
        {
            SessionState.SetInt(WaitFramesKey, waitFrames + 1);
            return;
        }

        try
        {
            switch (SessionState.GetInt(StageKey, 0))
            {
                case 10:
                    PrepareInsufficientMaterialState();
                    break;
                case 20:
                    ValidateVisibleCardsAndAttemptBlockedCraft();
                    break;
                case 30:
                    ValidateBlockedCraftAndPrepareSufficientState();
                    break;
                case 40:
                    ValidateSufficientStateAndCraft();
                    break;
                case 50:
                    ValidateCraftSuccessAndExit();
                    break;
            }
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void PrepareInsufficientMaterialState()
    {
        CraftingUI ui = CraftingUI.instance;
        Inventory inventory = Inventory.instance;
        Workbench workbench = GetValidationWorkbench();
        if (ui == null || inventory == null || workbench == null)
        {
            if (EditorApplication.timeSinceStartup < _runtimeDeadline) return;
            Require(ui != null, "CraftingUI runtime singleton is ready");
            Require(inventory != null, "Inventory runtime singleton is ready");
            Require(workbench != null, "active Basic Workbench is ready");
        }

        Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
            $"D3D11 is active ({SystemInfo.graphicsDeviceType})");

        _inventorySnapshot = Snapshot(inventory.slots);
        _hotbarSnapshot = inventory.hotbar != null ? Snapshot(inventory.hotbar.slots) : null;
        ClearInventory(inventory);
        ui.OpenForWorkbench(workbench);
        AdvanceTo(20);
    }

    static void ValidateVisibleCardsAndAttemptBlockedCraft()
    {
        CraftingUI ui = RequireUi();
        Workbench workbench = GetValidationWorkbench();
        Require(workbench != null, "Basic Workbench remains active");

        RecipeData[] expectedRecipes = GetBasicRecipes();
        Require(expectedRecipes.Length == 2, $"Basic recipe data count is exactly 2 ({expectedRecipes.Length})");
        List<GameObject> cards = ValidateCardPresentation(ui, expectedRecipes, true);
        Require(cards.Count == expectedRecipes.Length,
            $"visible card count matches recipe count ({cards.Count}/{expectedRecipes.Length})");

        RecipeData plankRecipe = LoadPlankRecipe();
        Button plankButton = FindRecipeButton(cards, plankRecipe);
        Require(plankButton != null, "Plank recipe is selectable from its visible card");

        SessionState.SetInt(WoodBeforeKey, Inventory.instance.CountItems(plankRecipe.ingredients[0].item));
        SessionState.SetInt(PlankBeforeKey, Inventory.instance.CountItems(plankRecipe.outputItem));
        plankButton.onClick.Invoke();
        AdvanceTo(30);
    }

    static void ValidateBlockedCraftAndPrepareSufficientState()
    {
        CraftingUI ui = RequireUi();
        RecipeData recipe = LoadPlankRecipe();
        Item wood = recipe.ingredients[0].item;

        Require(Inventory.instance.CountItems(wood) == SessionState.GetInt(WoodBeforeKey, -1),
            "insufficient craft does not consume ingredients");
        Require(Inventory.instance.CountItems(recipe.outputItem) == SessionState.GetInt(PlankBeforeKey, -1),
            "insufficient craft does not grant output");
        ValidateCardPresentation(ui, GetBasicRecipes(), true);

        Require(Inventory.instance.AddInstance(new ItemInstance(wood, recipe.ingredients[0].count)
        {
            quality = 1f,
            currentPrice = wood.basePrice
        }), "required Wood can be added for sufficient-material validation");

        ui.OpenForWorkbench(GetValidationWorkbench());
        AdvanceTo(40);
    }

    static void ValidateSufficientStateAndCraft()
    {
        CraftingUI ui = RequireUi();
        Workbench workbench = GetValidationWorkbench();
        RecipeData recipe = LoadPlankRecipe();
        List<GameObject> cards = ValidateCardPresentation(ui, GetBasicRecipes(), false);
        Button plankButton = FindRecipeButton(cards, recipe);

        Require(plankButton != null && plankButton.interactable,
            "sufficient unlocked Plank recipe button is interactable");
        SessionState.SetInt(WoodBeforeKey, Inventory.instance.CountItems(recipe.ingredients[0].item));
        SessionState.SetInt(PlankBeforeKey, Inventory.instance.CountItems(recipe.outputItem));
        SessionState.SetInt(FeedbackBeforeKey, workbench.CraftFeedbackCount);
        plankButton.onClick.Invoke();
        AdvanceTo(50);
    }

    static void ValidateCraftSuccessAndExit()
    {
        CraftingUI ui = RequireUi();
        Workbench workbench = GetValidationWorkbench();
        RecipeData recipe = LoadPlankRecipe();
        Item wood = recipe.ingredients[0].item;

        Require(Inventory.instance.CountItems(wood) ==
                SessionState.GetInt(WoodBeforeKey, -1) - recipe.ingredients[0].count,
            "successful craft consumes the required Wood");
        Require(Inventory.instance.CountItems(recipe.outputItem) ==
                SessionState.GetInt(PlankBeforeKey, -1) + recipe.outputCount,
            "successful craft grants the Plank output");
        Require(workbench.CraftFeedbackCount == SessionState.GetInt(FeedbackBeforeKey, -1) + 1 &&
                workbench.LastCraftedItem == recipe.outputItem.itemName,
            "existing Workbench success pulse receives the crafted output");
        ValidateCardPresentation(ui, GetBasicRecipes(), true);

        RestoreInventory();
        DestroyValidationWorkbench();
        ui.Close();
        SessionState.SetBool(RanKey, true);
        Debug.Log("[Crafting Recipe Cards] PASS recipes=2 cards=2 active=2 bounds=PASS alpha=PASS " +
                  "insufficient=VISIBLE_AND_BLOCKED sufficient=CRAFTED inventory=PASS pulse=PASS capture=DEBT");
        EditorApplication.ExitPlaymode();
    }

    static List<GameObject> ValidateCardPresentation(
        CraftingUI ui, RecipeData[] expectedRecipes, bool expectMissingMaterials)
    {
        Require(ui != null && ui.IsOpen, "Basic Crafting UI is open");
        ScrollRect scroll = ui.craftingPanel.GetComponentInChildren<ScrollRect>(true);
        Require(scroll != null && scroll.viewport != null && scroll.content != null,
            "ScrollRect, Viewport, and Content exist");

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();

        Image maskGraphic = scroll.viewport.GetComponent<Image>();
        Mask mask = scroll.viewport.GetComponent<Mask>();
        Require(maskGraphic != null && mask != null && maskGraphic.color.a > 0.99f && !mask.showMaskGraphic,
            "Viewport mask is opaque for stencil writes while its graphic stays hidden");

        List<GameObject> cards = new List<GameObject>();
        foreach (Transform child in scroll.content)
        {
            if (child != null && child.name.StartsWith("Recipe_", StringComparison.Ordinal))
                cards.Add(child.gameObject);
        }

        Require(cards.Count == expectedRecipes.Length,
            $"generated recipe card count matches expected ({cards.Count}/{expectedRecipes.Length})");
        Bounds viewportBounds = GetWorldBounds(scroll.viewport);

        foreach (RecipeData recipe in expectedRecipes)
        {
            GameObject card = cards.FirstOrDefault(candidate => candidate.name == "Recipe_" + recipe.name);
            Require(card != null, $"card exists for {recipe.name}");
            Require(card.activeSelf && card.activeInHierarchy, $"{recipe.name} card is activeInHierarchy");

            RectTransform rect = card.GetComponent<RectTransform>();
            Require(rect != null && rect.rect.width > 0.5f && rect.rect.height > 0.5f,
                $"{recipe.name} card has non-zero layout ({rect?.rect.width:F1}x{rect?.rect.height:F1})");
            Require(Contains(viewportBounds, GetWorldBounds(rect), 1f),
                $"{recipe.name} card is inside Viewport bounds");

            Image cardImage = card.GetComponent<Image>();
            Require(cardImage != null && cardImage.enabled && cardImage.color.a > 0.1f &&
                    EffectiveCanvasGroupAlpha(card.transform) > 0.1f,
                $"{recipe.name} card has visible image and CanvasGroup alpha");

            TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);
            Require(label != null && label.text.Contains(recipe.outputItem.itemName),
                $"{recipe.name} card identifies its output");
            foreach (RecipeIngredient ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
                Require(label.text.Contains(ingredient.item.itemName) && label.text.Contains($"/{ingredient.count}"),
                    $"{recipe.name} card shows ingredient owned/required information");
            }
        }

        RecipeData plank = LoadPlankRecipe();
        GameObject plankCard = cards.First(candidate => candidate.name == "Recipe_" + plank.name);
        TMP_Text plankLabel = plankCard.GetComponentInChildren<TMP_Text>(true);
        int ownedWood = Inventory.instance.CountItems(plank.ingredients[0].item);
        Require(plankLabel.text.Contains($"{ownedWood}/{plank.ingredients[0].count}"),
            $"Plank card reflects Wood count ({ownedWood}/{plank.ingredients[0].count})");
        Require(expectMissingMaterials == (ownedWood < plank.ingredients[0].count),
            expectMissingMaterials ? "insufficient-material state is active" : "sufficient-material state is active");
        return cards;
    }

    static CraftingUI RequireUi()
    {
        Require(CraftingUI.instance != null, "CraftingUI singleton exists");
        return CraftingUI.instance;
    }

    static Workbench GetValidationWorkbench()
    {
        if (_validationWorkbenchObject != null)
            return _validationWorkbenchObject.GetComponent<Workbench>();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkbenchPrefabPath);
        Require(prefab != null, "B05 Workbench production prefab loads for validation");
        _validationWorkbenchObject = UnityEngine.Object.Instantiate(prefab);
        _validationWorkbenchObject.name = "PA_CraftingRecipeCardValidator_B05";
        _validationWorkbenchObject.transform.position = new Vector3(10000f, -1000f, 10000f);
        Workbench workbench = _validationWorkbenchObject.GetComponent<Workbench>();
        Require(workbench != null && workbench.workbenchType == WorkbenchType.BasicWorkbench &&
                workbench.ApplyFunctionalArt() && workbench.IsFunctionalArtReady,
            "B05 Workbench production feedback fixture is ready");
        return workbench;
    }

    static RecipeData[] GetBasicRecipes()
    {
        return Resources.LoadAll<RecipeData>("Recipes")
            .Where(recipe => recipe != null &&
                (recipe.requiredWorkbench == WorkbenchType.None ||
                 recipe.requiredWorkbench == WorkbenchType.BasicWorkbench))
            .OrderBy(recipe => recipe.name, StringComparer.Ordinal)
            .ToArray();
    }

    static RecipeData LoadPlankRecipe()
    {
        RecipeData recipe = Resources.Load<RecipeData>("Recipes/Recipe_Plank");
        Require(recipe != null && recipe.outputItem != null && recipe.ingredients != null &&
                recipe.ingredients.Count > 0 && recipe.ingredients[0].item != null,
            "Recipe_Plank data is complete");
        return recipe;
    }

    static Button FindRecipeButton(IEnumerable<GameObject> cards, RecipeData recipe)
    {
        GameObject card = cards.FirstOrDefault(candidate => candidate.name == "Recipe_" + recipe.name);
        return card != null ? card.GetComponent<Button>() : null;
    }

    static Bounds GetWorldBounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++) bounds.Encapsulate(corners[i]);
        return bounds;
    }

    static bool Contains(Bounds outer, Bounds inner, float tolerance)
    {
        return inner.min.x >= outer.min.x - tolerance && inner.max.x <= outer.max.x + tolerance &&
               inner.min.y >= outer.min.y - tolerance && inner.max.y <= outer.max.y + tolerance;
    }

    static float EffectiveCanvasGroupAlpha(Transform transform)
    {
        float alpha = 1f;
        for (Transform current = transform; current != null; current = current.parent)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group == null) continue;
            alpha *= group.alpha;
            if (group.ignoreParentGroups) break;
        }
        return alpha;
    }

    static List<ItemInstance> Snapshot(IList<InventorySlot> slots)
    {
        if (slots == null) return null;
        var result = new List<ItemInstance>(slots.Count);
        foreach (InventorySlot slot in slots) result.Add(Clone(slot != null ? slot.instance : null));
        return result;
    }

    static ItemInstance Clone(ItemInstance source)
    {
        if (source == null || source.data == null || source.count <= 0) return null;
        return new ItemInstance(source.data, source.count)
        {
            instanceId = source.instanceId,
            quality = source.quality,
            currentPrice = source.currentPrice
        };
    }

    static void ClearInventory(Inventory inventory)
    {
        foreach (InventorySlot slot in inventory.slots) slot?.Clear();
        if (inventory.hotbar != null && inventory.hotbar.slots != null)
            foreach (InventorySlot slot in inventory.hotbar.slots) slot?.Clear();
        inventory.RefreshAllUI();
    }

    static void RestoreInventory()
    {
        Inventory inventory = Inventory.instance;
        if (inventory == null || _inventorySnapshot == null) return;
        Restore(inventory.slots, _inventorySnapshot);
        if (inventory.hotbar != null && _hotbarSnapshot != null)
            Restore(inventory.hotbar.slots, _hotbarSnapshot);
        inventory.RefreshAllUI();
        _inventorySnapshot = null;
        _hotbarSnapshot = null;
    }

    static void DestroyValidationWorkbench()
    {
        if (_validationWorkbenchObject == null) return;
        UnityEngine.Object.Destroy(_validationWorkbenchObject);
        _validationWorkbenchObject = null;
    }

    static void Restore(IList<InventorySlot> slots, IList<ItemInstance> snapshot)
    {
        int count = Math.Min(slots.Count, snapshot.Count);
        for (int i = 0; i < count; i++) slots[i]?.SetInstance(Clone(snapshot[i]));
    }

    static void AdvanceTo(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(WaitFramesKey, 0);
    }

    static void Fail(Exception ex)
    {
        try { RestoreInventory(); } catch { }
        DestroyValidationWorkbench();
        if (CraftingUI.instance != null) CraftingUI.instance.Close();
        SessionState.SetBool(ErrorKey, true);
        SessionState.SetBool(RanKey, true);
        Debug.LogError($"[Crafting Recipe Cards] FAIL {ex.Message}\n{ex}");
        EditorApplication.ExitPlaymode();
    }

    static void Finish()
    {
        bool failed = SessionState.GetBool(ErrorKey, false);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(ErrorKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseInt(WaitFramesKey);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        Debug.Log(failed
            ? "[Crafting Recipe Cards] FINISHED_WITH_ERRORS"
            : "[Crafting Recipe Cards] FINISHED_PASS");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[Crafting Recipe Cards] PASS {message}");
    }
}
#endif
