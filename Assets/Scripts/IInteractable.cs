using UnityEngine;

// "나랑 상호작용 하려면 이 기능을 반드시 구현해!" 라는 계약서
public interface IInteractable
{
    // 상호작용 실행 함수
    // interactor: 나를 건드린 사람 (주로 플레이어)
    void Interact(GameObject interactor);

    // (선택 사항) 화면에 띄울 안내 문구
    // 예: "F키를 눌러 앉기", "F키를 눌러 상점 열기"
    string GetInteractPrompt();
}