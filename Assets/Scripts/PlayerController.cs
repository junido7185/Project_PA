using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; 
    private CharacterController controller; 
    private Animator anim;

    // 👇 [추가] 앉은 상태인지 확인하는 깃발
    public bool isSitting = false; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // ⭐ [추가] 'I' 키를 누르면 인벤토리 토글
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (InventoryUI.instance != null)
            {
                InventoryUI.instance.Toggle();
            }
        }

        // 인벤토리가 열려있으면 이동 막기 (선택 사항 - 원하면 주석 해제)
        if (InventoryUI.instance.gameObject.activeSelf) return;

        // ⭐ [추가] 앉아있을 때는 이동 로직을 막습니다!
        if (isSitting)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            {
                StandUp();
            }
            return; 
        }

        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical");   

        Vector3 direction = new Vector3(h, 0, v).normalized;

        if (anim != null)
        {
            anim.SetFloat("Speed", direction.magnitude, 0.1f, Time.deltaTime);
        }

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

            controller.Move(direction * moveSpeed * Time.deltaTime);
        }
    }

    // 앉기 함수
    public void SitDown(Transform targetSeat)
    {
        isSitting = true;
        controller.enabled = false; // 물리 충돌 및 이동 끄기 (텔레포트 위해 필수)
        
        // 의자의 '앉는 위치(SitPoint)'로 순간이동 & 의자 방향 보기
        transform.position = targetSeat.position;
        transform.rotation = targetSeat.rotation;

        if (anim != null) 
        {
            anim.SetBool("IsSitting", true); // 애니메이터에 파라미터 전달
        }
    }

    // 일어나기 함수
    public void StandUp()
    {
        isSitting = false;
        
        // ❌ [기존 코드의 문제점]
        // controller.enabled = true; // 먼저 켜버리면...
        // transform.position += ...  // 이동할 때 물리 충돌이 발생해서 튕겨나감!

        // ✅ [수정된 코드] 순서 변경!
        // 1. 먼저 안전한 곳(의자 앞)으로 이동시킵니다.
        transform.position += transform.forward * 1.0f; 

        // 2. 이동이 끝난 뒤에 물리 엔진(컨트롤러)을 켭니다.
        controller.enabled = true; 

        if (anim != null) 
        {
            anim.SetBool("IsSitting", false);
        }
    }
}