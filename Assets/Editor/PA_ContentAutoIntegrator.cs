#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Project P.A. content integration layer.
//
// 목표:
// - 아트 완성도를 기다리지 않고 게임 루프가 돌아가도록 모델/프리팹/데이터를 자동 연결한다.
// - B01~B12 빌딩 모델이 있으면 실제 모델을 쓰고, 없으면 플레이스홀더로 기능을 유지한다.
// - 반복 실행해도 같은 역할의 오브젝트/에셋을 중복 생성하지 않는다.
public static class PA_ContentAutoIntegrator
{
    const string ModelDir = "Assets/Models/Buildings";
    const string PrefabDir = "Assets/Prefabs/Buildings";
    const string BuildingDataDir = "Assets/Resources/Buildings";
    const string BlueprintItemDir = "Assets/Resources/Items/Blueprints";
    const string BuildingMaterialDir = "Assets/Materials/Buildings";
    const float CharacterScaleReferenceHeight = 3.6f; // 현재 플레이어/NPC 2배 스케일 기준 평균 키.
    static readonly Vector3 BuildingVisualAxisCorrectionEuler = new Vector3(-90f, 0f, 0f);

    static readonly BuildingDef[] Buildings =
    {
        new("B01_MarketStall",      "노점 상점",       "B01_MarketStall.fbx",      BuildingRole.Shop,      0,   500, 1001, new Vector3(5.0f, 4.2f, 4.0f),  4),
        new("B02_GeneralStore",     "잡화점",          "B02_GeneralStore.fbx",     BuildingRole.Shop,      1,  3000, 1002, new Vector3(8.0f, 6.0f, 6.0f),  8),
        new("B03_ConvenienceStore", "편의점",          "B03_ConvenienceStore.fbx", BuildingRole.Shop,      2, 12000, 1003, new Vector3(10.0f, 6.2f, 7.0f), 16),
        new("B04_DepartmentStore",  "백화점",          "B04_DepartmentStore.fbx",  BuildingRole.Shop,      3, 60000, 1004, new Vector3(13.0f, 10.0f, 8.5f), 32),
        new("B05_Workbench",        "기본 작업대",     "B05_Workbench.fbx",        BuildingRole.Workbench, 0,   350, 1005, new Vector3(3.2f, 2.0f, 2.6f),  0, WorkbenchType.BasicWorkbench),
        new("B06_KitchenStation",   "주방 스테이션",   "B06_KitchenStation.fbx",   BuildingRole.Workbench, 0,   650, 1006, new Vector3(3.4f, 2.6f, 2.8f),  0, WorkbenchType.Kitchen),
        new("B07_BlacksmithForge",  "대장간 스테이션", "B07_BlacksmithForge.fbx",  BuildingRole.Workbench, 1,   900, 1007, new Vector3(4.4f, 3.0f, 3.0f),  0, WorkbenchType.Forge),
        new("B08_SewingTable",      "재봉 스테이션",   "B08_SewingTable.fbx",      BuildingRole.Workbench, 1,   900, 1008, new Vector3(3.4f, 2.4f, 2.6f),  0, WorkbenchType.SewingTable),
        new("B09_StorageShed",      "창고",            "B09_StorageShed.fbx",      BuildingRole.Storage,   0,   600, 1009, new Vector3(7.0f, 5.2f, 5.5f),  0),
        new("B10_Cottage",          "NPC 집",          "B10_Cottage.fbx",          BuildingRole.Decor,     1,  2500, 1010, new Vector3(8.0f, 6.2f, 6.5f),  0),
        new("B11_PlazaFountain",    "광장 중심 시설",  "B11_PlazaFountain.fbx",    BuildingRole.Decor,     2,  5000, 1011, new Vector3(6.0f, 1.8f, 6.0f),  0),
        new("B12_TradePort",        "무역 항구 부두",  "B12_TradePort.fbx",        BuildingRole.Decor,     3, 25000, 1012, new Vector3(10.0f, 1.3f, 5.0f),  0),
    };

    public static void BuildAllFromMenu()
    {
        BuildAll(showDialog: true);
    }

    public static void BuildAllBatch()
    {
        BuildAll(showDialog: false);
    }

    public static void ValidateFromMenu()
    {
        ValidateContent(showDialog: true);
    }

    public static void RepairFromMenu()
    {
        RepairGeneratedContent(showDialog: true);
    }

