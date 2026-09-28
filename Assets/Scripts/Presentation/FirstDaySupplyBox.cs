using System.Collections;
using System.Linq;
using UnityEngine;

// Owns only this supply interaction's delivered entries. Inventory owns every received item.
public sealed class FirstDaySupplyBox : MonoBehaviour, IInteractable
{
    public bool Collected { get; private set; }
    int _next;
    bool _busy;
    GameObject _visual;

    void Start()
    {
        _visual = FirstDayStudioAssets.Place(FirstDayStudioAssets.Load().chestClosed, transform, transform.position, .85f);
        // Canon v2 §3: supply box must contain Workbench Kit (id 2014) among other tools/blueprints.
        // Log a diagnostic if key items are absent so the editor can identify missing SO entries.
        var supplies = FirstDayStudioAssets.Load()?.supplies;
        if (supplies == null || supplies.Length == 0)
        {
            Debug.LogWarning("[SupplyBox] FirstDayStudioAssets.supplies is empty — supply box will deliver nothing.");
            return;
        }
        bool hasWorkbench = supplies.Any(i => i != null && i.id == 2014);
        bool hasTent      = supplies.Any(i => i != null && i.id == 2013);
        bool hasShop      = supplies.Any(i => i != null && i.id == 2012);
        if (!hasWorkbench) Debug.LogWarning("[SupplyBox] Canon v2 §3: Field Workbench Kit (id 2014) not found in supplies. Add it to FirstDayStudio/Assets SO.");
        if (!hasTent)      Debug.LogWarning("[SupplyBox] Canon v2 §3: Resident Tent Blueprint (id 2013) not found in supplies.");
        if (!hasShop)      Debug.LogWarning("[SupplyBox] Canon v2 §3: Pioneer Shop/Base Blueprint (id 2012) not found in supplies.");
    }

    public string GetInteractPrompt() => "P.A. Pioneer Supply 열기";

    public void Interact(GameObject interactor)
    {
        if (_busy || Collected || EquipmentSystem.CurrentHeld(interactor) != null) return;
        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null) return;
        _busy = true;
        var supplies = FirstDayStudioAssets.Load().supplies;
        while (_next < supplies.Length)
        {
            var item = supplies[_next];
            // Smartphone (id 2010) is a system UI (P key) — not a physical hotbar item.
            if (item != null && item.id == 2010) { _next++; continue; }
            // Canon v2 §3: Resident Tent Blueprint x2, all others x1.
            int qty = (item != null && item.id == 2013) ? 2 : 1;
            if (!inventory.TryReceiveToHotbar(item, qty))
            { _busy = false; FirstDayWorldPresentation.Toast("가방에 자리를 만든 뒤 남은 보급품을 받아주세요."); return; }
            _next++;
        }
        Collected = true;
        FirstDayWorldPresentation.Toast("보급품을 받았어요. 1–9 선택 · X 빈손 · E 상호작용");
        StartCoroutine(OpenAndPack());
    }

    IEnumerator OpenAndPack()
    {
        if (_visual != null) { _visual.SetActive(false); Destroy(_visual); }
        _visual = FirstDayStudioAssets.Place(FirstDayStudioAssets.Load().chestOpen, transform, transform.position, .85f);
        yield return new WaitForSeconds(1.25f);
        Vector3 scale = transform.localScale;
        for (float t = 0; t < .6f; t += Time.deltaTime)
        { transform.localScale = scale * Mathf.SmoothStep(1, 0, t / .6f); yield return null; }
        gameObject.SetActive(false);
    }
}
