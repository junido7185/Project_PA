using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Visual Demo Integration Pass v2 — 데모 장면 코지 드레싱 사이드카.
//
// 목적 (PROJECT_PA_CREATIVE_NORTH_STAR / Docs/08 아트·씬 구성 가이드):
// - "실제 플레이 카메라" 화면 기준으로 프로토타입 테스트맵 인상을 지운다.
//   v2 는 추측 좌표가 아니라 씬에 실제로 보이는 오브젝트(판매대 슬롯/허브/보급 상자)의
//   월드 위치를 앵커로 사용한다.
// - 바닥 구획: 판매 데크(슬롯 영역) / 허브 러그 / 광장 파빙 / 준비 구역 매트.
// - 판매대 슬롯: 원시 큐브 → 나무 카운터 + 상판 + 앞면 가격판.
// - 상점 앞 상품 진열대: 실제 아이템 아이콘 5종이 한눈에 보이는 쇼케이스.
// - 간판 정리: 영어 스테이징 텍스트(MANAGEMENT HUB) → "코지 잡화점".
//
// 안전 규칙 (VC-001A 런타임 사이드카 패턴 준수):
// - 씬 파일을 수정하지 않는다. 전부 런타임 생성, 렌더러 전용(콜라이더 제거).
// - Shop/ShopSlot/NPC/경제/저장 로직을 읽지도 바꾸지도 않는다. 위치 앵커로만 사용.
// - CoreSlicePresentationMode 의 개발용 이름 필터와 겹치지 않는 PA_DemoDressing_ 접두사를 쓴다.
public class DemoVisualDressingController : MonoBehaviour
{
    public const string RootName = "PA_DemoCozyDressing";

    [Header("Demo Cozy Dressing")]
    public bool autoDress = true;
    public float refreshInterval = 2.0f;

    // ── 코지 팔레트 (기존 PA_MarketStall_Hub 천막/목재 톤과 동일 계열) ──
    static readonly Color WoodDark    = new Color(0.42f, 0.27f, 0.16f, 1f);
    static readonly Color WoodLight   = new Color(0.62f, 0.42f, 0.24f, 1f);
    static readonly Color CrateWood   = new Color(0.72f, 0.51f, 0.28f, 1f);
    static readonly Color Cream       = new Color(0.93f, 0.87f, 0.74f, 1f);
    static readonly Color LeafGreen   = new Color(0.45f, 0.62f, 0.33f, 1f);
    static readonly Color PlanterSoil = new Color(0.30f, 0.22f, 0.15f, 1f);
    static readonly Color LanternGlow = new Color(1.00f, 0.83f, 0.45f, 1f);
    static readonly Color IronDark    = new Color(0.22f, 0.21f, 0.20f, 1f);

    // 바닥 구획 톤 (저채도 — 갈색 맨땅 위에서 은은하게 읽히는 정도)
    static readonly Color DeckWood  = new Color(0.62f, 0.48f, 0.31f, 1f);
    static readonly Color DeckTrim  = new Color(0.44f, 0.32f, 0.20f, 1f);
    static readonly Color RugWarm   = new Color(0.80f, 0.47f, 0.38f, 1f);
    static readonly Color PaverA    = new Color(0.78f, 0.72f, 0.60f, 1f);
    static readonly Color PaverB    = new Color(0.69f, 0.63f, 0.52f, 1f);
    static readonly Color PrepMat   = new Color(0.66f, 0.58f, 0.44f, 1f);

    static readonly Color[] FlowerColors =
    {
        new Color(0.91f, 0.51f, 0.65f, 1f), // 분홍
        new Color(0.95f, 0.83f, 0.40f, 1f), // 노랑
        new Color(0.66f, 0.52f, 0.86f, 1f), // 보라
        new Color(0.95f, 0.95f, 0.92f, 1f)  // 흰색
    };

    GameObject _root;
    bool _plazaDressed;
    bool _zonesDressed;
    bool _signageTidied;
    float _nextRefreshAt;
    readonly HashSet<string> _dressedPrepIds = new HashSet<string>();
    readonly HashSet<int> _dressedSlotIds = new HashSet<int>();
    readonly List<Transform> _billboards = new List<Transform>();
    readonly Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>();

    IEnumerator Start()
    {
        // DayNightShopLoopController.Start 가 채집 포인트/간판을 만든 뒤에 드레싱한다.
        yield return null;
        yield return null;
        RefreshDressing();
    }

