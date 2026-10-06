using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    // ⭐ [추가] 외부에서 접근 가능하도록 싱글톤 설정
    public static InventoryUI instance;

    public Inventory inventory;
    public Hotbar hotbar;
    public Transform slotParent;
    public GameObject slotPrefab;
    public ItemTooltip tooltip;

    public RectTransform dragLayer;
    public Canvas rootCanvas;

    private List<InventorySlotUI> slotUIs;
    TMPro.TMP_Text _demoTitle;
    bool _demoPresented;

    void Awake()
    {
        // ⭐ [추가] 싱글톤 초기화
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void OnEnable()
    {
        BindRuntimeReferences();
        RefreshUI();
    }

    void Start()
    {
        BindRuntimeReferences();
        if (inventory == null || hotbar == null || slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("InventoryUI: missing inventory, hotbar, slotParent, or slotPrefab.");
            gameObject.SetActive(false);
            return;
        }

        slotUIs = new List<InventorySlotUI>();
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        for (int i = 0; i < inventory.size; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotParent);
            var slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.tooltip = tooltip;
            slotUI.Setup(inventory, hotbar, i, this);
            slotUIs.Add(slotUI);
        }

        RefreshUI();

        // ⭐ [추가] 시작하자마자 화면에서 숨기기!
        gameObject.SetActive(false);
    }

    public void RefreshUI()
    {
        BindRuntimeReferences();
        if (slotUIs == null || inventory == null) return;
        ApplyFirstDayPresentation();
        for (int i = 0; i < inventory.size; i++)
        {
            if (i >= slotUIs.Count) continue;
            if (PAUiTheme.Active && tooltip == null) tooltip = PAUiTheme.EnsureTooltip(rootCanvas);
            slotUIs[i].tooltip = tooltip;
            slotUIs[i].SetSlot(inventory.slots[i]);
            if (_demoPresented && slotUIs[i].TryGetComponent(out UnityEngine.UI.Image surface))
                surface.color = inventory.slots[i].IsEmpty
                    ? PAUiTheme.EmptySlot : PAUiTheme.FilledSlot;
        }
        if (_demoTitle != null)
            _demoTitle.text = $"가방  ·  {inventory.slots.FindAll(s => !s.IsEmpty).Count}/{inventory.size}";
    }

    // Opening Demo presentation only; the existing slots and drag ownership stay in this UI.
    void ApplyFirstDayPresentation()
    {
        if (_demoPresented || PlayerInputHandler.Instance?.FirstDayControls != true ||
            slotParent != transform || !(transform is RectTransform panel)) return;
        var grid = GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null) return;
        _demoPresented = true;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(960f, 480f);
        if (TryGetComponent(out UnityEngine.UI.Image background)) PAUiTheme.Surface(background, PAUiTheme.Cream);
        grid.cellSize = new Vector2(96f, 96f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(26, 26, 100, 60);
        grid.childAlignment = TextAnchor.UpperCenter;

        var header = new GameObject("BagHeader", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
        header.transform.SetParent(transform, false);
        header.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var headerRect = (RectTransform)header.transform;
        headerRect.anchorMin = new Vector2(0f, 1f); headerRect.anchorMax = Vector2.one;
        headerRect.pivot = new Vector2(.5f, 1f); headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 78f);
        PAUiTheme.Surface(header.GetComponent<UnityEngine.UI.Image>(), PAUiTheme.Teal);
        _demoTitle = BagText(header.transform, "Title", new Vector2(28f, -10f), new Vector2(660f, 58f), 32f, Color.white);
        _demoTitle.fontStyle = TMPro.FontStyles.Bold;

        var close = new GameObject("BagClose", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        close.transform.SetParent(header.transform, false);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, .5f);
        closeRect.pivot = new Vector2(1f, .5f); closeRect.anchoredPosition = new Vector2(-24f, 0f);
        closeRect.sizeDelta = new Vector2(148f, 44f);
        close.GetComponent<UnityEngine.UI.Image>().color = new Color(.44f, .58f, .57f);
        PAUiTheme.Button(close.GetComponent<UnityEngine.UI.Button>(), PAUiTheme.Muted);
        close.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Toggle);
        var closeText = BagText(close.transform, "Label", new Vector2(8f, 0f), new Vector2(132f, 44f), 20f, Color.white);
        closeText.alignment = TMPro.TextAlignmentOptions.Center; closeText.text = "닫기  Tab";

        var footer = BagText(transform, "BagControls", new Vector2(28f, -432f), new Vector2(900f, 38f), 18f, PAUiTheme.Ink);
        footer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        footer.text = "드래그해서 옮기기  ·  Shift + 드래그로 1개 옮기기  ·  Tab으로 닫기";
    }

    static TMPro.TMP_Text BagText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = fontSize; text.color = color; text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    public void Rebind(Inventory playerInventory, Hotbar playerHotbar)
    {
        if (playerInventory != null) inventory = playerInventory;
        hotbar = playerInventory != null && playerInventory.hotbar != null
            ? playerInventory.hotbar
            : playerHotbar;
        if (inventory != null && inventory.hotbar == null && hotbar != null)
            inventory.hotbar = hotbar;
        RefreshUI();
    }

    // ⭐ [추가] 껐다 켰다 하는 함수
    public void Toggle()
    {
        bool isActive = !gameObject.activeSelf; // 현재 상태의 반대로
        gameObject.SetActive(isActive);

        if (isActive)
        {
            RefreshUI(); // 켤 때 갱신 한 번 해줌

            // 📱 상호배타: 스마트폰이 열려있으면 강제로 닫는다 (Docs §레퍼런스.html)
            if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
                SmartphoneUI.instance.Toggle();

            // 인벤토리 열리면 마우스 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 닫으면 마우스 숨기고 게임으로 돌아가기
            PlayerInputHandler.RestoreGameplayCursor();
            
            // 툴팁도 같이 꺼주기 (혹시 켜져있을까봐)
            if(tooltip != null) tooltip.Hide();
        }
    }

    void BindRuntimeReferences()
    {
        if (Inventory.instance != null)
            inventory = Inventory.instance;
        else if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        if (inventory != null && inventory.hotbar != null)
            hotbar = inventory.hotbar;
        else if (hotbar == null)
            hotbar = FindAnyObjectByType<Hotbar>();

        if (inventory != null && inventory.hotbar == null && hotbar != null)
            inventory.hotbar = hotbar;
    }
}
