#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  PA_TMPFontFixer — 한글·이모지 □ 박스 문제 해결
//  메뉴: P.A. System > 🔤 한글/이모지 폰트 자동 설정 (priority = 7)
//
//  배경:
//   기본 LiberationSans SDF 폰트는 한글·이모지 glyph 가 없어 모든 한국어 UI 가
//   □ (0x25A1) 박스로 렌더링된다. PA_UIBuilder 가 출력하는 "선택 아이템",
//   "닫기", "감사", "채용", "📱 ⚙ 📶 🔋" 등이 모두 깨진다.
//
//  해결 방법:
//   1. Windows 시스템 폰트(맑은 고딕 + Segoe UI Emoji)를 Assets/ 에 복사
//   2. Dynamic SDF 폰트 자산 생성 (런타임 글리프 추가 모드)
//   3. LiberationSans SDF 의 fallbackFontAssetTable 에 등록
//
//  Dynamic SDF 의 장점:
//   • 모든 한글 11,172자를 미리 굽지 않음 → 빌드 시간·아틀라스 크기 절약
//   • 사용된 글리프만 자동으로 atlas 에 추가됨
//   • 한 번 실행으로 영구 적용 (자산이 ProjectSettings 에 저장됨)
//
//  플랫폼 노트:
//   • 현재는 Windows 전용. Mac/Linux 사용자는 NotoSansKR.ttf 를 수동으로
//     Assets/Fonts/ 에 넣은 뒤 도구를 다시 실행하면 자동 인식.
//   • TMP SDF 는 컬러 이모지를 지원하지 않음 → seguiemj.ttf 의 단색 outline 만 표시
// ═══════════════════════════════════════════════════════════════════════════
public static class PA_TMPFontFixer
{
    // ── 경로 ──────────────────────────────────────────────────────────────────
    const string FONTS_DIR     = "Assets/Fonts";
    const string KO_SRC_PATH   = FONTS_DIR + "/PA_Korean_Src.ttf";
    const string EM_SRC_PATH   = FONTS_DIR + "/PA_Emoji_Src.ttf";
    const string KO_SDF_PATH   = FONTS_DIR + "/PA_Korean_SDF.asset";
    const string EM_SDF_PATH   = FONTS_DIR + "/PA_Emoji_SDF.asset";

    // 후보 시스템 폰트 (윈도우 11/10)
    static readonly string[] WIN_KO_FONTS =
    {
        @"C:\Windows\Fonts\malgun.ttf",      // 맑은 고딕 — 가장 호환성 높음
        @"C:\Windows\Fonts\malgunbd.ttf",    // Bold 변형
        @"C:\Windows\Fonts\NanumGothic.ttf", // 사용자가 설치한 경우
    };
    static readonly string[] WIN_EM_FONTS =
    {
        @"C:\Windows\Fonts\seguiemj.ttf",    // Segoe UI Emoji
        @"C:\Windows\Fonts\seguisym.ttf",    // Segoe UI Symbol (보조)
    };

