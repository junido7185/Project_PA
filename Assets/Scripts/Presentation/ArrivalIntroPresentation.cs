using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 출항 항해 페이드에서 막 내린 새 게임에서만(DemoRouteController가 호출) 약 4.6초 상륙 연출을 재생한다.
// 레터박스 + "개척 섬" 타이틀 + 해변을 넓게 내려다보는 높은 구도에서 평소 플레이 카메라로 내려오는 이동.
// 끝나면(E / Space 길게 눌러 건너뛰기 포함) 조작·HUD·카메라 추적을 그대로 돌려주고, 보급 상자를 열 때까지
// 다음 목표 칩과 상자 위 표식을 띄운다. 이동/상호작용/보급/저장 권위는 바꾸지 않는다(표시 전용).
// Continue/저장 복원 경로에서는 호출되지 않는다. 캡처 도구는 IsPlaying/Elapsed를 리플렉션으로 읽는다.
public sealed class ArrivalIntroPresentation : MonoBehaviour
{
    public const float Duration = 4.6f;
    public const string ObjectiveText = "보급 상자를 열어 도구를 챙기세요";
    public static bool IsPlaying { get; private set; }
    public static float Elapsed { get; private set; }
    public static ArrivalIntroPresentation Current { get; private set; }
    public bool GuidanceActive => _guiding && _doneAt < 0f;

    static readonly Color ChipColor = new Color(.98f, .95f, .87f, .95f);
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static Material _markerMaterial;
    static Mesh _pinMesh, _ringMesh;
    static bool _markerShaderMissing;

