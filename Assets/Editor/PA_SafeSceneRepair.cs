#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Non-destructive scene repair for the Project P.A. hub.
// This intentionally does not rebuild map/content/UI roots. It only guarantees
// runtime-critical components and references on top of the current scene.
public static class PA_SafeSceneRepair
{
    public static int RepairCurrentScene(bool includeNavMeshBake = true, bool showDialog = true)
    {
        int changed = 0;

        GameObject services = EnsureSceneRoot("[Services]", ref changed);
        GameObject uiRoot = EnsureUiRoot(ref changed);
        GameObject runtimeUi = EnsureChild(uiRoot.transform, "PA_RuntimeUI", ref changed);
        GameObject audioRoot = EnsureSceneRoot("AudioManager", ref changed);

        changed += EnsureCoreSingletons(services, audioRoot);
        changed += EnsureEventSystem();
        changed += RewireServiceReferences();
        changed += EnsureRuntimeUi(runtimeUi);
        changed += EnsureSmartphoneApps();
        changed += EnsureNpcHooks();
        changed += EnsureShopSlotColliders();
        changed += EnsurePlayerHooks();

        if (includeNavMeshBake)
        {
            if (TryBakeNavMesh()) changed++;
            changed += SnapNpcsToNavMesh();
        }

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (showDialog)
            EditorUtility.DisplayDialog("Project P.A. Safe Repair",
                $"Non-destructive repair complete.\n\nChanged: {changed}",
                "OK");

        Debug.Log($"[PA SafeRepair] complete, changed={changed}");
        return changed;
    }

    public static bool IsNpcNavMeshReady(NpcController npc)
    {
        if (npc == null) return false;

        var agent = npc.GetComponent<NavMeshAgent>();
        if (agent == null) return false;

        if (Application.isPlaying)
            return agent.isOnNavMesh;

        return NavMesh.SamplePosition(npc.transform.position, out _, 4f, NavMesh.AllAreas);
    }

    static int EnsureCoreSingletons(GameObject services, GameObject audioRoot)
    {
        int changed = 0;

        changed += EnsureSingleOnHost<EconomyService>(services);
        changed += EnsureSingleOnHost<TierService>(services);
        changed += EnsureSingleOnHost<GameClock>(services);
        changed += EnsureSingleOnHost<ItemRegistry>(services);
        changed += EnsureSingleOnHost<FriendshipService>(services);
        changed += EnsureSingleOnHost<HiringService>(services);
        changed += EnsureSingleOnHost<GridService>(services);
        changed += EnsureSingleOnHost<PlayerInputHandler>(services);
        changed += EnsureSingleOnHost<AuditService>(services);
        changed += EnsureSingleOnHost<SalesLogManager>(services);
        changed += EnsureSingleOnHost<SaveManager>(services);
        changed += EnsureSingleOnHost<BuildingRegistry>(services);
        changed += EnsureSingleOnHost<BuildManager>(services);
        changed += EnsureSingleOnHost<GameManager>(services);
        changed += EnsureSingleOnHost<ScreenFader>(services);
        changed += EnsureSingleOnHost<AudioManager>(audioRoot);

        return changed;
    }

