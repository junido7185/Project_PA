using System;
using System.Collections.Generic;
using UnityEngine;

// NPC 채용 시스템 — 단일 싱글턴 서비스.
// Docs/05 §2 "NPC 채용 및 고용" 을 구현한다.
//
// 역할:
// 1) 이용 가능한 채용 후보 풀을 관리 (티어 잠금 필터링 포함).
// 2) 선택된 후보를 검증(비용/티어) 후 EconomyService.TrySpend 로 비용 차감.
// 3) 후보 전용 프리팹 또는 같은 역할의 기존 주민 구성을 스폰 포인트에 복제하고 정체성을 주입.
// 4) 고용된 NPC 명단을 보관 → AuditService/FriendshipService 등이 조회할 수 있다.
//
// 설계 원칙:
// - UI 무관. TryHire(candidate) 한 줄로 호출 가능 — 디버그 콘솔/자동 테스트/채용 UI 모두 재사용.
// - 결정론 없음 (채용은 플레이어 의사결정). 후보 풀 셔플이 필요하면 상위 UI 에서 처리.
// - 저장/로드: 기존 SaveManager v4+가 이 서비스의 런타임 기록과 RestoreHiredNpc 경로를 사용한다.
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
    private readonly Dictionary<NpcCandidateData, GameObject> _spawnedByCandidate = new Dictionary<NpcCandidateData, GameObject>();
    private readonly Dictionary<NpcCandidateData, string> _hiredIds = new Dictionary<NpcCandidateData, string>();
    private readonly HashSet<int> _runtimeSpawnInstanceIds = new HashSet<int>();

    // 스폰 포인트 라운드 로빈 인덱스.
    private int _spawnRotationIdx = 0;

    // (후보, 생성된 인스턴스) — 고용 이벤트 구독자가 사용할 수 있다.
    public event Action<NpcCandidateData, GameObject> OnHired;

    /// <summary>현재 고용된 후보 수.</summary>
    public int HiredCount => _hired.Count;

    /// <summary>특정 후보가 이미 고용되었는지.</summary>
    public bool IsHired(NpcCandidateData candidate) => candidate != null && _hired.Contains(candidate);

    public struct HiredNpcRuntimeRecord
    {
        public string HiredNpcId;
        public NpcCandidateData Candidate;
        public GameObject Instance;

        public HiredNpcRuntimeRecord(string hiredNpcId, NpcCandidateData candidate, GameObject instance)
        {
            HiredNpcId = hiredNpcId;
            Candidate = candidate;
            Instance = instance;
        }
    }

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
    /// 현재 상태에서 후보를 실제로 고용할 수 있는지와 플레이어에게 보여 줄 이유를 반환한다.
    /// 명시 프리팹이 없는 기존 후보는 같은 전문 분야의 원본 주민 구성을 사용한다.
    /// </summary>
    public bool CanHire(NpcCandidateData candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "후보 정보가 없습니다.";
            return false;
        }

        if (_hired.Contains(candidate))
        {
            reason = "이미 고용한 주민입니다.";
            return false;
        }

        int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        if (candidate.requiredTier > currentTier)
        {
            reason = $"Tier {candidate.requiredTier}부터 고용할 수 있습니다.";
            return false;
        }

        if (ResolveSpawnTemplate(candidate) == null)
        {
            reason = $"{candidate.specialty} 역할의 주민 모델을 찾지 못했습니다.";
            return false;
        }

        if (candidate.hireCost > 0 && EconomyService.Instance != null
            && EconomyService.Instance.Money < candidate.hireCost)
        {
            int shortage = candidate.hireCost - EconomyService.Instance.Money;
            reason = $"고용 비용이 {shortage:N0} G 부족합니다.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// 후보를 고용한다. 검사 → 비용 차감 → 스폰 → 이벤트 발생 순.
    /// 하나라도 실패하면 부분 상태가 남지 않도록 전체 롤백한다 (비용은 성공 확인 후 차감).
    /// 성공 시 spawnedNpc 에 생성된 GameObject 를 반환.
    /// </summary>
    public bool TryHire(NpcCandidateData candidate, out GameObject spawnedNpc)
    {
        return TryHire(candidate, out spawnedNpc, out _);
    }

    /// <summary>UI가 실패 원인을 즉시 설명할 수 있는 고용 경로.</summary>
    public bool TryHire(NpcCandidateData candidate, out GameObject spawnedNpc, out string failureReason)
    {
        spawnedNpc = null;
        failureReason = string.Empty;

        // 1. 후보·티어·스폰 원본·잔액을 비용 차감 전에 모두 검사한다.
        if (!CanHire(candidate, out failureReason))
        {
            Debug.LogWarning($"📝 HiringService: {failureReason}");
            return false;
        }

        GameObject spawnTemplate = ResolveSpawnTemplate(candidate);
        if (spawnTemplate == null)
        {
            failureReason = $"{candidate.ResolveDisplayName()}의 스폰 원본을 준비하지 못했습니다.";
            Debug.LogWarning($"📝 HiringService: {failureReason}");
            return false;
        }

        // 2. 비용 차감 (EconomyService 없으면 기존 디버그 편의를 유지한다.)
        if (candidate.hireCost > 0 && EconomyService.Instance != null)
        {
            if (!EconomyService.Instance.TrySpend(candidate.hireCost,
                    $"HiringService.TryHire: {candidate.ResolveDisplayName()}"))
            {
                failureReason = $"고용 비용 {candidate.hireCost:N0} G가 부족합니다.";
                Debug.Log($"💸 [{candidate.ResolveDisplayName()}] {failureReason}");
                return false;
            }
        }

        // 3. 스폰
        Vector3 spawnPos = ResolveSpawnPosition();
        Quaternion spawnRot = ResolveSpawnRotation();
        spawnedNpc = Instantiate(spawnTemplate, spawnPos, spawnRot);
        spawnedNpc.name = candidate.ResolveDisplayName();

        // 4. 후보 정체성 주입 — 원본 주민의 외형·기능 구성은 유지한다.
        InjectProfile(spawnedNpc, candidate);
        spawnedNpc.SetActive(true);

        // 5. 기록
        RegisterHired(candidate, spawnedNpc, BuildHiredNpcId(candidate));
        Debug.Log($"📝 고용 완료: [{candidate.ResolveDisplayName()}] ({candidate.specialty}) -{candidate.hireCost}G");

        OnHired?.Invoke(candidate, spawnedNpc);
        return true;
    }

    /// <summary>저장용 — 현재 고용된 후보 목록을 반환한다.</summary>
    public IReadOnlyCollection<NpcCandidateData> GetHiredCandidates() => _hired;

    /// <summary>Save-only snapshot containing candidate data and the spawned scene instance.</summary>
    public List<HiredNpcRuntimeRecord> GetHiredRuntimeRecords()
    {
        var records = new List<HiredNpcRuntimeRecord>(_hired.Count);
        foreach (var candidate in _hired)
        {
            if (candidate == null) continue;
            _spawnedByCandidate.TryGetValue(candidate, out GameObject instance);
            _hiredIds.TryGetValue(candidate, out string hiredNpcId);
            records.Add(new HiredNpcRuntimeRecord(
                string.IsNullOrEmpty(hiredNpcId) ? BuildHiredNpcId(candidate) : hiredNpcId,
                candidate,
                instance));
        }
        return records;
    }

    /// <summary>Restore a hired NPC without tier checks or money spending.</summary>
    public bool RestoreHiredNpc(
        NpcCandidateData candidate,
        string hiredNpcId,
        Vector3 position,
        Quaternion rotation,
        string objectName,
        out GameObject spawnedNpc)
    {
        spawnedNpc = null;
        if (candidate == null) return false;

        if (_hired.Contains(candidate))
        {
            if (_spawnedByCandidate.TryGetValue(candidate, out spawnedNpc) && spawnedNpc != null)
            {
                spawnedNpc.transform.SetPositionAndRotation(position, rotation);
                if (!string.IsNullOrEmpty(objectName)) spawnedNpc.name = objectName;
                return true;
            }

            _hired.Remove(candidate);
            _spawnedByCandidate.Remove(candidate);
            _hiredIds.Remove(candidate);
        }

        GameObject spawnTemplate = ResolveSpawnTemplate(candidate);
        if (spawnTemplate == null)
        {
            Debug.LogWarning($"📝 HiringService: [{candidate.ResolveDisplayName()}] 저장 복원용 주민 원본을 찾지 못했습니다.");
            return false;
        }

        spawnedNpc = Instantiate(spawnTemplate, position, rotation);
        spawnedNpc.name = string.IsNullOrEmpty(objectName) ? candidate.ResolveDisplayName() : objectName;
        InjectProfile(spawnedNpc, candidate);
        spawnedNpc.SetActive(true);
        RegisterHired(candidate, spawnedNpc, string.IsNullOrEmpty(hiredNpcId) ? BuildHiredNpcId(candidate) : hiredNpcId);
        OnHired?.Invoke(candidate, spawnedNpc);
        return true;
    }

    /// <summary>Restore a hired NPC at the configured spawn point.</summary>
    public bool RestoreHiredNpc(
        NpcCandidateData candidate,
        string hiredNpcId,
        string objectName,
        out GameObject spawnedNpc)
    {
        Vector3 position = ResolveSpawnPosition();
        Quaternion rotation = ResolveSpawnRotation();
        return RestoreHiredNpc(candidate, hiredNpcId, position, rotation, objectName, out spawnedNpc);
    }

    /// <summary>디버그/치트: 고용 목록을 초기화한다 (세이브 로드 전 사용).</summary>
    public void ClearHired(bool destroySpawnedInstances = false)
    {
        if (destroySpawnedInstances)
        {
            var spawned = new List<GameObject>(_spawnedByCandidate.Values);
            foreach (var go in spawned)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        _hired.Clear();
        _spawnedByCandidate.Clear();
        _hiredIds.Clear();
    }

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
        if (producer != null)
        {
            if (profile != null) producer.profile = profile;
            producer.specialty = candidate.specialty;
        }

        var specialist = npcGo.GetComponent<SpecialistNpcController>();
        if (specialist != null)
        {
            if (profile != null) specialist.profile = profile;
            specialist.ApplySpecialty(candidate.specialty);
            AssignMatchingSpecialistRecipes(specialist, candidate.specialty);
        }

        var schedule = npcGo.GetComponent<NpcScheduleController>();
        if (schedule != null && profile != null) schedule.profile = profile;

        // 원본 주민의 대화 데이터는 역할별로 재사용하되 친밀도 키는 고용 후보별로 분리한다.
        var dialogue = npcGo.GetComponent<NpcDialogue>();
        if (dialogue != null)
        {
            if (profile != null) dialogue.overrideProfile = profile;
            dialogue.friendshipId = BuildHiredNpcId(candidate);
        }
    }

    private void RegisterHired(NpcCandidateData candidate, GameObject spawnedNpc, string hiredNpcId)
    {
        if (candidate == null) return;
        _hired.Add(candidate);
        _hiredIds[candidate] = string.IsNullOrEmpty(hiredNpcId) ? BuildHiredNpcId(candidate) : hiredNpcId;
        if (spawnedNpc != null)
        {
            _spawnedByCandidate[candidate] = spawnedNpc;
            _runtimeSpawnInstanceIds.Add(spawnedNpc.GetInstanceID());
        }
    }

    private string BuildHiredNpcId(NpcCandidateData candidate)
    {
        return candidate != null ? candidate.name : string.Empty;
    }

    private void AssignMatchingSpecialistRecipes(SpecialistNpcController specialist, NpcSpecialty specialty)
    {
        if (specialist == null) return;

        WorkbenchType workbenchType = NpcSpecialtyMapping.GetWorkbenchType(specialty);
        if (workbenchType == WorkbenchType.None) return;

        var matching = new List<RecipeData>();
        RecipeData[] recipes = Resources.LoadAll<RecipeData>("Recipes");
        foreach (RecipeData recipe in recipes)
        {
            if (recipe == null || recipe.outputItem == null) continue;
            if (recipe.requiredWorkbench != workbenchType) continue;
            matching.Add(recipe);
        }

        matching.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        specialist.assignedRecipes = matching;
    }

    // -------- 내부: 스폰 원본 해결 --------

    private GameObject ResolveSpawnTemplate(NpcCandidateData candidate)
    {
        if (candidate == null) return null;

        // 향후 후보에 전용 프리팹이 연결되면 데이터가 계속 우선권을 가진다.
        if (candidate.spawnPrefab != null) return candidate.spawnPrefab;

        if (NpcSpecialtyMapping.IsCraftingSpecialty(candidate.specialty))
        {
            SpecialistNpcController[] specialists = FindObjectsByType<SpecialistNpcController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SpecialistNpcController specialist in specialists)
            {
                if (specialist == null || specialist.specialty != candidate.specialty) continue;
                if (IsUsableResidentTemplate(specialist.gameObject)) return specialist.gameObject;
            }
        }
        else if (candidate.specialty != NpcSpecialty.None)
        {
            ProducerNpcController[] producers = FindObjectsByType<ProducerNpcController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ProducerNpcController producer in producers)
            {
                if (producer == null || producer.specialty != candidate.specialty) continue;
                if (IsUsableResidentTemplate(producer.gameObject)) return producer.gameObject;
            }
        }

        return null;
    }

    private bool IsUsableResidentTemplate(GameObject source)
    {
        if (source == null || source.GetComponent<NpcController>() == null) return false;
        if (source.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) return false;
        if (_runtimeSpawnInstanceIds.Contains(source.GetInstanceID())) return false;

        foreach (GameObject hiredInstance in _spawnedByCandidate.Values)
        {
            if (hiredInstance == source) return false;
        }

        return true;
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
