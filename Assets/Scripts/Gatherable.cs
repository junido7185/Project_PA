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
    bool _directDepleted, _directRewardPending;
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
        _directDepleted = state != null && state.consumed && day < state.respawnDay;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = !_directDepleted;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = !_directDepleted;
        return !_directDepleted;
    }

    string DirectPrompt => _directDepleted ? "채집 완료" : _directFeedback + $" · 남은 타격 {_directHits}";

    void InteractDirect(GameObject interactor)
    {
        if (_directRewardPending || !RefreshDirectState() || interactor == null) return;
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
        if (_directHits > 1)
        {
            _directHits--;
            _directHitAt = Time.time;
            _directFeedback = "도끼 타격";
            return;
        }
        // AddInstance accepts the whole reward or changes nothing. Keep the final hit on failure.
        _directRewardPending = true;
        bool added = inventory.AddInstance(new ItemInstance(_directReward, 1));
        if (!added)
        {
            _directRewardPending = false;
            _directFeedback = "Inventory full - free a slot and try again";
            return;
        }
        _directHits = 0;
        _directDepleted = true;
        // Persist depletion only after successful reward; direct resources do not auto-regrow.
        if (!_directPersistence.SetResourceState(_directSpawnKey, true, int.MaxValue))
            Debug.LogError("Direct resource persistence rejected a previously validated spawn: " + _directSpawnKey);
        _directFeedback = "Wood +1 - gathered";
        FirstDayWorldPresentation.Acquired(_directReward, 1, transform.position);
        RefreshDirectState();
        _directRewardPending = false;
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