    public static int BuildAll(bool showDialog)
    {
        int changed = 0;
        try
        {
            EnsureFolders();
            EnsureTags();
            changed += EnsureBuildingPrefabs();
            changed += EnsureBuildingDataAssets();
            changed += EnsureBlueprintItems();
            changed += WireSaveManagerBuildingTypes();
            changed += ApplySceneModelOverlays();
            changed += EnsureStaticWorldProps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            string summary = $"빌딩 콘텐츠 통합 완료: 변경 {changed}건\n" +
                             $"B01~B12 모델 → 프리팹/BuildingData/설계도 아이템/씬 모델 오버레이 연결.\n" +
                             $"스케일 기준: 캐릭터 높이 약 {CharacterScaleReferenceHeight:0.0}m 기준으로 모델 bounds 자동 정규화.";
            Debug.Log($"[PA Content] {summary}");
            if (showDialog) EditorUtility.DisplayDialog("P.A. 콘텐츠 통합", summary, "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PA Content] 콘텐츠 통합 실패: {e}");
            if (showDialog) EditorUtility.DisplayDialog("P.A. 콘텐츠 통합 실패", e.Message, "확인");
        }

        return changed;
    }

    public static int RepairGeneratedContent(bool showDialog)
    {
        return BuildAll(showDialog);
    }

