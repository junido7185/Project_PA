#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 들어갈 수 있는 상점 기초 검증 — 외부 문 진입 → 실내 슬롯 진열 → 가격 UI → 퇴장.
[InitializeOnLoad]
public static class PA_EnterableShopValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.EnterShop.Active";
    const string EnteredKey = "PA.EnterShop.Entered";
    const string RanKey = "PA.EnterShop.Ran";
    const string HadErrorKey = "PA.EnterShop.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static int _step;
    static double _startedAt;
    static double _nextStepAt;
    static GameObject _player;
    static BuildingEntrance _doorOut;
    static BuildingEntrance _doorIn;
    static ShopSlot _interiorSlot;
    static PrototypeWorldLabel _storeSign;

    static PA_EnterableShopValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Enterable Shop Validation")]
    public static void RunEnterableShopValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA EnterShop: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false; _ran = false; _hadError = false; _step = 0;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();
        Debug.Log("PA EnterShop: entering Play Mode.");
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
            if (elapsed > 60.0) { Debug.LogError("PA EnterShop: timed out entering Play Mode."); MarkFailedAndExit(); }
            return;
        }

        if (!_ran && EditorApplication.isPlaying)
        {
            if (EditorApplication.timeSinceStartup < _nextStepAt) return;

            try
            {
                bool done = RunStep(_step);
                _step++;
                _nextStepAt = EditorApplication.timeSinceStartup + 1.6; // 워프 페이드 대기

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
                Debug.LogError($"PA EnterShop failed: {ex.Message}\n{ex}");
                _ran = true;
                SessionState.SetBool(RanKey, true);
                EditorApplication.ExitPlaymode();
            }
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA EnterShop: timed out before completing checks.");
            MarkFailedAndExit();
        }
    }

    static bool RunStep(int step)
    {
        switch (step)
        {
            case 0:
                _player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
                Require(_player != null, "player exists");

                Require(TierService.Instance != null, "TierService exists");
                TierService.Instance.ForceSetTier(0, 0, "PA_EnterableShopValidator tier-0 gate");

                var doorOutGo = GameObject.Find("PA_StoreDoor_Out");
                Require(doorOutGo != null, "exterior store door exists");
                _doorOut = doorOutGo.GetComponent<BuildingEntrance>();
                Require(_doorOut != null, "exterior door has BuildingEntrance");
                Require(ShopEvolutionController.Instance != null, "shop evolution controller exists");
                Require(!_doorOut.IsUnlocked, "Tier 0 exterior store door is locked");
                Require(_doorOut.GetInteractPrompt().Contains("Tier 1"), "locked prompt explains Tier 1 requirement");

                _storeSign = doorOutGo.GetComponentInChildren<PrototypeWorldLabel>(true);
                Require(_storeSign != null, "exterior store sign exists");
                Require(_storeSign.label.Contains("Tier 1"), "Tier 0 sign explains the next store stage");

                var interior = GameObject.Find("PA_StoreInterior");
                Require(interior != null, "store interior root exists");
                var doorInT = interior.transform.Find("Door_In");
                Require(doorInT != null, "interior exit door exists");
                _doorIn = doorInT.GetComponent<BuildingEntrance>();
                Require(_doorIn != null, "interior door has BuildingEntrance");

                var slots = interior.GetComponentsInChildren<ShopSlot>(true);
                Require(slots.Length == 6, $"interior sales grid has 6 slots (found {slots.Length})");
                _interiorSlot = slots[0];

                // 실제 저장 복원 경로와 같은 ForceSetTier를 사용한다. 다음 프레임에 S4가
                // 파생 상태(문/간판)를 재구성하는지 확인한 뒤 입장한다.
                TierService.Instance.ForceSetTier(1, 0, "PA_EnterableShopValidator tier-1 unlock");
                return false;

            case 1:
                Require(_doorOut.IsUnlocked, "Tier 1 exterior store door is unlocked");
                Require(ShopEvolutionController.Instance.IsInteriorUnlocked, "S4 reports interior store unlocked");
                Require(_storeSign.label.Contains("OPEN"), "Tier 1 sign visibly reports the open interior store");

                // 핫바에 판매 아이템 준비 후 진입
                var bread = Resources.Load<Item>("Items/Item_BreadLoaf");
                Require(bread != null, "BreadLoaf asset exists");
                Inventory.instance.hotbar.GetSlot(0).SetInstance(new ItemInstance(bread, 2) { quality = 1f, currentPrice = bread.basePrice });

                _doorOut.Interact(_player);
                return false;

            case 2:
                Require(_player.transform.position.y > 50f, $"player warped inside (y={_player.transform.position.y:0.#})");

                _interiorSlot.Interact(_player); // 빈 슬롯 → 진열
                Require(!_interiorSlot.IsEmpty, "interior slot stocks from hotbar");

                _interiorSlot.Interact(_player); // 찬 슬롯 → 가격 UI
                Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "interior slot opens ShopPriceUI");
                return false;

            case 3:
                _doorIn.Interact(_player);
                return false;

            default:
                Require(_player.transform.position.y < 50f, $"player warped back outside (y={_player.transform.position.y:0.#})");
                Debug.Log("PA Enterable Shop Validation passed. Tier0 locked→Tier1 unlock→enter→stock→price UI→exit");
                return true;
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA EnterShop Check OK: {message}");
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
            Debug.LogError("PA Enterable Shop Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Enterable Shop Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
