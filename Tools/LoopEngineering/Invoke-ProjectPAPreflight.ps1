param(
    [string]$ProjectRoot = "",
    [switch]$AsJson
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $ProjectRoot = (Resolve-Path $ProjectRoot).Path
}

$expectedRoot = "C:\Users\sdjsd\Desktop\Unity\Project_PA"
$referenceRoot = "C:\Users\sdjsd\Desktop\Unity\Project_D\Project_D"
$readyStatus = "READY_FOR_BOUNDED_TICKET_LOOP"
$status = $readyStatus
$reasons = New-Object System.Collections.Generic.List[string]

function Add-BlockReason {
    param([string]$Reason)
    if (-not $reasons.Contains($Reason)) {
        [void]$reasons.Add($Reason)
    }
}

if ($ProjectRoot -ne $expectedRoot) {
    $status = "BLOCKED_BY_WRONG_ROOT"
    Add-BlockReason "Current root is not Project_PA."
}

$requiredFolders = @("Assets", "Packages", "ProjectSettings")
$missingFolders = @()
foreach ($folder in $requiredFolders) {
    if (-not (Test-Path (Join-Path $ProjectRoot $folder))) {
        $missingFolders += $folder
    }
}
if ($missingFolders.Count -gt 0) {
    $status = "BLOCKED_BY_WRONG_ROOT"
    Add-BlockReason ("Missing Unity project folders: " + ($missingFolders -join ", "))
}

Push-Location $ProjectRoot
try {
    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
        $gitRoot = ""
        Add-BlockReason "Git root could not be resolved."
    }

    $gitStatus = (& git status --short --branch 2>$null)
    $dirtyGit = ($gitStatus | Where-Object { $_ -and ($_ -notlike "## *") }).Count -gt 0
}
finally {
    Pop-Location
}

$policyPath = Join-Path $ProjectRoot "Automation\LoopEngineering\loop-policy.json"
$policyValid = $false
$policy = $null
if (Test-Path $policyPath) {
    try {
        $policy = Get-Content $policyPath -Raw | ConvertFrom-Json
        $policyValid = $true
    }
    catch {
        Add-BlockReason "loop-policy.json is not valid JSON."
    }
} else {
    Add-BlockReason "loop-policy.json is missing."
}

if ($policyValid -and $policy.requireCleanGitBaseline -and $dirtyGit) {
    if ($status -eq $readyStatus) { $status = "BLOCKED_BY_DIRTY_GIT" }
    Add-BlockReason "Git working tree is dirty and policy requires a clean baseline."
}

$unityProcesses = @(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*Project_PA*" })
if ($unityProcesses.Count -gt 0) {
    if ($status -eq $readyStatus) { $status = "BLOCKED_BY_OPEN_UNITY" }
    Add-BlockReason "Unity Editor or worker process is running for Project_PA."
}

$crashReports = @(Get-ChildItem -Path $ProjectRoot -Filter "PROJECT_PA_CRASH_REPORT_*.md" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending)
$latestCrashReport = if ($crashReports.Count -gt 0) { $crashReports[0].Name } else { $null }
$crashResolutionPath = Join-Path $ProjectRoot "Automation\LoopEngineering\State\crash-resolution.json"
$crashResolutionValid = $false
$crashResolutionStatus = $null
$approvedGraphicsBackend = $null
$requiresForceD3D11 = $null
$requireCrashResolutionRecord = $policyValid -and ($policy.PSObject.Properties.Name -contains "requireCrashResolutionRecord") -and $policy.requireCrashResolutionRecord

if ($policyValid -and $policy.stopOnUnityCrashArtifact -and $latestCrashReport) {
    if ($requireCrashResolutionRecord) {
        if (-not (Test-Path $crashResolutionPath)) {
            Add-BlockReason "Crash report exists and crash-resolution.json is missing."
            if ($status -eq $readyStatus) { $status = "BLOCKED_BY_UNRESOLVED_CRASH" }
        } else {
            try {
                $crashResolution = Get-Content $crashResolutionPath -Raw | ConvertFrom-Json
                $crashResolutionStatus = $crashResolution.status
                $approvedGraphicsBackend = $crashResolution.approvedGraphicsBackend
                $requiresForceD3D11 = $crashResolution.requiresForceD3D11

                $crashResolutionValid =
                    $crashResolutionStatus -eq "human_verified_d3d11_stable" -and
                    $approvedGraphicsBackend -eq "D3D11" -and
                    $requiresForceD3D11 -eq $true

                if (-not $crashResolutionValid) {
                    Add-BlockReason "Crash report exists but crash-resolution.json does not approve the D3D11 stability baseline."
                    if ($status -eq $readyStatus) { $status = "BLOCKED_BY_UNRESOLVED_CRASH" }
                }
            }
            catch {
                Add-BlockReason "crash-resolution.json is not valid JSON."
                if ($status -eq $readyStatus) { $status = "BLOCKED_BY_UNRESOLVED_CRASH" }
            }
        }
    } else {
        Add-BlockReason "Crash report exists; review it before launch-heavy automation."
        if ($status -eq $readyStatus) { $status = "NEEDS_HUMAN_BASELINE" }
    }
}

$referenceExists = Test-Path $referenceRoot
$referenceGitStatus = $null
if ($referenceExists -and (Test-Path (Join-Path $referenceRoot ".git"))) {
    Push-Location $referenceRoot
    try {
        $referenceGitStatus = (& git status --short --branch 2>$null)
    }
    finally {
        Pop-Location
    }
}

$result = [ordered]@{
    status = $status
    projectRoot = $ProjectRoot
    expectedRoot = $expectedRoot
    gitRoot = $gitRoot
    dirtyGit = $dirtyGit
    missingUnityFolders = $missingFolders
    unityProcessCount = $unityProcesses.Count
    unityProcesses = @($unityProcesses | ForEach-Object {
        [ordered]@{
            processId = $_.ProcessId
            commandLine = $_.CommandLine
        }
    })
    latestCrashReport = $latestCrashReport
    crashResolutionPath = $crashResolutionPath
    crashResolutionValid = $crashResolutionValid
    crashResolutionStatus = $crashResolutionStatus
    approvedGraphicsBackend = $approvedGraphicsBackend
    requiresForceD3D11 = $requiresForceD3D11
    policyPath = $policyPath
    policyValid = $policyValid
    policyMode = if ($policyValid) { $policy.mode } else { $null }
    referenceRootExists = $referenceExists
    referenceGitStatus = $referenceGitStatus
    reasons = @($reasons)
    checkedAt = (Get-Date).ToString("s")
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
} else {
    "Project PA Preflight: $status"
    "ProjectRoot: $ProjectRoot"
    "GitRoot: $gitRoot"
    "DirtyGit: $dirtyGit"
    "UnityProcessCount: $($unityProcesses.Count)"
    "LatestCrashReport: $latestCrashReport"
    "CrashResolutionValid: $crashResolutionValid"
    "CrashResolutionStatus: $crashResolutionStatus"
    "ApprovedGraphicsBackend: $approvedGraphicsBackend"
    "PolicyValid: $policyValid"
    "PolicyMode: $($result.policyMode)"
    if ($reasons.Count -gt 0) {
        "Reasons:"
        foreach ($reason in $reasons) { "- $reason" }
    }
}

if ($status -eq $readyStatus) {
    exit 0
}

exit 2
