# Free asset research: black cat player + build mode

Researched 2026-11-16. For each asset I opened the page and read the license. Where I could download a file (Kenney, Quaternius, OpenGameArt), I unpacked it in a temporary folder outside the repo and inspected it. Nothing was downloaded into this repo.

These assets are for **private, personal mods**. CC0 needs no credit. CC-BY needs a credit line, for example in a `CREDITS.txt` inside the mod folder. That costs nothing, so a CC-BY asset is fine when it is clearly the best fit.

Where a page could not be opened directly, the entry says so: itch.io and Poly Pizza block automated page fetches, and Sketchfab downloads need a free login.

---

## A. Cat models

### Top 3

| Rank | Asset | License | Formats | Rig / animations | Tris | Black fur + yellow eyes? |
|---|---|---|---|---|---|---|
| **1** | **Cute Pets FREE Sample: Cat, Fox, Owl** (Vuena) | CC0 1.0, stated on the page; "no credit needed" | FBX (for Unity) + GLB; LOD0 and a mobile LOD1 | Skinned rig. **12 clips: idle, idle_alt, walk, run, jump, eat, sit, sleep, attack, call, hit, death.** 30 fps, in place, no root motion | Not listed (LOD0 + LOD1) | One material with a painted 2048 px albedo + ORM texture. To recolor, paint the albedo PNG (fur black, irises yellow) in Krita/GIMP. 15-30 min of work, not a slider |
| **2** | **Kenney Cube Pets: `animal-cat`** | CC0 1.0 (`License.txt` in the zip: "Creative Commons Zero, CC0") | FBX, GLB, OBJ | Not a skinned mesh. Separate parts (body, 4 legs, tail) animated by transform. **Clips: `static`, `walk`, `run`.** No jump or sit: do those in code (hop + squash) | **595** (counted from the GLB) | One shared `colormap.png` palette texture. Recolor by editing the palette pixels the cat uses. It is already a dark purple cube cat with orange eyes, so it is close already |
| **3** | **Quaternius Animal Pack Vol.2: `Cat`** | CC0, on the OpenGameArt upload (2017) | FBX, OBJ, Blend | Skinned (`CatArmature`). **Clips: `Idle`, `Walking` only** | About 800 (402 faces, mostly quads) | **Easiest to recolor:** 3 flat materials (`Grey`, `Pink`, `White`). Set Grey and White to black. **It has no modeled eyes**, so add 2 small yellow spheres (emissive looks great) parented to the head bone |

**Why #1 wins:** it is the only CC0 cat that is rigged, cartoony and ships with walk + run + jump + sit + idle. Its `call` clip is very likely a meow pose (the page also mentions "opening mouths"), so pair it with the meow sound. It fits Wobbly Life's soft, cute look best.

Caveats for #1:
- The pack is brand new: published about 17 days ago, animations patched to v3.1 about 12 days ago.
- The price is "name your own price", so $0 works.
- The page is tagged "AI Assisted" and explains that this means the code only: "meshes and textures not AI-generated". There is no NoAI clause.
- I could not download it to check bone count or whether the eyes have their own material, because itch.io blocks automated fetches. Check both in Blender before committing.
- Page: https://vuena.itch.io/blocky-pets-free (devlog: https://vuena.itch.io/blocky-pets-free/devlog/1749071/small-update-animation-fixes)

**#2 details.** https://kenney.nl/assets/cube-pets (also on OpenGameArt: https://opengameart.org/content/cube-pets). It is version 1.0 from March 2026, with 16 animals.
- Being blocky and having no bones is a plus here: it is trivial to attach to the player, and nothing in it can break.
- It is the fallback if #1's rig gives trouble.

**#3 details.** https://opengameart.org/content/animated-animales-low-poly. The cat is also mirrored on Poly Pizza as "Cat by Quaternius, CC0" (https://poly.pizza/m/2f54vbV0In). I saw that mirror only in search results, because poly.pizza returns 403 to automated fetches.
- The style is faceted and realistic-proportioned rather than chubby-cute.
- With only 2 clips, run and jump would have to be faked by speeding up the walk and adding a code hop.

### CC-BY alternatives (Sketchfab; free account needed to download; glTF always offered)

All of these are "CC Attribution" (CC BY 4.0). Credit format: *"<Model name>" by <author> (sketchfab URL), CC BY 4.0*. Sketchfab does not show clip names without downloading.