    void Update()
    {
        if (!autoDress) return;
        if (Time.unscaledTime < _nextRefreshAt) return;
        _nextRefreshAt = Time.unscaledTime + Mathf.Max(0.5f, refreshInterval);
        RefreshDressing();
    }

    void LateUpdate()
    {
        // 아이템 아이콘 빌보드는 항상 카메라를 본다 (PrototypeWorldLabel 과 동일 규칙).
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var billboard in _billboards)
        {
            if (billboard == null) continue;
            Vector3 toCamera = billboard.position - cam.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
                billboard.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    public void RefreshDressing()
    {
        if (!autoDress) return;

        Shop shop = FindFirstObjectByType<Shop>();
        if (shop == null) return; // 상점 없는 씬에서는 아무것도 하지 않는다.

        EnsureRoot();
        DressPrepPoints();
        DressShopSign();
        DressShopSlots(shop);
        DressGroundZones(shop);
        DressPlaza(shop);
        TidySignage();
    }

    void EnsureRoot()
    {
        if (_root != null) return;

        var existing = GameObject.Find(RootName);
        _root = existing != null ? existing : new GameObject(RootName);
    }

    // 데모 상점의 판매대 슬롯만. 씬에 상점이 2개라 거리 필터는 두 상점을 모두
    // 포함해 데크가 거대해지는 버그가 있었음 → 해당 Shop 의 자식 슬롯을 우선 사용.
    List<ShopSlot> FindDemoSlots(Shop shop)
    {
        var result = new List<ShopSlot>(shop.GetComponentsInChildren<ShopSlot>(true));
        if (result.Count > 0) return result;

        foreach (var slot in FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot == null) continue;
            if (Vector3.Distance(slot.transform.position, shop.transform.position) <= 12f)
                result.Add(slot);
        }
        return result;
    }

    static Vector3 CenterOf(List<ShopSlot> slots, Vector3 fallback)
    {
        if (slots == null || slots.Count == 0) return fallback;
        Vector3 sum = Vector3.zero;
        foreach (var s in slots) sum += s.transform.position;
        return sum / slots.Count;
    }

    // 실측 지오메트리 — Shop 원점은 슬롯 부모라 방향 계산에 못 쓴다(3라운드 원인).
    // 판매대 행 방향(rowDir)과 "카운터 앞"(frontDir = 슬롯 중심→플레이어)을 실제 위치로 구한다.
    struct PlazaFrame
    {
        public Vector3 slotsCenter;
        public Vector3 rowDir;    // 판매대 행을 따라가는 방향
        public Vector3 frontDir;  // 카운터 앞(손님/플레이어 쪽) 방향
        public float groundY;     // 기준 지면 높이 (장애물 히트 클램프용)
    }

    PlazaFrame ResolvePlazaFrame(List<ShopSlot> slots, Vector3 fallbackAnchor)
    {
        var frame = new PlazaFrame();
        frame.slotsCenter = CenterOf(slots, fallbackAnchor);

        // 행 방향: 서로 가장 먼 슬롯 쌍.
        frame.rowDir = Vector3.right;
        float best = 0f;
        for (int i = 0; i < slots.Count; i++)
            for (int j = i + 1; j < slots.Count; j++)
            {
                Vector3 d = slots[j].transform.position - slots[i].transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > best) { best = d.sqrMagnitude; frame.rowDir = d.normalized; }
            }

        // 앞 방향: 슬롯 중심 → 플레이어 (행 방향 성분 제거).
        var playerGo = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Vector3 toPlayer = playerGo != null
            ? playerGo.transform.position - frame.slotsCenter
            : fallbackAnchor - frame.slotsCenter;
        toPlayer.y = 0f;
        toPlayer -= Vector3.Dot(toPlayer, frame.rowDir) * frame.rowDir;
        frame.frontDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector3.forward;

