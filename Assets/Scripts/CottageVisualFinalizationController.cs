using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// B10 Cottage visual finalization sidecar.
// The Tripo source FBX, wrapper prefab, main scene, and BuildingEntrance contract stay unchanged.
[DefaultExecutionOrder(-420)]
public sealed class CottageVisualFinalizationController : MonoBehaviour
{
    const string ShopCottageName = "B10_Cottage_01";
    const string LegacyCottageName = "B10_Cottage_Static";
    const string ExteriorDoorName = "PA_StoreDoor_Out";
    const string OutsideSpawnName = "PlayerSpawn_Outside";
    const string VisualAnchorName = "B10_EntranceVisualAnchor";
    const string FinalSignName = "B10_FinalShopSign";
    const string FinalSignResource = "VisualFinalization/B10_Cottage_ShopSign";

    static readonly string[] MapCottageNames =
    {
        "B10_Cottage_01",
        "B10_Cottage_02",
        "B10_Cottage_03"
    };

    public static CottageVisualFinalizationController Instance { get; private set; }
    public bool IsApplied { get; private set; }
    public int FinalizedCottageCount { get; private set; }
    public bool LegacyDuplicateDisabled { get; private set; }
    public GameObject ExteriorDoor { get; private set; }
    public Transform FinalShopSign { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindFirstObjectByType<CottageVisualFinalizationController>() != null) return;
        if (GameObject.Find(ShopCottageName) == null) return;

        var host = new GameObject("PA_CottageVisualFinalizationController");
        GameObject services = GameObject.Find("[Services]");
        if (services != null) host.transform.SetParent(services.transform, false);
        host.AddComponent<CottageVisualFinalizationController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyFinalization();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool ApplyFinalization()
    {
        List<Transform> cottages = MapCottageNames
            .Select(GameObject.Find)
            .Where(go => go != null)
            .Select(go => go.transform)
            .ToList();
        if (cottages.Count == 0) return false;

        Shop plazaShop = PA_ShopLocator.FindPlazaShop();
        Vector3 plaza = plazaShop != null ? plazaShop.transform.position : Vector3.zero;

        foreach (Transform cottage in cottages)
            FaceAuthoredFacadeTowards(cottage, plaza);

        FinalizedCottageCount = cottages.Count;
        DisableLegacyDuplicate();
        FinalizeShopEntrance(cottages.FirstOrDefault(c => c.name == ShopCottageName));
        Physics.SyncTransforms();

        IsApplied = ExteriorDoor != null && FinalShopSign != null && FinalizedCottageCount == cottages.Count;
        Debug.Log(IsApplied
            ? $"🏠 [B10 Finalization] {FinalizedCottageCount}채 정면 정렬, 중복={LegacyDuplicateDisabled}, 실제 문+전용 간판 연결 완료"
            : "⚠ [B10 Finalization] Cottage는 정렬했지만 외부 문 또는 전용 간판 연결이 불완전합니다.");
        return IsApplied;
    }

    static void FaceAuthoredFacadeTowards(Transform cottage, Vector3 target)
    {
        Vector3 toTarget = target - cottage.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return;

        // B10's authored facade/door is local -X, not local -Z.
        Vector3 desiredForward = Vector3.Cross(Vector3.up, toTarget.normalized);
        cottage.rotation = Quaternion.LookRotation(desiredForward, Vector3.up);
    }

