# Project PA Unity Editor Crash Report - 2026-06-25

## Summary

Unity Editor did not crash because of a C# compile error. The latest crash was a native graphics-device crash in Direct3D 12.

Project root checked:

- `C:\Users\sdjsd\Desktop\Unity\Project_PA`

## Evidence

Latest Unity Editor log:

- `C:\Users\sdjsd\AppData\Local\Unity\Editor\Editor.log`

Latest Unity crash folder:

- `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-06-25_072048185`

Key log lines:

```text
d3d12: swapchain present failed (887a0005).
d3d12: Device failed error (887a0005).
d3d12: Device removed reason (887a0006).
d3d12: GfxDevice was not out of Local memory
d3d12: GfxDevice was not out of Non-Local memory
Unrecoverable D3D12 device error!
0x... D3D12SwapChain::Present
0x... GfxTaskExecutorD3D12::DoPresent
```

Interpretation:

- `887a0005` is a device removed/device failed class error.
- `887a0006` indicates the graphics device hung.
- The log says the editor was not out of local or non-local GPU memory.
- The native stack ends in D3D12 present/swapchain code, not a Project_PA C# stack.

## Cause Classification

Most likely cause:

- Unity Editor + D3D12 + NVIDIA driver/device removed path.

Not indicated by the log:

- C# compiler error.
- Managed runtime exception as the primary crash cause.
- NavMesh startup failure.
- Project_PA save/load logic.
- Project_D or copied reference assets.

## Repair Applied

Changed `ProjectSettings/ProjectSettings.asset` so Standalone graphics API is fixed to Direct3D 11 instead of allowing Unity to auto-select D3D12.

Relevant setting after repair:

```yaml
m_BuildTargetGraphicsAPIs:
- m_BuildTarget: Standalone
  m_APIs: 02000000
  m_Automatic: 0
```

`02000000` is Direct3D 11 in Unity's serialized graphics API list.

## Verification

1. D3D11 forced load:

   - Log: `Logs/Codex_CrashRepair_D3D11_Load.log`
   - Result: passed.
   - Evidence:

```text
Forcing GfxDevice: Direct3D 11
Version: Direct3D 11.0 [level 11.1]
Tundra build success
Exiting batchmode successfully now!
```

2. Project graphics API load without `-force-d3d11`:

   - Log: `Logs/Codex_CrashRepair_ProjectGraphicsAPI_Load.log`
   - Result: passed.
   - Evidence:

```text
Version: Direct3D 11.0 [level 11.1]
Tundra build success
```

3. Final route regression after repair:

   - Log: `Logs/Codex_CrashRepair_FinalRoute_D3D11.log`
   - Result: passed.
   - Evidence:

```text
Version: Direct3D 11.0 [level 11.1]
PA Final Route Validation passed. stocked=BreadLoaf, paid=30G
PA Final Route Validation finished successfully.
```

## Follow-Up If Crash Reappears

If Unity still crashes after this repair:

1. Launch Unity once with:

```text
"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe" -projectPath "C:\Users\sdjsd\Desktop\Unity\Project_PA" -force-d3d11
```

2. Update or roll back the NVIDIA driver. The crash log reported driver version `32.0.15.8088`.
3. Avoid switching Project Settings back to automatic graphics API or D3D12 until the driver/device issue is resolved.
4. If investigating D3D12 specifically, use `-force-d3d12-debug` only for diagnosis, not normal development.

