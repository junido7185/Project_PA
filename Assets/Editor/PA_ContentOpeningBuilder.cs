using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// CONTENT-001 전용 자산만 생성한다. 씬과 기존 역할 프리팹은 읽기만 한다.
public static class PA_ContentOpeningBuilder
{
    public const string PrefabPath = "Assets/Resources/Residents/Resident_Bori.prefab";
    public const string ProfilePath = "Assets/Resources/NPCs/Profile_Bori.asset";
    public const string DialoguePath = "Assets/Resources/Dialogues/Dialogue_Bori.asset";

    [MenuItem("Project PA/Content/Build Opening and Bori")]
    public static void Build()
    {
        AuditExistingCast();
        NpcProfile profile = LoadOrCreate<NpcProfile>(ProfilePath);
        profile.npcName = "보리";
        profile.bio = "이곳에 막 정착한 이웃. 새 물건을 반기지만 오늘 쓸 것과 가격을 먼저 살핀다.";
        profile.traitEI = 0.3f;
        profile.traitSN = 0.15f;
        profile.traitTF = 0.3f;
        profile.traitJP = -0.1f;
        profile.workEfficiency = 1f;
        profile.utilityConsumption = 1f;
        profile.luxuryConsumption = 0.8f;
        profile.priceSensitivity = 1.2f;
        EditorUtility.SetDirty(profile);

        DialogueData dialogue = LoadOrCreate<DialogueData>(DialoguePath);
        dialogue.label = "보리 · 첫 정착과 오늘의 장보기";
        dialogue.topics = new List<TopicDialoguePool>
        {
            Pool(DialogueTopic.Greeting, "보리예요. 오늘 밤엔 여기서 물건을 고를 수 있을까요? 낮에 모은 재료도 좋아요. 오늘 쓸 것부터 찾아볼게요."),
            Pool(DialogueTopic.SmallTalk, "아직 길이 낯설어요. 그래도 가게가 보이면 집에 가까워진 것 같아요."),
            Pool(DialogueTopic.ShopBrowse, "오늘 필요한 것부터 볼게요. 가격도 살펴보고요."),
            Pool(DialogueTopic.ShopBought, "이건 오늘 바로 쓸 수 있겠어요. 고마워요!"),
            Pool(DialogueTopic.ShopTooExpensive, "마음은 가는데… 오늘은 조금 더 생각해 볼게요."),
            Pool(DialogueTopic.Economy, "필요한 게 생기면 다시 들를게요. 내일도 문 여시죠?")
        };
        EditorUtility.SetDirty(dialogue);

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Character/C-01.fbx");
        if (source == null) throw new InvalidOperationException("Existing C-01 visual is required.");
        var root = new GameObject("Resident_Bori");
        root.SetActive(false);
        try
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 2.2f;
            root.AddComponent<CapsuleCollider>();
            var talk = root.AddComponent<NpcDialogue>();
            talk.dialogueData = dialogue;
            talk.overrideProfile = profile;
            talk.friendshipId = CampaignOpeningController.BoriId;
            talk.interactPrompt = "인사하기";
            var npc = root.AddComponent<NpcController>();
            npc.profile = profile;
            // 첫 인사 중에는 이웃이 멀리 배회하지 않는다. 밤 방문은 CONTENT-002 연결.
            npc.idleTickInterval = 99999f;
            npc.wanderRadius = 1f;
            npc.randomSeed = 10101;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            visual.name = "BoriVisual_C01";
            int materialIndex = 0;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    string path = $"Assets/Resources/Residents/Bori_Material_{materialIndex++}.mat";
                    Material variant = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (variant == null)
                    {
                        variant = new Material(materials[i]);
                        AssetDatabase.CreateAsset(variant, path);
                    }
                    if (variant.HasProperty("_BaseColor")) variant.SetColor("_BaseColor", new Color(0.65f, 0.85f, 0.9f));
                    EditorUtility.SetDirty(variant);
                    materials[i] = variant;
                }
                renderer.sharedMaterials = materials;
            }
            root.AddComponent<NpcPresentationNormalizer>();
            NpcPresentationNormalizer.Normalize(root);
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        Debug.Log("[CONTENT-001] BUILD_PASS dedicatedBori=true existingCastUnchanged=true sceneWrites=0");
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static TopicDialoguePool Pool(DialogueTopic topic, string line) => new TopicDialoguePool
    {
        topic = topic,
        thinkingLines = new List<string> { line },
        feelingLines = new List<string> { line }
    };

    static void AuditExistingCast()
    {
        Directory.CreateDirectory("Logs/Content/CONTENT001");
        var lines = new List<string>();
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Residents" })
                     .Select(AssetDatabase.GUIDToAssetPath).Where(path => path != PrefabPath).OrderBy(path => path))
        {
            string models = string.Join(",", AssetDatabase.GetDependencies(path).Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)));
            if (models.Contains("Character/C-01.fbx")) throw new InvalidOperationException("C-01 already assigned to a role resident.");
            lines.Add(path + " -> " + models);
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Prototype_FirstDay.unity", OpenSceneMode.Single);
        foreach (NpcDialogue npc in Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None))
            lines.Add($"Golden {npc.name}: friendship={npc.friendshipId}, profile={npc.ResolveProfile()?.name}");
        if (scene.isDirty) throw new InvalidOperationException("Read-only Golden audit dirtied the scene.");
        File.WriteAllLines("Logs/Content/CONTENT001/ExistingCastAudit.txt", lines);
    }
}
