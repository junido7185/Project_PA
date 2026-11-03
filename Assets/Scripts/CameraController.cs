using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;       // 카메라가 쫓아갈 대상 (Player)
    public float smoothSpeed = 5f; // 따라가는 속도 (높을수록 빠릿빠릿함)
    private Vector3 offset;        // 대상과 카메라 사이의 거리(간격)

    public bool OpeningFraming { get; private set; }
    public bool OpeningBuildMode { get; private set; }
    public float OpeningDistance { get; private set; } = 12.5f;
    Vector3 _followVelocity;
    float _zoomVelocity, _pitch = 40f, _fov = 34f, _distance = 12.5f;
    float _obstructionDistance = 12.5f;
    // 2026-10-31 사용자 피드백: 낚시 중 찌가 화면 아래 안내 상자·핫바에 가려진다. 줄을 던져 둔 동안 구도 중심을
    // 플레이어와 찌(또는 뭍에 오른 물고기) 사이로 옮기고, 화면 아래 UI 높이만큼 더 내려 찌가 UI 위에 보이게 한다.
    const float FishingFocusShare = .62f, FishingFocusExtraDown = 1.1f, FishingFocusSmooth = .32f;
    Vector3 _focusShift, _focusShiftVelocity;
    public Vector3 FocusShift => _focusShift;

    Vector3 FishingFocusTarget()
    {
        var spot = FishingSpot.ActiveCast;
        if (spot == null || target == null || FishingSpot.ActiveDirectCaster == null ||
            !target.IsChildOf(FishingSpot.ActiveDirectCaster.transform) && !FishingSpot.ActiveDirectCaster.transform.IsChildOf(target))
            return Vector3.zero;
        Vector3 point;
        if (spot.LandedFish != null) point = spot.LandedFish.transform.position;
        else if (spot.Bobber != null && spot.Bobber.activeInHierarchy) point = spot.Bobber.transform.position;
        else point = spot.WaterTarget;
        Vector3 toward = point - target.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 12f * 12f) return Vector3.zero; // 찌 위치가 비정상이면 구도를 건드리지 않는다
        // 카메라 요가 0이라 화면 아래 = 월드 −z. 찌가 화면 아래쪽(−z)에 있을수록 UI를 피해 더 내려간다.
        float down = Mathf.Clamp01(-toward.normalized.z);
        return toward * FishingFocusShare + Vector3.back * (FishingFocusExtraDown * down);
    }

    // 2026-11-03 레퍼런스(딩컴 대화 장면 04/05): 대화하는 동안 카메라가 조금 다가가 두 사람 사이를 잡고,
    // 아래 대화창 높이만큼 구도를 내려 두 사람이 창 위에 보이게 한다. 창이 닫히면 원래 따라가기로 돌아온다.
    const float DialogueFocusShare = .5f, DialogueFocusDown = 1.3f, DialogueZoom = .74f;
    public bool DialogueFraming { get; private set; }
    Vector3 DialogueFocusTarget()
    {
        var speaker = DialogueUI.Speaker;
        DialogueFraming = false;
        if (speaker == null || target == null || !DialogueUI.IsOpen) return Vector3.zero;
        Vector3 toward = speaker.position - target.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 6f * 6f) return Vector3.zero;
        DialogueFraming = true;
        return toward * DialogueFocusShare + Vector3.back * DialogueFocusDown;
    }

    // OPENING-FEEL-001: 같은 follow 권위 안에서만 가까운 플레이/배치 구도를 전환한다.
    public void ConfigureOpening(Transform subject, float distance = 12.5f, float pitch = 40f, float fov = 34f)
    {
        target = subject; OpeningFraming = true; OpeningDistance = distance; _pitch = pitch; _fov = fov;
        var camera = GetComponent<Camera>(); camera.orthographic = false; camera.fieldOfView = fov;
        _distance = distance; _followVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(pitch, 0, 0);
        offset = OpeningOffset(distance);
    }
    public void SetOpeningBuildMode(bool active) { OpeningBuildMode = active; }
    Vector3 OpeningOffset(float distance)
    {
        // 시선 중심을 머리 위에 두어 몸 중심이 화면 위에서 약 62%에 놓인다.
        return Vector3.up * 2.3f - Quaternion.Euler(_pitch, 0, 0) * Vector3.forward * distance;
    }

    void Start()
    {
        // 게임 시작 시점의 카메라 위치와 플레이어 위치의 차이를 계산해서 저장해둡니다.
        // 즉, 에디터에서 잡아둔 그 '얼짱 각도' 거리를 계속 유지하겠다는 뜻입니다.
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    // Update가 아니라 LateUpdate를 쓰는 이유:
    // Player가 Update에서 먼저 움직이고 난 뒤에, 카메라가 그 위치로 이동해야 덜덜 떨림(Jitter)이 없습니다.
    void LateUpdate()
    {
        if (target == null) return;

        if (OpeningFraming)
        {
            Vector3 shift = FishingFocusTarget();
            Vector3 talk = shift == Vector3.zero ? DialogueFocusTarget() : Vector3.zero;
            if (shift == Vector3.zero) shift = talk;
            float zoom = OpeningBuildMode ? 1.65f : DialogueFraming ? DialogueZoom : 1f;
            _distance = Mathf.SmoothDamp(_distance, OpeningDistance * zoom, ref _zoomVelocity, DialogueFraming ? .45f : .22f);
            offset = OpeningOffset(_distance);
            transform.rotation = Quaternion.Euler(_pitch, 0, 0);
            _focusShift = Vector3.SmoothDamp(_focusShift, shift, ref _focusShiftVelocity, DialogueFraming ? .4f : FishingFocusSmooth);
            Vector3 anchor = target.position + _focusShift;
            Vector3 focus = anchor + Vector3.up * 1.4f;
            Vector3 desired = anchor + offset;
            Vector3 ray = desired - focus;
            float clear = ray.magnitude;
            foreach (var hit in Physics.SphereCastAll(focus, .22f, ray.normalized, clear, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(target) || hit.distance < .1f) continue;
                // 지붕이 걷힌 실내의 플레이어 경계는 보이는 카메라 장애물이 아니다.
                var interior = hit.collider.GetComponentInParent<DemoShopInterior>();
                if (interior != null && interior.IsInside) continue;
                clear = Mathf.Min(clear, Mathf.Max(2.2f, hit.distance - .25f));
            }
            _obstructionDistance = clear < _obstructionDistance ? clear : Mathf.MoveTowards(_obstructionDistance, clear, Time.deltaTime * 5f);
            desired = focus + ray.normalized * Mathf.Min(clear, _obstructionDistance);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _followVelocity, .10f);
            return;
        }
        // 1. 목표 위치 계산 (플레이어 현재 위치 + 아까 저장한 간격, 낚시 중이면 찌 쪽 구도 이동)
        _focusShift = Vector3.SmoothDamp(_focusShift, FishingFocusTarget(), ref _focusShiftVelocity, FishingFocusSmooth);
        Vector3 desiredPosition = target.position + _focusShift + offset;

        // 2. 부드러운 이동 (선형 보간 - Linear Interpolation)
        // 현재 위치에서 목표 위치까지 부드럽게 섞어줍니다.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. 적용
        transform.position = smoothedPosition;
    }

    // 🚪 Docs/08 §건물 진입 — 플레이어 순간이동 직후 카메라를 즉시 타겟에 고정
    // Lerp 경로가 실내·실외 사이를 가로지르면 긴 패닝이 보이므로 1프레임만에 스냅해야 한다.
    public void SnapToTarget()
    {
        if (target == null) return;
        _focusShift = _focusShiftVelocity = Vector3.zero;
        transform.position = target.position + offset;
    }
}
