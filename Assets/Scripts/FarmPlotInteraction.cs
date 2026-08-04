using UnityEngine;

[RequireComponent(typeof(Farmland), typeof(Collider))]
public class FarmPlotInteraction : MonoBehaviour, IInteractable
{
    public const int RuntimePlotCount = 2;

    [Header("고정 밭")]
    public string plotId = "farm-plot-1";
    public string displayName = "마을 밭";
    public string seedResourcePath = "Items/Item_15_Seed";
    public string harvestResourcePath = "Items/Item_Wheat";
    [Min(1)] public int seedCost = 1;
    [Min(1)] public int harvestCount = 3;
    [Min(0.1f)] public float growthSecondsPerStage = 6f;

    Farmland _land;
    PrototypeWorldLabel _label;
    string _lastLabelText;

    public Farmland Land => _land != null ? _land : (_land = GetComponent<Farmland>());
    public Crop CurrentCrop => Land != null ? Land.CurrentCrop : null;

    void Awake()
    {
        _land = GetComponent<Farmland>();
        EnsureLabel();
        RefreshLabel();
    }

    void Update()
    {
        RefreshLabel();
    }

    public void Configure(string id, string label)
    {
        if (!string.IsNullOrWhiteSpace(id)) plotId = id.Trim();
        if (!string.IsNullOrWhiteSpace(label)) displayName = label.Trim();
        EnsureLabel();
        RefreshLabel();
    }

    public void Interact(GameObject interactor)
    {
        if (!IsDayPreparation())
        {
            ShowFeedback("농사는 낮 준비 시간에 할 수 있어요.");
            return;
        }

        if (Inventory.instance == null)
        {
            ShowFeedback("가방을 확인할 수 없어 밭을 사용할 수 없어요.");
            return;
        }

        Crop crop = CurrentCrop;
        if (crop == null)
        {
            TryPlant();
            return;
        }

        if (!crop.isFullyGrown)
        {
            ShowFeedback($"Wheat가 자라는 중이에요 · {crop.CurrentStageNumber}/{crop.StageCount}단계");
            RefreshLabel();
            return;
        }

        Item harvest = crop.harvestItem;
        int count = Mathf.Max(1, crop.harvestCount);
        if (harvest == null || !Inventory.instance.CanAddItems(harvest, count))
        {
            ShowFeedback($"Wheat x{count}을 담을 가방 공간이 필요해요.");
            return;
        }

        if (!crop.TryHarvest(Inventory.instance))
        {
            ShowFeedback("수확하지 못했어요. 가방과 작물 상태를 확인하세요.");
            return;
        }

        ShowFeedback($"수확 완료 · Wheat x{count}");
        RefreshLabel();
    }

    public string GetInteractPrompt()
    {
        if (!IsDayPreparation())
            return $"{displayName}: 낮에 돌보기";

        Crop crop = CurrentCrop;
        if (crop == null)
        {
            Item seed = ResolveSeed();
            int owned = seed != null && Inventory.instance != null ? Inventory.instance.CountItems(seed) : 0;
            return owned >= seedCost
                ? $"{displayName}: 씨앗 {owned}/{seedCost} · 심기"
                : $"{displayName}: 씨앗 필요 {owned}/{seedCost}";
        }

        if (crop.isFullyGrown)
            return $"{displayName}: Wheat x{Mathf.Max(1, crop.harvestCount)} 수확하기";

        return $"{displayName}: 성장 중 {crop.CurrentStageNumber}/{crop.StageCount}";
    }

    void TryPlant()
    {
        Item seed = ResolveSeed();
        Item harvest = Resources.Load<Item>(harvestResourcePath);
        GameObject cropPrefab = seed != null ? seed.cropPrefab : null;

        if (seed == null || harvest == null || cropPrefab == null || cropPrefab.GetComponent<Crop>() == null)
        {
            ShowFeedback("씨앗과 Wheat 작물 데이터가 아직 연결되지 않았어요.");
            return;
        }

        int owned = Inventory.instance.CountItems(seed);
        if (owned < seedCost)
        {
            ShowFeedback($"씨앗이 필요해요 · 보유 {owned}/{seedCost}");
            return;
        }

        if (Land == null || !Land.Plant(cropPrefab))
        {
            ShowFeedback("이 밭에는 지금 심을 수 없어요.");
            return;
        }

        Crop crop = CurrentCrop;
        if (crop == null)
        {
            Land.ClearLand();
            ShowFeedback("작물 생성에 실패했어요. 씨앗은 차감하지 않았습니다.");
            return;
        }

        crop.Configure(harvest, harvestCount, growthSecondsPerStage, "PA_DemoProps/Prop_Wheat");
        Inventory.instance.RemoveItems(seed, seedCost);
        ShowFeedback($"씨앗 x{seedCost}을 심었어요 · Wheat 성장 시작");
        RefreshLabel();
    }

