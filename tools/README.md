# Automatic mod updates (and shared saves)

For a PC that plays Wobbly Life with Skylar's mods. Today that is one machine: the laptop **ROADWS3**.
(The Legion, ROADWS2, is gone. REMOTEWS2 is being moved to a relative's house and is not the play machine.)

## How ROADWS3 is set up

- Scheduled task **Wobbly Mods Updater**: every hour and at sign-in, hidden.
- The updater's copy of the mods: `C:\dev\personal-TedKelly561\wobbly-mods-main` (given to the installer with
  `-RepoDir`). Sessions also work in that folder; see "Give the updater its own clone" below.
- Saves: `G:\My Drive\WobblyLifeSaves`.
- Branch followed: `ccr-f341f2cb-pr00zz` (`main` is still the empty first commit).

## One-time setup (per PC)

Needs: Wobbly Life (Steam) with BepInEx 5 installed and run once, `git`, and the .NET SDK
(`winget install Git.Git`, `winget install Microsoft.DotNet.SDK.8`), and GitHub access to the mods' repository.

In a normal PowerShell window:

```powershell
# a clone that only the updater uses
git clone -b ccr-f341f2cb-pr00zz https://github.com/TedKelly561/wobbly-mods.git C:\dev\wobbly-mods
powershell -ExecutionPolicy Bypass -File C:\dev\wobbly-mods\tools\Install-WobblyUpdater.ps1 -RepoDir C:\dev\wobbly-mods
```

The installer registers the scheduled task, sets up the save folder, and installs the latest version now.

- **`-RepoDir`** is where the updater's copy lives. The task remembers it, so give it every time you run the
  installer (without it the task is pointed at the default, `C:\dev\wobbly-mods`).
- **`-Branch`** is the branch to follow; the task remembers that too.
- **Saves:** a PC whose mod config already names a save folder **keeps it**. To set or move it, pass
  `-SaveFolder "G:\My Drive\WobblyLifeSaves"`. A PC with no save folder yet gets `%OneDrive%\WobblyLifeSaves`
  if OneDrive is set up (existing saves are copied there, never overwriting).

## What the updater does (`Update-WobblyMods.ps1`)

1. Skips while Wobbly Life is running (nothing changes mid-game; the next run catches up).
2. Fetches the branch. If nothing is new and this PC has that version installed: "Up to date".
3. Otherwise it moves its copy forward to GitHub's version. It **never throws work away**: if the copy has changed
   or new files, or commits that are not on GitHub, it logs why and stops **without building**.
4. Builds (`dotnet build ... -p:DeployToGame=false`: the build itself touches nothing in the game).
5. Checks again that the game isn't running. If someone started it during the build, nothing is copied and the
   next run installs it.
6. Copies `WobblyMods.HouseMenu/Core/BuildMode.Core/Visuals.dll` into `BepInEx\plugins\WobblyMods\`, all four or
   none (the previous four are kept in `BepInEx\WobblyMods-previous\` and put back if a copy fails), then the
   changed model files into `plugins\WobblyMods\models\`.
7. Moves older hand-installed copies of those DLLs to `BepInEx\plugins-backup-auto\<time>\` (Scout is left alone).
8. Writes `installed-commit.txt` (which version is installed).

Where to look:

- **Log:** `%LOCALAPPDATA%\WobblyMods\update.log`. Every run starts with a "Started" line, so a run with no
  ending was cut off (for example git waiting for a sign-in that nobody could see).
- **Last result:** `BepInEx\plugins\WobblyMods\update-status.txt` (`time=...` and `result=...`: `up to date`,
  `installed <commit>`, `waiting: ...` or `FAILED: <why>`). The mod reads it (`HouseMenu/src/UpdateStatus.cs`) so the
  menu can say when updates have stopped. A run that skips because the game is running leaves the file as it was.
- Run it by hand any time; `-Force` rebuilds and reinstalls.

## Give the updater its own clone

The updater builds whatever is in its copy of the mods, so that copy should be one nobody edits. If a session
works in the same folder, the updater now refuses to run over its changes (step 3: any changed file that git
tracks, or any new source, project or model file; other new files such as notes are only mentioned in the log),
which keeps the work safe but
also means **the family gets no updates until that folder is clean and on the followed branch again**. Also, if
that folder is ever switched to a branch without `tools\` (such as the empty `main`), the task has no script to
run and fails without writing anything.

To move ROADWS3 to its own clone: make one (first command above), then run the installer from it with
`-RepoDir C:\dev\wobbly-mods`. The save folder is kept.

## Installing by hand, on purpose

A plain build does **not** copy anything into the game (it used to, which meant any build on the play laptop
replaced the family's mods). To build and install deliberately, with the game closed:

```powershell
dotnet build HouseMenu/HouseMenu.csproj -c Release -p:DeployToGame=true
# game somewhere else:  add  -p:GameDir="D:\Games\Wobbly Life"
```

That copies the four DLLs and the models but does not touch `installed-commit.txt`, so the updater puts the
followed branch's version back on its next run after a new commit (or right away with `-Force`).

## Shared saves

The mod setting `[Saves] SaveFolder` (in `BepInEx\config\wobblymods.housemenu.cfg`) can point more than one PC at
the same cloud-synced folder, so saved houses (`WobblyHouse\`) and builds (`WobblyBuilds\`) follow Skylar. Save on
one PC, wait for the cloud to sync, then load on the other. Don't play on two PCs at the same moment with the
same slot. If the folder can't be reached when the game starts, the mod logs a warning and uses
`BepInEx\config` for that session.

## If the branch changes

The updater follows `ccr-f341f2cb-pr00zz`. If the work moves (for example to `main`), run the installer again
with the new `-Branch` **and the same `-RepoDir`**. Until then the old branch simply stops getting new versions
and the log keeps saying "Up to date" (or `FAILED` if the branch was deleted).

## When changing these scripts

The updater updates itself by pulling the repo, so a version that doesn't parse or fails early can never
repair itself. Keep both scripts plain ASCII and Windows PowerShell 5.1-compatible (that is what the task
runs), and try a change against a throw-away clone and a fake game folder
(`<folder>\BepInEx\core\BepInEx.dll` is enough) before pushing it.
