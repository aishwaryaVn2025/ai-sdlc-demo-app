$payload = [Console]::In.ReadToEnd()

$scriptPath = Join-Path $PSScriptRoot "log-tool-use.cjs"

$payload | & node $scriptPath

exit $LASTEXITCODE