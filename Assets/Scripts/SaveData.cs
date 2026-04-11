using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // 플레이어 정보
    public int money;
    public Vector3 playerPosition;

    // 티어 시스템
    public int currentTier = 0;
    public long cumulativeRevenue = 0;
    public int reputation = 0;

    // 건물 정보 리스트
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
}

[System.Serializable]
public class BuildingSaveData
{
    public string buildingName; // 무슨 건물인지 (이름으로 식별)
    public Vector3 position;    // 어디에 있는지
    public Quaternion rotation; // 어느 방향인지

    public BuildingSaveData(string name, Vector3 pos, Quaternion rot)
    {
        buildingName = name;
        position = pos;
        rotation = rot;
    }
}