#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_ProcessingChainValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.ProcessingChainValidation.Active";
    const string EnteredKey = "PA.ProcessingChainValidation.Entered";
    const string RanKey = "PA.ProcessingChainValidation.Ran";
    const string HadErrorKey = "PA.ProcessingChainValidation.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_ProcessingChainValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Processing Chain Validation")]
    public static void RunProcessingChainValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Processing: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();

        Debug.Log("PA Processing: entering Play Mode.");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;

        Application.logMessageReceived += OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
        }
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(EnteredKey, true);
        }

        if (state == PlayModeStateChange.EnteredEditMode
            && SessionState.GetBool(ActiveKey, false)
            && SessionState.GetBool(RanKey, false))
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;

        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA Processing: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && elapsed > 2.5)
        {
            _ran = true;
            SessionState.SetBool(RanKey, true);
            RunRuntimeChecks();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 90.0)
        {
            Debug.LogError("PA Processing: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            Require(Inventory.instance != null, "Inventory exists");
            Require(GameClock.Instance != null, "GameClock exists");

            var advisor = RequireOne<ProcessingOpportunityController>("ProcessingOpportunityController");
            var recipe = SelectValidationRecipe();
            var opportunity = advisor.Evaluate(recipe);
            Require(opportunity != null, "processing opportunity can be calculated");
            Require(opportunity.expectedOutputValue > opportunity.inputBaseValue, "processed output has higher expected value than raw inputs");
            Require(opportunity.expectedMargin > 0, "processing opportunity has positive estimated margin");

            ClearRuntimeInventory();
            SeedIngredients(recipe);
            GameClock.Instance.ForceSet(8f, 4, "ProcessingChainValidator");
            advisor.RefreshNow();

            var ready = advisor.GetTopOpportunities(3, requireOwnedInputs: true);
            Require(ready.Count > 0, "advisor finds at least one ready processing chain");
            Require(advisor.CurrentAdvisorText.Contains("Processing Focus"), "advisor HUD shows processing focus");

            int outputBefore = CountItem(recipe.outputItem);
            Workbench workbench = CreateRuntimeWorkbench(recipe.requiredWorkbench);
            bool crafted = CraftingService.TryCraft(recipe, workbench);
            int outputAfter = CountItem(recipe.outputItem);
            Require(crafted, $"CraftingService crafts {recipe.recipeName}");
            Require(outputAfter > outputBefore, "crafting creates processed output item");

            Debug.Log($"PA Processing Chain Validation passed. recipe={recipe.recipeName}, margin={opportunity.expectedMargin}G");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Processing Chain Validation failed: {ex.Message}\n{ex}");
        }
    }

    static RecipeData SelectValidationRecipe()
    {
        string[] preferred =
        {
            "Recipes/Recipe_Bread",
            "Recipes/Recipe_Plank",
            "Recipes/Recipe_IronBar",
            "Recipes/Recipe_GrilledFish"
        };

        foreach (string path in preferred)
        {
            var recipe = Resources.Load<RecipeData>(path);
            if (IsValidRecipe(recipe)) return recipe;
        }

        foreach (var recipe in Resources.LoadAll<RecipeData>("Recipes"))
            if (IsValidRecipe(recipe)) return recipe;

        throw new InvalidOperationException("No valid processing recipe found under Resources/Recipes.");
    }

    static bool IsValidRecipe(RecipeData recipe)
    {
        return recipe != null
            && recipe.outputItem != null
            && recipe.ingredients != null
            && recipe.ingredients.Count > 0
            && recipe.ingredients[0] != null
            && recipe.ingredients[0].item != null;
    }

    static void SeedIngredients(RecipeData recipe)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            bool added = Inventory.instance.AddInstance(new ItemInstance(ingredient.item, ingredient.count)
            {
                quality = 1f,
                currentPrice = ingredient.item.basePrice
            });
            Require(added, $"seeded ingredient {ingredient.item.itemName} x{ingredient.count}");
        }
    }

    static Workbench CreateRuntimeWorkbench(WorkbenchType type)
    {
        if (type == WorkbenchType.None) return null;

        var go = new GameObject($"PA_ValidationWorkbench_{type}", typeof(BoxCollider), typeof(Workbench));
        var workbench = go.GetComponent<Workbench>();
        workbench.workbenchType = type;
        workbench.displayName = $"Validation {type}";
        return workbench;
    }

    static void ClearRuntimeInventory()
    {
        foreach (var slot in Inventory.instance.slots)
            slot?.Clear();

        if (Inventory.instance.hotbar != null)
        {
            foreach (var slot in Inventory.instance.hotbar.slots)
                slot?.Clear();
        }

        Inventory.instance.RefreshAllUI();
    }

    static int CountItem(Item item)
    {
        int count = 0;
        CountItemInSlots(Inventory.instance.slots, item, ref count);

        if (Inventory.instance.hotbar != null)
            CountItemInSlots(Inventory.instance.hotbar.slots, item, ref count);

        return count;
    }

    static void CountItemInSlots(System.Collections.Generic.List<InventorySlot> slots, Item item, ref int count)
    {
        if (slots == null || item == null) return;

        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item != item) continue;
            count += slot.count;
        }
    }

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA Processing Check OK: {message}");
    }

    static void MarkFailedAndExit()
    {
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool hadError = _hadError || SessionState.GetBool(HadErrorKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);

        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(HadErrorKey);

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Processing Chain Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Processing Chain Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
