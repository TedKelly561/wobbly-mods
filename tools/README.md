# Automatic mod updates (and shared saves)

For any PC that plays Wobbly Life with Skylar's mods (REMOTEWS2 in the living room, the office laptop).

## One-time setup (per PC)

Needs: Wobbly Life (Steam) with BepInEx 5 installed and run once, `git`, and the .NET SDK
(`winget install Git.Git`, `winget install Microsoft.DotNet.SDK.8`), and GitHub access to `TedKelly561/wobbly-mods`.

In PowerShell:

```powershell
# get the setup script (or clone the repo yourself first)
git clone -b ccr-f341f2cb-pr00zz https://github.com/TedKelly561/wobbly-mods.git C:\dev\wobbly-mods
powershell -ExecutionPolicy Bypass -File C:\dev\wobbly-mods\tools\Install-WobblyUpdater.ps1
```

It registers the scheduled task **Wobbly Mods Updater** (at sign-in and every hour, hidden), points the mod's saves at
`%OneDrive%\WobblyLifeSaves` (copying existing saves there, never overwriting), and installs the latest version now.
Use `-SaveFolder "D:\Somewhere\Synced"` for a different synced folder, or `-SaveFolder ""` to keep saves local.

## What the updater does (`Update-WobblyMods.ps1`)

- Skips while Wobbly Life is running (nothing changes mid-game; the next run catches up).
- Fetches the branch; if there's a new commit (or this PC isn't on it yet), pulls, builds, and installs
  `WobblyMods.HouseMenu/Core/BuildMode.Core/Visuals.dll` into `BepInEx\plugins\WobblyMods\`, writing
  `installed-commit.txt` there.
- Moves older hand-installed copies of those DLLs to `BepInEx\plugins-backup-auto\<time>\` (Scout is left alone).
- Log: `%LOCALAPPDATA%\WobblyMods\update.log`. Run it by hand any time; `-Force` reinstalls.

## Shared saves

The mod setting `[Saves] SaveFolder` (in `BepInEx\config\wobblymods.housemenu.cfg`) points both PCs at the same
cloud-synced folder, so saved houses (`WobblyHouse\`) and builds (`WobblyBuilds\`) follow Skylar. Save on one PC, wait
for the cloud to sync, then load on the other. Don't play on both PCs at the same moment with the same slot.

## If the branch changes

The updater follows `ccr-f341f2cb-pr00zz`. If the work moves to `main`, re-run the installer with `-Branch main`.
