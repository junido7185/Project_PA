using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Narrow asset preparation, not a validator. Writes only the owned DemoStructure folder.
public static class PA_DemoStructureSetup
{
    const string Root = "Assets/Resources/DemoStructure";
    public static void Prepare()
    {
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        var art = FirstDayStudioAssets.Load();
        if (art == null) throw new InvalidOperationException("Prepared First Day catalog required.");
        var catalog = Asset<DemoPlaceableCatalog>("Catalog");
        var bench = RootObject("FieldWorkbench");
        FirstDayStudioAssets.Place(art.workbench, bench.transform, Vector3.zero, 1.2f);
        AddBox(bench, new Vector3(0, .6f, 0), new Vector3(1.6f, 1.2f, 1.4f));
        bench.AddComponent<Workbench>().displayName = "P.A. Field Workbench";
        bench.GetComponent<Workbench>().workbenchType = WorkbenchType.BasicWorkbench;
        SaveBuilding(bench, "FieldWorkbench");

        var stand = RootObject("StarterDisplayStand");
        var displayArt = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_MINIMARKET_DISPLAY_FRUIT.prefab");
        FirstDayStudioAssets.Place(displayArt, stand.transform, Vector3.zero, .95f);
        AddBox(stand, new Vector3(0, .5f, 0), new Vector3(1.5f, 1, 1.5f));
        stand.AddComponent<ShopSlot>().displayOffset = new Vector3(0, 1.03f, 0);
        var standPrefab = SaveBuilding(stand, "StarterDisplayStand");
        var standItem = Asset<Item>("Item_StarterDisplayStand");
        standItem.id = 2020; standItem.itemName = "Starter Display Stand"; standItem.maxStack = 3;
        standItem.category = ItemCategory.Tool; standItem.model = displayArt;
        EditorUtility.SetDirty(standItem);

        var furniture = RootObject("SimpleFurniture");
        var chair = PA_OwnedChairSetup.Model(); // 2026-11-09 Froggy Chair(라이선스 문서 없음, 07 큐 결정) 대신 소유 의자
        FirstDayStudioAssets.Place(chair, furniture.transform, Vector3.zero, 1.1f);
        AddBox(furniture, new Vector3(0, .5f, 0), new Vector3(1.4f, 1, 1.4f));
        var furniturePrefab = SaveBuilding(furniture, "SimpleFurniture");
        var furnitureItem = Asset<Item>("Item_SimpleFurniture");
        furnitureItem.id = 2021; furnitureItem.itemName = "Chair"; furnitureItem.category = ItemCategory.Tool;
        furnitureItem.model = chair; EditorUtility.SetDirty(furnitureItem);
        var chairRecipe = Asset<RecipeData>("Recipe_Chair");
        chairRecipe.recipeName = "Chair"; chairRecipe.requiredWorkbench = WorkbenchType.BasicWorkbench;
        chairRecipe.outputItem = furnitureItem; chairRecipe.outputCount = 1;
        chairRecipe.ingredients = new System.Collections.Generic.List<RecipeIngredient> {
            new RecipeIngredient { item=Resources.Load<Item>("Items/Item_Wood"), count=2 } };
        EditorUtility.SetDirty(chairRecipe);

        // An open-front home/shop shell. Interior cells remain walkable; only the
        // perimeter is occupied. No Management Hub and no extra placement grid.
        var perimeter = Enumerable.Range(0, 42).Select(i => new Vector2Int(i % 7, i / 7))
            .Where(c => c.x == 0 || c.x == 6 || c.y == 5).ToArray();
        Vector2 mean = perimeter.Aggregate(Vector2.zero, (sum, c) => sum + (Vector2)c) / perimeter.Length;
        var shop = RootObject("PioneerShopBase");
        shop.AddComponent<Shop>().allowDebugBulkSaleInteraction = false;
        var cottage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Buildings/B10_Cottage.fbx");
        FirstDayStudioAssets.Place(cottage, shop.transform, new Vector3(0, 0, (5 - mean.y) * 2), 3.4f);
        foreach (var cell in perimeter)
            AddBox(shop, new Vector3((cell.x - mean.x) * 2, 1.5f, (cell.y - mean.y) * 2), new Vector3(1.85f, 3, 1.85f));
        Zone(shop, "ShopFloor", WorldPlaceableSurface.ShopFloor, new Vector3(0, 0, (1 - mean.y) * 2), new Vector3(10, 4, 6), 10);
        Zone(shop, "Living", WorldPlaceableSurface.Living, new Vector3(-3, 0, (3.5f - mean.y) * 2), new Vector3(4, 4, 4), 10);
        Zone(shop, "Utility", WorldPlaceableSurface.Utility, new Vector3(3, 0, (3.5f - mean.y) * 2), new Vector3(4, 4, 4), 10);
        Zone(shop, "DoorAndCustomerAisle", WorldPlaceableSurface.None, new Vector3(0, 0, (2 - mean.y) * 2), new Vector3(1.9f, 4, 12), 20);
        BuildInterior(shop);
        SaveBuilding(shop, "PioneerShopBase");

        catalog.entries = new[]
        {
            new DemoPlaceableEntry { item=Resources.Load<Item>("FirstDayStudio/Item_2012"), key="shop-base", kind=DemoPlaceableKind.ShopBase, buildingResource="DemoStructure/PioneerShopBase", footprint=perimeter, entrance=new Vector2Int(3,-1), canMove=false, surfaces=WorldPlaceableSurface.Settlement },
            new DemoPlaceableEntry { item=Resources.Load<Item>("FirstDayStudio/Item_2013"), key="resident-tent", kind=DemoPlaceableKind.ResidentTent, buildingResource="DepartureTutorial/Settlement/Shelter", size=new Vector2Int(2,2), surfaces=WorldPlaceableSurface.Settlement },
            new DemoPlaceableEntry { item=Resources.Load<Item>("FirstDayStudio/Item_2014"), key="field-workbench", kind=DemoPlaceableKind.Workbench, buildingResource="DemoStructure/FieldWorkbench", surfaces=WorldPlaceableSurface.Settlement | WorldPlaceableSurface.Utility | WorldPlaceableSurface.Living },
            new DemoPlaceableEntry { item=standItem, key="display-stand", kind=DemoPlaceableKind.DisplayStand, buildingResource="DemoStructure/StarterDisplayStand", surfaces=WorldPlaceableSurface.ShopFloor },
            new DemoPlaceableEntry { item=furnitureItem, key="furniture-chair", kind=DemoPlaceableKind.Furniture, buildingResource="DemoStructure/SimpleFurniture", surfaces=WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor } // 2026-11-02 가게 꾸밈(손님 매력)
        // 용도별 작업대(조리대·화로)는 PA_CraftingStationsSetup이 만든다. 이 배열을 다시 써도 이미 만든 항목은 남긴다.
        }.Concat(PA_CraftingStationsSetup.ExistingEntries()).Concat(PA_BeachFindsSetup.ExistingEntries()).Concat(PA_BeachGlassSetup.ExistingEntries()).Concat(PA_PotterySetup.ExistingEntries()).Concat(PA_CollectionPlaqueSetup.ExistingEntries()).Concat(PA_CrystalSetup.ExistingEntries()).Concat(PA_InsectBoardSetup.ExistingEntries()).Concat(PA_ChairVariantsSetup.ExistingEntries()).ToArray();
        var improved = Asset<Item>("Item_ImprovedPickaxe");
        improved.id = 2022; improved.itemName = "구리 곡괭이"; improved.toolType = ToolType.Pickaxe;
        improved.category = ItemCategory.Tool; improved.maxStack = 1; improved.model = art.pickaxe;
        var recipe = Asset<RecipeData>("Recipe_ImprovedPickaxe");
        recipe.recipeName = "Improved Pickaxe"; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench;
        recipe.requiredTier = 0; recipe.outputItem = improved; recipe.outputCount = 1;
        var starter = art.supplies.First(i => i.toolType == ToolType.Pickaxe);
        recipe.ingredients = new System.Collections.Generic.List<RecipeIngredient> {
            new RecipeIngredient { item=starter, count=1 },
            new RecipeIngredient { item=Resources.Load<Item>("Items/Item_Wood"), count=2 },
            new RecipeIngredient { item=Resources.Load<Item>("Items/Item_Ore"), count=2 } };
        catalog.upgrades = new[] { new DemoToolUpgrade { recipe=recipe, tool=improved, starterTool=starter,
            root=DemoSpecialization.None, specialty=NpcSpecialty.Miner, companionPrefix="Miner",
            production=Resources.Load<ProductionData>("Production/Production_Ore"), npcEfficiency=1.5f, playerWorkStrength=2 } };
        EditorUtility.SetDirty(improved); EditorUtility.SetDirty(recipe);
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        // 2026-11-02 도구 단계: 구리 곡괭이 레시피(화로·구리 주괴)는 PA_ToolProgressionSetup이 정의한다. 위 기본값을 덮어쓴다.
        if (AssetDatabase.LoadAssetAtPath<Item>("Assets/Resources/Items/Item_CopperBar.asset") != null) PA_ToolProgressionSetup.Apply();
        Debug.Log("[DEMO-STRUCTURE] Owned assets prepared; no scene edit or Play executed.");
    }
    // 기존 자산 하나만 수정한다. 전체 Prepare를 실행해 다른 dirty 자산을 재생성하지 않는다.
    public static void UpgradeShopInterior()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (shop.GetComponent<DemoShopInterior>() != null)
                throw new InvalidOperationException("Interior already authored; inspect instead of rebuilding.");
            BuildInterior(shop);
            PrefabUtility.SaveAsPrefabAsset(shop, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    // 2026-11-04 ACNH 소품 연구 H1: 기존 가게 프리팹의 InteriorCutaway에 아기자기 소품만 더한다(있으면 지우고 다시).
    // 배치: Unity -batchmode -executeMethod PA_DemoStructureSetup.FinishCozyInteriorBatch
    public static void FinishCozyInterior()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var binding = shop.GetComponent<DemoShopInterior>() ?? throw new InvalidOperationException("Interior binding missing.");
            var room = binding.interior.transform;
            var old = room.Find("CozyDressing");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            DressInteriorCozy(room);
            PrefabUtility.SaveAsPrefabAsset(shop, path);
            Debug.Log("[DEMO-STRUCTURE] Cozy interior dressing: " + room.Find("CozyDressing").childCount + " parts");
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    public static void FinishCozyInteriorBatch()
    {
        int code = 0;
        try { FinishCozyInterior(); AssetDatabase.SaveAssets(); }
        catch (Exception e) { Debug.LogError("[DEMO-STRUCTURE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void RepairShopDoorBoundary()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            FinishDoorBoundary(shop.GetComponent<DemoShopInterior>());
            PrefabUtility.SaveAsPrefabAsset(shop, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    // Canon v2 §7–8: 기존 출입/점유를 유지하고 표현 자식만 조립한다.
    public static void FinishShopPresentation()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var binding = shop.GetComponent<DemoShopInterior>();
            var shell = binding.exterior.transform;
            if (shell.Find("ShopArchitecture") != null)
                throw new InvalidOperationException("Presentation already authored; inspect before changing.");
            foreach (Transform old in shell) old.gameObject.SetActive(false);
            shell.localScale = Vector3.one; shell.localPosition = Vector3.zero;
            var facade = new GameObject("ShopArchitecture").transform;
            facade.SetParent(shell, false);
            var wood = RoomMaterial("InteriorWood", new Color(.56f, .37f, .20f));
            var cream = RoomMaterial("InteriorPlaster", new Color(.88f, .80f, .63f));
            var teal = RoomMaterial("InteriorRug", new Color(.28f, .48f, .40f));
            var dark = RoomMaterial("ShopTrim", new Color(.24f, .16f, .10f));
            var glass = RoomMaterial("ShopGlass", new Color(.45f, .68f, .68f));
            var linen = RoomMaterial("LivingLinen", new Color(.93f, .88f, .72f));
            RoomBox(facade, "Body", new Vector3(0, 1.4f, -2), new Vector3(10.2f, 2.8f, 10.5f), cream);
            RoomBox(facade, "Foundation", new Vector3(0, .14f, -2), new Vector3(10.5f, .28f, 10.7f), wood);
            foreach (int side in new[] { -1, 1 })
            {
                RoomBox(facade, "Roof" + side, new Vector3(side * 2.7f, 3.55f, -2), new Vector3(5.7f, .18f, 11.3f), teal);
                facade.Find("Roof" + side).localRotation = Quaternion.Euler(0, 0, -side * 17);
                RoomBox(facade, "Corner" + side, new Vector3(side * 5, 1.4f, -7.29f), new Vector3(.18f, 2.8f, .16f), dark);
                RoomBox(facade, "WindowFrame" + side, new Vector3(side * 3, 1.5f, -7.32f), new Vector3(2.45f, 1.5f, .14f), dark);
                RoomBox(facade, "Window" + side, new Vector3(side * 3, 1.5f, -7.41f), new Vector3(2.2f, 1.25f, .08f), glass);
                RoomBox(facade, "WindowBar" + side, new Vector3(side * 3, 1.5f, -7.48f), new Vector3(.08f, 1.25f, .05f), wood);
                RoomBox(facade, "WindowSill" + side, new Vector3(side * 3, .83f, -7.48f), new Vector3(2.5f, .12f, .3f), wood);
            }
            RoomBox(facade, "Ridge", new Vector3(0, 4.38f, -2), new Vector3(.16f, .18f, 11.4f), dark);
            RoomBox(facade, "DoorFrame", new Vector3(0, 1.12f, -7.38f), new Vector3(1.9f, 2.24f, .18f), dark);
            RoomBox(facade, "Door", new Vector3(0, 1.04f, -7.50f), new Vector3(1.55f, 2.08f, .10f), wood);
            RoomBox(facade, "DoorGlass", new Vector3(0, 1.38f, -7.57f), new Vector3(.98f, .85f, .06f), glass);
            RoomBox(facade, "Handle", new Vector3(.55f, .85f, -7.63f), new Vector3(.08f, .24f, .08f), linen);
            RoomBox(facade, "Awning", new Vector3(0, 2.55f, -7.7f), new Vector3(3.5f, .15f, 1.2f), teal);
            facade.Find("Awning").localRotation = Quaternion.Euler(-10, 0, 0);
            RoomBox(facade, "ShopSign", new Vector3(0, 3.02f, -7.36f), new Vector3(3.2f, .55f, .16f), dark);
            var sign = new GameObject("ShopName").AddComponent<TMPro.TextMeshPro>();
            sign.transform.SetParent(facade, false); sign.transform.localPosition = new Vector3(0, 3.03f, -7.47f);
            sign.transform.localRotation = Quaternion.identity;
            sign.text = "P.A. SHOP"; sign.fontSize = 3; sign.color = new Color(.97f, .91f, .72f);
            sign.alignment = TMPro.TextAlignmentOptions.Center; sign.rectTransform.sizeDelta = new Vector2(3, .5f);
            binding.entrance.transform.Find("DoorPanel").gameObject.SetActive(false);

            var room = binding.interior.transform;
            var living = new GameObject("LivingFurnishings").transform; living.SetParent(room, false);
            RoomBox(living, "BedFrame", new Vector3(-4, .22f, .15f), new Vector3(1.45f, .40f, 2.3f), wood);
            RoomBox(living, "Mattress", new Vector3(-4, .48f, .15f), new Vector3(1.34f, .18f, 2.16f), linen);
            RoomBox(living, "Blanket", new Vector3(-4, .59f, -.22f), new Vector3(1.35f, .07f, 1.35f), teal);
            RoomBox(living, "Pillow", new Vector3(-4, .61f, .9f), new Vector3(.9f, .15f, .45f), linen);
            RoomBox(living, "Headboard", new Vector3(-4, .6f, 1.3f), new Vector3(1.5f, 1.12f, .12f), dark);
            RoomBox(living, "TableTop", new Vector3(-2, .69f, 1.45f), new Vector3(1.2f, .12f, .85f), wood);
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                RoomBox(living, "TableLeg", new Vector3(-2+x*.45f, .34f, 1.45f+z*.28f), new Vector3(.1f, .68f, .1f), dark);
            RoomBox(living, "LampStem", new Vector3(-2, .95f, 1.45f), new Vector3(.07f, .45f, .07f), dark);
            RoomBox(living, "LampShade", new Vector3(-2, 1.18f, 1.45f), new Vector3(.45f, .3f, .45f), linen);
            var chair = room.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CozyChair");
            if (chair != null) chair.position += shop.transform.TransformVector(new Vector3(1, 0, -.3f));
            // 고정 가구가 차지하는 칸은 기존 placeable zone으로만 제외한다.
            Zone(shop, "StarterBedReserved", WorldPlaceableSurface.None, new Vector3(-4, 0, .2f), new Vector3(1.65f, 4, 2.5f), 30);
            Zone(shop, "LivingTableReserved", WorldPlaceableSurface.None, new Vector3(-2, 0, 1.45f), new Vector3(1.4f, 4, 1), 30);
            AddBox(shop, new Vector3(-4, .4f, .2f), new Vector3(1.4f, .8f, 2.25f));
            AddBox(shop, new Vector3(-2, .38f, 1.45f), new Vector3(1.2f, .76f, .85f));
            var art = FirstDayStudioAssets.Load();
            FirstDayStudioAssets.Place(art.chestClosed, room, shop.transform.TransformPoint(new Vector3(4, .04f, 1.6f)), .8f);
            var shelf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_MINIMARKET_SHELF_END.prefab");
            FirstDayStudioAssets.Place(shelf, room, shop.transform.TransformPoint(new Vector3(4.3f, .04f, .1f)), 1.3f, 90);
            Zone(shop, "UtilityStorageReserved", WorldPlaceableSurface.None, new Vector3(4, 0, .8f), new Vector3(1.8f, 4, 2.8f), 30);
            RoomBox(room, "ShopRunner", new Vector3(0, .045f, -3.8f), new Vector3(1.65f, .02f, 5.5f), teal);
            RoomBox(room, "LivingDivider", new Vector3(-2.8f, .2f, -1.65f), new Vector3(4.3f, .32f, .14f), wood);
            PrefabUtility.SaveAsPrefabAsset(shop, path); AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    public static void FinishDisplayStand()
    {
        const string path = Root + "/StarterDisplayStand.prefab";
        var stand = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (stand.transform.Find("EmptyCounter") == null)
            {
                // Existing fruit display is one combined mesh; retain it inactive.
                foreach (Transform child in stand.transform) child.gameObject.SetActive(false);
                var counter = new GameObject("EmptyCounter").transform; counter.SetParent(stand.transform, false);
                var wood = RoomMaterial("InteriorWood", new Color(.56f, .37f, .20f));
                var cream = RoomMaterial("LivingLinen", new Color(.93f, .88f, .72f));
                RoomBox(counter, "CounterTop", new Vector3(0, .85f, 0), new Vector3(1.35f, .15f, 1.15f), wood);
                RoomBox(counter, "DisplayMat", new Vector3(0, .94f, 0), new Vector3(1.12f, .025f, .92f), cream);
                foreach (int x in new[] {-1,1}) foreach (int z in new[] {-1,1})
                    RoomBox(counter, "Leg", new Vector3(x*.5f, .4f, z*.4f), new Vector3(.13f, .8f, .13f), wood);
                RoomBox(counter, "FrontRail", new Vector3(0, .56f, -.4f), new Vector3(1.1f, .3f, .1f), wood);
            }
            stand.GetComponent<ShopSlot>().displayOffset = new Vector3(0, .97f, 0);
            PrefabUtility.SaveAsPrefabAsset(stand, path);
            var item = Asset<Item>("Item_StarterDisplayStand"); item.model = AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("EmptyCounter").gameObject;
            EditorUtility.SetDirty(item); AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(stand); }
    }

    // 2026-11-04 ACNH 소품 연구 H1: 진열대(EmptyCounter)에 식탁보 치마·양옆 늘어진 천·청록 단·다리 받침. 윗면 높이(.97 진열 기준)는 그대로.
    // 배치: Unity -batchmode -executeMethod PA_DemoStructureSetup.FinishCozyDisplayStandBatch
    public static void FinishCozyDisplayStand()
    {
        const string path = Root + "/StarterDisplayStand.prefab";
        var stand = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var counter = stand.transform.Find("EmptyCounter") ?? throw new InvalidOperationException("EmptyCounter missing.");
            var old = counter.Find("CozyCounter");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var cozy = new GameObject("CozyCounter").transform; cozy.SetParent(counter, false);
            var cloth = RoomMaterial("CounterClothCream", new Color(.97f, .93f, .82f));
            var hem = RoomMaterial("CounterClothHem", new Color(.3f, .58f, .54f));
            var dark = RoomMaterial("InteriorTrim", new Color(.42f, .26f, .14f));
            // 윗면 천(매트 위 2mm) + 앞·뒤로 늘어진 치마(위 .93 → 아래 .74) + 청록 단.
            RoomBox(cozy, "ClothTop", new Vector3(0, .955f, 0), new Vector3(1.2f, .006f, 1f), cloth);
            foreach (int z in new[] { -1, 1 })
            {
                RoomBox(cozy, "ClothSkirt" + z, new Vector3(0, .845f, z * .585f), new Vector3(1.2f, .21f, .01f), cloth);
                RoomBox(cozy, "ClothHem" + z, new Vector3(0, .745f, z * .59f), new Vector3(1.2f, .035f, .012f), hem);
            }
            foreach (int x in new[] { -1, 1 })
            {
                RoomBox(cozy, "ClothSide" + x, new Vector3(x * .685f, .87f, 0), new Vector3(.01f, .16f, 1.02f), cloth);
                RoomBox(cozy, "ClothSideHem" + x, new Vector3(x * .69f, .79f, 0), new Vector3(.012f, .03f, 1.02f), hem);
                foreach (int z in new[] { -1, 1 })
                    RoomBox(cozy, "LegFoot", new Vector3(x * .5f, .025f, z * .4f), new Vector3(.17f, .05f, .17f), dark);
            }
            PrefabUtility.SaveAsPrefabAsset(stand, path);
            Debug.Log("[DEMO-STRUCTURE] Cozy display stand: " + cozy.childCount + " parts");
        }
        finally { PrefabUtility.UnloadPrefabContents(stand); }
    }

    public static void FinishCozyDisplayStandBatch()
    {
        int code = 0;
        try { FinishCozyDisplayStand(); AssetDatabase.SaveAssets(); }
        catch (Exception e) { Debug.LogError("[DEMO-STRUCTURE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // 2026-11-04 ACNH 소품 연구 H1: 가게 앞(ShopArchitecture)에 창턱 꽃상자·모서리 화분·현관 매트. 표시 전용(충돌 없음, 문 통로·랜턴 자리 밖).
    // 배치: Unity -batchmode -executeMethod PA_DemoStructureSetup.FinishCozyExteriorBatch
    public static void FinishCozyExterior()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var binding = shop.GetComponent<DemoShopInterior>() ?? throw new InvalidOperationException("Interior binding missing.");
            var facade = binding.exterior.transform.Find("ShopArchitecture") ?? throw new InvalidOperationException("ShopArchitecture missing.");
            var old = facade.Find("CozyExterior");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var cozy = new GameObject("CozyExterior").transform; cozy.SetParent(facade, false);
            var box = RoomMaterial("ExteriorPlanterWood", new Color(.48f, .3f, .16f));
            var soil = RoomMaterial("ExteriorSoil", new Color(.33f, .22f, .14f));
            var leaf = RoomMaterial("InteriorLeaf", new Color(.42f, .66f, .34f));
            var pink = RoomMaterial("ExteriorFlowerPink", new Color(.98f, .62f, .72f));
            var yellow = RoomMaterial("ExteriorFlowerYellow", new Color(1f, .86f, .4f));
            var white = RoomMaterial("ExteriorFlowerWhite", new Color(.98f, .96f, .9f));
            var pot = RoomMaterial("InteriorPot", new Color(.8f, .46f, .3f));
            var mat = RoomMaterial("ExteriorDoormat", new Color(.72f, .52f, .3f));
            var matEdge = RoomMaterial("InteriorTrim", new Color(.42f, .26f, .14f));
            var flowers = new[] { pink, yellow, white };
            foreach (int side in new[] { -1, 1 })
            {
                // 창턱(WindowSill y .83, z -7.48, 폭 2.5) 위 꽃상자 + 흙 + 잎 + 꽃 7송이.
                var sill = new Vector3(side * 3, .97f, -7.56f);
                RoomBox(cozy, "PlanterBox" + side, sill, new Vector3(2.2f, .2f, .26f), box);
                RoomBox(cozy, "PlanterSoil" + side, sill + new Vector3(0, .1f, 0), new Vector3(2.1f, .02f, .2f), soil);
                for (int k = 0; k < 7; k++)
                {
                    float x = -.9f + k * .3f;
                    RoomBox(cozy, $"PlanterLeaf{side}_{k}", sill + new Vector3(x, .17f, 0), new Vector3(.22f, .12f, .18f), leaf);
                    RoomBox(cozy, $"PlanterFlower{side}_{k}", sill + new Vector3(x + .03f, .26f, -.02f), new Vector3(.12f, .1f, .12f), flowers[(k + (side > 0 ? 1 : 0)) % 3]);
                }
                // 정면 모서리 화분(창 바깥 ±4.65). cozy-r158: 문 옆 ±1.35는 NightGlowPresentation의 문 옆 작은 랜턴 자리라 겹쳤다.
                var potAt = new Vector3(side * 4.65f, 0, -7.75f);
                RoomBox(cozy, "DoorPot" + side, potAt + new Vector3(0, .2f, 0), new Vector3(.42f, .4f, .42f), pot);
                RoomBox(cozy, "DoorPotRim" + side, potAt + new Vector3(0, .41f, 0), new Vector3(.48f, .06f, .48f), pot);
                RoomBox(cozy, "DoorShrubA" + side, potAt + new Vector3(0, .62f, 0), new Vector3(.5f, .38f, .5f), leaf);
                RoomBox(cozy, "DoorShrubB" + side, potAt + new Vector3(.05f, .86f, -.03f), new Vector3(.32f, .24f, .32f), leaf);
                RoomBox(cozy, "DoorBloom" + side, potAt + new Vector3(-.12f, .8f, -.2f), new Vector3(.13f, .11f, .1f), side < 0 ? pink : yellow);
            }
            // 현관 매트(테두리 + 안쪽).
            RoomBox(cozy, "DoormatEdge", new Vector3(0, .012f, -8.25f), new Vector3(1.5f, .02f, .85f), matEdge);
            RoomBox(cozy, "Doormat", new Vector3(0, .024f, -8.25f), new Vector3(1.32f, .01f, .68f), mat);
            PrefabUtility.SaveAsPrefabAsset(shop, path);
            Debug.Log("[DEMO-STRUCTURE] Cozy exterior: " + cozy.childCount + " parts");
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    public static void FinishCozyExteriorBatch()
    {
        int code = 0;
        try { FinishCozyExterior(); AssetDatabase.SaveAssets(); }
        catch (Exception e) { Debug.LogError("[DEMO-STRUCTURE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // 2026-11-04 사용자 피드백(영업 중 손님이 문이 아닌 벽으로 드나듦): 손님 길(NavMesh)은 2m 칸으로 만들어지고 가게 앞줄 칸은
    // 통째로 열려 있어 앞벽(z -7.3) 어디로든 지나갔다. 칸 점유·배치·저장은 그대로 두고, 앞벽을 따라 문 틈(|x|<.85)만 남긴
    // 깎기 장애물(NavMeshObstacle carve, OutdoorPlacementController.EnsureCarvingObstacle와 같은 방식)을 둔다. 플레이어 이동·문 순간이동은 영향 없음.
    // 배치: Unity -batchmode -executeMethod PA_DemoStructureSetup.FinishShopFrontNavBlockBatch
    public static void FinishShopFrontNavBlock()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var old = shop.transform.Find("FrontWallNavBlock");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("FrontWallNavBlock").transform; root.SetParent(shop.transform, false);
            foreach (int side in new[] { -1, 1 })
            {
                var go = new GameObject(side < 0 ? "LeftFront" : "RightFront");
                go.transform.SetParent(root, false);
                // 옆벽 칸 안쪽(-5.4)까지 겹쳐 틈이 없게, 문 쪽은 .85에서 끊는다.
                go.transform.localPosition = new Vector3(side * 3.125f, 1f, -7.3f);
                var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(4.55f, 2f, .35f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
            }
            PrefabUtility.SaveAsPrefabAsset(shop, path);
            Debug.Log("[DEMO-STRUCTURE] Front wall nav block: 2 carving obstacles, door gap ±.85");
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    public static void FinishShopFrontNavBlockBatch()
    {
        int code = 0;
        try { FinishShopFrontNavBlock(); AssetDatabase.SaveAssets(); }
        catch (Exception e) { Debug.LogError("[DEMO-STRUCTURE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void FinishNightLighting()
    {
        const string path = Root + "/PioneerShopBase.prefab";
        var shop = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var room = shop.GetComponent<DemoShopInterior>().interior.transform;
            var light = room.Find("WarmInteriorLight").GetComponent<Light>();
            light.intensity = 6; light.range = 14; light.color = new Color(1, .85f, .67f);
            light.transform.localPosition = new Vector3(0, 3.5f, -3);
            var lamp = room.Find("LivingLampLight");
            var glow = lamp != null ? lamp.GetComponent<Light>() : new GameObject("LivingLampLight").AddComponent<Light>();
            glow.transform.SetParent(room, false); glow.transform.localPosition = new Vector3(-2, 1.45f, 1.45f);
            glow.type = LightType.Point; glow.color = new Color(1, .73f, .42f); glow.intensity = 2; glow.range = 4;
            PrefabUtility.SaveAsPrefabAsset(shop, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(shop); }
    }

    static void FinishDoorBoundary(DemoShopInterior binding)
    {
        if (binding == null) throw new InvalidOperationException("Authored interior missing.");
        if (!binding.GetComponents<BoxCollider>().Any(c => Mathf.Abs(c.center.z + 7.4f) < .01f))
            AddBox(binding.gameObject, new Vector3(0, 1.5f, -7.4f), new Vector3(10.1f, 3, .25f));
        binding.exit.transform.localPosition = new Vector3(0, 0, -7.4f);
        var wood = RoomMaterial("InteriorWood", new Color(.56f, .37f, .20f));
        if (binding.entrance.transform.Find("DoorPanel") == null)
            RoomBox(binding.entrance.transform, "DoorPanel", new Vector3(0, 1, -.2f), new Vector3(1.45f, 2, .15f), wood);
        var chair = binding.interior.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.transform.IsChildOf(binding.interior.transform) && r.name.Contains("Froggy"));
        foreach (var renderer in chair)
            renderer.sharedMaterials = Enumerable.Repeat(RoomMaterial("InteriorChair", new Color(.36f, .5f, .32f)), renderer.sharedMaterials.Length).ToArray();
    }

    static void BuildInterior(GameObject shop)
    {
        var cottage = shop.GetComponentsInChildren<MeshRenderer>().Single().gameObject;
        var shell = new GameObject("ExteriorShell"); shell.transform.SetParent(shop.transform, false);
        cottage.transform.SetParent(shell.transform, true);
        var bounds = cottage.GetComponent<Renderer>().bounds;
        // 기존 authored 축 변환은 자식에 유지하고 부모를 월드 축 기준으로 footprint에 맞춘다.
        shell.transform.localScale = new Vector3(12f / bounds.size.x, 5.5f / bounds.size.y, 11f / bounds.size.z);
        bounds = cottage.GetComponent<Renderer>().bounds;
        shell.transform.position += shop.transform.position + new Vector3(0, 0, -1.9f)
            - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        var room = new GameObject("InteriorCutaway"); room.transform.SetParent(shop.transform, false);
        var wood = RoomMaterial("InteriorWood", new Color(.56f, .37f, .20f));
        var cream = RoomMaterial("InteriorPlaster", new Color(.88f, .80f, .63f));
        var rug = RoomMaterial("InteriorRug", new Color(.28f, .48f, .40f));
        RoomBox(room.transform, "WoodFloor", new Vector3(0, .015f, -2), new Vector3(10.1f, .03f, 10), wood);
        RoomBox(room.transform, "BackWall", new Vector3(0, 1.35f, 2.65f), new Vector3(10.1f, 2.7f, .2f), cream);
        RoomBox(room.transform, "LeftWall", new Vector3(-5, .6f, -2), new Vector3(.2f, 1.2f, 10), cream);
        RoomBox(room.transform, "RightWall", new Vector3(5, .6f, -2), new Vector3(.2f, 1.2f, 10), cream);
        RoomBox(room.transform, "LivingRug", new Vector3(-3, .04f, .3f), new Vector3(3, .02f, 3), rug);
        DressInteriorCozy(room.transform);
        var art = FirstDayStudioAssets.Load();
        FirstDayStudioAssets.Place(art.chestClosed, room.transform, shop.transform.TransformPoint(new Vector3(-4, .04f, 1.7f)), .75f);
        FirstDayStudioAssets.Place(PA_OwnedChairSetup.Model(), room.transform,
            shop.transform.TransformPoint(new Vector3(-3, .04f, .8f)), 1.1f, 180).name = "CozyChair";
        var light = new GameObject("WarmInteriorLight").AddComponent<Light>();
        light.transform.SetParent(room.transform, false); light.transform.localPosition = new Vector3(0, 3, -2);
        light.type = LightType.Point; light.color = new Color(1, .81f, .58f); light.range = 12; light.intensity = 2;

        var binding = shop.AddComponent<DemoShopInterior>();
        binding.exterior = shell; binding.interior = room;
        binding.entrance = Door(shop.transform, "ShopEntrance", new Vector3(0, 0, -7.4f), Vector3.back);
        binding.exit = Door(shop.transform, "ShopExit", new Vector3(0, 0, -6.8f), Vector3.forward);
        // 출구의 문짝은 cutaway에서 실제로 보이며 trigger만 상호작용에 사용한다.
        RoomBox(binding.exit.transform, "DoorPanel", new Vector3(0, .55f, -.2f), new Vector3(1.5f, 1.1f, .15f), wood);
        binding.insideSpawn = Spawn(shop.transform, "InsideSpawn", new Vector3(0, .08f, -5.7f), Vector3.forward);
        binding.outsideSpawn = Spawn(shop.transform, "OutsideSpawn", new Vector3(0, .08f, -8.5f), Vector3.back);
        FinishDoorBoundary(binding);
        room.SetActive(false); binding.exit.gameObject.SetActive(false);
    }

    // 2026-11-04 ACNH 소품 연구(너굴 리스트) H1: 빈 상자 방 → 아기자기한 방. 표시 전용 부품만 더한다(충돌·배치 구역·이름 불변).
    // 바닥 널판 줄무늬, 벽 아래 나무 판벽·걸레받이, 뒷벽 창문(십자 창살·창턱 화분)·작은 액자, 러그 테두리·안쪽 무늬.
    static void DressInteriorCozy(Transform room)
    {
        var plankLight = RoomMaterial("InteriorPlankLight", new Color(.64f, .44f, .25f));
        var plankDark = RoomMaterial("InteriorPlankDark", new Color(.5f, .32f, .17f));
        var trim = RoomMaterial("InteriorTrim", new Color(.42f, .26f, .14f));
        var wainscot = RoomMaterial("InteriorWainscot", new Color(.74f, .55f, .34f));
        var glass = RoomMaterial("InteriorWindowSky", new Color(.62f, .84f, .92f));
        var frameCream = RoomMaterial("InteriorFrameCream", new Color(.96f, .92f, .82f));
        var rugBorder = RoomMaterial("InteriorRugBorder", new Color(.88f, .78f, .52f));
        var rugInner = RoomMaterial("InteriorRugInner", new Color(.36f, .58f, .5f));
        var leaf = RoomMaterial("InteriorLeaf", new Color(.42f, .66f, .34f));
        var pot = RoomMaterial("InteriorPot", new Color(.8f, .46f, .3f));
        var picture = RoomMaterial("InteriorPictureSea", new Color(.34f, .62f, .74f));
        var pictureSand = RoomMaterial("InteriorPictureSand", new Color(.95f, .82f, .55f));
        var cozy = new GameObject("CozyDressing").transform; cozy.SetParent(room, false);
        // 바닥 널판: 0.5m 폭 줄을 세 톤으로(cozy-r149: 1m 두 톤은 굵은 줄무늬로 읽혔다). 바닥 위 3mm.
        var plankMid = RoomMaterial("InteriorPlankMid", new Color(.57f, .38f, .2f));
        for (int i = 0; i < 20; i++)
            RoomBox(cozy, "FloorPlank" + i, new Vector3(-4.75f + i * .5f, .033f, -2), new Vector3(.47f, .006f, 9.9f), i % 3 == 0 ? plankLight : i % 3 == 1 ? plankMid : plankDark);
        // 뒷벽: 판벽(높이 .9) + 위 손잡이 띠 + 걸레받이. 옆벽은 걸레받이만(잘린 낮은 벽).
        RoomBox(cozy, "BackWainscot", new Vector3(0, .45f, 2.53f), new Vector3(10f, .9f, .04f), wainscot);
        RoomBox(cozy, "BackRail", new Vector3(0, .92f, 2.51f), new Vector3(10f, .06f, .07f), trim);
        RoomBox(cozy, "BackBaseboard", new Vector3(0, .07f, 2.51f), new Vector3(10f, .14f, .07f), trim);
        for (int k = 0; k < 9; k++) RoomBox(cozy, "BackPanelSeam" + k, new Vector3(-4f + k, .45f, 2.505f), new Vector3(.03f, .8f, .02f), trim);
        RoomBox(cozy, "LeftBaseboard", new Vector3(-4.87f, .07f, -2), new Vector3(.06f, .14f, 9.9f), trim);
        RoomBox(cozy, "RightBaseboard", new Vector3(4.87f, .07f, -2), new Vector3(.06f, .14f, 9.9f), trim);
        // 뒷벽 창문: 크림 창틀 + 하늘빛 유리 + 십자 창살 + 창턱과 작은 화분.
        var window = new Vector3(.8f, 1.85f, 2.52f);
        RoomBox(cozy, "WindowFrame", window, new Vector3(1.5f, 1.1f, .05f), frameCream);
        RoomBox(cozy, "WindowGlass", window + new Vector3(0, 0, -.03f), new Vector3(1.3f, .9f, .02f), glass);
        RoomBox(cozy, "WindowBarV", window + new Vector3(0, 0, -.05f), new Vector3(.06f, .9f, .02f), frameCream);
        RoomBox(cozy, "WindowBarH", window + new Vector3(0, 0, -.05f), new Vector3(1.3f, .06f, .02f), frameCream);
        RoomBox(cozy, "WindowSill", window + new Vector3(0, -.6f, -.08f), new Vector3(1.7f, .07f, .2f), trim);
        RoomBox(cozy, "SillPot", window + new Vector3(-.45f, -.5f, -.1f), new Vector3(.16f, .14f, .16f), pot);
        RoomBox(cozy, "SillLeafA", window + new Vector3(-.45f, -.36f, -.1f), new Vector3(.22f, .14f, .2f), leaf);
        RoomBox(cozy, "SillLeafB", window + new Vector3(-.41f, -.26f, -.12f), new Vector3(.12f, .1f, .12f), leaf);
        // 작은 액자(바다와 모래 두 칸 그림).
        var frame = new Vector3(2.7f, 1.95f, 2.52f);
        RoomBox(cozy, "PictureFrame", frame, new Vector3(.8f, .6f, .05f), trim);
        RoomBox(cozy, "PictureSea", frame + new Vector3(0, .08f, -.03f), new Vector3(.66f, .3f, .02f), picture);
        RoomBox(cozy, "PictureSand", frame + new Vector3(0, -.15f, -.03f), new Vector3(.66f, .16f, .02f), pictureSand);
        // 러그: 밝은 테두리 + 안쪽 진한 판 + 줄무늬 두 줄(정사각 러그를 '깐' 느낌으로).
        var rugCenter = new Vector3(-3, .052f, .3f);
        RoomBox(cozy, "RugBorder", rugCenter, new Vector3(3.1f, .004f, 3.1f), rugBorder);
        RoomBox(cozy, "RugInner", rugCenter + new Vector3(0, .004f, 0), new Vector3(2.6f, .004f, 2.6f), rugInner);
        RoomBox(cozy, "RugStripeA", rugCenter + new Vector3(0, .008f, .7f), new Vector3(2.3f, .004f, .12f), rugBorder);
        RoomBox(cozy, "RugStripeB", rugCenter + new Vector3(0, .008f, -.7f), new Vector3(2.3f, .004f, .12f), rugBorder);
        // 통로 러너(ShopRunner 1.65×5.5, 윗면 .055): 크림 테두리 + 안쪽 청록 + 가운데 작은 마름모 줄.
        var runnerCenter = new Vector3(0, .059f, -3.8f);
        var runnerInner = RoomMaterial("InteriorRunnerInner", new Color(.24f, .5f, .46f));
        RoomBox(cozy, "RunnerBorder", runnerCenter, new Vector3(1.75f, .004f, 5.6f), rugBorder);
        RoomBox(cozy, "RunnerInner", runnerCenter + new Vector3(0, .004f, 0), new Vector3(1.45f, .004f, 5.3f), runnerInner);
        for (int k = 0; k < 5; k++)
        {
            RoomBox(cozy, "RunnerDiamond" + k, runnerCenter + new Vector3(0, .008f, -2f + k), new Vector3(.24f, .004f, .24f), rugBorder);
            cozy.Find("RunnerDiamond" + k).localRotation = Quaternion.Euler(0, 45, 0);
        }
        // 침대(LivingFurnishings: 침대 -4,.,.15 / 이불 윗면 .625 / 머리판 1.3): 이불 접힌 띠·조각보 무늬·작은 쿠션·머리판 갓·다리.
        var quiltLight = RoomMaterial("InteriorQuiltLight", new Color(.42f, .66f, .6f));
        var quiltWarm = RoomMaterial("InteriorQuiltWarm", new Color(.95f, .72f, .52f));
        RoomBox(cozy, "BlanketFold", new Vector3(-4, .632f, .36f), new Vector3(1.37f, .03f, .22f), frameCream);
        RoomBox(cozy, "QuiltPatchA", new Vector3(-4.3f, .628f, -.05f), new Vector3(.38f, .01f, .38f), quiltLight);
        RoomBox(cozy, "QuiltPatchB", new Vector3(-3.7f, .628f, -.45f), new Vector3(.38f, .01f, .38f), quiltWarm);
        RoomBox(cozy, "QuiltPatchC", new Vector3(-3.7f, .628f, -.05f), new Vector3(.38f, .01f, .38f), quiltLight);
        RoomBox(cozy, "QuiltPatchD", new Vector3(-4.3f, .628f, -.45f), new Vector3(.38f, .01f, .38f), quiltWarm);
        RoomBox(cozy, "Cushion", new Vector3(-3.72f, .7f, .78f), new Vector3(.32f, .2f, .14f), quiltWarm);
        cozy.Find("Cushion").localRotation = Quaternion.Euler(-18, 12, 0);
        RoomBox(cozy, "HeadboardCap", new Vector3(-4, 1.18f, 1.3f), new Vector3(1.62f, .07f, .18f), plankLight);
        foreach (int x in new[] { -1, 1 })
        {
            RoomBox(cozy, "HeadboardPost" + x, new Vector3(-4 + x * .77f, .62f, 1.3f), new Vector3(.1f, 1.2f, .16f), plankLight);
            RoomBox(cozy, "BedFoot" + x, new Vector3(-4 + x * .64f, .03f, -.92f), new Vector3(.12f, .06f, .12f), trim);
        }
        // 책상(윗면 .75, -2,.,1.45)과 램프: 책 더미·머그·램프 받침과 갓 띠.
        var bookRed = RoomMaterial("InteriorBookRed", new Color(.78f, .36f, .3f));
        var bookBlue = RoomMaterial("InteriorBookBlue", new Color(.32f, .46f, .7f));
        RoomBox(cozy, "BookA", new Vector3(-2.38f, .78f, 1.5f), new Vector3(.3f, .05f, .22f), bookRed);
        RoomBox(cozy, "BookB", new Vector3(-2.36f, .83f, 1.5f), new Vector3(.27f, .05f, .2f), bookBlue);
        RoomBox(cozy, "BookC", new Vector3(-2.39f, .875f, 1.49f), new Vector3(.24f, .04f, .19f), frameCream);
        cozy.Find("BookB").localRotation = Quaternion.Euler(0, 12, 0);
        RoomBox(cozy, "Mug", new Vector3(-1.62f, .81f, 1.3f), new Vector3(.11f, .12f, .11f), frameCream);
        RoomBox(cozy, "MugHandle", new Vector3(-1.555f, .81f, 1.3f), new Vector3(.03f, .07f, .05f), frameCream);
        RoomBox(cozy, "LampBase", new Vector3(-2, .765f, 1.45f), new Vector3(.22f, .03f, .22f), trim);
        RoomBox(cozy, "LampShadeBand", new Vector3(-2, 1.05f, 1.45f), new Vector3(.47f, .04f, .47f), quiltWarm);
        // 칸막이(LivingDivider 끝 -.65,.36,-1.65) 위 작은 화분.
        RoomBox(cozy, "DividerPot", new Vector3(-.85f, .45f, -1.65f), new Vector3(.2f, .18f, .2f), pot);
        RoomBox(cozy, "DividerLeafA", new Vector3(-.85f, .62f, -1.65f), new Vector3(.3f, .18f, .26f), leaf);
        RoomBox(cozy, "DividerLeafB", new Vector3(-.82f, .75f, -1.62f), new Vector3(.16f, .14f, .16f), leaf);
    }

    static Material RoomMaterial(string name, Color color)
    {
        string path = Root + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path); return material;
    }

    static void RoomBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name;
        box.transform.SetParent(parent, false); box.transform.localPosition = position; box.transform.localScale = scale;
        // 지면·주변 충돌은 원래 월드/배치 collider가 소유한다.
        box.GetComponent<Collider>().enabled = false;
        box.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Transform Spawn(Transform parent, string name, Vector3 position, Vector3 forward)
    {
        var point = new GameObject(name).transform; point.SetParent(parent, false);
        point.localPosition = position; point.localRotation = Quaternion.LookRotation(forward); return point;
    }

    static BuildingEntrance Door(Transform parent, string name, Vector3 position, Vector3 outward)
    {
        var door = Spawn(parent, name, position, Vector3.forward);
        var trigger = door.gameObject.AddComponent<BoxCollider>(); trigger.isTrigger = true;
        trigger.center = Vector3.up; trigger.size = new Vector3(1.5f, 2, .35f);
        Spawn(door, "InteractionAnchor", Vector3.zero, outward);
        return door.gameObject.AddComponent<BuildingEntrance>();
    }

    static GameObject RootObject(string name) { return new GameObject(name); }
    static void AddBox(GameObject root, Vector3 center, Vector3 size)
    { var box = root.AddComponent<BoxCollider>(); box.center = center; box.size = size; }
    static void Zone(GameObject parent, string name, WorldPlaceableSurface surface, Vector3 center, Vector3 size, int priority)
    {
        var zone = new GameObject(name).AddComponent<WorldPlaceableZone>(); zone.transform.SetParent(parent.transform, false);
        zone.localBounds = new Bounds(center, size); zone.surface = surface; zone.priority = priority;
    }
    static GameObject SaveBuilding(GameObject root, string name)
    {
        // InstantiateInactive in the existing placement service controls runtime activation.
        root.SetActive(true);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        var data = Asset<BuildingData>(name); data.prefab = prefab; data.buildingName = name; data.price = 0;
        EditorUtility.SetDirty(data); return prefab;
    }
    static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Root + "/" + name + ".asset";
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
        return value;
    }
}
