using UnityEngine;

// §레퍼런스.html — 인벤토리 패널을 캐릭터 머리 위에 고정하는 스크린 추적 컴포넌트
// 딩컴/동물의 숲 스타일: 3D 캐릭터의 월드 좌표를 매 프레임 화면 좌표로 변환해
// Screen Space Overlay 캔버스 상의 RectTransform 위치를 갱신한다.
//
// 왜 World Space Canvas 가 아닌가:
//   기존 InventoryUI/InventorySlotUI 가 Screen Space Overlay 기반 드래그&드롭을
//   이미 구현해놓았으므로 호환성 유지를 위해 Overlay + 좌표 추적 방식을 채택한다.
public class InventoryAnchorFollower : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;                           // 보통 Player 캐릭터
    public string targetTag = "Player";                // target 이 null 이면 태그로 탐색

    [Header("머리 위치")]
    [Tooltip("직접 지정 시 이 Transform 위를 추적. 비워두면 Animator Head 뼈 자동 탐색.")]
    public Transform headBone;                                 // null 이면 Awake 에서 자동 탐색
    [Tooltip("머리뼈 찾았을 때 적용하는 추가 오프셋 (머리 바로 위)")]
    public Vector3 headBoneOffset = new Vector3(0f, 0.25f, 0f);
    [Tooltip("머리뼈 못 찾을 때: Player 루트 기준 오프셋 (캐릭터 전체 키 정도)")]
    public Vector3 worldOffset    = new Vector3(0f, 1.8f, 0f);
    public Vector2 screenOffset   = new Vector2(0f, 140f);    // 핫바(높이 100)와 겹치지 않게 충분히 위로

    [Header("경계 처리")]
    public bool   clampToScreen = true;
    public float  screenPadding = 60f; // 좌우/상단 여백

    RectTransform _rt;
    Camera        _cam;

    void Awake()
    {
        _rt = transform as RectTransform;
        // target 이 없으면 태그로 먼저 찾아두고, 이후 머리뼈 자동 탐색
        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go != null) target = go.transform;
        }
        if (target != null && headBone == null)
            headBone = FindHeadBone(target);
    }

    // Mixamo Humanoid 리깅 기준으로 머리뼈를 찾는다.
    // 1순위: Animator.GetBoneTransform(Head) — Humanoid Avatar 설정 시 확실함
    // 2순위: 자식 중 "head" 이름 포함 Transform
    // 3순위: null (worldOffset 만 사용)
    static Transform FindHeadBone(Transform root)
    {
        Animator anim = root.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            Transform bone = anim.GetBoneTransform(HumanBodyBones.Head);
            if (bone != null) return bone;
        }
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }
        return null;
    }

    // 🐾 활성화 첫 프레임 깜빡임 방지: Toggle 직후에도 즉시 올바른 머리 위 위치로 스냅한다.
    void OnEnable()
    {
        // 활성화될 때마다 head 재탐색 (씬 로드 타이밍 대응)
        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go != null) target = go.transform;
        }
        if (target != null && headBone == null)
            headBone = FindHeadBone(target);
        UpdatePosition();
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        // 대상 자동 탐색 (런타임 중 target 이 사라졌을 때 대비)
        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go != null)
            {
                target   = go.transform;
                headBone = FindHeadBone(target);
            }
        }
        if (target == null || _rt == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // 머리뼈가 있으면 뼈 위치 + headBoneOffset, 없으면 Player 루트 + worldOffset
        Vector3 anchorWorld = headBone != null
            ? headBone.position + headBoneOffset
            : target.position + worldOffset;

        // 월드 → 스크린 변환
        Vector3 world  = anchorWorld;
        Vector3 screen = _cam.WorldToScreenPoint(world);
        if (screen.z < 0f) return; // 카메라 뒤쪽이면 갱신 스킵 (이전 위치 유지)

        float x = screen.x + screenOffset.x;
        float y = screen.y + screenOffset.y;

        // 화면 밖으로 튀어나가지 않도록 클램프
        if (clampToScreen)
        {
            Vector2 size = _rt.sizeDelta;
            float halfW  = size.x * 0.5f;
            x = Mathf.Clamp(x, halfW + screenPadding, Screen.width - halfW - screenPadding);
            y = Mathf.Clamp(y, screenPadding, Screen.height - size.y - screenPadding);
        }

        _rt.position = new Vector3(x, y, 0f);
    }
}
