#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 🗺 PA_MapLayoutBuilder — Docs/08 §"마을 레이아웃 방향" 자동 구성기
//
// 좌표계 (Unity 기본):
//   Z+ = 북쪽, Z- = 남쪽, X+ = 동쪽, X- = 서쪽
//
// 구역 배치:
//   남쪽(Z-)  : 해변 / 무역항
//   중앙      : 상점 거리 (B01 또는 기존 Shop)
//   중앙 동쪽 : 광장 (B11) + SocialPoint 원형 배치
//   서쪽(X-)  : 주거 구역 (B10 Cottage × 3 + HomePoint_*)
//   동쪽(X+)  : 공방 구역 (B05~B08)
//   북쪽(Z+)  : 채집/파밍 구역 (FarmZone/GroveZone/MineZone/FishingZone)
//
// 멱등성(idempotent):
//   [PA_MapRoot] 는 매 빌드마다 통째로 재생성한다 — 자동 생성물만 관리.
//   기존 Shop/Workbench/StorageBox/[Markers] 같은 사용자 수동 배치는 삭제하지 않고
//   위치만 새 좌표로 이동하며 [PA_MapRoot]/[MapBuildings] 또는 기존 부모를 유지한다.
//   NPC.workSpot/homePoint Transform 참조는 그대로 보존되므로 NavMesh 가 Bake 되어 있다면
//   재실행 후에도 NPC 가 새 위치로 자연스럽게 이동한다.
public static class PA_MapLayoutBuilder
{
    // ── 루트 이름 상수 ──────────────────────────────────────────────────────
    const string MAP_ROOT       = "[PA_MapRoot]";
    const string SUB_TERRAIN    = "[Terrain]";
    const string SUB_ROADS      = "[Roads]";
    const string SUB_BUILDINGS  = "[MapBuildings]";
    const string SUB_MARKERS    = "[MapMarkers]";
    const string SUB_RESOURCES  = "[ResourceZones]";
    const string SUB_BEACH      = "[BeachAndWater]";

    // PA_ContentAutoIntegrator 가 생성하는 레거시 루트 — 본 빌더가 위치를 주관하므로 제거.
    const string LEGACY_WORLD_BUILDINGS = "[WorldBuildings]";

    const string PREFAB_DIR = "Assets/Prefabs/Buildings";

    // ── 레이아웃 상수 (Docs/08 §마을 레이아웃 방향 + 미션 사양) ─────────────
    static readonly Vector3 SHOP_POS           = new Vector3(  0, 0,   6);
    static readonly Vector3 STORAGE_POS        = new Vector3(-10, 0,   8);
    static readonly Vector3 PLAZA_POS          = new Vector3( 10, 0,   0);
    static readonly Vector3 PORT_POS           = new Vector3(  8, 0, -38);
    static readonly Vector3 PLAYER_SPAWN_POS   = new Vector3(  0, 0,  -6);
    static readonly Vector3 HIRING_SPAWN_POS   = new Vector3( 10, 0,  -4);

    static readonly (WorkbenchType type, Vector3 pos)[] WORKBENCH_LAYOUT =
    {
        (WorkbenchType.BasicWorkbench, new Vector3(22, 0,  10)),
        (WorkbenchType.Kitchen,        new Vector3(22, 0,   4)),
        (WorkbenchType.Forge,          new Vector3(22, 0,  -4)),
        (WorkbenchType.SewingTable,    new Vector3(22, 0, -10)),
    };

    static readonly Vector3[] COTTAGE_POSITIONS =
    {
        new Vector3(-22, 0,  8),
        new Vector3(-28, 0,  0),
        new Vector3(-20, 0, -8),
    };

