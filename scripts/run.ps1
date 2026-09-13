# Forward arguments to the developer CLI without a shell-command string.
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-DotNet
$cliPath = Join-Path $script:RepositoryRoot 'src\RefundScamBlocker.Cli\bin\Release\net10.0\RefundScamBlocker.Cli.dll'
if (-not (Test-Path -LiteralPath $cliPath -PathType Leaf)) { throw 'Build the developer CLI first with scripts/build.ps1.' }
& $script:DotNet $cliPath @args
exit $LASTEXITCODE