| Asset | URL | Tris | Anim clips | Notes |
|---|---|---|---|---|
| Toon Cat FREE (Omabuarts Studio) | https://sketchfab.com/3d-models/toon-cat-free-b2bd1ee7858444bda366110a2d960386 | 2.6k | 1 | Very cute quadruped, closest to Wobbly Life's look. One material + one texture, so the recolor is easy. Only 1 animation |
| Black Cat PSX (Bonvikt) | https://sketchfab.com/3d-models/black-cat-psx-62416ff46cef45599c51dc9bc0b83b43 | 466 | 4 | **Already a black cat with glowing yellow eyes**, and rigged. The look is retro/realistic, not cartoony |
| Cartoon Cat (baydinman) | https://sketchfab.com/3d-models/cartoon-cat-4a738eaed9d34547a3f5a705a0e37c6a | 4.1k | 25 | Hand-painted orange tabby with big eyes. The credits mention a sword and a gun, so it may be a standing "hero cat" rather than a pet. Check it before using |
| Black Cat (Mifan..Rice) | https://sketchfab.com/3d-models/black-cat-722d23065e004e2789796894a6db9ae8 | 1.6k | 2 | Black with yellow eyes, but it is a stylized silhouette of a sitting cat that moves to music. Not a walker |

### Checked and not recommended

