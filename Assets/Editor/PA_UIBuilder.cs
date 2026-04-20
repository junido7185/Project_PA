#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// P.A. UI 자동 구성 빌더 (Docs §레퍼런스.html).
// 메뉴: P.A. System > Build UI System
//
// 실행 순서:
//   1. 기존 PA_UIRoot / EventSystem 제거 (전적 재생성)
//   2. Canvas (Screen Space Overlay, Scale With Screen Size 1920x1080)
//   3. EventSystem (Input System 기반)
//   4. HotbarRoot — 하단 중앙 상시 노출
//   5. InventoryPanel — 캐릭터 머리 위 토글 (시작 비활성)
//   6. SmartphoneContainer — 좌하단 숨김 → P 키로 중앙 팝업
//   7. Tooltip / DragLayer
//   8. Inspector 참조 자동 연결
//
// 기존 PA_SceneAutoBuilder 와 동일한 패턴(SerializedObject, Prog, LoadOrCreate) 사용.
public static class PA_UIBuilder
{
    // ── 색상 팔레트 (Docs/레퍼런스 폴더 3장 분석 기반) ────────────────────────
    // 동물의 숲 인벤토리 + 데이브 더 다이버 스마트폰 스크린샷에서 추출한 컬러 키
    static readonly Color C_Coral      = new Color(0.941f, 0.502f, 0.439f, 1f); // #F08070 라벨
    static readonly Color C_Mint       = new Color(0.502f, 0.816f, 0.678f, 1f); // #80D0AD 동숲 라벨 태그
    static readonly Color C_Yellow     = new Color(0.961f, 0.843f, 0.431f, 1f); // #F5D76E
    static readonly Color C_Sand       = new Color(0.831f, 0.722f, 0.588f, 1f); // #D4B896
    static readonly Color C_PhoneBody  = new Color(0.910f, 0.337f, 0.337f, 1f); // #E85656 데이브 코랄 레드
    static readonly Color C_PhoneBezel = new Color(0.078f, 0.078f, 0.078f, 1f); // #141414 얇은 검정 베젤
    static readonly Color C_PhoneScr   = new Color(0.960f, 0.455f, 0.565f, 1f); // #F57490 핑크 월페이퍼 상단
    static readonly Color C_PhoneScr2  = new Color(0.878f, 0.345f, 0.478f, 1f); // #E0587A 핑크 월페이퍼 하단
    static readonly Color C_StatusBar  = new Color(0.835f, 0.275f, 0.357f, 1f); // #D54659 상태바
    static readonly Color C_InvBG      = new Color(1f, 0.996f, 0.984f, 1f);     // #FFFEFB 순백 (살짝 웜)
    static readonly Color C_HotbarBG   = new Color(0f, 0f, 0f, 0.35f);
    static readonly Color C_Shadow     = new Color(0f, 0f, 0f, 0.25f);
    static readonly Color C_SlotBG     = new Color(0.937f, 0.894f, 0.816f, 1f); // #efe4d0
    static readonly Color C_AppAudit   = new Color(0.388f, 0.714f, 0.925f, 1f); // #63B6EC
    static readonly Color C_AppHiring  = new Color(0.949f, 0.639f, 0.380f, 1f); // #F2A361
    static readonly Color C_AppFeed    = new Color(0.596f, 0.510f, 0.894f, 1f); // #9882E4
    static readonly Color C_AppSetting = new Color(0.502f, 0.502f, 0.502f, 1f); // #808080

    const string UI_SLOT_PREFAB_PATH = "Assets/Prefabs/UI_Slot.prefab";

    // ── 스프라이트 경로 (Docs/UI_스프라이트_가이드.md 기준) ─────────────────────
    // 스프라이트가 없어도 빌드는 성공하되 컬러 플랫으로 폴백됨.
    // 파일을 Assets/Art/UI/ 에 떨구고 메뉴를 다시 실행하면 자동으로 주입된다.
    const string SPR_DIR                = "Assets/Art/UI/";
    const string SPR_DROPSHADOW         = SPR_DIR + "ui_dropshadow.png";
    const string SPR_INV_PANEL          = SPR_DIR + "ui_inventory_panel.png";
    const string SPR_INV_LABEL          = SPR_DIR + "ui_inventory_label.png";
    const string SPR_HOTBAR_BG          = SPR_DIR + "ui_hotbar_bg.png";
    const string SPR_PHONE_BODY         = SPR_DIR + "ui_phone_body.png";
    const string SPR_PHONE_BEZEL        = SPR_DIR + "ui_phone_bezel.png";
    const string SPR_PHONE_SCREEN       = SPR_DIR + "ui_phone_screen.png";
    const string SPR_PHONE_STATUSBAR    = SPR_DIR + "ui_phone_statusbar.png";
    const string SPR_APP_ICON_BASE      = SPR_DIR + "ui_app_icon_base.png";
    const string SPR_ICON_AUDIT         = SPR_DIR + "icon_app_audit.png";
    const string SPR_ICON_HIRING        = SPR_DIR + "icon_app_hiring.png";
    const string SPR_ICON_FEED          = SPR_DIR + "icon_app_feed.png";
    const string SPR_ICON_SETTINGS      = SPR_DIR + "icon_app_settings.png";

