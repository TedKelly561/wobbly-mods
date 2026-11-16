# Task for a Claude Code session on the home PC: set up and collect a Scout run

**Who this is for:** a Claude Code session running on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for the `wobbly-mods` project
(GitHub: TedKelly561/wobbly-mods, see `PROJECT_NOTES.md` and `docs/research/scout-report-1.md`).

## Background

The family is building BepInEx 5 mods for Wobbly Life (the Steam "Play Crossplay" version,
`Wobbly Life_EOS.exe`). BepInEx 5.4.23.5 is already installed in this folder. "Scout" is a read-only
mod we wrote: it changes nothing in the game, and writes text reports to `BepInEx\WobblyScout\`.
The first Scout run wrote its class report, but its scene snapshots never ran. We think the game
destroys BepInEx's manager object on the first scene load. Two fixes need installing, then the parent
plays the game, then the reports get collected.

## Rules

- Only change the two things listed in steps 1 and 2. Don't touch save files, other game files, or
  other mods.
- Make a backup before editing any file.
- The parent plays the game. Don't start or close the game yourself.
- If anything looks different from what's described here, stop and ask the parent.

## Step 1: BepInEx setting

1. Copy `BepInEx\config\BepInEx.cfg` to `BepInEx\config\BepInEx.cfg.bak`.
2. In `BepInEx.cfg`, find the `[Chainloader]` section and the line `HideManagerGameObject = false`.
   Change only that value to `true`. Leave everything else exactly as it was.
3. Show the parent the changed line, and the lines around it.

## Step 2: install Scout 0.3

1. The parent saved `WobblyMods.Scout.dll` from the cloud chat, probably to `Downloads`. Ask where
   if it isn't there.
2. Check it's the right file. The SHA-256 must be
   `265d28cf71d2c0e588700d361bd1209dcfb62ac407b5ebb16008c0434a56bcc2` and the size 28,160 bytes.
   In PowerShell: `Get-FileHash <path> -Algorithm SHA256`.
   If it doesn't match, stop and tell the parent.
3. If `BepInEx\plugins\WobblyMods.Scout.dll` exists, rename it to `WobblyMods.Scout.dll.old`.
   Copy the new DLL into `BepInEx\plugins\`. Create the folder if it's missing.
4. Delete nothing else. Move the old reports out of the way: rename `BepInEx\WobblyScout` to
   `BepInEx\WobblyScout-run1`, so the new run starts clean.

## Step 3: the parent plays

Tell the parent:
- Start Wobbly Life from Steam with **Play Crossplay**.
- Spend about 20 seconds in each spot: the main menu, Skylar's house in the main world, the sandbox,
  and split screen with a second controller connected.
- Press **F9** once in each spot (harmless if nothing visible happens).
- Close the game normally, then tell you.

## Step 4: collect

1. Check `BepInEx\WobblyScout\` now contains files named `scene_*.txt` and `02_props.txt`, as well
   as `00_summary.txt` and `01_types.txt`. List them with sizes.
2. In `BepInEx\LogOutput.log`, find every line containing `Wobbly Scout`, `Error`, or `Exception`.
   Show the parent a short summary.
3. Make one zip on the Desktop named `WobblyScout-run2.zip` containing the `BepInEx\WobblyScout`
   folder and `BepInEx\LogOutput.log`. In PowerShell:
   `Compress-Archive -Path "BepInEx\WobblyScout","BepInEx\LogOutput.log" -DestinationPath "$env:USERPROFILE\Desktop\WobblyScout-run2.zip"`
4. Tell the parent to attach `WobblyScout-run2.zip` in the cloud chat.

## Done when

- `HideManagerGameObject = true` is set, with a backup kept.
- Scout 0.3 is installed and verified by its hash.
- `WobblyScout-run2.zip` is on the Desktop, with scene snapshots in it, and the parent knows to send it.
