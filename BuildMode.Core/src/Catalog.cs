using System;
using System.Collections.Generic;
using System.Globalization;

namespace WobblyMods.BuildMode
{
    /// <summary>What a block is, for menus and for choosing a mesh shape.</summary>
    public enum BlockKind
    {
        Floor,
        Wall,
        HalfWall,
        Roof,
        Stairs,
        Door,
        Window,
        Pillar,
        Beam,
        /// <summary>An L-shaped wall piece that joins two walls at a room corner without a gap.</summary>
        Corner,
    }

    /// <summary>
    /// Which part of a cell a block fills. Two blocks may share a cell if they use different layers.
    /// Floor: a thin slab on the bottom face of the cell (floors, flat roofs), so a wall, stairs or
    /// a player can stand in the same cell on top of it.
    /// Body: the rest of the cell (walls, doors, stairs, pillars and so on).
    /// </summary>
    public enum OccupancyLayer
    {
        Floor = 0,
        Body = 1,
    }

    /// <summary>
    /// One kind of block. Footprint is in cells before rotation: Width along local X,
    /// Height along Y, Depth along local Z. Ids are saved in build files, so never rename one.
    /// </summary>
    public sealed class BlockType
    {
        public BlockType(string id, string displayName, BlockKind kind, OccupancyLayer layer, int width, int height, int depth)
        {
            BuildCatalog.CheckId(id);
            if (width < 1 || height < 1 || depth < 1)
            {
                throw new ArgumentOutOfRangeException("width", "Block footprint must be at least 1 x 1 x 1.");
            }
            Id = id;
            DisplayName = displayName ?? id;
            Kind = kind;
            Layer = layer;
            Size = new GridPos(width, height, depth);
        }

        /// <summary>Stable save id, lowercase, e.g. "wall".</summary>
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public BlockKind Kind { get; private set; }
        public OccupancyLayer Layer { get; private set; }

        /// <summary>Footprint in cells (X = width, Y = height, Z = depth), before rotation.</summary>
        public GridPos Size { get; private set; }

        public int CellCount
        {
            get { return Size.X * Size.Y * Size.Z; }
        }

        public override string ToString()
        {
            return Id;
        }
    }

    /// <summary>
    /// A surface material. The Unity layer maps Id to a real Material/texture.
    /// Ids are saved in build files, so never rename one.
    /// </summary>
    public sealed class BuildMaterial
    {
        public BuildMaterial(string id, string displayName, PaintColor defaultColor, bool isTransparent)
        {
            BuildCatalog.CheckId(id);
            Id = id;
            DisplayName = displayName ?? id;
            DefaultColor = defaultColor;
            IsTransparent = isTransparent;
        }

        public string Id { get; private set; }
        public string DisplayName { get; private set; }

        /// <summary>The tint used when the player hasn't picked a paint colour. White means "texture as is".</summary>
        public PaintColor DefaultColor { get; private set; }

        /// <summary>True for glass: the renderer puts these in a separate transparent mesh per chunk.</summary>
        public bool IsTransparent { get; private set; }

        public override string ToString()
        {
            return Id;
        }
    }

    /// <summary>An RGBA paint colour in bytes. Saved as "#RRGGBBAA".</summary>
    public struct PaintColor : IEquatable<PaintColor>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public PaintColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public PaintColor(byte r, byte g, byte b) : this(r, g, b, 255)
        {
        }

        public static readonly PaintColor White = new PaintColor(255, 255, 255);

