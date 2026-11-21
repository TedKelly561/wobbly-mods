using System;
using System.Collections.Generic;
using System.IO;

namespace WobblyMods.Core.Shapes
{
    /// <summary>A plain 8-bit colour.</summary>
    public struct Rgb
    {
        public byte R, G, B;

        public Rgb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>A mesh together with the colour it is drawn in.</summary>
    public sealed class ColoredMesh
    {
        public ShapeMesh Mesh;
        public Rgb Color;

        public ColoredMesh(ShapeMesh mesh, Rgb color)
        {
            Mesh = mesh;
            Color = color;
        }
    }

    /// <summary>
    /// A tiny software renderer: an orthographic picture of ShapeMesh parts, so shapes can be looked at
    /// without the game. Camera: looks along -Z (sees the model's +Z side), +X to the right, +Y up.
    /// </summary>
    public static class Preview
    {
        // Fraction of the picture the model may fill (a 5 % margin in total, 2.5 % on each side).
        private const float Fill = 0.95f;

        private static readonly V3 LightDirection = new V3(0.3f, 0.5f, 0.8f).Normalized;

        /// <summary>
        /// Draws the parts. yawDegrees turns the model about Y first (same convention as
        /// ShapeMesh.Transformed: yaw 90 turns +Z into +X). Returns width * height pixels, row-major, row 0 on top.
        /// </summary>
        public static Rgb[] Render(IList<ColoredMesh> parts, int width, int height, float yawDegrees, Rgb background)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "width must be >= 1");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "height must be >= 1");

            var pixels = new Rgb[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;
            if (parts == null || parts.Count == 0) return pixels;

            // Turn every part and find the combined bounds.
            var turned = new List<ShapeMesh>();
            var colors = new List<Rgb>();
            V3 min = V3.Zero, max = V3.Zero;
            bool haveBounds = false;
            for (int p = 0; p < parts.Count; p++)
            {
                ColoredMesh part = parts[p];
                if (part == null || part.Mesh == null || part.Mesh.Triangles.Count < 3) continue;

                ShapeMesh mesh = part.Mesh.Transformed(new V3(1f, 1f, 1f), yawDegrees, 0f, V3.Zero);
                turned.Add(mesh);
                colors.Add(part.Color);

                V3 lo = mesh.Min, hi = mesh.Max;
                if (!haveBounds)
                {
                    min = lo;
                    max = hi;
                    haveBounds = true;
                }
                else
                {
                    min = new V3(Math.Min(min.X, lo.X), Math.Min(min.Y, lo.Y), Math.Min(min.Z, lo.Z));
                    max = new V3(Math.Max(max.X, hi.X), Math.Max(max.Y, hi.Y), Math.Max(max.Z, hi.Z));
                }
            }
            if (!haveBounds) return pixels;

            float sizeX = max.X - min.X;
            float sizeY = max.Y - min.Y;
            if (sizeX <= 1e-9f && sizeY <= 1e-9f) return pixels; // nothing with any size to draw

            // One scale for both axes; the axis that is the tighter fit decides.
            float scaleX = sizeX > 1e-9f ? width * Fill / sizeX : float.MaxValue;
            float scaleY = sizeY > 1e-9f ? height * Fill / sizeY : float.MaxValue;
            float scale = Math.Min(scaleX, scaleY);
            float centerX = (min.X + max.X) / 2f;
            float centerY = (min.Y + max.Y) / 2f;

            var depth = new float[width * height];
            for (int i = 0; i < depth.Length; i++) depth[i] = float.NegativeInfinity;

            for (int m = 0; m < turned.Count; m++)
            {
                ShapeMesh mesh = turned[m];
                Rgb color = colors[m];
                for (int t = 0; t + 2 < mesh.Triangles.Count; t += 3)
                {
                    V3 a = mesh.Vertices[mesh.Triangles[t]];
                    V3 b = mesh.Vertices[mesh.Triangles[t + 1]];
                    V3 c = mesh.Vertices[mesh.Triangles[t + 2]];

                    V3 normal = V3.Cross(b - a, c - a).Normalized;
                    if (normal.Length < 0.5f) continue; // zero-area triangle

                    float brightness = 0.35f + 0.65f * Math.Abs(V3.Dot(normal, LightDirection));
                    Rgb shaded = new Rgb(Shade(color.R, brightness), Shade(color.G, brightness), Shade(color.B, brightness));

                    // Picture space: x to the right, y down (row 0 on top).
                    float x0 = (a.X - centerX) * scale + width / 2f, y0 = height / 2f - (a.Y - centerY) * scale;
                    float x1 = (b.X - centerX) * scale + width / 2f, y1 = height / 2f - (b.Y - centerY) * scale;
                    float x2 = (c.X - centerX) * scale + width / 2f, y2 = height / 2f - (c.Y - centerY) * scale;

                    float area = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
                    if (Math.Abs(area) < 1e-12f) continue; // edge-on: nothing to fill

                    int firstX = Math.Max(0, (int)Math.Ceiling(Math.Min(x0, Math.Min(x1, x2)) - 0.5f));
                    int lastX = Math.Min(width - 1, (int)Math.Floor(Math.Max(x0, Math.Max(x1, x2)) - 0.5f));
                    int firstY = Math.Max(0, (int)Math.Ceiling(Math.Min(y0, Math.Min(y1, y2)) - 0.5f));
                    int lastY = Math.Min(height - 1, (int)Math.Floor(Math.Max(y0, Math.Max(y1, y2)) - 0.5f));

                    for (int py = firstY; py <= lastY; py++)
                    {
                        for (int px = firstX; px <= lastX; px++)
                        {
                            float sx = px + 0.5f, sy = py + 0.5f; // pixel centre
                            float w0 = ((y1 - y2) * (sx - x2) + (x2 - x1) * (sy - y2)) / area;
                            float w1 = ((y2 - y0) * (sx - x2) + (x0 - x2) * (sy - y2)) / area;
                            float w2 = 1f - w0 - w1;
                            const float edge = -1e-5f; // keeps neighbouring triangles from leaving pin holes
                            if (w0 < edge || w1 < edge || w2 < edge) continue;

                            float z = w0 * a.Z + w1 * b.Z + w2 * c.Z;
                            int index = py * width + px;
                            if (z > depth[index]) // nearest to the camera = largest Z
                            {
                                depth[index] = z;
                                pixels[index] = shaded;
                            }
                        }
                    }
                }
            }

            return pixels;
        }

