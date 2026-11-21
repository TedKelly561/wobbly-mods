# Task for the Claude Code session on the home PC: install and check test 9 (quick building)

**Who this is for:** the Claude Code session on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for `wobbly-mods` (see `PROJECT_NOTES.md`).

## Background

Test 8 went well. The parent's feedback was that building is slow: one block at a time, through the menu.
Test 9 (house menu **0.9.1**) adds:
- **Quick building:** with **Build mode: on** and the menu **closed**, a **quick tap of RB** places the current
  piece at the ghost, and a **quick tap of LB** undoes your last piece. Holding LB+RB still opens the menu, and
  never places or undoes anything.
- **Pufferfish fix:** it swam backwards in the fish tank.
- **Animals → Redtail catfish:** our own model. It says "couldn't place" until the Unity kit has built its
  bundle. That's expected for now.
- A **new 4th DLL**, `WobblyMods.Visuals.dll`. It must sit next to the other house menu DLLs.

## Rules

- Only replace or add the four DLLs below. Don't touch other files, settings, or save files.
- Back up before replacing. The parent plays; don't start or close the game yourself.
- If anything looks different from what's described, stop and ask the parent.

## Step 1: install

1. The parent saved `WobblyMods-test9.zip` from the cloud chat (probably `Downloads`).
2. Check the zip: SHA-256 `4948050c8f2eeceeb042ff5daf55c1cf7c897f496b042b582bcdbccbeb54a616`, 68,294 bytes.
   If it doesn't match, stop.
3. Copy the installed `WobblyMods.HouseMenu.dll`, `WobblyMods.Core.dll` and `WobblyMods.BuildMode.Core.dll` into
   a new folder `BepInEx\plugins-backup-before-test9\`.
4. Unzip all four DLLs into the same folder the house menu DLLs are in, replacing the old ones.
5. Check the installed files:
   - `WobblyMods.HouseMenu.dll`: `d745fb575cfa2a2f6add06a81f6693c1d802ed0cf23a750afec912f646fc13ea`, 88,576 bytes
   - `WobblyMods.Core.dll`: `2164757e7e8f9c8f38a9c92edd7891ba5d9defb88fa1d0927d713ab15e9a7d94`, 14,336 bytes
   - `WobblyMods.BuildMode.Core.dll`: `790ca1fd925a7efa2d5504f217c7c3d227bfd7de8bc490fde3bd8328b4b3337c`, 41,472 bytes (unchanged)
   - `WobblyMods.Visuals.dll` (new): `2bd31e90e2d89a1320da10bcd6d934529a54be271b691c96c714561ed1d46cfd`, 8,704 bytes
6. Make sure there's only one copy of each DLL under `BepInEx\plugins\`.
7. Scout isn't part of this test. Leave it as it is.

## Step 2: the parent tests

Start Wobbly Life (**Play Crossplay**):
1. Open the menu (hold LB+RB, or Map), go to **Build**, pick a block (say Wall) and a paint (orange), turn on
   **Build mode: on**, and close the menu.
2. Walk, then **tap RB**: a piece goes down at the ghost. Walk a step and tap again, to build a short wall quickly.
3. **Tap LB**: the last piece is removed.
4. **Hold LB+RB**: the menu opens, and no piece is placed or removed.
5. Place the **Giant fish tank** and check the pufferfish swims forwards.
6. Also, please answer from test 8: did the frogs, lizards and turtles move? Did Undo remove each tank?
7. Close the game normally.

## Step 3: report

1. From `BepInEx\LogOutput.log`, collect lines with `Wobbly House Menu`, `quick build`, `Build`, `Warning`,
   `Error` or `Exception`.
2. Post a comment with the results on Switchyard task **#494** (project "🐟 Wobbly Life mods (Skylar & family)"),
   and write a short summary for the parent to paste into the cloud chat.
3. If `WobblyScout-run3.zip` from test 8 hasn't reached the cloud chat yet, remind the parent to attach it.

## If something goes badly wrong

Put the DLLs back from `BepInEx\plugins-backup-before-test9\`, remove `WobblyMods.Visuals.dll`, and tell the parent.

## Handoff

When done, log a handoff in Switchyard Hub to the project **"🐟 Wobbly Life mods (Skylar & family)"**, with your
own agent name.
