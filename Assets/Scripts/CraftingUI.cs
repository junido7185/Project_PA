using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 제작 패널. C 키는 모든 기존 레시피와 필요한 작업대를 보여 주는 읽기 전용 도감,
// Workbench.Interact는 해당 작업대에서 실제 제작하는 제품 진입점이다.
// 재료 차감·결과 생성의 단일 권위는 계속 CraftingService.TryCraft에 둔다.
//
// 2026-11-02 런타임(데모) 패널 2단 구성(Docs/01_GAME_DESIGN/Content/CRAFTING_STATIONS.md, Docs/01_GAME_DESIGN/UI/DESIGN.md):
// 왼쪽 레시피 목록(행 클릭 = 선택), 오른쪽 상세(큰 아이콘·설명·쓰임·재료 보유/필요·[만들기]).
// 사용자 피드백 "쓸데 없는 생산물": 상세의 '쓰임'이 판매·설치·다른 레시피 재료·도구·동료 선물을 실제 데이터에서 읽어 보여 준다.
// 데모 경로(첫날 조작)에서 얻을 수 없는 농작물 재료 레시피는 숨기고, 같은 결과물 레시피는 재료 이름으로 구분한다.
// 씬에 직렬화된 기존 패널(레거시 MainGame)은 카드 클릭 = 제작을 그대로 쓴다.
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI instance;

    [Header("UI 참조")]
    public GameObject craftingPanel;
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("표시 옵션")]
    [Tooltip("티어/워크샵으로 잠긴 레시피를 회색으로 표시할 색상")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private RecipeData[] allRecipes;
    private Workbench _activeWorkbench;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _modeText;
    private TextMeshProUGUI _statusText;
    private PlayerInputHandler _input;
    private bool _runtimeGenerated;
    private ScrollRect _recipeScroll;
    private bool _cursorCaptured;
    private CursorLockMode _cursorLockBeforeOpen = CursorLockMode.Locked;
    private bool _cursorVisibleBeforeOpen;

    // 런타임 2단 패널
    RecipeData _selected;
    readonly List<RecipeData> _visible = new List<RecipeData>();
    readonly Dictionary<RecipeData, Image> _rows = new Dictionary<RecipeData, Image>();
    Image _detailIcon;
    TextMeshProUGUI _detailName, _detailMeta, _detailDescription, _detailUses, _craftLabel;
    public string DetailUsesText => _detailUses != null ? _detailUses.text : ""; // 캡처 확인용
    Transform _ingredientParent;
    Button _craftButton;
    float _popUntil = -1f;
    const float PopSeconds = .22f;

    public bool IsOpen => craftingPanel != null && craftingPanel.activeSelf;
    public bool IsRecipeBookMode => IsOpen && _activeWorkbench == null;
    public Workbench ActiveWorkbench => _activeWorkbench;
    public RecipeData SelectedRecipe => _selected;
    public Button CraftButton => _craftButton;
    // 격리 QA 촬영·검증용: 마우스 대신 목록에서 레시피를 고른다(제작은 실제 [만들기] 버튼 경로).
    public void SelectRecipe(RecipeData recipe) { if (_visible.Contains(recipe)) Select(recipe, true); }
    public IReadOnlyList<RecipeData> VisibleRecipes => _visible;

    // 디자인 토큰(Docs/01_GAME_DESIGN/UI/DESIGN.md). 공용 색은 PAUiTheme, 제작 창 전용 상태색만 여기서 정한다.
    static readonly Color SelectedRow = new Color(.99f, .93f, .74f);
    static readonly Color IdleRow = new Color(1f, .985f, .94f);
    static readonly Color DimRow = new Color(.93f, .92f, .86f);
    static readonly Color DetailSurface = new Color(1f, .99f, .96f);
    static readonly Color IconTile = new Color(.90f, .91f, .83f);
    const string ReadyHex = "#2E7356", WarnHex = "#A04A2C", MutedHex = "#5E6C66", TealHex = "#2D6E68";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // PA_RuntimeUI의 다른 UI 컴포넌트까지 파괴하지 않는다.
            Destroy(this);
            return;
        }

        instance = this;
        allRecipes = Resources.LoadAll<RecipeData>("Recipes").Concat(Resources.LoadAll<RecipeData>("DemoStructure")).Distinct().ToArray();
        System.Array.Sort(allRecipes, CompareRecipes);

        if (craftingPanel == null || slotParent == null || slotPrefab == null)
            BuildRuntimeUI();
    }

    void Start()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);

        _input = PlayerInputHandler.Instance;
        if (_input == null) return;

        _input.OnCraftToggle += ToggleUI;
        _input.OnInventoryToggle += CloseForOtherPanel;
        _input.OnPhoneToggle += CloseForOtherPanel;
    }

    void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnCraftToggle -= ToggleUI;
            _input.OnInventoryToggle -= CloseForOtherPanel;
            _input.OnPhoneToggle -= CloseForOtherPanel;
        }

        if (_cursorCaptured) RestoreCursor();
        if (instance == this) instance = null;
    }

    // 목록 이동(W/S·↑/↓)과 만들기(Enter·Space). 열려 있는 동안 이동·점프 입력은 PlayerInputHandler.ModalOpen이 막는다.
    void Update()
    {
        if (!_runtimeGenerated || !IsOpen) return;
        if (_detailIcon != null)
        {
            float left = _popUntil - Time.unscaledTime;
            float t = left > 0f ? 1f - left / PopSeconds : 1f;
            _detailIcon.rectTransform.localScale = Vector3.one * (1f + .14f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        }
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || _visible.Count == 0) return;
        int step = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ? 1
            : kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame ? -1 : 0;
        if (step != 0)
        {
            int index = Mathf.Clamp(_visible.IndexOf(_selected) + step, 0, _visible.Count - 1);
            Select(_visible[index], true);
        }
        if ((kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            && _craftButton != null && _craftButton.interactable)
            CraftSelected();
    }

    // -------- 외부 진입점 --------

    // Workbench.Interact 에서 호출. 해당 작업대 종류에 맞는 레시피만 표시한다.
    public void OpenForWorkbench(Workbench wb)
    {
        if (wb == null)
        {
            OpenRecipeBook();
            return;
        }

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        EnsureOpenState();
        _activeWorkbench = wb;
        _selected = null;
        GenerateSlotsForContext();
        if (_recipeScroll != null) _recipeScroll.verticalNormalizedPosition = 1f;
        SetStatus(_runtimeGenerated ? $"{StationName(wb)}에서 만들 것을 고르세요." : $"{wb.displayName}에서 만들 상품을 선택하세요.");
    }

    // C 키는 원격 제작이 아니라 전체 레시피와 요구 작업대를 확인하는 도감이다.
    public void OpenRecipeBook()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        EnsureOpenState();
        _activeWorkbench = null;
        _selected = null;
        GenerateSlotsForContext();
        if (_recipeScroll != null) _recipeScroll.verticalNormalizedPosition = 1f;
        SetStatus("도감은 확인만 할 수 있어요. 작업대 앞에서 [E]를 눌러 제작하세요.");
    }

    public void ToggleUI()
    {
        if (IsOpen) Close();
        else OpenRecipeBook();
    }

    public void Close()
    {
        Close(restoreCursor: true);
    }

    void Close(bool restoreCursor)
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);
        _activeWorkbench = null;

        if (restoreCursor) RestoreCursor();
        else _cursorCaptured = false;
    }

    void CloseForOtherPanel()
    {
        if (IsOpen)
            Close(restoreCursor: false);
    }

    void EnsureOpenState()
    {
        if (craftingPanel == null || slotParent == null || slotPrefab == null)
            BuildRuntimeUI();

        if (!IsOpen)
        {
            CloseConflictingPanels();
            CaptureCursor();
        }

        craftingPanel.transform.SetAsLastSibling();
        craftingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseConflictingPanels()
    {
        if (StorageUI.instance != null && StorageUI.instance.IsOpen)
            StorageUI.instance.CloseBox();

        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
            SmartphoneUI.instance.Toggle();

        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)
            InventoryUI.instance.Toggle();
    }

    void CaptureCursor()
    {
        _cursorLockBeforeOpen = Cursor.lockState;
        _cursorVisibleBeforeOpen = Cursor.visible;
        _cursorCaptured = true;
    }

    void RestoreCursor()
    {
        if (!_cursorCaptured) return;
        Cursor.lockState = _cursorLockBeforeOpen;
        Cursor.visible = _cursorVisibleBeforeOpen;
        _cursorCaptured = false;
    }

    // -------- 슬롯 생성 (컨텍스트 필터 적용) --------

    void GenerateSlotsForContext()
    {
        if (slotParent == null || slotPrefab == null) return;

        float scrollPosition = _recipeScroll != null ? _recipeScroll.verticalNormalizedPosition : 1f;
        // Destroy는 프레임 끝에 실행된다. 먼저 숨겨서 재제작 때 이전 카드가 레이아웃에 섞이지 않게 한다.
        foreach (Transform child in slotParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        _rows.Clear();
        _visible.Clear();

        if (allRecipes == null) return;

        _visible.AddRange(allRecipes.Where(r => r != null && PassesRouteFilter(r) && PassesWorkbenchFilter(r)));
        if (_runtimeGenerated)
        {
            // 묶음(재료→요리→상품→공예품→가구→도구→설비) 순서. 같은 묶음 안에서는 기존 정렬(작업대·등급·이름)을 유지한다.
            var order = _visible.Select((r, i) => (r, i)).OrderBy(p => GroupOrder(GroupOf(p.r))).ThenBy(p => p.i).Select(p => p.r).ToList();
            _visible.Clear(); _visible.AddRange(order);
        }

        foreach (RecipeData recipe in _visible)
        {
            bool unlocked = IsUnlocked(recipe);
            bool hasIngredients = HasAllIngredients(recipe);
            bool canCraftHere = _activeWorkbench != null && ContextMatches(recipe, _activeWorkbench);

            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            newSlot.SetActive(true);
            newSlot.name = $"Recipe_{recipe.name}";

            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>(true);
            if (slotText != null)
                slotText.text = _runtimeGenerated ? BuildRowLabel(recipe, unlocked, hasIngredients, canCraftHere)
                    : BuildSlotLabel(recipe, unlocked, hasIngredients, canCraftHere);

            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform == null) iconTransform = newSlot.transform.Find("IconTile/Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
            {
                icon.sprite = IconOf(recipe);
                icon.enabled = icon.sprite != null;
            }

            Button btn = newSlot.GetComponent<Button>();
            RecipeData captured = recipe;
            if (_runtimeGenerated)
            {
                // 행 클릭 = 선택. 제작은 상세의 [만들기]가 한다(재료·쓰임을 보고 고르게).
                if (newSlot.TryGetComponent(out Image row)) _rows[recipe] = row;
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => Select(captured, false));
                }
            }
            else
            {
                if (newSlot.TryGetComponent(out Image card))
                    card.color = ResolveCardColor(unlocked, hasIngredients, canCraftHere);
                if (btn != null)
                {
                    btn.interactable = unlocked && hasIngredients && canCraftHere;
                    Workbench wbCaptured = _activeWorkbench;
                    btn.onClick.AddListener(() => TryCraftRecipe(captured, wbCaptured));
                }
            }
        }

        // Runtime-generated cards are created while the panel is being opened. Force the
        // layout now so the first visible frame has non-zero content/card geometry.
        Canvas.ForceUpdateCanvases();
        if (slotParent is RectTransform contentRect)
        {
            // 긴 재료/잠금 설명도 카드 안에 들어가도록 실제 텍스트 폭으로 높이를 계산한다.
            foreach (Transform child in slotParent)
            {
                TMP_Text label = child.GetComponentInChildren<TMP_Text>(true);
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (label == null || element == null) continue;
                float width = Mathf.Max(1f, label.rectTransform.rect.width);
                element.preferredHeight = Mathf.Max(_runtimeGenerated ? 88f : 112f, label.GetPreferredValues(label.text, width, 0f).y + (_runtimeGenerated ? 22f : 28f));
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
        Canvas.ForceUpdateCanvases();
        if (_recipeScroll != null) _recipeScroll.verticalNormalizedPosition = scrollPosition;

        if (_runtimeGenerated)
        {
            if (_selected == null || !_visible.Contains(_selected))
                _selected = _visible.FirstOrDefault(r => CanCraft(r)) ?? _visible.FirstOrDefault();
            RefreshSelection();
        }

        if (_titleText != null)
            _titleText.text = _activeWorkbench == null
                ? "제작 도감"
                : _runtimeGenerated ? StationName(_activeWorkbench) : $"{_activeWorkbench.displayName} 제작";

        if (_modeText != null)
            _modeText.text = _runtimeGenerated
                ? (_activeWorkbench == null ? $"레시피 {_visible.Count}개 · 확인 전용 · 작업대 앞에서 [E]로 만들어요"
                    : $"{StationRole(_activeWorkbench.workbenchType)} · 레시피 {_visible.Count}개")
                : _activeWorkbench == null
                ? $"레시피 {_visible.Count}개 · 확인 전용 · 작업대에서 [E]로 제작"
                : $"{GetWorkbenchLabel(_activeWorkbench.workbenchType)} · 레시피 {_visible.Count}개 · 재료 보유 / 필요";
    }

    // 컨텍스트(작업대 종류)에 맞는지 판정한다.
    bool PassesWorkbenchFilter(RecipeData recipe)
    {
        // 도감은 전체 레시피를 보여 주되 버튼을 비활성화해 원격 제작을 차단한다.
        if (_activeWorkbench == null)
            return true;

        return ContextMatches(recipe, _activeWorkbench);
    }

    // 데모(첫날 조작) 경로는 농사를 끈다. 밀·당근·씨앗이 필요한 레시피는 얻을 길이 없는 '죽은 카드'라 숨긴다
    // (Content-crafting-plan D: "데모 밖 레시피는 숨긴다"). 등급 잠금은 성장 신호라 그대로 보인다.
    static readonly HashSet<int> FarmOnlyItems = new HashSet<int> { 1, 2, 15 };
    static bool PassesRouteFilter(RecipeData recipe)
    {
        if (!PAUiTheme.Active || recipe.ingredients == null) return true;
        // 도구 단계(TOOL_PROGRESSION.md): 데모에서 철 주괴는 철 광석으로만 녹인다. 옛 '광석 2 → 철괴' 레시피는 숨긴다.
        if (recipe.outputItem != null && recipe.outputItem.id == 6 && recipe.ingredients.Any(i => i?.item != null && i.item.id == 4)) return false;
        return !recipe.ingredients.Any(i => i != null && i.item != null && FarmOnlyItems.Contains(i.item.id));
    }

    string BuildSlotLabel(RecipeData recipe, bool unlocked, bool hasIngredients, bool canCraftHere)
    {
        string recipeName = TitleOf(recipe);
        string station = GetWorkbenchLabel(recipe.requiredWorkbench);
        string ingredients = BuildIngredientText(recipe);
        string state = !unlocked ? $"<color=#F0B39E>잠김 · {BuildLockReason(recipe)}</color>"
            : !canCraftHere ? "<color=#C8D8CA>도감 · 작업대에서 제작하세요</color>"
            : !hasIngredients ? "<color=#F0B39E>재료 부족</color>"
            : "<color=#CDE7C9>제작 가능 · 클릭하여 만들기</color>";

        return $"<b>{recipeName}</b>  <size=85%>완성 ×{recipe.outputCount}</size>\n" +
               $"<size=85%>{state}</size>\n" +
               $"<size=85%><color=#C8D8CA>재료 (보유/필요): {ingredients}</color></size>\n" +
               $"<size=80%><color=#91B69A>{station}</color></size>";
    }

    // 목록 행: 이름 + 상태(제작 가능/재료 부족/잠김 이유) + 묶음. 상태 문구는 검증기가 읽는 계약이다.
    string BuildRowLabel(RecipeData recipe, bool unlocked, bool hasIngredients, bool canCraftHere)
    {
        string state = !unlocked ? $"<color={WarnHex}>잠김 · {BuildLockReason(recipe)}</color>"
            : !canCraftHere ? $"<color={MutedHex}>{StationLabel(recipe.requiredWorkbench)}에서 제작</color>"
            : !hasIngredients ? $"<color={WarnHex}>재료 부족</color>"
            : $"<color={ReadyHex}>제작 가능</color>";
        return $"<b>{TitleOf(recipe)}</b>\n<size=74%>{state}<color={MutedHex}>  ·  {GroupOf(recipe)}</color></size>";
    }

    string BuildIngredientText(RecipeData recipe)
    {
        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            return "재료 정보 없음";

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            if (result.Length > 0) result.Append(" · ");

            int owned = Inventory.instance != null ? Inventory.instance.CountItems(ingredient.item) : 0;
            string color = owned >= ingredient.count ? "#CDE7C9" : "#F0B39E";
            result.Append($"<color={color}>{ItemDisplayName.For(ingredient.item)} {owned}/{ingredient.count}</color>");
        }

        return result.Length > 0 ? result.ToString() : "재료 정보 없음";
    }

    // -------- 선택·상세(런타임) --------

    void Select(RecipeData recipe, bool scrollIntoView)
    {
        if (recipe == null) return;
        _selected = recipe;
        RefreshSelection();
        if (scrollIntoView && _recipeScroll != null && _visible.Count > 1)
            _recipeScroll.verticalNormalizedPosition = 1f - _visible.IndexOf(recipe) / (float)(_visible.Count - 1);
    }

    void RefreshSelection()
    {
        foreach (var pair in _rows)
        {
            if (pair.Value == null) continue;
            bool ready = CanCraft(pair.Key) || _activeWorkbench == null && IsUnlocked(pair.Key) && HasAllIngredients(pair.Key);
            pair.Value.color = pair.Key == _selected ? SelectedRow : ready ? IdleRow : DimRow;
        }
        RefreshDetail();
    }

    void RefreshDetail()
    {
        if (_detailName == null) return;
        var recipe = _selected;
        foreach (Transform child in _ingredientParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (recipe == null)
        {
            _detailIcon.enabled = false;
            _detailName.text = "만들 수 있는 것이 없어요";
            _detailMeta.text = "";
            _detailDescription.text = "재료를 모으면 여기에 레시피가 보여요.";
            _detailUses.text = "";
            SetCraftButton(false, "고를 레시피가 없어요");
            return;
        }

        var output = recipe.outputItem;
        _detailIcon.sprite = IconOf(recipe);
        _detailIcon.enabled = _detailIcon.sprite != null;
        _detailName.text = TitleOf(recipe);
        _detailMeta.text = $"{GroupOf(recipe)}  ·  완성 ×{recipe.outputCount}  ·  {StationLabel(recipe.requiredWorkbench)}";
        _detailDescription.text = output != null && !string.IsNullOrWhiteSpace(output.description) ? output.description : "";
        var uses = UsesOf(output);
        _detailUses.text = uses.Count > 0 ? string.Join("\n", uses) : $"<color={MutedHex}>아직 쓰임이 없어요.</color>";

        if (recipe.ingredients != null)
            foreach (var ingredient in recipe.ingredients)
                if (ingredient != null && ingredient.item != null && ingredient.count > 0)
                    BuildIngredientRow(ingredient);

        bool unlocked = IsUnlocked(recipe), has = HasAllIngredients(recipe);
        bool here = _activeWorkbench != null && ContextMatches(recipe, _activeWorkbench);
        SetCraftButton(unlocked && has && here,
            !here ? $"{StationLabel(recipe.requiredWorkbench)} 앞에서 만들 수 있어요"
            : !unlocked ? "잠겨 있어요"
            : !has ? "재료가 부족해요"
            : "만들기");
        Canvas.ForceUpdateCanvases();
    }

    void SetCraftButton(bool interactable, string label)
    {
        if (_craftButton == null) return;
        _craftButton.interactable = interactable;
        _craftButton.GetComponent<Image>().color = interactable ? PAUiTheme.Teal : new Color(.80f, .81f, .75f);
        _craftLabel.text = label;
        _craftLabel.color = interactable ? Color.white : new Color(.33f, .38f, .36f);
        _craftLabel.fontSize = interactable ? 28f : 21f;
    }

    void BuildIngredientRow(RecipeIngredient ingredient)
    {
        int owned = Inventory.instance != null ? Inventory.instance.CountItems(ingredient.item) : 0;
        bool enough = owned >= ingredient.count;
        var row = PAUiTheme.Panel(_ingredientParent, "Ingredient_" + ingredient.item.name, enough ? new Color(.91f, .95f, .89f) : new Color(.99f, .91f, .85f));
        row.raycastTarget = false;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRT = (RectTransform)icon.transform;
        iconRT.SetParent(row.transform, false);
        iconRT.anchorMin = iconRT.anchorMax = iconRT.pivot = new Vector2(0f, .5f);
        iconRT.anchoredPosition = new Vector2(10f, 0f);
        iconRT.sizeDelta = new Vector2(44f, 44f);
        var image = icon.GetComponent<Image>();
        image.sprite = ingredient.item.icon; image.preserveAspect = true; image.raycastTarget = false; image.enabled = image.sprite != null;
        var name = Label(row.transform, "Name", ItemDisplayName.For(ingredient.item), 21f, PAUiTheme.Ink, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.offsetMin = new Vector2(66f, 0f); name.rectTransform.offsetMax = new Vector2(-150f, 0f);
        var count = Label(row.transform, "Count", $"<color={(enough ? ReadyHex : WarnHex)}><b>{owned}</b></color><size=80%><color={MutedHex}> / {ingredient.count}</color></size>",
            23f, PAUiTheme.Ink, TextAlignmentOptions.MidlineRight);
        count.rectTransform.offsetMin = new Vector2(0f, 0f); count.rectTransform.offsetMax = new Vector2(-16f, 0f);
    }

    void CraftSelected()
    {
        if (_selected == null) return;
        if (TryCraftRecipe(_selected, _activeWorkbench)) _popUntil = Time.unscaledTime + PopSeconds;
    }

    bool TryCraftRecipe(RecipeData recipe, Workbench workbench)
    {
        if (recipe == null || workbench == null)
        {
            SetStatus("도감에서는 제작할 수 없습니다. 필요한 작업대를 이용하세요.", true);
            return false;
        }

        bool success = CraftingService.TryCraft(recipe, workbench);
        // P6 데모: 제작 결과도 줍기처럼 핫바를 우선한다(Inventory 안에서 칸만 옮김).
        int hotbarSlot = success ? DemoHotbarPreference.Prefer(recipe.outputItem) : -1;
        GenerateSlotsForContext();

        SetStatus(success
            ? $"{ItemDisplayName.For(recipe.outputItem)} ×{recipe.outputCount} 제작 완료 · " +
              (hotbarSlot >= 0 ? $"핫바 {hotbarSlot + 1}번에서 바로 쓸 수 있어요." : "가방에서 확인하세요.")
            : BuildFailureMessage(recipe), !success);
        return success;
    }

    string BuildFailureMessage(RecipeData recipe)
    {
        if (!IsUnlocked(recipe))
            return BuildLockReason(recipe);

        if (Inventory.instance == null)
            return "플레이어 인벤토리를 찾지 못했습니다.";

        if (!HasAllIngredients(recipe))
            return "재료가 부족합니다. 카드의 보유량/필요량을 확인하세요.";

        return "제작하지 못했습니다. 작업대와 레시피 정보, 가방 공간을 확인하세요.";
    }

    // 권위의 실제 실패 순서를 그대로 읽는다. 잠김 이유는 성장 규칙을 바꾸지 않는다.
    string BuildLockReason(RecipeData recipe)
    {
        if (!DemoPlaceableCatalog.RecipeUnlocked(recipe))
        {
            var required = DemoPlaceableCatalog.RequiredRoot(recipe);
            string root = required == DemoSpecialization.None ? "해당" : GetRootLabel(required);
            return $"{root} 전문 분야를 선택해야 해요.";
        }
        if (TierService.Instance != null && !TierService.Instance.IsUnlocked(recipe.requiredTier))
            return $"본사 승인 등급 {recipe.requiredTier} 필요 · 현재 {TierService.Instance.CurrentTier}";
        if (FriendshipService.Instance != null && !FriendshipService.Instance.IsRecipeUnlocked(recipe))
            return "주민과 교류해 비법을 전수받아야 해요.";
        return "";
    }

    static string GetRootLabel(DemoSpecialization root)
    {
        switch (root)
        {
            case DemoSpecialization.Forestry: return "임업";
            case DemoSpecialization.Mining: return "광업";
            case DemoSpecialization.Fisheries: return "수산업";
            case DemoSpecialization.Agriculture: return "농업";
            default: return "해당";
        }
    }

    bool IsUnlocked(RecipeData recipe)
    {
        return DemoPlaceableCatalog.RecipeUnlocked(recipe) && (TierService.Instance == null || TierService.Instance.IsUnlocked(recipe.requiredTier))
            && (FriendshipService.Instance == null || FriendshipService.Instance.IsRecipeUnlocked(recipe));
    }

    bool HasAllIngredients(RecipeData recipe)
    {
        if (Inventory.instance == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
            return false;

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            if (!Inventory.instance.HasItems(ingredient.item, ingredient.count)) return false;
        }

        return true;
    }

    bool CanCraft(RecipeData recipe) => _activeWorkbench != null && ContextMatches(recipe, _activeWorkbench) && IsUnlocked(recipe) && HasAllIngredients(recipe);

    static bool ContextMatches(RecipeData recipe, Workbench workbench)
    {
        return recipe != null && workbench != null
            && (recipe.requiredWorkbench == WorkbenchType.None
                || recipe.requiredWorkbench == workbench.workbenchType);
    }

    // -------- 데이터에서 읽는 표시(이름·묶음·쓰임) --------

    static Sprite IconOf(RecipeData recipe) => recipe.icon != null ? recipe.icon : recipe.outputItem != null ? recipe.outputItem.icon : null;

    // 같은 결과물을 만드는 레시피가 여럿이면(생선구이 3종) 첫 재료 이름으로 구분한다.
    string TitleOf(RecipeData recipe)
    {
        if (recipe.outputItem == null)
            return string.IsNullOrWhiteSpace(recipe.recipeName) ? recipe.name : recipe.recipeName;
        string name = ItemDisplayName.For(recipe.outputItem);
        bool shared = allRecipes.Count(r => r != null && r.outputItem == recipe.outputItem && PassesRouteFilter(r)) > 1;
        var first = recipe.ingredients?.FirstOrDefault(i => i != null && i.item != null);
        return shared && first != null ? $"{name} · {ItemDisplayName.For(first.item)}" : name;
    }

    static readonly Dictionary<string, Workbench> StationPrefabs = new Dictionary<string, Workbench>();
    static Workbench StationIn(DemoPlaceableEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.buildingResource)) return null;
        if (!StationPrefabs.TryGetValue(entry.buildingResource, out var bench))
        {
            var prefab = Resources.Load<GameObject>(entry.buildingResource);
            bench = prefab != null ? prefab.GetComponentInChildren<Workbench>(true) : null;
            StationPrefabs[entry.buildingResource] = bench;
        }
        return bench;
    }

    bool UsedAsIngredient(Item item) => allRecipes.Any(r => r != null && PassesRouteFilter(r) && r.ingredients != null
        && r.ingredients.Any(i => i != null && i.item == item));

    string GroupOf(RecipeData recipe)
    {
        var item = recipe.outputItem;
        if (item == null) return "기타";
        var entry = DemoPlaceableCatalog.Load()?.Find(item);
        if (entry != null) return entry.kind == DemoPlaceableKind.Furniture && StationIn(entry) == null ? "가구" : "설비";
        if (item.toolType != ToolType.None) return "도구";
        if (recipe.requiredWorkbench == WorkbenchType.Kitchen) return "요리";
        if (UsedAsIngredient(item)) return "재료";
        return item.category == ItemCategory.Luxury ? "공예품" : "상품";
    }

    static int GroupOrder(string group)
    {
        switch (group)
        {
            case "재료": return 0;
            case "요리": return 1;
            case "상품": return 2;
            case "공예품": return 3;
            case "가구": return 4;
            case "도구": return 5;
            case "설비": return 6;
            default: return 7;
        }
    }

    static string UseLine(string label, string text) => $"<color={TealHex}><b>{label}</b></color>   {text}";

    // 산출물의 실제 쓰임. 판매(Tool 제외 가판대 규칙)·설치(배치 카탈로그)·도구(내구도)·동료 선물(전문 업그레이드)·다른 레시피 재료.
    List<string> UsesOf(Item item)
    {
        var lines = new List<string>();
        if (item == null) return lines;
        var catalog = DemoPlaceableCatalog.Load();
        var entry = catalog?.Find(item);
        if (entry != null)
        {
            var station = StationIn(entry);
            lines.Add(UseLine("설치", station != null
                ? $"정착지에 놓으면 {StationLabel(station.workbenchType)}에서 {StationRole(station.workbenchType)}을 할 수 있어요"
                // qa232: 산호빛·바다빛 의자도 ChairKeys로 손님 +1·쉼터인데 "손님 수는 그대로"로 나왔다.
                : System.Array.IndexOf(DemoSettlementController.ChairKeys, entry.key) >= 0
                    ? $"가게 안에 놓으면 그날 밤 손님이 1명 더 오고, 손님이 앉아 쉬어 가요(꾸밈 최대 {DemoSettlementController.ShopDecorCap}개)"
                    : entry.key == DemoSettlementController.ShellDecorKey
                        ? $"가게 안에 놓으면 그날 밤 손님이 1명 더 와요(의자와 합쳐 꾸밈 최대 {DemoSettlementController.ShopDecorCap}개)"
                        : "집이나 정착지·가게 바닥에 놓아 꾸며요(손님 수는 그대로)"));
        }
        // qa232: 러그는 쓰임이 없다고 나왔다 → 가게 고정 자리(러그·벽·책상) 쓰임도 보인다.
        var rug = ShopRugSwap.ForItem(item);
        if (rug != null) lines.Add(UseLine("깔기", "들고 가게 안 생활 러그 위에서 [E] — 무늬가 바뀌고, 깔려 있던 러그는 돌려받아요"));
        if (ShopWallHang.IsPlaque(item)) lines.Add(UseLine("걸기", "들고 가게 뒷벽(책상과 창문 사이) 앞에서 [E] — 벽에 걸면 손님이 알아봐요"));
        if (ShopDeskShelf.Accepts(item)) lines.Add(UseLine("소장", "들고 가게 책상 앞에서 [E] — 램프 옆에 올려 두면 손님이 알아봐요"));
        if (item.toolType != ToolType.None)
        {
            string verb = item.toolType == ToolType.Axe ? "나무를 베요" : item.toolType == ToolType.Pickaxe ? "바위를 캐요"
                : item.toolType == ToolType.FishingRod ? "물고기를 낚아요" : item.toolType == ToolType.Net ? "곤충을 잡아요"
                : item.toolType == ToolType.Shovel ? "흙이 들썩이는 자리를 파요(점토·옛 동전·화석·토기 조각)" : "작업에 써요";
            int uses = ToolDurability.MaxFor(item);
            int power = ToolPower.For(item);
            string tier = power >= 3 ? " · 힘 3(가장 단단한 바위도)" : power == 2 ? " · 힘 2(철 광맥도 캐요)" : "";
            if (item.toolType != ToolType.Pickaxe && item.toolType != ToolType.Axe) tier = "";
            else if (item.toolType == ToolType.Axe) tier = power >= 2 ? $" · 힘 {power}(더 적게 휘둘러요)" : "";
            lines.Add(UseLine("도구", uses > 0 ? $"{verb}{tier} · {uses}번 쓰면 닳아요" : verb + tier));
        }
        var upgrade = catalog?.upgrades?.FirstOrDefault(u => u != null && u.tool == item);
        if (upgrade != null)
        {
            string mate = upgrade.root != DemoSpecialization.None ? GetRootLabel(upgrade.root)
                : upgrade.companionPrefix == "Miner" ? "광부" : upgrade.companionPrefix == "Lumberjack" ? "벌목꾼" : "함께 온";
            lines.Add(UseLine("선물", $"{mate} 동료에게 주면 생산이 늘어요"));
        }
        var consumers = allRecipes.Where(r => r != null && PassesRouteFilter(r) && r.ingredients != null && r.ingredients.Any(i => i != null && i.item == item))
            .Select(TitleOf).Distinct().ToList();
        if (consumers.Count > 0)
            lines.Add(UseLine("재료", string.Join(", ", consumers.Take(4)) + (consumers.Count > 4 ? $" 외 {consumers.Count - 4}개" : "")));
        if (item.category != ItemCategory.Tool)
            lines.Add(UseLine("판매", $"가판대에 올려 팔아요 · 기본가 {item.basePrice}G" + (item.category == ItemCategory.Luxury ? " · 관광객이 좋아해요" : "")));
        return lines;
    }

    // 데모 작업대 이름(첫날 조작). 레거시 경로는 기존 표시 이름을 쓴다.
    static string StationLabel(WorkbenchType type)
    {
        if (!PAUiTheme.Active) return GetWorkbenchLabel(type);
        switch (type)
        {
            case WorkbenchType.BasicWorkbench: return "현장 작업대";
            case WorkbenchType.Kitchen: return "조리대";
            case WorkbenchType.Forge: return "화로";
            case WorkbenchType.SewingTable: return "재봉대";
            default: return "작업대";
        }
    }

    static string StationRole(WorkbenchType type)
    {
        switch (type)
        {
            case WorkbenchType.BasicWorkbench: return "목공·설비 만들기";
            case WorkbenchType.Kitchen: return "요리";
            case WorkbenchType.Forge: return "제련·도구 만들기";
            case WorkbenchType.SewingTable: return "바느질";
            default: return "제작";
        }
    }

    static string StationName(Workbench wb) => wb.workbenchType == WorkbenchType.BasicWorkbench ? wb.displayName : StationLabel(wb.workbenchType);

    Color ResolveCardColor(bool unlocked, bool hasIngredients, bool canCraftHere)
    {
        if (!unlocked) return lockedColor;
        if (!canCraftHere) return new Color(0.12f, 0.16f, 0.14f, 0.90f);
        return hasIngredients
            ? new Color(0.15f, 0.34f, 0.23f, 0.98f)
            : new Color(0.31f, 0.22f, 0.15f, 0.96f);
    }

    void SetStatus(string message, bool isError = false)
    {
        if (_statusText == null) return;
        _statusText.text = message;
        _statusText.color = _runtimeGenerated
            ? (isError ? new Color(.61f, .27f, .17f) : PAUiTheme.Ink)
            : (isError ? new Color(1f, .67f, .57f) : new Color(.77f, .88f, .79f));
    }

    static int CompareRecipes(RecipeData left, RecipeData right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return 1;
        if (right == null) return -1;

        int workbench = left.requiredWorkbench.CompareTo(right.requiredWorkbench);
        if (workbench != 0) return workbench;
        int tier = left.requiredTier.CompareTo(right.requiredTier);
        if (tier != 0) return tier;
        return string.Compare(left.recipeName, right.recipeName, System.StringComparison.Ordinal);
    }

    static string GetWorkbenchLabel(WorkbenchType type)
    {
        switch (type)
        {
            case WorkbenchType.BasicWorkbench: return "목재 가공 작업대";
            case WorkbenchType.Kitchen: return "주방 작업대";
            case WorkbenchType.Forge: return "대장간 작업대";
            case WorkbenchType.SewingTable: return "재봉 작업대";
            default: return "기본 제작";
        }
    }

    // -------- 런타임 UI 구성 --------

    void BuildRuntimeUI()
    {
        _runtimeGenerated = true;
        // 씬의 부분 직렬화 패널이 세 참조를 모두 갖추지 못했다면 화면만 숨기고
        // 재현 가능한 런타임 제품 UI를 별도로 구성한다.
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        Transform parent = ResolveUiParent();

        craftingPanel = new GameObject("CraftingOverlay", typeof(RectTransform), typeof(Image));
        var overlayRT = (RectTransform)craftingPanel.transform;
        overlayRT.SetParent(parent, false);
        Stretch(overlayRT);
        craftingPanel.GetComponent<Image>().color = new Color(.04f, .07f, .08f, .42f);

        // 부드러운 그림자: 창 아래로 내린 흐린 같은 모양(오프셋 + 투명도). 색 후광이 아니라 깊이.
        var shadow = PAUiTheme.Panel(craftingPanel.transform, "CraftingShadow", new Color(.05f, .09f, .08f, .30f));
        Place(shadow.rectTransform, new Vector2(.5f, .5f), new Vector2(1236f, 876f), new Vector2(0f, -10f));
        shadow.raycastTarget = false;

        var window = PAUiTheme.Panel(craftingPanel.transform, "CraftingPanel", PAUiTheme.Cream);
        Place(window.rectTransform, new Vector2(.5f, .5f), new Vector2(1220f, 860f), Vector2.zero);

        var header = PAUiTheme.Panel(window.transform, "Header", PAUiTheme.Teal);
        var headerRT = header.rectTransform;
        headerRT.anchorMin = new Vector2(0f, 1f); headerRT.anchorMax = Vector2.one; headerRT.pivot = new Vector2(.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 92f); headerRT.anchoredPosition = Vector2.zero;
        header.raycastTarget = false;

        _titleText = Label(window.transform, "Title", "제작 도감", 36f, Color.white, TextAlignmentOptions.MidlineLeft);
        TopBand(_titleText.rectTransform, 12f, 50f, 36f, 200f);
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.enableAutoSizing = true; _titleText.fontSizeMin = 26f; _titleText.fontSizeMax = 36f;

        _modeText = Label(window.transform, "Mode", "전체 레시피", 19f, new Color(.86f, .94f, .90f), TextAlignmentOptions.MidlineLeft);
        TopBand(_modeText.rectTransform, 58f, 26f, 38f, 200f);

        var closeButton = PAUiTheme.Panel(window.transform, "CloseButton", new Color(1f, 1f, 1f, .18f)).gameObject.AddComponent<Button>();
        var closeRT = (RectTransform)closeButton.transform;
        closeRT.anchorMin = closeRT.anchorMax = closeRT.pivot = Vector2.one;
        closeRT.sizeDelta = new Vector2(120f, 46f); closeRT.anchoredPosition = new Vector2(-24f, -23f);
        var closeText = Label(closeButton.transform, "Text", "닫기  Esc", 19f, Color.white, TextAlignmentOptions.Center);
        closeText.fontStyle = FontStyles.Bold;
        closeButton.onClick.AddListener(Close);

        // 왼쪽: 레시피 목록
        var listFrame = new GameObject("RecipeList", typeof(RectTransform)).GetComponent<RectTransform>();
        listFrame.SetParent(window.transform, false);
        listFrame.anchorMin = new Vector2(0f, 0f); listFrame.anchorMax = new Vector2(0f, 1f); listFrame.pivot = new Vector2(0f, .5f);
        listFrame.offsetMin = new Vector2(24f, 96f); listFrame.offsetMax = new Vector2(24f + 470f, -116f);

        var scrollGO = new GameObject("RecipeScroll", typeof(RectTransform), typeof(ScrollRect));
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.SetParent(listFrame, false);
        Stretch(scrollRT);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = (RectTransform)viewportGO.transform;
        viewportRT.SetParent(scrollGO.transform, false);
        Stretch(viewportRT);
        // Mask uses the graphic alpha when writing its stencil. showMaskGraphic hides the
        // viewport background, so the mask graphic itself must stay opaque for its children.
        viewportGO.GetComponent<Image>().color = Color.white;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.SetParent(viewportGO.transform, false);
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f); contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false; layout.childControlHeight = true;
        layout.spacing = 8f; layout.padding = new RectOffset(2, 18, 2, 2);
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportRT; scroll.content = contentRT;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 34f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        _recipeScroll = scroll;

        var rail = PAUiTheme.Panel(scrollGO.transform, "RecipeScrollbar", new Color(.85f, .84f, .76f));
        var railRT = rail.rectTransform;
        railRT.anchorMin = new Vector2(1f, 0f); railRT.anchorMax = Vector2.one; railRT.pivot = new Vector2(1f, .5f);
        railRT.sizeDelta = new Vector2(8f, -8f);
        var handle = PAUiTheme.Panel(rail.transform, "Handle", PAUiTheme.Teal);
        Stretch(handle.rectTransform);
        var scrollbar = rail.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // 오른쪽: 상세
        var detail = PAUiTheme.Panel(window.transform, "RecipeDetail", DetailSurface);
        var detailRT = detail.rectTransform;
        detailRT.anchorMin = Vector2.zero; detailRT.anchorMax = Vector2.one;
        detailRT.offsetMin = new Vector2(24f + 470f + 20f, 96f); detailRT.offsetMax = new Vector2(-24f, -116f);
        detail.raycastTarget = false;

        var tile = PAUiTheme.Panel(detail.transform, "IconTile", IconTile);
        Place(tile.rectTransform, new Vector2(0f, 1f), new Vector2(152f, 152f), new Vector2(24f, -24f), new Vector2(0f, 1f));
        tile.raycastTarget = false;
        var detailIconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var detailIconRT = (RectTransform)detailIconGO.transform;
        detailIconRT.SetParent(tile.transform, false);
        Stretch(detailIconRT); detailIconRT.offsetMin = new Vector2(12f, 12f); detailIconRT.offsetMax = new Vector2(-12f, -12f);
        _detailIcon = detailIconGO.GetComponent<Image>();
        _detailIcon.preserveAspect = true; _detailIcon.raycastTarget = false;

        _detailName = Label(detail.transform, "Name", "", 34f, PAUiTheme.Ink, TextAlignmentOptions.TopLeft);
        Corner(_detailName.rectTransform, new Vector2(196f, -28f), new Vector2(460f, 46f));
        _detailName.fontStyle = FontStyles.Bold;
        _detailName.enableAutoSizing = true; _detailName.fontSizeMin = 24f; _detailName.fontSizeMax = 34f;
        _detailMeta = Label(detail.transform, "Meta", "", 19f, PAUiTheme.Muted, TextAlignmentOptions.TopLeft);
        Corner(_detailMeta.rectTransform, new Vector2(198f, -80f), new Vector2(460f, 28f));
        _detailDescription = Label(detail.transform, "Description", "", 20f, PAUiTheme.Ink, TextAlignmentOptions.TopLeft);
        Corner(_detailDescription.rectTransform, new Vector2(198f, -114f), new Vector2(456f, 64f));
        _detailDescription.enableAutoSizing = true; _detailDescription.fontSizeMin = 16f; _detailDescription.fontSizeMax = 20f;

        var usesTitle = Label(detail.transform, "UsesTitle", "쓰임", 20f, PAUiTheme.Muted, TextAlignmentOptions.TopLeft);
        Corner(usesTitle.rectTransform, new Vector2(26f, -196f), new Vector2(300f, 26f));
        usesTitle.fontStyle = FontStyles.Bold;
        _detailUses = Label(detail.transform, "Uses", "", 19f, PAUiTheme.Ink, TextAlignmentOptions.TopLeft);
        Corner(_detailUses.rectTransform, new Vector2(26f, -226f), new Vector2(630f, 104f));
        _detailUses.lineSpacing = 8f;
        _detailUses.enableAutoSizing = true; _detailUses.fontSizeMin = 15f; _detailUses.fontSizeMax = 19f;

        var ingredientsTitle = Label(detail.transform, "IngredientsTitle", "재료  (보유 / 필요)", 20f, PAUiTheme.Muted, TextAlignmentOptions.TopLeft);
        Corner(ingredientsTitle.rectTransform, new Vector2(26f, -338f), new Vector2(400f, 26f));
        ingredientsTitle.fontStyle = FontStyles.Bold;
        var ingredientsGO = new GameObject("Ingredients", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var ingredientsRT = (RectTransform)ingredientsGO.transform;
        ingredientsRT.SetParent(detail.transform, false);
        ingredientsRT.anchorMin = new Vector2(0f, 1f); ingredientsRT.anchorMax = new Vector2(1f, 1f); ingredientsRT.pivot = new Vector2(.5f, 1f);
        // 상세 높이 648 = 860 − 머리 116 − 상태 줄 96. 재료 3줄(52+6)이 [만들기](아래 22~90) 위에 들어간다.
        ingredientsRT.offsetMin = new Vector2(24f, -550f); ingredientsRT.offsetMax = new Vector2(-24f, -370f);
        var ingredientLayout = ingredientsGO.GetComponent<VerticalLayoutGroup>();
        ingredientLayout.spacing = 6f; ingredientLayout.childForceExpandHeight = false; ingredientLayout.childControlHeight = true;
        ingredientLayout.childForceExpandWidth = true;
        _ingredientParent = ingredientsRT;

        var craft = PAUiTheme.Panel(detail.transform, "CraftButton", PAUiTheme.Teal);
        var craftRT = craft.rectTransform;
        craftRT.anchorMin = new Vector2(0f, 0f); craftRT.anchorMax = new Vector2(1f, 0f); craftRT.pivot = new Vector2(.5f, 0f);
        craftRT.offsetMin = new Vector2(24f, 22f); craftRT.offsetMax = new Vector2(-24f, 22f + 68f);
        _craftButton = craft.gameObject.AddComponent<Button>();
        _craftButton.targetGraphic = craft;
        var colors = _craftButton.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(.86f, 1f, .95f); colors.pressedColor = new Color(.70f, .86f, .80f);
        colors.selectedColor = Color.white; colors.disabledColor = Color.white; colors.fadeDuration = .12f;
        _craftButton.colors = colors;
        _craftButton.onClick.AddListener(CraftSelected);
        _craftLabel = Label(craft.transform, "Label", "만들기", 28f, Color.white, TextAlignmentOptions.Center);
        _craftLabel.fontStyle = FontStyles.Bold;

        // 하단 상태 줄
        var feedback = PAUiTheme.Panel(window.transform, "Feedback", new Color(.86f, .93f, .86f));
        var feedbackRT = feedback.rectTransform;
        feedbackRT.anchorMin = Vector2.zero; feedbackRT.anchorMax = new Vector2(1f, 0f); feedbackRT.pivot = new Vector2(.5f, 0f);
        feedbackRT.sizeDelta = new Vector2(-48f, 60f); feedbackRT.anchoredPosition = new Vector2(0f, 20f);
        feedback.raycastTarget = false;
        _statusText = Label(window.transform, "Status", "작업대에서 [E]를 누르면 제작할 수 있어요.", 20f, PAUiTheme.Ink, TextAlignmentOptions.Center);
        var statusRT = _statusText.rectTransform;
        statusRT.anchorMin = Vector2.zero; statusRT.anchorMax = new Vector2(1f, 0f); statusRT.pivot = new Vector2(.5f, 0f);
        statusRT.sizeDelta = new Vector2(-80f, 60f); statusRT.anchoredPosition = new Vector2(0f, 20f);
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        slotParent = contentRT;
        slotPrefab = CreateRuntimeSlotPrefab(window.transform);
        craftingPanel.SetActive(false);
    }

    GameObject CreateRuntimeSlotPrefab(Transform parent)
    {
        var go = new GameObject("RecipeSlot_RuntimePrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.SetActive(false);
        PAUiTheme.Surface(go.GetComponent<Image>(), IdleRow);
        go.GetComponent<LayoutElement>().preferredHeight = 88f;
        ColorBlock colors = go.GetComponent<Button>().colors;
        colors.highlightedColor = new Color(.93f, .97f, .93f);
        colors.pressedColor = new Color(.84f, .92f, .87f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.fadeDuration = .08f;
        go.GetComponent<Button>().colors = colors;

        var tile = PAUiTheme.Panel(go.transform, "IconTile", IconTile);
        Place(tile.rectTransform, new Vector2(0f, .5f), new Vector2(68f, 68f), new Vector2(10f, 0f), new Vector2(0f, .5f));
        tile.raycastTarget = false;
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.SetParent(tile.transform, false);
        Stretch(iconRT); iconRT.offsetMin = new Vector2(6f, 6f); iconRT.offsetMax = new Vector2(-6f, -6f);
        var icon = iconGO.GetComponent<Image>();
        icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = false;

        var label = Label(go.transform, "Label", "", 24f, PAUiTheme.Ink, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.offsetMin = new Vector2(92f, 8f); label.rectTransform.offsetMax = new Vector2(-14f, -8f);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = -4f;
        return go;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Stretch(rt);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.sizeDelta = size; rt.anchoredPosition = position;
    }

    static void Corner(RectTransform rt, Vector2 position, Vector2 size) =>
        Place(rt, new Vector2(0f, 1f), size, position, new Vector2(0f, 1f));

    static void TopBand(RectTransform rt, float top, float height, float left, float right)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height); rt.offsetMax = new Vector2(-right, -top);
    }

    Transform ResolveUiParent()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        if (uiRoot != null) return uiRoot.transform;

        var canvasGO = new GameObject("CraftingUI_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGO.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
