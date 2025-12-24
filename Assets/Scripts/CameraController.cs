using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;       // 카메라가 쫓아갈 대상 (Player)
    public float smoothSpeed = 5f; // 따라가는 속도 (높을수록 빠릿빠릿함)
    private Vector3 offset;        // 대상과 카메라 사이의 거리(간격)

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

        // 1. 목표 위치 계산 (플레이어 현재 위치 + 아까 저장한 간격)
        Vector3 desiredPosition = target.position + offset;

        // 2. 부드러운 이동 (선형 보간 - Linear Interpolation)
        // 현재 위치에서 목표 위치까지 부드럽게 섞어줍니다.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. 적용
        transform.position = smoothedPosition;
    }
}