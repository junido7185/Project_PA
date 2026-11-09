using System.Collections.Generic;
using UnityEngine;

// Harvest support — 기본 도끼/곡괭이를 든 동안 실제로 채집할 수 있는 직접 채집 나무·바위를 멀리서도 알아보게 한다.
// 장식 나무(FirstDayToolSurface)와 모양이 같아 근접 프롬프트로만 구분되던 문제(user-request 2026-10-25)에 대한 표시 전용 계층이다.
// 채집 판정·타격 수·보상·저장·인벤토리는 Gatherable/MiningSpot이 그대로 소유하고, 여기서는 그 공개 상태만 읽는다.
// 든 도구의 아이콘(Item.icon)을 크림색 바탕·노란 테두리 원판(딩컴 알림 06의 패널 규칙) 위에 얹어 가까운 대상 최대 4개 위에 띄운다.
// 절차 메시 + 런타임 재질 1개(자산·.meta 없음) + MaterialPropertyBlock. 셰이더는 빌드에 항상 포함되는 Sprites/Default만 쓰고,
// 없으면 표식 없이 기존 프롬프트만 남긴다. 표식 4개는 처음 필요할 때 만들어 꺼 두었다가 다시 쓴다.
public sealed class HarvestTargetMarkers : MonoBehaviour
{
    // UI 비평(Harvest-ui-critic-20261025): 가까운 4개만(화면 밖 포함) 표시하면 표식 없는 나무도 채집 가능할 수 있었다.
    // 범위 안 화면에 보이는 대상은 모두 받아 "표식 없음 = 이 도구로 못 캠"이 되게 한다.
    const int MaxMarkers = 8, HitBuffer = 512;
    // 탐색 반경·주기. 놓친 대상은 다음 탐색(0.25초)에 잡힌다. 이미 표식이 붙은 대상은 조금 더 가깝게 쳐서 표식이 자주 옮겨 다니지 않게 한다.
    const float Range = 12f, ScanInterval = .25f, KeepBonus = .75f;
    // 원판 지름 .55m 안에 아이콘 .42m. 수관/바위 윗면에서 .6m 위, 시선 방향으로 카메라 쪽에 당겨 이웃 수관에 덜 묻힌다(화면 위치는 그대로).
    // Lift .6→.25: 숲에서 표식이 수관 위 빈 풀밭에 떠 어느 나무 것인지 흐렸다(UI 비평).
    const float BadgeSize = .55f, IconSize = .42f, Lift = .25f, PullTowardCamera = .6f;
    // 6m까지 선명하고 12m에서 .35까지 흐려진다. 새로 붙은 표식은 .2초 동안 나타난다.
    const float FullAlphaDistance = 6f, FarAlpha = .35f, FadeIn = .2f, BobHeight = .06f, BobSpeed = 2.4f;

    static HarvestTargetMarkers _instance;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static Material _material;
    static Mesh _badge;
    static bool _shaderMissing;

    sealed class Marker
    {
        public GameObject root;
        public MeshRenderer badge;
        public SpriteRenderer icon;
        public Component target; // Gatherable 또는 MiningSpot. 비어 있으면 풀에서 쉬는 표식이다.
        public Vector3 top;
        public float age;
    }

    readonly Marker[] _markers = new Marker[MaxMarkers];
    readonly Collider[] _hits = new Collider[HitBuffer];
    readonly Component[] _nearest = new Component[MaxMarkers];
    readonly float[] _nearestDistance = new float[MaxMarkers];
    // 수관 윗면은 처음 본 때 한 번만 잰다(렌더러 순회). 자원은 제자리에 있고, 쓰러짐/부서짐 중에는 표식이 없다.
    readonly Dictionary<Component, Vector3> _tops = new Dictionary<Component, Vector3>();
    MaterialPropertyBlock _block;
    GameObject _player;
    ToolType _tool;
    Sprite _icon;
    float _nextScan;

