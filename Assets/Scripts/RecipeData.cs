using System.Collections.Generic;
using UnityEngine;

// Docs/02 Process 3.0 "2차 가공 및 제작" — 1개 레시피의 정의.
//
// 설계 의도:
// - 다중 재료 지원: ingredients 리스트로 N종 재료 ×N개 조합 가능.
// - 부가가치 표현: baseOutputQuality 가 가공 결과의 품질을 결정 → PurchaseEvaluator 가
//   idealPrice = basePrice × (1 + 0.5 × (quality - 1)) 로 자동 가산한다.
// - 워크샵 종류 분기: requiredWorkbench 로 "주방에서만 만들 수 있는 요리" 같은 분리 가능.
// - Tier 잠금: 기획서 Docs/05 의 본사 승인 등급과 자연 연동.
// - 레거시 호환: 기존 inputItem/inputCount 단일 재료 에셋은 OnValidate 가 자동으로
//   ingredients 리스트로 마이그레이션한다 (한 번만 수행됨).
[System.Serializable]
public class RecipeIngredient
{
    public Item item;
    public int count = 1;
}

// 어떤 종류의 작업대에서 만들 수 있는지 분류한다.
// None 은 "어디서나 가능" 을 의미하며, 디버그 글로벌 토글에서도 보이는 레시피이다.
public enum WorkbenchType
{
    None,
    BasicWorkbench,
    Kitchen,
    Forge,
    SewingTable
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "P.A. System/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("기본 정보")]
    public string recipeName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("재료 (다중)")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    [Header("결과물")]
    public Item outputItem;
    public int outputCount = 1;

    [Header("가공 품질 (부가가치)")]
    [Tooltip("결과물의 기본 품질. 1.0=평균, 1.2=가공품 권장. PurchaseEvaluator 가 idealPrice 를 가산한다.")]
    [Range(0.5f, 3f)] public float baseOutputQuality = 1.2f;

    [Tooltip("재료의 평균 quality 가 결과 품질에 미치는 영향 계수.\n" +
             "최종 quality = baseOutputQuality + ingredientQualityWeight × (재료평균 - 1)")]
    [Range(0f, 1f)] public float ingredientQualityWeight = 0.3f;

    [Header("해금 조건")]
    [Tooltip("이 레시피를 사용하려면 필요한 최소 티어 (0=무제한)")]
    public int requiredTier = 0;

    [Tooltip("이 레시피를 사용 가능한 작업대 종류. None=어디서나 가능 (디버그 토글에서도 보임)")]
    public WorkbenchType requiredWorkbench = WorkbenchType.None;

    // ---- 레거시 호환 (OnValidate 자동 마이그레이션) ----
    // 기존 단일-재료 RecipeData 에셋을 ingredients 리스트로 한 번에 옮긴다.
    [HideInInspector] public Item inputItem;
    [HideInInspector] public int inputCount = 0;

    void OnValidate()
    {
        // 레거시 필드가 채워져 있고 새 리스트가 비어 있으면 자동 이전.
        if (inputItem != null && inputCount > 0 && (ingredients == null || ingredients.Count == 0))
        {
            ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = inputItem, count = inputCount }
            };
            inputItem = null;
            inputCount = 0;
        }

        // null 슬롯 정리
        if (ingredients != null)
        {
            for (int i = ingredients.Count - 1; i >= 0; i--)
            {
                if (ingredients[i] == null) ingredients.RemoveAt(i);
            }
        }
    }
}
