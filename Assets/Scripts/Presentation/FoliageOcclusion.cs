using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// P3 — 채집 중 캐릭터·도구·타격이 나무 수관에 가리지 않게 한다(Canon v2 §23 Camera: obstruction 대응).
// 카메라와 플레이어 사이를 가리는 큰 나무만 '그림자만' 렌더링으로 잠시 숨기고, 벗어나면 되돌린다.
// 지금 베고 있는 바로 앞 나무(2.2m 안)는 타격 대상이라 숨기지 않는다. 재질/셰이더 추가·오브젝트 삭제 없음.
// 수관 형태는 나무 축 둘레의 원기둥으로 근사한다(경계 상자보다 헛숨김이 적다).
public sealed class FoliageOcclusion : MonoBehaviour
{
    sealed class Tree
    {
        public Renderer[] renderers;
        public ShadowCastingMode[] modes;
        public Vector3 axis;       // 줄기 바닥 중심
        public float radius, bottom, top;
        public bool hidden;
        public bool target;        // 직접 채집 나무(Gatherable)
    }

    readonly List<Tree> _trees = new List<Tree>();
    Transform _player;
    bool _directScanned;
    float _next;
    public int HiddenCount { get; private set; }

    public void Configure(Transform player, IEnumerable<Transform> dressingRoots)
    {
        _player = player;
        _trees.Clear();
        _directScanned = false;
        foreach (var root in dressingRoots)
            if (root != null)
                foreach (Transform child in root) Add(child, false);
    }

    // 직접 채집 나무는 월드 구성 순서상 장식보다 늦게 생길 수 있어 처음 보일 때 한 번 모은다.
    void ScanDirectTrees()
    {
        var gathers = FindObjectsByType<Gatherable>(FindObjectsSortMode.None);
        foreach (var gather in gathers)
            if (gather != null && gather.IsDirectWorld) Add(gather.transform, true);
        _directScanned = gathers.Length > 0;
    }

    void Add(Transform root, bool directTree)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y < 2.5f) return; // 풀·덤불·바위는 가리지 않는다.
        var modes = new ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) modes[i] = renderers[i].shadowCastingMode;
        _trees.Add(new Tree
        {
            renderers = renderers, modes = modes, target = directTree,
            axis = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * .8f,
            bottom = bounds.min.y + bounds.size.y * .3f, top = bounds.max.y
        });
    }

    void LateUpdate()
    {
        if (_player == null || Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + .08f;
        if (!_directScanned) ScanDirectTrees();
        var camera = Camera.main;
        if (camera == null) return;
        Vector3 eye = camera.transform.position;
        Vector3 chest = _player.position + Vector3.up * 1.1f;
        int hidden = 0;
        foreach (var tree in _trees)
        {
            bool near = (tree.axis - _player.position).sqrMagnitude < 14f * 14f;
            Vector3 flat = tree.axis - _player.position; flat.y = 0f;
            bool chopping = tree.target && flat.sqrMagnitude < 2.2f * 2.2f;
            bool occludes = near && !chopping && tree.renderers[0] != null && tree.renderers[0].gameObject.activeInHierarchy &&
                            Blocks(tree, eye, chest);
            if (occludes != tree.hidden) Apply(tree, occludes);
            if (tree.hidden) hidden++;
        }
        HiddenCount = hidden;
    }

    static bool Blocks(Tree tree, Vector3 from, Vector3 to)
    {
        // 카메라→가슴 선분을 따라 샘플해 수관 원기둥 안에 들어가는지 본다(플레이어 바로 앞 0.3m는 제외).
        for (int i = 1; i < 16; i++)
        {
            Vector3 p = Vector3.Lerp(to, from, i / 16f);
            if ((p - to).sqrMagnitude < .09f) continue;
            if (p.y < tree.bottom || p.y > tree.top) continue;
            Vector3 d = p - tree.axis; d.y = 0f;
            if (d.sqrMagnitude < tree.radius * tree.radius) return true;
        }
        return false;
    }

    static void Apply(Tree tree, bool hide)
    {
        tree.hidden = hide;
        for (int i = 0; i < tree.renderers.Length; i++)
            if (tree.renderers[i] != null)
                tree.renderers[i].shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : tree.modes[i];
    }

    void OnDisable()
    {
        foreach (var tree in _trees) if (tree.hidden) Apply(tree, false);
        HiddenCount = 0;
    }
}
