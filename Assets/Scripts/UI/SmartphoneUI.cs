using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// §레퍼런스.html — 📱 스마트폰 UI
// 좌하단에 숨겨져 있다가 P 키 또는 마우스 근접으로 화면 중앙까지 떠오른다.
// 인벤토리와 상호배타 (한 쪽이 열리면 다른 쪽은 강제로 닫힌다).
//
// 4개 탭: 감사 / 채용 / 피드 / 설정 — 현재는 플레이스홀더.
public class SmartphoneUI : MonoBehaviour
{
    public static SmartphoneUI instance;

    [Header("참조")]
    public RectTransform root;          // 이동 대상 (SmartphoneContainer 자신)
    public RectTransform hoverTrigger;  // 마우스 근접 감지 영역 (보통 root 와 동일)
    public GameObject    homeScreen;    // 홈(앱 그리드) — 패널 진입 시 비활성/복귀 시 활성
    public GameObject[]  tabPanels;     // 4개 패널 (감사/채용/피드/설정)
    public Button[]      tabButtons;    // 4개 탭 버튼

    [Header("앵커 위치 (anchoredPosition 기준)")]
    // 기준: Canvas 1920x1080, 폰 380x680, anchor/pivot = (0,0) 좌하단
    // 데이브 더 다이버 레퍼런스: 폰 윗부분 80~140px 만 peek 되도록 설계
    // ⚠️ hiddenPos/peekPos 는 캔버스 해상도에 무관하게 좌하단 기준이라 고정값 OK.
    //    activePos 는 런타임에 ComputeActivePos() 로 계산 — Free Aspect 대응.
    public Vector2 hiddenPos = new Vector2(40f, -600f);    // 상단 80px peek (대부분 화면 밖)
    public Vector2 peekPos   = new Vector2(40f, -540f);    // 상단 140px peek (호버 시)

