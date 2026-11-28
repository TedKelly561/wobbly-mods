# Giant Redtail Catfish: free 3D model research

Goal: a giant redtail catfish (*Phractocephalus hemioliopterus*) for a private Wobbly Life mod, in three forms:
1. a spawnable sandbox prop
2. a rideable boat (catfish mesh on top of a game boat)
3. a drivable car (catfish mesh on top of a game car)

The catfish only has to look right. The game vehicle keeps doing the physics.

Researched 2026-11-16. Each license below was read from the model's own page or from the Sketchfab API `license` field. Nothing was downloaded into the repo. Only small files for inspection were saved to the session scratchpad.

---

## TL;DR: top 3

| Rank | Model | License | Why |
|---|---|---|---|
| **1** | **"Red Tail Catifish"** by fredy120488 (Sketchfab) | **CC BY 4.0** | The only free model that is actually a redtail catfish, already painted dark back, white belly band and red tail. 11k tris and 1 material. It can be used almost as-is. |
| **2** | **"3d low poly catfish"** by gfroad (OpenGameArt) | **CC0** | Real catfish shape with whiskers, dark back and cream belly, only 284 polys. The tail fin is its own island on the texture, so it is easy to paint red. The best option if CC0 is a must. |
| **3** | **"Free Low Poly Cartoon Koi Fish"** by MarkoJantti (Sketchfab) | **CC BY 4.0** | The most "Wobbly Life" looking option: chunky toon fish, dark grey body, long whiskers, 6 animations including swim, and 5 flat-colour materials with no textures, so recolouring is just changing material colours. |

**Pick:** use #1 for the prop and for the boat and car shells. It reads as a redtail right away, which matters most to a 5-year-old. Credit: *"Red Tail Catifish" by fredy120488, CC BY 4.0, https://sketchfab.com/3d-models/red-tail-catifish-03424c31e4f448cf8343d2e9df9ca702*. If you want a wagging tail, use #3 (it has a rig) and recolour it. If you need CC0 only, use #2.

No CC0 redtail catfish exists on any source checked. No CC0 fish-shaped car or boat was found either; the best ones are CC BY (see section C).

---

## A. Redtail catfish and close substitutes

### 1. Red Tail Catifish (fredy120488), RANK 1
- **URL:** https://sketchfab.com/3d-models/red-tail-catifish-03424c31e4f448cf8343d2e9df9ca702
- **License:** "CC Attribution: Author must be credited. Commercial use is allowed." Links to http://creativecommons.org/licenses/by/4.0/. No NoAI or other extra tags on the model.
- **Formats:** Sketchfab download. Every downloadable model gets an auto-converted **glTF/GLB and USDZ**, and the original upload format is usually offered too. A free Sketchfab account is needed to download.
- **Polys:** 11,260 faces and 5,632 verts.
- **Rig/anim:** none (0 animations).
- **Materials:** 1 material with 3 textures (likely base colour, normal and one more map). Published 2024-05. No description.
- **Look:** soft, hand-sculpted, almost clay-like style. Dark blue-grey back, **white belly band**, **red-pink tail and fins**, yellow eye. Chunky and friendly, which suits Wobbly Life.
- **Work to make it a redtail:** very little. Optional extras:
  - push the tail from pink-red to a brighter orange-red with a hue shift in the texture
  - darken the back towards charcoal
  - add a few black spots on the back
  - whiskers are not clearly visible in the thumbnail; if missing, add 3 pairs of simple tapered cylinder barbels as child objects
- **Tail wag:** the mesh is one piece with no bones. Either rig it in Blender (3 to 4 bones along the spine) or skip wagging.

### 2. 3d low poly catfish (gfroad), RANK 2 (best CC0)
- **URL:** https://opengameart.org/content/3d-low-poly-catfish
- **License:** page lists **"License(s): CC0"**. Also in the OGA collection "3d Animals under CC0".
- **Files:**
  - `catfish.zip`: 3ds Max `.max` files: raw model, **Swim** animation and **Jump** animation
  - `catfish_obj_0.zip`: static OBJ + MTL
  - `catfish.png`: 1024x1024 RGBA texture
- **Polys:** 284 polys and 151 verts. The OBJ has 284 faces and one group `catfish`.
- **Rig/anim:** swim and jump exist, but **only inside the .max files**. Opening them needs 3ds Max. The author says the OBJ is "only raw model without bones and animations".
- **Materials and texture layout:** I inspected these in the scratchpad.
  - One material (`Material__28`) uses `catfish.png` as diffuse **and as alpha** (`map_d`). The fins are cut out by alpha, so in Unity use an alpha-clip/cutout shader.
  - The texture is a hand-painted realistic UV atlas. The body is one big diagonal island: dark olive-grey back, **cream-white belly**, eye, and a mouth with barbels at the lower left.
  - Each fin is its **own separate island**: tail fin at top right, dorsal fin, pectoral fins, pelvic fins and anal fin. There are also two thin strips at the bottom for the long whiskers.
