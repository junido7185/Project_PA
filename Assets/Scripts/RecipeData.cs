using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "P.A. System/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;     // 레시피 이름 (예: 통나무 가공)
    
    [Header("재료 (Input)")]
    public ItemData inputItem;    // 필요한 재료 (통나무)
    public int inputCount = 1;    // 필요한 개수
    
    [Header("결과물 (Output)")]
    public ItemData outputItem;   // 나오는 물건 (판자)
    public int outputCount = 1;   // 나오는 개수
}