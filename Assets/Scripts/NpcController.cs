using UnityEngine;
using UnityEngine.AI; // 내비게이션 쓰려면 필수!

public class NpcController : MonoBehaviour
{
    // NPC의 상태 정의 (FSM)
    public enum State { Idle, MovingToShop, Shopping }
    public State currentState = State.Idle;

    public Transform shopLocation; // 상점 위치 (목표)
    private NavMeshAgent agent;    // 이동 담당 컴포넌트
    private float timer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // 씬에 있는 'Shop_Bin'을 찾아서 목표로 설정 (임시)
        GameObject shop = GameObject.FindGameObjectWithTag("Shop");
        if (shop != null)
        {
            shopLocation = shop.transform;
        }
        
        // 처음엔 랜덤하게 딴짓하기
        ChangeState(State.Idle);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.MovingToShop:
                UpdateMovingToShop();
                break;
        }
    }

    // [상태 1] 배회하기 (Idle)
    void UpdateIdle()
    {
        timer += Time.deltaTime;

        // 3초마다 랜덤한 위치로 이동
        if (timer > 3f)
        {
            timer = 0;
            // 내 위치 주변 5미터 반경 랜덤 좌표
            Vector3 randomDir = Random.insideUnitSphere * 5f;
            randomDir += transform.position;
            
            NavMeshHit hit;
            // 갈 수 있는 땅인지 체크
            if (NavMesh.SamplePosition(randomDir, out hit, 5f, 1))
            {
                agent.SetDestination(hit.position);
            }

            // (테스트용) 10% 확률로 "상점 가자!" 상태로 변경
            if (Random.Range(0, 10) == 0)
            {
                ChangeState(State.MovingToShop);
            }
        }
    }

    // [상태 2] 상점으로 가기
    void UpdateMovingToShop()
    {
        // 상점에 거의 도착했니? (거리가 1.5m 이내)
        if (!agent.pathPending && agent.remainingDistance < 1.5f)
        {
            Debug.Log("🤖 NPC: 상점 도착! 물건을 삽니다.");
            
            // 실제 구매 로직 (플레이어에게 돈 주기)
            GameManager.instance.AddMoney(50); // 50원 지출
            
            // 다시 배회하러 감
            ChangeState(State.Idle);
        }
    }

    void ChangeState(State newState)
    {
        currentState = newState;
        
        if (newState == State.MovingToShop)
        {
            Debug.Log("🤖 NPC: 쇼핑하러 가볼까?");
            if (shopLocation != null)
            {
                agent.SetDestination(shopLocation.position);
            }
        }
    }
}