# Model packs for the house menu

Put Wavefront `.obj` files (and their `.mtl` colour files, same name or named by `mtllib`) in `models/furniture/`.
The updater (or a build with `-p:DeployToGame=true`) copies the `.obj` and `.mtl` files to `BepInEx\plugins\WobblyMods\models\`, and every `.obj` becomes a menu item
automatically: the name gives the label ("kitchenFridge" becomes "kitchen fridge") and the room page
(Kitchen, Bathroom, Bedroom, Living room, Kids' room, Garden, or More).

Only add models whose licence allows it, and record the pack, author and licence below.

| Pack | Author | Licence | Source | Added |
|---|---|---|---|---|
| Furniture Kit 2.0 (140 models, OBJ + MTL only; no textures needed) | Kenney (www.kenney.nl) | CC0 1.0 (Creative Commons Zero) | https://kenney.nl/assets/furniture-kit (kenney_furniture-kit.zip, SHA-256 e67652d0932cee41683f74711c03d3e192a2af9979ef8e6b237711f5482d46b0) | 2026-11-20 |