        private static byte Shade(byte channel, float brightness)
        {
            int value = (int)(channel * brightness + 0.5f);
            if (value < 0) value = 0;
            if (value > 255) value = 255;
            return (byte)value;
        }

        /// <summary>24-bit uncompressed BMP: bottom-up rows, B G R, each row padded to 4 bytes.</summary>
        public static byte[] ToBmp(Rgb[] pixels, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "width must be >= 1");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "height must be >= 1");
            if (pixels == null || pixels.Length != width * height)
                throw new ArgumentException("pixels must hold width * height values", nameof(pixels));

            int rowSize = ((width * 3 + 3) / 4) * 4;
            int dataSize = rowSize * height;
            int fileSize = 54 + dataSize;
            var bytes = new byte[fileSize];

            bytes[0] = (byte)'B';
            bytes[1] = (byte)'M';
            WriteInt(bytes, 2, fileSize);
            WriteInt(bytes, 10, 54);     // where the pixel data starts
            WriteInt(bytes, 14, 40);     // size of this header
            WriteInt(bytes, 18, width);
            WriteInt(bytes, 22, height); // positive: rows run bottom-up
            bytes[26] = 1;               // colour planes
            bytes[28] = 24;              // bits per pixel
            WriteInt(bytes, 34, dataSize);
            WriteInt(bytes, 38, 2835);   // about 72 dpi
            WriteInt(bytes, 42, 2835);

            for (int row = 0; row < height; row++)
            {
                int sourceRow = height - 1 - row; // the first stored row is the bottom row of the picture
                int offset = 54 + row * rowSize;
                for (int x = 0; x < width; x++)
                {
                    Rgb p = pixels[sourceRow * width + x];
                    bytes[offset + x * 3] = p.B;
                    bytes[offset + x * 3 + 1] = p.G;
                    bytes[offset + x * 3 + 2] = p.R;
                }
            }

            return bytes;
        }

        /// <summary>Renders the parts and writes the picture to path as a BMP.</summary>
        public static void SaveBmp(string path, IList<ColoredMesh> parts, int width, int height, float yawDegrees, Rgb background)
        {
            Rgb[] pixels = Render(parts, width, height, yawDegrees, background);
            File.WriteAllBytes(path, ToBmp(pixels, width, height));
        }

        private static void WriteInt(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)(value & 0xFF);
            bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
            bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
            bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }
}
