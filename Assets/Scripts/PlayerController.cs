using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // 이동 속도 (Inspector에서 조절 가능)
    private CharacterController controller; // 캐릭터 컨트롤러 컴포넌트

    // 👇 추가: 애니메이터 변수
    private Animator anim;

    void Start()
    {
        // 내 몸에 붙어있는 CharacterController를 찾아온다.
        controller = GetComponent<CharacterController>();

        // 👇 추가: 자식 오브젝트에 있는 Animator 찾기
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 1. 키보드 입력 받기 (WASD 또는 화살표)
        // Unity의 레거시 입력 시스템 사용
        float h = Input.GetAxisRaw("Horizontal"); // A, D, 좌, 우 (-1 ~ 1)
        float v = Input.GetAxisRaw("Vertical");   // W, S, 상, 하 (-1 ~ 1)

        // 2. 이동 방향 벡터 만들기 (x, y, z)
        // 쿼터뷰에서는 위(W)를 누르면 (0, 0, 1)이 아니라 (1, 0, 1)처럼 대각선으로 보여야 할 수도 있지만,
        // 우선 기본적인 월드 기준 이동으로 구현합니다.
        Vector3 direction = new Vector3(h, 0, v).normalized;

        // 👇 추가: 애니메이션 제어
        // 움직임 벡터의 크기(magnitude)를 Speed 파라미터로 전달
        // 0이면 멈춤(Idle), 1이면 달림(Run)
        if (anim != null)
        {
            // DampTime 0.1f를 주면 값이 부드럽게 변해서 모션이 자연스러워짐
            anim.SetFloat("Speed", direction.magnitude, 0.1f, Time.deltaTime);
        }

        // 3. 이동하기
        if (direction.magnitude >= 0.1f)
        {
            // 움직이는 방향으로 캐릭터 회전시키기 (선택 사항)
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

            // 실제 이동 (방향 * 속도 * 프레임보정)
            controller.Move(direction * moveSpeed * Time.deltaTime);
        }
    }
}