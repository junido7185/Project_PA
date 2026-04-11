using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 가공(Crafting) 패널 UI.
//
// 두 가지 진입 경로:
// 1) Workbench 에서 OpenForWorkbench(Workbench) 호출 — 정식 진입.
//    - 해당 워크샵 종류와 일치하는 레시피만 표시.
//    - 티어 잠긴 레시피는 회색 처리(클릭은 가능하나 CraftingService 가 차단).
// 2) C 키 글로벌 토글 (디버그) — _activeWorkbench=null 로 동작.
//    - requiredWorkbench == None 인 레시피만 표시.
//    - 워크샵 진입 흐름이 미완성인 동안의 임시 도구.
//
// 클릭 시:
// - CraftingService.TryCraft(recipe, _activeWorkbench) 로 모든 검사·차감·결과 생성을 위임.
// - UI 는 패널 표시/필터링만 담당하고, 비즈니스 로직은 보유하지 않는다.
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

    void Awake()
    {
        instance = this;
        allRecipes = Resources.LoadAll<RecipeData>("Recipes");
    }

    void Start()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);

        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnCraftToggle += ToggleUI;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnCraftToggle -= ToggleUI;
    }

    // -------- 외부 진입점 --------

    // Workbench.Interact 에서 호출. 해당 작업대 종류에 맞는 레시피만 표시한다.
    public void OpenForWorkbench(Workbench wb)
    {
        _activeWorkbench = wb;
        GenerateSlotsForContext();

        if (craftingPanel != null) craftingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // C 키 글로벌 디버그 토글. _activeWorkbench=null → requiredWorkbench=None 만 표시.
    public void ToggleUI()
    {
        if (craftingPanel == null) return;

        if (craftingPanel.activeSelf)
        {
            craftingPanel.SetActive(false);
            _activeWorkbench = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // 디버그 글로벌 모드로 열기
            OpenForWorkbench(null);
        }
    }

    // -------- 슬롯 생성 (컨텍스트 필터 적용) --------

    void GenerateSlotsForContext()
    {
        if (slotParent == null || slotPrefab == null) return;

        // 기존 자식 슬롯 제거
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        if (allRecipes == null) return;

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (!PassesWorkbenchFilter(recipe)) continue;

            bool unlocked = TierService.Instance == null
                            || TierService.Instance.IsUnlocked(recipe.requiredTier);

            GameObject newSlot = Instantiate(slotPrefab, slotParent);

            // 라벨 — 첫 재료를 미리보기로 표시 (다중 재료는 첫 재료 + ⋯ 로 압축)
            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>();
            if (slotText != null)
            {
                slotText.text = BuildSlotLabel(recipe, unlocked);
            }

            // 잠금 시 이미지 회색 처리
            if (!unlocked)
            {
                foreach (var img in newSlot.GetComponentsInChildren<Image>())
                {
                    img.color = lockedColor;
                }
            }

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                RecipeData captured = recipe;       // 클로저 캡처
                Workbench wbCaptured = _activeWorkbench;
                btn.onClick.AddListener(() => CraftingService.TryCraft(captured, wbCaptured));
            }
        }
    }

    // 컨텍스트(작업대 종류)에 맞는지 판정한다.
    bool PassesWorkbenchFilter(RecipeData recipe)
    {
        // 디버그 글로벌 모드: requiredWorkbench=None 만 보이게
        if (_activeWorkbench == null)
            return recipe.requiredWorkbench == WorkbenchType.None;

        // 정식 모드: None 은 어디서나, 그 외는 종류 일치 시
        return recipe.requiredWorkbench == WorkbenchType.None
            || recipe.requiredWorkbench == _activeWorkbench.workbenchType;
    }

    string BuildSlotLabel(RecipeData recipe, bool unlocked)
    {
        string lockMark = unlocked ? "" : $"🔒 (Tier {recipe.requiredTier}) ";
        string ingredientText;

        if (recipe.ingredients != null && recipe.ingredients.Count > 0 && recipe.ingredients[0] != null && recipe.ingredients[0].item != null)
        {
            var first = recipe.ingredients[0];
            string firstName = first.item.itemName;
            string suffix = recipe.ingredients.Count > 1 ? $" 외 {recipe.ingredients.Count - 1}종" : "";
            ingredientText = $"{firstName} ×{first.count}{suffix}";
        }
        else
        {
            ingredientText = "재료 누락";
        }

        return $"{lockMark}{recipe.recipeName}\n<size=80%>({ingredientText})</size>";
    }
}
