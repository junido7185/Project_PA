using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldBuildingPlacementService))]
[RequireComponent(typeof(WorldGridDebugView))]
public sealed class WorldBuildingPlacementDebugController : MonoBehaviour
{
    WorldBuildingPlacementService _placement;
    WorldGridDebugView _gridDebug;
    int _quarterTurns;

    void Awake()
    {
        _placement = GetComponent<WorldBuildingPlacementService>();
        _gridDebug = GetComponent<WorldGridDebugView>();
    }

    void Update()
    {
        if (_placement == null || _gridDebug == null) return;

        if (_placement.HasPreview && _gridDebug.HasSelectedCell)
            _placement.UpdatePreview(_gridDebug.SelectedCell, _quarterTurns);

        if (Input.GetKeyDown(KeyCode.B) && _gridDebug.HasSelectedCell)
        {
            _quarterTurns = 0;
            _placement.BeginPlacementPreview(
                WorldBuildingPlacementService.PrototypeInstanceId,
                _gridDebug.SelectedCell,
                _quarterTurns);
        }
        if (Input.GetKeyDown(KeyCode.M) && _gridDebug.HasSelectedCell &&
            _placement.TryGetPlacement(
                WorldBuildingPlacementService.PrototypeInstanceId,
                out WorldPlacedBuildingRuntime existing))
        {
            _quarterTurns = existing.QuarterTurns;
            _placement.BeginMovePreview(existing.InstanceId, _gridDebug.SelectedCell, _quarterTurns);
        }
        if (_placement.HasPreview && Input.GetKeyDown(KeyCode.Q))
        {
            _quarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(
                _quarterTurns - 1);
            _placement.UpdatePreview(_gridDebug.SelectedCell, _quarterTurns);
        }
        if (_placement.HasPreview && Input.GetKeyDown(KeyCode.E))
        {
            _quarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(
                _quarterTurns + 1);
            _placement.UpdatePreview(_gridDebug.SelectedCell, _quarterTurns);
        }
        if (_placement.HasPreview &&
            (Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            _placement.CommitPreview();
        }
        if (_placement.HasPreview && Input.GetKeyDown(KeyCode.Escape))
            _placement.CancelPreview();
    }

    void OnGUI()
    {
        if (_placement == null) return;
        string state = _placement.HasPreview
            ? _placement.LastResult.Succeeded
                ? "VALID — Enter to confirm"
                : $"BLOCKED — {_placement.LastResult.Failure}"
            : _placement.RegisteredCount == 0
                ? "Select a cell, then B to place the storage shed"
                : "M moves the storage shed from the selected anchor";

        GUILayout.BeginArea(new Rect(18f, 208f, 690f, 66f), GUI.skin.box);
        GUILayout.Label("WORLD-005  RELOCATABLE STORAGE SHED");
        GUILayout.Label($"{state}  |  Q/E rotate  Enter confirm  Esc cancel");
        GUILayout.EndArea();
    }
}
