using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "P.A. System/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName; // 건물 이름
    public int price;           // 건설 비용
    public GameObject prefab;   // 실제로 지어질 건물 모델 (프리팹)
}