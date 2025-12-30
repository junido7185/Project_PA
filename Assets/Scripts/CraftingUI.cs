using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 👈 TMP를 쓰려면 이게 필수입니다!

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
        
        // 1. 로드 시도
        allRecipes = Resources.LoadAll<RecipeData>("Recipes");

        // 2. 디버그 로그 (범인 색출)
        if (allRecipes == null)
        {
            Debug.LogError("🚨 비상! allRecipes 배열 자체가 null입니다! Resources 폴더가 없는 것 같습니다.");
        }
        else if (allRecipes.Length == 0)
        {
            Debug.LogError("🚨 비상! 폴더는 찾았는데 파일이 0개입니다! 'Recipes' 폴더 안에 데이터가 없거나, 경로 이름이 틀렸습니다.");
        }
        else
        {
            Debug.Log("✅ 성공! 레시피 " + allRecipes.Length + "개를 찾았습니다.");
        }
    }

    void Start()
    {
        GenerateSlots();
        craftingPanel.SetActive(false);
    }

    // CraftingUI.cs 클래스 안쪽, 아무데나 추가

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleUI(); // 이제 에러 안 날 겁니다!
        }
    }

    void GenerateSlots()
    {
        // 🔍 용의자 1: 부모(Slot Parent) 확인
        if (slotParent == null)
        {
            Debug.LogError("🚨 범인 검거: Inspector에서 [Slot Parent]가 연결 안 됨! (CraftingPanel을 드래그해서 넣으세요)");
            return;
        }

        // 기존 슬롯 청소
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;

            // 🔍 용의자 2: 프리팹(Slot Prefab) 확인
            if (slotPrefab == null)
            {
                Debug.LogError("🚨 범인 검거: Inspector에서 [Slot Prefab]이 연결 안 됨!");
                return;
            }

            GameObject newSlot = Instantiate(slotPrefab, slotParent);

            // 🔍 용의자 3: 버튼 컴포넌트 확인
            Button btn = newSlot.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("🚨 범인 검거: [Recipe_Slot_Prefab]에 [Button] 컴포넌트가 없습니다! 프리팹을 확인하세요.");
                // 버튼이 없으면 밑에서 에러 나니까 여기서 멈춤
                continue; 
            }

            // 텍스트 설정 (TMP)
            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>();
            if (slotText != null)
            {
                string inputName = (recipe.inputItem != null) ? recipe.inputItem.itemName : "재료누락";
                slotText.text = $"{recipe.recipeName}\n<size=80%>({inputName} x{recipe.inputCount})</size>";
            }
            
            // 버튼 기능 연결
            btn.onClick.AddListener(() => OnClickCraft(recipe));
        }
    }

    void OnClickCraft(RecipeData recipe)
    {
        // 1. 재료 검사: "재료(Input)가 필요한 만큼 있니?"
        if (Inventory.instance.HasItems(recipe.inputItem, recipe.inputCount))
        {
            // 2. 재료 차감: "있으면 가져간다!"
            Inventory.instance.RemoveItems(recipe.inputItem, recipe.inputCount);

            // 3. 결과물 지급: "여기 판자 받아라!"
            Inventory.instance.AddItem(recipe.outputItem); // 개수 처리 필요하면 반복문 쓰면 됨

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