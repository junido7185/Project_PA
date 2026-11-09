param(
    [ValidateSet('claude','codex','status','resume','production-prompt')]
    [string]$Provider = 'claude',
    [string]$PythonExecutable,
    [switch]$NoMod
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$root = 'C:\Users\sdjsd\Desktop\Unity\Project_PA'
$tools = Join-Path $root 'Tools\PaAgentConsole'
$configFile = Join-Path $tools 'startup\installed-runtime.json'
if (-not (Test-Path -LiteralPath $configFile)) {
    throw 'Run Setup-ProjectPA.ps1 -Apply from the downloaded starter package first.'
}
$runtime = Get-Content -LiteralPath $configFile -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $PythonExecutable) { $PythonExecutable = $runtime.python }
if (-not (Test-Path -LiteralPath $PythonExecutable)) { throw 'Installed Python path changed. Run setup again.' }
Set-Location -LiteralPath $root
& $PythonExecutable -X utf8 (Join-Path $tools 'startup\check_runtime.py')
if ($LASTEXITCODE -ne 0) { throw 'Project preflight failed. Use 00_RESCUE.md in the working extension.' }
if ($Provider -eq 'status') {
    & $PythonExecutable -X utf8 (Join-Path $tools 'pa_console.py') --root $root
    exit $LASTEXITCODE
}
if ($Provider -eq 'resume') {
    & $PythonExecutable -X utf8 (Join-Path $tools 'pa_console.py') --root $root --resume-note
    Write-Host 'Paste startup/prompts/03_RESUME.md into the available provider.'
    exit $LASTEXITCODE
}
$promptName = if ($Provider -eq 'production-prompt') { '02_PRODUCTION_START.md' } else { '01_ENVIRONMENT_SETUP.md' }
$promptPath = Join-Path $tools ('startup\prompts\' + $promptName)
$promptText = Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8
if (Get-Command Set-Clipboard -ErrorAction SilentlyContinue) {
    Set-Clipboard -Value $promptText
    Write-Host ('Prompt copied. Paste with Ctrl+V after opening the AI. Source: ' + $promptPath)
} else { Write-Host ('Open and copy this file: ' + $promptPath) }
if ($Provider -eq 'production-prompt') {
    Write-Host 'Current-checkpoint resume prompt. Read and send it yourself; copying does not activate production or reset budgets.'
    exit 0
}
$cli = Get-Command $Provider -ErrorAction SilentlyContinue
if (-not $cli) { throw ('CLI missing: ' + $Provider + '. Paste the copied prompt into the working VS Code extension instead.') }
$projectSettings = Get-Content -LiteralPath (Join-Path $root '.claude\settings.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$installedMod = $projectSettings.enabledPlugins.'pa-production-console@project-pa-local' -eq $true
if ($Provider -eq 'claude' -and -not $NoMod -and -not $installedMod) {
    & $cli.Source --plugin-dir (Join-Path $tools 'pa-production-mod')
} else { & $cli.Source }
exit $LASTEXITCODE
