using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// 주민 인사/잡담 대사 조합기 — 무작위 풀 대신 "지금 상황"에서 할 말을 고른다.
//
// 설계 의도:
// - 한 번 말을 걸면 한 가지 이야기(beat)를 한다. 후보는 실제 상태(ResidentDialogueContext)에서만 만든다:
//   도착 → 다음 할 일(보급 상자·상점·텐트·분야·진열·영업·마감) → 방금 한 일(잡은 물고기·부서진 도구·가득 찬 가방)
//   → 동행의 일과 → 있는 곳·함께 온 동료·시간대 → 개인 이야기 순으로 우선한다.
// - 같은 날 다시 말을 걸면 이미 한 이야기는 건너뛰고 다음 이야기로 넘어간다(대화가 이어지는 느낌).
//   네 번 넘게 말을 걸면 새 소식이 없을 때 짧은 대답 + 지금 할 일 한마디로 줄인다.
// - 개인 이야기는 하루 두 개까지, 친밀도가 오를수록 뒤 이야기가 열린다.
// - 직전 12줄은 다시 쓰지 않는다. 날이 바뀌면 그날 한 이야기 목록만 비운다.
// - 실패하면 false 를 돌려 기존 DialogueData 풀(DialogueService)로 폴백한다.
public sealed class ResidentConversationMemory
{
    public int Day = int.MinValue;
    public int TalksToday, TotalTalks, DaysTalked, StoryIndex, StoriesToday;
    public readonly HashSet<string> UsedToday = new HashSet<string>();
    public readonly HashSet<string> UsedEver = new HashSet<string>();
    public readonly List<string> Recent = new List<string>();
    public readonly Dictionary<ResidentItemKind, int> MentionedCounts = new Dictionary<ResidentItemKind, int>();
    public readonly Dictionary<string, float> HintSaidAt = new Dictionary<string, float>();
    public string LastBeat;
    public Item NotedHeld;
    public float NotedHeldAt = -100f;
    // 과묵한 말투의 머뭇거림(문장 앞 "…")을 오늘 이미 보였는지. 매 대사마다 붙으면 버릇처럼 들린다.
    public bool HesitatedToday;

    // 대화 누적으로 본 친밀도(친밀도 ID 가 없는 동행용). 하루에 한 단계를 넘지 않게 천천히 오른다.
    public int Familiarity => Mathf.Clamp(Mathf.Max(0, DaysTalked - 1) + TotalTalks / 8, 0, 5);

    public void BeginDay(int day)
    {
        if (day == Day) return;
        Day = day; TalksToday = 0; StoriesToday = 0; UsedToday.Clear(); DaysTalked++; HesitatedToday = false;
    }

    public void Remember(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Recent.Remove(text);
        Recent.Add(text);
        if (Recent.Count > 12) Recent.RemoveAt(0);
    }
}

public static class ResidentContextDialogue
{
    const int BusyAfterTalks = 4;        // 같은 날 다섯 번째 대화부터 새 소식이 없으면 짧게 답한다.
    const int BusyBypassPriority = 40;   // 이보다 급한 이야기(할 일·방금 생긴 일)는 짧은 대답 단계에서도 꺼낸다.
    const int StoriesPerDay = 2;
    const float NotedHeldSeconds = 20f;  // 손에 들고 다가왔다가 빈손으로 말을 건 경우를 "들고 있던 것"으로 본다.
    const float HintRepeatSeconds = 120f; // 같은 할 일을 다시 꺼내기까지의 실제 시간.
    const int MaxLineLength = 92;        // 대화창 3줄 안에 들어오게 인사+본문 길이를 제한한다.
    static readonly System.Random Rng = new System.Random();
    static readonly Regex Token = new Regex(@"\{(\w+)(?::([가-힣]+))?\}", RegexOptions.Compiled);

    readonly struct Beat
    {
        public readonly string Key, Topic, Fallback;
        public readonly int Priority;
        public readonly bool Once, NoOpener;
        public Beat(string key, string topic, int priority, bool once = false, bool noOpener = false, string fallback = null)
        { Key = key; Topic = topic; Priority = priority; Once = once; NoOpener = noOpener; Fallback = fallback; }
    }

