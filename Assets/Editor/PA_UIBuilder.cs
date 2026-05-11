#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    static readonly Color C_HotbarBG   = new Color(0.18f, 0.14f, 0.10f, 0.78f); // 어두운 다크우드 (밝은 슬롯 베이스 강조)
    static readonly Color C_Shadow     = new Color(0f, 0f, 0f, 0.25f);
    static readonly Color C_SlotBG     = new Color(0.937f, 0.894f, 0.816f, 1f); // #efe4d0 동숲 베이지
    static readonly Color C_SlotEmpty  = new Color(0.85f, 0.80f, 0.72f, 1f);    // 슬롯 fallback (스프라이트 없을 때 채움)
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
    const string SPR_INV_SLOT           = SPR_DIR + "ui_inventory_slot.png";
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

            Prog("UI fallback sprite 준비 중...", 0.05f);
            EnsureSlotPrefabStyle();

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
        // ⚠ v3: 스프라이트 미존재 시 fallback 색을 어두운 무채색 → 다크 우드 브라운 으로 변경.
        //   슬롯이 베이지(#EFE4D0) 라 대비가 살아남.
        hotbarBgImg.color = C_HotbarBG;
        hotbarBgImg.type  = Image.Type.Sliced;
        hotbarBgImg.raycastTarget = false;
        ApplySprite(hotbarBgImg, SPR_HOTBAR_BG);

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
        // 동물의 숲 스타일: 둥근 흰색 콩 패널.
        // 변경:
        //   • "선택 아이템" 큰 라벨 제거 → 자연스러운 통합 디자인
        //   • 패널 알파/색감을 부드러운 흰색(살짝 웜)으로 통일
        //   • 핫바와 겹치지 않도록 캐릭터 머리 위에서 더 위로 띄움
        //   • 코드 fallback 만으로도 둥근 느낌이 나도록 외곽선 + 진한 그림자 활용
        var go = new GameObject("InventoryPanel",
            typeof(RectTransform),
            typeof(InventoryAnchorFollower));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);    // 하단 중심 — 머리 위로 뜸
        rt.sizeDelta = new Vector2(760, 220);    // rounded AC-like pocket panel, 8x3 expandable base

        // 1) 부드러운 그림자 (콩 패널 아래 살짝 큼직하게)
        var shadowGO = AddChild(go.transform, "Shadow", typeof(RectTransform), typeof(Image));
        var shadowRT = (RectTransform)shadowGO.transform;
        Stretch(shadowRT);
        shadowRT.anchoredPosition = new Vector2(0, -8);
        shadowRT.sizeDelta        = new Vector2(24, 24);  // 패널보다 큼직하게
        var shadowImg = shadowGO.GetComponent<Image>();
        shadowImg.color = new Color(0f, 0f, 0f, 0.22f); // 부드러운 검정
        shadowImg.raycastTarget = false;
        ApplySprite(shadowImg, SPR_DROPSHADOW);

        // 2) 흰색 배경 (콩 모양 — 9-slice 둥근 끝)
        var bgGO = AddChild(go.transform, "Background", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bgGO.transform;
        Stretch(bgRT);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(1f, 0.998f, 0.988f, 1f); // 동숲 톤 살짝 웜한 순백
        bgImg.type  = Image.Type.Sliced;
        ApplySprite(bgImg, SPR_INV_PANEL);

        // 3) 부드러운 외곽선 — 스프라이트 미존재 시 둥근 느낌을 살리는 fallback
        //    (스프라이트 들어오면 자동으로 가려져 보이지 않음)
        var rimGO = AddChild(go.transform, "Rim", typeof(RectTransform), typeof(Image));
        var rimRT = (RectTransform)rimGO.transform;
        rimRT.anchorMin = Vector2.zero;
        rimRT.anchorMax = Vector2.one;
        rimRT.offsetMin = new Vector2(-2, -2);
        rimRT.offsetMax = new Vector2(2, 2);
        var rimImg = rimGO.GetComponent<Image>();
        rimImg.color = new Color(0.92f, 0.88f, 0.80f, 0.6f); // 부드러운 베이지 외곽선
        rimImg.type  = Image.Type.Sliced;
        rimImg.raycastTarget = false;
        ApplySprite(rimImg, SPR_INV_PANEL);

        // 4) 슬롯 그리드 — 큰 셀 + 넉넉한 패딩으로 동숲 분위기
        //    8x3 = 24슬롯, 셀 60x60, 캡슐 좌우 둥근 끝 패딩 96px
        var gridGO = AddChild(go.transform, "SlotGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        var gridRT = (RectTransform)gridGO.transform;
        gridRT.anchorMin = new Vector2(0f, 0f);
        gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.offsetMin = new Vector2(94, 18);
        gridRT.offsetMax = new Vector2(-94, -18);
        var glg = gridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize    = new Vector2(58, 58);
        glg.spacing     = new Vector2(12, 12);
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
        public GameObject    homeScreen;
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

        // ── 스크린 (베젤 내부에 4px 추가 들여쓰기) — VLG 제거, 수동 anchored 레이아웃
        //
        // ⚠ v3 변경: 기존 VerticalLayoutGroup 이 StatusBar/ContentArea/CloseHint 의
        //   배치를 흐트러뜨려 상태바가 화면 중앙에 떠버리는 버그가 발생했음.
        //   해결: VLG 폐기 → 각 자식의 anchorMin/Max 로 직접 위치 고정.
        //   StatusBar = 상단 40px / CloseHint = 하단 28px / ContentArea = 그 사이.
        var screenGO = AddChild(bezelGO.transform, "PhoneScreen",
            typeof(RectTransform), typeof(Image));
        var screenRT = (RectTransform)screenGO.transform;
        Stretch(screenRT, 4f);
        var screenImg = screenGO.GetComponent<Image>();
        screenImg.color = C_PhoneScr;
        ApplySprite(screenImg, SPR_PHONE_SCREEN);

        // ── 상태바 (스크린 최상단 고정 — 40px)
        BuildStatusBar(screenGO.transform);

        // ── 닫기 힌트 (스크린 최하단 고정 — Button 으로 격상해서 클릭으로도 닫기)
        // 텍스트 힌트 + 클릭 가능한 영역. Image 알파 0 으로 보이지 않지만 Raycast 받음.
        var hintGO = AddChild(screenGO.transform, "CloseHint",
            typeof(RectTransform), typeof(Image), typeof(Button));
        var hintRT = (RectTransform)hintGO.transform;
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot     = new Vector2(0.5f, 0f);
        hintRT.sizeDelta = new Vector2(0f, 32f);
        hintRT.anchoredPosition = new Vector2(0f, 6f);
        var hintBgImg = hintGO.GetComponent<Image>();
        hintBgImg.color = new Color(0f, 0f, 0f, 0.001f); // 거의 투명 — Raycast 만 받음
        // 클릭 시 폰 토글 (홈에서는 닫기, 패널에서는 홈 복귀)
        hintGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (SmartphoneUI.instance != null) SmartphoneUI.instance.OnEscape();
        });
        var hintTxGO = AddChild(hintGO.transform, "Text", typeof(TextMeshProUGUI));
        Stretch((RectTransform)hintTxGO.transform);
        var hintTx = hintTxGO.GetComponent<TextMeshProUGUI>();
        hintTx.text      = "[P / ESC] 닫기";
        hintTx.fontSize  = 14;
        hintTx.alignment = TextAlignmentOptions.Center;
        hintTx.color     = new Color(1f, 1f, 1f, 0.85f);
        hintTx.raycastTarget = false;

        // ── 우상단 [×] 닫기 버튼 (스크린 우상단 고정 — 명시적 닫기)
        var closeBtnGO = AddChild(screenGO.transform, "CloseButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        var closeBtnRT = (RectTransform)closeBtnGO.transform;
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot     = new Vector2(1f, 1f);
        closeBtnRT.sizeDelta = new Vector2(36, 36);
        closeBtnRT.anchoredPosition = new Vector2(-6, -46); // 상태바(40) 아래 6px
        closeBtnGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
        closeBtnGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (SmartphoneUI.instance != null) SmartphoneUI.instance.Close();
        });
        var closeTxGO = AddChild(closeBtnGO.transform, "X", typeof(TextMeshProUGUI));
        Stretch((RectTransform)closeTxGO.transform);
        var closeTx = closeTxGO.GetComponent<TextMeshProUGUI>();
        closeTx.text      = "×";
        closeTx.fontSize  = 28;
        closeTx.fontStyle = FontStyles.Bold;
        closeTx.alignment = TextAlignmentOptions.Center;
        closeTx.color     = Color.white;
        closeTx.raycastTarget = false;

        // ── 앱 컨텐츠 영역 (StatusBar 와 CloseHint 사이를 stretch)
        var contentGO = AddChild(screenGO.transform, "ContentArea",
            typeof(RectTransform));
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.offsetMin = new Vector2(0f, 36f);   // CloseHint(28) + 8px 여백
        contentRT.offsetMax = new Vector2(0f, -48f);  // StatusBar(40) + 8px 여백

        // 4개 앱 정의 (감사/채용/피드/설정)
        // ⚠ v3: 이모지(📊🤝📱⚙) 제거 → SDF 폰트 미지원으로 □ 박스가 떠서 시각이 깨짐.
        //   대신 큰 한글 글자(Jalnan2 가 잘 표현) 를 아이콘 한가운데 배치.
        var appDefs = new (string name, string label, string color1Letter, Color color, string placeholder, string iconPath)[]
        {
            ("AuditPanel",    "감사", "감", C_AppAudit,   "감사 시스템 준비 중", SPR_ICON_AUDIT),
            ("HiringPanel",   "채용", "채", C_AppHiring,  "채용 시스템 준비 중", SPR_ICON_HIRING),
            ("FeedPanel",     "피드", "피", C_AppFeed,    "피드 시스템 준비 중", SPR_ICON_FEED),
            ("SettingsPanel", "설정", "설", C_AppSetting, "설정 준비 중",       SPR_ICON_SETTINGS),
        };

        var panels  = new GameObject[appDefs.Length];
        var buttons = new Button[appDefs.Length];

        // Home 화면 (앱 그리드) — ContentArea stretch.
        // 비율 정상화 노트:
        //   • 폰 사이즈 380x680, ContentArea ≈ 380x550.
        //   • 2x2 그리드에 적당한 여백을 두면 셀 ≈ 140x140 (정사각).
        //   • cellSize 의 height 를 기존 170 → 144 로 축소하여 아이콘이
        //     가로/세로 비율 ≈ 1:1 + 라벨 공간 확보. 찌그러짐 제거.
        var homeGO = AddChild(contentGO.transform, "HomeScreen",
            typeof(RectTransform), typeof(GridLayoutGroup));
        Stretch((RectTransform)homeGO.transform);
        var homeGrid = homeGO.GetComponent<GridLayoutGroup>();
        homeGrid.cellSize   = new Vector2(140, 168); // 아이콘 110 + 라벨 24 + 여백
        homeGrid.spacing    = new Vector2(20, 24);
        homeGrid.padding    = new RectOffset(24, 24, 28, 24);
        homeGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        homeGrid.constraintCount = 2;
        homeGrid.childAlignment = TextAnchor.UpperCenter;

        for (int i = 0; i < appDefs.Length; i++)
        {
            var d = appDefs[i];

            // 앱 타일 (세로 VLG: 아이콘 + 라벨)
            var appGO = AddChild(homeGO.transform, d.name + "_Tile",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            var appVLG = appGO.GetComponent<VerticalLayoutGroup>();
            appVLG.spacing                = 6;
            appVLG.childAlignment         = TextAnchor.UpperCenter;
            appVLG.childForceExpandWidth  = true;
            appVLG.childForceExpandHeight = false;
            appVLG.childControlWidth      = true;
            appVLG.childControlHeight     = true;

            // 아이콘 버튼 (정사각 둥근 컬러 사각형)
            // ⚠ 비율 고정: AspectRatioFitter 사용 → 부모 너비를 따라가도 정사각 유지.
            var iconGO = AddChild(appGO.transform, "Icon",
                typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement), typeof(AspectRatioFitter));
            var iconLE = iconGO.GetComponent<LayoutElement>();
            iconLE.preferredHeight = 110;
            iconLE.preferredWidth  = 110;
            var iconAR = iconGO.GetComponent<AspectRatioFitter>();
            iconAR.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            iconAR.aspectRatio = 1f;
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.color = d.color;
            iconImg.type  = Image.Type.Sliced;
            ApplySprite(iconImg, SPR_APP_ICON_BASE);
            buttons[i] = iconGO.GetComponent<Button>();

            var appIconSpr = TryLoadSprite(d.iconPath);
            if (appIconSpr != null)
            {
                var iconFgGO = AddChild(iconGO.transform, "IconFG",
                    typeof(RectTransform), typeof(Image));
                Stretch((RectTransform)iconFgGO.transform, 14f);
                var iconFgImg = iconFgGO.GetComponent<Image>();
                iconFgImg.sprite         = appIconSpr;
                iconFgImg.preserveAspect = true;     // ⚠ 비율 보존
                iconFgImg.raycastTarget  = false;
            }
            else
            {
                // 이모지 대신 큰 한글 1글자 (Jalnan2 톤)
                var bigGO = AddChild(iconGO.transform, "BigLetter", typeof(TextMeshProUGUI));
                Stretch((RectTransform)bigGO.transform);
                var bigTx = bigGO.GetComponent<TextMeshProUGUI>();
                bigTx.text      = d.color1Letter;
                bigTx.fontSize  = 56;
                bigTx.fontStyle = FontStyles.Bold;
                bigTx.alignment = TextAlignmentOptions.Center;
                bigTx.color     = Color.white;
                bigTx.raycastTarget = false;
            }

            // 라벨 (아이콘 아래)
            var labelGO = AddChild(appGO.transform, "Label",
                typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGO.GetComponent<LayoutElement>().preferredHeight = 24;
            var labelTx = labelGO.GetComponent<TextMeshProUGUI>();
            labelTx.text      = d.label;
            labelTx.fontSize  = 18;
            labelTx.fontStyle = FontStyles.Bold;
            labelTx.alignment = TextAlignmentOptions.Center;
            labelTx.color     = new Color(1f, 0.98f, 0.9f, 1f);
            labelTx.raycastTarget = false;

            // 앱 상세 패널 (ContentArea 자식, 기본 비활성)
            var panelGO = AddChild(contentGO.transform, d.name,
                typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)panelGO.transform);
            panelGO.GetComponent<Image>().color = new Color(0.98f, 0.94f, 0.88f, 0.98f);

            // 패널 헤더 ("← 홈" 뒤로 가기 버튼)
            var backGO = AddChild(panelGO.transform, "BackButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            var backRT = (RectTransform)backGO.transform;
            backRT.anchorMin = new Vector2(0f, 1f);
            backRT.anchorMax = new Vector2(0f, 1f);
            backRT.pivot     = new Vector2(0f, 1f);
            backRT.sizeDelta = new Vector2(80, 32);
            backRT.anchoredPosition = new Vector2(8, -8);
            backGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            var backTxGO = AddChild(backGO.transform, "Text", typeof(TextMeshProUGUI));
            Stretch((RectTransform)backTxGO.transform);
            var backTx = backTxGO.GetComponent<TextMeshProUGUI>();
            backTx.text      = "← 홈";
            backTx.fontSize  = 16;
            backTx.fontStyle = FontStyles.Bold;
            backTx.alignment = TextAlignmentOptions.Center;
            backTx.color     = Color.white;

            // 패널 본문 placeholder
            var phTxGO = AddChild(panelGO.transform, "Placeholder", typeof(TextMeshProUGUI));
            var phRT = (RectTransform)phTxGO.transform;
            phRT.anchorMin = new Vector2(0f, 0f);
            phRT.anchorMax = new Vector2(1f, 1f);
            phRT.offsetMin = new Vector2(20, 20);
            phRT.offsetMax = new Vector2(-20, -50); // 헤더 아래
            var phTx = phTxGO.GetComponent<TextMeshProUGUI>();
            phTx.text      = d.placeholder;
            phTx.fontSize  = 22;
            phTx.alignment = TextAlignmentOptions.Center;
            phTx.color     = new Color(0.2f, 0.2f, 0.2f, 1f);

            panelGO.SetActive(false); // 시작은 홈 화면만 노출
            panels[i] = panelGO;

            // 뒤로가기 → SmartphoneUI.ReturnToHome (모든 패널 비활성)
            var backBtn = backGO.GetComponent<Button>();
            backBtn.onClick.AddListener(() =>
            {
                if (SmartphoneUI.instance != null) SmartphoneUI.instance.ReturnToHome();
            });
        }

        var ui = go.AddComponent<SmartphoneUI>();
        return new PhoneCtx
        {
            ui           = ui,
            root         = rt,
            hoverTrigger = rt,
            homeScreen   = homeGO,
            tabPanels    = panels,
            tabButtons   = buttons,
        };
    }

    // 상태바: 스크린 최상단 anchored 고정. 좌=DT 로고 / 가운데=시각 / 우측=배터리 텍스트.
    // ⚠ v3: 이모지(📶🔋) 제거 → SDF 미지원으로 □ 표시되던 부분을 텍스트 "100%" 로.
    //   이전 LayoutElement+VLG 자식 의존을 제거하고 anchored 위치로 직접 고정.
    static void BuildStatusBar(Transform screenParent)
    {
        var barGO = AddChild(screenParent, "StatusBar",
            typeof(RectTransform), typeof(Image));
        var barRT = (RectTransform)barGO.transform;
        // 스크린 최상단 안쪽 stretch — 높이 40
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot     = new Vector2(0.5f, 1f);
        barRT.sizeDelta = new Vector2(0f, 40f);
        barRT.anchoredPosition = new Vector2(0f, 0f);

        var barImg = barGO.GetComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.20f);
        barImg.type  = Image.Type.Sliced;
        barImg.raycastTarget = false;
        ApplySprite(barImg, SPR_PHONE_STATUSBAR);

        // 좌측: DT 로고 (영문 — 폰트 호환 OK)
        var leftGO = AddChild(barGO.transform, "Logo",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        var leftRT = (RectTransform)leftGO.transform;
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.3f, 1f);
        leftRT.offsetMin = new Vector2(12f, 0f);
        leftRT.offsetMax = Vector2.zero;
        var leftTx = leftGO.GetComponent<TextMeshProUGUI>();
        leftTx.text      = "DT";
        leftTx.fontSize  = 16;
        leftTx.fontStyle = FontStyles.Bold;
        leftTx.alignment = TextAlignmentOptions.MidlineLeft;
        leftTx.color     = Color.white;

        // 가운데: 시각 (런타임에 GameClock 으로 갱신 가능 — 이름 'Time' 으로 검색)
        var timeGO = AddChild(barGO.transform, "Time",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        var timeRT = (RectTransform)timeGO.transform;
        timeRT.anchorMin = new Vector2(0.3f, 0f);
        timeRT.anchorMax = new Vector2(0.7f, 1f);
        timeRT.offsetMin = Vector2.zero;
        timeRT.offsetMax = Vector2.zero;
        var timeTx = timeGO.GetComponent<TextMeshProUGUI>();
        timeTx.text      = "06:00 AM";
        timeTx.fontSize  = 16;
        timeTx.alignment = TextAlignmentOptions.Center;
        timeTx.color     = Color.white;

        // 우측: 배터리 (이모지 → 텍스트 "100%" 로 대체)
        var rightGO = AddChild(barGO.transform, "Battery",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        var rightRT = (RectTransform)rightGO.transform;
        rightRT.anchorMin = new Vector2(0.7f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = Vector2.zero;
        rightRT.offsetMax = new Vector2(-12f, 0f);
        var rightTx = rightGO.GetComponent<TextMeshProUGUI>();
        rightTx.text      = "100%";
        rightTx.fontSize  = 14;
        rightTx.fontStyle = FontStyles.Bold;
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
            SetRef(so, "homeScreen",   phone.homeScreen); // ← 신규: 홈 비활성/복귀 토글용

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
    static void EnsureSlotPrefabStyle()
    {
        if (!File.Exists(UI_SLOT_PREFAB_PATH)) return;

        var slotSprite = TryLoadSprite(SPR_INV_SLOT);
        var root = PrefabUtility.LoadPrefabContents(UI_SLOT_PREFAB_PATH);
        if (root == null) return;

        bool changed = false;
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject.name != "UI_Slot") continue;

            if (img.sprite != slotSprite)
            {
                img.sprite = slotSprite;
                changed = true;
            }

            if (img.type != Image.Type.Simple)
            {
                img.type = Image.Type.Simple;
                changed = true;
            }

            if (img.color != Color.white)
            {
                img.color = Color.white;
                changed = true;
            }

            img.preserveAspect = true;
        }

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!tmp.gameObject.name.Contains("Count")) continue;
            var target = new Color(0.18f, 0.14f, 0.10f, 1f);
            if (tmp.color != target)
            {
                tmp.color = target;
                changed = true;
            }
        }

        if (changed)
            PrefabUtility.SaveAsPrefabAsset(root, UI_SLOT_PREFAB_PATH);

        PrefabUtility.UnloadPrefabContents(root);
    }

    static Sprite TryLoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr == null && TryGenerateFallbackSprite(path))
            spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr == null && _missingSprCache.Add(path))
            Debug.LogWarning($"🎨 스프라이트 미발견: {path} — 컬러 플랫으로 폴백. Nano Banana 출력을 이 경로에 저장 후 재빌드.");
        return spr;
    }

    // Image 에 스프라이트를 안전하게 주입. null 이면 sprite 를 비우고 9-slice 도 유지.
    static bool TryGenerateFallbackSprite(string path)
    {
        int width;
        int height;
        float radius;
        Color color;
        Vector4 border;
        bool shadow = false;

        switch (path)
        {
            case SPR_INV_PANEL:
                width = 512; height = 192; radius = 78f;
                color = new Color(1.00f, 0.985f, 0.925f, 1f);
                border = new Vector4(78, 78, 78, 78);
                break;
            case SPR_DROPSHADOW:
                width = 512; height = 192; radius = 78f;
                color = new Color(0f, 0f, 0f, 0.34f);
                border = new Vector4(78, 78, 78, 78);
                shadow = true;
                break;
            case SPR_INV_SLOT:
                width = 128; height = 128; radius = 46f;
                color = new Color(0.72f, 0.66f, 0.54f, 0.34f);
                border = new Vector4(44, 44, 44, 44);
                break;
            case SPR_APP_ICON_BASE:
                width = 128; height = 128; radius = 28f;
                color = Color.white;
                border = new Vector4(28, 28, 28, 28);
                break;
            case SPR_HOTBAR_BG:
                width = 512; height = 96; radius = 18f;
                color = Color.white;
                border = new Vector4(18, 18, 18, 18);
                break;
            case SPR_PHONE_BODY:
                width = 256; height = 512; radius = 32f;
                color = Color.white;
                border = new Vector4(32, 32, 32, 32);
                break;
            case SPR_PHONE_BEZEL:
                width = 228; height = 476; radius = 22f;
                color = Color.white;
                border = new Vector4(22, 22, 22, 22);
                break;
            case SPR_PHONE_SCREEN:
                width = 212; height = 448; radius = 18f;
                color = Color.white;
                border = new Vector4(18, 18, 18, 18);
                break;
            case SPR_PHONE_STATUSBAR:
                width = 212; height = 44; radius = 12f;
                color = Color.white;
                border = new Vector4(12, 12, 12, 12);
                break;
            default:
                return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (!File.Exists(path))
        {
            var texture = BuildRoundedTexture(width, height, radius, color, shadow);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteImporter(path, border);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return true;
    }

    static Texture2D BuildRoundedTexture(int width, int height, float radius, Color color, bool shadow)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "PA_GeneratedUISprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sd = RoundedRectSignedDistance(x + 0.5f, y + 0.5f, width, height, radius);
                float alpha = shadow
                    ? Mathf.Clamp01(1f - Mathf.Max(0f, sd) / 26f) * color.a
                    : (sd < -1f ? 1f : sd > 1f ? 0f : 0.5f - sd * 0.5f) * color.a;

                texture.SetPixel(x, y, alpha <= 0f ? clear : new Color(color.r, color.g, color.b, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    static float RoundedRectSignedDistance(float x, float y, float width, float height, float radius)
    {
        Vector2 p = new Vector2(x - width * 0.5f, y - height * 0.5f);
        Vector2 q = new Vector2(
            Mathf.Abs(p.x) - (width * 0.5f - radius),
            Mathf.Abs(p.y) - (height * 0.5f - radius));

        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
    }

    static void ConfigureSpriteImporter(string path, Vector4 border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = border;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

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
