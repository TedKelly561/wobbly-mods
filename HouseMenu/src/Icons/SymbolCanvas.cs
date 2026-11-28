using System;
using UnityEngine;

namespace WobblyMods.HouseMenu.Icons
{
    /// <summary>
    /// A tiny anti-aliased drawing surface for the menu's symbols (128 x 128, y pointing DOWN like the screen).
    /// Shapes are drawn with signed distances, so edges are smooth at any thickness. Colors are stored
    /// premultiplied; Erase cuts holes (eyes, mortar lines). ToTexture adds a soft drop shadow so white
    /// symbols stay readable on light tab colors such as yellow.
    /// </summary>
    internal sealed class SymbolCanvas
    {
        public const int Size = 128;

        private float[] _r = new float[Size * Size];
        private float[] _g = new float[Size * Size];
        private float[] _b = new float[Size * Size];
        private float[] _a = new float[Size * Size];

        /// <summary>Color used by the next shapes (white by default).</summary>
        public Color Ink = Color.white;

        private delegate float Distance(float x, float y);

        // ---------- Shapes ----------

        /// <summary>Thick line with round ends.</summary>
        public void Line(float x0, float y0, float x1, float y1, float width, bool erase = false)
        {
            float half = width * 0.5f;
            Shape(Math.Min(x0, x1) - half, Math.Min(y0, y1) - half, Math.Max(x0, x1) + half, Math.Max(y0, y1) + half,
                  (x, y) => SegmentDistance(x, y, x0, y0, x1, y1) - half, erase);
        }

        public void Circle(float cx, float cy, float r, bool erase = false)
        {
            Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => Length(x - cx, y - cy) - r, erase);
        }

        /// <summary>Axis-aligned rectangle (x, y = top-left), optionally with round corners.</summary>
        public void Rect(float x, float y, float w, float h, float radius = 0f, bool erase = false)
        {
            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            float hx = w * 0.5f - radius, hy = h * 0.5f - radius;
            Shape(x, y, x + w, y + h, (px, py) =>
            {
                float qx = Math.Abs(px - cx) - hx, qy = Math.Abs(py - cy) - hy;
                float outside = Length(Math.Max(qx, 0f), Math.Max(qy, 0f));
                return outside + Math.Min(Math.Max(qx, qy), 0f) - radius;
            }, erase);
        }

        /// <summary>Rectangle outline with the stroke centred on the edge.</summary>
        public void RectOutline(float x, float y, float w, float h, float width, bool erase = false)
        {
            Line(x, y, x + w, y, width, erase);
            Line(x + w, y, x + w, y + h, width, erase);
            Line(x + w, y + h, x, y + h, width, erase);
            Line(x, y + h, x, y, width, erase);
        }

        /// <summary>
        /// Arc of a ring from angle a0 to a1 (degrees; 0 = right, 90 = DOWN because y points down), round ends.
        /// </summary>
        public void Arc(float cx, float cy, float r, float width, float a0, float a1, bool erase = false)
        {
            float lo = Math.Min(a0, a1), hi = Math.Max(a0, a1);
            float half = width * 0.5f;
            float e0x = cx + r * Cos(lo), e0y = cy + r * Sin(lo);
            float e1x = cx + r * Cos(hi), e1y = cy + r * Sin(hi);
            float ext = r + half;
            Shape(cx - ext, cy - ext, cx + ext, cy + ext, (x, y) =>
            {
                float ang = (float)(Math.Atan2(y - cy, x - cx) * 180.0 / Math.PI);
                // Bring the angle into [lo, lo + 360).
                while (ang < lo) ang += 360f;
                while (ang >= lo + 360f) ang -= 360f;
                if (ang <= hi)
                {
                    return Math.Abs(Length(x - cx, y - cy) - r) - half;
                }
                return Math.Min(Length(x - e0x, y - e0y), Length(x - e1x, y - e1y)) - half;
            }, erase);
        }

        /// <summary>An arc with an arrow head at its a1 end, pointing the way the arc runs.</summary>
        public void ArcArrow(float cx, float cy, float r, float width, float a0, float a1, float head)
        {
            Arc(cx, cy, r, width, a0, a1);
            float ex = cx + r * Cos(a1), ey = cy + r * Sin(a1);
            float dir = a1 >= a0 ? 1f : -1f;
            float tx = -Sin(a1) * dir, ty = Cos(a1) * dir;  // tangent the way the arc runs
            float nx = Cos(a1), ny = Sin(a1);              // outward normal
            Poly(ex + tx * head, ey + ty * head,
                 ex + nx * head * 0.95f - tx * head * 0.15f, ey + ny * head * 0.95f - ty * head * 0.15f,
                 ex - nx * head * 0.95f - tx * head * 0.15f, ey - ny * head * 0.95f - ty * head * 0.15f);
        }

        /// <summary>Filled polygon from x,y pairs (any shape, even-odd fill).</summary>
        public void Poly(params float[] xy)
        {
            PolyCore(false, xy);
        }

        public void ErasePoly(params float[] xy)
        {
            PolyCore(true, xy);
        }

