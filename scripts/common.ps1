Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$script:SolutionPath = Join-Path $script:RepositoryRoot 'RefundScamBlocker.slnx'

function Initialize-DotNet {
    $sdkVersion = (Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
    $localDotnet = Join-Path $script:RepositoryRoot '.tools\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $localDotnet -PathType Leaf) {
        $script:DotNet = $localDotnet
    } else {
        $command = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $command) { throw 'The pinned .NET SDK is missing. Run scripts/bootstrap-dotnet.ps1 or install the SDK from global.json.' }
        $script:DotNet = $command.Source
    }

    # Keep SDK and package caches inside the checkout; never change the user's PATH.
    $env:DOTNET_CLI_HOME = Join-Path $script:RepositoryRoot '.tools\dotnet-home'
    $env:NUGET_PACKAGES = Join-Path $script:RepositoryRoot '.tools\nuget-packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
    $env:NUGET_HTTP_CACHE_PATH = Join-Path $script:RepositoryRoot '.tools\nuget-http-cache'
    $env:NUGET_PLUGINS_CACHE_PATH = Join-Path $script:RepositoryRoot '.tools\nuget-plugins-cache'
    Push-Location $script:RepositoryRoot
    try {
        $detectedVersion = & $script:DotNet --version
        if ($LASTEXITCODE -ne 0 -or $detectedVersion -ne $sdkVersion) {
            throw "This checkout requires .NET SDK $sdkVersion. Run scripts/bootstrap-dotnet.ps1 or install that exact SDK."
        }
    } finally { Pop-Location }
}

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    Push-Location $script:RepositoryRoot
    try {
        & $script:DotNet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet command failed (exit $LASTEXITCODE)." }
    } finally { Pop-Location }
}
