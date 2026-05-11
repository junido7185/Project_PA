using System;
using UnityEngine;

// 계절 enum — GameClock.CurrentSeason 으로 산출.
// Spring → Summer → Autumn → Winter → Spring ... 순환.
public enum Season { Spring, Summer, Autumn, Winter }

// 게임 내 시간 흐름을 관리하는 싱글톤 서비스 — Docs/03 일과 스케줄의 기반.
//
// 설계 의도:
// - NPC 일과 스케줄(NpcScheduleController)이 정각마다 OnHourTick 이벤트를 구독해
//   페이즈를 재평가한다. GameClock 은 단순히 시간 진행만 담당한다.
// - secondsPerGameHour 로 시간 배속을 Inspector 에서 쉽게 조정할 수 있다.
//   (테스트: 10초 = 1시간 / 실사용: 60초 = 1시간)
// - 저장/로드는 ForceSet() 단일 경로로 복구한다.
// - 멀티플레이 전환 시 _currentHour 를 NetworkVariable<float> 로 교체하면
//   호스트가 시간을 소유하는 서버 권한 방식으로 전환된다.
//
// 실행 순서: -80
//   EconomyService(-100) → TierService(-90) → GameClock(-80) → 나머지
[DefaultExecutionOrder(-80)]
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Header("시간 배속 설정")]
    [Tooltip("게임 내 1시간 = 실제 몇 초.\n" +
             "  10  → 테스트용 (10초 = 1게임시간, 240초 = 하루)\n" +
             "  60  → 기본 (1분 = 1게임시간, 24분 = 하루)\n" +
             " 120  → 느린 진행 (2분 = 1게임시간, 48분 = 하루)")]
    public float secondsPerGameHour = 60f;

    [Tooltip("씬 시작 시각 (0~23.99). 기본 7시 = 기상 직전")]
    [Range(0f, 23.99f)] public float startHour = 7f;

    [Header("계절 설정")]
    [Tooltip("한 계절의 길이 (일수). 기본 7일 = 28일에 1년 순환")]
    public int daysPerSeason = 7;

    [Header("현재 상태 (Inspector 읽기 전용)")]
    [SerializeField] private float _currentHour;
    [SerializeField] private int _currentDay = 1;
    [SerializeField] private Season _currentSeason;

    /// <summary>현재 게임 시각 (0~24 float). 예: 7.5 = 오전 7:30.</summary>
    public float CurrentHour => _currentHour;

    /// <summary>현재 게임 일수 (1 부터 시작).</summary>
    public int CurrentDay => _currentDay;

    /// <summary>현재 시각의 정수 (0~23). 페이즈 비교에 사용.</summary>
    public int CurrentHourInt => Mathf.FloorToInt(_currentHour);

    /// <summary>현재 계절. daysPerSeason 단위로 Spring→Summer→Autumn→Winter 순환.</summary>
    public Season CurrentSeason => ComputeSeason(_currentDay);

    /// <summary>정수 시각이 바뀔 때 발화. 파라미터: 새 시각(0~23).</summary>
    public event Action<int> OnHourTick;

    /// <summary>분 단위가 바뀔 때 발화. 파라미터: 0~59 의 새 분(分).
    /// 시계 UI(ClockHUD) 가 매 게임-분 단위로 표시 갱신을 받기 위해 추가.
    /// 정수 분이 바뀔 때만 1회 발화하므로 매 프레임 폭주는 없다.</summary>
    public event Action<int> OnMinuteTick;

    /// <summary>자정(00:00)을 지나 새 날이 시작될 때 발화. 파라미터: 새 일수.</summary>
    public event Action<int> OnNewDay;

    /// <summary>계절이 바뀔 때 발화. 파라미터: 새 계절.</summary>
    public event Action<Season> OnSeasonChanged;

    private int _prevHourInt;
    private int _prevMinuteInt;
    private Season _prevSeason;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _currentHour = startHour;
        _prevHourInt = Mathf.FloorToInt(_currentHour);
        _prevMinuteInt = Mathf.FloorToInt((_currentHour - _prevHourInt) * 60f);
        _prevSeason = ComputeSeason(_currentDay);
        _currentSeason = _prevSeason;
    }

    void Update()
    {
        // 매 프레임 시간 진행:
        //   deltaHours = 실제 경과 시간(초) / 게임 1시간당 실제 초
        float deltaHours = Time.deltaTime / Mathf.Max(0.1f, secondsPerGameHour);
        _currentHour += deltaHours;

        // 자정 넘기기
        if (_currentHour >= 24f)
        {
            _currentHour -= 24f;
            _currentDay++;
            Debug.Log($"🌅 GameClock: Day {_currentDay} 시작 (0시)");
            OnNewDay?.Invoke(_currentDay);

            // 계절 전환 확인
            Season newSeason = ComputeSeason(_currentDay);
            _currentSeason = newSeason;
            if (newSeason != _prevSeason)
            {
                Debug.Log($"🌸 계절 변화: {_prevSeason} → {newSeason} (Day {_currentDay})");
                _prevSeason = newSeason;
                OnSeasonChanged?.Invoke(newSeason);
            }
        }

        // 정각 이벤트 (정수 시각이 바뀔 때만 1회)
        int hourInt = Mathf.FloorToInt(_currentHour);
        if (hourInt != _prevHourInt)
        {
            _prevHourInt = hourInt;
            Debug.Log($"⏰ GameClock: {_currentDay}일 {hourInt:D2}:00");
            OnHourTick?.Invoke(hourInt);
        }

        // 분 단위 이벤트 (정수 분이 바뀔 때만 1회) — ClockHUD 갱신 트리거
        int minuteInt = Mathf.FloorToInt((_currentHour - hourInt) * 60f);
        if (minuteInt != _prevMinuteInt)
        {
            _prevMinuteInt = minuteInt;
            OnMinuteTick?.Invoke(minuteInt);
        }
    }

    // -------- 공개 API --------

    /// <summary>현재 시각을 HH:MM 형식 문자열로 반환 (UI 표시용).</summary>
    public string GetTimeString()
    {
        int h = Mathf.FloorToInt(_currentHour);
        int m = Mathf.FloorToInt((_currentHour - h) * 60f);
        return $"{h:D2}:{m:D2}";
    }

    /// <summary>저장/로드 전용 강제 세팅. 일반 게임플레이 코드가 호출해서는 안 된다.</summary>
    public void ForceSet(float hour, int day, string reason)
    {
        _currentHour = Mathf.Clamp(hour, 0f, 23.99f);
        _currentDay  = Mathf.Max(1, day);
        _prevHourInt = Mathf.FloorToInt(_currentHour);
        _prevMinuteInt = Mathf.FloorToInt((_currentHour - _prevHourInt) * 60f);
        _prevSeason  = ComputeSeason(_currentDay);
        _currentSeason = _prevSeason;
        Debug.Log($"💾 GameClock[{reason}]: Day {_currentDay} {GetTimeString()} ({_currentSeason})");
    }

    // -------- 내부: 계절 산출 --------

    private Season ComputeSeason(int day)
    {
        int safeDaysPerSeason = Mathf.Max(1, daysPerSeason);
        return (Season)(((day - 1) / safeDaysPerSeason) % 4);
    }
}
