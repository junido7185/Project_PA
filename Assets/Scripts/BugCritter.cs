using UnityEngine;

// Small demo critter. Inventory remains the only reward authority.
public sealed class BugCritter : MonoBehaviour, IInteractable
{
    Item _reward;
    Vector3 _home;
    float _phase, _elapsed;
    bool _captured, _claiming;
    string _feedback;
    Transform _leftWing, _rightWing;

    public bool Captured => _captured;
    public Vector3 Home => _home;
    public const float RoamRadius = .45f;

    public void Configure(Item reward, float phase)
    {
        _reward = reward;
        _home = transform.position;
        _phase = phase;
        _leftWing = transform.Find("LeftWing");
        _rightWing = transform.Find("RightWing");
    }

    void Update()
    {
        if (_captured) return;
        _elapsed += Time.deltaTime;
        // Bounded idle drift, with no global random state or navigation ownership.
        float t = _elapsed + _phase;
        transform.position = _home + new Vector3(Mathf.Sin(t * .65f) * RoamRadius,
            Mathf.Sin(t * 2f) * .08f, Mathf.Cos(t * .43f) * RoamRadius);
        float flap = Mathf.Sin(t * 15f) * 35f;
        if (_leftWing != null) _leftWing.localRotation = Quaternion.Euler(0, 0, flap);
        if (_rightWing != null) _rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
    }

    public void Interact(GameObject interactor)
    {
        if (_captured || _claiming || !isActiveAndEnabled || interactor == null || _reward == null) return;
        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || inventory != Inventory.instance) return;
        if (EquipmentSystem.CurrentHeld(interactor) == null || EquipmentSystem.CurrentHeld(interactor).toolType != ToolType.Net)
        { _feedback = "Select Net in the hotbar to catch a butterfly."; return; }
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - interactor.transform.position, Vector3.up);
        if (delta.sqrMagnitude > 2.05f * 2.05f || delta.sqrMagnitude < .0025f ||
            Mathf.Abs(transform.position.y - interactor.transform.position.y) > 2f ||
            Vector3.Dot(interactor.transform.forward, delta.normalized) < .75f)
        { _feedback = "Move closer and face the butterfly."; return; }

        _claiming = true;
        bool added;
        try { added = inventory.AddInstance(new ItemInstance(_reward)); }
        finally { _claiming = false; }
        if (!added)
        { _feedback = "Bag full. Make room and try the Net again."; return; }
        _captured = true;
        _feedback = "Butterfly caught!";
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
    }

    public string GetInteractPrompt() => _feedback ?? "Butterfly: select Net and press Space.";
}
