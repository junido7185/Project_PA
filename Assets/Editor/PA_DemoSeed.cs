#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 🌱 PA_DemoSeed — 수직 슬라이스용 즉시 플레이 가능 상태 주입
//
// 설계 의도 (Docs/06 개발 로드맵):
// - 게임 루프 검증을 위해 빈 인벤토리/돈/진열대로 시작하면 5분 안에 모든 시스템을 보기 어렵다.
// - 이 도구는 Editor/Debug 전용 경로로 다음을 한 번에 주입:
//   1️⃣ EconomyService.money = 50000G
//   2️⃣ Hotbar 8슬롯 — 진열·제작 가능한 핵심 원자재/가공품
//   3️⃣ Inventory 12슬롯 — 가공품 + 건물 설계도 3종
//   4️⃣ 첫 Shop 의 빈 슬롯 3개 — 가격 책정된 샘플 진열
//   5️⃣ 모든 NPC NavMeshAgent 보장 + NavMesh 스냅
//
// ⚠️ 중요: SaveManager.SaveGame() 을 호출하지 않으므로 실제 저장 파일과 충돌하지 않는다.
//          순수 씬 상태 주입 — Play 종료 시 폐기되거나 사용자가 명시적으로 저장해야 영구화.
//
// 호출 경로:
// - Play 모드: 모든 컴포넌트가 Awake/Start 완료 → 정상 API 사용
// - Edit 모드: 컴포넌트 인스턴스만 있고 _slots 가 미초기화 → 직접 초기화 후 주입
public static class PA_DemoSeed
{
    const int   StartingMoney = 50000;
    const string DEMO_TAG     = "[DemoSeed]";

    // 핫바 8슬롯 시드 — 핵심 원자재/가공품/설계도 1종
    static readonly (string resource, int count)[] HotbarRecipe =
    {
        ("Items/Item_Wood",            30),
        ("Items/Item_Ore",             20),
        ("Items/Item_Wheat",           25),
        ("Items/Item_Plank",           15),
        ("Items/Item_BreadLoaf",        8),
        ("Items/Item_09_BakedPotato",   6),
        ("Items/Item_Fish",             5),
        ("Items/Blueprints/Blueprint_B02_GeneralStore", 1),
    };

    // 인벤토리 시드 — 진열용 가공품 + 추가 설계도
    static readonly (string resource, int count)[] InventoryRecipe =
    {
        ("Items/Item_IronBar",         10),
        ("Items/Item_Carrot",          12),
        ("Items/Item_11_Furniture",     3),
        ("Items/Item_13_Clothes",       5),
        ("Items/Item_12_ToolSet",       2),
        ("Items/Item_10_GrilledFish",   4),
        ("Items/Item_15_Seed",         20),
        ("Items/Item_14_Hoe",           1),
        ("Items/Blueprints/Blueprint_B05_Workbench",      1),
        ("Items/Blueprints/Blueprint_B06_KitchenStation", 1),
        ("Items/Blueprints/Blueprint_B09_StorageShed",    1),
    };

    // 첫 Shop 의 첫 3슬롯에 가격 책정해 진열할 샘플
    static readonly (string resource, int count, int displayPrice)[] ShopRecipe =
    {
        ("Items/Item_BreadLoaf",  5, 25),
        ("Items/Item_Plank",      8, 60),
        ("Items/Item_Wheat",     10, 12),
    };

    [MenuItem("P.A. System/🌱 Demo Seed Only", priority = -9)]
    public static void ApplyMenu()
    {
        int n = Apply();
        EditorUtility.DisplayDialog("🌱 Demo Seed",
            $"{n}건 주입 완료.\n\n" +
            "💰 50000G + Hotbar/Inventory + Shop 진열\n" +
            "Console 에 상세 로그가 남았습니다.",
            "확인");
    }