    // 씬·프리팹 연결 없이 플레이 시작 때 숨은 호스트 하나를 만든다. 씬 전환에도 남는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var host = new GameObject("PA_HarvestTargetMarkers") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<HarvestTargetMarkers>();
    }

    void Awake() => _block = new MaterialPropertyBlock();

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // 카메라가 따라온 뒤에 카메라를 향하도록 LateUpdate에서 세운다.
    void LateUpdate()
    {
        var camera = Camera.main;
        Item held = ResolveHeld();
        ToolType tool = held != null && (held.toolType == ToolType.Axe || held.toolType == ToolType.Pickaxe || held.toolType == ToolType.Shovel) ? held.toolType : ToolType.None;
        if (tool == ToolType.None || camera == null || !EnsureResources())
        {
            if (_tool != ToolType.None) ReleaseAll();
            _tool = ToolType.None;
            return;
        }
        // 도구를 바꾸면 대상 종류가 바뀌므로 바로 다시 찾는다.
        if (tool != _tool)
        {
            ReleaseAll();
            _tool = tool;
            _nextScan = 0f;
        }
        if (held.icon != _icon)
        {
            _icon = held.icon;
            for (int i = 0; i < MaxMarkers; i++) if (_markers[i] != null) ApplyIcon(_markers[i]);
        }
        if (Time.time >= _nextScan)
        {
            _nextScan = Time.time + ScanInterval;
            Scan();
        }

        Transform view = camera.transform;
        Vector3 player = _player.transform.position;
        for (int i = 0; i < MaxMarkers; i++)
        {
            var marker = _markers[i];
            if (marker == null || ReferenceEquals(marker.target, null)) continue;
            // 베는 순간(쓰러짐/부서짐 시작)·고갈·파괴는 다음 탐색을 기다리지 않고 바로 끈다.
            if (!Harvestable(marker.target)) { Release(marker); continue; }
            marker.age += Time.deltaTime;
            float distance = Vector3.Distance(player, marker.target.transform.position);
            float alpha = Mathf.Lerp(1f, FarAlpha, Mathf.InverseLerp(FullAlphaDistance, Range, distance)) * Mathf.Clamp01(marker.age / FadeIn);
            float bob = Mathf.Sin(Time.time * BobSpeed + i * 1.7f) * BobHeight;
            marker.root.transform.SetPositionAndRotation(marker.top + Vector3.up * (Lift + bob) - view.forward * PullTowardCamera, view.rotation);
            var color = new Color(1f, 1f, 1f, alpha);
            _block.SetColor(ColorId, color);
            marker.badge.SetPropertyBlock(_block);
            marker.icon.color = color;
        }
    }

    // 손에 든 물건 판정은 채집 경로와 같은 EquipmentSystem.CurrentHeld를 쓴다(첫날 연출의 집어넣기 상태 포함).
    Item ResolveHeld()
    {
        var inventory = Inventory.instance;
        if (inventory == null) return null;
        _player = inventory.gameObject;
        return EquipmentSystem.CurrentHeld(_player);
    }

    // 직접 채집 대상의 트리거만 고른다. 고갈된 대상은 콜라이더가 꺼져 여기에 잡히지 않고, 아래 상태 확인으로도 거른다.
    void Scan()
    {
        for (int i = 0; i < MaxMarkers; i++) { _nearest[i] = null; _nearestDistance[i] = float.MaxValue; }
        Vector3 center = _player.transform.position;
        int count = Physics.OverlapSphereNonAlloc(center, Range, _hits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var collider = _hits[i];
            _hits[i] = null; // 버퍼가 파괴될 오브젝트를 붙잡아 두지 않게 비운다.
            if (collider == null) continue;
            // 2026-11-03 삽: 아직 파지 않은 파기 자국(DigSpot)에도 같은 표식을 띄운다(레퍼런스 09 지면 대상 표시).
            Component target = _tool == ToolType.Axe ? collider.GetComponentInParent<Gatherable>()
                : _tool == ToolType.Shovel ? collider.GetComponentInParent<DigSpot>()
                : (Component)collider.GetComponentInParent<MiningSpot>();
            if (target == null || !Harvestable(target)) continue;
            float distance = Vector3.Distance(center, target.transform.position);
            if (distance > Range || !OnScreen(CrownTop(target))) continue;
            if (MarkerFor(target) != null) distance -= KeepBonus;
            Insert(target, distance);
        }
        // 빠진 대상의 표식을 먼저 비우고 새 대상에 빈 표식을 준다. 남은 대상의 표식은 나타남·흔들림을 그대로 잇는다.
        for (int i = 0; i < MaxMarkers; i++)
        {
            var marker = _markers[i];
            if (marker != null && !ReferenceEquals(marker.target, null) && System.Array.IndexOf(_nearest, marker.target) < 0) Release(marker);
        }
        for (int i = 0; i < MaxMarkers; i++)
            if (_nearest[i] != null && MarkerFor(_nearest[i]) == null) Acquire(_nearest[i]);
    }

    // 가까운 순 4개를 고정 배열에 끼워 넣는다. 한 대상의 여러 콜라이더는 한 번만 들어간다.
    void Insert(Component target, float distance)
    {
        for (int i = 0; i < MaxMarkers; i++) if (_nearest[i] == target) return;
        if (distance >= _nearestDistance[MaxMarkers - 1]) return;
        int slot = MaxMarkers - 1;
        while (slot > 0 && distance < _nearestDistance[slot - 1])
        {
            _nearest[slot] = _nearest[slot - 1];
            _nearestDistance[slot] = _nearestDistance[slot - 1];
            slot--;
        }
        _nearest[slot] = target;
        _nearestDistance[slot] = distance;
    }

    // 화면 밖 대상은 표식 수를 차지하지 않는다(가장자리 여유 5%).
    static bool OnScreen(Vector3 top)
    {
        var camera = Camera.main;
        if (camera == null) return false;
        Vector3 view = camera.WorldToViewportPoint(top);
        return view.z > 0f && view.x > -.05f && view.x < 1.05f && view.y > -.05f && view.y < 1.05f;
    }

    static bool Harvestable(Component target)
    {
        if (target == null || !((Behaviour)target).isActiveAndEnabled) return false;
        if (target is Gatherable tree) return tree.IsDirectWorld && !tree.DirectDepleted && !tree.PresentationFalling;
        if (target is DigSpot spot) return !spot.Dug;
        var rock = (MiningSpot)target;
        return rock.IsDirectWorld && !rock.DirectDepleted && !rock.PresentationCrumbling;
    }

    Marker MarkerFor(Component target)
    {
        for (int i = 0; i < MaxMarkers; i++)
            if (_markers[i] != null && ReferenceEquals(_markers[i].target, target)) return _markers[i];
        return null;
    }

    void Acquire(Component target)
    {
        for (int i = 0; i < MaxMarkers; i++)
        {
            var marker = _markers[i] ??= CreateMarker();
            if (!ReferenceEquals(marker.target, null)) continue;
            marker.target = target;
            marker.top = CrownTop(target);
            marker.age = 0f;
            ApplyIcon(marker);
            marker.root.SetActive(true);
            return;
        }
    }

    void Release(Marker marker)
    {
        marker.target = null;
        marker.root.SetActive(false);
    }

    void ReleaseAll()
    {
        for (int i = 0; i < MaxMarkers; i++) if (_markers[i] != null) Release(_markers[i]);
    }

    // 수관/바위 윗면의 가운데. 렌더러가 없으면 루트에서 대략의 높이를 쓴다.
    Vector3 CrownTop(Component target)
    {
        if (_tops.TryGetValue(target, out var top)) return top;
        Transform visual = target is Gatherable tree ? tree.PresentationVisual : target is MiningSpot rock ? rock.PresentationVisual : target.transform;
        if (visual == null) visual = target.transform;
        top = ResourcePresentation.TryGetBounds(visual, out var bounds)
            ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)
            : target.transform.position + Vector3.up * (target is Gatherable ? 2.5f : 1f);
        if (_tops.Count >= 1024) _tops.Clear(); // 월드를 다시 만들면 새 인스턴스가 생기므로 오래된 항목을 비운다.
        _tops[target] = top;
        return top;
    }

    Marker CreateMarker()
    {
        var root = new GameObject("HarvestTargetMarker");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        var badge = new GameObject("Badge");
        badge.transform.SetParent(root.transform, false);
        badge.transform.localScale = Vector3.one * (BadgeSize * .5f); // 메시 반지름 1 → 지름 BadgeSize
        badge.AddComponent<MeshFilter>().sharedMesh = _badge;
        var badgeRenderer = badge.AddComponent<MeshRenderer>();
        badgeRenderer.sharedMaterial = _material;
        Quiet(badgeRenderer);
        var icon = new GameObject("Icon");
        icon.transform.SetParent(root.transform, false);
        var iconRenderer = icon.AddComponent<SpriteRenderer>();
        iconRenderer.sharedMaterial = _material;
        iconRenderer.sortingOrder = 1; // 같은 재질·큐에서 원판 위에 그린다.
        Quiet(iconRenderer);
        return new Marker { root = root, badge = badgeRenderer, icon = iconRenderer };
    }

    static void Quiet(Renderer renderer)
    {
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    // 아이콘이 없으면 원판만 남는다. 스프라이트 피벗이 가운데가 아니어도 원판 중심에 맞추고,
    // 원판보다 조금 카메라 쪽(로컬 -Z: 루트가 카메라 회전을 따르므로)에 둔다.
    void ApplyIcon(Marker marker)
    {
        var icon = marker.icon;
        icon.sprite = _icon;
        icon.enabled = _icon != null;
        if (_icon == null) return;
        Bounds bounds = _icon.bounds;
        float scale = IconSize / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.y));
        icon.transform.localScale = Vector3.one * scale;
        icon.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.center.y * scale, -.02f);
    }

    static bool EnsureResources()
    {
        if (_material != null && _badge != null) return true;
        if (_shaderMissing) return false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            _shaderMissing = true;
            Debug.LogWarning("[HarvestTargetMarkers] Sprites/Default is missing from this build; harvest markers disabled.");
            return false;
        }
        if (_material == null)
            _material = new Material(shader) { name = "PA_HarvestTargetMarker (runtime)", hideFlags = HideFlags.HideAndDontSave, renderQueue = 3100 };
        if (_badge == null) _badge = BuildBadge();
        return true;
    }

    // 크림색 원판과 노란 테두리(정점 색). 알파는 블록의 _Color로 흐린다.
    static Mesh BuildBadge()
    {
        const int segments = 28;
        const float rim = .84f;
        var cream = new Color(1f, .97f, .88f, .95f);
        var gold = new Color(1f, .78f, .12f, 1f);
        var vertices = new Vector3[1 + segments * 3];
        var colors = new Color[vertices.Length];
        var triangles = new int[segments * 9];
        colors[0] = cream;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            int inner = 1 + i, rimInner = 1 + segments + i, rimOuter = 1 + segments * 2 + i;
            vertices[inner] = direction * rim; colors[inner] = cream;
            vertices[rimInner] = direction * rim; colors[rimInner] = gold;
            vertices[rimOuter] = direction; colors[rimOuter] = gold;
            int next = (i + 1) % segments, t = i * 9;
            triangles[t] = 0; triangles[t + 1] = inner; triangles[t + 2] = 1 + next;
            triangles[t + 3] = rimInner; triangles[t + 4] = rimOuter; triangles[t + 5] = 1 + segments * 2 + next;
            triangles[t + 6] = rimInner; triangles[t + 7] = 1 + segments * 2 + next; triangles[t + 8] = 1 + segments + next;
        }
        var mesh = new Mesh { name = "PA_HarvestMarkerBadge", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = new Vector2[vertices.Length];
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
