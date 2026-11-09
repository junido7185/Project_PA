using System.Collections.Generic;
using UnityEngine;

// 2026-11-09 전시용 '새 콘텐츠 찾기 쉽게': 관람객은 몇 분만 플레이하므로, 시간대가 맞아야 보이는 물고기·곤충을
// 그 시간이 되면(아직 도감에 없을 때만) 한 번씩 알림으로 알려 준다. 정착(가게+텐트) 뒤, 대화·창이 없을 때만.
// 알림끼리는 실시간 75초 이상 띄우고, 같은 것은 한 섬 세션에 한 번. 표시 전용 — 출현·보상·저장은 건드리지 않는다.
public sealed class DiscoveryHints : MonoBehaviour
{
    sealed class Hint { public int ItemId; public float From, Until; public string Text; }

    static readonly Hint[] Hints =
    {
        // 같은 시간에 여럿이면 앞의 것부터: 밤에는 가게 앞 등불(흰불나방)처럼 가까운 것부터(visitor-r254: 첫 영업 직후 먼 하구 장어가 먼저 떴다).
        new Hint { ItemId = 3008, From = 6f, Until = 16f, Text = "섬 동쪽 강에서 송어가 물길을 따라 오르내려요\n낚싯대를 들고 강가로 가 봐요" },
        new Hint { ItemId = 3010, From = 7f, Until = 18f, Text = "고지대 바위 사이 작은 연못에 붕어가 살아요\n둘레 위에서 낚싯대를 드리워 봐요" },
        new Hint { ItemId = 3102, From = 17.5f, Until = 23.5f, Text = "가게 앞 등불에 흰불나방이 모여들어요" },
        new Hint { ItemId = 3111, From = 18f, Until = 23.5f, Text = "강가 풀 위에서 반딧불이가 깜빡여요\n빛이 멈췄을 때 살금살금 다가가요" },
        new Hint { ItemId = 3109, From = 16.5f, Until = 23f, Text = "해 질 녘 숲 나무 그늘에서\n왕귀뚜라미가 울기 시작했어요" },
        new Hint { ItemId = 3009, From = 14f, Until = 23f, Text = "오후부터 강이 바다와 만나는 하구에\n길게 굽이치는 장어 그림자가 나와요" },
    };

    const float Gap = 75f;
    static readonly HashSet<int> Shown = new HashSet<int>();
    public static int ShownCount => Shown.Count;
    public static bool WasShown(int itemId) => Shown.Contains(itemId);
    public static string LastText { get; private set; } = "";
    public static readonly List<float> ShownTimes = new List<float>(); // 캡처 확인용(실시간)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Shown.Clear(); LastText = ""; ShownTimes.Clear(); }

    float _nextCheck, _lastShownAt = -999f;

    void Awake() { Shown.Clear(); ShownTimes.Clear(); }

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 1f;
        if (Time.unscaledTime - _lastShownAt < Gap) return;
        var settlement = DemoSettlementController.Instance;
        if (settlement == null || !settlement.Established) return;
        if (PlayerInputHandler.ModalOpen || DialogueUI.IsOpen) return;
        if (PlayerInputHandler.Instance == null || !PlayerInputHandler.Instance.FirstDayControls) return;
        // visitor-r249: 정착·전문 분야를 고르면 곧바로 해가 지고 약 100초 뒤 첫 영업이다 → 첫 영업을 마치기 전에는 안내로 관람객을 멀리 보내지 않는다.
        var loop = DayNightShopLoopController.Instance;
        if (loop != null && loop.IsOpeningDemo && !loop.OpeningSessionCompleted) return;
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f;
        foreach (var hint in Hints)
        {
            if (Shown.Contains(hint.ItemId) || DiscoveryLog.IsFound(hint.ItemId)) continue;
            if (hour < hint.From || hour >= hint.Until) continue;
            Shown.Add(hint.ItemId);
            LastText = hint.Text;
            _lastShownAt = Time.unscaledTime;
            ShownTimes.Add(Time.unscaledTime);
            FirstDayWorldPresentation.Toast(hint.Text);
            return;
        }
    }
}
