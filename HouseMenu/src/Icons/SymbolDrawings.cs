using UnityEngine;

namespace WobblyMods.HouseMenu.Icons
{
    /// <summary>
    /// The menu's symbols, drawn in code: thick, simple shapes a 5-year-old can tell apart, white on the
    /// tab color (the tile behind supplies the color). Coordinates are on a 128 x 128 canvas, y pointing down.
    /// Names are the part of a "sym:" key after the prefix (see SymbolIcons for the key format).
    /// </summary>
    internal static class SymbolDrawings
    {
        private const float W = 16f; // standard stroke

        /// <summary>Draws the named symbol. Returns false for an unknown name.</summary>
        /// <param name="tint">Optional color from the key (#RRGGBB), for paint drops and material cubes.</param>
        public static bool Draw(SymbolCanvas c, string name, Color? tint)
        {
            switch (name)
            {
                // Arrows (Change tab moves, Raise/Lower).
                case "right": Arrow(c); return true;
                case "down": Arrow(c); c.Rotate90(1); return true;
                case "left": Arrow(c); c.Rotate90(2); return true;
                case "up": Arrow(c); c.Rotate90(3); return true;
                case "closer": Closer(c); return true;
                case "farther": Farther(c); return true;
                case "back": Back(c); return true;

                case "undo": Undo(c); return true;
                case "redo": Undo(c); c.FlipX(); return true;
                case "turn": Turn(c); return true;
                case "autoturn": Turn(c); c.Star(64, 64, 20, 9); return true;
                case "plus": c.Rect(22, 54, 84, 20, 6); c.Rect(54, 22, 20, 84, 6); return true;
                case "minus": c.Rect(22, 54, 84, 20, 6); return true;
                case "paint": Drop(c, tint ?? new Color(1f, 0.5f, 0f)); return true;
                case "copy": Copy(c); return true;
                case "save": Disk(c); return true;
                case "load": LoadBox(c); return true;
                case "broom": Broom(c); return true;
                case "ghost": Ghost(c); return true;
                case "yes": c.Line(24, 66, 52, 94, 20); c.Line(52, 94, 106, 32, 20); return true;
                case "no": c.Line(30, 30, 98, 98, 20); c.Line(98, 30, 30, 98, 20); return true;
                case "star": c.Star(64, 66, 52, 22); return true;
                case "fish": Fish(c); return true;
                case "redtail": Fish(c); c.Line(20, 70, 4, 92, 5); c.Line(26, 76, 16, 100, 5); return true;
                case "shirt": c.Poly(40, 18, 88, 18, 120, 44, 104, 64, 90, 54, 90, 112, 38, 112, 38, 54, 24, 64, 8, 44); c.Circle(64, 18, 14, true); return true;
                case "cat": c.Circle(64, 74, 44); c.Poly(22, 62, 26, 12, 62, 40); c.Poly(106, 62, 102, 12, 66, 40); Face(c, 74); return true;
                case "bunny": c.Circle(64, 84, 38); c.Ellipse(44, 34, 13, 34); c.Ellipse(84, 34, 13, 34); Face(c, 82); return true;
                case "puppy": c.Circle(64, 66, 42); c.Ellipse(20, 70, 15, 38); c.Ellipse(108, 70, 15, 38); Face(c, 62); c.Ellipse(64, 104, 9, 12); return true;
                case "trex": c.Rect(14, 30, 100, 50, 22); c.Rect(60, 70, 54, 34, 12); c.Circle(44, 50, 9, true);
                             for (int i = 0; i < 4; i++) c.ErasePoly(22 + i * 10, 80, 34 + i * 10, 80, 28 + i * 10, 96);
                             c.Poly(50, 32, 62, 8, 74, 32); c.Poly(76, 32, 88, 8, 100, 32); return true;
                case "1": One(c); return true;
                case "2": Two(c); return true;
                case "3": Three(c); return true;
                case "button": c.Circle(64, 64, 60); return true;

                // Build tab.
                case "brick": Brick(c); return true;
                case "wall": Bricks(c, 22, 14, 84, 100); return true;
                case "halfwall": Bricks(c, 22, 62, 84, 52); c.Line(10, 116, 118, 116, 6); return true;
                case "longwall": Bricks(c, 6, 46, 116, 52); return true;
                case "door": c.RectOutline(34, 16, 60, 96, 12); c.Circle(80, 66, 8); c.Line(10, 114, 118, 114, 8); return true;
                case "window": c.RectOutline(22, 22, 84, 84, 12); c.Line(64, 22, 64, 106, 10); c.Line(22, 64, 106, 64, 10); return true;
                case "stairs": c.Poly(14, 114, 14, 90, 38, 90, 38, 66, 62, 66, 62, 42, 86, 42, 86, 18, 114, 18, 114, 114); return true;
                case "roof": Roof(c); return true;
                case "roofflat": c.Rect(8, 24, 112, 22, 4); c.Rect(22, 52, 84, 62, 2); c.Rect(54, 76, 20, 38, 3, true); return true;
                case "floor": Floor(c, 3); return true;
                case "bigfloor": Floor(c, 5); return true;
                case "pillar": c.Rect(48, 22, 32, 84); c.Rect(36, 12, 56, 14, 4); c.Rect(36, 102, 56, 14, 4); return true;
                case "beam": c.Rect(8, 50, 112, 28, 4); c.Line(30, 56, 30, 72, 4, true); c.Line(64, 56, 64, 72, 4, true); c.Line(98, 56, 98, 72, 4, true); return true;
                case "corner": c.Poly(18, 18, 50, 18, 50, 78, 110, 78, 110, 110, 18, 110); return true;
                case "room": House(c, false); return true;
                case "bigroom": House(c, true); return true;
                case "ghostblock": GhostBlock(c); return true;
                case "size": c.Rect(12, 74, 34, 34, 4); c.Rect(56, 22, 62, 86, 6); return true;
                case "tofloor": c.Line(64, 14, 64, 66, W); c.Poly(36, 60, 92, 60, 64, 94); c.Rect(14, 102, 100, 14, 6); return true;
                case "trash": Trash(c); return true;
                case "block": Cube(c, tint ?? new Color(0.7f, 0.7f, 0.7f)); return true;

                // Tab buttons.
                case "tab-furniture": Sofa(c); return true;
                case "tab-toys": Ball(c); return true;
                case "tab-animals": Paw(c); return true;
                case "tab-change": Wand(c); return true;
                case "tab-build": Hammer(c); return true;

                default: return false;
            }
        }

