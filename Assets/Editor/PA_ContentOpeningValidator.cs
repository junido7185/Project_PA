using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_ContentOpeningValidator
{
    const string Key = "PA.Content001.Active";
    const string Output = "Logs/Content/CONTENT001/Runtime";
    static Task _task;
    static bool _failed;
    static double _started;

    static PA_ContentOpeningValidator()
    {
        if (SessionState.GetBool(Key, false)) Subscribe();
    }

    [MenuItem("Project PA/Validation/Run Content Opening Validation")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Failed", false);
        SessionState.SetBool(Key + ".Done", false);
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        if (!Application.isBatchMode) ConfigureExistingGameViewSize();
        EditorApplication.EnterPlaymode();
    }

    static void Subscribe()
    {
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnMode;
        EditorApplication.playModeStateChanged += OnMode;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    static void ConfigureExistingGameViewSize()
    {
        // 기존 Full HD 프리셋만 선택한다. ProjectSettings나 새 전역 프리셋을 작성하지 않는다.
        Assembly editor = typeof(Editor).Assembly;
        Type viewType = editor.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        view.Show();
        Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
        Type groupType = editor.GetType("UnityEditor.GameViewSizeGroupType");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        string[] labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = Array.FindIndex(labels, label => label.Contains("1920") && label.Contains("1080"));
        if (index >= 0)
            viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
        else Debug.LogWarning("[CONTENT-001] Existing Full HD preset unavailable; capture dimensions must be reviewed.");
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _failed = true;
        SessionState.SetBool(Key + ".Failed", true);
    }

    static void OnMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) _started = EditorApplication.timeSinceStartup;
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Done", false)) return;
        bool failed = SessionState.GetBool(Key + ".Failed", false);
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Debug.Log(failed ? "[CONTENT-001] VALIDATION_FAIL" : "[CONTENT-001] VALIDATION_PASS");
        if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-executeMethod"))
            EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".Done", false)) return;
        if (_task == null && EditorApplication.isPlaying && WorldAlphaPlayableController.Instance?.IsReady == true)
            _task = Checks();
        if (_task != null && _task.IsCompleted)
        {
            if (_task.IsFaulted)
            {
                _failed = true;
                Debug.LogError("[CONTENT-001] " + _task.Exception?.GetBaseException());
            }
            SessionState.SetBool(Key + ".Failed", _failed);
            SessionState.SetBool(Key + ".Done", true);
            EditorApplication.ExitPlaymode();
        }
        else if (EditorApplication.timeSinceStartup - _started > 160)
        {
            SessionState.SetBool(Key, false);
            Debug.LogError("[CONTENT-001] Runtime timeout.");
            if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-executeMethod"))
                EditorApplication.Exit(1);
        }
    }

    static async Task Checks()
    {
        WorldAlphaPlayableController alpha = WorldAlphaPlayableController.Instance;
        SaveManager save = SaveManager.instance;
        var repository = new LocalJsonSaveRepository(Path.GetFullPath(Output));
        typeof(SaveManager).GetField("_repository", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(save, repository);
        int startMoney = EconomyService.Instance.Money;
        Check(alpha.BeginNewGame() && alpha.BeginNewGame(), "new game entry is idempotent");
        CampaignOpeningController opening = alpha.Opening;
        Check(opening.HasStarted && !opening.HasGreetedBori && !opening.HasSecuredStock, "fresh A01 has no fabricated greeting or stock");
        Check(EconomyService.Instance.Money == startMoney, "no duplicate starter money");
        Check(BoriCount() == 1, "one dedicated Bori");
        Check(opening.Bori.GetComponent<ProducerNpcController>() == null &&
              opening.Bori.GetComponent<SpecialistNpcController>() == null, "Bori is a consumer and neighbour, not a ninth producer");
        Check(!Resources.LoadAll<NpcCandidateData>("Candidates").Any(c => c.profile == opening.Bori.GetComponent<NpcController>().profile), "Bori has no hiring candidate");
        Check(!alpha.PlayerControlHint.Contains("채용") && !alpha.PlayerControlHint.Contains("납품"), "opening introduces movement, neighbour and stock first");

        var other = new GameObject("Content001_OtherDialogueProbe");
        other.AddComponent<CapsuleCollider>();
        var otherDialogue = other.AddComponent<NpcDialogue>();
        otherDialogue.dialogueData = Resources.Load<DialogueData>("Dialogues/Dialogue_Farmer");
        otherDialogue.overrideProfile = Resources.Load<NpcProfile>("NPCs/Profile_Farmer");
        otherDialogue.Interact(alpha.Adapter.PlayerRoot);
        Check(!opening.HasGreetedBori, "another NPC opening dialogue does not complete Bori greeting");
        Object.Destroy(other);
        DialogueUI.instance?.Hide();

        // 실제 PlayerInteraction의 공간 탐색과 Space 진입을 사용한다. 순간이동은 이동 시간만 줄이는 fixture다.
        NpcDialogue bori = opening.Bori.GetComponent<NpcDialogue>();
        ApproachAndInteract(alpha, bori.transform, bori);
        Check(opening.HasGreetedBori && FriendshipService.Instance.GetPoints("bori") == 2, "direct Bori greeting records identity and daily +2");
        DialogueUI.instance?.Hide();
        ApproachAndInteract(alpha, bori.transform, bori);
        Check(FriendshipService.Instance.GetPoints("bori") == 2, "repeat greeting does not duplicate friendship");
        DialogueUI.instance?.Hide();
        await Task.Delay(120);
        await save.SaveGameAsync();
        string beforeStock = await repository.LoadAsync("savegame");
        await save.LoadGameAsync();
        Check(opening.HasGreetedBori && !opening.HasSecuredStock && BoriCount() == 1, "greeting-only save restores without inventing stock");
        Check(alpha.CurrentPlayerObjective.Contains("숲 채집터"), "greeting-only load directs the player to stock even before the movement milestone");

        var forest = alpha.Adapter.FindDaytimeActivity("forest-forage");
        ApproachAndInteract(alpha, forest.transform, forest);
        await Task.Delay(120);
        Check(opening.OpeningComplete && opening.Capture().firstStockDay == 1, "real forest activity produces sale stock and completes A01");
        int count = Inventory.instance.CountItems(Resources.Load<Item>("Items/Item_Carrot"));
        Check(count > 0, "first stock exists in inventory");
        await save.SaveGameAsync();
        string completedSave = await repository.LoadAsync("savegame");
        SaveData data = JsonUtility.FromJson<SaveData>(completedSave);
        Check(data.version == SaveManager.CurrentSaveVersion && data.campaign.boriGreeted && data.campaign.firstStockItemId >= 0, "current JSON contains explicit A01 evidence");
        await save.LoadGameAsync();
        await save.LoadGameAsync();
        Check(opening.OpeningComplete && BoriCount() == 1 && FriendshipService.Instance.GetPoints("bori") == 2 &&
              Inventory.instance.CountItems(Resources.Load<Item>("Items/Item_Carrot")) == count, "repeat load preserves progress, stock and one resident");

        data.version = 12;
        data.campaign = null;
        await repository.SaveAsync("savegame", JsonUtility.ToJson(data));
        await save.LoadGameAsync();
        Check(!opening.HasStarted && !opening.Bori.activeSelf, "v12 migration does not infer campaign from old friendship or inventory");
        await save.SaveGameAsync();
        SaveData migrated = JsonUtility.FromJson<SaveData>(await repository.LoadAsync("savegame"));
        Check(migrated.version == SaveManager.CurrentSaveVersion && migrated.m85RecoveryRevision == data.m85RecoveryRevision &&
              migrated.hasPlayerRotation == data.hasPlayerRotation && migrated.shopOpenedDay == data.shopOpenedDay,
            "v12 migration preserves recovery envelope fields");
        await repository.SaveAsync("savegame", completedSave);
        await save.LoadGameAsync();
        Check(opening.OpeningComplete && BoriCount() == 1, "campaign reload reactivates the same Bori");

        // 순서를 바꾼 경로: 인사 전 실제 재고를 갖고 있어도 인사를 생략할 수 없다.
        await repository.SaveAsync("savegame", beforeStock);
        await save.LoadGameAsync();
        CampaignProgressSaveData reverse = opening.Capture();
        reverse.boriGreeted = false;
        opening.Restore(alpha, reverse);
        ApproachAndInteract(alpha, forest.transform, forest);
        await Task.Delay(120);
        Check(opening.HasSecuredStock && !opening.OpeningComplete, "stock before greeting is accepted without skipping Bori");
        ApproachAndInteract(alpha, bori.transform, bori);
        Check(opening.OpeningComplete, "stock-first order completes after real greeting");
        DialogueUI.instance?.Hide();
        await repository.SaveAsync("savegame", beforeStock);
        await save.LoadGameAsync();
        string activityMessage = (string)typeof(DayNightShopLoopController).GetField("_lastActivityResult", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DayNightShopLoopController.Instance);
        Check(string.IsNullOrEmpty(activityMessage), "load clears stale stock success text from the later session");
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "/Opening.png"), Camera.main, null);
        Check(Mathf.Approximately(Camera.main.orthographicSize, CampaignOpeningController.PlayCameraSize), "world reprojection after load preserves the readable campaign camera");
        Debug.Log($"[CONTENT-001] CAPTURE_DIMENSIONS width={Screen.width} height={Screen.height}");
        Debug.Log("[CONTENT-001] CHECKS_PASS greeting=true stock=true idempotent=true v12Migration=true sameSessionSaveLoad=true");
    }

    static int BoriCount() => Object.FindObjectsByType<NpcDialogue>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(n => n.friendshipId == "bori");

    static void ApproachAndInteract(WorldAlphaPlayableController alpha, Transform target, IInteractable expected)
    {
        GameObject player = alpha.Adapter.PlayerRoot;
        Vector3 direction = player.transform.position - target.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        var guard = player.GetComponent<WorldPlayerTraversalGuard>();
        Check(guard != null && guard.TryTeleportTo(target.position + direction.normalized * 1.3f + Vector3.up * 0.05f), "interaction approach is walkable");
        Vector3 facing = target.position - player.transform.position;
        facing.y = 0f;
        player.transform.rotation = Quaternion.LookRotation(facing);
        Physics.SyncTransforms();
        var interaction = player.GetComponent<PlayerInteraction>();
        object[] args = { null, null };
        bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(interaction, args);
        Check(found && ReferenceEquals(args[0], expected), "Space resolves the intended nearby interaction");
        typeof(PlayerInteraction).GetMethod("TryInteract", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(interaction, null);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[CONTENT-001] CHECK_OK " + message);
    }
}
