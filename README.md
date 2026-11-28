# Wobbly Mods

BepInEx mods for Wobbly Life (Steam, BepInEx 5 Mono).

- `HouseMenu/`: a big-button menu to decorate the house in the main world, with undo.

See [PROJECT_NOTES.md](PROJECT_NOTES.md) for goals, decisions, status, and the task board.

## Quick start

1. Install BepInEx 5 (Mono) into the Wobbly Life folder and run the game once.
2. Build and install, with the game closed: `dotnet build HouseMenu/HouseMenu.csproj -c Release -p:DeployToGame=true`.
   The four DLLs and the furniture models are copied into `BepInEx/plugins/WobblyMods`. (A build without
   `-p:DeployToGame=true` copies nothing. On the play laptop a scheduled task does all this by itself: see `tools/README.md`.)
3. In game, hold **LB + RB** (or **Map**) for a second to open the menu with a controller; on the keyboard, **F1** opens it and **F2** undoes.

Game files and decompiled game code are not stored in this repo.
