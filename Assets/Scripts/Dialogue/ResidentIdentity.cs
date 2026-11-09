using System;
using System.Linq;
using UnityEngine;

// 주민 대화의 화자 정체성 — 역할·표시 이름·말투.
//
// 설계 의도:
// - NpcProfile.npcName("Farmer_01")은 RNG 시드·판매 기록·검증 키로 쓰이는 내부 ID 라 바꾸지 않는다.
//   화면에 나가는 이름만 이 파일에서 풀어 준다(저장/검색 키는 그대로).
// - 이름 우선순위: 동행 선택 카드의 이름(아를로/로건/미라) → 사람 이름인 npcName(보리) → 캐논 역할 이름.
//   캐논 이름 출처: Docs/01_GAME_DESIGN/Canon/CONTENT_CANON_BIBLE.md §6.3~6.10.
// - 말투(T/F)는 NpcProfile.traitTF 경계(±0.2)를 DialogueService 와 같은 기준으로 쓴다.
public enum ResidentRole { Any = -1, Neighbor = 0, Farmer, Miner, Lumberjack, Fisher, Chef, Blacksmith, Tailor, Carpenter }
public enum ResidentVoice { Any, Thinking, Feeling }

public static class ResidentNames
{
    const float TraitTFBoundary = 0.2f;
    const string TempSuffix = "TEMP";

    // 역할별 캐논 개인 이름 / 직업명
    public static string CanonName(ResidentRole role) => role switch
    {
        ResidentRole.Farmer => "미라", ResidentRole.Miner => "로건", ResidentRole.Lumberjack => "아를로",
        ResidentRole.Fisher => "타라", ResidentRole.Chef => "노아", ResidentRole.Blacksmith => "펠릭스",
        ResidentRole.Tailor => "준", ResidentRole.Carpenter => "루카", _ => "이웃"
    };

    public static string RoleTitle(ResidentRole role) => role switch
    {
        ResidentRole.Farmer => "농부", ResidentRole.Miner => "광부", ResidentRole.Lumberjack => "벌목꾼",
        ResidentRole.Fisher => "어부", ResidentRole.Chef => "요리사", ResidentRole.Blacksmith => "대장장이",
        ResidentRole.Tailor => "재봉사", ResidentRole.Carpenter => "목수", _ => "이웃"
    };

