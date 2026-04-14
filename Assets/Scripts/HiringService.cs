using System;
using System.Collections.Generic;
using UnityEngine;

// NPC 채용 시스템 — 단일 싱글턴 서비스.
// Docs/05 §2 "NPC 채용 및 고용" 을 구현한다.
//
// 역할:
// 1) 이용 가능한 채용 후보 풀을 관리 (티어 잠금 필터링 포함).
// 2) 선택된 후보를 검증(비용/티어) 후 EconomyService.TrySpend 로 비용 차감.
// 3) 후보 프리팹을 지정된 스폰 포인트에 Instantiate 하고 profile/specialty 를 주입.
// 4) 고용된 NPC 명단을 보관 → AuditService/FriendshipService 등이 조회할 수 있다.
//
// 설계 원칙:
// - UI 무관. TryHire(candidate) 한 줄로 호출 가능 — 디버그 콘솔/자동 테스트/채용 UI 모두 재사용.
// - 결정론 없음 (채용은 플레이어 의사결정). 후보 풀 셔플이 필요하면 상위 UI 에서 처리.
// - 저장/로드: 현재는 씬에 고용된 NPC GameObject 를 그대로 씬 저장에 의존. 후속 SaveData 확장에서
//   hiredCandidateIds 리스트를 추가해 씬 로드 시 재스폰할 수 있게 확장 가능 (Task #24).
[DefaultExecutionOrder(-60)]   // EconomyService/TierService 이후, 일반 게임플레이보다 이른 초기화.
public class HiringService : MonoBehaviour
{
    public static HiringService Instance { get; private set; }

    [Header("후보 풀")]
    [Tooltip("이 프로젝트의 모든 채용 후보. 티어 잠금은 GetAvailableCandidates() 에서 자동 필터링됨.")]
    public List<NpcCandidateData> availableCandidates = new List<NpcCandidateData>();

    [Header("스폰")]
    [Tooltip("비워 두면 이 GameObject 의 Transform 을 사용. 여러 스폰 포인트가 필요하면 자식으로 두고 라운드 로빈으로 선택.")]
    public Transform spawnPoint;

    [Tooltip("spawnPoint 가 비어 있고 이 리스트에 값이 있으면 순차적으로 순회하며 사용.")]
    public List<Transform> spawnPointRotation = new List<Transform>();

    // 이미 고용한 후보 (중복 고용 방지).
    private readonly HashSet<NpcCandidateData> _hired = new HashSet<NpcCandidateData>();

    // 스폰 포인트 라운드 로빈 인덱스.
    private int _spawnRotationIdx = 0;

    // (후보, 생성된 인스턴스) — 고용 이벤트 구독자가 사용할 수 있다.
    public event Action<NpcCandidateData, GameObject> OnHired;

    /// <summary>현재 고용된 후보 수.</summary>
    public int HiredCount => _hired.Count;

    /// <summary>특정 후보가 이미 고용되었는지.</summary>
    public bool IsHired(NpcCandidateData candidate) => candidate != null && _hired.Contains(candidate);

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------- 공개 API --------

    /// <summary>
    /// 현재 티어에서 고용 가능한 후보만 반환한다.
    /// - 이미 고용된 후보는 제외.
    /// - requiredTier 가 현재 티어보다 높은 후보는 제외.
    /// </summary>
    public List<NpcCandidateData> GetAvailableCandidates()
    {
        var result = new List<NpcCandidateData>();
        if (availableCandidates == null) return result;

        int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        foreach (var c in availableCandidates)
        {
            if (c == null) continue;
            if (_hired.Contains(c)) continue;
            if (c.requiredTier > currentTier) continue;
            result.Add(c);
        }
        return result;
    }

