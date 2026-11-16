# BuildMode.Core

The game-independent heart of the sandbox build mode: snapping blocks, materials, a chunked build
model that stays fast for big builds, per-player undo/redo for split screen, and save/load.

It has **no Unity references** (netstandard2.0, C# 7.3), so it loads in BepInEx 5 / Unity Mono and
is fully unit-tested with plain `dotnet test` (see `../BuildMode.Core.Tests`). The Unity layer
(first version) lives in the house menu: `../HouseMenu/src/Build/` turns it into chunk meshes,
colliders, a ghost preview per player and the menu's Build tab.

## Design

| Piece | File | What it does |
|---|---|---|
| `GridPos`, `Vec3`, `Rotation`, `ChunkPos` | `src/GridMath.cs` | Small math types. `Rotation` is 0-3 quarter turns around Y, clockwise from above, the same as Unity's `Quaternion.Euler(0, deg, 0)`. |
| `GridSettings` | `src/GridSettings.cs` | World position to cell (and back). Cell size 1 m, or 0.5 m with `GridSettings.HalfStep()`. Has a world `Origin`, so a build can sit anywhere. `CellAgainstSurface` picks the cell to build into from a raycast hit. |
| `BlockType`, `BuildMaterial`, `PaintColor`, `BuildCatalog` | `src/Catalog.cs` | Blocks (floor, wall, half wall, roof slope, flat roof, stairs, door, window, pillar, beam) with a footprint in cells, materials (wood, brick, stone, glass, concrete, roof tiles, plaster) and RGBA paint. Ids are stable lowercase strings (`BuildCatalog.Ids`), used in save files. Never rename one. |
| `Footprint` | `src/Footprint.cs` | Which cells a block covers for a given anchor and rotation. |
| `BuildModel`, `BlockSpec`, `PlacedBlock` | `src/BuildModel.cs` | The build. Place, remove, repaint, query; overlaps are rejected. Chunk index and dirty-chunk tracking. |
| `BuildEditor`, `BuildHistory` | `src/BuildHistory.cs` | The front door for edits. Records every change in the acting **player's own** undo/redo history. Fill a box as one undo step. |
| `PlayerBuildState` | `src/PlayerBuildState.cs` | One player's selection (block, material, paint, rotation, cursor) with controller-friendly "cycle"/"rotate"/"move" calls. Takes an optional paint palette (favourite colours first). |
| `BuildSerializer`, `LoadResult` | `src/BuildSerializer.cs` | Versioned, human-readable text save format. Exact round trip. Unknown ids are skipped with warnings. |

### Cells, layers and footprints

- A block has an **anchor cell** (its local cell 0,0,0). Its footprint runs `Width` along local X,
  `Height` up and `Depth` along local Z, then turns around the anchor by its rotation. The anchor
  never moves, so rotating the ghost keeps it under the cursor.
- Each cell has two **layers**: `Floor` (a thin slab on the cell's bottom face: floors and flat
  roofs) and `Body` (everything else). A wall can stand on a floor tile in the same cell; two
  bodies or two floors in one cell are an overlap.
- Default footprints: door 1x2x1, stairs 1x1x2 (one up over two deep), beam 3x1x1, the rest 1x1x1.
  Footprints count cells, so at 0.5 m cells everything is half size; the Unity layer scales meshes
  by `CellSize`.

### Big builds without lag

- Storage is sparse: a `Dictionary<GridPos, int>` per layer (O(1) overlap checks), an id-to-block
  dictionary, and a chunk-to-ids index. Memory grows with blocks, not with the build's volume.
  The tests place 100,000 blocks and save/load 25,000.
- The world is split into **16 x 16 x 16-cell chunks**. A block belongs to the chunk of its anchor
  cell (even when a beam pokes into the next chunk). Every place, remove, repaint, undo and redo
  marks that chunk **dirty**.
- The renderer rebuilds **one merged mesh per dirty chunk**, a few per frame, never the whole build.
- `MaxBlocks` (default 200,000) and `MinY`/`MaxY` stop a runaway fill from freezing the game.

### Per-player undo (split screen)

`BuildEditor.Place(player, ...)`, `RemoveAt`, `PaintAt`, `Fill`, `PlaceMany`, `Undo(player)`,
`Redo(player)`, and `RemoveAll(player, ...)` (clear the build as one undoable step). Each player number gets its own `BuildHistory` (200 steps by default). Actions refer
to blocks by id, and ids are never reused, so one player's undo never removes another player's
block. If someone else changed the spot in the meantime (removed the block, or built where it
used to be), undo returns `Conflict` ("Someone else changed that") and drops that step instead of
breaking anything. `BuildEditor.MessageFor` gives short, kid-friendly messages.

### Save format

```
wobblybuild 1
# Wobbly Mods build mode save. Edit with care: one block per line.
name Skylar's castle
cell 1 0 0 0
block floor wood 0 0 0 0 #FFFFFFFF
block door wood 0 0 -3 2 #FF8FABFF
end
```

- `cell <size> <originX> <originY> <originZ>`; `block <type> <material> <x> <y> <z> <quarterTurns> <#RRGGBBAA>`.
- Invariant culture, round-trip float formatting, `\n` line endings, blocks in placement order:
  save, load, save gives identical text.
- Loading never throws on bad content. Unknown block/material ids, bad lines, overlaps and unknown
  keywords are skipped and listed in `LoadResult.Warnings` (one warning per unknown id). Only a
  missing header or a **newer** format version fails (`LoadResult.Error`).
- Extra fields at the end of a `block` line are ignored, so a future version can add data
  without breaking old readers. Bump `FormatVersion` only for changes old readers can't skip.

## How the Unity layer should use it

1. **Setup.** One `BuildCatalog.CreateDefault()`, one `BuildModel`, one `BuildEditor`, one
   `GridSettings` (origin = where the build lives in the world). One `PlayerBuildState` per
   split-screen player.
2. **Mapping.** Map each `BlockType.Id` to a mesh and each `BuildMaterial.Id` to a Unity
   `Material`/texture. Author meshes with the pivot at the bottom centre of the anchor cell,
   extending toward +X (width), +Y and +Z (depth), at 1 unit per cell; scale by `CellSize`.
   Place with `position = GridSettings.BlockPivot(spec.Anchor)` and
   `rotation = Quaternion.Euler(0, spec.Rotation.Degrees, 0)`. Convert with tiny helpers
   (`new Vector3(v.X, v.Y, v.Z)`).
3. **Aiming.** Raycast from **that player's camera** (not `Camera.main`). Build cell:
   `CellAgainstSurface(hit.point, hit.normal)`; remove/paint cell: `CellBehindSurface(...)`.
   With no hit, the D-pad moves the cursor with `PlayerBuildState.MoveCursor(..., cameraYaw)`.
4. **Ghost preview.** Each frame, draw the selected mesh at the cursor, tinted green or red by
   `model.CanPlace(type, cursor, rotation)`. This allocates nothing.
5. **Buttons.** A = `editor.Place(player, state.CurrentSpec())`, X = `RemoveAt`, Y = `PaintAt`,
   bumpers = `CycleBlock`/`CycleMaterial`, triggers = `RotateClockwise`, Back = `Undo(player)`.
   Show `EditResult.Message` on that player's half of the screen.
6. **Chunk meshes (the important part for performance).** Keep one GameObject per `ChunkPos`
   with an opaque mesh and a separate transparent mesh (for `BuildMaterial.IsTransparent`, i.e.
   glass). Every frame (or every few frames):
   ```csharp
   dirty.Clear();
   model.TakeDirtyChunks(dirty, 2, nearestPlayerCell); // 1-4 per frame spreads the cost
   foreach (var c in dirty) RebuildChunk(c);           // empty chunk -> destroy its GameObject
   ```
   `RebuildChunk` gets the blocks with `GetBlocksInChunk`, and merges their meshes with
   `Mesh.CombineMeshes` (set `indexFormat = UInt32` for big chunks), grouping by material into
   submeshes so a chunk costs a handful of draw calls. Paint goes into **vertex colours**, so one
   material per texture serves every paint colour, with no per-block material instances.
   Give the chunk a `MeshCollider` from the same merged mesh. Never create one GameObject per block.
   After loading a build or a scene change, call `model.MarkAllDirty()`.
7. **Save/load.** `BuildSerializer.Write(model, grid, name)` to a file under
   `BepInEx/config/WobblyMods/builds/` (or similar); `Read(text, catalog)` to load, then
   `editor.ClearAllHistory()` and `MarkAllDirty()`. Show a short summary of `Warnings` if any.
   To paste a saved build where a player stands, `ReadBlocks`, then
   `editor.PlaceMany(player, blocks.Select(b => b.MovedBy(offset)), "Load ...")` (one undo step).
8. **Threading.** The model is not thread-safe. Use it on Unity's main thread. Mesh building can
   be split across frames using the dirty-chunk budget above.

### Still to do in the Unity layer

- Meshes and textures for every block/material id (door and window need openings; walls are full cells).
- Neighbour face culling inside a chunk (optional; the model's `IsOccupied` gives what's needed).
  If added, a change on a chunk edge should also rebuild the neighbouring chunk.
- Where builds live in the world (`GridSettings.Origin`), how they interact with the game's own
  physics and players, and the per-player controller input (depends on T5/T14 in PROJECT_NOTES.md).
