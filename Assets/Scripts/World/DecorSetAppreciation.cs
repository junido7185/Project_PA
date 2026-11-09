using System.Collections.Generic;
using UnityEngine;

// 2026-11-04 ACNH 소품 연구 H2: 가게 안에 꾸밈 테마 세트(DecorThemeSets)가 다 놓인 밤, 가게에 들어온 손님이 한 번 알아보는 말풍선.
// 2026-11-09 단독 꾸밈(DecorThemeSets.Singles, 곤충 관찰 안내판)도 같은 규칙으로 알아본다.
// 표시 전용: 구매 판단·가격·손님 수는 건드리지 않는다(NpcController/PurchaseEvaluator가 소유). 손님마다 영업 한 번에 한 줄만.
public sealed class DecorSetAppreciation : MonoBehaviour
{
    public static int Remarks { get; private set; }
    public static string LastRemark { get; private set; }
    public static readonly List<string> Said = new List<string>(); // 이번 실행에서 손님이 한 말(확인용)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Remarks = 0; LastRemark = null; Said.Clear(); }

    const float Interval = .5f;
    readonly HashSet<int> _remarked = new HashSet<int>();
    float _nextAt;
    bool _wasOpen;

    void Update()
    {
        if (Time.time < _nextAt) return;
        _nextAt = Time.time + Interval;
        var loop = DayNightShopLoopController.Instance;
        bool open = loop != null && loop.IsShopOpenForCustomers;
        if (open != _wasOpen) { _wasOpen = open; _remarked.Clear(); }
        if (!open) return;
        var settlement = DemoSettlementController.Instance;
        if (settlement == null) return;
        var lines = DecorThemeSets.RemarksInShop();
        if (lines.Count == 0) return;
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            // 동행 주민(가게 방문 포함)은 제외하고 손님만.
            if (npc == null || !npc.isActiveAndEnabled || npc.GetComponent<DemoResident>() != null) continue;
            int id = npc.GetInstanceID();
            if (_remarked.Contains(id) || !settlement.InsideShop(npc.transform.position)) continue;
            var bubble = npc.GetComponentInChildren<NpcBubbleUI>(true);
            if (bubble == null) continue;
            _remarked.Add(id);
            string line = lines[_remarked.Count % lines.Count];
            LastRemark = line;
            Said.Add(line);
            bubble.Show(line, 3.2f);
            Remarks++;
        }
    }
}
