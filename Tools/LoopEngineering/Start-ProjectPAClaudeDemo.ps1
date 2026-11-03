[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$PrepareSession,
    [ValidateSet('claude-code','codex')][string]$Owner = 'claude-code',
    [ValidateSet('medium','high','xhigh','max')][string]$Effort = 'high',
    [switch]$IntakeCheck,
    [switch]$Headless,
    [switch]$Interactive,
    [switch]$Status,
    [switch]$Watch,
    [switch]$Once,
    [switch]$Pause,
    [string]$PromptFile,
    [string]$ResumeSession,
    # Compatibility name: minutes without child output/owned-file activity, not total work time.
    [ValidateRange(0.01, [double]::MaxValue)][double]$TimeoutMinutes = 45
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not [string]::Equals($projectRoot.TrimEnd('\'), 'C:\Users\sdjsd\Desktop\Unity\Project_PA', [StringComparison]::OrdinalIgnoreCase)) { throw 'Launcher is outside Project_PA.' }
$entryArguments = @('-B','-X','utf8',(Join-Path $PSScriptRoot 'Invoke-ProjectPAProduction.py'),'start','--owner',$Owner,'--effort',$Effort)
if ($Watch -and ($Status -or $Pause)) { throw 'Choose -Watch, -Status, or -Pause.' }
if ($Once -and -not $Watch) { throw '-Once requires -Watch.' }
if ($Watch) {
    $watchArguments = @('-B','-X','utf8',(Join-Path $PSScriptRoot 'Invoke-ProjectPAProduction.py'),'autonomy','watch')
    if ($Once) { $watchArguments += '--once' }
    & python @watchArguments
    exit $LASTEXITCODE
}
if ($Status -or $Pause) {
    $operation = if ($Pause) { 'pause' } else { 'status' }
    & python -B -X utf8 (Join-Path $PSScriptRoot 'Invoke-ProjectPAProduction.py') autonomy $operation
    exit $LASTEXITCODE
}
if (-not ($Interactive -or $Headless -or $IntakeCheck) -and $Owner -eq 'claude-code') { $entryArguments += '--autonomous' }
if ($CheckOnly) { $entryArguments += '--check-only' }
if ($PrepareSession) { $entryArguments += '--prepare-session' }
if ($IntakeCheck) { $entryArguments += '--intake-check' }
if ($Headless) { $entryArguments += '--headless' }
if ($PromptFile) { $entryArguments += @('--prompt-file',$PromptFile) }
if ($ResumeSession) { $entryArguments += @('--resume-session',$ResumeSession) }
$entryArguments += @('--timeout',"$TimeoutMinutes")
& python @entryArguments
exit $LASTEXITCODE
