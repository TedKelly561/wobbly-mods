using System;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// Converts between world positions and grid cells.
    ///
    /// Cell (0,0,0) spans world Origin to Origin + CellSize on every axis.
    /// CellSize is 1 m by default; use <see cref="HalfStep"/> (0.5 m) for finer building.
    /// Block footprints are counted in cells, so a 1 x 2 x 1 door is 1 m x 2 m at 1 m cells
    /// and 0.5 m x 1 m at half-step cells (the Unity layer scales the meshes by CellSize).
    /// </summary>
    public sealed class GridSettings
    {
        public const float DefaultCellSize = 1f;
        public const float HalfStepCellSize = 0.5f;

        public GridSettings(float cellSize, Vec3 origin)
        {
            if (!(cellSize > 0f) || float.IsInfinity(cellSize))
            {
                throw new ArgumentOutOfRangeException("cellSize", "Cell size must be a positive number.");
            }
            CellSize = cellSize;
            Origin = origin;
        }

        public GridSettings(float cellSize) : this(cellSize, Vec3.Zero)
        {
        }

        public GridSettings() : this(DefaultCellSize, Vec3.Zero)
        {
        }

        /// <summary>A 0.5 m grid, for detailed builds.</summary>
        public static GridSettings HalfStep()
        {
            return new GridSettings(HalfStepCellSize);
        }

        /// <summary>Edge length of one cell in metres.</summary>
        public float CellSize { get; private set; }

        /// <summary>World position of the corner of cell (0,0,0). Lets a build sit anywhere in the world.</summary>
        public Vec3 Origin { get; private set; }

        /// <summary>The cell containing a world position (floors, so negative positions work).</summary>
        public GridPos WorldToCell(Vec3 world)
        {
            return new GridPos(
                FloorToCell(world.X - Origin.X),
                FloorToCell(world.Y - Origin.Y),
                FloorToCell(world.Z - Origin.Z));
        }

        /// <summary>World position of a cell's lowest corner.</summary>
        public Vec3 CellMinCorner(GridPos cell)
        {
            return new Vec3(
                Origin.X + cell.X * CellSize,
                Origin.Y + cell.Y * CellSize,
                Origin.Z + cell.Z * CellSize);
        }

        /// <summary>World position of a cell's centre.</summary>
        public Vec3 CellCenter(GridPos cell)
        {
            float h = CellSize * 0.5f;
            Vec3 c = CellMinCorner(cell);
            return new Vec3(c.X + h, c.Y + h, c.Z + h);
        }

        /// <summary>
        /// Where to put a block's transform: the centre of its anchor cell at floor level.
        /// Rotating a mesh around this point by <see cref="Rotation.Degrees"/> lines it up with
        /// <see cref="Footprint"/>. See README.md for how meshes should be authored.
        /// </summary>
        public Vec3 BlockPivot(GridPos anchor)
        {
            float h = CellSize * 0.5f;
            Vec3 c = CellMinCorner(anchor);
            return new Vec3(c.X + h, c.Y, c.Z + h);
        }

        /// <summary>Snaps a world position to the nearest cell centre.</summary>
        public Vec3 SnapToCellCenter(Vec3 world)
        {
            return CellCenter(WorldToCell(world));
        }

        /// <summary>
        /// The cell to build into when aiming at a surface: steps half a cell out along the
        /// surface normal from the hit point, so aiming at the top of a floor picks the cell
        /// above it, and aiming at a wall's side picks the cell beside it.
        /// </summary>
        public GridPos CellAgainstSurface(Vec3 hitPoint, Vec3 hitNormal)
        {
            return WorldToCell(hitPoint + hitNormal * (CellSize * 0.5f));
        }

        /// <summary>
        /// The cell half a cell behind a hit point. Right for full blocks, but NOT for thin pieces: a floor's top
        /// is near the bottom of its cell, so half a cell behind it is the cell below. To find the piece a player
        /// is looking at, use <see cref="BlockPickMap{TKey}"/> instead.
        /// </summary>
        public GridPos CellBehindSurface(Vec3 hitPoint, Vec3 hitNormal)
        {
            return WorldToCell(hitPoint - hitNormal * (CellSize * 0.5f));
        }

        private int FloorToCell(float offset)
        {
            // A tiny epsilon stops 2.9999999 (float noise on a cell boundary) landing one cell low.
            double cells = offset / (double)CellSize;
            return (int)Math.Floor(cells + 1e-5);
        }
    }
}
