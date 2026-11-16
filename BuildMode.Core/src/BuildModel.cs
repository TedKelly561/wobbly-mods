using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>Outcome of a build operation. Shown to the player as a short message or sound.</summary>
    public enum BuildResult
    {
        Ok,
        /// <summary>Another block already fills one of the cells.</summary>
        Overlap,
        /// <summary>Outside the allowed height range.</summary>
        OutOfBounds,
        /// <summary>The build hit <see cref="BuildModel.MaxBlocks"/>.</summary>
        LimitReached,
        /// <summary>No block there.</summary>
        NotFound,
        NothingToUndo,
        NothingToRedo,
        /// <summary>Undo/redo couldn't apply because someone else changed that spot in the meantime.</summary>
        Conflict,
        /// <summary>Bad arguments (missing block type or material).</summary>
        Invalid,
    }

    /// <summary>
    /// Everything needed to (re)create a block: what, where, which way, and how it looks.
    /// Immutable, so undo entries and save files can hold it safely.
    /// </summary>
    public sealed class BlockSpec : IEquatable<BlockSpec>
    {
        public BlockSpec(BlockType type, BuildMaterial material, PaintColor paint, GridPos anchor, Rotation rotation)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (material == null)
            {
                throw new ArgumentNullException("material");
            }
            Type = type;
            Material = material;
            Paint = paint;
            Anchor = anchor;
            Rotation = rotation;
        }

        /// <summary>Uses the material's default colour.</summary>
        public BlockSpec(BlockType type, BuildMaterial material, GridPos anchor, Rotation rotation)
            : this(type, material, material == null ? PaintColor.White : material.DefaultColor, anchor, rotation)
        {
        }

        public BlockType Type { get; private set; }
        public BuildMaterial Material { get; private set; }
        public PaintColor Paint { get; private set; }
        public GridPos Anchor { get; private set; }
        public Rotation Rotation { get; private set; }

        public BlockSpec WithLook(BuildMaterial material, PaintColor paint)
        {
            return new BlockSpec(Type, material, paint, Anchor, Rotation);
        }

        public BlockSpec MovedBy(GridPos offset)
        {
            return new BlockSpec(Type, Material, Paint, Anchor + offset, Rotation);
        }

        public bool Equals(BlockSpec other)
        {
            return other != null
                && ReferenceEquals(Type, other.Type)
                && ReferenceEquals(Material, other.Material)
                && Paint == other.Paint
                && Anchor == other.Anchor
                && Rotation == other.Rotation;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BlockSpec);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.Id.GetHashCode() * 397) ^ Anchor.GetHashCode() ^ (Rotation.QuarterTurns << 28);
            }
        }

        public override string ToString()
        {
            return Type.Id + "/" + Material.Id + " at " + Anchor + " " + Rotation;
        }
    }

    /// <summary>A block in the build. Id is unique for the model's lifetime and never reused.</summary>
    public sealed class PlacedBlock
    {
        internal PlacedBlock(int id, BlockSpec spec)
        {
            Id = id;
            Spec = spec;
        }

        public int Id { get; private set; }

        /// <summary>Current look and position. Replaced (not mutated) when the block is repainted.</summary>
        public BlockSpec Spec { get; internal set; }

        /// <summary>The chunk whose mesh draws this block (the anchor cell's chunk).</summary>
        public ChunkPos Chunk
        {
            get { return ChunkPos.FromCell(Spec.Anchor); }
        }

        public override string ToString()
        {
            return "#" + Id + " " + Spec;
        }
    }

    /// <summary>
    /// The whole build: which block fills which cell.
    ///
    /// Storage is sparse, so big builds cost memory per block, not per volume:
    ///  - a cell -> block id dictionary per <see cref="OccupancyLayer"/> for O(1) overlap checks,
    ///  - an id -> block dictionary,
    ///  - a chunk -> block ids index so the renderer can rebuild one 16^3 chunk at a time.
    /// Every change marks the anchor chunk dirty; the renderer pulls a few dirty chunks per frame
    /// with <see cref="TakeDirtyChunks"/> and re-merges only those meshes.
    ///
    /// Not thread-safe: use it from Unity's main thread.
    /// </summary>
    public sealed class BuildModel
    {
        public const int DefaultMaxBlocks = 200000;

        private readonly Dictionary<GridPos, int>[] _occupancy =
        {
            new Dictionary<GridPos, int>(),
            new Dictionary<GridPos, int>(),
        };
        private readonly Dictionary<int, PlacedBlock> _blocks = new Dictionary<int, PlacedBlock>();
        private readonly Dictionary<ChunkPos, HashSet<int>> _chunks = new Dictionary<ChunkPos, HashSet<int>>();
        private readonly HashSet<ChunkPos> _dirty = new HashSet<ChunkPos>();
        private readonly List<GridPos> _scratchCells = new List<GridPos>(16);
        private int _nextId = 1;

        public BuildModel(BuildCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }
            Catalog = catalog;
            MaxBlocks = DefaultMaxBlocks;
            MinY = -64;
            MaxY = 255;
        }

        public BuildCatalog Catalog { get; private set; }

        /// <summary>Safety cap on block count so a runaway fill can't freeze the game.</summary>
        public int MaxBlocks { get; set; }

        /// <summary>Lowest allowed cell Y (inclusive).</summary>
        public int MinY { get; set; }

        /// <summary>Highest allowed cell Y (inclusive).</summary>
        public int MaxY { get; set; }

        public int Count
        {
            get { return _blocks.Count; }
        }

        /// <summary>All blocks, in no particular order.</summary>
        public IEnumerable<PlacedBlock> Blocks
        {
            get { return _blocks.Values; }
        }

        /// <summary>All blocks sorted by id (oldest first). Used for deterministic saving.</summary>
        public List<PlacedBlock> BlocksInPlacementOrder()
        {
            var list = new List<PlacedBlock>(_blocks.Values);
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            return list;
        }

        // ---------- Queries ----------

        public PlacedBlock GetBlock(int id)
        {
            PlacedBlock block;
            return _blocks.TryGetValue(id, out block) ? block : null;
        }

        /// <summary>The block filling a cell in one layer, or null.</summary>
        public PlacedBlock GetBlockAt(GridPos cell, OccupancyLayer layer)
        {
            int id;
            return _occupancy[(int)layer].TryGetValue(cell, out id) ? _blocks[id] : null;
        }

        /// <summary>The block the player most likely means at a cell: Body first, then Floor. Null if empty.</summary>
        public PlacedBlock GetBlockAt(GridPos cell)
        {
            return GetBlockAt(cell, OccupancyLayer.Body) ?? GetBlockAt(cell, OccupancyLayer.Floor);
        }

        public bool IsOccupied(GridPos cell, OccupancyLayer layer)
        {
            return _occupancy[(int)layer].ContainsKey(cell);
        }

        /// <summary>
        /// Checks whether a block could go here, without changing anything. Use every frame for the
        /// ghost preview colour. <paramref name="blockingId"/> is the first block in the way (0 if none).
        /// </summary>
        public BuildResult CanPlace(BlockType type, GridPos anchor, Rotation rotation, out int blockingId)
        {
            blockingId = 0;
            if (type == null)
            {
                return BuildResult.Invalid;
            }
            if (_blocks.Count >= MaxBlocks)
            {
                return BuildResult.LimitReached;
            }
            _scratchCells.Clear();
            Footprint.GetCells(type, anchor, rotation, _scratchCells);
            Dictionary<GridPos, int> layer = _occupancy[(int)type.Layer];
            for (int i = 0; i < _scratchCells.Count; i++)
            {
                GridPos cell = _scratchCells[i];
                if (cell.Y < MinY || cell.Y > MaxY)
                {
                    return BuildResult.OutOfBounds;
                }
                int other;
                if (layer.TryGetValue(cell, out other))
                {
                    blockingId = other;
                    return BuildResult.Overlap;
                }
            }
            return BuildResult.Ok;
        }

        public BuildResult CanPlace(BlockType type, GridPos anchor, Rotation rotation)
        {
            int ignored;
            return CanPlace(type, anchor, rotation, out ignored);
        }

        // ---------- Changes ----------

        /// <summary>Places a block if all its cells are free. Overlaps are rejected, never merged.</summary>
        public BuildResult TryPlace(BlockSpec spec, out PlacedBlock placed)
        {
            placed = null;
            if (spec == null)
            {
                return BuildResult.Invalid;
            }
            BuildResult check = CanPlace(spec.Type, spec.Anchor, spec.Rotation);
            if (check != BuildResult.Ok)
            {
                return check;
            }
            placed = Insert(_nextId++, spec);
            return BuildResult.Ok;
        }

        public BuildResult TryPlace(BlockSpec spec)
        {
            PlacedBlock ignored;
            return TryPlace(spec, out ignored);
        }

        /// <summary>
        /// Puts a removed block back with its old id (used by undo/redo so later history entries
        /// that refer to the id keep working). Fails with Overlap if the spot was taken meanwhile.
        /// </summary>
        internal BuildResult TryRestore(int id, BlockSpec spec, out PlacedBlock placed)
        {
            placed = null;
            if (_blocks.ContainsKey(id))
            {
                return BuildResult.Conflict;
            }
            BuildResult check = CanPlace(spec.Type, spec.Anchor, spec.Rotation);
            if (check != BuildResult.Ok)
            {
                return check;
            }
            if (id >= _nextId)
            {
                _nextId = id + 1;
            }
            placed = Insert(id, spec);
            return BuildResult.Ok;
        }

        /// <summary>Removes a block by id.</summary>
        public bool Remove(int id, out PlacedBlock removed)
        {
            if (!_blocks.TryGetValue(id, out removed))
            {
                return false;
            }
            _blocks.Remove(id);

            _scratchCells.Clear();
            BlockSpec spec = removed.Spec;
            Footprint.GetCells(spec.Type, spec.Anchor, spec.Rotation, _scratchCells);
            Dictionary<GridPos, int> layer = _occupancy[(int)spec.Type.Layer];
            for (int i = 0; i < _scratchCells.Count; i++)
            {
                layer.Remove(_scratchCells[i]);
            }

            ChunkPos chunk = removed.Chunk;
            HashSet<int> ids;
            if (_chunks.TryGetValue(chunk, out ids))
            {
                ids.Remove(id);
                if (ids.Count == 0)
                {
                    _chunks.Remove(chunk);
                }
            }
            _dirty.Add(chunk);
            return true;
        }

        public bool Remove(int id)
        {
            PlacedBlock ignored;
            return Remove(id, out ignored);
        }

        /// <summary>Changes material and paint colour. Position and shape stay the same.</summary>
        public BuildResult SetLook(int id, BuildMaterial material, PaintColor paint)
        {
            if (material == null)
            {
                return BuildResult.Invalid;
            }
            PlacedBlock block;
            if (!_blocks.TryGetValue(id, out block))
            {
                return BuildResult.NotFound;
            }
            block.Spec = block.Spec.WithLook(material, paint);
            _dirty.Add(block.Chunk);
            return BuildResult.Ok;
        }

        /// <summary>Removes everything. Every chunk that had blocks becomes dirty so the renderer clears it.</summary>
        public void Clear()
        {
            foreach (ChunkPos chunk in _chunks.Keys)
            {
                _dirty.Add(chunk);
            }
            _chunks.Clear();
            _blocks.Clear();
            _occupancy[0].Clear();
            _occupancy[1].Clear();
        }

        private PlacedBlock Insert(int id, BlockSpec spec)
        {
            var block = new PlacedBlock(id, spec);
            _blocks.Add(id, block);

            _scratchCells.Clear();
            Footprint.GetCells(spec.Type, spec.Anchor, spec.Rotation, _scratchCells);
            Dictionary<GridPos, int> layer = _occupancy[(int)spec.Type.Layer];
            for (int i = 0; i < _scratchCells.Count; i++)
            {
                layer.Add(_scratchCells[i], id);
            }

            ChunkPos chunk = block.Chunk;
            HashSet<int> ids;
            if (!_chunks.TryGetValue(chunk, out ids))
            {
                ids = new HashSet<int>();
                _chunks.Add(chunk, ids);
            }
            ids.Add(id);
            _dirty.Add(chunk);
            return block;
        }

        // ---------- Chunks and dirty tracking ----------

        /// <summary>Chunks that currently hold at least one block.</summary>
        public IEnumerable<ChunkPos> NonEmptyChunks
        {
            get { return _chunks.Keys; }
        }

        /// <summary>Adds the blocks drawn by one chunk to <paramref name="into"/> (not cleared first).</summary>
        public void GetBlocksInChunk(ChunkPos chunk, List<PlacedBlock> into)
        {
            HashSet<int> ids;
            if (!_chunks.TryGetValue(chunk, out ids))
            {
                return;
            }
            foreach (int id in ids)
            {
                into.Add(_blocks[id]);
            }
        }

        public int CountInChunk(ChunkPos chunk)
        {
            HashSet<int> ids;
            return _chunks.TryGetValue(chunk, out ids) ? ids.Count : 0;
        }

        /// <summary>How many chunk meshes are out of date.</summary>
        public int DirtyChunkCount
        {
            get { return _dirty.Count; }
        }

        public bool IsDirty(ChunkPos chunk)
        {
            return _dirty.Contains(chunk);
        }

        /// <summary>Snapshot of the dirty chunks (does not clear them).</summary>
        public List<ChunkPos> GetDirtyChunks()
        {
            return new List<ChunkPos>(_dirty);
        }

        /// <summary>
        /// Moves up to <paramref name="max"/> dirty chunks into <paramref name="into"/> and marks them clean.
        /// Call once per frame with a small max (1 to 4) to spread mesh rebuilding over frames.
        /// Returns how many were taken. A taken chunk may now be empty: destroy its mesh then.
        /// </summary>
        public int TakeDirtyChunks(List<ChunkPos> into, int max)
        {
            int taken = 0;
            if (max <= 0 || _dirty.Count == 0)
            {
                return 0;
            }
            if (max >= _dirty.Count)
            {
                into.AddRange(_dirty);
                taken = _dirty.Count;
                _dirty.Clear();
                return taken;
            }
            var picked = new List<ChunkPos>(max);
            foreach (ChunkPos chunk in _dirty)
            {
                picked.Add(chunk);
                if (picked.Count == max)
                {
                    break;
                }
            }
            for (int i = 0; i < picked.Count; i++)
            {
                _dirty.Remove(picked[i]);
            }
            into.AddRange(picked);
            return picked.Count;
        }

        /// <summary>
        /// Like <see cref="TakeDirtyChunks(List{ChunkPos}, int)"/> but nearest to a cell first
        /// (pass the player's or camera's cell), so what players are looking at updates first.
        /// </summary>
        public int TakeDirtyChunks(List<ChunkPos> into, int max, GridPos nearCell)
        {
            if (max <= 0 || _dirty.Count == 0)
            {
                return 0;
            }
            var all = new List<ChunkPos>(_dirty);
            ChunkPos near = ChunkPos.FromCell(nearCell);
            all.Sort((a, b) => DistanceSq(a, near).CompareTo(DistanceSq(b, near)));
            int count = Math.Min(max, all.Count);
            for (int i = 0; i < count; i++)
            {
                _dirty.Remove(all[i]);
                into.Add(all[i]);
            }
            return count;
        }

        /// <summary>Marks every non-empty chunk dirty (e.g. after the renderer lost its meshes on scene load).</summary>
        public void MarkAllDirty()
        {
            foreach (ChunkPos chunk in _chunks.Keys)
            {
                _dirty.Add(chunk);
            }
        }

        public void ClearDirty()
        {
            _dirty.Clear();
        }

        private static long DistanceSq(ChunkPos a, ChunkPos b)
        {
            long dx = a.X - b.X;
            long dy = a.Y - b.Y;
            long dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
