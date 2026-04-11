using UnityEngine;

// 월드에 배치되는 작업대(Workbench) — 가공 시스템의 진입점.
//
// 역할:
// - 플레이어가 Space 키로 상호작용하면 CraftingUI 를 "이 작업대 컨텍스트" 로 연다.
// - workbenchType 으로 종류를 분기하여, 같은 종류의 RecipeData 만 표시되게 한다.
//   (예: BasicWorkbench → 가구·기초 가공품, Kitchen → 요리, Forge → 무기/도구, SewingTable → 의류)
//
// 씬 설정:
// - 빈 GameObject 에 Collider 를 부착하고 이 컴포넌트를 추가한다.
// - workbenchType 을 인스펙터에서 선택한다.
// - PlayerInteraction.cs 의 Raycast LayerMask 에 닿는 위치/레이어로 배치한다.
[RequireComponent(typeof(Collider))]
public class Workbench : MonoBehaviour, IInteractable
{
    [Header("작업대 정보")]
    [Tooltip("이 작업대에서 사용할 수 있는 RecipeData.requiredWorkbench 종류와 매칭된다.")]
    public WorkbenchType workbenchType = WorkbenchType.BasicWorkbench;

    [Tooltip("UI 프롬프트에 표시될 이름")]
    public string displayName = "작업대";

    public void Interact(GameObject interactor)
    {
        if (CraftingUI.instance == null)
        {
            Debug.LogWarning("🛠 Workbench: 씬에 CraftingUI 가 없습니다.");
            return;
        }
        CraftingUI.instance.OpenForWorkbench(this);
    }

    public string GetInteractPrompt() => $"{displayName} 사용하기";
}
