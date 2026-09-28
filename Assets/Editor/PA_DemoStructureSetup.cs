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
        var chair = Resources.Load<GameObject>("PA_DemoProps/Prop_FroggyChair");
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
            new DemoPlaceableEntry { item=furnitureItem, key="furniture-chair", kind=DemoPlaceableKind.Furniture, buildingResource="DemoStructure/SimpleFurniture", surfaces=WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility }
        };
        var improved = Asset<Item>("Item_ImprovedPickaxe");
        improved.id = 2022; improved.itemName = "Improved Pickaxe"; improved.toolType = ToolType.Pickaxe;
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
            root=DemoSpecialization.Mining, specialty=NpcSpecialty.Miner, companionPrefix="Miner",
            production=Resources.Load<ProductionData>("Production/Production_Ore"), npcEfficiency=1.5f, playerWorkStrength=2 } };
        EditorUtility.SetDirty(improved); EditorUtility.SetDirty(recipe);
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
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
            var chair = room.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.Contains("Froggy"));
            if (chair != null) chair.transform.position += shop.transform.TransformVector(new Vector3(1, 0, -.3f));
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
        var art = FirstDayStudioAssets.Load();
        FirstDayStudioAssets.Place(art.chestClosed, room.transform, shop.transform.TransformPoint(new Vector3(-4, .04f, 1.7f)), .75f);
        FirstDayStudioAssets.Place(Resources.Load<GameObject>("PA_DemoProps/Prop_FroggyChair"), room.transform,
            shop.transform.TransformPoint(new Vector3(-3, .04f, .8f)), 1.1f, 180);
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