    /// <summary>
    /// 후보를 고용한다. 검사 → 비용 차감 → 스폰 → 이벤트 발생 순.
    /// 하나라도 실패하면 부분 상태가 남지 않도록 전체 롤백한다 (비용은 성공 확인 후 차감).
    /// 성공 시 spawnedNpc 에 생성된 GameObject 를 반환.
    /// </summary>
    public bool TryHire(NpcCandidateData candidate, out GameObject spawnedNpc)
    {
        spawnedNpc = null;

        // 1. 기본 유효성
        if (candidate == null)
        {
            Debug.LogWarning("📝 HiringService: 후보가 null 입니다.");
            return false;
        }
        if (_hired.Contains(candidate))
        {
            Debug.Log($"📝 [{candidate.ResolveDisplayName()}] 은(는) 이미 고용된 상태입니다.");
            return false;
        }
        if (candidate.spawnPrefab == null)
        {
            Debug.LogWarning($"📝 [{candidate.ResolveDisplayName()}] spawnPrefab 이 비어 있습니다.");
            return false;
        }

        // 2. 티어 검사
        int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        if (candidate.requiredTier > currentTier)
        {
            Debug.Log($"🔒 [{candidate.ResolveDisplayName()}] Tier {candidate.requiredTier} 이상 필요 (현재 Tier {currentTier})");
            return false;
        }

        // 3. 비용 차감 (EconomyService 없으면 무상 고용 허용 — 디버그 편의)
        if (candidate.hireCost > 0 && EconomyService.Instance != null)
        {
            if (!EconomyService.Instance.TrySpend(candidate.hireCost,
                    $"HiringService.TryHire: {candidate.ResolveDisplayName()}"))
            {
                Debug.Log($"💸 [{candidate.ResolveDisplayName()}] 고용 비용 {candidate.hireCost}G 가 부족합니다.");
                return false;
            }
        }

        // 4. 스폰
        Vector3 spawnPos = ResolveSpawnPosition();
        Quaternion spawnRot = ResolveSpawnRotation();
        spawnedNpc = Instantiate(candidate.spawnPrefab, spawnPos, spawnRot);
        spawnedNpc.name = candidate.ResolveDisplayName();

        // 5. profile 주입 — 프리팹에 어떤 컨트롤러가 붙어 있든 공통으로 적용.
        InjectProfile(spawnedNpc, candidate);

        // 6. 기록
        _hired.Add(candidate);
        Debug.Log($"📝 고용 완료: [{candidate.ResolveDisplayName()}] ({candidate.specialty}) -{candidate.hireCost}G");

        OnHired?.Invoke(candidate, spawnedNpc);
        return true;
    }

    /// <summary>디버그/치트: 고용 목록을 초기화한다 (세이브 로드 전 사용).</summary>
    public void ClearHired() => _hired.Clear();

    // -------- 내부: 프리팹 profile 주입 --------

    // 프리팹에 여러 종류의 Npc*Controller 가 붙어 있을 수 있다.
    // 리플렉션 없이 각 컨트롤러를 시도해 profile 필드를 덮어쓴다.
    // SpecialistNpcController 는 Task #19 에서 추가될 예정 — 존재하면 profile/specialty 를 함께 주입한다.
    private void InjectProfile(GameObject npcGo, NpcCandidateData candidate)
    {
        if (npcGo == null) return;
        NpcProfile profile = candidate.profile;

        var consumer = npcGo.GetComponent<NpcController>();
        if (consumer != null && profile != null) consumer.profile = profile;

        var producer = npcGo.GetComponent<ProducerNpcController>();
        if (producer != null && profile != null) producer.profile = profile;

        // 대화 컴포넌트도 있으면 profile override 로 주입 (프리팹에 기본 연결이 없을 때).
        var dialogue = npcGo.GetComponent<NpcDialogue>();
        if (dialogue != null && profile != null && dialogue.overrideProfile == null)
            dialogue.overrideProfile = profile;

        // SpecialistNpcController 는 Task #19 에서 추가. 컴파일 순서 문제를 피하기 위해
        // GetComponent<MonoBehaviour>() 로 받고 SendMessage 로 주입하면 리플렉션 없이 연결할 수 있다.
        npcGo.SendMessage("ApplySpecialty", candidate.specialty, SendMessageOptions.DontRequireReceiver);
    }

    // -------- 내부: 스폰 위치 해석 --------

    private Vector3 ResolveSpawnPosition()
    {
        if (spawnPointRotation != null && spawnPointRotation.Count > 0)
        {
            Transform t = spawnPointRotation[_spawnRotationIdx % spawnPointRotation.Count];
            _spawnRotationIdx = (_spawnRotationIdx + 1) % spawnPointRotation.Count;
            if (t != null) return t.position;
        }
        if (spawnPoint != null) return spawnPoint.position;
        return transform.position;
    }

    private Quaternion ResolveSpawnRotation()
    {
        if (spawnPointRotation != null && spawnPointRotation.Count > 0)
        {
            int idx = (_spawnRotationIdx - 1 + spawnPointRotation.Count) % spawnPointRotation.Count;
            Transform t = spawnPointRotation[idx];
            if (t != null) return t.rotation;
        }
        if (spawnPoint != null) return spawnPoint.rotation;
        return Quaternion.identity;
    }
}