    // -------- 인사·잡담 --------

    public static bool TryComposeGreeting(NpcDialogue npc, GameObject player, out string line)
    {
        line = null;
        if (npc == null) return false;
        var memory = npc.Conversation;
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        memory.BeginDay(day);
        Item noted = Time.unscaledTime - memory.NotedHeldAt <= NotedHeldSeconds ? memory.NotedHeld : null;
        var ctx = ResidentDialogueContext.Capture(npc, player, noted, memory.Familiarity);

        // 역할이 없는 이웃(보리 등)의 첫 대화는 작가가 쓴 인사 에셋을 그대로 쓴다(캠페인 소개 문장 보존).
        if (ctx.Role == ResidentRole.Neighbor && memory.TotalTalks == 0 && npc.dialogueData != null &&
            npc.dialogueData.FindTopic(DialogueTopic.Greeting) != null)
        {
            string authored = DialogueService.GetLineFor(npc.ResolveProfile(), npc.dialogueData, DialogueTopic.Greeting);
            if (!string.IsNullOrEmpty(authored))
            {
                Commit(memory, new Beat("authored", "authored", 0, once: true), authored, authored);
                line = authored;
                return true;
            }
        }

        var vars = Variables(ctx);
        string body = null;
        Beat chosen = default;
        bool found = false;
        foreach (var beat in CollectBeats(ctx, memory).OrderBy(b => b.Priority))
        {
            if (memory.TalksToday >= BusyAfterTalks && beat.Priority >= BusyBypassPriority) break;
            if (beat.Once ? memory.UsedEver.Contains(beat.Key) : memory.UsedToday.Contains(beat.Key) && !HintDueAgain(memory, beat.Key)) continue;
            body = Pick(beat.Topic, ctx, memory, vars) ?? (beat.Fallback != null ? Pick(beat.Fallback, ctx, memory, vars) : null);
            if (body == null) continue;
            chosen = beat; found = true;
            break;
        }

        if (!found)
        {
            body = Pick("busy", ctx, memory, vars);
            if (body == null) return false;
            // 지금 할 일이 남아 있으면 짧은 대답 끝에 한마디로 다시 짚어 준다(직전 말이 그 안내가 아니었을 때만).
            // 연달아 잔소리하지 않도록 짧은 대답 두 번에 한 번만 붙인다.
            string reminderKey = ShortHintTopic(ctx);
            string reminder = reminderKey != null && memory.LastBeat != "hint" && memory.LastBeat != "busy:remind"
                ? Pick(reminderKey, ctx, memory, vars) : null;
            bool reminded = reminder != null && body.Length + reminder.Length + 1 <= MaxLineLength;
            if (reminded) body = body + " " + reminder;
            chosen = new Beat(reminded ? "busy:remind" : "busy", "busy", 100);
        }

        string shown = body;
        if (memory.TalksToday == 0 && !chosen.NoOpener)
        {
            string opener = Pick(OpenerTopic(ctx), ctx, memory, vars);
            if (opener != null && opener.Length + body.Length + 1 <= MaxLineLength)
            {
                shown = opener + " " + body;
                memory.Remember(opener);
            }
        }
        shown = Hesitate(memory, shown);
        Commit(memory, chosen, body, shown);
        line = shown;
        return true;
    }

    // 문장 머리의 "…"는 하루 첫 번째 하나만 남긴다(2026-11-02 사용자 "대화 수준이 아직": 아를로의 거의 모든 말이 "…"로 시작했다).
    // 문장 중간의 쉼("텐트… 나무 그늘")은 그대로 둔다.
    static readonly Regex LeadingPause = new Regex(@"(^|(?<=[.?!]\s))…\s*", RegexOptions.Compiled);
    static string Hesitate(ResidentConversationMemory memory, string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return LeadingPause.Replace(text, match =>
        {
            if (memory.HesitatedToday) return string.Empty;
            memory.HesitatedToday = true;
            return match.Value;
        });
    }