    // 자원/파밍 존 (북쪽)
    static readonly (string name, Vector3 pos, Color color)[] RESOURCE_ZONES =
    {
        ("FarmZone",     new Vector3(-12, 0, 28), new Color(0.55f, 0.78f, 0.42f)),
        ("GroveZone",    new Vector3(-28, 0, 24), new Color(0.35f, 0.62f, 0.32f)),
        ("MineZone",     new Vector3( 28, 0, 24), new Color(0.62f, 0.55f, 0.48f)),
        ("FishingZone",  new Vector3( 10, 0, 30), new Color(0.42f, 0.68f, 0.92f)),
    };

    // 기존 마커(이름) → 새 위치. NPC.workSpot/homePoint 참조 보존을 위해 Transform 객체는 유지.
    static readonly (string name, Vector3 pos)[] MARKER_REPOSITION =
    {
        // 작업장
        ("WorkSpot_Farmer",      new Vector3(-12, 0, 28)),  // FarmZone
        ("WorkSpot_Lumberjack",  new Vector3(-28, 0, 24)),  // GroveZone
        ("WorkSpot_Miner",       new Vector3( 28, 0, 24)),  // MineZone
        ("WorkSpot_Fisher",      new Vector3( 10, 0, 30)),  // FishingZone
        // 서쪽 주거 클러스터 (좁은 골목)
        ("HomePoint_Farmer",     new Vector3(-22, 0,  6)),
        ("HomePoint_Lumberjack", new Vector3(-28, 0,  2)),
        ("HomePoint_Miner",      new Vector3(-22, 0, -2)),
        ("HomePoint_Chef",       new Vector3(-20, 0, -6)),
        ("HomePoint_Blacksmith", new Vector3(-26, 0, -6)),
        ("HomePoint_Tailor",     new Vector3(-22, 0,-10)),
        ("HomePoint_Carpenter",  new Vector3(-28, 0,-10)),
        ("HomePoint_Fisher",     new Vector3( -8, 0,-30)),  // 항구 근처
    };

    // ── 메뉴 진입점 ──────────────────────────────────────────────────────
    [MenuItem("P.A. System/Map/Build Layout Map", priority = 40)]
    public static void BuildMenu() => Build(showDialog: true);

    public static void BuildBatch() => Build(showDialog: false);

    [MenuItem("P.A. System/Map/Validate Layout Map", priority = 41)]
    public static void ValidateMenu()
    {
        var report = Validate();
        Debug.Log("🗺 [MapLayout] 검증 결과:\n" + string.Join("\n", report));
        EditorUtility.DisplayDialog("🗺 Map Layout 검증", string.Join("\n", report), "확인");
    }

    [MenuItem("P.A. System/Map/Repair Layout Map", priority = 42)]
    public static void RepairMenu() => Build(showDialog: true);

    // ── 메인 빌드 ────────────────────────────────────────────────────────
    public static int Build(bool showDialog)
    {
        int changed = 0;
        try
        {
            Debug.Log("🗺 [MapLayout] ─── 시작 ───");

            // 0. 레거시 [WorldBuildings] 제거 (PA_ContentAutoIntegrator 의 정적 배치)
            //    본 빌더가 위치를 주관하므로 중복 방지.
            var legacy = GameObject.Find(LEGACY_WORLD_BUILDINGS);
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy);
                Debug.Log($"🧹 [MapLayout] 레거시 {LEGACY_WORLD_BUILDINGS} 제거");
                changed++;
            }

            // 1. 루트와 하위 그룹 재생성 (idempotent)
            var root = ResetMapRoot();
            changed++;
            var terrain   = MakeSubRoot(root, SUB_TERRAIN);
            var roads     = MakeSubRoot(root, SUB_ROADS);
            var buildings = MakeSubRoot(root, SUB_BUILDINGS);
            var markers   = MakeSubRoot(root, SUB_MARKERS);
            var resources = MakeSubRoot(root, SUB_RESOURCES);
            var beach     = MakeSubRoot(root, SUB_BEACH);

