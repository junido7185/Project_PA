#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class PA_PlayableDayBuilder
{
    const string RootName = "[FirstDayPrototype]";
    const string DemoShopName = "Prototype_Shop_For_Demo";
    const string SpawnName = "FirstDay_PlayerSpawn";
    const string PlayerModelName = "PlayerModel_C01";

    [MenuItem("P.A. System/Week 12/Build Playable Day Demo", priority = 1200)]
    public static void BuildMenu() => BuildPlayableDayDemo(true, false);

    public static void BuildSafeFromHub() => BuildPlayableDayDemo(false, false);

    [MenuItem("P.A. System/Week 12/Validate Playable Day Demo", priority = 1201)]
    public static void ValidateMenu() => ValidateAndReport();

    public static void BuildPlayableDayDemo(bool showDialog, bool rebuildBaseScene)
    {
        if (showDialog && !EditorUtility.DisplayDialog("Playable Day Demo", "첫날 시연 씬 구성을 안전하게 정리합니다.", "실행", "취소")) return;

        try
        {
            int changed = 0;
            if (rebuildBaseScene)
            {
                InvokeOptional("PA_DataBootstrapper", "BootstrapAll");
                PA_DataCreator.CreateAll();
                PA_SceneAutoBuilder.BuildFullScene();
                changed += PA_ContentAutoIntegrator.BuildAll(false);
                PA_MapLayoutBuilder.Build(false);
                PA_UIBuilder.BuildUISystem();
                PA_TMPFontFixer.Apply();
            }
            else
            {
                changed += PA_ContentAutoIntegrator.BuildAll(false);
            }

            changed += PA_SafeSceneRepair.RepairCurrentScene(false, false);
            changed += EnsureScenarioController();
            changed += EnsurePlayablePlayer();
            changed += EnsureDemoCamera();
            changed += EnsurePrototypeShop();
            changed += HardenShopSlotsSafe();
            TryBakeNavMesh();
            changed += EnsurePrototypeProps();
            changed += EnsureStarterInventory();
            changed += EnsureFirstSettler();
            changed += HardenNpcs();
            changed += EnsureRuntimeUiHooks();
            changed += EnsurePrototypeGuides();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayableDay] Build complete. Changed={changed}");
            ValidateAndReport();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayableDay] Build failed: {e}");
            if (showDialog) EditorUtility.DisplayDialog("Playable Day 실패", e.Message, "확인");
        }
    }

    static int EnsureScenarioController()
    {
        if (UnityEngine.Object.FindFirstObjectByType<PlayableDayScenarioController>() != null) return 0;
        var go = new GameObject("PlayableDayGuide");
        go.AddComponent<PlayableDayScenarioController>();
        EditorUtility.SetDirty(go);
        return 1;
    }

    static int EnsurePlayablePlayer()
    {
        int changed = 0;
        EnsureTag("Player");
        EnsureTag("MainCamera");
        GameObject player = ResolvePlayer();
        if (player == null) { player = new GameObject("Player"); changed++; }
        if (player.name != "Player") { player.name = "Player"; changed++; }
        if (SetTagSafe(player, "Player")) changed++;

        Vector3 spawn = ResolvePlayerSpawnPosition();
        if (Vector3.Distance(player.transform.position, spawn) > 0.05f) { player.transform.position = spawn; changed++; }
        if (Quaternion.Angle(player.transform.rotation, Quaternion.identity) > 0.5f) { player.transform.rotation = Quaternion.identity; changed++; }
        if (player.transform.localScale != Vector3.one) { player.transform.localScale = Vector3.one; changed++; }

        var cc = EnsureComponent<CharacterController>(player, ref changed);
        cc.height = 2f; cc.radius = 0.45f; cc.center = new Vector3(0f, 1f, 0f); EditorUtility.SetDirty(cc);
        EnsureComponent<PlayerController>(player, ref changed);
        var interaction = EnsureComponent<PlayerInteraction>(player, ref changed);
        interaction.interactDistance = 3.2f; interaction.interactRadius = 0.8f; interaction.interactOriginHeight = 1f; interaction.fallbackSearchRadius = 2.6f;
        EditorUtility.SetDirty(interaction);

        var hotbar = EnsureComponent<Hotbar>(player, ref changed);
        hotbar.size = 9; EnsureSlots(hotbar.size, ref hotbar.slots); EditorUtility.SetDirty(hotbar);
        var inv = EnsureComponent<Inventory>(player, ref changed);
        inv.size = 24; EnsureSlots(inv.size, ref inv.slots); inv.hotbar = hotbar; inv.selectedHotbarIndex = Mathf.Clamp(inv.selectedHotbarIndex, 0, hotbar.size - 1); EditorUtility.SetDirty(inv);
        EnsureComponent<EquipmentSystem>(player, ref changed);
        changed += EnsurePlayerVisual(player);
        changed += MuteDuplicateGeneratedPlayers(player);
        EditorUtility.SetDirty(player);
        return changed;
    }

    static int EnsureDemoCamera()
    {
        int changed = 0;
        GameObject player = ResolvePlayer();
        if (player == null) return 0;
        Camera cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) { cam = new GameObject("Main Camera").AddComponent<Camera>(); changed++; }
        if (SetTagSafe(cam.gameObject, "MainCamera")) changed++;
        Vector3 pos = player.transform.position + new Vector3(0f, 10.5f, -8.5f);
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation((player.transform.position + Vector3.up * 1.1f) - pos, Vector3.up);
        cam.fieldOfView = 42f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 250f;
        var follow = EnsureComponent<CameraController>(cam.gameObject, ref changed);
        follow.target = player.transform; follow.smoothSpeed = 7.5f;
        EditorUtility.SetDirty(cam); EditorUtility.SetDirty(follow);
        return changed;
    }

    static int EnsurePrototypeShop()
    {
        int changed = 0;
        EnsureTag("Shop");
        GameObject root = EnsurePrototypeRoot(ref changed);
        Transform found = root.transform.Find(DemoShopName);
        GameObject shopGo = found != null ? found.gameObject : new GameObject(DemoShopName);
        if (found == null) { shopGo.transform.SetParent(root.transform, false); changed++; }

        GameObject player = ResolvePlayer();
        Vector3 center = player != null ? player.transform.position + player.transform.forward * 3.2f + player.transform.right : new Vector3(1f, 0f, -2.6f);
        center.y = 0f;
        shopGo.transform.position = center;
        shopGo.transform.rotation = player != null ? Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f) : Quaternion.identity;
        if (SetTagSafe(shopGo, "Shop")) changed++;
        var shop = EnsureComponent<Shop>(shopGo, ref changed);
        changed += UpsertLocalBox(shopGo.transform, "Counter", Vector3.zero, new Vector3(3.8f, 0.35f, 1f), new Color(0.62f, 0.48f, 0.30f));

        var slots = new List<ShopSlot>();
        for (int i = 0; i < 4; i++)
        {
            string name = $"Demo_ShopSlot_{i + 1:00}";
            Vector3 local = new Vector3(-1.35f + i * 0.9f, 0.34f, -0.05f);
            changed += UpsertLocalBox(shopGo.transform, name, local, new Vector3(0.72f, 0.18f, 0.72f), new Color(0.95f, 0.86f, 0.62f));
            Transform tr = shopGo.transform.Find(name);
            if (tr == null) continue;
            var slot = EnsureComponent<ShopSlot>(tr.gameObject, ref changed);
            slot.displayOffset = new Vector3(0f, 0.5f, 0f); slot.fallbackDisplayScale = 0.36f; slot.RefreshDisplay();
            slots.Add(slot);
            var col = tr.GetComponent<BoxCollider>() ?? tr.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 0.8f, 1.2f); col.center = new Vector3(0f, 0.35f, 0f); EditorUtility.SetDirty(col);
        }
        shop.managedSlots = slots;
        EditorUtility.SetDirty(shop); EditorUtility.SetDirty(shopGo);
        return changed;
    }

    static int HardenShopSlotsSafe()
    {
        int changed = 0;
        foreach (var slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot == null) continue;
            var col = slot.GetComponent<BoxCollider>() ?? slot.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 0.8f, 1.2f); col.center = new Vector3(0f, 0.35f, 0f);
            EditorUtility.SetDirty(col); EditorUtility.SetDirty(slot.gameObject);
        }
        return changed;
    }

    static int HardenNpcs()
    {
        int changed = 0;
        Transform shop = ResolveDemoShopTransform();
        int visualIndex = 0;
        foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            if (shop != null) npc.shopLocation = shop;
            var col = npc.GetComponent<Collider>();
            if (col == null)
            {
                var cap = npc.gameObject.AddComponent<CapsuleCollider>();
                cap.height = 1.8f; cap.radius = 0.45f; cap.center = new Vector3(0f, 0.9f, 0f); changed++;
            }
            else if (col is CapsuleCollider cap)
            {
                cap.height = 1.8f; cap.radius = 0.45f; cap.center = new Vector3(0f, 0.9f, 0f); EditorUtility.SetDirty(cap);
            }
            var agent = npc.GetComponent<NavMeshAgent>() ?? npc.gameObject.AddComponent<NavMeshAgent>();
            agent.height = 1.8f; agent.radius = 0.4f; agent.speed = 2.5f; agent.angularSpeed = 360f; agent.acceleration = 8f; EditorUtility.SetDirty(agent);
            if (NavMesh.SamplePosition(npc.transform.position, out var hit, 8f, NavMesh.AllAreas) && Vector3.Distance(npc.transform.position, hit.position) > 0.1f)
            {
                npc.transform.position = hit.position; EditorUtility.SetDirty(npc.transform); changed++;
            }
            changed += EnsureNpcVisual(npc.gameObject, ResolveNpcModelPath(npc, visualIndex), $"NpcModel_{visualIndex + 2:00}");
            visualIndex++;
            EditorUtility.SetDirty(npc);
        }
        return changed;
    }

    static void TryBakeNavMesh()
    {
        try
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null) return;
            Type t = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation") ?? Type.GetType("UnityEngine.AI.NavMeshSurface");
            if (t == null) return;
            Component surface = ground.GetComponent(t) ?? ground.AddComponent(t);
            t.GetMethod("BuildNavMesh")?.Invoke(surface, null);
            EditorUtility.SetDirty(ground);
        }
        catch (Exception e) { Debug.LogWarning($"[PlayableDay] NavMesh bake failed: {e.Message}"); }
    }

    static int EnsurePrototypeProps()
    {
        int changed = 0;
        GameObject root = EnsurePrototypeRoot(ref changed);
        changed += UpsertBox(root.transform, "Support_Crate", new Vector3(-3.5f, 0.35f, -4.5f), new Vector3(1.4f, 0.7f, 1f), new Color(0.56f, 0.42f, 0.25f));
        changed += UpsertBox(root.transform, "Shop_Tent_Kit", new Vector3(-1.4f, 0.55f, -4.8f), new Vector3(1.6f, 1.1f, 1.2f), new Color(0.94f, 0.50f, 0.44f));
        changed += UpsertBox(root.transform, "Sales_Tent_Preview", new Vector3(2.8f, 0.5f, -1.4f), new Vector3(2.2f, 1f, 1.4f), new Color(0.68f, 0.78f, 0.62f));
        changed += UpsertBox(root.transform, "Arrival_Pier_Marker", new Vector3(0f, 0.08f, -8.5f), new Vector3(5f, 0.16f, 1.2f), new Color(0.56f, 0.39f, 0.24f));
        return changed;
    }

    static int EnsureStarterInventory()
    {
        int changed = 0;
        var econ = UnityEngine.Object.FindFirstObjectByType<EconomyService>();
        if (econ != null) { econ.ForceSet(500, "FirstDayPrototype"); econ.ForceSetCumulativeRevenue(0, "FirstDayPrototype"); EditorUtility.SetDirty(econ); changed++; }
        GameObject player = ResolvePlayer();
        var inv = player != null ? player.GetComponent<Inventory>() : UnityEngine.Object.FindFirstObjectByType<Inventory>();
        var hotbar = player != null ? player.GetComponent<Hotbar>() : UnityEngine.Object.FindFirstObjectByType<Hotbar>();
        if (inv == null || hotbar == null) return changed;
        EnsureSlots(inv.size, ref inv.slots); EnsureSlots(hotbar.size, ref hotbar.slots);
        foreach (var s in inv.slots) s.Clear(); foreach (var s in hotbar.slots) s.Clear();
        inv.hotbar = hotbar; inv.selectedHotbarIndex = 0;
        SetSlot(hotbar.slots, 0, "Items/Item_BreadLoaf", 3); SetSlot(hotbar.slots, 1, "Items/Item_Carrot", 5); SetSlot(hotbar.slots, 2, "Items/Item_Plank", 6);
        SetSlot(hotbar.slots, 3, "Items/Item_Wood", 10); SetSlot(hotbar.slots, 4, "Items/Item_Ore", 8); SetSlot(hotbar.slots, 5, "Items/Blueprints/Blueprint_B01_MarketStall", 1);
        foreach (var slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None)) { if (slot == null) continue; slot.currentItem = null; slot.displayPrice = 0; slot.RefreshDisplay(); EditorUtility.SetDirty(slot); }
        EditorUtility.SetDirty(inv); EditorUtility.SetDirty(hotbar);
        return changed + 1;
    }

    static int EnsureFirstSettler()
    {
        int changed = 0;
        var npc = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).FirstOrDefault();
        if (npc == null)
        {
            var go = new GameObject("NPC_Bori");
            go.transform.position = new Vector3(2.8f, 0f, -2.8f);
            npc = go.AddComponent<NpcController>();
            changed++;
        }
        npc.gameObject.name = "NPC_Bori_FirstSettler";
        npc.idleTickInterval = 1f; npc.maxSlotsPerVisit = 4; npc.browseDurationAtSlot = 0.75f; npc.slotArriveDistance = 1.5f; npc.wanderRadius = 3.5f;
        Vector3 target = new Vector3(2.8f, 0f, -2.8f);
        if (NavMesh.SamplePosition(target, out var hit, 8f, NavMesh.AllAreas)) target = hit.position;
        npc.transform.position = target;
        var dialogue = EnsureComponent<NpcDialogue>(npc.gameObject, ref changed);
        dialogue.friendshipId = "bori"; dialogue.interactPrompt = "대화하기"; EditorUtility.SetDirty(dialogue);
        Transform shop = ResolveDemoShopTransform(); if (shop != null) npc.shopLocation = shop;
        changed += EnsureNpcVisual(npc.gameObject, "Assets/Art/Character/C-02.fbx", "NpcModel_C02");
        EditorUtility.SetDirty(npc); EditorUtility.SetDirty(npc.transform);
        return changed;
    }

    static int EnsureRuntimeUiHooks()
    {
        int changed = 0;
        GameObject ui = GameObject.Find("PA_RuntimeUI");
        if (ui == null) { ui = new GameObject("PA_RuntimeUI"); changed++; }
        EnsureSceneComponent<InteractPromptUI>(ui, ref changed); EnsureSceneComponent<DialogueUI>(ui, ref changed); EnsureSceneComponent<ShopPriceUI>(ui, ref changed);
        EnsureSceneComponent<MoneyHUD>(ui, ref changed); EnsureSceneComponent<ClockHUD>(ui, ref changed); EnsureSceneComponent<CraftingUI>(ui, ref changed); EnsureSceneComponent<PauseManager>(ui, ref changed);
        EditorUtility.SetDirty(ui);
        return changed;
    }

    static int EnsurePrototypeGuides()
    {
        int changed = 0;
        GameObject root = EnsurePrototypeRoot(ref changed);
        Transform old = root.transform.Find("[PrototypeGuides]");
        if (old != null) { UnityEngine.Object.DestroyImmediate(old.gameObject); changed++; }
        var guides = new GameObject("[PrototypeGuides]"); guides.transform.SetParent(root.transform, false); changed++;
        GameObject player = ResolvePlayer();
        if (player != null) { AddGuideLabel(player.transform, "Guide_Player", "0. 플레이어\nWASD 이동 / Space 상호작용", new Vector3(0f, 2.55f, 0f), new Color(0.82f, 0.92f, 1f), 1.35f); changed++; }
        var npc = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).FirstOrDefault();
        if (npc != null) { AddGuideLabel(npc.transform, "Guide_FirstSettler", "1. 첫 이주자\n[Space] 대화", new Vector3(0f, 2.55f, 0f), new Color(0.68f, 1f, 0.78f), 1.8f); changed++; }
        var slots = ResolveDemoShopTransform()?.GetComponentsInChildren<ShopSlot>(true).OrderBy(s => s.transform.localPosition.x).Take(4).ToList() ?? new List<ShopSlot>();
        for (int i = 0; i < slots.Count; i++) { AddGuideLabel(slots[i].transform, $"Guide_ShopSlot_{i + 1}", $"2. 판매대 슬롯 {i + 1}\n[Space] 진열/가격", new Vector3(0f, 1.25f, 0f), new Color(1f, 0.92f, 0.54f), 1.35f); changed++; }
        Transform crate = root.transform.Find("Support_Crate");
        if (crate != null) { AddGuideLabel(crate, "Guide_Supplies", "보급품\n핫바 1~5", new Vector3(0f, 1.1f, 0f), new Color(0.75f, 0.9f, 1f), 1.35f); changed++; }
        Transform phone = root.transform.Find("Shop_Tent_Kit");
        if (phone != null) { AddGuideLabel(phone, "Guide_Phone", "P: 스마트폰\n감사 앱 확인", new Vector3(0f, 1.35f, 0f), new Color(1f, 0.74f, 0.86f), 1.35f); changed++; }
        EditorUtility.SetDirty(guides);
        return changed;
    }

    static void AddGuideLabel(Transform parent, string name, string text, Vector3 local, Color color, float size)
    {
        if (parent == null) return;
        Transform old = parent.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
        var label = go.AddComponent<PrototypeWorldLabel>(); label.Set(text, color, size); EditorUtility.SetDirty(go);
    }

    static int EnsurePlayerVisual(GameObject player)
    {
        int changed = 0;
        Transform visual = player.transform.Find(PlayerModelName);
        if (visual == null)
        {
            GameObject model = LoadPreferredPlayerModel(out string path);
            GameObject go = null;
            if (model != null) { ConfigureCharacterModelImporter(path); go = PrefabUtility.InstantiatePrefab(model, player.transform) as GameObject; if (go == null) go = UnityEngine.Object.Instantiate(model, player.transform); }
            if (go == null) { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.transform.SetParent(player.transform, false); UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); }
            go.name = PlayerModelName; go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one; visual = go.transform; changed++;
        }
        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col);
        NormalizeVisualHeight(visual, 1.85f); ConfigureAnimator(visual, "Assets/Art/Character/PlayerAnimator.controller"); EditorUtility.SetDirty(visual.gameObject);
        return changed;
    }

    static int EnsureNpcVisual(GameObject npc, string modelPath, string visualName)
    {
        int changed = 0;
        RemoveOldNpcVisuals(npc.transform, visualName);
        Transform visual = npc.transform.Find(visualName);
        if (visual == null)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject go = null;
            if (model != null) { ConfigureCharacterModelImporter(modelPath); go = PrefabUtility.InstantiatePrefab(model, npc.transform) as GameObject; if (go == null) go = UnityEngine.Object.Instantiate(model, npc.transform); }
            if (go == null) { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.transform.SetParent(npc.transform, false); UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); }
            go.name = visualName; go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one; visual = go.transform; changed++;
        }
        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col);
        NormalizeVisualHeight(visual, 1.75f); ConfigureAnimator(visual, "Assets/Art/Character/NpcAnimator.controller"); EditorUtility.SetDirty(visual.gameObject);
        return changed;
    }

    static string ResolveNpcModelPath(NpcController npc, int index)
    {
        string key = ((npc?.profile != null ? npc.profile.name + " " + npc.profile.npcName : "") + " " + (npc != null ? npc.gameObject.name : "")).ToLowerInvariant();
        if (key.Contains("lumber")) return "Assets/Art/Character/C-03.fbx";
        if (key.Contains("miner")) return "Assets/Art/Character/C-04.fbx";
        if (key.Contains("fisher")) return "Assets/Art/Character/C-05.fbx";
        if (key.Contains("chef")) return "Assets/Art/Character/C-06.fbx";
        if (key.Contains("blacksmith")) return "Assets/Art/Character/C-07.fbx";
        if (key.Contains("tailor")) return "Assets/Art/Character/C-08.fbx";
        if (key.Contains("carpenter")) return "Assets/Art/Character/C-09.fbx";

        string[] fallback =
        {
            "Assets/Art/Character/C-02.fbx",
            "Assets/Art/Character/C-03.fbx",
            "Assets/Art/Character/C-04.fbx",
            "Assets/Art/Character/C-05.fbx",
            "Assets/Art/Character/C-06.fbx",
            "Assets/Art/Character/C-07.fbx",
            "Assets/Art/Character/C-08.fbx",
            "Assets/Art/Character/C-09.fbx"
        };
        return fallback[Mathf.Abs(index) % fallback.Length];
    }

    static void RemoveOldNpcVisuals(Transform npcRoot, string keepName)
    {
        if (npcRoot == null) return;
        for (int i = npcRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = npcRoot.GetChild(i);
            if (!child.name.StartsWith("NpcModel_", StringComparison.Ordinal) || child.name == keepName) continue;
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    static void ConfigureAnimator(Transform visual, string controllerPath)
    {
        Animator animator = visual.GetComponentInChildren<Animator>(true) ?? visual.gameObject.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath); if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        Avatar avatar = LoadAvatarForVisual(visual); if (avatar != null) animator.avatar = avatar;
        if (animator.GetComponent<PlayerFootIkStabilizer>() == null) animator.gameObject.AddComponent<PlayerFootIkStabilizer>();
        EditorUtility.SetDirty(animator);
    }

    static GameObject LoadPreferredPlayerModel(out string path)
    {
        string[] paths = { "Assets/Art/Character/C-01.fbx", "Assets/Art/Character/C-02.fbx", "Assets/Art/Character/C-03.fbx" };
        foreach (string p in paths) { GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (model != null) { path = p; return model; } }
        path = null; return null;
    }

    static void ConfigureCharacterModelImporter(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        bool dirty = false;
        if (importer.animationType != ModelImporterAnimationType.Human) { importer.animationType = ModelImporterAnimationType.Human; dirty = true; }
        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true; }
        if (!importer.importAnimation) { importer.importAnimation = true; dirty = true; }
        if (importer.importCameras) { importer.importCameras = false; dirty = true; }
        if (importer.importLights) { importer.importLights = false; dirty = true; }
        if (dirty) importer.SaveAndReimport();
    }

    static Avatar LoadAvatarForVisual(Transform visual)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(visual.gameObject);
        if (string.IsNullOrEmpty(path))
        {
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
            if (source != null) path = AssetDatabase.GetAssetPath(source);
        }
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
    }

    static void NormalizeVisualHeight(Transform visual, float targetHeight)
    {
        if (visual == null || !TryGetRendererBounds(visual, out Bounds b) || b.size.y <= 0.001f) return;
        visual.localScale *= Mathf.Clamp(targetHeight / b.size.y, 0.05f, 10f);
        if (!TryGetRendererBounds(visual, out b)) return;
        visual.position += Vector3.up * (-b.min.y);
        visual.localPosition = new Vector3(0f, visual.localPosition.y, 0f);
    }

    static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = default; bool has = false;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    static GameObject ResolvePlayer()
    {
        try { GameObject tagged = GameObject.FindGameObjectWithTag("Player"); if (tagged != null) return tagged; } catch { }
        GameObject named = GameObject.Find("Player") ?? GameObject.Find("Prototype_Player") ?? GameObject.Find("Demo_Player");
        if (named != null) return named;
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>(); if (pc != null) return pc.gameObject;
        PlayerInteraction pi = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>(); return pi != null ? pi.gameObject : null;
    }

    static int MuteDuplicateGeneratedPlayers(GameObject keep)
    {
        int changed = 0;
        foreach (PlayerController pc in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc == null || pc.gameObject == keep) continue;
            string n = pc.gameObject.name;
            bool generated = n.StartsWith("Player", StringComparison.OrdinalIgnoreCase) || n.Contains("Prototype", StringComparison.OrdinalIgnoreCase) || n.Contains("Demo", StringComparison.OrdinalIgnoreCase);
            if (!generated) continue;
            pc.enabled = false; PlayerInteraction pi = pc.GetComponent<PlayerInteraction>(); if (pi != null) pi.enabled = false; SetTagSafe(pc.gameObject, "Untagged"); EditorUtility.SetDirty(pc.gameObject); changed++;
        }
        return changed;
    }

    static Vector3 ResolvePlayerSpawnPosition()
    {
        int changed = 0; GameObject root = EnsurePrototypeRoot(ref changed);
        Transform marker = root.transform.Find(SpawnName);
        if (marker == null) { var go = new GameObject(SpawnName); go.transform.SetParent(root.transform, false); go.transform.position = new Vector3(0f, 0f, -5.8f); marker = go.transform; EditorUtility.SetDirty(go); }
        Vector3 pos = marker.position; if (NavMesh.SamplePosition(pos, out var hit, 8f, NavMesh.AllAreas)) pos = hit.position; pos.y = Mathf.Max(0f, pos.y); return pos;
    }

    static Transform ResolveDemoShopTransform()
    {
        GameObject root = GameObject.Find(RootName); Transform demo = root != null ? root.transform.Find(DemoShopName) : null; if (demo != null) return demo;
        Shop shop = UnityEngine.Object.FindFirstObjectByType<Shop>(); return shop != null ? shop.transform : null;
    }

    static GameObject EnsurePrototypeRoot(ref int changed)
    {
        GameObject root = GameObject.Find(RootName); if (root != null) return root;
        root = new GameObject(RootName); changed++; EditorUtility.SetDirty(root); return root;
    }

    static int UpsertLocalBox(Transform parent, string name, Vector3 local, Vector3 scale, Color color)
    {
        int changed = UpsertBox(parent, name, parent.TransformPoint(local), scale, color);
        Transform child = parent.Find(name); if (child != null) { child.localPosition = local; child.localRotation = Quaternion.identity; EditorUtility.SetDirty(child); }
        return changed;
    }

    static int UpsertBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        Transform child = parent.Find(name); GameObject go; int changed = 0;
        if (child == null) { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); changed++; } else go = child.gameObject;
        go.transform.position = pos; go.transform.localScale = scale;
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (r.sharedMaterial == null || r.sharedMaterial.name.StartsWith("Default", StringComparison.Ordinal)) r.sharedMaterial = new Material(shader) { name = $"Mat_{name}" };
            r.sharedMaterial.color = color;
        }
        EditorUtility.SetDirty(go); return changed;
    }

    static void EnsureSlots(int size, ref List<InventorySlot> slots)
    {
        if (slots == null || slots.Count != size) { slots = new List<InventorySlot>(size); for (int i = 0; i < size; i++) slots.Add(new InventorySlot()); return; }
        for (int i = 0; i < slots.Count; i++) if (slots[i] == null) slots[i] = new InventorySlot();
    }

    static void SetSlot(List<InventorySlot> slots, int index, string resourcePath, int count)
    {
        if (slots == null || index < 0 || index >= slots.Count) return;
        global::Item item = Resources.Load<global::Item>(resourcePath);
        if (item == null) { Debug.LogWarning($"[PlayableDay] starter item missing: Resources/{resourcePath}"); return; }
        slots[index].Set(item, Mathf.Clamp(count, 1, item.maxStack));
    }

    static T EnsureComponent<T>(GameObject go, ref int changed) where T : Component
    {
        T c = go.GetComponent<T>(); if (c != null) return c;
        c = go.AddComponent<T>(); changed++; EditorUtility.SetDirty(go); return c;
    }

    static T EnsureSceneComponent<T>(GameObject host, ref int changed) where T : Component
    {
        T existing = UnityEngine.Object.FindFirstObjectByType<T>();
        if (existing != null) return existing;
        return EnsureComponent<T>(host, ref changed);
    }

    static void EnsureTag(string tag)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"); if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]); SerializedProperty tags = so.FindProperty("tags"); if (tags == null) return;
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.arraySize++; tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag; so.ApplyModifiedProperties();
    }

    static bool SetTagSafe(GameObject go, string tag)
    {
        if (go == null) return false;
        try { if (go.CompareTag(tag)) return false; go.tag = tag; EditorUtility.SetDirty(go); return true; } catch { return false; }
    }

    static void InvokeOptional(string typeName, string methodName)
    {
        Type t = Type.GetType(typeName); if (t == null) return;
        var m = t.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic); m?.Invoke(null, null);
    }

    public enum Level { Pass, Warn, Fail }
    public class ValidationItem { public string Label; public Level Level; public string Detail; }

    public static void ValidateAndReport()
    {
        List<ValidationItem> items = ValidateAll();
        int pass = items.Count(i => i.Level == Level.Pass), warn = items.Count(i => i.Level == Level.Warn), fail = items.Count(i => i.Level == Level.Fail);
        var sb = new System.Text.StringBuilder(); sb.AppendLine($"[PlayableDay] 검증 - PASS {pass} / WARN {warn} / FAIL {fail}");
        foreach (ValidationItem item in items) sb.AppendLine($"  {Mark(item.Level)} {item.Label}: {item.Detail}");
        if (fail > 0) Debug.LogError(sb.ToString()); else if (warn > 0) Debug.LogWarning(sb.ToString()); else Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Playable Day 검증", $"PASS: {pass}\nWARN: {warn}\nFAIL: {fail}\n\n자세한 내용은 Console 로그를 확인하세요.", "확인");
    }

    public static List<ValidationItem> ValidateAll()
    {
        var list = new List<ValidationItem>();
        GameObject player = ResolvePlayer();
        bool playerReady = player != null && HasTag(player, "Player") && player.GetComponent<CharacterController>() != null && player.GetComponent<PlayerController>() != null && player.GetComponent<Inventory>() != null && player.GetComponent<Hotbar>() != null && player.GetComponentInChildren<Renderer>(true) != null;
        list.Add(playerReady ? Pass("Player", $"{player.name} / model+controller+inventory OK") : Fail("Player", player == null ? "씬에 Player 없음" : "Tag/Controller/Inventory/Model 누락"));

        Inventory inv = player != null ? player.GetComponent<Inventory>() : UnityEngine.Object.FindFirstObjectByType<Inventory>();
        Hotbar hotbar = player != null ? player.GetComponent<Hotbar>() : UnityEngine.Object.FindFirstObjectByType<Hotbar>();
        int sellable = 0;
        if (inv?.slots != null) sellable += inv.slots.Count(s => s != null && !s.IsEmpty && s.item != null && s.item.category != ItemCategory.Tool);
        if (hotbar?.slots != null) sellable += hotbar.slots.Count(s => s != null && !s.IsEmpty && s.item != null && s.item.category != ItemCategory.Tool);
        list.Add(sellable >= 3 ? Pass("판매 가능 데모 아이템", $"{sellable}개") : Fail("판매 가능 데모 아이템", $"{sellable}/3"));

        ShopSlot[] slots = UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None);
        list.Add(slots.Length >= 4 ? Pass("ShopSlot 수", $"{slots.Length}/4+") : Fail("ShopSlot 수", $"{slots.Length}/4"));
        int emptySlots = slots.Count(s => s != null && s.IsEmpty);
        list.Add(emptySlots >= 1 ? Pass("첫 거래용 빈 진열대", $"{emptySlots}개") : Fail("첫 거래용 빈 진열대", "모든 슬롯이 이미 채워짐"));

        NpcController[] npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int nearMesh = npcs.Count(IsNpcNearNavMesh);
        list.Add(npcs.Length >= 1 ? Pass("첫 이주자/NPC", $"{npcs.Length}명") : Fail("첫 이주자/NPC", "없음"));
        list.Add(nearMesh >= 1 ? Pass("NPC NavMesh", $"{nearMesh}/{npcs.Length}") : Warn("NPC NavMesh", $"{nearMesh}/{npcs.Length} - Bake 후 재시도"));

        PlayerInteraction interaction = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
        list.Add(interaction != null ? Pass("PlayerInteraction", $"interactDistance={interaction.interactDistance}") : Fail("PlayerInteraction", "Player에 없음"));
        InteractPromptUI prompt = UnityEngine.Object.FindFirstObjectByType<InteractPromptUI>();
        list.Add(prompt != null ? Pass("InteractPromptUI", "씬 연결") : Fail("InteractPromptUI", "Space 프롬프트 없음"));

        SmartphoneUI phone = UnityEngine.Object.FindFirstObjectByType<SmartphoneUI>();
        int panels = phone?.tabPanels != null ? phone.tabPanels.Count(p => p != null) : 0; int buttons = phone?.tabButtons != null ? phone.tabButtons.Count(b => b != null) : 0;
        list.Add(phone != null && panels == 4 && buttons == 4 ? Pass("SmartphoneUI 4탭", $"패널 {panels}/4, 버튼 {buttons}/4") : Warn("SmartphoneUI 4탭", phone == null ? "SmartphoneUI 없음" : $"패널 {panels}/4, 버튼 {buttons}/4"));

        SaveManager save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        list.Add(save != null ? Pass("SaveManager", "씬 연결") : Fail("SaveManager", "없음 - F5/F9 불가"));
        PlayableDayScenarioController scenario = UnityEngine.Object.FindFirstObjectByType<PlayableDayScenarioController>();
        list.Add(scenario != null ? Pass("ScenarioController", $"단계={scenario.Current}, 이름={scenario.PlayerName}, 맵={scenario.SelectedMapName}") : Fail("ScenarioController", "없음"));

        GameObject root = GameObject.Find(RootName);
        bool hasProps = root != null && root.transform.Find("Support_Crate") != null && root.transform.Find("Shop_Tent_Kit") != null && root.transform.Find("Sales_Tent_Preview") != null;
        list.Add(hasProps ? Pass("첫날 프롭", "보급 상자/상점 텐트/판매대 배치") : Warn("첫날 프롭", "누락"));
        EconomyService econ = UnityEngine.Object.FindFirstObjectByType<EconomyService>();
        list.Add(econ != null && econ.Money == 500 ? Pass("첫날 시작금", "500G") : Warn("첫날 시작금", econ == null ? "EconomyService 없음" : $"{econ.Money}G"));
        bool loopReady = slots.Length >= 1 && npcs.Length >= 1 && nearMesh >= 1;
        list.Add(loopReady ? Pass("판매 루프 준비", "ShopSlot + NPC + NavMesh OK") : Fail("판매 루프 준비", "Slot 또는 NPC onMesh 부족"));
        return list;
    }

    static bool IsNpcNearNavMesh(NpcController npc) => npc != null && NavMesh.SamplePosition(npc.transform.position, out _, 2f, NavMesh.AllAreas);
    static bool HasTag(GameObject go, string tag) { if (go == null) return false; try { return go.CompareTag(tag); } catch { return false; } }
    static ValidationItem Pass(string label, string detail) => new ValidationItem { Label = label, Level = Level.Pass, Detail = detail };
    static ValidationItem Warn(string label, string detail) => new ValidationItem { Label = label, Level = Level.Warn, Detail = detail };
    static ValidationItem Fail(string label, string detail) => new ValidationItem { Label = label, Level = Level.Fail, Detail = detail };
    static string Mark(Level level) => level == Level.Pass ? "PASS" : level == Level.Warn ? "WARN" : "FAIL";
}
#endif
