using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// Which cells a block covers once placed.
    ///
    /// A block's anchor cell is its local cell (0,0,0). Local cells run 0..Width-1 along X,
    /// 0..Height-1 up, 0..Depth-1 along Z, and are then rotated around the anchor cell by the
    /// block's <see cref="Rotation"/>. The anchor always stays put, so a player rotating the
    /// ghost preview keeps it under their cursor.
    /// </summary>
    public static class Footprint
    {
        /// <summary>Adds every occupied world cell to <paramref name="into"/> (not cleared first). No allocations.</summary>
        public static void GetCells(BlockType type, GridPos anchor, Rotation rotation, List<GridPos> into)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            GetCells(type.Size, anchor, rotation, into);
        }

        /// <summary>Same as above for a raw size.</summary>
        public static void GetCells(GridPos size, GridPos anchor, Rotation rotation, List<GridPos> into)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    for (int x = 0; x < size.X; x++)
                    {
                        into.Add(anchor + rotation.Apply(new GridPos(x, y, z)));
                    }
                }
            }
        }

        /// <summary>Convenience version that returns a new list.</summary>
        public static List<GridPos> GetCells(BlockType type, GridPos anchor, Rotation rotation)
        {
            var list = new List<GridPos>(type.CellCount);
            GetCells(type, anchor, rotation, list);
            return list;
        }

        /// <summary>Inclusive min/max cells of the rotated footprint (for ghost boxes and chunk overlap).</summary>
        public static void GetBounds(BlockType type, GridPos anchor, Rotation rotation, out GridPos min, out GridPos max)
        {
            GridPos far = rotation.Apply(new GridPos(type.Size.X - 1, type.Size.Y - 1, type.Size.Z - 1));
            GridPos a = anchor;
            GridPos b = anchor + far;
            min = new GridPos(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            max = new GridPos(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }
    }
}
