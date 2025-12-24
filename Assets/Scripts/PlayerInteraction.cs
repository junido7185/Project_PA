using UnityEngine;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2.0f; // 상호작용 가능한 거리 (2미터)
    public LayerMask interactLayer;       // (심화) 특정 레이어만 감지할 때 사용
    private Animator anim; // 애니메이터 가져오기

    void Start()
    {
        // 내 몸(또는 자식)에 있는 Animator 찾기
        anim = GetComponentInChildren<Animator>();
    }
    void Update()
    {
        // 스페이스바(Space)를 눌렀을 때 실행
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // 1. 레이저 시작점: 발바닥(transform.position)이 아니라 가슴 높이 정도(Vector3.up * 0.5f)에서 쏴야 함.
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        // 2. 레이저 방향: 플레이어가 바라보는 앞쪽(transform.forward)
        Vector3 direction = transform.forward;

        // 3. 레이캐스트 발사! (충돌 정보는 hit 변수에 담김)
        RaycastHit hit;

        // ⭐ 핵심 변경: Raycast -> SphereCast (구체를 쏘는 것)
        // 반지름(radius)을 0.5f로 줘서 두꺼운 빔을 쏩니다.
        float radius = 0.5f;
        
        // 디버그용 그림도 레이저 대신 동그라미가 나가는 걸 표현하긴 어려우니 선으로 유지하되,
        // 마음속으로는 "이 선 주변 0.5미터는 다 맞는다"고 생각하세요.
        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        // Physics.SphereCast(시작점, 반지름, 방향, 결과담을변수, 거리)
        if (Physics.SphereCast(origin, radius, direction, out hit, interactDistance))
        {
            // 4. 무엇에 맞았는지 확인
            if (hit.collider.CompareTag("Tree"))
            {
                // 바로 채집하지 말고, 코루틴으로 '시간차'를 둡니다.
                StartCoroutine(ChopAndHarvest(hit.collider.gameObject));
            }
            else if (hit.collider.CompareTag("Shop"))
            {
                Debug.Log("🏪 상점 접속!");
                
                // 상점 스크립트를 가져와서 판매 함수 실행
                Shop shop = hit.collider.GetComponent<Shop>();
                if (shop != null)
                {
                    shop.SellAllItems();
                }
            }
            else if (hit.collider.CompareTag("NPC"))
            {
                Debug.Log("💬 NPC와 대화를 시작합니다.");
            }
            else if (hit.collider.CompareTag("Worktable"))
            {
                Crafter crafter = hit.collider.GetComponent<Crafter>();
                if (crafter != null)
                {
                    // 도끼질 모션 재활용 (제작 모션처럼 보이게!)
                    if (anim != null) anim.SetTrigger("DoChop"); 
                    
                    Debug.Log("⚙️ 가공 시작...");
                    crafter.Craft();
                }
            }
        }
    }

    // ⏳ 시간차 공격 함수 (Coroutine)
    IEnumerator ChopAndHarvest(GameObject treeObj)
    {
        // 1. 도끼질 애니메이션 실행!
        if (anim != null)
        {
            anim.SetTrigger("DoChop"); 
        }

        // 2. 도끼가 내려가는 시간(약 0.5초)만큼 기다림
        // (애니메이션 속도에 맞춰서 조절하세요)
        yield return new WaitForSeconds(0.5f);

        // 3. 나무가 아직 존재하면 채집 실행
        if (treeObj != null)
        {
            Gatherable gatherable = treeObj.GetComponent<Gatherable>();
            if (gatherable != null)
            {
                Debug.Log("🪓 쩍!");
                gatherable.Harvest();
            }
        }
    }
}