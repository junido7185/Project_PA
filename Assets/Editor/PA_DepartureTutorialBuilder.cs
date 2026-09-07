using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// VS-PRESENT-001: 승인된 별도 출항 인증 씬만 생성한다. 기존 게임플레이 권위와 ART-000 원본은 재사용한다.
public static class PA_DepartureTutorialBuilder
{
    public const string ScenePath = "Assets/Scenes/PA_DepartureTutorial.unity";
    public const string ResourceRoot = "Assets/Resources/DepartureTutorial";
    public const string FruitPath = ResourceRoot + "/Item_TutorialFruit.asset";
    public const string BuyerProfilePath = ResourceRoot + "/Profile_TutorialBuyer.asset";
    const string DerivedRoot = "Assets/Art/ProjectPA/Derived/Departure/";
    const string IntakeRoot = "Assets/Art/ProjectPA/Prefabs/Intake/";
    static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();
    static TMP_FontAsset _font;

    [MenuItem("Project PA/Presentation/Build Departure Tutorial")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building the departure scene.");
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("An open scene has unsaved changes. Departure builder will not close it.");

        // 새 자산 경로만 소유한다. 기존 씬/리소스/FBX/importer 설정은 수정하지 않는다.
        EnsureFolder(ResourceRoot + "/Materials");
        EnsureFolder(ResourceRoot + "/Visuals");
        PreparePalette();
        _font = Require<TMP_FontAsset>("Assets/Fonts/Jalnan2_SDF.asset");
        GameObject shelfVisual = DerivedPrefab("PA_DepartureTrainingShelf");
        GameObject checkpointVisual = DerivedPrefab("PA_DepartureCheckpoint");
        GameObject fruitVisual = DerivedPrefab("PA_DepartureFruit");
        GameObject boatVisual = DerivedPrefab("PA_DepartureBoat");
        GameObject moveBoard = DerivedPrefab("PA_DepartureTrainingBoards", "Board_Move");
        GameObject harvestBoard = DerivedPrefab("PA_DepartureTrainingBoards", "Board_Harvest");
        GameObject tradeBoard = DerivedPrefab("PA_DepartureTrainingBoards", "Board_Trade");
        Item fruit = CreateFruit(fruitVisual);
        NpcProfile buyerProfile = CreateBuyerProfile();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject course = new GameObject("PA_DepartureCourse");
        Transform architecture = Child(course.transform, "HarborArchitecture", Vector3.zero);
        BuildHarbor(architecture, boatVisual);
        BuildBayFloor(architecture, "Bay01_Move", new Vector3(-6.7f, 0f, -2.3f), new Vector2(6.4f, 5.4f), "01", "MOVE");
        BuildBayFloor(architecture, "Bay02_Harvest", new Vector3(0f, 0f, -1.7f), new Vector2(5.6f, 6.6f), "02", "GATHER");
        BuildBayFloor(architecture, "Bay03_Trade", new Vector3(7f, 0f, -1.7f), new Vector2(6.4f, 6.6f), "03", "TRADE");
        for (float x = -7.7f; x <= 8.8f; x += 2f)
            FloorArrow(architecture, new Vector3(x, 0.028f, -3.5f));

        Place(moveBoard, architecture, "TrainingBoard_Move", new Vector3(-7.9f, 0f, 0.3f)).transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Place(harvestBoard, architecture, "TrainingBoard_Harvest", new Vector3(-2.05f, 0f, 1.35f)).transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Place(tradeBoard, architecture, "TrainingBoard_Trade", new Vector3(4.6f, 0f, 1.25f)).transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        WorldText(architecture, "MoveBoardCaption", "이동 인증", new Vector3(-7.9f, 2.85f, 0.15f), 0.28f, C("Cream"));
        WorldText(architecture, "GatherBoardCaption", "자원 확보", new Vector3(-2.05f, 2.85f, 1.2f), 0.28f, C("Cream"));
        WorldText(architecture, "TradeBoardCaption", "유통 인증", new Vector3(4.6f, 2.85f, 1.1f), 0.28f, C("Cream"));

        // 통과점은 이동을 막는 trigger/collider 없이 두 기둥과 바닥선으로 읽힌다.
        GameObject checkpoint = Place(checkpointVisual, architecture, "MovementCheckpoint", new Vector3(-5f, 0f, -3f));
        checkpoint.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Renderer moveLight = Box(architecture, "MoveCheckpointLight", new Vector3(-5f, 1.62f, -4.16f), new Vector3(0.24f, 0.16f, 0.24f), "Gold", false).GetComponent<Renderer>();
        Renderer gatherLight = Beacon(architecture, new Vector3(2.15f, 0f, -3.1f), "GatherCheckpointLight");
        Renderer tradeLight = Beacon(architecture, new Vector3(10.5f, 0f, -3.1f), "TradeCheckpointLight");

        Transform nature = Child(architecture, "NaturePractice", Vector3.zero);
        Box(nature, "SoilPracticeBed", new Vector3(0f, 0.018f, -0.35f), new Vector3(3.2f, 0.035f, 3f), "WoodLight", false);
        GameObject treeVisual = Intake("ULTIMATENATURE_COMMONTREE_1", nature, new Vector3(0f, 0f, -1f), 1f);
        ScaleToHeight(treeVisual.transform, 3.6f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * 2.39996f;
            Intake("ULTIMATENATURE_GRASS_SHORT", nature, new Vector3(Mathf.Sin(a) * 1.2f, 0.04f, -0.7f + Mathf.Cos(a) * 1.1f), 0.8f);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 0.4f;
            Place(fruitVisual, nature, "VisibleTrainingFruit_" + i, new Vector3(Mathf.Sin(a) * 0.9f, 2.08f + i % 2 * 0.24f, -1.45f + Mathf.Cos(a) * 0.52f));
        }
        // DaytimeStockPrepPoint는 자식 Renderer 색을 바꾸므로 실제 나무는 형제 visual로 보존한다.
        Transform treeAnchor = Child(nature, "TrainingFruitTree", new Vector3(0f, 0f, -1f));
        var treeCollider = treeAnchor.gameObject.AddComponent<CapsuleCollider>();
        treeCollider.radius = 0.7f;
        treeCollider.height = 2.4f;
        treeCollider.center = new Vector3(0f, 1.2f, 0f);
        treeCollider.isTrigger = true;
        var fruitTree = treeAnchor.gameObject.AddComponent<DaytimeStockPrepPoint>();
        fruitTree.activityId = "departure-tutorial-fruit";
        fruitTree.itemResourcePath = "DepartureTutorial/Item_TutorialFruit";
        fruitTree.grantCount = 3;
        fruitTree.displayName = "실습용 열매나무";
        Transform treeLabel = Child(treeAnchor, "Label", new Vector3(0f, 1.05f, 0f));
        treeLabel.gameObject.SetActive(false);
        Box(nature, "TreeTrunkPhysics", new Vector3(0f, 0.7f, -1f), new Vector3(0.38f, 1.4f, 0.38f), "Wood", true).GetComponent<Renderer>().enabled = false;

        Transform trade = Child(architecture, "TrainingShop", new Vector3(7f, 0f, -1f));
        var shop = trade.gameObject.AddComponent<Shop>();
        shop.allowDebugBulkSaleInteraction = false;
        Place(shelfVisual, trade, "TrainingShelfVisual", Vector3.zero, true);
        Box(trade, "ShelfPhysics", new Vector3(0f, 0.52f, 0f), new Vector3(1.72f, 1.04f, 1.18f), "Wood", true, true).GetComponent<Renderer>().enabled = false;
        Transform slotAnchor = Child(trade, "TrainingShopSlot", Vector3.zero);
        var slot = slotAnchor.gameObject.AddComponent<ShopSlot>();
        slot.displayOffset = new Vector3(0f, 1.015f, 0f);
        slot.fallbackDisplayScale = 1f;
        slot.displayPrice = 7;
        var slotCollider = slotAnchor.gameObject.AddComponent<BoxCollider>();
        slotCollider.center = new Vector3(0f, 0.65f, -0.25f);
        slotCollider.size = new Vector3(1.8f, 1.3f, 1.2f);
        slotCollider.isTrigger = true;
        shop.managedSlots.Add(slot);
        Child(slotAnchor, "InteractionAnchor", new Vector3(0f, 0f, -1.4f));
        WorldText(architecture, "ShelfCompanyMark", "P.A. TRAINING", new Vector3(7f, 0.7f, -1.68f), 0.19f, C("Ink"));
        WorldText(architecture, "SuggestedPrice", "열매  ·  실습 가격 7G", new Vector3(8.9f, 0.85f, -0.85f), 0.18f, C("Ink"));
        Intake("CUBEWORLDKIT_CHEST_CLOSED", architecture, new Vector3(9.6f, 0f, 0.75f), 0.65f);
        Intake("FOODKIT_BARREL", architecture, new Vector3(10.3f, 0f, 1.15f), 0.9f);

        Transform services = BuildAuthorities();
        Transform player = BuildPlayer();
        BuildCamera(player);
        NpcController buyer = BuildBuyer(buyerProfile, shop);
        var tutorial = services.gameObject.AddComponent<DepartureTutorialController>();
        var presentation = services.gameObject.AddComponent<DepartureTutorialPresentation>();
        presentation.font = _font;
        presentation.tutorial = tutorial;
        tutorial.player = player;
        tutorial.movementCheckpoint = checkpoint.transform;
        tutorial.fruit = fruit;
        tutorial.harvestFruitVisuals = nature.GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("VisibleTrainingFruit_")).Select(x => x.gameObject).ToArray();
        tutorial.fruitTree = fruitTree;
        tutorial.trainingSlot = slot;
        tutorial.buyer = buyer;
        tutorial.presentation = presentation;
        tutorial.checkpointLights = new[] { moveLight, gatherLight, tradeLight };

