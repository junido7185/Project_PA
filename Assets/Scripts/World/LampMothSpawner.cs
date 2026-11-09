using UnityEngine;

// 등불 나방(Content-insects-plan §C.3): 가게 문 양옆 등불 가까이 두 마리. 17시~다음 날 6시에만 보인다(낮에는 개체를 끈다).
// 가게가 정착 뒤에 생기므로 가게가 생길 때까지 기다렸다가 한 번 놓는다. 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로.
public sealed class LampMothSpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Moth_";
    const float EveningFrom = 17f, MorningUntil = 6f;
    BugCritter[] _moths;
    float _nextCheck;

    public static bool Evening(float hour) => hour >= EveningFrom || hour < MorningUntil;
    public int Spawned => _moths != null ? _moths.Length : 0;

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_moths == null) TrySpawn();
        if (_moths == null) return;
        bool on = Evening(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        foreach (var moth in _moths) if (moth != null && moth.gameObject.activeSelf != on) moth.gameObject.SetActive(on);
    }

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Moth");
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        if (item == null || item.model == null || shop == null) return;
        var interior = shop.GetComponentInParent<DemoShopInterior>(true);
        Vector3 door = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : shop.transform.position;
        Vector3 outward = Vector3.ProjectOnPlane(door - shop.transform.position, Vector3.up);
        outward = outward.sqrMagnitude > .04f ? outward.normalized : Vector3.ProjectOnPlane(shop.transform.forward, Vector3.up).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, outward);
        _moths = new BugCritter[2];
        for (int i = 0; i < 2; i++)
        {
            Vector3 at = door + outward * .6f + lateral * (i == 0 ? 2.4f : -2.4f);
            if (Physics.Raycast(at + Vector3.up * 6f, Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
            at += Vector3.up * 1.45f; // 등불 높이 근처
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = at;
            FirstDayStudioAssets.Place(item.model, go.transform, at, .1f); // 첫 .05는 기본 카메라에서 점처럼 작았다(moth-r1)
            go.AddComponent<SphereCollider>().isTrigger = true;
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.7f + .4f);
            _moths[i] = critter;
        }
    }
}