    // 첫 영업을 마친 그날 밤(저녁·밤)에만 마감 이야기를 한다. 다음 날 아침엔 "어제 첫 영업" 이야기로 넘어간다.
    static bool JustClosed(ResidentDialogueContext c) =>
        c.SessionClosed && (c.Part == ResidentDayPart.Evening || c.Part == ResidentDayPart.Night);

    static void Commit(ResidentConversationMemory memory, Beat beat, string body, string shown)
    {
        if (beat.Once) memory.UsedEver.Add(beat.Key);
        memory.UsedToday.Add(beat.Key);
        if (beat.Key == "arrival") memory.UsedToday.Add("hint:supply"); // 도착 인사가 상자 안내를 겸한다.
        if (beat.Key == "arrival") memory.HintSaidAt["hint:supply"] = Time.unscaledTime;
        if (beat.Key.StartsWith("hint:")) memory.HintSaidAt[beat.Key] = Time.unscaledTime;
        if (beat.Key == "hint:tent_one_have") memory.UsedEver.Add("work:home"); // 텐트 인사는 한 번만.
        // "got:Fish:3" — 이 주민에게 이야기한 채집물 수를 기억해 같은 물고기로 두 번 놀라지 않게 한다.
        string[] gain = beat.Key.StartsWith("got:") ? beat.Key.Split(':') : null;
        if (gain != null && gain.Length == 3 && System.Enum.TryParse(gain[1], out ResidentItemKind kind) && int.TryParse(gain[2], out int count))
            memory.MentionedCounts[kind] = count;
        memory.LastBeat = beat.Key.StartsWith("hint:") || beat.Key == "arrival" ? "hint" : beat.Key;
        memory.Remember(body);
        if (shown != body) memory.Remember(shown);
        memory.TalksToday++;
        memory.TotalTalks++;
        if (beat.Key.StartsWith("story:")) { memory.StoryIndex++; memory.StoriesToday++; }
    }

    // 같은 할 일 안내는 하루 한 번이 기본이지만, 상태가 그대로인 채 한참 지나 다시 물으면 한 번 더 짚어 준다.
    static bool HintDueAgain(ResidentConversationMemory memory, string key) =>
        key.StartsWith("hint:") && memory.HintSaidAt.TryGetValue(key, out float at) && Time.unscaledTime - at > HintRepeatSeconds;

