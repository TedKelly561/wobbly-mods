# Wobbly Life: House Mods for Skylar (project notes)

This file is the project's memory and handoff. Anyone picking up work (including the project manager or Opus) starts here.

## Goal

Let Skylar, age 5, decorate her house in the main world of Wobbly Life, then later become a black cat everywhere in the game.

Playing on one computer, with family. Steam version only, using BepInEx 5 (Mono).

This is now a **big project** with three parts:
1. **More for Skylar:** many more house items, features, and polish, then the cat.
2. **Mods for the whole family:** other family members get their own mods or menus. Who plays and what they want is still to be asked.
3. **A team of agents:** one Claude session is the project manager. It hands scoped tasks to other Claude sessions and tracks them on the task board below.

Not planned: public release (Thunderstore or Nexus).

## What Skylar likes (use this for every design choice)

- Monster trucks, dinosaurs, animals.
- Favorite colors: **orange and green**.
- Wants to decorate her house with **fish tanks, lizard tanks, frog tanks and turtle tanks**.
- Already asked for: playing as a black cat with yellow eyes, and a giant redtail catfish to ride.

- Not reading yet (2026-11-18): menus must work with pictures, colors and symbols. Words stay small under pictures.

## Decisions so far

| Topic | Decision | Source |
|---|---|---|
| Game version | Steam, BepInEx 5 (Mono) | Question answers |
| Modding route | BepInEx plugins (not the official Mod SDK, which is maps-only) | Question answers |
| Stage 1 | House menu first | Question answers |
| House changes | Furniture and objects, colors and looks, fun extras | Question answers |
| Menu style | Big picture buttons, few words | Question answers |
| Controls | On-screen menu, toggled by F1 (changeable in config) | Question answers |
| Undo | Always available: Undo (F2), and Reset house | Question answers |
| Multiplayer | Same computer only, so changes are local (nothing to sync) | Question answers |
| Parent switch | None chosen. Recommended to revisit (see Open questions) | Question answers |
| Cat look | Black cat with yellow eyes | Question answers |
| Cat moves | Meows, jumps, and climbs. Four-legged walking undecided | Question answers |
| Cat location | Everywhere in the game | Question answers |
| Cat plan | Decide after stage 1 | Question answers |
| Cat model | Undecided. Top pick: Vuena "Cute Pets FREE Sample" cat (CC0; walk, run, jump, sit, call). See `docs/research/assets.md`. A better cat can come later | Parent + research |
| Cat approach | Hide the body, attach the cat model, drive its animations from movement (1-2 evenings). True four-legged physics would take weeks | Research |
| Project scope | Big: more for Skylar, family mods, agent team. Not public | Question answers |
| Access to game files | Connect the home PC to the cloud session (Remote Control). Fallback: parent reports what they see | Question answers |
| Repo layout | `main` starts empty. Work goes to review through draft PRs | 2026-11-16 |
| Family play | Split screen together on one computer | Question answers |
| Game folder | `C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life` (the default). `Wobbly Life_Data` confirmed | Screenshot 2026-11-16 |
| Two game builds | The folder has `Wobbly Life.exe` + `Wobbly Life_Data` and `Wobbly Life_EOS.exe` + `Wobbly Life_EOS_Data`, plus `SharedAssets`. Steam offers two launch options: **"Play Crossplay"** (`Wobbly Life_EOS.exe`) and **"Play Steam Workshop"** (`Wobbly Life.exe`). The family plays **Crossplay** (confirmed from the Steam launch screen, 2026-11-16; an earlier note said otherwise). The project builds against `Wobbly Life_EOS_Data\Managed`; for the Workshop build, pass `-p:GameDataDir="Wobbly Life_Data"`. BepInEx loads in both. Research notes say the crossplay build uses Unity 2022.3.62f2 (Scout will confirm). Mods are for local split-screen play; avoid using them in online crossplay lobbies with strangers | Screenshot 2026-11-16 |
| BepInEx | 5.4.23.5 win x64 extracted into the game folder | 2026-11-16 |
| Family wishes | House menus, fun powers, vehicles and spawning, sandbox build mode | Question answers |
| Picture menu | All chosen by the parent (2026-11-18): **real item pictures** (the mod snapshots each item: shape and colors only, no game code), **a color per tab** (Furniture orange, Toys yellow, Animals green, Change blue, Build brown), **symbols for actions**, a **big picture grid** (3 across; stick moves, A picks, B back, **tap LB/RB to switch tabs**), **small words under pictures**, and **fun little sounds** (may be silent if the game turned off Unity audio for FMOD; logged) | Parent, 2026-11-18 |
| Menu button | **Hold LB + RB together, or hold Map, for 1 second** opens/closes the menu (`ToggleAction = UILB+UIRB,Map`, `ToggleHoldSeconds = 1`). Hold Map is confirmed working in game; LB + RB is untested. BepInEx keeps the first-run settings in `BepInEx\config\wobblymods.housemenu.cfg`, so new defaults only apply to fresh installs | Parent, 2026-11-16 |
| Controls | Everyone uses a controller. Replaces the mouse-click menu; F1/F2 stay as a keyboard backup | Question answers |
| Build mode priorities | Snapping blocks, good materials, big builds without lag, save and load | Question answers |