        /// <summary>Filled ellipse (as a smooth polygon).</summary>
        public void Ellipse(float cx, float cy, float rx, float ry, bool erase = false)
        {
            const int n = 48;
            var pts = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                pts[2 * i] = cx + rx * Cos(a);
                pts[2 * i + 1] = cy + ry * Sin(a);
            }
            PolyCore(erase, pts);
        }

        /// <summary>Five-pointed star.</summary>
        public void Star(float cx, float cy, float outer, float inner, bool erase = false)
        {
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? outer : inner;
                float a = -90f + i * 36f;
                pts[2 * i] = cx + r * Cos(a);
                pts[2 * i + 1] = cy + r * Sin(a);
            }
            PolyCore(erase, pts);
        }

        // ---------- Whole-canvas changes ----------

        /// <summary>Mirror left-right (redo from undo, and so on).</summary>
        public void FlipX()
        {
            Remap((x, y) => (Size - 1 - x) + y * Size);
        }

        /// <summary>Turn the picture clockwise by 90 degrees, quarter times.</summary>
        public void Rotate90(int quarter)
        {
            quarter = ((quarter % 4) + 4) % 4;
            for (int q = 0; q < quarter; q++)
            {
                // Clockwise on screen (y down): new(x, y) = old(y, Size-1-x).
                Remap((x, y) => y + (Size - 1 - x) * Size);
            }
        }

        /// <summary>
        /// Draws a "switched off" slash across the whole symbol, with a gap cut around it so it reads clearly.
        /// </summary>
        public void OffSlash()
        {
            Color ink = Ink;
            Line(20, 20, 108, 108, 30, true);
            Ink = new Color(1f, 0.35f, 0.3f);
            Line(20, 20, 108, 108, 14);
            Ink = ink;
        }

        /// <summary>Makes the texture: the symbol over a soft dark drop shadow, on a transparent background.</summary>
        public Texture2D ToTexture(string name)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels32(ToPixels());
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>The finished pixels (bottom row first, as Texture2D expects), shadow included.</summary>
        public Color32[] ToPixels()
        {
            const int shadowX = 3, shadowY = 4;
            const float shadowStrength = 0.45f;
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = x + y * Size;
                    int sx = x - shadowX, sy = y - shadowY;
                    float sa = sx >= 0 && sy >= 0 ? _a[sx + sy * Size] * shadowStrength : 0f;
                    float a = _a[i];
                    float outA = a + sa * (1f - a);
                    float r = 0f, g = 0f, b = 0f;
                    if (outA > 0.0001f)
                    {
                        // Shadow is black, so it only adds alpha; un-premultiply the symbol's color.
                        r = _r[i] / outA;
                        g = _g[i] / outA;
                        b = _b[i] / outA;
                    }
                    // Texture rows start at the bottom; the canvas starts at the top.
                    pixels[x + (Size - 1 - y) * Size] = new Color32(ToByte(r), ToByte(g), ToByte(b), ToByte(outA));
                }
            }

            return pixels;
        }

        // ---------- Internals ----------

        private void PolyCore(bool erase, float[] xy)
        {
            int n = xy.Length / 2;
            if (n < 3) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                minX = Math.Min(minX, xy[2 * i]); maxX = Math.Max(maxX, xy[2 * i]);
                minY = Math.Min(minY, xy[2 * i + 1]); maxY = Math.Max(maxY, xy[2 * i + 1]);
            }
            Shape(minX, minY, maxX, maxY, (x, y) =>
            {
                bool inside = false;
                float best = float.MaxValue;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float xi = xy[2 * i], yi = xy[2 * i + 1], xj = xy[2 * j], yj = xy[2 * j + 1];
                    if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                    {
                        inside = !inside;
                    }
                    best = Math.Min(best, SegmentDistance(x, y, xi, yi, xj, yj));
                }
                return inside ? -best : best;
            }, erase);
        }

        /// <summary>Applies a shape given its bounds and signed distance (negative inside).</summary>
        private void Shape(float minX, float minY, float maxX, float maxY, Distance distance, bool erase)
        {
            int x0 = Math.Max(0, (int)Math.Floor(minX) - 1), y0 = Math.Max(0, (int)Math.Floor(minY) - 1);
            int x1 = Math.Min(Size - 1, (int)Math.Ceiling(maxX) + 1), y1 = Math.Min(Size - 1, (int)Math.Ceiling(maxY) + 1);
            float pr = Ink.r * Ink.a, pg = Ink.g * Ink.a, pb = Ink.b * Ink.a, pa = Ink.a;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float cov = 0.5f - distance(x + 0.5f, y + 0.5f);
                    if (cov <= 0f) continue;
                    if (cov > 1f) cov = 1f;
                    int i = x + y * Size;
                    if (erase)
                    {
                        float keep = 1f - cov;
                        _r[i] *= keep; _g[i] *= keep; _b[i] *= keep; _a[i] *= keep;
                    }
                    else
                    {
                        float keep = 1f - cov * pa;
                        _r[i] = pr * cov + _r[i] * keep;
                        _g[i] = pg * cov + _g[i] * keep;
                        _b[i] = pb * cov + _b[i] * keep;
                        _a[i] = pa * cov + _a[i] * keep;
                    }
                }
            }
        }

        /// <summary>Rebuilds every channel: new pixel (x, y) takes old pixel source(x, y).</summary>
        private void Remap(Func<int, int, int> source)
        {
            var r = new float[Size * Size];
            var g = new float[Size * Size];
            var b = new float[Size * Size];
            var a = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int s = source(x, y), d = x + y * Size;
                    r[d] = _r[s]; g[d] = _g[s]; b[d] = _b[s]; a[d] = _a[s];
                }
            }
            _r = r; _g = g; _b = b; _a = a;
        }

        private static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float len2 = dx * dx + dy * dy;
            float t = len2 < 1e-6f ? 0f : ((px - ax) * dx + (py - ay) * dy) / len2;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            return Length(px - (ax + t * dx), py - (ay + t * dy));
        }

        private static float Length(float x, float y)
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static float Cos(float degrees)
        {
            return (float)Math.Cos(degrees * Math.PI / 180.0);
        }

        private static float Sin(float degrees)
        {
            return (float)Math.Sin(degrees * Math.PI / 180.0);
        }

        private static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 1f) return 255;
            return (byte)(v * 255f + 0.5f);
        }
    }
}
