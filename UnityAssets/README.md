# Unity kit: turn downloaded models into game-ready bundles

Our own models (Skylar's redtail catfish, later her black cat) must be packed into AssetBundles with the
**same Unity version as the game: 2022.3.62f2**. This folder is a ready-made Unity project with buttons that
do the packing. You only need to do the setup once.

The game uses Unity's built-in renderer, so our models use their own simple shader (`WobblyMods/SimpleLit`),
which is packed into the bundle. That way they can't come out pink.

> Written without a Unity Editor to test against. If Unity shows an error, copy it to the cloud chat.

## One-time setup (about 30 minutes, mostly downloading)

1. Install **Unity Hub** from unity.com (free; a free Unity account is needed).
2. In Unity Hub: **Installs → Install Editor → Archive**, and pick **2022.3.62f2**.
   If only `2022.3.62f1` is offered, that's fine too. Add **Windows Build Support (Mono)** if it's offered.
3. Get this folder onto the PC. Either clone the repo, or the PC session can copy `UnityAssets/` from it.
4. Unity Hub: **Projects → Add → Add project from disk**, and pick the `UnityAssets` folder. Open it.
   The first open takes a few minutes. It also downloads Unity's glTF importer (`com.unity.cloud.gltfast`)
   for `.glb`/`.gltf` models. If that package version fails, open **Window → Package Manager**, remove it, and add
   "glTFast" from the Unity Registry instead.

## Make Skylar's redtail catfish

1. Unzip the Sketchfab download ("Free Low Poly Cartoon Koi Fish", glTF) into `UnityAssets/Assets/Models/Koi/`.
2. In Unity's **Project** window, open `Assets/Models/Koi` and click the model (the `.glb`/`.gltf`, or `scene`).
3. Menu **Wobbly Mods → Make Redtail Catfish from selected model**. It makes
   `Assets/Bundles/wobbly_animals/RedtailCatfish.prefab`:
   - dark grey body
   - cream-white belly
   - bright orange-red tail and fins
   - dark whiskers
   - its swim animation playing
4. Look at it in the Scene view. If a part has the wrong color, click that material under
   `Assets/Bundles/wobbly_animals/Materials` and change its **Color**.
5. Menu **Wobbly Mods → Build Bundles**. If the game is in the default Steam folder, the bundle is copied
   to `BepInEx\plugins\WobblyMods\bundles\`. Otherwise, copy the files from `UnityAssets/BuiltBundles/`
   (not the `.manifest` files) there yourself.
6. In the game (house menu 0.9.0 or newer): **Animals → Redtail catfish**.

**Credit (the license requires it):** "Free Low Poly Cartoon Koi Fish" by MarkoJantti (Sketchfab), CC BY 4.0.

## Make the black cat (later)

Same steps, with the cat model in `Assets/Models/Cat/`, and **Wobbly Mods → Make Black Cat from selected model**
(black fur, yellow eyes). Playing *as* the cat needs more mod work. See `PROJECT_NOTES.md`, stages 3 and 4.

## What's in here

- `Assets/Editor/WobblyModelMaker.cs`: the menu buttons (recolor, set up the animation, build bundles).
- `Assets/Shaders/WobblySimpleLit.shader`: the simple cartoon shader packed with our models.
- `Assets/Models/`: put downloads here (not committed to git).
- `Assets/Bundles/<bundle>/`: prefabs to pack. Each folder becomes one bundle.