        var surface = architecture.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Departure NavMesh did not bake.");
        string navPath = ResourceRoot + "/DepartureNavigation.asset";
        NavMeshData existingData = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if (existingData == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
        else
        {
            NavMeshData generated = surface.navMeshData;
            surface.RemoveData();
            EditorUtility.CopySerialized(generated, existingData);
            surface.navMeshData = existingData;
            surface.AddData();
            EditorUtility.SetDirty(existingData);
        }
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save departure scene.");
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        Debug.Log("[VS-PRESENT-001] BUILD_PASS scene=" + ScenePath + " bays=3 existingAuthorities=true saveManagers=0");
    }

    [MenuItem("Project PA/Presentation/Open Departure Tutorial")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Open scene has unsaved changes; departure entry has stopped.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static Transform BuildAuthorities()
    {
        Transform services = new GameObject("PA_DepartureAuthorities").transform;
        services.gameObject.AddComponent<EconomyService>();
        services.gameObject.AddComponent<PlayerInputHandler>();
        var clock = services.gameObject.AddComponent<GameClock>();
        clock.startHour = 9f;
        clock.secondsPerGameHour = 3600f;
        services.gameObject.AddComponent<SalesLogManager>();
        var loop = services.gameObject.AddComponent<DayNightShopLoopController>();
        loop.autoCreateUI = false;
        loop.autoCreateDayPrepPoint = false;
        loop.keepDay1TutorialShopOpen = true;
        services.gameObject.AddComponent<ShopPriceUI>();
        services.gameObject.AddComponent<InteractPromptUI>();
        var feedback = services.gameObject.AddComponent<PurchaseFeedbackPresentationController>();
        feedback.autoCreateUI = false;
        services.gameObject.AddComponent<ShopCustomerApproachController>();
        services.gameObject.AddComponent<OutdoorPlacementController>().enabled = false;
        services.gameObject.AddComponent<ShopCustomizationController>().enabled = false;
        var culture = services.gameObject.AddComponent<VillageCultureVisualController>();
        culture.autoCreateVisual = false;
        culture.autoCreateHint = false;
        culture.enabled = false;
        Transform hiddenSign = Child(services, "UnusedDayNightSign", new Vector3(-80f, -30f, 0f));
        hiddenSign.gameObject.AddComponent<ShopOpenSign>().enabled = false;
        Child(hiddenSign, "Label", Vector3.zero).gameObject.SetActive(false);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        return services;
    }

    static Transform BuildPlayer()
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = new Vector3(-8f, 0.04f, -3f);
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        GameObject visual = Place(Require<GameObject>("Assets/Art/Character/C-01.fbx"), go.transform, "CharacterVisual", Vector3.zero, true);
        ScaleToHeight(visual.transform, 1.75f);
        var controller = go.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.25f;
        go.AddComponent<PlayerController>().moveSpeed = 4.2f;
        go.AddComponent<PlayerInteraction>().interactLayer = ~0;
        var hotbar = go.AddComponent<Hotbar>();
        var inventory = go.AddComponent<Inventory>();
        inventory.hotbar = hotbar;
        go.AddComponent<NpcHumanoidProceduralAnimator>();
        return go.transform;
    }

