using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI instance;

    public GameObject craftingPanel;
    public Transform slotParent;
    public GameObject slotPrefab;

    private RecipeData[] allRecipes;

    void Awake()
    {
        instance = this;
        allRecipes = Resources.LoadAll<RecipeData>("Recipes");
    }

    void Start()
    {
        GenerateSlots();
        craftingPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleUI(); 
        }
    }

    void GenerateSlots()
    {
        if (slotParent == null || slotPrefab == null) return;

        foreach (Transform child in slotParent) Destroy(child.gameObject);

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;

            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            Button btn = newSlot.GetComponent<Button>();
            
            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>();
            if (slotText != null)
            {
                string inputName = (recipe.inputItem != null) ? recipe.inputItem.itemName : "재료누락";
                slotText.text = $"{recipe.recipeName}\n<size=80%>({inputName} x{recipe.inputCount})</size>";
            }
            
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnClickCraft(recipe));
            }
        }
    }

    void OnClickCraft(RecipeData recipe)
    {
        if (Inventory.instance.HasItems(recipe.inputItem, recipe.inputCount))
        {
            Inventory.instance.RemoveItems(recipe.inputItem, recipe.inputCount);
            Inventory.instance.AddItem(recipe.outputItem, recipe.outputCount); // ⭐ 개수 추가

            Debug.Log($"✅ 제작 성공: {recipe.recipeName}");
        }
        else
        {
            Debug.Log($"🚫 재료 부족! (필요: {recipe.inputItem.itemName} {recipe.inputCount}개)");
        }
    }

    public void ToggleUI()
    {
        craftingPanel.SetActive(!craftingPanel.activeSelf);
        
        bool isActive = craftingPanel.activeSelf;
        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isActive;
    }
}