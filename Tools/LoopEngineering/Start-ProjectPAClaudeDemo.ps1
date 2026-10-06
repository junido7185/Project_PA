[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$PrepareSession,
    [ValidateSet('max', 'xhigh', 'high')][string]$Effort = 'max',
    [ValidateSet('auto', 'acceptEdits')][string]$PermissionMode = 'auto',
    [ValidateRange(30, 600)][int]$StartupTimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\', '/')
$expectedRoot = 'C:\Users\sdjsd\Desktop\Unity\Project_PA'
if (-not [string]::Equals($projectRoot, $expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Launcher must remain inside the approved Project_PA root.'
}

$state = Get-Content -LiteralPath (Join-Path $projectRoot 'Automation/LoopEngineering/State/loop-state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$routing = $state.demoProductionRouting
$approvalProperty = $state.PSObject.Properties[$routing.selectedApproval]
if ($routing.unityEditorOwner -ne 'claude-code' -or $null -eq $approvalProperty) {
    throw 'Claude production ownership/selected approval is unavailable. No ownership is inferred or changed.'
}
$approval = $approvalProperty.Value
$productionStartAllowed = -not $routing.stopAtMilestoneReached -and $approval.status -in @('approved_not_started', 'preapproved', 'active', 'in_progress')
if (-not $PrepareSession -and -not $CheckOnly -and -not $productionStartAllowed) {
    throw 'The recorded milestone/approval does not allow a production start.'
}
if ($approval.approvedTickets -notcontains $approval.activeTicket) { throw 'Active ticket is outside the selected approval.' }

$promptPath = Join-Path $projectRoot 'AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md'
$preparePromptPath = Join-Path $projectRoot 'AI_WORKFLOW/03_TASKS/CLAUDE_DEMO_PREPARATION_PROMPT.md'
$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
$claudeCommand = Get-Command claude -CommandType Application -ErrorAction Stop
if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf) -or -not (Test-Path -LiteralPath $promptPath -PathType Leaf)) {
    throw 'Existing Unity executable or reviewed production prompt is missing. This launcher installs nothing.'
}
if (-not (Test-Path -LiteralPath $preparePromptPath -PathType Leaf)) {
    throw 'The UTF-8 preparation prompt is missing.'
}
# Keep non-ASCII prompt text out of .ps1 literals: Windows PowerShell 5.1 reads BOM-less scripts as ANSI.
$preparePrompt = Get-Content -LiteralPath $preparePromptPath -Raw -Encoding UTF8
$preparePromptHasHangul = [regex]::IsMatch($preparePrompt, '[\uAC00-\uD7A3]')
if (-not $preparePromptHasHangul -or $preparePrompt.Contains([string][char]0xFFFD)) {
    throw 'The preparation prompt must contain valid UTF-8 Korean text.'
}

# 사용자 프로필의 UnityMCP/unityMCP 중복을 이번 세션에서만 피한다.
# 설치된 claude-mem MCP와 plugin hooks는 유지하고, 전역 설정은 수정하지 않는다.
$mcpConfigPaths = @((Join-Path $projectRoot '.mcp.json'))
$claudeUserDirectory = 'C:\Users\sdjsd\.claude'
$userSettingsPath = Join-Path $claudeUserDirectory 'settings.json'
$pluginRegistryPath = Join-Path $claudeUserDirectory 'plugins/installed_plugins.json'
if ((Test-Path -LiteralPath $userSettingsPath) -and (Test-Path -LiteralPath $pluginRegistryPath)) {
    $userSettings = Get-Content -LiteralPath $userSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($userSettings.enabledPlugins.'claude-mem@thedotmack' -eq $true) {
        $pluginRegistry = Get-Content -LiteralPath $pluginRegistryPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $memoryInstall = @($pluginRegistry.plugins.'claude-mem@thedotmack') | Where-Object { $_.scope -eq 'user' } | Select-Object -First 1
        if ($null -ne $memoryInstall) {
            $memoryMcpConfig = Join-Path $memoryInstall.installPath '.mcp.json'
            if (Test-Path -LiteralPath $memoryMcpConfig -PathType Leaf) { $mcpConfigPaths += $memoryMcpConfig }
        }
    }
}

if ($CheckOnly) {
    $settings = Get-Content -LiteralPath (Join-Path $projectRoot '.claude/settings.local.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    [ordered]@{
        projectRoot = $projectRoot
        owner = $routing.unityEditorOwner
        selectedApproval = $routing.selectedApproval
        activeTicket = $approval.activeTicket
        stopAtMilestone = $approval.stopAtMilestone
        productionStartAllowed = $productionStartAllowed
        prepareSession = [bool]$PrepareSession
        approvalRequired = -not $productionStartAllowed -or [bool]$PrepareSession
        claudeVersion = ((& $claudeCommand.Source --version) | Out-String).Trim()
        requestedModel = 'opus'
        requestedEffort = $Effort
        requestedPermissionMode = $PermissionMode
        configuredModel = $settings.model
        enabledProjectMcpServers = $settings.enabledMcpjsonServers
        sessionMcpConfigPaths = $mcpConfigPaths
        strictMcpConfig = $true
        promptExists = $true
        preparePromptPath = $preparePromptPath
        preparePromptHasHangul = $preparePromptHasHangul
        preparePromptReadEncoding = 'UTF8'
        unityExecutableExists = $true
        execution = 'CHECK_ONLY_NO_EDITOR_OR_AI_SESSION_STARTED'
    } | ConvertTo-Json -Depth 5
    return
}

if ($PrepareSession) {
    # 준비 세션은 Editor/approval을 활성화하지 않는다. 실제 사용자의 새 승인 메시지를 기다린다.
    $prepareLocation = Get-Location
    $prepareOffline = [Environment]::GetEnvironmentVariable('UV_OFFLINE', 'Process')
    try {
        Set-Location -LiteralPath $projectRoot
        $env:UV_OFFLINE = '1'
        $prepareArguments = @('--model', 'opus', '--effort', $Effort, '--permission-mode', $PermissionMode, '--mcp-config') +
            $mcpConfigPaths + @('--strict-mcp-config', $preparePrompt)
        Write-Host 'Starting one preparation session. No Editor or production approval is started by this launcher.'
        & $claudeCommand.Source @prepareArguments
        if ($LASTEXITCODE -ne 0) { throw "Claude preparation exited with code $LASTEXITCODE. No automatic restart was attempted." }
    } finally {
        [Environment]::SetEnvironmentVariable('UV_OFFLINE', $prepareOffline, 'Process')
        Set-Location -LiteralPath $prepareLocation.Path
    }
    return
}

function Test-LocalUnityMcpPort {
    $tcpClient = [Net.Sockets.TcpClient]::new()
    try {
        $pending = $tcpClient.BeginConnect('127.0.0.1', 8080, $null, $null)
        if (-not $pending.AsyncWaitHandle.WaitOne(500)) { return $false }
        $tcpClient.EndConnect($pending)
        return $true
    } catch { return $false }
    finally { $tcpClient.Dispose() }
}

# 오프라인 캐시만 사용한다. 전역 환경/사용자 설정을 변경하지 않는다.
$previousOffline = [Environment]::GetEnvironmentVariable('UV_OFFLINE', 'Process')
$previousLocation = Get-Location
try {
    Set-Location -LiteralPath $projectRoot
    $env:UV_OFFLINE = '1'
    $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")
    $projectEditor = $null
    foreach ($process in $unityProcesses) {
        if ($process.CommandLine -notmatch '(?i)(?:^|\s)-projectPath\s+(?:"([^"]+)"|(\S+))') {
            throw 'An existing Unity Editor has an unidentified project. Identify it before starting another Editor.'
        }
        $openedPath = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
        $openedPath = [IO.Path]::GetFullPath($openedPath).TrimEnd('\', '/')
        if ([string]::Equals($openedPath, $projectRoot, [StringComparison]::OrdinalIgnoreCase)) {
            if ($null -ne $projectEditor) { throw 'Multiple Editors target Project_PA. No process will be stopped.' }
            $projectEditor = $process
        }
    }
    if ($null -eq $projectEditor) {
        $crashReport = Get-ChildItem -LiteralPath $projectRoot -Filter 'PROJECT_PA_CRASH_REPORT_*.md' -File | Sort-Object Name -Descending | Select-Object -First 1
        if ($null -eq $crashReport) { throw 'Required crash-report review is unavailable.' }
        Write-Host (Get-Content -LiteralPath $crashReport.FullName -Raw -Encoding UTF8)
        $logDirectory = Join-Path $projectRoot 'Logs/ClaudeDemoLauncher'
        [IO.Directory]::CreateDirectory($logDirectory) | Out-Null
        $unityLog = Join-Path $logDirectory (('Editor-{0}.log' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff')))
        $unityArguments = '-projectPath "{0}" -force-d3d11 -logFile "{1}"' -f $projectRoot, $unityLog
        # 실제 GameView를 확인할 대화형 Unity Editor 창이다. 배치/helper 콘솔은 열지 않는다.
        Start-Process -FilePath $unityPath -ArgumentList $unityArguments -WindowStyle Normal -PassThru | Out-Null
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    $nextUpdate = [DateTime]::UtcNow
    while (-not (Test-LocalUnityMcpPort)) {
        if ([DateTime]::UtcNow -ge $deadline) {
            throw 'Unity MCP startup timed out. Keep the Editor/logs; inspect Window/MCP for Unity and reconnect its existing service. No repeated launch was attempted.'
        }
        if ([DateTime]::UtcNow -ge $nextUpdate) {
            Write-Host 'Waiting for the existing Unity MCP auto-start (no install/restart loop)...'
            $nextUpdate = [DateTime]::UtcNow.AddSeconds(30)
        }
        Start-Sleep -Seconds 3
    }

    # 포트 열림은 identity 증거가 아니다. 읽기 전용 doctor를 정확히 한 번 실행한다.
    $doctorOutput = & python -X utf8 Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py --agent claude-code
    if ($LASTEXITCODE -ne 0) { throw ($doctorOutput | Out-String) }
    $doctor = ($doctorOutput | Out-String) | ConvertFrom-Json
    if (-not $doctor.configurationReady -or -not $doctor.runtimeReady -or $doctor.routing.nextTicket -ne $approval.activeTicket) {
        throw ($doctorOutput | Out-String)
    }
    $prompt = Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8
    Write-Host 'Starting one interactive Claude production session. Existing writers must not edit the same ticket concurrently.'
    $claudeArguments = @('--model', 'opus', '--effort', $Effort, '--permission-mode', $PermissionMode, '--mcp-config') +
        $mcpConfigPaths + @('--strict-mcp-config', $prompt)
    & $claudeCommand.Source @claudeArguments
    if ($LASTEXITCODE -ne 0) { throw "Claude exited with code $LASTEXITCODE. Review the output; no automatic fallback or restart." }
} finally {
    Set-Location -LiteralPath $previousLocation.Path
    if ($null -eq $previousOffline) { Remove-Item Env:UV_OFFLINE -ErrorAction SilentlyContinue }
    else { $env:UV_OFFLINE = $previousOffline }
}
