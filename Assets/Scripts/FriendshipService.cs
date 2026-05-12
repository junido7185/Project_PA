using System;
using System.Collections.Generic;
using UnityEngine;

// NPC 친밀도 시스템 — 단일 싱글턴 서비스.
// Docs/05 §2.3 "관계·친밀도" 를 구현한다.
//
// 핵심 개념:
// - friendshipId: NPC 를 고유하게 식별하는 문자열 키. NpcDialogue.friendshipId 에 설정한다.
//   같은 프리팹이 여러 번 스폰되는 경우에도 서로 다른 id 를 지정하면 각자의 친밀도가 쌓인다.
// - 점수(Points): 원자적 누적값. 대화/거래 이벤트가 들어올 때마다 증가.
// - 단계(Level): 0~MaxLevel. 점수 구간으로 자동 산출되며 레벨업 이벤트 발생.
//
// 기본 포인트 튜닝 (MVP):
//   대화 1회: +2
//   NPC 가 이 상점에서 구매 성공: +5
//   레벨업 구간: 10, 25, 50, 80, 120 (누계 기준, 5단계)
//
// 저장/로드:
// - 현재 SaveData 에 친밀도 필드가 없어 런타임 전용. Task #24 에서 SaveData 확장 시 같이 복구한다.
// - 임시 수동 복구용 ForceSetPoints(id, points) 를 제공.
[DefaultExecutionOrder(-60)]
public class FriendshipService : MonoBehaviour
{
    public static FriendshipService Instance { get; private set; }

    [Header("레벨 커브 (누적 점수)")]
    [Tooltip("각 단계에 도달하기 위한 '누적 점수' 문턱값. 0번째 원소가 Level 1 승급 점수.")]
    public int[] levelThresholds = new int[] { 10, 25, 50, 80, 120 };

    [Header("포인트 설정")]
    [Tooltip("대화 1회당 획득 점수")]
    public int pointsPerDialogue = 2;

    [Tooltip("NPC 가 상점에서 구매 성공 시 획득 점수")]
    public int pointsPerPurchase = 5;

    [Header("히든 블루프린트")]
    [Tooltip("친밀도 단계에 따라 해금되는 히든 레시피 모음")]
    public List<HiddenBlueprintData> hiddenBlueprints = new List<HiddenBlueprintData>();

    // id → 누적 점수
    private readonly Dictionary<string, int> _points = new Dictionary<string, int>();
    // id → 현재 단계 (캐시; 점수 변경 시 재계산)
    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
    // 해금된 레시피 집합
    private readonly HashSet<RecipeData> _unlockedRecipes = new HashSet<RecipeData>();

    // 같은 NPC 와 같은 게임 날짜에 대화로 친밀도가 이미 올랐는지 추적.
    // 하루 1회 제한 — 대화 자체는 계속 가능하되 점수 가산은 1회만.
    // GameClock.CurrentDay 와 비교해 새 날이 되면 자연스럽게 풀림.
    // ⚠ MVP 런타임 전용 — 저장/로드 미구현. SaveData 확장 필요(개발일지 메모 참고).
    private readonly Dictionary<string, int> _lastDialogueDay = new Dictionary<string, int>();

    /// <summary>(id, oldLevel, newLevel) — UI/사운드/블루프린트 해금에 구독.</summary>
    public event Action<string, int, int> OnLevelChanged;

    /// <summary>(해금된 RecipeData, 출처 블루프린트) — CraftingUI/알림 UI 에서 구독.</summary>
    public event Action<RecipeData, HiddenBlueprintData> OnRecipeUnlocked;

    public int MaxLevel => levelThresholds != null ? levelThresholds.Length : 0;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------- 공개 API --------

