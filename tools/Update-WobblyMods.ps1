<#
.SYNOPSIS
  Keeps Skylar's Wobbly Life mods up to date on this PC.

.DESCRIPTION
  Fetches the mods' GitHub branch. If there's a newer version (or this PC doesn't have it installed yet), it
  pulls, builds, and installs the house menu DLLs into Wobbly Life\BepInEx\plugins\WobblyMods\.
  It never changes anything while Wobbly Life is running; the next run picks it up.
  It never throws away work: if its copy of the mods has changes or commits that are not on GitHub, it says so
  and stops without building. Best is a clone that only the updater uses (see tools\README.md).
  Normally run by the "Wobbly Mods Updater" scheduled task (Install-WobblyUpdater.ps1 sets that up).
  Log: %LOCALAPPDATA%\WobblyMods\update.log
  Result of the last run, for the mod's menu: BepInEx\plugins\WobblyMods\update-status.txt
  Works in Windows PowerShell 5.1 and PowerShell 7. Keep it that way, and keep this file plain ASCII: the
  scheduled task runs it with Windows PowerShell 5.1, and this script is how every later fix arrives.

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
try {
    if ((Test-Path $LogFile) -and ((Get-Item $LogFile).Length -gt 1MB)) {
        Move-Item -Force $LogFile (Join-Path $LogDir "update.old.log")
    }
} catch {
    # A log that can't be moved (open in an editor) must not stop the update.
}

function Write-Log([string]$Message) {
    $line = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $Message
    try { Add-Content -Path $LogFile -Value $line } catch { }
    Write-Host $line
}

# The result of this run, in a small file next to installed-commit.txt, so the mod can tell the family when
# updates have stopped working (HouseMenu\src\UpdateStatus.cs reads it). Two lines: time=... and result=...
# Set once the game folder is known. Never throws.
$script:StatusFile = $null
function Write-Status([string]$Result) {
    if (-not $script:StatusFile) { return }
    try {
        $dir = Split-Path $script:StatusFile
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        $oneLine = ($Result -replace "\s+", " ").Trim()
        if ($oneLine.Length -gt 300) { $oneLine = $oneLine.Substring(0, 300) }
        $lines = @(("time=" + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")), ("result=" + $oneLine))
        Set-Content -Path $script:StatusFile -Value $lines -Encoding ASCII
    } catch {
        # The log already has the result.
    }
}

# Runs a program and throws (with its output) if it fails. Returns the lines it wrote to its normal output.
# $What says in plain words what went wrong if it fails; it starts the message, which is what the menu shows.
function Invoke-Tool([string]$Exe, [string[]]$Arguments, [string]$What = "") {
    # Windows PowerShell 5.1 turns anything a program writes to its error stream into a script-stopping error
    # while ErrorActionPreference is "Stop", even when the program succeeded (git writes harmless notes
    # there). So this one call runs with "Continue", and only the exit code decides success or failure.
    $ErrorActionPreference = "Continue"
    $output = & $Exe @Arguments 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    if ($code -ne 0) {
        $text = ($output | ForEach-Object { "$_" }) -join "`n"
        if ($What) { $What = $What + ". " }
        throw ("{0}{1} {2} failed (exit {3}):`n{4}" -f $What, $Exe, ($Arguments -join " "), $code, $text)
    }
    return @($output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
}

# The full commit id a name like "HEAD" or "origin/branch" points at, in the updater's copy of the mods.
function Get-Commit([string]$Name) {
    $id = Invoke-Tool "git" @("-C", $RepoDir, "rev-parse", $Name) |
        Where-Object { $_ -match '^[0-9a-f]{40,64}$' } | Select-Object -First 1
    if (-not $id) { throw "Couldn't read which version '$Name' is in '$RepoDir'." }
    return [string]$id
}

function Test-GameRunning {
    $running = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like "Wobbly Life*" }
    if ($running) { return $true }
    return $false
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
    Write-Log ("Started (branch {0}, mods at {1})." -f $Branch, $RepoDir)

    # Nobody can see or answer a password question in a hidden window, so git must fail instead of waiting.
    $env:GIT_TERMINAL_PROMPT = "0"
    $env:GCM_INTERACTIVE = "never"

    # 1. Never touch files while the game runs (the DLLs are in use, and it would surprise a player).
    if (Test-GameRunning) {
        Write-Log "Wobbly Life is running; skipping this check."
        exit 0
    }

    if (-not $GameDir) { $GameDir = Find-GameDir }
    if (-not $GameDir) { throw "Wobbly Life folder not found. Pass -GameDir." }
    if (-not (Test-Path (Join-Path $GameDir "BepInEx\core\BepInEx.dll"))) {
        throw "BepInEx 5 is not installed in '$GameDir'."
    }
    $target = Join-Path $GameDir "BepInEx\plugins\WobblyMods"
    $stamp = Join-Path $target "installed-commit.txt"
    $script:StatusFile = Join-Path $target "update-status.txt"

    if (-not (Test-Path (Join-Path $RepoDir ".git"))) {
        throw "No copy of the mods at '$RepoDir'. Run Install-WobblyUpdater.ps1 first."
    }

    # 2. Is there something new?
    Invoke-Tool "git" @("-C", $RepoDir, "fetch", "--quiet", "origin", $Branch) "Could not get the latest version from GitHub" | Out-Null
    $remote = Get-Commit "origin/$Branch"
    $local = Get-Commit "HEAD"
    $installed = ""
    if (Test-Path $stamp) { $installed = ("" + (Get-Content $stamp -Raw)).Trim() }   # an empty file reads as nothing

    if (-not $Force -and $local -eq $remote -and $installed -eq $remote) {
        Write-Log ("Up to date ({0})." -f $remote.Substring(0, 7))
        Write-Status ("up to date ({0})" -f $remote.Substring(0, 7))
        exit 0
    }

    # 3. Get the new version, fast-forward only, and only into a clean copy. Someone may be working in this
    #    folder: their changes must not be lost, and must not be built into the family's game by mistake.
    #    What stops the update: a changed file that git tracks, or a new source, project or model file (those
    #    would be built in). Other new files (notes, logs) can't change the build: they are noted and left alone.
    $changed = @(Invoke-Tool "git" @("-C", $RepoDir, "status", "--porcelain", "--untracked-files=all"))
    $blocking = @($changed | Where-Object { ($_ -notmatch '^\?\?') -or ($_ -match '\.(cs|csproj|props|targets|sln|obj|mtl)"?\s*$') })
    if ($changed.Count -gt $blocking.Count) {
        Write-Log ("Note: {0} new file(s) in the updater's copy of the mods are not on GitHub. They don't affect the build." -f ($changed.Count - $blocking.Count))
    }
    if ($blocking.Count -gt 0) {
        throw ("The updater's copy of the mods has {1} changed or new file(s) that are not on GitHub (first: {2}). Nothing was updated or built. The copy is at '{0}'. Commit or remove them, or give the updater its own clone (see tools\README.md)." -f
               $RepoDir, $blocking.Count, ("" + $blocking[0]).Trim())
    }
    Invoke-Tool "git" @("-C", $RepoDir, "checkout", "--quiet", $Branch) "Could not switch the updater's copy of the mods to branch $Branch" | Out-Null
    try {
        Invoke-Tool "git" @("-C", $RepoDir, "merge", "--quiet", "--ff-only", "origin/$Branch") | Out-Null
    } catch {
        throw ("The updater's copy of the mods can't simply move forward to GitHub's version of {1} (it has its own commits). Nothing was built. The copy is at '{0}'. Give the updater its own clone (see tools\README.md). git said: {2}" -f
               $RepoDir, $Branch, $_.Exception.Message)
    }
    $head = Get-Commit "HEAD"
    if ($head -ne $remote) {
        throw ("The updater's copy of the mods has commits that are not on GitHub (it is at {1}, GitHub's {2} is at {3}). Nothing was built. The copy is at '{0}'. Push them, or give the updater its own clone (see tools\README.md)." -f
               $RepoDir, $head.Substring(0, 7), $Branch, $remote.Substring(0, 7))
    }

    # 4. Build. Nothing in the game folder is touched yet (the project only copies into the game when asked).
    $project = Join-Path $RepoDir "HouseMenu\HouseMenu.csproj"
    Invoke-Tool "dotnet" @("build", $project, "-c", "Release", "-nologo", "-v", "q", "-p:DeployToGame=false") "The new version did not build" | Out-Null

    # Where the build puts its files (netstandard2.1 is the TargetFramework in HouseMenu.csproj).
    $built = Join-Path $RepoDir "HouseMenu\bin\Release\netstandard2.1"
    $names = @("WobblyMods.HouseMenu.dll", "WobblyMods.Core.dll", "WobblyMods.BuildMode.Core.dll", "WobblyMods.Visuals.dll")
    foreach ($name in $names) {
        $file = Join-Path $built $name
        if (-not (Test-Path -LiteralPath $file)) { throw "Build finished but $name is missing in $built." }
        if ((Get-Item -LiteralPath $file).Length -eq 0) { throw "Build finished but $name in $built is empty." }
    }

    # 5. Building takes a while. If someone started the game meanwhile, leave everything as it is: the
    #    installed-commit stamp is not written, so the next run installs this version.
    if (Test-GameRunning) {
        Write-Log "Wobbly Life was started while the update was building; nothing was copied. The next run installs it."
        Write-Status "waiting: Wobbly Life was started while the update was building; the next run installs it"
        exit 0
    }

    # 6. Install: all four DLLs or none. The ones in use now are kept aside first (outside plugins, so BepInEx
    #    never loads them) and put back if a copy fails, so the game never starts with a mixed set.
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    $previous = Join-Path $GameDir "BepInEx\WobblyMods-previous"
    New-Item -ItemType Directory -Force -Path $previous | Out-Null
    foreach ($name in $names) {
        $current = Join-Path $target $name
        $kept = Join-Path $previous $name
        if (Test-Path -LiteralPath $kept) { Remove-Item -LiteralPath $kept -Force }
        if (Test-Path -LiteralPath $current) { Copy-Item -LiteralPath $current -Destination $kept -Force }
    }
    try {
        foreach ($name in $names) {
            Copy-Item -LiteralPath (Join-Path $built $name) -Destination (Join-Path $target $name) -Force
        }
    } catch {
        $why = $_.Exception.Message
        foreach ($name in $names) {
            $current = Join-Path $target $name
            $kept = Join-Path $previous $name
            try {
                if (Test-Path -LiteralPath $kept) { Copy-Item -LiteralPath $kept -Destination $current -Force }
                elseif (Test-Path -LiteralPath $current) { Remove-Item -LiteralPath $current -Force }
            } catch {
                Write-Log ("Could not put back {0}: {1}" -f $name, $_.Exception.Message)
            }
        }
        throw ("Copying the new mod files into the game failed, so the ones from before were put back. " + $why)
    }

    # Model packs (models\**: .obj/.mtl furniture) go next to the plugin; only new or changed files are copied.
    $modelsFrom = Join-Path $RepoDir "models"
    if (Test-Path -LiteralPath $modelsFrom) {
        $modelsRoot = (Get-Item -LiteralPath $modelsFrom).FullName
        $modelsTo = Join-Path $target "models"
        $modelFiles = Get-ChildItem -LiteralPath $modelsRoot -Recurse -File |
            Where-Object { $_.Extension -eq ".obj" -or $_.Extension -eq ".mtl" }
        foreach ($f in $modelFiles) {
            $relative = $f.FullName.Substring($modelsRoot.Length).TrimStart('\', '/')
            $dest = Join-Path $modelsTo $relative
            $destDir = Split-Path $dest
            if (-not (Test-Path -LiteralPath $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
            $same = $false
            if (Test-Path -LiteralPath $dest) {
                $there = Get-Item -LiteralPath $dest
                $same = ($there.Length -eq $f.Length) -and ($there.LastWriteTimeUtc -eq $f.LastWriteTimeUtc)
            }
            if (-not $same) { Copy-Item -LiteralPath $f.FullName -Destination $dest -Force }
        }
    }

    # 7. Now that the new copy is in: move older hand-installed copies of our DLLs out of the way (two copies
    #    would both load). Scout stays. This comes after the build on purpose: if the build fails, the game
    #    keeps whatever worked before.
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

    # 8. Done: remember which version is installed.
    Set-Content -Path $stamp -Value $remote
    $subject = "" + (Invoke-Tool "git" @("-C", $RepoDir, "log", "-1", "--format=%s") | Select-Object -First 1)
    Write-Log ("Installed {0}: {1}" -f $remote.Substring(0, 7), $subject)
    Write-Status ("installed {0}: {1}" -f $remote.Substring(0, 7), $subject)
    exit 0
}
catch {
    $why = "" + $_.Exception.Message
    Write-Log ("FAILED: {0}" -f $why)
    Write-Status ("FAILED: " + $why)
    exit 1
}
