using System;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// A whole-number grid cell (or a cell offset). Y is up, like Unity.
    /// Used as a dictionary key, so equality and hashing are cheap and allocation-free.
    /// </summary>
    public struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GridPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly GridPos Zero = new GridPos(0, 0, 0);

        public static GridPos operator +(GridPos a, GridPos b)
        {
            return new GridPos(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static GridPos operator -(GridPos a, GridPos b)
        {
            return new GridPos(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static bool operator ==(GridPos a, GridPos b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(GridPos a, GridPos b)
        {
            return !(a == b);
        }

        public bool Equals(GridPos other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos && this == (GridPos)obj;
        }

        public override int GetHashCode()
        {
            // Large primes spread neighbouring cells across buckets.
            unchecked
            {
                return (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }
    }

    /// <summary>A plain float vector. The Unity layer converts to and from UnityEngine.Vector3.</summary>
    public struct Vec3 : IEquatable<Vec3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator *(Vec3 a, float s)
        {
            return new Vec3(a.X * s, a.Y * s, a.Z * s);
        }

        public bool Equals(Vec3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec3 && Equals((Vec3)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ (Y.GetHashCode() * 31) ^ Z.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }
    }

    /// <summary>
    /// A rotation around the vertical (Y) axis in quarter turns: 0 = 0 deg, 1 = 90, 2 = 180, 3 = 270.
    /// Turns are clockwise seen from above, matching Unity's Quaternion.Euler(0, degrees, 0):
    /// one turn maps local +Z (forward) to world +X, and local +X (right) to world -Z.
    /// </summary>
    public struct Rotation : IEquatable<Rotation>
    {
        private readonly byte _turns;

        public Rotation(int quarterTurns)
        {
            _turns = (byte)Mod4(quarterTurns);
        }

        public static readonly Rotation None = new Rotation(0);

        /// <summary>0 to 3.</summary>
        public int QuarterTurns
        {
            get { return _turns; }
        }

        /// <summary>0, 90, 180 or 270. Feed to Quaternion.Euler(0, Degrees, 0).</summary>
        public int Degrees
        {
            get { return _turns * 90; }
        }

        public Rotation Clockwise()
        {
            return new Rotation(_turns + 1);
        }

        public Rotation CounterClockwise()
        {
            return new Rotation(_turns + 3);
        }

        public Rotation Plus(Rotation other)
        {
            return new Rotation(_turns + other._turns);
        }

        /// <summary>Snaps any yaw angle (degrees, any sign or size) to the nearest quarter turn.</summary>
        public static Rotation FromDegrees(float yawDegrees)
        {
            if (float.IsNaN(yawDegrees) || float.IsInfinity(yawDegrees))
            {
                return None;
            }
            double turns = Math.Round(yawDegrees / 90.0, MidpointRounding.AwayFromZero);
            return new Rotation((int)(turns % 4));
        }

        /// <summary>Rotates a local cell offset into world orientation (around the anchor cell).</summary>
        public GridPos Apply(GridPos local)
        {
            switch (_turns)
            {
                case 1: return new GridPos(local.Z, local.Y, -local.X);
                case 2: return new GridPos(-local.X, local.Y, -local.Z);
                case 3: return new GridPos(-local.Z, local.Y, local.X);
                default: return local;
            }
        }

        /// <summary>Rotates a float direction the same way as <see cref="Apply(GridPos)"/>.</summary>
        public Vec3 Apply(Vec3 local)
        {
            switch (_turns)
            {
                case 1: return new Vec3(local.Z, local.Y, -local.X);
                case 2: return new Vec3(-local.X, local.Y, -local.Z);
                case 3: return new Vec3(-local.Z, local.Y, local.X);
                default: return local;
            }
        }

        private static int Mod4(int value)
        {
            int m = value % 4;
            return m < 0 ? m + 4 : m;
        }

        public static bool operator ==(Rotation a, Rotation b)
        {
            return a._turns == b._turns;
        }

        public static bool operator !=(Rotation a, Rotation b)
        {
            return a._turns != b._turns;
        }

        public bool Equals(Rotation other)
        {
            return _turns == other._turns;
        }

        public override bool Equals(object obj)
        {
            return obj is Rotation && Equals((Rotation)obj);
        }

        public override int GetHashCode()
        {
            return _turns;
        }

        public override string ToString()
        {
            return Degrees + " deg";
        }
    }

    /// <summary>
    /// Index of a 16 x 16 x 16 cell chunk. The renderer builds one merged mesh per chunk,
    /// so changing one block only rebuilds that chunk, not the whole build.
    /// </summary>
    public struct ChunkPos : IEquatable<ChunkPos>
    {
        /// <summary>Cells per chunk side. A power of two so the cell-to-chunk math is a shift.</summary>
        public const int Size = 16;
        private const int Shift = 4;

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public ChunkPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>The chunk holding a cell. Arithmetic shift floors, so cell -1 is in chunk -1.</summary>
        public static ChunkPos FromCell(GridPos cell)
        {
            return new ChunkPos(cell.X >> Shift, cell.Y >> Shift, cell.Z >> Shift);
        }

        /// <summary>The chunk's lowest corner cell.</summary>
        public GridPos MinCell
        {
            get { return new GridPos(X << Shift, Y << Shift, Z << Shift); }
        }

        public static bool operator ==(ChunkPos a, ChunkPos b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(ChunkPos a, ChunkPos b)
        {
            return !(a == b);
        }

        public bool Equals(ChunkPos other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkPos && this == (ChunkPos)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
            }
        }

        public override string ToString()
        {
            return "chunk(" + X + ", " + Y + ", " + Z + ")";
        }
    }
}