    public static void ValidateContent(bool showDialog)
    {
        var missing = new List<string>();

        foreach (var def in Buildings)
        {
            string modelPath = $"{ModelDir}/{def.ModelFile}";
            string prefabPath = $"{PrefabDir}/{def.Id}.prefab";
            string dataPath = $"{BuildingDataDir}/Building_{def.Id}.asset";
            string itemPath = $"{BlueprintItemDir}/Blueprint_{def.Id}.asset";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null) missing.Add($"모델 없음: {modelPath}");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) missing.Add($"프리팹 없음: {prefabPath}");
            if (AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath) == null) missing.Add($"BuildingData 없음: {dataPath}");
            if (AssetDatabase.LoadAssetAtPath<Item>(itemPath) == null) missing.Add($"설계도 Item 없음: {itemPath}");
            if (string.IsNullOrEmpty(ResolveBuildingTexturePath(def))) missing.Add($"Texture missing: {def.Id} (.fbm/Color.jpg)");
            if (AssetDatabase.LoadAssetAtPath<Material>(GetBuildingMaterialPath(def)) == null) missing.Add($"Material missing: {GetBuildingMaterialPath(def)}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var collider = prefab.GetComponent<BoxCollider>();
                if (collider == null) missing.Add($"대표 BoxCollider 없음: {prefabPath}");
                else if (!Approximately(collider.size, def.ColliderSize)) missing.Add($"스케일 기준 불일치: {def.Id} collider {collider.size} != {def.ColliderSize}");
            }
        }

        string message = missing.Count == 0
            ? "B01~B12 빌딩 콘텐츠가 모두 연결되어 있습니다."
            : string.Join("\n", missing);

        if (missing.Count == 0) Debug.Log($"[PA Content] {message}");
        else Debug.LogWarning($"[PA Content] 검증 필요:\n{message}");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                missing.Count == 0 ? "콘텐츠 검증 성공" : "콘텐츠 검증 필요",
                message,
                "확인");
        }
    }

    static int EnsureBuildingPrefabs()
    {
        int changed = 0;
        foreach (var def in Buildings)
        {
            string prefabPath = $"{PrefabDir}/{def.Id}.prefab";
            GameObject root = CreateBuildingPrefabRoot(def);

            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
            UnityEngine.Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogWarning($"[PA Content] 프리팹 저장 실패: {prefabPath}");
                continue;
            }

            if (!existed) changed++;
        }

        return changed;
    }

    static GameObject CreateBuildingPrefabRoot(BuildingDef def)
    {
        var root = new GameObject(def.Id);
        root.tag = def.Role == BuildingRole.Shop ? "Shop" : "Building";

        AddVisual(root.transform, def, "Visual");
        AddColliderAndObstacle(root, def);
        AddGameplayComponents(root, def);

        return root;
    }

    static void AddVisual(Transform parent, BuildingDef def, string name)
    {
        string path = $"{ModelDir}/{def.ModelFile}";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject visual;

        if (model != null)
        {
            visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null) visual = UnityEngine.Object.Instantiate(model);
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.localScale = def.ColliderSize;
            Debug.LogWarning($"[PA Content] 모델 없음, 플레이스홀더 사용: {path}");
        }

        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = model != null
            ? Quaternion.Euler(BuildingVisualAxisCorrectionEuler)
            : Quaternion.identity;
        visual.transform.localScale = model != null ? Vector3.one : def.ColliderSize;

        foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        AssignBuildingMaterial(visual, def);
        NormalizeVisualToTargetBounds(visual.transform, parent, def);
    }

    static void AssignBuildingMaterial(GameObject visual, BuildingDef def)
    {
        var material = ResolveBuildingMaterial(def);
        if (material == null) return;

        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            if (slots == null || slots.Length == 0)
            {
                renderer.sharedMaterial = material;
            }
            else
            {
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = material;
                renderer.sharedMaterials = slots;
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    static Material ResolveBuildingMaterial(BuildingDef def)
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Buildings");

        string materialPath = GetBuildingMaterialPath(def);
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = ResolveDefaultShader();
            if (shader == null)
            {
                Debug.LogWarning($"[PA Content] Shader not found for building material: {def.Id}");
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        string texturePath = ResolveBuildingTexturePath(def);
        if (!string.IsNullOrEmpty(texturePath))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Shader ResolveDefaultShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");
    }

    static string GetBuildingMaterialPath(BuildingDef def)
    {
        return $"{BuildingMaterialDir}/Mat_{def.Id}.mat";
    }

    static string ResolveBuildingTexturePath(BuildingDef def)
    {
        string folder = $"{ModelDir}/{def.Id}.fbm";
        string preferred = $"{folder}/Color.jpg";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(preferred) != null) return preferred;
        if (!AssetDatabase.IsValidFolder(folder)) return null;

        return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
            .ThenBy(path => path)
            .FirstOrDefault();
    }

    static void NormalizeVisualToTargetBounds(Transform visual, Transform relativeTo, BuildingDef def)
    {
        if (!TryCalculateLocalRendererBounds(visual, relativeTo, out Bounds bounds)) return;
        if (bounds.size.x <= 0.001f || bounds.size.y <= 0.001f || bounds.size.z <= 0.001f) return;

        Vector3 target = def.ColliderSize;
        float scaleX = target.x / bounds.size.x;
        float scaleY = target.y / bounds.size.y;
        float scaleZ = target.z / bounds.size.z;
        float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);

        if (float.IsNaN(uniformScale) || float.IsInfinity(uniformScale) || uniformScale <= 0.001f) return;

        visual.localScale *= uniformScale;

        if (!TryCalculateLocalRendererBounds(visual, relativeTo, out Bounds scaledBounds)) return;
        Vector3 offset = new Vector3(-scaledBounds.center.x, -scaledBounds.min.y, -scaledBounds.center.z);
        visual.localPosition += offset;
    }

    static bool TryCalculateLocalRendererBounds(Transform root, Transform relativeTo, out Bounds bounds)
    {
        bounds = default;
        var renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r != null && r.enabled)
            .ToArray();
        if (renderers.Length == 0) return false;

        bool initialized = false;
        foreach (var renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners =
            {
                new(min.x, min.y, min.z),
                new(min.x, min.y, max.z),
                new(min.x, max.y, min.z),
                new(min.x, max.y, max.z),
                new(max.x, min.y, min.z),
                new(max.x, min.y, max.z),
                new(max.x, max.y, min.z),
                new(max.x, max.y, max.z),
            };

            foreach (var corner in corners)
            {
                Vector3 local = relativeTo.InverseTransformPoint(corner);
                if (!initialized)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return initialized;
    }

    static void AddColliderAndObstacle(GameObject root, BuildingDef def)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, def.ColliderSize.y * 0.5f, 0f);
        collider.size = def.ColliderSize;

        var obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = collider.center;
        obstacle.size = collider.size;
        obstacle.carving = true;
    }

    static void AddGameplayComponents(GameObject root, BuildingDef def)
    {
        switch (def.Role)
        {
            case BuildingRole.Shop:
                var shop = root.AddComponent<Shop>();
                shop.managedSlots = CreateShopSlots(root.transform, def);
                EditorUtility.SetDirty(shop);
                break;
            case BuildingRole.Workbench:
                var workbench = root.AddComponent<Workbench>();
                workbench.workbenchType = def.WorkbenchType;
                workbench.displayName = def.DisplayName;
                EditorUtility.SetDirty(workbench);
                break;
            case BuildingRole.Storage:
                var storage = root.AddComponent<StorageBox>();
                storage.boxName = def.DisplayName;
                storage.maxSlotCount = 24;
                EditorUtility.SetDirty(storage);
                break;
        }
    }

    static List<ShopSlot> CreateShopSlots(Transform root, BuildingDef def)
    {
        var slots = new List<ShopSlot>();
        var parent = new GameObject("ShopSlots");
        parent.transform.SetParent(root, false);

        int count = Mathf.Max(4, def.ShopSlotCount);
        int columns = Mathf.Min(8, count);
        int rows = Mathf.CeilToInt(count / (float)columns);
        float spacing = 1.05f;
        float startZ = -def.ColliderSize.z * 0.5f - 0.75f;

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.name = $"ShopSlot_{i:00}";
            slot.transform.SetParent(parent.transform, false);
            slot.transform.localPosition = new Vector3(
                (col - (columns - 1) * 0.5f) * spacing,
                0.08f,
                startZ - row * spacing);
            slot.transform.localScale = new Vector3(0.8f, 0.16f, 0.8f);

            var renderer = slot.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Brown.mat");

            slots.Add(slot.AddComponent<ShopSlot>());
        }

        return slots;
    }

    static int EnsureBuildingDataAssets()
    {
        int changed = 0;
        foreach (var def in Buildings)
        {
            string path = $"{BuildingDataDir}/Building_{def.Id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(data, path);
                changed++;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{def.Id}.prefab");
            bool dirty = false;
            dirty |= SetValue(ref data.buildingName, def.DisplayName);
            dirty |= SetValue(ref data.price, def.Price);
            dirty |= SetValue(ref data.prefab, prefab);
            dirty |= SetValue(ref data.requiredTier, def.RequiredTier);

            if (dirty)
            {
                EditorUtility.SetDirty(data);
                changed++;
            }
        }

        return changed;
    }

    static int EnsureBlueprintItems()
    {
        int changed = 0;
        foreach (var def in Buildings)
        {
            string path = $"{BlueprintItemDir}/Blueprint_{def.Id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<Item>();
                AssetDatabase.CreateAsset(item, path);
                changed++;
            }

            var data = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataDir}/Building_{def.Id}.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{def.Id}.prefab");

            bool dirty = false;
            dirty |= SetValue(ref item.id, def.BlueprintItemId);
            dirty |= SetValue(ref item.itemName, $"설계도: {def.DisplayName}");
            dirty |= SetValue(ref item.description, $"{def.DisplayName}을(를) 건설하는 프로토타입 설계도입니다.");
            dirty |= SetValue(ref item.basePrice, 0);
            dirty |= SetValue(ref item.toolType, ToolType.Building);
            dirty |= SetValue(ref item.category, ItemCategory.Tool);
            dirty |= SetValue(ref item.maxStack, 99);
            dirty |= SetValue(ref item.requiredTier, def.RequiredTier);
            dirty |= SetValue(ref item.buildingToBuild, data);
            dirty |= SetValue(ref item.model, prefab);

            if (dirty)
            {
                EditorUtility.SetDirty(item);
                changed++;
            }
        }

        return changed;
    }

    static int WireSaveManagerBuildingTypes()
    {
        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (save == null) return 0;

        var all = LoadAllBuildingData();
        if (SameList(save.allBuildingTypes, all)) return 0;

        save.allBuildingTypes = all;
        EditorUtility.SetDirty(save);
        return 1;
    }

    static int ApplySceneModelOverlays()
    {
        int changed = 0;

        var shop = GameObject.FindWithTag("Shop") ?? GameObject.Find("Shop");
        changed += EnsureOverlay(shop, Find("B01_MarketStall"), "PA_ModelOverlay_B01", hideExistingRenderers: true);

        changed += EnsureOverlay(GameObject.Find("Workbench_Basic"),       Find("B05_Workbench"),       "PA_ModelOverlay_B05", hideExistingRenderers: true);
        changed += EnsureOverlay(GameObject.Find("Workbench_Kitchen"),     Find("B06_KitchenStation"),  "PA_ModelOverlay_B06", hideExistingRenderers: true);
        changed += EnsureOverlay(GameObject.Find("Workbench_Forge"),       Find("B07_BlacksmithForge"), "PA_ModelOverlay_B07", hideExistingRenderers: true);
        changed += EnsureOverlay(GameObject.Find("Workbench_SewingTable"), Find("B08_SewingTable"),     "PA_ModelOverlay_B08", hideExistingRenderers: true);

        return changed;
    }

    static int EnsureStaticWorldProps()
    {
        int changed = 0;
        var parent = GameObject.Find("[WorldBuildings]");
        if (parent == null)
        {
            parent = new GameObject("[WorldBuildings]");
            changed++;
        }

        changed += EnsureStaticInstance(parent.transform, Find("B09_StorageShed"),   new Vector3(-22f, 0f,  4f), Quaternion.Euler(0f,  30f, 0f));
        changed += EnsureStaticInstance(parent.transform, Find("B10_Cottage"),       new Vector3(-24f, 0f, -5f), Quaternion.Euler(0f,  20f, 0f));
        changed += EnsureStaticInstance(parent.transform, Find("B11_PlazaFountain"), new Vector3(  0f, 0f,-18f), Quaternion.identity);
        changed += EnsureStaticInstance(parent.transform, Find("B12_TradePort"),     new Vector3(  8f, 0f, 25f), Quaternion.identity);

        return changed;
    }

    static int EnsureStaticInstance(Transform parent, BuildingDef def, Vector3 position, Quaternion rotation)
    {
        if (def == null) return 0;
        string name = $"{def.Id}_Static";
        if (parent.Find(name) != null) return 0;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{def.Id}.prefab");
        GameObject go = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : CreateBuildingPrefabRoot(def);

        if (go == null) return 0;
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(position, rotation);
        return 1;
    }

    static int EnsureOverlay(GameObject host, BuildingDef def, string overlayName, bool hideExistingRenderers)
    {
        if (host == null || def == null) return 0;
        if (host.transform.Find(overlayName) != null) return 0;

        if (hideExistingRenderers)
        {
            foreach (var renderer in host.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.name.StartsWith("PA_ModelOverlay_", StringComparison.Ordinal)) continue;
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }
        }

        AddVisual(host.transform, def, overlayName);
        return 1;
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Buildings");
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Buildings");
        EnsureFolder("Assets/Resources", "Items");
        EnsureFolder("Assets/Resources/Items", "Blueprints");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Buildings");
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent)) return;
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    static void EnsureTags()
    {
        EnsureTag("Shop");
        EnsureTag("Building");
    }

    static void EnsureTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var tagManager = new SerializedObject(assets[0]);
        var tagsProp = tagManager.FindProperty("tags");
        if (tagsProp == null) return;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static List<BuildingData> LoadAllBuildingData()
    {
        return AssetDatabase.FindAssets("t:BuildingData", new[] { BuildingDataDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<BuildingData>)
            .Where(x => x != null && x.prefab != null)
            .OrderBy(x => x.prefab.name)
            .ToList();
    }

    static BuildingDef Find(string id) => Buildings.FirstOrDefault(x => x.Id == id);

    static bool SameList<T>(List<T> a, List<T> b) where T : UnityEngine.Object
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.01f
            && Mathf.Abs(a.y - b.y) < 0.01f
            && Mathf.Abs(a.z - b.z) < 0.01f;
    }

    static bool SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        return true;
    }

    enum BuildingRole
    {
        Shop,
        Workbench,
        Storage,
        Decor
    }

    sealed class BuildingDef
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string ModelFile;
        public readonly BuildingRole Role;
        public readonly int RequiredTier;
        public readonly int Price;
        public readonly int BlueprintItemId;
        public readonly Vector3 ColliderSize;
        public readonly int ShopSlotCount;
        public readonly WorkbenchType WorkbenchType;

        public BuildingDef(
            string id,
            string displayName,
            string modelFile,
            BuildingRole role,
            int requiredTier,
            int price,
            int blueprintItemId,
            Vector3 colliderSize,
            int shopSlotCount,
            WorkbenchType workbenchType = WorkbenchType.BasicWorkbench)
        {
            Id = id;
            DisplayName = displayName;
            ModelFile = modelFile;
            Role = role;
            RequiredTier = requiredTier;
            Price = price;
            BlueprintItemId = blueprintItemId;
            ColliderSize = colliderSize;
            ShopSlotCount = shopSlotCount;
            WorkbenchType = workbenchType;
        }
    }
}
#endif