    // ── 메뉴 진입 ─────────────────────────────────────────────────────────────
    [MenuItem("P.A. System/🔤 한글·이모지 폰트 자동 설정", priority = 7)]
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog("🔤 한글 폰트 자동 설정",
            "Windows 시스템 폰트를 가져와 한글·이모지 □ 박스 문제를 해결합니다.\n\n" +
            "처리 절차:\n" +
            "  1. 맑은 고딕 (malgun.ttf) → Assets/Fonts/ 복사\n" +
            "  2. Segoe UI Emoji (seguiemj.ttf) → Assets/Fonts/ 복사\n" +
            "  3. Dynamic SDF 폰트 자산 2개 생성\n" +
            "  4. LiberationSans SDF 의 fallback 으로 등록\n\n" +
            "한 번만 실행하면 모든 TMP 텍스트에 자동 적용됩니다.\n" +
            "(약 5초 소요)",
            "적용", "취소")) return;

        try
        {
            EditorUtility.DisplayProgressBar("한글 폰트 설정", "폴더 준비 중...", 0.05f);
            EnsureFolder("Assets", "Fonts");

            // 1. 시스템 폰트 → Assets 복사
            EditorUtility.DisplayProgressBar("한글 폰트 설정", "맑은 고딕 복사 중...", 0.15f);
            string copiedKo = CopyFirstAvailable(WIN_KO_FONTS, KO_SRC_PATH);
            EditorUtility.DisplayProgressBar("한글 폰트 설정", "Segoe UI Emoji 복사 중...", 0.30f);
            string copiedEm = CopyFirstAvailable(WIN_EM_FONTS, EM_SRC_PATH);
            AssetDatabase.Refresh();

            if (copiedKo == null && copiedEm == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("폰트 없음",
                    "Windows 시스템 폰트를 찾지 못했습니다.\n\n" +
                    "Mac/Linux 사용자는 NotoSansKR-Regular.ttf 같은 한글 폰트를\n" +
                    "Assets/Fonts/PA_Korean_Src.ttf 로 직접 복사 후 다시 실행하세요.",
                    "확인");
                return;
            }

            // 2. Dynamic SDF 자산 생성
            TMP_FontAsset koSdf = null, emSdf = null;
            if (copiedKo != null)
            {
                EditorUtility.DisplayProgressBar("한글 폰트 설정", "한글 SDF 생성 중...", 0.50f);
                koSdf = CreateDynamicSDF(copiedKo, KO_SDF_PATH);
            }
            if (copiedEm != null)
            {
                EditorUtility.DisplayProgressBar("한글 폰트 설정", "이모지 SDF 생성 중...", 0.70f);
                emSdf = CreateDynamicSDF(copiedEm, EM_SDF_PATH);
            }

            // 3. fallback 등록
            EditorUtility.DisplayProgressBar("한글 폰트 설정", "Fallback 등록 중...", 0.85f);
            int registered = RegisterFallbacks(koSdf, emSdf);

            // 4. 씬/리소스의 모든 TMP 텍스트 강제 새로고침
            EditorUtility.DisplayProgressBar("한글 폰트 설정", "TMP 텍스트 새로고침 중...", 0.95f);
            int refreshed = RefreshAllTMPText();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg =
                "🔤 한글 폰트 자동 설정 완료!\n\n" +
                $"  • 한글 SDF: {(koSdf != null ? "✅ 생성" : "❌ 실패")}\n" +
                $"  • 이모지 SDF: {(emSdf != null ? "✅ 생성" : "❌ 실패")}\n" +
                $"  • Fallback 등록: {registered}개 폰트\n" +
                $"  • TMP 텍스트 갱신: {refreshed}개\n\n" +
                "이제 Play 모드에서 한글이 정상 표시됩니다.\n" +
                "(이모지는 SDF 한계로 단색 outline 만 표시됩니다)";

            Debug.Log("[PA TMPFontFixer] " + msg.Replace("\n", "  "));
            EditorUtility.DisplayDialog("완료", msg, "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ PA_TMPFontFixer 실패: {e}");
            EditorUtility.DisplayDialog("실패",
                $"{e.Message}\n\nConsole 에서 상세 스택 트레이스를 확인하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 1. 시스템 폰트 복사
    //    여러 후보 중 처음 발견된 것을 Assets/Fonts/ 로 복사한다.
    // ──────────────────────────────────────────────────────────────────────────
    static string CopyFirstAvailable(string[] candidates, string destAssetPath)
    {
        // 이미 복사돼 있으면 그대로 사용 (재실행 시 빠름)
        if (File.Exists(destAssetPath))
        {
            Debug.Log($"  📁 이미 존재: {destAssetPath}");
            return destAssetPath;
        }

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
        Debug.LogWarning($"  ❌ 후보 폰트 모두 없음: {string.Join(", ", candidates.Select(Path.GetFileName))}");
        return null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Dynamic SDF 폰트 자산 생성
    //    AtlasPopulationMode.Dynamic = 사용된 글리프만 런타임에 자동 추가.
    //    한글 11,172자를 미리 굽지 않으므로 atlas 가 작고 빠르다.
    // ──────────────────────────────────────────────────────────────────────────
    static TMP_FontAsset CreateDynamicSDF(string srcAssetPath, string destAssetPath)
    {
        // 기존 자산이 있으면 그대로 반환 (재실행 호환)
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destAssetPath);
        if (existing != null)
        {
            Debug.Log($"  📁 이미 존재: {destAssetPath}");
            return existing;
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(srcAssetPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"  ❌ 소스 폰트 로드 실패: {srcAssetPath}");
            return null;
        }

        // CreateFontAsset 시그니처:
        //   (Font src, int samplingPx, int padding, GlyphRenderMode, int atlasW, int atlasH,
        //    AtlasPopulationMode, bool enableMultiAtlasSupport)
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize:    90,                                  // 권장값
            atlasPadding:         9,
            renderMode:           UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            atlasWidth:           2048,
            atlasHeight:          2048,
            atlasPopulationMode:  AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        AssetDatabase.CreateAsset(fontAsset, destAssetPath);
        EditorUtility.SetDirty(fontAsset);
        Debug.Log($"  🔤 Dynamic SDF 생성: {destAssetPath}");
        return fontAsset;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. LiberationSans SDF 의 fallback 등록
    //    TMP_Settings.defaultFontAsset.fallbackFontAssetTable 에 추가하면
    //    모든 TMP 텍스트가 자동으로 fallback 을 검색한다.
    //    (개별 텍스트의 font asset 을 교체할 필요 없음)
    // ──────────────────────────────────────────────────────────────────────────
    static int RegisterFallbacks(TMP_FontAsset koSdf, TMP_FontAsset emSdf)
    {
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            Debug.LogWarning("  ⚠️ TMP_Settings.defaultFontAsset 가 null. fallback 등록 스킵.");
            return 0;
        }

        if (defaultFont.fallbackFontAssetTable == null)
            defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

        int n = 0;
        if (koSdf != null && !defaultFont.fallbackFontAssetTable.Contains(koSdf))
        {
            defaultFont.fallbackFontAssetTable.Add(koSdf);
            n++;
        }
        if (emSdf != null && !defaultFont.fallbackFontAssetTable.Contains(emSdf))
        {
            defaultFont.fallbackFontAssetTable.Add(emSdf);
            n++;
        }

        EditorUtility.SetDirty(defaultFont);
        Debug.Log($"  🔗 fallback 등록: {n}개 (총 {defaultFont.fallbackFontAssetTable.Count}개)");
        return n;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. 씬·프리팹의 모든 TMP_Text 강제 재렌더
    //    fallback 변경은 즉시 반영되지 않을 수 있으므로 SetVerticesDirty 호출.
    // ──────────────────────────────────────────────────────────────────────────
    static int RefreshAllTMPText()
    {
        int n = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null) continue;
            // 에디터에서만 강제 재배치
            t.SetAllDirty();
            n++;
        }
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