## Stages

1. **House menu (in progress).** Spawn furniture, move it, recolor it, undo, and reset. Framework built. Game connections still to do.
2. **Cat pets in the house.** She can spawn cats and place them.
3. **Cat costume.** Her character looks like a black cat, but moves like a person.
4. **Full cat character.** Four-legged walking, if possible. Hardest part, which may not fully work with BepInEx alone.

## Status

- Framework (done, compiles): `HouseMenu/`. The plugin builds and deploys to the game's `BepInEx/plugins` folder.
- Undo logic (done, tested): `UndoStack` passed a standalone test of five checks.
- Game connections (not done): every `TODO(decompile)` in `HouseMenu/src/GameBindings.cs`. Until these are filled in, spawning shows "isn't connected yet", and moving, recoloring, and undo only work on items the menu itself spawned.
- Review fixes (done): the menu unlocks the mouse while open, aiming skips her own character, buttons fall back to the last placed item, and a missing game folder shows a clear message.
- Not tested in the game yet. The build used stand-in libraries, so the real game DLLs may need fixes.
- **Shared Core (done, 23 tests pass):** `Core/` holds the undo stack, input abstraction (`IPlayerInput`), controller-navigable `MenuModel`, and `PlayerSession`/`SessionManager` for up to 4 split-screen players with their own undo. HouseMenu renders it; keyboard input is a placeholder until the Rewired adapter (T18).
- **Scout (done, sent to parent):** read-only mod in `Scout/` that writes reports to `BepInEx\WobblyScout`: Unity version, input, assemblies, keyword-matching game classes, local players and their cameras, Rewired actions and controllers, managers, and every Addressables prefab key. Builds from the BepInEx NuGet feed, so no game files are needed.
- **Build-mode core (done, 82 tests pass):** `BuildMode.Core/` is pure C#: grid snapping (1 m or 0.5 m), 10 block types, 7 materials, paint colors, overlap checks, per-chunk dirty tracking for big builds, per-player undo and redo with conflict detection, and a versioned save format. Its README explains the Unity layer still to build (meshes per chunk, materials, aiming).
- **Controllers (built, not tested in game):** `HouseMenu/src/RewiredPlayerInput.cs` reads each split-screen player's own Rewired player. Action names are config settings, defaulting to Rewired's standard UI names (`UIVertical`, `UIHorizontal`, `UISubmit`, `UICancel`) as a guess; the menu-toggle button is unset. Scout 0.2 lists the real names. Wrong names are switched off with one log line. Player 1 also keeps the keyboard (F1/F2, arrows, Enter, Backspace). Each menu sits on its player's own camera area.
- **Scout report 2 (2026-11-15):** split screen confirmed (2 players stacked top/bottom, in order, Rewired Player0/Player1), and the menu actions `UIVertical`/`UIHorizontal`/`UISubmit`/`UICancel` exist. See `docs/research/scout-report-2.md`. **Open:** which controller button opens the menu (every action is already used by the game).
- **In-game tests (2026-11-16):** the house menu opens with F1 and draws correctly. Test 4: **the controller drives the menu** (hold Map opens it, all options work), so finding the game's players and reading Rewired works. LB + RB didn't apply because the config file kept test 2's "Map" setting.
- **Controller test passed (2026-11-16, 0.4.3):** holding LB+RB and holding Map both open the menu and pause player 1's controls. No warnings or errors.
- **Spawning (built in 0.5.0, untested):** `HouseMenu/src/GameSpawner.cs` spawns through the game's `NetworkPrefab.SpawnNetworkPrefab(address, ...)` (host only; local split screen is host), placed 3-4 m in front of the player on the floor, facing them. Undo removes through `networkObject.Destroy()`. The catalog uses real sandbox prop addresses (beds, wardrobe, desk, lamps, toys, foam castle, ramp). The **giant catfish** is the game's own `CatFish_Static` fishing model at 8x size, placed as a local copy: a quick redtail stand-in before our own model.
- **Giant fish tank (built in 0.6.0, untested):** `HouseMenu/src/Tanks/`. Skylar's choice: a giant walk-around tank (8 x 5 x 3.5 m) with glass walls (colliders kept, so players walk around it), an orange rim and green base, sand, rocks, seaweed that sways, coral, a clam shell, starfish, a giant crab, a castle turret centerpiece, 12 swimming fish and seahorses (the game's fishing models), and the game's "Fish Bubbles" effect (own bubbles as a fallback). Glass uses the game's own glass material if found, else Standard transparent, else an always-included shader; the log says which. In the menu under Fun, placed 7 m in front of the player. Recolor skips see-through materials.
- **Test 6 result (2026-11-16):** parent: "we tested most of everything and it looked great"; the redtail catfish and the fish tank were a pleasant surprise and Skylar was very happy. Keep building.
- **More tanks (built in 0.7.0, untested):** new **Animals** tab (replaces Fun; foam castle and ramp moved to Toys) with: giant fish tank, **small fish tank** (the same tank at 30%), and **frog, lizard and turtle tanks** (`HouseMenu/src/Tanks/Terrarium.cs`, 5 x 3 x 2.5 m). Their animals are built from simple shapes in orange and green (`Critters.cs`), because the game has no frog, lizard or turtle animals; its frog and crocodile hats are clothing. Frogs hop around a pond with lily pads; lizards scurry and wag their tails under an orange heat lamp; turtles plod between land and a shallow pool. Plants are the game's own (red and yellow plants, cattails, grass) and sway gently.
- **Scout 0.4 (built, untested):** also writes `03_catalog.txt`, listing every costume, vehicle and pet with its Addressables address (for T25 dress-up and T26 monster truck). It lists the clothing, player, vehicle and pet classes with all their members, and caps automatic snapshots at 12 per run. Dress-up waits for this report, so it uses exact clothing calls rather than guesses.
- **Test 7 package:** `WobblyMods-test7.zip` (house menu 0.7.0 + Scout 0.4); PC instructions in `docs/handoffs/pc-session-test7.md`.
- **Build mode v1 (house menu 0.8.0, built, untested):** `HouseMenu/src/Build/`. A **Build** tab (the Undo tab's items moved into **Change**, keeping 5 tabs) with a per-player ghost block on a 1 m grid about 2.5 m ahead (green fits, red blocked); place, block type, material, paint (orange and green first), rotate, level up/down, remove, undo/redo (BuildMode.Core per-player history), "Build mode: on" (the ghost stays with the menu closed, to walk and aim), save/load in 3 slots in `BepInEx\config\WobblyBuilds\`, and clear (with confirm; undoable). Blocks render as merged meshes per chunk with box colliders. Local only (not networked). Known clunkiness: aiming means closing the menu, walking, and reopening, unless Build mode is on. Uncertain in game: physics on stairs and roofs, door size, whether `GetPlayerPosition` is the feet or the hips.
- **Test 8 package** (replaces test 7): `WobblyMods-test8.zip` (house menu 0.8.0 + Core + BuildMode.Core + Scout 0.4); PC instructions in `docs/handoffs/pc-session-test8.md`.
- **Unity kit (written, untested; no Unity Editor here):** `UnityAssets/` is a Unity 2022.3.62f2 project for the home PC (task T23). Menu **Wobbly Mods**: make the redtail catfish from the cartoon koi (dark body, cream belly, orange-red tail, dark whiskers, own swim animation), make the black cat (black fur, yellow eyes), and Build Bundles (copies to `BepInEx\plugins\WobblyMods\bundles\`). Uses its own shader `WobblyMods/SimpleLit` (the game is built-in pipeline + post-processing, no URP), and the glTFast importer for `.glb`/`.gltf`. Step-by-step guide: `UnityAssets/README.md`.
- **House menu 0.9.0 (built, not yet packaged for testing):** Animals → **Redtail catfish** loads the bundle model (about 4 m, floats slightly, collider for climbing on). Says "couldn't place" until the bundle is installed. Adds a 4th DLL: `WobblyMods.Visuals.dll`. Test 8 (0.8.0) stays the current test.
- **Test 8 result (2026-11-17, Switchyard #493):** installed cleanly, no warnings or errors; fish tank built 5 times; Scout 0.4 wrote 12 snapshots and `03_catalog.txt`. Parent feedback: the **pufferfish swims backwards** (fixed), and **building is slow, one block at a time**; they asked for a repeat-last-piece key. Vehicle names: `Vehicle_MuscleCar`, `Vehicle_HelicopterBig` (two players fit inside), `Vehicle_Sea_SpeedBoat`. Still to answer: did the tank animals move, did Undo remove each tank.
- **Quick building (house menu 0.9.1):** with **Build mode: on** and the menu closed, a **quick tap of RB** places the current piece at the ghost and a **quick tap of LB** undoes the last piece. A tap only counts if it's under 0.35 s and the other bumper wasn't pressed, so the LB+RB menu combo never places anything (`Core/src/TapWatcher.cs`, 6 tests). Config: `BuildQuickPlaceAction`, `BuildQuickUndoAction`.
- **Test 9 package:** `WobblyMods-test9.zip` (house menu 0.9.1 + Core + BuildMode.Core + **Visuals**, the new 4th DLL); PC instructions in `docs/handoffs/pc-session-test9.md`; Switchyard #494.
- **No-clip in the main world (house menu 0.10.0, Skylar's request, untested):** Change tab → "No-clip (fly through walls)". No-clip lives on the player's own `PlayerCharacterMovement` (field `bNoClip`); the sandbox only adds a button for it. The switch method's exact name was cut off in the Scout report, so `HouseMenu/src/Powers.cs` searches for a method with NoClip in its name taking one bool (preferring Server…, then Set…), and logs every NoClip method it saw plus which one it used.
- **Test 10 package** (supersedes test 9): `WobblyMods-test10.zip` = test 9 + no-clip.
- **Test 9 crash (2026-11-17):** the game crashed placing the second wall piece (house menu 0.9.1). No managed error was logged; the only Unity-log error was the game's own ("Destination scene is being unloaded", earlier in the session). The crash logs (`Player-prev.log`, Unity Crashes folder) weren't collected. Response: 0.10.1 guards build colliders against bad values and logs every placement and chunk draw.
- **Test 10b result (2026-11-17, house menu 0.10.1, Switchyard #494):** **no crash**, no warnings, errors or exceptions, and no skipped colliders. **No-clip works in the main world**: the game's switch is `PlayerCharacterMovement.SetNoClipEnabled(bool)`. **Quick building works**: 8 roof pieces placed by tapping RB, plus a floor and 7 half-walls through the menu. **Parent confirmed in game: "Everything worked!"** That covers build mode, quick taps, no-clip in her world, and the pufferfish fix.
- **Bigger build pieces (house menu 0.11.0):** parent: "the pieces are a bit small" and "2 meters is good". New builds now use **2 m** pieces by default. The Build tab's **Piece size** button cycles 2, 3 and 1 m, only while the build is empty. Saved builds keep their own size. The ghost sits farther ahead for bigger pieces.
- **Test 11 result (2026-11-17, Switchyard #495):** 2 m pieces: "It was good"; about 30 quick builds; no warnings or errors; no-clip worked again. Bug: at 3 m, half walls came out as separate pieces with gaps. Cause: pieces always faced one way, so walking forward and tapping made parallel slabs across the path.
- **Auto-turn (house menu 0.11.1):** on by default. Wall-like pieces (wall, half wall, door, window, beam) turn to run along the way the player faces; stairs and roofs face that way. Walk-and-tap now builds one continuous wall. The Build tab has "Auto-turn: on/off"; pressing Rotate switches it off for manual control.
- **Overnight house set (house menu 0.13.0, 2026-11-17, untested in game):**
  - **Save house / Load house** (Change tab, 3 slots in `BepInEx\config\WobblyHouse\`): every menu-placed item with its spot, facing, size and color. Load replaces what's placed (asks first). Format: `Core/src/HouseLayout.cs`, 7 tests. Local-model job #498 adds edge tests on its own branch.
  - **Change tab:** Copy (another beside it, same size, facing and color), Turn (45 degrees), Bigger, Smaller (0.25x-4x); all undoable.
  - **New items:** wardrobe 2, drawers, books, computer, stereo, TV stand, toilet; orange and green floor lamps, a night light and party lights (real lights, `HouseMenu/src/Decor.cs`); house plants from the game's own trees and flowers (shrunk; sizes are guesses); gold treasure and gold bars in Toys and in the giant fish tank.
  - **Room kits** (Build tab, "Room kits...", `BuildMode.Core/src/RoomKits.cs`, 90 new tests): small room 3x3 with a door (+ roof), big room 5x4 with windows (+ roof), floors 3x3 and 5x5, walls 3 and 5 long, stairs up with a landing. Each kit is one undo step, uses the current material and paint, and faces the player. It places what fits and reports what was skipped, and shows a ghost preview. A new `corner_wall` piece closes the room corners. Rooms are 2 levels tall (4 m at 2 m pieces).
- **Picture menu (house menu 0.14.0, untested in game):**
  - **Big tile grid**, 3 across. The stick moves a pulsing yellow glow (Left/Right go in reading order, Up/Down by row); A picks, B goes back; **tap LB/RB to change tab** (Q/E on keyboard). Holding LB+RB still toggles the menu and never changes tab.
  - **Tab colors and icons:** Furniture orange (sofa), Toys yellow (ball), Animals green (paw), Change blue (wand), Build brown (hammer).
  - **Real pictures:** `HouseMenu/src/Icons/IconCache.cs` photographs each catalog item a few per frame on layer 31 (assumed unused by the game). Game props are copied mesh-only (no game scripts run); our builds are built 5000 m below the world, photographed and deleted. A letter shows until the photo is ready.
  - **55 drawn symbols** for actions and build pieces; small words under every tile.
  - **Sounds** made in code (`MenuSounds.cs`): tick, pop, swish, chime. These may be silent if the game disabled Unity audio for FMOD; the log says once. Config: `[Sounds] MenuSounds`.
  - Core grid and tab navigation: 13 new tests (50 total).
  - **Fixed:** "Closer" used to move things away from the camera; Closer and Farther are now the right way round.
- **Test 13 result (2026-11-18, #499):** lamps, night light and party lights lit up; cactus and palm tree a sensible size; Copy and Turn worked; gold in the fish tank; room kits worked (walk in, no rain inside, Undo removes a whole room). Parent request: blinking party lights. Not tested yet: Save/Load house, auto-turn walls.
- **Test 14 result (2026-11-18, #500):** the picture menu looked "pretty darn good". **Skylar and her friend both used it** in split screen. 46 of 47 photos were made (the redtail needs the Unity kit bundle). **No sound**: the log confirms Unity audio is off (the game uses FMOD). Not answered yet: photo quality, tab colors and glow, Closer, Skylar alone.
- **House menu 0.15.0:** menu sounds now use the **game's own FMOD sounds** (`HouseMenu/src/FmodSounds.cs`): it lists every FMOD event, picks menu-like ones by name (hover, confirm, back, open), and logs the list and its picks. Config overrides: `[Sounds] FmodTick/FmodPick/FmodBack/FmodTab`. **Party lights blink** (`Twinkle` in `Decor.cs`), orange and green taking turns.
- **Test 15 result (2026-11-18, #502): everything passed.** Menu sounds "perfect" (FMOD picks: move `event:/UI/UI_ButtonHover`, pick `event:/UI/UI_ButtonClick`, back `event:/Objects_Space/Objects_Space_FlightSuit_Rocket_Burst_Back`, tab `event:/UI/UI_Arcade_SandboxMenu_Open`; 1,360 FMOD events, 162 menu-like). Party lights blink; photos and tab colors good; Closer fixed; **Skylar can use the menu on her own**; Save/Load house worked (parent's report). Known minor issue: half walls placed by walking and tapping still leave a gap (parent: "okay"); the room kits' wall lines are gap-free.
- **Catfish rides (house menu 0.16.0, untested in game; parent chose this next, 2026-11-18):** Animals tab → **"Catfish ride!"** turns the nearest vehicle within 8 m (any `PlayerVehicle`; Skylar's picks: `Vehicle_MuscleCar`, `Vehicle_HelicopterBig`, `Vehicle_Sea_SpeedBoat`) into a giant catfish. Its own renderers are hidden (never riders), a visual-only fish is attached and sized to the vehicle's length with a speed-based wiggle, and driving, flying and floating stay the game's. Pick again for the normal look; **"Flip catfish"** turns the fish around. The model is Skylar's redtail from the Unity kit bundle if installed, otherwise the **game's own CatFish_Static**, so it works before the Unity kit. `HouseMenu/src/CatfishRide.cs`. Risk: the game's vehicle optimization might switch hidden renderers back on.
- **Known issue (fix built, untested):** while a menu is open, the game still sees the same button presses (A may also jump). Fix later by blocking game input while a menu is open.
- **Visuals (started, builds):** `Visuals/` is shared Unity-side code. `ModelLibrary` loads our models from AssetBundles, `Reskin` hides a game object's body and attaches our model as visual-only (with Restore for undo), and `TailWag` swishes a tail faster when moving. Used by the cat and catfish (T22). Not tested in game.
- **Research (done):** `docs/research/game-internals.md` lists confirmed game names from open-source mods. `docs/research/assets.md` lists CC0 cat models, sounds, materials, and building kits.
- Draft PR #1 is open: https://github.com/TedKelly561/wobbly-mods/pull/1
- Home setup started 2026-11-16 (step 1: find the game folder).

## Files

```
HouseMenu/
  HouseMenu.csproj         build file. Set GameDir if your install differs.
  src/Plugin.cs            BepInEx entry point, keys (F1 menu, F2 undo)
  src/HouseMenuWindow.cs   the big-button menu (IMGUI)
  src/ObjectEdits.cs       move, recolor, remove. Each one records how to undo itself
  src/UndoStack.cs         generic undo list (no Unity code, tested)
  src/FurnitureCatalog.cs  list of items. PrefabName stays null until verified
  src/GameBindings.cs      the ONLY file that touches Wobbly Life's own code
```

## Build and install

- HouseMenu, Visuals, and Scout build **without game files**: BepInEx and Unity reference DLLs come from
  the BepInEx NuGet feed (`nuget.bepinex.dev`). HouseMenu and Visuals target netstandard2.1 with Unity
  **2022.3.62** references (the game's exact version); Scout stays on Unity 2019.4 references (proven in game).
- Game classes are reached through reflection (`HouseMenu/src/GameReflection.cs`), using names confirmed by Scout.
- Build: `dotnet build HouseMenu/HouseMenu.csproj -c Release`. If the game is installed at the default Steam
  path, the DLLs are copied to `BepInEx\plugins\WobblyMods\` (skip with `-p:DeployToGame=false`).
- Install by hand: copy `WobblyMods.HouseMenu.dll`, `WobblyMods.Core.dll` and `WobblyMods.BuildMode.Core.dll` (from 0.8.0) into the same folder under `BepInEx\plugins\`.
- BepInEx setting needed: `HideManagerGameObject = true` in `BepInEx\config\BepInEx.cfg` (set 2026-11-15).

## Key game facts (from research, confirmed in open-source mods)

- Players: `GameInstance.Instance.GetLocalPlayerControllers()` gives one `PlayerController` per split-screen player. Each has `GetPlayerCharacter()`, `GetGameplayCamera().GetCamera()`, and `GetPlayerControllerInputManager()`.
- Input is **Rewired**: each local player has their own Rewired player (`PlayerControllerInputManager.player`); buttons are read with `GetButtonDown("<Action>")`. Scout 0.2 lists all the action names.
- Networking is **HawkNetworking**. Only the host spawns networked objects (`NetworkPrefab.SpawnNetworkPrefab`).
- Props come from **Addressables**. Scout writes every prefab key to `02_props.txt`.
- Clothing: `CharacterCustomize.SetClothingPiece`. Pets: `PetManager.Instance.GetAllPets()`.
- Not found anywhere: a furniture placement or free-form building system. Build mode has to be our own.
- `WobblyLife.GameLibs` (NuGet) ships the game's own DLLs and would give exact signatures. **Not used**: it redistributes the game's binaries, so it's the parent's call.

## Rules for this project

- Don't commit game files: DLLs, decompiled game code, or game assets. `.gitignore` blocks the DLLs and build output.
- Cat models need a license that allows personal use. Check it before downloading.
- Changes to the house must be undoable. Keep that rule for every new feature.

## Task board (for the project manager / Opus)

Each task says who checks it. Mark it done here when finished.

| ID | Task | Needs | Done when |
|---|---|---|---|
| T1 | Confirm the Unity DLL folder name and the Unity version under `Wobbly Life_Data\Managed` | Game files at home | `HouseMenu.csproj` builds against the real DLLs |
| T2 | Find the game's furniture prefabs and fill in `PrefabName` in `FurnitureCatalog.cs` | Decompiled `Assembly-CSharp.dll`, Cinematic Unity Explorer | Each item lists a real prefab name |
| T3 | Implement `GameBindings.Spawn` and set `SpawnReady` to true | T2 | Spawning works in the game |
| T4 | Decide which house objects the menu may edit (exclude walls, floors, and the player). Update `IsEditable` | Looking at the game's house objects | Only furniture can be moved or recolored |
| T5 | Check the input method (legacy or new Input System). If it's the new one, change `Plugin.cs` | Game files | F1 and F2 open the menu and undo |
| T6 | Choose a cat model with a clear personal-use license, and record it here | Parent's choice (cat model) | Model and license are noted |
| T7 | Stage 2: cat pets | T3, T6 | Cat pets can be spawned and undone |
| T8 | Stage 3: black cat costume on the player | Decompiled player code | Her character looks like a black cat |
| T9 | Stage 4: four-legged walking (feasibility check first) | T8 | Decision recorded: possible or not |
| T10 | Ask the family who plays and what each person wants | Parent's answers | A row per person in "Family mods" below |
| T11 | Design a shared base so each family mod reuses the menu, undo, and GameBindings | T3, T10 | Second mod builds without copying code |
| T12 | Big-picture buttons: replace text buttons with item thumbnails | T2 | Skylar can pick items without reading |
| T13 | Write a "how to hand off a task" checklist for agent sessions | None | Checklist added below |
| T14 | Split-screen design: find how the game tracks each local player, their camera, and their controller | Decompiled player and camera code | Design written here, menu works per player |
| T15 | Sandbox build mode: study the game's current building system and design snapping blocks and materials | Decompiled building code, family's wish list | Design written here |
| T16 | Fun powers mod (flying, super jump, physics gun) | T14 | Each power works per player and can be turned off |
| T17 | Vehicles and spawning mod | T3, T14 | Family can spawn vehicles and giant objects |
| T19 | Redtail catfish prop: spawnable giant catfish in the sandbox | Catfish model, T3 | Spawns, can be moved and undone |
| T20 | Redtail catfish vehicles: "Make it a catfish!" reskin for the **speed boat** and the **large helicopter** | Catfish bundle, Scout 0.4 vehicle names | Skylar can ride a catfish on water and fly one |
| T21 | Redtail catfish car: same reskin for the **muscle car** | Catfish bundle, Scout 0.4 vehicle names | Skylar can drive it |
| T22 | Shared "reskin" helper: hide a game object's body and attach a visual model (used by cat, catfish boat, catfish car) | Scout reports | Works on one vehicle and on the player |
| T23 | Install Unity Hub and the game's exact Unity version on the home PC, and make a small "asset project" that packs our models (cat, catfish, blocks) into AssetBundles | Unity version from Scout | One test bundle loads in game via `ModelLibrary` |
| T24 | Animal tanks for the house: fish tank built from a glass box, sand and the game's fishing fish (gently swimming); then frog, lizard and turtle tanks (need free models) | Spawning working (T3) | Skylar can place tanks and they look alive |
| T25 | Dinosaur dress-up: put the game's own T-Rex / DinoSteed / crocodile / frog / shark clothes on a player from the menu (`CharacterCustomize.SetClothingPiece`) | Clothing API check | Skylar can become a T-Rex from the menu |
| T26 | Monster truck: list the game's vehicles (Scout 0.4: `AllVehiclesAssetReferences`), then a big-wheeled orange/green truck reskin | Scout 0.4 | Skylar can drive a monster truck |
| T18 | Controller menu: replace mouse clicks with controller navigation, one menu per player | T5, T14 | Each player opens and drives their own menu with their controller |

## Family mods

Players: Skylar (5), parent (the project owner), another parent, and cousin LL (age not given).
They play **split screen together** on one computer, and **everyone uses a controller**.

| Wish | Who | Notes |
|---|---|---|
| House menu for their own houses | Family | Reuse Skylar's menu (T11) |
| Fun powers (flying, super jump, physics gun) | Family | New mod |
| Vehicles and spawning (cars, boats, giant objects) | Family | New mod. The original sandbox-spawner goal |
| **Sandbox build mode** with real building blocks and materials | Family | The game's current building isn't good enough for anything substantial. All four matter: snapping walls, floors, roofs, and stairs; good materials (wood, brick, glass, stone, paint); big builds without lag; save and load. Large: its own stage |

### Skylar's giant redtail catfish (T19-T21)

Skylar asked for a giant redtail catfish (dark back, white belly band, whiskers, bright red tail) that she can ride. Three forms:
1. **Prop:** a giant redtail catfish she can spawn in the sandbox.
2. **Boat:** a rideable catfish that floats and steers like a boat.
3. **Car:** a drivable catfish on wheels.

Skylar's choices: **boat-sized** (a couple of players can ride), **tail wags**, **bubbles and splashes**, **fish sounds**, and the car should drive on **land and water** (amphibious, only possible if the game has a vehicle that does both; otherwise the boat and car stay separate).

**Vehicles chosen by Skylar and her parent (in game, 2026-11-17): the muscle car, the large helicopter and the speed boat.** These replace the earlier hovercraft idea. The helicopter makes a flying catfish.

Planned approach: a **"Make it a catfish!"** menu button that reskins the vehicle the player is sitting in (or looking at), if it's one of those three. The game's driving, flying and floating stay as they are; Undo puts the original look back (`Visuals/Reskin.Restore`). Needs from Scout 0.4 (test 8): the three vehicles' exact names, and how to find the vehicle a player is in. Also needs the RedtailCatfish bundle (Unity kit).

Approach: reskin real game vehicles. Spawn a game boat or car, hide its body, and attach the catfish model as a visual-only child, scaled big. The game's own driving, floating, and seats keep working. Colliders stay on the game vehicle. The same "hide the body, attach a model" technique powers the cat costume, so the work is shared. Model search: `docs/research/redtail-catfish.md`. **Chosen (parent, 2026-11-16): "Free Low Poly Cartoon Koi Fish" by MarkoJantti on Sketchfab (CC BY 4.0, credit the author), recolored as a redtail** for the boat, car and prop. Why: flat material colors stay crisp at boat size (no stretched texture), it already has whiskers and 6 animations including swim (a real tail wag), and it matches Wobbly Life's cartoon look. Recolor: body dark grey/black, belly band cream-white, tail bright orange-red, whiskers dark grey. The realistic "Red Tail Catifish" (fredy120488) was set aside because its painted texture may blur when scaled up.

### Split screen changes the design (T14)

The current menu assumes one player: one mouse, one camera (`Camera.main`), one keyboard. In split screen:
- Each player needs their own menu, opened and driven by their own controller.
- "Look at it" must use that player's camera, not `Camera.main`.
- Undo should be per player, so one person's undo doesn't remove someone else's work.
- Everyone uses a controller, so the current mouse-click menu doesn't work for anyone, Skylar included. The menu must be navigable with a controller (D-pad or stick to move, A to pick, B to back out). How the game reads controllers depends on T5.

## How the agent team works

- **Switchyard Hub project: "🐟 Wobbly Life mods (Skylar & family)"** (created 2026-11-16). Every session logs its handoff there with its own agent name, and test or setup jobs for the home PC go there as tasks.

- The project manager session owns this file and the task board. It writes the decisions and marks tasks done.
- A task goes to another session only when its row lists everything it needs and how to check it's done.
- Each session works on its own branch and opens a draft PR. The project manager reviews it before anything merges.
- Nothing merges without the parent's OK.
- Anything that needs the game (T1 to T5) waits for the home PC to be connected, or for the parent to report what they see.

## Open questions

- **Unity Editor on the home PC (T23):** our own models only load into the game as AssetBundles built with the game's exact Unity version. That needs the free Unity Editor installed once. Scout reports the version.

- **Parent switch:** none was chosen. Recommend a grown-up key that opens or closes the menu. Ask a parent again.
- **The "giant" object:** the giant ball is a placeholder in the catalog. Needs a description, and a model if it's new.
- **Cat plan:** four-legged walking or the human-walking version? Decide after stage 1.
- **Which main-world objects to change:** furniture only for now, or other things?

## Handoff log

- 2026-11-15: framework, undo tests, and planning notes created. The game wasn't available in the sandbox. Waiting on home tasks T1 and T5.
- 2026-11-16 (later): Opus took over as project manager. Shared Core with per-player menus and undo, Scout 0.1 and 0.2 (sent), and game-internals and asset research. Build-mode core library in progress. Waiting on the Scout reports from the home PC.
- 2026-11-16: review fixes (mouse unlock, aiming past the player, last-placed fallback, build message). `main` rebuilt as an empty base, and draft PR #1 opened. Scope expanded to the big project. Home setup started.