        private static void Arrow(SymbolCanvas c)
        {
            c.Line(18, 64, 76, 64, 22);
            c.Poly(64, 26, 112, 64, 64, 102);
        }

        // Toward you: the shaft widens as it comes closer, ending in a big head.
        private static void Closer(SymbolCanvas c)
        {
            c.Poly(56, 10, 72, 10, 86, 68, 42, 68);
            c.Poly(18, 62, 110, 62, 64, 118);
        }

        // Away from you: a wide shaft narrowing into a small head far away.
        private static void Farther(SymbolCanvas c)
        {
            c.Poly(40, 118, 88, 118, 72, 54, 56, 54);
            c.Poly(38, 58, 90, 58, 64, 14);
        }

        private static void Back(SymbolCanvas c)
        {
            c.Arc(64, 64, 54, 9, 0, 360);
            c.Line(40, 64, 88, 64, 18);
            c.Poly(54, 34, 22, 64, 54, 94);
        }

        // Curved arrow going over the top, coming down on the left: "go back".
        private static void Undo(SymbolCanvas c)
        {
            c.ArcArrow(68, 72, 36, W, 10, -180, 24);
        }

        // Circular arrow, clockwise.
        private static void Turn(SymbolCanvas c)
        {
            c.ArcArrow(64, 66, 38, W, -40, 230, 24);
        }

        private static void Drop(SymbolCanvas c, Color color)
        {
            Color ink = c.Ink;
            c.Circle(64, 80, 36);
            c.Poly(64, 6, 31, 64, 97, 64);
            color.a = 1f;
            c.Ink = color;
            c.Circle(64, 80, 27);
            c.Poly(64, 22, 39, 70, 89, 70);
            c.Ink = new Color(1f, 1f, 1f, 0.75f);
            c.Circle(50, 82, 7); // shine
            c.Ink = ink;
        }

        private static void Copy(SymbolCanvas c)
        {
            c.Rect(14, 14, 66, 66, 10);
            c.Rect(26, 26, 42, 42, 4, true);
            c.Rect(40, 40, 78, 78, 14, true);
            c.Rect(48, 48, 66, 66, 10);
        }

        private static void Disk(SymbolCanvas c)
        {
            c.Poly(16, 16, 96, 16, 112, 32, 112, 112, 16, 112);
            c.Rect(36, 16, 48, 32, 2, true);  // metal shutter hole
            c.Rect(64, 22, 12, 20, 2);        // the shutter's slot
            c.Rect(30, 64, 68, 40, 6, true);  // label
            c.Line(40, 76, 88, 76, 5);        // writing on the label
            c.Line(40, 90, 76, 90, 5);
        }

