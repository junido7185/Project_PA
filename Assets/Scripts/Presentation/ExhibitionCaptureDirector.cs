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
        else Error("알 수 없는 -pa-capture-plan: " + Plan + " (story|settlement|feedback|motion|crafting|npc|tools|decor|beach|dig|ui|market|gift|night)");
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
            // 포스터용: 레터박스·타이틀·자막 없이 같은 장면(다음 프레임)을 한 장 더. 인코딩은 뒤로 미룬다.
            if (voyage != null && voyage.Sailing)
                yield return ShotClean($"story_{4 + i:00}_voyage_{i + 1}_clean.png",
                    $"voyage Elapsed={voyage.Elapsed:F2}s · UI 없음", true);
        }
        FlushPending();
    }

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

    IEnumerator WaitIslandAfterContinue()
    {
        float end = Now + 60f;
        while (Now < end && SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) yield return null;
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
        int shells = Mathf.Clamp(IntArg("-pa-decor-shells", 0), 0, 3);
        var shellDecor = Res("Items/Item_ShellDecor");
        foreach (var (item, count) in new[] { (chair, Mathf.Max(1, chairs)), (Res("Items/Item_Fish"), 4), (shellDecor, Mathf.Max(1, shells)) })
            if (item == null || !inventory.TryReceiveToHotbar(item, count) && !inventory.AddInstance(new ItemInstance(item, count))) Error("FIXTURE 지급 실패: " + item);
        inventory.RefreshAllUI();
        var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        if (interior == null || interior.entrance == null) { Error("상점 실내 없음"); yield break; }
        if (!interior.IsInside) interior.entrance.Interact(player.gameObject);
        yield return Until(() => interior.IsInside, 8f);
        yield return Wait(1.5f);
        Vector3 centre = player.position;
        for (int i = 0; i < chairs; i++) yield return PlaceKit(player, chair, centre, new[] { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f });
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
        int before = Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish);
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
        int after = Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish);
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(), 180f);
            yield return Until(() => BeachFinds.Instance != null && BeachFinds.Instance.Day > 0, 10f);
            yield return Wait(1.5f);
            beach = BeachFinds.Instance;
            inventory = Inventory.instance;
            int remainingAfter = beach != null ? beach.Finds.Count(f => f != null) : -1;
            Note($"save → continue same day: remaining {remainingBefore}→{remainingAfter} collected={beach?.CollectedToday} inventory={Count(PA_BeachIds.Scallop) + Count(PA_BeachIds.Conch) + Count(PA_BeachIds.Starfish)}");
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
        Note($"RESULT picked={picked} scallop={Count(PA_BeachIds.Scallop)} conch={Count(PA_BeachIds.Conch)} starfish={Count(PA_BeachIds.Starfish)} necklace={Count(PA_BeachIds.Necklace)}");
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
        int before = Count(DigIds.Clay) + Count(DigIds.OldCoin) + Count(DigIds.Fossil), dug = 0;
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
        int after = Count(DigIds.Clay) + Count(DigIds.OldCoin) + Count(DigIds.Fossil);
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(), 180f);
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
        if (Physics.Raycast(spot + Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) spot = hit.point;
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
            ("Items/Item_OldCoin", 1), ("Items/Item_FossilShard", 1), ("Items/Item_Fish", 3), ("Items/Item_Wood", 12), ("Items/Item_ShellNecklace", 1), ("Items/Item_FiredJar", 1), ("Items/Item_Shovel", 1) })
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
            slot.tooltip.Hide();
        }
        else Note("툴팁 대상 칸 없음");
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
            Note($"collection found={DiscoveryLog.FoundCount}/{DiscoveryLog.Total} completions={DiscoveryLog.Completions}");
            yield return ShotUI("ui_3b_phone_collection_ui.png", "휴대폰 도감");
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
        var goods = new[] { ("Items/Item_ShellNecklace", 2), ("Items/Item_FiredJar", 2), ("Items/Item_FossilShard", 1), ("Items/Item_OldCoin", 2), ("Items/Item_ConchShell", 3), ("Items/Item_ScallopShell", 5) };
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
        while (Now < end && loop.IsShopOpenForCustomers) yield return null;
        var sales = SalesLogManager.Instance != null ? SalesLogManager.Instance.GetRecent(200) : new List<SaleRecord>();
        var tonight = sales.Take(Mathf.Max(0, sales.Count - before)).ToList();
        Note("sales: " + string.Join(" | ", tonight.Select(r => $"{r.itemName} {r.price}G → {r.buyerName}")));
        Note($"RESULT sold={tonight.Count} revenue={tonight.Sum(r => r.price)} kinds={string.Join(",", tonight.GroupBy(r => r.itemName).Select(x => x.Key + "×" + x.Count()))}");
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(DemoRouteController.WorldScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return Step("island-continue-2", WaitIslandAfterContinue(), 180f);
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
        yield return Step("npc-gather", NpcGather(), 90f);
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
        float end = Now + 70f;
        while (Now < end && routines.Any(r => !r.AtGatherSpot && r.Current != DemoCompanionRoutine.Activity.Working)) yield return null;
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
