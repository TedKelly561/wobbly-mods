<#
.SYNOPSIS
  One-time setup of automatic mod updates (and shared saves) on a PC that plays Wobbly Life.

.DESCRIPTION
  Run once on each PC (REMOTEWS2 in the living room, and the office laptop), in a normal PowerShell window:
      powershell -ExecutionPolicy Bypass -File .\Install-WobblyUpdater.ps1
  It:
    1. Checks git and the .NET SDK are installed (and says how to install them if not).
    2. Clones the mods from GitHub to C:\dev\wobbly-mods (or updates an existing copy).
    3. Registers the scheduled task "Wobbly Mods Updater": runs Update-WobblyMods.ps1 at sign-in and every hour,
       hidden. It never changes anything while the game is running.
    4. Points the mod's save folder at a cloud-synced folder (default %OneDrive%\WobblyLifeSaves), so Skylar's
       houses and builds follow her between PCs, and copies any existing saves there (never overwriting).
    5. Runs the first update now.
  Works in Windows PowerShell 5.1 and PowerShell 7. Safe to run again.

.PARAMETER SaveFolder
  Cloud-synced folder for saves. Default: %OneDrive%\WobblyLifeSaves if OneDrive is set up. Use "" for no sync.
#>
[CmdletBinding()]
param(
    [string]$RepoDir = "C:\dev\wobbly-mods",
    [string]$Branch = "ccr-f341f2cb-pr00zz",
    [string]$RepoUrl = "https://github.com/TedKelly561/wobbly-mods.git",
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life",
    [string]$SaveFolder = "<auto>"
)

$ErrorActionPreference = "Stop"
function Say([string]$Message) { Write-Host ("[Wobbly setup] " + $Message) }

# 1. Tools.
foreach ($tool in @("git", "dotnet")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        $how = @{ git = "winget install Git.Git"; dotnet = "winget install Microsoft.DotNet.SDK.8" }[$tool]
        throw "$tool is not installed. Install it with:  $how   then open a new PowerShell window and run this again."
    }
}
if (-not (Test-Path (Join-Path $GameDir "BepInEx\core\BepInEx.dll"))) {
    throw "BepInEx 5 isn't installed in '$GameDir'. Install BepInEx_win_x64_5.4.23.5 there first (see PROJECT_NOTES.md), run the game once, then run this again."
}

# 2. The mods' source.
if (Test-Path (Join-Path $RepoDir ".git")) {
    Say "Mods already cloned at $RepoDir; fetching."
    git -C $RepoDir fetch --quiet origin $Branch
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed. Check GitHub access on this PC." }
} else {
    Say "Cloning the mods to $RepoDir ..."
    New-Item -ItemType Directory -Force -Path (Split-Path $RepoDir) | Out-Null
    git clone --quiet -b $Branch $RepoUrl $RepoDir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed. Check GitHub access on this PC (the repo may be private)." }
}

# 3. The scheduled task.
$updater = Join-Path $RepoDir "tools\Update-WobblyMods.ps1"
$arguments = ('-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "{0}" -RepoDir "{1}" -Branch "{2}" -GameDir "{3}"' -f
              $updater, $RepoDir, $Branch, $GameDir)
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arguments
$atLogon = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$hourly = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Hours 1)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                                         -ExecutionTimeLimit (New-TimeSpan -Minutes 20)
Register-ScheduledTask -TaskName "Wobbly Mods Updater" -Action $action -Trigger @($atLogon, $hourly) -Settings $settings `
    -Description "Keeps Skylar's Wobbly Life mods up to date from GitHub (skips while the game runs). Log: %LOCALAPPDATA%\WobblyMods\update.log" `
    -Force | Out-Null
Say "Scheduled task 'Wobbly Mods Updater' registered (at sign-in and every hour)."

# 4. Shared saves.
if ($SaveFolder -eq "<auto>") {
    if ($env:OneDrive) { $SaveFolder = Join-Path $env:OneDrive "WobblyLifeSaves" } else { $SaveFolder = "" }
}
$cfg = Join-Path $GameDir "BepInEx\config\wobblymods.housemenu.cfg"
if ($SaveFolder) {
    New-Item -ItemType Directory -Force -Path $SaveFolder | Out-Null
    foreach ($kind in @("WobblyHouse", "WobblyBuilds")) {
        $old = Join-Path $GameDir ("BepInEx\config\" + $kind)
        $new = Join-Path $SaveFolder $kind
        New-Item -ItemType Directory -Force -Path $new | Out-Null
        if (Test-Path $old) {
            foreach ($f in Get-ChildItem -Path $old -File) {
                $dest = Join-Path $new $f.Name
                if (Test-Path $dest) {
                    Say ("Kept the cloud copy of {0}\{1} (a local copy also exists in {2})." -f $kind, $f.Name, $old)
                } else {
                    Copy-Item $f.FullName $dest
                    Say ("Copied save {0}\{1} to the cloud folder." -f $kind, $f.Name)
                }
            }
        }
    }

    # Set [Saves] SaveFolder in the mod's config (the mod adds any other missing settings itself).
    $line = "SaveFolder = " + $SaveFolder
    if (Test-Path $cfg) {
        $lines = New-Object System.Collections.Generic.List[string]
        $lines.AddRange([string[]](Get-Content $cfg))
        $at = -1
        for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\s*SaveFolder\s*=') { $at = $i } }
        if ($at -ge 0) {
            $lines[$at] = $line
        } else {
            $section = -1
            for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq "[Saves]") { $section = $i } }
            if ($section -ge 0) { $lines.Insert($section + 1, $line) } else { $lines.Add(""); $lines.Add("[Saves]"); $lines.Add($line) }
        }
        Set-Content -Path $cfg -Value $lines
    } else {
        New-Item -ItemType Directory -Force -Path (Split-Path $cfg) | Out-Null
        Set-Content -Path $cfg -Value @("[Saves]", $line)
    }
    Say "Saves will be kept in $SaveFolder (set in $cfg)."
} else {
    Say "No cloud folder found (OneDrive not set up); saves stay on this PC. Run again with -SaveFolder <folder> to share them."
}

# 5. First update now.
Say "Running the first update (building takes a minute the first time) ..."
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $updater -RepoDir $RepoDir -Branch $Branch -GameDir $GameDir -Force
if ($LASTEXITCODE -ne 0) {
    throw "The first update failed. See $env:LOCALAPPDATA\WobblyMods\update.log"
}
Say "All set. Updates arrive by themselves; the log is at $env:LOCALAPPDATA\WobblyMods\update.log"