    Item ResolveSeed()
    {
        return Resources.Load<Item>(seedResourcePath);
    }

    static bool IsDayPreparation()
    {
        return DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.DayPreparation;
    }

    void ShowFeedback(string message)
    {
        if (DialogueUI.instance != null)
            DialogueUI.instance.Show(displayName, message);
        else
            Debug.Log($"[FarmPlot] {plotId}: {message}");
    }

    void EnsureLabel()
    {
        if (_label != null) return;

        Transform existing = transform.Find("Label");
        GameObject labelGo = existing != null ? existing.gameObject : new GameObject("Label");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        _label = labelGo.GetComponent<PrototypeWorldLabel>() ?? labelGo.AddComponent<PrototypeWorldLabel>();
    }

    void RefreshLabel()
    {
        EnsureLabel();
        Crop crop = CurrentCrop;
        string state = crop == null
            ? "빈 밭"
            : crop.isFullyGrown ? "수확 가능" : $"성장 {crop.CurrentStageNumber}/{crop.StageCount}";
        string text = $"{displayName} · {state}";
        if (_lastLabelText == text) return;

        _lastLabelText = text;
        _label.Set(text,
            crop != null && crop.isFullyGrown ? new Color(1f, 0.90f, 0.48f) : new Color(0.82f, 0.92f, 0.68f),
            0.95f);
    }

    public static void EnsureRuntimePlots()
    {
        FarmPlotInteraction[] existing = FindObjectsByType<FarmPlotInteraction>(FindObjectsSortMode.None);
        if (existing.Length >= RuntimePlotCount) return;

        Transform anchor = ResolveFarmAnchor();
        Vector3 right = anchor != null ? anchor.right : Vector3.right;
        Vector3 forward = anchor != null ? anchor.forward : Vector3.forward;
        Vector3 center = anchor != null ? anchor.position : new Vector3(-15f, 0f, 10f);
        center = GroundAt(center - forward * 2.4f);

        for (int i = existing.Length; i < RuntimePlotCount; i++)
        {
            float side = (i - 0.5f) * 2.8f;
            CreateRuntimePlot(i, GroundAt(center + right * side));
        }
    }

    static Transform ResolveFarmAnchor()
    {
        GameObject anchor = GameObject.Find("WorkSpot_Farmer");
        if (anchor != null) return anchor.transform;

        ProducerNpcController[] producers = FindObjectsByType<ProducerNpcController>(FindObjectsSortMode.None);
        foreach (ProducerNpcController producer in producers)
            if (producer != null && producer.specialty == NpcSpecialty.Farmer)
                return producer.transform;

        return null;
    }

    static Vector3 GroundAt(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 8f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.03f;

        position.y = 0.03f;
        return position;
    }

    static void CreateRuntimePlot(int index, Vector3 position)
    {
        GameObject plot = new GameObject($"PA_FarmPlot_{index + 1}");
        plot.transform.position = position;

        MeshFilter filter = plot.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildTilledSoilMesh();
        MeshRenderer renderer = plot.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        renderer.material = new Material(shader) { color = new Color(0.34f, 0.23f, 0.14f, 1f) };

        BoxCollider collider = plot.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 0.45f, 0f);
        collider.size = new Vector3(2.35f, 0.9f, 2.45f);

        plot.AddComponent<Farmland>();
        FarmPlotInteraction interaction = plot.AddComponent<FarmPlotInteraction>();
        interaction.Configure($"farm-plot-{index + 1}", $"마을 밭 {index + 1}");
    }

    static Mesh BuildTilledSoilMesh()
    {
        const int xSegments = 10;
        const int zSegments = 8;
        const float width = 2.25f;
        const float depth = 2.35f;

        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        for (int z = 0; z <= zSegments; z++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;
                float v = z / (float)zSegments;
                float ridge = Mathf.Pow(Mathf.Sin(u * Mathf.PI * 5f), 2f) * 0.11f;
                int index = z * (xSegments + 1) + x;
                vertices[index] = new Vector3((u - 0.5f) * width, 0.02f + ridge, (v - 0.5f) * depth);
                uv[index] = new Vector2(u, v);
            }
        }

        int triangle = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * (xSegments + 1) + x;
                int b = a + 1;
                int c = a + xSegments + 1;
                int d = c + 1;
                triangles[triangle++] = a;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = b;
                triangles[triangle++] = c;
                triangles[triangle++] = d;
            }
        }

        Mesh mesh = new Mesh { name = "PA_TilledSoil_Runtime" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