    // "Farmer_01", "Profile_Farmer", "Candidate_Farmer", "Dialogue_Farmer", "Companion_Farmer_01" 모두 같은 역할로 읽는다.
    public static ResidentRole RoleFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return ResidentRole.Neighbor;
        if (Has(key, "Lumberjack")) return ResidentRole.Lumberjack;
        if (Has(key, "Blacksmith")) return ResidentRole.Blacksmith;
        if (Has(key, "Carpenter")) return ResidentRole.Carpenter;
        if (Has(key, "Farmer")) return ResidentRole.Farmer;
        if (Has(key, "Miner")) return ResidentRole.Miner;
        if (Has(key, "Fisher")) return ResidentRole.Fisher;
        if (Has(key, "Tailor")) return ResidentRole.Tailor;
        if (Has(key, "Chef")) return ResidentRole.Chef;
        return ResidentRole.Neighbor;
    }

    public static ResidentRole RoleFromSpecialty(NpcSpecialty specialty) => specialty switch
    {
        NpcSpecialty.Farmer => ResidentRole.Farmer, NpcSpecialty.Miner => ResidentRole.Miner,
        NpcSpecialty.Lumberjack => ResidentRole.Lumberjack, NpcSpecialty.Fisher => ResidentRole.Fisher,
        NpcSpecialty.Chef => ResidentRole.Chef, NpcSpecialty.Blacksmith => ResidentRole.Blacksmith,
        NpcSpecialty.Tailor => ResidentRole.Tailor, NpcSpecialty.Carpenter => ResidentRole.Carpenter,
        _ => ResidentRole.Neighbor
    };

    // 동행 ID → 프로필 ID → 생산/전문 컴포넌트 → 대사 에셋 이름 → 오브젝트 이름 순으로 역할을 찾는다.
    public static ResidentRole ResolveRole(GameObject go, NpcProfile profile, DialogueData data = null)
    {
        ResidentRole role;
        var resident = go != null ? go.GetComponent<DemoResident>() : null;
        if (resident != null && (role = RoleFromKey(resident.CompanionId)) != ResidentRole.Neighbor) return role;
        if (profile != null)
        {
            if ((role = RoleFromKey(profile.npcName)) != ResidentRole.Neighbor) return role;
            if ((role = RoleFromKey(profile.name)) != ResidentRole.Neighbor) return role;
        }
        if (go != null)
        {
            var producer = go.GetComponent<ProducerNpcController>();
            if (producer != null && (role = RoleFromSpecialty(producer.specialty)) != ResidentRole.Neighbor) return role;
            var specialist = go.GetComponent<SpecialistNpcController>();
            if (specialist != null && (role = RoleFromSpecialty(specialist.specialty)) != ResidentRole.Neighbor) return role;
        }
        if (data != null && (role = RoleFromKey(data.name)) != ResidentRole.Neighbor) return role;
        return go != null ? RoleFromKey(go.name) : ResidentRole.Neighbor;
    }

    public static ResidentVoice VoiceOf(NpcProfile profile)
    {
        float tf = profile != null ? profile.traitTF : 0f;
        return tf <= -TraitTFBoundary ? ResidentVoice.Thinking : tf >= TraitTFBoundary ? ResidentVoice.Feeling : ResidentVoice.Any;
    }

    // 화면용 짧은 이름: "미라". 내부 ID 는 절대 돌려주지 않는다.
    public static string ShortName(GameObject go, NpcProfile profile, DialogueData data = null)
    {
        var routine = go != null ? go.GetComponent<DemoCompanionRoutine>() : null;
        if (routine != null && IsPersonName(routine.DisplayName)) return routine.DisplayName;
        var resident = go != null ? go.GetComponent<DemoResident>() : null;
        string companion = CompanionName(resident != null ? resident.CompanionId : null, profile);
        if (!string.IsNullOrEmpty(companion)) return companion;
        if (profile != null && IsPersonName(profile.npcName)) return StripTemp(profile.npcName);
        ResidentRole role = ResolveRole(go, profile, data);
        return role != ResidentRole.Neighbor ? CanonName(role) : "이웃";
    }

    // 대화창 이름표: "미라 · 농부". 역할이 없는 이웃은 이름만.
    public static string Display(GameObject go, NpcProfile profile, DialogueData data = null)
    {
        string name = ShortName(go, profile, data);
        ResidentRole role = ResolveRole(go, profile, data);
        return role == ResidentRole.Neighbor || name == RoleTitle(role) ? name : $"{name} · {RoleTitle(role)}";
    }

    // 선택한 동행 카드의 표시 이름("미라 · TEMP" → "미라"). 동행 ID 또는 같은 프로필로 찾는다.
    public static string CompanionName(string companionId, NpcProfile profile = null)
    {
        var selected = DemoRouteController.SelectedCompanions;
        if (selected == null) return null;
        var candidate = selected.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(companionId) && c.id == companionId)
                        ?? selected.FirstOrDefault(c => c != null && profile != null && c.profile == profile);
        if (candidate == null) return null;
        string name = StripTemp(candidate.displayName);
        if (!string.IsNullOrEmpty(name)) name = name.Split('·')[0].Trim();
        return IsPersonName(name) ? name : CanonName(RoleFromKey(candidate.id));
    }

    // DialogueUI 안전망: 어떤 경로로든 내부 ID 가 이름표로 들어오면 사람이 읽는 이름으로 바꾼다.
    public static string Sanitize(string shownName)
    {
        string name = StripTemp(shownName);
        if (string.IsNullOrEmpty(name) || !IsInternalId(name)) return name ?? string.Empty;
        ResidentRole role = RoleFromKey(name);
        string companion = null;
        var selected = DemoRouteController.SelectedCompanions;
        if (selected != null)
        {
            var candidate = selected.FirstOrDefault(c => c != null && (c.id == name || c.profile != null && c.profile.npcName == name));
            if (candidate != null) companion = CompanionName(candidate.id);
        }
        string person = !string.IsNullOrEmpty(companion) ? companion : role != ResidentRole.Neighbor ? CanonName(role) : "이웃";
        return role != ResidentRole.Neighbor ? $"{person} · {RoleTitle(role)}" : person;
    }

    // "Farmer_01", "Profile_Bori", "Runtime_ResidentProfile_Miner_01", "Companion_Farmer_01" 처럼 사람이 읽을 이름이 아닌 키.
    public static bool IsInternalId(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        string t = text.Trim();
        if (t == "Unnamed" || t.StartsWith("Profile_", StringComparison.Ordinal) || t.StartsWith("Candidate_", StringComparison.Ordinal) ||
            t.StartsWith("Runtime_", StringComparison.Ordinal) || t.StartsWith("Companion_", StringComparison.Ordinal) ||
            t.StartsWith("Resident_", StringComparison.Ordinal) || t.StartsWith("NPC_", StringComparison.Ordinal)) return true;
        // 영문/숫자/밑줄만으로 된 키(Farmer_01, Miner01 등)는 내부 ID 로 본다. 한글 이름은 통과.
        foreach (char c in t)
            if (!(c < 128 && (char.IsLetterOrDigit(c) || c == '_'))) return false;
        return t.Contains('_') || t.Any(char.IsDigit) || RoleFromKey(t) != ResidentRole.Neighbor;
    }

    static bool IsPersonName(string text) => !string.IsNullOrWhiteSpace(StripTemp(text)) && !IsInternalId(StripTemp(text));

    // 대사 속 아이템 이름. 이미 한글 이름인 에셋은 그대로 쓰고, 영문 키 이름(Wood/Fish)만 데모 표시 이름으로 바꾼다.
    // (ItemDisplayName 은 id 로만 고르므로 id 가 겹치는 옛 ScriptableObjects/Items 에 잘못된 이름을 붙이지 않게 한다.)
    public static string ItemName(Item item)
    {
        if (item == null) return string.Empty;
        string raw = item.itemName ?? string.Empty;
        foreach (char c in raw) if (c >= 0xAC00 && c <= 0xD7A3) return raw;
        string shown = ItemDisplayName.For(item);
        return string.IsNullOrWhiteSpace(shown) ? raw : shown;
    }

    public static string StripTemp(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        string t = text.Trim();
        if (t.EndsWith(TempSuffix, StringComparison.Ordinal))
        {
            t = t.Substring(0, t.Length - TempSuffix.Length).TrimEnd();
            if (t.EndsWith("·", StringComparison.Ordinal)) t = t.Substring(0, t.Length - 1).TrimEnd();
        }
        return t;
    }

    static bool Has(string key, string token) => key.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

    // -------- 조사 --------

    // 마지막 글자 받침 유무. 숫자는 한국어 읽기(일·삼·육·칠·팔·십·영)를 따른다. 영문/기호는 받침 없음으로 본다.
    public static bool HasBatchim(string word, out bool rieul)
    {
        rieul = false;
        string trimmed = string.IsNullOrEmpty(word) ? word : word.TrimEnd(' ', '.', '!', '?', '…', ')', '"', '\'');
        if (string.IsNullOrEmpty(trimmed)) return false;
        char c = trimmed[trimmed.Length - 1];
        if (c >= 0xAC00 && c <= 0xD7A3)
        {
            int final = (c - 0xAC00) % 28;
            rieul = final == 8;
            return final != 0;
        }
        if (char.IsDigit(c))
        {
            rieul = c == '1' || c == '7' || c == '8';
            return c == '0' || c == '1' || c == '3' || c == '6' || c == '7' || c == '8';
        }
        return false;
    }

    // particle 은 받침형/무받침형 어느 쪽으로 적어도 된다: 이/가, 을/를, 은/는, 과/와, 으로/로, 이에요/예요,
    // 이네요/네요, 이다/다, 아/야, 이랑/랑, 이나/나.
    public static string Josa(string word, string particle)
    {
        bool batchim = HasBatchim(word, out bool rieul);
        switch (particle)
        {
            case "이": case "가": return batchim ? "이" : "가";
            case "을": case "를": return batchim ? "을" : "를";
            case "은": case "는": return batchim ? "은" : "는";
            case "과": case "와": return batchim ? "과" : "와";
            case "으로": case "로": return batchim && !rieul ? "으로" : "로";
            case "이에요": case "예요": return batchim ? "이에요" : "예요";
            case "이네요": case "네요": return batchim ? "이네요" : "네요";
            case "이다": case "다": return batchim ? "이다" : "다";
            case "아": case "야": return batchim ? "아" : "야";
            case "이랑": case "랑": return batchim ? "이랑" : "랑";
            case "이나": case "나": return batchim ? "이나" : "나";
            default: return particle;
        }
    }

    public static string WithJosa(string word, string particle) => word + Josa(word, particle);
}