    void DisableLegacyDuplicate()
    {
        GameObject legacy = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t != null && t.name == LegacyCottageName)?.gameObject;
        if (legacy == null) return;
        legacy.SetActive(false);
        LegacyDuplicateDisabled = !legacy.activeSelf;
    }

    void FinalizeShopEntrance(Transform cottage)
    {
        if (cottage == null) return;

        GameObject door = GameObject.Find(ExteriorDoorName);
        if (door == null) return;
        ExteriorDoor = door;

        Bounds localVisual = CalculateLocalMeshBounds(cottage);
        Vector3 facadeLocal = new Vector3(localVisual.min.x - 0.06f, 0.5f, 0f);
        Vector3 outward = -cottage.right;
        door.transform.SetPositionAndRotation(
            cottage.TransformPoint(facadeLocal),
            Quaternion.LookRotation(outward, Vector3.up));

        // The authored B10 already has a modeled door. Keep only the interaction collider.
        MeshRenderer legacyDoorRenderer = door.GetComponent<MeshRenderer>();
        if (legacyDoorRenderer != null) legacyDoorRenderer.enabled = false;

        Transform legacySign = door.transform.Find("Sign");
        PrototypeWorldLabel label = door.GetComponentInChildren<PrototypeWorldLabel>(true);
        if (legacySign != null)
        {
            MeshRenderer legacySignRenderer = legacySign.GetComponent<MeshRenderer>();
            if (legacySignRenderer != null) legacySignRenderer.enabled = false;
        }

        Transform visualAnchor = door.transform.Find(VisualAnchorName);
        if (visualAnchor == null)
        {
            var anchor = new GameObject(VisualAnchorName);
            visualAnchor = anchor.transform;
            visualAnchor.SetParent(door.transform, false);
        }

        Vector3 signWorldPosition = cottage.TransformPoint(new Vector3(localVisual.min.x - 0.10f, 2.12f, 0f));
        visualAnchor.localPosition = door.transform.InverseTransformPoint(signWorldPosition);
        visualAnchor.localRotation = Quaternion.identity;
        Vector3 doorScale = door.transform.localScale;
        visualAnchor.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(doorScale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(doorScale.y)),
            1f / Mathf.Max(0.001f, Mathf.Abs(doorScale.z)));

        Transform finalSign = visualAnchor.Find(FinalSignName);
        if (finalSign == null)
        {
            GameObject signPrefab = Resources.Load<GameObject>(FinalSignResource);
            if (signPrefab != null)
            {
                GameObject instance = Instantiate(signPrefab, visualAnchor, false);
                instance.name = FinalSignName;
                finalSign = instance.transform;
            }
        }

        if (finalSign != null)
        {
            finalSign.localPosition = Vector3.zero;
            finalSign.localRotation = Quaternion.identity;
            finalSign.localScale = Vector3.one;
            FinalShopSign = finalSign;

            if (label != null)
            {
                // ShopEvolutionController deliberately discovers the tier label below the
                // exterior BuildingEntrance. Preserve that contract while matching the
                // separate, unscaled Cottage sign mesh in world space.
                label.transform.SetParent(finalSign, false);
                label.transform.localPosition = new Vector3(0f, 0f, 0.065f);
                // Fixed 3D signage must stay parallel to its plaque. Billboard rotation
                // intersects the plaque at oblique game-camera angles and hides half the text.
                label.enabled = false;
                label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                label.transform.localScale = Vector3.one * 0.56f;
                TextMeshPro text = label.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.rectTransform.sizeDelta = new Vector2(4.2f, 0.9f);
                    text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.margin = Vector4.zero;
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        if (legacySign != null) legacySign.gameObject.SetActive(false);

        GameObject outsideSpawn = GameObject.Find(OutsideSpawnName);
        if (outsideSpawn != null)
        {
            Vector3 position = door.transform.position + outward * 1.35f;
            position.y = cottage.position.y + 0.05f;
            outsideSpawn.transform.SetPositionAndRotation(position, Quaternion.LookRotation(outward, Vector3.up));
        }
    }

    static Bounds CalculateLocalMeshBounds(Transform root)
    {
        bool initialized = false;
        Bounds bounds = default;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Bounds meshBounds = mesh.bounds;
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;
            Vector3[] corners =
            {
                new(min.x, min.y, min.z), new(min.x, min.y, max.z),
                new(min.x, max.y, min.z), new(min.x, max.y, max.z),
                new(max.x, min.y, min.z), new(max.x, min.y, max.z),
                new(max.x, max.y, min.z), new(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!initialized)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else bounds.Encapsulate(local);
            }
        }

        if (!initialized)
            bounds = new Bounds(Vector3.up * 3f, new Vector3(5.5f, 6f, 5.9f));
        return bounds;
    }
}
