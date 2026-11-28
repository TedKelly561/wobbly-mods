# Scout report 2 (2026-11-15, home PC, Scout 0.3, Crossplay build)

Scene snapshots now work (53 of them: the main world streams in chunk scenes named like
`WobblyIsland_2_-8`). No errors or warnings in `LogOutput.log`.

## Split screen (CONFIRMED live, 2 players)

| Player | PlayerController | Camera rect (viewport) | Rewired player |
|---|---|---|---|
| 1 | `FreemodePlayerController` | x 0, y 0.5, w 1, h 0.5 (top half) | id 0 `Player0` |
| 2 | `FreemodePlayerController` | x 0, y 0, w 1, h 0.5 (bottom half) | id 1 `Player1` |

- `GameInstance.Instance.GetLocalPlayerControllers()` order = split-screen order = Rewired player id.
- Two XInput gamepads seen by Rewired. Rewired has 4 players (`Player0`-`Player3`).
- Gamemode in the main world: `WobblyIslandGamemode`.

## Rewired actions (CONFIRMED)

Menu navigation actions exist with the names our controller menu already uses:
`UIHorizontal` (axis), `UIVertical` (axis), `UISubmit`, `UICancel` (category 4, UI).
Also UI: `UILB`, `UIRB`, `UIPause`, `UIDefaults`, `UILocalPlayerJoin`, `UIMapZoom`, `UIWaypoint`,
`UITodoList`, `UIRightHorizontal`, `UIRightVertical`.

Gameplay (category 0): `Horizontal`, `Vertical`, `LeftArmGrab`, `RightArmGrab`, `LookDeltaX/Y`, `Jump`,
`Ragdoll`, `ActionEnterExitInteract`, `Map`, `FovZoom`, `Character_Special_01/02`, `EmoteWheel`,
`Sprint`, `SpectatePrevious/Next`.
Vehicle (category 1): `Acceleration`, `SteerAxis`, `Horn`, `VehicleFire`, `Handbrake`,
`Vehicle_Special_1-3`, `VehicleHeight`, `VehicleBoost`, `UFOBeam`, `VehicleLookLeft/Right`.
Sandbox (category 7): `SandboxMenu`. Dev (category 6): `DevMenu`, `DevMenuCombo_0/1`.

**Open:** every action is already used by the game, so there's no free action to open our menu.
Options: a button the family never uses, a hold, or a two-button combo (decision needed).
**Unknown:** whether the UI category is enabled during normal play. If `UIVertical` reads 0 while
playing, also read `Vertical` / `Horizontal`.

## Managers (CONFIRMED present in the main world)

GameInstance, PetManager (34 pets as `PetAssetReference`), ClothingManager, SaveGameManager,
WorldManager, WeatherSystem, AchievementManager. `HawkNetworkManager.DefaultInstance` read as null.
It may need a different accessor; check before using it for "is host" checks.

## Spawnables

- 2,276 Addressables prefab keys (`02_props.txt`, kept off the repo): 1,207 under
  `Assets/Content/Game`, 504 under `Assets/Content/Arcade` (sandbox and hide-and-seek props,
  including whole buildings), 273 under `Assets/Content/Space`.
- **Drivable vehicles are not listed by path.** They live in `AllVehiclesAssetReferences`
  (`VehicleAssetReference`: `guidStr`, `prefab`, `vehicleGuid`). Scout 0.4 should list them.
- Vehicles seen in the world: `Vehicle_OldCar`, `Vehicle_Estate`, `Vehicle_Pickup`, `Vehicle_HatchBack`,
  `Vehicle_Taxi`, `Vehicle_PoliceCar`, `Vehicle_GarbageTruck`, `Vehicle_ThreeWheelCar`,
  `Vehicle_MotorChopper`, `Vehicle_Convertable`, plus NPC traffic.

## Vehicles for the redtail catfish (CONFIRMED classes)

- **Boat:** `PlayerBoat : PlayerVehicleSea` (`PlayerBoatMovement`, `PlayerBoatSound`, `BoatInput`).
  Also `PlayerBumperBoat`.
- **Land and water (Skylar's amphibious catfish):** `PlayerHoverCraft : PlayerVehicle` with
  `PlayerHoverCraftMovement : PlayerVehicleSeaMovement`. A hovercraft goes on land and water, so
  reskinning it gives the amphibious catfish.
- **Car:** any `PlayerVehicleLand` car (for example `Vehicle_Convertable`).
- `PlayerVehicle` has `vehicleCustomize`, `actionInteract`, `bIndestructable`, and events
  `onPlayerEntered` / `onPlayerExited`, which can trigger the catfish's bubbles and sounds.
