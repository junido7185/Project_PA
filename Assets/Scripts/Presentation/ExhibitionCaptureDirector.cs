using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전시 인쇄물용 무인 촬영 감독(Windows 플레이어 전용).
// `-pa-exhibition-capture <출력폴더>`가 있을 때만 켜지고, 없으면 아무것도 만들지 않는다.
// `-pa-capture-plan story|settlement`(기본 story). 사용자 저장 보호를 위해 `-pa-save-root <격리 폴더>`가 없으면 촬영하지 않는다.
// 실제 타이틀 버튼·동행 선택 API·항해·이어하기 경로를 그대로 쓰고, 단계마다 시간 제한과 예외 기록 뒤 다음 촬영으로 넘어가며
// 끝나면 capture-manifest.json을 남기고 종료한다. 선택 인자: -pa-capture-supersize 2, -pa-capture-msaa 8,
// -pa-capture-timeout 360, -pa-capture-companions Miner_01,Farmer_01.
public sealed class ExhibitionCaptureDirector : MonoBehaviour
{
    public const string ArgOutput = "-pa-exhibition-capture";
    public const string ArgPlan = "-pa-capture-plan";
    public const string ManifestName = "capture-manifest.json";
    public const string ClockReason = "exhibition-capture";
    public const int CleanWidth = 3840, CleanHeight = 2160;
    const float CleanAspect = CleanWidth / (float)CleanHeight;
    // 인쇄(B2 300dpi)용으로 -pa-capture-clean-width <px>를 주면 CLEAN 컷만 같은 16:9 비율로 키운다.
    int _cleanWidth = CleanWidth, _cleanHeight = CleanHeight;
    public static bool IsActive { get; private set; }
    public static ExhibitionCaptureDirector Instance { get; private set; }
    public string Plan { get; private set; }
    public string OutputDirectory { get; private set; }
    public bool Finished { get; private set; }

    [Serializable] public sealed class ShotRecord
    {
        public string file, kind, plan, scene, camera;
        public float gameHour = -1f;
        public int gameDay;
        public Vector3 cameraPosition, cameraRotation;
        public float fov;
        public bool hudVisible;
        public int width, height;
        public float realtime;
        public string notes = string.Empty, error = string.Empty;
    }

    [Serializable] public sealed class Manifest
    {
        public string tool = "ExhibitionCaptureDirector";
        public string plan, result = "running", outputDir, saveRoot, startedLocal, finishedLocal, unityVersion, screen;
        public int superSize, maxMsaa;
        public float seconds;
        public List<ShotRecord> shots = new List<ShotRecord>();
        public List<string> notes = new List<string>();
        public List<string> errors = new List<string>();
    }

    sealed class PendingShot { public Texture2D texture; public ShotRecord record; }
    sealed class CameraHold { public Camera cam; public CameraController follow; public bool followEnabled; public Vector3 position; public Quaternion rotation; public float fov; }
    sealed class ShopFrame { public bool found; public Bounds bounds; public Vector3 facade = Vector3.back; public Vector3 door; public Transform root; public DemoShopInterior interior; public string how = "상점 없음"; }

    readonly Manifest _manifest = new Manifest();
    readonly List<PendingShot> _pending = new List<PendingShot>();
    float _startedAt, _overallTimeout = 360f;
    int _superSize = 2, _maxMsaa = 8;
    string[] _companions = { "Miner_01", "Farmer_01" };
    Button _primary, _secondary;
    DepartureVoyagePresentation _voyage;

    static float Now => Time.realtimeSinceStartup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        string output = Arg(ArgOutput);
        if (output == null || Instance != null) return; // 인자가 없으면 완전 비활성
        if (Application.isEditor)
        {
            // Editor의 SaveManager는 -pa-save-root 대신 SessionState를 쓰므로 사용자 저장 보호를 보장할 수 없다.
            Debug.LogWarning("[ExhibitionCapture] Editor에서는 비활성입니다. Windows 플레이어로 실행하세요.");
            return;
        }
        var go = new GameObject("PA_ExhibitionCaptureDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<ExhibitionCaptureDirector>().Begin(output);
    }

    void Begin(string output)
    {
        Instance = this;
        IsActive = true;
        _startedAt = Now;
        Application.runInBackground = true; // 포커스를 잃어도 무인 촬영이 멈추지 않게
        Plan = (Arg(ArgPlan) ?? "story").Trim().ToLowerInvariant();
        _superSize = Mathf.Clamp(IntArg("-pa-capture-supersize", 2), 1, 4);
        _maxMsaa = Mathf.Clamp(IntArg("-pa-capture-msaa", 8), 1, 8);
        _overallTimeout = Mathf.Clamp(FloatArg("-pa-capture-timeout", 360f), 60f, 1800f);
        _cleanWidth = Mathf.Clamp(IntArg("-pa-capture-clean-width", CleanWidth), 1280, 7680) / 16 * 16;
        _cleanHeight = _cleanWidth * 9 / 16;
        string companions = Arg("-pa-capture-companions");
        if (!string.IsNullOrWhiteSpace(companions))
            _companions = companions.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        try
        {
            OutputDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(output)
                ? Path.Combine(Application.persistentDataPath, "ExhibitionCapture") : output);
            Directory.CreateDirectory(OutputDirectory);
        }
        catch (Exception e)
        {
            Debug.LogError("[ExhibitionCapture] 출력 폴더를 만들 수 없습니다: " + e.Message);
            Finished = true;
            Debug.Log("PA_EXHIBITION_CAPTURE result=failed reason=output-dir");
            Application.Quit();
            return;
        }
        _manifest.plan = Plan;
        _manifest.outputDir = OutputDirectory;
        _manifest.saveRoot = Arg("-pa-save-root") ?? string.Empty;
        _manifest.startedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        _manifest.unityVersion = Application.unityVersion;
        _manifest.screen = $"{Screen.width}x{Screen.height} fullScreen={Screen.fullScreenMode}";
        _manifest.superSize = _superSize;
        _manifest.maxMsaa = _maxMsaa;
        Note($"plan={Plan} companions={string.Join(",", _companions)} timeout={_overallTimeout:F0}s");
        WriteManifest();
        StartCoroutine(Run());
    }

    void Update()
    {
        // 코루틴이 어떤 이유로 멈춰도 반드시 종료한다.
        if (!Finished && Now - _startedAt > _overallTimeout + 30f)
        {
            StopAllCoroutines();
            Error("전체 시간 초과(감시) — 남은 촬영을 건너뛰고 종료");
            Finish();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsActive = false; }
    }

    IEnumerator Run()
    {
        yield return null; // 첫 씬의 Start 이후
        if (string.IsNullOrEmpty(Arg("-pa-save-root")))
            Error("-pa-save-root <격리 폴더>가 없어 사용자 저장 보호를 위해 촬영하지 않습니다.");
        else if (Plan == "story") yield return Step("story", PlanStory(), _overallTimeout);
        else if (Plan == "settlement") yield return Step("settlement", PlanSettlement(), _overallTimeout);
        else if (Plan == "feedback") yield return Step("feedback", PlanFeedback(), _overallTimeout);
        else if (Plan == "motion") yield return Step("motion", PlanMotion(), _overallTimeout);
        else if (Plan == "crafting") yield return Step("crafting", PlanCrafting(), _overallTimeout);
        else if (Plan == "npc") yield return Step("npc", PlanNpc(), _overallTimeout);
        else if (Plan == "tools") yield return Step("tools", PlanTools(), _overallTimeout);
        else if (Plan == "decor") yield return Step("decor", PlanDecor(), _overallTimeout);
        else if (Plan == "beach") yield return Step("beach", PlanBeach(), _overallTimeout);
        else if (Plan == "dig") yield return Step("dig", PlanDig(), _overallTimeout);
        else if (Plan == "ui") yield return Step("ui", PlanUi(), _overallTimeout);
        else if (Plan == "market") yield return Step("market", PlanMarket(), _overallTimeout);
        else if (Plan == "gift") yield return Step("gift", PlanGift(), _overallTimeout);
        else if (Plan == "night") yield return Step("night", PlanNight(), _overallTimeout);
        else if (Plan == "sardine") yield return Step("sardine", PlanSardine(), _overallTimeout);
        else if (Plan == "mackerel") yield return Step("mackerel", PlanMackerel(), _overallTimeout);
        else if (Plan == "flounder") yield return Step("flounder", PlanFlounder(), _overallTimeout);
        else if (Plan == "decorset") yield return Step("decorset", PlanDecorSet(), _overallTimeout);
        else if (Plan == "insectboard") yield return Step("insectboard", PlanInsectBoard(), _overallTimeout);
        else if (Plan == "ladybug") yield return Step("ladybug", PlanLadybug(), _overallTimeout);
        else if (Plan == "dragonfly") yield return Step("dragonfly", PlanDragonfly(), _overallTimeout);
        else if (Plan == "bumblebee") yield return Step("bumblebee", PlanBumblebee(), _overallTimeout);
        else if (Plan == "grasshopper") yield return Step("grasshopper", PlanGrasshopper(), _overallTimeout);
        else if (Plan == "stagbeetle") yield return Step("stagbeetle", PlanStagBeetle(), _overallTimeout);
        else if (Plan == "longhorn") yield return Step("longhorn", PlanLonghorn(), _overallTimeout);
        else if (Plan == "cricket") yield return Step("cricket", PlanCricket(), _overallTimeout);
        else if (Plan == "chairpaint") yield return Step("chairpaint", PlanChairPaint(), _overallTimeout);
        else if (Plan == "mantis") yield return Step("mantis", PlanMantis(), _overallTimeout);
        else if (Plan == "rug") yield return Step("rug", PlanRug(), _overallTimeout);
        else if (Plan == "wallhang") yield return Step("wallhang", PlanWallHang(), _overallTimeout);
        else if (Plan == "desk") yield return Step("desk", PlanDesk(), _overallTimeout);
        else if (Plan == "pendant") yield return Step("pendant", PlanPendant(), _overallTimeout);
        else if (Plan == "hints") yield return Step("hints", PlanHints(), _overallTimeout);
        else if (Plan == "visitor") yield return Step("visitor", PlanVisitor(), _overallTimeout);
        else if (Plan == "freshwater") yield return Step("freshwater", PlanFreshwaterProbe(), _overallTimeout);
        else if (Plan == "trout") yield return Step("trout", PlanFreshFish(FishingSpot.TroutSpecies), _overallTimeout);
        else if (Plan == "eel") yield return Step("eel", PlanFreshFish(FishingSpot.EelSpecies), _overallTimeout);
        else if (Plan == "crucian") yield return Step("crucian", PlanFreshFish(FishingSpot.CrucianSpecies), _overallTimeout);
        else if (Plan == "firefly") yield return Step("firefly", PlanFirefly(), _overallTimeout);
        else if (Plan == "mullet") yield return Step("mullet", PlanZoneFish(FishingSpot.MulletSpecies), _overallTimeout);
        else if (Plan == "puffer") yield return Step("puffer", PlanZoneFish(FishingSpot.PufferSpecies), _overallTimeout);
        else if (Plan == "beetle") yield return Step("beetle", PlanBeetle(), _overallTimeout);
        else if (Plan == "moth") yield return Step("moth", PlanMoth(), _overallTimeout);
        else if (Plan == "tutorial") yield return Step("tutorial", PlanTutorialFruit(), _overallTimeout);
        else Error("알 수 없는 -pa-capture-plan: " + Plan + " (story|settlement|feedback|motion|crafting|npc|tools|decor|beach|dig|ui|market|gift|night|sardine)");
        Finish();
    }

    // ───────────── plan: story ─────────────
    IEnumerator PlanStory()
    {
        yield return Step("title", StoryTitle(), 50f);
        if (_primary == null) { Error("타이틀 '새 게임' 버튼이 준비되지 않아 story 촬영을 중단합니다."); yield break; }
        yield return Step("new-game", StoryNewGame(), 25f);
        yield return Step("tutorial", StoryTutorial(), 70f);
        yield return Step("companions", StoryCompanions(), 35f);
        var selection = FindFirstObjectByType<DepartureCompanionSelection>();
        if (selection == null || !selection.IsConfirmed) { Error("동행 확정이 없어 항해·도착 촬영을 중단합니다."); yield break; }
        yield return Step("voyage", StoryVoyage(), 60f);
        yield return Step("arrival", StoryArrival(), 200f);
    }

    IEnumerator StoryTitle()
    {
        yield return TitleReady(false);
        if (_primary == null) yield break;
        yield return Wait(2.5f); // 타이틀 배경·버튼이 자리 잡은 뒤
        yield return ShotUI("story_01_title_ui.png", "타이틀 · " + ButtonLabel(_primary));
    }

    IEnumerator StoryNewGame()
    {
        // onClick.Invoke는 interactable을 무시하므로 저장 확인(비동기)이 끝나 버튼이 실제로 눌릴 수 있을 때만 누른다.
        yield return Until(() => _primary == null || _primary.interactable, 10f);
        if (_primary == null) { Error("타이틀 버튼이 사라졌습니다."); yield break; }
        _primary.onClick.Invoke();
        yield return Wait(1f);
        if (SceneManager.GetActiveScene().name == DepartureTutorialController.SceneName) yield break;
        var scenario = PlayableDayScenarioController.Instance;
        var confirm = FindFlowButton("PrimaryButton");
        if (scenario != null && scenario.TitleMenuOpen && confirm != null && confirm.interactable && ButtonLabel(confirm) == "새 게임 시작")
        {
            Note("격리 저장 폴더에 기존 저장이 있어 '새 게임 시작' 확인을 눌렀습니다(story는 빈 폴더 권장).");
            confirm.onClick.Invoke();
        }
    }

    IEnumerator StoryTutorial()
    {
        DepartureTutorialController tutorial = null;
        float end = Now + 60f;
        while (Now < end && ((tutorial = FindFirstObjectByType<DepartureTutorialController>()) == null || !tutorial.IsReady)) yield return null;
        if (tutorial == null || !tutorial.IsReady) { Error("출항 교육 씬이 준비되지 않았습니다."); yield break; }
        yield return Wait(3f);
        yield return ShotUI("story_02_tutorial_ui.png", "출항 교육 · " + tutorial.ObjectiveText);
    }

    IEnumerator StoryCompanions()
    {
        DepartureCompanionSelection selection = null;
        float end = Now + 25f;
        while (Now < end && ((selection = FindFirstObjectByType<DepartureCompanionSelection>()) == null ||
                             !selection.enabled || selection.Tutorial == null)) yield return null;
        if (selection == null || selection.Tutorial == null) { Error("동행 선택 컴포넌트를 찾지 못했습니다."); yield break; }
        _voyage = selection.GetComponent<DepartureVoyagePresentation>();
        yield return Wait(.5f);
        bool opened = TryOpenSelection(selection, out string how);
        Note("동행 선택 열기: " + how);
        if (opened)
        {
            yield return Wait(.4f);
            foreach (string id in _companions)
                if (!selection.Toggle(id)) Error("동행 선택 실패: " + id);
            // 요청한 ID가 바뀌었으면 실제 후보 순서대로 채워 항해 경로는 계속 확인한다.
            if (selection.SelectedIds.Count < 2 && selection.candidates != null)
                foreach (var candidate in selection.candidates)
                {
                    if (selection.SelectedIds.Count >= 2) break;
                    if (candidate != null && !selection.SelectedIds.Contains(candidate.id) && selection.Toggle(candidate.id))
                        Note("대체 동행 선택: " + candidate.id);
                }
            yield return Wait(1.2f);
            yield return ShotUI("story_03_companions_ui.png", "동행 선택 · " + string.Join(" + ", selection.SelectedIds));
            if (selection.SelectedIds.Count == 2) selection.Confirm(); // DepartureConfirmed → 실제 항해
        }
        if (!selection.IsConfirmed && !selection.IsOpen && !selection.RestoreConfirmedSelection(_companions))
            Error("동행 확정 실패: " + string.Join(",", _companions));
        else if (!selection.IsConfirmed && selection.IsOpen) Error("동행 2명을 고르지 못해 확정하지 못했습니다.");
        if (selection.IsConfirmed) Note("CONFIRMED " + string.Join(",", selection.ConfirmedIds));
    }

    // Editor 검사는 OpenDevelopmentSelection(Editor 전용)으로 같은 선택 UI를 연다. 플레이어에서는 같은 내부 진입점을 쓴다.
    bool TryOpenSelection(DepartureCompanionSelection selection, out string how)
    {
        how = "열기 실패";
        try
        {
            if (selection.IsOpen) { how = "이미 열림"; return true; }
            if (selection.Tutorial != null && selection.Tutorial.CompanionSelectionUnlocked)
            {
                selection.OpenAfterCertification();
                if (selection.IsOpen) { how = "OpenAfterCertification"; return true; }
            }
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (string name in new[] { "OpenDevelopmentSelection", "OpenSelection" })
            {
                MethodInfo method = typeof(DepartureCompanionSelection).GetMethod(name, flags, null, Type.EmptyTypes, null);
                if (method == null) continue;
                method.Invoke(selection, null);
                if (selection.IsOpen) { how = name + " (개발 진입)"; return true; }
            }
        }
        catch (Exception e)
        {
            Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
            Error("동행 선택 열기 예외: " + inner.Message);
        }
        return false;
    }

    IEnumerator StoryVoyage()
    {
        DepartureVoyagePresentation voyage = _voyage != null ? _voyage : FindFirstObjectByType<DepartureVoyagePresentation>();
        if (voyage == null) { Error("항해 연출 컴포넌트를 찾지 못했습니다."); yield break; }
        float end = Now + 30f;
        while (Now < end && voyage != null && !voyage.Sailing && !voyage.ArrivalFadeFinished) yield return null;
        if (voyage == null || !voyage.Sailing) { Error("항해(Sailing)가 시작되지 않았습니다."); yield break; }
        float total = Mathf.Max(1f, voyage.voyageSeconds);
        float[] targets = { 1.5f, 4f, 7.5f, 11f };
        for (int i = 0; i < targets.Length; i++)
        {
            float target = Mathf.Clamp(targets[i], .1f, total - .3f);
            while (voyage != null && voyage.Sailing && voyage.Elapsed < target) yield return null;
            if (voyage == null || !voyage.Sailing) { Error($"항해 {i + 1}번째 컷 전에 항해가 끝났습니다(target {target:F1}s)."); continue; }
            // 인코딩을 뒤로 미뤄 항해 시간이 촬영 때문에 건너뛰지 않게 한다.
            yield return ShotUI($"story_{4 + i:00}_voyage_{i + 1}_ui.png",
                $"voyage Elapsed={voyage.Elapsed:F2}s target={target:F2}s / {total:F1}s", true);
            Note($"voyage companions anim speed: {CompanionAnimSpeeds()}");
            // 포스터용: 레터박스·타이틀·자막 없이 같은 장면(다음 프레임)을 한 장 더. 인코딩은 뒤로 미룬다.
            if (voyage != null && voyage.Sailing)
                yield return ShotClean($"story_{4 + i:00}_voyage_{i + 1}_clean.png",
                    $"voyage Elapsed={voyage.Elapsed:F2}s · UI 없음", true);
        }
        FlushPending();
    }

    // 2026-11-04 사용자 제보(컷신 중 동행이 계속 달림) 확인: 화면 속 동행(플레이어 제외)의 애니메이션 이동 속도. 0 근처면 서 있음.
    string CompanionAnimSpeeds() => string.Join(", ", FindObjectsByType<NpcHumanoidProceduralAnimator>(FindObjectsSortMode.None)
        .Where(a => a != null && a.isActiveAndEnabled && a.GetComponentInParent<PlayerController>() == null)
        .Select(a => $"{a.transform.root.name}/{a.name}={a.CurrentPlanarSpeed:0.00}"));

    IEnumerator StoryArrival()
    {
        Type intro = FindType("ArrivalIntroPresentation");
        Note(intro != null ? "ArrivalIntroPresentation 발견 — 도착 연출 2컷 촬영" : "ArrivalIntroPresentation 없음 — 도착 연출 컷 생략");
        float end = Now + 120f;
        while (Now < end && SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) yield return null;
        if (SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) { Error("섬 씬(WorldSandbox)이 열리지 않았습니다."); yield break; }
        Note($"섬 씬 로드 t={Now - _startedAt:F1}s");
        if (intro != null)
        {
            float startBy = Now + 30f, playableSince = -1f;
            while (Now < startBy && !IntroPlaying(intro))
            {
                if (WorldPlayable()) { if (playableSince < 0f) playableSince = Now; else if (Now - playableSince > 6f) break; }
                yield return null;
            }
            if (IntroPlaying(intro))
            {
                float seenAt = Now, duration = IntroDuration(intro);
                float[] targets = duration > 1f ? new[] { duration * .3f, duration * .68f } : new[] { 1.2f, 3.6f };
                for (int i = 0; i < targets.Length; i++)
                {
                    float limit = Now + 30f;
                    while (Now < limit && IntroPlaying(intro) && IntroTime(intro, seenAt) < targets[i]) yield return null;
                    if (!IntroPlaying(intro)) { Error($"도착 연출 {i + 1}번째 컷 전에 연출이 끝났습니다."); break; }
                    yield return ShotUI($"story_{8 + i:00}_arrival_intro_{i + 1}_ui.png",
                        $"arrival intro t={IntroTime(intro, seenAt):F2}s / {(duration > 0f ? duration.ToString("F1", CultureInfo.InvariantCulture) : "?")}s", true);
                    Note($"arrival companions anim speed: {CompanionAnimSpeeds()}");
                }
                FlushPending();
                float doneBy = Now + 45f;
                while (Now < doneBy && IntroPlaying(intro)) yield return null;
            }
            else Note("도착 연출(IsPlaying)이 관측되지 않았습니다.");
        }
        float readyBy = Now + 90f;
        while (Now < readyBy && !WorldPlayable()) yield return null;
        if (!WorldPlayable()) { Error("섬 조작 복귀(DemoRouteController.IsPlayable)를 확인하지 못했습니다."); yield break; }
        yield return Wait(2.5f);
        yield return ShotUI("story_10_island_gameplay_ui.png", "섬 도착 뒤 첫 조작 화면");
        yield return ShotClean("story_11_island_gameplay_clean.png", "섬 도착 뒤 첫 조작 화면 · UI 없음");
    }

    // ───────────── plan: settlement ─────────────
    IEnumerator PlanSettlement()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 settlement 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke(); // 실제 이어하기 경로(PrepareContinue → WorldSandbox → 저장 복원)
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("day", HourShots(10.5f, "day", 1), 60f);
        yield return Step("golden", HourShots(17.6f, "golden", 2), 60f);
        yield return Step("night", HourShots(20.8f, "night", 3), 60f);
        yield return Step("interior", InteriorShots(), 45f);
    }

    // previousScene: 섬 → 섬 다시 열기(같은 씬 이름)에서는 옛 씬이 아직 IsPlayable이라 바로 통과했다(beach-reg150 저장 확인 실패).
    // 새 씬 인스턴스(handle)가 활성이 될 때까지 기다린다.
    IEnumerator WaitIslandAfterContinue(int previousScene = 0)
    {
        float end = Now + 60f;
        while (Now < end && (SceneManager.GetActiveScene().name != DemoRouteController.WorldScene || previousScene != 0 && SceneManager.GetActiveScene().handle == previousScene)) yield return null;
        if (previousScene != 0 && SceneManager.GetActiveScene().handle == previousScene) { Error("섬을 다시 열지 못했습니다(같은 씬 그대로)."); yield break; }
        if (SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) { Error("이어하기 뒤 섬 씬이 열리지 않았습니다."); yield break; }
        float ready = Now + 110f;
        while (Now < ready && !WorldPlayable())
        {
            if (SceneManager.GetActiveScene().name != DemoRouteController.WorldScene)
            { Error("저장 복원 실패로 타이틀로 돌아갔습니다(fixture 호환 확인 필요)."); yield break; }
            yield return null;
        }
        if (!WorldPlayable()) Error("이어하기 뒤 섬 조작 준비(IsPlayable)를 확인하지 못했습니다.");
        else Note($"이어하기 완료 t={Now - _startedAt:F1}s");
    }

    IEnumerator SettlementPrepare()
    {
        yield return Wait(3f);
        Transform player = PlayerTransform();
        if (player == null) Error("플레이어를 찾지 못했습니다.");
        ShopFrame shop = LocateShop(player);
        Note(shop.found
            ? $"상점: {shop.how} center={shop.bounds.center.ToString("F1")} size={shop.bounds.size.ToString("F1")} facade={shop.facade.ToString("F2")}"
            : "상점을 찾지 못해 플레이어 중심으로 구도를 잡습니다.");
        var residents = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.Residents : null;
        Note("동행 주민 " + (residents != null ? residents.Count : 0) + "명");
        if (PioneerReportCardUI.Instance != null && PioneerReportCardUI.Instance.IsOpen)
            Note("개척 보고서 카드가 열려 있어 UI 컷에 포함될 수 있습니다.");
        if (shop.interior != null && shop.interior.IsInside && shop.interior.exit != null && player != null)
        {
            Note("저장 위치가 상점 안이라 기존 문으로 밖에 나갑니다.");
            shop.interior.exit.Interact(player.gameObject);
            yield return Until(() => shop.interior == null || !shop.interior.IsInside, 8f);
            yield return Wait(1.2f);
        }
    }

    IEnumerator HourShots(float hour, string tag, int index)
    {
        string prefix = $"settlement_{index}_{tag}";
        SetHour(hour); yield return Wait(.5f);
        SetHour(hour); yield return Wait(1.5f); // 조명·NPC 일과 반영
        SetHour(hour);
        yield return ShotUI(prefix + "_a_gameplay_ui.png", $"일반 플레이 카메라 · {hour:F1}h");
        Camera cam = Camera.main;
        if (cam == null) { Error(prefix + ": Camera.main 없음"); yield break; }
        Transform player = PlayerTransform();
        ShopFrame shop = LocateShop(player);
        CameraHold hold = HoldCamera(cam);
        try
        {
            string how = PlaceEstablishing(cam, shop, player);
            yield return Wait(.4f); SetHour(hour);
            yield return ShotClean(prefix + "_b_establishing_clean.png", how);
            how = PlaceHero(cam, shop, player);
            yield return Wait(.4f); SetHour(hour);
            yield return ShotClean(prefix + "_c_hero_clean.png", how);
        }
        finally { ReleaseCamera(hold); }
        yield return Wait(.6f);
    }

    // 선택: 기존 문(BuildingEntrance.Interact)으로 들어가 실내 cutaway를 찍고 다시 나온다. 입력 시뮬레이션 없음.
    IEnumerator InteriorShots()
    {
        Transform player = PlayerTransform();
        ShopFrame shop = LocateShop(player);
        DemoShopInterior interior = shop.interior;
        if (player == null || interior == null || interior.entrance == null) { Note("상점 실내 진입점이 없어 실내 컷을 건너뜁니다."); yield break; }
        if (!interior.entrance.IsUnlocked) { Note("상점 문이 아직 잠겨 있어(정착 미완료) 실내 컷을 건너뜁니다."); yield break; }
        SetHour(10.5f);
        yield return Wait(1f);
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior == null || interior.IsInside, 8f);
        if (interior == null || !interior.IsInside) { Error("상점 실내 진입을 확인하지 못했습니다."); yield break; }
        yield return Wait(1.8f); // 워프 페이드·카메라 스냅 이후
        SetHour(10.5f);
        yield return ShotUI("settlement_4_interior_a_ui.png", "상점 실내 · 일반 플레이 카메라");
        yield return ShotClean("settlement_4_interior_b_clean.png", "상점 실내 · UI 없음");
        // 밤 영업 분위기의 실내(램프·창 조명)도 같은 구도로 남긴다.
        SetHour(20.8f); yield return Wait(1.2f); SetHour(20.8f);
        if (interior != null && interior.IsInside)
            yield return ShotClean("settlement_4_interior_c_night_clean.png", "상점 실내 · 밤 20.8h · UI 없음");
        if (interior != null && interior.exit != null) interior.exit.Interact(player.gameObject);
        yield return Until(() => interior == null || !interior.IsInside, 8f);
    }

    // ───────────── plan: feedback (2026-10-31 사용자 피드백 회귀 확인) ─────────────
    // 저장 사본으로 이어하기 → 키트·물고기·목재·도구를 들었을 때의 자세/조명 → 해안 낚시 → 동행과 연속 대화(낮·물고기 든 채·밤).
    // 대사 원문은 manifest notes에 남겨 화면 판독 없이도 문맥을 검토한다. 아이템 지급은 격리 저장 사본에서만 일어난다.
    IEnumerator PlanFeedback()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 feedback 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("held", FeedbackHeld(), 120f);
        yield return Step("fishing", FeedbackFishing(), 90f);
        yield return Step("talk", FeedbackTalk(), 90f);
    }

    static readonly (string path, string tag)[] HeldProbes =
    {
        ("FirstDayStudio/Item_2012", "shopkit"), ("FirstDayStudio/Item_2013", "tentkit"), ("FirstDayStudio/Item_2014", "workbench"),
        ("Items/Item_Fish", "fish"), ("Items/Item_Wood", "wood"), ("FirstDayStudio/Item_2011", "rod"), ("Items/Item_Axe", "axe"),
    };

    IEnumerator FeedbackHeld()
    {
        Transform player = PlayerTransform();
        if (player == null || Inventory.instance == null) { Error("플레이어/인벤토리 없음"); yield break; }
        int index = 1;
        foreach (var (path, tag) in HeldProbes)
        {
            var item = Resources.Load<Item>(path);
            if (item == null) { Note($"held {tag}: Resources/{path} 없음 — 건너뜀"); continue; }
            if (!SelectItem(item)) { Error($"held {tag}: 핫바에 넣지 못함"); continue; }
            SetHour(10.5f);
            yield return Wait(1.4f); // 자세 블렌드·배치 미리보기 생성
            var equipment = player.GetComponent<EquipmentSystem>();
            Note($"held {tag}: selected={item.itemName}#{item.id} visual={(equipment != null && equipment.HeldVisual != null ? equipment.HeldVisual.name : "없음")}");
            yield return ShotUI($"feedback_1_{index}_{tag}_ui.png", $"{tag} 들기 · 일반 카메라 10.5h");
            Camera cam = Camera.main;
            if (cam != null)
            {
                CameraHold hold = HoldCamera(cam);
                try
                {
                    Vector3 eye = player.position + player.forward * 2.3f + player.right * .7f + Vector3.up * 1.45f;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(player.position + Vector3.up * .85f - eye));
                    cam.fieldOfView = 38f;
                    yield return Wait(.25f);
                    yield return ShotClean($"feedback_1_{index}_{tag}_close_clean.png", $"{tag} 들기 · 정면 근접");
                }
                finally { ReleaseCamera(hold); }
            }
            index++;
        }
    }

    // 지정 아이템을 핫바에 두고 선택한다(없으면 빈 칸, 빈 칸도 없으면 마지막 칸을 덮는다 — 격리 저장 사본 전용).
    static bool SelectItem(Item item)
    {
        var inventory = Inventory.instance;
        if (inventory == null || inventory.hotbar == null || inventory.hotbar.slots == null) return false;
        int found = inventory.hotbar.slots.FindIndex(s => s != null && !s.IsEmpty && s.item == item);
        // 가방에 있으면(핫바가 가득 차 제작 결과가 가방으로 간 경우) 마지막 핫바 칸과 맞바꾼다. 덮어써서 재료를 잃지 않는다.
        int inBag = found < 0 && inventory.slots != null ? inventory.slots.FindIndex(s => s != null && !s.IsEmpty && s.item == item) : -1;
        if (inBag >= 0)
        {
            int empty = inventory.hotbar.slots.FindIndex(s => s != null && s.IsEmpty);
            found = empty >= 0 ? empty : inventory.hotbar.slots.Count - 1;
            var hotbarInstance = inventory.hotbar.slots[found].instance;
            inventory.hotbar.slots[found].SetInstance(inventory.slots[inBag].instance);
            if (hotbarInstance != null) inventory.slots[inBag].SetInstance(hotbarInstance); else inventory.slots[inBag].Clear();
            inventory.RefreshAllUI();
        }
        if (found < 0)
        {
            found = inventory.hotbar.slots.FindIndex(s => s != null && s.IsEmpty);
            if (found < 0) found = inventory.hotbar.slots.Count - 1;
            if (found < 0) return false;
            inventory.hotbar.slots[found].SetInstance(new ItemInstance(item, 1));
            inventory.RefreshAllUI();
        }
        inventory.SelectHotbarSlot(found);
        return true;
    }

    IEnumerator FeedbackFishing()
    {
        Transform player = PlayerTransform();
        var grid = FindFirstObjectByType<WorldGridService>();
        if (player == null || grid == null || !TryShore(grid, player.position, out Vector3 stand, out Vector3 water))
        { Error("해안 칸을 찾지 못해 낚시 컷을 건너뜁니다."); yield break; }
        SetHour(10.5f);
        Teleport(player, stand, water - stand);
        var rod = Resources.Load<Item>("FirstDayStudio/Item_2011");
        if (rod == null || !SelectItem(rod)) { Error("낚싯대를 선택하지 못했습니다."); yield break; }
        yield return Wait(3f); // 물고기 실루엣이 주변에 생길 시간
        Note($"fishing: stand={stand.ToString("F1")} water={water.ToString("F1")}");
        yield return ShotUI("feedback_2_1_shore_ui.png", "해안 · 낚싯대 든 상태(실루엣 확인)");
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraHold hold = HoldCamera(cam);
            try
            {
                Vector3 look = Vector3.Lerp(stand, water, .5f) + Flat(water - stand).normalized * 3f;
                Vector3 eye = look - Flat(water - stand).normalized * 6.5f + Vector3.up * 9f;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                yield return Wait(.25f);
                yield return ShotClean("feedback_2_2_shore_top_clean.png", "해안 물 위에서 본 실루엣");
            }
            finally { ReleaseCamera(hold); }
        }
        FishingSpot spot = null;
        float best = 4.5f;
        foreach (var candidate in FindObjectsByType<FishingSpot>(FindObjectsSortMode.None))
        {
            if (candidate == null || !candidate.isActiveAndEnabled) continue;
            float d = Flat(candidate.transform.position - player.position).magnitude;
            if (d < best) { best = d; spot = candidate; }
        }
        if (spot == null) { Error("플레이어 앞 4.5m 안에 낚시터(FishingSpot)가 없습니다."); yield break; }
        for (int cast = 1; cast <= 3; cast++)
        {
            Note($"fishing cast {cast}: prompt='{spot.GetInteractPrompt()}' phase={spot.Phase}");
            spot.Interact(player.gameObject);
            yield return Wait(1.6f);
            yield return ShotUI($"feedback_2_3_cast{cast}_ui.png", $"찌 던짐 {cast}회차 · phase={spot.Phase}");
            float end = Now + 14f;
            while (Now < end && spot.Phase != FishingSpot.DemoPhase.Bite) yield return null;
            Note($"fishing cast {cast}: phase={spot.Phase} after {14f - Mathf.Max(0f, end - Now):F1}s");
            if (spot.Phase == FishingSpot.DemoPhase.Bite)
                yield return ShotUI($"feedback_2_4_bite{cast}_ui.png", "입질");
            // 결과와 무관하게 다음 회차가 가능한지(일일 제한 없음) 확인하려고 줄을 거두고 다시 던진다.
            spot.Interact(player.gameObject);
            yield return Wait(2.5f);
            Note($"fishing cast {cast}: after reel-in phase={spot.Phase} prompt='{spot.GetInteractPrompt()}'");
        }
    }

    static bool TryShore(WorldGridService grid, Vector3 near, out Vector3 stand, out Vector3 water)
    {
        stand = water = Vector3.zero;
        if (!grid.WorldToCell(near, out var origin)) return false;
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        float bestDistance = float.MaxValue;
        for (int dx = -40; dx <= 40; dx++)
            for (int dz = -40; dz <= 40; dz++)
            {
                var land = origin + new Vector2Int(dx, dz);
                float distance = dx * dx + dz * dz;
                if (distance >= bestDistance || !grid.TryGetCell(land, out var cell) || !cell.IsWalkable) continue;
                foreach (var step in around)
                {
                    // 앞으로 3칸이 물이어야 찌와 실루엣이 보이는 트인 물가로 본다.
                    bool open = true;
                    for (int k = 1; k <= 3 && open; k++) open = grid.TryGetCell(land + step * k, out var w) && w.HasWater;
                    if (!open || !grid.CellToWorld(land, out var a) || !grid.CellToWorld(land + step * 2, out var b)) continue;
                    bestDistance = distance; stand = a; water = b;
                    break;
                }
            }
        return bestDistance < float.MaxValue;
    }

    static void Teleport(Transform player, Vector3 position, Vector3 facing)
    {
        var body = player.GetComponent<CharacterController>();
        if (body != null) body.enabled = false;
        player.position = position + Vector3.up * .05f;
        if (Flat(facing).sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(Flat(facing));
        if (body != null) body.enabled = true;
        Camera.main?.GetComponent<CameraController>()?.SnapToTarget();
    }

    IEnumerator FeedbackTalk()
    {
        Transform player = PlayerTransform();
        if (player == null) yield break;
        NpcDialogue npc = null;
        float best = float.MaxValue;
        foreach (var candidate in FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None))
        {
            if (candidate == null || !candidate.isActiveAndEnabled) continue;
            float d = Flat(candidate.transform.position - player.position).sqrMagnitude;
            if (d < best) { best = d; npc = candidate; }
        }
        if (npc == null) { Error("말을 걸 NPC(NpcDialogue)가 없습니다."); yield break; }
        var axe = Resources.Load<Item>("Items/Item_Axe");
        if (axe != null) SelectItem(axe);
        yield return TalkShots(npc, player, "day", 10.5f, 3);
        var fish = Resources.Load<Item>("Items/Item_Fish");
        if (fish != null && SelectItem(fish)) yield return TalkShots(npc, player, "fish", 11.2f, 1);
        yield return TalkShots(npc, player, "dusk", 17.8f, 1);
        yield return TalkShots(npc, player, "night", 21.5f, 2);
    }

    IEnumerator TalkShots(NpcDialogue npc, Transform player, string tag, float hour, int count)
    {
        SetHour(hour);
        for (int i = 1; i <= count && npc != null; i++)
        {
            Vector3 front = npc.transform.position + Flat(npc.transform.forward).normalized * 1.7f;
            Teleport(player, front, npc.transform.position - front);
            DialogueUI.instance?.Hide();
            yield return Wait(.6f);
            SetHour(hour);
            string prompt = npc.GetInteractPrompt();
            npc.Interact(player.gameObject);
            yield return Wait(2.4f); // 타자 효과 완료
            string speaker = DialogueUI.instance != null && DialogueUI.instance.nameText != null ? DialogueUI.instance.nameText.text : "?";
            Note($"talk {tag}#{i} {hour:F1}h held={Inventory.instance?.GetSelectedItem()?.itemName ?? "없음"} prompt='{prompt}' speaker='{speaker}' line='{npc.LastLine.Replace("\n", " / ")}'");
            yield return ShotUI($"feedback_3_talk_{tag}_{i}_ui.png", $"{tag} 대화 {i} · {hour:F1}h");
        }
        DialogueUI.instance?.Hide();
    }

    // ───────────── plan: motion (이동 애니메이션·조작감 시계열) ─────────────
    // 저장 사본으로 이어하기 → 빈손 → 트인 평지를 찾아 같은 입력 대본(정지·직진·정지·90° 전환·반대 전환·대각선·Shift 질주·정지)을 두 번 재생한다.
    // A: 실제 게임 카메라, B: 진행 방향 옆에서 따라가는 근접 카메라. 이동은 월드 방향(f=앞, r=오른쪽)으로 정하고 매 프레임 현재 카메라 기준
    // 입력으로 바꿔 넣는다(카메라를 바꿔도 같은 경로). 프레임은 captureFramerate로 고정 간격 JPG, 지지발 미끄럼을 구간별로 잰다.
    static readonly (string label, float forward, float right, bool run, float seconds)[] MotionScript =
    {
        ("idle", 0f, 0f, false, 1.6f), ("jog", 1f, 0f, false, 2.6f), ("stop", 0f, 0f, false, 1.2f),
        ("turn90", 0f, 1f, false, 1.6f), ("reverse", 0f, -1f, false, 1.6f), ("diag", -.7071f, -.7071f, false, 1.4f),
        ("sprint", 1f, 0f, true, 1.8f), ("stop2", 0f, 0f, false, 1.6f),
    };

    // ───────────── plan: crafting (2026-11-02 용도별 작업대·제작 창) ─────────────
    // 현장 작업대 → 조리대·화로 키트 제작 → 실제 핫바 배치 → 각 작업대 [E] 창에서 요리·제련·도구 제작 → 도감.
    // 재료는 격리 저장 사본에 FIXTURE로 넣는다(직접 채집은 P3·feedback에서 검증됨). 제작·배치는 실제 UI 버튼·배치 컨트롤러 경로.
    IEnumerator PlanCrafting()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 crafting 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("stations", CraftingStations(), 300f);
    }

    static Item Res(string path) => Resources.Load<Item>(path);

    IEnumerator CraftingStations()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        if (player == null || inventory == null || settlement == null) { Error("플레이어/인벤토리/정착지 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        var bench = settlement.PlacedWorkbench;
        if (bench == null)
        {
            Note("저장에 현장 작업대가 없어 보급 키트로 설치합니다.");
            yield return PlaceKit(player, Res("FirstDayStudio/Item_2014"), player.position);
            bench = settlement.PlacedWorkbench;
            if (bench == null) { Error("현장 작업대를 설치하지 못했습니다."); yield break; }
        }
        var grants = new (string path, int count)[] { ("Items/Item_Wood", 21), ("Items/Item_Ore", 10), ("Items/Item_Fish", 3),
            ("Items/Item_Fish_RedSnapper", 2), ("Items/Item_Fish_YellowTang", 2), ("Items/Item_Butterfly", 1) };
        foreach (var (path, count) in grants)
        {
            var item = Res(path);
            if (item == null || !inventory.TryReceiveToHotbar(item, count) && !inventory.AddInstance(new ItemInstance(item, count)))
                Error("FIXTURE 지급 실패: " + path);
        }
        inventory.RefreshAllUI();
        Note("FIXTURE Wood21 Ore10 Fish(참치3·도미2·옐로탱2) Butterfly1 — 직접 채집은 P3/feedback에서 검증");

        // 1) 현장 작업대
        var ui = CraftingUI.instance;
        Teleport(player, bench.transform.position - bench.transform.forward * 2.2f, bench.transform.forward);
        yield return Wait(.6f);
        bench.Interact(player.gameObject);
        yield return Wait(.6f);
        if (ui == null || !ui.IsOpen) { Error("현장 작업대 제작 창이 열리지 않았습니다."); yield break; }
        Note("workbench rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
        yield return ShotUI("crafting_1_workbench_ui.png", "현장 작업대 · 처음 열었을 때");
        yield return Craft(ui, 7, 7);
        yield return Pick(ui, 2023);
        yield return ShotUI("crafting_2_kitchenkit_ui.png", "현장 작업대 · 조리대 키트 상세(쓰임·재료)");
        yield return Craft(ui, 2023, 1);
        yield return Craft(ui, 2024, 1);
        yield return Pick(ui, 2025);
        yield return ShotUI("crafting_3_frame_ui.png", "현장 작업대 · 나비 표본 액자 상세");
        yield return Craft(ui, 2025, 1);
        ui.Close();
        yield return Wait(.4f);

        // 2) 조리대·화로 실제 배치
        Vector3 around = bench.transform.position;
        yield return PlaceKit(player, Res("DemoStructure/Item_FieldKitchenKit"), around);
        yield return PlaceKit(player, Res("DemoStructure/Item_FieldForgeKit"), around);
        var stations = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var kitchen = stations.FirstOrDefault(w => w.workbenchType == WorkbenchType.Kitchen);
        var forge = stations.FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        Note($"stations kitchen={(kitchen != null ? kitchen.transform.position.ToString("F1") : "없음")} forge={(forge != null ? forge.transform.position.ToString("F1") : "없음")}");

        // 3) 조리대: 생선구이 3종
        if (kitchen != null)
        {
            Teleport(player, kitchen.InteractionAnchor != null ? kitchen.InteractionAnchor.position : kitchen.transform.position - kitchen.transform.forward * 2.2f, kitchen.transform.position - player.position);
            yield return Wait(.4f);
            kitchen.Interact(player.gameObject);
            yield return Wait(.6f);
            Note("kitchen rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
            yield return ShotUI("crafting_4_kitchen_ui.png", "조리대 · 생선구이 3종(재료 이름으로 구분)");
            foreach (var recipe in ui.VisibleRecipes.ToArray())
            {
                ui.SelectRecipe(recipe); yield return Wait(.15f);
                if (ui.CraftButton.interactable) ui.CraftButton.onClick.Invoke();
                yield return Wait(.2f);
            }
            yield return ShotUI("crafting_5_kitchen_done_ui.png", "조리대 · 생선구이 제작 뒤");
            ui.Close(); yield return Wait(.3f);
        }
        else Error("조리대 배치 실패");

        // 4) 화로(2026-11-02 도구 단계): 구리 주괴 → 구리 도끼, 철 곡괭이는 철 주괴·광업 전문 조건으로 잠김 확인
        if (forge != null)
        {
            Teleport(player, forge.InteractionAnchor != null ? forge.InteractionAnchor.position : forge.transform.position - forge.transform.forward * 2.2f, forge.transform.position - player.position);
            yield return Wait(.4f);
            forge.Interact(player.gameObject);
            yield return Wait(.6f);
            Note("forge rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
            yield return Craft(ui, 2029, 2);
            yield return Pick(ui, 2030);
            yield return ShotUI("crafting_6_forge_ui.png", "화로 · 구리 도끼 상세(이전 도구 + 구리 주괴 2 + 나무 1)");
            yield return Craft(ui, 2030, 1);
            yield return Pick(ui, 2031);
            Note($"iron pickaxe: craftable={ui.CraftButton != null && ui.CraftButton.interactable}");
            yield return ShotUI("crafting_7_forge_done_ui.png", "화로 · 철 곡괭이 상세(철 주괴·전문 조건)");
            ui.Close(); yield return Wait(.3f);
        }
        else Error("화로 배치 실패");

        // 5) 도감 + 세 작업대 전경
        ui.OpenRecipeBook();
        yield return Wait(.5f);
        yield return ShotUI("crafting_8_recipebook_ui.png", "C 도감 · 데모에서 얻을 수 없는 농작물 레시피 숨김");
        ui.Close(); yield return Wait(.3f);
        Camera cam = Camera.main;
        if (cam != null && kitchen != null && forge != null)
        {
            CameraHold hold = HoldCamera(cam);
            try
            {
                Vector3 centre = (bench.transform.position + kitchen.transform.position + forge.transform.position) / 3f;
                Vector3 eye = centre + new Vector3(0f, 7.5f, -9f);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(centre + Vector3.up * .6f - eye));
                cam.fieldOfView = 40f;
                yield return Wait(.3f);
                yield return ShotClean("crafting_9_stations_clean.png", "현장 작업대·조리대·화로 배치 전경");
            }
            finally { ReleaseCamera(hold); }
        }
        string Count(int id) { var item = ItemRegistry.Instance?.Find(id, null); return item != null ? inventory.CountItems(item).ToString() : "?"; }
        Note($"RESULT plank={Count(7)} kitchenKit={Count(2023)} forgeKit={Count(2024)} frame={Count(2025)} grilled={Count(10)} skewer={Count(2032)} smoked={Count(2033)} copperBar={Count(2029)} copperAxe={Count(2030)} ironBar={Count(6)} wood={Count(3)} ore={Count(4)}");
    }

    IEnumerator Pick(CraftingUI ui, int outputId)
    {
        var recipe = ui.VisibleRecipes.FirstOrDefault(r => r.outputItem != null && r.outputItem.id == outputId);
        if (recipe == null) { Error("레시피 없음: 결과 id " + outputId); yield break; }
        ui.SelectRecipe(recipe);
        yield return Wait(.2f);
    }

    IEnumerator Craft(CraftingUI ui, int outputId, int times)
    {
        yield return Pick(ui, outputId);
        for (int i = 0; i < times; i++)
        {
            if (ui.CraftButton == null || !ui.CraftButton.interactable) { Error($"제작 불가: 결과 id {outputId} ({i}/{times})"); yield break; }
            ui.CraftButton.onClick.Invoke();
            yield return Wait(.25f);
        }
        Note($"crafted id {outputId} ×{times}");
    }

    // 실제 핫바 배치 경로: 키트 선택 → TryUseSelected(미리보기) → 설치 가능한 앞자리까지 둘러보며 RefreshPreview → Confirm.
    IEnumerator PlaceKit(Transform player, Item kit, Vector3 around, float[] radii = null)
    {
        var controller = Inventory.instance != null ? Inventory.instance.GetComponent<WorldHotbarPlacementController>() : null;
        if (kit == null || controller == null || !SelectItem(kit)) { Error("배치 준비 실패: " + (kit != null ? kit.name : "키트 없음")); yield break; }
        yield return Wait(.5f);
        foreach (float radius in radii ?? new[] { 4f, 6f, 8f, 10f, 13f, 16f })
            for (int a = 0; a < 12; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward;
                Vector3 spot = around + dir * radius;
                if (!Physics.Raycast(spot + Vector3.up * 30f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Teleport(player, hit.point, dir);
                yield return null;
                if (!controller.IsPlacing) { controller.TryUseSelected(); yield return null; }
                controller.RefreshPreview();
                yield return null;
                if (controller.IsPlacing && controller.PreviewResult.Succeeded)
                {
                    controller.Confirm();
                    yield return Wait(.6f);
                    if (!controller.IsPlacing) { Note($"placed {kit.name} at {player.position.ToString("F1")} r={radius}"); yield break; }
                }
            }
        controller.Cancel();
        Error("설치 가능한 자리를 찾지 못했습니다: " + kit.name + " (" + controller.Status + ")");
    }

    // ───────────── plan: decor (2026-11-03 가게 꾸밈 효과 검증 · CRAFTING_STATIONS §6-1) ─────────────
    // 첫 영업 전 저장 사본(보고서 rankKey 비움)으로 이어하기 → 상점 안에 의자 2개를 실제 핫바 배치로 놓기 → 진열 →
    // 밤에 영업 시작 → 그날 밤 관광객 상한·실제 방문 수를 기록한다. 같은 저장으로 의자 없이 돌린 값과 비교한다.
    IEnumerator PlanDecor()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 decor 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("decor", ShopDecor(), 240f);
    }

    IEnumerator ShopDecor()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        if (player == null || inventory == null || settlement == null || loop == null || settlement.OperatingShop == null) { Error("플레이어/정착지/상점 없음"); yield break; }
        int chairs = Mathf.Clamp(IntArg("-pa-decor-chairs", 2), 0, 3);
        SetHour(18.5f);
        var chair = Res("DemoStructure/Item_SimpleFurniture");
        // chairpaint 플랜: 작업대에서 칠한 의자(가방에 이미 있음)를 그대로 놓는다.
        var chairList = _decorChairs != null ? _decorChairs.ToList() : Enumerable.Repeat(chair, chairs).ToList();
        chairs = chairList.Count;
        int shells = Mathf.Clamp(IntArg("-pa-decor-shells", 0), 0, 3);
        var shellDecor = Res("Items/Item_ShellDecor");
        foreach (var (item, count) in new[] { (chair, _decorChairs != null ? 0 : Mathf.Max(1, chairs)), (Res("Items/Item_Fish"), 4), (shellDecor, Mathf.Max(1, shells)) })
            if (count > 0)
            if (item == null || !inventory.TryReceiveToHotbar(item, count) && !inventory.AddInstance(new ItemInstance(item, count))) Error("FIXTURE 지급 실패: " + item);
        inventory.RefreshAllUI();
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Wait(1.5f);
        Vector3 centre = player.position;
        for (int i = 0; i < chairs; i++) yield return PlaceKit(player, chairList[i], centre, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f });
        for (int i = 0; i < shells; i++) yield return PlaceKit(player, shellDecor, centre, new[] { 2f, 3f, 4f, 5f });
        Note($"decor placed chairs={chairs} shells={shells} counted={settlement.CountShopDecor()} seats={settlement.ShopChairs().Count}");
        // 빈 진열대에 생선을 실제 경로(들고 E)로 올린다.
        int stocked = 0;
        foreach (var slot in settlement.OperatingShop.Slots.Where(s => s != null && s.isActiveAndEnabled && s.IsEmpty).Take(2))
        {
            if (!SelectItem(Res("Items/Item_Fish"))) break;
            Teleport(player, slot.transform.position - slot.transform.forward * 1.2f, slot.transform.position - player.position);
            yield return Wait(.3f);
            slot.Interact(player.gameObject); stocked++;
            yield return Wait(.3f);
        }
        player.GetComponent<EquipmentSystem>()?.Holster();
        Note($"stocked {stocked} · nonEmpty={settlement.OperatingShop.Slots.Count(s => s != null && !s.IsEmpty)}");
        yield return ShotUI("decor_1_interior_ui.png", $"상점 안 의자 {chairs}개 · 진열");
        if (interior.exit != null) interior.exit.Interact(player.gameObject);
        yield return Until(() => !interior.IsInside, 8f);
        yield return Wait(1f);
        SetHour(20.2f);
        yield return Wait(1.5f);
        bool opened = loop.TryOpenShop();
        Note($"open={opened} sessionCompleted={loop.OpeningSessionCompleted} nightReady={settlement.NightReady}");
        yield return Wait(1.2f);
        var arrivals = CustomerArrivalController.Instance;
        Note($"tonight decor={settlement.ShopDecorTonight} maxTourists={arrivals?.maxTouristsPerOpening} luxury={arrivals?.touristLuxuryScale:F2} price={arrivals?.touristPriceSensitivityScale:F2}");
        yield return ShotUI("decor_2_open_ui.png", "영업 시작 알림(가게 꾸밈 손님 +n)");
        float end = Now + 80f;
        // 의자에 앉은 손님이 생기면 상점 안으로 들어가 찍는다(앉기 5.5초 안).
        if (chairs > 0)
        {
            while (Now < end && loop.IsShopOpenForCustomers && (arrivals?.SeatedThisOpening ?? 0) == 0) yield return null;
            if ((arrivals?.SeatedThisOpening ?? 0) > 0)
            {
                interior.entrance.Interact(player.gameObject);
                yield return Until(() => interior.IsInside, 6f);
                yield return Wait(1.2f);
                yield return ShotClean("decor_3_seated_clean.png", "쇼핑을 마친 손님이 의자에 앉아 쉼");
            }
            else Note("앉은 손님 없음");
        }
        while (Now < end && loop.IsShopOpenForCustomers) yield return null;
        Note($"RESULT decor={settlement.ShopDecorTonight} touristsSpawned={arrivals?.TouristsSpawnedThisOpening} seated={arrivals?.SeatedThisOpening} sales={settlement.PioneerReport?.sales ?? -1}");
    }

    // ───────────── plan: tools (2026-11-02 도구 단계 · TOOL_PROGRESSION.md) ─────────────
    // 저장 사본 이어하기 → 화로 확보 → 기본 곡괭이로 철 광맥(튕김) → 화로: 구리 주괴·구리 곡괭이·구리 도끼
    // → 구리 곡괭이로 철 광맥 채집·줍기 → 철 주괴 → 구리 곡괭이로 일반 바위·구리 도끼로 나무(휘두른 횟수) → 광업 전문 전후 철 곡괭이 잠김.
    IEnumerator PlanTools()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 tools 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("tiers", ToolTiers(), 360f);
    }

    IEnumerator ToolTiers()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        if (player == null || inventory == null || settlement == null || ui == null) { Error("플레이어/인벤토리/정착지/제작 창 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        var rocks = FindObjectsByType<MiningSpot>(FindObjectsSortMode.None).Where(m => m.IsDirectWorld && !m.DirectDepleted).ToArray();
        Note($"rocks total={rocks.Length} iron={rocks.Count(r => r.DirectHardness >= 2)} copper={rocks.Count(r => r.DirectHardness <= 1)}");
        foreach (var (path, count) in new[] { ("Items/Item_Ore", 8), ("Items/Item_Wood", 8), ("Items/Item_Plank", 2), ("DemoStructure/Item_FieldForgeKit", 1) })
        {
            var item = Res(path);
            if (item == null || !inventory.TryReceiveToHotbar(item, count) && !inventory.AddInstance(new ItemInstance(item, count))) Error("FIXTURE 지급 실패: " + path);
        }
        inventory.RefreshAllUI();
        Note("FIXTURE Ore8 Wood8 Plank2 ForgeKit1 — 철 광석은 실제 채집");
        string Count(int id) { var item = ItemRegistry.Instance?.Find(id, null); return item != null ? inventory.CountItems(item).ToString() : "?"; }
        var art = FirstDayStudioAssets.Load();
        Item starterPick = art.supplies.First(i => i != null && i.toolType == ToolType.Pickaxe);

        // 1) 기본 곡괭이로 철 광맥 → 튕김
        var iron = rocks.Where(r => r.DirectHardness >= 2).OrderBy(r => Flat(r.transform.position - player.position).sqrMagnitude).FirstOrDefault();
        if (iron == null) { Error("철 광맥 바위가 없습니다."); yield break; }
        Note($"iron rock {iron.name} at {iron.transform.position.ToString("F1")} prompt='{iron.GetInteractPrompt()}'");
        if (!SelectItem(starterPick)) Error("기본 곡괭이 선택 실패");
        yield return StandAt(player, iron.transform);
        int before = iron.DirectRemainingHits;
        iron.Interact(player.gameObject);
        yield return Wait(GatherFeedback.ContactDelay + .25f);
        Note($"bounce: hits {before}->{iron.DirectRemainingHits} feedback='{iron.LastFeedback}' use='{player.GetComponent<EquipmentSystem>()?.LastUseFeedback}'");
        yield return ShotUI("tools_1_iron_bounce_ui.png", "기본 곡괭이로 철 광맥 · 튕김 안내");

        // 2) 화로 배치·제작
        var forge = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        if (forge == null)
        {
            yield return PlaceKit(player, Res("DemoStructure/Item_FieldForgeKit"), settlement.PlacedWorkbench != null ? settlement.PlacedWorkbench.transform.position : player.position);
            forge = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        }
        if (forge == null) { Error("화로를 준비하지 못했습니다."); yield break; }
        yield return OpenStation(player, forge);
        Note("forge rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
        yield return Craft(ui, 2029, 4);
        yield return Pick(ui, 2022);
        yield return ShotUI("tools_2_forge_copper_ui.png", "화로 · 구리 곡괭이 상세");
        yield return Craft(ui, 2022, 1);
        yield return Craft(ui, 2030, 1);
        yield return Pick(ui, 2031);
        Note($"iron pickaxe before root: craftable={ui.CraftButton.interactable} root={settlement.SelectedRoot}");
        ui.Close(); yield return Wait(.3f);

        // 3) 구리 곡괭이로 철 광맥 채집 → 줍기
        if (!SelectItem(Res("DemoStructure/Item_ImprovedPickaxe"))) Error("구리 곡괭이 선택 실패");
        yield return StandAt(player, iron.transform);
        int swings = 0;
        while (!iron.DirectDepleted && swings < 8)
        {
            iron.Interact(player.gameObject); swings++;
            yield return Wait(GatherFeedback.ContactDelay + .35f);
            if (swings == 1) yield return ShotUI("tools_3_iron_mine_ui.png", "구리 곡괭이로 철 광맥 · 첫 타격");
        }
        Note($"iron mined with copper pickaxe: swings={swings} depleted={iron.DirectDepleted}");
        yield return Wait(.8f);
        yield return CollectDrops(player, iron.transform.position);
        Note($"after iron: ironOre={Count(2028)}");

        // 4) 철 주괴
        yield return OpenStation(player, forge);
        yield return Craft(ui, 6, 1);
        yield return Pick(ui, 2031);
        yield return ShotUI("tools_4_forge_iron_ui.png", "화로 · 철 주괴 뒤 철 곡괭이 상세");
        ui.Close(); yield return Wait(.3f);

        // 5) 구리 곡괭이로 일반 바위, 구리 도끼로 나무 — 휘두른 횟수
        var copperRock = rocks.Where(r => r != iron && r.DirectHardness <= 1 && !r.DirectDepleted).OrderBy(r => Flat(r.transform.position - player.position).sqrMagnitude).FirstOrDefault();
        if (copperRock != null)
        {
            if (!SelectItem(Res("DemoStructure/Item_ImprovedPickaxe"))) Error("구리 곡괭이 재선택 실패");
            yield return StandAt(player, copperRock.transform);
            int hits = copperRock.DirectRemainingHits; swings = 0;
            while (!copperRock.DirectDepleted && swings < 8) { copperRock.Interact(player.gameObject); swings++; yield return Wait(GatherFeedback.ContactDelay + .35f); }
            Note($"copper rock with copper pickaxe: hits={hits} swings={swings}");
            yield return Wait(.8f);
            yield return CollectDrops(player, copperRock.transform.position);
        }
        var tree = FindObjectsByType<Gatherable>(FindObjectsSortMode.None).Where(g => g.IsDirectWorld && !g.DirectDepleted)
            .OrderBy(g => Flat(g.transform.position - player.position).sqrMagnitude).FirstOrDefault();
        if (tree != null && SelectItem(Res("Items/Item_CopperAxe")))
        {
            yield return StandAt(player, tree.transform);
            int hits = tree.DirectRemainingHits; swings = 0;
            while (!tree.DirectDepleted && swings < 8) { tree.Interact(player.gameObject); swings++; yield return Wait(GatherFeedback.ContactDelay + .35f); }
            Note($"tree with copper axe: hits={hits} swings={swings}");
        }

        // 6) 광업 전문 전후 철 곡괭이 잠김
        if (settlement.SelectedRoot == DemoSpecialization.None) Note($"choose mining: {settlement.TryChooseRoot(DemoSpecialization.Mining)}");
        yield return OpenStation(player, forge);
        yield return Pick(ui, 2031);
        Note($"iron pickaxe after root={settlement.SelectedRoot}: craftable={ui.CraftButton.interactable}");
        yield return ShotUI("tools_5_iron_pickaxe_ui.png", "화로 · 광업 전문 선택 뒤 철 곡괭이(철 주괴 2 필요)");
        ui.Close();
        Note($"RESULT copperBar={Count(2029)} copperPick={Count(2022)} copperAxe={Count(2030)} ironOre={Count(2028)} ironBar={Count(6)} ore={Count(4)} wood={Count(3)}");
    }

    IEnumerator StandAt(Transform player, Transform target)
    {
        Vector3 from = player.position - target.position; from.y = 0f;
        Vector3 dir = from.sqrMagnitude > .01f ? from.normalized : Vector3.back;
        Vector3 spot = target.position + dir * 1.45f;
        if (Physics.Raycast(spot + Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) spot = hit.point;
        Teleport(player, spot, target.position - spot);
        yield return Wait(.4f);
    }

    IEnumerator OpenStation(Transform player, Workbench station)
    {
        Teleport(player, station.InteractionAnchor != null ? station.InteractionAnchor.position : station.transform.position - station.transform.forward * 2.2f, station.transform.position - player.position);
        yield return Wait(.4f);
        station.Interact(player.gameObject);
        yield return Wait(.6f);
        if (CraftingUI.instance == null || !CraftingUI.instance.IsOpen) Error("작업대 제작 창이 열리지 않았습니다: " + station.workbenchType);
    }

    IEnumerator CollectDrops(Transform player, Vector3 near)
    {
        // 게임 규칙: 바닥 드롭은 빈손일 때 [E]로 줍는다.
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return null;
        foreach (var pickup in FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None)
                     .Where(p => p != null && Flat(p.transform.position - near).magnitude < 3f).ToArray())
        {
            pickup.Interact(player.gameObject);
            yield return Wait(.3f);
        }
    }

    // ───────────── plan: beach (2026-11-03 해변 줍기, BEACHCOMBING.md) ─────────────
    // 저장 사본 이어하기 → 오늘 놓인 해변 발견물 수·종류를 기록 → 가장 가까운 발견물 앞 [E] 프롬프트 컷 → 빈손으로 차례로 줍기
    // → 작업대에서 조개 목걸이(가리비 2 + 소라 1) 제작. 주운 것이 모자라면 부족분만 FIXTURE로 채우고 notes에 남긴다.
    IEnumerator PlanBeach()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 beach 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("beachcomb", BeachComb(), 240f);
        yield return Step("beach-seaweed", BeachSeaweed(), 120f);
        yield return Step("beach-glass", BeachGlass(), 200f);
    }

    // 해변 유리(연구 H05·R06): 유리가 놓인 날을 찾아 실제 [E]로 줍고 → 작업대에서 유리 모빌(부족분 FIXTURE) → 실제 핫바 배치.
    IEnumerator BeachGlass()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var glass = Res("Items/Item_BeachGlass");
        if (player == null || inventory == null || settlement == null || glass == null) { Error("플레이어/정착지/유리 없음"); yield break; }
        InventoryFramework.PickupItem find = null;
        for (int day = 0; day < 8 && find == null; day++)
        {
            if (day > 0) { GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason); SetHour(10.5f); }
            yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Day == (GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1) && BeachFinds.Instance.Placed > 0, 8f);
            var finds = BeachFinds.Instance.Finds.Where(f => f != null).ToList();
            Note($"glass search day={BeachFinds.Instance.Day} kinds=" + string.Join(",", finds.GroupBy(f => ItemDisplayName.For(f.item)).Select(g => g.Key + "×" + g.Count())));
            find = finds.FirstOrDefault(f => f.item != null && f.item.id == 2047);
        }
        if (find == null) { Error("8일 동안 해변 유리가 놓이지 않았습니다."); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        Teleport(player, find.transform.position + Vector3.back * 2.4f, Vector3.forward);
        yield return Wait(1.2f);
        yield return ShotClean("beach_8_glass_clean.png", "모래 위 해변 유리 조각(기본 카메라)");
        // 같은 날 산호 조각(연구 H06)이 놓였으면 기본 카메라 크기도 남긴다.
        var coralFind = BeachFinds.Instance.Finds.FirstOrDefault(f => f != null && f.item != null && f.item.id == 2054);
        if (coralFind != null)
        {
            Teleport(player, coralFind.transform.position + Vector3.back * 2.4f, Vector3.forward);
            yield return Wait(1.2f);
            yield return ShotClean("beach_8b_coral_clean.png", "모래 위 산호 조각(기본 카메라)");
        }
        else Note("오늘 산호 조각 없음(촬영 건너뜀)");
        // 2026-11-04 레퍼런스 03: 물가에 반쯤 잠긴 납작 바위(표시 전용). 플레이어에서 가장 가까운 것을 모래 쪽에서 본다.
        var stones = CoastalVegetationField.WaterlineStones;
        Note($"waterline stones={stones.Count}");
        if (stones.Count > 0)
        {
            var origin = player.position;
            var stone = stones.OrderBy(s => (s.Shore - origin).sqrMagnitude).First();
            Vector3 look = stone.Stone - stone.Shore; look.y = 0;
            Teleport(player, stone.Shore - look.normalized * 1.2f, look.normalized);
            yield return Wait(1.2f);
            Note($"waterline shot stone={stone.Stone} shore={stone.Shore} player={player.position}");
            var field = FindFirstObjectByType<CoastalVegetationField>();
            if (field != null) Note("waterline near: " + field.QaNear(stone.Stone, 1.2f));
            // 바위 인스턴싱 변형 보존(2026-11-04) 뒤 물가 화면 프레임 측정: 3초 동안 평균·최악 프레임.
            float measureStart = Time.unscaledTime, worst = 0f; int frames = 0;
            while (Time.unscaledTime - measureStart < 3f) { yield return null; frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime); }
            Note($"waterline perf: avgFps={frames / (Time.unscaledTime - measureStart):F1} worstMs={worst * 1000f:F1} vSync={QualitySettings.vSyncCount} target={Application.targetFrameRate}");
            yield return ShotClean("beach_8c_waterline_clean.png", "물가에 반쯤 잠긴 납작 바위(기본 카메라, 레퍼런스 03)");
        }
        int before = inventory.CountItems(glass);
        yield return PressNear(player, find.transform);
        yield return Wait(.8f);
        yield return Until(() => DiscoveryLog.IsFound(2047), 2f);
        Note($"glass picked {before}→{inventory.CountItems(glass)} found={DiscoveryLog.IsFound(2047)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (inventory.CountItems(glass) <= before) { Error("해변 유리를 줍지 못했습니다."); yield break; }
        // 작업대 → 유리 모빌.
        if (inventory.CountItems(glass) < 2) inventory.TryReceiveToHotbar(glass, 2 - inventory.CountItems(glass));
        inventory.TryReceiveToHotbar(Res("Items/Item_Plank"), 1); inventory.TryReceiveToHotbar(Res("Items/Item_Wood"), 1);
        inventory.RefreshAllUI();
        var bench = settlement.PlacedWorkbench;
        var ui = CraftingUI.instance;
        if (bench == null || ui == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        Teleport(player, bench.transform.position - bench.transform.forward * 2.2f, bench.transform.forward);
        yield return Wait(.6f);
        bench.Interact(player.gameObject);
        yield return Wait(.6f);
        if (!ui.IsOpen) { Error("작업대 창이 열리지 않았습니다."); yield break; }
        yield return Pick(ui, 2048);
        yield return ShotUI("beach_9_mobile_recipe_ui.png", "현장 작업대 · 유리 모빌 상세");
        yield return Craft(ui, 2048, 1);
        ui.Close(); yield return Wait(.4f);
        var mobile = Res("Items/Item_GlassMobile");
        if (inventory.CountItems(mobile) < 1) { Error("유리 모빌을 만들지 못했습니다."); yield break; }
        int placedBefore = FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("GlassMobile"));
        yield return PlaceKit(player, mobile, bench.transform.position);
        yield return Wait(.6f);
        var placed = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("GlassMobile")).ToList();
        Note($"RESULT mobile placed {placedBefore}→{placed.Count} left={inventory.CountItems(mobile)} decorTonight={settlement.CountShopDecor()}");
        if (placed.Count <= placedBefore) { Error("유리 모빌이 배치되지 않았습니다."); yield break; }
        // 방금 놓은 자리(플레이어 옆)에 가장 가까운 모빌을 시야가 트인 방향에서 본다(가게 뒤에 놓이면 기본 카메라에선 지붕에 가린다).
        Vector3 placedAt = player.position;
        var m = placed.OrderBy(t => Flat(t.position - placedAt).magnitude).First();
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return ShotPlaced(m, "beach_10_mobile_placed_clean.png", "배치한 유리 모빌");
        // 저장 → 이어하기: 모빌 배치가 그대로인지(기존 배치 저장 경로).
        if (SaveManager.instance != null)
        {
            Vector3 savedAt = m.position;
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            var after = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("GlassMobile")).ToList();
            float nearest = after.Count > 0 ? after.Min(t => Flat(t.position - savedAt).magnitude) : -1f;
            Note($"save → continue: mobiles={after.Count} nearestToSaved={nearest:0.00}m found={DiscoveryLog.IsFound(2047)}");
            if (after.Count == 0 || nearest > .5f) Error("이어하기 뒤 유리 모빌 배치가 사라졌습니다.");
        }
    }

    // 해초(연구 H04): 해초가 놓인 날까지 아침을 넘겨 가며 종류 분포를 기록하고, 해초 하나를 실제 [E]로 줍는다.
    IEnumerator BeachSeaweed()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var seaweed = Res("Items/Item_Seaweed");
        if (player == null || inventory == null || seaweed == null) { Error("플레이어/해초 없음"); yield break; }
        InventoryFramework.PickupItem find = null;
        for (int day = 0; day < 6 && find == null; day++)
        {
            if (day > 0) { GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason); SetHour(10.5f); }
            yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Day == (GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1) && BeachFinds.Instance.Placed > 0, 8f);
            var finds = BeachFinds.Instance.Finds.Where(f => f != null).ToList();
            Note($"beach day={BeachFinds.Instance.Day} kinds=" + string.Join(",", finds.GroupBy(f => ItemDisplayName.For(f.item)).Select(g => g.Key + "×" + g.Count())));
            find = finds.FirstOrDefault(f => f.item != null && f.item.id == 2045);
        }
        if (find == null) { Error("6일 동안 해초가 놓이지 않았습니다."); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        Vector3 at = find.transform.position;
        Teleport(player, at + Vector3.back * 2.4f, Vector3.forward);
        yield return Wait(1.2f);
        yield return ShotClean("beach_6_seaweed_clean.png", "모래 위 해초(기본 카메라)");
        int before = inventory.CountItems(seaweed);
        yield return PressNear(player, find.transform);
        yield return Wait(.8f);
        yield return Until(() => DiscoveryLog.IsFound(2045), 2f);
        Note($"RESULT seaweed {before}→{inventory.CountItems(seaweed)} found={DiscoveryLog.IsFound(2045)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (inventory.CountItems(seaweed) <= before) Error("해초를 줍지 못했습니다.");
        yield return ShotUI("beach_7_seaweed_found_ui.png", "해초 첫 발견");
    }

    IEnumerator BeachComb()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        if (player == null || inventory == null) { Error("플레이어/인벤토리 없음"); yield break; }
        int Count(int id) { var item = ItemRegistry.Instance?.Find(id, null); return item != null ? inventory.CountItems(item) : 0; }
        yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Day > 0 && BeachFinds.Instance.Placed > 0, 12f);
        var beach = BeachFinds.Instance;
        if (beach == null || beach.Placed == 0) { Error($"해변 발견물이 놓이지 않았습니다 (candidates={beach?.CandidateCells ?? -1})"); yield break; }
        var finds = beach.Finds.Where(f => f != null).ToList();
        Note($"beach day={beach.Day} placed={beach.Placed} candidates={beach.CandidateCells} kinds=" +
             string.Join(",", finds.GroupBy(f => ItemDisplayName.For(f.item)).Select(g => g.Key + "×" + g.Count())));
        player.GetComponent<EquipmentSystem>()?.Holster();
        int before = Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish) + Count(2045) + Count(2047) + Count(2054);
        finds = finds.OrderBy(f => Flat(f.transform.position - player.position).sqrMagnitude).ToList();
        var first = finds[0];
        yield return StandAt(player, first.transform);
        yield return Wait(.8f);
        yield return ShotUI("beach_1_find_ui.png", $"해변 발견물 앞 · {ItemDisplayName.For(first.item)} · 프롬프트 '{first.GetInteractPrompt()}'");
        yield return ShotClean("beach_2_find_clean.png", "해변 발견물 · 모래 물가");
        int picked = 0;
        foreach (var find in finds.Take(7))
        {
            if (find == null) continue;
            yield return StandOnLand(player, find.transform);
            string name = ItemDisplayName.For(find.item);
            PressInteract(); // 실제 [E] 경로(빈손 맥락 줍기)
            yield return Wait(.5f);
            if (find == null || !find.gameObject.activeInHierarchy) { picked++; Note("picked " + name); }
            else Error($"줍지 못함: {name} held={EquipmentSystem.CurrentHeld(player.gameObject)?.itemName ?? "-"} selected={inventory.selectedHotbarIndex} " +
                       $"dist={Flat(find.transform.position - player.position).magnitude:0.00} facing={Vector3.Dot(player.forward, Flat(find.transform.position - player.position).normalized):0.00} listeners={LastPressListeners}");
            if (picked == 1) yield return ShotUI("beach_3_picked_ui.png", "첫 발견물 획득 직후(획득 연출·핫바)");
        }
        int after = Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish) + Count(2045) + Count(2047) + Count(2054);
        Note($"picked={picked} inventory {before}→{after} collectedToday={beach.CollectedToday} remaining={beach.Finds.Count(f => f != null)}");
        if (after - before != picked) Error($"주운 수와 가방 증가가 다릅니다: picked={picked} Δ={after - before}");

        // 한계 해결 확인(BEACHCOMBING.md §2): 격리 저장 → 같은 날 불러오기 → 주운 자리는 다시 놓이지 않는다.
        if (SaveManager.instance != null)
        {
            int remainingBefore = beach.Finds.Count(f => f != null);
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            // 진행 중인 섬에는 덮어 불러올 수 없다(SaveManager 규칙). 실제 플레이처럼 섬을 다시 열고 타이틀 '이어하기'로 들어온다.
            // 타이틀의 [이어하기]와 같은 경로: 저장 검사 → DemoRouteController.PrepareContinue → 섬 다시 열기.
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out string continueReason))
            { Error("다시 이어하기 준비 실패: " + (inspect.IsCompletedSuccessfully ? "PrepareContinue" : inspect.Exception?.Message)); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Day > 0, 10f);
            yield return Wait(1.5f);
            beach = BeachFinds.Instance;
            inventory = Inventory.instance;
            int remainingAfter = beach != null ? beach.Finds.Count(f => f != null) : -1;
            Note($"save → continue same day: remaining {remainingBefore}→{remainingAfter} collected={beach?.CollectedToday} inventory={Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish) + Count(2045) + Count(2047) + Count(2054)}");
            if (remainingAfter != remainingBefore) Error($"불러오기 뒤 해변 발견물이 다시 놓였습니다: {remainingBefore}→{remainingAfter}");
            player = PlayerTransform();
            player.GetComponent<EquipmentSystem>()?.Holster();
        }

        foreach (var (id, need, path) in new[] { (PA_BeachIds.Scallop, 2, "Items/Item_ScallopShell"), (PA_BeachIds.Conch, 1, "Items/Item_ConchShell") })
        {
            int lack = need - Count(id);
            if (lack <= 0) continue;
            inventory.TryReceiveToHotbar(Res(path), lack);
            Note($"FIXTURE {path} +{lack} (오늘 해변에서 부족분)");
        }
        inventory.RefreshAllUI();
        var settlement = DemoSettlementController.Instance;
        var bench = settlement != null ? settlement.PlacedWorkbench : null;
        if (bench == null) { Error("현장 작업대가 없어 목걸이 제작을 건너뜁니다."); yield break; }
        var ui = CraftingUI.instance;
        Teleport(player, bench.transform.position - bench.transform.forward * 2.2f, bench.transform.forward);
        yield return Wait(.6f);
        bench.Interact(player.gameObject);
        yield return Wait(.6f);
        if (ui == null || !ui.IsOpen) { Error("현장 작업대 제작 창이 열리지 않았습니다."); yield break; }
        yield return Pick(ui, PA_BeachIds.Necklace);
        yield return ShotUI("beach_4_necklace_ui.png", "현장 작업대 · 조개 목걸이 상세(쓰임·재료)");
        yield return Craft(ui, PA_BeachIds.Necklace, 1);
        yield return Wait(.4f);
        yield return ShotUI("beach_5_necklace_done_ui.png", "조개 목걸이 제작 직후");
        ui.Close();
        Note($"RESULT picked={picked} scallop={Count(PA_BeachIds.Scallop)} conch={Count(PA_BeachIds.Conch)} starfish={Count(PA_BeachIds.Starfish)} seaweed={Count(2045)} necklace={Count(PA_BeachIds.Necklace)}");
    }

    static class PA_BeachIds { public const int Scallop = 2034, Conch = 2035, Starfish = 2036, Necklace = 2037; }

    // ───────────── plan: dig (2026-11-03 삽·파기 자국, DIGGING.md) ─────────────
    // 저장 사본 이어하기 → 작업대에서 삽 제작(재료 부족분만 FIXTURE) → 삽을 들고 가까운 파기 자국 3곳을 파서 줍기
    // → 화로에서 구운 토기 항아리(점토 부족분만 FIXTURE). 파기 동작은 접촉 순간 프레임으로 남긴다.
    IEnumerator PlanDig()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 dig 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("dig", DigLoop(), 300f);
        yield return Step("dig-mosaic", DigMosaic(), 320f);
        yield return Step("dig-crystal", DigCrystal(), 320f);
        yield return Step("dig-amber", DigAmber(), 200f);
    }

    IEnumerator DigLoop()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        if (player == null || inventory == null || settlement == null || ui == null) { Error("플레이어/인벤토리/정착지/제작 창 없음"); yield break; }
        int Count(int id) { var item = ItemRegistry.Instance?.Find(id, null); return item != null ? inventory.CountItems(item) : 0; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Until(() => DigSpots.Instance != null && DigSpots.Instance.Day > 0 && DigSpots.Instance.Spots.Count > 0, 12f);
        var dig = DigSpots.Instance;
        if (dig == null || dig.Spots.Count == 0) { Error($"파기 자국이 놓이지 않았습니다 (candidates={dig?.CandidateCells ?? -1})"); yield break; }
        Note($"dig day={dig.Day} spots={dig.Spots.Count} candidates={dig.CandidateCells} loot=" +
             string.Join(",", dig.Spots.Where(s => s != null).GroupBy(s => ItemDisplayName.For(s.LootItem)).Select(g => g.Key + "×" + g.Count())));

        // 1) 작업대에서 삽(나무 2 + 구리 광석 1)
        foreach (var (path, id, need) in new[] { ("Items/Item_Wood", 3, 3), ("Items/Item_Ore", 4, 1) })
        {
            int lack = need - Count(id);
            if (lack > 0) { inventory.TryReceiveToHotbar(Res(path), lack); Note($"FIXTURE {path} +{lack}"); }
        }
        var bench = settlement.PlacedWorkbench;
        if (bench == null) { Error("현장 작업대가 없습니다."); yield break; }
        yield return OpenStation(player, bench);
        yield return Pick(ui, DigIds.Shovel);
        yield return ShotUI("dig_1_shovel_recipe_ui.png", "현장 작업대 · 삽 상세(쓰임·재료)");
        yield return Craft(ui, DigIds.Shovel, 1);
        ui.Close(); yield return Wait(.3f);
        var shovel = Res("Items/Item_Shovel");
        if (Count(DigIds.Shovel) < 1 || !SelectItem(shovel)) { Error("삽을 만들거나 들지 못했습니다."); yield break; }

        // 2) 가까운 파기 자국 3곳
        var spots = dig.Spots.Where(s => s != null && !s.Dug).OrderBy(s => Flat(s.transform.position - player.position).sqrMagnitude).Take(3).ToList();
        int before = Count(DigIds.Clay) + Count(DigIds.OldCoin) + Count(DigIds.Fossil) + Count(2049) + Count(2052) + Count(2055), dug = 0;
        var equipment = player.GetComponent<EquipmentSystem>();
        foreach (var spot in spots)
        {
            // 기본 카메라(요 0)는 플레이어 뒤에서 본다. 자국 옆(동쪽)에서 다가가야 자국·구멍이 몸에 가리지 않는다.
            Teleport(player, spot.transform.position + Vector3.right * 3.2f, Vector3.left);
            yield return Wait(.4f);
            if (dug == 0) yield return ShotClean("dig_0_mound_clean.png", "흙이 들썩이는 파기 자국(3m 옆)");
            yield return StandAt(player, spot.transform);
            SelectItem(shovel);
            yield return Wait(.5f);
            if (dug == 0) yield return ShotUI("dig_2_spot_prompt_ui.png", $"파기 자국 앞 · 삽을 든 프롬프트 '{spot.GetInteractPrompt()}'");
            string loot = ItemDisplayName.For(spot.LootItem);
            // 실제 [E] 경로(PlayerInteraction: 대상 선택 → Interact → PlayAction 삽 파기 자세).
            PressInteract();
            if (dug == 0)
            {
                yield return Wait(GatherFeedback.ContactDelay - .02f);
                yield return ShotClean("dig_3_contact_clean.png", "삽날이 땅에 닿는 순간");
            }
            yield return Wait(GatherFeedback.HeavyToolReturnDelay + .3f);
            if (!spot.Dug) { Error("파지 못함: " + loot); continue; }
            dug++;
            if (dug == 1) yield return ShotClean("dig_4_dug_clean.png", $"파낸 자리 · {loot} 드롭");
            yield return CollectDrops(player, spot.transform.position);
            Note($"dug #{dug} {loot} durability={ToolDurability.Remaining(inventory.GetSelectedInstance())}");
        }
        int after = Count(DigIds.Clay) + Count(DigIds.OldCoin) + Count(DigIds.Fossil) + Count(2049) + Count(2052) + Count(2055);
        Note($"dug={dug} loot {before}→{after} clay={Count(DigIds.Clay)} coin={Count(DigIds.OldCoin)} fossil={Count(DigIds.Fossil)} dugToday={dig.DugToday}");
        if (after <= before) Error("파낸 보상이 가방에 들어오지 않았습니다.");
        yield return ShotUI("dig_5_after_ui.png", "파기 3곳 뒤 핫바");

        // 저장 → 이어하기(같은 날): 판 자리는 다시 놓이지 않는다(해변 발견물과 같은 경로).
        if (SaveManager.instance != null)
        {
            int openBefore = dig.Spots.Count(s => s != null && !s.Dug);
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _))
            { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Until(() => DigSpots.Instance != null && DigSpots.Instance.Day > 0, 10f);
            yield return Wait(1.5f);
            dig = DigSpots.Instance;
            int openAfter = dig != null ? dig.Spots.Count(s => s != null && !s.Dug) : -1;
            Note($"save → continue same day: open spots {openBefore}→{openAfter} dugToday={dig?.DugToday}");
            if (openAfter != openBefore) Error($"이어하기 뒤 판 자리가 다시 놓였습니다: {openBefore}→{openAfter}");
            player = PlayerTransform(); inventory = Inventory.instance; settlement = DemoSettlementController.Instance; ui = CraftingUI.instance;
            bench = settlement != null ? settlement.PlacedWorkbench : null;
            if (bench == null || ui == null) { Error("이어하기 뒤 작업대/제작 창 없음"); yield break; }
        }

        // 3) 화로: 구운 토기 항아리(점토 3 + 나무 1)
        int clayLack = 3 - Count(DigIds.Clay);
        if (clayLack > 0) { inventory.TryReceiveToHotbar(Res("Items/Item_Clay"), clayLack); Note($"FIXTURE Clay +{clayLack} (오늘 파낸 점토 부족분)"); }
        if (Count(3) < 1) inventory.TryReceiveToHotbar(Res("Items/Item_Wood"), 1);
        var forge = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        if (forge == null)
        {
            inventory.TryReceiveToHotbar(Res("DemoStructure/Item_FieldForgeKit"), 1);
            Note("FIXTURE ForgeKit1");
            yield return PlaceKit(player, Res("DemoStructure/Item_FieldForgeKit"), bench.transform.position);
            forge = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        }
        if (forge == null) { Error("화로를 준비하지 못했습니다."); yield break; }
        yield return OpenStation(player, forge);
        yield return Pick(ui, DigIds.Jar);
        yield return ShotUI("dig_6_jar_ui.png", "화로 · 구운 토기 항아리 상세");
        yield return Craft(ui, DigIds.Jar, 1);
        ui.Close();
        Note($"RESULT dug={dug} shovel={Count(DigIds.Shovel)} clay={Count(DigIds.Clay)} coin={Count(DigIds.OldCoin)} fossil={Count(DigIds.Fossil)} jar={Count(DigIds.Jar)}");
    }

    // 물가 발견물: 물 칸이 아닌 쪽(8방향 중 땅 칸)에서 1.3m 앞에 선다(물 쪽에 세우면 안전 복귀로 먼 곳에 옮겨진다).
    IEnumerator StandOnLand(Transform player, Transform target)
    {
        var grid = FindFirstObjectByType<WorldGridService>();
        Vector3 best = target.position + Vector3.back * 1.3f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.back;
            Vector3 spot = target.position + dir * 1.3f;
            if (grid != null && grid.WorldToCell(spot, out var cell) && grid.TryGetCell(cell, out var data) && data.IsWalkable && !data.HasWater) { best = spot; break; }
        }
        if (Physics.Raycast(best + Vector3.up * 6f, Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) best.y = hit.point.y;
        Teleport(player, best, target.position - best);
        yield return Wait(.3f);
    }

    // 움직이는 대상 바로 앞(1.3m)에 세우고 같은 프레임 흐름에서 바로 [E](StandAt의 0.4초 대기 동안 걸어가 버리지 않게).
    IEnumerator PressNear(Transform player, Transform target)
    {
        Vector3 from = Flat(player.position - target.position);
        Vector3 spot = target.position + (from.sqrMagnitude > .01f ? from.normalized : Vector3.back) * 1.3f;
        // r172t3: 위 20m에서 내리쏘면 나무 갓에 맞아 플레이어를 갓 위에 올렸다(사과 줍기 실패) → 대상 높이 +2.5m에서 내리쏜다.
        Vector3 castFrom = new Vector3(spot.x, target.position.y + 2.5f, spot.z);
        if (Physics.Raycast(castFrom, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) spot = hit.point;
        Teleport(player, spot, target.position - spot);
        yield return null;
        PressInteract();
    }

    static string NearestAgent(UnityEngine.AI.NavMeshAgent self)
    {
        if (self == null) return "-";
        UnityEngine.AI.NavMeshAgent best = null; float bestDistance = float.MaxValue;
        foreach (var other in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
        {
            if (other == self || !other.isActiveAndEnabled) continue;
            float d = Flat(other.transform.position - self.transform.position).magnitude;
            if (d < bestDistance) { bestDistance = d; best = other; }
        }
        return best == null ? "none" : $"{best.name}@{bestDistance:0.00}m v={best.velocity.magnitude:0.0}";
    }

    // QA: [E] 입력 이벤트를 그대로 울린다(PlayerInteraction의 대상 선택·조건을 함께 검증).
    static void PressInteract()
    {
        var input = PlayerInputHandler.Instance;
        var field = typeof(PlayerInputHandler).GetField("OnInteractPressed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var handler = field?.GetValue(input) as System.Action;
        LastPressListeners = handler != null ? handler.GetInvocationList().Length : -1;
        handler?.Invoke();
    }
    static int LastPressListeners;

    // 배치물 확인 촬영: 사람이 보는 그대로 — 대상 앞 빈 바닥(8방향 중 막히지 않은 곳)에 플레이어를 세우고 기본 게임 카메라로 찍는다.
    // (2026-11-03 r91~r96: 가게 바닥에 놓인 경우 문을 거치지 않고 순간이동하면 실내 보기가 꺼져 지붕만 찍혔다 → 가게 안이면 문으로 들어간 뒤 찍고 나온다.)
    IEnumerator ShotPlaced(Transform target, string file, string label)
    {
        Transform player = PlayerTransform();
        if (player == null || target == null) yield break;
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        bool inShop = interior != null && interior.CoversGround(target.position);
        if (inShop && !interior.IsInside && interior.entrance != null)
        {
            interior.entrance.Interact(player.gameObject);
            yield return Until(() => interior.IsInside, 8f);
            yield return Wait(.6f);
        }
        Vector3 stand = target.position + Vector3.back * 2.2f;
        for (int k = 0; k < 8; k++)
        {
            Vector3 cand = target.position + Quaternion.Euler(0f, 180f + k * 45f, 0f) * Vector3.forward * 2.2f;
            if (!Physics.Raycast(cand + Vector3.up * 3f, Vector3.down, out var ground, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(ground.point.y - target.position.y) > .6f) continue;
            if (Physics.CheckCapsule(ground.point + Vector3.up * .4f, ground.point + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore)) continue;
            stand = ground.point; break;
        }
        Teleport(player, stand, target.position - stand);
        yield return Wait(1.2f);
        Note($"placed shot {file}: root={target.position.ToString("F2")} stand={stand.ToString("F1")} inShop={inShop} inside={interior?.IsInside}");
        yield return ShotClean(file, $"{label} {target.position.ToString("F1")}");
        if (inShop && interior.IsInside && interior.exit != null)
        {
            interior.exit.Interact(player.gameObject);
            yield return Until(() => !interior.IsInside, 8f);
        }
    }

    // 토기 조각·모자이크(연구 D04·R07): 토기 조각이 묻힌 날을 찾아 실제 [E]로 파고 → 화로에서 모자이크(부족분 FIXTURE)
    // → 실제 핫바 배치 → 저장→이어하기 유지.
    IEnumerator DigMosaic()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var ui = CraftingUI.instance;
        var shovel = Res("Items/Item_Shovel");
        var shardItem = Res("Items/Item_PotteryShard");
        if (player == null || inventory == null || ui == null || shovel == null || shardItem == null) { Error("파기 준비 실패"); yield break; }
        if (inventory.CountItems(shovel) < 1) inventory.TryReceiveToHotbar(shovel, 1);
        DigSpot target = null;
        for (int day = 0; day < 8 && target == null; day++)
        {
            if (day > 0) { GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason); SetHour(10.5f); }
            int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            yield return Until(() => DigSpots.Instance != null && DigSpots.Instance.Day == today && DigSpots.Instance.Spots.Count > 0, 8f);
            var spots = DigSpots.Instance.Spots.Where(s => s != null).ToList();
            Note($"shard search day={DigSpots.Instance.Day} loot=" + string.Join(",", spots.GroupBy(s => ItemDisplayName.For(s.LootItem)).Select(g => g.Key + "×" + g.Count())));
            target = spots.FirstOrDefault(s => !s.Dug && s.LootItem != null && s.LootItem.id == 2049);
        }
        if (target == null) { Error("8일 동안 토기 조각이 묻히지 않았습니다."); yield break; }
        int before = inventory.CountItems(shardItem);
        Teleport(player, target.transform.position + Vector3.right * 3.2f, Vector3.left);
        yield return Wait(.4f);
        yield return StandAt(player, target.transform);
        SelectItem(shovel);
        yield return Wait(.5f);
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + GatherFeedback.HeavyToolReturnDelay + .3f);
        if (!target.Dug) { Error("토기 조각 자리를 파지 못했습니다."); yield break; }
        yield return CollectDrops(player, target.transform.position);
        yield return Until(() => DiscoveryLog.IsFound(2049), 2f);
        Note($"shard dug {before}→{inventory.CountItems(shardItem)} found={DiscoveryLog.IsFound(2049)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (inventory.CountItems(shardItem) <= before) { Error("토기 조각이 가방에 들어오지 않았습니다."); yield break; }
        yield return ShotUI("dig_7_shard_found_ui.png", "토기 조각 첫 발견");
        int lack = 2 - inventory.CountItems(shardItem);
        if (lack > 0) { inventory.TryReceiveToHotbar(shardItem, lack); Note($"FIXTURE 토기 조각 +{lack}"); }
        if (inventory.CountItems(Res("Items/Item_Clay")) < 1) inventory.TryReceiveToHotbar(Res("Items/Item_Clay"), 1);
        inventory.RefreshAllUI();
        var forge = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Forge);
        if (forge == null) { Error("화로 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, forge);
        yield return Pick(ui, 2050);
        yield return ShotUI("dig_8_mosaic_recipe_ui.png", "화로 · 토기 모자이크 상세");
        yield return Craft(ui, 2050, 1);
        ui.Close(); yield return Wait(.4f);
        var mosaic = Res("Items/Item_PotteryMosaic");
        if (inventory.CountItems(mosaic) < 1) { Error("토기 모자이크를 만들지 못했습니다."); yield break; }
        var settlement = DemoSettlementController.Instance;
        Vector3 around = settlement != null && settlement.PlacedWorkbench != null ? settlement.PlacedWorkbench.transform.position : forge.transform.position;
        yield return PlaceKit(player, mosaic, around);
        yield return Wait(.6f);
        Vector3 placedAt = player.position;
        var placed = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("PotteryMosaic")).ToList();
        Note($"RESULT mosaic placed={placed.Count} left={inventory.CountItems(mosaic)} decorTonight={settlement?.CountShopDecor()}");
        if (placed.Count == 0) { Error("토기 모자이크가 배치되지 않았습니다."); yield break; }
        var m = placed.OrderBy(t => Flat(t.position - placedAt).magnitude).First();
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return ShotPlaced(m, "dig_9_mosaic_placed_clean.png", "배치한 토기 모자이크");
        if (SaveManager.instance != null)
        {
            Vector3 savedAt = m.position;
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-3", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            var after = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("PotteryMosaic")).ToList();
            float nearest = after.Count > 0 ? after.Min(t => Flat(t.position - savedAt).magnitude) : -1f;
            Note($"save → continue: mosaics={after.Count} nearestToSaved={nearest:0.00}m shardFound={DiscoveryLog.IsFound(2049)}");
            if (after.Count == 0 || nearest > .5f) Error("이어하기 뒤 토기 모자이크 배치가 사라졌습니다.");
        }
    }

    // 수정 원석·광물 받침(연구 D05·R08): 수정이 묻힌 날을 찾아 실제 [E]로 파고 → 현장 작업대에서 받침(부족분 FIXTURE)
    // → 실제 핫바 배치 → 저장→이어하기 유지.
    IEnumerator DigCrystal()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var ui = CraftingUI.instance;
        var shovel = Res("Items/Item_Shovel");
        var shardItem = Res("Items/Item_Crystal");
        if (player == null || inventory == null || ui == null || shovel == null || shardItem == null) { Error("파기 준비 실패"); yield break; }
        if (inventory.CountItems(shovel) < 1) inventory.TryReceiveToHotbar(shovel, 1);
        DigSpot target = null;
        for (int day = 0; day < 8 && target == null; day++)
        {
            if (day > 0) { GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason); SetHour(10.5f); }
            int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            yield return Until(() => DigSpots.Instance != null && DigSpots.Instance.Day == today && DigSpots.Instance.Spots.Count > 0, 8f);
            var spots = DigSpots.Instance.Spots.Where(s => s != null).ToList();
            Note($"crystal search day={DigSpots.Instance.Day} loot=" + string.Join(",", spots.GroupBy(s => ItemDisplayName.For(s.LootItem)).Select(g => g.Key + "×" + g.Count())));
            target = spots.FirstOrDefault(s => !s.Dug && s.LootItem != null && s.LootItem.id == 2052);
        }
        if (target == null) { Error("8일 동안 수정 원석이 묻히지 않았습니다."); yield break; }
        int before = inventory.CountItems(shardItem);
        Teleport(player, target.transform.position + Vector3.right * 3.2f, Vector3.left);
        yield return Wait(.4f);
        yield return StandAt(player, target.transform);
        SelectItem(shovel);
        yield return Wait(.5f);
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + GatherFeedback.HeavyToolReturnDelay + .3f);
        if (!target.Dug) { Error("수정 원석 자리를 파지 못했습니다."); yield break; }
        yield return CollectDrops(player, target.transform.position);
        yield return Until(() => DiscoveryLog.IsFound(2052), 2f);
        Note($"crystal dug {before}→{inventory.CountItems(shardItem)} found={DiscoveryLog.IsFound(2052)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (inventory.CountItems(shardItem) <= before) { Error("수정 원석이 가방에 들어오지 않았습니다."); yield break; }
        yield return ShotUI("dig_10_crystal_found_ui.png", "수정 원석 첫 발견");
        int lack = 1 - inventory.CountItems(shardItem);
        if (lack > 0) { inventory.TryReceiveToHotbar(shardItem, lack); Note($"FIXTURE 수정 원석 +{lack}"); }
        if (inventory.CountItems(Res("Items/Item_Plank")) < 1) inventory.TryReceiveToHotbar(Res("Items/Item_Plank"), 1);
        inventory.RefreshAllUI();
        var forge = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.PlacedWorkbench : null;
        if (forge == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, forge);
        yield return Pick(ui, 2053);
        yield return ShotUI("dig_11_stand_recipe_ui.png", "현장 작업대 · 작은 광물 받침 상세");
        yield return Craft(ui, 2053, 1);
        ui.Close(); yield return Wait(.4f);
        var mosaic = Res("Items/Item_CrystalStand");
        if (inventory.CountItems(mosaic) < 1) { Error("작은 광물 받침을 만들지 못했습니다."); yield break; }
        var settlement = DemoSettlementController.Instance;
        // 작업대 둘레의 놓을 자리는 앞 단계(모자이크)가 이미 차지했다 → 가게 바닥 가운데 둘레에서 찾는다.
        Vector3 around = settlement != null && settlement.OperatingShop != null ? settlement.OperatingShop.transform.position : forge.transform.position;
        yield return PlaceKit(player, mosaic, around, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 6f, 8f });
        yield return Wait(.6f);
        Vector3 placedAt = player.position;
        var placed = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("CrystalStand")).ToList();
        Note($"RESULT crystalStand placed={placed.Count} left={inventory.CountItems(mosaic)} decorTonight={settlement?.CountShopDecor()}");
        if (placed.Count == 0) { Error("작은 광물 받침이 배치되지 않았습니다."); yield break; }
        var m = placed.OrderBy(t => Flat(t.position - placedAt).magnitude).First();
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return ShotPlaced(m, "dig_12_stand_placed_clean.png", "배치한 작은 광물 받침");
        if (SaveManager.instance != null)
        {
            Vector3 savedAt = m.position;
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-4", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            var after = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("CrystalStand")).ToList();
            float nearest = after.Count > 0 ? after.Min(t => Flat(t.position - savedAt).magnitude) : -1f;
            Note($"save → continue: crystalStands={after.Count} nearestToSaved={nearest:0.00}m crystalFound={DiscoveryLog.IsFound(2052)}");
            if (after.Count == 0 || nearest > .5f) Error("이어하기 뒤 작은 광물 받침 배치가 사라졌습니다.");
        }
    }

    // 호박 조각(연구 D06, 6%): 묻힌 날을 찾아 실제 [E]로 파고 드롭·가방·도감을 확인한다(소장·판매 전용, 제작 없음).
    IEnumerator DigAmber()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var shovel = Res("Items/Item_Shovel");
        var amber = Res("Items/Item_AmberPiece");
        if (player == null || inventory == null || shovel == null || amber == null) { Error("파기 준비 실패"); yield break; }
        if (inventory.CountItems(shovel) < 1) inventory.TryReceiveToHotbar(shovel, 1);
        DigSpot target = null;
        for (int day = 0; day < 12 && target == null; day++)
        {
            GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason); SetHour(10.5f);
            int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            yield return Until(() => DigSpots.Instance != null && DigSpots.Instance.Day == today && DigSpots.Instance.Spots.Count > 0, 8f);
            target = DigSpots.Instance.Spots.FirstOrDefault(s => s != null && !s.Dug && s.LootItem != null && s.LootItem.id == 2055);
            Note($"amber search day={today} found={(target != null)}");
        }
        if (target == null) { Note("12일 동안 호박 조각이 묻히지 않음(6% · 하루 6곳) — 건너뜀"); yield break; }
        int before = inventory.CountItems(amber);
        Teleport(player, target.transform.position + Vector3.right * 3.2f, Vector3.left);
        yield return Wait(.4f);
        yield return StandAt(player, target.transform);
        SelectItem(shovel);
        yield return Wait(.5f);
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + GatherFeedback.HeavyToolReturnDelay + .3f);
        if (!target.Dug) { Error("호박 조각 자리를 파지 못했습니다."); yield break; }
        yield return ShotClean("dig_13_amber_drop_clean.png", "파낸 자리 · 호박 조각 드롭");
        yield return CollectDrops(player, target.transform.position);
        yield return Until(() => DiscoveryLog.IsFound(2055), 2f);
        Note($"RESULT amber {before}→{inventory.CountItems(amber)} found={DiscoveryLog.IsFound(2055)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (inventory.CountItems(amber) <= before) Error("호박 조각이 가방에 들어오지 않았습니다.");
        yield return ShotUI("dig_14_amber_found_ui.png", "호박 조각 첫 발견");
    }

    static class DigIds { public const int Shovel = 2038, Clay = 2039, OldCoin = 2040, Fossil = 2041, Jar = 2042; }

    // ───────────── plan: ui (가방·툴팁 화면 비교, 레퍼런스 08) ─────────────
    IEnumerator PlanUi()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 ui 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("bag", UiBag(), 60f);
    }

    IEnumerator UiBag()
    {
        var inventory = Inventory.instance;
        var bag = InventoryUI.instance;
        if (inventory == null || bag == null) { Error("인벤토리/가방 창 없음"); yield break; }
        foreach (var (path, count) in new[] { ("Items/Item_ScallopShell", 6), ("Items/Item_ConchShell", 2), ("Items/Item_Starfish", 1), ("Items/Item_Clay", 4),
            ("Items/Item_OldCoin", 1), ("Items/Item_FossilShard", 1), ("Items/Item_Fish", 3), ("Items/Item_Wood", 12), ("Items/Item_ShellNecklace", 1), ("Items/Item_FiredJar", 1), ("Items/Item_Shovel", 1),
            ("Items/Item_Seaweed", 2), ("Items/Item_BeachGlass", 1), ("Items/Item_PotteryShard", 1), ("Items/Item_CoralPiece", 1), ("Items/Item_Crystal", 1), ("Items/Item_AmberPiece", 1), ("Items/Item_SturdyRod", 1) })
        {
            var item = Res(path);
            if (item == null || !inventory.AddInstance(new ItemInstance(item, count))) Note("FIXTURE 가방 넣기 실패: " + path);
        }
        inventory.RefreshAllUI();
        Note("FIXTURE 가방 채우기(화면 비교용)");
        if (!bag.gameObject.activeSelf) bag.Toggle();
        yield return Wait(.6f);
        yield return ShotUI("ui_1_bag_ui.png", "가방(Tab) · 아이템 11종");
        var slot = bag.GetComponentsInChildren<InventorySlotUI>().FirstOrDefault(x => x.GetSlot() != null && !x.GetSlot().IsEmpty && x.GetSlot().item.id == 2041);
        if (slot != null && slot.tooltip != null)
        {
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position);
            slot.tooltip.Show(slot.GetSlot().item, at);
            yield return Wait(.3f);
            yield return ShotUI("ui_2_tooltip_ui.png", "가방 · 화석 조각 툴팁");
            string tip = slot.tooltip.descriptionText != null ? slot.tooltip.descriptionText.text : "";
            Note("fossil tooltip='" + tip.Replace("\n", " / ") + "'");
            if (!tip.Contains("소장")) Error("화석 조각 툴팁에 책상 위 소장 안내가 없습니다.");
            slot.tooltip.Hide();
        }
        else Note("툴팁 대상 칸 없음");
        // 도구 사용법 툴팁(삽·튼튼한 낚싯대: 설명 + 조작 한 줄).
        foreach (int toolId in new[] { 2038, 2027 })
        {
            var toolSlot = bag.GetComponentsInChildren<InventorySlotUI>().FirstOrDefault(x => x.GetSlot() != null && !x.GetSlot().IsEmpty && x.GetSlot().item.id == toolId);
            if (toolSlot == null || toolSlot.tooltip == null) { Note("툴팁 대상 칸 없음: " + toolId); continue; }
            toolSlot.tooltip.Show(toolSlot.GetSlot().item, RectTransformUtility.WorldToScreenPoint(null, toolSlot.transform.position));
            yield return Wait(.3f);
            Note($"tooltip {toolId}: '{toolSlot.tooltip.descriptionText.text.Replace("\n", " / ")}'");
            yield return ShotUI($"ui_2b_tooltip_{toolId}_ui.png", "가방 · 도구 툴팁 " + toolId);
            toolSlot.tooltip.Hide();
        }
        bag.Toggle();
        yield return Wait(.4f);

        // 휴대폰(홈·상점 앱)과 진열 가격 창 — DESIGN.md §6 "가격·폰 앱 규격" 비교용.
        var phone = SmartphoneUI.instance;
        if (phone != null)
        {
            if (!phone.IsOpen) phone.Toggle();
            yield return Wait(.6f);
            yield return ShotUI("ui_3_phone_ui.png", "휴대폰 홈");
            phone.OpenCollectionApp();
            yield return Wait(.6f);
            var plaqueItem = Res("Items/Item_CollectionPlaque");
            Note($"collection found={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} completions={DiscoveryLog.Completions} plaques={DiscoveryLog.PlaquesGiven} inBag={(plaqueItem != null ? inventory.CountItems(plaqueItem) : -1)}");
            if (DiscoveryLog.Completions > 0 && DiscoveryLog.PlaquesGiven != DiscoveryLog.Completions) Error("섹션 완성 수와 기념패 수가 다릅니다.");
            yield return ShotUI("ui_3b_phone_collection_ui.png", "휴대폰 도감");
            // 칸 상세(연구 §8.1): 발견한 것(가리비)·못 찾은 것(정어리)을 실제 칸 버튼으로 연다.
            var app = phone.GetComponentInChildren<CollectionPhoneUI>(true);
            // 섹션 탭: 해변 탭을 실제 버튼으로 누른 화면도 남긴다.
            var beachTab = app != null ? app.transform.Find("Tabs/Tab_해변")?.GetComponent<Button>() : null;
            if (beachTab != null) { beachTab.onClick.Invoke(); yield return Wait(.3f); yield return ShotUI("ui_3d_collection_tab_beach_ui.png", "도감 · 해변 탭"); }
            else Error("도감 해변 탭 버튼 없음");
            // 2026-11-09 곤충 8칸(섹션당 9칸 한도): 곤충 탭 배치·실루엣·힌트가 넘치지 않는지.
            var insectTab = app != null ? app.transform.Find("Tabs/Tab_곤충")?.GetComponent<Button>() : null;
            if (insectTab != null) { insectTab.onClick.Invoke(); yield return Wait(.3f); yield return ShotUI("ui_3e_collection_tab_insect_ui.png", "도감 · 곤충 탭(8칸)"); }
            else Error("도감 곤충 탭 버튼 없음");
            foreach (var (id, tag) in new[] { (2034, "found"), (2041, "fossil"), (3003, "unknown") })
            {
                string section = id == 3003 ? "물고기" : id == 2041 ? "땅속" : "해변";
                app?.transform.Find("Tabs/Tab_" + section)?.GetComponent<Button>()?.onClick.Invoke();
                yield return Wait(.2f);
                var button = app != null ? app.transform.Find("Section_" + section + "/Tile_" + id)?.GetComponent<Button>() : null;
                if (button == null) { Error("도감 칸 버튼 없음: " + id); continue; }
                button.onClick.Invoke();
                yield return Wait(.4f);
                Note($"collection detail {tag}: open={app.DetailOpen} id={app.DetailId} text='{app.DetailText.Replace("\n", " / ")}'");
                yield return ShotUI($"ui_3c_collection_detail_{tag}_ui.png", "도감 칸 상세 · " + tag);
                if (!app.DetailOpen || app.DetailId != id) Error("도감 칸 상세가 열리지 않았습니다: " + id);
                // 2026-11-09 쓰임 보강: 해변·땅속 발견물은 '소장(책상 위)', 화석 조각은 '선물(로건)'까지 — 넘치지 않는지.
                if (tag != "unknown" && DiscoveryLog.IsFound(id) && !app.DetailText.Contains("소장")) Error("발견물 상세에 소장(책상 위) 쓰임이 없습니다: " + id);
                if (tag == "fossil" && DiscoveryLog.IsFound(id) && !app.DetailText.Contains("로건")) Error("화석 조각 상세에 선물(로건) 쓰임이 없습니다.");
                if (app.DetailOverflow) Error("도감 상세 글이 칸을 넘칩니다: " + id);
                app.HideDetail();
                yield return Wait(.2f);
            }
            phone.OpenShopApp();
            yield return Wait(.6f);
            yield return ShotUI("ui_4_phone_shop_ui.png", "휴대폰 상점 앱");
            if (phone.IsOpen) phone.Toggle();
            yield return Wait(.4f);
        }
        else Note("휴대폰 없음");
        var settlement = DemoSettlementController.Instance;
        var price = ShopPriceUI.instance;
        var slotWithItem = settlement != null && settlement.OperatingShop != null ? settlement.OperatingShop.Slots.FirstOrDefault(x => x != null && !x.IsEmpty) : null;
        if (price != null && slotWithItem != null)
        {
            var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
            var player = PlayerTransform();
            if (interior != null && !interior.IsInside && player != null) interior.entrance.Interact(player.gameObject);
            yield return Until(() => interior == null || interior.IsInside, 6f);
            yield return Wait(.8f);
            price.Open(slotWithItem);
            yield return Wait(.6f);
            yield return ShotUI("ui_5_price_ui.png", "진열 가격 창");
            price.Close();
        }
        else Note("가격 창/진열 상품 없음");
    }

    // ───────────── plan: market (새 생활 콘텐츠 상품의 실제 판매 확인) ─────────────
    // 첫 영업 전 사본(make_decor_save) 이어하기 → 진열대에 조개 목걸이·토기 항아리·화석·옛 동전·소라·가리비를 들고 [E]로 올림
    // → 20:00 영업 → 손님이 무엇을 샀는지 판매 기록(SalesLogManager)으로 남긴다. 가격·판단은 기존 PurchaseEvaluator 그대로.
    IEnumerator PlanMarket()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 market 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("market", MarketNight(), 200f);
    }

    IEnumerator MarketNight()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        if (player == null || inventory == null || settlement == null || loop == null || settlement.OperatingShop == null) { Error("플레이어/정착지/상점 없음"); yield break; }
        SetHour(18.5f);
        var goods = new[] { ("Items/Item_Bug_Beetle", 1), ("Items/Item_ShellNecklace", 2), ("Items/Item_FiredJar", 2), ("Items/Item_SardinePlate", 2), ("Items/Item_SeaweedRoll", 2), ("Items/Item_FossilShard", 1), ("Items/Item_OldCoin", 2), ("Items/Item_ConchShell", 3), ("Items/Item_ScallopShell", 5) };
        foreach (var (path, count) in goods)
            if (!inventory.TryReceiveToHotbar(Res(path), count) && !inventory.AddInstance(new ItemInstance(Res(path), count))) Error("FIXTURE 지급 실패: " + path);
        inventory.RefreshAllUI();
        Note("FIXTURE 새 상품 " + string.Join(",", goods.Select(g => g.Item1.Replace("Items/Item_", "") + "×" + g.Item2)));
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Wait(1.2f);
        // 저장 사본의 진열(광석·나비·판재)은 기존 [E] 회수 경로로 가방에 되돌린 뒤 새 상품만 올린다.
        foreach (var slot in settlement.OperatingShop.Slots.Where(x => x != null && x.isActiveAndEnabled && !x.IsEmpty).ToList())
        { slot.RetrieveItem(); yield return Wait(.15f); }
        int stocked = 0, g = 0;
        foreach (var slot in settlement.OperatingShop.Slots.Where(x => x != null && x.isActiveAndEnabled && x.IsEmpty).ToList())
        {
            if (g >= goods.Length) break;
            var item = Res(goods[g++].Item1);
            if (!SelectItem(item)) continue;
            Teleport(player, slot.transform.position - slot.transform.forward * 1.2f, slot.transform.position - player.position);
            yield return Wait(.3f);
            slot.Interact(player.gameObject); stocked++;
            yield return Wait(.3f);
        }
        player.GetComponent<EquipmentSystem>()?.Holster();
        Note($"stocked {stocked} slots: " + string.Join(" | ", settlement.OperatingShop.Slots.Where(x => x != null && !x.IsEmpty).Select(x => $"{ItemDisplayName.For(x.currentItem.data)}×{x.currentItem.count}@{x.displayPrice}G")));
        yield return ShotUI("market_1_shelves_ui.png", "새 상품 진열");
        if (interior.exit != null) interior.exit.Interact(player.gameObject);
        yield return Until(() => !interior.IsInside, 8f);
        yield return Wait(1f);
        SetHour(20.2f);
        yield return Wait(1.5f);
        int before = SalesLogManager.Instance != null ? SalesLogManager.Instance.GetRecent(200).Count : 0;
        bool opened = loop.TryOpenShop();
        Note($"open={opened}");
        float end = Now + 90f;
        // 2026-11-04 사용자 피드백(영업 중 손님이 벽을 뚫고 다니고 서로 막음) 측정: 0.5초마다 앞벽(문 밖) 넘기·멈춤·겹침.
        // 같은 빌드 비교용 QA 인자: 앞벽 길막기 장애물을 끈 채로 잰다(-pa-qa-no-frontblock).
        if (System.Environment.GetCommandLineArgs().Contains("-pa-qa-no-frontblock"))
        {
            var block = settlement.OperatingShop.transform.root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "FrontWallNavBlock");
            if (block != null) block.gameObject.SetActive(false);
            Note($"QA front wall nav block disabled (found={block != null})");
        }
        // 2026-11-09 지식 라이브러리 A/B(Docs/DevKnowledge/techniques/navmesh-avoidance-prediction-time.md): 같은 빌드에서 회피 예측 시간만 바꿔 잰다.
        var args = System.Environment.GetCommandLineArgs();
        int avoidArg = System.Array.IndexOf(args, "-pa-qa-avoid-time");
        if (avoidArg >= 0 && avoidArg + 1 < args.Length && float.TryParse(args[avoidArg + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float avoidTime))
            UnityEngine.AI.NavMesh.avoidancePredictionTime = avoidTime;
        Note($"avoidancePredictionTime={UnityEngine.AI.NavMesh.avoidancePredictionTime:0.00}s");
        var flow = new CustomerFlow(settlement.OperatingShop.transform);
        bool doorShot = false; int openingsBefore = ShopDoorSwing.Openings;
        while (Now < end && loop.IsShopOpenForCustomers)
        {
            flow.Sample();
            // 손님이 문을 지날 때 열린 문을 한 장(표시 확인).
            if (!doorShot && ShopDoorSwing.CurrentAngle > 70f && FindObjectsByType<NpcController>(FindObjectsSortMode.None)
                    .Any(n => n != null && n.isActiveAndEnabled && n.GetComponent<DemoResident>() == null
                        && Flat(n.transform.position - settlement.OperatingShop.transform.TransformPoint(new Vector3(0f, 0f, -7.5f))).magnitude < 1.7f))
            {
                doorShot = true;
                yield return ShotClean("market_2_door_open_clean.png", "손님이 지날 때 열린 가게 문");
            }
            yield return null;
        }
        Note("customer flow: " + flow.Summary());
        Note($"shop door: openings {openingsBefore}→{ShopDoorSwing.Openings} shot={doorShot}");
        var sales = SalesLogManager.Instance != null ? SalesLogManager.Instance.GetRecent(200) : new List<SaleRecord>();
        var tonight = sales.Take(Mathf.Max(0, sales.Count - before)).ToList();
        Note("sales: " + string.Join(" | ", tonight.Select(r => $"{r.itemName} {r.price}G → {r.buyerName}")));
        Note($"RESULT sold={tonight.Count} revenue={tonight.Sum(r => r.price)} kinds={string.Join(",", tonight.GroupBy(r => r.itemName).Select(x => x.Key + "×" + x.Count()))}");
    }

    // 손님 동선 측정(표시 없음): 매 프레임 가게 기준 좌표로 앞벽(z -7.3, 문 틈 |x|<.9 제외)을 걸어서 넘은 횟수,
    // 한 프레임 3m 넘는 순간이동(따로 셈), 경로가 남았는데 멈춘 시간, 0.6m 안 겹침(0.5초마다).
    sealed class CustomerFlow
    {
        const float FrontZ = -7.3f, DoorHalf = .9f, HalfWidth = 5.1f, JumpMetres = 3f;
        readonly Transform _shop;
        readonly Dictionary<int, Vector3> _last = new Dictionary<int, Vector3>();
        readonly Dictionary<int, float> _stuck = new Dictionary<int, float>();
        readonly Dictionary<int, string> _lastLink = new Dictionary<int, string>();
        // 가게 안 자연스러움: 움직이는 동안 방향이 꺾인 각(도/초), 경로가 남았는데 절반 속도 아래로 머뭇거린 시간.
        readonly Dictionary<int, Vector3> _lastDir = new Dictionary<int, Vector3>();
        float _turnDeg, _moveSec, _slowSec;
        readonly List<string> _wallAt = new List<string>(), _jumps = new List<string>();
        int _wallCross, _doorCross, _jumpCount, _crowd, _frames;
        float _stuckTotal, _crowdAt;
        public CustomerFlow(Transform shop) { _shop = shop; }
        public void Sample()
        {
            _frames++;
            float dt = Time.deltaTime;
            var customers = FindObjectsByType<NpcController>(FindObjectsSortMode.None)
                .Where(n => n != null && n.isActiveAndEnabled && n.GetComponent<DemoResident>() == null).ToList();
            foreach (var npc in customers)
            {
                int id = npc.GetInstanceID();
                Vector3 local = _shop.InverseTransformPoint(npc.transform.position);
                if (_last.TryGetValue(id, out var prev))
                {
                    float step = Flat(local - prev).magnitude;
                    if (step > JumpMetres)
                    {
                        _jumpCount++;
                        var ja = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        _lastLink.TryGetValue(id, out string link); link ??= "-";
                        if (_jumps.Count < 6) _jumps.Add($"{npc.name}:{npc.currentState} ({prev.x:0.0},{prev.z:0.0})→({local.x:0.0},{local.z:0.0}) dt={dt:0.000} link={link} " +
                            $"path={(ja != null ? ja.pathStatus.ToString() : "-")} spd={(ja != null ? ja.speed : 0f):0.0} y={prev.y:0.0}→{local.y:0.0}");
                    }
                    else if ((prev.z - FrontZ) * (local.z - FrontZ) < 0f)
                    {
                        float x = Mathf.Lerp(prev.x, local.x, Mathf.InverseLerp(prev.z, local.z, FrontZ));
                        if (Mathf.Abs(x) <= HalfWidth)
                        {
                            if (Mathf.Abs(x) > DoorHalf) { _wallCross++; if (_wallAt.Count < 12) _wallAt.Add($"x{x:0.0}{(local.z > prev.z ? "in" : "out")}"); }
                            else _doorCross++;
                        }
                    }
                }
                _last[id] = local;
                var agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh && agent.isOnOffMeshLink)
                {
                    var data = agent.currentOffMeshLinkData;
                    Vector3 a = _shop.InverseTransformPoint(data.startPos), b = _shop.InverseTransformPoint(data.endPos);
                    _lastLink[id] = $"{(data.owner != null ? data.owner.name : data.linkType.ToString())}[{a.x:0.0},{a.z:0.0}→{b.x:0.0},{b.z:0.0}]";
                }
                else _lastLink[id] = "-";
                if (agent != null && agent.isOnNavMesh && agent.hasPath && !agent.pathPending
                    && agent.remainingDistance > agent.stoppingDistance + .5f && agent.velocity.magnitude < .08f)
                {
                    _stuck.TryGetValue(id, out float t); _stuck[id] = t + dt; _stuckTotal += dt;
                }
                bool inside = Mathf.Abs(local.x) <= HalfWidth && local.z > FrontZ && local.z < -FrontZ;
                Vector3 v = agent != null ? Flat(agent.velocity) : Vector3.zero;
                if (inside && v.magnitude > .3f)
                {
                    if (_lastDir.TryGetValue(id, out var pd)) _turnDeg += Vector3.Angle(pd, v);
                    _lastDir[id] = v.normalized; _moveSec += dt;
                    if (agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + 1f && v.magnitude < .5f * agent.speed) _slowSec += dt;
                }
                else _lastDir.Remove(id);
            }
            if (Time.time < _crowdAt) return;
            _crowdAt = Time.time + .5f;
            for (int i = 0; i < customers.Count; i++)
                for (int j = i + 1; j < customers.Count; j++)
                    if (Flat(customers[i].transform.position - customers[j].transform.position).magnitude < .6f) _crowd++;
        }
        public string Summary() => $"frames={_frames} customers={_last.Count} wallCross={_wallCross} doorCross={_doorCross} jumps={_jumpCount} " +
            $"stuckSec={_stuckTotal:0.0} worstStuck={(_stuck.Count > 0 ? _stuck.Values.Max() : 0f):0.0}s crowdPairs={_crowd} " +
            $"insideMoveSec={_moveSec:0.0} turnRate={(_moveSec > 0f ? _turnDeg / _moveSec : 0f):0}deg/s hesitateSec={_slowSec:0.0} " +
            $"wallAt=[{string.Join(" ", _wallAt)}] jumpsAt=[{string.Join(" | ", _jumps)}]";
    }

    // ───────────── plan: gift (2026-11-03 동행 선물 → 친밀도 하트) ─────────────
    IEnumerator PlanGift()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 gift 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("gift", GiftRun(), 90f);
    }

    IEnumerator GiftRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        if (player == null || inventory == null) { Error("플레이어/인벤토리 없음"); yield break; }
        foreach (var path in new[] { "Items/Item_FossilShard", "Items/Item_ScallopShell" })
            inventory.TryReceiveToHotbar(Res(path), 1);
        inventory.RefreshAllUI();
        Note("FIXTURE 화석 조각 1·가리비 껍데기 1");
        var residents = FindObjectsByType<DemoResident>(FindObjectsSortMode.None).Where(r => r.CompanionId != null).OrderBy(r => r.CompanionId).ToList();
        var miner = residents.FirstOrDefault(r => r.CompanionId.StartsWith("Miner"));
        var other = residents.FirstOrDefault(r => r != miner);
        if (miner == null || other == null) { Error("동행 두 명 없음"); yield break; }
        var service = FriendshipService.Instance;
        // 광부와 먼저 빈손으로 이야기(+2)한 뒤 좋아하는 화석(+8)을 주면 10점 → 친밀도 1단계 → 답례(구리 주괴 ×2).
        int barsBefore = inventory.CountItems(Res("Items/Item_CopperBar"));
        foreach (var (resident, path, tag) in new[] { (miner, "Items/Item_FossilShard", "liked"), (other, "Items/Item_ScallopShell", "plain") })
        {
            var talk = resident.GetComponent<NpcDialogue>();
            // 실제 [E]로 빈손 대화(+2) → 동행이 멈춰 플레이어를 본다(HoldForTalk) → 물건을 꺼내 다시 [E].
            player.GetComponent<EquipmentSystem>()?.Holster();
            yield return PressNear(player, resident.transform);
            yield return Wait(.6f);
            Note($"talk via E: open={DialogueUI.TalkingWith(resident.transform)} points={(service != null ? service.GetPoints(talk.friendshipId) : -1)}");
            int before = service != null && talk != null ? service.GetPoints(talk.friendshipId) : -1;
            if (!SelectItem(Res(path))) { Error("선물 선택 실패: " + path); continue; }
            yield return StandAt(player, resident.transform);
            yield return Wait(.5f);
            string prompt = resident.GetInteractPrompt();
            yield return ShotUI($"gift_{tag}_1_prompt_ui.png", $"선물 프롬프트 '{prompt}'");
            // 실제 [E]와 같은 경로: PlayerInputHandler.OnInteractPressed → PlayerInteraction.TryInteract(대상 고르기 포함).
            // 촬영 사이 동행이 걸어갔을 수 있어 다시 옆에 선 다음 프레임에 누른다.
            float pressDist = Flat(resident.transform.position - player.position).magnitude;
            // 앞 대화가 떠 있으면 실제 [E] 한 번으로 넘긴다(대화 넘기기 규칙). 글자가 나오는 중이면 두 번(다 보이기 → 닫기).
            for (int close = 0; close < 2 && DialogueUI.IsOpen; close++) { PressInteract(); yield return Wait(.15f); }
            Note($"dialogue closed by E: open={DialogueUI.IsOpen} advances={DialogueUI.Advances}");
            yield return PressNear(player, resident.transform);
            Note($"press at dist={pressDist:0.00}→{Flat(resident.transform.position - player.position).magnitude:0.00}");
            var tracer = resident.GetComponent<PositionPhaseTracer>() ?? resident.gameObject.AddComponent<PositionPhaseTracer>();
            tracer.ResetCounters();
            Vector3 p0 = player.position, r0 = resident.transform.position;
            // 프레임별 진단: 동행이 움직인 프레임의 상태(대화·멈춤·속도·경로)를 남긴다.
            var diagRoutine = resident.GetComponent<DemoCompanionRoutine>();
            var diagAgent = diagRoutine != null ? diagRoutine.Agent : null;
            var log = new System.Text.StringBuilder();
            Vector3 last = resident.transform.position;
            float diagEnd = Now + .8f; int frame = 0;
            while (Now < diagEnd)
            {
                yield return null; frame++;
                float step = Flat(resident.transform.position - last).magnitude;
                last = resident.transform.position;
                if (step > .003f && log.Length < 1400)
                    log.Append($"[f{frame} step={step:0.000} talk={DialogueUI.TalkingWith(resident.transform)} stopped={diagAgent?.isStopped} vel={diagAgent?.velocity.magnitude:0.00} " +
                               $"onMesh={diagAgent?.isOnNavMesh} paused={diagRoutine?.TalkPaused} agents={resident.GetComponents<UnityEngine.AI.NavMeshAgent>().Length} " +
                               $"next-tr={(diagAgent != null ? Flat(diagAgent.nextPosition - resident.transform.position).magnitude : -1):0.000} hasPath={diagAgent?.hasPath} dvel={diagAgent?.desiredVelocity.magnitude:0.00} " +
                               $"near={NearestAgent(diagAgent)}] ");
            }
            Note($"moved player={Flat(player.position - p0).magnitude:0.00} resident={Flat(resident.transform.position - r0).magnitude:0.00} frames={frame} " +
                 $"phase: {tracer.Summary}");
            Note($"emote bursts={NpcEmote.Bursts}");
            yield return ShotUI($"gift_{tag}_1b_emote_ui.png", "선물 순간 · 머리 둘레 별");
            var routine = resident.GetComponent<DemoCompanionRoutine>();
            Note($"after gift .8s: dist={Flat(resident.transform.position - player.position).magnitude:0.00} talkPaused={routine?.TalkPaused} stopped={routine?.Agent?.isStopped} vel={routine?.Agent?.velocity.magnitude:0.00} speaker={(DialogueUI.Speaker != null ? DialogueUI.Speaker.name : "-")} open={DialogueUI.IsOpen} activity={routine?.Current}");
            yield return Wait(1.6f);
            int after = service != null && talk != null ? service.GetPoints(talk.friendshipId) : -1;
            Note($"press listeners={LastPressListeners} dist={Flat(resident.transform.position - player.position).magnitude:0.00} facing={Vector3.Dot(player.forward, Flat(resident.transform.position - player.position).normalized):0.00} held={EquipmentSystem.CurrentHeld(player.gameObject)?.itemName}");
            Note($"gift {tag} {resident.CompanionId} item={path} prompt='{prompt}' points {before}→{after} line='{talk?.LastLine}' hearts={DialogueUI.HeartsShown}");
            yield return ShotUI($"gift_{tag}_2_reaction_ui.png", "선물 반응 · 이름표 하트");
            if (after <= before) Error("선물 뒤 친밀도가 오르지 않았습니다: " + resident.CompanionId);
            DialogueUI.instance?.Hide();
        }
        // 같은 날 두 번째 선물은 받지 않는다(물건도 그대로).
        inventory.TryReceiveToHotbar(Res("Items/Item_ScallopShell"), 1);
        SelectItem(Res("Items/Item_ScallopShell"));
        int shells = inventory.CountItems(Res("Items/Item_ScallopShell"));
        yield return StandAt(player, miner.transform);
        miner.Interact(player.gameObject);
        yield return Wait(.8f);
        Note($"second gift same day: shells {shells}→{inventory.CountItems(Res("Items/Item_ScallopShell"))} line='{miner.GetComponent<NpcDialogue>()?.LastLine}'");
        int barsAfter = inventory.CountItems(Res("Items/Item_CopperBar"));
        Note($"RESULT gifts={residents.Sum(r => r.GiftsGiven)} thanks={residents.Sum(r => r.ThanksGiven)} copperBar {barsBefore}→{barsAfter} minerLevel={service?.GetLevel(miner.GetComponent<NpcDialogue>().friendshipId)}");
        if (barsAfter <= barsBefore) Error("친밀도 1단계 답례(구리 주괴)가 들어오지 않았습니다.");

        // 저장 → 이어하기(같은 날): 친밀도 점수·오늘 선물 기록·도감이 그대로인지.
        if (SaveManager.instance != null)
        {
            string minerId = miner.GetComponent<NpcDialogue>().friendshipId;
            int pointsBefore = service.GetPoints(minerId), foundBefore = DiscoveryLog.FoundCount;
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            service = FriendshipService.Instance;
            var minerAfter = FindObjectsByType<DemoResident>(FindObjectsSortMode.None).FirstOrDefault(r => r.CompanionId != null && r.CompanionId.StartsWith("Miner"));
            inventory = Inventory.instance;
            var fossil = Res("Items/Item_FossilShard");
            inventory.TryReceiveToHotbar(fossil, 1); SelectItem(fossil);
            string prompt = minerAfter != null ? minerAfter.GetInteractPrompt() : "-";
            int pointsAfter = service != null ? service.GetPoints(minerId) : -1;
            Note($"save → continue: minerPoints {pointsBefore}→{pointsAfter} level={service?.GetLevel(minerId)} prompt='{prompt}' found {foundBefore}→{DiscoveryLog.FoundCount}");
            if (pointsAfter != pointsBefore) Error("이어하기 뒤 친밀도 점수가 달라졌습니다.");
            if (!prompt.Contains("오늘 선물 완료")) Error("이어하기 뒤 오늘 선물 기록이 사라졌습니다.");
            if (DiscoveryLog.FoundCount < foundBefore) Error("이어하기 뒤 도감 기록이 줄었습니다.");

            // 다음 날: 2단계 직전(24점)에서 좋아하는 화석(+8)을 실제 [E]로 주면 2단계 답례(철 주괴 ×1).
            if (minerAfter != null && service != null)
            {
                GameClock.Instance?.AdvanceToNextDayMorning(9f, ClockReason);
                SetHour(10.5f);
                yield return Wait(1f);
                service.ForceSetPoints(minerId, 24);
                var iron = Res("Items/Item_IronBar");
                int ironBefore = inventory.CountItems(iron), thanksBefore = minerAfter.ThanksGiven;
                inventory.TryReceiveToHotbar(fossil, 1); inventory.RefreshAllUI();
                if (!SelectItem(fossil)) Error("2단계용 화석 선택 실패");
                Transform player2 = PlayerTransform();
                yield return StandAt(player2, minerAfter.transform);
                yield return Wait(.4f);
                string prompt2 = minerAfter.GetInteractPrompt();
                yield return PressNear(player2, minerAfter.transform);
                yield return Wait(1.6f);
                var minerTalk = minerAfter.GetComponent<NpcDialogue>();
                Note($"level2 day={GameClock.Instance?.CurrentDay} prompt='{prompt2}' points 24→{service.GetPoints(minerId)} level={service.GetLevel(minerId)} iron {ironBefore}→{inventory.CountItems(iron)} thanks {thanksBefore}→{minerAfter.ThanksGiven} line='{minerTalk?.LastLine}' hearts={DialogueUI.HeartsShown}");
                yield return ShotUI("gift_level2_reaction_ui.png", "친밀도 2단계 답례");
                if (service.GetLevel(minerId) < 2) Error("2단계에 도달하지 않았습니다.");
                if (inventory.CountItems(iron) <= ironBefore) Error("친밀도 2단계 답례(철 주괴)가 들어오지 않았습니다.");
                DialogueUI.instance?.Hide();
            }
        }
    }

    // ───────────── plan: tutorial (10/27 미해결: 출항 교육 STEP2 '흔들기 없이 사과 3개 → 즉시 STAGE3' 간헐 재현 조사) ─────────────
    // 새 게임 → 실제 점프로 1단계 → 2단계에서 흔들기 전 2초 동안 사과 수/FruitCount를 프레임마다 기록 → 실제 [E] 흔들기 → 떨어진 사과 실제 [E] 줍기.
    IEnumerator PlanTutorialFruit()
    {
        yield return Step("title", StoryTitle(), 50f);
        if (_primary == null) { Error("타이틀 '새 게임' 버튼 없음"); yield break; }
        yield return Step("new-game", StoryNewGame(), 25f);
        yield return Step("fruit", TutorialFruit(), 90f);
    }

    IEnumerator TutorialFruit()
    {
        DepartureTutorialController tutorial = null;
        float end = Now + 60f;
        while (Now < end && ((tutorial = FindFirstObjectByType<DepartureTutorialController>()) == null || !tutorial.IsReady)) yield return null;
        if (tutorial == null || !tutorial.IsReady) { Error("출항 교육 씬이 준비되지 않았습니다."); yield break; }
        var inventory = Inventory.instance;
        Transform player = tutorial.player;
        int Apples() => inventory != null ? inventory.CountItems(tutorial.fruit) : -1;
        Note($"ready: stage={tutorial.Stage} fruitCount={tutorial.FruitCount} applesInBag={Apples()} physical={tutorial.fruitTree.PhysicalFruit}");
        // 1단계: 체크포인트에서 실제 점프.
        Teleport(player, tutorial.movementCheckpoint.position, tutorial.fruitTree.transform.position - tutorial.movementCheckpoint.position);
        yield return Wait(.5f);
        PlayerInputHandler.QaJumpOnce = true;
        yield return Until(() => tutorial.Stage >= 2, 4f);
        Note($"after jump: stage={tutorial.Stage} fruitCount={tutorial.FruitCount} applesInBag={Apples()}");
        if (tutorial.Stage < 2) { Error("점프로 1단계를 넘지 못했습니다."); yield break; }
        // 2단계: 흔들기 전 2초 관찰(버그면 여기서 FruitCount가 3이 되고 3단계로 넘어간다).
        int maxSeen = 0; float t0 = Now; int stageBefore = tutorial.Stage;
        while (Now < t0 + 2f) { maxSeen = Mathf.Max(maxSeen, tutorial.FruitCount); yield return null; }
        Note($"pre-shake 2s: stage {stageBefore}→{tutorial.Stage} maxFruitCount={maxSeen} applesInBag={Apples()} dropped={tutorial.fruitTree.FruitDropped}");
        if (tutorial.Stage >= 3) Error("흔들기 전에 3단계로 넘어갔습니다(STEP2 자동 통과 재현).");
        // 실제 [E]로 흔들기.
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return PressNear(player, tutorial.fruitTree.transform);
        yield return Wait(1.2f);
        var drops = FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None).Where(p => p != null && p.item == tutorial.fruit).ToList();
        Note($"after shake: dropped={tutorial.fruitTree.FruitDropped} pickups={drops.Count} stage={tutorial.Stage} fruitCount={tutorial.FruitCount} applesInBag={Apples()}");
        yield return ShotUI("tutorial_1_fruit_dropped_ui.png", "출항 교육 · 흔든 뒤 떨어진 사과");
        int picks = 0;
        foreach (var drop in drops)
        {
            if (drop == null) continue;
            yield return PressNear(player, drop.transform);
            yield return Wait(.5f);
            picks++;
            Note($"pick {picks}: stage={tutorial.Stage} fruitCount={tutorial.FruitCount} applesInBag={Apples()}");
        }
        // r171t1: 사과가 나무 밑에 떨어지면 PressNear의 위에서 내리쏜 땅 찾기가 나무 갓 위를 잡아 줍지 못했다(캡처 도구 문제 추정).
        // 남은 사과는 네 방향(땅 높이는 사과 높이 기준)에서 다시 다가가 실제 [E]로 줍고, 어느 방향에서 됐는지 남긴다.
        foreach (var drop in drops)
        {
            if (drop == null || tutorial.Stage >= 3) continue;
            Vector3 at = drop.transform.position;
            bool got = false;
            for (int side = 0; side < 4 && drop != null && !got; side++)
            {
                Vector3 dir = Quaternion.Euler(0f, side * 90f, 0f) * Vector3.back;
                Vector3 spot = new Vector3(at.x, at.y, at.z) + dir * 1.1f;
                Teleport(player, spot, at - spot);
                yield return Wait(.25f);
                PressInteract();
                yield return Wait(.5f);
                got = drop == null;
                Note($"retry apple at {at} side {side}: picked={got} fruitCount={tutorial.FruitCount} trunk={Flat(at - tutorial.fruitTree.transform.position).magnitude:0.00}m");
            }
        }
        Note($"RESULT stage={tutorial.Stage} fruitCount={tutorial.FruitCount} applesInBag={Apples()} picks={picks}");
        if (tutorial.Stage < 3) Error("사과 3개를 주웠는데 3단계로 넘어가지 않았습니다.");
    }

    // ───────────── plan: moth (§C.3 저녁 등불 나방: 낮에는 없음 → 17시 이후 가게 문 옆 등불 → 잠자리채로 잡기) ─────────────
    IEnumerator PlanMoth()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 moth 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("moth-catch", MothCatch(), 120f);
    }

    IEnumerator MothCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var mothItem = Res("Items/Item_Bug_Moth");
        if (player == null || inventory == null || net == null || mothItem == null) { Error("플레이어/잠자리채/나방 아이템 없음"); yield break; }
        SetHour(12f);
        yield return Wait(1.2f);
        var all = FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.name.StartsWith(LampMothSpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        int activeNoon = all.Count(m => m.gameObject.activeInHierarchy);
        Note($"noon: moths={all.Count} active={activeNoon}");
        if (all.Count == 0) { Error("등불 나방이 놓이지 않았습니다."); yield break; }
        if (activeNoon > 0) Error("한낮에 나방이 보입니다.");
        SetHour(19.2f);
        yield return Wait(1.2f);
        int activeEvening = all.Count(m => m.gameObject.activeInHierarchy);
        var moth = all.First();
        Note($"evening 19.2h: active={activeEvening} at={moth.transform.position.ToString("F1")}");
        if (activeEvening != all.Count) Error("저녁에 나방이 모두 나타나지 않았습니다.");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        var shop = DemoSettlementController.Instance?.OperatingShop;
        Vector3 outward = shop != null ? Flat(moth.transform.position - shop.transform.position).normalized : Vector3.back;
        Vector3 view = moth.transform.position + outward * 4.5f;
        if (Physics.Raycast(view + Vector3.up * 10f, Vector3.down, out var vh, 20f, ~0, QueryTriggerInteraction.Ignore)) view = vh.point;
        Teleport(player, view, moth.transform.position - view);
        yield return Wait(.8f);
        yield return ShotClean("moth_1_lantern_clean.png", "저녁 가게 앞 등불 가까이 나방");
        Vector3 m = moth.transform.position;
        Vector3 near = m + outward * 1.0f;
        if (Physics.Raycast(near + Vector3.up * 10f, Vector3.down, out var nh, 20f, ~0, QueryTriggerInteraction.Ignore)) near = nh.point;
        Teleport(player, near, m - near);
        yield return Wait(.3f);
        int before = inventory.CountItems(mothItem);
        string prompt = moth.GetInteractPrompt();
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3102), 2f);
        Note($"RESULT prompt='{prompt}' captured={moth.Captured} moth {before}→{inventory.CountItems(mothItem)} found={DiscoveryLog.IsFound(3102)} feedback='{moth.LastFeedback}'");
        if (!moth.Captured || inventory.CountItems(mothItem) <= before) Error("등불 나방을 잡지 못했습니다.");
        yield return ShotUI("moth_2_caught_ui.png", "등불 나방 포획 · 새 발견");
    }

    // ───────────── plan: beetle (Content-insects-plan §B: 숲 가장자리 장수풍뎅이 — 달려가면 날아감 → 천천히 다가가 잡기 → 도감·진열) ─────────────
    IEnumerator PlanBeetle()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 beetle 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("beetle-catch", BeetleCatch(), 150f);
    }

    IEnumerator BeetleCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var beetleItem = Res("Items/Item_Bug_Beetle");
        if (player == null || inventory == null || net == null || beetleItem == null) { Error("플레이어/잠자리채/장수풍뎅이 아이템 없음"); yield break; }
        var beetles = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(ForestBeetleSpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        var butterflies = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(b => b.name.StartsWith("FirstDay_Butterfly_"));
        Note($"beetles={beetles.Count} butterflies={butterflies} at=" + string.Join(" ", beetles.Select(b => b.transform.position.ToString("F1"))));
        if (beetles.Count == 0) { Error("장수풍뎅이가 놓이지 않았습니다."); yield break; }
        var beetle = beetles[0];
        float groundGap = Physics.Raycast(beetle.transform.position + Vector3.up * 2f, Vector3.down, out var g, 6f, ~0, QueryTriggerInteraction.Ignore) ? beetle.transform.position.y - g.point.y : -1f;
        Vector3 rest = beetle.transform.position;
        yield return Wait(1f);
        Note($"perched={beetle.Perched} reach={beetle.Reach:0.00} moved1s={Flat(beetle.transform.position - rest).magnitude:0.00} groundGap={groundGap:0.00}");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        Vector3 toward = Flat(beetle.transform.position - player.position).normalized;
        if (toward.sqrMagnitude < .01f) toward = Vector3.forward;
        // (b) 달려서 다가가면 먼저 날아간다(실제 이동 입력 + 달리기).
        // 같은 단(높이 차 .4m 안)·막힘 없는 방향에서 7m 떨어져 출발한다(다른 단에서 달리면 턱에 막힌다).
        Vector3 start = beetle.transform.position - toward * 7f;
        for (int k = 0; k < 8; k++)
        {
            Vector3 dir8 = Quaternion.Euler(0f, k * 45f, 0f) * toward;
            Vector3 cand = beetle.transform.position - dir8 * 7f;
            if (!Physics.Raycast(cand + Vector3.up * 20f, Vector3.down, out var sh, 40f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(sh.point.y - beetle.transform.position.y) > .4f) continue;
            if (Physics.Linecast(sh.point + Vector3.up * .8f, beetle.transform.position + Vector3.up * .8f, ~0, QueryTriggerInteraction.Ignore)) continue;
            start = sh.point; toward = dir8; break;
        }
        Teleport(player, start, toward);
        yield return Wait(.6f);
        yield return ShotClean("beetle_1_tree_clean.png", "나무 밑동 장수풍뎅이(기본 카메라, 7m)");
        int startlesBefore = beetle.WaryStartles, count0 = inventory.CountItems(beetleItem);
        var cam = Camera.main;
        PlayerInputHandler.QaRun = true;
        var runPc = player.GetComponent<PlayerController>();
        float runMaxSpeed = 0f, runMinDist = float.MaxValue;
        for (float t = 0; t < 2.6f && beetle.WaryStartles == startlesBefore; t += Time.deltaTime)
        {
            if (runPc != null) runMaxSpeed = Mathf.Max(runMaxSpeed, runPc.ActualPlanarSpeed);
            runMinDist = Mathf.Min(runMinDist, Flat(beetle.transform.position - player.position).magnitude);
            Vector3 dir = Flat(beetle.transform.position - player.position).normalized;
            if (cam != null) { Vector3 cf = Flat(cam.transform.forward).normalized, cr = Flat(cam.transform.right).normalized; PlayerInputHandler.QaMove = new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)); }
            yield return null;
        }
        PlayerInputHandler.QaMove = null; PlayerInputHandler.QaRun = false;
        Note($"run approach: maxSpeed={runMaxSpeed:0.0} minDist={runMinDist:0.0} startles {startlesBefore}→{beetle.WaryStartles} fleeing={beetle.Fleeing} feedback='{beetle.LastFeedback}' beetleInBag={inventory.CountItems(beetleItem)}");
        if (beetle.WaryStartles <= startlesBefore) Error("달려가도 장수풍뎅이가 날아가지 않았습니다.");
        if (inventory.CountItems(beetleItem) != count0) Error("달려간 접근에서 장수풍뎅이가 잡혔습니다.");
        yield return ShotUI("beetle_2_startled_ui.png", "달려가면 날아감 · 안내");
        yield return Wait(3.4f); // 내려앉고 다시 놀라는 쿨다운
        // (d) 1.45m에서 휘두르면 빗나감(그물 1.3m).
        Vector3 b = beetle.transform.position;
        Vector3 farSpot = b - toward * 1.45f;
        Teleport(player, farSpot, b - farSpot);
        yield return Wait(.5f);
        string prompt = beetle.GetInteractPrompt();
        beetle.Interact(player.gameObject);
        yield return Wait(GatherFeedback.ContactDelay + .2f);
        Note($"far swing 1.45m: feedback='{beetle.LastFeedback}' captured={beetle.Captured}");
        if (beetle.Captured) Error("1.45m에서 잡혔습니다(그물 1.3m).");
        yield return Wait(3.4f);
        // (c) 천천히(걸어서) 다가가 1.0m에서 실제 [E].
        b = beetle.transform.position;
        Vector3 near = b - toward * 1.0f;
        Teleport(player, near, b - near);
        yield return Wait(.5f);
        int durability = ToolDurability.Remaining(inventory.GetSelectedInstance());
        yield return ShotClean("beetle_2b_near_clean.png", "1m 앞 장수풍뎅이(기본 카메라)");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3101), 2f);
        Note($"RESULT prompt='{prompt}' captured={beetle.Captured} beetle {count0}→{inventory.CountItems(beetleItem)} durability {durability}→{ToolDurability.Remaining(inventory.GetSelectedInstance())} found={DiscoveryLog.IsFound(3101)} feedback='{beetle.LastFeedback}'");
        if (!beetle.Captured || inventory.CountItems(beetleItem) <= count0) Error("천천히 다가가 잡지 못했습니다.");
        if (!prompt.Contains("장수풍뎅이")) Error("프롬프트가 장수풍뎅이 이름을 쓰지 않습니다: " + prompt);
        yield return ShotUI("beetle_3_caught_ui.png", "장수풍뎅이 포획 · 새 발견");
        // 하루 안 반복(§C.1): 게임 시간 1.5시간 뒤 처음 자리로 돌아오고, 개체마다 하루 2번까지.
        float caughtHour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 10.5f;
        SetHour(caughtHour + 1.6f);
        yield return Wait(1f);
        Note($"rearm after 1.6h: captured={beetle.Captured} rearms={beetle.Rearms} caughtToday={BugCritter.CaughtToday(beetle.name)} at={beetle.transform.position.ToString("F1")}");
        if (beetle.Captured) Error("1.5시간 뒤 장수풍뎅이가 돌아오지 않았습니다.");
        else
        {
            yield return ShotClean("beetle_4_rearmed_clean.png", "1.5시간 뒤 같은 자리로 돌아온 장수풍뎅이");
            b = beetle.transform.position;
            Vector3 near2 = b - toward * 1.0f;
            Teleport(player, near2, b - near2);
            yield return Wait(.5f);
            int before2 = inventory.CountItems(beetleItem);
            PressInteract();
            yield return Wait(GatherFeedback.ContactDelay + .6f);
            Note($"second catch: captured={beetle.Captured} beetle {before2}→{inventory.CountItems(beetleItem)} caughtToday={BugCritter.CaughtToday(beetle.name)}");
            if (!beetle.Captured || inventory.CountItems(beetleItem) <= before2) Error("돌아온 장수풍뎅이를 다시 잡지 못했습니다.");
            SetHour((GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f) + 1.6f);
            yield return Wait(1f);
            Note($"after cap +1.6h: captured={beetle.Captured} rearms={beetle.Rearms} caughtToday={BugCritter.CaughtToday(beetle.name)}");
            if (!beetle.Captured) Error($"하루 {BugCritter.DailyCap}번을 넘어 다시 돌아왔습니다.");
        }
        // 공예 쓰임(§C.2): 현장 작업대에서 장수풍뎅이 표본 상자(장수풍뎅이 1 + 판재 1, 판재는 FIXTURE).
        var settlementB = DemoSettlementController.Instance;
        var benchB = settlementB != null ? settlementB.PlacedWorkbench : null;
        var uiB = CraftingUI.instance;
        if (benchB != null && uiB != null && inventory.CountItems(beetleItem) > 0)
        {
            inventory.TryReceiveToHotbar(Res("Items/Item_Plank"), 1);
            player.GetComponent<EquipmentSystem>()?.Holster();
            yield return OpenStation(player, benchB);
            yield return Pick(uiB, 2056);
            yield return ShotUI("beetle_5_specimen_recipe_ui.png", "현장 작업대 · 장수풍뎅이 표본 상자 상세");
            int beforeBox = inventory.CountItems(Res("Items/Item_BeetleSpecimenBox"));
            yield return Craft(uiB, 2056, 1);
            uiB.Close(); yield return Wait(.3f);
            Note($"RESULT specimenBox {beforeBox}→{inventory.CountItems(Res("Items/Item_BeetleSpecimenBox"))} beetleLeft={inventory.CountItems(beetleItem)}");
            if (inventory.CountItems(Res("Items/Item_BeetleSpecimenBox")) <= beforeBox) Error("장수풍뎅이 표본 상자를 만들지 못했습니다.");
        }
        else Error("표본 상자 제작 준비 실패(작업대/장수풍뎅이)");
        // 나비는 그대로 '나비' 이름·떠다님.
        var butterfly = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(x => x.name.StartsWith("FirstDay_Butterfly_")).OrderBy(x => x.name).FirstOrDefault();
        if (butterfly != null) Note($"butterfly prompt='{butterfly.GetInteractPrompt()}' perched={butterfly.Perched} reach={butterfly.Reach:0.00}");
        // 저장 → 이어하기: 잡은 장수풍뎅이는 가방에 남고, 곤충 개체가 다시 놓이는지(계획서 D 미확인 항목) 기록한다.
        if (SaveManager.instance != null)
        {
            int bagBefore = inventory.CountItems(beetleItem);
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            inventory = Inventory.instance;
            var critters = FindObjectsByType<BugCritter>(FindObjectsSortMode.None);
            Note($"save → continue: beetleBag {bagBefore}→{inventory.CountItems(beetleItem)} beetlesLive={critters.Count(c => c.name.StartsWith(ForestBeetleSpawner.NamePrefix) && !c.Captured)} butterfliesLive={critters.Count(c => c.name.StartsWith("FirstDay_Butterfly_") && !c.Captured)} found={DiscoveryLog.IsFound(3101)}");
            if (inventory.CountItems(beetleItem) != bagBefore) Error("이어하기 뒤 가방의 장수풍뎅이 수가 달라졌습니다.");
            int beetlesLive = critters.Count(c => c.name.StartsWith(ForestBeetleSpawner.NamePrefix) && !c.Captured);
            Note($"restored hidden={BugCritter.RestoredHidden} caughtDay={BugCritter.CaughtDay}");
            if (beetlesLive != 1) Error($"이어하기 뒤 같은 날 잡은 장수풍뎅이가 다시 놓였습니다(살아 있는 수 {beetlesLive}, 기대 1).");
        }
    }

    // ───────────── plan: sardine (아이템 생태 연구 F04·R01: 정어리 무리 → 실제 낚시 → 도감 → 조리대 한 접시) ─────────────
    IEnumerator PlanSardine()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 sardine 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("sardine-school", SardineSchool(), 150f);
        yield return Step("sardine-catch", SardineCatch(), 240f);
        yield return Step("sardine-plate", SardinePlate(), 90f);
    }

    ShoreFishSchool.ShoreFish SardineLeader()
    {
        var school = ShoreFishSchool.Instance;
        if (school == null) return null;
        // 따라붙은 정어리가 있는 우두머리(=무리)를 먼저, 없으면 아무 정어리.
        var sardines = school.Fish.Where(f => f.Alive && f.Species == FishingSpot.SardineSpecies).ToList();
        return sardines.FirstOrDefault(f => sardines.Any(m => m != f && Flat(m.Position - f.Position).magnitude < 1.6f)) ?? sardines.FirstOrDefault();
    }

    IEnumerator SardineSchool()
    {
        Transform player = PlayerTransform();
        var grid = FindFirstObjectByType<WorldGridService>();
        if (player == null || grid == null) { Error("플레이어/격자 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        // 정어리 무리가 보일 때까지 물가 몇 곳을 옮겨 다닌다(물가 근처에서만 물고기가 생긴다).
        ShoreFishSchool.ShoreFish lead = null;
        Vector3 probe = player.position;
        for (int hop = 0; hop < 9 && lead == null; hop++)
        {
            // 4곳에서 못 찾으면(6종 뒤 정어리 .11) QA 출현 편향을 켠다 — FIXTURE. 무리·낚시·도감은 실제 경로.
            // reg244: 4곳 뒤 편향이면 찾는 데 70초 안팎이라 90초 단계 제한에 자주 걸렸다(편향이 필요한 실행 절반) → 2곳 뒤에 켠다.
            if (hop == 2 && !FishingSpot.QaPreferSpecies.HasValue) { FishingSpot.QaPreferSpecies = FishingSpot.SardineSpecies; Note("FIXTURE sardine: 2곳에서 못 찾아 QA 출현 편향 켬"); }
            if (!TryShore(grid, probe, out Vector3 stand, out Vector3 water)) { Error("물가를 찾지 못했습니다."); yield break; }
            Teleport(player, stand, water - stand);
            float until = Now + 12f;
            while (Now < until && (lead = SardineLeader()) == null) yield return null;
            probe = stand + Quaternion.Euler(0f, hop * 70f, 0f) * Vector3.forward * 30f;
        }
        var school = ShoreFishSchool.Instance;
        int species0 = school.Fish.Count(f => f.Alive && f.Species == 0), sardines = school.Fish.Count(f => f.Alive && f.Species == FishingSpot.SardineSpecies);
        Note($"school fish={school.Fish.Count} tuna={species0} sardines={sardines} followers={school.SchoolFollowers}");
        if (lead == null) { Error("정어리를 찾지 못했습니다."); yield break; }
        // 4초 동안 무리 간격(우두머리 ↔ 가장 먼 동료)을 잰다.
        float widest = 0f, travelled = 0f; Vector3 last = lead.Position;
        for (float t = 0f; t < 4f; t += Time.deltaTime)
        {
            foreach (var mate in school.Fish.Where(f => f.Alive && f != lead && f.Species == FishingSpot.SardineSpecies && Flat(f.Position - lead.Position).magnitude < 4f))
                widest = Mathf.Max(widest, Flat(mate.Position - lead.Position).magnitude);
            travelled += Flat(lead.Position - last).magnitude; last = lead.Position;
            yield return null;
        }
        Note($"sardine school: followers={school.SchoolFollowers} widestGap={widest:0.00}m leaderMoved={travelled:0.0}m length={lead.Length:0.00} vs tuna≈.6~.76");
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraHold hold = HoldCamera(cam);
            try
            {
                Vector3 look = lead.Position;
                Vector3 eye = look + new Vector3(0f, 7.5f, -4.5f);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                yield return Wait(.2f);
                yield return ShotClean("sardine_1_school_clean.png", "정어리 무리(작은 그림자 셋)와 다른 물고기");
            }
            finally { ReleaseCamera(hold); }
        }
        if (school.SchoolFollowers == 0) Error("정어리가 무리로 다니지 않았습니다.");
    }

    IEnumerator SardineCatch()
    {
        Transform player = PlayerTransform();
        var grid = FindFirstObjectByType<WorldGridService>();
        var inventory = Inventory.instance;
        var sardineItem = Res("Items/Item_Fish_Sardine");
        var rod = Resources.Load<Item>("FirstDayStudio/Item_2011");
        if (player == null || grid == null || inventory == null || sardineItem == null || rod == null) { Error("낚시 준비 실패"); yield break; }
        int before = inventory.CountItems(sardineItem), foundBefore = DiscoveryLog.FoundCount;
        var caught = new List<string>();
        for (int cast = 1; cast <= 10 && inventory.CountItems(sardineItem) <= before; cast++)
        {
            // 정어리 무리 바로 앞 물가에 서서 던진다(무리가 없으면 지금 자리).
            var lead = SardineLeader();
            Vector3 near = lead != null ? lead.Position : player.position;
            if (TryShore(grid, near, out Vector3 stand, out Vector3 water)) Teleport(player, stand, (lead != null ? lead.Position : water) - stand);
            if (!SelectItem(rod)) { Error("낚싯대 선택 실패"); yield break; }
            yield return Wait(.5f);
            FishingSpot spot = FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).Where(s => s != null && s.isActiveAndEnabled)
                .OrderBy(s => Flat(s.transform.position - player.position).magnitude).FirstOrDefault();
            if (spot == null) { Error("낚시터 없음"); yield break; }
            spot.Interact(player.gameObject);
            float end = Now + 22f;
            while (Now < end && spot.Phase != FishingSpot.DemoPhase.Bite) yield return null;
            if (spot.Phase != FishingSpot.DemoPhase.Bite) { Note($"cast {cast}: 입질 없음 phase={spot.Phase}"); spot.Interact(player.gameObject); yield return Wait(1f); continue; }
            string biting = spot.SelectedFish != null ? spot.SelectedFish.itemName : "?";
            spot.Interact(player.gameObject); // 실제 [E]와 같은 당기기 → 릴 미니게임
            yield return Until(() => FirstDayFishingMinigame.IsOpen, 4f);
            var game = FirstDayFishingMinigame.Instance;
            while (FirstDayFishingMinigame.IsOpen)
            {
                // 사람처럼: 물고기가 초록 띠보다 위면 누르고, 아래면 뗀다.
                PlayerInputHandler.QaReel = game != null && game.FishPosition > game.BandPosition;
                yield return null;
            }
            PlayerInputHandler.QaReel = null;
            var landed = spot.LandedFish;
            if (landed == null) { Note($"cast {cast}: {biting} 놓침"); yield return Wait(1f); continue; }
            yield return Until(() => landed == null || landed.OnLand, 4f);
            if (landed != null && landed.Item != null && landed.Item.id == 3003)
            {
                Teleport(player, landed.transform.position + Vector3.back * 2.2f, Vector3.forward);
                yield return Wait(.3f);
                yield return ShotClean("sardine_2_landed_clean.png", "뭍에 올린 정어리(작은 몸)");
            }
            // 뭍에 오른 물고기를 실제 [E]로 두드려 잡고 줍는다.
            for (int hit = 0; hit < 8 && landed != null; hit++)
            {
                player.GetComponent<EquipmentSystem>()?.Holster();
                yield return PressNear(player, landed.transform);
                yield return Wait(.7f);
            }
            caught.Add(biting);
            Note($"cast {cast}: {biting} → 가방 정어리 {inventory.CountItems(sardineItem)}");
        }
        PlayerInputHandler.QaReel = null;
        FishingSpot.QaPreferSpecies = null;
        yield return Until(() => DiscoveryLog.IsFound(3003), 2f); // 도감은 0.5초마다 가방을 훑는다
        Note($"RESULT caught=[{string.Join(", ", caught)}] sardine {before}→{inventory.CountItems(sardineItem)} found {foundBefore}→{DiscoveryLog.FoundCount}/{DiscoveryLog.Total} sardineFound={DiscoveryLog.IsFound(3003)}");
        if (inventory.CountItems(sardineItem) <= before) { Error("실제 낚시로 정어리를 잡지 못했습니다."); yield break; }
        if (!DiscoveryLog.IsFound(3003)) Error("정어리가 도감에 기록되지 않았습니다.");
        yield return Wait(1.2f);
        yield return ShotUI("sardine_3_caught_ui.png", "정어리 첫 발견 알림");
        var phone = SmartphoneUI.instance;
        if (phone != null)
        {
            if (!phone.IsOpen) phone.Toggle();
            yield return Wait(.5f);
            phone.OpenCollectionApp();
            yield return Wait(.6f);
            yield return ShotUI("sardine_4_collection_ui.png", "도감 바다 4칸");
            if (phone.IsOpen) phone.Toggle();
            yield return Wait(.4f);
        }
    }

    IEnumerator SardinePlate()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var sardineItem = Res("Items/Item_Fish_Sardine");
        if (player == null || inventory == null || settlement == null) { Error("플레이어/정착지 없음"); yield break; }
        int have = inventory.CountItems(sardineItem);
        if (have < 3) inventory.TryReceiveToHotbar(sardineItem, 3 - have);
        Note($"FIXTURE 정어리 {have}→{inventory.CountItems(sardineItem)} (실제 포획 {have}마리 + 지급)");
        player.GetComponent<EquipmentSystem>()?.Holster();
        var kitchen = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Kitchen);
        if (kitchen == null)
        {
            inventory.TryReceiveToHotbar(Res("DemoStructure/Item_FieldKitchenKit"), 1);
            Vector3 around = settlement.PlacedWorkbench != null ? settlement.PlacedWorkbench.transform.position : player.position;
            yield return PlaceKit(player, Res("DemoStructure/Item_FieldKitchenKit"), around);
            kitchen = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Kitchen);
        }
        if (kitchen == null) { Error("조리대 없음"); yield break; }
        var ui = CraftingUI.instance;
        Teleport(player, kitchen.InteractionAnchor != null ? kitchen.InteractionAnchor.position : kitchen.transform.position - kitchen.transform.forward * 2.2f, kitchen.transform.position - player.position);
        yield return Wait(.4f);
        kitchen.Interact(player.gameObject);
        yield return Wait(.6f);
        if (ui == null || !ui.IsOpen) { Error("조리대 창이 열리지 않았습니다."); yield break; }
        Note("kitchen rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
        yield return Pick(ui, 2044);
        yield return ShotUI("sardine_5_kitchen_ui.png", "조리대 · 정어리 한 접시 상세");
        yield return Craft(ui, 2044, 1);
        var plate = Res("Items/Item_SardinePlate");
        Note($"RESULT plate={inventory.CountItems(plate)} sardineLeft={inventory.CountItems(sardineItem)} price={plate?.basePrice}");
        if (inventory.CountItems(plate) < 1) Error("정어리 한 접시를 만들지 못했습니다.");
        // 해초 생선말이(연구 H04·R02): 낚시(참치)와 해변(해초)을 잇는 요리. 재료는 FIXTURE(해초 줍기는 beach 플랜).
        ui.Close(); yield return Wait(.3f);
        inventory.TryReceiveToHotbar(Res("Items/Item_Fish"), 1); inventory.TryReceiveToHotbar(Res("Items/Item_Seaweed"), 1);
        kitchen.Interact(player.gameObject);
        yield return Wait(.6f);
        yield return Pick(ui, 2046);
        yield return ShotUI("sardine_6_seaweedroll_ui.png", "조리대 · 해초 생선말이 상세");
        yield return Craft(ui, 2046, 1);
        var roll = Res("Items/Item_SeaweedRoll");
        Note($"RESULT seaweedRoll={inventory.CountItems(roll)} price={roll?.basePrice}");
        if (inventory.CountItems(roll) < 1) Error("해초 생선말이를 만들지 못했습니다.");
        ui.Close();
    }

    // ───────────── plan: mackerel (고등어 F05: 길쭉한 그림자 → 실제 낚시 → 도감 → 훈제 대체 재료) ─────────────
    IEnumerator PlanMackerel()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 mackerel 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("mackerel-shadow", FishShadow(FishingSpot.MackerelSpecies, "mackerel", "고등어"), 120f);
        yield return Step("mackerel-catch", FishCatch(FishingSpot.MackerelSpecies, "mackerel", "고등어", "Items/Item_Fish_Mackerel", 3004), 360f);
        yield return Step("mackerel-smoke", FishKitchen("mackerel", "Items/Item_Fish_Mackerel", "Items/Item_SmokedFish", "Recipe_SmokedMackerel", "훈제 생선 · 고등어", true), 90f);
    }

    // ───────────── plan: flounder (넙치 F06: 넓적한 그림자·오래 쉬기 → 실제 낚시 → 도감 → 생선구이 대체 재료) ─────────────
    IEnumerator PlanFlounder()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 flounder 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("flounder-shadow", FishShadow(FishingSpot.FlounderSpecies, "flounder", "넙치"), 150f);
        yield return Step("flounder-catch", FishCatch(FishingSpot.FlounderSpecies, "flounder", "넙치", "Items/Item_Fish_Flounder", 3005), 420f);
        yield return Step("flounder-grill", FishKitchen("flounder", "Items/Item_Fish_Flounder", "Items/Item_10_GrilledFish", "Recipe_GrilledFlounder", "생선구이 · 넙치", false), 90f);
    }

    // ───────────── plan: mullet / puffer (연구 F07·F08: 항구 앞 숭어, 물가 바위 곁 복어 — 구역 안에서만 그림자 → 실제 낚시 → 도감 → 숭어는 생선구이) ─────────────
    IEnumerator PlanZoneFish(int species)
    {
        bool mullet = species == FishingSpot.MulletSpecies;
        string key = mullet ? "mullet" : "puffer", label = mullet ? "숭어" : "복어";
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error($"'이어하기'를 쓸 수 없어 {key} 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        Transform player = PlayerTransform();
        Vector3? start = null;
        if (mullet) start = FishingSpot.HarborZone;
        else if (player != null && CoastalVegetationField.WaterlineStones.Count > 0)
            start = CoastalVegetationField.WaterlineStones.OrderBy(w => Flat(w.Stone - player.position).magnitude).First().Shore;
        Note($"{key} zone start={(start.HasValue ? start.Value.ToString("F1") : "-")} harbor={(FishingSpot.HarborZone.HasValue ? FishingSpot.HarborZone.Value.ToString("F1") : "-")} stones={CoastalVegetationField.WaterlineStones.Count}");
        if (!start.HasValue) { Error(label + " 구역을 찾지 못했습니다."); yield break; }
        // 구역 밖(먼 물가)에는 나오지 않는지: 지금 물고기 중 그 종이 구역 밖에 있으면 오류.
        var school = ShoreFishSchool.Instance;
        int outside = school != null ? school.Fish.Count(f => f.Alive && f.Species == species && !FishingSpot.InZone(species, f.Position)) : 0;
        Note($"{key} outsideZone={outside}");
        if (outside > 0) Error(label + "가 구역 밖에 나타났습니다.");
        yield return Step(key + "-shadow", FishShadow(species, key, label, start), 180f);
        yield return Step(key + "-catch", FishCatch(species, key, label, mullet ? "Items/Item_Fish_Mullet" : "Items/Item_Fish_Puffer", mullet ? 3006 : 3007), 420f);
        if (mullet) yield return Step(key + "-grill", FishKitchen(key, "Items/Item_Fish_Mullet", "Items/Item_10_GrilledFish", "Recipe_GrilledMullet", "생선구이 · 숭어", false), 90f);
    }

    ShoreFishSchool.ShoreFish SpeciesFish(int species, Vector3 near) =>
        ShoreFishSchool.Instance?.Fish.Where(f => f.Alive && f.Species == species)
            .OrderBy(f => Flat(f.Position - near).magnitude).FirstOrDefault();

    IEnumerator FishShadow(int species, string key, string label, Vector3? start = null)
    {
        Transform player = PlayerTransform();
        var grid = FindFirstObjectByType<WorldGridService>();
        if (player == null || grid == null) { Error("플레이어/격자 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        ShoreFishSchool.ShoreFish fish = null;
        Vector3 probe = start ?? player.position, lastStand = Vector3.zero;
        for (int hop = 0; hop < 12 && fish == null; hop++)
        {
            // 3곳을 돌아도 없으면(드문 종) 그 종을 반쯤 섞어 뽑는 QA 편향을 켠다 — FIXTURE로 남긴다. 낚시·릴·도감은 그대로 실제 경로.
            // reg168: 6곳 뒤에 켜면 이미 있는 물고기가 늦게 바뀌어 남은 4곳에서 못 봤다 → 일찍 켜고, 아래에서 더 멀리 옮겨 새로 생기게 한다.
            if (hop == 3 && !FishingSpot.QaPreferSpecies.HasValue) { FishingSpot.QaPreferSpecies = species; Note($"FIXTURE {key}: 3곳에서 못 찾아 QA 출현 편향 켬"); }
            // reg170: 55m 옮기기가 섬 밖을 가리키면 물가를 못 찾았다 → 마지막 물가에서 더 가까이(25m) 다시 찾는다.
            if (!TryShore(grid, probe, out Vector3 stand, out Vector3 water))
            {
                if (lastStand == Vector3.zero) { Error("물가를 찾지 못했습니다."); yield break; }
                probe = lastStand + Quaternion.Euler(0f, hop * 110f, 0f) * Vector3.forward * 25f;
                continue;
            }
            lastStand = stand;
            Teleport(player, stand, water - stand);
            float until = Now + 12f;
            while (Now < until && (fish = SpeciesFish(species, player.position)) == null) yield return null;
            // 자리 물고기(숭어·복어)는 구역 안에서만 → 구역 둘레 몇 m 안에서 물가를 바꿔 본다.
            probe = start.HasValue ? start.Value + Quaternion.Euler(0f, hop * 70f, 0f) * Vector3.forward * (2f + hop % 4 * 3f)
                : stand + Quaternion.Euler(0f, hop * 70f, 0f) * Vector3.forward * (hop >= 3 ? 55f : 30f);
        }
        var school = ShoreFishSchool.Instance;
        Note("school species=" + string.Join(",", school.Fish.Where(f => f.Alive).GroupBy(f => f.Species).OrderBy(g => g.Key).Select(g => g.Key + "×" + g.Count())));
        if (fish == null) { Error(label + " 그림자를 찾지 못했습니다."); yield break; }
        // 6초 동안 달리기 횟수(속도 급변)와 멈춘 시간을 잰다(고등어: 짧게 달리고 쉬기, 넙치: 오래 멈춤).
        int dashes = 0; bool fast = false; float travelled = 0f, still = 0f, measured = 0f; Vector3 last = fish.Position;
        for (float t = 0f; t < 6f && fish.Alive; t += Time.deltaTime)
        {
            float step = Flat(fish.Position - last).magnitude; travelled += step; last = fish.Position;
            bool now = Time.deltaTime > 0f && step / Time.deltaTime > 1.6f;
            if (Time.deltaTime > 0f && step / Time.deltaTime < .08f) still += Time.deltaTime;
            measured += Time.deltaTime;
            if (now && !fast) dashes++;
            fast = now;
            yield return null;
        }
        var scale = fish.root != null ? fish.root.localScale : Vector3.zero;
        Note($"{key} shadow: length={fish.Length:0.00} scale={scale} dashes6s={dashes} moved={travelled:0.0}m still={still:0.0}/{measured:0.0}s");
        Camera cam = Camera.main;
        if (cam != null && fish.Alive)
        {
            CameraHold hold = HoldCamera(cam);
            try
            {
                Vector3 look = fish.Position;
                Vector3 eye = look + new Vector3(0f, 7.5f, -4.5f);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                yield return Wait(.2f);
                yield return ShotClean(key + "_1_shadow_clean.png", label + " 그림자와 다른 물고기");
            }
            finally { ReleaseCamera(hold); }
        }
        if (species == FishingSpot.MackerelSpecies && scale.x >= scale.z * .9f) Error("고등어 그림자가 길쭉하지 않습니다.");
        if (species == FishingSpot.FlounderSpecies && scale.x <= scale.z * 1.2f) Error("넙치 그림자가 넓적하지 않습니다.");
        if (species == FishingSpot.MulletSpecies && scale.x >= scale.z * .9f) Error("숭어 그림자가 길쭉하지 않습니다.");
        if (species == FishingSpot.PufferSpecies && (scale.x <= scale.z || fish.Length > .7f)) Error("복어 그림자가 작고 동글지 않습니다.");
        if (species == FishingSpot.TroutSpecies && scale.x >= scale.z * .8f) Error("송어 그림자가 날렵하지 않습니다.");
        if (species == FishingSpot.CrucianSpecies && (fish.Length > .62f || scale.x < scale.z * .8f)) Error("붕어 그림자가 작고 둥글지 않습니다.");
        if (species == FishingSpot.EelSpecies && (fish.segs == null || fish.Length < 1f)) Error("장어 그림자가 길게 이어지지 않습니다.");
        if (species >= FishingSpot.MulletSpecies && !FishingSpot.InZone(species, fish.Position)) Error(label + " 그림자가 제 구역 밖에 있습니다.");
    }

    Vector3? _fishStand; // 정해진 낚시 자리(연못 둘레) — 없으면 그림자 앞 가장 가까운 물가 땅

    IEnumerator FishCatch(int species, string key, string label, string resource, int id)
    {
        Transform player = PlayerTransform();
        var grid = FindFirstObjectByType<WorldGridService>();
        var inventory = Inventory.instance;
        var mackerelItem = Res(resource);
        var rod = Resources.Load<Item>("FirstDayStudio/Item_2011");
        if (player == null || grid == null || inventory == null || mackerelItem == null || rod == null) { Error("낚시 준비 실패"); yield break; }
        int before = inventory.CountItems(mackerelItem), foundBefore = DiscoveryLog.FoundCount;
        var caught = new List<string>();
        for (int cast = 1; cast <= 16 && inventory.CountItems(mackerelItem) <= before; cast++)
        {
            // 그 종 그림자 바로 앞 물가에 서서 던진다(없으면 지금 자리).
            var target = SpeciesFish(species, player.position);
            Vector3 near = target != null ? target.Position : player.position;
            // qa232: 연못은 '가장 가까운 물가 땅'이 구덩이 안 턱(실제로 내려갈 수 없음)이라 거기서 던졌다 → 정한 자리(둘레)가 있으면 거기서만 던진다.
            if (_fishStand.HasValue) Teleport(player, _fishStand.Value, (target != null ? target.Position : FishingSpot.PondCentre ?? _fishStand.Value + Vector3.forward) - _fishStand.Value);
            else if (TryShore(grid, near, out Vector3 stand, out Vector3 water)) Teleport(player, stand, (target != null ? target.Position : water) - stand);
            if (!SelectItem(rod)) { Error("낚싯대 선택 실패"); yield break; }
            yield return Wait(.5f);
            FishingSpot spot = FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).Where(s => s != null && s.isActiveAndEnabled)
                .OrderBy(s => Flat(s.transform.position - player.position).magnitude).FirstOrDefault();
            if (spot == null) { Error("낚시터 없음"); yield break; }
            spot.Interact(player.gameObject);
            float end = Now + 22f;
            while (Now < end && spot.Phase != FishingSpot.DemoPhase.Bite) yield return null;
            if (spot.Phase != FishingSpot.DemoPhase.Bite) { Note($"cast {cast}: 입질 없음 phase={spot.Phase}"); spot.Interact(player.gameObject); yield return Wait(1f); continue; }
            string biting = spot.SelectedFish != null ? spot.SelectedFish.itemName : "?";
            spot.Interact(player.gameObject); // 실제 [E]와 같은 당기기 → 릴 미니게임
            yield return Until(() => FirstDayFishingMinigame.IsOpen, 4f);
            var game = FirstDayFishingMinigame.Instance;
            while (FirstDayFishingMinigame.IsOpen)
            {
                PlayerInputHandler.QaReel = game != null && game.FishPosition > game.BandPosition;
                yield return null;
            }
            PlayerInputHandler.QaReel = null;
            var landed = spot.LandedFish;
            if (landed == null) { Note($"cast {cast}: {biting} 놓침"); yield return Wait(1f); continue; }
            yield return Until(() => landed == null || landed.OnLand, 4f);
            if (landed != null && landed.Item != null && landed.Item.id == id)
            {
                Teleport(player, landed.transform.position + Vector3.back * 2.2f, Vector3.forward);
                yield return Wait(.3f);
                yield return ShotClean(key + "_2_landed_clean.png", "뭍에 올린 " + label);
            }
            for (int hit = 0; hit < 8 && landed != null; hit++)
            {
                player.GetComponent<EquipmentSystem>()?.Holster();
                yield return PressNear(player, landed.transform);
                yield return Wait(.7f);
            }
            caught.Add(biting);
            Note($"cast {cast}: {biting} → 가방 {label} {inventory.CountItems(mackerelItem)} standY={player.position.y:F1}");
        }
        PlayerInputHandler.QaReel = null;
        FishingSpot.QaPreferSpecies = null;
        yield return Until(() => DiscoveryLog.IsFound(id), 2f);
        Note($"RESULT caught=[{string.Join(", ", caught)}] {key} {before}→{inventory.CountItems(mackerelItem)} found {foundBefore}→{DiscoveryLog.FoundCount}/{DiscoveryLog.Total} {key}Found={DiscoveryLog.IsFound(id)}");
        if (inventory.CountItems(mackerelItem) <= before) { Error("실제 낚시로 " + label + "를 잡지 못했습니다."); yield break; }
        if (!DiscoveryLog.IsFound(id)) Error(label + "가 도감에 기록되지 않았습니다.");
        yield return Wait(1.2f);
        yield return ShotUI(key + "_3_caught_ui.png", label + " 첫 발견 알림");
        var phone = SmartphoneUI.instance;
        if (phone != null)
        {
            if (!phone.IsOpen) phone.Toggle();
            yield return Wait(.5f);
            phone.OpenCollectionApp();
            yield return Wait(.6f);
            yield return ShotUI(key + "_4_collection_ui.png", "도감 바다 칸");
            if (phone.IsOpen) phone.Toggle();
            yield return Wait(.4f);
        }
    }

    // 조리대: 새 물고기를 기존 결과물의 대체 재료로 쓰는 레시피를 실제 창에서 골라 만든다(참치 수 불변 확인).
    IEnumerator FishKitchen(string key, string resource, string outputResource, string recipeAsset, string label, bool needsWood)
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var mackerelItem = Res(resource);
        var wood = Res("Items/Item_Wood");
        var smoked = Res(outputResource);
        int outputId = smoked != null ? smoked.id : -1;
        int CountOutput() => inventory.slots.Concat(inventory.hotbar != null ? inventory.hotbar.slots : new List<InventorySlot>())
            .Where(x => x != null && !x.IsEmpty && x.item != null && x.item.id == outputId).Sum(x => x.count);
        if (player == null || inventory == null || settlement == null || mackerelItem == null || smoked == null) { Error("플레이어/정착지/아이템 없음"); yield break; }
        if (inventory.CountItems(mackerelItem) < 1) inventory.TryReceiveToHotbar(mackerelItem, 1);
        if (needsWood && inventory.CountItems(wood) < 2) inventory.TryReceiveToHotbar(wood, 2);
        int tunaBefore = inventory.CountItems(Res("Items/Item_Fish")), smokedBefore = CountOutput();
        Note($"FIXTURE {key} {inventory.CountItems(mackerelItem)} 나무 {inventory.CountItems(wood)} 참치 {tunaBefore}");
        player.GetComponent<EquipmentSystem>()?.Holster();
        var kitchen = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Kitchen);
        if (kitchen == null)
        {
            inventory.TryReceiveToHotbar(Res("DemoStructure/Item_FieldKitchenKit"), 1);
            Vector3 around = settlement.PlacedWorkbench != null ? settlement.PlacedWorkbench.transform.position : player.position;
            yield return PlaceKit(player, Res("DemoStructure/Item_FieldKitchenKit"), around);
            kitchen = FindObjectsByType<Workbench>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(w => w.workbenchType == WorkbenchType.Kitchen);
        }
        if (kitchen == null) { Error("조리대 없음"); yield break; }
        var ui = CraftingUI.instance;
        Teleport(player, kitchen.InteractionAnchor != null ? kitchen.InteractionAnchor.position : kitchen.transform.position - kitchen.transform.forward * 2.2f, kitchen.transform.position - player.position);
        yield return Wait(.4f);
        kitchen.Interact(player.gameObject);
        yield return Wait(.6f);
        if (ui == null || !ui.IsOpen) { Error("조리대 창이 열리지 않았습니다."); yield break; }
        Note("kitchen rows: " + string.Join(" | ", ui.VisibleRecipes.Select(r => r.name)));
        var recipe = ui.VisibleRecipes.FirstOrDefault(r => r.name == recipeAsset);
        if (recipe == null) { Error(recipeAsset + " 레시피가 조리대에 없습니다."); yield break; }
        ui.SelectRecipe(recipe);
        yield return Wait(.3f);
        yield return ShotUI(key + "_5_kitchen_ui.png", "조리대 · " + label + " 상세");
        if (ui.CraftButton == null || !ui.CraftButton.interactable) { Error(label + "를 만들 수 없습니다."); yield break; }
        ui.CraftButton.onClick.Invoke();
        yield return Wait(.4f);
        int tunaAfter = inventory.CountItems(Res("Items/Item_Fish"));
        var held = inventory.slots.Concat(inventory.hotbar != null ? inventory.hotbar.slots : new List<InventorySlot>())
            .Where(x => x != null && !x.IsEmpty).Select(x => $"{x.item?.itemName}#{x.item?.id}x{x.count}{(x.item == smoked ? "*" : "")}");
        Note($"{key} bag after craft: {string.Join(", ", held)}");
        Note($"RESULT {key} output {smokedBefore}→{CountOutput()} left={inventory.CountItems(mackerelItem)} tuna {tunaBefore}→{tunaAfter}");
        if (CountOutput() <= smokedBefore) Error(label + "를 만들지 못했습니다.");
        if (tunaAfter != tunaBefore) Error(label + " 제작이 참치를 썼습니다.");
        ui.Close();
    }

    // ───────────── plan: insectboard (연구 R09: 현장 작업대 곤충 관찰 안내판 → 곤충 비소모 → 가게 배치 → 저장·이어하기) ─────────────
    IEnumerator PlanInsectBoard()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 insectboard 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        yield return Step("insectboard", InsectBoardRun(), 180f);
    }

    IEnumerator InsectBoardRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        var plank = Res("Items/Item_Plank");
        var wood = Res("Items/Item_Wood");
        var board = Res("Items/Item_InsectBoard");
        var butterfly = Res("Items/Item_Butterfly");
        if (player == null || inventory == null || settlement == null || ui == null || plank == null || wood == null || board == null) { Error("플레이어/정착지/재료/안내판 아이템 없음"); yield break; }
        if (inventory.CountItems(plank) < 2) inventory.TryReceiveToHotbar(plank, 2 - inventory.CountItems(plank));
        if (inventory.CountItems(wood) < 1) inventory.TryReceiveToHotbar(wood, 1);
        // 곤충 비소모(연구 R09): 가방에 곤충이 있어도 안내판 제작이 곤충을 쓰지 않는다.
        if (butterfly != null && inventory.CountItems(butterfly) < 1) inventory.TryReceiveToHotbar(butterfly, 1);
        inventory.RefreshAllUI();
        int plank0 = inventory.CountItems(plank), wood0 = inventory.CountItems(wood), bug0 = butterfly != null ? inventory.CountItems(butterfly) : 0;
        Note($"FIXTURE 판재 {plank0} 나무 {wood0} 나비 {bug0}");
        var bench = settlement.PlacedWorkbench;
        if (bench == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, bench);
        yield return Pick(ui, 2057);
        yield return ShotUI("insectboard_1_recipe_ui.png", "현장 작업대 · 곤충 관찰 안내판 상세(판재 2 + 나무 1)");
        yield return Craft(ui, 2057, 1);
        ui.Close(); yield return Wait(.4f);
        int bug1 = butterfly != null ? inventory.CountItems(butterfly) : 0;
        Note($"RESULT craft board={inventory.CountItems(board)} 판재 {plank0}→{inventory.CountItems(plank)} 나무 {wood0}→{inventory.CountItems(wood)} 나비 {bug0}→{bug1}");
        if (inventory.CountItems(board) < 1) { Error("곤충 관찰 안내판을 만들지 못했습니다."); yield break; }
        if (bug1 != bug0) Error("안내판 제작이 곤충을 썼습니다.");
        // 툴팁: 값 대신 꾸밈 전용 한 줄.
        string tooltipLine = "";
        var bag = InventoryUI.instance;
        if (bag != null)
        {
            inventory.AddInstance(new ItemInstance(board, 1));
            inventory.RefreshAllUI();
            if (!bag.gameObject.activeSelf) bag.Toggle();
            yield return Wait(.6f);
            var slot = bag.GetComponentsInChildren<InventorySlotUI>().FirstOrDefault(x => x.GetSlot() != null && !x.GetSlot().IsEmpty && x.GetSlot().item.id == 2057);
            if (slot != null && slot.tooltip != null)
            {
                slot.tooltip.Show(slot.GetSlot().item, RectTransformUtility.WorldToScreenPoint(null, slot.transform.position));
                yield return Wait(.3f);
                tooltipLine = slot.tooltip.descriptionText.text.Replace('\n', ' ');
                yield return ShotUI("insectboard_2_tooltip_ui.png", "곤충 관찰 안내판 툴팁 · 꾸밈 전용");
                slot.tooltip.Hide();
            }
            bag.Toggle();
            yield return Wait(.3f);
        }
        Note($"tooltip 2057: '{tooltipLine}'");
        if (!tooltipLine.Contains("꾸밈 전용")) Error("안내판 툴팁이 꾸밈 전용을 알리지 않습니다.");
        // 가게 바닥에 세운다.
        Vector3 around = settlement.OperatingShop != null ? settlement.OperatingShop.transform.position : bench.transform.position;
        yield return PlaceKit(player, board, around, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 6f, 8f });
        yield return Wait(.6f);
        Vector3 placedAt = player.position;
        var placed = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("InsectBoard")).ToList();
        Note($"RESULT board placed={placed.Count} decorTonight={settlement.CountShopDecor()}");
        if (placed.Count == 0) { Error("곤충 관찰 안내판이 배치되지 않았습니다."); yield break; }
        var m = placed.OrderBy(t => Flat(t.position - placedAt).magnitude).First();
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return ShotPlaced(m, "insectboard_3_placed_clean.png", "가게에 세운 곤충 관찰 안내판");
        // 2026-11-09 도감 기념패(도감 섹션 완성 보상)도 가게에 세워 단독 꾸밈 반응을 함께 본다(decorset 가게는 두 세트로 자리가 꽉 참).
        var plaque = Res("Items/Item_CollectionPlaque");
        if (plaque != null)
        {
            if (inventory.CountItems(plaque) < 1) inventory.TryReceiveToHotbar(plaque, 1);
            int plaques0 = FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("CollectionPlaque"));
            yield return PlaceKit(player, plaque, around, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 6f, 8f });
            yield return Wait(.4f);
            Note($"placed CollectionPlaque {plaques0}→{FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("CollectionPlaque"))}");
        }
        if (SaveManager.instance != null)
        {
            Vector3 savedAt = m.position;
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Wait(1.5f);
            var after = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("InsectBoard")).ToList();
            float nearest = after.Count > 0 ? after.Min(t => Flat(t.position - savedAt).magnitude) : -1f;
            Note($"save → continue: insectBoards={after.Count} nearestToSaved={nearest:0.00}m");
            if (after.Count == 0 || nearest > .5f) Error("이어하기 뒤 곤충 관찰 안내판 배치가 사라졌습니다.");
        }
        // 2026-11-09 단독 꾸밈 반응: 저녁 영업에 들어온 손님이 안내판을 알아보는 말풍선(모은 곤충 → 꾸밈 → 손님).
        var loop = DayNightShopLoopController.Instance;
        settlement = DemoSettlementController.Instance;
        player = PlayerTransform();
        if (loop == null || settlement == null || settlement.OperatingShop == null || player == null) { Error("영업 확인 준비 실패"); yield break; }
        SetHour(20.2f);
        yield return Wait(1.5f);
        int remarksBefore = DecorSetAppreciation.Remarks;
        bool opened = loop.TryOpenShop();
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        if (interior != null && !interior.IsInside) { interior.entrance.Interact(player.gameObject); yield return Until(() => interior.IsInside, 8f); }
        float end = Now + 90f;
        bool shot = false;
        while (Now < end && loop.IsShopOpenForCustomers)
        {
            if (!shot && DecorSetAppreciation.Remarks > remarksBefore && (DecorSetAppreciation.LastRemark ?? "").Contains("곤충 안내판"))
            {
                yield return Wait(.4f);
                yield return ShotUI("insectboard_4_remark_ui.png", "손님이 곤충 관찰 안내판을 알아봄");
                shot = true;
            }
            yield return null;
        }
        Note($"RESULT open={opened} remarks {remarksBefore}→{DecorSetAppreciation.Remarks} last='{DecorSetAppreciation.LastRemark}' shot={shot} said=[{string.Join(" | ", DecorSetAppreciation.Said.Distinct())}]");
        if (!shot) Error("손님이 곤충 관찰 안내판을 알아보지 않았습니다.");
        if (!DecorSetAppreciation.Said.Any(l => l.Contains("도감 기념패"))) Error("손님이 도감 기념패를 알아보지 않았습니다.");
    }

    // ───────────── plan: decorset (ACNH 소품 연구 H2: 꾸밈 테마 세트 → 툴팁 → 가게 배치 → 손님이 알아봄) ─────────────
    IEnumerator PlanDecorSet()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 decorset 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(18.5f);
        yield return Step("decorset", DecorSetRun(), 240f);
    }

    IEnumerator DecorSetRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        if (player == null || inventory == null || settlement == null || loop == null || settlement.OperatingShop == null) { Error("플레이어/정착지/상점 없음"); yield break; }
        var pieces = new[] { ("Items/Item_ShellDecor", "ShellDecor"), ("Items/Item_GlassMobile", "GlassMobile"), ("Items/Item_PotteryMosaic", "PotteryMosaic"), ("Items/Item_CrystalStand", "CrystalStand") };
        foreach (var (path, _) in pieces) inventory.TryReceiveToHotbar(Res(path), 1);
        inventory.AddInstance(new ItemInstance(Res("Items/Item_GlassMobile"), 1)); // 가방 칸 툴팁용 한 개 더
        inventory.RefreshAllUI();
        Note("FIXTURE 꾸밈 4종(조개 장식·유리 모빌·토기 모자이크·광물 받침) 각 1");
        // 툴팁: 세트 한 줄이 보이는지.
        var bag = InventoryUI.instance;
        if (bag != null)
        {
            if (!bag.gameObject.activeSelf) bag.Toggle();
            yield return Wait(.6f);
            var slot = bag.GetComponentsInChildren<InventorySlotUI>().FirstOrDefault(x => x.GetSlot() != null && !x.GetSlot().IsEmpty && x.GetSlot().item.id == 2048);
            if (slot != null && slot.tooltip != null)
            {
                slot.tooltip.Show(slot.GetSlot().item, RectTransformUtility.WorldToScreenPoint(null, slot.transform.position));
                yield return Wait(.3f);
                Note($"tooltip 2048: '{slot.tooltip.descriptionText.text.Replace("\n", " / ")}'");
                yield return ShotUI("decorset_1_tooltip_ui.png", "유리 모빌 툴팁 · 바닷가 세트 줄");
                slot.tooltip.Hide();
            }
            else Note("툴팁 대상 칸 없음(가방에 유리 모빌이 없고 핫바에만 있음)");
            bag.Toggle();
            yield return Wait(.3f);
        }
        Vector3 around = settlement.OperatingShop.transform.position;
        foreach (var (path, name) in pieces)
        {
            int before = FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith(name));
            yield return PlaceKit(player, Res(path), around, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 6f, 8f });
            yield return Wait(.4f);
            int after = FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith(name));
            Note($"placed {name} {before}→{after}");
        }
        var complete = DecorThemeSets.CompleteInShop();
        Note($"complete sets in shop: [{string.Join(", ", complete.Select(x => x.Name))}] decorCount={settlement.CountShopDecor()}");
        if (complete.Count < 2) Error("가게 안에 두 세트가 다 놓이지 않았습니다.");
        // H3 유리 모빌 흔들림: 2초 스캔 뒤 진자 수와 유리 조각 이동량.
        yield return Wait(2.5f);
        var gem = settlement.ShopDecorWith("decor-glass").SelectMany(x => x.GetComponentsInChildren<Transform>()).FirstOrDefault(x => x.name == "MobileGem0");
        Vector3 gemA = gem != null ? gem.position : Vector3.zero;
        yield return Wait(.7f);
        float gemMoved = gem != null ? (gem.position - gemA).magnitude : -1f;
        Note($"mobile swing: pendulums={DecorAmbientMotion.Swinging} gem0 moved={gemMoved:0.000}m in .7s");
        if (DecorAmbientMotion.Swinging == 0 || gemMoved <= .001f) Error("유리 모빌이 흔들리지 않습니다.");
        yield return Wait(2.5f);
        Note($"crystal twinkles={DecorAmbientMotion.Twinkles}");
        if (DecorAmbientMotion.Twinkles == 0) Error("광물 받침이 반짝이지 않습니다.");
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        if (interior != null && !interior.IsInside) { interior.entrance.Interact(player.gameObject); yield return Until(() => interior.IsInside, 8f); }
        yield return Wait(1f);
        yield return ShotClean("decorset_2_room_clean.png", "가게 안 · 바닷가 세트 + 땅속 보물 세트");
        if (interior != null && interior.exit != null) { interior.exit.Interact(player.gameObject); yield return Until(() => !interior.IsInside, 8f); }
        SetHour(20.2f);
        yield return Wait(1.5f);
        int remarksBefore = DecorSetAppreciation.Remarks;
        bool opened = loop.TryOpenShop();
        Note($"open={opened}");
        // 손님이 들어오면 실내를 보며 알아보는 말풍선을 찍는다.
        if (interior != null && !interior.IsInside) { interior.entrance.Interact(player.gameObject); yield return Until(() => interior.IsInside, 8f); }
        float end = Now + 90f;
        bool shot = false;
        while (Now < end && loop.IsShopOpenForCustomers)
        {
            if (!shot && DecorSetAppreciation.Remarks > remarksBefore)
            {
                yield return Wait(.4f);
                yield return ShotUI("decorset_3_remark_ui.png", "손님이 꾸밈 세트를 알아봄");
                shot = true;
            }
            yield return null;
        }
        Note($"RESULT remarks {remarksBefore}→{DecorSetAppreciation.Remarks} last='{DecorSetAppreciation.LastRemark}' said=[{string.Join(" | ", DecorSetAppreciation.Said.Distinct())}]");
        if (DecorSetAppreciation.Remarks <= remarksBefore) Error("손님이 꾸밈 세트를 알아보지 않았습니다.");
    }

    // ───────────── plan: ladybug (연구 B03: 꽃 위 칠성무당벌레 → 낮에만 → 천천히 다가가 잡기 → 도감) ─────────────
    IEnumerator PlanLadybug()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 ladybug 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("ladybug-catch", LadybugCatch(), 120f);
    }

    IEnumerator LadybugCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Ladybug");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/칠성무당벌레 아이템 없음"); yield break; }
        yield return Until(() => FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Any(b => b.name.StartsWith(LadybugSpawner.NamePrefix)), 8f);
        var bugs = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(LadybugSpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        Note($"ladybugs={bugs.Count} flowerSpots={CoastalVegetationField.FlowerSpots.Count} at=" + string.Join(" ", bugs.Select(b => b.transform.position.ToString("F1"))));
        if (bugs.Count == 0) { Error("칠성무당벌레가 놓이지 않았습니다."); yield break; }
        var bug = bugs[0];
        // 기본 카메라는 남쪽 위에서 내려다본다 → 남쪽 3m에서 본다.
        Teleport(player, bug.transform.position + Vector3.back * 3f, Vector3.forward);
        yield return Wait(1f);
        yield return ShotClean("ladybug_1_flower_clean.png", "꽃 위 칠성무당벌레(기본 카메라, 3m)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        Vector3 b = bug.transform.position;
        Vector3 near = new Vector3(b.x, player.position.y, b.z - 1.0f);
        Teleport(player, near, Vector3.forward);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        yield return ShotUI("ladybug_2_near_ui.png", "1m 앞 칠성무당벌레 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3103), 2f);
        Note($"RESULT prompt='{prompt}' captured={bug.Captured} ladybug {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3103)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("칠성무당벌레를 잡지 못했습니다.");
        if (!prompt.Contains("칠성무당벌레")) Error("프롬프트가 칠성무당벌레 이름을 쓰지 않습니다: " + prompt);
        yield return ShotUI("ladybug_3_caught_ui.png", "칠성무당벌레 포획 · 새 발견");
        // 밤에는 보이지 않는다.
        SetHour(20f);
        yield return Wait(1.2f);
        int visible = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(x => x.name.StartsWith(LadybugSpawner.NamePrefix) && x.gameObject.activeInHierarchy);
        Note($"night visible ladybugs={visible}");
        if (visible > 0) Error("밤에도 칠성무당벌레가 보입니다.");
    }

    // 대상 옆 dist m에 발밑 땅이 있고(절벽 너머 제외) 플레이어 몸(캡슐)이 다른 충돌체와 겹치지 않는 방향(대상을 바라보는 방향). 서·동·남·북 순, 모두 막히면 서쪽.
    static Vector3 FreeSide(Transform player, Vector3 target, float dist)
    {
        foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            Vector3 at = new Vector3(target.x, player.position.y, target.z) - dir * dist;
            bool ground = Physics.Raycast(at + Vector3.up * 1f, Vector3.down, out var hit, 1.7f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(player);
            if (ground && !Physics.OverlapCapsule(at + Vector3.up * .45f, at + Vector3.up * 1.5f, .38f, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(player))) return dir;
        }
        return Vector3.right;
    }

    // 지금 화면에 뜬 [E] 안내 글자(그 대상에 붙은 말풍선일 때만). 없으면 빈 문자열.
    static string ShownPrompt(Transform anchor)
    {
        var ui = InteractPromptUI.instance;
        if (ui == null || ui.promptText == null || !ui.promptText.gameObject.activeInHierarchy) return string.Empty;
        string text = ui.promptText.text.Replace('\n', ' ');
        return ui.Anchor == anchor ? text : "(다른 대상) " + text;
    }

    // ───────────── plan: bumblebee (연구 B05: 꽃 사이를 옮겨 다니는 호박벌 → 나는 동안은 못 잡음 → 앉았을 때 잡기 → 도감 → 밤엔 없음) ─────────────
    IEnumerator PlanBumblebee()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 bumblebee 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("bumblebee-catch", BumblebeeCatch(), 150f);
    }

    IEnumerator BumblebeeCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Bumblebee");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/호박벌 아이템 없음"); yield break; }
        yield return Until(() => FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Any(b => b.name.StartsWith(BumblebeeSpawner.NamePrefix)), 10f);
        var spawner = FindFirstObjectByType<BumblebeeSpawner>();
        var bees = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(BumblebeeSpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        string routes = spawner != null ? string.Join(",", Enumerable.Range(0, spawner.Spawned).Select(spawner.RouteLength)) : "-";
        Note($"bumblebees={bees.Count} routes={routes} flowerSpots={CoastalVegetationField.FlowerSpots.Count} at=" + string.Join(" ", bees.Select(b => b.transform.position.ToString("F1"))));
        if (bees.Count == 0 || spawner == null) { Error("호박벌이 놓이지 않았습니다."); yield break; }
        int pick = Enumerable.Range(0, spawner.Spawned).FirstOrDefault(i => spawner.RouteLength(i) >= 2);
        if (spawner.RouteLength(pick) < 2) { Error("호박벌이 옮겨 다닐 꽃 경로가 없습니다: " + routes); yield break; }
        var bee = bees[pick];
        // 1) 꽃 위 호박벌(기본 카메라는 남쪽 위에서 내려다본다 → 남쪽 3m).
        Teleport(player, bee.transform.position + Vector3.back * 3f, Vector3.forward);
        yield return Wait(1f);
        yield return ShotClean("bumblebee_1_flower_clean.png", "꽃 위 호박벌(기본 카메라, 3m)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        // 2) 다음 꽃으로 날아가는 동안: 안내는 '기다려요', 휘둘러도 잡히지 않는다.
        // bumblebee-r178: 첫 촬영 사이 이미 날기 시작했으면 내려앉은 뒤부터 잰다(그때 잰 출발점은 이미 다음 꽃이었다).
        yield return Until(() => !bee.Fleeing, 4f);
        Vector3 from = bee.Home;
        int hops0 = BumblebeeSpawner.Hops;
        yield return Until(() => bee.Fleeing, 12f);
        if (!bee.Fleeing) { Error($"호박벌이 12초 안에 다음 꽃으로 옮겨 가지 않았습니다(hops {hops0}→{BumblebeeSpawner.Hops})."); yield break; }
        // 나는 길 옆(서쪽 1.2m)에서 동쪽을 본다 — 기본 카메라에서 벌과 안내가 플레이어에 가리지 않게.
        Vector3 b = bee.transform.position;
        Teleport(player, new Vector3(b.x - 1.2f, player.position.y, b.z), Vector3.right);
        yield return null;
        yield return null;
        string flyPrompt = bee.GetInteractPrompt();
        string flyShown = ShownPrompt(bee.transform); // 실제 화면 안내(PlayerInteraction이 나는 곤충에 붙인 말풍선)
        yield return ShotUI("bumblebee_2_flying_ui.png", "꽃 사이를 나는 호박벌 · 기다리라는 안내");
        bool stillFlying = bee.Fleeing;
        int count0 = inventory.CountItems(bugItem);
        PressInteract();
        yield return Wait(.25f);
        bool caughtInFlight = bee.Captured || inventory.CountItems(bugItem) > count0;
        yield return Until(() => !bee.Fleeing, 4f);
        float hop = Flat(bee.Home - from).magnitude;
        Note($"FLIGHT prompt='{flyPrompt}' shown='{flyShown}' pressedWhileFlying={stillFlying} caughtInFlight={caughtInFlight} hop={hop:0.0}m hops {hops0}→{BumblebeeSpawner.Hops} landed={!bee.Fleeing}");
        if (!flyPrompt.Contains("기다려요")) Error("나는 동안 안내가 기다리라고 하지 않습니다: " + flyPrompt);
        if (!flyShown.Contains("기다려요")) Error("나는 동안 화면 안내가 기다리라고 하지 않습니다: " + flyShown);
        if (caughtInFlight) Error("나는 동안 호박벌이 잡혔습니다.");
        if (hop < 1.5f) Error($"호박벌이 다른 꽃으로 옮겨 가지 않았습니다({hop:0.0}m).");
        // 3) 내려앉은 꽃 서쪽 1m에서 동쪽을 보고 → 잡기(남쪽에서 다가가면 기본 카메라에서 플레이어 머리에 벌이 가렸다).
        b = bee.transform.position;
        Teleport(player, new Vector3(b.x - 1.0f, player.position.y, b.z), Vector3.right);
        yield return Wait(.4f);
        string prompt = bee.GetInteractPrompt();
        yield return ShotUI("bumblebee_3_near_ui.png", "꽃에 앉은 호박벌 1m 앞 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3105), 2f);
        Note($"RESULT prompt='{prompt}' captured={bee.Captured} bumblebee {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3105)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bee.LastFeedback}'");
        if (!bee.Captured || inventory.CountItems(bugItem) <= count0) Error("앉은 호박벌을 잡지 못했습니다.");
        if (!prompt.Contains("호박벌") || !prompt.Contains("휘두르기")) Error("앉은 호박벌 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("bumblebee_4_caught_ui.png", "호박벌 포획 · 새 발견");
        // 밤에는 보이지 않는다.
        SetHour(20f);
        yield return Wait(1.2f);
        int visible = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(x => x.name.StartsWith(BumblebeeSpawner.NamePrefix) && x.gameObject.activeInHierarchy);
        Note($"night visible bumblebees={visible}");
        if (visible > 0) Error("밤에도 호박벌이 보입니다.");
    }

    // ───────────── plan: stagbeetle (연구 B08: 오전엔 없음 → 오후 숲 나무 밑동 넓적사슴벌레 → 천천히 다가가 잡기 → 도감) ─────────────
    IEnumerator PlanStagBeetle()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 stagbeetle 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("stagbeetle-catch", StagBeetleCatch(), 120f);
    }

    IEnumerator StagBeetleCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_StagBeetle");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/넓적사슴벌레 아이템 없음"); yield break; }
        BugCritter Stag() => FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.name == ForestBeetleSpawner.StagName);
        yield return Until(() => Stag() != null, 8f);
        var bug = Stag();
        if (bug == null) { Error("넓적사슴벌레가 놓이지 않았습니다."); yield break; }
        // 오전에는 보이지 않는다.
        SetHour(10.5f);
        yield return Wait(1.2f);
        bool morning = bug.gameObject.activeInHierarchy;
        SetHour(15f);
        yield return Wait(1.2f);
        bool afternoon = bug.gameObject.activeInHierarchy;
        var beetles = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(ForestBeetleSpawner.NamePrefix)).Select(b => b.transform.position).ToList();
        float nearestBeetle = beetles.Count > 0 ? beetles.Min(p => Flat(p - bug.transform.position).magnitude) : -1f;
        Note($"stag at={bug.transform.position.ToString("F1")} morning={morning} afternoon={afternoon} beetles={beetles.Count} nearestBeetle={nearestBeetle:0.0}m");
        if (morning) Error("오전에도 넓적사슴벌레가 보입니다.");
        if (!afternoon) { Error("오후에 넓적사슴벌레가 보이지 않습니다."); yield break; }
        // 기본 카메라(남쪽 위)에서 밑동 옆 사슴벌레.
        Teleport(player, bug.transform.position + Vector3.back * 3f, Vector3.forward);
        yield return Wait(1f);
        yield return ShotClean("stagbeetle_1_trunk_clean.png", "오후 숲 나무 밑동 넓적사슴벌레(기본 카메라, 3m)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        Vector3 b = bug.transform.position;
        Vector3 side = FreeSide(player, b, 1.0f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.0f, side);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        string shown = ShownPrompt(bug.transform);
        yield return ShotUI("stagbeetle_2_near_ui.png", "넓적사슴벌레 1m 옆 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3107), 2f);
        Note($"RESULT prompt='{prompt}' shown='{shown}' captured={bug.Captured} stag {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3107)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bug.LastFeedback}'");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("넓적사슴벌레를 잡지 못했습니다.");
        if (!prompt.Contains("넓적사슴벌레") || !prompt.Contains("휘두르기")) Error("넓적사슴벌레 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("stagbeetle_3_caught_ui.png", "넓적사슴벌레 포획 · 새 발견");
    }

    // ───────────── plan: longhorn (연구 B09: 낮 숲 나무 밑동 알락하늘소 → 더듬이 흔들림 → 옆에서 잡기 → 도감 → 밤엔 없음) ─────────────
    IEnumerator PlanLonghorn()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 longhorn 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("longhorn-catch", LonghornCatch(), 120f);
    }

    IEnumerator LonghornCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Longhorn");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/알락하늘소 아이템 없음"); yield break; }
        BugCritter Find(string n) => FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.name == n);
        yield return Until(() => Find(ForestBeetleSpawner.LonghornName) != null, 8f);
        var bug = Find(ForestBeetleSpawner.LonghornName);
        if (bug == null) { Error("알락하늘소가 놓이지 않았습니다."); yield break; }
        SetHour(21f);
        yield return Wait(1.2f);
        bool night = bug.gameObject.activeInHierarchy;
        SetHour(10.5f);
        yield return Wait(1.2f);
        bool day = bug.gameObject.activeInHierarchy;
        var stag = Find(ForestBeetleSpawner.StagName);
        float fromStag = stag != null ? Flat(stag.transform.position - bug.transform.position).magnitude : -1f;
        // 더듬이 흔들림: 1초 사이 더듬이 회전이 실제로 바뀌는지.
        var antennae = bug.GetComponentsInChildren<Transform>(true).Where(t => t.name == "LonghornAntennaL" || t.name == "LonghornAntennaR").ToList();
        var before = antennae.Select(a => a.localRotation).ToList();
        yield return Wait(1f);
        float swing = antennae.Select((a, i) => Quaternion.Angle(before[i], a.localRotation)).DefaultIfEmpty(0f).Max();
        Note($"longhorn at={bug.transform.position.ToString("F1")} day={day} night={night} antennae={antennae.Count} swing1s={swing:0.0}deg fromStag={fromStag:0.0}m");
        if (night) Error("밤에도 알락하늘소가 보입니다.");
        if (!day) { Error("낮에 알락하늘소가 보이지 않습니다."); yield break; }
        if (antennae.Count != 2 || swing < 1f) Error("알락하늘소 더듬이가 흔들리지 않습니다.");
        Teleport(player, bug.transform.position + Vector3.back * 3f, Vector3.forward);
        yield return Wait(1f);
        yield return ShotClean("longhorn_1_trunk_clean.png", "낮 숲 나무 밑동 알락하늘소(기본 카메라, 3m)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        Vector3 b = bug.transform.position;
        Vector3 side = FreeSide(player, b, 1.0f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.0f, side);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        string shown = ShownPrompt(bug.transform);
        yield return ShotUI("longhorn_2_near_ui.png", "알락하늘소 1m 옆 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3108), 2f);
        Note($"RESULT prompt='{prompt}' shown='{shown}' captured={bug.Captured} longhorn {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3108)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bug.LastFeedback}'");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("알락하늘소를 잡지 못했습니다.");
        if (!prompt.Contains("알락하늘소") || !prompt.Contains("휘두르기")) Error("알락하늘소 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("longhorn_3_caught_ui.png", "알락하늘소 포획 · 새 발견");
    }

    // ───────────── plan: cricket (연구 B07: 낮엔 없음 → 해 질 녘 나무 그늘 왕귀뚜라미 → 울음·풀 떨림 → 가까이 가면 조용 → 잡기 → 도감 → 아침에도 있음) ─────────────
    IEnumerator PlanCricket()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 cricket 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("cricket-catch", CricketCatch(), 150f);
    }

    IEnumerator CricketCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Cricket");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/왕귀뚜라미 아이템 없음"); yield break; }
        BugCritter Find(string n) => FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.name == n);
        yield return Until(() => Find(ForestBeetleSpawner.CricketName) != null, 8f);
        var bug = Find(ForestBeetleSpawner.CricketName);
        if (bug == null) { Error("왕귀뚜라미가 놓이지 않았습니다."); yield break; }
        SetHour(12f); yield return Wait(1.2f);
        bool noon = bug.gameObject.activeInHierarchy;
        SetHour(7f); yield return Wait(1.2f);
        bool morning = bug.gameObject.activeInHierarchy;
        SetHour(17.5f); yield return Wait(1.2f);
        bool evening = bug.gameObject.activeInHierarchy;
        Note($"cricket at={bug.transform.position.ToString("F1")} noon={noon} morning={morning} evening={evening}");
        if (noon) Error("한낮에도 왕귀뚜라미가 보입니다.");
        if (!morning || !evening) { Error("이른 아침·해 질 녘에 왕귀뚜라미가 보이지 않습니다."); yield break; }
        // 멀리(6m)서 울음과 풀 떨림.
        Teleport(player, bug.transform.position + Vector3.back * 6f, Vector3.forward);
        int chirps0 = CricketSong.Chirps;
        yield return Wait(4f);
        int farChirps = CricketSong.Chirps - chirps0;
        float grass = CricketSong.LastGrassAngle;
        Teleport(player, bug.transform.position + Vector3.back * 3f, Vector3.forward);
        yield return Until(() => (Time.time * 70f) > 0 && CricketSong.Chirps > chirps0 + farChirps, 3f);
        yield return Wait(.08f);
        yield return ShotClean("cricket_1_shade_clean.png", "해 질 녘 나무 그늘 풀숲 왕귀뚜라미(기본 카메라, 3m) · 울 때 풀 떨림");
        // 가까이(1.2m) 가면 조용해진다.
        Vector3 b = bug.transform.position;
        Vector3 side = FreeSide(player, b, 1.2f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.2f, side);
        int chirps1 = CricketSong.Chirps;
        yield return Wait(3f);
        int nearChirps = CricketSong.Chirps - chirps1;
        Note($"CHIRP far4s={farChirps} grassMax={grass:0.0}deg near3s={nearChirps}");
        if (farChirps < 2) Error("멀리서 왕귀뚜라미 울음이 들리지 않습니다.");
        if (grass < 2f) Error("울 때 풀이 떨리지 않습니다(소리만 단서).");
        if (nearChirps > 0) Error("가까이 가도 조용해지지 않습니다.");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        b = bug.transform.position;
        side = FreeSide(player, b, 1.0f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.0f, side);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        yield return ShotUI("cricket_2_near_ui.png", "왕귀뚜라미 1m 옆 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3109), 2f);
        Note($"RESULT prompt='{prompt}' captured={bug.Captured} cricket {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3109)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bug.LastFeedback}'");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("왕귀뚜라미를 잡지 못했습니다.");
        if (!prompt.Contains("왕귀뚜라미") || !prompt.Contains("휘두르기")) Error("왕귀뚜라미 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("cricket_3_caught_ui.png", "왕귀뚜라미 포획 · 새 발견");
        // 곤충 10칸 → 도감 곤충 탭 4열.
        var phone = SmartphoneUI.instance;
        if (phone != null)
        {
            phone.OpenCollectionApp();
            yield return Wait(.5f);
            var app = phone.GetComponentInChildren<CollectionPhoneUI>(true);
            var insectTab = app != null ? app.transform.Find("Tabs/Tab_곤충")?.GetComponent<Button>() : null;
            if (insectTab != null) { insectTab.onClick.Invoke(); yield return Wait(.3f); yield return ShotUI("cricket_4_collection_insect_ui.png", "도감 · 곤충 탭 10칸(4열)"); }
            else Error("도감 곤충 탭 버튼 없음");
            if (phone.IsOpen) phone.Toggle();
        }
    }

    // ───────────── plan: chairpaint (너굴 리스트 연구 H4: 나무 의자 + 산호 조각/해변 유리 → 색 변형 의자 → 툴팁 → 가게에 놓고 손님이 앉음) ─────────────
    Item[] _decorChairs;

    IEnumerator PlanChairPaint()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 chairpaint 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("chairpaint-craft", ChairPaintCraft(), 120f);
        if (_decorChairs != null) yield return Step("chairpaint-seat", ShopDecor(), 200f);
    }

    IEnumerator ChairPaintCraft()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        var chair = Res("DemoStructure/Item_SimpleFurniture");
        var coral = Res("Items/Item_CoralPiece");
        var glass = Res("Items/Item_BeachGlass");
        var coralChair = Res("Items/Item_ChairCoral");
        var seaChair = Res("Items/Item_ChairSea");
        if (player == null || inventory == null || settlement == null || ui == null || chair == null || coral == null || glass == null || coralChair == null || seaChair == null) { Error("의자 칠하기 준비 실패(아이템/작업대)"); yield break; }
        if (inventory.CountItems(chair) < 2) inventory.TryReceiveToHotbar(chair, 2 - inventory.CountItems(chair));
        if (inventory.CountItems(coral) < 1) inventory.TryReceiveToHotbar(coral, 1);
        if (inventory.CountItems(glass) < 1) inventory.TryReceiveToHotbar(glass, 1);
        inventory.RefreshAllUI();
        Note($"FIXTURE 의자 {inventory.CountItems(chair)} 산호 {inventory.CountItems(coral)} 유리 {inventory.CountItems(glass)}");
        var bench = settlement.PlacedWorkbench;
        if (bench == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, bench);
        yield return Pick(ui, 2058);
        yield return ShotUI("chairpaint_1_coral_recipe_ui.png", "현장 작업대 · 산호빛 의자(나무 의자 + 산호 조각)");
        yield return Craft(ui, 2058, 1);
        yield return Pick(ui, 2059);
        yield return ShotUI("chairpaint_2_sea_recipe_ui.png", "현장 작업대 · 바다빛 의자(나무 의자 + 해변 유리)");
        yield return Craft(ui, 2059, 1);
        ui.Close(); yield return Wait(.4f);
        Note($"RESULT paint coral={inventory.CountItems(coralChair)} sea={inventory.CountItems(seaChair)} chairLeft={inventory.CountItems(chair)} coralLeft={inventory.CountItems(coral)} glassLeft={inventory.CountItems(glass)}");
        if (inventory.CountItems(coralChair) < 1 || inventory.CountItems(seaChair) < 1) { Error("색 변형 의자를 만들지 못했습니다."); yield break; }
        string tip = ItemTooltipLineFor(coralChair);
        Note($"tooltip 2058 variant line='{tip}'");
        if (tip == null || !tip.Contains("색 변형")) Error("색 변형 툴팁 줄이 없습니다.");
        // 연구 H4 '보유/전체': 나무 의자 남은 것 + 산호빛 + 바다빛을 가졌으면 보유 3/3(가방 기준).
        int ownedColours = DecorThemeSets.OwnedChairColours();
        Note($"chair colours owned={ownedColours}/3");
        if (tip == null || !tip.Contains("보유 ")) Error("의자 툴팁에 보유/전체가 없습니다: " + tip);
        if (ownedColours < 2) Error($"색 변형 의자를 만들었는데 보유 색이 {ownedColours}입니다.");
        _decorChairs = new[] { coralChair, seaChair };
    }

    static string ItemTooltipLineFor(Item item) => DecorThemeSets.TooltipLine(item);

    // ───────────── plan: mantis (연구 B10: 낮 꽃 위 왕사마귀 → 좌우 살랑 → 옆에서 잡기 → 도감 → 밤엔 없음) ─────────────
    IEnumerator PlanMantis()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 mantis 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("mantis-catch", MantisCatch(), 120f);
    }

    IEnumerator MantisCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Mantis");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/왕사마귀 아이템 없음"); yield break; }
        BugCritter Find() => FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.name == MantisSpawner.Name);
        yield return Until(() => Find() != null, 10f);
        var bug = Find();
        if (bug == null) { Error("왕사마귀가 놓이지 않았습니다."); yield break; }
        var others = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b != bug).Select(b => Flat(b.transform.position - bug.transform.position).magnitude).DefaultIfEmpty(-1f).Min();
        yield return Wait(6f);
        Note($"mantis at={bug.transform.position.ToString("F1")} nearestOtherBug={others:0.0}m swayMax={MantisSway.LastAngle:0.0}deg");
        if (MantisSway.LastAngle < 2f) Error("왕사마귀가 살랑이지 않습니다.");
        // 꽃 자리는 풀밭 가장자리(절벽 끝일 때가 많다) → 사마귀가 앉은 땅 높이에서 옆쪽으로 다가간다(mantis-r198: 남쪽 3m가 절벽 아래 모래였다).
        Vector3 b = bug.transform.position, ground = new Vector3(b.x, b.y - .3f, b.z);
        Teleport(player, ground, Vector3.forward);
        yield return Wait(.2f);
        Vector3 side = FreeSide(player, b, 2.5f);
        Teleport(player, ground - side * 2.5f, side);
        yield return Wait(1f);
        yield return ShotClean("mantis_1_flower_clean.png", "꽃 위 왕사마귀(기본 카메라, 2.5m 옆)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        side = FreeSide(player, b, 1.0f);
        Teleport(player, ground - side * 1.0f, side);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        yield return ShotUI("mantis_2_near_ui.png", "왕사마귀 1m 옆 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3110), 2f);
        Note($"RESULT prompt='{prompt}' captured={bug.Captured} mantis {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3110)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bug.LastFeedback}'");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("왕사마귀를 잡지 못했습니다.");
        if (!prompt.Contains("왕사마귀") || !prompt.Contains("휘두르기")) Error("왕사마귀 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("mantis_3_caught_ui.png", "왕사마귀 포획 · 새 발견");
        SetHour(20f);
        yield return Wait(1.2f);
        bool night = Find()?.gameObject.activeInHierarchy ?? false;
        Note($"night visible mantis={night}");
        if (night) Error("밤에도 왕사마귀가 보입니다.");
    }

    // ───────────── plan: rug (너굴 리스트 연구 H5 러그 교체: 해초·산호 러그 짜기 → 실내 러그 위에서 들고 [E] → 바뀜·이전 러그 돌려받음 → 저장·이어하기) ─────────────
    IEnumerator PlanRug()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 rug 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        yield return Step("rug-swap", RugSwapRun(), 200f);
    }

    IEnumerator RugSwapRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        var seaweed = Res("Items/Item_Seaweed");
        var coral = Res("Items/Item_CoralPiece");
        var rugSea = Res("Items/Item_RugSeaweed");
        var rugCoral = Res("Items/Item_RugCoral");
        if (player == null || inventory == null || settlement == null || ui == null || seaweed == null || coral == null || rugSea == null || rugCoral == null) { Error("러그 준비 실패(아이템/작업대)"); yield break; }
        if (inventory.CountItems(seaweed) < 4) inventory.TryReceiveToHotbar(seaweed, 4 - inventory.CountItems(seaweed));
        if (inventory.CountItems(coral) < 1) inventory.TryReceiveToHotbar(coral, 1);
        inventory.RefreshAllUI();
        var bench = settlement.PlacedWorkbench;
        if (bench == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, bench);
        yield return Pick(ui, 2060);
        yield return ShotUI("rug_1_recipe_ui.png", "현장 작업대 · 해초 러그(해초 3)");
        string rugUses = ui.DetailUsesText;
        Note($"rug recipe uses='{rugUses.Replace("\n", " / ")}'");
        if (!rugUses.Contains("깔기")) Error("러그 레시피 쓰임에 깔기 안내가 없습니다: " + rugUses);
        yield return Craft(ui, 2060, 1);
        yield return Pick(ui, 2061);
        yield return Craft(ui, 2061, 1);
        ui.Close(); yield return Wait(.4f);
        Note($"RESULT weave seaweedRug={inventory.CountItems(rugSea)} coralRug={inventory.CountItems(rugCoral)}");
        if (inventory.CountItems(rugSea) < 1 || inventory.CountItems(rugCoral) < 1) { Error("러그를 짜지 못했습니다."); yield break; }
        var interior = settlement.OperatingShop != null ? settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Until(() => ShopRugSwap.Instance != null && ShopRugSwap.Instance.Spot != null, 5f);
        var spot = ShopRugSwap.Instance != null ? ShopRugSwap.Instance.Spot : null;
        if (spot == null) { Error("러그 교체 자리를 찾지 못했습니다."); yield break; }
        var shop = settlement.OperatingShop.transform;
        Vector3 floor = new Vector3(spot.position.x, player.position.y, spot.position.z);
        Vector3 toward = Vector3.ProjectOnPlane(shop.forward, Vector3.up).normalized; // 가게 안쪽(문 반대)
        Teleport(player, floor - toward * 1.7f, toward);
        yield return Wait(.6f);
        string before = ShopRugSwap.Current;
        yield return ShotClean("rug_2_before_clean.png", "기본 러그");
        if (!SelectItem(rugSea)) { Error("해초 러그 선택 실패"); yield break; }
        yield return Wait(.6f);
        string shown = ShownPrompt(spot);
        var spotBox = spot.GetComponent<Collider>();
        Note($"rug spot={spot.position:F2} player={player.position:F2} dist={Flat(spot.position - player.position).magnitude:F2} active={spot.gameObject.activeInHierarchy} enabled={spot.GetComponent<ShopRugSpot>()?.enabled} collider={spotBox != null && spotBox.enabled} held={EquipmentSystem.CurrentHeld(player.gameObject)?.id} interiors={ShopRugSwap.InteriorCount} inside={interior.IsInside}");
        yield return ShotUI("rug_3_prompt_ui.png", "해초 러그를 들고 러그 위 · 안내");
        PressInteract();
        yield return Wait(.6f);
        string afterSea = ShopRugSwap.Current;
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(.3f);
        yield return ShotClean("rug_4_seaweed_clean.png", "해초 러그로 바뀜");
        if (!SelectItem(rugCoral)) { Error("산호 러그 선택 실패"); yield break; }
        yield return Wait(.6f);
        PressInteract();
        yield return Wait(.6f);
        string afterCoral = ShopRugSwap.Current;
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(.3f);
        yield return ShotClean("rug_5_coral_clean.png", "산호 러그로 바뀜");
        Note($"RESULT swap shown='{shown}' {before}→{afterSea}→{afterCoral} swaps={ShopRugSwap.Swaps} seaweedRugBack={inventory.CountItems(rugSea)} coralRugLeft={inventory.CountItems(rugCoral)}");
        if (!shown.Contains("러그 바꾸기")) Error("러그를 든 채 러그 위 안내가 없습니다: " + shown);
        if (afterSea != "seaweed" || afterCoral != "coral") Error("러그가 바뀌지 않았습니다.");
        if (inventory.CountItems(rugSea) != 1) Error("깔려 있던 해초 러그가 가방으로 돌아오지 않았습니다.");
        if (SaveManager.instance != null)
        {
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Until(() => ShopRugSwap.Instance != null && ShopRugSwap.Instance.Spot != null && ShopRugSwap.Pending == null, 8f);
            Note($"save → continue: rug={ShopRugSwap.Current}");
            if (ShopRugSwap.Current != "coral") Error("이어하기 뒤 러그가 돌아가지 않았습니다: " + ShopRugSwap.Current);
        }
    }

    // ───────────── plan: wallhang (너굴 리스트 연구 H5 벽걸이: 도감 기념패를 들고 뒷벽 → [E] 걸기(바닥 배치가 시작되지 않음) → 손님 말 목록 → 빈손 [E] 떼기 → 다시 걸기 → 저장·이어하기) ─────────────
    IEnumerator PlanWallHang()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 wallhang 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        yield return Step("wall-hang", WallHangRun(), 200f);
    }

    IEnumerator WallHangRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var plaque = Res("Items/Item_CollectionPlaque");
        if (player == null || inventory == null || settlement == null || plaque == null) { Error("벽걸이 준비 실패"); yield break; }
        if (inventory.CountItems(plaque) < 1) inventory.TryReceiveToHotbar(plaque, 1);
        inventory.RefreshAllUI();
        var interior = settlement.OperatingShop != null ? settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Until(() => ShopWallHang.Instance != null && ShopWallHang.Instance.Spot != null, 5f);
        var spot = ShopWallHang.Instance != null ? ShopWallHang.Instance.Spot : null;
        if (spot == null) { Error("벽 걸이 자리를 찾지 못했습니다."); yield break; }
        Vector3 wallDir = Vector3.ProjectOnPlane(spot.parent.forward, Vector3.up).normalized; // 뒷벽 쪽(+z)
        Teleport(player, new Vector3(spot.position.x, player.position.y, spot.position.z) - wallDir * 1.2f, wallDir);
        yield return Wait(.6f);
        yield return ShotClean("wallhang_1_empty_clean.png", "빈 뒷벽(책상과 창문 사이)");
        int before = inventory.CountItems(plaque);
        if (!SelectItem(plaque)) { Error("기념패 선택 실패"); yield break; }
        yield return Wait(.6f);
        string shownHang = ShownPrompt(spot);
        yield return ShotUI("wallhang_2_prompt_ui.png", "기념패를 들고 벽 앞 · 안내");
        PressInteract();
        yield return Wait(.6f);
        // 고르면 바닥 미리보기가 열리는 것은 기존 배치 문법 — [E] 뒤에도 배치가 남아 있거나 바닥에 놓였으면 오류.
        var placing = player.GetComponent<WorldHotbarPlacementController>();
        bool placingStarted = placing != null && placing.IsPlacing;
        if (placingStarted) placing.Cancel();
        string afterHang = ShopWallHang.Current;
        int afterHangCount = inventory.CountItems(plaque);
        var shown = ShopWallHang.Instance.Shown;
        var board = shown != null ? shown.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("PlaqueBoard")) : null;
        float height = board != null ? board.bounds.center.y - player.position.y : -1f; // 판 가운데 높이(뿌리는 판 아래 .7m)
        bool standHidden = shown != null && shown.GetComponentsInChildren<Transform>(true).Where(t => t.name == "PlaqueFoot" || t.name.StartsWith("PlaqueLeg")).All(t => !t.gameObject.activeSelf);
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(.4f);
        yield return ShotClean("wallhang_3_hung_clean.png", "뒷벽에 걸린 도감 기념패");
        var remarks = DecorThemeSets.RemarksInShop();
        bool remark = remarks.Any(r => r.Contains("기념패"));
        string shownTake = ShownPrompt(spot);
        PressInteract();
        yield return Wait(.6f);
        string afterTake = ShopWallHang.Current;
        int afterTakeCount = inventory.CountItems(plaque);
        Note($"RESULT wall shownHang='{shownHang}' placingStarted={placingStarted} hang={afterHang} plaque {before}→{afterHangCount}→{afterTakeCount} heightAbovePlayer={height:F2} standHidden={standHidden} remark={remark} shownTake='{shownTake}' take={afterTake}");
        if (!shownHang.Contains("벽에 걸기")) Error("기념패를 든 채 벽 앞 안내가 없습니다: " + shownHang);
        if (placingStarted) Error("벽에 걸지 않고 바닥 배치가 시작됐습니다.");
        if (afterHang != ShopWallHang.PlaqueKey || afterHangCount != before - 1) Error("기념패가 벽에 걸리지 않았습니다.");
        if (height < 1.3f || height > 2.4f || !standHidden) Error($"걸린 기념패 높이·모양이 이상합니다: {height:F2} stand={standHidden}");
        if (!remark) Error("벽의 기념패를 손님 말 목록이 알아보지 못합니다.");
        if (!shownTake.Contains("벽에서 떼기") || afterTake != "" || afterTakeCount != before) Error("빈손으로 기념패를 떼지 못했습니다: " + shownTake);
        // 다시 걸고 저장 → 이어하기 뒤 그대로인지.
        if (!SelectItem(plaque)) { Error("기념패 다시 선택 실패"); yield break; }
        yield return Wait(.6f);
        PressInteract();
        yield return Wait(.6f);
        player.GetComponent<EquipmentSystem>()?.Holster();
        if (ShopWallHang.Current != ShopWallHang.PlaqueKey) { Error("다시 걸지 못했습니다."); yield break; }
        if (SaveManager.instance != null)
        {
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Until(() => ShopWallHang.Instance != null && ShopWallHang.Instance.Spot != null && ShopWallHang.Pending == null, 8f);
            Note($"save → continue: wall={ShopWallHang.Current} shown={(ShopWallHang.Instance != null && ShopWallHang.Instance.Shown != null)}");
            if (ShopWallHang.Current != ShopWallHang.PlaqueKey || ShopWallHang.Instance == null || ShopWallHang.Instance.Shown == null) Error("이어하기 뒤 벽 기념패가 돌아오지 않았습니다.");
        }
    }

    // ───────────── plan: desk (너굴 리스트 연구 H5 탁상 위: 불가사리 들고 책상 → [E] 올리기 → 화석 조각으로 바꾸기(불가사리 돌아옴) → 손님 말 목록 → 빈손 내리기 → 다시 올리고 저장·이어하기) ─────────────
    IEnumerator PlanDesk()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 desk 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        yield return Step("desk-shelf", DeskRun(), 200f);
    }

    IEnumerator DeskRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var star = Res("Items/Item_Starfish");
        var fossil = Res("Items/Item_FossilShard");
        if (player == null || inventory == null || settlement == null || star == null || fossil == null) { Error("책상 준비 실패"); yield break; }
        if (inventory.CountItems(star) < 1) inventory.TryReceiveToHotbar(star, 1);
        if (inventory.CountItems(fossil) < 1) inventory.TryReceiveToHotbar(fossil, 1);
        inventory.RefreshAllUI();
        var interior = settlement.OperatingShop != null ? settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Until(() => ShopDeskShelf.Instance != null && ShopDeskShelf.Instance.Spot != null, 5f);
        var spot = ShopDeskShelf.Instance != null ? ShopDeskShelf.Instance.Spot : null;
        if (spot == null) { Error("책상 자리를 찾지 못했습니다."); yield break; }
        Vector3 wallDir = Vector3.ProjectOnPlane(spot.parent.forward, Vector3.up).normalized; // 책상 쪽(+z)
        Teleport(player, new Vector3(spot.position.x, player.position.y, spot.position.z) - wallDir * 1.1f, wallDir);
        yield return Wait(.6f);
        yield return ShotClean("desk_1_empty_clean.png", "빈 책상(책·머그·램프)");
        int stars = inventory.CountItems(star), fossils = inventory.CountItems(fossil);
        if (!SelectItem(star)) { Error("불가사리 선택 실패"); yield break; }
        yield return Wait(.6f);
        string shownSet = ShownPrompt(spot);
        yield return ShotUI("desk_2_prompt_ui.png", "불가사리를 들고 책상 앞 · 안내");
        PressInteract();
        yield return Wait(.6f);
        var placing = player.GetComponent<WorldHotbarPlacementController>();
        bool placingStarted = placing != null && placing.IsPlacing;
        if (placingStarted) placing.Cancel();
        int afterSet = ShopDeskShelf.Current, starsAfterSet = inventory.CountItems(star);
        var shown = ShopDeskShelf.Instance.Shown;
        Bounds shownBounds = default;
        bool hasBounds = false;
        if (shown != null) foreach (var r in shown.GetComponentsInChildren<Renderer>()) { if (!hasBounds) { shownBounds = r.bounds; hasBounds = true; } else shownBounds.Encapsulate(r.bounds); }
        float sitOn = hasBounds ? shownBounds.min.y - spot.parent.TransformPoint(new Vector3(0, .75f, 0)).y : -1f;
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(.4f);
        yield return ShotClean("desk_3_star_clean.png", "책상 위 불가사리");
        // qa239: 책상 앞에 서면 기본 카메라에서 플레이어 머리가 책상 일부를 가린다 → 옆으로 비켜선 화면에서 올려 둔 물건이 보이는지 따로 확인.
        Vector3 keepAt = player.position;
        Vector3 aside = Vector3.ProjectOnPlane(spot.parent.right, Vector3.up).normalized;
        Teleport(player, keepAt + aside * 1.6f, wallDir);
        yield return Wait(.4f);
        var cam = Camera.main;
        Vector3 vp = cam != null && shown != null ? cam.WorldToViewportPoint(shown.transform.position) : Vector3.zero;
        bool onScreen = vp.z > 0f && vp.x > .05f && vp.x < .95f && vp.y > .05f && vp.y < .95f;
        bool blocked = false;
        if (cam != null && shown != null)
            foreach (var hit in Physics.RaycastAll(cam.transform.position, (shown.transform.position - cam.transform.position).normalized, Vector3.Distance(cam.transform.position, shown.transform.position) - .15f))
                if (hit.collider != null && hit.collider.transform.IsChildOf(player)) { blocked = true; break; }
        Note($"desk aside view: viewport={vp:F2} onScreen={onScreen} playerBlocks={blocked}");
        yield return ShotClean("desk_3b_star_aside_clean.png", "옆으로 비켜서 본 책상 위 불가사리");
        if (!onScreen || blocked) Error("비켜선 화면에서도 책상 위 물건이 보이지 않습니다.");
        Teleport(player, keepAt, wallDir);
        yield return Wait(.3f);
        if (!SelectItem(fossil)) { Error("화석 조각 선택 실패"); yield break; }
        yield return Wait(.6f);
        string shownSwap = ShownPrompt(spot);
        PressInteract();
        yield return Wait(.6f);
        if (placing != null && placing.IsPlacing) { placingStarted = true; placing.Cancel(); }
        int afterSwap = ShopDeskShelf.Current, starsBack = inventory.CountItems(star), fossilsLeft = inventory.CountItems(fossil);
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(.4f);
        yield return ShotClean("desk_4_fossil_clean.png", "화석 조각으로 바꿈");
        var remarks = DecorThemeSets.RemarksInShop();
        string remark = remarks.FirstOrDefault(r => r.StartsWith("책상 위")) ?? "";
        string shownTake = ShownPrompt(spot);
        PressInteract();
        yield return Wait(.6f);
        int afterTake = ShopDeskShelf.Current, fossilsBack = inventory.CountItems(fossil);
        Note($"RESULT desk shownSet='{shownSet}' set={afterSet} star {stars}→{starsAfterSet}→{starsBack} sitOn={sitOn:F3} size={(hasBounds ? shownBounds.size.ToString("F2") : "-")} shownSwap='{shownSwap}' swap={afterSwap} fossil {fossils}→{fossilsLeft}→{fossilsBack} remark='{remark}' shownTake='{shownTake}' take={afterTake} placingStarted={placingStarted}");
        if (!shownSet.Contains("책상에 올리기")) Error("불가사리를 든 채 책상 안내가 없습니다: " + shownSet);
        if (placingStarted) Error("책상에 올리지 않고 바닥 배치가 시작됐습니다.");
        if (afterSet != star.id || starsAfterSet != stars - 1) Error("불가사리가 책상에 올라가지 않았습니다.");
        if (!hasBounds || Mathf.Abs(sitOn) > .03f || Mathf.Max(shownBounds.size.x, Mathf.Max(shownBounds.size.y, shownBounds.size.z)) > .34f) Error("책상 위 물건이 떠 있거나 크기가 이상합니다.");
        if (afterSwap != fossil.id || starsBack != stars || fossilsLeft != fossils - 1) Error("화석 조각으로 바꾸지 못했거나 불가사리가 돌아오지 않았습니다.");
        if (!remark.Contains("화석")) Error("책상 위 물건을 손님 말 목록이 알아보지 못합니다: " + remark);
        if (!shownTake.Contains("책상에서 내리기") || afterTake != 0 || fossilsBack != fossils) Error("빈손으로 내리지 못했습니다: " + shownTake);
        if (!SelectItem(star)) { Error("불가사리 다시 선택 실패"); yield break; }
        yield return Wait(.6f);
        PressInteract();
        yield return Wait(.6f);
        player.GetComponent<EquipmentSystem>()?.Holster();
        if (ShopDeskShelf.Current != star.id) { Error("다시 올리지 못했습니다."); yield break; }
        if (SaveManager.instance != null)
        {
            var save = SaveManager.instance.SaveGameAsync();
            yield return Until(() => save.IsCompleted, 10f);
            var inspect = SaveManager.instance.InspectContinueAsync();
            yield return Until(() => inspect.IsCompleted, 10f);
            if (!inspect.IsCompletedSuccessfully || !DemoRouteController.PrepareContinue(inspect.Result.CompanionIds, out _)) { Error("다시 이어하기 준비 실패"); yield break; }
            int sceneBefore = SceneManager.GetActiveScene().handle;
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(sceneBefore), 180f);
            yield return Until(() => ShopDeskShelf.Instance != null && ShopDeskShelf.Instance.Spot != null && !ShopDeskShelf.Pending.HasValue, 8f);
            Note($"save → continue: desk={ShopDeskShelf.Current} shown={(ShopDeskShelf.Instance != null && ShopDeskShelf.Instance.Shown != null)}");
            if (ShopDeskShelf.Current != star.id || ShopDeskShelf.Instance == null || ShopDeskShelf.Instance.Shown == null) Error("이어하기 뒤 책상 위 불가사리가 돌아오지 않았습니다.");
        }
    }

    // ───────────── plan: freshwater (탐사: 생성 섬의 연못·강 칸이 실제 격자·화면에 남아 있는지 — 연구 F09~F12·B12 전제 확인) ─────────────
    IEnumerator PlanFreshwaterProbe()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 freshwater 탐사를 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        var generated = WorldPersistenceService.Instance != null ? WorldPersistenceService.Instance.ActiveGeneratedWorld : null;
        var grid = FindFirstObjectByType<WorldGridService>();
        Transform player = PlayerTransform();
        if (generated == null || grid == null || player == null) { Error("생성 섬/격자 없음"); yield break; }
        var pond = generated.Cells.Where(c => c.WaterType == WorldWaterType.Pond).Select(c => c.Terrain.Coordinate).ToList();
        var river = generated.Cells.Where(c => c.WaterType == WorldWaterType.River).Select(c => c.Terrain.Coordinate).ToList();
        int pondWet = pond.Count(c => grid.TryGetCell(c, out var cell) && cell.HasWater);
        int riverWet = river.Count(c => grid.TryGetCell(c, out var cell) && cell.HasWater);
        Note($"FRESH seed={generated.Seed} size={generated.Definition.Width}x{generated.Definition.Height} pond={pond.Count} wet={pondWet} river={river.Count} wet={riverWet}");
        if (pond.Count > 0)
        {
            var c = new Vector2Int((int)pond.Average(v => v.x), (int)pond.Average(v => v.y));
            grid.CellToWorld(c, out Vector3 at);
            Note($"FRESH pond center cell={c} world={at:F1} harbor={(FishingSpot.HarborZone.HasValue ? FishingSpot.HarborZone.Value.ToString("F1") : "-")}");
            Teleport(player, Grounded(at + Vector3.back * 5f), Vector3.forward);
            yield return Wait(1.2f);
            Note($"FRESH at pond player={player.position:F1} water y? groundAtCenter={Grounded(at):F1}");
            yield return ShotClean("fresh_1_pond_clean.png", "연못 자리(생성 3x3)");
            // 연못 둘레 7x7칸: 높이·물 높이/깊이·지면·생성 물 종류(P 연못, R 강, . 마른 땅) — 화면에서 물이 일부만 보이는 까닭을 찾는다.
            for (int dz = 3; dz >= -3; dz--)
            {
                var row = new System.Text.StringBuilder();
                for (int dx = -3; dx <= 3; dx++)
                {
                    var k = c + new Vector2Int(dx, dz);
                    string kind = generated.TryGetCell(k, out var gc) ? (gc.WaterType == WorldWaterType.Pond ? "P" : gc.WaterType == WorldWaterType.River ? "R" : gc.IsOcean ? "O" : ".") : "?";
                    if (grid.TryGetCell(k, out var rc)) row.Append($"{kind}e{rc.ElevationLevel}w{rc.WaterSurfaceLevel}/{rc.WaterDepthLevels}{rc.GroundType.ToString()[0]} ");
                    else row.Append("---- ");
                }
                Note($"FRESH pond row dz={dz}: {row}");
            }
            Camera cam = Camera.main;
            if (cam != null)
            {
                CameraHold hold = HoldCamera(cam);
                try
                {
                    Vector3 eye = at + new Vector3(0f, 16f, -6f);
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at - eye));
                    yield return Wait(.3f);
                    yield return ShotClean("fresh_1b_pond_top_clean.png", "연못 위에서");
                }
                finally { ReleaseCamera(hold); }
            }
        }
        if (river.Count > 0)
        {
            var mid = river[river.Count / 2];
            grid.CellToWorld(mid, out Vector3 at);
            var last = river.OrderBy(v => v.x).Last();
            grid.CellToWorld(last, out Vector3 mouth);
            Note($"FRESH river mid cell={mid} world={at:F1} mouth cell={last} world={mouth:F1} cells=" + string.Join(" ", river.Take(40)));
            Teleport(player, Grounded(at + Vector3.back * 5f), Vector3.forward);
            yield return Wait(1.2f);
            Note($"FRESH at river player={player.position:F1}");
            yield return ShotClean("fresh_2_river_clean.png", "강 가운데");
            Teleport(player, Grounded(mouth + Vector3.back * 5f + Vector3.left * 3f), Vector3.forward);
            yield return Wait(1.2f);
            yield return ShotClean("fresh_3_mouth_clean.png", "강 하구");
        }
    }

    // ───────────── plan: trout / eel (연구 F12·F09: 강 송어 낮 06–18 · 하구 장어 14–24 — 바다 물고기는 강 안쪽에 없음 → 구역 그림자(장어는 마디가 굽이침) → 실제 낚시 → 도감 → 대체 요리) ─────────────
    IEnumerator PlanFreshFish(int species)
    {
        bool trout = species == FishingSpot.TroutSpecies, crucian = species == FishingSpot.CrucianSpecies;
        string key = trout ? "trout" : crucian ? "crucian" : "eel", label = trout ? "송어" : crucian ? "붕어" : "장어";
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error($"'이어하기'를 쓸 수 없어 {key} 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(trout || crucian ? 10.5f : 16f);
        var generated = WorldPersistenceService.Instance != null ? WorldPersistenceService.Instance.ActiveGeneratedWorld : null;
        var grid = FindFirstObjectByType<WorldGridService>();
        Transform player = PlayerTransform();
        var mouths = FishingSpot.RiverMouths;
        Note($"{key} freshCells={FishingSpot.FreshCellCount} mouths={mouths.Count} " + string.Join(" ", mouths.Select(m => m.ToString("F0"))));
        if (generated == null || grid == null || player == null || mouths.Count == 0) { Error("강·하구를 찾지 못했습니다."); yield break; }
        Vector3 mouth = mouths[0];
        Vector3 start = mouth;
        if (crucian)
        {
            if (!FishingSpot.PondCentre.HasValue) { Error("연못을 찾지 못했습니다."); yield break; }
            start = FishingSpot.PondCentre.Value;
        }
        if (trout && !crucian)
        {
            // 하구(12m)를 막 벗어난 강 칸 — 모래밭을 지나는 얕은 구간이라 그림자가 잘 보인다.
            var river = generated.Cells.Where(c => c.WaterType == WorldWaterType.River).Select(c => { grid.CellToWorld(c.Terrain.Coordinate, out var w); return w; })
                .Where(w => !FishingSpot.IsEstuary(w) && (FishingSpot.OpenRiverCount == 0 || FishingSpot.IsOpenRiver(w))).OrderBy(w => Mathf.Abs(Flat(w - mouth).magnitude - 18f)).ToList();
            if (river.Count == 0) { Error("하구 밖 강 칸이 없습니다."); yield break; }
            start = river[0];
        }
        Note($"{key} start={start:F1} estuary={FishingSpot.IsEstuary(start)} fresh={FishingSpot.IsFresh(start)} openRiver={FishingSpot.OpenRiverCount}/{FishingSpot.RiverCells.Count}");
        Teleport(player, Grounded(start + Vector3.back * 4f), Vector3.forward);
        yield return Wait(4f); // 그림자가 새로 생길 시간
        var school = ShoreFishSchool.Instance;
        int seaInRiver = 0;
        for (float t = 0f; t < 4f; t += .5f)
        {
            if (school != null) seaInRiver = Mathf.Max(seaInRiver, school.Fish.Count(f => f.Alive && f.Species < FishingSpot.MulletSpecies && FishingSpot.IsFresh(f.Position) && !FishingSpot.IsEstuary(f.Position)));
            yield return Wait(.5f);
        }
        int outside = school != null ? school.Fish.Count(f => f.Alive && f.Species == species && !FishingSpot.InZone(species, f.Position)) : 0;
        Note($"{key} seaFishInRiver={seaInRiver} outsideZone={outside} school=" + (school != null ? string.Join(",", school.Fish.Where(f => f.Alive).GroupBy(f => f.Species).OrderBy(g => g.Key).Select(g => g.Key + "×" + g.Count())) : "-"));
        if (crucian && school != null)
        {
            var pondCells = generated.Cells.Where(c => c.WaterType == WorldWaterType.Pond).Select(c => c.Terrain.Coordinate).ToList();
            int cand = pondCells.Count(school.IsCandidate);
            int roam = pondCells.Count(c => grid.CellToWorld(c, out var w) && school.RoamableAt(w));
            grid.WorldToCell(player.position, out var pc); grid.TryGetCell(pc, out var pcell);
            Note($"crucian pond cells={pondCells.Count} candidates={cand} roamableCentres={roam} player={player.position:F1} cell={pc} e{pcell.ElevationLevel} water={pcell.HasWater} shoreDressing={PondShoreDressing.Placed}");
            if (PondShoreDressing.Placed == 0) Error("연못 턱 장식이 놓이지 않았습니다.");
            // 둘레(높이 4)에서 연못 쪽으로 걸어 들어가면 구덩이로 떨어지는지(빠지면 나올 길이 없는 3단 구덩이).
            var body = player.GetComponent<CharacterController>();
            Vector3 keep = player.position;
            if (body != null)
            {
                Vector3 toward = Flat(FishingSpot.PondCentre.Value - player.position).normalized;
                for (int i = 0; i < 60; i++) { body.Move((toward * 4.2f + Vector3.down * 9f) * .05f); yield return null; }
                Note($"crucian rim walk: {keep:F1} → {player.position:F1} dropped={(keep.y - player.position.y):F1}m");
                Teleport(player, keep, toward);
                yield return Wait(.3f);
            }
        }
        if (seaInRiver > 0) Error("바다 물고기 그림자가 강 안쪽에 있습니다.");
        if (outside > 0) Error(label + "가 구역 밖에 나타났습니다.");
        yield return Step(key + "-shadow", FishShadow(species, key, label, start), 180f);
        if (!trout && !crucian)
        {
            // 연구 F09: 길고 굽이치는 몸 — 마디가 실제로 좌우로 굽는지 2초 동안 잰다.
            var eel = SpeciesFish(species, player.position);
            float sway = 0f;
            for (float t = 0f; t < 2f && eel != null && eel.Alive; t += Time.deltaTime) { sway = Mathf.Max(sway, eel.SegmentSway); yield return null; }
            Note($"eel sway={sway:F2} (마디 최대 좌우, 머리 기준 단위)");
            if (sway < .12f) Error("장어 그림자가 굽이치지 않습니다.");
        }
        // 연못은 둘레 바위(높이 4)에서 드리워야 한다(구덩이 안 턱은 실제로 내려갈 수 없음) — 남쪽 둘레 칸, 연못 쪽을 본다.
        if (crucian) _fishStand = Grounded(new Vector3(FishingSpot.PondCentre.Value.x, 0f, FishingSpot.PondCentre.Value.z - 3.6f));
        yield return Step(key + "-catch", FishCatch(species, key, label, trout ? "Items/Item_Fish_Trout" : crucian ? "Items/Item_Fish_Crucian" : "Items/Item_Fish_Eel", trout ? 3008 : crucian ? 3010 : 3009), 420f);
        if (crucian)
        {
            Note($"crucian rim stand={_fishStand.Value:F1} (둘레 높이 — 구덩이 안 턱이면 y≈1)");
            if (_fishStand.Value.y < 3f) Error("붕어를 둘레가 아닌 구덩이 안에서 낚았습니다.");
            _fishStand = null;
        }
        if (crucian) yield return Step(key + "-grill", FishKitchen(key, "Items/Item_Fish_Crucian", "Items/Item_10_GrilledFish", "Recipe_GrilledCrucian", "생선구이 · 붕어", false), 90f);
        else if (trout) yield return Step(key + "-smoke", FishKitchen(key, "Items/Item_Fish_Trout", "Items/Item_SmokedFish", "Recipe_SmokedTrout", "훈제 생선 · 송어", true), 90f);
        else yield return Step(key + "-grill", FishKitchen(key, "Items/Item_Fish_Eel", "Items/Item_10_GrilledFish", "Recipe_GrilledEel", "생선구이 · 장어", false), 90f);
    }

    // ───────────── plan: firefly (연구 B12: 밤 강가 애반딧불이 → 멈춰 깜빡임 → 짧게 날 때는 기다림 안내·안 잡힘 → 멈췄을 때 잡기 → 도감 → 낮엔 없음) ─────────────
    IEnumerator PlanFirefly()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 firefly 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(22f); // firefly-r222: 20:30은 아직 밝아 빛이 잘 안 보였다
        yield return Step("firefly-catch", FireflyCatch(), 150f);
    }

    IEnumerator FireflyCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Firefly");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/반딧불이 아이템 없음"); yield break; }
        yield return Until(() => FindFirstObjectByType<FireflySpawner>()?.Spawned > 0, 10f);
        var spawner = FindFirstObjectByType<FireflySpawner>();
        var flies = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(FireflySpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        Note($"fireflies={flies.Count} routes={(spawner != null ? string.Join(",", Enumerable.Range(0, spawner.Spawned).Select(spawner.RouteLength)) : "-")} river={FishingSpot.RiverCells.Count} at=" + string.Join(" ", flies.Select(b => b.transform.position.ToString("F1"))));
        if (flies.Count == 0 || spawner == null) { Error("반딧불이가 놓이지 않았습니다."); yield break; }
        int pick = Enumerable.Range(0, spawner.Spawned).FirstOrDefault(i => spawner.RouteLength(i) >= 2);
        var fly = flies[pick];
        Vector3 at = fly.transform.position;
        Note($"firefly over fresh={FishingSpot.IsFresh(at)} water={(FindFirstObjectByType<WorldGridService>() is WorldGridService g && g.WorldToCell(at, out var cell) && g.TryGetCell(cell, out var data) && data.HasWater)}");
        Vector3 side = LandSide(at, 2.6f);
        Teleport(player, Grounded(at + side * 2.6f), -side);
        yield return Wait(1.2f);
        // 멈춘 동안 깜빡임: 3초 동안 빛의 최소·최대.
        yield return Until(() => !fly.Fleeing, 4f);
        float lo = 1f, hi = 0f;
        for (float t = 0f; t < 3f; t += Time.deltaTime) { float gl = spawner.GlowOf(pick); lo = Mathf.Min(lo, gl); hi = Mathf.Max(hi, gl); yield return null; }
        yield return Until(() => spawner.GlowOf(pick) > .9f, 2f);
        yield return ShotClean("firefly_1_glow_clean.png", "밤 강가 · 깜빡이는 애반딧불이(기본 카메라, 2.6m)");
        Note($"BLINK paused glow min={lo:F2} max={hi:F2}");
        if (hi < .8f || lo > .2f) Error("멈춘 반딧불이가 깜빡이지 않습니다.");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        Vector3 from = fly.Home;
        int hops0 = FireflySpawner.Hops;
        // firefly-r219/r220: 출발 자리 앞이면 날아가 손 닿는 거리 밖, 나는 도중에 옮겨 서면 놀라 달아났다 →
        // 날기 전에 다음 자리와의 가운데 남쪽 1.3m에 미리 서서(둘 다 놀람 거리 밖) 기다리고, 나는 동안 화면 안내를 계속 본다.
        // firefly-r221: '남쪽'이 강 건너편이면 강 바닥에 서서 높이 차로 대상이 안 됐다 → 반딧불이 쪽 마른 땅(높이 비슷한 쪽)에서 다가간다.
        Vector3 mid = Vector3.Lerp(from, spawner.NextOf(pick), .5f);
        side = LandSide(mid, 1.9f); // reg245: 1.3m는 강가 미끄러짐이 놀람(1.6m)으로 읽힐 때가 있었다 → 놀람 밖·안내(2.05m) 안
        Teleport(player, Grounded(mid + side * 1.9f), -side);
        int startles0 = fly.WaryStartles; float runMax = 0f; var pcFly = player.GetComponent<PlayerController>();
        for (float t = 0f; t < 10f && !fly.Fleeing; t += Time.deltaTime) { if (pcFly != null) runMax = Mathf.Max(runMax, pcFly.ActualPlanarSpeed); yield return null; }
        Note($"firefly wait: hops {hops0}→{FireflySpawner.Hops} startles {startles0}→{fly.WaryStartles} playerMaxSpeed={runMax:F1} player={player.position:F1} fly={fly.transform.position:F1}");
        if (!fly.Fleeing) { Error($"반딧불이가 10초 안에 다음 자리로 옮기지 않았습니다(hops {hops0}→{FireflySpawner.Hops})."); yield break; }
        string flyShown = "";
        bool flying = false;
        while (fly.Fleeing)
        {
            flying = true;
            flyShown = ShownPrompt(fly.transform);
            if (flyShown.Contains("기다려요")) break;
            yield return null;
        }
        int count0 = inventory.CountItems(bugItem);
        PressInteract();
        yield return Wait(.25f);
        bool caughtInFlight = fly.Captured || inventory.CountItems(bugItem) > count0;
        yield return Until(() => !fly.Fleeing, 4f);
        float hop = Flat(fly.Home - from).magnitude;
        Note($"FLIGHT shown='{flyShown}' pressedWhileFlying={flying} caughtInFlight={caughtInFlight} hop={hop:0.0}m");
        if (!flyShown.Contains("기다려요")) Error("나는 동안 화면 안내가 기다리라고 하지 않습니다: " + flyShown);
        if (caughtInFlight) Error("나는 동안 반딧불이가 잡혔습니다.");
        if (hop < 1.2f) Error($"반딧불이가 다른 자리로 옮기지 않았습니다({hop:0.0}m).");
        Vector3 b = fly.transform.position;
        side = LandSide(b, 1f);
        Teleport(player, Grounded(b + side * 1f), -side);
        yield return Wait(.4f);
        string prompt = fly.GetInteractPrompt();
        yield return ShotUI("firefly_2_near_ui.png", "멈춘 반딧불이 1m 앞 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3111), 2f);
        Note($"RESULT prompt='{prompt}' captured={fly.Captured} firefly {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3111)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total}");
        if (!fly.Captured || inventory.CountItems(bugItem) <= count0) Error("멈춘 반딧불이를 잡지 못했습니다.");
        yield return ShotUI("firefly_3_caught_ui.png", "반딧불이 포획 · 새 발견");
        SetHour(12f);
        yield return Wait(1.2f);
        int visible = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(x => x.name.StartsWith(FireflySpawner.NamePrefix) && x.gameObject.activeInHierarchy);
        Note($"day visible fireflies={visible}");
        if (visible > 0) Error("낮에도 반딧불이가 보입니다.");
    }

    // 강가 곤충 앞: 네 방향 중 dist 앞이 물이 아니고 땅 높이가 곤충 아래 땅과 가장 비슷한 방향(강 건너·강 바닥에 서지 않게).
    static Vector3 LandSide(Vector3 at, float dist)
    {
        var grid = FindFirstObjectByType<WorldGridService>();
        float baseY = Grounded(at).y;
        Vector3 best = Vector3.back; float bestScore = float.MaxValue;
        foreach (var dir in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward })
        {
            Vector3 p = at + dir * dist;
            bool water = grid != null && grid.WorldToCell(p, out var c) && grid.TryGetCell(c, out var cell) && cell.HasWater;
            float score = (water ? 100f : 0f) + Mathf.Abs(Grounded(p).y - baseY) + (dir == Vector3.back ? 0f : .05f);
            if (score < bestScore) { bestScore = score; best = dir; }
        }
        return best;
    }

    // ───────────── plan: pendant (연구 R05: 해변 유리1 + 구리 주괴1 → 현장 작업대 해변 유리 펜던트 → 쓰임(판매·관광객) → 가방) ─────────────
    IEnumerator PlanPendant()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 pendant 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(11f);
        yield return Step("pendant-craft", PendantRun(), 120f);
    }

    IEnumerator PendantRun()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        var ui = CraftingUI.instance;
        var glass = Res("Items/Item_BeachGlass");
        var copper = Res("Items/Item_CopperBar");
        var pendant = Res("Items/Item_GlassPendant");
        if (player == null || inventory == null || settlement == null || ui == null || glass == null || copper == null || pendant == null) { Error("펜던트 준비 실패"); yield break; }
        if (inventory.CountItems(glass) < 1) inventory.TryReceiveToHotbar(glass, 1);
        if (inventory.CountItems(copper) < 1) inventory.TryReceiveToHotbar(copper, 1);
        inventory.RefreshAllUI();
        var bench = settlement.PlacedWorkbench;
        if (bench == null) { Error("현장 작업대 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return OpenStation(player, bench);
        yield return Pick(ui, 2062);
        string uses = ui.DetailUsesText;
        yield return ShotUI("pendant_1_recipe_ui.png", "현장 작업대 · 해변 유리 펜던트(유리1 + 구리 주괴1)");
        int before = inventory.CountItems(pendant), glassBefore = inventory.CountItems(glass), copperBefore = inventory.CountItems(copper);
        yield return Craft(ui, 2062, 1);
        yield return ShotUI("pendant_2_done_ui.png", "펜던트 완성");
        ui.Close(); yield return Wait(.4f);
        Note($"RESULT pendant {before}→{inventory.CountItems(pendant)} glass {glassBefore}→{inventory.CountItems(glass)} copper {copperBefore}→{inventory.CountItems(copper)} uses='{uses.Replace("\n", " / ")}'");
        if (inventory.CountItems(pendant) != before + 1) Error("펜던트를 만들지 못했습니다.");
        if (inventory.CountItems(glass) != glassBefore - 1 || inventory.CountItems(copper) != copperBefore - 1) Error("재료가 맞게 쓰이지 않았습니다.");
        if (!uses.Contains("판매") || !uses.Contains("관광객")) Error("펜던트 쓰임에 판매·관광객 안내가 없습니다: " + uses);
    }

    // ───────────── plan: hints (전시용 새 콘텐츠 안내: 정착 뒤 시간대마다 아직 못 찾은 종을 한 번씩 알림 → 겹치지 않음 → 찾은 종은 안 알림) ─────────────
    IEnumerator PlanHints()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 hints 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        // 고정 저장은 오후라 정착 준비 중 이미 오후 안내(장어)가 먼저 뜰 수 있다 → 시간 순서 대신 '오전에 송어가 뜨는지'와 '간격'을 본다.
        SetHour(10.5f);
        yield return Until(() => DiscoveryHints.WasShown(3008), 95f);
        string morning = DiscoveryHints.LastText;
        yield return Wait(.5f); // r247: 알림을 띄운 그 프레임에 찍으면 아직 그려지지 않았다
        yield return ShotUI("hints_1_morning_ui.png", "오전 안내(송어)");
        SetHour(14.5f);
        // reg255: 같은 시간대엔 가까운 것부터(붕어 → … → 장어)라 오후에 붕어가 먼저 뜨면 장어는 75초 뒤 → 넉넉히 기다린다.
        yield return Until(() => DiscoveryHints.WasShown(3009), 170f);
        yield return ShotUI("hints_2_afternoon_ui.png", "오후 안내(장어)");
        var times = DiscoveryHints.ShownTimes;
        float minGap = float.MaxValue;
        for (int i = 1; i < times.Count; i++) minGap = Mathf.Min(minGap, times[i] - times[i - 1]);
        Note($"RESULT hints shown={DiscoveryHints.ShownCount} trout={DiscoveryHints.WasShown(3008)} eel={DiscoveryHints.WasShown(3009)} minGap={(times.Count > 1 ? minGap.ToString("F0") : "-")}s morning='{morning.Replace("\n", " / ")}'");
        if (!DiscoveryHints.WasShown(3008) || !morning.Contains("송어")) Error("오전 송어 안내가 뜨지 않았습니다: " + morning);
        if (!DiscoveryHints.WasShown(3009)) Error("오후 장어 안내가 뜨지 않았습니다.");
        if (times.Count > 1 && minGap < 74f) Error($"안내가 간격 없이 연달아 떴습니다({minGap:F0}s).");
    }

    // ───────────── plan: visitor (전시 관람객 동선 리허설: 새 게임 → 튜토리얼 → 동행 → 항해·도착 → 보급 상자 → 가게·텐트 설치 → 정착 → 전문 분야(휴대폰) → 새 콘텐츠 안내 → 첫 영업 → 보고서, 저장 없이 한 세션) ─────────────
    IEnumerator PlanVisitor()
    {
        float started = Now;
        yield return Step("title", StoryTitle(), 50f);
        if (_primary == null) { Error("타이틀 '새 게임' 버튼이 없습니다."); yield break; }
        yield return Step("new-game", StoryNewGame(), 25f);
        yield return Step("tutorial", StoryTutorial(), 70f);
        yield return Step("companions", StoryCompanions(), 35f);
        var selection = FindFirstObjectByType<DepartureCompanionSelection>();
        if (selection == null || !selection.IsConfirmed) { Error("동행 확정이 없습니다."); yield break; }
        yield return Step("voyage", StoryVoyage(), 60f);
        yield return Step("arrival", StoryArrival(), 200f);
        if (!WorldPlayable()) { Error("섬에서 조작할 수 없습니다."); yield break; }
        Note($"VISITOR island reached t={Now - started:F0}s");
        yield return Step("supply", VisitorSupply(), 60f);
        yield return Step("settle", VisitorSettle(), 300f);
        yield return Step("root", VisitorRoot(), 30f);
        Note($"VISITOR settled t={Now - started:F0}s");
        // 실제 흐름: 전문 분야를 고르면 해가 지기 시작해 밤(20시)까지 시간을 건드리지 않고 기다린다.
        var settlement = DemoSettlementController.Instance;
        float sunsetFrom = Now; int hintsBefore = DiscoveryHints.ShownCount;
        // 해 질 녘 준비: 가게를 세우면 받은 가판대를 실제 핫바 배치로 가게 안에 놓는다(첫날 실제 순서).
        yield return Step("stands", VisitorStands(), 90f);
        yield return Until(() => settlement != null && settlement.NightReady, 160f);
        Note($"VISITOR sunset {Now - sunsetFrom:F0}s nightReady={settlement?.NightReady} hintsDuringSunset={DiscoveryHints.ShownCount - hintsBefore}");
        if (settlement == null || !settlement.NightReady) { Error("해가 져 첫 영업 시간이 오지 않았습니다."); yield break; }
        if (DiscoveryHints.ShownCount != hintsBefore) Error("첫 영업 전(해 질 녘 준비 중)에 새 콘텐츠 안내가 떴습니다.");
        yield return Step("market", MarketNight(), 220f);
        var loop = DayNightShopLoopController.Instance;
        yield return Until(() => loop == null || loop.OpeningSessionCompleted, 60f);
        var report = FindFirstObjectByType<DemoPioneerReport>()?.LastReport;
        Note($"VISITOR first opening done t={Now - started:F0}s completed={loop?.OpeningSessionCompleted} report={(report != null ? report.rankKey + " total=" + report.total : "-")}");
        yield return Step("hints", VisitorHints(), 100f);
        Note($"VISITOR done t={Now - started:F0}s");
    }

    IEnumerator VisitorSupply()
    {
        Transform player = PlayerTransform();
        var box = FindFirstObjectByType<FirstDaySupplyBox>();
        if (player == null || box == null) { Error("보급 상자 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return StandAt(player, box.transform);
        string prompt = ShownPrompt(box.transform);
        int before = Inventory.instance != null ? Inventory.instance.hotbar.slots.Count(x => x != null && !x.IsEmpty) : 0;
        PressInteract();
        yield return Wait(1.2f);
        int after = Inventory.instance != null ? Inventory.instance.hotbar.slots.Count(x => x != null && !x.IsEmpty) : 0;
        yield return ShotUI("visitor_1_supply_ui.png", "보급 상자를 연 뒤 핫바");
        Note($"VISITOR supply prompt='{prompt}' hotbar {before}→{after}");
        if (after <= before) Error("보급 상자에서 받은 것이 없습니다: " + prompt);
    }

    IEnumerator VisitorSettle()
    {
        Transform player = PlayerTransform();
        var settlement = DemoSettlementController.Instance;
        var catalog = DemoPlaceableCatalog.Load();
        var shopKit = catalog?.Find(DemoPlaceableKind.ShopBase)?.item;
        var tentKit = catalog?.Find(DemoPlaceableKind.ResidentTent)?.item;
        var box = FindFirstObjectByType<FirstDaySupplyBox>();
        if (player == null || settlement == null || shopKit == null || tentKit == null) { Error("정착 준비 실패(키트)"); yield break; }
        Note($"VISITOR kits shop={Inventory.instance.CountItems(shopKit)} tent={Inventory.instance.CountItems(tentKit)}");
        Vector3 around = box != null ? box.transform.position : player.position;
        yield return PlaceKit(player, shopKit, around + Vector3.forward * 6f, new[] { 6f, 9f, 12f, 16f, 20f });
        yield return PlaceKit(player, tentKit, around + Vector3.left * 6f, new[] { 4f, 6f, 8f, 10f, 13f });
        yield return PlaceKit(player, tentKit, around + Vector3.right * 6f, new[] { 4f, 6f, 8f, 10f, 13f });
        yield return Until(() => settlement.Established, 60f);
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Wait(1f);
        yield return ShotUI("visitor_2_settled_ui.png", "가게·텐트 설치 뒤");
        Note($"VISITOR established={settlement.Established} tents={settlement.TentCount} residents={settlement.Residents.Count} shop={(settlement.OperatingShop != null)}");
        if (!settlement.Established) Error("가게와 텐트 둘을 세웠는데 정착이 완료되지 않았습니다.");
    }

    IEnumerator VisitorRoot()
    {
        var settlement = DemoSettlementController.Instance;
        var phone = SmartphoneUI.instance;
        if (settlement == null || phone == null) { Error("휴대폰/정착지 없음"); yield break; }
        if (!phone.IsOpen) phone.Toggle();
        yield return Wait(.4f);
        phone.OpenShopApp();
        yield return Wait(.6f);
        var mining = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Root1");
        if (mining != null) mining.onClick.Invoke();
        yield return Wait(.5f);
        yield return ShotUI("visitor_3_root_ui.png", "휴대폰 상점 앱 · 광업 선택");
        if (phone.IsOpen) phone.Toggle();
        yield return Wait(.3f);
        Note($"VISITOR root button={(mining != null)} selected={settlement.SelectedRoot}");
        if (settlement.SelectedRoot != DemoSpecialization.Mining) Error("휴대폰에서 광업을 고르지 못했습니다.");
    }

    IEnumerator VisitorStands()
    {
        Transform player = PlayerTransform();
        var settlement = DemoSettlementController.Instance;
        var stand = DemoPlaceableCatalog.Load()?.Find(DemoPlaceableKind.DisplayStand)?.item;
        if (player == null || settlement == null || settlement.OperatingShop == null || stand == null) { Error("가판대 준비 실패"); yield break; }
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        int have = Inventory.instance.CountItems(stand);
        Note($"VISITOR stands in bag={have} slotsBefore={settlement.OperatingShop.Slots.Count(x => x != null && x.isActiveAndEnabled)}");
        if (interior != null && !interior.IsInside) { player.GetComponent<EquipmentSystem>()?.Holster(); interior.entrance.Interact(player.gameObject); }
        yield return Until(() => interior == null || interior.IsInside, 8f);
        yield return Wait(1f);
        Vector3 centre = interior != null && interior.insideSpawn != null ? interior.insideSpawn.position : settlement.OperatingShop.transform.position;
        for (int i = 0; i < have; i++)
            yield return PlaceKit(player, stand, centre, new[] { 1.5f, 2.5f, 3.5f, 4.5f });
        player.GetComponent<EquipmentSystem>()?.Holster();
        int slots = settlement.OperatingShop.Slots.Count(x => x != null && x.isActiveAndEnabled);
        Note($"VISITOR stands placed slots={slots} left={Inventory.instance.CountItems(stand)}");
        if (slots == 0) Error("가게 안에 가판대를 놓지 못했습니다.");
    }

    IEnumerator VisitorHints()
    {
        // 첫 영업을 마친 밤: 시간을 건드리지 않고(밤 곤충·하구 장어 시간) 안내가 뜨는지 본다. 보고서 카드는 실제 [닫기]로 닫는다.
        if (PioneerReportCardUI.Instance != null && PioneerReportCardUI.Instance.IsOpen) PioneerReportCardUI.Instance.Hide();
        // visitor-r253: 첫 영업이 끝나면 보고서를 보여 주려고 휴대폰이 열린다 → 실제 플레이어처럼 닫고 본다.
        yield return Wait(1.5f);
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen) { yield return ShotUI("visitor_3b_report_phone_ui.png", "첫 영업 뒤 휴대폰 보고서"); SmartphoneUI.instance.Toggle(); }
        yield return Wait(.5f);
        var loopH = DayNightShopLoopController.Instance;
        Note($"VISITOR modal parts timeScale={Time.timeScale} phone={SmartphoneUI.instance?.IsOpen} panel={DemoSettlementController.Instance?.IsPanelOpen} craft={CraftingUI.instance?.IsOpen} price={ShopPriceUI.instance?.IsOpen} bag={(InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)} fishing={FirstDayFishingMinigame.IsOpen}");
        Note($"VISITOR before hints hour={(GameClock.Instance != null ? GameClock.Instance.CurrentHour : -1):F2} day={(GameClock.Instance != null ? GameClock.Instance.CurrentDay : -1)} modal={PlayerInputHandler.ModalOpen} dialogue={DialogueUI.IsOpen} firstDay={PlayerInputHandler.Instance?.FirstDayControls} established={DemoSettlementController.Instance?.Established} openingDemo={loopH?.IsOpeningDemo} completed={loopH?.OpeningSessionCompleted}");
        yield return Until(() => DiscoveryHints.ShownCount >= 1, 90f);
        yield return Wait(.5f);
        yield return ShotUI("visitor_4_hint_ui.png", "첫 영업 뒤 새 콘텐츠 안내");
        Note($"VISITOR hints shown={DiscoveryHints.ShownCount} text='{DiscoveryHints.LastText.Replace("\n", " / ")}'");
        if (DiscoveryHints.ShownCount == 0) Error("첫 영업 뒤 새 콘텐츠 안내가 뜨지 않았습니다.");
    }

    static Vector3 Grounded(Vector3 at)
    {
        var hits = Physics.RaycastAll(new Vector3(at.x, 80f, at.z), Vector3.down, 160f, ~0, QueryTriggerInteraction.Ignore);
        float y = hits.Length > 0 ? hits.Max(h => h.point.y) : at.y;
        return new Vector3(at.x, y, at.z);
    }

    // ───────────── plan: grasshopper (연구 B04: 풀밭·모래 가장자리 방아깨비 → 뛰는 동안은 못 잡음 → 멈췄을 때 잡기 → 도감 → 밤엔 없음) ─────────────
    IEnumerator PlanGrasshopper()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 grasshopper 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("grasshopper-catch", GrasshopperCatch(), 150f);
    }

    IEnumerator GrasshopperCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Grasshopper");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/방아깨비 아이템 없음"); yield break; }
        yield return Until(() => FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Any(b => b.name.StartsWith(GrasshopperSpawner.NamePrefix)), 10f);
        var hoppers = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(GrasshopperSpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        Note($"grasshoppers={hoppers.Count} flowerSpots={CoastalVegetationField.FlowerSpots.Count} at=" + string.Join(" ", hoppers.Select(b => b.transform.position.ToString("F1"))));
        if (hoppers.Count == 0) { Error("방아깨비가 놓이지 않았습니다."); yield break; }
        var bug = hoppers[0];
        // 1) 풀밭 가장자리 땅 위 방아깨비(기본 카메라, 서쪽 2.5m에서 동쪽을 본다).
        Teleport(player, bug.transform.position + Vector3.left * 2.5f, Vector3.right);
        yield return Wait(1f);
        yield return ShotClean("grasshopper_1_edge_clean.png", "풀밭·모래 가장자리 방아깨비(기본 카메라, 2.5m)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        // 2) 폴짝 뛰는 동안: 안내는 '기다려요', 휘둘러도 잡히지 않는다.
        yield return Until(() => !bug.Fleeing, 3f);
        Vector3 from = bug.Home;
        int hops0 = GrasshopperSpawner.Hops;
        yield return Until(() => bug.Fleeing, 12f);
        if (!bug.Fleeing) { Error($"방아깨비가 12초 안에 뛰지 않았습니다(hops {hops0}→{GrasshopperSpawner.Hops})."); yield break; }
        string jumpPrompt = bug.GetInteractPrompt();
        Vector3 b = bug.transform.position;
        Vector3 side = FreeSide(player, b, 1.2f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.2f, side);
        // reg225: 첫 두 프레임만 보면 방아깨비가 이미 정면 밖으로 뛰어 '장식 나무' 안내가 잡혔다 → 뛰는 동안 화면 안내를 계속 본다(반딧불이와 같은 방식).
        string jumpShown = "";
        bool stillJumping = false;
        while (bug.Fleeing)
        {
            stillJumping = true;
            jumpShown = ShownPrompt(bug.transform);
            if (jumpShown.Contains("기다려요")) break;
            yield return null;
        }
        int count0 = inventory.CountItems(bugItem);
        PressInteract();
        yield return Wait(.25f);
        bool caughtInJump = bug.Captured || inventory.CountItems(bugItem) > count0;
        yield return Until(() => !bug.Fleeing, 3f);
        float hop = Flat(bug.Home - from).magnitude;
        Note($"JUMP prompt='{jumpPrompt}' shown='{jumpShown}' pressedWhileJumping={stillJumping} caughtInJump={caughtInJump} hop={hop:0.0}m hops {hops0}→{GrasshopperSpawner.Hops} landed={!bug.Fleeing}");
        if (!jumpPrompt.Contains("기다려요")) Error("뛰는 동안 안내가 기다리라고 하지 않습니다: " + jumpPrompt);
        if (stillJumping && !jumpShown.Contains("기다려요")) Error("뛰는 동안 화면 안내가 기다리라고 하지 않습니다: " + jumpShown);
        if (caughtInJump) Error("뛰는 동안 방아깨비가 잡혔습니다.");
        if (hop < .8f) Error($"방아깨비가 뛰어 자리를 옮기지 않았습니다({hop:0.0}m).");
        // 3) 멈춘 자리 옆 1m(나무·덤불과 겹치지 않는 쪽)에서 → 잡기.
        // grasshopper-r182: 나무 줄기와 겹친 자리로 순간이동하면 다음 프레임 밀려나는 거리가 '달리기'(>4.6m/s)로 재여 방아깨비가 놀라 뛰었다(캡처 배치 문제).
        b = bug.transform.position;
        side = FreeSide(player, b, 1.0f);
        Teleport(player, new Vector3(b.x, player.position.y, b.z) - side * 1.0f, side);
        yield return Wait(.4f);
        string prompt = bug.GetInteractPrompt();
        yield return ShotUI("grasshopper_2_near_ui.png", "멈춘 방아깨비 1m 옆 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3106), 2f);
        Note($"RESULT prompt='{prompt}' captured={bug.Captured} grasshopper {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3106)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} feedback='{bug.LastFeedback}'");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("멈춘 방아깨비를 잡지 못했습니다.");
        if (!prompt.Contains("방아깨비") || !prompt.Contains("휘두르기")) Error("멈춘 방아깨비 프롬프트가 잡기를 안내하지 않습니다: " + prompt);
        yield return ShotUI("grasshopper_3_caught_ui.png", "방아깨비 포획 · 새 발견");
        // 밤에는 보이지 않는다.
        SetHour(20f);
        yield return Wait(1.2f);
        int visible = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(x => x.name.StartsWith(GrasshopperSpawner.NamePrefix) && x.gameObject.activeInHierarchy);
        Note($"night visible grasshoppers={visible}");
        if (visible > 0) Error("밤에도 방아깨비가 보입니다.");
    }

    // ───────────── plan: dragonfly (연구 B06: 물가 바위 위 고추잠자리 → 물가에서 잡기 → 도감 → 밤엔 없음) ─────────────
    IEnumerator PlanDragonfly()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 dragonfly 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        SetHour(10.5f);
        yield return Step("dragonfly-catch", DragonflyCatch(), 120f);
    }

    IEnumerator DragonflyCatch()
    {
        Transform player = PlayerTransform();
        var inventory = Inventory.instance;
        var net = Res("Items/Item_Net");
        var bugItem = Res("Items/Item_Bug_Dragonfly");
        if (player == null || inventory == null || net == null || bugItem == null) { Error("플레이어/잠자리채/고추잠자리 아이템 없음"); yield break; }
        yield return Until(() => FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Any(b => b.name.StartsWith(DragonflySpawner.NamePrefix)), 8f);
        var bugs = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => b.name.StartsWith(DragonflySpawner.NamePrefix)).OrderBy(b => b.name).ToList();
        Note($"dragonflies={bugs.Count} stones={CoastalVegetationField.WaterlineStones.Count} at=" + string.Join(" ", bugs.Select(b => b.transform.position.ToString("F1"))));
        if (bugs.Count == 0) { Error("고추잠자리가 놓이지 않았습니다."); yield break; }
        var bug = bugs[0];
        // 가장 가까운 물가 바위의 모래 칸에서 물 쪽을 본다.
        var stone = CoastalVegetationField.WaterlineStones.OrderBy(s => Flat(s.Shore - bug.transform.position).sqrMagnitude).First();
        Vector3 toWater = Flat(stone.Stone - stone.Shore).normalized;
        Teleport(player, stone.Shore - toWater * 2.2f, toWater);
        yield return Wait(1f);
        yield return ShotClean("dragonfly_1_waterside_clean.png", "물가 바위 위 고추잠자리(기본 카메라)");
        if (inventory.CountItems(net) < 1) inventory.TryReceiveToHotbar(net, 1);
        if (!SelectItem(net)) { Error("잠자리채 선택 실패"); yield break; }
        int count0 = inventory.CountItems(bugItem);
        // 떠다니는 자리 바로 아래 모래 쪽 1m.
        Vector3 b = bug.transform.position;
        Vector3 near = new Vector3(b.x, stone.Shore.y, b.z) - toWater * 1.0f;
        Teleport(player, near, toWater);
        yield return Wait(.5f);
        string prompt = bug.GetInteractPrompt();
        yield return ShotUI("dragonfly_2_near_ui.png", "물가에서 고추잠자리 · 안내");
        PressInteract();
        yield return Wait(GatherFeedback.ContactDelay + .6f);
        yield return Until(() => DiscoveryLog.IsFound(3104), 2f);
        Note($"RESULT prompt='{prompt}' captured={bug.Captured} dragonfly {count0}→{inventory.CountItems(bugItem)} found={DiscoveryLog.IsFound(3104)} collection={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} dist={Flat(bug.transform.position - player.position).magnitude:0.00}");
        if (!bug.Captured || inventory.CountItems(bugItem) <= count0) Error("고추잠자리를 잡지 못했습니다.");
        if (!prompt.Contains("고추잠자리")) Error("프롬프트가 고추잠자리 이름을 쓰지 않습니다: " + prompt);
        yield return ShotUI("dragonfly_3_caught_ui.png", "고추잠자리 포획 · 새 발견");
        SetHour(20f);
        yield return Wait(1.2f);
        int visible = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Count(x => x.name.StartsWith(DragonflySpawner.NamePrefix) && x.gameObject.activeInHierarchy);
        Note($"night visible dragonflies={visible}");
        if (visible > 0) Error("밤에도 고추잠자리가 보입니다.");
    }

    // ───────────── plan: night (물가·정착지의 밤 화면 확인) ─────────────
    IEnumerator PlanNight()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 night 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("night-shots", NightShots(), 90f);
    }

    IEnumerator NightShots()
    {
        Transform player = PlayerTransform();
        if (player == null) { Error("플레이어 없음"); yield break; }
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Placed > 0, 10f);
        var find = BeachFinds.Instance?.Finds.FirstOrDefault(f => f != null);
        var settlement = DemoSettlementController.Instance;
        // 다음 날 아침으로 넘겨 아침 소식(해변 발견물·파기 자국 새로 놓임)과 새 배치를 확인한다.
        int dayBefore = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        int newsBefore = DiscoveryLog.NewsShown;
        GameClock.Instance?.AdvanceToNextDayMorning(7f, ClockReason);
        yield return Until(() => DiscoveryLog.NewsShown > newsBefore, 8f);
        Note($"next day {dayBefore}→{GameClock.Instance?.CurrentDay} beachDay={BeachFinds.Instance?.Day} placed={BeachFinds.Instance?.Placed} digDay={DigSpots.Instance?.Day} spots={DigSpots.Instance?.Spots.Count} news={DiscoveryLog.NewsShown - newsBefore}");
        if (DiscoveryLog.NewsShown <= newsBefore) Error("다음 날 아침 소식이 뜨지 않았습니다.");
        yield return ShotUI("night_0_morning_news_ui.png", "다음 날 아침 소식");
        find = BeachFinds.Instance?.Finds.FirstOrDefault(f => f != null);
        foreach (var (hour, tag) in new[] { (21.5f, "night"), (5.6f, "dawn"), (18.2f, "dusk") })
        {
            SetHour(hour);
            if (find != null)
            {
                Teleport(player, find.transform.position + Vector3.right * 2.5f, Vector3.left);
                yield return Wait(1.5f);
                // 반짝임이 켜진 순간에 찍는다(2.8초 주기 중 0.55초).
                var sparkle = find.GetComponentInChildren<FindSparkle>(true)?.GetComponent<SpriteRenderer>();
                yield return Until(() => sparkle == null || sparkle.enabled && sparkle.color.a > .7f, 3.5f);
                Note($"sparkle {tag} visible={(sparkle != null && sparkle.enabled)} alpha={(sparkle != null ? sparkle.color.a : 0):0.00}");
                yield return ShotClean($"night_{tag}_shore_clean.png", $"물가 {hour:0.0}h");
            }
            if (settlement != null && settlement.OperatingShop != null)
            {
                var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
                Vector3 front = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : settlement.OperatingShop.transform.position;
                Teleport(player, front + Vector3.back * 4f, Vector3.forward);
                yield return Wait(1.5f);
                yield return ShotClean($"night_{tag}_settlement_clean.png", $"정착지 {hour:0.0}h");
            }
        }
    }

    // ───────────── plan: npc (2026-11-02 사용자 "NPC가 거꾸로 다닌다 · 광부 모자 매칭 · 대화 수준") ─────────────
    // 저장 사본 이어하기 → 동행마다 외형(메시)·역할을 기록 → 낮 일과로 걷는 동행의 몸 정면(엉덩이 축)과 실제 이동 방향의
    // 각도를 매 프레임 잰다(0° 앞으로, 180° 뒷걸음) → 따라가는 카메라 컷 → 아침·밤 연속 대화 원문을 notes에 남긴다.
    IEnumerator PlanNpc()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 npc 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        yield return Step("npc-walk", NpcWalk(), 120f);
        yield return Step("npc-work", NpcWork(), 90f);
        yield return Step("npc-talk", NpcTalk(), 120f);
        yield return Step("npc-glance", NpcGlancePass(), 60f);
        yield return Step("npc-inspect", NpcInspect(), 70f);
        yield return Step("npc-gather", NpcGather(), 220f);
    }

    // K-04 저녁 모임: 17:15~ 동행이 가게 문 옆에 각자 자리를 잡고 서로·간판을 번갈아 본다. 문 앞 손님 동선은 비운다.
    IEnumerator NpcGather()
    {
        Transform player = PlayerTransform();
        var settlement = DemoSettlementController.Instance;
        var shop = settlement != null ? settlement.OperatingShop : null;
        if (player == null || shop == null) { Error("플레이어/가게 없음"); yield break; }
        DialogueUI.instance?.Hide();
        SetHour(17.3f);
        var routines = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None).ToList();
        // 생산 FSM이 일하는 동행(광산 생산 중)은 모임에 오지 않는다(일을 마저 함). 나머지가 모두 자리에 설 때까지(광산에서 걸어오는 시간 포함).
        yield return Wait(.5f);
        float end = Now + 80f;
        bool Busy(DemoCompanionRoutine r) => r.GetComponent<ProducerNpcController>() is var p && p != null && p.StarterWorking;
        while (Now < end && routines.Any(r => !r.AtGatherSpot && !Busy(r))) yield return null;
        var interior = shop.GetComponentInParent<DemoShopInterior>(true);
        Vector3 door = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : shop.transform.position;
        foreach (var r in routines)
            Note($"gather {r.name} activity={r.Current} at={r.AtGatherSpot} status='{r.Status}' doorDist={Flat(r.transform.position - door).magnitude:0.0} hour={GameClock.Instance?.CurrentHour:0.00}");
        var gathered = routines.Where(r => r.AtGatherSpot).ToList();
        if (gathered.Count == 0) { Error("가게 앞에 모인 동행이 없습니다."); yield break; }
        if (gathered.Any(r => Flat(r.transform.position - door).magnitude < 1.2f)) Error("동행이 문 바로 앞(손님 동선)에 섰습니다.");
        Vector3 outward = Flat(door - shop.transform.position).normalized;
        Teleport(player, door + outward * 7f, -outward);
        int looksBefore = gathered.Sum(r => r.GatherLooks);
        yield return Wait(5f);
        Note($"gather looks {looksBefore}→{gathered.Sum(r => r.GatherLooks)} gathered={gathered.Count}/{routines.Count}");
        yield return ShotClean("npc_7_evening_gather_clean.png", "문 열기 전 가게 앞 저녁 모임");
        if (gathered.Sum(r => r.GatherLooks) <= looksBefore) Error("모인 동행이 서로·가게를 보지 않았습니다.");
        // 말 걸기: 실제 [E] 경로로 모임 대사.
        var first = gathered[0];
        player.GetComponent<EquipmentSystem>()?.Holster();
        yield return PressNear(player, first.transform);
        yield return Wait(.8f);
        Note($"gather talk line='{first.GetComponent<NpcDialogue>()?.LastLine}'");
        yield return ShotUI("npc_8_evening_gather_talk_ui.png", "모임 중 대화");
        DialogueUI.instance?.Hide();
        // 문을 열지 않으면 18:30에 집으로.
        SetHour(18.55f);
        yield return Wait(1.5f);
        Note($"after window: {string.Join(", ", routines.Select(r => $"{r.name}={r.Current}"))}");
        if (routines.Any(r => r.Current == DemoCompanionRoutine.Activity.Gathering)) Error("모임 시간이 지나도 흩어지지 않았습니다.");

        // K-03 이어짐: 다시 모인 뒤 18시에 문을 열면 모여 있던 동행이 그대로 첫 손님으로 들어간다.
        SetHour(17.4f);
        yield return Wait(.5f);
        float regather = Now + 60f;
        while (Now < regather && routines.Any(r => !r.AtGatherSpot)) yield return null;
        Note("regather: " + string.Join(", ", routines.Select(r => $"{r.name}={r.Current}/{r.AtGatherSpot}")));
        var loop = DayNightShopLoopController.Instance;
        SetHour(18.02f);
        yield return Wait(.3f);
        bool opened = loop != null && loop.TryOpenShop();
        Note($"door open={opened} customersOpen={loop?.IsShopOpenForCustomers}");
        if (!opened) { Note("이 저장에서는 영업을 열 수 없어 첫 방문 전환은 건너뜀"); yield break; }
        var entered = new HashSet<string>();
        float watch = Now + 30f;
        bool shot = false;
        while (Now < watch && entered.Count < routines.Count)
        {
            foreach (var r in routines)
                if (r.Current == DemoCompanionRoutine.Activity.Shopping || r.Current == DemoCompanionRoutine.Activity.GoingToShop) entered.Add(r.name);
            if (!shot && entered.Count > 0) { shot = true; yield return ShotClean("npc_9_gather_to_shop_clean.png", "문이 열리자 모여 있던 동행이 들어감"); }
            yield return null;
        }
        Note($"door → visit: entered={entered.Count}/{routines.Count} [{string.Join(", ", entered)}] states=" + string.Join(", ", routines.Select(r => $"{r.name}={r.Current}")));
        if (entered.Count == 0) Error("문을 열었는데 모여 있던 동행이 가게에 들어가지 않았습니다.");
    }

    // K-01 벌목꾼 살피기: 실제 채집 나무 앞에서 수관을 올려다보는 장면(생산 없음).
    IEnumerator NpcInspect()
    {
        Transform player = PlayerTransform();
        var lumberjack = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.GetComponent<DemoResident>()?.CompanionId?.StartsWith("Lumberjack") == true);
        if (player == null || lumberjack == null) { Note("벌목꾼 동행 없음(선택 조합) · 건너뜀"); yield break; }
        DialogueUI.instance?.Hide();
        SetHour(10f);
        float end = Now + 55f;
        while (Now < end && lumberjack.Current != DemoCompanionRoutine.Activity.Working) yield return null;
        var target = lumberjack.WorkTarget;
        Note($"lumberjack activity={lumberjack.Current} target={(target != null ? target.name : "-")} status='{lumberjack.Status}'");
        if (lumberjack.Current != DemoCompanionRoutine.Activity.Working) { Error("벌목꾼이 일터에 도착하지 않았습니다."); yield break; }
        Teleport(player, lumberjack.transform.position + Vector3.right * 4f - Vector3.forward * 2f, Vector3.left);
        yield return Wait(2.6f);
        var glance = lumberjack.GetComponent<NpcGlance>();
        Note($"inspect glance weight={glance?.Weight:0.00} hasGatherable={target != null && target.GetComponent<Gatherable>() != null}");
        yield return ShotClean("npc_6_lumberjack_inspect_clean.png", "벌목꾼이 실제 나무 앞에서 수관을 살핌");
    }

    // K-02 지나갈 때 알아보기: 동행 옆 3m 줄을 실제 이동 입력으로 걸어 지나가며 고개 돌림 가중치·횟수를 기록한다.
    IEnumerator NpcGlancePass()
    {
        Transform player = PlayerTransform();
        var routine = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None).FirstOrDefault();
        if (player == null || routine == null) { Error("플레이어/동행 없음"); yield break; }
        DialogueUI.instance?.Hide();
        var cam = Camera.main;
        // 대화를 마친 직후 6초는 알아보기를 쉬는 규칙(DemoCompanionRoutine.HoldForTalk)이라 그 뒤에, 그때 동행의 자리·정면 기준으로 지나간다.
        yield return Wait(7f);
        Vector3 side = Flat(routine.transform.right).normalized, ahead = Flat(routine.transform.forward).normalized;
        Vector3 start = routine.transform.position + ahead * 2.6f - side * 4f;
        Teleport(player, start, side);
        yield return Wait(.3f);
        int before = routine.Glances;
        float peak = 0f, minDistance = float.MaxValue, maxSpeed = 0f, maxAngleIn = 999f; int frames = 0;
        Vector3 startPos = player.position;
        var pc = player.GetComponent<PlayerController>();
        var glance = routine.GetComponent<NpcGlance>();
        for (float t = 0; t < 2.6f; t += Time.deltaTime)
        {
            if (cam != null)
            {
                Vector3 cf = Flat(cam.transform.forward).normalized, cr = Flat(cam.transform.right).normalized;
                PlayerInputHandler.QaMove = new Vector2(Vector3.Dot(side, cr), Vector3.Dot(side, cf));
            }
            glance ??= routine.GetComponent<NpcGlance>();
            if (glance != null) peak = Mathf.Max(peak, glance.Weight);
            Vector3 toPlayer = Flat(player.position - routine.transform.position);
            minDistance = Mathf.Min(minDistance, toPlayer.magnitude);
            if (pc != null) maxSpeed = Mathf.Max(maxSpeed, pc.ActualPlanarSpeed);
            maxAngleIn = Mathf.Min(maxAngleIn, Vector3.Angle(routine.transform.forward, toPlayer));
            if (frames++ == 70) yield return ShotClean("npc_5_glance_clean.png", "지나가는 플레이어를 고개로 알아봄");
            yield return null;
        }
        PlayerInputHandler.QaMove = null;
        Note($"glance pass: glances {before}→{routine.Glances} peakWeight={peak:0.00} status='{routine.Status}' talkPaused={routine.TalkPaused} " +
             $"moved={Flat(player.position - startPos).magnitude:0.0}m minDist={minDistance:0.0} maxSpeed={maxSpeed:0.0} minAngle={maxAngleIn:0} modal={PlayerInputHandler.ModalOpen} dialogue={DialogueUI.IsOpen}");
        if (routine.Glances <= before) Error("지나가는 플레이어를 알아보지 않았습니다.");
    }

    // 도구를 든 동행이 일하는 모습(들고 걷기 → 곡괭이 휘두르기)을 앞·옆 카메라로 30fps 프레임으로 남긴다(2026-11-02 "도구 들고 다니는 것").
    IEnumerator NpcWork()
    {
        SetHour(10f);
        DemoCompanionRoutine worker = null;
        float end = Now + 50f;
        while (Now < end && worker == null)
        {
            worker = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None).FirstOrDefault(c =>
                c.Current == DemoCompanionRoutine.Activity.Working && c.GetComponent<NpcHeldToolPose>() != null && c.GetComponent<NpcHeldToolPose>().Tool != null);
            yield return null;
        }
        if (worker == null) { Note("npc-work: 도구를 든 채 일하는 동행이 없어 건너뜁니다."); yield break; }
        var pose = worker.GetComponent<NpcHeldToolPose>();
        Note($"npc-work {worker.name} tool={pose.Tool.name} type={pose.Type}");
        Camera cam = Camera.main;
        if (cam == null) yield break;
        CameraHold hold = HoldCamera(cam);
        string dir = Path.Combine(OutputDirectory, "npc_work");
        Directory.CreateDirectory(dir);
        var rt = new RenderTexture(960, 540, 0);
        var read = new Texture2D(960, 540, TextureFormat.RGB24, false);
        int frame = 0, swings = 0;
        float lastSwing = pose.ActionStartedAt, stop = Now + 5f;
        try
        {
            while (Now < stop)
            {
                yield return null;
                Vector3 f = Flat(worker.transform.forward).normalized, r = Vector3.Cross(Vector3.up, f);
                cam.transform.position = worker.transform.position + f * 2.4f + r * 2.2f + Vector3.up * 1.4f;
                cam.transform.LookAt(worker.transform.position + Vector3.up * .9f);
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                Graphics.Blit(shot, rt);
                Destroy(shot);
                RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                read.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, frame.ToString("0000") + ".jpg"), read.EncodeToJPG(88));
                if (pose.ActionStartedAt != lastSwing) { lastSwing = pose.ActionStartedAt; swings++; Note($"npc-work swing{swings} frame={frame}"); }
                frame++;
            }
        }
        finally { ReleaseCamera(hold); Destroy(rt); Destroy(read); }
        Note($"npc-work frames={frame} swings={swings}");
    }

    IEnumerator NpcWalk()
    {
        SetHour(9.5f);
        yield return Wait(1f);
        var companions = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
        if (companions.Length == 0) { Error("동행(DemoCompanionRoutine)이 없습니다."); yield break; }
        foreach (var c in companions)
        {
            var skin = c.GetComponentInChildren<SkinnedMeshRenderer>();
            Note($"companion {c.name} name='{c.DisplayName}' mesh='{(skin != null && skin.sharedMesh != null ? skin.sharedMesh.name : "?")}' activity={c.Current}");
        }
        var last = companions.Select(c => c.transform.position).ToArray();
        var angles = companions.Select(_ => new List<float>()).ToArray();
        Camera cam = Camera.main;
        CameraHold hold = cam != null ? HoldCamera(cam) : null;
        int shots = 0;
        float start = Now, nextShot = Now + 3f;
        bool nudged = false;
        try
        {
            while (Now - start < 24f)
            {
                yield return null;
                float dt = Time.deltaTime;
                if (dt <= 0f) continue;
                DemoCompanionRoutine moving = null;
                Vector3 travel = Vector3.zero;
                for (int i = 0; i < companions.Length; i++)
                {
                    var c = companions[i];
                    if (c == null) continue;
                    Vector3 delta = Flat(c.transform.position - last[i]);
                    last[i] = c.transform.position;
                    if (delta.magnitude / dt < .6f || delta.magnitude / dt > 12f) continue;
                    var animator = c.GetComponentInChildren<Animator>();
                    if (animator == null || !animator.isHuman) continue;
                    Transform l = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), r = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    if (l == null || r == null) continue;
                    Vector3 body = Vector3.Cross(Flat(r.position - l.position).normalized, Vector3.up);
                    angles[i].Add(Mathf.Abs(Vector3.SignedAngle(delta.normalized, body, Vector3.up)));
                    if (moving == null) { moving = c; travel = delta.normalized; }
                }
                // 일과상 6초 동안 아무도 걷지 않으면 항구 쪽 NavMesh 지점으로 한 번 걷게 한다(측정용, 격리 저장 사본).
                if (!nudged && Now - start > 6f && angles.All(a => a.Count == 0))
                {
                    nudged = true;
                    foreach (var c in companions)
                        if (c != null && c.Agent != null && c.Agent.isOnNavMesh &&
                            UnityEngine.AI.NavMesh.SamplePosition(c.transform.position + Flat(-c.transform.forward).normalized * 10f + Vector3.right * 4f, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                            c.Agent.SetDestination(hit.position);
                    Note("일과상 걷는 동행이 없어 측정용 목적지를 한 번 지정했습니다.");
                }
                if (cam != null && moving != null)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, travel);
                    cam.transform.position = moving.transform.position + travel * 3.2f + side * 2.4f + Vector3.up * 1.9f;
                    cam.transform.LookAt(moving.transform.position + Vector3.up * .9f);
                    if (Now >= nextShot && shots < 4)
                    {
                        nextShot = Now + 4.5f;
                        shots++;
                        yield return ShotClean($"npc_1_walk_{shots}_clean.png", $"{moving.DisplayName} 걷기 · 카메라는 진행 방향 앞·옆(얼굴이 보여야 정상)");
                    }
                }
            }
        }
        finally { ReleaseCamera(hold); }
        for (int i = 0; i < companions.Length; i++)
        {
            var a = angles[i];
            Note(a.Count == 0
                ? $"facing {companions[i].name}: 이동 표본 없음"
                : $"facing {companions[i].name}: samples={a.Count} mean={a.Average():F0}° max={a.Max():F0}° backward(>120°)={a.Count(x => x > 120f)}");
        }
    }

    IEnumerator NpcTalk()
    {
        Transform player = PlayerTransform();
        if (player == null) yield break;
        player.GetComponent<EquipmentSystem>()?.Holster();
        var npcs = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None).OrderBy(c => c.name)
            .Select(c => c.GetComponent<NpcDialogue>()).Where(d => d != null).ToArray();
        if (npcs.Length == 0) { Error("말을 걸 동행이 없습니다."); yield break; }
        foreach (var npc in npcs)
        {
            string id = npc.name.Replace("Companion_", "");
            yield return TalkShots(npc, player, "morning_" + id, 10.5f, 3);
            yield return TalkShots(npc, player, "night_" + id, 21.5f, 1);
        }
    }

    IEnumerator PlanMotion()
    {
        yield return Step("title-continue", TitleReady(true), 50f);
        if (_secondary == null || !_secondary.interactable) { Error("'이어하기'를 쓸 수 없어 motion 촬영을 중단합니다."); yield break; }
        _secondary.onClick.Invoke();
        yield return Step("island-continue", WaitIslandAfterContinue(), 180f);
        if (!WorldPlayable()) yield break;
        yield return Step("prepare", SettlementPrepare(), 30f);
        Transform player = PlayerTransform();
        if (player == null) yield break;
        SetHour(10.5f);
        player.GetComponent<EquipmentSystem>()?.Holster();
        var grid = FindFirstObjectByType<WorldGridService>();
        if (!TryOpenGround(grid, player, out Vector3 start, out Vector3 forward))
        { Error("대본 경로(앞 21m·옆 7m)가 트인 평지를 찾지 못했습니다."); yield break; }
        Note($"motion start={start.ToString("F1")} forward={forward.ToString("F2")}");
        yield return Step("motion-game", MotionPass("A_game", player, start, forward, false), 180f);
        yield return Step("motion-side", MotionPass("B_side", player, start, forward, true), 180f);
        PlayerInputHandler.QaMove = null;
        PlayerInputHandler.QaRun = false;
    }

    // 플레이어 주변 링 × 16방향에서 대본 경로가 모두 걸을 수 있는 땅이고 장애물이 없는 출발점·정면을 고른다.
    // 1차는 풀이 아닌 모래·흙 바닥만(긴 풀이 다리를 가리지 않게), 없으면 아무 땅.
    static bool TryOpenGround(WorldGridService grid, Transform player, out Vector3 start, out Vector3 forward)
    {
        start = player.position; forward = Flat(player.forward).normalized;
        if (grid == null) return false;
        foreach (bool bare in new[] { true, false })
        foreach (float radius in new[] { 0f, 8f, 14f, 20f, 26f, 34f, 42f, 52f, 64f })
            for (int a = 0; a < (radius == 0f ? 1 : 16); a++)
            {
                Vector3 origin = player.position + Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward * radius;
                for (int y = 0; y < 16; y++)
                {
                    Vector3 f = Quaternion.Euler(0f, y * 22.5f, 0f) * Vector3.forward, r = Vector3.Cross(Vector3.up, f);
                    if (Clear(grid, origin, f, 21f, bare) && Clear(grid, origin + f * 11f, r, 7.5f, bare) &&
                        Clear(grid, origin - r * 4.5f, f, 21f, bare) && Clear(grid, origin + f * 11f, -r * .7071f - f * .7071f, 6.5f, bare))
                    {
                        if (!grid.WorldToCell(origin, out var cell) || !grid.CellToWorld(cell, out var centre)) continue;
                        start = new Vector3(origin.x, centre.y, origin.z); forward = f;
                        return true;
                    }
                }
            }
        return false;
    }

    // 경로의 모든 1m 지점이 같은 높이의 걸을 수 있는 땅이고, 무릎~가슴 높이에 콜라이더가 없는지.
    static bool Clear(WorldGridService grid, Vector3 from, Vector3 dir, float length, bool bare = false)
    {
        float baseY = float.NaN;
        for (float d = 0f; d <= length; d += 1f)
        {
            Vector3 p = from + dir * d;
            if (!grid.WorldToCell(p, out var c) || !grid.TryGetCell(c, out var cell) || !cell.IsWalkable || cell.HasWater) return false;
            if (bare && cell.GroundType == WorldGroundType.Grass) return false;
            if (!grid.CellToWorld(c, out var centre)) return false;
            if (float.IsNaN(baseY)) baseY = centre.y; else if (Mathf.Abs(centre.y - baseY) > .3f) return false;
        }
        Vector3 o = new Vector3(from.x, (float.IsNaN(baseY) ? from.y : baseY) + .5f, from.z);
        foreach (var hit in Physics.SphereCastAll(o, .45f, dir, length, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider != null && Inventory.instance != null && !hit.collider.transform.IsChildOf(Inventory.instance.transform)) return false;
        return true;
    }

    IEnumerator MotionPass(string tag, Transform player, Vector3 start, Vector3 forward, bool chase)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Teleport(player, start, forward);
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
        PlayerInputHandler.QaMove = Vector2.zero;
        yield return Wait(1f);
        string dir = Path.Combine(OutputDirectory, "motion_" + tag);
        Directory.CreateDirectory(dir);
        Camera cam = Camera.main;
        CameraHold hold = chase && cam != null ? HoldCamera(cam) : null;
        var animator = player.GetComponentInChildren<Animator>();
        Transform[] feet = animator != null && animator.isHuman
            ? new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) } : null;
        var rt = new RenderTexture(960, 540, 0);
        var read = new Texture2D(960, 540, TextureFormat.RGB24, false);
        int frame = 0;
        const int Fps = 30;
        Time.captureFramerate = Fps;
        // captureFramerate는 게임 시간을 프레임당 1/30초로 고정하지만 화면은 렌더 속도대로 돌아 약 1.8배 빨리 보인다.
        // 보는 사람이 실제 속도로 판단하도록 실시간보다 앞서면 그만큼 기다린다(기록 프레임·측정값은 같음).
        float realStart = Time.realtimeSinceStartup;
        var slips = new Dictionary<string, List<float>>();
        var signedSlips = new Dictionary<string, List<float>>();
        var footPrev = new Vector3[2];
        float restAnkle = float.MaxValue; // 대기 중 발 뼈의 지면 위 높이(지지 판정 기준)
        var pc = player.GetComponent<PlayerController>();
        try
        {
            foreach (var (label, f, r, run, seconds) in MotionScript)
            {
                Vector3 world = forward * f + right * r;
                PlayerInputHandler.QaRun = run;
                int frames = Mathf.RoundToInt(seconds * Fps);
                for (int i = 0; i < frames; i++)
                {
                    if (cam != null)
                    {
                        // PlayerController.ComputeMoveDirection과 같은 카메라 투영으로 월드 방향을 입력으로 되돌린다.
                        Vector3 cf = Flat(cam.transform.forward).normalized, cr = Flat(cam.transform.right).normalized;
                        PlayerInputHandler.QaMove = world.sqrMagnitude < .01f ? Vector2.zero : new Vector2(Vector3.Dot(world, cr), Vector3.Dot(world, cf));
                    }
                    yield return null; // 이번 프레임 Update(이동) 이후
                    if (chase && cam != null)
                    {
                        Vector3 look = player.position + Vector3.up * .85f;
                        Vector3 eye = look + right * 4.6f + Vector3.up * .9f;
                        cam.transform.position = Vector3.Lerp(cam.transform.position, eye, frame == 0 ? 1f : .25f);
                        cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position);
                        cam.fieldOfView = 34f;
                    }
                    yield return new WaitForEndOfFrame();
                    var shot = ScreenCapture.CaptureScreenshotAsTexture();
                    Graphics.Blit(shot, rt);
                    Destroy(shot);
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                    read.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, frame.ToString("0000") + ".jpg"), read.EncodeToJPG(88));
                    if (feet != null && feet[0] != null && feet[1] != null)
                    {
                        float ground = player.position.y;
                        int low = feet[0].position.y <= feet[1].position.y ? 0 : 1;
                        float height = feet[low].position.y - ground;
                        if (label == "idle") restAnkle = Mathf.Min(restAnkle, height);
                        else if (frame > 0 && height < restAnkle + .025f)
                        {
                            Vector3 footDelta = Flat(feet[low].position - footPrev[low]) * Fps;
                            if (!slips.TryGetValue(label, out var list)) slips[label] = list = new List<float>();
                            list.Add(footDelta.magnitude);
                            // 몸 진행 방향 성분(+는 지지발이 앞으로 끌림 = 재생이 느림, -는 뒤로 밀림 = 재생이 빠름).
                            if (!signedSlips.TryGetValue(label, out var signedList)) signedSlips[label] = signedList = new List<float>();
                            signedList.Add(Vector3.Dot(footDelta, Flat(player.forward).normalized));
                        }
                        footPrev[0] = feet[0].position; footPrev[1] = feet[1].position;
                    }
                    frame++;
                    float ahead = realStart + frame / (float)Fps - Time.realtimeSinceStartup;
                    if (ahead > 0f) System.Threading.Thread.Sleep(Mathf.CeilToInt(ahead * 1000f));
                }
                Note($"motion {tag} {label}: speed={(pc != null ? pc.ActualPlanarSpeed : 0f):F2}m/s pos={player.position.ToString("F1")}");
            }
        }
        finally
        {
            Time.captureFramerate = 0;
            PlayerInputHandler.QaMove = Vector2.zero;
            PlayerInputHandler.QaRun = false;
            if (hold != null) ReleaseCamera(hold);
            rt.Release();
            Destroy(read);
        }
        Note($"motion {tag} restAnkle={restAnkle:F3}m");
        foreach (var pair in slips)
        {
            var list = pair.Value; list.Sort();
            float signedMedian = 0f;
            if (signedSlips.TryGetValue(pair.Key, out var signed) && signed.Count > 0) { signed.Sort(); signedMedian = signed[signed.Count / 2]; }
            Note($"motion {tag} footslip {pair.Key}: n={list.Count} median={list[list.Count / 2]:F2} p75={list[list.Count * 3 / 4]:F2} signedMedian={signedMedian:+0.00;-0.00}m/s");
        }
        Note($"motion {tag}: {frame} frames @ {Fps}fps → {dir}");
    }

    // ───────────── title helpers ─────────────
    IEnumerator TitleReady(bool requireContinue)
    {
        _primary = _secondary = null;
        float end = Now + 45f;
        while (Now < end)
        {
            var scenario = PlayableDayScenarioController.Instance;
            Button primary = FindFlowButton("PrimaryButton"), secondary = FindFlowButton("SecondaryButton");
            bool ready = scenario != null && scenario.TitleMenuOpen && primary != null && primary.interactable;
            if (ready && !requireContinue) { _primary = primary; _secondary = secondary; yield break; }
            if (ready && secondary != null)
            {
                string label = ButtonLabel(secondary);
                if (secondary.interactable && label == "이어하기") { _primary = primary; _secondary = secondary; yield break; }
                if (label == "저장 없음" || label == "호환 불가")
                { Error($"이어하기 불가('{label}'): -pa-save-root에 상점이 지어진 savegame-v17.json 복사본이 필요합니다."); yield break; }
            }
            yield return null;
        }
        Error(requireContinue ? "타이틀 '이어하기'가 준비되지 않았습니다." : "타이틀 '새 게임'이 준비되지 않았습니다.");
    }

    static Button FindFlowButton(string name)
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (button != null && button.name == name && button.isActiveAndEnabled) return button;
        return null;
    }

    static string ButtonLabel(Button button)
    {
        if (button == null) return string.Empty;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        return label != null ? label.text : string.Empty;
    }

    // ───────────── world helpers ─────────────
    static bool WorldPlayable()
    {
        if (SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) return false;
        foreach (var route in FindObjectsByType<DemoRouteController>(FindObjectsSortMode.None))
            if (route != null && route.IsPlayable && route.gameObject.scene.name == DemoRouteController.WorldScene) return true;
        return false;
    }

    static Transform PlayerTransform()
    {
        if (Inventory.instance != null) return Inventory.instance.transform;
        var adapter = WorldGameplayAdapterService.Instance;
        if (adapter != null && adapter.PlayerRoot != null) return adapter.PlayerRoot.transform;
        return null;
    }

    static void SetHour(float hour)
    {
        GameClock clock = GameClock.Instance;
        if (clock != null) clock.ForceSet(hour, Mathf.Max(1, clock.CurrentDay), ClockReason);
    }

    static ShopFrame LocateShop(Transform player)
    {
        var frame = new ShopFrame();
        DemoSettlementController settlement = DemoSettlementController.Instance;
        Shop shop = settlement != null ? settlement.OperatingShop : null;
        DemoShopInterior interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior == null) interior = FindFirstObjectByType<DemoShopInterior>();
        frame.interior = interior;
        GameObject visual = null;
        if (interior != null)
        {
            frame.root = interior.transform;
            visual = interior.exterior != null && interior.exterior.activeInHierarchy ? interior.exterior : interior.gameObject;
            frame.how = "DemoShopInterior";
        }
        else if (shop != null)
        {
            DemoPlacedObject placed = shop.GetComponentInParent<DemoPlacedObject>(true);
            frame.root = placed != null ? placed.transform : shop.transform;
            visual = frame.root.gameObject;
            frame.how = "OperatingShop";
        }
        if (visual == null || !TryRendererBounds(visual, out frame.bounds)) { frame.how = "상점 없음"; return frame; }
        frame.found = true;
        bool hasDoor = false;
        if (interior != null && interior.outsideSpawn != null) { frame.door = interior.outsideSpawn.position; hasDoor = true; }
        else if (interior != null && interior.entrance != null) { frame.door = interior.entrance.transform.position; hasDoor = true; }
        else if (settlement != null && settlement.TryGetShopEntrance(out Vector2Int cell))
        {
            WorldGridService grid = FindFirstObjectByType<WorldGridService>();
            if (grid != null && grid.CellToWorld(cell, out Vector3 world)) { frame.door = world; hasDoor = true; }
        }
        Vector3 facing = hasDoor ? Flat(frame.door - frame.bounds.center) : Vector3.zero;
        if (facing.sqrMagnitude < .01f && player != null) facing = Flat(player.position - frame.bounds.center);
        frame.facade = facing.sqrMagnitude > .01f ? facing.normalized : Vector3.back;
        if (!hasDoor) frame.door = frame.bounds.center + frame.facade * FacadeExtent(frame.bounds, frame.facade);
        return frame;
    }

    static bool TryRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
        {
            if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer ||
                renderer is TrailRenderer || renderer is LineRenderer) continue;
            Bounds b = renderer.bounds;
            if (b.size.sqrMagnitude < 1e-4f || b.size.magnitude > 80f) continue; // 지면 장식·거대 오브젝트 제외
            if (!any) { bounds = b; any = true; } else bounds.Encapsulate(b);
        }
        return any;
    }

    // ───────────── camera framing ─────────────
    static CameraHold HoldCamera(Camera cam)
    {
        var hold = new CameraHold { cam = cam, position = cam.transform.position, rotation = cam.transform.rotation, fov = cam.fieldOfView };
        hold.follow = cam.GetComponent<CameraController>();
        if (hold.follow != null) { hold.followEnabled = hold.follow.enabled; hold.follow.enabled = false; }
        return hold;
    }

    static void ReleaseCamera(CameraHold hold)
    {
        if (hold == null || hold.cam == null) return;
        hold.cam.transform.SetPositionAndRotation(hold.position, hold.rotation);
        hold.cam.fieldOfView = hold.fov;
        if (hold.follow != null)
        {
            hold.follow.enabled = hold.followEnabled;
            if (hold.followEnabled) hold.follow.SnapToTarget();
        }
    }

    // 높은 3/4 전경: 상점 정면 + 플레이어 + 가까운 동행 주민이 모두 들어가는 최소 거리.
    string PlaceEstablishing(Camera cam, ShopFrame shop, Transform player)
    {
        var points = new List<Vector3>();
        Vector3 anchor;
        if (shop.found) { AddCorners(points, shop.bounds); anchor = shop.bounds.center; }
        else
        {
            anchor = player != null ? player.position : cam.transform.position + cam.transform.forward * 10f;
            AddCorners(points, new Bounds(anchor + Vector3.up, new Vector3(6f, 2.5f, 6f)));
        }
        int subjects = 0;
        if (player != null && Flat(player.position - anchor).magnitude < 35f)
        { points.Add(player.position); points.Add(player.position + Vector3.up * 1.9f); subjects++; }
        var settlement = DemoSettlementController.Instance;
        if (settlement != null && settlement.Residents != null)
            foreach (var resident in settlement.Residents)
            {
                if (resident == null || !resident.gameObject.activeInHierarchy) continue;
                Vector3 p = resident.transform.position;
                if (Flat(p - anchor).magnitude > 30f) continue;
                points.Add(p); points.Add(p + Vector3.up * 1.9f); subjects++;
            }
        var all = new Bounds(points[0], Vector3.zero);
        foreach (var p in points) all.Encapsulate(p);
        Vector3 facade = shop.found ? shop.facade : PlayerFacing(player);
        string result = Frame(cam, all.center, facade, points, new[] { 35f, -35f, 55f, -55f, 15f }, 33f, 35f, .82f, 10f, 140f, 2.5f, shop.root, player);
        return $"establishing · {(shop.found ? shop.how : "상점 없음→플레이어")} · subjects={subjects} · {result}";
    }

    // 낮은 각도 히어로: 상점 입구 쪽 정면을 크게.
    string PlaceHero(Camera cam, ShopFrame shop, Transform player)
    {
        Bounds b = shop.found ? shop.bounds
            : new Bounds((player != null ? player.position : cam.transform.position + cam.transform.forward * 6f) + Vector3.up, new Vector3(3f, 2.2f, 3f));
        Vector3 facade = shop.found ? shop.facade : PlayerFacing(player);
        var points = new List<Vector3>();
        AddCorners(points, b);
        Vector3 look = new Vector3(b.center.x, b.min.y + b.size.y * .42f, b.center.z) + facade * (FacadeExtent(b, facade) * .35f);
        string result = Frame(cam, look, facade, points, new[] { 22f, -22f, 35f, -35f, 0f }, 8f, 32f, .9f, 4f, 90f, 1.5f, shop.root, player);
        return $"hero · {(shop.found ? shop.how : "상점 없음→플레이어")} · {result}";
    }

    string Frame(Camera cam, Vector3 look, Vector3 facade, List<Vector3> points, float[] yaws, float pitch, float fov,
        float margin, float minDistance, float maxDistance, float groundClearance, Transform subjectRoot, Transform player)
    {
        Vector3 bestPosition = cam.transform.position;
        Quaternion bestRotation = cam.transform.rotation;
        float bestYaw = 0f;
        bool clear = false;
        for (int i = 0; i < yaws.Length && !clear; i++)
        {
            Vector3 flat = Quaternion.AngleAxis(yaws[i], Vector3.up) * facade;
            float p = pitch * Mathf.Deg2Rad;
            Vector3 toCamera = (flat * Mathf.Cos(p) + Vector3.up * Mathf.Sin(p)).normalized;
            Vector3 position = FitDistance(look, toCamera, points, fov, CleanAspect, margin, minDistance, maxDistance);
            position = KeepAboveGround(position, groundClearance);
            Quaternion rotation = Quaternion.LookRotation(look - position, Vector3.up);
            bool unobstructed = Unobstructed(position, look, subjectRoot, player);
            if (i == 0 || unobstructed) { bestPosition = position; bestRotation = rotation; bestYaw = yaws[i]; }
            clear = unobstructed;
        }
        cam.fieldOfView = fov;
        cam.transform.SetPositionAndRotation(bestPosition, bestRotation);
        return $"yaw={bestYaw:F0} pitch={pitch:F0} fov={fov:F0} dist={Vector3.Distance(bestPosition, look):F1} clear={clear}";
    }

    // 모든 점이 화면 안(margin)에 들어오는 최소 거리를 이분 탐색한다(방향 고정이라 회전도 고정).
    static Vector3 FitDistance(Vector3 look, Vector3 toCamera, List<Vector3> points, float fov, float aspect, float margin, float min, float max)
    {
        Quaternion inverse = Quaternion.Inverse(Quaternion.LookRotation(-toCamera, Vector3.up));
        float tanV = Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * margin, tanH = tanV * aspect;
        bool Fits(float distance)
        {
            Vector3 eye = look + toCamera * distance;
            foreach (var point in points)
            {
                Vector3 v = inverse * (point - eye);
                if (v.z < .3f || Mathf.Abs(v.x) > v.z * tanH || Mathf.Abs(v.y) > v.z * tanV) return false;
            }
            return true;
        }
        if (!Fits(max)) return look + toCamera * max;
        float lo = min, hi = max;
        if (Fits(lo)) return look + toCamera * lo;
        for (int i = 0; i < 24; i++)
        {
            float mid = (lo + hi) * .5f;
            if (Fits(mid)) hi = mid; else lo = mid;
        }
        return look + toCamera * hi;
    }

    static Vector3 KeepAboveGround(Vector3 position, float clearance)
    {
        if (Physics.Raycast(position + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore) &&
            hit.point.y + clearance > position.y)
            position.y = hit.point.y + clearance;
        return position;
    }

    static bool Unobstructed(Vector3 from, Vector3 to, Transform subjectRoot, Transform player)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < .01f) return true;
        foreach (var hit in Physics.RaycastAll(from, delta / length, length * .85f, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform t = hit.collider != null ? hit.collider.transform : null;
            if (t == null) continue;
            if (subjectRoot != null && t.IsChildOf(subjectRoot)) continue;
            if (player != null && t.IsChildOf(player)) continue;
            if (t.GetComponentInParent<DemoResident>() != null) continue;
            return false;
        }
        return true;
    }

    static Vector3 PlayerFacing(Transform player)
    {
        Vector3 forward = player != null ? Flat(player.forward) : Vector3.zero;
        return forward.sqrMagnitude > .01f ? forward.normalized : Vector3.back;
    }

    static void AddCorners(List<Vector3> points, Bounds b)
    {
        Vector3 min = b.min, max = b.max;
        for (int i = 0; i < 8; i++)
            points.Add(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
    }

    static float FacadeExtent(Bounds b, Vector3 facade) => Mathf.Abs(facade.x) * b.extents.x + Mathf.Abs(facade.z) * b.extents.z;
    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ───────────── capture ─────────────
    // (a) UI 포함: 프레임 끝에서 화면 그대로 superSize 배율(1920x1080 → 3840x2160).
    IEnumerator ShotUI(string file, string notes, bool defer = false)
    {
        yield return new WaitForEndOfFrame();
        ShotRecord record = NewRecord(file, "ui", !ExhibitionPhotoMode.HudHidden, notes);
        Texture2D shot = null;
        try
        {
            shot = ScreenCapture.CaptureScreenshotAsTexture(_superSize);
            if (shot == null) throw new InvalidOperationException("ScreenCapture returned no texture");
            if (_superSize > 1 && LooksBlack(shot))
            {
                // 일부 그래픽 경로에서 슈퍼샘플 결과가 비는 경우 같은 프레임 백버퍼를 1배로 다시 읽는다.
                Destroy(shot);
                shot = ScreenCapture.CaptureScreenshotAsTexture(1);
                if (shot == null) throw new InvalidOperationException("ScreenCapture(1) returned no texture");
                record.notes += " | superSize 결과가 검정이라 1배로 재촬영";
            }
            record.width = shot.width; record.height = shot.height;
            if (shot.width != Screen.width * _superSize)
                record.notes += $" | superSize {_superSize} 미적용 가능(화면 {Screen.width}x{Screen.height})";
            if (defer) { _pending.Add(new PendingShot { texture = shot, record = record }); shot = null; }
            else SavePng(shot, record);
        }
        catch (Exception e) { ShotError(record, e); }
        finally { if (shot != null) Destroy(shot); }
    }

    // (b) UI 없음: 화면 공간 Canvas를 끄고 메인 카메라를 3840x2160 RT(24bit depth, MSAA)로 일반 URP 경로에서 그린 뒤 읽는다.
    // Camera.Render 직접 호출은 쓰지 않는다(QA FrameSequenceCapture의 2026-07-17 크래시 기록).
    IEnumerator ShotClean(string file, string notes, bool defer = false)
    {
        Camera cam = Camera.main;
        if (cam == null) { ShotError(NewRecord(file, "clean", false, notes), new InvalidOperationException("Camera.main 없음")); yield break; }
        RenderTexture target = CreateCleanTarget(out int msaa);
        if (target == null) { ShotError(NewRecord(file, "clean", false, notes), new InvalidOperationException("RenderTexture 생성 실패")); yield break; }
        RenderTexture previous = cam.targetTexture;
        List<Canvas> hidden = ExhibitionPhotoMode.HideScreenSpaceCanvases();
        cam.targetTexture = target;
        try
        {
            yield return null; // 새 대상 크기로 한 프레임 전체를 그린다
            yield return new WaitForEndOfFrame();
            ShotRecord record = NewRecord(file, "clean", false, notes + $" | msaa {msaa}");
            ReadAndSave(target, record, defer);
        }
        finally
        {
            if (cam != null) { cam.targetTexture = previous; cam.ResetAspect(); }
            ExhibitionPhotoMode.RestoreCanvases(hidden);
            if (RenderTexture.active == target) RenderTexture.active = null;
            target.Release();
            Destroy(target);
        }
    }

    RenderTexture CreateCleanTarget(out int msaa)
    {
        foreach (int samples in new[] { 8, 4, 2, 1 })
        {
            if (samples > _maxMsaa) continue;
            var rt = new RenderTexture(_cleanWidth, _cleanHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            { antiAliasing = samples, name = "PA_ExhibitionClean" };
            if (rt.Create()) { msaa = samples; return rt; }
            Destroy(rt);
        }
        msaa = 0;
        return null;
    }

    void ReadAndSave(RenderTexture source, ShotRecord record, bool defer = false)
    {
        RenderTexture previous = RenderTexture.active, resolved = null;
        Texture2D texture = null;
        try
        {
            resolved = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, resolved); // MSAA resolve
            RenderTexture.active = resolved;
            texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            texture.Apply(false);
            record.width = texture.width; record.height = texture.height;
            RenderTexture.active = previous;
            if (LooksBlack(texture)) Error(record.file + " · CLEAN 결과가 거의 검정입니다(렌더 경로 확인, -pa-capture-msaa 1 시도).");
            if (defer) { _pending.Add(new PendingShot { texture = texture, record = record }); texture = null; }
            else SavePng(texture, record);
        }
        catch (Exception e) { ShotError(record, e); }
        finally
        {
            RenderTexture.active = previous;
            if (resolved != null) RenderTexture.ReleaseTemporary(resolved);
            if (texture != null) Destroy(texture);
        }
    }

    // 8x5 격자 표본이 모두 거의 검정이면 빈 프레임으로 본다(페이드 중 프레임도 여기에 걸릴 수 있다).
    static bool LooksBlack(Texture2D texture)
    {
        if (texture == null || texture.width < 8 || texture.height < 5) return false;
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 8; x++)
            {
                Color c = texture.GetPixel((x * 2 + 1) * texture.width / 16, (y * 2 + 1) * texture.height / 10);
                if (c.r > .02f || c.g > .02f || c.b > .02f) return false;
            }
        return true;
    }

    void SavePng(Texture2D texture, ShotRecord record)
    {
        string path = Path.Combine(OutputDirectory, record.file);
        File.WriteAllBytes(path, ExhibitionPhotoMode.EncodeOpaquePng(texture));
        Debug.Log($"PA_EXHIBITION_CAPTURE shot={record.file} {record.width}x{record.height} scene={record.scene} hour={record.gameHour:F2}");
        WriteManifest();
    }

    void FlushPending()
    {
        foreach (var pending in _pending)
        {
            try { SavePng(pending.texture, pending.record); }
            catch (Exception e) { ShotError(pending.record, e); }
            finally { if (pending.texture != null) Destroy(pending.texture); }
        }
        _pending.Clear();
    }

    ShotRecord NewRecord(string file, string kind, bool hud, string notes)
    {
        var record = new ShotRecord
        {
            file = file, kind = kind, plan = Plan, scene = SceneManager.GetActiveScene().name,
            hudVisible = hud, notes = notes ?? string.Empty, realtime = Now - _startedAt
        };
        GameClock clock = GameClock.Instance;
        if (clock != null) { record.gameHour = clock.CurrentHour; record.gameDay = clock.CurrentDay; }
        Camera cam = Camera.main;
        if (cam != null)
        {
            record.camera = cam.name;
            record.cameraPosition = cam.transform.position;
            record.cameraRotation = cam.transform.eulerAngles;
            record.fov = cam.fieldOfView;
        }
        _manifest.shots.Add(record);
        return record;
    }

    void ShotError(ShotRecord record, Exception e)
    {
        record.error = e.GetType().Name + ": " + e.Message;
        Error(record.file + " · " + record.error);
    }

    // ───────────── step runner ─────────────
    // 중첩 IEnumerator를 직접 펼쳐 실행해 단계별 예외·시간 초과를 잡는다. 중단 시 Dispose로 finally(카메라/Canvas 복원)를 실행한다.
    IEnumerator Step(string name, IEnumerator body, float timeout)
    {
        float end = Now + timeout;
        var stack = new Stack<IEnumerator>();
        stack.Push(body);
        Note($"▶ {name} t={Now - _startedAt:F1}s");
        try
        {
            while (stack.Count > 0)
            {
                if (Now > end) { Error($"{name}: 시간 초과 {timeout:F0}s"); yield break; }
                IEnumerator top = stack.Peek();
                bool moved = false, failed = false;
                object current = null;
                try
                {
                    moved = top.MoveNext();
                    if (moved) current = top.Current;
                }
                catch (Exception e)
                {
                    failed = true;
                    Error($"{name}: {e.GetType().Name}: {e.Message}");
                    Debug.LogException(e);
                }
                if (failed) yield break;
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
        }
        finally
        {
            while (stack.Count > 0)
            {
                IEnumerator pending = stack.Pop();
                try { (pending as IDisposable)?.Dispose(); } catch (Exception) { }
            }
        }
    }

    static IEnumerator Wait(float seconds)
    {
        float end = Now + seconds;
        while (Now < end) yield return null;
    }

    static IEnumerator Until(Func<bool> condition, float timeout)
    {
        float end = Now + timeout;
        while (Now < end && !condition()) yield return null;
    }

    // ───────────── ArrivalIntroPresentation (있을 때만, 리플렉션) ─────────────
    static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = null;
            try { type = assembly.GetType(name, false); } catch (Exception) { }
            if (type != null) return type;
        }
        return null;
    }

    static bool ReadStatic(Type type, string member, out object value)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        value = null;
        try
        {
            PropertyInfo property = type.GetProperty(member, flags);
            if (property != null && property.GetIndexParameters().Length == 0) { value = property.GetValue(null); return true; }
            FieldInfo field = type.GetField(member, flags);
            if (field != null) { value = field.GetValue(null); return true; }
        }
        catch (Exception) { }
        return false;
    }

    static bool IntroPlaying(Type type) => type != null && ReadStatic(type, "IsPlaying", out object v) && v is bool playing && playing;
    static float IntroTime(Type type, float seenAt) => ReadStatic(type, "Elapsed", out object v) && v is float elapsed ? elapsed : Now - seenAt;

    static float IntroDuration(Type type)
    {
        foreach (string name in new[] { "Duration", "TotalSeconds", "Seconds", "Length" })
            if (ReadStatic(type, name, out object v) && v is float seconds && seconds > .5f) return seconds;
        try
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(type)) return -1f;
            UnityEngine.Object[] found = FindObjectsByType(type, FindObjectsSortMode.None);
            if (found == null || found.Length == 0) return -1f;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo field in type.GetFields(flags))
                if (field.FieldType == typeof(float) && LooksLikeDuration(field.Name) && field.GetValue(found[0]) is float f && f > .5f) return f;
            foreach (PropertyInfo property in type.GetProperties(flags))
                if (property.PropertyType == typeof(float) && property.CanRead && property.GetIndexParameters().Length == 0 &&
                    LooksLikeDuration(property.Name) && property.GetValue(found[0]) is float f && f > .5f) return f;
        }
        catch (Exception) { }
        return -1f;
    }

    static bool LooksLikeDuration(string name) =>
        name.IndexOf("duration", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("seconds", StringComparison.OrdinalIgnoreCase) >= 0;

    // ───────────── manifest / args ─────────────
    void Note(string message)
    {
        _manifest.notes.Add(message);
        Debug.Log("[ExhibitionCapture] " + message);
    }

    void Error(string message)
    {
        _manifest.errors.Add($"t={Now - _startedAt:F1}s {message}");
        Debug.LogWarning("[ExhibitionCapture] ERROR " + message);
        WriteManifest();
    }

    void WriteManifest()
    {
        if (string.IsNullOrEmpty(OutputDirectory)) return;
        try
        {
            _manifest.seconds = Now - _startedAt;
            File.WriteAllText(Path.Combine(OutputDirectory, ManifestName), JsonUtility.ToJson(_manifest, true));
        }
        catch (Exception e) { Debug.LogWarning("[ExhibitionCapture] manifest 저장 실패: " + e.Message); }
    }

    void Finish()
    {
        if (Finished) return;
        Finished = true;
        try { FlushPending(); } catch (Exception e) { Error("보류 컷 저장 실패: " + e.Message); }
        int saved = _manifest.shots.Count(s => string.IsNullOrEmpty(s.error));
        _manifest.result = _manifest.errors.Count == 0 ? "ok" : saved > 0 ? "partial" : "failed";
        _manifest.finishedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        WriteManifest();
        Debug.Log($"PA_EXHIBITION_CAPTURE result={_manifest.result} plan={Plan} shots={saved} errors={_manifest.errors.Count} " +
                  $"seconds={_manifest.seconds:F1} manifest={Path.Combine(OutputDirectory ?? string.Empty, ManifestName)}");
        Application.Quit();
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
            return i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : string.Empty;
        }
        return null;
    }

    static int IntArg(string name, int fallback) =>
        int.TryParse(Arg(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

    static float FloatArg(string name, float fallback) =>
        float.TryParse(Arg(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
}
