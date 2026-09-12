param(
    [ValidateSet('Play', 'Build', 'Verify')][string]$Task = 'Play',
    [string]$Unity
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'unity'
$player = Join-Path $PSScriptRoot 'builds/Discontinuity/Discontinuity.exe'
if ($Task -eq 'Play' -and (Test-Path -LiteralPath $player)) {
    Start-Process -FilePath $player -WorkingDirectory $PSScriptRoot
    return
}
if (-not $Unity) {
    $versionLine = Select-String -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$'
    $version = $versionLine.Matches[0].Groups[1].Value.Trim()
    $Unity = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
}
if (-not (Test-Path -LiteralPath $Unity)) { throw 'Unity editor not found. Supply its path with -Unity.' }
$method = if ($Task -eq 'Verify') { 'Verify' } else { 'Windows' }
$log = Join-Path $PSScriptRoot 'unity-build.log'
$arguments = "-batchmode -nographics -quit -projectPath `"$project`" -executeMethod Discontinuity.PrototypeBuild.$method -logFile `"$log`""
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) {
    Get-Content -LiteralPath $log -Tail 35
    throw "Unity exited with code $($process.ExitCode). See $log"
}
if ($Task -eq 'Play') { Start-Process -FilePath $player -WorkingDirectory $PSScriptRoot }
else { Write-Output "$Task complete. See $log" }
