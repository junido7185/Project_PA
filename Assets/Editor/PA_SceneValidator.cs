using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// §7 PA_SceneValidator — 메뉴: P.A. System > Validate Scene
// 씬에서 필수 서비스 누락/중복, Inspector null 참조를 검사해 콘솔 경고를 출력한다.
public static class PA_SceneValidator
{
    [MenuItem("P.A. System/Validate Scene", priority = 10)]
    public static void Validate()
    {
        int warnings = 0;
        int ok       = 0;

        // ── 필수 싱글톤 서비스 존재 체크 ──────────────────────────────────────────
        var requiredSingletons = new[]
        {
            typeof(EconomyService),
            typeof(TierService),
            typeof(GameClock),
            typeof(ItemRegistry),
            typeof(FriendshipService),
            typeof(HiringService),
            typeof(GridService),
            typeof(PlayerInputHandler),
            typeof(AuditService),
            typeof(SaveManager),
        };

        foreach (var t in requiredSingletons)
        {
            var objs = Object.FindObjectsByType(t, FindObjectsSortMode.None);
            if (objs.Length == 0)
            {
                Debug.LogWarning($"[PA Validator] ❌ 필수 서비스 없음: {t.Name}");
                warnings++;
            }
            else if (objs.Length > 1)
            {
                Debug.LogWarning($"[PA Validator] ⚠️ 중복 인스턴스 {objs.Length}개: {t.Name}");
                warnings++;
            }
            else
            {
                ok++;
            }
        }

        // ── NpcDialogue: dialogueData 연결 여부 ─────────────────────────────────
        var dialogues = Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None);
        foreach (var nd in dialogues)
        {
            if (nd.dialogueData == null)
            {
                Debug.LogWarning($"[PA Validator] ⚠️ NpcDialogue '{nd.gameObject.name}' — dialogueData 미연결");
                warnings++;
            }
        }

        // ── HiringService: availableCandidates 비어있는지 ────────────────────────
        var hiring = Object.FindFirstObjectByType<HiringService>();
        if (hiring != null && (hiring.availableCandidates == null || hiring.availableCandidates.Count == 0))
        {
            Debug.LogWarning("[PA Validator] ⚠️ HiringService.availableCandidates 가 비어 있습니다. Candidates/ 에셋을 드래그하세요.");
            warnings++;
        }

        // ── SaveManager: allBuildingTypes 비어있는지 ────────────────────────────
        var saveManager = Object.FindFirstObjectByType<SaveManager>();
        if (saveManager != null && (saveManager.allBuildingTypes == null || saveManager.allBuildingTypes.Count == 0))
        {
            Debug.LogWarning("[PA Validator] ⚠️ SaveManager.allBuildingTypes 가 비어 있습니다. 건물 로드/저장이 동작하지 않습니다.");
            warnings++;
        }

        // ── ItemRegistry: allItems 비어있는지 ───────────────────────────────────
        var registry = Object.FindFirstObjectByType<ItemRegistry>();
        if (registry != null)
        {
            var items = registry.allItems;
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[PA Validator] ⚠️ ItemRegistry.allItems 가 비어 있습니다. Items/ 에셋을 드래그하세요.");
                warnings++;
            }
        }

        // ── 결과 요약 ─────────────────────────────────────────────────────────────
        string summary = warnings == 0
            ? $"✅ 씬 검증 완료 — 경고 없음 ({ok}개 서비스 정상)"
            : $"⚠️ 씬 검증 완료 — 경고 {warnings}건 (콘솔 확인)";

        Debug.Log($"[PA Validator] {summary}");
        EditorUtility.DisplayDialog("PA Scene Validator", summary, "확인");
    }
}
