using System.Collections.Generic;
using UnityEngine;

// NPC 하루 일과의 페이즈 목록.
// Docs/03 §1.2 FSM 상태 예시를 시간대 기반으로 구체화한 것이다.
public enum SchedulePhase
{
    Sleep,      // 수면 — 이동 정지, 집/지정 위치 대기
    WakeUp,     // 기상·준비 — 짧은 배회
    Work,       // 작업·생산 — ProducerNpcController 활성 (농부/광부/벌목꾼)
    Lunch,      // 점심 휴식 — 배회 (쇼핑 우선도 낮음)
    Afternoon,  // 자유 시간 — 배회 + 낮은 확률 쇼핑
    Shopping,   // 쇼핑 시간 — NpcController 쇼핑 우선 모드
    Evening,    // 귀가 이동 — 배회 (점점 집 방향)
    Rest        // 귀가 후 대기 — 정지
}

// 특정 시간대에 수행할 페이즈 창(Window).
[System.Serializable]
public class ScheduleWindow
{
    [Tooltip("이 시간대에 수행할 행동 페이즈")]
    public SchedulePhase phase;

    [Tooltip("시작 시각 (0~23 정수). 포함.")]
    [Range(0, 23)] public int startHour;

    [Tooltip("종료 시각 (1~24 정수). 미포함 — endHour=12 이면 12:00 전까지.")]
    [Range(1, 24)] public int endHour;
}

// NPC 하루 일과표 — ScriptableObject.
//
// 사용법:
//   Assets > Create > P.A. System > NPC Daily Schedule 으로 에셋을 만든다.
//   NpcScheduleController.scheduleData 에 드래그 드롭한다.
//   ContextMenu "기본 농부 스케줄 적용" 버튼으로 기본값을 한 번에 채울 수 있다.
//
// 시간대 설정 규칙:
//   - windows 가 겹치면 목록에서 먼저 나오는 항목이 우선한다.
//   - 비어 있는 시간대는 defaultPhase 를 따른다.
[CreateAssetMenu(fileName = "New NPC Schedule", menuName = "P.A. System/NPC Daily Schedule")]
public class NpcDailySchedule : ScriptableObject
{
    [Header("시간대별 일과 창 (겹치지 않게 설정)")]
    public List<ScheduleWindow> windows = new List<ScheduleWindow>();

    [Tooltip("windows 에 정의되지 않은 시간대의 기본 행동")]
    public SchedulePhase defaultPhase = SchedulePhase.Afternoon;

    // -------- 공개 API --------

    /// <summary>currentHour 에 해당하는 페이즈를 반환한다.</summary>
    public SchedulePhase GetPhaseAt(float currentHour)
    {
        int hour = Mathf.FloorToInt(currentHour);
        if (windows == null) return defaultPhase;

        foreach (var w in windows)
        {
            if (hour >= w.startHour && hour < w.endHour) return w.phase;
        }
        return defaultPhase;
    }

    // -------- Inspector 기본값 설정 (ContextMenu) --------

    // 농부 / 광부 / 벌목꾼 같은 "생산형" NPC 의 일반적인 스케줄.
    // Inspector 에서 우클릭 → "기본 농부 스케줄 적용" 으로 호출한다.
    [ContextMenu("기본 농부 스케줄 적용 (생산형 NPC)")]
    void ApplyFarmerDefaults()
    {
        windows = new List<ScheduleWindow>
        {
            new ScheduleWindow { phase = SchedulePhase.Sleep,     startHour = 0,  endHour = 6  },
            new ScheduleWindow { phase = SchedulePhase.WakeUp,    startHour = 6,  endHour = 7  },
            new ScheduleWindow { phase = SchedulePhase.Work,      startHour = 7,  endHour = 12 },
            new ScheduleWindow { phase = SchedulePhase.Lunch,     startHour = 12, endHour = 13 },
            new ScheduleWindow { phase = SchedulePhase.Afternoon, startHour = 13, endHour = 15 },
            new ScheduleWindow { phase = SchedulePhase.Shopping,  startHour = 15, endHour = 17 },
            new ScheduleWindow { phase = SchedulePhase.Evening,   startHour = 17, endHour = 19 },
            new ScheduleWindow { phase = SchedulePhase.Rest,      startHour = 19, endHour = 24 },
        };
        Debug.Log($"[{name}] 기본 농부 스케줄이 적용되었습니다.");
    }

    // 전문직(쉐프/엔지니어) 등 오후 늦게 쇼핑하는 NPC 용.
    [ContextMenu("기본 전문직 스케줄 적용 (오후형)")]
    void ApplyProfessionalDefaults()
    {
        windows = new List<ScheduleWindow>
        {
            new ScheduleWindow { phase = SchedulePhase.Sleep,     startHour = 0,  endHour = 7  },
            new ScheduleWindow { phase = SchedulePhase.WakeUp,    startHour = 7,  endHour = 9  },
            new ScheduleWindow { phase = SchedulePhase.Work,      startHour = 9,  endHour = 18 },
            new ScheduleWindow { phase = SchedulePhase.Shopping,  startHour = 18, endHour = 20 },
            new ScheduleWindow { phase = SchedulePhase.Rest,      startHour = 20, endHour = 24 },
        };
        Debug.Log($"[{name}] 기본 전문직 스케줄이 적용되었습니다.");
    }

    // 주로 소비 활동 위주인 마을 주민 NPC 용.
    [ContextMenu("기본 주민 스케줄 적용 (소비형 NPC)")]
    void ApplyResidentDefaults()
    {
        windows = new List<ScheduleWindow>
        {
            new ScheduleWindow { phase = SchedulePhase.Sleep,     startHour = 0,  endHour = 8  },
            new ScheduleWindow { phase = SchedulePhase.WakeUp,    startHour = 8,  endHour = 9  },
            new ScheduleWindow { phase = SchedulePhase.Afternoon, startHour = 9,  endHour = 12 },
            new ScheduleWindow { phase = SchedulePhase.Lunch,     startHour = 12, endHour = 13 },
            new ScheduleWindow { phase = SchedulePhase.Shopping,  startHour = 13, endHour = 16 },
            new ScheduleWindow { phase = SchedulePhase.Afternoon, startHour = 16, endHour = 19 },
            new ScheduleWindow { phase = SchedulePhase.Rest,      startHour = 19, endHour = 24 },
        };
        Debug.Log($"[{name}] 기본 주민 스케줄이 적용되었습니다.");
    }
}
