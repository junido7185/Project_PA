using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private CharacterController controller;
    private Animator anim;

    public bool isSitting = false;

    // 현재 이동 방향 — PlayerInputHandler.OnMoveChanged 콜백으로 갱신된다.
    private Vector3 _moveDir = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim       = GetComponentInChildren<Animator>();

        // Input System 이벤트 구독 (모든 Awake 이후 Start 에서 구독 → Instance 보장)
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnMoveChanged     += HandleMoveInput;
            PlayerInputHandler.Instance.OnInventoryToggle += ToggleInventory;
        }
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnMoveChanged     -= HandleMoveInput;
            PlayerInputHandler.Instance.OnInventoryToggle -= ToggleInventory;
        }
    }

    void Update()
    {
        // 인벤토리 열려있으면 이동 차단
        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) return;

        if (isSitting)
        {
            // 방향키 입력이 들어오면 일어나기
            if (_moveDir.magnitude > 0.01f) StandUp();
            return;
        }

        if (anim != null)
            anim.SetFloat("Speed", _moveDir.magnitude, 0.1f, Time.deltaTime);

        if (_moveDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(_moveDir.x, _moveDir.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            controller.Move(_moveDir * moveSpeed * Time.deltaTime);
        }
    }

    // -------- 입력 핸들러 --------

    private void HandleMoveInput(Vector2 input)
    {
        _moveDir = new Vector3(input.x, 0f, input.y);
    }

    private void ToggleInventory()
    {
        if (InventoryUI.instance != null) InventoryUI.instance.Toggle();
    }

    // -------- 앉기/일어나기 --------

    public void SitDown(Transform targetSeat)
    {
        isSitting        = true;
        controller.enabled = false;

        transform.position = targetSeat.position;
        transform.rotation = targetSeat.rotation;

        if (anim != null) anim.SetBool("IsSitting", true);
    }

    public void StandUp()
    {
        isSitting = false;
        transform.position += transform.forward * 1.0f;
        controller.enabled  = true;
        if (anim != null) anim.SetBool("IsSitting", false);
    }
}
