# Task for the Claude Code session on the home PC: set the menu button, then check the test

**Who this is for:** the Claude Code session on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for the `wobbly-mods` project
(GitHub: TedKelly561/wobbly-mods, see `PROJECT_NOTES.md`).

## Background

The house menu mod (`BepInEx\plugins\WobblyMods.HouseMenu.dll`, version 0.4.3 or newer) works in game:
holding **Map** for 1 second opens it, and the controller drives it. The family also wants **LB + RB
together** to open it. BepInEx saved the old setting (Map only) the first time the mod ran, so the new
default doesn't apply until the settings file is changed.

## Rules

- Only change the two lines in step 1. Don't touch other settings, other files, or save files.
- Make a backup first.
- The parent plays the game. Don't start or close it yourself.
- If the file looks different from what's described, stop and ask the parent.

## Step 1: settings

1. Open `BepInEx\config\wobblymods.housemenu.cfg`. If it doesn't exist, stop and tell the parent:
   the game needs to be run once with the house menu installed first.
2. Copy it to `wobblymods.housemenu.cfg.bak`.
3. Under `[Controller]`, make sure these two lines read exactly:
   ```
   ToggleAction = UILB+UIRB,Map
   ToggleHoldSeconds = 1
   ```
   If they already do, change nothing and tell the parent it was already done.
4. Show the parent the `[Controller]` section before and after.

## Step 2: the parent tests

Ask the parent to start Wobbly Life (**Play Crossplay**), go into the world or sandbox, and:
1. **Hold LB + RB together for 1 second.** Does the house menu open?
2. **Hold Map for 1 second.** Does it still open?
3. Close the game when done.

## Step 3: report

1. In `BepInEx\LogOutput.log`, find every line containing `Wobbly House Menu`, `Local players found`,
   `Rewired action`, `Menu button`, `Paused player`, `Could not pause`, `Warning`, `Error` or `Exception`.
2. Write a short summary for the parent to paste into the cloud chat. Include:
   - whether LB + RB opened the menu, and whether Map did
   - whether the log shows `Menu button 'UILB' pressed` and `Menu button 'UIRB' pressed`
     (if LB + RB didn't work and these lines are missing, the game isn't sending those buttons during normal play)
   - the matching log lines, copied exactly

## Optional (ask the parent first)

Scout (`BepInEx\plugins\WobblyMods.Scout.dll`) writes a report on every level load. For normal play,
offer to rename it to `WobblyMods.Scout.dll.off`. Only do it if the parent says yes.

## Done when

The two settings lines are correct (with a backup kept), the parent has tested, and the summary is ready to paste.
