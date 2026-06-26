using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_VisualAccelerationBuilder
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string MarketPrefabFolder = "Assets/Prefabs/Market";
    const string MarketMaterialFolder = "Assets/Materials/Market";
    const string MarketArtFolder = "Assets/Art/Market";
    const string HubPrefabPath = MarketPrefabFolder + "/PA_MarketStall_Hub.prefab";
    const string PlaySmokeActiveKey = "PA.VisualAcceleration.PlaySmoke.Active";
    const string PlaySmokeEnteredKey = "PA.VisualAcceleration.PlaySmoke.Entered";
    const string PlaySmokeHadErrorKey = "PA.VisualAcceleration.PlaySmoke.HadError";

    static readonly Color Wood = new Color(0.58f, 0.36f, 0.20f);
    static readonly Color DarkWood = new Color(0.32f, 0.20f, 0.13f);
    static readonly Color Cream = new Color(0.94f, 0.86f, 0.66f);
    static readonly Color Coral = new Color(0.94f, 0.39f, 0.30f);
    static readonly Color Gold = new Color(0.96f, 0.78f, 0.28f);
    static readonly Color SupplyBlue = new Color(0.35f, 0.66f, 0.82f);
    static readonly Color CustomerGreen = new Color(0.44f, 0.78f, 0.45f);
    static readonly Color ProcessPurple = new Color(0.62f, 0.50f, 0.78f);
    static readonly Color PathClay = new Color(0.72f, 0.54f, 0.34f);
    static readonly Color LabelWarm = new Color(1f, 0.94f, 0.62f);

    static bool _playSmokeEntered;
    static bool _playSmokeHadError;
    static double _playSmokeStartTime;

    static PA_VisualAccelerationBuilder()
    {
        if (!SessionState.GetBool(PlaySmokeActiveKey, false))
            return;

        _playSmokeEntered = SessionState.GetBool(PlaySmokeEnteredKey, false);
        _playSmokeHadError = SessionState.GetBool(PlaySmokeHadErrorKey, false);
        RegisterPlaySmokeCallbacks();

        if (_playSmokeEntered && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += FinishPlaySmoke;
    }

    [MenuItem("Project PA/Visual Acceleration/Build T010-T014 Demo Staging")]
    public static void RunT010ToT014()
    {
        EnsureFolders();
        var materials = EnsureMaterials();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Visual Acceleration: failed to open scene at {ScenePath}");
            return;
        }

        var shop = FindDemoShop();
        if (shop == null)
        {
            Debug.LogError("PA Visual Acceleration: no Shop component found in Prototype_FirstDay.");
            return;
        }

        var hubRoot = EnsureChild(shop.transform, "PA_MarketStall_Hub_Visual");
        hubRoot.localPosition = Vector3.zero;
        hubRoot.localRotation = Quaternion.identity;
        hubRoot.localScale = Vector3.one;

        BuildMarketHub(hubRoot, shop, materials);
        BuildSlotMarkers(hubRoot, shop, materials);
        BuildDemoRouteMarkers(shop.transform, materials);
        BuildNpcRoleBadges(materials);
        BuildScreenshotMarker(shop.transform);

        PrefabUtility.SaveAsPrefabAsset(hubRoot.gameObject, HubPrefabPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PA Visual Acceleration: T010-T014 staging complete. Gameplay components were preserved.");
    }

    [MenuItem("Project PA/Visual Acceleration/Verify Prototype FirstDay Staging")]
    public static void VerifyPrototypeFirstDayStaging()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Visual Verification: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        int shopCount = Object.FindObjectsByType<Shop>(FindObjectsSortMode.None).Length;
        int slotCount = Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None).Length;
        int roleBadgeCount = CountTransformsNamed("PA_EconomicRoleBadge");
        int visualRootCount = CountTransformsNamed("PA_MarketStall_Hub_Visual");
        int routeRootCount = CountTransformsNamed("PA_DemoRoute_VisualMarkers");
        int screenshotMarkerCount = CountTransformsNamed("PA_ScreenshotCameraMarker_MarketHub");

        bool buildSettingsOk = EditorBuildSettings.scenes.Length > 0
            && EditorBuildSettings.scenes[0].enabled
            && EditorBuildSettings.scenes[0].path == ScenePath;

        Debug.Log($"PA Visual Verification: shops={shopCount}, shopSlots={slotCount}, hubVisuals={visualRootCount}, routeMarkers={routeRootCount}, roleBadges={roleBadgeCount}, screenshotMarkers={screenshotMarkerCount}, buildSettingsOk={buildSettingsOk}");

        bool ok = shopCount > 0
            && slotCount >= 4
            && visualRootCount > 0
            && routeRootCount > 0
            && roleBadgeCount > 0
            && screenshotMarkerCount > 0
            && buildSettingsOk;

        if (!ok)
        {
            Debug.LogError("PA Visual Verification failed. Stop before continuing visual acceleration.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("PA Visual Verification passed. Prototype_FirstDay staging and core shop references are present.");
    }

    [MenuItem("Project PA/Visual Acceleration/Repair Prototype FirstDay NavMesh")]
    public static void RepairPrototypeFirstDayNavMeshForPlayer()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA NavMesh Repair: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogError("PA NavMesh Repair: Ground object was not found.");
            EditorApplication.Exit(1);
            return;
        }

        var surface = EnsureNavMeshSurface(ground);
        if (surface == null)
        {
            Debug.LogError("PA NavMesh Repair: NavMeshSurface type was not available.");
            EditorApplication.Exit(1);
            return;
        }

        ConfigureNavMeshSurface(surface);

        var bakeMethod = surface.GetType().GetMethod("BuildNavMesh");
        if (bakeMethod == null)
        {
            Debug.LogError("PA NavMesh Repair: BuildNavMesh method was not available.");
            EditorApplication.Exit(1);
            return;
        }

        bakeMethod.Invoke(surface, null);
        surface.GetType().GetMethod("AddData")?.Invoke(surface, null);

        int sampleHits = CountNavMeshSampleHits();
        int snappedNpcs = SnapNpcsToNavMesh();
        int disabledAgents = DisableSceneNpcAgentsForRuntimeBinder();

        EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"PA NavMesh Repair: sampleHits={sampleHits}, snappedNpcs={snappedNpcs}, disabledNpcAgents={disabledAgents}. RuntimeBinder will enable NPC agents after NavMeshSurface data is active.");

        if (sampleHits == 0)
        {
            Debug.LogError("PA NavMesh Repair failed: no valid NavMesh sample positions were found after bake.");
            EditorApplication.Exit(1);
        }
    }

    static int CountTransformsNamed(string transformName)
    {
        int count = 0;
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform.name == transformName)
                count++;
        }
        return count;
    }

    static Component EnsureNavMeshSurface(GameObject ground)
    {
        Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation")
            ?? Type.GetType("UnityEngine.AI.NavMeshSurface");
        if (surfaceType == null) return null;

        return ground.GetComponent(surfaceType) ?? ground.AddComponent(surfaceType);
    }

    static void ConfigureNavMeshSurface(Component surface)
    {
        SetSurfaceProperty(surface, "collectObjects", 0);
        SetSurfaceProperty(surface, "useGeometry", NavMeshCollectGeometry.RenderMeshes);
        SetSurfaceProperty(surface, "defaultArea", 0);
        SetSurfaceProperty(surface, "ignoreNavMeshAgent", true);
        SetSurfaceProperty(surface, "ignoreNavMeshObstacle", true);
        SetSurfaceProperty(surface, "minRegionArea", 0.5f);
        EditorUtility.SetDirty(surface);
    }

    static void SetSurfaceProperty(Component surface, string propertyName, object value)
    {
        var property = surface.GetType().GetProperty(propertyName);
        if (property == null || !property.CanWrite) return;

        object typedValue = value;
        if (property.PropertyType.IsEnum && value is int enumValue)
            typedValue = Enum.ToObject(property.PropertyType, enumValue);

        property.SetValue(surface, typedValue);
    }

    static int CountNavMeshSampleHits()
    {
        int hits = 0;
        Vector3[] probes =
        {
            Vector3.zero,
            new Vector3(0f, 0f, -3f),
            new Vector3(-3f, 0f, 0f),
            new Vector3(3f, 0f, 0f),
            new Vector3(0f, 0f, 4f)
        };

        foreach (var point in probes)
        {
            if (NavMesh.SamplePosition(point, out _, 20f, NavMesh.AllAreas))
                hits++;
        }

        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc != null && NavMesh.SamplePosition(npc.transform.position, out _, 12f, NavMesh.AllAreas))
                hits++;
        }

        return hits;
    }

    static int SnapNpcsToNavMesh()
    {
        int changed = 0;
        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            if (!NavMesh.SamplePosition(npc.transform.position, out var hit, 12f, NavMesh.AllAreas)) continue;
            if (Vector3.Distance(npc.transform.position, hit.position) <= 0.05f) continue;

            npc.transform.position = hit.position;
            EditorUtility.SetDirty(npc.transform);
            changed++;
        }
        return changed;
    }

    static int DisableSceneNpcAgentsForRuntimeBinder()
    {
        int changed = 0;
        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled) continue;

            agent.enabled = false;
            EditorUtility.SetDirty(agent);
            changed++;
        }
        return changed;
    }

    [MenuItem("Project PA/Visual Acceleration/Run Prototype FirstDay Play Smoke Test")]
    public static void RunPrototypeFirstDayPlaySmokeTest()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Play Smoke: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _playSmokeEntered = false;
        _playSmokeHadError = false;
        _playSmokeStartTime = EditorApplication.timeSinceStartup;
        SessionState.SetBool(PlaySmokeActiveKey, true);
        SessionState.SetBool(PlaySmokeEnteredKey, false);
        SessionState.SetBool(PlaySmokeHadErrorKey, false);

        RegisterPlaySmokeCallbacks();

        Debug.Log("PA Play Smoke: entering Play Mode for Prototype_FirstDay.");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterPlaySmokeCallbacks()
    {
        Application.logMessageReceived -= OnPlaySmokeLog;
        EditorApplication.playModeStateChanged -= OnPlaySmokeStateChanged;
        EditorApplication.update -= OnPlaySmokeUpdate;

        Application.logMessageReceived += OnPlaySmokeLog;
        EditorApplication.playModeStateChanged += OnPlaySmokeStateChanged;
        EditorApplication.update += OnPlaySmokeUpdate;
        _playSmokeStartTime = EditorApplication.timeSinceStartup;
    }

    static void OnPlaySmokeLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _playSmokeHadError = true;
            SessionState.SetBool(PlaySmokeHadErrorKey, true);
        }
    }

    static void OnPlaySmokeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            CapturePlaySmokeRuntimeCounts();

        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(PlaySmokeActiveKey, false))
            FinishPlaySmoke();
    }

    static void OnPlaySmokeUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _playSmokeStartTime;
        if (!_playSmokeEntered && EditorApplication.isPlaying)
            CapturePlaySmokeRuntimeCounts();

        if (!_playSmokeEntered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA Play Smoke timed out before entering Play Mode.");
                CleanupPlaySmoke();
                EditorApplication.Exit(1);
            }
            return;
        }

        if (elapsed > 5.0 && EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();

        if (_playSmokeEntered && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            FinishPlaySmoke();
    }

    static void CapturePlaySmokeRuntimeCounts()
    {
        if (_playSmokeEntered)
            return;

        _playSmokeEntered = true;
        SessionState.SetBool(PlaySmokeEnteredKey, true);
        _playSmokeStartTime = EditorApplication.timeSinceStartup;

        int playerCount = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;
        int shopCount = Object.FindObjectsByType<Shop>(FindObjectsSortMode.None).Length;
        int slotCount = Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None).Length;
        int economyCount = Object.FindObjectsByType<EconomyService>(FindObjectsSortMode.None).Length;
        int priceUiCount = Object.FindObjectsByType<ShopPriceUI>(FindObjectsSortMode.None).Length;
        var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int npcCount = npcs.Length;
        int npcAgentCount = 0;
        int npcAgentsEnabled = 0;
        int npcAgentsOnMesh = 0;
        int npcNearNavMesh = 0;

        foreach (var npc in npcs)
        {
            if (npc == null) continue;

            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                npcAgentCount++;
                if (agent.enabled) npcAgentsEnabled++;
                if (agent.enabled && agent.isOnNavMesh) npcAgentsOnMesh++;
            }

            if (NavMesh.SamplePosition(npc.transform.position, out _, 12f, NavMesh.AllAreas))
                npcNearNavMesh++;
        }

        Debug.Log($"PA Play Smoke: entered Play Mode. players={playerCount}, shops={shopCount}, shopSlots={slotCount}, economyServices={economyCount}, shopPriceUI={priceUiCount}, npcs={npcCount}, npcAgents={npcAgentsEnabled}/{npcAgentCount}, npcAgentsOnMesh={npcAgentsOnMesh}, npcNearNavMesh={npcNearNavMesh}");

        if (playerCount == 0
            || shopCount == 0
            || slotCount < 4
            || economyCount == 0
            || (npcCount > 0 && (npcAgentCount == 0 || npcAgentsOnMesh == 0 || npcNearNavMesh == 0)))
        {
            _playSmokeHadError = true;
            SessionState.SetBool(PlaySmokeHadErrorKey, true);
        }
    }

    static void FinishPlaySmoke()
    {
        bool hadError = _playSmokeHadError || SessionState.GetBool(PlaySmokeHadErrorKey, false);
        bool entered = _playSmokeEntered || SessionState.GetBool(PlaySmokeEnteredKey, false);

        CleanupPlaySmoke();
        SessionState.EraseBool(PlaySmokeActiveKey);
        SessionState.EraseBool(PlaySmokeEnteredKey);
        SessionState.EraseBool(PlaySmokeHadErrorKey);

        if (!entered || hadError)
        {
            Debug.LogError("PA Play Smoke failed. Check runtime log before continuing.");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("PA Play Smoke passed. Prototype_FirstDay entered Play Mode and core runtime objects were present.");
            EditorApplication.Exit(0);
        }
    }

    static void CleanupPlaySmoke()
    {
        Application.logMessageReceived -= OnPlaySmokeLog;
        EditorApplication.playModeStateChanged -= OnPlaySmokeStateChanged;
        EditorApplication.update -= OnPlaySmokeUpdate;
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Market");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Market");
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Market");
    }

    static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    static MaterialSet EnsureMaterials()
    {
        return new MaterialSet
        {
            Wood = EnsureMaterial("PA_Market_Wood", Wood),
            DarkWood = EnsureMaterial("PA_Market_DarkWood", DarkWood),
            Cream = EnsureMaterial("PA_Market_AwningCream", Cream),
            Coral = EnsureMaterial("PA_Market_AwningCoral", Coral),
            Gold = EnsureMaterial("PA_Market_PriceGold", Gold),
            SupplyBlue = EnsureMaterial("PA_Market_SupplyBlue", SupplyBlue),
            CustomerGreen = EnsureMaterial("PA_Market_CustomerGreen", CustomerGreen),
            ProcessPurple = EnsureMaterial("PA_Market_ProcessPurple", ProcessPurple),
            PathClay = EnsureMaterial("PA_Market_PathClay", PathClay),
            LabelWarm = EnsureMaterial("PA_Market_LabelWarm", LabelWarm)
        };
    }

    static Material EnsureMaterial(string name, Color color)
    {
        var path = $"{MarketMaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Shop FindDemoShop()
    {
        Shop fallback = null;
        foreach (var shop in Object.FindObjectsByType<Shop>(FindObjectsSortMode.None))
        {
            if (fallback == null) fallback = shop;
            if (shop.name.Contains("Prototype") || shop.name == "Shop")
                return shop;
        }
        return fallback;
    }

    static void BuildMarketHub(Transform hubRoot, Shop shop, MaterialSet mat)
    {
        EnsurePrimitive(hubRoot, "PA_WoodPost_FL", PrimitiveType.Cube, new Vector3(-2.35f, 1.1f, -1.9f), Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), mat.Wood);
        EnsurePrimitive(hubRoot, "PA_WoodPost_FR", PrimitiveType.Cube, new Vector3(2.35f, 1.1f, -1.9f), Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), mat.Wood);
        EnsurePrimitive(hubRoot, "PA_WoodPost_BL", PrimitiveType.Cube, new Vector3(-2.35f, 1.1f, 1.65f), Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), mat.Wood);
        EnsurePrimitive(hubRoot, "PA_WoodPost_BR", PrimitiveType.Cube, new Vector3(2.35f, 1.1f, 1.65f), Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), mat.Wood);

        EnsurePrimitive(hubRoot, "PA_FrontCounter", PrimitiveType.Cube, new Vector3(0f, 0.72f, -1.9f), Vector3.zero, new Vector3(4.65f, 0.28f, 0.55f), mat.DarkWood);
        EnsurePrimitive(hubRoot, "PA_BackShelf", PrimitiveType.Cube, new Vector3(0f, 1.2f, 1.12f), Vector3.zero, new Vector3(4.3f, 0.22f, 0.42f), mat.Wood);
        EnsurePrimitive(hubRoot, "PA_MidProductShelf", PrimitiveType.Cube, new Vector3(0f, 1.0f, -0.25f), Vector3.zero, new Vector3(4.0f, 0.16f, 0.45f), mat.Wood);

        EnsurePrimitive(hubRoot, "PA_Awning_Base_Cream", PrimitiveType.Cube, new Vector3(0f, 2.55f, -0.1f), new Vector3(0f, 0f, 0f), new Vector3(4.95f, 0.12f, 3.85f), mat.Cream);
        EnsurePrimitive(hubRoot, "PA_Awning_Stripe_Left", PrimitiveType.Cube, new Vector3(-1.65f, 2.63f, -0.1f), Vector3.zero, new Vector3(0.55f, 0.08f, 3.95f), mat.Coral);
        EnsurePrimitive(hubRoot, "PA_Awning_Stripe_Center", PrimitiveType.Cube, new Vector3(0f, 2.64f, -0.1f), Vector3.zero, new Vector3(0.55f, 0.08f, 3.95f), mat.Coral);
        EnsurePrimitive(hubRoot, "PA_Awning_Stripe_Right", PrimitiveType.Cube, new Vector3(1.65f, 2.63f, -0.1f), Vector3.zero, new Vector3(0.55f, 0.08f, 3.95f), mat.Coral);

        EnsurePrimitive(hubRoot, "PA_MarketSign_Board", PrimitiveType.Cube, new Vector3(0f, 2.95f, -2.05f), Vector3.zero, new Vector3(2.4f, 0.12f, 0.55f), mat.DarkWood);
        EnsureWorldLabel(hubRoot, "PA_MarketSign_Label", "P.A. 운영 허브\nMANAGEMENT HUB", new Vector3(0f, 3.1f, -2.12f), LabelWarm, 1.25f);

        EnsurePrimitive(hubRoot, "PA_SupplyDropBox", PrimitiveType.Cube, new Vector3(-3.15f, 0.35f, 0.95f), Vector3.zero, new Vector3(0.85f, 0.7f, 0.85f), mat.SupplyBlue);
        EnsurePrimitive(hubRoot, "PA_ProductCrate_RawGoods", PrimitiveType.Cube, new Vector3(-1.65f, 0.55f, -1.2f), Vector3.zero, new Vector3(0.72f, 0.42f, 0.55f), mat.PathClay);
        EnsurePrimitive(hubRoot, "PA_ProductCrate_ProcessedGoods", PrimitiveType.Cube, new Vector3(1.65f, 0.55f, -1.2f), Vector3.zero, new Vector3(0.72f, 0.42f, 0.55f), mat.ProcessPurple);
        EnsurePrimitive(hubRoot, "PA_WarmLantern_Left", PrimitiveType.Sphere, new Vector3(-2.05f, 2.18f, -1.85f), Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), mat.Gold);
        EnsurePrimitive(hubRoot, "PA_WarmLantern_Right", PrimitiveType.Sphere, new Vector3(2.05f, 2.18f, -1.85f), Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), mat.Gold);

        EnsureWorldLabel(hubRoot, "PA_Supply_Label", "납품\nSUPPLY", new Vector3(-3.15f, 1.05f, 0.95f), SupplyBlue, 1.1f);
        EnsureWorldLabel(hubRoot, "PA_Process_Label", "가공\nPROCESS", new Vector3(0f, 1.65f, 1.12f), ProcessPurple, 1.1f);
        EnsureWorldLabel(hubRoot, "PA_Sale_Label", "진열/가격\nPRICE", new Vector3(0f, 1.35f, -1.92f), Gold, 1.1f);
    }

    static void BuildSlotMarkers(Transform hubRoot, Shop shop, MaterialSet mat)
    {
        var slots = shop.GetComponentsInChildren<ShopSlot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            var local = shop.transform.InverseTransformPoint(slot.transform.position);
            var markerPosition = local + new Vector3(0f, 0.36f, 0f);
            var tagPosition = local + new Vector3(0f, 0.78f, -0.34f);

            EnsurePrimitive(hubRoot, $"PA_ShopSlotMarker_{i:00}", PrimitiveType.Cube, markerPosition, Vector3.zero, new Vector3(0.72f, 0.06f, 0.72f), mat.Gold);
            EnsurePrimitive(hubRoot, $"PA_PriceTag_{i:00}", PrimitiveType.Cube, tagPosition, new Vector3(22f, 0f, 0f), new Vector3(0.58f, 0.04f, 0.22f), mat.LabelWarm);
            EnsureWorldLabel(hubRoot, $"PA_PriceTagLabel_{i:00}", $"Slot {i + 1}\n가격", tagPosition + new Vector3(0f, 0.16f, -0.03f), Gold, 0.78f);
        }
    }

    static void BuildDemoRouteMarkers(Transform shopRoot, MaterialSet mat)
    {
        var routeRoot = EnsureChild(shopRoot, "PA_DemoRoute_VisualMarkers");
        routeRoot.localPosition = Vector3.zero;
        routeRoot.localRotation = Quaternion.identity;
        routeRoot.localScale = Vector3.one;

        EnsurePrimitive(routeRoot, "PA_CustomerApproachPad", PrimitiveType.Cube, new Vector3(0f, 0.03f, -3.45f), Vector3.zero, new Vector3(2.1f, 0.06f, 1.0f), mat.CustomerGreen);
        EnsureWorldLabel(routeRoot, "PA_CustomerApproach_Label", "고객 구매 판단\nCUSTOMER CHECK", new Vector3(0f, 0.55f, -3.45f), CustomerGreen, 0.95f);

        EnsurePrimitive(routeRoot, "PA_DeliveryPad", PrimitiveType.Cube, new Vector3(-3.15f, 0.025f, 0.2f), Vector3.zero, new Vector3(1.1f, 0.05f, 1.4f), mat.SupplyBlue);
        EnsurePrimitive(routeRoot, "PA_ReinvestmentPad", PrimitiveType.Cube, new Vector3(3.15f, 0.025f, 0.2f), Vector3.zero, new Vector3(1.1f, 0.05f, 1.4f), mat.Gold);
        EnsureWorldLabel(routeRoot, "PA_Reinvestment_Label", "수익/성장\nREINVEST", new Vector3(3.15f, 0.58f, 0.2f), Gold, 0.95f);

        EnsurePrimitive(routeRoot, "PA_PathStep_01_Talk", PrimitiveType.Cube, new Vector3(-2.7f, 0.02f, -2.75f), Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), mat.PathClay);
        EnsurePrimitive(routeRoot, "PA_PathStep_02_Stock", PrimitiveType.Cube, new Vector3(-1.35f, 0.02f, -2.95f), Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), mat.PathClay);
        EnsurePrimitive(routeRoot, "PA_PathStep_03_Price", PrimitiveType.Cube, new Vector3(0f, 0.02f, -3.08f), Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), mat.PathClay);
        EnsurePrimitive(routeRoot, "PA_PathStep_04_Sale", PrimitiveType.Cube, new Vector3(1.35f, 0.02f, -2.95f), Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), mat.PathClay);
        EnsurePrimitive(routeRoot, "PA_PathStep_05_Revenue", PrimitiveType.Cube, new Vector3(2.7f, 0.02f, -2.75f), Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), mat.PathClay);

        EnsureWorldLabel(routeRoot, "PA_DemoRoute_Label", "말 걸기 → 진열 → 가격 → 구매 → 매출", new Vector3(0f, 0.72f, -4.25f), LabelWarm, 0.95f);
    }

    static void BuildNpcRoleBadges(MaterialSet mat)
    {
        foreach (var dialogue in Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None))
        {
            if (dialogue == null) continue;
            var npcName = dialogue.gameObject.name;
            string role = ResolveNpcRole(dialogue.gameObject);
            if (string.IsNullOrEmpty(role)) continue;

            var badge = EnsureChild(dialogue.transform, "PA_EconomicRoleBadge");
            badge.localPosition = new Vector3(0f, 2.55f, 0f);
            badge.localRotation = Quaternion.identity;
            badge.localScale = Vector3.one;

            var color = role.Contains("생산자") ? SupplyBlue :
                role.Contains("전문가") ? ProcessPurple :
                role.Contains("안내") ? LabelWarm :
                CustomerGreen;

            var label = badge.GetComponent<PrototypeWorldLabel>() ?? badge.gameObject.AddComponent<PrototypeWorldLabel>();
            label.Set($"{role}\n{npcName}", color, 1.15f);
            EditorUtility.SetDirty(label);
        }
    }

    static string ResolveNpcRole(GameObject npc)
    {
        var name = npc.name.ToLowerInvariant();
        if (npc.GetComponent<ProducerNpcController>() != null || name.Contains("farmer") || name.Contains("miner") || name.Contains("fisher"))
            return "생산자";
        if (npc.GetComponent<SpecialistNpcController>() != null || name.Contains("chef") || name.Contains("blacksmith"))
            return "전문가";
        if (name.Contains("firstsettler") || name.Contains("bori"))
            return "안내 NPC";
        if (npc.GetComponent<NpcController>() != null)
            return "소비자";
        return "";
    }

    static void BuildScreenshotMarker(Transform shopRoot)
    {
        var marker = EnsureChild(shopRoot, "PA_ScreenshotCameraMarker_MarketHub");
        marker.localPosition = new Vector3(0f, 6.5f, -8.0f);
        marker.localRotation = Quaternion.Euler(58f, 0f, 0f);
        marker.localScale = Vector3.one;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        var existing = parent.Find(name);
        GameObject go;
        if (existing == null)
        {
            go = GameObject.CreatePrimitive(type);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            foreach (var collider in go.GetComponents<Collider>())
                Object.DestroyImmediate(collider);
        }
        else
        {
            go = existing.gameObject;
        }

        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        EditorUtility.SetDirty(go);
        return go;
    }

    static PrototypeWorldLabel EnsureWorldLabel(Transform parent, string name, string text, Vector3 localPosition, Color color, float fontSize)
    {
        var child = EnsureChild(parent, name);
        child.localPosition = localPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        var label = child.GetComponent<PrototypeWorldLabel>() ?? child.gameObject.AddComponent<PrototypeWorldLabel>();
        label.Set(text, color, fontSize);
        EditorUtility.SetDirty(label);
        return label;
    }

    sealed class MaterialSet
    {
        public Material Wood;
        public Material DarkWood;
        public Material Cream;
        public Material Coral;
        public Material Gold;
        public Material SupplyBlue;
        public Material CustomerGreen;
        public Material ProcessPurple;
        public Material PathClay;
        public Material LabelWarm;
    }
}
