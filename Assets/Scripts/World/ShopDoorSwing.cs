using UnityEngine;

// 2026-11-07 영업 손님이 문 틈으로 드나들게 된 뒤(FrontWallNavBlock) 닫힌 나무 문을 그대로 통과해 보였다.
// 플레이어·손님·동행이 문 앞 1.7m 안에 오면 문짝(Door·DoorGlass·Handle)이 왼쪽 경첩을 축으로 바깥으로 열리고, 멀어지면 닫힌다.
// 표시 전용: 충돌·NavMesh·문 순간이동(DemoShopInterior)·저장은 그대로. 가게 프리팹은 바꾸지 않고 실행 중에 경첩만 만든다.
public sealed class ShopDoorSwing : MonoBehaviour
{
    public static int Openings { get; private set; }
    public static float CurrentAngle { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Openings = 0; CurrentAngle = 0f; }

    const float OpenAngle = 85f, Speed = 260f, Reach = 1.7f, HingeX = -.775f, DoorZ = -7.5f;

    Transform _hinge, _facade;
    float _angle, _bindAt, _senseAt;
    bool _wantOpen;

    void Update()
    {
        if (_hinge == null)
        {
            if (Time.time < _bindAt) return;
            _bindAt = Time.time + 1f;
            Bind();
            if (_hinge == null) return;
        }
        if (Time.time >= _senseAt)
        {
            _senseAt = Time.time + .15f;
            bool open = Someone(_facade.TransformPoint(new Vector3(0f, 0f, DoorZ)));
            if (open && !_wantOpen) Openings++;
            _wantOpen = open;
        }
        _angle = Mathf.MoveTowards(_angle, _wantOpen ? OpenAngle : 0f, Speed * Time.deltaTime);
        _hinge.localRotation = Quaternion.Euler(0f, _angle, 0f);
        CurrentAngle = _angle;
    }

    void Bind()
    {
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        if (shop == null) return;
        _facade = null;
        foreach (var t in shop.GetComponentsInChildren<Transform>(true)) if (t.name == "ShopArchitecture") { _facade = t; break; }
        if (_facade == null) return;
        var door = _facade.Find("Door");
        if (door == null) return;
        var hinge = new GameObject("DoorHinge").transform;
        hinge.SetParent(_facade, false);
        hinge.localPosition = new Vector3(HingeX, 0f, DoorZ);
        foreach (var name in new[] { "Door", "DoorGlass", "Handle" })
        {
            var part = _facade.Find(name);
            if (part != null) part.SetParent(hinge, true);
        }
        _hinge = hinge;
    }

    static bool Someone(Vector3 door)
    {
        float reach2 = Reach * Reach;
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        if (player != null && Flat(player.position - door) < reach2) return true;
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
            if (npc != null && npc.isActiveAndEnabled && Flat(npc.transform.position - door) < reach2) return true;
        foreach (var resident in FindObjectsByType<DemoResident>(FindObjectsSortMode.None))
            if (resident != null && resident.isActiveAndEnabled && Flat(resident.transform.position - door) < reach2) return true;
        return false;
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
