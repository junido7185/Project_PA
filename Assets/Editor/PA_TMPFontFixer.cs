#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ═══════════════════════════════════════════════════════════════════════════
//  PA_TMPFontFixer (v3) — Jalnan2 폰트 전면 적용
//  메뉴: P.A. System > 🔤 한글/이모지 폰트 자동 설정 (priority = 7)
//
//  변경 이력:
//   v1 — 시스템 맑은고딕 fallback. 한글 일부 텍스트 미반영.
//   v2 — sub-asset 영구 저장 패치. MissingReferenceException 폭주 해결.
//   v3 — 사용자 지정 Jalnan2TTF.ttf 를 메인 폰트로 사용. fallback 이 아닌
//        TMP_Settings.defaultFontAsset 자체를 Jalnan2 SDF 로 교체.
//        씬 + 프리팹 + ScriptableObject 의 모든 TMP_Text 를 일괄 강제 갱신.
//
//  설계 의도 (Docs/UI_스프라이트_가이드.md):
//   • Jalnan2 = 굵고 둥근 한글 캐주얼 디스플레이 폰트 — 동물의 숲 톤 매칭
//   • LiberationSans SDF 가 한글을 못 그리는 근본 문제를 자산 교체로 해결
//   • Segoe UI Emoji 는 fallback 으로만 추가 (TMP SDF 컬러 이모지 미지원이지만
//     단색 outline 표시 가능)
// ═══════════════════════════════════════════════════════════════════════════
public static class PA_TMPFontFixer
{
    // ── 경로 ──────────────────────────────────────────────────────────────────
    const string FONTS_DIR     = "Assets/Fonts";
    const string KO_SRC_PATH   = FONTS_DIR + "/Jalnan2TTF.ttf";       // 사용자 제공 — 절대 변경 금지
    const string EM_SRC_PATH   = FONTS_DIR + "/PA_Emoji_Src.ttf";     // Segoe UI Emoji 복사본
    const string KO_SDF_PATH   = FONTS_DIR + "/Jalnan2_SDF.asset";    // 메인 폰트 (defaultFontAsset)
    const string EM_SDF_PATH   = FONTS_DIR + "/PA_Emoji_SDF.asset";   // 이모지 fallback

    // 예전 (v1/v2) 가 만든 폐기 자산 — 발견 시 자동 삭제
    static readonly string[] LEGACY_PATHS =
    {
        FONTS_DIR + "/PA_Korean_SDF.asset",
        FONTS_DIR + "/PA_Korean_Src.ttf",
    };

    static readonly string[] WIN_EM_FONTS =
    {
        @"C:\Windows\Fonts\seguiemj.ttf",
        @"C:\Windows\Fonts\seguisym.ttf",
    };

