using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// BETA-006 production asset bridge.
// Keeps the original C-02~C-09 model assets untouched and builds lightweight,
// role-exact resident prefab wrappers that HiringService can instantiate in a
// scene which does not already contain a resident of that specialty.
public static class PA_HiringResidentTemplateBuilder
{
    const string OutputRoot = "Assets/Resources/Residents";

    readonly struct ResidentSpec
    {
        public readonly string Key;
        public readonly string ModelPath;
        public readonly NpcSpecialty Specialty;
        public readonly string ProductionAsset;

        public ResidentSpec(string key, string modelPath, NpcSpecialty specialty,
            string productionAsset = null)
        {
            Key = key;
            ModelPath = modelPath;
            Specialty = specialty;
            ProductionAsset = productionAsset;
        }
    }

    static readonly ResidentSpec[] Specs =
    {
        new ResidentSpec("Farmer", "Assets/Art/Character/C-02.fbx", NpcSpecialty.Farmer, "Production_Wheat"),
        new ResidentSpec("Lumberjack", "Assets/Art/Character/C-03.fbx", NpcSpecialty.Lumberjack, "Production_Wood"),
        new ResidentSpec("Miner", "Assets/Art/Character/C-04.fbx", NpcSpecialty.Miner, "Production_Ore"),
        new ResidentSpec("Fisher", "Assets/Art/Character/C-05.fbx", NpcSpecialty.Fisher, "Production_Fish"),
        new ResidentSpec("Chef", "Assets/Art/Character/C-06.fbx", NpcSpecialty.Chef),
        new ResidentSpec("Blacksmith", "Assets/Art/Character/C-07.fbx", NpcSpecialty.Blacksmith),
        new ResidentSpec("Tailor", "Assets/Art/Character/C-08.fbx", NpcSpecialty.Tailor),
        new ResidentSpec("Carpenter", "Assets/Art/Character/C-09.fbx", NpcSpecialty.Carpenter),
    };

    [MenuItem("Project PA/Beta/BETA-006/Build Hiring Resident Templates")]
    public static void BuildHiringResidentTemplates()
    {
        BuildAll(exitWhenDone: false);
    }

    public static void BuildHiringResidentTemplatesBatch()
    {
        BuildAll(exitWhenDone: true);
    }

