using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// §08 §4.11 — 건물 FBX 임포트 설정 일괄 보정 (역할: 임포트 옵션만)
//
// ⚠️ 역할 분리 (2026-05-12): Prefab/BuildingData/Blueprint 생성은 PA_ContentAutoIntegrator
// 가 전담한다 (Assets/Prefabs/Buildings/ 경로). 이 도구는 더 이상 prefab 을 만들지 않고,
// Tripo3D 출력의 FBX 임포트 단계 결함만 한 번에 정리한다.
//
// 보정 항목:
//   1️⃣ Animation Type: Humanoid (오감지) → None (정적 건물)
//   2️⃣ Keep Quads: ON  (self-intersecting 폴리곤 경고 완화)
//   3️⃣ Weld Vertices: OFF (Tripo3D 원본 정점 보존)
//   4️⃣ Read/Write Enabled: OFF (런타임 메모리 절약)
//   5️⃣ Material Search: Local 폴더 (.fbm 텍스처 자동 재사용)
[InitializeOnLoad]
public static class PA_BuildingImportFixer
{
    const string MODELS_DIR = "Assets/Models/Buildings";
    const string AUTO_KEY   = "PA_BuildingImportFixer_AutoRunV2"; // V2: prefab 로직 제거
    static readonly Regex BUILDING_NAME = new Regex(@"^B\d{2}_", RegexOptions.Compiled);

    // 첫 어셈블리 로드 시 1회만 자동 실행 (EditorPrefs 마커로 중복 차단)
    static PA_BuildingImportFixer()
    {
        if (EditorPrefs.GetBool(AUTO_KEY, false)) return;
        EditorApplication.delayCall += () =>
        {
            FixAllBuildings(silent: false);
            EditorPrefs.SetBool(AUTO_KEY, true);
        };
    }

    public static void FixAllBuildingsMenu() => FixAllBuildings(silent: false);

    public static void ResetAutoRunFlag()
    {
        EditorPrefs.DeleteKey(AUTO_KEY);
        Debug.Log("[BuildingFixer] AutoRun 마커 리셋 — 다음 컴파일에서 다시 자동 실행됩니다.");
    }

    public static void FixAllBuildings(bool silent)
    {
        if (!AssetDatabase.IsValidFolder(MODELS_DIR))
        {
            if (!silent) Debug.LogWarning($"[BuildingFixer] 폴더 없음: {MODELS_DIR} — 스킵.");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Model", new[] { MODELS_DIR });
        int importFixed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (!BUILDING_NAME.IsMatch(name)) continue;

                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

                bool changed = false;

                // 🔧 Rig — 정적 건물
                if (importer.animationType != ModelImporterAnimationType.None)
                { importer.animationType = ModelImporterAnimationType.None; changed = true; }
                if (importer.importAnimation)
                { importer.importAnimation = false; changed = true; }
                if (importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar)
                { importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar; changed = true; }

                // 🔧 Mesh — Tripo3D 결함 완화
                if (!importer.keepQuads)    { importer.keepQuads = true;    changed = true; }
                if ( importer.weldVertices) { importer.weldVertices = false; changed = true; }
                if ( importer.isReadable)   { importer.isReadable = false;   changed = true; }

                // 🔧 Material — 로컬 폴더에서 텍스처 검색
                if (importer.materialSearch != ModelImporterMaterialSearch.Local)
                { importer.materialSearch = ModelImporterMaterialSearch.Local; changed = true; }

                if (changed)
                {
                    importer.SaveAndReimport();
                    importFixed++;
                    if (!silent) Debug.Log($"[BuildingFixer] ✓ 임포트 보정: {name}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (!silent)
            Debug.Log($"[BuildingFixer] ✅ 완료 — FBX 임포트 보정 {importFixed}건 (Prefab 생성은 PA_ContentAutoIntegrator 가 담당)");
    }
}

// 신규 FBX 임포트 시 자동으로 같은 보정 적용
public class PA_BuildingPostprocessor : AssetPostprocessor
{
    static readonly Regex BUILDING_PATH = new Regex(
        @"^Assets/Models/Buildings/B\d{2}_[^/]+\.fbx$", RegexOptions.Compiled);

    void OnPreprocessModel()
    {
        if (!BUILDING_PATH.IsMatch(assetPath)) return;
        if (!(assetImporter is ModelImporter importer)) return;

        // 🔧 §08 §4.11 — 건물 FBX 표준 임포트 설정
        importer.animationType   = ModelImporterAnimationType.None;
        importer.importAnimation = false;
        importer.avatarSetup     = ModelImporterAvatarSetup.NoAvatar;
        importer.keepQuads       = true;
        importer.weldVertices    = false;
        importer.isReadable      = false;
        importer.materialSearch  = ModelImporterMaterialSearch.Local;
    }
}
