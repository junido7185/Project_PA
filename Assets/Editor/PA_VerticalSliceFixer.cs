#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Vertical Slice 정비 — 접지/충돌/NPC 정지거리 씬 수정 + 들어갈 수 있는 상점(실내) 구축.
//
// PA_PhysicsAudit 실측 근거:
// - 물리 지면(Ground MeshCollider)은 y=-0.5, 길/광장 비주얼(Road_NS/EW, Pavement_ShopPlaza)은
//   콜라이더 없이 y≈+0.05 에 떠 있어 캐릭터가 길 위에서 0.48m 파묻혀 보였다 → 길을 지면 높이로 내림.
// - 플레이어 모델(PlayerModel_C01)의 발이 캡슐 바닥보다 0.15 아래 → 모델을 +0.17 올림.
// - 건물 BoxCollider 8개가 비주얼보다 최대 6.4m 넓어 투명 벽을 만들었다 → 렌더러 경계로 축소.
// - NPC stoppingDistance 0.2 → 0.75 (카운터/서로에게 파고드는 문제 완화).
//
// 원본 프리팹/FBX/meta 는 건드리지 않는다. 씬 오버라이드와 신규 오브젝트만 사용한다.
public static class PA_VerticalSliceFixer
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string BackupPath = "Assets/Scenes/_Backups/Prototype_FirstDay_before_vslice_fix_20260713.unity";

    // ── 1) 접지/충돌/NPC 수정 ────────────────────────────────────────────────
    [MenuItem("Project PA/Fix/Run Grounding And Collider Fix")]
    public static void RunGroundingAndColliderFix()
    {
        try
        {
            BackupScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int changes = 0;
            changes += LowerFloatingWalkVisuals();
            changes += FixPlayerModelOffset();
            changes += ShrinkOversizedColliders();
            changes += TuneNpcAgents();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"PA VSliceFix: grounding/collider fix saved. changes={changes}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"PA VSliceFix failed: {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static void BackupScene()
    {
        if (File.Exists(BackupPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
        File.Copy(ScenePath, BackupPath);
        AssetDatabase.Refresh();
        Debug.Log($"PA VSliceFix: scene backup at {BackupPath}");
    }

    // 길/광장 비주얼을 물리 지면(-0.5) 바로 위 절대 높이로 맞춘다 (멱등, NavMesh 무영향).
    static int LowerFloatingWalkVisuals()
    {
        (string name, float targetY)[] targets =
        {
            ("Road_NS", -0.47f),
            ("Road_EW", -0.47f),
            ("Pavement_ShopPlaza", -0.46f)
        };
        int changed = 0;

        foreach (var (n, targetY) in targets)
        {
            var go = GameObject.Find(n);
            if (go == null) { Debug.LogWarning($"PA VSliceFix: '{n}' not found"); continue; }

            var pos = go.transform.position;
            if (Mathf.Abs(pos.y - targetY) < 0.001f) continue;
            go.transform.position = new Vector3(pos.x, targetY, pos.z);
            changed++;
            Debug.Log($"PA VSliceFix: set '{n}' y {pos.y:0.###} -> {targetY:0.###}");
        }

        return changed;
    }

    // 플레이어 발(렌더러 최저점)이 캡슐 바닥과 일치하도록 모델을 올린다.
    static int FixPlayerModelOffset()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogWarning("PA VSliceFix: Player not found"); return 0; }

        var model = player.transform.Find("PlayerModel_C01");
        if (model == null) { Debug.LogWarning("PA VSliceFix: PlayerModel_C01 not found"); return 0; }

        var local = model.localPosition;
        model.localPosition = new Vector3(local.x, 0.17f, local.z);
        Debug.Log($"PA VSliceFix: player model localY {local.y:0.###} -> 0.17 (feet ≒ capsule bottom)");
        return 1;
    }

    // 렌더러 경계보다 훨씬 넓은 BoxCollider 를 실제 보이는 범위로 축소한다.
    static int ShrinkOversizedColliders()
    {
        int changed = 0;

        foreach (var box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (box == null || !box.enabled || box.isTrigger) continue;
            if (box.GetComponentInParent<NpcController>() != null) continue;
            if (box.GetComponent<ShopSlot>() != null) continue; // 상호작용 슬롯은 유지

            var filters = box.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) continue;

            // 회전된 건물에서 월드 AABB 를 로컬로 재변환하면 이중으로 부풀므로,
            // 각 메시의 "로컬 bounds 8모서리"를 메시→월드→콜라이더 로컬로 직접 변환한다.
            bool hasLocal = false;
            Bounds local = new Bounds();
            Matrix4x4 worldToBox = box.transform.worldToLocalMatrix;
            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null) continue;
                Bounds mb = filter.sharedMesh.bounds;
                Matrix4x4 meshToBox = worldToBox * filter.transform.localToWorldMatrix;
                Vector3 min = mb.min, max = mb.max;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = meshToBox.MultiplyPoint3x4(new Vector3(
                        (i & 1) == 0 ? min.x : max.x,
                        (i & 2) == 0 ? min.y : max.y,
                        (i & 4) == 0 ? min.z : max.z));
                    if (!hasLocal) { local = new Bounds(corner, Vector3.zero); hasLocal = true; }
                    else local.Encapsulate(corner);
                }
            }
            if (!hasLocal) continue;

            Vector3 beforeSize = box.size;
            bool oversized = beforeSize.x > local.size.x + 0.8f
                || beforeSize.z > local.size.z + 0.8f
                || (beforeSize.x * beforeSize.y * beforeSize.z)
                    > Mathf.Max(0.001f, local.size.x * local.size.y * local.size.z) * 1.8f;
            if (!oversized) continue;

            // 절대 키우지 않는다 — 축별로 기존보다 작아지는 경우만 적용.
            Vector3 newSize = new Vector3(
                Mathf.Min(beforeSize.x, local.size.x),
                Mathf.Min(beforeSize.y, local.size.y),
                Mathf.Min(beforeSize.z, local.size.z));

            box.center = local.center;
            box.size = newSize;
            changed++;
            Debug.Log($"PA VSliceFix: shrink collider '{GetPath(box.transform)}' {beforeSize} -> {box.size}");
        }

        return changed;
    }

    static int TuneNpcAgents()
    {
        int changed = 0;
        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null) continue;
            agent.stoppingDistance = 0.75f;
            changed++;
        }
        Debug.Log($"PA VSliceFix: NPC stoppingDistance -> 0.75 ({changed} agents)");
        return changed;
    }

    // ── 1-b) S2/S3: 실내 상점 Shop 등록 + NavMesh 리베이크(실내 아일랜드 포함) ──
    // 실내 슬롯 6개가 Shop.Slots 로 등록되어 NPC 가 기존 FSM 으로 실내를 둘러볼 수 있게 된다.
    // 앵커 오염은 PA_ShopLocator(FindPlazaShop, y<50)로 이미 차단됨.
    [MenuItem("Project PA/Fix/Register Interior Shop And Rebake")]
    public static void RunRegisterInteriorShop()
    {
        try
        {
            BackupScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var interior = GameObject.Find("PA_StoreInterior");
            if (interior == null) throw new InvalidOperationException("PA_StoreInterior not found — run Build Enterable Shop first");

            if (interior.GetComponent<Shop>() == null)
            {
                interior.AddComponent<Shop>();
                Debug.Log("PA VSliceFix: Shop component registered on PA_StoreInterior");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            RebakeAllSurfaces(scene);

            Debug.Log("PA VSliceFix: interior shop registered + NavMesh rebaked (interior island).");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"PA VSliceFix interior-shop failed: {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static void RebakeAllSurfaces(UnityEngine.SceneManagement.Scene scene)
    {
        Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType == null) throw new InvalidOperationException("NavMeshSurface type not found");

        var surfaces = Object.FindObjectsByType(surfaceType, FindObjectsSortMode.None);
        MethodInfo build = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Public | BindingFlags.Instance);
        foreach (var surface in surfaces)
        {
            build.Invoke(surface, null);
            Debug.Log($"PA VSliceFix: NavMesh rebaked on '{((Component)surface).gameObject.name}'");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    // ── 2) NavMesh 리베이크 (콜라이더 축소로 걷기 가능 영역이 넓어짐) ─────────
    [MenuItem("Project PA/Fix/Rebake NavMesh")]
    public static void RunNavMeshRebake()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null) throw new InvalidOperationException("NavMeshSurface type not found");

            var surfaces = Object.FindObjectsByType(surfaceType, FindObjectsSortMode.None);
            if (surfaces.Length == 0) throw new InvalidOperationException("No NavMeshSurface in scene");

            MethodInfo build = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Public | BindingFlags.Instance);
            foreach (var surface in surfaces)
            {
                build.Invoke(surface, null);
                Debug.Log($"PA VSliceFix: NavMesh rebaked on '{((Component)surface).gameObject.name}'");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("PA VSliceFix: NavMesh rebake saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"PA VSliceFix rebake failed: {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    // ── 3) 들어갈 수 있는 상점 — 실내 공간 + 양방향 문 + 판매 슬롯 그리드 ──────
    // BuildingEntrance 의 단일 씬 Y+100 실내 관례(Docs/08)를 그대로 따른다.
    [MenuItem("Project PA/Fix/Build Enterable Shop Interior")]
    public static void RunBuildEnterableShop()
    {
        try
        {
            BackupScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            if (GameObject.Find("PA_StoreInterior") != null)
            {
                Debug.Log("PA VSliceFix: PA_StoreInterior already exists — skip");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            BuildInterior(out Transform insideSpawn, out Transform interiorDoorAnchor);
            BuildExteriorDoor(insideSpawn, interiorDoorAnchor);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("PA VSliceFix: enterable shop interior built and saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"PA VSliceFix shop build failed: {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static void BuildInterior(out Transform insideSpawn, out Transform interiorDoorAnchor)
    {
        var root = new GameObject("PA_StoreInterior");
        root.transform.position = new Vector3(0f, 100f, -40f);

        Color floorWood = new Color(0.55f, 0.40f, 0.26f);
        Color wallCream = new Color(0.88f, 0.80f, 0.66f);
        Color counterWood = new Color(0.62f, 0.44f, 0.27f);
        Color counterTop = new Color(0.82f, 0.74f, 0.60f);

        // 바닥(콜라이더 유지 — 플레이어가 서는 면) + 벽 4면
        CreateBox(root.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 9f), floorWood, keepCollider: true);
        CreateBox(root.transform, "Wall_N", new Vector3(0f, 1.5f, 4.4f), new Vector3(12f, 3f, 0.2f), wallCream, keepCollider: true);
        CreateBox(root.transform, "Wall_S", new Vector3(0f, 1.5f, -4.4f), new Vector3(12f, 3f, 0.2f), wallCream, keepCollider: true);
        CreateBox(root.transform, "Wall_E", new Vector3(5.9f, 1.5f, 0f), new Vector3(0.2f, 3f, 9f), wallCream, keepCollider: true);
        CreateBox(root.transform, "Wall_W", new Vector3(-5.9f, 1.5f, 0f), new Vector3(0.2f, 3f, 9f), wallCream, keepCollider: true);

        // 따뜻한 실내 조명 2개
        CreateWarmLight(root.transform, new Vector3(-2.5f, 2.4f, 0f));
        CreateWarmLight(root.transform, new Vector3(2.5f, 2.4f, 0f));

        // 문라이터식 판매 그리드 — 2행 x 3열 진열대. 각 진열대가 ShopSlot(진열/가격/구매).
        // (다음 단계에서 Shop 컴포넌트 등록 + NPC 실내 진입을 붙인다 — 지금은 플레이어 전용 기초)
        var grid = new GameObject("SalesGrid");
        grid.transform.SetParent(root.transform, false);
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = -2.2f + col * 2.2f;
                float z = 1.6f - row * 2.8f;
                var table = CreateBox(grid.transform, $"InteriorShopSlot_{row}_{col}",
                    new Vector3(x, 0.35f, z), new Vector3(1.05f, 0.7f, 0.85f), counterWood, keepCollider: true);
                CreateBox(table.transform, "Top", new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.06f, 0.9f), counterTop, keepCollider: false, isLocalScale: true);

                var slot = table.AddComponent<ShopSlot>();
                slot.displayOffset = new Vector3(0f, 0.75f, 0f);
            }
        }

        // 실내 출구 문 (남쪽 벽) — 밖으로 나가는 워프
        interiorDoorAnchor = CreateBox(root.transform, "Door_In", new Vector3(0f, 1.0f, -4.15f),
            new Vector3(1.1f, 2.0f, 0.25f), new Color(0.45f, 0.30f, 0.18f), keepCollider: true).transform;

        // 실내 스폰 지점 (문 바로 앞)
        var spawn = new GameObject("PlayerSpawn_Inside");
        spawn.transform.SetParent(root.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 0.05f, -3.0f);
        spawn.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        insideSpawn = spawn.transform;
    }

    static void BuildExteriorDoor(Transform insideSpawn, Transform interiorDoorAnchor)
    {
        // 광장에서 가장 가까운 기존 실건물(오두막)을 잡화점 본점으로 쓴다 — 신규 외형 없이 재사용.
        var cottage = GameObject.Find("B10_Cottage_01") ?? GameObject.Find("B10_Cottage_Static");
        var shop = Object.FindFirstObjectByType<Shop>();
        Vector3 plazaAnchor = shop != null ? shop.transform.position : Vector3.zero;

        Vector3 doorPos;
        Quaternion doorRot;
        if (cottage != null)
        {
            // 오두막에서 광장을 바라보는 면 앞 1.6m 지점에 문을 세운다.
            Vector3 toPlaza = plazaAnchor - cottage.transform.position;
            toPlaza.y = 0f;
            toPlaza = toPlaza.sqrMagnitude > 0.01f ? toPlaza.normalized : Vector3.forward;

            var col = cottage.GetComponent<Collider>();
            float half = col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 3.5f;
            doorPos = cottage.transform.position + toPlaza * (half + 0.35f);
            doorPos.y = -0.5f;
            doorRot = Quaternion.LookRotation(-toPlaza, Vector3.up);
        }
        else
        {
            doorPos = plazaAnchor + new Vector3(8f, -0.5f, 6f);
            doorRot = Quaternion.identity;
        }

        // 외부 문(간판 포함) — 실내로 들어가는 워프
        var doorOut = CreateBox(null, "PA_StoreDoor_Out", doorPos + Vector3.up * 1.0f,
            new Vector3(1.1f, 2.0f, 0.25f), new Color(0.45f, 0.30f, 0.18f), keepCollider: true);
        doorOut.transform.rotation = doorRot;

        var sign = CreateBox(doorOut.transform, "Sign", new Vector3(0f, 1.25f, 0.05f),
            new Vector3(1.5f, 0.4f, 0.08f), new Color(0.93f, 0.87f, 0.74f), keepCollider: false, isLocalScale: true);
        var signLabel = new GameObject("Label");
        signLabel.transform.SetParent(sign.transform, false);
        signLabel.transform.localPosition = new Vector3(0f, 0f, 0.6f);
        signLabel.AddComponent<PrototypeWorldLabel>().Set("잡화점", new Color(0.35f, 0.22f, 0.12f), 1.4f);

        // 외부 스폰 지점 (문 앞)
        var outsideSpawn = new GameObject("PlayerSpawn_Outside");
        outsideSpawn.transform.position = doorPos + doorRot * Vector3.forward * -1.4f + Vector3.up * 0.55f;
        outsideSpawn.transform.rotation = Quaternion.LookRotation(doorRot * Vector3.forward * -1f, Vector3.up);
        // 바깥쪽(광장 방향)을 보게 — 문 반대 방향
        outsideSpawn.transform.rotation = Quaternion.LookRotation((plazaAnchor - doorPos).normalized, Vector3.up);

        var entranceOut = doorOut.AddComponent<BuildingEntrance>();
        SetPrivateField(entranceOut, "targetSpawn", insideSpawn);
        SetPrivateField(entranceOut, "promptLabel", "잡화점 들어가기");

        var entranceIn = interiorDoorAnchor.gameObject.AddComponent<BuildingEntrance>();
        SetPrivateField(entranceIn, "targetSpawn", outsideSpawn.transform);
        SetPrivateField(entranceIn, "promptLabel", "잡화점 나가기");

        Debug.Log($"PA VSliceFix: exterior door at {doorPos} (anchor={(cottage != null ? cottage.name : "fallback")})");
    }

    static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 size,
        Color color, bool keepCollider, bool isLocalScale = false)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        if (parent != null) box.transform.SetParent(parent, false);
        if (isLocalScale)
        {
            box.transform.localPosition = position;
        }
        else if (parent != null)
        {
            box.transform.localPosition = position;
        }
        else
        {
            box.transform.position = position;
        }
        box.transform.localScale = size;

        if (!keepCollider)
        {
            var col = box.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        var renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.sharedMaterial = new Material(shader) { color = color };
        }

        return box;
    }

    static void CreateWarmLight(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("InteriorLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.86f, 0.62f);
        light.intensity = 2.2f;
        light.range = 9f;
    }

    static void SetPrivateField(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        if (info == null) throw new InvalidOperationException($"field '{field}' not found on {target.GetType().Name}");
        info.SetValue(target, value);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
#endif
