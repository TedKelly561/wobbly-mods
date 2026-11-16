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
| Cat model | Undecided. Needs a clear license | Question answers |
| Project scope | Big: more for Skylar, family mods, agent team. Not public | Question answers |
| Access to game files | Connect the home PC to the cloud session (Remote Control). Fallback: parent reports what they see | Question answers |
| Repo layout | `main` starts empty. Work goes to review through draft PRs | 2026-11-16 |
| Family play | Split screen together on one computer | Question answers |
| Family wishes | House menus, fun powers, vehicles and spawning, sandbox build mode | Question answers |
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
| T10 | Ask the family who plays and what each person wants | Parent's answers | A row per person in "Family mods" below |
| T11 | Design a shared base so each family mod reuses the menu, undo, and GameBindings | T3, T10 | Second mod builds without copying code |
| T12 | Big-picture buttons: replace text buttons with item thumbnails | T2 | Skylar can pick items without reading |
| T13 | Write a "how to hand off a task" checklist for agent sessions | None | Checklist added below |
| T14 | Split-screen design: find how the game tracks each local player, their camera, and their controller | Decompiled player and camera code | Design written here, menu works per player |
| T15 | Sandbox build mode: study the game's current building system and design snapping blocks and materials | Decompiled building code, family's wish list | Design written here |
| T16 | Fun powers mod (flying, super jump, physics gun) | T14 | Each power works per player and can be turned off |
| T17 | Vehicles and spawning mod | T3, T14 | Family can spawn vehicles and giant objects |
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

- **Parent switch:** none was chosen. Recommend a grown-up key that opens or closes the menu. Ask a parent again.
- **The "giant" object:** the giant ball is a placeholder in the catalog. Needs a description, and a model if it's new.
- **Cat plan:** four-legged walking or the human-walking version? Decide after stage 1.
- **Which main-world objects to change:** furniture only for now, or other things?

## Handoff log

- 2026-11-15: framework, undo tests, and planning notes created. The game wasn't available in the sandbox. Waiting on home tasks T1 and T5.
- 2026-11-16: review fixes (mouse unlock, aiming past the player, last-placed fallback, build message). `main` rebuilt as an empty base, and draft PR #1 opened. Scope expanded to the big project. Home setup started.