    /// <summary>현재 단계 (0~MaxLevel). id 가 없으면 0.</summary>
    public int GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _levels.TryGetValue(id, out int lv) ? lv : 0;
    }

    /// <summary>현재 누적 점수. id 가 없으면 0.</summary>
    public int GetPoints(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _points.TryGetValue(id, out int pts) ? pts : 0;
    }

    /// <summary>대화 성공 시 호출 — 기본 포인트를 부여한다.
    /// ⚠ 하루 1회 제한: 같은 NPC 와 같은 GameClock.CurrentDay 안에 두 번째 호출부터는
    /// 점수 가산이 스킵된다. 대화 자체(라인 출력)는 NpcDialogue 가 항상 진행.
    /// 자정이 지나(GameClock.OnNewDay 발화) 다음 날이 되면 자동으로 다시 +N 가능.
    /// </summary>
    public void AddDialoguePoints(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (pointsPerDialogue == 0) return;

        // 현재 게임 날짜 조회 (GameClock 미존재 시 day=0 으로 안전하게 폴백)
        int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 0;

        if (_lastDialogueDay.TryGetValue(id, out int lastDay) && lastDay == today)
        {
            // 오늘 이미 가산됨 — 로그만 남기고 점수는 변경하지 않음
            Debug.Log($"💬 친밀도 [{id}] 오늘({today}일차) 이미 +{pointsPerDialogue} 받음 — 가산 스킵");
            return;
        }

        _lastDialogueDay[id] = today;
        AddPoints(id, pointsPerDialogue, $"대화(Day {today})");
    }

    /// <summary>NPC 가 상점에서 구매 성공 시 호출.</summary>
    public void AddPurchasePoints(string id)
    {
        if (pointsPerPurchase != 0) AddPoints(id, pointsPerPurchase, "구매");
    }

    /// <summary>임의의 이벤트(선물/이벤트/감사)로 포인트를 부여한다. reason 은 로그용.</summary>
    public void AddPoints(string id, int amount, string reason)
    {
        if (string.IsNullOrEmpty(id) || amount == 0) return;

        int before = GetPoints(id);
        int after = Mathf.Max(0, before + amount);
        _points[id] = after;

        int oldLevel = GetLevel(id);
        int newLevel = ComputeLevel(after);

        if (newLevel != oldLevel)
        {
            _levels[id] = newLevel;
            Debug.Log($"💞 친밀도 [{id}] Lv.{oldLevel} → Lv.{newLevel} (점수 {after}, 사유: {reason})");
            OnLevelChanged?.Invoke(id, oldLevel, newLevel);

            // 새 레벨에서 해금되는 블루프린트 평가
            EvaluateBlueprints(id, newLevel);
        }
        else
        {
            Debug.Log($"💞 친밀도 [{id}] +{amount} (Lv.{newLevel}, 점수 {after}, 사유: {reason})");
        }
    }

    /// <summary>현재 해금된 모든 히든 레시피 집합.</summary>
    public IReadOnlyCollection<RecipeData> UnlockedRecipes => _unlockedRecipes;

    /// <summary>
    /// 이 레시피가 "히든" 이 아니거나 이미 해금된 경우 true 를 반환한다.
    /// CraftingService 는 이 함수로 잠금 여부를 한 줄 확인한다.
    /// </summary>
    public bool IsRecipeUnlocked(RecipeData recipe)
    {
        if (recipe == null) return false;
        if (hiddenBlueprints == null || hiddenBlueprints.Count == 0) return true;

        bool isHidden = false;
        foreach (var bp in hiddenBlueprints)
        {
            if (bp != null && bp.unlockRecipe == recipe) { isHidden = true; break; }
        }
        if (!isHidden) return true;  // 히든 아님 → 기본 공개

        return _unlockedRecipes.Contains(recipe);
    }

    /// <summary>저장/로드용 강제 세팅 — 일반 게임플레이 코드가 직접 호출해선 안 된다.</summary>
    public void ForceSetPoints(string id, int points)
    {
        if (string.IsNullOrEmpty(id)) return;
        _points[id] = Mathf.Max(0, points);
        int level = ComputeLevel(_points[id]);
        _levels[id] = level;
        EvaluateBlueprints(id, level);
    }

    /// <summary>Restore the last day that dialogue points were granted for this friendship id.</summary>
    public void ForceSetLastDialogueDay(string id, int day)
    {
        if (string.IsNullOrEmpty(id) || day <= 0) return;
        _lastDialogueDay[id] = day;
    }

    /// <summary>저장용 — 전체 id→points 딕셔너리를 반환한다.</summary>
    public IReadOnlyDictionary<string, int> GetAllPoints() => _points;

    /// <summary>Save-only view of the daily dialogue cooldown map.</summary>
    public IReadOnlyDictionary<string, int> GetAllLastDialogueDays() => _lastDialogueDay;

    /// <summary>모든 친밀도/해금을 초기화 (씬 로드 직전).</summary>
    public void Clear()
    {
        _points.Clear();
        _levels.Clear();
        _unlockedRecipes.Clear();
        _lastDialogueDay.Clear();
    }

    // -------- 내부 --------

    private int ComputeLevel(int points)
    {
        if (levelThresholds == null) return 0;
        int level = 0;
        for (int i = 0; i < levelThresholds.Length; i++)
        {
            if (points >= levelThresholds[i]) level = i + 1;
            else break;
        }
        return level;
    }

    private void EvaluateBlueprints(string id, int level)
    {
        if (hiddenBlueprints == null) return;
        foreach (var bp in hiddenBlueprints)
        {
            if (bp == null || bp.unlockRecipe == null) continue;
            if (bp.friendshipId != id) continue;
            if (level < bp.requiredFriendshipLevel) continue;
            if (_unlockedRecipes.Contains(bp.unlockRecipe)) continue;

            _unlockedRecipes.Add(bp.unlockRecipe);

            string msg = string.IsNullOrEmpty(bp.unlockMessage)
                ? $"[{id}] 이 당신에게 [{bp.unlockRecipe.recipeName}] 의 비법을 전수했다!"
                : bp.unlockMessage;
            Debug.Log($"📜 히든 레시피 해금: {msg}");
            OnRecipeUnlocked?.Invoke(bp.unlockRecipe, bp);
        }
    }
}