    static IEnumerable<Beat> CollectBeats(ResidentDialogueContext c, ResidentConversationMemory m)
    {
        // 0) 첫 만남
        if (c.IsCompanion && c.DemoActive && c.SupplyPending && m.TotalTalks == 0)
            yield return new Beat("arrival", "arrival", 0, once: true, noOpener: true);
        if (!c.IsCompanion && c.Role != ResidentRole.Neighbor && m.TotalTalks == 0)
            yield return new Beat("intro", "intro", 0, once: true, noOpener: true);

        // 1) 지금 해야 할 일(데모 정착 진행). 영업 중에는 판매 건수가 바뀔 때마다 새 이야기로 본다.
        string hint = DemoHintTopic(c);
        if (hint != null)
        {
            string key = "hint:" + hint + (hint.StartsWith("open") ? ":" + SalesShown(c) : "");
            yield return new Beat(key, "hint:" + hint, hint.EndsWith("_held") ? 10 : 20);
        }

        // 2) 동행 도구·작업대
        foreach (var beat in ToolBeats(c)) yield return beat;

        // 3) 방금 생긴 일
        if (!string.IsNullOrEmpty(c.BrokenToolKey)) yield return new Beat("broke:" + c.BrokenToolKey, "tool:broke", 26, once: true);
        if (c.BagFull) yield return new Beat("bag:full", "bag:full", 29);
        foreach (var beat in GainBeats(c, m)) yield return beat;
        string held = HeldTopic(c);
        if (held != null) yield return new Beat(held, held, 35);

        // 4) 동행의 일과 / 일반 낮·밤 루프
        if (c.IsCompanion)
        {
            if (c.HasHome) yield return new Beat("work:home", "work:home", 38, once: true);
            if (c.Activity == DemoCompanionRoutine.Activity.Shopping || c.Activity == DemoCompanionRoutine.Activity.GoingToShop)
                yield return new Beat("work:shopping", "work:shopping", 39);
            if (c.Activity == DemoCompanionRoutine.Activity.Gathering) yield return new Beat("work:gather", "work:gather", 39);
            if (c.ProducerWorking) yield return new Beat("work:mining", "work:mining", 41);
            else if (c.Activity == DemoCompanionRoutine.Activity.Waiting && !c.HasHome) yield return new Beat("work:waiting", "work:waiting", 42);
            else if (c.Activity == DemoCompanionRoutine.Activity.Working || c.Activity == DemoCompanionRoutine.Activity.GoingToWork)
                yield return new Beat("work:field", "work:field", 43);
            else if (c.Activity == DemoCompanionRoutine.Activity.Resting)
                yield return c.Part == ResidentDayPart.Evening || c.Part == ResidentDayPart.Night
                    ? new Beat("work:night", "work:night", 43) : new Beat("work:resting", "work:resting", 45);
        }
        if (c.LoopActive)
        {
            if (c.CanOpenShop) yield return new Beat("loop:open_ready", "loop:open_ready", 22);
            else if (c.ShopOpen) yield return new Beat("loop:open:" + c.SalesToday, "loop:open", 44);
            else if (c.Phase == PADayNightPhase.Settlement) yield return new Beat("loop:settle", "loop:settle", 44);
            else if (c.Phase == PADayNightPhase.DayPreparation) yield return new Beat("loop:prep", "loop:prep", 46);
        }
        // 첫 영업 다음 날(마감 시각이 지난 낮): 어제 결과를 돌아보고 오늘 할 일로 잇는다. 하루 한 번.
        if (c.DemoActive && c.SessionClosed && !JustClosed(c))
            yield return new Beat("after", SalesShown(c) > 0 ? "after" : "after_none", 30);
        if (c.Day >= 2)
        {
            if (c.YesterdaySales > 0 && !string.IsNullOrEmpty(c.YesterdayTopItem)) yield return new Beat("yesterday", "yesterday", 42);
            else if (c.YesterdayRejections > 0) yield return new Beat("yesterday", "yesterday_none", 42);
        }

        // 5) 있는 곳 · 동료 · 시간 · 개인 이야기 (대화를 이어 가는 순서)
        if (c.Place != ResidentPlace.Unknown) yield return new Beat("place:" + c.Place, PlaceTopic(c.Place), 50);
        string story = StoryTopic(c, m);
        if (story != null && m.StoriesToday == 0) yield return new Beat("story:" + m.StoryIndex, story, 52);
        string partner = PartnerTopic(c);
        if (partner != null) yield return new Beat(partner, partner, 53);
        if (!string.IsNullOrEmpty(c.PartnerName))
            yield return new Beat("pair", "pair:" + c.PartnerRole, 56, fallback: "pair:Any");
        // 영업 중에는 가게가 화제의 중심이라 시간대 잡담을 꺼내지 않는다.
        if (!c.ShopOpen) yield return new Beat("time:" + c.Part, "time:" + PartName(c.Part), 58);
        if (story != null && m.StoriesToday > 0 && m.StoriesToday < StoriesPerDay)
            yield return new Beat("story:" + m.StoryIndex, story, 62);
    }

