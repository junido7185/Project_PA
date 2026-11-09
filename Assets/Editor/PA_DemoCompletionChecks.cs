using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 데모 완성 D1~D4 회귀 검사. 순수 계산/설정값만 확인하며 씬·저장을 수정하지 않는다.
public static class PA_DemoCompletionChecks
{
    [MenuItem("Project PA/Validation/Demo Completion Checks (D1-D4)")]
    public static void RunFromMenu()
    {
        var failures = Run();
        if (failures.Count == 0) Debug.Log("✅ [DemoCompletion] PASS");
        else Debug.LogError("❌ [DemoCompletion] FAIL\n" + string.Join("\n", failures));
    }

    public static List<string> Run()
    {
        var failures = new List<string>();
        CheckD1Report(failures);
        CheckD2Business(failures);
        return failures;
    }

    // D2 — 데모 경로에서만 손님 밀도/관광객 성향을 주입하고, 자동 마감이 정산 단계 전에 일어나는지 확인한다.
    static void CheckD2Business(List<string> f)
    {
        var go = new GameObject("PA_DemoCompletionChecks_Arrivals") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var arrivals = go.AddComponent<CustomerArrivalController>();
            Expect(f, arrivals.maxTouristsPerOpening == 2 && Mathf.Approximately(arrivals.touristPriceSensitivityScale, 1f),
                "D2 기본값(Golden 경로)이 바뀜");
            typeof(DemoSettlementController).GetMethod("ConfigureOpeningCustomers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { arrivals });
            Expect(f, arrivals.maxTouristsPerOpening >= 6 && arrivals.maxConcurrentTourists >= 2 && arrivals.touristInviteInterval <= 8f,
                $"D2 방문 밀도 부족 {arrivals.maxTouristsPerOpening}/{arrivals.maxConcurrentTourists}/{arrivals.touristInviteInterval}");
            Expect(f, arrivals.touristPriceSensitivityScale < 1f && arrivals.touristLuxuryScale > 1f,
                "D2 관광객과 주민의 구매 성향 차이 없음");
            var loopDefaults = new GameObject("PA_DemoCompletionChecks_Loop") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var loop = loopDefaults.AddComponent<DayNightShopLoopController>();
                Expect(f, DemoSettlementController.OpeningCloseHour > loop.shopOpenHour && DemoSettlementController.OpeningCloseHour < loop.settlementHour,
                    "D2 자동 마감 시각이 영업 단계 밖");
            }
            finally { UnityEngine.Object.DestroyImmediate(loopDefaults); }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    // D1 — Canon v2 §20: 배점 25/30/25/20, S90/A75/B60, 영업량 비례.
    static void CheckD1Report(List<string> f)
    {
        string[] mustPathBiomes = { "Coast", "Meadow", "Forest" };
        string[] mustPathActions = { "forestry", "mining" };

        // 보고된 사례: 정착 완료 + 특성화 + 도구 제작, 2건·24G 판매 → A가 되면 안 된다.
        var reported = DemoPioneerReport.Evaluate(true, 2, FirstRoot(), true, false, 2, 24, 0, 1, mustPathBiomes, mustPathActions);
        Expect(f, reported.rankKey != "rank.a" && reported.rankKey != "rank.s",
            $"D1 2건·24G가 {reported.rankKey}/{reported.total}점으로 후함");

        // 만점 경로는 정확히 100점, 영역별 상한은 캐논 배점과 같아야 한다.
        string[] allBiomes = { "Coast", "Meadow", "Forest", "Highland", "Beach" };
        string[] allActions = { "forestry", "mining", "fish", "bug" };
        var best = DemoPioneerReport.Evaluate(true, 2, FirstRoot(), true, true, 20, 500, 5, 5, allBiomes, allActions);
        Expect(f, best.settlement == 25 && best.commerce == 30 && best.development == 25 && best.exploration == 20,
            $"D1 영역 상한 불일치 {best.settlement}/{best.commerce}/{best.development}/{best.exploration}");
        Expect(f, best.total == 100 && best.rankKey == "rank.s", $"D1 만점 {best.total}/{best.rankKey}");

        // 판매가 늘면 Commerce가 단조 증가해야 한다(2건에서 포화되지 않음).
        int previous = -1; bool monotonic = true;
        for (int sales = 0; sales <= 8; sales++)
        {
            var r = DemoPioneerReport.Evaluate(true, 2, FirstRoot(), false, false, sales, sales * 15, 0, 1, mustPathBiomes, mustPathActions);
            if (r.commerce < previous) monotonic = false;
            if (sales > 2 && r.commerce <= previous) monotonic = false;
            previous = r.commerce;
        }
        Expect(f, monotonic, "D1 Commerce가 판매량에 비례하지 않음");

        // 아무것도 안 해도 C(최저), 실패 등급(D) 없음.
        var none = DemoPioneerReport.Evaluate(false, 0, DemoSpecialization.None, false, false, 0, 0, 0, 0, null, null);
        Expect(f, none.rankKey == "rank.c", $"D1 최저 등급 {none.rankKey}");

        // 필수 경로 + 적당한 영업(6건·90G, 가격 3종) + 낚시 → A 도달 가능해야 한다.
        var good = DemoPioneerReport.Evaluate(true, 2, FirstRoot(), true, false, 6, 90, 1, 3,
            new[] { "Coast", "Meadow", "Forest", "Highland" }, new[] { "forestry", "mining", "fish" });
        Expect(f, good.rankKey == "rank.a" || good.rankKey == "rank.s", $"D1 성실한 플레이가 {good.rankKey}/{good.total}");
        Debug.Log($"[DemoCompletion] D1 reported={reported.total}/{reported.rankKey} good={good.total}/{good.rankKey} best={best.total} none={none.total}");
    }

    static DemoSpecialization FirstRoot()
    {
        foreach (DemoSpecialization root in Enum.GetValues(typeof(DemoSpecialization)))
            if (root != DemoSpecialization.None) return root;
        return DemoSpecialization.None;
    }

    static void Expect(List<string> failures, bool condition, string message)
    {
        if (!condition) failures.Add(message);
    }
}