    Camera _camera;
    CameraController _follow;
    PlayerController _movement;
    PlayerInteraction _interaction;
    PlayerInputHandler _input;
    bool _followWasEnabled, _movementWasEnabled, _interactionWasEnabled, _inputWasEnabled, _controlRestored = true;
    bool _playing, _guiding, _titleShown, _skipped;
    float _fov, _markerAlpha, _markerTarget, _chipAlpha, _doneAt = -1f;
    Vector3 _startPos, _bendPos, _endPos, _startLook, _endLook;
    Quaternion _endRot;
    VoyageCinematicOverlay _overlay;
    readonly List<Canvas> _hidden = new List<Canvas>();
    FirstDaySupplyBox _supply;
    Transform _marker, _ring;
    MeshRenderer _pinRenderer, _ringRenderer;
    MaterialPropertyBlock _block;
    RectTransform _chip;
    CanvasGroup _chipGroup;
    TextMeshProUGUI _chipTag, _chipText;
    Image _chipAccent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsPlaying = false;
        Elapsed = 0f;
        Current = null;
    }

    // 같은 프레임에 같은 페이드 색으로 덮고 시작하므로 호출자는 바로 전환 오버레이를 걷어도 된다.
    public static bool Play(Transform player)
    {
        if (player == null || Camera.main == null || IsPlaying) return false;
        if (Current != null) Destroy(Current.gameObject);
        var intro = new GameObject("PA_ArrivalIntro").AddComponent<ArrivalIntroPresentation>();
        intro.Begin(player);
        return true;
    }

    public static void ForceFinish()
    {
        if (Current != null && Current._playing) Current.Finish();
        else IsPlaying = false;
    }

    // 시작 도중 예외: 조작/카메라/HUD를 즉시 돌려주고 연출 객체를 지운다(안내 없음).
    public static void Abort()
    {
        if (Current != null)
        {
            Current.RestoreControl();
            Current._playing = false;
            Destroy(Current.gameObject);
        }
        IsPlaying = false;
    }

    void Begin(Transform player)
    {
        Current = this;
        _camera = Camera.main;
        _follow = _camera.GetComponent<CameraController>();
        _followWasEnabled = _follow != null && _follow.enabled;
        // 끝 구도 = 지금 막 맞춰 둔 평소 플레이 카메라. 여기부터는 무엇이 실패해도 RestoreControl이 되돌린다.
        _fov = _camera.fieldOfView;
        _endPos = _camera.transform.position;
        _endRot = _camera.transform.rotation;
        _controlRestored = false;
        if (_follow != null) _follow.enabled = false;
        _endLook = _endPos + _endRot * Vector3.forward * (_follow != null ? Mathf.Max(4f, _follow.OpeningDistance) : 14f);
        var world = FirstDayWorldPresentation.Instance;
        _supply = world != null ? world.Supply : null;
        // 시작 구도: 바다 쪽 높은 곳에서 항구·동행·보급 상자와 섬 안쪽을 함께 내려다본다.
        Vector3 focus = world != null ? world.Harbor + new Vector3(1f, 1f, 4f) : player.position + new Vector3(1f, 1f, 2f);
        _startPos = focus + new Vector3(-12f, 20f, -28f);
        _startLook = focus + new Vector3(3f, 0f, 14f);
        _bendPos = Vector3.Lerp(_startPos, _endPos, .45f) + new Vector3(-2.5f, 3f, -3f);

        _movement = player.GetComponent<PlayerController>();
        _interaction = player.GetComponent<PlayerInteraction>();
        _input = PlayerInputHandler.Instance;
        _movementWasEnabled = _movement != null && _movement.enabled;
        _interactionWasEnabled = _interaction != null && _interaction.enabled;
        _inputWasEnabled = _input != null && _input.enabled;
        if (_movement != null) { _movement.enabled = false; _movement.ResetMotionAfterTeleport(); }
        if (_interaction != null) _interaction.enabled = false;
        if (_input != null) _input.enabled = false;

        _overlay = VoyageCinematicOverlay.Create(transform, "PA_ArrivalCinematic", 140, "P.A. COMPANY  /  첫 상륙");
        _overlay.SetFade(1f);
        _overlay.SetLetterboxImmediate(1f);
        _playing = IsPlaying = true;
        Elapsed = 0f;
        HideHud();
        PoseCamera(0f);
        Debug.Log("[ARRIVAL-INTRO] start fresh voyage arrival");
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (_playing && Elapsed < Duration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            Elapsed = Mathf.Min(Duration, Elapsed + dt);
            if (!_skipped && _overlay.SkipRequested)
            {
                // 건너뛰기: 마지막 0.5초(카메라 착지·띠 걷힘)만 남긴다.
                _skipped = true;
                Elapsed = Mathf.Max(Elapsed, Duration - .5f);
                _overlay.LetterboxSeconds = .4f;
            }
            _overlay.SetFade(1f - VoyageCinematicOverlay.Ease(Elapsed / .8f));
            if (!_titleShown && !_skipped && Elapsed >= .35f && Elapsed < 3.1f)
            {
                _titleShown = true;
                _overlay.ShowTitle("ARRIVAL", "개척 섬", "Day 1 · 첫 상륙");
            }
            if (Elapsed >= 3.1f || _skipped) _overlay.HideTitle();
            if (Elapsed >= Duration - .95f) { _overlay.LetterboxTarget = 0f; _overlay.SkipEnabled = false; }
            if (Elapsed >= 2.8f) ShowMarker();
            HideHud();
            PoseCamera(Elapsed);
            yield return null;
        }
        if (_playing) Finish();
    }

    // 베지어 하강(왼쪽 위로 살짝 휘어 내려온다) + 시선은 섬 안쪽에서 플레이어 머리 위로 옮겨 온다.
    void PoseCamera(float time)
    {
        if (_camera == null) return;
        float move = VoyageCinematicOverlay.Smoother((time - .15f) / 3.75f);
        float turn = VoyageCinematicOverlay.Smoother((time - .05f) / 3.85f);
        Vector3 position = Vector3.Lerp(Vector3.Lerp(_startPos, _bendPos, move), Vector3.Lerp(_bendPos, _endPos, move), move);
        Vector3 look = Vector3.Lerp(_startLook, _endLook, turn);
        Quaternion rotation = move >= 1f && turn >= 1f ? _endRot : Quaternion.LookRotation(look - position, Vector3.up);
        _camera.transform.SetPositionAndRotation(position, rotation);
        _camera.fieldOfView = Mathf.Lerp(_fov + 6f, _fov, move);
    }

    void Finish()
    {
        if (!_playing) return;
        Elapsed = Duration;
        PoseCamera(Duration);
        if (_overlay != null)
        {
            _overlay.SetFade(0f);
            _overlay.HideTitle();
            _overlay.LetterboxTarget = 0f;
            _overlay.SkipEnabled = false;
        }
        RestoreControl();
        _playing = false;
        if (Current == this) IsPlaying = false;
        Debug.Log("[ARRIVAL-INTRO] control returned" + (_skipped ? " (skipped)" : string.Empty));
        BeginGuidance();
    }

    void RestoreControl()
    {
        if (_controlRestored) return;
        _controlRestored = true;
        if (_camera != null)
        {
            _camera.transform.SetPositionAndRotation(_endPos, _endRot);
            _camera.fieldOfView = _fov;
        }
        if (_follow != null && _followWasEnabled) _follow.enabled = true;
        if (_movement != null && _movementWasEnabled) _movement.enabled = true;
        if (_interaction != null && _interactionWasEnabled) _interaction.enabled = true;
        if (_input != null && _inputWasEnabled) _input.enabled = true;
        RestoreHud();
    }

    // 연출 동안 화면 HUD(시계·돈·핫바·알림 카드 등)를 잠시 끈다. 지연 생성된 HUD도 매 프레임 잡는다.
    void HideHud()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas == null || !canvas.enabled || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (canvas.transform.IsChildOf(transform)) continue;
            canvas.enabled = false;
            if (!_hidden.Contains(canvas)) _hidden.Add(canvas);
        }
    }

    void RestoreHud()
    {
        // 개발용 낮/밤 루프 캔버스는 FirstDayWorldPresentation이 계속 꺼 두므로 다시 켜지 않는다.
        foreach (var canvas in _hidden)
            if (canvas != null && canvas.name != "DayNightShopLoopCanvas") canvas.enabled = true;
        _hidden.Clear();
    }

    bool SupplyDone() => _supply == null || _supply.Collected || !_supply.gameObject.activeInHierarchy;

    void BeginGuidance()
    {
        if (SupplyDone()) return;
        _guiding = true;
        FirstDayWorldPresentation.Toast("DAY 1 · 개척 섬에 도착했어요\n" + ObjectiveText + ".", false);
        BuildChip();
        ShowMarker();
    }

    void Update()
    {
        if (_playing) return;
        float dt = Time.unscaledDeltaTime;
        if (!_guiding) _markerTarget = 0f;
        if (_overlay != null && _overlay.Letterbox <= 0f) { Destroy(_overlay.gameObject); _overlay = null; }
        if (_guiding && _doneAt < 0f && SupplyDone())
        {
            _doneAt = Time.unscaledTime;
            _markerTarget = 0f;
            if (_chipText != null)
            {
                _chipTag.text = "목표 완료";
                _chipText.text = "보급품을 챙겼어요";
                _chipAccent.color = PAUiTheme.Success;
            }
        }
        if (_chip != null)
        {
            bool visible = _guiding && (_doneAt < 0f || Time.unscaledTime < _doneAt + 1.8f) && !PlayerInputHandler.ModalOpen;
            _chipAlpha = Mathf.MoveTowards(_chipAlpha, visible ? 1f : 0f, dt / .3f);
            float e = VoyageCinematicOverlay.Ease(_chipAlpha);
            _chipGroup.alpha = e;
            _chip.anchoredPosition = new Vector2(24f - (1f - e) * 36f, -112f);
        }
        bool finished = !_guiding || _doneAt >= 0f && Time.unscaledTime > _doneAt + 2.4f;
        if (finished && _overlay == null && _markerAlpha <= 0f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 씬 전환·예외로 중간에 사라져도 조작/카메라/HUD는 반드시 돌려준다.
        RestoreControl();
        _playing = false;
        if (Current == this) { Current = null; IsPlaying = false; }
    }

    // ---- 보급 상자 표식: 카메라를 향한 금색 핀(잉크 외곽선) + 바닥의 천천히 도는 점선 고리 ----
    void ShowMarker()
    {
        if (SupplyDone()) return;
        if (_marker == null && !BuildMarker()) return;
        _markerTarget = 1f;
    }

    void LateUpdate()
    {
        if (_marker == null) return;
        float dt = Time.unscaledDeltaTime;
        if (_supply == null) _markerTarget = 0f;
        _markerAlpha = Mathf.MoveTowards(_markerAlpha, _markerTarget, dt / .35f);
        bool show = _markerAlpha > 0f && _supply != null;
        if (_marker.gameObject.activeSelf != show) _marker.gameObject.SetActive(show);
        if (_ring.gameObject.activeSelf != show) _ring.gameObject.SetActive(show);
        if (!show) return;
        float t = Time.unscaledTime;
        Vector3 ground = _supply.transform.position;
        float pop = VoyageCinematicOverlay.Ease(_markerAlpha);
        _marker.position = ground + Vector3.up * (1.95f + Mathf.Sin(t * 2.6f) * .12f);
        var view = _playing ? _camera : Camera.main;
        if (view != null) _marker.rotation = view.transform.rotation;
        _marker.localScale = Vector3.one * (.85f * Mathf.Lerp(.6f, 1f, pop) * (1f + Mathf.Sin(t * 5.2f) * .03f));
        float pulse = .5f + .5f * Mathf.Sin(t * 3.1f);
        _ring.SetPositionAndRotation(ground + Vector3.up * .05f, Quaternion.Euler(0f, t * 40f, 0f));
        _ring.localScale = Vector3.one * (1f + pulse * .12f);
        _block.SetColor(ColorId, new Color(1f, 1f, 1f, _markerAlpha));
        _pinRenderer.SetPropertyBlock(_block);
        _block.SetColor(ColorId, new Color(1f, 1f, 1f, _markerAlpha * (.55f + .35f * (1f - pulse))));
        _ringRenderer.SetPropertyBlock(_block);
    }

    bool BuildMarker()
    {
        if (_supply == null || !EnsureMarkerResources()) return false;
        _block = new MaterialPropertyBlock();
        _marker = new GameObject("SupplyMarker").transform;
        _marker.SetParent(transform, false);
        _pinRenderer = MeshObject("Pin", _marker, _pinMesh);
        _ring = new GameObject("SupplyRing").transform;
        _ring.SetParent(transform, false);
        _ringRenderer = MeshObject("Ring", _ring, _ringMesh);
        _marker.gameObject.SetActive(false);
        _ring.gameObject.SetActive(false);
        return true;
    }

    static MeshRenderer MeshObject(string name, Transform parent, Mesh mesh)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _markerMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return renderer;
    }

    // HarvestTargetMarkers와 같은 규칙: 빌드에 항상 포함되는 Sprites/Default + 정점 색. 없으면 표식 없이 칩/알림만 남는다.
    static bool EnsureMarkerResources()
    {
        if (_markerMaterial != null && _pinMesh != null && _ringMesh != null) return true;
        if (_markerShaderMissing) return false;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            _markerShaderMissing = true;
            Debug.LogWarning("[ARRIVAL-INTRO] Sprites/Default is missing from this build; supply marker disabled.");
            return false;
        }
        if (_markerMaterial == null)
            _markerMaterial = new Material(shader) { name = "PA_SupplyMarker (runtime)", hideFlags = HideFlags.HideAndDontSave, renderQueue = 3100 };
        if (_pinMesh == null) _pinMesh = BuildPin();
        if (_ringMesh == null) _ringMesh = BuildRing();
        return true;
    }

    // 잉크 외곽선 → 금색 원판·뾰족한 끝 → 크림 안쪽 → 청록 점. 같은 평면이며 그리는 순서대로 겹친다.
    static Mesh BuildPin()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        var ink = new Color(.10f, .22f, .24f, 1f);
        var gold = new Color(1f, .78f, .16f, 1f);
        var cream = new Color(1f, .97f, .88f, 1f);
        var teal = new Color(.20f, .55f, .56f, 1f);
        var center = new Vector3(0f, .62f, 0f);
        Disc(vertices, colors, triangles, center, .5f, ink, 32);
        Triangle(vertices, colors, triangles, new Vector3(-.36f, .34f, 0f), new Vector3(.36f, .34f, 0f), new Vector3(0f, -.15f, 0f), ink);
        Disc(vertices, colors, triangles, center, .44f, gold, 32);
        Triangle(vertices, colors, triangles, new Vector3(-.31f, .36f, 0f), new Vector3(.31f, .36f, 0f), new Vector3(0f, -.07f, 0f), gold);
        Disc(vertices, colors, triangles, center, .3f, cream, 28);
        Disc(vertices, colors, triangles, center, .13f, teal, 20);
        var mesh = new Mesh { name = "PA_SupplyMarkerPin", hideFlags = HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, new List<Vector2>(new Vector2[vertices.Count]));
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // 바닥에 눕힌 점선 고리(2칸 그리고 1칸 비움).
    static Mesh BuildRing()
    {
        const int segments = 36;
        const float inner = .92f, outer = 1.12f;
        var gold = new Color(1f, .80f, .22f, 1f);
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            if (i % 3 == 2) continue;
            float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
            int b = vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * inner);
            vertices.Add(new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * outer);
            vertices.Add(new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * inner);
            vertices.Add(new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * outer);
            for (int k = 0; k < 4; k++) colors.Add(gold);
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 2);
            triangles.Add(b + 2); triangles.Add(b + 1); triangles.Add(b + 3);
        }
        var mesh = new Mesh { name = "PA_SupplyMarkerRing", hideFlags = HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, new List<Vector2>(new Vector2[vertices.Count]));
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void Disc(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3 center, float radius, Color color, int segments)
    {
        int start = vertices.Count;
        vertices.Add(center); colors.Add(color);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            vertices.Add(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            colors.Add(color);
        }
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(start); triangles.Add(start + 1 + i); triangles.Add(start + 1 + (i + 1) % segments);
        }
    }

    static void Triangle(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        colors.Add(color); colors.Add(color); colors.Add(color);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }

    // ---- 다음 목표 칩: 왼쪽 위 시계 칩 아래 크림 카드(청록 막대 + 잉크 글씨). 상자를 열면 완료로 바뀌었다 사라진다. ----
    void BuildChip()
    {
        var root = new GameObject("PA_ArrivalObjective", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _chip = (RectTransform)new GameObject("ObjectiveChip", typeof(RectTransform), typeof(Image)).transform;
        _chip.SetParent(root.transform, false);
        _chip.anchorMin = _chip.anchorMax = _chip.pivot = new Vector2(0f, 1f);
        _chip.anchoredPosition = new Vector2(-12f, -112f);
        _chip.sizeDelta = new Vector2(500f, 76f);
        var image = _chip.GetComponent<Image>();
        PAUiTheme.Surface(image, ChipColor);
        image.raycastTarget = false;
        _chipGroup = _chip.gameObject.AddComponent<CanvasGroup>();
        _chipGroup.alpha = 0f; _chipGroup.interactable = false; _chipGroup.blocksRaycasts = false;
        var accent = (RectTransform)new GameObject("Accent", typeof(RectTransform), typeof(Image)).transform;
        accent.SetParent(_chip, false);
        // 2026-11-03 DESIGN.md(왼쪽 색 띠 금지)·알림 카드와 맞춤: 왼쪽 막대 대신 "다음 목표" 알약 표지(완료 시 초록).
        accent.anchorMin = accent.anchorMax = accent.pivot = new Vector2(0f, 1f);
        accent.anchoredPosition = new Vector2(22f, -8f); accent.sizeDelta = new Vector2(100f, 26f);
        _chipAccent = accent.GetComponent<Image>();
        PAUiTheme.Surface(_chipAccent, PAUiTheme.Teal); _chipAccent.raycastTarget = false;
        _chipTag = Label(_chip, "Tag", "다음 목표", new Vector2(22f, -9f), new Vector2(100f, 24f), 16f, Color.white);
        _chipTag.alignment = TextAlignmentOptions.Center;
        _chipText = Label(_chip, "Objective", ObjectiveText, new Vector2(24f, -36f), new Vector2(462f, 32f), 23f, PAUiTheme.Ink);
        _chipText.enableAutoSizing = true; _chipText.fontSizeMin = 16f; _chipText.fontSizeMax = 23f;
        _chipText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = rect.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = fontSize; label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
        return label;
    }
}