    static int RewireServiceReferences()
    {
        int changed = 0;

        var registry = UnityEngine.Object.FindFirstObjectByType<ItemRegistry>();
        if (registry != null && (registry.allItems == null || registry.allItems.Count == 0))
        {
            registry.allItems = Resources.LoadAll<Item>("Items")
                .Where(i => i != null)
                .OrderBy(i => i.id)
                .ThenBy(i => i.itemName)
                .ToList();
            EditorUtility.SetDirty(registry);
            changed++;
        }

        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (save != null && (save.allBuildingTypes == null || save.allBuildingTypes.Count == 0))
        {
            save.allBuildingTypes = Resources.LoadAll<BuildingData>("Buildings")
                .Where(b => b != null && b.prefab != null)
                .OrderBy(b => b.prefab.name)
                .ToList();
            EditorUtility.SetDirty(save);
            changed++;
        }

        var tier = UnityEngine.Object.FindFirstObjectByType<TierService>();
        if (tier != null && CountTierDefinitions(tier) == 0)
        {
            var defs = LoadAllAssets<TierDefinition>(
                    "Assets/Resources/Tiers",
                    "Assets/ScriptableObjects/Tiers")
                .Where(t => t != null)
                .GroupBy(t => t.tier)
                .Select(g => g.First())
                .OrderBy(t => t.tier)
                .ToList();

            if (defs.Count > 0 && SetPrivateList(tier, "tierDefinitions", defs))
            {
                EditorUtility.SetDirty(tier);
                changed++;
            }
        }

        var hiring = UnityEngine.Object.FindFirstObjectByType<HiringService>();
        if (hiring != null)
        {
            if (hiring.availableCandidates == null || hiring.availableCandidates.Count == 0)
            {
                hiring.availableCandidates = Resources.LoadAll<NpcCandidateData>("Candidates")
                    .Where(c => c != null)
                    .ToList();
                EditorUtility.SetDirty(hiring);
                changed++;
            }

            if (hiring.spawnPoint == null)
            {
                var spawn = GameObject.Find("PlayerSpawn_Outside_Shop")
                         ?? GameObject.Find("PlayerSpawn_Outside")
                         ?? GameObject.FindGameObjectWithTag("Player");
                if (spawn != null)
                {
                    hiring.spawnPoint = spawn.transform;
                    EditorUtility.SetDirty(hiring);
                    changed++;
                }
            }
        }

        var buildManager = UnityEngine.Object.FindFirstObjectByType<BuildManager>();
        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (buildManager != null && player != null && buildManager.placementAnchor != player.transform)
        {
            buildManager.placementAnchor = player.transform;
            EditorUtility.SetDirty(buildManager);
            changed++;
        }

        return changed;
    }

    static int EnsureRuntimeUi(GameObject runtimeUi)
    {
        int changed = 0;
        runtimeUi.SetActive(true);

        changed += EnsureSceneSingletonPresent<InteractPromptUI>(runtimeUi);
        changed += EnsureSceneSingletonPresent<DialogueUI>(runtimeUi);
        changed += EnsureSceneSingletonPresent<FriendshipUI>(runtimeUi);
        changed += EnsureSceneSingletonPresent<ClockHUD>(runtimeUi);
        changed += EnsureSceneSingletonPresent<CraftingUI>(runtimeUi);
        changed += EnsureSceneSingletonPresent<PauseManager>(runtimeUi);
        changed += EnsureSceneSingletonPresent<MoneyHUD>(runtimeUi);

        return changed;
    }

    static int EnsureSmartphoneApps()
    {
        int changed = 0;
        changed += EnsurePanelApp<AuditResultUI>("AuditPanel");
        changed += EnsurePanelApp<HiringUI>("HiringPanel");
        changed += EnsurePanelApp<FeedUI>("FeedPanel");
        changed += EnsurePanelApp<SettingsUI>("SettingsPanel");
        return changed;
    }

    static int EnsureNpcHooks()
    {
        int changed = 0;
        DialogueData[] dialogues = Resources.LoadAll<DialogueData>("Dialogues");
        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
        {
            if (npc == null) continue;

            if (npc.GetComponent<Collider>() == null)
            {
                var cc = npc.gameObject.AddComponent<CapsuleCollider>();
                cc.height = 1.8f;
                cc.radius = 0.45f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                EditorUtility.SetDirty(npc.gameObject);
                changed++;
            }

            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = npc.gameObject.AddComponent<NavMeshAgent>();
                changed++;
            }

            if (ConfigureAgent(agent))
            {
                EditorUtility.SetDirty(agent);
                changed++;
            }

            var dialogue = npc.GetComponent<NpcDialogue>();
            if (dialogue == null)
            {
                dialogue = npc.gameObject.AddComponent<NpcDialogue>();
                changed++;
            }

            if (dialogue.dialogueData == null)
            {
                dialogue.dialogueData = MatchDialogue(dialogues, npc);
                if (dialogue.dialogueData != null)
                {
                    EditorUtility.SetDirty(dialogue);
                    changed++;
                }
            }

            if (string.IsNullOrEmpty(dialogue.friendshipId))
            {
                dialogue.friendshipId = ResolveFriendshipId(npc);
                EditorUtility.SetDirty(dialogue);
                changed++;
            }

            if (npc.GetComponentInChildren<NpcBubbleUI>(true) == null)
            {
                var bubble = new GameObject("NpcBubbleUI", typeof(RectTransform), typeof(Canvas));
                bubble.transform.SetParent(npc.transform, false);
                bubble.AddComponent<NpcBubbleUI>();
                changed++;
            }
        }