    // 데모 정착에서 지금 가장 먼저 할 일. 순서는 실제 게이트(DemoSettlementController·DayNightShopLoopController)를 따른다.
    static string DemoHintTopic(ResidentDialogueContext c)
    {
        if (!c.DemoActive) return null;
        if (c.SupplyPending) return "supply";
        if (!c.ShopPlaced) return c.HeldKind == ResidentItemKind.ShopKit ? "shop_held" : "shop";
        if (!c.Established)
        {
            if (c.Tents >= 2) return null;
            if (c.HeldKind == ResidentItemKind.TentKit) return "tent_held";
            if (c.Tents == 1) return !c.IsCompanion ? "tent_one" : c.HasHome ? "tent_one_have" : "tent_one_wait";
            return c.IsCompanion && !c.HasHome ? "tent_c" : "tent";
        }
        if (c.SessionClosed) return !JustClosed(c) ? null : SalesShown(c) > 0 ? "closed" : "closed_none";
        if (c.ShopOpen) return SalesShown(c) <= 0 ? "open_none" : c.StockedSlots == 0 ? "open_soldout" : "open";
        if (c.RootPending) return "root";
        if (c.HeldKind == ResidentItemKind.DisplayStand) return "stand_held";
        if (c.Count(ResidentItemKind.DisplayStand) > 0) return "stands";
        if (c.StockedSlots == 0) return c.SaleGoods > 0 ? "stock" : "stock_empty";
        if (c.NightReady) return "open_ready";
        if (c.Root != DemoSpecialization.None) return "sunset";
        return null;
    }

    // 짧은 대답 뒤에 붙일 한마디 안내 주제.
    static string ShortHintTopic(ResidentDialogueContext c)
    {
        string hint = DemoHintTopic(c);
        if (hint == null) return null;
        if (hint.StartsWith("shop")) return "short:shop";
        if (hint.StartsWith("tent_one")) return "short:tent_one";
        if (hint.StartsWith("tent")) return "short:tent";
        if (hint.StartsWith("stand")) return "short:stands";
        if (hint.StartsWith("stock")) return "short:stock";
        if (hint.StartsWith("open_ready")) return "short:open_ready";
        if (hint.StartsWith("open")) return "short:open";
        if (hint.StartsWith("closed")) return "short:closed";
        return "short:" + hint;
    }

    static IEnumerable<Beat> ToolBeats(ResidentDialogueContext c)
    {
        if (!c.DemoActive || !c.ShopPlaced) yield break;
        bool mining = c.Root == DemoSpecialization.Mining;
        bool hasUpgrade = c.Count(ResidentItemKind.ImprovedPickaxe) > 0 || c.HeldKind == ResidentItemKind.ImprovedPickaxe;
        if (!c.WorkbenchPlaced && c.Count(ResidentItemKind.WorkbenchKit) > 0)
        {
            if (c.HeldKind == ResidentItemKind.WorkbenchKit) yield return new Beat("tool:wb_held", "tool:wb_held", 12);
            else if (mining) yield return new Beat("tool:wb_mining", "tool:wb_mining", 30);
            else if (c.Established) yield return new Beat("tool:wb", "tool:wb", 34);
        }
        // 광부 동행은 정착 직후부터 기본 곡괭이로 일한다(분야 선택과 무관). 모아 온 광석은 상대 동행도 알려 준다.
        if (c.IsCompanion && c.PartnerBatchReady) yield return new Beat("tool:batch_partner", "tool:batch_partner", 28);
        // 2026-11-02 도구 단계: 구리 곡괭이는 분야와 무관하게 화로에서 만든다. 정착이 끝나면 화로 → 구리 곡괭이 → 건네기 순으로 안내한다.
        if (!c.IsCompanion || !c.Established) yield break;
        if (c.Role == ResidentRole.Miner)
        {
            if (c.ReceivedTool) yield return new Beat("tool:thanks", "tool:thanks", 32, once: true);
            else if (hasUpgrade) yield return new Beat("tool:give", "tool:give", 15);
            else if (c.ForgePlaced) yield return new Beat("tool:craft", "tool:craft", 30);
            else if (c.WorkbenchPlaced) yield return new Beat("tool:forge", "tool:forge", 33);
        }
        else if (c.PartnerRole == ResidentRole.Miner && !c.PartnerReceivedTool)
        {
            if (hasUpgrade) yield return new Beat("tool:give_partner", "tool:give_partner", 25);
            else if (c.ForgePlaced) yield return new Beat("tool:craft_partner", "tool:craft_partner", 36);
            else if (c.WorkbenchPlaced) yield return new Beat("tool:forge_partner", "tool:forge_partner", 37);
        }
    }