- **Quaternius Ultimate Animated Animal Pack**: 12 animals, CC0, great clips (https://quaternius.com/packs/ultimateanimatedanimals.html), but **no cat**. None of Quaternius's current packs include a cat; Vol.2 above is the only one.
- **Toon Cat** (Blend Swap, squibblejack, CC0), https://www.blendswap.com/blend/22931: a Blender/Cycles render model with hair particles. Not game-ready.
- **Cat Pilot**, CC0, https://opengameart.org/content/cat-pilot-rigged-animated: a pilot character. Its clips are walk, run, cheer, goggles and flying. Not a pet cat.
- **Low poly cat thing** (miziziziz), CC0, https://opengameart.org/content/low-poly-cat-thing: a tiny two-legged cone blob. Collada only, with idle/walk/jump clips.
- **Free Cats Characters** (Alpha Centauri Studio, itch.io): no license text on the page, and no animations ("ready to animate").
- **Lowpoly Cat + Run** (Daily Lowpoly, itch.io): the license is only "can use in commercial projects", which is vague, and it has a run clip only.
- **Quirky Series FREE Animals** (Omabuarts, itch.io): no cat, and no license text on the page.
- **Sketchfab fan-art cats** (Poppy Playtime, Adventure Time, Murdered: Soul Suspect, "Maxwell" meme): CC-BY on Sketchfab, but they depict other companies' characters. Skip them.
- **Sketchfab CC0 filter**: a search for animated cats returned **zero** CC0 cats (only a catfish).

---

## B. Cat sounds (all CC0, each page opened)

**Top pick: "Cat Purr & Meow" by Kerzoven (OpenGameArt), CC0.** https://opengameart.org/content/cat-purr-meow
- The author recorded these specifically because CC0 cat sounds were hard to find.
- Files: `cat_purrsleepy_loop.wav` and `cat_purractive_loop.wav` (ready-made **purr loops**), `cat_mewfood.wav`, `cat_mewpurr.wav`, `cat_mewpurr2.wav`, `cat_softmew.wav`.
- The author notes some background noise on the meows.

Extra meows and purrs, with license, length and format read from each page:

| Sound | URL | License | Length / format | Use |
|---|---|---|---|---|
| "cat meow" (tuberatanka) | https://freesound.org/people/tuberatanka/sounds/110011/ | CC0 | 1.5 s, WAV 44.1k | Classic hungry meow; 47k downloads |
| "cat meow short" (skymary) | https://freesound.org/people/skymary/sounds/412017/ | CC0 | 1.75 s, WAV 44.1k | Short "meow" button |
| "Small Cat Meow" (PolterKaist) | https://freesound.org/people/PolterKaist/sounds/795848/ | CC0 | 0.96 s, WAV 48k | Small, kitten-like |
| "SND_CatHighMeows" (megrez7274) | https://freesound.org/people/megrez7274/sounds/508686/ | CC0 | 1.7 s, WAV 48k mono | Two high meows |
| "Cat, various meows, squeeks, purrs" (TRP) | https://freesound.org/people/TRP/sounds/571216/ | CC0 | 51.7 s, FLAC 48k | Cut several variants from it |
| "Purr of a cat" (lextrack) | https://freesound.org/people/lextrack/sounds/263778/ | CC0 | 36.7 s, MP3 | Long purr bed |
| "Cat purr" (Richard1052) | https://freesound.org/people/Richard1052/sounds/585775/ | CC0 | 7 s, WAV | Natural purr |
| "kitten mew" (AntumDeluge, OGA) | https://opengameart.org/content/kitten-mew | CC0 | OGG + WAV | A human imitating a kitten; cute and very clean |
| "Meow" (IgnasD, OGA) | https://opengameart.org/content/meow | CC0 | OGG | Extra variant |

**Tip:** pick 3-4 meows and play a random one with slight pitch variation (0.9-1.2). That keeps it fun for a 5-year-old pressing the button 200 times.

I did not find any cat sounds from Kenney. Its audio packs are UI, impact and sci-fi sounds.

---

## C. Building materials and kits

### Licenses (pages read)

- **ambientCG**: "Creative Commons CC0 1.0 Universal License" for every asset (https://docs.ambientcg.com/license/).
- **Poly Haven**: "Our assets are all licensed as CC0" (https://polyhaven.com/license).
- **Kenney**: each kit page says "License: Creative Commons CC0", and every zip contains a `License.txt` stating CC0.
  - kenney.nl pages carry an HTML `<meta name="robots" content="noai, noimageai">` tag.
  - That tag is a crawler instruction for the website. **It is not part of the asset license.** The assets themselves are plain CC0.

### PBR textures (CC0). Use 1K or 2K: Wobbly Life is cartoony and small, so 4K is wasted.

| Material | ambientCG (https://ambientcg.com/view?id=ID) | Poly Haven (https://polyhaven.com/a/SLUG) |
|---|---|---|
| Wood planks / floor | `WoodFloor043`, `WoodFloor051`, `Planks037A` | `wood_floor`, `weathered_brown_planks`, `plywood` |
| Brick | `Bricks060`, `Bricks097`, `Bricks051` | `red_brick`, `red_brick_03`, `brick_wall_001` |
| Stone / cobbles | `PavingStones128`, `PavingStones138`, `Rock030` | `stone_wall`, `stone_brick_wall_001`, `stone_tiles_02` |
| Concrete | `Concrete034`, `Concrete046`, `Concrete042A` | `concrete_floor_worn_001`, `concrete_floor_02` |
| Roof tiles | `RoofingTiles013A`, `RoofingTiles014A`, `RoofingTiles006` | `roof_09`, `ceramic_roof_01`, `roof_slates_03` |
| Plaster / paint | `Plaster001`, `PaintedPlaster017`, `Plaster002` | `painted_plaster_wall`, `white_plaster_02`, `grey_plaster` |
| Floor tiles | `Tiles141`, `Tiles143` | n/a |
| Glass | No good CC0 *texture* exists, and none is needed | Use a URP/Standard **transparent** material: tint color, alpha about 0.3, high smoothness. Optionally use `Facade001` (ambientCG) for windowed facades |

ambientCG offers each material as 1K/2K/4K… in JPG or PNG. `WoodFloor043` 1K-JPG is about 3.4 MB. The zips include Color, NormalGL/DX, Roughness, AO and Displacement maps. Use NormalGL for Unity.

**Style tip:** photoreal PBR can look out of place next to Wobbly Life's flat pastel look. To keep it cohesive, try one of these:
- Desaturate and brighten the albedo.
- Drop the roughness/AO maps.
- Use the Kenney kits below, which are flat-shaded already, and keep PBR only for the materials the "paint" tool offers.

### Modular kits that snap to a grid

| Kit | URL | License | Contents (verified) | Formats | Fit |
|---|---|---|---|---|---|
| **Kenney Building Kit** (top pick) | https://kenney.nl/assets/building-kit | CC0 | 80 files (v1.0, 2024). The preview image shows: straight, corner and curved walls; walls with many window styles; doorways and arches; doors (page lists "Animation"); straight and spiral-ish stairs with railings; floor and ceiling tiles; flat roof pieces with parapets; columns; pipes; fences | Kenney standard: FBX/GLB/OBJ | Clean, toy-like, snaps on a grid. Matches Wobbly Life's look best. Pitched roofs are missing, so combine it with a roof kit |
| Kenney Modular Buildings | https://kenney.nl/assets/modular-buildings | CC0 | 100 files (v2.1, complete remake), with variations | FBX/GLB/OBJ | Town houses from stackable floors |
| Kenney Fantasy Town Kit | https://kenney.nl/assets/fantasy-town-kit | CC0 | 160 files (v2.0, 2022): medieval walls, roofs, town pieces | FBX/GLB/OBJ | Has **pitched roofs** |
| Kenney Furniture Kit | https://kenney.nl/assets/furniture-kit | CC0 | 140 files | FBX/GLB/OBJ | Interior props for houses |
| Kenney Prototype Textures | https://kenney.nl/assets/prototype-textures | CC0 | 75 grid textures | PNG | Ghost/preview material for build-mode placement |
| **Quaternius Medieval Village MegaKit** | https://quaternius.com/packs/medievalvillagemegakit.html | CC0 (pack page; see caveat below) | 304 models (Jan 2025). "Over 300 modular environment pieces, all designed to snap perfectly to a grid": walls with inside and outside faces, floors, stairs, **modular roofs**, doors, windows, vines | FBX, OBJ, glTF (Blend in the paid Source tier) | The most complete free snap-kit. The free tier is about 60-70% of the pack |
| Quaternius Downtown City MegaKit | https://quaternius.com/packs/downtowncitymegakit.html | CC0 (pack page) | 315 models (May 2026): modular city buildings and streets | FBX, glTF | Modern city look |
| Quaternius Ultimate House Interior | https://quaternius.com/packs/ultimatehomeinterior.html | CC0 (pack page) | 123 models: doors, windows, kitchen and bathroom furniture | FBX, OBJ, Blend | Interiors |

**Quaternius license caveat.** Every pack page still says "License CC0". However, quaternius.com/license.html now shows a new **"Quaternius Asset License (QAL) v1.0"** (updated 8/28/2026).
- What QAL allows: free personal and commercial use, no credit required.
- What QAL forbids: redistributing the assets themselves as assets.
- QAL is not retroactive, and pack pages still say CC0. Either way, **private mod use is fine.**
- If you ever publish the mod, ship the assets compiled inside a Unity AssetBundle, not as loose FBX files.

---

## D. Feasibility: playing as a four-legged cat

The game's player is an active-ragdoll humanoid. Per `game-internals.md`:
- `PlayerCharacter` contains a hip `Rigidbody`, a `RagdollController`, and joint drives in `RagdollAnimTarget`.
- `PlayerCharacterMovement` has `IsGrounded()`.

**Recommended approach: a "costume" cat that rides the existing physics body.**

1. **Prepare the asset.** Import the cat FBX into a Unity project on the **same Unity version as the game**. Set the rig to **Generic**, not Humanoid. Humanoid retargeting does not work for quadrupeds. Recolor it, make a prefab with an `Animator`, and build an **AssetBundle**.
2. **At runtime (mod code):**
   - Load the bundle and instantiate the cat.
   - Parent it to the player's hip Rigidbody transform, or have it follow the hip position each `LateUpdate`, keeping the yaw only so the cat stays level.
   - Lower it so the paws touch the ground.
   - Hide the player's own body and clothing renderers. `ClothingPiece.Hide()` handles the clothing; the body needs `GetComponentsInChildren<Renderer>()` on the character.
3. **Drive the Animator from the physics.**
   - `Speed` = horizontal velocity magnitude of the hip Rigidbody. Use it to blend idle, walk and run.
   - `Grounded` = `PlayerCharacterMovement.IsGrounded()`. When it is false, play jump/fall.
   - A "meow" key triggers the `call` clip plus a random meow AudioSource.
   - Standing still for a few seconds plays sit or sleep, and starts the purr loop.
   - With in-place clips (which Vuena's are), set **Apply Root Motion = off**.
4. **Climbing.** The existing game's grabbing and climbing still works on the hidden humanoid, because the physics bodies are untouched. While the player is grabbing a wall, play a "jump" or "climb" pose on the cat. The cat then appears to scramble up whatever the wobbly body climbs.
5. **Collision fit.** The humanoid capsule is taller than a cat. You can either:
   - accept it (it is invisible), or
   - scale the player down with the existing scale-mod approach (`PlayerScaleManager`). That makes doorways and climbing feel cat-sized.
6. **Multiplayer.** Other players see the cat only if they run the mod too. Fine for a family LAN or private game.

**Effort:** about 1-2 evenings for a working cat that walks, runs, jumps and meows, once mod loading and AssetBundles are set up.

**Not recommended: true quadruped physics.** That would mean replacing the humanoid ragdoll with a 4-legged active ragdoll: new joints, balance controllers, and new grab, climb and ragdoll logic hooked into `PlayerCharacterMovement` and `RagdollController`. It is weeks of work, likely to fight the game's netcode, and would break every time the game updates.

**Gotchas:**
- Kenney Cube Pets animate by transforms, not bones. An `Animator` still plays the clips as long as the hierarchy names stay intact.
- If the cat jitters, smooth its follow with `Vector3.Lerp` and `Quaternion.Slerp` on the hip.
- Keep the cat's colliders off (or on an ignored layer) so it never pushes the real body.

---

## Credits template (only needed for CC-BY picks)

```
Cat model: "Toon Cat FREE" by Omabuarts Studio (https://sketchfab.com/3d-models/toon-cat-free-b2bd1ee7858444bda366110a2d960386), CC BY 4.0
CC0 assets used (credit appreciated, not required): Vuena, Kenney, Quaternius, Kerzoven, ambientCG, Poly Haven, freesound users listed above.
```
