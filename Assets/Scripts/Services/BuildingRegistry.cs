using System.Collections.Generic;
using UnityEngine;

// 건설된 건물들의 진실(source of truth) 레지스트리.
//
// 설계 의도:
// - 기존 SaveManager 는 FindGameObjectsWithTag("Building") 으로 씬을 긁어 저장 대상을 찾았다.
//   이는 건물 수가 늘어날수록 비용이 크고, 씬에 존재하지 않는 "논리적 상태" (파괴 예약 등)는
//   표현할 수 없다. 또 멀티 전환 시 씬 스캔은 호스트/클라이언트 상태 불일치의 원인이 된다.
// - BuildingRegistry는 건물 생성/파괴 시 명시적으로 등록/해지되는 목록을 들고 있다.
//   저장/로드는 이 목록만 직렬화한다.
// - BuildManager 가 Instantiate 후 Register 를 호출하고, 건물이 파괴될 때 Unregister 를 호출하면 된다.
public class BuildingRegistry : MonoBehaviour
{
    public static BuildingRegistry Instance { get; private set; }

    // 등록된 건물 인스턴스. 순서는 저장/로드 안정성을 위해 리스트로 보관.
    private readonly List<BuildingInstance> _buildings = new List<BuildingInstance>();

    public IReadOnlyList<BuildingInstance> Buildings => _buildings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 건물 생성 직후 호출한다. prefab 이름을 id 로 사용하므로, prefab 이름과 저장 키가 반드시 일치해야 한다.
    public void Register(BuildingData data, GameObject go)
    {
        if (data == null || data.prefab == null || go == null) return;
        _buildings.Add(new BuildingInstance
        {
            prefabName = data.prefab.name,
            gameObject = go
        });
    }

    // 건물 파괴 직전 호출한다.
    public void Unregister(GameObject go)
    {
        _buildings.RemoveAll(b => b.gameObject == go);
    }

    // 로드 시 씬 전체를 초기화할 때 사용 — 등록된 모든 건물 오브젝트를 파괴한다.
    public void ClearAll()
    {
        foreach (var b in _buildings)
        {
            if (b.gameObject != null) Destroy(b.gameObject);
        }
        _buildings.Clear();
    }
}

// 레지스트리가 보관하는 하나의 건물 엔트리. GameObject 참조는 런타임에만 유효.
public class BuildingInstance
{
    public string prefabName;
    public GameObject gameObject;
}