        // 기준 지면: 카운터 앞 빈 지점 레이캐스트 (플레이어/상자 위에 얹히는 것 방지용 기준값).
        Vector3 probe = frame.slotsCenter + frame.frontDir * 1.4f + frame.rowDir * 1.7f;
        Vector3 hit = SnapToGround(probe, frame.slotsCenter.y - 0.3f);
        frame.groundY = hit.y;
        return frame;
    }

    // 지면 스냅 + 기준 높이 클램프: 레이가 플레이어/소품 위를 맞으면 기준 지면으로 강제.
    Vector3 GroundAt(Vector3 target, float referenceY)
    {
        Vector3 snapped = SnapToGround(target, referenceY);
        if (Mathf.Abs(snapped.y - referenceY) > 0.9f)
            snapped.y = referenceY;
        return snapped;
    }

    // ── 1) 채집 포인트: 단색 큐브 → 나무 궤짝 + 작물 + 아이콘 빌보드 ────────────
    void DressPrepPoints()
    {
        foreach (var point in FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None))
        {
            if (point == null) continue;

            string id = string.IsNullOrWhiteSpace(point.activityId) ? point.name : point.activityId.Trim();
            if (_dressedPrepIds.Contains(id)) continue;

            string dressName = $"PA_DemoDressing_Prep_{id}";
            if (_root.transform.Find(dressName) != null)
            {
                _dressedPrepIds.Add(id);
                continue;
            }

            var dress = new GameObject(dressName);
            dress.transform.SetParent(_root.transform, false);
            dress.transform.position = point.transform.position;
            dress.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // 나무 궤짝 받침 (포인트 큐브를 감싸는 진열대 느낌)
            CreatePart(dress.transform, PrimitiveType.Cube, "Crate",
                new Vector3(0f, -0.18f, 0f), new Vector3(0.95f, 0.34f, 0.95f), CrateWood);
            CreatePart(dress.transform, PrimitiveType.Cube, "CrateRim",
                new Vector3(0f, -0.02f, 0f), new Vector3(1.02f, 0.07f, 1.02f), WoodDark);

            // 작물 표현 (아이템 색으로 구 3개)
            Color produce = ResolveProduceColor(point.itemResourcePath);
            CreatePart(dress.transform, PrimitiveType.Sphere, "Produce_A",
                new Vector3(-0.20f, 0.16f, 0.12f), Vector3.one * 0.30f, produce);
            CreatePart(dress.transform, PrimitiveType.Sphere, "Produce_B",
                new Vector3(0.18f, 0.14f, -0.10f), Vector3.one * 0.26f, produce);
            CreatePart(dress.transform, PrimitiveType.Sphere, "Produce_C",
                new Vector3(0.02f, 0.20f, 0.20f), Vector3.one * 0.22f, produce);

            // 아이템 아이콘 빌보드 (Resources 의 실제 아이콘 재사용, 소형 — 공중부양 느낌 방지)
            CreateItemIconBillboard(dress.transform, point.itemResourcePath, new Vector3(0f, 1.10f, 0f), 0.30f);

            _dressedPrepIds.Add(id);
        }
    }

    // ── 2) 영업 간판: 큐브 단독 → 나무 기둥 + 걸이대 + 랜턴 ────────────────────
    void DressShopSign()
    {
        var sign = FindFirstObjectByType<ShopOpenSign>();
        if (sign == null) return;

        const string dressName = "PA_DemoDressing_ShopSign";
        if (_root.transform.Find(dressName) != null) return;

        var dress = new GameObject(dressName);
        dress.transform.SetParent(_root.transform, false);
        dress.transform.position = sign.transform.position;
        dress.transform.rotation = sign.transform.rotation;

        CreatePart(dress.transform, PrimitiveType.Cylinder, "Post",
            new Vector3(0f, 0.25f, -0.14f), new Vector3(0.12f, 0.85f, 0.12f), WoodDark);
        CreatePart(dress.transform, PrimitiveType.Cube, "CrossBar",
            new Vector3(0f, 1.05f, -0.06f), new Vector3(0.78f, 0.09f, 0.09f), WoodLight);
        CreatePart(dress.transform, PrimitiveType.Cube, "BoardTrim",
            new Vector3(0f, 0.52f, 0.02f), new Vector3(0.70f, 1.00f, 0.10f), Cream);
        CreatePart(dress.transform, PrimitiveType.Sphere, "LanternGlow",
            new Vector3(0.44f, 1.00f, -0.02f), Vector3.one * 0.18f, LanternGlow, emissive: true);
    }

    // ── 3) 판매대 슬롯: 원시 큐브 → 나무 카운터 + 상판 + 앞면 가격판 ────────────
    void DressShopSlots(Shop shop)
    {
        var slots = FindDemoSlots(shop);
        if (slots.Count == 0) return;

        // 카운터 정면 = 손님/플레이어 쪽 (Shop 원점 기준 방향은 신뢰 불가 — PlazaFrame 실측 사용).
        Vector3 frontDir = ResolvePlazaFrame(slots, shop.transform.position).frontDir;

        foreach (var slot in slots)
        {
            int key = slot.GetInstanceID();
            if (_dressedSlotIds.Contains(key)) continue;

            string dressName = $"PA_DemoDressing_Slot_{key}";
            if (_root.transform.Find(dressName) != null)
            {
                _dressedSlotIds.Add(key);
                continue;
            }

            var dress = new GameObject(dressName);
            dress.transform.SetParent(_root.transform, false);
            dress.transform.position = slot.transform.position;
            dress.transform.rotation = Quaternion.LookRotation(frontDir, Vector3.up);

            // 카운터 본체 + 밝은 상판 + 하단 트림 (슬롯 큐브를 감싸 진열대로 읽히게)
            CreatePart(dress.transform, PrimitiveType.Cube, "CounterBody",
                new Vector3(0f, -0.10f, 0f), new Vector3(1.04f, 0.46f, 0.82f), WoodLight);
            CreatePart(dress.transform, PrimitiveType.Cube, "CounterTop",
                new Vector3(0f, 0.15f, 0f), new Vector3(1.12f, 0.05f, 0.90f),
                new Color(0.82f, 0.74f, 0.60f, 1f)); // 상판은 크림보다 한 톤 낮춰 형광 느낌 방지
            CreatePart(dress.transform, PrimitiveType.Cube, "CounterBase",
                new Vector3(0f, -0.30f, 0f), new Vector3(1.10f, 0.10f, 0.88f), WoodDark);

            // 앞면 소형 가격판 (디버그 텍스트가 아니라 월드 소품으로)
            CreatePart(dress.transform, PrimitiveType.Cube, "PriceBoard",
                new Vector3(0f, -0.02f, 0.47f), new Vector3(0.34f, 0.26f, 0.03f), Cream);
            CreatePart(dress.transform, PrimitiveType.Cube, "PriceBoardTrim",
                new Vector3(0f, -0.02f, 0.455f), new Vector3(0.38f, 0.30f, 0.02f), WoodDark);

            _dressedSlotIds.Add(key);
        }
    }

    // ── 4) 바닥 구획: 판매 데크 / 허브 러그 / 광장 파빙 / 준비 매트 ─────────────
    void DressGroundZones(Shop shop)
    {
        if (_zonesDressed) return;

        const string dressName = "PA_DemoDressing_GroundZones";
        if (_root.transform.Find(dressName) != null)
        {
            _zonesDressed = true;
            return;
        }

        var slots = FindDemoSlots(shop);
        if (slots.Count == 0) return; // 슬롯이 아직 없으면 다음 refresh 에서 재시도.

        var zones = new GameObject(dressName);
        zones.transform.SetParent(_root.transform, false);

        PlazaFrame frame = ResolvePlazaFrame(slots, shop.transform.position);
        Quaternion frontRot = Quaternion.LookRotation(frame.frontDir, Vector3.up);

        // 4-0) 광장 베이스 플레이트 — 갈색 맨땅이 화면의 절반을 차지하는 것이
        // 테스트맵 인상의 최대 원인이므로, 판매대 앞(플레이어 쪽) 광장 전체를 밝은 석재 톤으로 덮는다.
        // 플레이어 시작 지점 뒤까지 커버하도록 깊이를 넉넉히 잡는다 (after4: 하단에 흙 노출).
        Vector3 plazaCenter = GroundAt(frame.slotsCenter + frame.frontDir * 5.5f, frame.groundY);
        var basePlate = CreatePart(zones.transform, PrimitiveType.Cube, "PlazaBasePlate",
            plazaCenter + Vector3.up * 0.006f, new Vector3(30f, 0.012f, 30f),
            new Color(0.60f, 0.55f, 0.46f, 1f), worldSpace: true);
        basePlate.transform.rotation = frontRot;

        // 4-1) 판매 데크 — 슬롯 열 전체를 받치는 나무 단.
        Bounds slotBounds = new Bounds(slots[0].transform.position, Vector3.zero);
        foreach (var s in slots) slotBounds.Encapsulate(s.transform.position);
        // 오프셋 계층: 플레이트(+0.006) 위에 얇은 구획들이 묻히지 않도록 층을 나눈다 (after4 원인 수정).
        Vector3 deckCenter = GroundAt(slotBounds.center, frame.groundY);
        float rowLength = Mathf.Max(slotBounds.size.x, slotBounds.size.z) + 2.4f;
        var deck = CreatePart(zones.transform, PrimitiveType.Cube, "SalesDeck",
            deckCenter + Vector3.up * 0.045f, new Vector3(rowLength, 0.06f, 3.0f), DeckWood, worldSpace: true);
        deck.transform.rotation = Quaternion.LookRotation(frame.frontDir, Vector3.up);
        var deckTrim = CreatePart(zones.transform, PrimitiveType.Cube, "SalesDeckTrim",
            deckCenter + Vector3.up * 0.030f, new Vector3(rowLength + 0.3f, 0.04f, 3.3f), DeckTrim, worldSpace: true);
        deckTrim.transform.rotation = deck.transform.rotation;

        // 4-2) 카운터 앞 러그 — 손님이 서는 자리를 따뜻한 판매 공간으로 구획.
        Vector3 rugCenter = GroundAt(frame.slotsCenter + frame.frontDir * 2.5f, frame.groundY) + Vector3.up * 0.055f;
        var rug = CreatePart(zones.transform, PrimitiveType.Cube, "HubRug",
            rugCenter, new Vector3(4.6f, 0.03f, 2.2f), RugWarm, worldSpace: true);
        rug.transform.rotation = frontRot;

        // 4-3) 광장 파빙 — 러그에서 플레이어 쪽으로 이어지는 밝은 석재 타일 동선.
        for (int r = 0; r < 4; r++)
        {
            for (int c = -2; c <= 2; c++)
            {
                Vector3 tilePos = frame.slotsCenter
                    + frame.frontDir * (4.0f + r * 1.05f)
                    + frame.rowDir * (c * 1.05f);
                tilePos = GroundAt(tilePos, frame.groundY) + Vector3.up * 0.040f;
                var tile = CreatePart(zones.transform, PrimitiveType.Cube, $"Paver_{r}_{c}",
                    tilePos, new Vector3(0.95f, 0.025f, 0.95f), ((r + c) & 1) == 0 ? PaverA : PaverB,
                    worldSpace: true);
                tile.transform.rotation = frontRot;
            }
        }

        // 4-4) 준비 구역 매트 — 보급 상자 주변 (씬 오브젝트 Support_Crate 앵커, 밝은 모래톤 소형).
        var supportCrate = GameObject.Find("Support_Crate");
        if (supportCrate != null)
        {
            Vector3 matCenter = GroundAt(supportCrate.transform.position, frame.groundY) + Vector3.up * 0.045f;
            CreatePart(zones.transform, PrimitiveType.Cube, "PrepMat",
                matCenter, new Vector3(3.2f, 0.03f, 2.6f), PrepMat, worldSpace: true);
        }

        _zonesDressed = true;
        var playerGo = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Debug.Log($"🧱 [DemoDressing] 바닥 구획 완료 — slotsCenter={frame.slotsCenter}, rowDir={frame.rowDir}, "
            + $"frontDir={frame.frontDir}, groundY={frame.groundY:0.00}, plaza={basePlate.transform.position}, "
            + $"player={(playerGo != null ? playerGo.transform.position.ToString() : "?")}");
    }

    // ── 5) 광장 소품: 상품 쇼케이스 / 화단 / 가로등 / 벤치 / 궤짝 ───────────────
    void DressPlaza(Shop shop)
    {
        if (_plazaDressed) return;

        const string dressName = "PA_DemoDressing_Plaza";
        if (_root.transform.Find(dressName) != null)
        {
            _plazaDressed = true;
            return;
        }

        var slots = FindDemoSlots(shop);
        if (slots.Count == 0) return;

        var plaza = new GameObject(dressName);
        plaza.transform.SetParent(_root.transform, false);

        PlazaFrame frame = ResolvePlazaFrame(slots, shop.transform.position);
        Vector3 slotsCenter = frame.slotsCenter;
        Vector3 rowDir = frame.rowDir;
        Vector3 frontDir = frame.frontDir;

        // 판매대 행의 실제 절반 길이 (소품을 행 양끝에 붙이기 위함, 프레임 이탈 방지 캡).
        Bounds slotBounds = new Bounds(slots[0].transform.position, Vector3.zero);
        foreach (var s in slots) slotBounds.Encapsulate(s.transform.position);
        float rowHalf = Mathf.Min(Mathf.Max(slotBounds.extents.x, slotBounds.extents.z) + 1.2f, 3.6f);

        // 5-1) 상품 쇼케이스 — 준비 구역(보급 상자) 옆, 카운터 앞(손님 쪽)을 바라보게.
        var supportCrate = GameObject.Find("Support_Crate");
        Vector3 showcasePos = supportCrate != null
            ? supportCrate.transform.position + frontDir * 2.2f
            : slotsCenter - rowDir * (rowHalf + 1.4f);
        CreateShowcase(plaza.transform,
            GroundAt(showcasePos, frame.groundY),
            Quaternion.LookRotation(frontDir, Vector3.up));

        // 5-2) 화단 — 열린 광장 위 (판매대 행 양끝은 씬 배치 박스에 가려짐 → 러그/파빙 옆으로).
        Vector3[] planterSpots =
        {
            slotsCenter + frontDir * 1.2f - rowDir * 3.2f,
            slotsCenter + frontDir * 1.2f + rowDir * 3.2f,
            slotsCenter + frontDir * 5.2f - rowDir * 3.0f,
            slotsCenter + frontDir * 5.2f + rowDir * 3.0f
        };
        for (int i = 0; i < planterSpots.Length; i++)
            CreatePlanter(plaza.transform, GroundAt(planterSpots[i], frame.groundY), i);

        // 5-3) 가로등 — 러그 양옆 열린 자리 (저녁 영업의 따뜻한 톤, 시야 안).
        CreateLanternPost(plaza.transform, GroundAt(slotsCenter + frontDir * 3.0f - rowDir * 3.4f, frame.groundY));
        CreateLanternPost(plaza.transform, GroundAt(slotsCenter + frontDir * 3.0f + rowDir * 3.4f, frame.groundY));

        // 5-4) 궤짝 더미 + 통 — 준비 매트 위 (빈 매트가 아니라 물류 코너로 읽히게).
        if (supportCrate != null)
        {
            CreateCrateStack(plaza.transform, GroundAt(supportCrate.transform.position + rowDir * 1.3f, frame.groundY));
            Vector3 barrelPos = GroundAt(supportCrate.transform.position - rowDir * 1.2f + frontDir * 0.6f, frame.groundY);
            CreatePart(plaza.transform, PrimitiveType.Cylinder, "Barrel",
                barrelPos + Vector3.up * 0.34f, new Vector3(0.52f, 0.34f, 0.52f), WoodLight, worldSpace: true);
        }
        else
        {
            CreateCrateStack(plaza.transform, GroundAt(showcasePos + rowDir * 2.2f, frame.groundY));
        }

        // 5-5) 분수 벤치 — 분수가 있으면 광장 중심으로 유지.
        GameObject fountain = GameObject.Find("B11_PlazaFountain_Static") ?? GameObject.Find("B11_PlazaFountain");
        if (fountain != null)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = 55f + i * 115f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 pos = GroundAt(fountain.transform.position + dir * 2.6f, frame.groundY);
                CreateBench(plaza.transform, pos, Quaternion.LookRotation(-dir, Vector3.up));
            }
        }

        _plazaDressed = true;
        Debug.Log("🪑 [DemoDressing] 광장 소품 완료: 쇼케이스/화단 4/가로등 2/궤짝/벤치");
    }

    // 상품 쇼케이스: 나무 진열대 + 아이템 아이콘 5종 + 작물 더미.
    void CreateShowcase(Transform parent, Vector3 position, Quaternion rotation)
    {
        var showcase = new GameObject("PA_DemoDressing_Showcase");
        showcase.transform.SetParent(parent, false);
        showcase.transform.SetPositionAndRotation(position, rotation);

        CreatePart(showcase.transform, PrimitiveType.Cube, "TableTop",
            new Vector3(0f, 0.72f, 0f), new Vector3(2.30f, 0.08f, 0.72f), WoodLight);
        CreatePart(showcase.transform, PrimitiveType.Cube, "TableApron",
            new Vector3(0f, 0.56f, 0f), new Vector3(2.20f, 0.24f, 0.62f), WoodDark);
        CreatePart(showcase.transform, PrimitiveType.Cube, "LegL",
            new Vector3(-0.95f, 0.28f, 0f), new Vector3(0.12f, 0.56f, 0.5f), WoodDark);
        CreatePart(showcase.transform, PrimitiveType.Cube, "LegR",
            new Vector3(0.95f, 0.28f, 0f), new Vector3(0.12f, 0.56f, 0.5f), WoodDark);

        string[] itemPaths =
        {
            "Items/Item_BreadLoaf", "Items/Item_Carrot", "Items/Item_Fish",
            "Items/Item_Wheat", "Items/Item_Ore"
        };

        for (int i = 0; i < itemPaths.Length; i++)
        {
            float x = -0.88f + i * 0.44f;
            Color produce = ResolveProduceColor(itemPaths[i]);
            CreatePart(showcase.transform, PrimitiveType.Sphere, $"Goods_{i}",
                new Vector3(x, 0.86f, 0f), Vector3.one * 0.24f, produce);
            CreateItemIconBillboard(showcase.transform, itemPaths[i],
                new Vector3(x, 1.08f, 0f), 0.26f);
        }
    }

    void CreateBench(Transform parent, Vector3 position, Quaternion rotation)
    {
        var bench = new GameObject("PA_DemoDressing_Bench");
        bench.transform.SetParent(parent, false);
        bench.transform.SetPositionAndRotation(position, rotation);

        CreatePart(bench.transform, PrimitiveType.Cube, "Seat",
            new Vector3(0f, 0.42f, 0f), new Vector3(1.35f, 0.09f, 0.42f), WoodLight);
        CreatePart(bench.transform, PrimitiveType.Cube, "LegL",
            new Vector3(-0.52f, 0.20f, 0f), new Vector3(0.10f, 0.40f, 0.38f), WoodDark);
        CreatePart(bench.transform, PrimitiveType.Cube, "LegR",
            new Vector3(0.52f, 0.20f, 0f), new Vector3(0.10f, 0.40f, 0.38f), WoodDark);
    }

    void CreatePlanter(Transform parent, Vector3 position, int index)
    {
        var planter = new GameObject($"PA_DemoDressing_Planter_{index}");
        planter.transform.SetParent(parent, false);
        planter.transform.position = position;

        CreatePart(planter.transform, PrimitiveType.Cube, "Box",
            new Vector3(0f, 0.16f, 0f), new Vector3(0.62f, 0.32f, 0.62f), WoodDark);
        CreatePart(planter.transform, PrimitiveType.Cube, "Soil",
            new Vector3(0f, 0.30f, 0f), new Vector3(0.54f, 0.06f, 0.54f), PlanterSoil);
        CreatePart(planter.transform, PrimitiveType.Sphere, "Leaf",
            new Vector3(0f, 0.42f, 0f), Vector3.one * 0.34f, LeafGreen);

        Color flower = FlowerColors[index % FlowerColors.Length];
        CreatePart(planter.transform, PrimitiveType.Sphere, "Flower_A",
            new Vector3(-0.12f, 0.52f, 0.08f), Vector3.one * 0.14f, flower);
        CreatePart(planter.transform, PrimitiveType.Sphere, "Flower_B",
            new Vector3(0.10f, 0.55f, -0.06f), Vector3.one * 0.12f, flower);
        CreatePart(planter.transform, PrimitiveType.Sphere, "Flower_C",
            new Vector3(0.02f, 0.50f, 0.14f), Vector3.one * 0.11f,
            FlowerColors[(index + 1) % FlowerColors.Length]);
    }

    void CreateLanternPost(Transform parent, Vector3 position)
    {
        var post = new GameObject("PA_DemoDressing_LanternPost");
        post.transform.SetParent(parent, false);
        post.transform.position = position;

        CreatePart(post.transform, PrimitiveType.Cylinder, "Pole",
            new Vector3(0f, 0.85f, 0f), new Vector3(0.09f, 0.85f, 0.09f), IronDark);
        CreatePart(post.transform, PrimitiveType.Cube, "Arm",
            new Vector3(0.16f, 1.68f, 0f), new Vector3(0.34f, 0.06f, 0.06f), IronDark);
        CreatePart(post.transform, PrimitiveType.Sphere, "Glow",
            new Vector3(0.30f, 1.56f, 0f), Vector3.one * 0.22f, LanternGlow, emissive: true);
    }

    void CreateCrateStack(Transform parent, Vector3 position)
    {
        var stack = new GameObject("PA_DemoDressing_CrateStack");
        stack.transform.SetParent(parent, false);
        stack.transform.position = position;
        stack.transform.rotation = Quaternion.Euler(0f, 18f, 0f);

        CreatePart(stack.transform, PrimitiveType.Cube, "Crate_A",
            new Vector3(0f, 0.24f, 0f), new Vector3(0.50f, 0.48f, 0.50f), CrateWood);
        CreatePart(stack.transform, PrimitiveType.Cube, "Crate_B",
            new Vector3(0.06f, 0.70f, -0.04f), new Vector3(0.42f, 0.42f, 0.42f), WoodLight);
        CreatePart(stack.transform, PrimitiveType.Cube, "Crate_C",
            new Vector3(0.58f, 0.20f, 0.22f), new Vector3(0.40f, 0.40f, 0.40f), CrateWood);
    }

    // ── 6) 간판/씬 소품 정리: 영어 스테이징 텍스트 한국어화 + 상자 톤 통일 ───────
    void TidySignage()
    {
        if (_signageTidied) return;

        bool touchedSign = false;

        // "MANAGEMENT HUB" → "코지 잡화점" (씬 파일은 그대로, 런타임 텍스트만 교체).
        var signLabel = GameObject.Find("PA_MarketSign_Label");
        if (signLabel != null)
        {
            var tmp = signLabel.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "코지 잡화점";
                touchedSign = true;
            }
        }

        // 보급 상자/텐트 키트가 원색 박스로 읽히지 않도록 목재 톤으로 통일.
        RecolorSceneProp("Support_Crate", CrateWood);
        RecolorSceneProp("Shop_Tent_Kit", WoodLight);

        if (touchedSign)
        {
            _signageTidied = true;
            Debug.Log("🪧 [DemoDressing] 간판 정리: MANAGEMENT HUB → 코지 잡화점");
        }
    }

    void RecolorSceneProp(string objectName, Color color)
    {
        var prop = GameObject.Find(objectName);
        if (prop == null) return;

        foreach (var renderer in prop.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer == null) continue;
            renderer.material = ResolveMaterial(color, emissive: false);
        }
    }

    // ── 공용 헬퍼 ────────────────────────────────────────────────────────────────
    GameObject CreatePart(Transform parent, PrimitiveType type, string partName,
        Vector3 localPosition, Vector3 localScale, Color color,
        bool emissive = false, bool worldSpace = false)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, worldSpace);
        if (worldSpace) part.transform.position = localPosition;
        else part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        // 렌더러 전용 소품: 콜라이더를 제거해 플레이어/NPC 이동·NavMesh 에 영향이 없다.
        foreach (var collider in part.GetComponentsInChildren<Collider>(true))
            Destroy(collider);

        var renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = ResolveMaterial(color, emissive);

        return part;
    }

    Material ResolveMaterial(Color color, bool emissive)
    {
        if (!emissive && _materialCache.TryGetValue(color, out var cached) && cached != null)
            return cached;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };

        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.6f);
            return material; // 발광 재질은 캐시하지 않는다 (색 배율이 다를 수 있음).
        }

        _materialCache[color] = material;
        return material;
    }

    void CreateItemIconBillboard(Transform parent, string itemResourcePath, Vector3 localPosition, float worldHeight)
    {
        if (string.IsNullOrWhiteSpace(itemResourcePath)) return;

        var item = Resources.Load<Item>(itemResourcePath);
        if (item == null || item.icon == null) return;

        var iconGo = new GameObject("ItemIconBillboard");
        iconGo.transform.SetParent(parent, false);
        iconGo.transform.localPosition = localPosition;

        var sprite = iconGo.AddComponent<SpriteRenderer>();
        sprite.sprite = item.icon;

        float worldSize = Mathf.Max(item.icon.bounds.size.x, item.icon.bounds.size.y);
        if (worldSize > 0.01f)
            iconGo.transform.localScale = Vector3.one * (worldHeight / worldSize);

        _billboards.Add(iconGo.transform);
    }

    static Color ResolveProduceColor(string itemResourcePath)
    {
        if (string.IsNullOrEmpty(itemResourcePath))
            return new Color(0.85f, 0.62f, 0.35f, 1f);

        if (itemResourcePath.Contains("BreadLoaf")) return new Color(0.83f, 0.58f, 0.30f, 1f);
        if (itemResourcePath.Contains("Carrot")) return new Color(0.92f, 0.49f, 0.20f, 1f);
        if (itemResourcePath.Contains("Fish"))   return new Color(0.52f, 0.68f, 0.82f, 1f);
        if (itemResourcePath.Contains("Wheat"))  return new Color(0.89f, 0.76f, 0.38f, 1f);
        if (itemResourcePath.Contains("Ore"))    return new Color(0.45f, 0.44f, 0.46f, 1f);
        return new Color(0.85f, 0.62f, 0.35f, 1f);
    }

    static Vector3 SnapToGround(Vector3 target, float fallbackY)
    {
        Vector3 rayStart = target + Vector3.up * 6f;
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 20f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        target.y = fallbackY;
        return target;
    }
}
