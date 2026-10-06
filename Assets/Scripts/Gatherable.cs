using UnityEngine;

public class Gatherable : MonoBehaviour, IInteractable
{
    public Item dropItem; // ⭐ ItemData -> Item
    public ToolType requiredTool;


    // DIRECT-GATHERING-01A: explicit generated-resource mode; legacy behavior stays intact.
    WorldPersistenceService _directPersistence;
    WorldGenerationResult _directWorld;
    Item _directReward;
    string _directSpawnKey;
    int _directHits;
    bool _directDepleted, _contactPending, _felledPendingClaim;
    float _directHitAt = -10f;
    Vector3 _directBaseScale;
    string _directFeedback;
    public bool IsDirectWorld => _directWorld != null;
    public string DirectSpawnKey => _directSpawnKey;
    public int DirectRemainingHits => _directHits;
    public bool DirectDepleted => _directDepleted;

    public void ConfigureDirectWorld(WorldPersistenceService persistence,
        WorldResourceSpawnRecord spawn, Item reward, int hits = 3)
    {
        if (persistence == null || persistence.ActiveGeneratedWorld == null || spawn.Kind != WorldResourceKind.Timber || reward == null)
            throw new System.ArgumentException("Invalid direct Timber binding.");
        bool registered = false;
        foreach (var candidate in persistence.ActiveGeneratedWorld.ResourceSpawns)
            if (candidate.SpawnKey == spawn.SpawnKey && candidate.Kind == spawn.Kind && candidate.Coordinate == spawn.Coordinate)
                registered = true;
        if (!registered) throw new System.ArgumentException("Direct resource spawn is not registered.");
        _directPersistence = persistence;
        _directWorld = persistence.ActiveGeneratedWorld;
        _directSpawnKey = spawn.SpawnKey;
        _directReward = reward;
        _directHits = Mathf.Clamp(hits, 2, 3);
        _directBaseScale = transform.localScale;
        _directFeedback = "도끼로 나무 채집";
        RefreshDirectState();
    }

    bool RefreshDirectState()
    {
        if (_directPersistence == null || !ReferenceEquals(_directWorld, _directPersistence.ActiveGeneratedWorld))
            return false;
        var states = _directPersistence.CaptureState(transform.position, null).resourceStates;
        var state = states?.Find(entry => entry != null && entry.spawnKey == _directSpawnKey);
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        // 베어 낸 뒤 드롭을 줍기 전까지는 세션에서만 사라진 상태다(저장에는 줍기 후 확정).
        _directDepleted = _felledPendingClaim || state != null && state.consumed && day < state.respawnDay;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = !_directDepleted;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = !_directDepleted;
        return !_directDepleted;
    }

    string DirectPrompt => _directDepleted ? "채집 완료" : _directFeedback + $" · 남은 타격 {_directHits}";

    void InteractDirect(GameObject interactor)
    {
        if (_contactPending || !RefreshDirectState() || interactor == null) return;
        var inventory = interactor.GetComponent<Inventory>();
        Vector3 delta = interactor.transform.position - transform.position;
        if (inventory == null || inventory != Inventory.instance ||
            Vector3.ProjectOnPlane(delta, Vector3.up).magnitude > 2.05f || Mathf.Abs(delta.y) > 2f) return;
        if (EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Axe)
        {
            _directFeedback = "도끼를 손에 들어주세요";
            return;
        }
        if (Time.time - _directHitAt < .25f) return;
        StartCoroutine(DirectContact(interactor, inventory));
    }

    // 스윙이 나무에 닿는 순간 타격이 성립한다. 그 사이의 연타·도구 교체는 타격/보상을 만들지 않는다.
    System.Collections.IEnumerator DirectContact(GameObject interactor, Inventory inventory)
    {
        _contactPending = true;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _contactPending = false;
        if (interactor == null || _directDepleted || EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Axe) yield break;
        _directHitAt = Time.time;
        Vector3 contact = Vector3.Lerp(transform.position, interactor.transform.position, .35f) + Vector3.up * 1.0f;
        GatherFeedback.Hit(contact, false);
        if (_directHits > 1)
        {
            _directHits--;
            _directFeedback = "도끼 타격";
            yield break;
        }
        // 성공한 작업 결과: 나무가 쓰러지고 Wood가 바닥에 떨어진다. 도구 내구도는 이 결과에만 줄어든다.
        _directHits = 0;
        _felledPendingClaim = true;
        RefreshDirectState();
        _directFeedback = "나무를 베었어요";
        ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        GatherFeedback.SpawnDrop(_directReward, transform.position, interactor.transform.position, ClaimDirectDrop);
    }

    void ClaimDirectDrop()
    {
        _felledPendingClaim = false;
        // Persist depletion only after the dropped reward is in the bag; direct resources do not auto-regrow.
        if (_directPersistence == null || !_directPersistence.SetResourceState(_directSpawnKey, true, int.MaxValue))
            Debug.LogError("Direct resource persistence rejected a previously validated spawn: " + _directSpawnKey);
        RefreshDirectState();
    }

    void Update()
    {
        if (!IsDirectWorld) return;
        float elapsed = Time.time - _directHitAt;
        transform.localScale = _directBaseScale * (1f + (elapsed < .22f ? .1f * Mathf.Sin(elapsed / .22f * Mathf.PI) : 0f));
    }

    public void Interact(GameObject interactor)
    {
        if (IsDirectWorld) { InteractDirect(interactor); return; }
        Item heldItem = EquipmentSystem.CurrentHeld(interactor);

        if (requiredTool != ToolType.None)
        {
            if (heldItem == null || heldItem.toolType != requiredTool)
            {
                Debug.Log($"🚫 {requiredTool}이(가) 필요합니다.");
                return; 
            }
        }

        Animator playerAnim = interactor.GetComponentInChildren<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("DoChop");

        Harvest(); 
    }

    public string GetInteractPrompt() => IsDirectWorld ? DirectPrompt : $"{dropItem.itemName} 채집하기";
    
    public void Harvest()
    {
        if (IsDirectWorld) return; // Do not bypass direct-mode hit and reward checks.
        if (dropItem != null)
        {
            Inventory.instance.AddItem(dropItem);
        }
        Destroy(gameObject);
    }
}
