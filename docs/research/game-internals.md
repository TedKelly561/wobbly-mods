# Wobbly Life game internals (names taken from public mod source)

Research date: 2026-11-16. Target: Wobbly Life, Steam build, Unity Mono, BepInEx 5.

## How this was gathered and how far to trust it

- **Main source:** `lstwoMODS/lstwoMODS-WobblyLife` (branch `main`, about 170 C# files). The files were read through the jsDelivr GitHub mirror, because github.com and api.github.com returned 403 from this sandbox. File paths below are relative to that repo.
  - The repo has no `LICENSE` file at its root (raw fetch returned 404), so no open licence can be assumed. This document quotes only short snippets, credited to that repo.
- **No other source found:** I found no public source for WobblyPhysgun (Zappyix), Better WL Mod List, LargerLobbyMod (TitusXRyu) or AzzaMods. Their pages offer only Thunderstore packages or a Windows app, with no repo link.
  - LargerLobbyMod targets the Xbox / Game Pass IL2CPP build with BepInEx 6, so it would not apply to the Steam build anyway.
- **Official Mod SDK:** Rubber Band Games publishes a separate Mod SDK (`ModWobblyLife` namespace, Doxygen docs). It covers the game's own UMod-based modding, which is a different, wrapped API (`ModPlayerController`, `ModNetworkBehaviour`, ...). It is listed at the end for context only. It is **not** the internal Assembly-CSharp API that BepInEx/Harmony mods use.
- **How code reaches game types:** The mod pack compiles against the NuGet package **`WobblyLife.GameLibs`** (author "lstwo"; versions 0.9.4 to 1.1.0 on nuget.org). That package ships the game's Managed DLLs.
  - We did **not** download it, because it redistributes the game's binaries.
  - If the project owner decides that is acceptable, it would give exact signatures. That is a decision for them.
- **Legend:**
  - **CONFIRMED** means the name is used in compiled mod code. Since the mod builds against the real game DLLs, the name exists in at least one recent game version.
  - **GUESS** means it is inferred from names or context and not seen used.
- Most game types live in the **global namespace** in Assembly-CSharp (for example `GameInstance`, `PlayerController`). Networking types are in namespace `HawkNetworking`. Input types are in namespace `Rewired`.

---

## 1. Local player / character

### Main types (all CONFIRMED)

| Concept | Game type | How mods get it |
|---|---|---|
| Game singleton | `GameInstance` | `GameInstance.Instance`, `GameInstance.InstanceExists` |
| First local player | `PlayerController` | `GameInstance.Instance.GetFirstLocalPlayerController()` |
| All local players (split screen) | `List<PlayerController>` | `GameInstance.Instance.GetLocalPlayerControllers()` |
| All players (incl. remote) | `List<PlayerController>` | `GameInstance.Instance.GetPlayerControllers()` |
| Character (the wobbly body actor) | `PlayerCharacter` | `controller.GetPlayerCharacter()`; back: `character.GetPlayerController()` |
| Physical body | `PlayerBody` | `character.GetPlayerBody()` |
| Hip rigidbody | `Rigidbody` | `character.GetHipRigidbody()`, `body.GetRigidbody()`, field `PlayerCharacterMovement.hipRigidbody` |
| Position / facing | `Vector3` | `character.GetPlayerPosition()`, `character.GetPlayerForward()`, `character.SetPlayerPosition(v)` |
| Ragdoll | `RagdollController` | `character.GetRagdollController()` with `.Ragdoll()`, `.Knockout()`, `.LockRagdollState()`, `.UnlockRagdollState()`, `.IsRagdollStateLocked()` |
| Movement | `PlayerCharacterMovement` | `character.GetPlayerCharacterMovement()`; fields `bCanJump`, `bIsGrounded`, `speedMultiplier`, `jumpMultiplier`; methods `IsGrounded()`, `CanJump()`; private `SimulateMovement`, `SimulateJump`, `SimulateWater`, `OnRagdollCollisionEnter` |
| Character input | `PlayerCharacterInput` | `character.GetPlayerCharacterInput()`; private `HandleInput`, `EnqueueInput(PlayerInput, bool)`, `IsControlsEnabled()` |
| Appearance | `CharacterCustomize` | `character.GetPlayerCharacterCustomize()` (see section 7) |
| Face | `CharacterFace` | `character.GetComponent<CharacterFace>()` |
| World / chunk object | `WorldDynamicObject` | `character.GetWorldDynamicObject()` |
| Player name | `string` | `controller.GetPlayerName()`, `controller.SetServerPlayerName(name)` (server) |
| Is local / owned | `bool` | `controller.IsLocal()`, `controller.networkObject.IsOwner()` |
| Events | | `GameInstance.onAssignedPlayerController`, `onUnassignedPlayerController`, `onAssignedPlayerCharacter`, `onSceneLoaded` |

### PlayerController sub-components (CONFIRMED)

These come from `UI/TabMenus/PlayerBasedModsWindow.cs`, class `PlayerRef`:

- `GetPlayerControllerInputManager()` returns `PlayerControllerInputManager`.
- `GetPlayerControllerUnlocker()` returns `PlayerControllerUnlocker`.
- `GetPlayerControllerEmployment()` returns `PlayerControllerEmployment`.
- `GetPlayerControllerInteractor()` returns `PlayerControllerInteractor`.
- `GetPlayerControllerPet()` returns `PlayerControllerPet`.
- `GetGameplayCamera()` returns `GameplayCamera`.
- `GetFreeCamera()` returns `FreeCamera`.
- `GetMinimapCamera()` returns `MinimapCamera`.
- `GetCurrentCameraFocus()` returns `CameraFocus`.
- `GetPlayerBasedUI()` returns `PlayerBasedUI`.
- `GetPlayerControllerUI()` returns `PlayerControllerUI`.
- `GetGameState()` returns `BaseGameState`.
- `GetPlayerSettings()` returns `SavePlayerSettingsData`.
- `GetPlayerPersistentData()` returns `SavePlayerPersistentData`.
- `GetRespawnSystem()` and `GetRespawnOptions()`.
- Also `GetPlayerControllerSpectate()`, `GetPlayerControllerWaypoint()`, `GetPlayerControllerInputHint()` and `GetPlayerControllerSettings()`.

Snippet from `PropSpawner/PropSpawnManager.cs` (lstwoMODS):

```csharp
var player = GameInstance.Instance?.GetFirstLocalPlayerController();
var character = player?.GetPlayerCharacter();
var forward = character.GetPlayerForward();
position = character.GetPlayerPosition() + forward;
```

### Camera (CONFIRMED)

- `GameplayCamera` is the per-player camera rig. `camera.GetCamera()` returns the Unity `Camera`, and `camera.GetUICamera()` returns the paired UI camera.
- Other `GameplayCamera` members:
  - `camera.cameraFocus` (field) and `camera.SetCameraFocus(CameraFocus)`
  - `camera.GetPlayerController()`
  - `camera.player` (a Rewired `Player`)
- `CameraFocus` is the abstract base, with a virtual `UpdateCamera(GameplayCamera)`. Subclasses:
  - `CameraFocusPlayerCharacter` (private field `focusTransform`, `rotationSpeed`, `SetUsingCharacterCutoff(bool)`)
  - `CameraFocusVehicle`
  - `CameraFocusFree`
- `GameplayCameraWater.GetWaterPostProcessing` also exists.
- `PlayerController.onGameplayCameraCreated` is an event with signature `(controller, GameplayCamera)`.
- Sources: `Mods/FirstPerson.cs`, `Mods/PlayerCharacterCutoffToggler.cs`, `Mods/FakePlayer/FakePlayerManager.cs`.

### Bones and renderers

- Head bone path (CONFIRMED in `Mods/FirstPerson.cs`): `character.transform` then `"Player/Wobbly/Hip/Chest/Head"`.

  ```csharp
  root.Find("Player")?.Find("Wobbly")?.Find("Hip")?.Find("Chest")?.Find("Head");
  ```

- Clothing renderers (CONFIRMED): `ClothingPiece.GetSkinnedMeshRenderer()`, `ClothingPiece.GetExternalRenderers()` (an array), `piece.Hide()` / `piece.Show()`.
- Body renderers: no direct API seen. Plain `GetComponentsInChildren<Renderer>()` on the `PlayerCharacter` would work (**GUESS**).
- Joint drives (CONFIRMED in `Mods/PlayerScaleManager.cs`): `RagdollAnimTarget` (private `CreateDrives`, fields `drive120` ... `drive16000` of type `JointDrive`).
- Prefab paths and spawning:
  - The player character prefab address is `"Game/Prefabs/Player/Characters/PlayerCharacter.prefab"` (CONFIRMED, `FakePlayerManager.cs`).
  - The controller prefab comes from `UnitySingleton<PersistentContentManager>.Instance.GetDefaultPlayerControllerPrefab()`.
  - The character spawns via `controller.ServerSpawnPlayerCharacter(prefabOrNull, onSpawned, position, rotation, bool)`.
  - The controller field `playerCharacterPrefab` also exists.

## 2. Split screen / multiple local players

All CONFIRMED:

- Every local split-screen player is its own `PlayerController` with its own `GameplayCamera`, `PlayerControllerInputManager` and Rewired `Player`. List them with `GameInstance.Instance.GetLocalPlayerControllers()`.
- Each controller has a local player id. Seen in use: `controller.SetLocalPlayerid(0)` and the private `PlayerControllerInputManager.OnLocalPlayeridAssigned`. The getter name was not seen; `GetLocalPlayerid()` is a **GUESS**.
- `GameInstance.GetMaxAllowedLocalPlayers()` caps local players. `Mods/FakePlayer/FakePlayerPatches.cs` patches it to return 32.
- From the comments in `PlayerIdentity.cs`: split-screen controllers share the host account. Ownership (`networkObject.GetOwner()`) is filled in only on the host.
- `WeatherSystem` keeps per-local-player arrays indexed by local player id, for example `SetOverrideFogDistance(controller, ...)` (`Mods/WeatherEditor.cs`).
- Pausing one local player (`GamePause.cs`) works through the input manager, with a token object:
  - `inputManager.DisableGameplayInput(token)`
  - `DisableGameplayCameraInput`
  - `DisableInteratorInput` (sic)
  - `DisablePlayerTransformInput`
  - `DisableUIInput`
  - then `UIPauseMenuCanvasInstance.Instance?.ShowPauseMenu(player)`

```csharp
foreach (var player in GameInstance.Instance.GetLocalPlayerControllers())
{
    var inputManager = player.GetPlayerControllerInputManager();
    inputManager.DisableGameplayInput(InputToken);
}
```

## 3. Input

The game uses **Rewired** (CONFIRMED).

- The CustomItems project references `Rewired_Core`, `Rewired_GameCore` and `Rewired_Windows`, and the mod code uses `using Rewired;` with the `Rewired.Player` type.
- `Unity.InputSystem` 1.14 and `UnityEngine.InputLegacyModule` are also in the Managed folder. No mod reads them for gameplay.

### Per-player Rewired `Player` (CONFIRMED)

- `PlayerControllerInputManager.player` (field, `Rewired.Player`)
- `PlayerCharacter.player` (field), plus `PlayerCharacter.GetRewiredPlayer()`
- `GameplayCamera.player` (field)

Reading a button, from `Mods/MovementManager.cs`:

```csharp
Player?.Character?.GetRewiredPlayer()?.GetButtonDown("Jump")
```

### Action names seen (CONFIRMED)

- `"Jump"`
- `"FreeCameraForward"`, `"FreeCameraSideward"`, `"FreeCameraUp"`, `"FreeCameraDown"`, `"FreeCameraBoost"`

The free-camera names are read via `GameplayCamera.GetAxisNonRelative(name)` and `GameplayCamera.GetButton(name)` (`Mods/FreeCamModifier.cs`).

### Other input APIs (CONFIRMED)

- Camera look deltas: `GameplayCamera.GetAxisDeltaX()` and `GetAxisDeltaY()`. Sensitivity is already applied; `Mods/MouseInvertMod.cs` patches `GetAxisDeltaX`.
- Simulated character input:
  - `PlayerInput` is a game struct. `PlayerCharacterInput.EnqueueInput(PlayerInput, bool)` feeds it.
  - The latest input is read via `ClothingMoonBoots.input.GetLatestInput().bJump`.
  - Fields other than `bJump` are not shown.
- Swapping who controls a character means reassigning the `player` field on the input manager, the character and the camera (`Mods/FakePlayer/FakePlayerInstance.cs`).

### Recommended pattern for our mods

1. For each local `PlayerController`, take `GetPlayerControllerInputManager().player`.
2. Call `GetButtonDown("<Action>")` on it.
3. Raw keys (mod hotkeys) also work: `ReInput.players` (**GUESS**, standard Rewired API) or BepInEx `UnityInput.Current` / `Input.GetKeyDown` for keyboard.

## 4. Spawning props/objects

All CONFIRMED (`AssetDatabase.cs`, `PropSpawner/*.cs`, `NetworkPrefabHelper.cs`).

### Where props come from

- Props are found through **Unity Addressables**, not Resources.
  - The mod walks `Addressables.ResourceLocators` and each `locator.Keys` / `locator.Locate(key, null, out IList<IResourceLocation>)`.
  - It keeps entries whose `ResourceType` is `UnityEngine.GameObject`.
- Addresses look like `Assets/Content/....prefab` or `Game/Prefabs/...prefab`. The resolver also tries the address with the prefix and suffix removed.
- A networked prefab carries a `HawkNetworkBehaviour` with string field **`assetID`** (accessor `GetAssetId()`), which is its network prefab id.

### Networked spawn (host only)

Sources: `PropSpawner/PropSpawnManager.cs`, `Mods/ClothingStack/ClothingStackNetworking.cs`.

```csharp
NetworkPrefab.SpawnNetworkPrefab(
    keyOrAssetIdOrAddress,            // string, or a GameObject prefab
    behaviour => { /* HawkNetworkBehaviour or null */ },
    position: pos, rotation: rot, owner: null,
    bUseChunkSystem: true, bSendTransform: true, bCheckChunk: true);
// simpler overload also used:
NetworkPrefab.SpawnNetworkPrefab(_managerPrefab, Vector3.zero);
```

- `NetworkPrefab` is a static game class. It has a private `HandleSpawnedObject`, which `Mods/PropDespawnModifier.cs` patches.
- Lower level:
  - `HawkNetworkManager.DefaultInstance.InstantiateNetworkPrefab(prefabGameObject or assetId, Action<HawkNetworkBehaviour> cb, position?, rotation?, ownerConnection)`
  - `HawkNetworkManager.DefaultInstance.RegisterPrefab(HawkNetworkBehaviour)` registers a runtime-created prefab. The mod sets the private `assetID` by reflection first.
- Local, non-networked spawn: `Addressables.InstantiateAsync(key, pos, rot)`.
- Despawn:
  - Networked, on the server: `hawkBehaviour.networkObject.Destroy()`.
  - Local: `Addressables.ReleaseInstance(go)` or `Object.Destroy`.
- Gotcha documented in `NetworkPrefabHelper.cs`: a template made with `HideFlags.HideAndDontSave` passes those flags to its clones. That breaks `WorldManager.UnloadScene` chunk unloading. Clear `hideFlags` on spawned instances in `NetworkPost`.

### Props and world streaming (CONFIRMED)

- Props are `DynamicObject` or `WorldDynamicObject`, and are streamed by `WorldManager` in chunks.
- Water despawn uses `DynamicObject.OnEnteredWater` / `OnExitedWater`, the field `inWaterDestroyCoroutine` and `DestroyDynamicObjectInWaterEnumerator()`.
- View distance is set with `WorldManager.SetViewDistance`.

### Prop purchase

- `PropShop` has a server method `ServerPurchase(..., HawkRPCInfo info)`, patched in `Mods/ExploitPrevention.cs`.

## 5. Houses / furniture

CONFIRMED (`Mods/BuyUnlimitedHouses.cs`):

- `UIPlayerBasedHouseBuyHouse`:
  - fields `playerController`, `houseSign`, `playerBasedUI`, `buyButton`
  - private `TryPurchaseHouse_Internal()`
- `houseSign.GetBuyableHouse()` returns a buyable house, with `IsEnoughMoney(int)`. `houseSign.Buy(playerController)` buys it.
- `PlayerControllerUnlocker`:
  - `UnlockHouse(Scene scene, Guid houseGUID)` returns bool
  - `IsHouseUnlocked(buyableHouse)`, `OnHouseUnlocked(scene, guid)`
  - RPC ids `RPC_SERVER_HOUSE_UNLOCK` and `RPC_CLIENT_HOUSE_UNLOCK`
- Saved houses: `SavePlayerPersistentData.HousesData.AddHouse(scene, guid)`.
- Money: `PlayerControllerEmployment.GetLocalMoney()`.
- Prompt: `playerBasedUI.GetUIPromptCanvas().ShowPrompt_NotEnoughMoney()`.

```csharp
var buyableHouse = __instance.houseSign.GetBuyableHouse();
if (unlocker.IsHouseUnlocked(buyableHouse)) return false;
__instance.houseSign.Buy(__instance.playerController);
```

- Wardrobes (furniture, CONFIRMED): `Wardrobe` component with `actionEnterExitInteract.RequestEnter(controller)` and `characterUI.GetCharacterCustomize()` (`Mods/TeleportToWardrobe.cs`, `Mods/ClothingStack/*`).
- Interaction check: `PlayerControllerInteractor.HasEnteredAction()`.
- **Not found:** no house-furniture placement, house-customisation or placeable-object classes appear in any mod. The class name of the object returned by `GetBuyableHouse()` was never written out in code; something like `BuyableHouse` is a **GUESS**. The type of `houseSign` is also unknown; `HouseSign` is a **GUESS**.

## 6. Building system

- The only building-related code is a **job**: `ConstructionBuildingJobMission : JobMission`. It has fields `money` and `moneyPerBuildingPiece`, and coroutines `ServerSpawnResources()` and `ServerSpawnHammers()` (CONFIRMED, `Mods/JobManager/ConstructionBuildingJobManager.cs`).
- `ConstructionDestructionJobManager.cs` also exists and suggests a `ConstructionDestructionJobMission` (**GUESS** from the file name).
- Active job: `PlayerControllerEmployment.GetActiveJob()` returns `JobMission`.
- No free-form player building or construction system was found. Any build mode would have to be our own code, built on prop spawning (section 4).

## 7. Animals/pets; costumes/clothing/appearance

### Pets (CONFIRMED, `Mods/PetUnlocker.cs`)

- `PetManager.Instance.GetAllPets()` returns `PetAssetReference[]`, with fields `prefab` and `petName`. `PetManager.InstanceExists` is also available.
- `controller.GetPlayerControllerPet()` returns `PlayerControllerPet`, with `.UnlockPet(pet.prefab)` and `.LockPet(pet.prefab)`.
- No cat- or dog-specific classes appear. Telling species apart would mean checking `petName` or the prefab name at runtime (**GUESS**).
- How a pet is summoned or spawned was not seen.

### Clothing / appearance (CONFIRMED)

Sources: `Mods/FakePlayer/FakePlayerInstance.cs`, `Mods/ClothingStack/*`, `Mods/OutfitManagerMod.cs`, `Mods/ClothingManagerMod.cs`.

- **Clothing catalogue:** `ClothingManager.Instance`
  - `.GetAllClothingReferences()` returns `ClothingAssetReference[]`; each has `.clothingPrefab` and `.IsRawObjectValid()`.
  - `.GetClothingReference(Guid)`
  - `.allClothingAssetReferences.GetClothing(guid)`
- **Pooling:** `UnitySingleton<ClothingPoolingManager>.Instance.PopOrAllocClothing(reference, Action<ClothingPiece>)` and `.MakeAvaliable(piece)` (sic).
- **`CharacterCustomize` (per character):**
  - `SetClothingPiece(clothing, ClothingSelectionType.Hat|Top|Bottom, data: new ClothingPieceData{ clothingPrimaryColor = ... })`
  - `GetClothingHat()`, `GetClothingTop()`, `GetClothingBottom()`
  - `GetAttachedClothing()`, `AttachClothing(piece)`, `UnattachClothing(piece)`
  - `GetClothesData()` and `SetClothesData(PlayerClothesData)`. `PlayerClothesData` has `ClothingHat`, `ClothingTop`, `ClothingBottom` and `ClothingOutfit`.
  - `IsWearingOutfit()`
  - `GetCharacterColor()` and `SetCharacterColor(color, bool, float)`
- **`ClothingPiece`:**
  - `GetGuid()`, `GetPrimaryColor()`, `SetPrimaryColor(c)`, `Default()`
  - `GetClothingSelectionType()`
  - `Hide()` / `Show()`, `IsDestroyed()`
  - `GetSkinnedMeshRenderer()`, `GetExternalRenderers()`
- **Ability clothing classes:** `ClothingJetpack`, `ClothingMoonBoots`, `ClothingBananaBackpack`.
- **Unlocks:** `PlayerControllerUnlocker.UnlockClothing(owner, clothing)` and `LockClothing(clothing)`.
- **Permission:** `controller.ServerSetAllowedCustomClothingAbilities(bool)` and the field `bServerAllowedCustomsClothingAbilities`.
- Clothing changes made through `SetClothingPiece` / `SetClothesData` are synced by the game. Pieces added with only `AttachClothing` are local visuals and are not synced (from the doc comment in `ClothingStackClothing.cs`).

```csharp
fakeCustomize.SetClothingPiece(
    refs.GetClothing(sourceCustomize.GetClothingHat().GetGuid()),
    ClothingSelectionType.Hat,
    data: new ClothingPieceData { clothingPrimaryColor = sourceCustomize.GetClothingHat().GetPrimaryColor() });
```

## 8. Networking library and spawn authority

The game uses **HawkNetworking**, Rubber Band Games' own library: `HawkNetworking.dll` plus `HawkNetworkingPlugins.dll` (CONFIRMED).

### Transport

- The Steam build uses Steam P2P: `SteamP2PNetworkManager`, `SteamConnection.steamId`, Facepunch.Steamworks.
- The crossplay build swaps in EOS: `EOSNetworkManager`, `HawkNetworking.EOSConnection`, which exists only in the crossplay `HawkNetworking.dll`. See `GameBuild.cs` and `PlayerIdentity.cs`.

### Core types

- `HawkNetworkManager.DefaultInstance` (also `InstanceExists`), with:
  - `IsServer()`, `GetMe()` (local `HawkConnection`), `GetPlayers()`, `GetTimestep()`
  - `InstantiateNetworkPrefab(...)`, `RegisterPrefab(...)`
- `HawkNetworkBehaviour` (base MonoBehaviour), with:
  - `networkObject` (`HawkNetworkObject`) and string field `assetID`
  - overrides `RegisterRPCs(HawkNetworkObject)`, `NetworkPost(HawkNetworkObject)` (called on spawn on host and clients) and `OnDestroy()`
  - private `Release` and `SetupCallbacks`
- `HawkNetworkObject`, with:
  - `IsOwner()`, `IsServer()`, `GetOwner()`
  - `RegisterRPC(handler[, RPCValidateMask.Server])` returns a `byte` id
  - `SendRPC(id, RPCRecievers.Server|Others|Owner, args...)`
  - `Destroy()`
- RPC handler signature: `void Handler(HawkNetReader reader, HawkRPCInfo info)`, read with `reader.ReadString()`. `info.sender` is a `HawkConnection` with `.IsHost` and `.Me`.
- Transform sync: `HawkTransformSync`, with `Setup()`, `OnSnapshot`, `bSetup`, `mainTransformSyncSetting`, `externalTransformsToSync`.

### Spawn and authority rule (CONFIRMED by guards in the mod code)

- **Only the host/server may spawn or destroy networked objects.** Examples: `FakePlayerManager.CanSpawn => HawkNetworkManager.DefaultInstance.IsServer()`; the PropSpawner reports "networked props can only be spawned by the host".
- Clients can spawn only local, non-networked Addressables instances.
- Clients ask the host through an RPC to `RPCRecievers.Server`. The host then rebroadcasts with `RPCRecievers.Others`.
- `Others` RPCs relayed through the server arrive with `info.sender` set to the host.
- Custom network managers are spawned by the host in `SceneManager.sceneLoaded`. The mod skips additive loads and the `MainMenu` / `LoadingScene` scenes.

```csharp
public override void RegisterRPCs(HawkNetworkObject networkObject)
{
    base.RegisterRPCs(networkObject);
    RPC_REQUEST = networkObject.RegisterRPC(ServerReceiveStack);
    RPC_BROADCAST = networkObject.RegisterRPC(ClientReceiveStack, RPCValidateMask.Server);
}
```

(from `Mods/ClothingStack/ClothingStackNetworkManager.cs`, lstwoMODS)

## 9. Unity version and Managed DLLs

### Unity version

- The WobblyPhysgun Thunderstore page says it targets "the Wobbly Life EOS assemblies and Unity 2022.3.62f2". That is a **secondary-source claim** and refers to the crossplay build. The Steam build is most likely the same Unity version.
- The lstwoMODS projects target `net472` and use `BepInEx.Core 5.*`, `UniverseLib.Mono 1.5.1` and HarmonyX.

### Managed DLLs

Listed in the references in `CustomItems/CustomItems.csproj`, all from `WobblyLife.GameLibs 1.1.0`:

- **Game code:** `Assembly-CSharp.dll` (most gameplay types), `HawkNetworking.dll`, `HawkNetworkingPlugins.dll`, `Game.dll`, `Sandbox.dll`, `Space.dll`, `HideAndSeek.dll`, `ArcadeMachine*.dll`, `ModWobblyLife.dll` (official mod SDK), `UMod*.dll`.
- **Third-party:**
  - Input and networking: `Rewired_Core.dll`, `Rewired_GameCore.dll`, `Rewired_Windows.dll`, `Facepunch.Steamworks.Win64.dll`, `EOS.dll`.
  - Audio and camera: `FMODUnity.dll`, `Cinemachine.dll`.
  - Vehicles and physics helpers: `NWH.*` (vehicles and water), `TechniePhysicsCreator.dll`.
  - Serialization and compression: `Bitstream.dll`, `FloatCrusher.dll`, `QuatCompress.dll`, `HalFloat.dll`, `ByteConverter.dll`, `Newtonsoft.Json.dll`.
  - Other: `TrashMan.dll`, `MeshCombine.dll`.
- **Unity packages:** `Unity.Addressables.dll`, `Unity.ResourceManager.dll`, `Unity.InputSystem.dll` (1.14), `Unity.TextMeshPro.dll`, `Unity.Localization.dll`, `Unity.Postprocessing.Runtime.dll`, `Unity.ProBuilder*.dll`, `Unity.AI.Navigation.dll`, `UnityEngine.*Module.dll`.
- Minimum for our mods: `Assembly-CSharp`, `HawkNetworking`, `Rewired_Core`, `Unity.Addressables`, `Unity.ResourceManager`, `UnityEngine.CoreModule`, `UnityEngine.PhysicsModule` and `UnityEngine.AnimationModule`. Add `Facepunch.Steamworks.Win64` only if Steam ids are needed.

## Other confirmed singletons and classes, for reference

- `WeatherSystem.Instance` (`ServerLightingStrike(pos)`, `SetWeather`, `GetDefaultRainingType`)
- `DayNightCycle`
- `SaveGameManager`
- `AchievementManager.Instance.UnlockAchievement(ach, controller)`
- `RewardManager`
- `FreemodeGamemode`, `HideAndSeekGamemode`, `WobbleRunGamemode`; `GameInstance.Instance.GetGamemode()`
- `TelephoneBox.ServerSpawnVehicle`
- Vehicles: `PlayerVehicle`, `PlayerVehicleMovement`, `PlayerVehicleRoadMovement`, `PlayerVehicleDestructable`
- `FishingSystem`
- `UnitySingleton<T>` (generic singleton base)
- `MinimapIcon`
- `PlayerNPCDialog`

## Official Mod SDK (context only, different API)

The Doxygen docs at rubberbandgames.com list wrapper types for the game's own modding (`ModWobblyLife` namespace). Examples:

- Players and characters: `ModPlayerController`, `ModPlayerCharacter`, `ModPlayerBody`, `ModPlayerCharacterMovement`, `ModRagdollController`
- Camera and input: `ModGameplayCamera`, `ModInputPlayer`, `ModPlayerControllerInputManager`
- Networking: `ModNetworkBehaviour`, `ModNetworkManager`, `ModNetworkObject`
- Clothing and wardrobe: `ModCharacterCustomize`, `ModClothingManager`, `ModWardrobe`
- Objects and interaction: `ModDynamicObject`, `ModActionEnterExitInteract`

These wrappers mirror the internal names above, which helps cross-check them. BepInEx mods use the internal types directly.

## Sources

- https://github.com/lstwoMODS/lstwoMODS-WobblyLife (read via https://cdn.jsdelivr.net/gh/lstwoMODS/lstwoMODS-WobblyLife@main/ and https://data.jsdelivr.com/v1/packages/gh/lstwoMODS/lstwoMODS-WobblyLife@main?structure=flat). Files read include:
  - `lstwoMODS WobblyLife/UI/TabMenus/PlayerBasedModsWindow.cs`
  - `lstwoMODS WobblyLife/PropSpawner/PropSpawnManager.cs`, `PropAddressResolver.cs`
  - `lstwoMODS WobblyLife/AssetDatabase.cs`, `NetworkPrefabHelper.cs`, `GamePause.cs`, `GameBuild.cs`, `PlayerIdentity.cs`
  - `lstwoMODS WobblyLife/Mods/FakePlayer/*.cs`, `FirstPerson.cs`, `MovementManager.cs`, `FreeCamModifier.cs`, `PlayerScaleManager.cs`, `PlayerCharacterCutoffToggler.cs`, `RagdollManager.cs`, `CharacterManager.cs`, `ControllerManager.cs`, `BuyUnlimitedHouses.cs`, `TeleportToWardrobe.cs`, `PetUnlocker.cs`, `ClothingManagerMod.cs`, `OutfitManagerMod.cs`, `ClothingStack/*.cs`, `JobManager/ConstructionBuildingJobManager.cs`, `PropDespawnModifier.cs`, `ExploitPrevention.cs`, `WeatherEditor.cs`, `PlayerMacroType.cs`
  - `CustomItems/CustomItems.csproj`, `CustomItems/CustomItem.cs`, `lstwoMODS WobblyLife/lstwoMODS WobblyLife.csproj`
- https://github.com/lstwoMODS/lstwoMODS-Core (README only; it is game-independent)
- https://www.nuget.org/packages/WobblyLife.GameLibs (package metadata only; not downloaded)
- https://thunderstore.io/c/wobbly-life/p/Zappyix/WobblyPhysgun/ (Unity 2022.3.62f2 claim; no source link)
- https://thunderstore.io/c/wobbly-life/p/TitusXRyu/LargerLobbyMod/ (BepInEx 6 IL2CPP, Game Pass build; no source link)
- https://azzamods-en.pages.dev/mods/wobbly-life/remove-world-bounds (AzzaMods; closed-source app)
- https://www.rubberbandgames.com/wobblylife/modsdkdocs/classes.html and https://rubberbandgames.com/wobblylife/modsdkdocs/creatingacustomplayercontrollerpage.html (official Mod SDK)