- **Look:** a real channel-catfish silhouette with flat head and long whiskers. Realistic paint, not cartoony.
- **Work to make it a redtail:** easy, because the tail is a separate UV island.
  1. Fill the tail-fin island bright red-orange (#E8401C to #FF5A1F).
  2. Darken the back to charcoal (#2B2D31) and add a few black spots.
  3. Keep the cream belly and push it whiter.
  4. Optionally tint the dorsal fin dark grey.

  This takes about 15 minutes in Krita, GIMP or Photoshop. Because it is so low-poly, it looks fine scaled up as a cartoony prop.
- **Tail wag:** to get the swim animation, re-rig the OBJ in Blender (3 to 4 bones) or use the Unity script trick in the notes below.

### 3. Free Low Poly Cartoon Koi Fish (MarkoJantti), RANK 3 (best rigged and cartoony)
- **URL:** https://sketchfab.com/3d-models/free-low-poly-cartoon-koi-fish-62ea29193ca14f7fb65c5876bfc2ea66
- **License:** "CC Attribution: Author must be credited. Commercial use is allowed." (CC BY 4.0)
- **Formats:** Sketchfab download (auto glTF/GLB and the original upload).
- **Polys:** 4,330 faces and 3,030 verts.
- **Rig/anim:** **yes, 6 animations**: Swimming, Turning left/right, Clapping fins, Blinking (blend shape), Jumping from the water.
- **Materials:** **5 materials, 0 textures.** The colours are flat material colours, so recolouring is just a matter of changing material colours in Unity.
- **Look:** big-eyed toon fish with a **dark grey/black body, lighter grey belly**, long **yellow whiskers**, yellow trim lines and a few red patches. The colours are already close to a redtail.
- **Work to make it a redtail:** set the belly material to cream-white and the trim/whisker material to dark grey or white.
  - The tail is grey with yellow edging and probably shares a material with the other fins. To get a **red tail only**, split the tail faces into their own material in Blender (select the tail, assign a new material). That takes about 5 minutes.
- **Caveat:** the body is a deep perch/koi shape, not a long flat catfish. Squash it in Blender (scale Z about 0.75, stretch the body) to get closer.

### Other candidates checked

| Model | URL | License | Polys | Anim | Notes |
|---|---|---|---|---|---|
| Quaternius **Animated Cute Fish Pack** (52 models incl. `ArmoredCatfish` and `Boat`) | https://quaternius.com/packs/cutefish.html | **CC0** (page says "License CC0" and links CC0 1.0) | low-poly | Yes, animated (FBX/OBJ/Blend) | The Google Drive file list contains **ArmoredCatfish.fbx/.obj** and **Boat.fbx**. I could **not inspect** them because Drive returned "Quota exceeded" on every download. "Armored catfish" is a pleco/corydoras type, so expect a cute round fish rather than a long redtail. Big-eyed style fits Wobbly Life well. Worth a manual look; Poly Pizza also mirrors Quaternius packs in glTF. |
| Quaternius **Animated Fish Pack** (2018, 7 species) | https://quaternius.itch.io/lowpoly-animated-fish (also https://opengameart.org/content/animated-fish) | **CC0** | low-poly | 1 swim anim each, rigged | No catfish (shark, whale, dolphin, manta and others). Usable as a rigged base if you want a CC0 swim cycle. |
| Catfish (VivernaNeva) | https://sketchfab.com/3d-models/catfish-7b83bd67d1d34c82ae5763a26488cb01 | CC BY 4.0 | 2,328 | No | Untextured, 1 material, curled pose. Looks like a smiling dugong more than a catfish. Not recommended. |
| Catfish test (masterdef) | https://sketchfab.com/3d-models/catfish-test-afa228948e694cdf88edf5ce8a82842a | CC BY 4.0 | 4,270 | No | Wels-type with whiskers and an open mouth, sculpt test, 1 material and 2 textures. Realistic and a bit scary for a 5-year-old. |
| Flathead / Channel / Wels catfish (faerbogdan99) | e.g. https://sketchfab.com/3d-models/flathead-catfish-7e931ef527754407af8b3dd845c137da | CC BY 4.0 | 40k each | No | Realistic PBR, 1 material and 4 textures. Fine shape, but the tail is not a separate material. |
| ffishAsia CC0 catfish scans (Amur, Ariake, Gigi, Torrent) | e.g. https://sketchfab.com/3d-models/cc0-ariake-stream-catfis-animation-51e0cc7840c54790bfd674d70dfb6bab | **CC0** | 28k to 2.8M | One has 1 anim (28k version) | Photogrammetry of real specimens. Very realistic and very heavy. Recolouring a photo-scan texture into a redtail is hard. Last-resort CC0 option. |
| CatFish (Kavazaki.Purinsuseta) | https://sketchfab.com/3d-models/catfish-126f2dc5c3b846dc9bddfaa0a5f0300b | CC BY 4.0 | 9,093 | 1 | A fish leaping out of a wine glass. Not usable as-is. |
| Fish - Low Poly Medieval, CC0 (Avramania) | https://sketchfab.com/3d-models/fish-low-poly-medieval-cc0-d681fe491a1e405faec920838ea3f11f | **CC0** | 600 | No | Generic food fish, shared palette texture, no whiskers. |
| Fish redtail catfish - Pirarara (trillobit3s) | https://trillobit3s-indie-games.itch.io/fish | Paid ($15) | ? | Yes (swim AI) | A real redtail but **not free**. Listed for completeness. |
| Cozy Stylized Wels Catfish (liray3d) | https://liray3d.itch.io/cozy-stylized-wels-catfish | Paid ($6.49 on sale) | 82k | 12 clips, 31 bones | Not free, and the page says "AI-assisted". Has a recolour mask, but it is a wels, not a redtail. Skip. |
| freshwater fish (Jint-Hwang, voxel) | https://jint-hwang.itch.io/freshwater-fish | "Please use it freely, both non-commercial and commercial, or you can modify it yourself." (no named license) | voxel OBJ | No | Includes a suckermouth catfish in voxel style. Free-use wording but not a standard license. |

### Sources with no match
- **Kenney:** no 3D fish. Kenney's "Fish Pack" is 2D sprites only (CC0).
- **itch.io free assets tagged "catfish":** 0 results.
- **Poly Pizza:** the site returns HTTP 403 to automated requests, so I could not search it. It mirrors Quaternius's CC0 packs, so the **Cute Fish Pack "Armored Catfish"** should be there as a glTF download. Search "catfish" in a normal browser.
- **Free3D "animated red tail catfish"** (https://free3d.com/3d-model/animated-red-tail-catfish-3734.html): returned 403. Search snippets say "royalty free", FBX/Collada/C4D, animated. Free3D's own license is not CC and needs checking by hand. Not recommended until the terms are read.

---

## B. Tail wag / swim animation (nice-to-have)

| Option | Effort |
|---|---|
| **Cartoon Koi (rank 3)**: has a Swimming clip and a rig, so play it on loop with an Animator. | None |
| **Quaternius Cute Fish / Animated Fish packs (CC0)**: rigged, with swim animations. | None (if a fish shape works) |
| **OGA catfish (rank 2)**: the Swim .max needs 3ds Max. Otherwise rig the OBJ in Blender with 3 to 4 spine bones, then weight-paint automatically (Ctrl+P > With Automatic Weights). | About 30 min |
| **Any static mesh**: in Blender, cut the mesh at the tail base into a separate object with its pivot at the cut. In Unity, rotate that child with a small script: `localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * speed) * 20f, 0)`. Scale `speed` by the vehicle's velocity so it wags faster when driving fast. | About 15 min |

---

## C. Fish-shaped car or boat models (optional)

No **CC0** fish car or fish boat was found. The closest CC BY options:

| Model | URL | License | Polys | Notes |
|---|---|---|---|---|
| **Fishcar** (Ronny_Hellr) | https://sketchfab.com/3d-models/fishcar-0377f5e14bde41a0945592027804ce31 | CC BY 4.0 | 154k | Cartoony orange-goldfish kart with tail fins, sitting on a shell. **7 materials, 0 textures**, so it is easy to recolour into a dark-grey body, white belly and red tail. Heavy: decimate in Blender to about 20k. Best "fish car" option. |
| Road Shark (Soline_Carton) | https://sketchfab.com/3d-models/road-shark-961d0320f62447f68488eecbeedcb83d | CC BY 4.0 | 8.6k | A muscle car with shark fins and a shark tail, 1 animation. Recolour the fins red and the body dark grey for a "catfish car". |
| Quaternius Cute Fish Pack `Boat.fbx` | https://quaternius.com/packs/cutefish.html | CC0 | low | Probably a small fishing boat, not fish-shaped. Not inspected (Drive quota). |

**Avoid:**
- "Hammerhead Shark Car" (Sxi.Sai): tagged `xbox-models`, which suggests a ripped game asset, so its rights are unclear.
- "a colorful fish car..." (klrxyz): tagged `createdwithai`/`meshy`, an AI-generated model.

**Practical recommendation:** you don't need a fish car model. Put the rank-1 catfish over the hidden game car or boat mesh. A giant catfish "driving" on hidden wheels reads as a fish car to a 5-year-old. For the boat, sink the catfish so the waterline sits around the belly band.

---

## Recolouring a low-poly fish into a redtail

Reference colours (cartoon-friendly):

| Region | Colour | Hex (suggestion) |
|---|---|---|
| Back, top of head, dorsal fin | Charcoal / very dark slate | `#2B2D31` |
| Spots on the back and upper sides | Black, small round dots (optional) | `#121214` |
| Flanks below the dark back: **wide belly band** running from behind the gill to just before the tail | Cream / off-white | `#F2EAD8` |
| Lower jaw / throat | Light grey | `#B9B9B4` |
| **Tail (caudal fin)** | **Bright red-orange, the key feature** | `#FF4A1C` |
| Top edge of the dorsal fin, adipose fin, optional anal-fin edge | Red-orange accent | `#E8401C` |
| Pectoral and pelvic fins | Dark grey with orange-red tips | `#3A3A3E` → `#E8401C` |
| Barbels (6 whiskers) | Dark grey / black | `#1E1E1E` |
| Eye | Small, yellow-gold iris, black pupil | `#E8C34A` |

The shape cues that make it read as a redtail:
- a very **broad, flat head**
- the **sharp border between the dark back and the white band**, which should be a crisp line, not a gradient
- the **red tail**

Tips:
- Paint the colours flat and saturated, the way Wobbly Life does.
- If the model uses a texture atlas, paint by UV island: the tail island goes fully red.
- If the model uses flat materials, split the faces so the tail and belly get their own materials.
- In Unity, use the same shader as the game's own props (often a toon or Standard shader with low smoothness) so it doesn't look out of place.

---

## Making it "giant" in Unity without breaking physics

Keep the catfish **visual-only**, as a child of the game object that has the physics:

```
GameBoat / GameCar (root: Rigidbody, colliders, vehicle scripts)  ← don't scale this
 ├─ OriginalMesh  (MeshRenderer disabled, or renderer.enabled = false)
 └─ CatfishVisual (empty GameObject: position/rotation offset, scale here)
     └─ catfish model (MeshRenderer / SkinnedMeshRenderer, NO colliders, NO Rigidbody)
```

Rules:
1. **Never scale the root or anything with a Rigidbody or colliders.** Scale only `CatfishVisual`, for example `localScale = (4, 4, 4)`. Scaling a Rigidbody's transform changes collider sizes, centre of mass and inertia, which breaks vehicle handling and wheel colliders.
2. **Remove every collider from the imported model.** Untick "Generate Colliders" in the import settings and delete any MeshCollider. A mesh collider on a child gets added to the parent Rigidbody's compound collider and will change the physics.
3. **Hide the original mesh by disabling its renderers**, not its GameObject. The GameObject may also hold colliders or wheel visuals the scripts expect. Keep the wheels visible or hide them, whichever looks better.
4. **Set the visual on its own layer** (or one that doesn't collide), so stray colliders or raycasts don't hit it.
5. **Align it by eye:** rotate so the fish faces the vehicle's forward (+Z) and lift it so the belly sits just above the wheels. For the boat, set the waterline at the belly band.
6. **Sandbox prop:** the prop needs a collider to be grabbable or physical. Put a simple **BoxCollider or CapsuleCollider on the prop root** sized to the scaled fish, plus a Rigidbody with mass matching other large props (for example 50 to 200). Don't use a MeshCollider on a skinned or animated mesh.
7. **Performance:** at 11k tris, rank 1 is fine. If you use Fishcar (154k), decimate in Blender first. Set `SkinnedMeshRenderer.updateWhenOffscreen = false` and keep shadow casting on, since it's the star of the show.
8. **Credit:** for CC BY models, put an attribution line in the mod's README or credits file, e.g. *"Red Tail Catifish" by fredy120488 (Sketchfab), CC BY 4.0*. CC0 needs no credit, but crediting is still nice.

---

## Summary of licenses

- **CC0 (no credit needed):**
  - OGA 3d low poly catfish (gfroad)
  - Quaternius Cute Fish and Animated Fish packs
  - ffishAsia scans
  - Avramania fish
- **CC BY 4.0 (credit the author):**
  - Red Tail Catifish (fredy120488)
  - Cartoon Koi (MarkoJantti)
  - Fishcar (Ronny_Hellr)
  - Road Shark (Soline_Carton)
  - VivernaNeva, masterdef and faerbogdan99 catfish
- **Paid, not free:** trillobit3s Pirarara (itch, $15) and liray3d Wels catfish (itch, AI-assisted).
- **Unverified:** Free3D animated red tail catfish (page blocked; site-specific license, not CC).
