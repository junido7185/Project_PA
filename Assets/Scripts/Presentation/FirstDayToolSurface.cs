using System.Collections;
using UnityEngine;

// Existing scenery can react to a tool without becoming a second resource/save authority.
public sealed class FirstDayToolSurface : MonoBehaviour, IInteractable
{
    public bool Stone { get; private set; }
    bool _reacting;
    public static void Attach(GameObject model, bool stone)
    {
        if (model.GetComponent<FirstDayToolSurface>() != null) return;
        var surface = model.AddComponent<FirstDayToolSurface>(); surface.Stone = stone;
        var contact = new GameObject("ToolSurfaceContact");
        contact.transform.SetParent(model.transform, false);
        contact.transform.position = model.transform.position + Vector3.up * (stone ? .45f : .85f);
        var collider = contact.AddComponent<SphereCollider>(); collider.isTrigger = true;
        collider.radius = .5f / Mathf.Max(.001f, Mathf.Abs(contact.transform.lossyScale.x));
    }

    public void Interact(GameObject player)
    {
        if (_reacting || player == null || player.GetComponent<Inventory>() != Inventory.instance) return;
        var equipment = player.GetComponent<EquipmentSystem>();
        if (equipment == null) return;
        StartCoroutine(React(player, equipment));
    }

    IEnumerator React(GameObject player, EquipmentSystem equipment)
    {
        _reacting = true;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        if (player == null || PlayerInputHandler.ModalOpen) { _reacting = false; yield break; }
        Vector3 flat = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
        if (flat.sqrMagnitude > 2.05f * 2.05f || Vector3.Dot(player.transform.forward, flat.normalized) < .75f)
        { equipment.FailedUse("빗나갔어요.", false); _reacting = false; yield break; }
        var held = EquipmentSystem.CurrentHeld(player);
        bool matches = held != null && held.toolType == (Stone ? ToolType.Pickaxe : ToolType.Axe);
        string message = matches ? Stone ? "이 바위는 지금 캘 수 없어요." : "이 나무는 지금 벨 수 없어요." :
            Stone ? "바위는 곡괭이로 캘 수 있어요." : "나무는 도끼로 벨 수 있어요.";
        equipment.FailedUse(message, true); GatherFeedback.ToolBlocked();
        FirstDayWorldPresentation.Toast(message, false);
        Quaternion rest = transform.rotation;
        for (float elapsed = 0; elapsed < .22f; elapsed += Time.deltaTime)
        {
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(elapsed * 45f) * (1 - elapsed / .22f) * (Stone ? 1 : 3), Vector3.forward) * rest;
            yield return null;
        }
        transform.rotation = rest; _reacting = false;
    }

    public string GetInteractPrompt() => Stone ? "바위 두드리기" : "나무 두드리기";
}
