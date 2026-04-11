using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "P.A. System/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName; // 건물 이름
    public int price;           // 건설 비용
    public GameObject prefab;   // 실제로 지어질 건물 모델 (프리팹)

    [Header("해금 조건 (Tier System)")]
    [Tooltip("이 건물을 건설하려면 필요한 최소 티어 (0=무제한, 1=지점장, 2=관리자, 3=사업가, 4=파트너)")]
    public int requiredTier = 0;
}