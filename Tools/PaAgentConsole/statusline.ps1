param([string]$PythonExecutable = 'python')
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$inputJson = [Console]::In.ReadToEnd()
$inputJson | & $PythonExecutable -X utf8 (Join-Path $PSScriptRoot 'pa_console.py') --root $projectRoot --statusline
exit $LASTEXITCODE
