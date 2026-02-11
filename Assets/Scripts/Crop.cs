using UnityEngine;
using System.Collections;

public class Crop : MonoBehaviour
{
    [Header("성장 설정")]
    public GameObject[] growthStages; // 단계별 모습 (0: 씨앗, 1: 새싹, 2: 다 자람)
    public float timePerStage = 3.0f; // 다음 단계까지 걸리는 시간 (테스트용 3초)
    
    [Header("수확 설정")]
    public Item harvestItem;      // 다 자라고 수확하면 줄 아이템
    
    private int currentStageIndex = 0;
    public bool isFullyGrown = false;

    void Start()
    {
        // 시작하면 성장 코루틴 발동!
        UpdateModel();
        StartCoroutine(GrowRoutine());
    }

    IEnumerator GrowRoutine()
    {
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
        // 모든 모델 끄고
        foreach (GameObject g in growthStages) g.SetActive(false);
        
        // 현재 단계 모델만 켜기
        if (currentStageIndex < growthStages.Length)
        {
            growthStages[currentStageIndex].SetActive(true);
        }
    }

    // 수확 함수
    public void Harvest()
    {
        if (!isFullyGrown) return; // 아직 덜 자랐으면 무시

        // 1. 아이템 지급
        if (harvestItem != null)
        {
            Inventory.instance.AddItem(harvestItem);
        }

        // 2. 땅 비우기 (부모인 Farmland에게 알림)
        Farmland myLand = GetComponentInParent<Farmland>();
        if (myLand != null)
        {
            myLand.ClearLand();
        }

        // 3. 삭제
        Debug.Log("🌽 수확 완료!");
        Destroy(gameObject);
    }
}