    // 누락된 스프라이트를 한 번씩만 경고하기 위한 중복 제거 집합
    static readonly HashSet<string> _missingSprCache = new HashSet<string>();

    // ── 메뉴 엔트리 ───────────────────────────────────────────────────────────
    [MenuItem("P.A. System/Build UI System", priority = 2)]
    public static void BuildUISystem()
    {
        if (!EditorUtility.DisplayDialog("P.A. UI 자동 구성",
            "기존 UI 계층(PA_UIRoot)을 삭제하고 재생성합니다.\n\n" +
            "• Canvas + EventSystem 재구축\n" +
            "• Hotbar / Inventory / Smartphone / Tooltip / DragLayer 생성\n" +
            "• 씬의 Inventory·Hotbar 를 찾아 참조 자동 연결\n\n" +
            "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
            "빌드 시작", "취소"))
            return;

        try
        {
            _missingSprCache.Clear();

            Prog("기존 UI 제거 중...", 0.00f);
            PurgeOldUI();

            Prog("Canvas 생성 중...", 0.10f);
            var canvas = CreateCanvas(out var canvasGO);

            Prog("EventSystem 생성 중...", 0.15f);
            CreateEventSystem();

            Prog("DragLayer 생성 중...", 0.20f);
            var dragLayer = CreateDragLayer(canvasGO.transform);

            Prog("Tooltip 생성 중...", 0.30f);
            var tooltip = CreateTooltip(canvasGO.transform);

            Prog("Hotbar 생성 중...", 0.45f);
            var hotbarCtx = CreateHotbar(canvasGO.transform);

            Prog("Inventory Panel 생성 중...", 0.60f);
            var inventoryCtx = CreateInventoryPanel(canvasGO.transform);

            Prog("Smartphone UI 생성 중...", 0.80f);
            var phoneCtx = CreateSmartphone(canvasGO.transform);

            Prog("MoneyHUD / ShopPriceUI 배치 중...", 0.88f);
            PlaceHUDServices(canvasGO.transform);

            Prog("참조 자동 연결 중...", 0.92f);
            WireReferences(canvas, tooltip, dragLayer, hotbarCtx, inventoryCtx, phoneCtx);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("완료",
                "UI 자동 구성 완료!\n\n" +
                "• I 키: 머리 위 인벤토리 토글\n" +
                "• P 키: 스마트폰 중앙 팝업\n" +
                "• Space 키: ShopSlot 상호작용 → 가격 조정 UI 팝업\n" +
                "• 우상단: MoneyHUD (재화·티어 상시 표시)\n\n" +
                "Play 모드에서 확인해주세요.", "확인");

            Debug.Log("✅ PA_UIBuilder: UI 자동 구성 완료!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ PA_UIBuilder 오류: {e}");
            EditorUtility.DisplayDialog("빌드 오류",
                $"{e.Message}\n\nConsole 에서 상세 스택 트레이스를 확인하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void Prog(string msg, float t) =>
        EditorUtility.DisplayProgressBar("P.A. UI 빌더", msg, t);

    // ──────────────────────────────────────────────────────────────────────────
    // 1. 기존 UI 제거
    // ──────────────────────────────────────────────────────────────────────────
    static void PurgeOldUI()
    {
        var old = GameObject.Find("PA_UIRoot");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        foreach (var es in UnityEngine.Object.FindObjectsByType<EventSystem>(
                    FindObjectsSortMode.None))
        {
            UnityEngine.Object.DestroyImmediate(es.gameObject);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Canvas
    // ──────────────────────────────────────────────────────────────────────────
    static Canvas CreateCanvas(out GameObject go)
    {
        go = new GameObject("PA_UIRoot",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        return canvas;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. EventSystem (Input System 기반)
    // ──────────────────────────────────────────────────────────────────────────
    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. DragLayer
    // ──────────────────────────────────────────────────────────────────────────
    static RectTransform CreateDragLayer(Transform parent)
    {
        var go = new GameObject("DragLayer", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Stretch(rt);
        rt.SetAsLastSibling(); // 최상위

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;
        return rt;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 5. Tooltip
    // ──────────────────────────────────────────────────────────────────────────
    static ItemTooltip CreateTooltip(Transform parent)
    {
        var go = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta       = new Vector2(260, 120);
        rt.anchorMin       = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot           = new Vector2(0f, 1f);

        // 배경
        var bg = AddChild(go.transform, "Background", typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        Stretch(bgRT);
        bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // 아이콘
        var iconGO = AddChild(go.transform, "Icon", typeof(Image));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 1f);
        iconRT.pivot = new Vector2(0f, 1f);
        iconRT.anchoredPosition = new Vector2(10, -10);
        iconRT.sizeDelta = new Vector2(48, 48);

        // 이름
        var nameGO = AddChild(go.transform, "Name", typeof(TextMeshProUGUI));
        var nameRT = (RectTransform)nameGO.transform;
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot     = new Vector2(0f, 1f);
        nameRT.anchoredPosition = new Vector2(68, -10);
        nameRT.sizeDelta = new Vector2(-78, 28);
        var nameTx = nameGO.GetComponent<TextMeshProUGUI>();
        nameTx.fontSize = 20;
        nameTx.color    = Color.white;
        nameTx.text     = "Item Name";

        // 설명
        var descGO = AddChild(go.transform, "Description", typeof(TextMeshProUGUI));
        var descRT = (RectTransform)descGO.transform;
        descRT.anchorMin = new Vector2(0f, 0f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot     = new Vector2(0f, 1f);
        descRT.anchoredPosition = new Vector2(68, -42);
        descRT.sizeDelta = new Vector2(-78, -52);
        var descTx = descGO.GetComponent<TextMeshProUGUI>();
        descTx.fontSize = 14;
        descTx.color    = new Color(0.9f, 0.9f, 0.9f, 1f);
        descTx.text     = "";

        var tooltip = go.AddComponent<ItemTooltip>();
        using var so = new SerializedObject(tooltip);
        so.FindProperty("nameText").objectReferenceValue        = nameTx;
        so.FindProperty("descriptionText").objectReferenceValue = descTx;
        so.FindProperty("icon").objectReferenceValue            = iconGO.GetComponent<Image>();
        so.FindProperty("rectTransform").objectReferenceValue   = rt;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return tooltip;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 6. Hotbar — 하단 중앙
    // ──────────────────────────────────────────────────────────────────────────
    struct HotbarCtx
    {
        public HotbarUI      ui;
        public Transform     slotParent;
        public RectTransform root;
    }

    static HotbarCtx CreateHotbar(Transform parent)
    {
        var go = new GameObject("HotbarRoot", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        // 하단 중앙 앵커
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f);

        // 슬롯 9개 × 80 + 여백
        rt.sizeDelta = new Vector2(9 * 80 + 8 * 10 + 20, 100);

        var hotbarBgImg = go.GetComponent<Image>();
        hotbarBgImg.color = C_HotbarBG;
        hotbarBgImg.type  = Image.Type.Sliced;
        ApplySprite(hotbarBgImg, SPR_HOTBAR_BG); // ui_hotbar_bg.png (512x96, 9-slice L=R=40)

        // 슬롯 컨테이너
        var slotGO = AddChild(go.transform, "SlotGrid",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var slotRT = (RectTransform)slotGO.transform;
        Stretch(slotRT, 10f);

        var hlg = slotGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing         = 10;
        hlg.childAlignment  = TextAnchor.MiddleCenter;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;

        var ui = go.AddComponent<HotbarUI>();
        return new HotbarCtx { ui = ui, slotParent = slotGO.transform, root = rt };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 7. Inventory Panel — 캐릭터 머리 위 토글
    // ──────────────────────────────────────────────────────────────────────────
    struct InventoryCtx
    {
        public InventoryUI ui;
        public Transform   slotParent;
        public RectTransform root;
    }

    static InventoryCtx CreateInventoryPanel(Transform parent)
    {
        // 동물의 숲 스타일: 가로로 넓은 캡슐/알약 형태. 머리 위에 떠오른다.
        // 루트는 빈 RectTransform(추적용), 그 아래 Shadow → Background → LabelTag → SlotGrid 계층.
        var go = new GameObject("InventoryPanel",
            typeof(RectTransform),
            typeof(InventoryAnchorFollower));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);    // 하단 중심 — 머리 위로 뜸
        rt.sizeDelta = new Vector2(720, 200);    // 가로:세로 ≈ 3.6:1 캡슐 비율 (동숲 기준)

        // 1) 드롭 섀도 (배경 뒤쪽, 살짝 아래로 오프셋)
        var shadowGO = AddChild(go.transform, "Shadow", typeof(RectTransform), typeof(Image));
        var shadowRT = (RectTransform)shadowGO.transform;
        Stretch(shadowRT);
        shadowRT.anchoredPosition = new Vector2(0, -6);
        shadowRT.sizeDelta        = new Vector2(12, 12);  // 살짝 크게
        var shadowImg = shadowGO.GetComponent<Image>();
        shadowImg.color = C_Shadow;
        shadowImg.raycastTarget = false;
        ApplySprite(shadowImg, SPR_DROPSHADOW); // ui_dropshadow.png (9-slice L=R=T=B=96)

        // 2) 배경 (캡슐)
        var bgGO = AddChild(go.transform, "Background", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bgGO.transform;
        Stretch(bgRT);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = C_InvBG;
        bgImg.type  = Image.Type.Sliced;
        ApplySprite(bgImg, SPR_INV_PANEL); // ui_inventory_panel.png (512x200, 9-slice L=100 R=100)

        // 3) 라벨 태그 (좌상단 돌출, 민트 캡슐) — 선택된 아이템 이름 표시
        var labelGO = AddChild(go.transform, "SelectedItemLabel",
            typeof(RectTransform), typeof(Image));
        var labelRT = (RectTransform)labelGO.transform;
        labelRT.anchorMin = new Vector2(0f, 1f);
        labelRT.anchorMax = new Vector2(0f, 1f);
        labelRT.pivot     = new Vector2(0.5f, 0.5f);
        labelRT.sizeDelta = new Vector2(180, 44);
        labelRT.anchoredPosition = new Vector2(120, 8); // 우측 8px, 상단 8px 돌출
        var labelImg = labelGO.GetComponent<Image>();
        labelImg.color = C_Mint;
        labelImg.type  = Image.Type.Sliced;
        ApplySprite(labelImg, SPR_INV_LABEL); // ui_inventory_label.png (256x64, 9-slice L=32 R=32)

        var labelTxGO = AddChild(labelGO.transform, "Text", typeof(TextMeshProUGUI));
        Stretch((RectTransform)labelTxGO.transform, 8f);
        var labelTx = labelTxGO.GetComponent<TextMeshProUGUI>();
        labelTx.text      = "선택 아이템";
        labelTx.fontSize  = 16;
        labelTx.fontStyle = FontStyles.Bold;
        labelTx.color     = Color.white;
        labelTx.alignment = TextAlignmentOptions.Center;

        // 4) 슬롯 그리드 (동숲 레퍼런스: 2행 × 8열 = 최대 16슬롯 가시)
        //    실제 Inventory.size(기본 24) 가 더 많으면 추가 행 wrap — GridLayout 자동 처리
        var gridGO = AddChild(go.transform, "SlotGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        var gridRT = (RectTransform)gridGO.transform;
        gridRT.anchorMin = new Vector2(0f, 0f);
        gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.offsetMin = new Vector2(80, 20);      // 캡슐 좌우 둥근 끝 여백 80px
        gridRT.offsetMax = new Vector2(-80, -20);
        var glg = gridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize    = new Vector2(64, 64);
        glg.spacing     = new Vector2(8, 8);
        glg.constraint  = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 8;
        glg.childAlignment  = TextAnchor.MiddleCenter;

        var ui = go.AddComponent<InventoryUI>();
        // ⚠ 주의: InventoryUI.Start() 에서 슬롯을 생성한 뒤 직접 gameObject.SetActive(false) 를 호출한다.
        //    따라서 빌드 시점에 active=false 로 두면 Start 가 실행되지 않아 슬롯이 영원히 만들어지지 않는다.

        return new InventoryCtx { ui = ui, slotParent = gridGO.transform, root = rt };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 8. Smartphone — 좌하단 hidden → 중앙 팝업
    // ──────────────────────────────────────────────────────────────────────────
    struct PhoneCtx
    {
        public SmartphoneUI ui;
        public RectTransform root;
        public RectTransform hoverTrigger;
        public GameObject[]  tabPanels;
        public Button[]      tabButtons;
    }

    static PhoneCtx CreateSmartphone(Transform parent)
    {
        // ── 데이브 더 다이버 레퍼런스 분석:
        //   1) 바디 = 코랄 레드 (#E85656), 양 모서리 크게 라운드된 직사각형
        //   2) 얇은 검정 베젤이 화면을 감싼다 (≈ 14px)
        //   3) 스크린 = 핑크/코랄 그라데이션 월페이퍼
        //   4) 상단 상태바(DT / 시각 / 신호·배터리)
        //   5) 앱 그리드 — 둥근 사각형 컬러 타일 + 라벨

        // 캔버스 기준 해상도 1920x1080. 폰 크기 380x680.
        // 앵커 = pivot = 좌하단(0,0). anchoredPosition 으로 제어.
        //   hiddenPos  = (40, -600)   → 상단 80px 만 peek
        //   peekPos    = (40, -540)   → 상단 140px peek
        //   activePos  = (770, 200)   → (1920-380)/2 = 770, (1080-680)/2 = 200 중앙 배치

        var go = new GameObject("SmartphoneContainer",
            typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(380, 680);
        rt.anchoredPosition = new Vector2(40, -600); // 기본 hidden (대부분 화면 밖)

        var bodyImg = go.GetComponent<Image>();
        bodyImg.color = C_PhoneBody;
        bodyImg.type  = Image.Type.Sliced;
        ApplySprite(bodyImg, SPR_PHONE_BODY); // ui_phone_body.png (512x1024, 9-slice L=R=T=B=120)

        // ── 얇은 검정 베젤 (바디 내부에 14px 들여쓰기)
        var bezelGO = AddChild(go.transform, "PhoneBezel",
            typeof(RectTransform), typeof(Image));
        var bezelRT = (RectTransform)bezelGO.transform;
        Stretch(bezelRT, 14f);
        var bezelImg = bezelGO.GetComponent<Image>();
        bezelImg.color = C_PhoneBezel;
        bezelImg.type  = Image.Type.Sliced;
        ApplySprite(bezelImg, SPR_PHONE_BEZEL); // ui_phone_bezel.png (512x1024, 9-slice L=R=T=B=110)

        // ── 스크린 (베젤 내부에 4px 추가 들여쓰기)
        var screenGO = AddChild(bezelGO.transform, "PhoneScreen",
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        var screenRT = (RectTransform)screenGO.transform;
        Stretch(screenRT, 4f);
        var screenImg = screenGO.GetComponent<Image>();
        screenImg.color = C_PhoneScr;   // 핑크 월페이퍼 (그라데이션은 스프라이트에서)
        ApplySprite(screenImg, SPR_PHONE_SCREEN); // ui_phone_screen.png (400x800 그라데이션, 9-slice 40/40/40/40)

        var screenVLG = screenGO.GetComponent<VerticalLayoutGroup>();
        screenVLG.childForceExpandHeight = false;
        screenVLG.childForceExpandWidth  = true;
        screenVLG.childControlWidth      = true;
        screenVLG.childControlHeight     = true;
        screenVLG.spacing                = 0;
        screenVLG.padding                = new RectOffset(16, 16, 12, 12);

        // ── 상태바 (DT / 06:00 AM / 신호·배터리)
        BuildStatusBar(screenGO.transform);

        // ── 앱 아이콘 컨텐츠 영역 (ContentArea)
        var contentGO = AddChild(screenGO.transform, "ContentArea",
            typeof(RectTransform), typeof(LayoutElement));
        contentGO.GetComponent<LayoutElement>().flexibleHeight = 1;

        // 4개 앱 정의 (감사/채용/피드/설정)
        // — 데이브 레퍼런스의 앱 그리드를 모방하여 2×2 큰 타일 형태로 제작.
        //   탭 UI 가 아니라 '홈 스크린' 스타일: 각 앱은 하나의 패널 대응.
        var appDefs = new (string name, string label, string emoji, Color color, string placeholder, string iconPath)[]
        {
            ("AuditPanel",    "감사", "📊", C_AppAudit,   "감사 시스템 준비 중", SPR_ICON_AUDIT),
            ("HiringPanel",   "채용", "🤝", C_AppHiring,  "채용 시스템 준비 중", SPR_ICON_HIRING),
            ("FeedPanel",     "피드", "📱", C_AppFeed,    "피드 시스템 준비 중", SPR_ICON_FEED),
            ("SettingsPanel", "설정", "⚙",  C_AppSetting, "설정 준비 중",       SPR_ICON_SETTINGS),
        };

        var panels  = new GameObject[appDefs.Length];
        var buttons = new Button[appDefs.Length];

        // Home 화면 (앱 그리드)
        var homeGO = AddChild(contentGO.transform, "HomeScreen",
            typeof(RectTransform), typeof(GridLayoutGroup));
        Stretch((RectTransform)homeGO.transform);
        var homeGrid = homeGO.GetComponent<GridLayoutGroup>();
        homeGrid.cellSize   = new Vector2(140, 160); // 아이콘 영역 + 라벨
        homeGrid.spacing    = new Vector2(16, 24);
        homeGrid.padding    = new RectOffset(20, 20, 40, 20);
        homeGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        homeGrid.constraintCount = 2;
        homeGrid.childAlignment = TextAnchor.UpperCenter;

        for (int i = 0; i < appDefs.Length; i++)
        {
            var d = appDefs[i];

            // 앱 아이콘 타일 (세로형: 아이콘 정사각 + 아래 라벨)
            var appGO = AddChild(homeGO.transform, d.name + "_Tile",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            var appVLG = appGO.GetComponent<VerticalLayoutGroup>();
            appVLG.spacing                = 6;
            appVLG.childAlignment         = TextAnchor.UpperCenter;
            appVLG.childForceExpandWidth  = true;
            appVLG.childForceExpandHeight = false;

            // 아이콘 버튼 (둥근 컬러 사각형)
            var iconGO = AddChild(appGO.transform, "Icon",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            iconGO.GetComponent<LayoutElement>().preferredHeight = 120;
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.color = d.color;
            iconImg.type  = Image.Type.Sliced;
            ApplySprite(iconImg, SPR_APP_ICON_BASE); // ui_app_icon_base.png (128x128, 9-slice 24)
            buttons[i] = iconGO.GetComponent<Button>();

            // 앱 전용 아이콘이 있으면 이모지 대신 Image 로 표시.
            var appIconSpr = TryLoadSprite(d.iconPath);
            if (appIconSpr != null)
            {
                var iconFgGO = AddChild(iconGO.transform, "IconFG",
                    typeof(RectTransform), typeof(Image));
                var iconFgRT = (RectTransform)iconFgGO.transform;
                Stretch(iconFgRT, 14f); // 베이스 안쪽 14px 여백
                var iconFgImg = iconFgGO.GetComponent<Image>();
                iconFgImg.sprite        = appIconSpr;
                iconFgImg.preserveAspect = true;
                iconFgImg.raycastTarget  = false;
            }
            else
            {
                // 폴백: 이모지 플레이스홀더
                var emojiGO = AddChild(iconGO.transform, "Emoji", typeof(TextMeshProUGUI));
                Stretch((RectTransform)emojiGO.transform);
                var emojiTx = emojiGO.GetComponent<TextMeshProUGUI>();
                emojiTx.text      = d.emoji;
                emojiTx.fontSize  = 56;
                emojiTx.alignment = TextAlignmentOptions.Center;
                emojiTx.color     = Color.white;
            }

            // 라벨 (아이콘 아래)
            var labelGO = AddChild(appGO.transform, "Label",
                typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGO.GetComponent<LayoutElement>().preferredHeight = 28;
            var labelTx = labelGO.GetComponent<TextMeshProUGUI>();
            labelTx.text      = d.label;
            labelTx.fontSize  = 18;
            labelTx.fontStyle = FontStyles.Bold;
            labelTx.alignment = TextAlignmentOptions.Center;
            labelTx.color     = new Color(1f, 0.98f, 0.9f, 1f);

            // 앱 상세 패널 (ContentArea 자식으로 Home 위에 덮어씌움)
            var panelGO = AddChild(contentGO.transform, d.name,
                typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)panelGO.transform);
            panelGO.GetComponent<Image>().color = new Color(0.98f, 0.94f, 0.88f, 0.95f);

            var phTxGO = AddChild(panelGO.transform, "Placeholder", typeof(TextMeshProUGUI));
            Stretch((RectTransform)phTxGO.transform);
            var phTx = phTxGO.GetComponent<TextMeshProUGUI>();
            phTx.text      = d.placeholder;
            phTx.fontSize  = 22;
            phTx.alignment = TextAlignmentOptions.Center;
            phTx.color     = new Color(0.2f, 0.2f, 0.2f, 1f);

            panelGO.SetActive(false); // 기본은 Home 표시
            panels[i] = panelGO;
        }

        // 하단 닫기 힌트 ("P 키로 닫기")
        var hintGO = AddChild(screenGO.transform, "CloseHint",
            typeof(TextMeshProUGUI), typeof(LayoutElement));
        hintGO.GetComponent<LayoutElement>().preferredHeight = 32;
        var hintTx = hintGO.GetComponent<TextMeshProUGUI>();
        hintTx.text      = "[P] 닫기";
        hintTx.fontSize  = 14;
        hintTx.alignment = TextAlignmentOptions.Center;
        hintTx.color     = new Color(1f, 1f, 1f, 0.7f);

        var ui = go.AddComponent<SmartphoneUI>();
        return new PhoneCtx
        {
            ui           = ui,
            root         = rt,
            hoverTrigger = rt,
            tabPanels    = panels,
            tabButtons   = buttons,
        };
    }

    // 상태바: 좌측 "DT" 로고, 가운데 시각, 우측 신호·배터리 아이콘
    static void BuildStatusBar(Transform screenParent)
    {
        var barGO = AddChild(screenParent, "StatusBar",
            typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        barGO.GetComponent<LayoutElement>().preferredHeight = 36;
        var barImg = barGO.GetComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.15f);
        barImg.type  = Image.Type.Sliced;
        ApplySprite(barImg, SPR_PHONE_STATUSBAR); // ui_phone_statusbar.png (400x48, 9-slice L=R=20)
        var hlg = barGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(12, 12, 4, 4);
        hlg.childForceExpandWidth = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        // 좌측: DT
        var leftGO = AddChild(barGO.transform, "Logo", typeof(TextMeshProUGUI));
        var leftTx = leftGO.GetComponent<TextMeshProUGUI>();
        leftTx.text      = "DT";
        leftTx.fontSize  = 16;
        leftTx.fontStyle = FontStyles.Bold;
        leftTx.alignment = TextAlignmentOptions.MidlineLeft;
        leftTx.color     = Color.white;

        // 가운데: 시각 (GameClock 연동 추후)
        var timeGO = AddChild(barGO.transform, "Time", typeof(TextMeshProUGUI));
        var timeTx = timeGO.GetComponent<TextMeshProUGUI>();
        timeTx.text      = "06:00 AM";
        timeTx.fontSize  = 16;
        timeTx.alignment = TextAlignmentOptions.Center;
        timeTx.color     = Color.white;

        // 우측: 신호·배터리 (이모지 플레이스홀더)
        var rightGO = AddChild(barGO.transform, "Signal", typeof(TextMeshProUGUI));
        var rightTx = rightGO.GetComponent<TextMeshProUGUI>();
        rightTx.text      = "📶🔋";
        rightTx.fontSize  = 14;
        rightTx.alignment = TextAlignmentOptions.MidlineRight;
        rightTx.color     = Color.white;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 9. 참조 자동 연결
    // ──────────────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────────────
    // HUD 서비스 배치 (MoneyHUD + ShopPriceUI)
    // 두 컴포넌트 모두 자기완결(Self-Build) 이므로 빈 GO 에 붙이기만 하면 된다.
    // MoneyHUD  : PA_UIRoot 자식으로 배치 → Awake 에서 같은 Canvas 위에 HUD 패널 생성
    // ShopPriceUI: 독립 Canvas (sortOrder 200) 를 Awake 에서 자체 생성하므로 씬 루트 배치
    // ──────────────────────────────────────────────────────────────────────────
    static void PlaceHUDServices(Transform canvasTransform)
    {
        // ── MoneyHUD ──────────────────────────────────────────────────────────
        // 기존 MoneyHUD 이미 있으면 재생성하지 않는다.
        var existingHud = UnityEngine.Object.FindAnyObjectByType<MoneyHUD>();
        if (existingHud == null)
        {
            var hudGO = new GameObject("MoneyHUD", typeof(RectTransform));
            hudGO.transform.SetParent(canvasTransform, false);
            hudGO.AddComponent<MoneyHUD>();
            Debug.Log("  💰 MoneyHUD 배치 완료");
        }
        else
        {
            Debug.Log("  💰 MoneyHUD 이미 존재 — 스킵");
        }

        // ── ShopPriceUI ───────────────────────────────────────────────────────
        // 독립 Canvas 를 자체 생성하므로 씬 루트(최상위) 에 배치.
        var existingShopUI = UnityEngine.Object.FindAnyObjectByType<ShopPriceUI>();
        if (existingShopUI == null)
        {
            // 씬 루트에 배치 (Canvas 의 자식이 아님)
            var shopUIGO = new GameObject("ShopPriceUI");
            shopUIGO.AddComponent<ShopPriceUI>();
            Debug.Log("  🏷️ ShopPriceUI 배치 완료 (독립 Canvas sortOrder=200 은 PlayMode 에서 자동 생성)");
        }
        else
        {
            Debug.Log("  🏷️ ShopPriceUI 이미 존재 — 스킵");
        }
    }

    static void WireReferences(Canvas canvas, ItemTooltip tooltip, RectTransform dragLayer,
                                HotbarCtx hotbar, InventoryCtx inv, PhoneCtx phone)
    {
        // 씬에서 Inventory / Hotbar / Player 탐색
        var sceneInventory = UnityEngine.Object.FindAnyObjectByType<Inventory>();
        var sceneHotbar    = UnityEngine.Object.FindAnyObjectByType<Hotbar>();
        var player         = GameObject.FindWithTag("Player");

        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_SLOT_PREFAB_PATH);
        if (slotPrefab == null)
            Debug.LogWarning($"⚠ UI_Slot 프리팹을 {UI_SLOT_PREFAB_PATH} 에서 찾지 못했습니다. 슬롯 프리팹 참조를 수동 지정해주세요.");

        if (sceneInventory == null)
            Debug.LogWarning("⚠ 씬에서 Inventory 컴포넌트를 찾지 못했습니다. Inspector 에서 수동 지정해주세요.");
        if (sceneHotbar == null)
            Debug.LogWarning("⚠ 씬에서 Hotbar 컴포넌트를 찾지 못했습니다. Inspector 에서 수동 지정해주세요.");

        // InventoryUI
        {
            using var so = new SerializedObject(inv.ui);
            SetRef(so, "inventory",  sceneInventory);
            SetRef(so, "hotbar",     sceneHotbar);
            SetRef(so, "slotParent", inv.slotParent);
            SetRef(so, "slotPrefab", slotPrefab);
            SetRef(so, "tooltip",    tooltip);
            SetRef(so, "dragLayer",  dragLayer);
            SetRef(so, "rootCanvas", canvas);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // InventoryAnchorFollower — Player 참조
        {
            var follower = inv.ui.GetComponent<InventoryAnchorFollower>();
            if (follower != null)
            {
                using var so = new SerializedObject(follower);
                if (player != null) SetRef(so, "target", player.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // HotbarUI
        {
            using var so = new SerializedObject(hotbar.ui);
            SetRef(so, "hotbar",      sceneHotbar);
            SetRef(so, "inventory",   sceneInventory);
            SetRef(so, "slotParent",  hotbar.slotParent);
            SetRef(so, "slotPrefab",  slotPrefab);
            SetRef(so, "tooltip",     tooltip);
            SetRef(so, "dragLayer",   dragLayer);
            SetRef(so, "rootCanvas",  canvas);
            // toolsParent — Player 손 홀더가 있으면 연결 시도
            if (player != null)
            {
                var holder = FindDescendant(player.transform, "Hand_Holder")
                           ?? FindDescendant(player.transform, "ToolsParent");
                if (holder != null) SetRef(so, "toolsParent", holder);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // SmartphoneUI
        {
            using var so = new SerializedObject(phone.ui);
            SetRef(so, "root",         phone.root);
            SetRef(so, "hoverTrigger", phone.hoverTrigger);

            var tabPanelsProp = so.FindProperty("tabPanels");
            tabPanelsProp.arraySize = phone.tabPanels.Length;
            for (int i = 0; i < phone.tabPanels.Length; i++)
                tabPanelsProp.GetArrayElementAtIndex(i).objectReferenceValue = phone.tabPanels[i];

            var tabButtonsProp = so.FindProperty("tabButtons");
            tabButtonsProp.arraySize = phone.tabButtons.Length;
            for (int i = 0; i < phone.tabButtons.Length; i++)
                tabButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = phone.tabButtons[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("  🔗 Inspector 참조 자동 연결 완료");
    }

    static void SetRef(SerializedObject so, string name, UnityEngine.Object target)
    {
        var p = so.FindProperty(name);
        if (p == null)
        {
            Debug.LogWarning($"⚠ '{so.targetObject.GetType().Name}.{name}' 필드를 찾지 못했습니다.");
            return;
        }
        p.objectReferenceValue = target;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ──────────────────────────────────────────────────────────────────────────
    static GameObject AddChild(Transform parent, string name, params Type[] components)
    {
        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    // 스프라이트 로더: 없으면 한 번만 경고하고 null 반환 → 호출자가 컬러 플랫 폴백.
    static Sprite TryLoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr == null && _missingSprCache.Add(path))
            Debug.LogWarning($"🎨 스프라이트 미발견: {path} — 컬러 플랫으로 폴백. Nano Banana 출력을 이 경로에 저장 후 재빌드.");
        return spr;
    }

    // Image 에 스프라이트를 안전하게 주입. null 이면 sprite 를 비우고 9-slice 도 유지.
    static void ApplySprite(Image img, string path, Image.Type type = Image.Type.Sliced)
    {
        if (img == null) return;
        var spr = TryLoadSprite(path);
        if (spr != null)
        {
            img.sprite = spr;
            img.type   = type;
        }
    }

    static Transform FindDescendant(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindDescendant(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
