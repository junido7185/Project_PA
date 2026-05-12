#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the generated base villager set idempotent.
// Hired NPCs are intentionally excluded unless they live under [NPCs] with an NPC_* generated name.
public static class PA_NpcDuplicateGuard
{
    const string RootName = "[NPCs]";

    static readonly string[] BaseNpcKeys =
    {
        "Farmer",
        "Miner",
        "Lumberjack",
        "Fisher",
        "Chef",
        "Blacksmith",
        "Tailor",
        "Carpenter"
    };

    [MenuItem("P.A. System/NPC/Repair Generated NPC Duplicates", priority = 35)]
    public static void RepairGeneratedNpcDuplicatesMenu()
    {
        int removed = RepairGeneratedNpcDuplicates();
        EditorUtility.DisplayDialog(
            "P.A. NPC 중복 수리",
            $"자동 생성 기본 NPC 중복 {removed}개를 정리했습니다.\n\n고용 NPC는 [NPCs]/NPC_* 기본 주민 세트가 아니면 건드리지 않습니다.",
            "확인");
    }

    public static int PrepareForSceneAutoBuild()
    {
        int removed = 0;
        foreach (var npc in FindGeneratedBaseNpcs())
        {
            if (npc == null) continue;
            UnityEngine.Object.DestroyImmediate(npc);
            removed++;
        }

        foreach (var root in FindNpcRoots())
        {
            if (root == null) continue;
            UnityEngine.Object.DestroyImmediate(root);
            removed++;
        }

        if (removed > 0)
            Debug.Log($"[PA NPC Guard] SceneAutoBuilder 재실행 전 자동 NPC 루트 정리: {removed}개");
        return removed;
    }

    public static int RepairGeneratedNpcDuplicates()
    {
        int removed = 0;
        var keepRoot = EnsureSingleNpcRoot(ref removed);
        var groups = FindGeneratedBaseNpcs()
            .Select(go => new { GameObject = go, Key = ResolveBaseNpcKey(go) })
            .Where(x => x.GameObject != null && !string.IsNullOrEmpty(x.Key))
            .GroupBy(x => x.Key);

        foreach (var group in groups)
        {
            var ordered = group
                .Select(x => x.GameObject)
                .Distinct()
                .OrderByDescending(IsExactGeneratedName)
                .ThenByDescending(HasScheduleLinks)
                .ThenBy(GetHierarchyOrder)
                .ToList();

            if (ordered.Count == 0) continue;

            var keep = ordered[0];
            keep.name = $"NPC_{group.Key}";
            if (keepRoot != null && keep.transform.parent != keepRoot.transform)
                keep.transform.SetParent(keepRoot.transform, true);

            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] == null) continue;
                UnityEngine.Object.DestroyImmediate(ordered[i]);
                removed++;
            }
        }

        removed += RemoveEmptyExtraNpcRoots(keepRoot);

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[PA NPC Guard] 자동 생성 기본 NPC 중복 정리: {removed}개 제거");
        }

        return removed;
    }

    public static int CountGeneratedBaseNpcDuplicates()
    {
        return FindGeneratedBaseNpcs()
            .Select(ResolveBaseNpcKey)
            .Where(key => !string.IsNullOrEmpty(key))
            .GroupBy(key => key)
            .Sum(group => Mathf.Max(0, group.Count() - 1));
    }

    static GameObject EnsureSingleNpcRoot(ref int removed)
    {
        var roots = FindNpcRoots().ToList();
        if (roots.Count == 0) return new GameObject(RootName);

        var keep = roots
            .OrderByDescending(root => root.transform.childCount)
            .ThenBy(GetHierarchyOrder)
            .First();

        foreach (var root in roots)
        {
            if (root == keep) continue;
            while (root.transform.childCount > 0)
                root.transform.GetChild(0).SetParent(keep.transform, true);
            UnityEngine.Object.DestroyImmediate(root);
            removed++;
        }

        return keep;
    }

    static int RemoveEmptyExtraNpcRoots(GameObject keepRoot)
    {
        int removed = 0;
        foreach (var root in FindNpcRoots())
        {
            if (root == keepRoot) continue;
            if (root.transform.childCount > 0) continue;
            UnityEngine.Object.DestroyImmediate(root);
            removed++;
        }
        return removed;
    }

    static IEnumerable<GameObject> FindGeneratedBaseNpcs()
    {
        return Resources.FindObjectsOfTypeAll<NpcController>()
            .Where(npc => npc != null && IsSceneObject(npc.gameObject))
            .Select(npc => npc.gameObject)
            .Where(IsGeneratedBaseNpc)
            .Distinct();
    }

    static IEnumerable<GameObject> FindNpcRoots()
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go != null && go.name == RootName && IsSceneObject(go));
    }

    static bool IsGeneratedBaseNpc(GameObject go)
    {
        if (go == null) return false;
        string key = ResolveBaseNpcKey(go);
        if (string.IsNullOrEmpty(key)) return false;
        return IsExactGeneratedName(go) || HasAncestor(go.transform, RootName);
    }

    static string ResolveBaseNpcKey(GameObject go)
    {
        if (go == null) return null;

        var candidates = new List<string> { go.name };
        var npc = go.GetComponent<NpcController>();
        if (npc != null && npc.profile != null)
        {
            candidates.Add(npc.profile.name);
            candidates.Add(npc.profile.npcName);
        }

        var producer = go.GetComponent<ProducerNpcController>();
        if (producer != null)
            candidates.Add(producer.specialty.ToString());

        var specialist = go.GetComponent<SpecialistNpcController>();
        if (specialist != null)
            candidates.Add(specialist.specialty.ToString());

        foreach (string raw in candidates)
        {
            string normalized = Normalize(raw);
            if (string.IsNullOrEmpty(normalized)) continue;

            foreach (string key in BaseNpcKeys)
            {
                if (normalized.Contains(Normalize(key)))
                    return key;
            }
        }

        return null;
    }

    static bool IsExactGeneratedName(GameObject go)
    {
        string key = ResolveBaseNpcKey(go);
        return !string.IsNullOrEmpty(key)
            && string.Equals(CleanCloneSuffix(go.name), $"NPC_{key}", StringComparison.Ordinal);
    }

    static bool HasScheduleLinks(GameObject go)
    {
        var schedule = go.GetComponent<NpcScheduleController>();
        return schedule != null
            && schedule.scheduleData != null
            && (schedule.homePoint != null
                || schedule.producerController != null
                || schedule.specialistController != null);
    }

    static bool HasAncestor(Transform transform, string name)
    {
        for (var t = transform; t != null; t = t.parent)
            if (t.name == name) return true;
        return false;
    }

    static int GetHierarchyOrder(GameObject go)
    {
        if (go == null) return int.MaxValue;
        int order = go.transform.GetSiblingIndex();
        for (var t = go.transform.parent; t != null; t = t.parent)
            order += t.GetSiblingIndex() * 1000;
        return order;
    }

    static bool IsSceneObject(GameObject go)
    {
        return go != null
            && go.scene.IsValid()
            && go.scene.isLoaded
            && go.scene == SceneManager.GetActiveScene();
    }

    static string CleanCloneSuffix(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.Replace("(Clone)", "").Trim();
    }

    static string Normalize(string value)
    {
        return CleanCloneSuffix(value)
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }
}
#endif