        /// <summary>"#RRGGBBAA", uppercase.</summary>
        public string ToHex()
        {
            return "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2") + A.ToString("X2");
        }

        /// <summary>Parses "#RRGGBB" or "#RRGGBBAA" (the # is optional). Returns false on bad input.</summary>
        public static bool TryParseHex(string text, out PaintColor color)
        {
            color = White;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            string s = text[0] == '#' ? text.Substring(1) : text;
            if (s.Length != 6 && s.Length != 8)
            {
                return false;
            }
            byte r, g, b, a = 255;
            if (!TryByte(s, 0, out r) || !TryByte(s, 2, out g) || !TryByte(s, 4, out b))
            {
                return false;
            }
            if (s.Length == 8 && !TryByte(s, 6, out a))
            {
                return false;
            }
            color = new PaintColor(r, g, b, a);
            return true;
        }

        private static bool TryByte(string s, int start, out byte value)
        {
            return byte.TryParse(s.Substring(start, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Kid-friendly paint palette for the controller menu (cycled with a button).</summary>
        public static readonly PaintColor[] Palette =
        {
            White,
            new PaintColor(230, 57, 70),    // red
            new PaintColor(244, 162, 97),   // orange
            new PaintColor(255, 214, 10),   // yellow
            new PaintColor(82, 183, 136),   // green
            new PaintColor(69, 123, 157),   // blue
            new PaintColor(155, 93, 229),   // purple
            new PaintColor(255, 143, 171),  // pink
            new PaintColor(139, 94, 60),    // brown
            new PaintColor(120, 120, 120),  // grey
            new PaintColor(30, 30, 30),     // black
        };

        public static bool operator ==(PaintColor a, PaintColor b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }

        public static bool operator !=(PaintColor a, PaintColor b)
        {
            return !(a == b);
        }

        public bool Equals(PaintColor other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is PaintColor && this == (PaintColor)obj;
        }

        public override int GetHashCode()
        {
            return (R << 24) | (G << 16) | (B << 8) | A;
        }

        public override string ToString()
        {
            return ToHex();
        }
    }

    /// <summary>
    /// The blocks and materials a build can use, looked up by stable string id.
    /// <see cref="CreateDefault"/> gives the standard set; mods can Add more.
    /// </summary>
    public sealed class BuildCatalog
    {
        private readonly Dictionary<string, BlockType> _blocksById = new Dictionary<string, BlockType>(StringComparer.Ordinal);
        private readonly Dictionary<string, BuildMaterial> _materialsById = new Dictionary<string, BuildMaterial>(StringComparer.Ordinal);
        private readonly List<BlockType> _blocks = new List<BlockType>();
        private readonly List<BuildMaterial> _materials = new List<BuildMaterial>();

        /// <summary>Blocks in menu order.</summary>
        public IList<BlockType> Blocks
        {
            get { return _blocks.AsReadOnly(); }
        }

        /// <summary>Materials in menu order.</summary>
        public IList<BuildMaterial> Materials
        {
            get { return _materials.AsReadOnly(); }
        }

        public void Add(BlockType block)
        {
            if (block == null)
            {
                throw new ArgumentNullException("block");
            }
            if (_blocksById.ContainsKey(block.Id))
            {
                throw new ArgumentException("Duplicate block id: " + block.Id);
            }
            _blocksById.Add(block.Id, block);
            _blocks.Add(block);
        }

        public void Add(BuildMaterial material)
        {
            if (material == null)
            {
                throw new ArgumentNullException("material");
            }
            if (_materialsById.ContainsKey(material.Id))
            {
                throw new ArgumentException("Duplicate material id: " + material.Id);
            }
            _materialsById.Add(material.Id, material);
            _materials.Add(material);
        }

        public BlockType GetBlock(string id)
        {
            BlockType block;
            return id != null && _blocksById.TryGetValue(id, out block) ? block : null;
        }

        public BuildMaterial GetMaterial(string id)
        {
            BuildMaterial material;
            return id != null && _materialsById.TryGetValue(id, out material) ? material : null;
        }

        /// <summary>Stable ids of the default set. Use these constants instead of string literals.</summary>
        public static class Ids
        {
            public const string Floor = "floor";
            public const string Wall = "wall";
            public const string HalfWall = "half_wall";
            public const string RoofSlope = "roof_slope";
            public const string RoofFlat = "roof_flat";
            public const string Stairs = "stairs";
            public const string Door = "door";
            public const string Window = "window";
            public const string Pillar = "pillar";
            public const string Beam = "beam";
            public const string CornerWall = "corner_wall";
            // Added 2026-11-22 (parent: "more pieces", "certain things not fitting together").
            public const string TWall = "t_wall";
            public const string CrossWall = "cross_wall";
            public const string Doorway = "doorway";
            public const string Ramp = "ramp";
            public const string RoofLow = "roof_low";
            public const string Fence = "fence";

            public const string Wood = "wood";
            public const string Brick = "brick";
            public const string Stone = "stone";
            public const string Glass = "glass";
            public const string Concrete = "concrete";
            public const string RoofTile = "roof_tile";
            public const string Plaster = "plaster";
        }

        /// <summary>The standard blocks and materials.</summary>
        public static BuildCatalog CreateDefault()
        {
            var c = new BuildCatalog();

            //                     id             name          kind               layer                 W  H  D
            c.Add(new BlockType(Ids.Floor,     "Floor",      BlockKind.Floor,    OccupancyLayer.Floor, 1, 1, 1));
            c.Add(new BlockType(Ids.Wall,      "Wall",       BlockKind.Wall,     OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.HalfWall,  "Half wall",  BlockKind.HalfWall, OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.RoofSlope, "Roof",       BlockKind.Roof,     OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.RoofFlat,  "Flat roof",  BlockKind.Roof,     OccupancyLayer.Floor, 1, 1, 1));
            c.Add(new BlockType(Ids.Stairs,    "Stairs",     BlockKind.Stairs,   OccupancyLayer.Body,  1, 1, 2));
            c.Add(new BlockType(Ids.Door,      "Door",       BlockKind.Door,     OccupancyLayer.Body,  1, 2, 1));
            c.Add(new BlockType(Ids.Window,    "Window",     BlockKind.Window,   OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.Pillar,    "Pillar",     BlockKind.Pillar,   OccupancyLayer.Body,  1, 1, 1));
            // Added for room kits: at rotation 0 its two arms run from the cell centre toward local +X and +Z.
            c.Add(new BlockType(Ids.CornerWall, "Corner wall", BlockKind.Corner,  OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.Beam,      "Beam",       BlockKind.Beam,     OccupancyLayer.Body,  3, 1, 1));
            // Connectors: where a wall meets the middle of another (T), or two cross (+), without a gap.
            c.Add(new BlockType(Ids.TWall,     "T wall",     BlockKind.Corner,   OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.CrossWall, "Cross wall", BlockKind.Corner,   OccupancyLayer.Body,  1, 1, 1));
            // A doorway one wall high (the Door is two cells tall), so it fits in a row of walls.
            c.Add(new BlockType(Ids.Doorway,   "Doorway",    BlockKind.Door,     OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.Ramp,      "Ramp",       BlockKind.Stairs,   OccupancyLayer.Body,  1, 1, 2));
            c.Add(new BlockType(Ids.RoofLow,   "Low roof",   BlockKind.Roof,     OccupancyLayer.Body,  1, 1, 1));
            c.Add(new BlockType(Ids.Fence,     "Fence",      BlockKind.HalfWall, OccupancyLayer.Body,  1, 1, 1));

            c.Add(new BuildMaterial(Ids.Wood,     "Wood",       new PaintColor(255, 255, 255), false));
            c.Add(new BuildMaterial(Ids.Brick,    "Brick",      new PaintColor(255, 255, 255), false));
            c.Add(new BuildMaterial(Ids.Stone,    "Stone",      new PaintColor(255, 255, 255), false));
            c.Add(new BuildMaterial(Ids.Glass,    "Glass",      new PaintColor(200, 230, 255, 110), true));
            c.Add(new BuildMaterial(Ids.Concrete, "Concrete",   new PaintColor(255, 255, 255), false));
            c.Add(new BuildMaterial(Ids.RoofTile, "Roof tiles", new PaintColor(255, 255, 255), false));
            c.Add(new BuildMaterial(Ids.Plaster,  "Plaster",    new PaintColor(255, 255, 255), false));

            return c;
        }

        /// <summary>Ids must be non-empty and use only a-z, 0-9, '_', '-', '.', ':' so they survive the save format.</summary>
        internal static void CheckId(string id)
        {
            if (!IsValidId(id))
            {
                throw new ArgumentException("Invalid id '" + id + "'. Use lowercase letters, digits, _ - . : only.");
            }
        }

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            foreach (char ch in id)
            {
                bool ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-' || ch == '.' || ch == ':';
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
