#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Task 019 — 판매대 품절 표시 검증.
// NPC 구매로 비워진 슬롯이 "품절" 라벨/프롬프트를 보여주고,
// 재진열 또는 다음날이 되면 자연히 풀리는지 확인한다.
[InitializeOnLoad]
public static class PA_ShopSoldOutValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.ShopSoldOut.Active";
    const string EnteredKey = "PA.ShopSoldOut.Entered";
    const string RanKey = "PA.ShopSoldOut.Ran";
    const string HadErrorKey = "PA.ShopSoldOut.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static int _step;
    static double _startedAt;
    static double _nextStepAt;
    static ShopSlot _slot;
    static Item _bread;

    static PA_ShopSoldOutValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Shop Sold-Out Validation")]
    public static void RunShopSoldOutValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA ShopSoldOut: failed to open scene at {ScenePath}");
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
        Debug.Log("PA ShopSoldOut: entering Play Mode.");
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
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            _nextStepAt = _startedAt + 3.0;
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
                Debug.LogError("PA ShopSoldOut: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying)
        {
            if (EditorApplication.timeSinceStartup < _nextStepAt) return;

            try
            {
                // ClearDisplay 는 플레이 모드에서 지연 Destroy 를 쓰므로,
                // 라벨 소멸 검증은 프레임이 흐른 다음 스텝에서 수행한다.
                bool done = RunStep(_step);
                _step++;
                _nextStepAt = EditorApplication.timeSinceStartup + 0.6;

                if (done)
                {
                    _ran = true;
                    SessionState.SetBool(RanKey, true);
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception ex)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA ShopSoldOut failed: {ex.Message}\n{ex}");
                _ran = true;
                SessionState.SetBool(RanKey, true);
                EditorApplication.ExitPlaymode();
            }
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA ShopSoldOut: timed out before completing checks.");
            MarkFailedAndExit();
        }
    }

    // true 반환 = 마지막 스텝 완료.
    static bool RunStep(int step)
    {
        switch (step)
        {
            case 0:
                _slot = Object.FindFirstObjectByType<ShopSlot>();
                Require(_slot != null, "ShopSlot exists");
                Require(GameClock.Instance != null, "GameClock exists");
                _bread = Resources.Load<Item>("Items/Item_BreadLoaf");
                Require(_bread != null, "BreadLoaf item asset exists");

                GameClock.Instance.ForceSet(19f, 2, "PA_ShopSoldOut seed");

                // 1) 진열 → 정상 표시 (품절 아님)
                _slot.currentItem = new ItemInstance(_bread, 1) { quality = 1f, currentPrice = _bread.basePrice };
                _slot.displayPrice = 30;
                _slot.RefreshDisplay();
                Require(!_slot.IsSoldOutToday, "stocked slot is not sold out");
                Require(_slot.GetInteractPrompt().Contains("가격"), "stocked slot shows price prompt");

                // 2) NPC 구매로 소진 → 품절 상태/프롬프트/라벨
                Require(_slot.TryPurchaseByNpc("SoldOutValidator", out int paid) && paid == 30,
                    "NPC purchase empties the slot at 30G");
                Require(_slot.IsEmpty, "slot is empty after purchase");
                Require(_slot.IsSoldOutToday, "slot reports sold-out today after purchase");
                Require(_slot.GetInteractPrompt().Contains("품절"), "empty slot prompt mentions sold-out");
                Require(FindSoldOutLabel(_slot) != null, "sold-out world label is shown");
                return false;

            case 1:
                // 3) 재진열 → 품절 해제 (라벨 소멸은 다음 스텝에서 프레임 경과 후 확인)
                _slot.currentItem = new ItemInstance(_bread, 1) { quality = 1f, currentPrice = _bread.basePrice };
                _slot.RefreshDisplay();
                Require(!_slot.IsSoldOutToday, "restocking clears sold-out state");
                return false;

            case 2:
                Require(FindSoldOutLabel(_slot) == null, "sold-out label is gone after restock");

                // 4) 다시 소진 후 다음날 → 품절 자동 해제
                Require(_slot.TryPurchaseByNpc("SoldOutValidator", out _), "second purchase empties the slot");
                Require(_slot.IsSoldOutToday, "slot is sold out again");
                GameClock.Instance.ForceSet(8f, 3, "PA_ShopSoldOut next day");
                Require(!_slot.IsSoldOutToday, "sold-out state expires on the next day");
                _slot.RefreshDisplay();
                return false;

            default:
                Require(FindSoldOutLabel(_slot) == null, "sold-out label is gone on the next day");
                Require(_slot.GetInteractPrompt() == "판매대에 상품 진열", "next-day empty prompt is the normal stocking prompt");
                Debug.Log("PA Shop Sold-Out Validation passed. purchase→품절 label→restock/next-day clear");
                return true;
        }
    }

    static PrototypeWorldLabel FindSoldOutLabel(ShopSlot slot)
    {
        foreach (var label in slot.GetComponentsInChildren<PrototypeWorldLabel>(true))
        {
            if (label != null && label.label != null && label.label.Contains("품절"))
                return label;
        }
        return null;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA ShopSoldOut Check OK: {message}");
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
            Debug.LogError("PA Shop Sold-Out Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Shop Sold-Out Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
