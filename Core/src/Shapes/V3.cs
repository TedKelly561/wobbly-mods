using System;
using System.Globalization;

namespace WobblyMods.Core.Shapes
{
    public struct V3
    {
        public float X, Y, Z;

        public V3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly V3 Zero = new V3(0f, 0f, 0f);
        public static readonly V3 Up = new V3(0f, 1f, 0f);
        public static readonly V3 Forward = new V3(0f, 0f, 1f);
        public static readonly V3 Right = new V3(1f, 0f, 0f);

        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, float s) => new V3(a.X * s, a.Y * s, a.Z * s);
        public static V3 operator *(float s, V3 a) => new V3(a.X * s, a.Y * s, a.Z * s);
        public static V3 operator /(V3 a, float s) => new V3(a.X / s, a.Y / s, a.Z / s);
        public static V3 operator -(V3 a) => new V3(-a.X, -a.Y, -a.Z);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public V3 Normalized
        {
            get
            {
                float len = Length;
                if (len < 1e-6f) return Zero;
                return this / len;
            }
        }

        public static float Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static V3 Cross(V3 a, V3 b)
        {
            return new V3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public static V3 Lerp(V3 a, V3 b, float t)
        {
            return new V3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        public override string ToString()
        {
            return $"({X.ToString("R", CultureInfo.InvariantCulture)}, {Y.ToString("R", CultureInfo.InvariantCulture)}, {Z.ToString("R", CultureInfo.InvariantCulture)})";
        }
    }
}
