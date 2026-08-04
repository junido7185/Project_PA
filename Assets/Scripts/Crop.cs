using UnityEngine;
using System.Collections;

public class Crop : MonoBehaviour
{
    [Header("성장 설정")]
    public GameObject[] growthStages; // 단계별 모습 (0: 씨앗, 1: 새싹, 2: 다 자람)
    public float timePerStage = 3.0f; // 다음 단계까지 걸리는 시간 (테스트용 3초)
    
    [Header("수확 설정")]
    public Item harvestItem;      // 다 자라고 수확하면 줄 아이템
    [Min(1)] public int harvestCount = 3;
    
    private int currentStageIndex = 0;
    public bool isFullyGrown = false;
    public int CurrentStageNumber => Mathf.Clamp(currentStageIndex + 1, 1, StageCount);
    public int StageCount => growthStages != null ? Mathf.Max(1, growthStages.Length) : 1;

    public void Configure(Item item, int count, float secondsPerStage, string presentationResourcePath)
    {
        harvestItem = item;
        harvestCount = Mathf.Max(1, count);
        timePerStage = Mathf.Max(0.1f, secondsPerStage);
        currentStageIndex = 0;
        isFullyGrown = false;

        if (!string.IsNullOrWhiteSpace(presentationResourcePath))
            ReplaceGrowthStagesWithResource(presentationResourcePath);
    }

    void Start()
    {
        // 시작하면 성장 코루틴 발동!
        UpdateModel();
        StartCoroutine(GrowRoutine());
    }

    IEnumerator GrowRoutine()
    {
        if (growthStages == null || growthStages.Length == 0)
        {
            isFullyGrown = true;
            yield break;
        }

        // 마지막 단계 전까지만 성장
        while (currentStageIndex < growthStages.Length - 1)
        {
            yield return new WaitForSeconds(timePerStage);
            currentStageIndex++;
            UpdateModel();
        }

        isFullyGrown = true;
        Debug.Log("✨ 작물이 다 자랐습니다! 수확 가능!");
        
        // 다 자라면 태그를 바꿔서 수확 가능하게 만듦 (필요 시)
        // gameObject.tag = "Tree"; // 예: 나무처럼 캘 수 있게
    }

    void UpdateModel()
    {
        if (growthStages == null || growthStages.Length == 0) return;

        // 모든 모델 끄고
        foreach (GameObject g in growthStages)
            if (g != null) g.SetActive(false);
        
        // 현재 단계 모델만 켜기
        if (currentStageIndex < growthStages.Length)
        {
            if (growthStages[currentStageIndex] != null)
                growthStages[currentStageIndex].SetActive(true);
        }
    }

    void ReplaceGrowthStagesWithResource(string resourcePath)
    {
        GameObject wheatVisual = Resources.Load<GameObject>(resourcePath);
        if (wheatVisual == null) return;

        if (growthStages != null)
        {
            foreach (GameObject legacy in growthStages)
                if (legacy != null) legacy.SetActive(false);
        }

        growthStages = new GameObject[3];
        int[] clusterCounts = { 1, 3, 5 };
        float[] scales = { 0.18f, 0.42f, 0.68f };

        for (int stage = 0; stage < growthStages.Length; stage++)
        {
            GameObject root = new GameObject($"WheatStage_{stage}");
            root.transform.SetParent(transform, false);
            int clusterCount = clusterCounts[stage];
            for (int i = 0; i < clusterCount; i++)
            {
                GameObject visual = Instantiate(wheatVisual, root.transform);
                float angle = i * (360f / Mathf.Max(1, clusterCount));
                float radius = stage == 0 ? 0f : 0.18f + 0.04f * stage;
                visual.transform.localPosition = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                visual.transform.localRotation = Quaternion.Euler(0f, angle + stage * 17f, 0f);
                visual.transform.localScale = Vector3.one * scales[stage];
            }
            root.SetActive(stage == 0);
            growthStages[stage] = root;
        }
    }

    // 수확 함수
    public void Harvest()
    {
        TryHarvest(Inventory.instance);
    }

    public bool TryHarvest(Inventory inventory)
    {
        if (!isFullyGrown || harvestItem == null || inventory == null)
            return false;

        int count = Mathf.Max(1, harvestCount);
        if (!inventory.CanAddItems(harvestItem, count))
            return false;

        // 1. 아이템 지급
        if (!inventory.AddItem(harvestItem, count))
            return false;
        inventory.RefreshAllUI();

        // 2. 땅 비우기 (부모인 Farmland에게 알림)
        Farmland myLand = GetComponentInParent<Farmland>();
        if (myLand != null)
        {
            myLand.ClearLand();
        }

        // 3. 삭제
        Debug.Log($"🌾 수확 완료: {harvestItem.itemName} x{count}");
        Destroy(gameObject);
        return true;
    }
}