        return changed;
    }

    static int EnsureShopSlotColliders()
    {
        int changed = 0;
        foreach (var slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot == null || slot.GetComponent<Collider>() != null) continue;

            var bc = slot.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.9f, 0.4f, 0.9f);
            bc.center = new Vector3(0f, 0.2f, 0f);
            EditorUtility.SetDirty(slot.gameObject);
            changed++;
        }
        return changed;
    }

    static int EnsurePlayerHooks()
    {
        int changed = 0;
        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player == null) return 0;

        changed += EnsureSingleOnObject<EquipmentSystem>(player);

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
            changed += EnsureSingleOnObject<PlayerFootIkStabilizer>(animator.gameObject);

        return changed;
    }

    static int SnapNpcsToNavMesh()
    {
        int changed = 0;
        foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
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

    static bool TryBakeNavMesh()
    {
        try
        {
            var ground = GameObject.Find("Ground");
            if (ground == null) return false;

            var surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null) return false;

            var surface = ground.GetComponent(surfaceType) ?? ground.AddComponent(surfaceType);
            var bakeMethod = surfaceType.GetMethod("BuildNavMesh");
            if (bakeMethod == null) return false;

            bakeMethod.Invoke(surface, null);
            EditorUtility.SetDirty(ground);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PA SafeRepair] NavMesh bake failed: {e.Message}");
            return false;
        }
    }

    static int EnsureEventSystem()
    {
        var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        int changed = 0;

        EventSystem keep = systems.FirstOrDefault(s => s != null && s.gameObject.name == "EventSystem")
                        ?? systems.FirstOrDefault();

        if (keep == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            AddInputModule(go);
            changed++;
            keep = go.GetComponent<EventSystem>();
        }

        if (keep.GetComponent<BaseInputModule>() == null)
        {
            AddInputModule(keep.gameObject);
            changed++;
        }

        foreach (var system in systems)
        {
            if (system == null || system == keep) continue;
            UnityEngine.Object.DestroyImmediate(system.gameObject);
            changed++;
        }

        return changed;
    }

    static void AddInputModule(GameObject go)
    {
        var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModule != null) go.AddComponent(inputSystemModule);
        else go.AddComponent<StandaloneInputModule>();
    }

    static int EnsureSingleOnHost<T>(GameObject host) where T : Component
    {
        int changed = 0;
        T keep = host.GetComponent<T>();
        if (keep == null)
        {
            keep = host.AddComponent<T>();
            EditorUtility.SetDirty(host);
            changed++;
        }

        var all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var comp in all)
        {
            if (comp == null || comp == keep) continue;
            UnityEngine.Object.DestroyImmediate(comp);
            changed++;
        }

        return changed;
    }

    static int EnsureSceneSingletonPresent<T>(GameObject fallbackHost) where T : Component
    {
        int changed = 0;
        var all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        T keep = all.FirstOrDefault(c => c != null && c.gameObject == fallbackHost)
              ?? all.FirstOrDefault(c => c != null);

        if (keep == null)
        {
            fallbackHost.AddComponent<T>();
            EditorUtility.SetDirty(fallbackHost);
            return 1;
        }

        foreach (var comp in all)
        {
            if (comp == null || comp == keep) continue;
            UnityEngine.Object.DestroyImmediate(comp);
            changed++;
        }

        return changed;
    }

    static int EnsureSingleOnObject<T>(GameObject host) where T : Component
    {
        var components = host.GetComponents<T>();
        if (components.Length == 0)
        {
            host.AddComponent<T>();
            EditorUtility.SetDirty(host);
            return 1;
        }

        int changed = 0;
        for (int i = 1; i < components.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(components[i]);
            changed++;
        }
        return changed;
    }

    static int EnsurePanelApp<T>(string panelName) where T : Component
    {
        GameObject panel = GameObject.Find(panelName);
        if (panel == null) return 0;
        if (panel.GetComponentInChildren<T>(true) != null) return 0;

        Transform placeholder = panel.transform.Find("Placeholder");
        if (placeholder != null) placeholder.gameObject.SetActive(false);

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
        EditorUtility.SetDirty(content);
        return 1;
    }

    static GameObject EnsureUiRoot(ref int changed)
    {
        var root = GameObject.Find("PA_UIRoot");
        if (root == null)
        {
            root = new GameObject("PA_UIRoot", typeof(RectTransform));
            changed++;
        }

        if (root.GetComponent<Canvas>() == null)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            changed++;
        }

        if (root.GetComponent<CanvasScaler>() == null)
        {
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            changed++;
        }

        if (root.GetComponent<GraphicRaycaster>() == null)
        {
            root.AddComponent<GraphicRaycaster>();
            changed++;
        }

        EditorUtility.SetDirty(root);
        return root;
    }

    static GameObject EnsureSceneRoot(string name, ref int changed)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;

        changed++;
        return new GameObject(name);
    }

    static GameObject EnsureChild(Transform parent, string name, ref int changed)
    {
        var child = parent.Find(name);
        if (child != null) return child.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        changed++;
        return go;
    }

    static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child.gameObject;
        }
        return null;
    }

    static bool ConfigureAgent(NavMeshAgent agent)
    {
        if (agent == null) return false;

        bool changed = false;
        if (!Mathf.Approximately(agent.height, 1.8f)) { agent.height = 1.8f; changed = true; }
        if (!Mathf.Approximately(agent.radius, 0.4f)) { agent.radius = 0.4f; changed = true; }
        if (!Mathf.Approximately(agent.speed, 2.5f)) { agent.speed = 2.5f; changed = true; }
        if (!Mathf.Approximately(agent.angularSpeed, 360f)) { agent.angularSpeed = 360f; changed = true; }
        if (!Mathf.Approximately(agent.acceleration, 8f)) { agent.acceleration = 8f; changed = true; }
        return changed;
    }

    static DialogueData MatchDialogue(DialogueData[] dialogues, NpcController npc)
    {
        if (dialogues == null || dialogues.Length == 0 || npc == null) return null;

        string[] keys =
        {
            npc.gameObject.name,
            npc.gameObject.name.Replace("NPC_", ""),
            npc.profile != null ? npc.profile.name : "",
            npc.profile != null ? npc.profile.npcName : ""
        };

        foreach (string raw in keys)
        {
            string key = Normalize(raw);
            if (string.IsNullOrEmpty(key)) continue;

            var match = dialogues.FirstOrDefault(d => d != null && Normalize(d.name).Contains(key));
            if (match != null) return match;
        }

        return dialogues.FirstOrDefault(d => d != null);
    }

    static string ResolveFriendshipId(NpcController npc)
    {
        if (npc == null) return "";
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.name))
            return npc.profile.name;
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.npcName))
            return npc.profile.npcName;
        return npc.gameObject.name;
    }

    static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.ToLowerInvariant()
            .Replace("npc_", "")
            .Replace("profile_", "")
            .Replace("dialogue_", "")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
    }

    static int CountTierDefinitions(TierService tier)
    {
        var field = typeof(TierService).GetField("tierDefinitions",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field?.GetValue(tier) is System.Collections.IList list ? list.Count : 0;
    }

    static bool SetPrivateList<T>(object target, string fieldName, List<T> values)
    {
        var field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null) return false;
        field.SetValue(target, values);
        return true;
    }

    static List<T> LoadAllAssets<T>(params string[] folders) where T : UnityEngine.Object
    {
        var result = new List<T>();
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && !result.Contains(asset))
                    result.Add(asset);
            }
        }
        return result;
    }
}
#endif
