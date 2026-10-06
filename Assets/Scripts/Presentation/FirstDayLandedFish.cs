using System.Collections;
using UnityEngine;

// Temporary world presentation. FishingSpot and the existing daily stock loop own the reward.
public sealed class FirstDayLandedFish : MonoBehaviour, IInteractable
{
    public Item Item { get; private set; }
    public int Hits { get; private set; }
    public bool ReadyToClaim => Hits >= 2;
    public bool OnLand { get; private set; }
    public Vector3 DryRest { get; private set; }
    FishingSpot _owner;
    Transform _visual;
    Quaternion _restRotation;
    Vector3 _visualRest;
    bool _pending, _claimed;
    float _nextHitAt, _landedAt;

    public static FirstDayLandedFish Create(FishingSpot owner, Item item, GameObject model, Vector3 from, Vector3 dryRest)
    {
        if (owner == null || item == null || model == null) return null;
        var root = new GameObject("LandedFish_" + item.id);
        root.transform.SetParent(owner.transform, true); root.transform.position = from;
        var fish = root.AddComponent<FirstDayLandedFish>();
        fish._owner = owner; fish.Item = item; fish.DryRest = dryRest;
        var visual = Instantiate(model, root.transform);
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Destroy(root); return null; }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.transform.localScale *= .85f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, .01f);
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.transform.position += root.transform.position - bounds.center + Vector3.up * (bounds.size.y / 2 + .05f);
        fish._visual = visual.transform;
        fish._visualRest = visual.transform.localPosition; fish._restRotation = visual.transform.localRotation;
        var contact = root.AddComponent<SphereCollider>(); contact.isTrigger = true;
        contact.radius = .42f; contact.center = Vector3.up * .25f;
        fish.StartCoroutine(fish.Land(from));
        return fish;
    }

    IEnumerator Land(Vector3 from)
    {
        for (float elapsed = 0; elapsed < .85f; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / .85f);
            transform.position = Vector3.Lerp(from, DryRest, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.5f);
            yield return null;
        }
        transform.position = DryRest; OnLand = true; _landedAt = Time.time;
        GatherFeedback.Splash();
    }

    void Update()
    {
        if (!OnLand || _visual == null || _claimed) return;
        float t = Time.time - _landedAt;
        float flop = ReadyToClaim ? 0 : Mathf.Max(0, Mathf.Sin(t * 8)) * .18f;
        // All motion stays on the checked dry cell. The fish cannot jump into the sea or a building.
        transform.position = DryRest + new Vector3(Mathf.Sin(t * 1.9f), 0, Mathf.Cos(t * 2.3f)) * (ReadyToClaim ? 0 : .12f);
        _visual.localPosition = _visualRest + Vector3.up * flop;
        _visual.localRotation = _restRotation * Quaternion.Euler(0, Mathf.Sin(t * 2) * 12, ReadyToClaim ? 25 : Mathf.Sin(t * 8) * 32);
    }

    public static bool CanStrike(Item held) => held != null &&
        (held.toolType == ToolType.Axe || held.toolType == ToolType.Pickaxe || held.toolType == ToolType.Weapon);

    bool InReach(GameObject player)
    {
        if (player == null || player.GetComponent<Inventory>() != Inventory.instance) return false;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
        return delta.sqrMagnitude <= 2.05f * 2.05f && delta.sqrMagnitude > .0025f &&
            Mathf.Abs(transform.position.y - player.transform.position.y) < 2 &&
            Vector3.Dot(player.transform.forward, delta.normalized) >= .75f;
    }

    public void Interact(GameObject player)
    {
        if (!OnLand || _claimed || _pending || _owner == null || !InReach(player)) return;
        if (ReadyToClaim) { Claim(player); return; }
        if (!CanStrike(EquipmentSystem.CurrentHeld(player)) || Time.time < _nextHitAt) return;
        _pending = true; _nextHitAt = Time.time + .55f;
        StartCoroutine(Contact(player));
    }

    IEnumerator Contact(GameObject player)
    {
        var inventory = player.GetComponent<Inventory>();
        var tool = inventory != null ? inventory.GetSelectedInstance() : null;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _pending = false;
        if (_claimed || !InReach(player) || !CanStrike(EquipmentSystem.CurrentHeld(player)) ||
            inventory == null || inventory.GetSelectedInstance() != tool) yield break;
        Hits++;
        GatherFeedback.Splash();
        ToolDurability.ConsumeSuccess(inventory, tool);
        if (ReadyToClaim) Claim(player);
    }

    void Claim(GameObject player)
    {
        if (!_owner.TryClaimLandedFish(this, player)) return;
        _claimed = true; gameObject.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        string name = ItemDisplayName.For(Item);
        if (!OnLand) return name + " · 끌어올리는 중";
        if (ReadyToClaim) return name + " · 가방을 비우고 담기";
        return CanStrike(EquipmentSystem.CurrentHeld(Inventory.instance != null ? Inventory.instance.gameObject : null))
            ? name + " · 도구로 포획 (" + Hits + "/2)"
            : name + " · 도끼나 곡괭이를 들고 포획하세요";
    }
}