    [Header("트랜지션")]
    public float transitionTime = 0.45f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("마우스 근접 감지")]
    public float hoverPadding = 80f;    // hoverTrigger 바깥 허용 거리

    [Header("탭 색상")]
    public Color tabNormalColor   = new Color(1f, 1f, 1f, 0.9f);
    public Color tabSelectedColor = new Color(0.96f, 0.84f, 0.43f, 1f); // #F5D76E

    public bool IsOpen => _isOpen;
    public int CurrentTabIndex => _currentTab;

    bool      _isOpen;
    Coroutine _moveCo;
    int       _currentTab;
    Canvas    _canvas; // activePos 계산용 캔버스 참조
    Color[]   _tabBaseColors;

    void Awake()
    {
        instance = this;
        if (root == null) root = transform as RectTransform;
        if (hoverTrigger == null) hoverTrigger = root;
        _canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        // 초기 위치: 좌하단 숨김
        if (root != null) root.anchoredPosition = hiddenPos;
        CacheTabButtonColors();

        // P 키 구독
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPhoneToggle += Toggle;

        // 탭 버튼 콜백 (앱 아이콘 클릭 → 해당 패널만 활성화)
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                int captured = i; // 클로저 캡처
                tabButtons[i].onClick.AddListener(() => SelectTab(captured));
            }
        }

        // ⚠ v3 변경: SelectTab(0) 자동 호출 제거.
        //   기존: Start 직후 AuditPanel 이 켜져 홈 그리드를 가렸음 (스크린샷 1 의 버그).
        //   변경: 시작 시 모든 패널 비활성 → 홈 그리드만 노출. 사용자가 앱 아이콘 클릭 시
        //         SelectTab(idx) 가 해당 패널을 띄움. 패널의 BackButton 이 닫음.
        ReturnToHome();
    }

    // 모든 패널 비활성화 → 홈 화면(앱 그리드) 만 보이게
    public void ReturnToHome()
    {
        if (tabPanels != null)
            foreach (var p in tabPanels)
                if (p != null) p.SetActive(false);
        if (homeScreen != null) homeScreen.SetActive(true);
        _currentTab = -1;
        RestoreTabButtonColors();
    }

    void CacheTabButtonColors()
    {
        if (tabButtons == null)
        {
            _tabBaseColors = null;
            return;
        }

        _tabBaseColors = new Color[tabButtons.Length];
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var img = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
            _tabBaseColors[i] = img != null ? img.color : Color.white;
        }
    }

    void RestoreTabButtonColors()
    {
        if (tabButtons == null) return;
        if (_tabBaseColors == null || _tabBaseColors.Length != tabButtons.Length)
            CacheTabButtonColors();

        for (int i = 0; i < tabButtons.Length; i++)
        {
            var img = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
            if (img != null && _tabBaseColors != null && i < _tabBaseColors.Length)
                img.color = _tabBaseColors[i];
        }
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPhoneToggle -= Toggle;
    }

    void Update()
    {
        // 열린 상태에서는 hover peek 동작하지 않음
        if (_isOpen) return;
        if (root == null || hoverTrigger == null) return;
        if (Mouse.current == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();
        bool near = IsMouseNear(hoverTrigger, mouse, hoverPadding);

        // 트랜지션 중이 아니면 스냅하듯 타겟 변경
        Vector2 targetPos = near ? peekPos : hiddenPos;
        if ((root.anchoredPosition - targetPos).sqrMagnitude > 0.1f
            && _moveCo == null)
        {
            StartMove(targetPos, transitionTime * 0.5f); // hover 전환은 빠르게
        }
    }

    // 📱 외부 토글 진입점 (P 키 or 버튼)
    // 닫힐 때는 항상 홈으로 복귀시켜 다음 열기에서 깨끗한 상태 보장.
    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
            NpcBubbleUI.HideAll();

        // 상호배타: 열릴 때 인벤토리가 열려있으면 강제로 닫는다.
        if (_isOpen
            && InventoryUI.instance != null
            && InventoryUI.instance.gameObject.activeSelf)
        {
            InventoryUI.instance.Toggle();
        }

        // 🚪 열릴 때 activePos 를 캔버스 실제 크기 기준으로 재계산 (Free Aspect 대응)
        Vector2 target = _isOpen ? ComputeActivePos() : hiddenPos;
        StartMove(target, transitionTime);

        // 닫힐 때: 홈 복귀 + 커서 정상 복구
        if (!_isOpen)
        {
            ReturnToHome();
        }

        // 커서 잠금: 폰이 열려 있을 때만 해제 — 닫히면 항상 게임 상태로 복구
        ApplyCursorState();
    }

    // 명시적 닫기 — UI 닫기 버튼/ESC 가 호출. 이미 닫혀있으면 noop.
    public void Close()
    {
        if (!_isOpen) return;
        Toggle();
    }

    // ESC 처리 — 패널이 열려 있으면 홈으로, 홈만 보이면 폰 자체를 닫는다.
    public void OnEscape()
    {
        if (!_isOpen) return;
        if (_currentTab >= 0)
        {
            // 앱 패널 → 홈 복귀
            ReturnToHome();
        }
        else
        {
            // 홈 → 폰 닫기
            Close();
        }
    }

    // 닫힘 상태에서 게임 커서로 복구 (안전한 단일 진입점)
    void ApplyCursorState()
    {
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = _isOpen;
    }

    // 캔버스 실제 rect 크기를 기반으로 스마트폰이 정중앙에 오는 anchoredPosition 계산.
    // anchor/pivot 이 (0,0) 좌하단이므로: (canvasW - phoneW) / 2, (canvasH - phoneH) / 2
    Vector2 ComputeActivePos()
    {
        if (root == null) return new Vector2(770f, 200f);
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return new Vector2(770f, 200f);

        Vector2 canvasSize = ((RectTransform)_canvas.transform).rect.size;
        Vector2 phoneSize  = root.rect.size;
        return new Vector2(
            (canvasSize.x - phoneSize.x) * 0.5f,
            (canvasSize.y - phoneSize.y) * 0.5f
        );
    }

    public void SelectTab(int index)
    {
        if (tabPanels == null) return;
        _currentTab = Mathf.Clamp(index, 0, tabPanels.Length - 1);

        // 홈 화면 비활성 — 패널이 아이콘 위에 겹쳐 보이는 시각 버그 방지
        if (homeScreen != null) homeScreen.SetActive(false);

        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == _currentTab);

            if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null)
            {
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = (i == _currentTab) ? tabSelectedColor : tabNormalColor;
            }
        }
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────────

    void StartMove(Vector2 target, float duration)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveCoroutine(target, duration));
    }

    IEnumerator MoveCoroutine(Vector2 target, float duration)
    {
        if (root == null) yield break;
        Vector2 start = root.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = easeCurve.Evaluate(Mathf.Clamp01(t / duration));
            root.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        root.anchoredPosition = target;
        _moveCo = null;
    }

    static bool IsMouseNear(RectTransform rt, Vector2 screenPoint, float padding)
    {
        // RectTransform 의 월드 모서리 4개 → 스크린 좌표 AABB 로 근접 판정
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = Mathf.Min(corners[0].x, corners[2].x) - padding;
        float maxX = Mathf.Max(corners[0].x, corners[2].x) + padding;
        float minY = Mathf.Min(corners[0].y, corners[2].y) - padding;
        float maxY = Mathf.Max(corners[0].y, corners[2].y) + padding;
        return screenPoint.x >= minX && screenPoint.x <= maxX
            && screenPoint.y >= minY && screenPoint.y <= maxY;
    }
}
