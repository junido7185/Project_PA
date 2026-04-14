using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // 스키마 버전 — SaveManager 가 로드 시 마이그레이션에 사용한다.
    // 새 필드가 추가되면 CurrentSaveVersion(SaveManager) 을 올리고 마이그레이션 함수를 추가한다.
    public int version = 0;

    // 플레이어 정보
    public int money;
    public Vector3 playerPosition;

    // 티어 시스템
    public int currentTier = 0;
    public long cumulativeRevenue = 0;
    public int reputation = 0;

    // 인게임 시간 (GameClock)
    public float gameHour = 7f;   // 0~23.99
    public int   gameDay  = 1;    // 1 이상

    // 감사 시스템 (AuditService) — v2 추가
    public int lastAuditDay = 0;

    // 건물 정보 리스트
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();

    // 인벤토리 · 핫바 (SlotSaveData.count == 0 이면 빈 칸)
    public List<SlotSaveData> inventorySlots = new List<SlotSaveData>();
    public List<SlotSaveData> hotbarSlots    = new List<SlotSaveData>();
}

// 인벤토리/핫바 한 칸을 직렬화한 DTO.
// ItemRegistry.Find(itemId, itemName) 로 Item 원형을 복원한다.
[System.Serializable]
public class SlotSaveData
{
    public int    itemId;       // Item.id (복원 1차 키)
    public string itemName;    // Item.itemName (폴백 키)
    public int    count;       // 0 = 빈 칸
    public float  quality;
    public int    currentPrice;
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