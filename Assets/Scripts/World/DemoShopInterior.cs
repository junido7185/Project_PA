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
        var light = interior.transform.Find("WarmInteriorLight")?.GetComponent<Light>();
        if (light != null) { light.intensity = 8f; light.color = new Color(1f, .88f, .74f); }
        ShowInterior(false);
    }

    void ShowInterior(bool inside)
    {
        IsInside = inside;
        exterior.SetActive(!inside);
        interior.SetActive(inside);
        entrance.gameObject.SetActive(!inside);
        exit.gameObject.SetActive(inside);
    }
}
