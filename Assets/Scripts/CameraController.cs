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
            _distance = Mathf.SmoothDamp(_distance, OpeningDistance * (OpeningBuildMode ? 1.65f : 1f), ref _zoomVelocity, .22f);
            offset = OpeningOffset(_distance);
            transform.rotation = Quaternion.Euler(_pitch, 0, 0);
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref _followVelocity, .16f);
            return;
        }
        // 1. 목표 위치 계산 (플레이어 현재 위치 + 아까 저장한 간격)
        Vector3 desiredPosition = target.position + offset;

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
        transform.position = target.position + offset;
    }
}