    // 지난번 이 주민에게 들려준 뒤로 늘어난 채집물만 이야기한다. 줄어들었으면(팔았으면) 기준을 내린다.
    static IEnumerable<Beat> GainBeats(ResidentDialogueContext c, ResidentConversationMemory m)
    {
        var watched = new (ResidentItemKind kind, string topic, int min, int priority)[]
        {
            (ResidentItemKind.Fish, "got:fish", 1, 30), (ResidentItemKind.Butterfly, "got:bug", 1, 31),
            (ResidentItemKind.Ore, "got:ore", 2, 32), (ResidentItemKind.Wood, "got:wood", 3, 33), (ResidentItemKind.Crop, "got:crop", 2, 34)
        };
        foreach (var w in watched)
        {
            int now = c.Count(w.kind);
            m.MentionedCounts.TryGetValue(w.kind, out int before);
            if (now < before) { m.MentionedCounts[w.kind] = now; continue; }
            if (now >= w.min && now > before) yield return new Beat("got:" + w.kind + ":" + now, w.topic, w.priority, once: true);
        }
    }

    static string HeldTopic(ResidentDialogueContext c) => c.HeldKind switch
    {
        ResidentItemKind.Axe => "held:axe",
        ResidentItemKind.Pickaxe => "held:pickaxe",
        ResidentItemKind.Net => "held:net",
        ResidentItemKind.Rod => "held:rod",
        ResidentItemKind.Phone => "held:phone",
        // 상품을 들고 왔고 가판대가 이미 상점에 놓였을 때만 진열 방법을 말한다.
        ResidentItemKind.Fish or ResidentItemKind.Wood or ResidentItemKind.Ore or ResidentItemKind.Butterfly or
            ResidentItemKind.Crop or ResidentItemKind.Goods when c.ShopPlaced && c.Count(ResidentItemKind.DisplayStand) == 0 => "held:goods",
        _ => null
    };

    static string PartnerTopic(ResidentDialogueContext c)
    {
        if (string.IsNullOrEmpty(c.PartnerName)) return null;
        if (c.PartnerWorking) return "partner:mining";
        return c.PartnerActivity switch
        {
            DemoCompanionRoutine.Activity.Shopping or DemoCompanionRoutine.Activity.GoingToShop => "partner:shopping",
            DemoCompanionRoutine.Activity.Gathering => "partner:gather",
            DemoCompanionRoutine.Activity.Resting when c.PartnerHasHome => "partner:resting",
            DemoCompanionRoutine.Activity.Waiting when !c.PartnerHasHome => "partner:waiting",
            _ => null
        };
    }

    // 개인 이야기: 처음엔 0~2번, 친밀도 한 단계마다 두 개씩 더 열린다.
    static string StoryTopic(ResidentDialogueContext c, ResidentConversationMemory m)
    {
        if (m.StoryIndex > 2 + c.Familiarity * 2) return null;
        string topic = "story" + m.StoryIndex;
        var entries = ResidentDialogueLines.For(topic);
        foreach (var e in entries) if (e.Role == c.Role) return topic;
        return null;
    }

    static string PlaceTopic(ResidentPlace place) => place switch
    {
        ResidentPlace.Harbor => "place:harbor", ResidentPlace.Shore => "place:shore", ResidentPlace.Meadow => "place:meadow",
        ResidentPlace.Forest => "place:forest", ResidentPlace.Highland => "place:highland", ResidentPlace.NearShop => "place:shop",
        ResidentPlace.Home => "place:home", _ => null
    };

    static string PartName(ResidentDayPart part) => part switch
    {
        ResidentDayPart.Dawn => "dawn", ResidentDayPart.Morning => "morning", ResidentDayPart.Midday => "midday",
        ResidentDayPart.Afternoon => "afternoon", ResidentDayPart.Dusk => "dusk", ResidentDayPart.Evening => "evening", _ => "night"
    };

