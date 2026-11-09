using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // §2.3 플레이어 이동 — 최대 이동 속도
    public float moveSpeed = 5f;

    // §2.3 조작감 — 동물의 숲 스타일 가속/감속/회전 파라미터 (Inspector에서 실시간 조절 가능)
    [Header("Movement Feel")]
    [SerializeField] float acceleration  = 18f;   // 가속률 (m/s²) — 권장 16~20 🐾
    [SerializeField] float deceleration  = 22f;   // 감속률 (m/s²) — 권장 20~25 🐾
    [SerializeField] float stopThreshold = 0.08f; // 정지 스냅 임계 (normalized) — 권장 0.05~0.12
    [SerializeField] float rotationSpeed = 12f;   // Slerp 회전 계수 — 권장 10~14 🐾

    [HideInInspector] public float rotateSpeed = 480f; // 구버전 호환용 — 더 이상 사용 안 함

    private CharacterController controller;
    private Animator anim;
    private float _modelYOffset;
    private float _verticalVelocity;

    // §2.3 조작감 — 런타임 이동 상태
    private float   _currentSpeed  = 0f;            // 현재 실제 속도 (0 ~ moveSpeed)
    private Vector3 _smoothMoveDir = Vector3.zero;  // 몸이 향한 이동 방향(속도는 항상 이 방향으로만 낸다)
    private float   _yaw, _yawVelocity;             // 이동 방향 요(도)와 SmoothDampAngle 상태

    // 2026-10-31 조작감 재구성(모동숲·딩컴 기준): 몸이 입력 방향으로 빠르게 돌고, 이동은 몸이 향한 쪽으로만 한다.
    // 이전에는 이동 방향과 몸 회전을 각각 Slerp해 방향 전환 때 몸이 늦게 돌며 옆으로 미끄러졌다(얼음판 느낌).
    // 급회전(뒤돌기)에서는 돌 동안 속도를 줄였다가 다시 붙여 발이 몸 방향과 어긋나지 않는다.
    const float MaxTurnDegPerSec = 1440f;
    const float SharpTurnMinSpeed = 0.3f; // 정반대 입력일 때 돌면서 유지하는 속도 비율
    float TurnSmoothTime => Mathf.Clamp(1.2f / Mathf.Max(1f, rotationSpeed), .035f, .12f);

    public bool isSitting = false;

    public float ActualPlanarSpeed { get; private set; }
    public bool IsAirborne => controller != null && !controller.isGrounded;
    public float VerticalSpeed => _verticalVelocity;
    public int JumpCount { get; private set; }
    public void ResetMotionAfterTeleport()
    {
        _verticalVelocity = 0; _currentSpeed = 0; ActualPlanarSpeed = 0;
        SyncYawToTransform();
    }

    // 텔레포트·앉기 해제·시작 시 이동 요를 현재 몸 방향에 맞춘다(모델 자식 회전 오프셋 포함).
    void SyncYawToTransform()
    {
        _yaw = transform.eulerAngles.y + _modelYOffset;
        _yawVelocity = 0f;
        _smoothMoveDir = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
    }
    // 대화가 시작되면 멈춰 선 채 말 상대 쪽으로 돈다(이동 입력이 들어오면 바로 취소). 이동 요(_yaw)와 같이 돌려 다음 걸음이 튀지 않는다.
    float? _faceYaw;
    public void TurnToward(Vector3 point)
    {
        Vector3 flat = point - transform.position; flat.y = 0f;
        if (flat.sqrMagnitude < .04f) return;
        _faceYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
    }

    public void ApplyOpeningFeel()
    {
        acceleration = 30f; deceleration = 36f; rotationSpeed = 20f;
    }

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

        // 🐾 첫 입력 시 (0,0,0)에서 시작하는 현상 방지 — 캐릭터 정면으로 초기화
        SyncYawToTransform();

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
        if (Time.timeScale == 0) { ActualPlanarSpeed=0; return; }
        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) return;
        // 📱 스마트폰 열려있을 때도 이동 차단 (인벤토리와 동일 패턴)
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen) return;
        // 🏷️ ShopPriceUI 열려있을 때 이동 차단
        if (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) return;

        if (isSitting)
        {
            // 방향키 입력 시 일어나기
            Vector2 sittingInput = PlayerInputHandler.Instance != null
                ? PlayerInputHandler.Instance.MoveInput : Vector2.zero;
            if (sittingInput.magnitude > 0.01f) StandUp();
            return;
        }

        // ── 입력을 매 프레임 직접 읽기 ──────────────────────────────────────────
        Vector2 rawInput  = PlayerInputHandler.Instance != null
            ? PlayerInputHandler.Instance.MoveInput : Vector2.zero;
        Vector3 desiredDir = ComputeMoveDirection(rawInput);

        // ── § 가속/감속 ──────────────────────────────────────────────────────────
        // Deadzone: desiredDir.magnitude(0~1) × moveSpeed = 목표 속도
        // 키보드는 항상 magnitude=1(최대), 게임패드는 반틸트면 0.5 등 자연스럽게 처리된다.
        float targetSpeed = desiredDir.magnitude * moveSpeed *
            (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.RunHeld ? 1.6f : 1f);

        // ── § 몸 방향(요) — 입력이 있을 때만 빠르게 돌린다. 입력이 없으면 마지막 방향을 유지한 채 멈춘다.
        if (desiredDir.magnitude >= 0.1f)
        {
            float targetYaw = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;
            _yaw = Mathf.SmoothDampAngle(_yaw, targetYaw, ref _yawVelocity, TurnSmoothTime, MaxTurnDegPerSec, Time.deltaTime);
            _smoothMoveDir = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            // 몸 방향과 입력이 어긋난 만큼(급회전) 목표 속도를 줄인다: 정면 1, 직각 약 0.65, 정반대 0.3.
            float align = Vector3.Dot(_smoothMoveDir, desiredDir.normalized);
            targetSpeed *= Mathf.Lerp(SharpTurnMinSpeed, 1f, Mathf.Clamp01(align * .5f + .5f));
        }

        if (targetSpeed > _currentSpeed)
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, deceleration * Time.deltaTime);

        // §정지 마찰: 속도가 임계 이하이고 입력 없으면 즉시 0 스냅 (떨림 방지)
        if (_currentSpeed < stopThreshold * moveSpeed && targetSpeed == 0f)
            _currentSpeed = 0f;

        // ── 중력 ─────────────────────────────────────────────────────────────────
        if (controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f; // 지면에 붙어 있도록 약한 하향력 유지
        if (controller.isGrounded && PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.JumpPressed)
        { _verticalVelocity = 5.5f; JumpCount++; }
        _verticalVelocity += -9.81f * Time.deltaTime;

        // ── 이동 적용 ─────────────────────────────────────────────────────────────
        Vector3 motion = _smoothMoveDir * _currentSpeed + Vector3.up * _verticalVelocity;
        Vector3 beforeMove = transform.position;
        controller.Move(motion * Time.deltaTime);
        ActualPlanarSpeed = Vector3.ProjectOnPlane(transform.position-beforeMove,Vector3.up).magnitude / Mathf.Max(.001f,Time.deltaTime);

        if (desiredDir.magnitude >= 0.1f) _faceYaw = null;
        else if (_faceYaw.HasValue)
        {
            _yaw = Mathf.SmoothDampAngle(_yaw, _faceYaw.Value, ref _yawVelocity, .12f, 720f, Time.deltaTime);
            _smoothMoveDir = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            transform.rotation = Quaternion.Euler(0f, _yaw - _modelYOffset, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, _faceYaw.Value)) < 1f) _faceYaw = null;
        }

        // ── 몸 회전 = 이동 요(이중 보간 없음). 제자리에서 방향키만 눌러도 몸이 돌아선다.
        if (desiredDir.magnitude >= 0.1f || _currentSpeed >= 0.05f)
            transform.rotation = Quaternion.Euler(0f, _yaw - _modelYOffset, 0f);

        // ── 애니메이션 Speed ──────────────────────────────────────────────────────
        // 물리 가속도 자체가 Damping 역할을 하므로 dampTime 인자를 제거한다.
        // 이를 통해 Idle↔Walk 블렌딩이 실제 이동 속도와 정확히 동기화된다.
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetFloat("Speed", ActualPlanarSpeed / moveSpeed, .08f, Time.deltaTime);
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
        isSitting         = false;
        _verticalVelocity = 0f;

        // 🐾 앉아있다 일어날 때 이전 관성 잔재 리셋 — 이상한 방향으로 미끄러짐 방지
        _currentSpeed  = 0f;
        SyncYawToTransform();

        transform.position += transform.forward * 1.0f;
        controller.enabled  = true;
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool("IsSitting", false);
    }
}