    // 메인 진입점 — 반환값은 주입 성공 카운트
    public static int Apply()
    {
        int total = 0;
        string mode = Application.isPlaying ? "Play" : "Edit";
        Debug.Log($"🌱 {DEMO_TAG} 시작 — Mode: {mode}");

        total += SeedMoney();
        total += SeedHotbar();
        total += SeedInventory();
        total += SeedShopSlots();
        total += EnsureNpcNavmesh();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"🌱 {DEMO_TAG} 완료 — 총 {total}건 주입");
        return total;
    }

    // ── 1. 돈 주입 ────────────────────────────────────────────────────────────
    static int SeedMoney()
    {
        var econ = Object.FindFirstObjectByType<EconomyService>();
        if (econ == null)
        {
            Debug.LogWarning($"{DEMO_TAG} EconomyService 가 씬에 없음 — 돈 주입 스킵");
            return 0;
        }

        // ForceSet 은 Awake 와 무관하게 _money 필드를 직접 세팅한다.
        // Edit 모드에서는 OnMoneyChanged 구독자가 없을 수 있으나 영향 없음.
        econ.ForceSet(StartingMoney, "DemoSeed");
        EditorUtility.SetDirty(econ);
        Debug.Log($"💰 {DEMO_TAG} 돈 → {StartingMoney}G");
        return 1;
    }

    // ── 2. Hotbar 주입 ────────────────────────────────────────────────────────
    static int SeedHotbar()
    {
        var hotbar = Object.FindFirstObjectByType<Hotbar>();
        if (hotbar == null)
        {
            Debug.LogWarning($"{DEMO_TAG} Hotbar 가 씬에 없음 — 핫바 주입 스킵");
            return 0;
        }

        EnsureSlotList(hotbar, hotbar.size, ref hotbar.slots);

        int filled = 0;
        for (int i = 0; i < HotbarRecipe.Length && i < hotbar.size; i++)
        {
            var (path, cnt) = HotbarRecipe[i];
            var item = Resources.Load<Item>(path);
            if (item == null)
            {
                Debug.LogWarning($"{DEMO_TAG} Resources/{path} 못찾음 — 슬롯 {i} 비움");
                continue;
            }
            hotbar.slots[i].Set(item, Mathf.Clamp(cnt, 1, item.maxStack));
            filled++;
        }

        EditorUtility.SetDirty(hotbar);
        Debug.Log($"🎒 {DEMO_TAG} Hotbar {filled}/{HotbarRecipe.Length} 슬롯 채움");
        return filled;
    }

    // ── 3. Inventory 주입 ────────────────────────────────────────────────────
    static int SeedInventory()
    {
        var inv = Object.FindFirstObjectByType<Inventory>();
        if (inv == null)
        {
            Debug.LogWarning($"{DEMO_TAG} Inventory 가 씬에 없음 — 인벤토리 주입 스킵");
            return 0;
        }

        EnsureSlotList(inv, inv.size, ref inv.slots);

        int filled = 0;
        for (int i = 0; i < InventoryRecipe.Length && i < inv.size; i++)
        {
            var (path, cnt) = InventoryRecipe[i];
            var item = Resources.Load<Item>(path);
            if (item == null)
            {
                Debug.LogWarning($"{DEMO_TAG} Resources/{path} 못찾음 — 슬롯 {i} 비움");
                continue;
            }
            inv.slots[i].Set(item, Mathf.Clamp(cnt, 1, item.maxStack));
            filled++;
        }

        EditorUtility.SetDirty(inv);
        Debug.Log($"🎁 {DEMO_TAG} Inventory {filled}/{InventoryRecipe.Length} 슬롯 채움");
        return filled;
    }

    // ── 4. Shop 샘플 진열 ────────────────────────────────────────────────────
    static int SeedShopSlots()
    {
        // 첫 번째 활성 Shop 을 찾아 빈 슬롯 앞에서부터 채운다.
        var shops = Object.FindObjectsByType<Shop>(FindObjectsSortMode.None);
        if (shops.Length == 0)
        {
            Debug.LogWarning($"{DEMO_TAG} 씬에 Shop 이 없음 — 진열 스킵");
            return 0;
        }

        var shop = shops[0];

        // ShopSlot 들은 자식에서 직접 수집 (Shop._slots 는 ShopSlot.Awake 에서 등록되므로
        // Edit 모드에서는 비어있을 수 있다 → 자식 검색이 더 안정적).
        var slots = shop.GetComponentsInChildren<ShopSlot>(includeInactive: true);
        if (slots.Length == 0)
        {
            Debug.LogWarning($"{DEMO_TAG} {shop.name} 에 ShopSlot 자식이 없음 — 진열 스킵");
            return 0;
        }

        int placed = 0;
        int recipeIdx = 0;
        for (int i = 0; i < slots.Length && recipeIdx < ShopRecipe.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            if (!slot.IsEmpty) continue;

            var (path, cnt, price) = ShopRecipe[recipeIdx];
            var item = Resources.Load<Item>(path);
            if (item == null)
            {
                Debug.LogWarning($"{DEMO_TAG} Resources/{path} 못찾음 — 진열 슬롯 스킵");
                recipeIdx++;
                continue;
            }

            // ShopSlot 진열은 ItemInstance 1개 + displayPrice
            slot.currentItem = new ItemInstance(item, Mathf.Clamp(cnt, 1, item.maxStack));
            slot.displayPrice = price;
            EditorUtility.SetDirty(slot);
            placed++;
            recipeIdx++;
        }

        Debug.Log($"🏪 {DEMO_TAG} Shop[{shop.name}] 슬롯 {placed}개 진열 (가격 책정됨)");
        return placed;
    }

    // ── 5. NPC NavMesh 보장 ──────────────────────────────────────────────────
    static int EnsureNpcNavmesh()
    {
        var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        if (npcs.Length == 0) return 0;

        int touched = 0;
        foreach (var npc in npcs)
        {
            if (npc == null) continue;

            // NavMeshAgent 자동 추가
            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = npc.gameObject.AddComponent<NavMeshAgent>();
                agent.height = 1.8f;
                agent.radius = 0.4f;
                agent.speed  = 2.5f;
                touched++;
            }

            // NavMesh 위에 스냅 (NavMesh 가 Bake 되어있을 때만 동작)
            if (NavMesh.SamplePosition(npc.transform.position, out var hit, 5f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(npc.transform.position, hit.position) > 0.05f)
                {
                    npc.transform.position = hit.position;
                    EditorUtility.SetDirty(npc);
                    touched++;
                }
            }
        }

        if (touched > 0)
            Debug.Log($"🧭 {DEMO_TAG} NPC {touched}건 NavMesh 보정 (Agent 추가/Snap)");
        return touched;
    }

    // ── 헬퍼: 슬롯 리스트가 null/empty 일 때 초기화 ──────────────────────────
    // Edit 모드에서는 Awake 가 호출되지 않아 slots 가 null/0 인 경우가 있다.
    // Play 모드 진입 시 Awake 가 다시 초기화하지 않도록 size 는 그대로 유지한다.
    static void EnsureSlotList(Object owner, int size, ref List<InventorySlot> list)
    {
        if (list == null || list.Count != size)
        {
            list = new List<InventorySlot>(size);
            for (int i = 0; i < size; i++) list.Add(new InventorySlot());
            Debug.Log($"{DEMO_TAG} {owner.name}.slots 초기화 ({size}슬롯)");
        }
    }
}
#endif
