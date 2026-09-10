using UnityEngine;

// OPENING-FEEL-001: 기존 opening 객체의 조작/표현 설정과 안전 복귀만 연결한다.
[DefaultExecutionOrder(250)]
public sealed class OpeningFeelPresentation : MonoBehaviour
{
    DepartureCompanionSelection _selection;
    DepartureVoyagePresentation _voyage;
    PlayerController _player;
    CharacterController _body;
    Vector3 _safe;
    bool _ready;
    public int Recoveries { get; private set; }

    void Awake()
    {
        // 보류된 미커밋 P4를 보존하되 이번 opening 실행에서는 활성화하지 않는다.
        foreach (var behaviour in GetComponents<Behaviour>())
            if (behaviour.GetType().Name == "FirstProductionController") behaviour.enabled = false;
        _selection = GetComponent<DepartureCompanionSelection>();
        _voyage = GetComponent<DepartureVoyagePresentation>();
    }

    void Update()
    {
        if (!_ready)
        {
            var t = _selection.Tutorial;
            if (t == null || !t.IsReady) return;
            _player = t.player.GetComponent<PlayerController>();
            _body = t.player.GetComponent<CharacterController>();
            _player.ApplyOpeningFeel();
            _safe = t.player.position;
            var camera = Camera.main.GetComponent<CameraController>();
            camera.ConfigureOpening(t.player);
            camera.SnapToTarget();
            var anchor = t.trainingSlot.transform.Find("InteractionAnchor");
            if (anchor == null) anchor = new GameObject("InteractionAnchor").transform;
            anchor.SetParent(t.trainingSlot.transform,false);
            anchor.localPosition = new Vector3(0,0,-1.1f);
            anchor.localRotation = Quaternion.Euler(0,180,0);
            // 기존 LogisticsFloor 실제 치수를 따른 항구 난간. 출항은 선택 UI로 안내한다.
            Boundary("HarborBackRail",new Vector3(0,.55f,6.7f),new Vector3(28.6f,1.1f,.18f));
            Boundary("HarborFrontRail",new Vector3(0,.55f,-8.1f),new Vector3(28.6f,1.1f,.18f));
            Boundary("HarborLeftRail",new Vector3(-14.2f,.55f,-.7f),new Vector3(.18f,1.1f,15));
            Boundary("HarborRightRail",new Vector3(14.2f,.55f,-.7f),new Vector3(.18f,1.1f,15));
            _ready=true;
        }
        // 완주 CTA는 실제 빈 진열대와 캐릭터를 가리지 않는 오른쪽에 둔다.
        var completion = _selection.Tutorial.presentation.CompanionButton.transform.parent as RectTransform;
        if (completion != null) completion.anchoredPosition = new Vector2(600f,0f);
        if (_selection.Tutorial.Complete && !_selection.IsConfirmed)
            _selection.Tutorial.player.GetComponent<PlayerInteraction>().enabled = false;
        if (_voyage.Sailing)
            _safe = _voyage.Boat.position + new Vector3(0,1.5f,-.9f);
        else if (!_voyage.Arrived && _body.isGrounded && _player.transform.position.y>-.5f)
            _safe = _player.transform.position;
        else if (_voyage.Arrived)
        {
            var guard = _player.GetComponent<WorldPlayerTraversalGuard>();
            if (guard != null) _safe=guard.LastSafePosition;
        }
        if (_player.transform.position.y < -3f)
        {
            bool enabledBefore=_body.enabled;_body.enabled=false;
            _player.transform.position=_safe;_player.ResetMotionAfterTeleport();_body.enabled=enabledBefore;
            Recoveries++;Debug.Log("[OPENING-FEEL] FALL_RECOVERED");
        }
    }

    void Boundary(string label,Vector3 position,Vector3 size)
    {
        var rail=GameObject.CreatePrimitive(PrimitiveType.Cube);rail.name=label;
        rail.transform.SetParent(transform,false);rail.transform.position=position;rail.transform.localScale=size;
        rail.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("DepartureTutorial/Materials/PA_Teal");
    }
}