        // An open box with an arrow coming out: "get it back out".
        private static void LoadBox(SymbolCanvas c)
        {
            c.Line(18, 70, 18, 110, 14);
            c.Line(18, 110, 110, 110, 14);
            c.Line(110, 110, 110, 70, 14);
            c.Line(64, 96, 64, 44, 18);
            c.Poly(34, 52, 94, 52, 64, 12);
        }

        private static void Broom(SymbolCanvas c)
        {
            c.Line(96, 10, 66, 66, 12);
            c.Poly(48, 58, 86, 76, 76, 118, 18, 104);
            c.Line(40, 84, 30, 106, 4, true);
            c.Line(54, 90, 46, 112, 4, true);
            c.Line(68, 96, 62, 116, 4, true);
            c.Rect(44, 60, 46, 10, 4); // the band holding the bristles
        }

        private static void Ghost(SymbolCanvas c)
        {
            c.Circle(64, 54, 38);
            c.Rect(26, 54, 76, 46);
            c.Circle(38, 100, 12);
            c.Circle(64, 100, 12);
            c.Circle(90, 100, 12);
            c.Circle(51, 112, 9, true);
            c.Circle(77, 112, 9, true);
            c.Ellipse(50, 52, 8, 11, true);
            c.Ellipse(78, 52, 8, 11, true);
        }

        private static void GhostBlock(SymbolCanvas c)
        {
            // A dashed box: a see-through "ghost" piece.
            for (int i = 0; i < 4; i++)
            {
                float a = 18 + i * 24, b = a + 14;
                c.Line(a, 18, b, 18, 10);
                c.Line(a, 110, b, 110, 10);
                c.Line(18, a, 18, b, 10);
                c.Line(110, a, 110, b, 10);
            }
            c.Ellipse(50, 60, 7, 10);
            c.Ellipse(78, 60, 7, 10);
        }

        /// <summary>Two eyes and a nose cut out of an animal's head.</summary>
        private static void Face(SymbolCanvas c, float y)
        {
            c.Ellipse(48, y - 6, 7, 10, true);
            c.Ellipse(80, y - 6, 7, 10, true);
            c.Poly(56, y + 12, 72, y + 12, 64, y + 22);
            c.ErasePoly(56, y + 12, 72, y + 12, 64, y + 22);
        }

        private static void Fish(SymbolCanvas c)
        {
            c.Ellipse(54, 64, 40, 28);
            c.Poly(84, 64, 120, 34, 120, 94);
            c.Circle(34, 58, 6, true);
        }

        private static void One(SymbolCanvas c)
        {
            c.Line(66, 16, 66, 110, 22);
            c.Line(66, 16, 42, 38, 18);
            c.Line(40, 110, 92, 110, 18);
        }

        private static void Two(SymbolCanvas c)
        {
            c.Arc(64, 44, 28, 20, -170, 30);
            c.Line(88, 58, 34, 110, 20);
            c.Line(34, 110, 98, 110, 18);
        }

        private static void Three(SymbolCanvas c)
        {
            c.Arc(62, 40, 24, 18, -160, 80);
            c.Arc(62, 86, 27, 18, -80, 160);
            c.Line(56, 62, 66, 62, 16);
        }

        private static void Brick(SymbolCanvas c)
        {
            // One chunky brick in 3/4 view: front, top and side faces.
            Color ink = c.Ink;
            c.Rect(12, 56, 84, 48, 4);
            c.Ink = new Color(1f, 1f, 1f, 0.8f);
            c.Poly(12, 56, 36, 32, 118, 32, 96, 56);
            c.Ink = new Color(1f, 1f, 1f, 0.65f);
            c.Poly(96, 56, 118, 32, 118, 80, 96, 104);
            c.Ink = ink;
        }

        private static void Bricks(SymbolCanvas c, float x, float y, float w, float h)
        {
            c.Rect(x, y, w, h, 3);
            const float rowH = 17f;
            int row = 0;
            for (float ry = y + rowH; ry < y + h - 4; ry += rowH, row++)
            {
                c.Line(x, ry, x + w, ry, 4, true);
            }
            row = 0;
            for (float ry = y; ry < y + h - 4; ry += rowH, row++)
            {
                float offset = row % 2 == 0 ? 0f : 14f;
                for (float vx = x + 14 + offset; vx < x + w - 4; vx += 28)
                {
                    c.Line(vx, ry + 2, vx, ry + rowH - 2, 4, true);
                }
            }
        }

