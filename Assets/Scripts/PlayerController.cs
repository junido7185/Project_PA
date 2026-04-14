using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed   = 5f;
    public float rotateSpeed = 480f; // 초당 회전 각도 — Inspector 에서 조절

    private CharacterController controller;
    private Animator anim;
    private float _modelYOffset;
    private float _verticalVelocity;

    public bool isSitting = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim       = GetComponentInChildren<Animator>();

        // ★ 루트 모션 비활성화
        // Mixamo 애니메이션의 루트 모션이 켜져 있으면 애니메이션이
        // 캐릭터의 위치·회전을 직접 덮어써서 '방향마다 순간이동' 현상이 발생한다.
        if (anim != null) anim.applyRootMotion = false;

        // 직접 자식(FBX 모델 루트)의 초기 localY를 오프셋으로 캐싱
        Transform modelRoot = transform.childCount > 0 ? transform.GetChild(0) : null;
        _modelYOffset = modelRoot != null ? modelRoot.localEulerAngles.y : 0f;

        // XZ 위치가 부모 중심에서 벗어나면 회전 시 원을 그리므로 0으로 정렬
        if (modelRoot != null)
        {
            Vector3 p = modelRoot.localPosition;
            modelRoot.localPosition = new Vector3(0f, p.y, 0f);
        }

        // 인벤토리 토글만 이벤트 구독 (이동은 Update 에서 직접 폴링)
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnInventoryToggle += ToggleInventory;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnInventoryToggle -= ToggleInventory;
    }

    void Update()
    {
        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) return;

        if (isSitting)
        {
            // 방향키 입력 시 일어나기
            Vector2 sittingInput = PlayerInputHandler.Instance != null
                ? PlayerInputHandler.Instance.MoveInput : Vector2.zero;
            if (sittingInput.magnitude > 0.01f) StandUp();
            return;
        }

        // ── 입력을 매 프레임 직접 읽기 (이벤트가 아닌 폴링) ──────────────────────
        Vector2 rawInput = PlayerInputHandler.Instance != null
            ? PlayerInputHandler.Instance.MoveInput : Vector2.zero;

        Vector3 moveDir = ComputeMoveDirection(rawInput);

        // ── 중력 ─────────────────────────────────────────────────────────────────
        if (controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f; // 지면에 붙어 있도록 약한 하향력 유지
        _verticalVelocity += -9.81f * Time.deltaTime;

        // ── 애니메이션 ───────────────────────────────────────────────────────────
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetFloat("Speed", moveDir.magnitude, 0.1f, Time.deltaTime);

        // ── 이동 ─────────────────────────────────────────────────────────────────
        Vector3 motion = moveDir * moveSpeed + Vector3.up * _verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        // ── 회전 (이동 방향으로 부드럽게) ────────────────────────────────────────
        if (moveDir.magnitude >= 0.1f)
        {
            float rawAngle    = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float targetAngle = rawAngle - _modelYOffset;
            Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }

    // ── 카메라 기준 이동 방향 계산 ───────────────────────────────────────────────
    // 동물의 숲처럼 W = 항상 화면 위쪽, A = 화면 왼쪽으로 이동.
    // 카메라 forward/right 를 XZ 평면에 투영해 화면 방향을 구한다.
    private Vector3 ComputeMoveDirection(Vector2 raw)
    {
        Vector2 clamped = Vector2.ClampMagnitude(raw, 1f);
        Camera cam = Camera.main;

        if (cam != null)
        {
            Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (camFwd.sqrMagnitude > 0.001f)
            {
                camFwd.Normalize();
                Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                return Vector3.ClampMagnitude(camFwd * clamped.y + camRight * clamped.x, 1f);
            }
        }
        return new Vector3(clamped.x, 0f, clamped.y); // 폴백: 카메라 없거나 완전 탑뷰
    }

    // ── 인벤토리 ─────────────────────────────────────────────────────────────────
    private void ToggleInventory()
    {
        if (InventoryUI.instance != null) InventoryUI.instance.Toggle();
    }

    // ── 앉기 / 일어나기 ──────────────────────────────────────────────────────────
    public void SitDown(Transform targetSeat)
    {
        isSitting          = true;
        controller.enabled = false;
        transform.position = targetSeat.position;
        transform.rotation = targetSeat.rotation;
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool("IsSitting", true);
    }

    public void StandUp()
    {
        isSitting          = false;
        _verticalVelocity  = 0f;
        transform.position += transform.forward * 1.0f;
        controller.enabled  = true;
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool("IsSitting", false);
    }
}