    static string OpenerTopic(ResidentDialogueContext c)
    {
        if (JustClosed(c)) return "open:closed";
        if (c.ShopOpen) return "open:shop";
        if (c.Familiarity >= 2 && Rng.NextDouble() < .5) return "open:warm";
        return c.Part switch
        {
            ResidentDayPart.Dawn => "open:dawn", ResidentDayPart.Morning => "open:morning", ResidentDayPart.Midday => "open:midday",
            ResidentDayPart.Afternoon => "open:afternoon", ResidentDayPart.Dusk => "open:dusk", _ => "open:night"
        };
    }

    static int SalesShown(ResidentDialogueContext c) => c.Report != null ? c.Report.sales : c.SalesToday;

    // -------- 우리 가게에서 산 직후 / 망설인 직후 --------

    public static bool TryComposeShopReaction(NpcDialogue npc, DialogueTopic topic, out string line)
    {
        line = null;
        if (npc == null || topic != DialogueTopic.ShopBought && topic != DialogueTopic.ShopTooExpensive) return false;
        var memory = npc.Conversation;
        NpcProfile profile = npc.ResolveProfile();
        var ctx = new ResidentDialogueContext
        {
            Role = ResidentNames.ResolveRole(npc.gameObject, profile, npc.dialogueData),
            Voice = ResidentNames.VoiceOf(profile),
            Name = ResidentNames.ShortName(npc.gameObject, profile, npc.dialogueData)
        };
        var vars = new Dictionary<string, string> { ["name"] = ctx.Name };
        var shopper = npc.GetComponent<NpcController>();
        Item item = null;
        int price = 0;
        if (topic == DialogueTopic.ShopBought)
        {
            // 방금 이 손님 이름으로 기록된 판매(ShopSlot.TryPurchaseByNpc → SalesLogManager)를 읽는다.
            string buyer = shopper != null && shopper.profile != null && !string.IsNullOrEmpty(shopper.profile.npcName)
                ? shopper.profile.npcName : shopper != null ? shopper.gameObject.name : null;
            var sale = SalesLogManager.Instance != null ? SalesLogManager.Instance.GetRecent(3).FirstOrDefault(r => r != null && r.buyerName == buyer) : null;
            if (sale != null)
            {
                item = ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(0, sale.itemName) : null;
                vars["item"] = item != null ? ResidentNames.ItemName(item) : sale.itemName;
                price = sale.price;
            }
        }
        else if (shopper != null && shopper.CurrentShopSlotTarget != null && !shopper.CurrentShopSlotTarget.IsEmpty)
        {
            item = shopper.CurrentShopSlotTarget.currentItem.data;
            vars["item"] = ResidentNames.ItemName(item);
            price = shopper.CurrentShopSlotTarget.EffectiveDisplayPrice;
        }
        if (price > 0) vars["price"] = price.ToString();
        string baseTopic = topic == DialogueTopic.ShopBought ? "bought" : "pass";
        string text = vars.ContainsKey("item") ? Pick(baseTopic, ctx, memory, vars) : null;
        text ??= Pick(baseTopic + "_unknown", ctx, memory, vars);
        if (text == null) return false;
        memory.Remember(text);
        line = text;
        return true;
    }

    // -------- 전문 주민 재료 요청 앞말 --------

    // 요청 정보 줄(요청: 재료 x수량 · 보유) 앞에 붙는 역할다운 한 문장. 없으면 null(기존 Economy 풀 폴백).
    public static string RequestIntro(NpcDialogue npc, Item item)
    {
        if (npc == null || item == null) return null;
        NpcProfile profile = npc.ResolveProfile();
        var ctx = new ResidentDialogueContext
        {
            Role = ResidentNames.ResolveRole(npc.gameObject, profile, npc.dialogueData),
            Voice = ResidentNames.VoiceOf(profile),
            Name = ResidentNames.ShortName(npc.gameObject, profile, npc.dialogueData)
        };
        string text = Pick("request", ctx, npc.Conversation,
            new Dictionary<string, string> { ["name"] = ctx.Name, ["item"] = ResidentNames.ItemName(item) });
        if (text != null) npc.Conversation.Remember(text);
        return text;
    }

