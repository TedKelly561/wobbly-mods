# 0.40.0 — the refinement pass (Nov 28)

Ted asked for "all refinements that you see need doing". Four code reviews (aquariums, toys, menu and saves,
build mode and updater) found the problems below; the fixes were made in separate copies, checked line by line,
and merged. **Nothing here has been seen in the game.** Builds are clean; 351 Core tests and 185 build-mode
tests pass; the updater was run end to end against a fake game folder (PowerShell 7 only, see the end).

## For the ROADWS3 session: three things change how you work

1. **A plain `dotnet build` no longer installs into the game.** To install by hand on purpose, game closed:
   `dotnet build HouseMenu/HouseMenu.csproj -c Release -p:DeployToGame=true`. The updater task is unaffected.
2. **The updater now stops (and says FAILED) if its copy of the mods has edits.** That means a changed tracked
   file, a new `.cs`/`.csproj`/model file, or commits not on GitHub. Notes and other new files are only
   mentioned in the log. The updater's copy on ROADWS3 is the folder sessions work in
   (`C:\dev\personal-TedKelly561\wobbly-mods-main`): please give it its own clone (tools/README.md, "Give the
   updater its own clone"; the save folder is kept).
3. **New files:** `plugins\WobblyMods\update-status.txt` (last run's result), `BepInEx\WobblyMods-previous\`
   (the DLLs from before the last install), `house1.bak1..3` and `build1.bak1..3` next to the saves.
   The menu shows the version bottom right; it turns orange with a "!" when the last update failed.

## What changed

**Jungle (0.39.1)** — rebuilt on tested numbers (`Core/src/Shapes/JungleLayout.cs`):
- Trees are sized to stay inside the glass (the first version had about 10 through the walls, 12 through the roof).
- Treehouse platforms are 3 m up (were 4.2), under the leaves; the ramp is 15 degrees like the building's ramps;
  smooth unseen boards under the bridge planks; the slide has a push zone so it really slides.
- Parrots: most perches were inside the leaves. Each tree now has two bare low branches; 18 sit (mostly low),
  9 fly low loops over the lagoon, 4 on the treehouse. Wings fold along the body.
- Pictures: `docs/previews/forest/plan.png`, `jungle.png`, `treehouse.png`.

**Aquariums (0.39.2)**
- One shared material per color (about 1,200 per big aquarium down to about 45). `[Aquarium] Instancing = false`
  switches the batching hint off if anything looks wrong.
- The river's swim zone no longer reaches into the dry tunnel; gathered redtails stay above the tunnel arch.

**Saves (0.39.3)**
- Safe writes with three backups; the shared folder is checked on every save and load; if it is away the save
  goes to this computer and the message says so; loading takes the newer file. Build saves do the same (0.40.0).
- Loading no longer wipes the standing house when the file is missing or unusable; one bad item can't stop the rest.
- Model-pack furniture no longer turns 180 degrees per load; the redtail no longer rises 0.6 m per load;
  Change > Copy no longer faces backwards. Old files are corrected once on load (new files carry
  `# placed-as-given`).

**Menu (0.39.3)**
- Controller input: cached lookups (they ran about 48 times a frame with four players) and no stale taps when a
  controller goes away.
- Hidden items come back when the self-test passes them; an interrupted self-test retries; empty rooms show no tile.
- Change > Copy falls back to the last thing placed; slot labels refresh; failed photos show a symbol.

**Toys (0.39.4)**
- Tools don't fire while one of the game's own screens has the controls.
- Bubble gun: loosened parts are held again after the pop; aim assist skips things already in a bubble.
- Paint brush never paints the riders of a car, and refuses a whole room.
- New color really changes catalog props and world copies now (plain color; any texture is replaced).
- "Catfish car!" in catfish world makes that car her invincible ride. Swarm capped at 300.

**Build mode, shapes, updater, docs (0.40.0)**
- Hold LB removes the piece the crosshair is really on (it took the wall when she looked at the floor beside it).
- Changing the piece size clears build undo history.
- Cones (ears, teeth, spines, beaks) are lit properly.
- Updater: start line in the log, never waits for a sign-in, re-checks the game before copying, installs the four
  DLLs all-or-none with the previous set kept, status file. Installer keeps an existing save folder.
- PROJECT_NOTES.md, tools/README.md and the script headers no longer describe the old machines.

## Please test, in this order

1. **Update.** Close the game, run the updater task twice. Log should say `Installed <commit>` then `Up to date`.
   Menu shows `v0.40.0` bottom right, not orange.
2. **Saves (her houses matter most).** Load an existing house: does everything face and sit as it did before?
   Save it, load it again: nothing turned, the redtail did not rise. Check `house1.bak1` appeared.
3. **Jungle** (hub task 558, with the corrections above): trees inside the glass, treehouse walkable, slide
   slides, parrots visible low on branches and over the lagoon. Frame rate on the top floor.
4. **Tools.** Bubble gun, paint brush, Change > New color on a catalog prop, Copy this, Change > Copy.
5. **Build mode.** Look at a floor piece next to a wall, hold LB: the floor goes, not the wall.
6. **Tunnel.** Walk through the river tunnel on floor 1: no floating; redtails stay outside the glass.

## Known and left alone
- Menu pictures are still re-photographed on every launch (a disk cache was judged too risky to add blind).
- House saves still leave out My copies, paint-brush paint and build pieces (builds have their own slots).
- `Outfits.cs` (owned by the laptop session): after a failed re-wear the fur color and hidden face stay and
  Take off refuses. Rare.
- Changed gravity survives a world reload; a follower pet stays behind when its player leaves; when a middle
  split-screen player leaves, the next player inherits their menu state.
- Build mode redraws a whole 16-cell chunk per edit (possible hitch on very big builds).
- The updater's fix for git's harmless error-stream notes is the standard pattern but was only run on
  PowerShell 7; the scheduled task uses Windows PowerShell 5.1. If the first run after this update logs
  FAILED with git text that looks harmless, that is why.
- Giant/Tiny (task 527) and one catfish per train car are still to do.