        private static void Roof(SymbolCanvas c)
        {
            c.Rect(80, 24, 16, 34, 2);          // chimney
            c.Poly(4, 76, 64, 18, 124, 76, 104, 76, 64, 40, 24, 76);
            c.Line(30, 82, 30, 112, 10);
            c.Line(98, 82, 98, 112, 10);
        }

        private static void Floor(SymbolCanvas c, int tiles)
        {
            c.Poly(32, 52, 120, 52, 96, 100, 8, 100);
            for (int i = 1; i < tiles; i++)
            {
                float t = i / (float)tiles;
                c.Line(32 + 88 * t, 52, 8 + 88 * t, 100, 4, true);
            }
            c.Line(20, 76, 108, 76, 4, true);
        }

        private static void House(SymbolCanvas c, bool big)
        {
            if (big)
            {
                c.Poly(4, 62, 64, 16, 124, 62, 116, 62, 116, 114, 12, 114, 12, 62);
                c.Rect(54, 76, 20, 38, 3, true);
                c.Rect(24, 72, 20, 18, 3, true);
                c.Rect(84, 72, 20, 18, 3, true);
            }
            else
            {
                c.Poly(16, 64, 64, 20, 112, 64, 102, 64, 102, 112, 26, 112, 26, 64);
                c.Rect(54, 76, 22, 36, 3, true);
            }
        }

        private static void Trash(SymbolCanvas c)
        {
            c.Rect(48, 10, 32, 16, 5);
            c.Rect(54, 16, 20, 6, 0, true);
            c.Rect(18, 24, 92, 14, 5);
            c.Poly(26, 44, 102, 44, 92, 118, 36, 118);
            c.Line(50, 56, 52, 106, 6, true);
            c.Line(64, 56, 64, 106, 6, true);
            c.Line(78, 56, 76, 106, 6, true);
        }

        private static void Cube(SymbolCanvas c, Color color)
        {
            Color ink = c.Ink;
            color.a = 1f;
            // White outline first (slightly bigger), then the three faces in light, mid and dark shades.
            c.Poly(64, 6, 118, 34, 118, 94, 64, 122, 10, 94, 10, 34);
            c.Ink = Color.Lerp(color, Color.white, 0.35f);
            c.Poly(64, 16, 106, 38, 64, 60, 22, 38);
            c.Ink = color;
            c.Poly(18, 46, 60, 68, 60, 112, 18, 90);
            c.Ink = Color.Lerp(color, Color.black, 0.3f);
            c.Poly(68, 68, 110, 46, 110, 90, 68, 112);
            c.Ink = ink;
        }

        private static void Sofa(SymbolCanvas c)
        {
            c.Rect(26, 30, 76, 40, 10);  // back
            c.Rect(18, 66, 92, 26, 6);   // seat
            c.Rect(8, 50, 22, 46, 9);    // arms
            c.Rect(98, 50, 22, 46, 9);
            c.Rect(16, 94, 12, 18, 3);   // legs
            c.Rect(100, 94, 12, 18, 3);
        }

        private static void Ball(SymbolCanvas c)
        {
            c.Circle(64, 64, 50);
            c.Arc(64, 64, 50, 6, 0, 360, true);
            c.Circle(64, 64, 47);
            c.Arc(20, 64, 40, 7, -60, 60, true);
            c.Arc(108, 64, 40, 7, 120, 240, true);
            c.Line(14, 64, 114, 64, 7, true);
        }

        private static void Paw(SymbolCanvas c)
        {
            c.Ellipse(64, 86, 30, 25);
            c.Ellipse(28, 56, 12, 15);
            c.Ellipse(50, 32, 12, 15);
            c.Ellipse(78, 32, 12, 15);
            c.Ellipse(100, 56, 12, 15);
        }

        private static void Wand(SymbolCanvas c)
        {
            c.Line(18, 110, 72, 56, 14);
            c.Star(86, 42, 36, 15);
            c.Circle(30, 30, 6);
            c.Circle(112, 92, 6);
            c.Circle(104, 112, 4);
        }

        private static void Hammer(SymbolCanvas c)
        {
            c.Line(64, 46, 64, 118, 16);
            c.Rect(26, 16, 76, 32, 6);
            c.Rect(96, 22, 14, 20, 3);
        }
    }
}
