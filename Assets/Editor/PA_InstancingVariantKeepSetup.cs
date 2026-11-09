using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 2026-11-04 beach-r137/r138 실측: CoastalVegetationField가 FBX 내장 재질(URP/Lit)을 런타임에 복사해 enableInstancing을 켜지만,
// GraphicsSettings m_InstancingStripping=0(Strip Unused)이라 빌드에 인스턴싱 변형이 없다 → Windows 빌드에서 바위(물속·모래·물가)와
// 꽃잎 등 Green 계열이 아닌 인스턴스가 그려지지 않았다(에디터는 그려져 놓치기 쉬움). 같은 재질의 인스턴싱 사본을 Resources에 두어
// 빌드가 그 키워드 조합의 INSTANCING_ON 변형을 남기게 한다. 런타임 코드는 읽지 않는다(변형 보존 전용).
// 배치: Unity -batchmode -executeMethod PA_InstancingVariantKeepSetup.BatchApply
public static class PA_InstancingVariantKeepSetup
{
    const string Nature = "Assets/Art/Ultimate Nature Pack - Jun 2019/FBX/";
    const string Folder = "Assets/Resources/WorldArt/InstancingKeep";
    static readonly string[] Models = { "Rock_1", "Rock_2", "Rock_3", "Rock_Moss_1", "Grass", "Grass_2", "Grass_Short", "Flowers", "Bush_1", "Bush_2" };

    [MenuItem("Project PA/World/Keep Instancing Variants (Vegetation)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources/WorldArt", "InstancingKeep");
        var seen = new HashSet<Material>();
        var log = new List<string>();
        foreach (var name in Models)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Nature + name + ".fbx");
            if (prefab == null) { log.Add($"missing {name}"); continue; }
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach (var source in renderer.sharedMaterials)
            {
                if (source == null || !seen.Add(source)) continue;
                string path = $"{Folder}/PA_Keep_{source.name}.mat";
                var keep = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (keep == null) { keep = new Material(source); AssetDatabase.CreateAsset(keep, path); }
                else keep.CopyPropertiesFromMaterial(source);
                keep.shader = source.shader;
                keep.shaderKeywords = source.shaderKeywords;
                keep.enableInstancing = true;
                EditorUtility.SetDirty(keep);
                log.Add($"{name}:{source.name} shader={source.shader.name} keywords=[{string.Join(",", source.shaderKeywords)}] -> {path}");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[PA_InstancingVariantKeepSetup]\n" + string.Join("\n", log));
    }

    public static void BatchApply()
    {
        try { Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
