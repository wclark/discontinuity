$ErrorActionPreference = 'Stop'
$player = Join-Path $PSScriptRoot 'builds/Discontinuity/Discontinuity.exe'
$icon = Join-Path $PSScriptRoot 'builds/Discontinuity/Discontinuity.ico'
if (-not (Test-Path -LiteralPath $player) -or -not (Test-Path -LiteralPath $icon)) {
    throw 'Build the game first with ./prototype.ps1 Build.'
}
$desktop = [Environment]::GetFolderPath('Desktop')
$path = Join-Path $desktop 'Discontinuity.lnk'
$shell = New-Object -ComObject WScript.Shell
if (Test-Path -LiteralPath $path) {
    $existing = $shell.CreateShortcut($path)
    if ($existing.TargetPath -ne $player) {
        throw "A shortcut to a different application already exists at $path. It has not been changed."
    }
}
$shortcut = $shell.CreateShortcut($path)
$shortcut.TargetPath = $player
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.IconLocation = "$icon,0"
$shortcut.Description = 'Discontinuity - one morning, many lives'
$shortcut.Arguments = ''
$shortcut.WindowStyle = 1
$shortcut.Save()
$verified = $shell.CreateShortcut($path)
if ($verified.TargetPath -ne $player -or $verified.IconLocation -ne "$icon,0") {
    throw 'The desktop shortcut did not retain its target and custom icon.'
}
Write-Output "Desktop shortcut: $path"
Write-Output "Game: $($verified.TargetPath)"
Write-Output "Icon: $($verified.IconLocation)"
