using UnityEngine;

// 배치 꾸밈물(Furniture)의 보이는 모델을 실제 바닥 높이에 맞춘다(2026-11-03 beach/dig-r94: 격자 칸 높이보다 0.3~0.4m 높은
// 흙 단 위에 놓은 토기 모자이크가 통째로 묻히고 유리 모빌은 받침대가 묻혔다). 루트 위치·회전·칸·저장은 배치 권위
// (WorldBuildingPlacementService) 그대로 두고, 자식(모델)만 위로 올린다. 표현 전용.
public sealed class PlacedFloorSnap : MonoBehaviour
{
    const float MaxLift = .9f;
    float _applied;
    Vector3 _lastPosition = new Vector3(float.NaN, 0f, 0f);

    public float Lift => _applied;

    void LateUpdate()
    {
        if (transform.position == _lastPosition) return;
        _lastPosition = transform.position;
        float target = SurfaceLift();
        float delta = target - _applied;
        if (Mathf.Abs(delta) < .005f) return;
        Vector3 up = transform.InverseTransformVector(Vector3.up * delta);
        foreach (Transform child in transform) child.localPosition += up;
        _applied = target;
    }

    // 루트 바로 위에서 아래로: 자기 몸·트리거·움직이는 것(플레이어·NPC)을 빼고 루트보다 높은 가장 높은 바닥.
    float SurfaceLift()
    {
        Vector3 origin = transform.position + Vector3.up * (MaxLift + .6f);
        float best = 0f;
        foreach (var hit in Physics.RaycastAll(origin, Vector3.down, MaxLift + 1.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            var c = hit.collider;
            if (c == null || c.transform.IsChildOf(transform) || c.attachedRigidbody != null || c is CharacterController
                || c.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue;
            float lift = hit.point.y - transform.position.y;
            if (lift > best && lift <= MaxLift) best = lift;
        }
        return best;
    }
}