    // ── 메뉴 진입 ─────────────────────────────────────────────────────────────
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog("🔤 Jalnan2 한글 폰트 전면 적용 (v3)",
            "Assets/Fonts/Jalnan2TTF.ttf 를 메인 SDF 폰트로 등록합니다.\n\n" +
            "처리 절차:\n" +
            "  1. 구버전(PA_Korean_*) 잔여 자산 자동 삭제\n" +
            "  2. Jalnan2_SDF.asset 생성 (atlas/material 포함)\n" +
            "  3. TMP_Settings.defaultFontAsset 교체 → 신규 TMP 자동 적용\n" +
            "  4. 씬·프리팹·ScriptableObject 의 모든 TMP_Text 폰트 강제 교체\n" +
            "  5. Segoe UI Emoji 를 fallback 으로 등록 (선택)\n\n" +
            "끝까지 실행해도 약 5초.\n" +
            "이미 실행했어도 안전하게 재실행할 수 있습니다.",
            "적용", "취소")) return;

        try
        {
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "준비 중...", 0.05f);
            EnsureFolder("Assets", "Fonts");

            // 0. 구버전 잔여 자산 정리
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "구버전 자산 정리 중...", 0.10f);
            int legacyCleaned = CleanupLegacyAssets();

            // 1. Jalnan2 소스 검증
            if (!File.Exists(KO_SRC_PATH))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Jalnan2 폰트 없음",
                    $"필수 폰트 파일이 없습니다:\n  {KO_SRC_PATH}\n\n" +
                    "Jalnan2TTF.ttf 를 Assets/Fonts/ 에 넣은 뒤 다시 실행하세요.",
                    "확인");
                return;
            }

            // 2. 이모지 폰트 복사 (Windows 만, 실패해도 무시)
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "이모지 폰트 복사 중...", 0.20f);
            string copiedEm = CopyFirstAvailable(WIN_EM_FONTS, EM_SRC_PATH);
            AssetDatabase.Refresh();

            // 3. Dynamic SDF 생성
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "Jalnan2 SDF 생성 중...", 0.40f);
            var koSdf = CreateDynamicSDF(KO_SRC_PATH, KO_SDF_PATH);

            TMP_FontAsset emSdf = null;
            if (copiedEm != null)
            {
                EditorUtility.DisplayProgressBar("Jalnan2 적용", "이모지 SDF 생성 중...", 0.55f);
                emSdf = CreateDynamicSDF(copiedEm, EM_SDF_PATH);
            }

            if (koSdf == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("실패", "Jalnan2_SDF.asset 생성 실패. Console 확인.", "확인");
                return;
            }

            // 4. TMP 기본 폰트 자체를 교체
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "TMP 기본 폰트 교체 중...", 0.65f);
            ReplaceDefaultFont(koSdf, emSdf);

            // 5. 모든 TMP_Text 의 font 필드 강제 갱신
            EditorUtility.DisplayProgressBar("Jalnan2 적용", "씬·프리팹 일괄 갱신 중...", 0.85f);
            int sceneCount = ApplyToCurrentScene(koSdf);
            int prefabCount = ApplyToAllPrefabs(koSdf);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (SceneManager.GetActiveScene().IsValid())
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            string msg =
                "🔤 Jalnan2 한글 폰트 전면 적용 완료!\n\n" +
                $"  • 구버전 자산 정리: {legacyCleaned}개\n" +
                $"  • Jalnan2_SDF: ✅ ({KO_SDF_PATH})\n" +
                $"  • 이모지 SDF: {(emSdf != null ? "✅" : "⚠ 스킵")}\n" +
                $"  • 씬 TMP_Text 갱신: {sceneCount}개\n" +
                $"  • 프리팹 TMP_Text 갱신: {prefabCount}개\n\n" +
                "이제 Play 모드에서 한글이 Jalnan2 톤으로 표시됩니다.\n" +
                "(이모지는 SDF 한계로 단색 outline 만 표시됩니다)";

            Debug.Log("[PA TMPFontFixer v3] " + msg.Replace("\n", "  "));
            EditorUtility.DisplayDialog("완료", msg, "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ PA_TMPFontFixer v3 실패: {e}");
            EditorUtility.DisplayDialog("실패", $"{e.Message}\n\nConsole 확인.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 0. 구버전 자산 정리
    // ──────────────────────────────────────────────────────────────────────────
    static int CleanupLegacyAssets()
    {
        int n = 0;
        foreach (var p in LEGACY_PATHS)
        {
            if (File.Exists(p) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p) != null)
            {
                if (AssetDatabase.DeleteAsset(p))
                {
                    Debug.Log($"  🗑 구버전 자산 삭제: {p}");
                    n++;
                }
            }
        }
        // .meta 가 남아있을 수도 있으니 한 번 더 정리
        AssetDatabase.Refresh();
        return n;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 1. 시스템 폰트 복사 (이모지 전용)
    // ──────────────────────────────────────────────────────────────────────────
    static string CopyFirstAvailable(string[] candidates, string destAssetPath)
    {
        if (File.Exists(destAssetPath)) return destAssetPath;
        foreach (var src in candidates)
        {
            if (!File.Exists(src)) continue;
            try
            {
                File.Copy(src, destAssetPath, overwrite: true);
                Debug.Log($"  📥 {Path.GetFileName(src)} → {destAssetPath}");
                return destAssetPath;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"  ⚠️ 복사 실패 ({src}): {e.Message}");
            }
        }
        return null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Dynamic SDF 폰트 자산 생성 (sub-asset 영구 저장 — v2 패치 유지)
    // ──────────────────────────────────────────────────────────────────────────
    static TMP_FontAsset CreateDynamicSDF(string srcAssetPath, string destAssetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destAssetPath);
        if (existing != null)
        {
            if (IsFontAssetHealthy(existing))
            {
                Debug.Log($"  📁 정상 자산 재사용: {destAssetPath}");
                return existing;
            }
            Debug.LogWarning($"  🔧 깨진 자산 감지 — 삭제 후 재생성: {destAssetPath}");
            AssetDatabase.DeleteAsset(destAssetPath);
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(srcAssetPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"  ❌ 소스 폰트 로드 실패: {srcAssetPath}");
            return null;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize:    90,
            atlasPadding:         9,
            renderMode:           UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            atlasWidth:           2048,
            atlasHeight:          2048,
            atlasPopulationMode:  AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogError($"  ❌ TMP_FontAsset.CreateFontAsset 실패: {srcAssetPath}");
            return null;
        }

        AssetDatabase.CreateAsset(fontAsset, destAssetPath);

        // sub-asset 영구 저장 (v2 패치 — Texture/Material 이 메모리에서 GC 되지 않도록)
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex == null) continue;
                tex.name = $"Atlas {i}";
                if (!AssetDatabase.IsSubAsset(tex))
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }
        if (fontAsset.material != null && !AssetDatabase.IsSubAsset(fontAsset.material))
        {
            fontAsset.material.name = "Font Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(destAssetPath);

        Debug.Log($"  🔤 Dynamic SDF 생성 + sub-asset 저장: {destAssetPath}");
        return fontAsset;
    }

    static bool IsFontAssetHealthy(TMP_FontAsset font)
    {
        if (font == null) return false;
        if (font.material == null) return false;
        if (font.atlasTextures == null || font.atlasTextures.Length == 0) return false;
        if (font.atlasTextures[0] == null) return false;
        return true;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. TMP 기본 폰트 교체 — defaultFontAsset 자체를 Jalnan2 로 바꾼다.
    //    이것이 v3 의 핵심: fallback 이 아니라 *기본*. 새로 만들어지는 모든
    //    TMP_Text 는 Jalnan2 를 자동으로 사용한다.
    // ──────────────────────────────────────────────────────────────────────────
    static void ReplaceDefaultFont(TMP_FontAsset koSdf, TMP_FontAsset emSdf)
    {
        // TMP_Settings 는 ScriptableObject — SerializedObject 로 수정해야 영구 적용됨
        var settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("  ⚠️ TMP_Settings.instance 가 null. 기본 폰트 교체 스킵.");
            return;
        }

        using var so = new SerializedObject(settings);
        var fontProp = so.FindProperty("m_defaultFontAsset");
        if (fontProp != null)
        {
            fontProp.objectReferenceValue = koSdf;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            Debug.Log("  🎨 TMP_Settings.defaultFontAsset → Jalnan2_SDF");
        }
        else
        {
            // 백업: public property setter (있다면)
            try { TMP_Settings.defaultFontAsset = koSdf; }
            catch { Debug.LogWarning("  ⚠️ defaultFontAsset 교체 실패 (private)"); }
        }

        // 이모지 SDF 는 Jalnan2 의 fallback 으로 추가 (Jalnan2 가 이모지를 못 그리므로)
        if (emSdf != null && koSdf != null)
        {
            if (koSdf.fallbackFontAssetTable == null)
                koSdf.fallbackFontAssetTable = new List<TMP_FontAsset>();
            koSdf.fallbackFontAssetTable.RemoveAll(f => f == null || !IsFontAssetHealthy(f));
            if (!koSdf.fallbackFontAssetTable.Contains(emSdf))
            {
                koSdf.fallbackFontAssetTable.Add(emSdf);
                EditorUtility.SetDirty(koSdf);
                Debug.Log("  🔗 Jalnan2 → Emoji fallback 등록");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. 씬·프리팹 일괄 갱신
    //    이미 LiberationSans SDF 가 박혀있는 기존 TMP_Text 들도 Jalnan2 로 교체.
    // ──────────────────────────────────────────────────────────────────────────
    static int ApplyToCurrentScene(TMP_FontAsset target)
    {
        int n = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null) continue;
            // 프리팹 자산 자체는 ApplyToAllPrefabs 가 처리
            if (PrefabUtility.IsPartOfPrefabAsset(t)) continue;
            if (!t.gameObject.scene.IsValid()) continue;

            if (t.font != target)
            {
                t.font = target;
                t.SetAllDirty();
                EditorUtility.SetDirty(t);
                n++;
            }
        }
        Debug.Log($"  🔁 씬 TMP_Text 갱신: {n}개");
        return n;
    }

    static int ApplyToAllPrefabs(TMP_FontAsset target)
    {
        int n = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool changed = false;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                if (t.font != target)
                {
                    t.font = target;
                    t.SetAllDirty();
                    changed = true;
                    n++;
                }
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"  🔁 프리팹 TMP_Text 갱신: {n}개");
        return n;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ──────────────────────────────────────────────────────────────────────────
    static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