    // -------- 동행 혼잣말 (DemoCompanionRoutine 토스트) --------

    public static string Bark(GameObject speaker, NpcProfile profile, string topic, string fallback)
    {
        var ctx = new ResidentDialogueContext
        {
            Role = ResidentNames.ResolveRole(speaker, profile),
            Voice = ResidentNames.VoiceOf(profile),
            Name = ResidentNames.ShortName(speaker, profile)
        };
        var dialogue = speaker != null ? speaker.GetComponent<NpcDialogue>() : null;
        var memory = dialogue != null ? dialogue.Conversation : new ResidentConversationMemory();
        string text = Pick(topic, ctx, memory, new Dictionary<string, string> { ["name"] = ctx.Name });
        if (text == null) return fallback;
        memory.Remember(text);
        return text;
    }

    // -------- 줄 고르기 · 자리표시자 --------

    static string Pick(string topic, ResidentDialogueContext ctx, ResidentConversationMemory memory, Dictionary<string, string> vars)
    {
        if (string.IsNullOrEmpty(topic)) return null;
        var entries = ResidentDialogueLines.For(topic);
        if (entries.Count == 0) return null;
        var own = new List<string>();
        var shared = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Role != ctx.Role && entry.Role != ResidentRole.Any) continue;
            if (entry.Voice != ResidentVoice.Any && ctx.Voice != ResidentVoice.Any && entry.Voice != ctx.Voice) continue;
            string text = Format(entry.Text, vars);
            if (text == null) continue;
            (entry.Role == ctx.Role ? own : shared).Add(text);
        }
        var pool = own.Count > 0 ? own : shared;
        if (pool.Count == 0) return null;
        var fresh = pool.Where(t => !memory.Recent.Contains(t)).ToList();
        var from = fresh.Count > 0 ? fresh : pool;
        return from[Rng.Next(from.Count)];
    }

    static string Format(string text, Dictionary<string, string> vars)
    {
        bool missing = false;
        string result = Token.Replace(text, match =>
        {
            if (!vars.TryGetValue(match.Groups[1].Value, out string value) || string.IsNullOrEmpty(value)) { missing = true; return string.Empty; }
            return match.Groups[2].Success ? ResidentNames.WithJosa(value, match.Groups[2].Value) : value;
        });
        return missing ? null : result;
    }

    static Dictionary<string, string> Variables(ResidentDialogueContext c)
    {
        var vars = new Dictionary<string, string>();
        void Put(string key, string value) { if (!string.IsNullOrEmpty(value)) vars[key] = value; }
        void Num(string key, int value) { if (value > 0) vars[key] = value.ToString(); }
        Put("name", c.Name);
        Put("partner", c.PartnerName);
        if (c.Sample.TryGetValue(ResidentItemKind.Fish, out var fish) && fish != null) Put("fishname", ResidentNames.ItemName(fish));
        Num("fish", c.Count(ResidentItemKind.Fish));
        Num("wood", c.Count(ResidentItemKind.Wood));
        Num("ore", c.Count(ResidentItemKind.Ore));
        Num("bug", c.Count(ResidentItemKind.Butterfly));
        Num("crop", c.Count(ResidentItemKind.Crop));
        Num("stands", c.Count(ResidentItemKind.DisplayStand));
        Num("stocked", c.StockedSlots);
        Put("goods", c.GoodsSummary());
        Num("sales", SalesShown(c));
        Num("revenue", c.Report != null ? c.Report.revenue : c.RevenueToday);
        if (c.Report != null) Num("score", c.Report.total);
        Num("ysales", c.YesterdaySales);
        Num("yrevenue", c.YesterdayRevenue);
        Put("ytop", c.YesterdayTopItem);
        Put("tool", c.BrokenTool);
        return vars;
    }
}
