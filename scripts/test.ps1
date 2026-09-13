[CmdletBinding()]
param(
    [ValidateSet('Unit', 'Integration')][string]$Suite = 'Unit',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)

. (Join-Path $PSScriptRoot 'common.ps1')
if ($Suite -ne 'Unit') { throw 'Live enforcement integration tests require Phase 2 target selection and recovery tooling and are not implemented.' }
Initialize-DotNet
Invoke-DotNet -Arguments @('restore', $script:SolutionPath, '--locked-mode', '--configfile', (Join-Path $script:RepositoryRoot 'NuGet.Config'))
Invoke-DotNet -Arguments @('test', $script:SolutionPath, '--no-restore', '--configuration', $Configuration,
    '--results-directory', (Join-Path $script:RepositoryRoot 'artifacts\test-results'), '--logger', 'trx', '--disable-build-servers')