    static NpcController BuildBuyer(NpcProfile profile, Shop shop)
    {
        GameObject go = new GameObject("TutorialBuyer_TEMP");
        go.transform.position = new Vector3(11f, 0.04f, -2f);
        Place(Require<GameObject>("Assets/Art/Character/C-02.fbx"), go.transform, "CharacterVisual", Vector3.zero, true);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.speed = 2.2f;
        agent.acceleration = 10f;
        go.AddComponent<NpcPresentationNormalizer>();
        NpcPresentationNormalizer.Normalize(go);
        var npc = go.AddComponent<NpcController>();
        npc.profile = profile;
        npc.shopLocation = shop.transform;
        npc.randomSeed = 20260908;
        npc.idleTickInterval = 99999f;
        npc.wanderRadius = 0.1f;
        npc.maxSlotsPerVisit = 1;
        npc.browseDurationAtSlot = 2.5f;
        return npc;
    }

    static void BuildCamera(Transform player)
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = player.position + new Vector3(0f, 12f, -10f);
        go.transform.LookAt(player.position);
        Camera camera = go.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 140f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.69f, 0.85f, 0.91f);
        go.AddComponent<CameraController>().target = player;
        var sun = new GameObject("Harbor Afternoon Sun", typeof(Light));
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light light = sun.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.81f);
        light.intensity = 1.6f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.65f;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.64f, 0.73f, 0.76f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 48f;
        RenderSettings.fogEndDistance = 125f;
        RenderSettings.fogColor = camera.backgroundColor;
    }

    static void BuildHarbor(Transform parent, GameObject boat)
    {
        Box(parent, "HarborWater", new Vector3(5f, -0.65f, 20f), new Vector3(110f, 0.15f, 100f), "Teal", false);
        Box(parent, "QuaysideFoundation", new Vector3(0f, -0.48f, -0.7f), new Vector3(29f, 0.92f, 15.4f), "WoodLight", true);
        Box(parent, "LogisticsFloor", new Vector3(0f, -0.035f, -0.7f), new Vector3(28.6f, 0.07f, 15f), "Cream", true);
        Box(parent, "RearDockEdge", new Vector3(0f, 0.1f, 6.75f), new Vector3(28.8f, 0.2f, 0.35f), "Wood", true);
        Box(parent, "LeftSafetyRail", new Vector3(-14.15f, 0.6f, -0.7f), new Vector3(0.16f, 1.2f, 15f), "Wood", true);
        Box(parent, "FrontSafetyRail", new Vector3(0f, 0.38f, -8.2f), new Vector3(28.5f, 0.76f, 0.18f), "Wood", true);
        Box(parent, "RightSafetyRail", new Vector3(14.1f, 0.6f, -0.7f), new Vector3(0.16f, 1.2f, 15f), "Wood", true);
        for (int i = 0; i < 9; i++)
        {
            float x = -13f + i * 3.2f;
            Box(parent, "DockPile_" + i, new Vector3(x, -0.4f, 6.6f), new Vector3(0.42f, 2.6f, 0.42f), "Wood", false);
            if (i < 6)
                Box(parent, "CargoAisleDash_" + i, new Vector3(-11.5f + i * 4.5f, 0.018f, 3.1f), new Vector3(2.5f, 0.025f, 0.09f), "Terracotta", false);
        }
        // 실제 물류 공간의 배경: 낮은 적재대, 포장 작업대, 부두와 출항 대기 선박.
        CargoRack(parent, new Vector3(-10.8f, 0f, 4.3f));
        CargoRack(parent, new Vector3(-5.5f, 0f, 4.3f));
        PackingTable(parent, new Vector3(1f, 0f, 4.5f));
        Intake("FOODKIT_BARREL", parent, new Vector3(-12.8f, 0f, 1.9f), 1.05f);
        Intake("CUBEWORLDKIT_CHEST_CLOSED", parent, new Vector3(-11.2f, 0f, 1.5f), 0.7f);
        Intake("CUBEWORLDKIT_CHEST_CLOSED", parent, new Vector3(4f, 0f, 4.6f), 0.8f);
        Intake("FOODKIT_BARREL", parent, new Vector3(5.5f, 0f, 4.7f), 1f);
        GameObject dock = Intake("CUTEFISH_DOCK_LONG_NOROPE", parent, new Vector3(9f, -0.1f, 8.3f), 1f);
        dock.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Place(boat, parent, "DepartureBoat_Waiting", new Vector3(9f, -0.2f, 12f));
        Box(parent, "DepartureGateLeft", new Vector3(6.5f, 1.85f, 6f), new Vector3(0.24f, 3.7f, 0.24f), "Wood", true);
        Box(parent, "DepartureGateRight", new Vector3(11.5f, 1.85f, 6f), new Vector3(0.24f, 3.7f, 0.24f), "Wood", true);
        Box(parent, "DepartureGateHeader", new Vector3(9f, 3.3f, 6f), new Vector3(5.4f, 0.85f, 0.22f), "Ink", false);
        WorldText(parent, "DepartureGateTitle", "P.A. COMPANY  /  DEPARTURE", new Vector3(9f, 3.34f, 5.85f), 0.21f, C("Cream"));
        WorldText(parent, "DepartureGateCaption", "개척 파트너 출항 인증 구역", new Vector3(9f, 2.93f, 5.82f), 0.15f, C("Gold"));
        // 수면의 얇은 밝은 띠는 장식이며 navigation source에서 제외한다.
        for (int i = 0; i < 14; i++)
            Box(parent, "WaterGlint_" + i, new Vector3(-24f + i * 4.1f, -0.56f, 15f + i % 4 * 5.1f), new Vector3(1.8f + i % 3, 0.02f, 0.12f), "WaterLight", false);
    }

    static void BuildBayFloor(Transform parent, string name, Vector3 center, Vector2 size, string number, string action)
    {
        Transform bay = Child(parent, name, Vector3.zero);
        Box(bay, "TrainingPad", center + Vector3.up * 0.007f, new Vector3(size.x, 0.018f, size.y), "Pad", false);
        Box(bay, "LeftLine", center + new Vector3(-size.x / 2f, 0.024f, 0f), new Vector3(0.075f, 0.018f, size.y), "Gold", false);
        Box(bay, "RightLine", center + new Vector3(size.x / 2f, 0.024f, 0f), new Vector3(0.075f, 0.018f, size.y), "Gold", false);
        Box(bay, "BackLine", center + new Vector3(0f, 0.024f, size.y / 2f), new Vector3(size.x, 0.018f, 0.075f), "Gold", false);
        GameObject floorLabel = WorldText(bay, "BayFloorMark", number + " / " + action,
            center + new Vector3(0f, 0.036f, -size.y / 2f + 0.5f), 0.36f, C("Ink"));
        floorLabel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static void FloorArrow(Transform parent, Vector3 position)
    {
        Box(parent, "RouteArrowStem", position, new Vector3(0.75f, 0.02f, 0.12f), "Terracotta", false);
        GameObject left = Box(parent, "RouteArrowWing", position + new Vector3(0.29f, 0f, 0.14f), new Vector3(0.43f, 0.02f, 0.12f), "Terracotta", false);
        left.transform.rotation = Quaternion.Euler(0f, 43f, 0f);
        GameObject right = Box(parent, "RouteArrowWing", position + new Vector3(0.29f, 0f, -0.14f), new Vector3(0.43f, 0.02f, 0.12f), "Terracotta", false);
        right.transform.rotation = Quaternion.Euler(0f, -43f, 0f);
    }

    static Renderer Beacon(Transform parent, Vector3 position, string name)
    {
        Box(parent, name + "Post", position + Vector3.up * 0.45f, new Vector3(0.16f, 0.9f, 0.16f), "Wood", false);
        return Box(parent, name, position + Vector3.up * 0.97f, new Vector3(0.32f, 0.22f, 0.32f), "Gold", false).GetComponent<Renderer>();
    }

    static void CargoRack(Transform parent, Vector3 position)
    {
        Transform rack = Child(parent, "CargoRack", position);
        for (int i = 0; i < 4; i++)
            Box(rack, "RackPost", new Vector3(i % 2 == 0 ? -1.4f : 1.4f, 1.45f, i < 2 ? -0.55f : 0.55f), new Vector3(0.18f, 2.9f, 0.18f), "Ink", true, true);
        for (int level = 0; level < 2; level++)
        {
            Box(rack, "CargoShelf", new Vector3(0f, 0.18f + level * 1.45f, 0f), new Vector3(3.1f, 0.16f, 1.4f), "Wood", true, true);
            Intake("CUBEWORLDKIT_CHEST_CLOSED", rack, new Vector3(-0.7f, 0.26f + level * 1.45f, 0f), 0.5f, true);
            Intake("CUBEWORLDKIT_CHEST_CLOSED", rack, new Vector3(0.75f, 0.26f + level * 1.45f, 0f), 0.55f, true);
        }
    }

    static void PackingTable(Transform parent, Vector3 position)
    {
        Transform table = Child(parent, "PackingTable", position);
        Box(table, "Top", new Vector3(0f, 1f, 0f), new Vector3(2.6f, 0.18f, 1.3f), "WoodLight", true, true);
        for (int i = 0; i < 4; i++)
            Box(table, "Leg", new Vector3(i % 2 == 0 ? -1f : 1f, 0.5f, i < 2 ? -0.42f : 0.42f), new Vector3(0.18f, 1f, 0.18f), "Wood", true, true);
        Intake("CUBEWORLDKIT_CHEST_CLOSED", table, new Vector3(-0.5f, 1.09f, 0f), 0.4f, true);
        Box(table, "PackingPaper", new Vector3(0.65f, 1.1f, 0f), new Vector3(0.65f, 0.035f, 0.65f), "Cream", false, true);
    }

    static Item CreateFruit(GameObject visual)
    {
        Item item = LoadOrCreate<Item>(FruitPath);
        item.id = 910001;
        item.itemName = "실습용 열매";
        item.description = "TEMP · VS-PRESENT-001 출항 인증용 열매. 기존 Item/Inventory/Shop 가격 정책 사용.";
        item.maxStack = 20;
        item.basePrice = 10;
        item.toolType = ToolType.None;
        item.category = ItemCategory.Raw;
        item.requiredTier = 0;
        item.model = visual;
        EditorUtility.SetDirty(item);
        return item;
    }

    static NpcProfile CreateBuyerProfile()
    {
        NpcProfile source = Require<NpcProfile>("Assets/Resources/NPCs/Profile_Farmer.asset");
        NpcProfile profile = LoadOrCreate<NpcProfile>(BuyerProfilePath);
        EditorUtility.CopySerialized(source, profile);
        profile.name = "Profile_TutorialBuyer";
        profile.bio = "TEMP · 출항 인증 실습 손님. Profile_Farmer의 기존 이름과 MBTI를 재사용하며 실습 가격은 7G.";
        profile.priceSensitivity = 0.8f;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static GameObject DerivedPrefab(string modelName, string childName = null)
    {
        GameObject source = Require<GameObject>(DerivedRoot + modelName + ".fbx");
        GameObject wrapper = new GameObject(childName == null ? modelName : modelName + "_" + childName);
        try
        {
            GameObject visual = Place(source, wrapper.transform, "Visual", Vector3.zero, true);
            if (childName != null)
            {
                bool found = false;
                foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                {
                    if (!child.name.StartsWith("Board_", StringComparison.Ordinal)) continue;
                    bool selected = child.name == childName;
                    child.gameObject.SetActive(selected);
                    found |= selected;
                }
                if (!found) throw new InvalidOperationException("Required Blender board child missing: " + childName);
            }
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i] != null ? materials[i].name : string.Empty;
                    string key = Palette.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => name.StartsWith("PA_" + k, StringComparison.Ordinal));
                    if (key == null) throw new InvalidOperationException("Unmapped derived material: " + name);
                    materials[i] = Palette[key];
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            return PrefabUtility.SaveAsPrefabAsset(wrapper, ResourceRoot + "/Visuals/" + wrapper.name + ".prefab");
        }
        finally { Object.DestroyImmediate(wrapper); }
    }

    static GameObject Intake(string id, Transform parent, Vector3 position, float scale, bool local = false)
    {
        GameObject go = Place(Require<GameObject>(IntakeRoot + "PA_REF_" + id + ".prefab"), parent, "Dressing_" + id, position, local);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 position, bool local = false)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        if (local) go.transform.localPosition = position;
        else go.transform.position = position;
        return go;
    }

    static Transform Child(Transform parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, string material, bool solid, bool local = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = position;
        else go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = Palette[material];
        go.GetComponent<Collider>().enabled = solid;
        return go;
    }

    static GameObject WorldText(Transform parent, string name, string value, Vector3 position, float size, Color color)
    {
        var go = new GameObject(name, typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var text = go.GetComponent<TextMeshPro>();
        text.font = _font;
        text.text = value;
        text.fontSize = 3.6f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.rectTransform.sizeDelta = new Vector2(18f, 3f);
        go.transform.localScale = Vector3.one * size;
        return go;
    }

    static void ScaleToHeight(Transform root, float height)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("No renderers in " + root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (bounds.size.y < 0.001f) throw new InvalidOperationException("Invalid visual height: " + root.name);
        root.localScale *= height / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        root.position += Vector3.up * (root.parent.position.y + 0.02f - bounds.min.y);
    }

    static T Require<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("Departure source asset missing: " + path);
        return asset;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Color C(string key) => Palette[key].color;

    static void PreparePalette()
    {
        Palette.Clear();
        string[] colors = { "Wood:A07850", "WoodLight:D4B896", "Leaf:8DB87A", "LeafDark:547957", "Terracotta:D4714A",
            "Coral:F08070", "Gold:F5D76E", "Teal:5BAFC0", "Cream:F5EAD5", "Ink:344B4E", "Apple:D6604F", "Pad:D2D9BA", "WaterLight:91CAD1" };
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("Existing URP Lit shader is required.");
        foreach (string entry in colors)
        {
            string[] pair = entry.Split(':');
            string path = ResourceRoot + "/Materials/PA_" + pair[0] + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "PA_" + pair[0] };
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString("#" + pair[1], out Color color);
            material.color = color;
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            Palette.Add(pair[0], material);
        }
    }
}
