using UnityEngine;

// 🚪 Docs/08 §건물 진입 시스템 — 문 앞에서 F키로 실내/실외 워프
//
// 사용 방식 (Moonlighter2 식 실내 진입):
//   · 건물 외부에 빈 GameObject "Door_Out" → 이 컴포넌트 + Collider(Trigger off) + Layer=Interact
//       · targetSpawn = 실내 Y=+100 오프셋 영역의 "PlayerSpawn_Inside" 지점
//   · 건물 내부 벽면에 빈 GameObject "Door_In" → 이 컴포넌트 + Collider
//       · targetSpawn = 외부의 "PlayerSpawn_Outside" 지점
//
// 단일 씬 전략(씬 분리 안 함)이므로 NavMesh·싱글톤 서비스·SaveManager 상태가 모두 유지된다.
// Docs/08 §2 레이아웃의 "상점 중심 방사형" 배치와 호환.
public class BuildingEntrance : MonoBehaviour, IInteractable
{
    [Header("워프 대상")]
    [Tooltip("플레이어가 이 문을 이용해 도착할 위치(와 바라볼 방향)")]
    [SerializeField] Transform targetSpawn;

    [Header("UI")]
    [SerializeField] string promptLabel = "문 열기"; // "상점 입장", "상점 나가기" 등으로 Inspector에서 개별 설정

    public string GetInteractPrompt() => promptLabel;

    public void Interact(GameObject interactor)
    {
        if (targetSpawn == null)
        {
            Debug.LogWarning($"🚪 {name} — targetSpawn 이 비어있어 워프 불가");
            return;
        }

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.PlayWarpFade(() => Warp(interactor));
        else
            Warp(interactor); // 페이더 없어도 동작은 유지 — 디버그 편의
    }

    // ── 실제 텔레포트 수행 (페이드 미드포인트에서 호출) ─────────────────
    private void Warp(GameObject interactor)
    {
        // CharacterController.enabled 토글 없이 position 을 직접 대입하면
        // 내부 누적 이동량이 한 프레임 뒤에 덮어써 실패한다. 반드시 끄고 옮긴 뒤 다시 켠다.
        CharacterController cc = interactor.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        interactor.transform.SetPositionAndRotation(targetSpawn.position, targetSpawn.rotation);

        if (cc != null) cc.enabled = true;

        // 카메라가 Lerp 로 따라오면 실내→실외 구간을 한 번에 가로질러 패닝이 보인다 → 즉시 스냅
        CameraController cam = Camera.main != null
            ? Camera.main.GetComponent<CameraController>()
            : null;
        if (cam != null) cam.SnapToTarget();

        Debug.Log($"🚪 워프: {interactor.name} → {targetSpawn.name}");
    }
}
