# Task for the Claude Code session on the home PC: install and check test 6 (spawning + fish tank)

**Who this is for:** the Claude Code session on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for `wobbly-mods`
(GitHub: TedKelly561/wobbly-mods, see `PROJECT_NOTES.md`). Switchyard Hub task #492.

## Background

House menu 0.4.3 is installed and works: the controller drives it, and holding LB+RB or Map opens it.
Test 6 (house menu **0.6.0**) adds:
- Spawning real furniture and toys through the game's own network spawner.
- A giant catfish: the game's fishing catfish at 8x.
- **Skylar's giant walk-around fish tank**, 8 x 5 x 3.5 m. It has glass, swaying seaweed, swimming fish, a castle and bubbles, and is under the **Fun** tab.

## Rules

- Only replace the two house menu DLLs. Don't touch other files, other settings, or save files.
- Back up before replacing.
- The parent plays the game. Don't start or close it yourself.
- If anything looks different from what's described, stop and ask the parent.

## Step 1: install

1. The parent saved `HouseMenu-test6.zip` from the cloud chat, probably to `Downloads`. Ask where if it isn't there.
2. Check the zip: SHA-256 `91e2a20c042f7838c69af0ebf3e72d039d3f25511784401aca5674435c65dfd0`, 29,630 bytes
   (PowerShell: `Get-FileHash <path> -Algorithm SHA256`). If it doesn't match, stop and tell the parent.
3. Find the installed `WobblyMods.HouseMenu.dll` and `WobblyMods.Core.dll` (in `BepInEx\plugins\` or
   `BepInEx\plugins\WobblyMods\`). Copy them into a new folder `BepInEx\plugins-backup-0.4.3\`.
4. Unzip the two DLLs from the zip over the installed ones, in the same folder they were in.
5. Check the installed files:
   - `WobblyMods.HouseMenu.dll`: SHA-256 `5af386d22754f01206bd9cb53ad55f38f88a85df34e9c0c4c392bddb5e21797b`, 52,224 bytes
   - `WobblyMods.Core.dll`: SHA-256 `da6096aaa66f0cba9f613b09188dbd492248cb8afdb44c837e01707cd0b6735d`, 14,336 bytes
6. Make sure there's only one copy of each DLL anywhere under `BepInEx\plugins\`. Two copies would both load.
   Move any extra copy into the backup folder.

## Step 2: the parent tests

Ask the parent to start Wobbly Life (**Play Crossplay**) and, in the **main world** and then the **sandbox**:
1. Open the menu (hold LB+RB, or Map, for 1 second).
2. Furniture: place a **Yellow bed**. Toys: place a **Soft toy**. Fun: place a **Giant catfish**.
3. Fun: place the **Giant fish tank**. It appears about 7 m in front, so stand somewhere open.
4. Press **Undo** once, and see what disappears.
5. Take a **phone photo** of the fish tank, and of anything that looks wrong.
6. Close the game normally.

## Step 3: report

1. In `BepInEx\LogOutput.log`, find every line containing `Wobbly House Menu`, `Spawn`, `couldn't place`,
   `Fish tank`, `glass material`, `Building`, `Warning`, `Error` or `Exception`.
2. Write a short summary for the parent to paste into the cloud chat:
   - for each item (bed, soft toy, giant catfish, fish tank), whether it appeared, in the main world and in the sandbox
   - whether Undo removed it
   - how the tank looked (see-through glass? fish moving? seaweed swaying? bubbles?)
   - the matching log lines, copied exactly
3. Remind the parent to attach the photos in the cloud chat.

## If something goes badly wrong

If the game won't start or behaves badly, put the 0.4.3 DLLs back from `BepInEx\plugins-backup-0.4.3\`
and tell the parent.

## Handoff

When done, log a handoff in Switchyard Hub to the project **"🐟 Wobbly Life mods (Skylar & family)"**
with your own agent name, and add a comment with the results on task #492.
