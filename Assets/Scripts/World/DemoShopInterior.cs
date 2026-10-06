using UnityEngine;

// Canon v2 §7–9: 단일 배치 footprint를 그대로 쓰는 실내 cutaway 표현.
// 이동은 BuildingEntrance, 정착은 DemoSettlementController, 배치는 기존 World 서비스 소유다.
public sealed class DemoShopInterior : MonoBehaviour
{
    public GameObject exterior;
    public GameObject interior;
    public BuildingEntrance entrance;
    public BuildingEntrance exit;
    public Transform insideSpawn;
    public Transform outsideSpawn;
    public bool IsInside { get; private set; }

    // Canon v2 §7: perimeter만 점유하는 기존 배치의 빈 실내도 장식 부지에 포함한다.
    public bool CoversGround(Vector3 worldPoint)
    {
        var local = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(local.x) < 7f && local.z > -8f && local.z < 4f;
    }

    public void Bind(DemoSettlementController settlement)
    {
        entrance.ConfigureDestination(insideSpawn, "상점 들어가기",
            () => settlement != null && settlement.Established,
            _ => ShowInterior(true), "Shop/Base와 주민 텐트 2개를 먼저 설치하세요");
        exit.ConfigureDestination(outsideSpawn, "밖으로 나가기", onArrival: _ => ShowInterior(false));
        FirstDayWorldPresentation.Instance?.ClearShopSite(this);
        BindStorageChest();
        var light = interior.transform.Find("WarmInteriorLight")?.GetComponent<Light>();
        if (light != null) { light.intensity = 8f; light.color = new Color(1f, .88f, .74f); }
        ShowInterior(false);
    }

    // P6: 상점 쪽(+x) 장식 상자를 기존 StorageBox 권위로 쓰는 실내 보관함으로 만든다. 별도 Inventory 권위는 없다.
    // 배치 저장(storedItems)은 기존 DemoSettlement save bridge가 이 상자를 찾아 담는다.
    public StorageBox Storage { get; private set; }
    void BindStorageChest()
    {
        if (Storage != null) return;
        Transform chest = null;
        foreach (Transform child in interior.transform)
            if (child.name.StartsWith("PA_REF_CUBEWORLDKIT_CHEST") && (chest == null || child.localPosition.x > chest.localPosition.x)) chest = child;
        if (chest == null) { Debug.LogWarning("[DemoShopInterior] No interior chest for storage."); return; }
        var box = chest.gameObject.AddComponent<BoxCollider>();
        var renderers = chest.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            box.center = chest.InverseTransformPoint(bounds.center);
            Vector3 size = chest.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
        box.isTrigger = true;
        Storage = chest.gameObject.AddComponent<StorageBox>();
        Storage.boxName = "상점 보관함";
        Storage.maxSlotCount = 10;
        if (StorageUI.instance == null) new GameObject("StorageUI").AddComponent<StorageUI>();
    }

    // Continue: 저장 위치가 상점 footprint 안이면 문을 지나 들어온 것과 같은 실내 표시로 시작한다.
    public void RestoreForPlayer(Vector3 playerPosition) => ShowInterior(CoversGround(playerPosition));

    void ShowInterior(bool inside)
    {
        IsInside = inside;
        exterior.SetActive(!inside);
        interior.SetActive(inside);
        entrance.gameObject.SetActive(!inside);
        exit.gameObject.SetActive(inside);
    }
}
