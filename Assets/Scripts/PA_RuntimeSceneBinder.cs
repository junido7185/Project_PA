using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

// Project P.A. scene wiring safety net.
// MainGame.unity is generated/edited often, so this binder guarantees the
// runtime-critical services, UI apps, and NPC presentation hooks are connected
// every time Play mode enters a loaded scene.
public static class PA_RuntimeSceneBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BindLoadedScene()
    {
        // VS-PRESENT-001: 출항 코스는 builder가 기존 권위 컴포넌트를 명시 배치한다.
        // 일반 캠페인의 저장/휴대폰/월드 자동 연결을 개발용 인증 씬에 추가하지 않는다.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == DepartureTutorialController.SceneName)
            return;

        GameObject services = EnsureSceneRoot("[Services]");
        GameObject uiRoot = EnsureUiRoot();

        EnsureNavMeshForNpcAgents();
        EnsureCoreServices(services);
        EnsureUiServices(uiRoot);
        EnsureSmartphoneShell(uiRoot);
        EnsureSmartphoneApps();
        EnsureNpcRuntimeHooks();
        EnsurePlayerRuntimeHooks();

        Debug.Log("[PA RuntimeBinder] 씬 연결 완료: services/ui/smartphone/npc/player hooks");
    }

    static void EnsureCoreServices(GameObject services)
    {
        EnsureComponent<EconomyService>(services);
        var tier = EnsureComponent<TierService>(services);
        EnsureTierDefinitions(tier);

        EnsureComponent<GameClock>(services);
        EnsureComponent<GridService>(services);
        EnsureComponent<PlayerInputHandler>(services);
        EnsureComponent<AuditService>(services);
        EnsureComponent<FriendshipService>(services);
        EnsureComponent<BuildingRegistry>(services);
        EnsureComponent<SalesLogManager>(services);
        EnsureComponent<DayNightShopLoopController>(services);
        EnsureComponent<LongPlayProgressionController>(services);
        EnsureComponent<ProcessingOpportunityController>(services);
        EnsureComponent<CustomerDemandInsightController>(services);
        EnsureComponent<VillageChangeSignalController>(services);
        EnsureComponent<MerchandisingCornerController>(services);
        EnsureComponent<CustomerPreferencePresentationController>(services);
        EnsureComponent<PurchaseFeedbackPresentationController>(services);
        EnsureComponent<CustomerArrivalController>(services);
        EnsureComponent<InteriorCustomerController>(services);
        EnsureComponent<CoreSlicePresentationMode>(services);
        EnsureComponent<DemoVisualDressingController>(services);
        var save = EnsureComponent<SaveManager>(services);
        EnsureSaveBuildingTypes(save);
        EnsureComponent<GameManager>(services);
        EnsureComponent<AudioManager>(EnsureSceneRoot("AudioManager"));
        EnsureComponent<ScreenFader>(services);

        var registry = EnsureComponent<ItemRegistry>(services);
        EnsureItemRegistry(registry);

        var hiring = EnsureComponent<HiringService>(services);
        EnsureHiringCandidates(hiring);

        EnsureComponent<BuildManager>(services);
        EnsureDayNightVisual();
    }

    static void EnsureUiServices(GameObject uiRoot)
    {
        GameObject host = FindSceneGameObject("PA_RuntimeUI");
        if (host == null)
        {
            host = new GameObject("PA_RuntimeUI");
            host.transform.SetParent(uiRoot.transform, false);
        }

        EnsureComponent<InteractPromptUI>(host);
        EnsureComponent<DialogueUI>(host);
        EnsureComponent<FriendshipUI>(host);
        EnsureComponent<ClockHUD>(host);
        EnsureComponent<CraftingUI>(host);
        EnsureComponent<StorageUI>(host);
        EnsureComponent<PauseManager>(host);
        EnsureComponent<MoneyHUD>(host);
        EnsureComponent<ShopPriceUI>(host);
    }

    static void EnsureSmartphoneApps()
    {
        EnsurePanelApp<AuditResultUI>("AuditPanel");
        EnsurePanelApp<HiringUI>("HiringPanel");
        EnsurePanelApp<FeedUI>("FeedPanel");
        EnsurePanelApp<SettingsUI>("SettingsPanel");
    }

    // WorldSandbox is intentionally scene-light.  The Golden/Main scenes keep
    // their authored phone hierarchy; only scenes without a SmartphoneUI get
    // this compact runtime shell so the same P-key and four app components are
    // available to an actual player without serializing a second scene copy.
    static void EnsureSmartphoneShell(GameObject uiRoot)
    {
        EnsureEventSystem();
        if (FindSceneComponent<SmartphoneUI>() != null) return;

        var phone = AddUiChild(uiRoot.transform, "SmartphoneContainer", typeof(Image));
        var phoneRect = (RectTransform)phone.transform;
        phoneRect.anchorMin = Vector2.zero;
        phoneRect.anchorMax = Vector2.zero;
        phoneRect.pivot = Vector2.zero;
        phoneRect.sizeDelta = new Vector2(420f, 720f);
        phoneRect.anchoredPosition = new Vector2(40f, -640f);
        phone.GetComponent<Image>().color = new Color(0.88f, 0.34f, 0.34f, 0.99f);

        var screen = AddUiChild(phone.transform, "PhoneScreen", typeof(Image));
        Stretch((RectTransform)screen.transform, 16f);
        screen.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

        AddText(screen.transform, "StatusBar", "PROJECT P.A.  ·  PHONE", 17f,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -42f), new Vector2(-52f, -8f),
            TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.92f, 0.78f));

        Button closeButton = AddButton(screen.transform, "CloseButton", "×",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-46f, -42f), new Vector2(-8f, -8f),
            new Color(0.35f, 0.38f, 0.48f), 24f);

        var content = AddUiChild(screen.transform, "ContentArea");
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(12f, 42f);
        contentRect.offsetMax = new Vector2(-12f, -52f);

        var home = AddUiChild(content.transform, "HomeScreen", typeof(GridLayoutGroup));
        Stretch((RectTransform)home.transform);
        var grid = home.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(166f, 178f);
        grid.spacing = new Vector2(18f, 24f);
        grid.padding = new RectOffset(18, 18, 34, 24);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;

        string[] panelNames = { "AuditPanel", "HiringPanel", "FeedPanel", "SettingsPanel" };
        string[] labels = { "감사", "채용", "피드", "설정" };
        Color[] colors =
        {
            new Color(0.28f, 0.66f, 0.88f), new Color(0.94f, 0.57f, 0.31f),
            new Color(0.48f, 0.72f, 0.48f), new Color(0.56f, 0.54f, 0.76f)
        };
        var panels = new GameObject[panelNames.Length];
        var buttons = new Button[panelNames.Length];

        for (int i = 0; i < panelNames.Length; i++)
        {
            var tile = AddUiChild(home.transform, panelNames[i] + "_Tile", typeof(Image), typeof(Button));
            tile.GetComponent<Image>().color = colors[i];
            var layout = tile.AddComponent<LayoutElement>();
            layout.preferredWidth = 166f;
            layout.preferredHeight = 178f;
            AddText(tile.transform, "Label", labels[i], 30f,
                Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f),
                TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            buttons[i] = tile.GetComponent<Button>();

            var panel = AddUiChild(content.transform, panelNames[i], typeof(Image));
            Stretch((RectTransform)panel.transform);
            panel.GetComponent<Image>().color = new Color(0.95f, 0.92f, 0.86f, 1f);

            Button backButton = AddButton(panel.transform, "BackButton", "← 홈",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -44f), new Vector2(92f, -8f),
                new Color(0.22f, 0.24f, 0.3f), 16f);
            AddText(panel.transform, "PanelTitle", labels[i], 22f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(104f, -44f), new Vector2(-12f, -8f),
                TextAlignmentOptions.MidlineLeft, new Color(0.16f, 0.17f, 0.21f), FontStyles.Bold);

            backButton.onClick.AddListener(() => SmartphoneUI.instance?.ReturnToHome());
            panel.SetActive(false);
            panels[i] = panel;
        }

        AddText(screen.transform, "CloseHint", "P / ESC 닫기", 14f,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(12f, 8f), new Vector2(-12f, 36f),
            TextAlignmentOptions.Center, new Color(0.82f, 0.84f, 0.9f));

        SmartphoneUI smartphone = phone.AddComponent<SmartphoneUI>();
        smartphone.root = phoneRect;
        smartphone.hoverTrigger = phoneRect;
        smartphone.homeScreen = home;
        smartphone.tabPanels = panels;
        smartphone.tabButtons = buttons;
        smartphone.hiddenPos = new Vector2(40f, -640f);
        smartphone.peekPos = new Vector2(40f, -570f);
        smartphone.transitionTime = 0.25f;
        closeButton.onClick.AddListener(smartphone.Close);
    }

    static void EnsureEventSystem()
    {
        if (FindSceneComponent<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static GameObject AddUiChild(Transform parent, string name, params Type[] extraComponents)
    {
        var components = new List<Type> { typeof(RectTransform) };
        if (extraComponents != null) components.AddRange(extraComponents);
        var go = new GameObject(name, components.ToArray());
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string value, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        TextAlignmentOptions alignment, Color color, FontStyles fontStyle = FontStyles.Normal)
    {
        var go = AddUiChild(parent, name, typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static Button AddButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Color color, float fontSize)
    {
        var go = AddUiChild(parent, name, typeof(Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        go.GetComponent<Image>().color = color;
        AddText(go.transform, "Text", label, fontSize, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        return go.GetComponent<Button>();
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    static void EnsureNpcRuntimeHooks()
    {
        DialogueData[] dialogueAssets = Resources.LoadAll<DialogueData>("Dialogues");

        foreach (var npc in FindSceneComponents<NpcController>())
        {
            var dialogue = npc.GetComponent<NpcDialogue>();
            if (dialogue == null) dialogue = npc.gameObject.AddComponent<NpcDialogue>();

            if (dialogue.dialogueData == null)
                dialogue.dialogueData = MatchDialogue(dialogueAssets, npc);

            if (string.IsNullOrEmpty(dialogue.friendshipId))
                dialogue.friendshipId = ResolveFriendshipId(npc);

            if (npc.GetComponentInChildren<NpcBubbleUI>(true) == null)
            {
                var bubbleGO = new GameObject("NpcBubbleUI", typeof(RectTransform), typeof(Canvas));
                bubbleGO.transform.SetParent(npc.transform, false);
                bubbleGO.AddComponent<NpcBubbleUI>();
            }

            foreach (var bubble in npc.GetComponentsInChildren<NpcBubbleUI>(true))
            {
                bubble.offset = new Vector3(0f, 4.0f, 0f);
                bubble.transform.localPosition = bubble.offset;
            }

            var normalizer = npc.GetComponent<NpcPresentationNormalizer>();
            if (normalizer == null)
                normalizer = npc.gameObject.AddComponent<NpcPresentationNormalizer>();
            normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
            normalizer.animatorController = null;
            NpcPresentationNormalizer.Normalize(npc.gameObject);
        }
    }

    static void EnsureNavMeshForNpcAgents()
    {
        int surfaceCount = AddSceneNavMeshSurfaceData();
        int agentCount = 0;
        int enabledCount = 0;
        int snappedCount = 0;
        int onMeshCount = 0;

        foreach (var npc in FindSceneComponents<NpcController>())
        {
            if (npc == null) continue;

            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null) continue;

            agentCount++;

            if (NavMesh.SamplePosition(npc.transform.position, out var hit, 12f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(npc.transform.position, hit.position) > 0.05f)
                {
                    npc.transform.position = hit.position;
                    snappedCount++;
                }
            }

            if (!agent.enabled)
                agent.enabled = true;

            if (!agent.enabled) continue;
            enabledCount++;

            if (!agent.isOnNavMesh && NavMesh.SamplePosition(npc.transform.position, out hit, 12f, NavMesh.AllAreas))
            {
                if (agent.Warp(hit.position))
                    snappedCount++;
            }

            if (agent.isOnNavMesh)
                onMeshCount++;
        }

        Debug.Log($"[PA RuntimeBinder] NavMesh/NPC agents ready: surfaces={surfaceCount}, agents={enabledCount}/{agentCount}, onMesh={onMeshCount}, snapped={snappedCount}");
    }

    static int AddSceneNavMeshSurfaceData()
    {
        Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation")
            ?? Type.GetType("UnityEngine.AI.NavMeshSurface");
        if (surfaceType == null) return 0;

        MethodInfo addData = surfaceType.GetMethod("AddData", BindingFlags.Instance | BindingFlags.Public);
        if (addData == null) return 0;

        int count = 0;
        foreach (var obj in Resources.FindObjectsOfTypeAll(surfaceType))
        {
            if (obj is not Component component) continue;
            if (!IsSceneObject(component.gameObject)) continue;
            if (!component.gameObject.activeInHierarchy) continue;

            if (component is Behaviour behaviour && !behaviour.enabled)
                continue;

            try
            {
                addData.Invoke(component, null);
                count++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PA RuntimeBinder] NavMeshSurface AddData failed on {component.name}: {e.Message}");
            }
        }

        return count;
    }

    static void EnsurePlayerRuntimeHooks()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? FindSceneGameObject("Player");
        if (player == null) return;

        EnsureComponent<EquipmentSystem>(player);

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
            EnsureComponent<PlayerFootIkStabilizer>(animator.gameObject);

        var buildManager = FindSceneComponent<BuildManager>();
        if (buildManager != null)
            buildManager.placementAnchor = player.transform;
    }

    static void EnsureTierDefinitions(TierService tierService)
    {
        if (tierService == null || tierService.GetDefinition(0) != null) return;

        var defs = Resources.LoadAll<TierDefinition>("Tiers")
            .Where(t => t != null)
            .OrderBy(t => t.tier)
            .ToList();
        if (defs.Count == 0) return;

        var field = typeof(TierService).GetField("tierDefinitions",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(tierService, defs);
    }

    static void EnsureItemRegistry(ItemRegistry registry)
    {
        if (registry == null) return;
        if (registry.allItems != null && registry.allItems.Count > 0) return;

        registry.allItems = Resources.LoadAll<Item>("Items")
            .Where(i => i != null)
            .OrderBy(i => i.id)
            .ThenBy(i => i.itemName)
            .ToList();
    }

    static void EnsureSaveBuildingTypes(SaveManager save)
    {
        if (save == null) return;
        if (save.allBuildingTypes != null && save.allBuildingTypes.Count > 0) return;

        save.allBuildingTypes = Resources.LoadAll<BuildingData>("Buildings")
            .Where(b => b != null && b.prefab != null)
            .OrderBy(b => b.prefab.name)
            .ToList();
    }

    static void EnsureHiringCandidates(HiringService hiring)
    {
        if (hiring == null) return;

        if (hiring.availableCandidates == null || hiring.availableCandidates.Count == 0)
        {
            hiring.availableCandidates = Resources.LoadAll<NpcCandidateData>("Candidates")
                .Where(c => c != null)
                .ToList();
        }

        if (hiring.spawnPoint == null)
        {
            var spawn = FindSceneGameObject("PlayerSpawn_Outside_Shop")
                     ?? FindSceneGameObject("PlayerSpawn_Outside");
            if (spawn != null) hiring.spawnPoint = spawn.transform;
        }
    }

    static void EnsureDayNightVisual()
    {
        if (FindSceneComponent<DayNightVisual>() != null) return;

        Light light = FindSceneComponents<Light>()
            .FirstOrDefault(l => l != null && l.type == LightType.Directional);
        if (light == null)
        {
            var lightGO = new GameObject("Directional Light");
            light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        var visual = light.gameObject.AddComponent<DayNightVisual>();
        visual.directionalLight = light;
    }

    static void EnsurePanelApp<T>(string panelName) where T : Component
    {
        GameObject panel = FindSceneGameObject(panelName);
        if (panel == null) return;

        Transform placeholder = panel.transform.Find("Placeholder");
        if (placeholder != null) placeholder.gameObject.SetActive(false);
        if (panel.GetComponentInChildren<T>(true) != null) return;

        GameObject content = FindChild(panel.transform, "RuntimeContent");
        if (content == null)
        {
            content = new GameObject("RuntimeContent", typeof(RectTransform));
            var rt = (RectTransform)content.transform;
            rt.SetParent(panel.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 18f);
            rt.offsetMax = new Vector2(-18f, -54f);
        }

        content.AddComponent<T>();
    }

    static T EnsureComponent<T>(GameObject host) where T : Component
    {
        T existing = FindSceneComponent<T>();
        if (existing != null) return existing;

        T onHost = host.GetComponent<T>();
        return onHost != null ? onHost : host.AddComponent<T>();
    }

    static GameObject EnsureSceneRoot(string name)
    {
        return FindSceneGameObject(name) ?? new GameObject(name);
    }

    static GameObject EnsureUiRoot()
    {
        GameObject root = FindSceneGameObject("PA_UIRoot");
        if (root != null) return root;

        root = new GameObject("PA_UIRoot",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    static DialogueData MatchDialogue(DialogueData[] assets, NpcController npc)
    {
        if (assets == null || assets.Length == 0 || npc == null) return null;

        string[] keys =
        {
            npc.gameObject.name.Replace("NPC_", ""),
            npc.profile != null ? npc.profile.name : "",
            npc.profile != null ? npc.profile.npcName : ""
        };

        foreach (string rawKey in keys)
        {
            string key = Normalize(rawKey);
            if (string.IsNullOrEmpty(key)) continue;

            var match = assets.FirstOrDefault(d => d != null && Normalize(d.name).Contains(key));
            if (match != null) return match;
        }

        return assets.FirstOrDefault(d => d != null);
    }

    static string ResolveFriendshipId(NpcController npc)
    {
        if (npc == null) return "";
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.name))
            return npc.profile.name;
        return npc.gameObject.name;
    }

    static GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child.gameObject;
        }
        return null;
    }

    static T FindSceneComponent<T>() where T : Component
    {
        T active = Object.FindFirstObjectByType<T>();
        if (active != null) return active;

        return FindSceneComponents<T>().FirstOrDefault();
    }

    static List<T> FindSceneComponents<T>() where T : Component
    {
        var result = new List<T>();
        foreach (var component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component == null) continue;
            if (!IsSceneObject(component.gameObject)) continue;
            result.Add(component);
        }
        return result;
    }

    static GameObject FindSceneGameObject(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || go.name != name) continue;
            if (IsSceneObject(go)) return go;
        }
        return null;
    }

    static bool IsSceneObject(GameObject go)
    {
        return go != null && go.scene.IsValid() && go.scene.isLoaded;
    }

    static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
