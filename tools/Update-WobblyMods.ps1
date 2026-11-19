<#
.SYNOPSIS
  Keeps Skylar's Wobbly Life mods up to date on this PC.

.DESCRIPTION
  Fetches the mods' GitHub branch. If there's a newer version (or this PC doesn't have it installed yet), it
  pulls, builds, and installs the house menu DLLs into Wobbly Life\BepInEx\plugins\WobblyMods\.
  It never changes anything while Wobbly Life is running; the next run picks it up.
  Normally run by the "Wobbly Mods Updater" scheduled task (Install-WobblyUpdater.ps1 sets that up).
  Log: %LOCALAPPDATA%\WobblyMods\update.log
  Works in Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Force
  Rebuild and reinstall even if already up to date.
#>
[CmdletBinding()]
param(
    [string]$RepoDir = "C:\dev\wobbly-mods",
    [string]$Branch = "ccr-f341f2cb-pr00zz",
    [string]$GameDir = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$LogDir = Join-Path $env:LOCALAPPDATA "WobblyMods"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$LogFile = Join-Path $LogDir "update.log"
if ((Test-Path $LogFile) -and ((Get-Item $LogFile).Length -gt 1MB)) {
    Move-Item -Force $LogFile (Join-Path $LogDir "update.old.log")
}

function Write-Log([string]$Message) {
    $line = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $Message
    Add-Content -Path $LogFile -Value $line
    Write-Host $line
}

# Runs a program and throws (with its output) if it fails. Returns its output lines.
function Invoke-Tool([string]$Exe, [string[]]$Arguments) {
    $output = & $Exe @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ("{0} {1} failed (exit {2}):`n{3}" -f $Exe, ($Arguments -join " "), $LASTEXITCODE, ($output | Out-String))
    }
    return $output
}

# Finds the Wobbly Life folder: the default Steam folder, or any Steam library listed in libraryfolders.vdf.
function Find-GameDir {
    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add("C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life")
    try {
        $steam = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -ErrorAction Stop).SteamPath
        if ($steam) {
            $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
            if (Test-Path $vdf) {
                foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                    $lib = $m.Groups[1].Value -replace '\\\\', '\'
                    $candidates.Add((Join-Path $lib "steamapps\common\Wobbly Life"))
                }
            }
        }
    } catch {
        # No Steam registry key; the default path is still tried.
    }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "BepInEx\core\BepInEx.dll")) { return $c }
    }
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    return $null
}

try {
    # 1. Never touch files while the game runs (the DLLs are in use, and it would surprise a player).
    $running = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like "Wobbly Life*" }
    if ($running) {
        Write-Log "Wobbly Life is running; skipping this check."
        exit 0
    }

    if (-not $GameDir) { $GameDir = Find-GameDir }
    if (-not $GameDir) { throw "Wobbly Life folder not found. Pass -GameDir." }
    if (-not (Test-Path (Join-Path $GameDir "BepInEx\core\BepInEx.dll"))) {
        throw "BepInEx 5 is not installed in '$GameDir'."
    }
    if (-not (Test-Path (Join-Path $RepoDir ".git"))) {
        throw "No copy of the mods at '$RepoDir'. Run Install-WobblyUpdater.ps1 first."
    }

    # 2. Is there something new?
    Invoke-Tool "git" @("-C", $RepoDir, "fetch", "--quiet", "origin", $Branch) | Out-Null
    $remote = (Invoke-Tool "git" @("-C", $RepoDir, "rev-parse", "origin/$Branch") | Select-Object -First 1).ToString().Trim()
    $local = (Invoke-Tool "git" @("-C", $RepoDir, "rev-parse", "HEAD") | Select-Object -First 1).ToString().Trim()
    $target = Join-Path $GameDir "BepInEx\plugins\WobblyMods"
    $stamp = Join-Path $target "installed-commit.txt"
    $installed = ""
    if (Test-Path $stamp) { $installed = (Get-Content $stamp -Raw).Trim() }

    if (-not $Force -and $local -eq $remote -and $installed -eq $remote) {
        Write-Log ("Up to date ({0})." -f $remote.Substring(0, 7))
        exit 0
    }

    # 3. Get the new version (fast-forward only: this copy is only ever updated from GitHub).
    Invoke-Tool "git" @("-C", $RepoDir, "checkout", "--quiet", $Branch) | Out-Null
    Invoke-Tool "git" @("-C", $RepoDir, "merge", "--quiet", "--ff-only", "origin/$Branch") | Out-Null

    # 4. Move older hand-installed copies of our DLLs out of the way (two copies would both load). Scout stays.
    $stray = Get-ChildItem -Path (Join-Path $GameDir "BepInEx\plugins") -Recurse -Filter "WobblyMods.*.dll" |
        Where-Object { $_.DirectoryName -ne $target -and $_.Name -ne "WobblyMods.Scout.dll" }
    if ($stray) {
        $backup = Join-Path $GameDir ("BepInEx\plugins-backup-auto\{0:yyyyMMdd-HHmmss}" -f (Get-Date))
        New-Item -ItemType Directory -Force -Path $backup | Out-Null
        foreach ($f in $stray) {
            Move-Item -Force $f.FullName $backup
            Write-Log ("Moved old copy {0} to {1}" -f $f.FullName, $backup)
        }
    }

    # 5. Build and install (the project's DeployToGame step copies the DLLs into plugins\WobblyMods).
    $project = Join-Path $RepoDir "HouseMenu\HouseMenu.csproj"
    Invoke-Tool "dotnet" @("build", $project, "-c", "Release", "-nologo", "-v", "q",
                           "-p:GameDir=$GameDir", "-p:DeployToGame=true") | Out-Null

    $expected = @("WobblyMods.HouseMenu.dll", "WobblyMods.Core.dll", "WobblyMods.BuildMode.Core.dll", "WobblyMods.Visuals.dll")
    foreach ($name in $expected) {
        if (-not (Test-Path (Join-Path $target $name))) { throw "Build finished but $name is missing in $target." }
    }
    Set-Content -Path $stamp -Value $remote
    $subject = (Invoke-Tool "git" @("-C", $RepoDir, "log", "-1", "--format=%s") | Select-Object -First 1).ToString()
    Write-Log ("Installed {0}: {1}" -f $remote.Substring(0, 7), $subject)
    exit 0
}
catch {
    Write-Log ("FAILED: {0}" -f $_.Exception.Message)
    exit 1
}
