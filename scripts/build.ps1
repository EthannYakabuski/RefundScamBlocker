[CmdletBinding()]
param(
    [ValidateSet('Native', 'Browser', 'All')][string]$Components = 'Native',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)

. (Join-Path $PSScriptRoot 'common.ps1')
if ($Components -ne 'Native') { throw 'Browser components are not implemented. Phase 1 supports -Components Native only.' }
Initialize-DotNet
Invoke-DotNet -Arguments @('restore', $script:SolutionPath, '--locked-mode', '--configfile', (Join-Path $script:RepositoryRoot 'NuGet.Config'))
Invoke-DotNet -Arguments @('build', $script:SolutionPath, '--no-restore', '--configuration', $Configuration, '--disable-build-servers')
Write-Output 'Native developer tools built. Browser work was not selected. No protection was installed.'
