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
| Menu button | **Hold Map for 1 second** opens/closes the menu (a tap still opens the map). Config: `ToggleAction`, `ToggleHoldSeconds` | Parent, 2026-11-16 |
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
- Install by hand: copy `WobblyMods.HouseMenu.dll` and `WobblyMods.Core.dll` into `BepInEx\plugins\WobblyMods\`.
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
| T20 | Redtail catfish boat: reskin a game boat | Catfish model, vehicle spawn names from Scout | Skylar can ride it on water |
| T21 | Redtail catfish car: reskin a game car | Catfish model, vehicle spawn names from Scout | Skylar can drive it |
| T22 | Shared "reskin" helper: hide a game object's body and attach a visual model (used by cat, catfish boat, catfish car) | Scout reports | Works on one vehicle and on the player |
| T23 | Install Unity Hub and the game's exact Unity version on the home PC, and make a small "asset project" that packs our models (cat, catfish, blocks) into AssetBundles | Unity version from Scout | One test bundle loads in game via `ModelLibrary` |
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

Vehicles to reskin (Scout report 2): **boat** = `PlayerBoat`; **land and water** = `PlayerHoverCraft` (it already drives on land and water, so this is Skylar's amphibious catfish); **car** = a land car such as `Vehicle_Convertable`.

Approach: reskin real game vehicles. Spawn a game boat or car, hide its body, and attach the catfish model as a visual-only child, scaled big. The game's own driving, floating, and seats keep working. Colliders stay on the game vehicle. The same "hide the body, attach a model" technique powers the cat costume, so the work is shared. Model search: `docs/research/redtail-catfish.md`. **Chosen (parent, 2026-11-16): "Free Low Poly Cartoon Koi Fish" by MarkoJantti on Sketchfab (CC BY 4.0, credit the author), recolored as a redtail** for the boat, car and prop. Why: flat material colors stay crisp at boat size (no stretched texture), it already has whiskers and 6 animations including swim (a real tail wag), and it matches Wobbly Life's cartoon look. Recolor: body dark grey/black, belly band cream-white, tail bright orange-red, whiskers dark grey. The realistic "Red Tail Catifish" (fredy120488) was set aside because its painted texture may blur when scaled up.

### Split screen changes the design (T14)

The current menu assumes one player: one mouse, one camera (`Camera.main`), one keyboard. In split screen:
- Each player needs their own menu, opened and driven by their own controller.
- "Look at it" must use that player's camera, not `Camera.main`.
- Undo should be per player, so one person's undo doesn't remove someone else's work.
- Everyone uses a controller, so the current mouse-click menu doesn't work for anyone, Skylar included. The menu must be navigable with a controller (D-pad or stick to move, A to pick, B to back out). How the game reads controllers depends on T5.

## How the agent team works

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
