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
        string message = Stone ? "장식 바위는 채광할 수 없어요. '기본 곡괭이 가능' 표시를 찾아주세요." :
            "장식 나무는 벌목할 수 없어요. '기본 도끼 가능' 표시를 찾아주세요.";
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

    public string GetInteractPrompt() => Stone ? "장식 바위 · 채광 불가" : "장식 나무 · 벌목 불가";
}
