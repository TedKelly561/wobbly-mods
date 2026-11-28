# Task for the Claude Code session on the home PC: install and check test 7 (animal tanks + Scout 0.4)

**Who this is for:** the Claude Code session on the family's Windows PC, opened in
`C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life`.

**Who wrote it:** the cloud project-manager session for `wobbly-mods` (see `PROJECT_NOTES.md`).

## Background

Test 6 (house menu 0.6.0, spawning and the giant fish tank) went great, and Skylar loved the redtail catfish
and the fish tank. Test 7 adds:
- **House menu 0.7.0:** a new **Animals** tab with the giant fish tank, a **small fish tank**, and **frog, lizard
  and turtle tanks** with little hopping, scurrying and plodding animals in orange and green. The foam castle and
  ramp moved to **Toys**.
- **Scout 0.4:** also writes `03_catalog.txt`, listing every costume, vehicle and pet the game has. It also lists
  the clothing, player, vehicle and pet classes in full. That's for the next features: T-Rex dress-up and a
  monster truck. It now writes at most 12 automatic snapshots per run.

## Rules

- Only replace the three DLLs below. Don't touch other files, settings, or save files.
- Back up before replacing.
- The parent plays the game. Don't start or close it yourself.
- If anything looks different from what's described, stop and ask the parent.

## Step 1: install

1. The parent saved `WobblyMods-test7.zip` from the cloud chat, probably to `Downloads`. Ask where if needed.
2. Check the zip: SHA-256 `63ebb0287ec6f1f498d2a1c57735816160458aebca56614f04489656734372c9`, 47,630 bytes.
   If it doesn't match, stop.
3. Find the installed `WobblyMods.HouseMenu.dll`, `WobblyMods.Core.dll` and Scout. Scout may have been renamed
   `WobblyMods.Scout.dll.off`; if so, ask the parent before turning it back on. Copy them into a new folder
   `BepInEx\plugins-backup-0.6.0\`.
4. Unzip over the installed files, in the same folder they were in.
5. Check the installed files:
   - `WobblyMods.HouseMenu.dll`: SHA-256 `3b5085195aa2cefca994e3c1472ab5e4b2bf587e72af4f78507689166c578624`, 62,464 bytes
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
5. For Scout, while in the main world: **open the Wardrobe** (clothing menu) once, and get into a **vehicle**
   once, if easy.
6. Take **photos** of each tank.
7. Close the game normally.

## Step 3: report

1. From `BepInEx\LogOutput.log`, collect every line containing `Wobbly House Menu`, `Building`, `couldn't place`,
   `Fish tank`, `Warning`, `Error` or `Exception`.
2. Zip `BepInEx\WobblyScout` (with `03_catalog.txt`) and `BepInEx\LogOutput.log` to the Desktop as
   `WobblyScout-run3.zip`.
3. Write a short summary for the parent to paste into the cloud chat: for each tank, whether it appeared, whether
   its animals moved, and how it looked; whether Undo worked; and the log lines.
4. Remind the parent to attach `WobblyScout-run3.zip` and the photos in the cloud chat.

## If something goes badly wrong

Put the 0.6.0 DLLs back from `BepInEx\plugins-backup-0.6.0\` and tell the parent.

## Handoff

When done, log a handoff in Switchyard Hub to the project **"🐟 Wobbly Life mods (Skylar & family)"**, with your
own agent name.
