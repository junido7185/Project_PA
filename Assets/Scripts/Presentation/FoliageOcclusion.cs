using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// P3 — 채집 중 캐릭터·도구·타격이 나무 수관에 가리지 않게 한다(Canon v2 §23 Camera: obstruction 대응).
// 카메라와 플레이어 사이를 가리는 큰 나무만 '그림자만' 렌더링으로 잠시 숨기고, 벗어나면 되돌린다.
// 가까운 채집 소나무는 확인된 줄기 submesh만 다시 그려 수관 아래 타격을 보여 준다.
// 모르는 자산은 기존 대상 예외를 유지한다. 메시·원본 재질/셰이더 자산은 수정하지 않는다.
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
        public Gatherable gather;
        public Trunk trunk;
        public float fallStart = -1f; // 숨긴 채 쓰러지기 시작한 시각(수관을 서서히 되돌린다)
    }

    // 모션 검수(10/12 conifer 전체 벌목): 수관을 통째로 숨기면 줄기만 남은 막대로 보이고, 쓰러질 때
    // 수관이 한 프레임에 되살아났다. 숨긴 동안 수관 submesh를 화면 망점(PA/CanopyDither)으로 그리고,
    // 쓰러질 때 _Fade를 1까지 올린 뒤 원래 렌더러로 되돌린다. 재질이 없으면 기존 동작을 그대로 쓴다.
    const float ChopFade = .55f, FallFadeSeconds = .25f;
    static Material _dither;
    static bool _ditherMissing;
    static readonly int FadeId = Shader.PropertyToID("_Fade");

    static Material Dither
    {
        get
        {
            if (_dither == null && !_ditherMissing)
            {
                _dither = Resources.Load<Material>("WorldArt/PA_CanopyDither");
                _ditherMissing = _dither == null;
            }
            return _dither;
        }
    }

    sealed class Trunk
    {
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Mesh mesh;
        public Material material, canopy;
        public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _canopyProperties = new MaterialPropertyBlock();
        Material _litDither;
        readonly List<Material> _materials = new List<Material>(2);

        public bool Valid
        {
            get
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    mesh == null || mesh.subMeshCount != 2 || material == null || canopy == null || filter == null || filter.sharedMesh != mesh) return false;
                renderer.GetSharedMaterials(_materials);
                return _materials.Count == 2 && _materials[0] == material && _materials[1] == canopy;
            }
        }

        public void Draw(Camera camera)
        {
            renderer.GetPropertyBlock(properties, 0);
            if (properties.isEmpty) renderer.GetPropertyBlock(properties);
            // 원본 전체 메시가 그림자를 담당하므로 다시 그리는 줄기는 그림자를 추가하지 않는다. 현재 자세를 쓴다.
            Graphics.DrawMesh(mesh, filter.transform.localToWorldMatrix, material, renderer.gameObject.layer,
                camera, 0, properties, ShadowCastingMode.Off, renderer.receiveShadows,
                renderer.probeAnchor, renderer.lightProbeUsage);
        }

        // 수관 submesh(1)를 원래 색의 망점으로 그린다. 그림자는 원본(ShadowsOnly)이 계속 담당한다.
        public void DrawCanopy(Camera camera, Material dither, float fade)
        {
            // Keep the source Lit surface, keywords and probes; coverage alone differs.
            // One runtime copy per confirmed tree, released with this occlusion registration.
            if (_litDither == null || _litDither.shader != dither.shader)
            {
                ReleaseDither();
                _litDither = new Material(canopy) { shader = dither.shader,
                    name = canopy.name + "_Occlusion", hideFlags = HideFlags.DontSave };
            }
            else _litDither.CopyPropertiesFromMaterial(canopy);
            renderer.GetPropertyBlock(_canopyProperties, 1);
            if (_canopyProperties.isEmpty) renderer.GetPropertyBlock(_canopyProperties);
            _canopyProperties.SetFloat(FadeId, fade);
            Graphics.DrawMesh(mesh, filter.transform.localToWorldMatrix, _litDither, renderer.gameObject.layer,
                camera, 1, _canopyProperties, ShadowCastingMode.Off, renderer.receiveShadows,
                renderer.probeAnchor, renderer.lightProbeUsage);
        }

        public void ReleaseDither()
        {
            if (_litDither != null) Object.Destroy(_litDither);
            _litDither = null;
        }
    }

    readonly List<Tree> _trees = new List<Tree>();
    Transform _player;
    bool _directScanned;
    float _next;
    public int HiddenCount { get; private set; }

    public void Configure(Transform player, IEnumerable<Transform> dressingRoots)
    {
        RestoreAll();
        _player = player;
        foreach (var tree in _trees) tree.trunk?.ReleaseDither();
        _trees.Clear();
        _directScanned = false;
        _next = 0f;
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
            gather = directTree ? root.GetComponent<Gatherable>() : null,
            trunk = directTree ? ConfirmedTrunk(renderers) : null,
            axis = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * .8f,
            bottom = bounds.min.y + bounds.size.y * .3f, top = bounds.max.y
        });
    }

    static Trunk ConfirmedTrunk(Renderer[] renderers)
    {
        // 로컬 PineTree_1/2와 실제 Unity 검사에서만 확인된 Wood(0)+Green(1) 결합 메시.
        // 다른 나무/스킨/추가 렌더러/슬롯 구성은 그대로 두어 줄기를 통째로 지우지 않는다.
        if (renderers.Length != 1 || !(renderers[0] is MeshRenderer renderer)) return null;
        string model = renderer.name;
        if (!KnownModel(model, "PineTree_1") && !KnownModel(model, "PineTree_2")) return null;
        var filter = renderer.GetComponent<MeshFilter>();
        var mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null || mesh.subMeshCount != 2) return null;
        var materials = renderer.sharedMaterials;
        if (materials.Length != 2 || materials[0] == null || materials[1] == null ||
            !KnownMaterial(materials[0].name, "Wood") ||
            !(KnownMaterial(materials[1].name, "Green") || KnownMaterial(materials[1].name, "DarkGreen"))) return null;
        return new Trunk { renderer = renderer, filter = filter, mesh = mesh, material = materials[0], canopy = materials[1] };
    }

    static bool KnownModel(string name, string source) => name == source || name == source + "(Clone)" ||
        name == source + " (Clone)";

    static bool KnownMaterial(string name, string source) => name == source ||
        name.StartsWith(source + "_") || name.StartsWith(source + " (");

    void LateUpdate()
    {
        if (_player == null) { RestoreAll(); return; }
        var camera = Camera.main;
        if (camera == null || !camera.isActiveAndEnabled) { RestoreAll(); return; }
        bool sample = Time.unscaledTime >= _next;
        if (sample)
        {
            _next = Time.unscaledTime + .08f;
            if (!_directScanned) ScanDirectTrees();
        }
        Vector3 eye = camera.transform.position;
        Vector3 chest = _player.position + Vector3.up * 1.1f;
        int hidden = 0;
        foreach (var tree in _trees)
        {
            // 쓰러짐/고갈/교체는 샘플 간격을 기다리지 않는다. 비활성 렌더러를 대신 그리면 잔상이 남는다.
            bool live = tree.renderers[0] != null && tree.renderers[0].enabled && tree.renderers[0].gameObject.activeInHierarchy;
            // 숨긴 채 쓰러지기 시작한 대상은 줄기 + 망점 수관을 계속 그리며 _Fade를 1까지 올린 뒤 되돌린다.
            if (live && tree.hidden && tree.target && tree.trunk != null && Dither != null && tree.gather != null &&
                tree.gather.isActiveAndEnabled && tree.gather.PresentationFalling && tree.trunk.Valid)
            {
                if (tree.fallStart < 0f) tree.fallStart = Time.time;
                float fade = Mathf.Lerp(ChopFade, 1f, (Time.time - tree.fallStart) / FallFadeSeconds);
                if (fade < 1f)
                {
                    tree.trunk.Draw(camera);
                    tree.trunk.DrawCanopy(camera, Dither, fade);
                    hidden++;
                    continue;
                }
            }
            bool preserveFull = tree.target && (tree.gather == null || !tree.gather.isActiveAndEnabled ||
                tree.gather.PresentationFalling || tree.gather.DirectDepleted);
            if (!live || preserveFull)
            { if (tree.hidden) Apply(tree, false); tree.fallStart = -1f; continue; }
            bool near = (tree.axis - _player.position).sqrMagnitude < 14f * 14f;
            Vector3 flat = tree.axis - _player.position; flat.y = 0f;
            bool chopping = tree.target && flat.sqrMagnitude < 2.2f * 2.2f;
            if (tree.trunk != null && (sample || chopping || tree.hidden) && !tree.trunk.Valid)
            { if (tree.hidden) Apply(tree, false); continue; }
            bool canopyOnly = chopping && tree.trunk != null;
            if (chopping && !canopyOnly)
            { if (tree.hidden) Apply(tree, false); continue; }
            if (sample)
            {
                Vector3 target = canopyOnly ? tree.gather.transform.position : Vector3.zero;
                Vector3 contact = canopyOnly ? GatherFeedback.VisibleContact(target, target.y + 1f, .5f, _player.position) : Vector3.zero;
                bool occludes = near && (Blocks(tree, eye, chest) || canopyOnly && Blocks(tree, eye, contact));
                if (occludes != tree.hidden) Apply(tree, occludes);
            }
            // DrawMesh는 한 프레임짜리 제출이다. .08초 가림 검사와 달리 매 프레임 유지한다.
            if (tree.hidden && canopyOnly)
            {
                tree.trunk.Draw(camera);
                if (Dither != null) tree.trunk.DrawCanopy(camera, Dither, ChopFade);
            }
            if (tree.hidden) hidden++;
        }
        HiddenCount = hidden;
    }

    static bool Blocks(Tree tree, Vector3 from, Vector3 to)
    {
        // 카메라→가슴/접촉 선분을 따라 수관 원기둥 안을 샘플한다(끝점 바로 앞 0.3m는 제외).
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

    void RestoreAll()
    {
        foreach (var tree in _trees) if (tree.hidden) Apply(tree, false);
        HiddenCount = 0;
    }

    void OnDisable() => RestoreAll();

    void OnDestroy()
    {
        RestoreAll();
        foreach (var tree in _trees) tree.trunk?.ReleaseDither();
    }
}