            // 2. 지형
            changed += BuildTerrain(terrain.transform);
            // 3. 해변/바다
            changed += BuildBeachAndWater(beach.transform);
            // 4. 도로 + 광장 포장
            changed += BuildRoads(roads.transform);
            // 5. 자원 존 (시각용 컬러 패치)
            changed += BuildResourceZones(resources.transform);
            // 6. 빌딩 — 기존 객체 재활용 우선
            changed += PlaceShop(buildings.transform);
            changed += PlaceWorkbenches(buildings.transform);
            changed += PlaceCottages(buildings.transform);
            changed += PlaceStorage(buildings.transform);
            changed += PlacePlaza(buildings.transform);
            changed += PlaceTradePort(buildings.transform);
            // 7. 마커 — 기존 [Markers] 의 WorkSpot/HomePoint Transform 을 그대로 사용해 위치만 갱신
            changed += RepositionExistingMarkers();
            // 8. 광장 SocialPoint 6개 (신규 마커, [MapMarkers] 아래)
            changed += BuildSocialPoints(markers.transform);
            // 9. PlayerSpawn
            changed += EnsurePlayerSpawn(markers.transform);
            // 10. HiringService.spawnPoint
            changed += WireHiringSpawnPoint(markers.transform);

            // 11. NavMesh Bake
            changed += BakeNavMesh();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log($"🗺 [MapLayout] ✅ 완료 — {changed}건 변경");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("🗺 Map Layout 완료",
                    $"{changed}건 변경.\n\n다음 단계:\n  • Validate Layout Map 으로 검증\n  • ▶ Play 후 카메라로 둘러보기",
                    "확인");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [MapLayout] 실패: {e}");
            if (showDialog) EditorUtility.DisplayDialog("❌ Map Layout 실패", e.Message, "확인");
        }
        return changed;
    }

    // ── 루트/서브루트 ───────────────────────────────────────────────────
    static GameObject ResetMapRoot()
    {
        var existing = GameObject.Find(MAP_ROOT);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        var root = new GameObject(MAP_ROOT);
        return root;
    }

    static GameObject MakeSubRoot(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    // ── 지형 ────────────────────────────────────────────────────────────
    static int BuildTerrain(Transform parent)
    {
        // 기본 섬 Ground 96×96m. 이미 씬에 Ground 가 있으면 그것을 재사용 + 위치 정렬.
        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            // ⚠ Ground 는 NavMeshSurface 부착 대상이라 [PA_MapRoot] 바깥에 둔다.
            //   레이아웃 재빌드 시 NavMesh 데이터 손실을 피한다.
        }
        ground.transform.localScale = new Vector3(96f, 1f, 96f);
        ground.transform.position   = new Vector3(0f, -0.5f, 0f);

        var rend = ground.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Green.mat");
            if (mat != null) rend.sharedMaterial = mat;
            else rend.sharedMaterial.color = new Color(0.62f, 0.78f, 0.48f);
        }

        // 시각용 라벨 (배치 명목만 — Ground 는 PA_MapRoot 밖에 유지)
        var label = new GameObject("Ground_Ref");
        label.transform.SetParent(parent, false);
        return 2;
    }

    // ── 해변/바다 ────────────────────────────────────────────────────────
    static int BuildBeachAndWater(Transform parent)
    {
        int n = 0;
        // 해변 모래 (z -34 ~ -44)
        var beach = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beach.name = "Beach_Sand";
        beach.transform.SetParent(parent, false);
        beach.transform.position   = new Vector3(0f, 0.02f, -39f);
        beach.transform.localScale = new Vector3(80f, 0.05f, 10f);
        ColorRenderer(beach, new Color(0.92f, 0.86f, 0.62f));
        DestroyCollider(beach); n++;

        // 바다 (z -48 이하)
        var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Water_South";
        water.transform.SetParent(parent, false);
        water.transform.position   = new Vector3(0f, -0.15f, -60f);
        water.transform.localScale = new Vector3(120f, 0.3f, 24f);
        ColorRenderer(water, new Color(0.36f, 0.62f, 0.84f, 0.85f));
        DestroyCollider(water); n++;

        return n;
    }

    // ── 도로 + 상점 광장 포장 ────────────────────────────────────────────
    static int BuildRoads(Transform parent)
    {
        int n = 0;
        var roadColor = new Color(0.78f, 0.72f, 0.58f);

        // 남북 도로 (z -36 ~ 34, 중심 z=-1, 길이 70)
        var ns = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ns.name = "Road_NS";
        ns.transform.SetParent(parent, false);
        ns.transform.position   = new Vector3(0f, 0.03f, -1f);
        ns.transform.localScale = new Vector3(3f, 0.05f, 70f);
        ColorRenderer(ns, roadColor);
        DestroyCollider(ns); n++;

        // 동서 도로 (x -34 ~ 34, 중심 x=0, 길이 68)
        var ew = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ew.name = "Road_EW";
        ew.transform.SetParent(parent, false);
        ew.transform.position   = new Vector3(0f, 0.03f, 0f);
        ew.transform.localScale = new Vector3(68f, 0.05f, 3f);
        ColorRenderer(ew, roadColor);
        DestroyCollider(ew); n++;

        // 상점 앞 작은 광장 (Shop 위치 (0,0,6) 근처)
        var plaza = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plaza.name = "Pavement_ShopPlaza";
        plaza.transform.SetParent(parent, false);
        plaza.transform.position   = new Vector3(0f, 0.04f, 4f);
        plaza.transform.localScale = new Vector3(10f, 0.04f, 6f);
        ColorRenderer(plaza, new Color(0.88f, 0.84f, 0.68f));
        DestroyCollider(plaza); n++;

        return n;
    }

    // ── 자원 존 (시각용 컬러 패치) ────────────────────────────────────────
    static int BuildResourceZones(Transform parent)
    {
        int n = 0;
        foreach (var (zoneName, pos, color) in RESOURCE_ZONES)
        {
            var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = zoneName;
            patch.transform.SetParent(parent, false);
            patch.transform.position   = new Vector3(pos.x, 0.06f, pos.z);
            patch.transform.localScale = new Vector3(10f, 0.06f, 10f);
            ColorRenderer(patch, color);
            DestroyCollider(patch); n++;
        }
        return n;
    }

    // ── 빌딩 배치 ────────────────────────────────────────────────────────
    static int PlaceShop(Transform mapBuildings)
    {
        // 기존 Shop 우선 (Tag=Shop) — 이동만, 삭제 X
        var shop = PickPrimaryShop();
        if (shop != null)
        {
            int removed = RemoveDuplicateGeneratedShops(shop);
            shop.name = "Shop";
            shop.tag = "Shop";
            shop.transform.SetParent(mapBuildings, true);
            shop.transform.SetPositionAndRotation(SHOP_POS, Quaternion.identity);
            // Shop 의 부모는 그대로 둔다 (PA_ContentAutoIntegrator 의 [WorldBuildings] 자손이 아닐 수 있음)
            EditorUtility.SetDirty(shop);
            Debug.Log($"🏪 [MapLayout] 기존 Shop 이동 → {SHOP_POS}, duplicate {removed} removed");
            return 1 + removed;
        }

        // 신규: B01_MarketStall 프리팹으로 생성
        var prefab = LoadBuildingPrefab("B01_MarketStall");
        if (prefab == null)
        {
            // 폴백 — Primitive
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Shop";
            go.tag  = "Shop";
            go.transform.SetParent(mapBuildings, false);
            go.transform.SetPositionAndRotation(SHOP_POS, Quaternion.identity);
            go.transform.localScale = new Vector3(4f, 3f, 4f);
            go.AddComponent<Shop>();
            // 임시 ShopSlot 4개
            for (int i = 0; i < 4; i++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slot.name = $"ShopSlot_{i:00}";
                slot.transform.SetParent(go.transform, false);
                slot.transform.localPosition = new Vector3((i - 1.5f) * 1.2f, 0.5f, -2.5f);
                slot.transform.localScale = new Vector3(0.8f, 0.16f, 0.8f);
                slot.AddComponent<ShopSlot>();
            }
            Debug.Log($"🏪 [MapLayout] Shop placeholder 생성 (B01 prefab 없음)");
            return 1;
        }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = "Shop";
        inst.tag  = "Shop";
        inst.transform.SetParent(mapBuildings, false);
        inst.transform.SetPositionAndRotation(SHOP_POS, Quaternion.identity);
        // Shop 컴포넌트 자동 부착 (PA_ContentAutoIntegrator 가 prefab 에 이미 추가했지만 안전망)
        if (inst.GetComponent<Shop>() == null) inst.AddComponent<Shop>();
        Debug.Log($"🏪 [MapLayout] B01_MarketStall → Shop 으로 생성 → {SHOP_POS}");
        return 1;
    }

    static int PlaceWorkbenches(Transform mapBuildings)
    {
        int n = 0;
        var existing = UnityEngine.Object.FindObjectsByType<Workbench>(FindObjectsSortMode.None);

        foreach (var (type, pos) in WORKBENCH_LAYOUT)
        {
            var rotation = FaceNegativeZTowards(pos, new Vector3(0f, 0f, pos.z));
            var match = existing.FirstOrDefault(w => w.workbenchType == type);
            if (match != null)
            {
                match.transform.SetPositionAndRotation(pos, rotation);
                EditorUtility.SetDirty(match);
                Debug.Log($"🔨 [MapLayout] 기존 Workbench[{type}] 이동 → {pos}");
                n++;
                continue;
            }

            // 신규 — 해당 B05~B08 prefab 매핑
            string prefabName = type switch
            {
                WorkbenchType.BasicWorkbench => "B05_Workbench",
                WorkbenchType.Kitchen        => "B06_KitchenStation",
                WorkbenchType.Forge          => "B07_BlacksmithForge",
                WorkbenchType.SewingTable    => "B08_SewingTable",
                _ => null,
            };
            var prefab = prefabName != null ? LoadBuildingPrefab(prefabName) : null;
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = $"Workbench_{type}";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Workbench_{type}";
                go.transform.localScale = new Vector3(2f, 1.5f, 2f);
                var wb = go.AddComponent<Workbench>();
                wb.workbenchType = type;
            }
            go.transform.SetParent(mapBuildings, false);
            go.transform.SetPositionAndRotation(pos, rotation);
            // 안전망 — Workbench 컴포넌트 타입 보정
            var wbComp = go.GetComponent<Workbench>();
            if (wbComp == null) wbComp = go.AddComponent<Workbench>();
            wbComp.workbenchType = type;
            n++;
        }
        return n;
    }

    static int PlaceCottages(Transform mapBuildings)
    {
        int n = 0;
        var prefab = LoadBuildingPrefab("B10_Cottage");
        for (int i = 0; i < COTTAGE_POSITIONS.Length; i++)
        {
            string name = $"B10_Cottage_{i + 1:00}";
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(4f, 4f, 4f);
                ColorRenderer(go, new Color(0.88f, 0.78f, 0.62f));
            }
            go.name = name;
            go.transform.SetParent(mapBuildings, false);
            var position = COTTAGE_POSITIONS[i];
            go.transform.SetPositionAndRotation(position, FaceNegativeZTowards(position, PLAZA_POS));
            n++;
        }
        return n;
    }

    static int PlaceStorage(Transform mapBuildings)
    {
        // 기존 StorageBox 우선
        var existing = UnityEngine.Object.FindFirstObjectByType<StorageBox>();
        if (existing != null)
        {
            existing.transform.SetPositionAndRotation(STORAGE_POS, FaceNegativeZTowards(STORAGE_POS, SHOP_POS));
            EditorUtility.SetDirty(existing);
            Debug.Log($"📦 [MapLayout] 기존 StorageBox 이동 → {STORAGE_POS}");
            return 1;
        }

        var prefab = LoadBuildingPrefab("B09_StorageShed");
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(4f, 3.5f, 3f);
            ColorRenderer(go, new Color(0.62f, 0.48f, 0.32f));
            go.AddComponent<StorageBox>();
        }
        go.name = "B09_StorageShed";
        go.transform.SetParent(mapBuildings, false);
        go.transform.SetPositionAndRotation(STORAGE_POS, FaceNegativeZTowards(STORAGE_POS, SHOP_POS));
        if (go.GetComponent<StorageBox>() == null) go.AddComponent<StorageBox>();
        return 1;
    }

    static int PlacePlaza(Transform mapBuildings)
    {
        var prefab = LoadBuildingPrefab("B11_PlazaFountain");
        GameObject go;
        if (prefab != null)
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.localScale = new Vector3(4f, 0.7f, 4f);
            ColorRenderer(go, new Color(0.68f, 0.72f, 0.78f));
        }
        go.name = "B11_PlazaFountain";
        go.transform.SetParent(mapBuildings, false);
        go.transform.SetPositionAndRotation(PLAZA_POS, Quaternion.identity);
        return 1;
    }

    static int PlaceTradePort(Transform mapBuildings)
    {
        var prefab = LoadBuildingPrefab("B12_TradePort");
        GameObject go;
        if (prefab != null)
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(6f, 1.2f, 3f);
            ColorRenderer(go, new Color(0.58f, 0.42f, 0.32f));
        }
        go.name = "B12_TradePort";
        go.transform.SetParent(mapBuildings, false);
        go.transform.SetPositionAndRotation(PORT_POS, FaceNegativeZTowards(PORT_POS, new Vector3(PORT_POS.x, 0f, -60f)));
        return 1;
    }

    // ── 기존 [Markers] 의 WorkSpot/HomePoint 위치 갱신 ──────────────────
    // NPC.workSpot/homePoint 의 Transform 참조를 보존하기 위해 부모/이름을 바꾸지 않는다.
    // 마커가 아직 없으면 [MapMarkers] 아래에 새로 만든다.
    static int RepositionExistingMarkers()
    {
        int n = 0;
        var mapRoot = GameObject.Find(MAP_ROOT);
        var mapMarkers = mapRoot != null ? mapRoot.transform.Find(SUB_MARKERS) : null;

        foreach (var (name, pos) in MARKER_REPOSITION)
        {
            // 우선 씬 어디든 같은 이름의 마커 검색 (기존 [Markers] 자손 포함)
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                if (mapMarkers != null) go.transform.SetParent(mapMarkers, false);
                n++;
            }
            go.transform.position = pos;
            EditorUtility.SetDirty(go.transform);
        }
        Debug.Log($"📍 [MapLayout] 마커 {MARKER_REPOSITION.Length}개 위치 갱신");
        return n;
    }

    // ── SocialPoint 6개 광장 원형 배치 ───────────────────────────────────
    static int BuildSocialPoints(Transform mapMarkers)
    {
        const float radius = 4.5f;
        const int count = 6;
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            float x = PLAZA_POS.x + Mathf.Cos(angle) * radius;
            float z = PLAZA_POS.z + Mathf.Sin(angle) * radius;
            var go = new GameObject($"SocialPoint_{i + 1:00}");
            go.transform.SetParent(mapMarkers, false);
            go.transform.position = new Vector3(x, 0f, z);
        }
        return count;
    }

    // ── PlayerSpawn ─────────────────────────────────────────────────────
    static int EnsurePlayerSpawn(Transform mapMarkers)
    {
        // 우선 기존 PlayerSpawn_Default / PlayerSpawn_Outside / PlayerSpawn 검색
        var candidates = new[] { "PlayerSpawn_Default", "PlayerSpawn_Outside", "PlayerSpawn" };
        GameObject existing = null;
        foreach (var n in candidates)
        {
            existing = GameObject.Find(n);
            if (existing != null) break;
        }

        if (existing != null)
        {
            existing.transform.position = PLAYER_SPAWN_POS;
            EditorUtility.SetDirty(existing.transform);
            return 0;
        }

        var go = new GameObject("PlayerSpawn_Default");
        go.transform.SetParent(mapMarkers, false);
        go.transform.position = PLAYER_SPAWN_POS;
        return 1;
    }

    // ── HiringService.spawnPoint 자동 연결 ──────────────────────────────
    static int WireHiringSpawnPoint(Transform mapMarkers)
    {
        var hiring = UnityEngine.Object.FindFirstObjectByType<HiringService>();
        if (hiring == null) return 0;

        // [MapMarkers]/HiringSpawn 마커를 만들고 연결
        var existing = GameObject.Find("HiringSpawn");
        if (existing == null)
        {
            existing = new GameObject("HiringSpawn");
            existing.transform.SetParent(mapMarkers, false);
        }
        existing.transform.position = HIRING_SPAWN_POS;

        if (hiring.spawnPoint != existing.transform)
        {
            hiring.spawnPoint = existing.transform;
            EditorUtility.SetDirty(hiring);
            return 1;
        }
        return 0;
    }

    // ── NavMesh Bake ────────────────────────────────────────────────────
    static int BakeNavMesh()
    {
        try
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogWarning("🧭 [MapLayout] Ground 없음 — NavMesh Bake 스킵");
                return 0;
            }

            var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                Debug.LogWarning("🧭 [MapLayout] AI Navigation 패키지 없음 — Package Manager 에서 설치 필요");
                return 0;
            }

            int n = 0;
            var surface = ground.GetComponent(surfaceType);
            if (surface == null)
            {
                surface = ground.AddComponent(surfaceType);
                n++;
            }

            var bakeMethod = surfaceType.GetMethod("BuildNavMesh");
            if (bakeMethod == null)
            {
                Debug.LogWarning("🧭 [MapLayout] BuildNavMesh 메서드 없음 — Bake 스킵");
                return n;
            }

            bakeMethod.Invoke(surface, null);
            Debug.Log("🧭 [MapLayout] NavMesh Bake 완료");
            return n + 1;
        }
        catch (Exception e)
        {
            Debug.LogError($"🧭 [MapLayout] NavMesh Bake 실패: {e.Message}");
            return 0;
        }
    }

    // ── 검증 ────────────────────────────────────────────────────────────
    public static List<string> Validate()
    {
        var list = new List<string>();

        // 1. [PA_MapRoot] 중복 없음
        var roots = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t != null && t.name == MAP_ROOT && t.parent == null).ToList();
        list.Add(Mark(roots.Count == 1, $"[PA_MapRoot]: {roots.Count}"));

        // 2. Shop tag
        var shop = GameObject.FindGameObjectWithTag("Shop");
        list.Add(Mark(shop != null, shop != null ? $"Shop (Tag=Shop) @ {shop.transform.position}" : "Shop tag 없음"));

        // 3. ShopSlot ≥ 4
        var slots = UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None);
        list.Add(Mark(slots.Length >= 4, $"ShopSlot: {slots.Length}/4+"));

        // 4. Workbench 4종
        var benches = UnityEngine.Object.FindObjectsByType<Workbench>(FindObjectsSortMode.None);
        var types = benches.Select(b => b.workbenchType).Distinct().ToList();
        string typeJoin = string.Join(",", types);
        list.Add(Mark(types.Count >= 4, $"Workbench 타입: {types.Count}/4 ({typeJoin})"));

        // 5. StorageBox
        var storage = UnityEngine.Object.FindFirstObjectByType<StorageBox>();
        list.Add(Mark(storage != null, storage != null ? $"StorageBox @ {storage.transform.position}" : "StorageBox 없음"));

        // 6. PlayerSpawn
        bool hasSpawn = GameObject.Find("PlayerSpawn_Default") != null
            || GameObject.Find("PlayerSpawn_Outside") != null
            || GameObject.Find("PlayerSpawn") != null;
        list.Add(Mark(hasSpawn, "PlayerSpawn"));

        // 7. NPC HomePoint/WorkSpot
        int markerCount = 0;
        foreach (var (name, _) in MARKER_REPOSITION)
            if (GameObject.Find(name) != null) markerCount++;
        list.Add(Mark(markerCount >= 12, $"NPC HomePoint/WorkSpot: {markerCount}/{MARKER_REPOSITION.Length}"));

        // 8. SaveManager.allBuildingTypes
        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        int btCount = save != null ? (save.allBuildingTypes?.Count ?? 0) : 0;
        list.Add(Mark(btCount >= 12, $"SaveManager.allBuildingTypes: {btCount}/12"));

        // 9. NavMeshSurface
        var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        var ground = GameObject.Find("Ground");
        bool hasSurface = surfaceType != null && ground != null && ground.GetComponent(surfaceType) != null;
        list.Add(Mark(hasSurface, "NavMeshSurface on Ground"));

        // 10. 주요 빌딩 Collider 크기 — 너무 작거나(<1m) 너무 큰(>20m) 것 경고
        int colliderOk = 0, colliderBad = 0;
        foreach (var bc in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (bc.gameObject.name.StartsWith("B0") || bc.gameObject.name.StartsWith("B1") || bc.CompareTag("Shop") || bc.CompareTag("Building"))
            {
                Vector3 s = Vector3.Scale(bc.size, bc.transform.lossyScale);
                if (s.x < 1f || s.z < 1f || s.x > 20f || s.z > 20f) colliderBad++;
                else colliderOk++;
            }
        }
        list.Add(Mark(colliderBad == 0, $"Building Colliders: ok {colliderOk}, 이상 {colliderBad}"));

        return list;
    }

    static string Mark(bool ok, string text) => (ok ? "✅ " : "❌ ") + text;

    // ── 헬퍼 ────────────────────────────────────────────────────────────
    static GameObject PickPrimaryShop()
    {
        return SafeFindTaggedShops()
            .Where(go => go != null)
            .OrderByDescending(go => go.name == "Shop")
            .ThenBy(go => (go.transform.position - SHOP_POS).sqrMagnitude)
            .FirstOrDefault();
    }

    static int RemoveDuplicateGeneratedShops(GameObject keep)
    {
        int removed = 0;
        foreach (var shop in SafeFindTaggedShops())
        {
            if (shop == null || shop == keep) continue;
            if (!IsAutoGeneratedShop(shop)) continue;
            UnityEngine.Object.DestroyImmediate(shop);
            removed++;
        }

        return removed;
    }

    static GameObject[] SafeFindTaggedShops()
    {
        try
        {
            return GameObject.FindGameObjectsWithTag("Shop");
        }
        catch (UnityException)
        {
            return Array.Empty<GameObject>();
        }
    }

    static bool IsAutoGeneratedShop(GameObject go)
    {
        if (go.name == "Shop" || go.name.StartsWith("B01_", StringComparison.Ordinal)) return true;

        for (var t = go.transform; t != null; t = t.parent)
        {
            if (t.name == MAP_ROOT || t.name == SUB_BUILDINGS || t.name == LEGACY_WORLD_BUILDINGS)
                return true;
        }

        return false;
    }

    static Quaternion FaceNegativeZTowards(Vector3 position, Vector3 target)
    {
        var away = position - target;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) return Quaternion.identity;
        return Quaternion.LookRotation(away.normalized, Vector3.up);
    }

    static GameObject LoadBuildingPrefab(string id)
    {
        var path = $"{PREFAB_DIR}/{id}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogWarning($"⚠ [MapLayout] Prefab 없음: {path} — placeholder 사용");
        return prefab;
    }

    static void ColorRenderer(GameObject go, Color color)
    {
        var rend = go.GetComponent<MeshRenderer>();
        if (rend == null) return;
        // 인스턴스 머티리얼 (공유 자원 변경 방지)
        var mat = new Material(rend.sharedMaterial != null ? rend.sharedMaterial.shader : Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        rend.sharedMaterial = mat;
    }

    static void DestroyCollider(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
    }
}
#endif
