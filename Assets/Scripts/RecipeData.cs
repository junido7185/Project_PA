using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "P.A. System/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;     
    
    [Header("재료 (Input)")]
    public Item inputItem;    // ⭐ ItemData -> Item
    public int inputCount = 1;    
    
    [Header("결과물 (Output)")]
    public Item outputItem;   // ⭐ ItemData -> Item
    public int outputCount = 1;   
}