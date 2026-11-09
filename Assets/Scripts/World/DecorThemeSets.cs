using System.Collections.Generic;
using UnityEngine;

// 2026-11-04 ACNH 소품 연구(너굴 리스트) H2: P.A. 고유 꾸밈 테마 세트. 같은 곳에서 얻은 꾸밈 두 개를 한 세트로 묶는다.
// 표시·생활감 전용: 가게 손님 수·가격·판매 규칙은 그대로(꾸밈 개수 보너스는 기존 DemoSettlementController가 소유).
// 툴팁에 세트 이름을 보이고, 가게 안에 세트가 다 놓이면 들어온 손님이 한 번 알아보는 말풍선을 띄운다(DecorSetAppreciation).
public static class DecorThemeSets
{
    public sealed class Set
    {
        public string Name, Remark;
        public string[] Keys;   // DemoPlaceableEntry.key
        public int[] ItemIds;   // 같은 순서의 꾸밈 아이템 id
        public string[] Pieces; // 같은 순서의 이름(툴팁)
    }

    public static readonly Set[] All =
    {
        new Set { Name = "바닷가 세트", Keys = new[] { "decor-shells", "decor-glass" }, ItemIds = new[] { 2043, 2048 },
            Pieces = new[] { "조개 장식", "유리 모빌" }, Remark = "조개 장식에 유리 모빌, 바닷가 같아요!" },
        new Set { Name = "땅속 보물 세트", Keys = new[] { "decor-mosaic", "decor-crystal" }, ItemIds = new[] { 2050, 2053 },
            Pieces = new[] { "토기 모자이크", "광물 받침" }, Remark = "모자이크에 광물 받침, 보물 전시 같아요!" },
    };

    // 2026-11-09 세트가 아니어도 혼자 손님 눈길을 끄는 꾸밈(곤충 관찰 안내판: 모은 곤충 → 꾸밈 → 손님 반응). 말풍선 두 줄(약 24자) 안.
    public sealed class Single { public string Key, Remark; public int ItemId; }
    public static readonly Single[] Singles =
    {
        new Single { Key = "decor-insectboard", ItemId = 2057, Remark = "곤충 안내판이네요! 방아깨비도 있어요." },
        // 도감 섹션을 다 채워야 받는 기념패(수집 → 기념패 → 손님이 알아봄).
        new Single { Key = "decor-plaque", ItemId = 2051, Remark = "도감 기념패네요! 많이 모으셨어요." },
    };

    // 지금 가게 안에서 손님이 알아볼 말: 다 놓인 세트 + 놓인 단독 꾸밈.
    public static List<string> RemarksInShop()
    {
        var lines = new List<string>();
        foreach (var set in CompleteInShop()) lines.Add(set.Remark);
        var settlement = DemoSettlementController.Instance;
        if (settlement != null)
            foreach (var single in Singles)
                if (settlement.ShopDecorWith(single.Key).Count > 0) lines.Add(single.Remark);
        // 벽에 건 기념패도 가게 꾸밈으로 알아본다(바닥에도 있으면 한 번만).
        Single plaque = null; foreach (var s in Singles) if (s.ItemId == ShopWallHang.PlaqueId) plaque = s;
        if (plaque != null && ShopWallHang.Current != "" && !lines.Contains(plaque.Remark)) lines.Add(plaque.Remark);
        // 책상 위 발견물(말풍선 두 줄 안).
        var desk = ShopDeskShelf.CurrentItem;
        if (desk != null) { string name = ItemDisplayName.For(desk); lines.Add($"책상 위 {name}{ResidentNames.Josa(name, "이")} 예뻐요!"); }
        return lines;
    }

    public static Set ForItem(Item item)
    {
        if (item == null) return null;
        foreach (var set in All) if (System.Array.IndexOf(set.ItemIds, item.id) >= 0) return set;
        return null;
    }

    // decorset-r154: 말풍선은 두 줄(약 24자)이라 긴 문장은 "…"로 잘렸다 → 짧게.
    // 툴팁 한 줄: "바닷가 세트 · 조개 장식 + 유리 모빌 — 가게에 함께 두면 손님이 알아봐요".
    public static string TooltipLine(Item item)
    {
        var set = ForItem(item);
        if (set != null) return $"{set.Name} · {string.Join(" + ", set.Pieces)} — 가게에 함께 두면 손님이 알아봐요";
        // qa232: 기념패는 바닥뿐 아니라 가게 뒷벽에도 건다(ShopWallHang).
        if (item != null && ShopWallHang.IsPlaque(item)) return "가게 바닥이나 뒷벽(책상과 창문 사이)에 두면 손님이 알아봐요";
        if (item != null) foreach (var single in Singles) if (single.ItemId == item.id) return "가게에 두면 손님이 알아봐요";
        // 2026-11-09 의자 색 변형(H4): 나무 의자 + 모은 것으로 칠하기.
        if (item != null && (item.id == 2060 || item.id == 2061)) return "러그 교체 · 가게 안 생활 공간 러그 위에서 들고 [E] — 깔려 있던 러그는 가방으로";
        // 연구 H4 '도감에 보유/전체'(handoff 감사): 도감은 생물·발견물 전용이라, 의자 툴팁에 지금 가진 색(가방 + 가게 안)을 n/3으로 보인다(저장 추가 없음).
        if (item != null && (item.id == 2021 || item.id == 2058 || item.id == 2059))
            return $"색 변형 · 나무·산호빛·바다빛 (보유 {OwnedChairColours()}/3) — 현장 작업대에서 산호 조각·해변 유리로 칠해요";
        // qa232: 해변·땅속 발견물 툴팁에 책상 위 소장(ShopDeskShelf)이 안 보였다.
        if (ShopDeskShelf.Accepts(item)) return "소장 · 가게 책상 앞에서 들고 [E] — 램프 옆에 올려 둬요";
        return null;
    }

    static readonly (int id, string key)[] ChairColours = { (2021, DemoSettlementController.DecorKey), (2058, "furniture-chair-coral"), (2059, "furniture-chair-sea") };
    public static int OwnedChairColours()
    {
        var inventory = Inventory.instance;
        var settlement = DemoSettlementController.Instance;
        int owned = 0;
        foreach (var (id, key) in ChairColours)
        {
            bool inBag = false;
            if (inventory != null)
            {
                if (inventory.hotbar?.slots != null) foreach (var s in inventory.hotbar.slots) if (s != null && !s.IsEmpty && s.item != null && s.item.id == id) inBag = true;
                if (inventory.slots != null) foreach (var s in inventory.slots) if (s != null && !s.IsEmpty && s.item != null && s.item.id == id) inBag = true;
            }
            bool placed = settlement != null && settlement.ShopDecorWith(key).Count > 0;
            if (inBag || placed) owned++;
        }
        return owned;
    }

    // 지금 가게 안(DemoSettlementController의 상점 구역)에 조각이 모두 놓인 세트.
    public static List<Set> CompleteInShop()
    {
        var done = new List<Set>();
        var settlement = DemoSettlementController.Instance;
        if (settlement == null) return done;
        foreach (var set in All)
        {
            bool all = true;
            foreach (var key in set.Keys)
                if (settlement.ShopDecorWith(key).Count == 0) { all = false; break; }
            if (all) done.Add(set);
        }
        return done;
    }
}
