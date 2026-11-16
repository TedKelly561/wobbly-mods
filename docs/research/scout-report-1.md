# Scout report 1 (2026-11-15, home PC)

What Scout 0.2 found in the real game. Only the summary and class reports were written; the scene
snapshots did not run (see "Scout problem" below). Raw reports are kept off the repo; this file
lists only what the mods need.

## Game

- Steam launch option **"Play Crossplay"** = `Wobbly Life_EOS.exe`, data folder `Wobbly Life_EOS_Data`.
- Game version **1.1.0.0**, Unity **2022.3.62f2** (needed for building our AssetBundles, T23).
- BepInEx 5.4.23.5 loads fine.

## Where the game's code lives

Unlike older builds, most game classes are in an assembly named **`Game`** (3,826 types), not
`Assembly-CSharp` (355 types). Others: `Sandbox`, `Space`, `HideAndSeek`, `WobblyRun`,
`ModWobblyLife` (the official Mod SDK runtime), `HawkNetworking`, `Rewired_Core`.
Most classes are in the global namespace, so lookups by plain name work across assemblies.

## Input (CONFIRMED)

- Rewired (`Rewired_Core`), the new Input System, and legacy Input are all present. Legacy
  Input is enabled. Legacy Input saw 0 controllers (Rewired owns them).
- `PlayerController.controllerInputManager` (field) is the player's `PlayerControllerInputManager`.
- `PlayerControllerInputManager.player` (field, Rewired `Player`) and `GetRewiredPlayer()`.
  `PlayerCharacter.GetRewiredPlayer()` also exists.
- **The game's own way to pause one player's controls** (per player):
  `DisableGameplayInput(object handle, bool bResetTransformInput)` / `EnableGameplayInput(object handle)`,
  `DisableGameplayCameraInput(handle)` / `EnableGameplayCameraInput(handle)`,
  `ShowCursor(handle)` / `HideCursor(handle)`, `DisableUIInput` / `EnableUIInput`,
  `IsGameplayInputEnabled()`. Handles are any object; each Disable needs a matching Enable.

## Players and cameras (CONFIRMED)

- `PlayerController : HawkNetworkBehaviour` with fields `playerCharacter`, `gameplayCamera`,
  `controllerInputManager`, `localPlayerid` (int), and events `onGameplayCameraCreated`,
  `onLocalPlayeridAssigned`, `onPlayerSpawned`.
- `GameplayCamera.GetCamera()` returns the Unity `Camera`.
- `PlayerCharacter`: `GetRewiredPlayer()`, `GetPlayerBody()`, `GetPlayerCharacterCustomize()`,
  `GetGameplayCamera()`.
- `GameInstance` was not in this report (its name has no Scout keyword); Scout 0.3 adds it.

## Sandbox spawning (CONFIRMED, assembly `Sandbox`)

- `SandboxManager : HawkNetworkBehaviour`
  - `ClientSpawnProp(PlayerController, SandboxMenuPropScriptableObject)`: the game's own spawn
    call (client asks, server spawns).
  - `ClientIncreasePlayerSpeed`, `ClientIncreasePlayerJump`, `ClientToggleNoClip`,
    `ClientClearAllSpawnedObjects`: built-in "fun powers".
  - `categoriesData` (`SandboxMenuPropSpawnerCategoriesData`), `validSpawnables`
    (`HashSet<Guid>`, filled by `BakeValidSpawnables()`), `spawnedObjects`, static `MaxSpawnCount`.
- `SandboxMenuPropScriptableObject`: `propPrefab` and `propTexture` (Addressables `AssetReference`).
- `SandboxMenuPropSpawnerCategoriesData.propSpawners` → `SandBoxMenuPropSpawnerCategory`
  (`titleText`, `sandboxMenuCategories[]`).
- Menu UI: `SandboxPropMenu`, `SandboxPropCategoryMenu.GetMenu()`, `SandboxMenuItemIcon.iconTextureReference`.

**Idea:** add our own entries (the redtail catfish) into the game's own sandbox menu, and to
`validSpawnables`, instead of a separate spawner. Needs our prefab to be spawnable over HawkNetworking.

## Houses (CONFIRMED)

- `BuyableHouse : HawkNetworkBehaviour`: `owners` (`List<PlayerController>`), `spawnPoints`,
  `houseCost`, `propDetect`, events `onHouseOwned` / `onHouseSold`, `Lock()` / `Unlock()`.
- `BuyableHouseProp`: `GetHouse()`; marks objects belonging to a house (a way to tell house
  furniture from walls, task T4).
- `BuyableHouseManager` (singleton), `BuyHouseSign`, `BuyableHousePropDetect`.

## Vehicles and pets (CONFIRMED)

- `AllVehiclesAssetReferences.GetAllVehicleReferences()` → `VehicleAssetReference[]`; lookups by
  Guid or asset id. Vehicle physics include `NWH.DWP2` (boat/water physics) and `BoatInput`.
- `AllPetsScriptableObject`.

## Scout problem

Scene snapshots never ran, so Scout's timers stopped after the first scene load. Most likely
BepInEx's manager object is destroyed by the game. Fix: in `BepInEx\config\BepInEx.cfg`, under
`[Chainloader]`, set `HideManagerGameObject = true`. Then run Scout again.
