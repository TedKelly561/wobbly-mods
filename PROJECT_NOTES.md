# Wobbly Life: House Mods for Our Daughter (project notes)

This file is the project's memory and handoff. Anyone picking up work (including the project manager or Opus) starts here.

## Goal

Let a 5-year-old decorate her house in the main world of Wobbly Life, then later become a black cat everywhere in the game.

Playing on one computer, with family. Steam version only, using BepInEx 5 (Mono).

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
| Cat model | Undecided. Needs a clear license | Question answers |

## Stages

1. **House menu (in progress).** Spawn furniture, move it, recolor it, undo, and reset. Framework built. Game connections still to do.
2. **Cat pets in the house.** She can spawn cats and place them.
3. **Cat costume.** Her character looks like a black cat, but moves like a person.
4. **Full cat character.** Four-legged walking, if possible. Hardest part, which may not fully work with BepInEx alone.

## Status

- Framework (done, compiles): `HouseMenu/`. The plugin builds and deploys to the game's `BepInEx/plugins` folder.
- Undo logic (done, tested): `UndoStack` passed a standalone test of five checks.
- Game connections (not done): every `TODO(decompile)` in `HouseMenu/src/GameBindings.cs`. Until these are filled in, spawning shows "isn't connected yet", and moving, recoloring, and undo only work on items the menu itself spawned.
- Not tested in the game yet. The build used stand-in libraries, so the real game DLLs may need fixes.

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

## Build and install (at home, Windows)

1. Install BepInEx 5 (Mono) into the game folder and run the game once.
2. Open `HouseMenu/HouseMenu.csproj` in a build tool, or run `dotnet build` in `HouseMenu`.
3. If the game isn't in the default Steam folder, add `-p:GameDir="D:\path\to\Wobbly Life"`.
4. The DLL is copied to `BepInEx\plugins\WobblyMods\` automatically.

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

## Open questions

- **Parent switch:** none was chosen. Recommend a grown-up key that opens or closes the menu. Ask a parent again.
- **The "giant" object:** the giant ball is a placeholder in the catalog. Needs a description, and a model if it's new.
- **Cat plan:** four-legged walking or the human-walking version? Decide after stage 1.
- **Which main-world objects to change:** furniture only for now, or other things?

## Handoff log

- 2026-11-15: framework, undo tests, and planning notes created. The game wasn't available in the sandbox. Waiting on home tasks T1 and T5.
