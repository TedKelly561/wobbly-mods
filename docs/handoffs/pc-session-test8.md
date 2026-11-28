# Task for the Claude Code session on the home PC: install and check test 8 (animal tanks + build mode + Scout 0.4)

**Who this is for:** the Claude Code session on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for `wobbly-mods` (see `PROJECT_NOTES.md`).

## Background

Test 6 (house menu 0.6.0, spawning and the giant fish tank) went great, and Skylar loved the redtail catfish
and the fish tank. **Test 8 replaces test 7** (if test 7 was never installed, skip it). It has everything in test 7 plus build mode:
- **House menu 0.7.0:** a new **Animals** tab with the giant fish tank, a **small fish tank**, and **frog, lizard
  and turtle tanks** with little hopping, scurrying and plodding animals in orange and green. The foam castle and
  ramp moved to **Toys**.
- **Scout 0.4:** also writes `03_catalog.txt`, listing every costume, vehicle and pet the game has. It also lists
  the clothing, player, vehicle and pet classes in full. That's for the next features: T-Rex dress-up and a
  monster truck. It now writes at most 12 automatic snapshots per run.
- **Build mode (house menu 0.8.0):** a **Build** tab. A see-through ghost block shows where the next piece goes,
  green if it fits, red if not. You can place, change block type, material and paint (orange and green first),
  rotate, raise or lower, remove, undo and redo, and save or load in 3 slots. The menu's **Undo** and **Reset house**
  moved to the top and bottom of the **Change** tab.
- A **new third DLL**, `WobblyMods.BuildMode.Core.dll`, must sit next to the other two.

## Rules

- Only replace or add the four DLLs below. Don't touch other files, settings, or save files.
- Back up before replacing.
- The parent plays the game. Don't start or close it yourself.
- If anything looks different from what's described, stop and ask the parent.

## Step 1: install

1. The parent saved `WobblyMods-test8.zip` from the cloud chat, probably to `Downloads`. Ask where if needed.
2. Check the zip: SHA-256 `19e8a7b9d45055d76bbcc14738d3343a908d895bdb94043805f63f1171acf3fb`, 76,985 bytes.
   If it doesn't match, stop.
3. Find the installed `WobblyMods.HouseMenu.dll`, `WobblyMods.Core.dll` and Scout. Scout may have been renamed
   `WobblyMods.Scout.dll.off`; if so, ask the parent before turning it back on. Copy them into a new folder
   `BepInEx\plugins-backup-before-test8\`.
4. Unzip over the installed files, in the same folder they were in. Put `WobblyMods.BuildMode.Core.dll` in that same folder.
5. Check the installed files:
   - `WobblyMods.HouseMenu.dll`: SHA-256 `631f9d7b73bfa94395dc980f2f2ed79604ca6198c40eabc00575965986eee46f`, 86,016 bytes
   - `WobblyMods.BuildMode.Core.dll` (new): SHA-256 `790ca1fd925a7efa2d5504f217c7c3d227bfd7de8bc490fde3bd8328b4b3337c`, 41,472 bytes
   - `WobblyMods.Core.dll`: SHA-256 `da6096aaa66f0cba9f613b09188dbd492248cb8afdb44c837e01707cd0b6735d`, 14,336 bytes (unchanged)
   - `WobblyMods.Scout.dll`: SHA-256 `d4631ccfac0458fb9d5d6457be888ebb809c267198556df700a391b9c010b6ec`, 32,256 bytes
6. Make sure there's only one copy of each DLL under `BepInEx\plugins\`.
7. Rename `BepInEx\WobblyScout` to `BepInEx\WobblyScout-run2`, so Scout's new reports start clean.

## Step 2: the parent tests

Start Wobbly Life (**Play Crossplay**). In the main world (Skylar's house is ideal), and then in the sandbox:
1. Open the menu (hold LB+RB, or Map, for 1 second), and go to the **Animals** tab.
2. Place the **Frog tank**, the **Lizard tank**, the **Turtle tank** and the **Small fish tank**, one at a time.
   Stand somewhere open: the bigger tanks appear about 5 m in front.
3. Watch for a bit: do the frogs hop, do the lizards scurry and wag their tails, do the turtles walk?
4. Press **Undo** to remove each.
5. **Build mode:** go to the **Build** tab. A ghost block should appear in front of the player. Choose **Place block**
   a few times, try **Block** (switch to Wall or Stairs), **Paint** (orange, green), **Rotate**, **Raise**, then
   **Undo build**. Turn on **Build mode: on**, close the menu, walk around: the ghost should follow. **Save build**
   to slot 1.
6. For Scout, while in the main world: **open the Wardrobe** (clothing menu) once, and get into a **vehicle**
   once, if easy.
7. Take **photos** of each tank and of a small build.
8. Close the game normally.

## Step 3: report

1. From `BepInEx\LogOutput.log`, collect every line containing `Wobbly House Menu`, `Building`, `couldn't place`,
   `Fish tank`, `Build`, `Warning`, `Error` or `Exception`.
2. Zip `BepInEx\WobblyScout` (with `03_catalog.txt`) and `BepInEx\LogOutput.log` to the Desktop as
   `WobblyScout-run3.zip`.
3. Write a short summary for the parent to paste into the cloud chat: for each tank, whether it appeared, whether
   its animals moved, and how it looked; whether Undo worked; and the log lines.
4. Remind the parent to attach `WobblyScout-run3.zip` and the photos in the cloud chat.

## If something goes badly wrong

Put the old DLLs back from `BepInEx\plugins-backup-before-test8\` and remove `WobblyMods.BuildMode.Core.dll` and tell the parent.

## Handoff

When done, log a handoff in Switchyard Hub to the project **"🐟 Wobbly Life mods (Skylar & family)"**, with your
own agent name.
