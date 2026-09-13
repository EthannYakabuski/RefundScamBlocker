[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')
$pin = (Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
$sdkFound = $false
foreach ($candidate in @((Join-Path $script:RepositoryRoot '.tools\dotnet\dotnet.exe'), 'dotnet')) {
    $command = Get-Command $candidate -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) {
        $installed = & $command.Source --list-sdks
        if ($LASTEXITCODE -eq 0 -and ($installed | Where-Object { $_ -match ('^' + [regex]::Escape($pin) + '\s') })) { $sdkFound = $true; break }
    }
}
Write-Output "Required SDK: $pin; present: $sdkFound"
Write-Output "Windows host: $($env:OS -eq 'Windows_NT')"
Write-Output "PowerShell version: $($PSVersionTable.PSVersion)"
Write-Output 'Phase 1 needs only the pinned SDK. NSIS, Node and administrator rights are not needed.'
Write-Output 'OS enforcement is NOT IMPLEMENTED. This check neither installs nor enables protection.'
if (-not $sdkFound) { throw 'Install the pinned SDK or run scripts/bootstrap-dotnet.ps1.' }

