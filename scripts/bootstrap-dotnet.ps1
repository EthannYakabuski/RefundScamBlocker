[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitOperatingSystem -or
    $env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') {
    throw 'This optional SDK bootstrap supports Windows x64 only. Install the pinned SDK for your development platform separately.'
}
$sdk = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dotnet-sdk.json') -Raw | ConvertFrom-Json
$pin = (Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
if ($sdk.version -ne $pin) { throw 'SDK download metadata and global.json disagree.' }
$toolsPath = Join-Path $script:RepositoryRoot '.tools'
$destination = Join-Path $toolsPath 'dotnet'
$archive = Join-Path $toolsPath "dotnet-sdk-$pin-win-x64.zip"
New-Item -ItemType Directory -Path $toolsPath -Force | Out-Null
$localExecutable = Join-Path $destination 'dotnet.exe'
if (Test-Path -LiteralPath $localExecutable -PathType Leaf) {
    try {
        Initialize-DotNet
        Write-Output "SDK $pin is already available in .tools/dotnet."
        return
    } catch {
        Write-Output 'The local SDK is incomplete or does not match; restoring the pinned archive.'
    }
}
$archiveValid = (Test-Path -LiteralPath $archive -PathType Leaf) -and
    ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -eq $sdk.sha512)
if (-not $archiveValid) {
    Write-Output "Downloading the official .NET SDK $pin into .tools (no machine installation)."
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $sdk.url -OutFile $archive -UseBasicParsing -TimeoutSec 600
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $sdk.sha512) {
    throw 'SDK archive SHA-512 verification failed; nothing will be executed or extracted.'
}
Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force
Initialize-DotNet
Write-Output "Verified SDK $pin is ready in .tools/dotnet."