    static void BuildAll(bool exitWhenDone)
    {
        try
        {
            EnsureOutputFolder();
            int linked = 0;
            foreach (ResidentSpec spec in Specs)
            {
                GameObject prefab = BuildPrefab(spec);
                NpcCandidateData candidate = AssetDatabase.LoadAssetAtPath<NpcCandidateData>(
                    $"Assets/Resources/Candidates/Candidate_{spec.Key}.asset");
                if (candidate == null)
                    throw new InvalidOperationException($"Missing candidate asset: Candidate_{spec.Key}");

                if (candidate.specialty != spec.Specialty)
                    throw new InvalidOperationException(
                        $"Candidate_{spec.Key} specialty mismatch: {candidate.specialty} != {spec.Specialty}");

                candidate.spawnPrefab = prefab;
                EditorUtility.SetDirty(candidate);
                linked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateLinks();
            Debug.Log($"[BETA-006] RESIDENT_TEMPLATES_PASS prefabs={Specs.Length} linked={linked} " +
                      "source=C-02..C-09 originalsUntouched=true");
            if (exitWhenDone && Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BETA-006] RESIDENT_TEMPLATES_FAIL {exception.Message}\n{exception}");
            if (exitWhenDone && Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static GameObject BuildPrefab(ResidentSpec spec)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
        if (model == null)
            throw new InvalidOperationException($"Missing resident model: {spec.ModelPath}");

        NpcCandidateData candidate = AssetDatabase.LoadAssetAtPath<NpcCandidateData>(
            $"Assets/Resources/Candidates/Candidate_{spec.Key}.asset");
        if (candidate == null || candidate.profile == null)
            throw new InvalidOperationException($"Candidate_{spec.Key} has no profile.");

        var root = new GameObject($"ResidentTemplate_{spec.Key}");
        try
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 2f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = NpcPresentationNormalizer.AgentStoppingDistance;
            agent.height = NpcPresentationNormalizer.AgentHeight;
            agent.radius = NpcPresentationNormalizer.AgentRadius;
            agent.baseOffset = 0f;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = NpcPresentationNormalizer.ColliderRadius;
            collider.height = NpcPresentationNormalizer.ColliderHeight;
            collider.center = Vector3.up * (NpcPresentationNormalizer.ColliderHeight * 0.5f);

            var normalizer = root.AddComponent<NpcPresentationNormalizer>();
            normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
            normalizer.animatorController = null;

            var consumer = root.AddComponent<NpcController>();
            consumer.profile = candidate.profile;
            consumer.idleTickInterval = 3f;
            consumer.wanderRadius = 8f;
            consumer.maxSlotsPerVisit = 3;
            consumer.browseDurationAtSlot = 1.5f;

            ProducerNpcController producer = null;
            SpecialistNpcController specialist = null;
            bool craftingRole = NpcSpecialtyMapping.IsCraftingSpecialty(spec.Specialty);
            if (craftingRole)
            {
                specialist = root.AddComponent<SpecialistNpcController>();
                specialist.profile = candidate.profile;
                specialist.specialty = spec.Specialty;
                specialist.assignedRecipes = MatchingRecipes(spec.Specialty);
                specialist.idleTickInterval = 5f;
                specialist.baseWorkProbability = 0.5f;
            }
            else
            {
                producer = root.AddComponent<ProducerNpcController>();
                producer.profile = candidate.profile;
                producer.specialty = spec.Specialty;
                producer.productionData = AssetDatabase.LoadAssetAtPath<ProductionData>(
                    $"Assets/Resources/Production/{spec.ProductionAsset}.asset");
                if (producer.productionData == null)
                    throw new InvalidOperationException(
                        $"Missing production data for {spec.Key}: {spec.ProductionAsset}");
                producer.idleTickInterval = 5f;
                producer.baseWorkProbability = 0.4f;
            }

            var schedule = root.AddComponent<NpcScheduleController>();
            schedule.profile = candidate.profile;
            schedule.scheduleData = AssetDatabase.LoadAssetAtPath<NpcDailySchedule>(
                craftingRole
                    ? "Assets/Resources/Schedules/Schedule_Specialist.asset"
                    : "Assets/Resources/Schedules/Schedule_Producer.asset");
            schedule.consumerController = consumer;
            schedule.producerController = producer;
            schedule.specialistController = specialist;

            var dialogue = root.AddComponent<NpcDialogue>();
            dialogue.dialogueData = AssetDatabase.LoadAssetAtPath<DialogueData>(
                $"Assets/Resources/Dialogues/Dialogue_{spec.Key}.asset");
            dialogue.overrideProfile = candidate.profile;
            dialogue.friendshipId = $"Candidate_{spec.Key}";

            GameObject visual = PrefabUtility.InstantiatePrefab(model, root.transform) as GameObject;
            if (visual == null) visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "CharacterVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(childCollider);

            NpcPresentationNormalizer.Normalize(root);
            if (root.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                throw new InvalidOperationException($"{spec.Key} wrapper has no SkinnedMeshRenderer.");

            string prefabPath = $"{OutputRoot}/Resident_{spec.Key}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (saved == null)
                throw new InvalidOperationException($"Failed to save resident prefab: {prefabPath}");
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static List<RecipeData> MatchingRecipes(NpcSpecialty specialty)
    {
        WorkbenchType workbench = NpcSpecialtyMapping.GetWorkbenchType(specialty);
        return AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/Resources/Recipes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<RecipeData>)
            .Where(recipe => recipe != null && recipe.requiredWorkbench == workbench)
            .OrderBy(recipe => recipe.name)
            .ToList();
    }

    static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputRoot))
            AssetDatabase.CreateFolder("Assets/Resources", "Residents");
    }

    static void ValidateLinks()
    {
        foreach (ResidentSpec spec in Specs)
        {
            NpcCandidateData candidate = AssetDatabase.LoadAssetAtPath<NpcCandidateData>(
                $"Assets/Resources/Candidates/Candidate_{spec.Key}.asset");
            if (candidate == null || candidate.spawnPrefab == null)
                throw new InvalidOperationException($"Candidate_{spec.Key} prefab link is missing.");
            if (candidate.spawnPrefab.GetComponent<NpcController>() == null ||
                candidate.spawnPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                throw new InvalidOperationException($"Candidate_{spec.Key} prefab is not a real resident template.");

            Component role = NpcSpecialtyMapping.IsCraftingSpecialty(spec.Specialty)
                ? candidate.spawnPrefab.GetComponent<SpecialistNpcController>()
                : candidate.spawnPrefab.GetComponent<ProducerNpcController>();
            if (role == null)
                throw new InvalidOperationException($"Candidate_{spec.Key} prefab has no role controller.");
        }
    }
}
