using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    /// <summary>
    /// Furniture and toys made in code for Skylar's house: the pieces no model pack has (her colors are orange and
    /// green; she loves cats, dinosaurs, monster trucks and her redtail catfish). Sizes are in meters for a Wobbly
    /// (about 1.4 tall). Every piece stands on y = 0, centered on X and Z, front toward +Z.
    /// </summary>
    public static class SkylarFurniture
    {
        public static readonly string[] Ids =
        {
            "kidtable", "kidchair", "beanbag", "catbed", "cattree", "fishrug", "monstertruck", "dinotoy", "redtailplush", "trampoline"
        };

        public static readonly Rgb Orange = new Rgb(255, 128, 13);
        public static readonly Rgb Green = new Rgb(64, 170, 60);
        public static readonly Rgb Cream = new Rgb(245, 235, 205);
        public static readonly Rgb Dark = new Rgb(45, 45, 50);
        public static readonly Rgb White = new Rgb(255, 255, 255);
        public static readonly Rgb Black = new Rgb(10, 10, 10);
        private static readonly V3 One = new V3(1f, 1f, 1f);

        public static string NiceName(string id)
        {
            switch (id)
            {
                case "kidtable": return "Kids' table";
                case "kidchair": return "Kids' chair";
                case "beanbag": return "Bean bag";
                case "catbed": return "Cat bed";
                case "cattree": return "Cat tree";
                case "fishrug": return "Fish rug";
                case "monstertruck": return "Monster truck toy";
                case "dinotoy": return "Dinosaur toy";
                case "redtailplush": return "Redtail plush";
                case "trampoline": return "Trampoline rug";
                default: return id;
            }
        }

        public static List<ColoredMesh> Build(string id)
        {
            var p = new List<ColoredMesh>();
            switch (id)
            {
                case "kidtable": KidTable(p); break;
                case "kidchair": KidChair(p); break;
                case "beanbag": BeanBag(p); break;
                case "catbed": CatBed(p); break;
                case "cattree": CatTree(p); break;
                case "fishrug": FishRug(p); break;
                case "monstertruck": MonsterTruck(p); break;
                case "dinotoy": DinoToy(p); break;
                case "redtailplush": RedtailPlush(p); break;
                case "trampoline": TrampolineRug(p); break;
                default: throw new ArgumentException("Unknown furniture id: " + id, nameof(id));
            }
            return OnTheFloor(p);
        }

        private static void KidTable(List<ColoredMesh> p)
        {
            p.Add(Box(Orange, 1.0f, 0.06f, 0.7f, 0f, 0.47f, 0f));
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    p.Add(Post(Green, 0.045f, 0.44f, 0.42f * x, 0f, 0.27f * z));
        }

        private static void KidChair(List<ColoredMesh> p)
        {
            p.Add(Box(Green, 0.42f, 0.05f, 0.42f, 0f, 0.3f, 0f));
            p.Add(Box(Green, 0.42f, 0.36f, 0.05f, 0f, 0.55f, -0.185f));
            p.Add(Box(Orange, 0.3f, 0.08f, 0.055f, 0f, 0.6f, -0.185f)); // a stripe on the backrest
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    p.Add(Post(Orange, 0.03f, 0.28f, 0.17f * x, 0f, 0.17f * z));
        }

        private static void BeanBag(List<ColoredMesh> p)
        {
            p.Add(Blob(Green, 0.48f, 0.26f, 0.48f, 0f, 0.26f, 0f));
            p.Add(Blob(Green, 0.36f, 0.24f, 0.3f, 0f, 0.46f, -0.14f));
            p.Add(Blob(Orange, 0.07f, 0.04f, 0.07f, 0f, 0.69f, -0.14f)); // the little tie on top
        }

        private static void CatBed(List<ColoredMesh> p)
        {
            var ring = new List<V3>();
            var radii = new List<float>();
            for (int i = 0; i <= 20; i++)
            {
                double a = i * Math.PI * 2 / 20;
                ring.Add(new V3((float)Math.Cos(a) * 0.38f, 0.11f, (float)Math.Sin(a) * 0.38f));
                radii.Add(0.11f);
            }
            p.Add(new ColoredMesh(Shapes.Tube(ring, radii, 10), Orange));
            p.Add(Blob(Cream, 0.36f, 0.07f, 0.36f, 0f, 0.08f, 0f));
            p.Add(Blob(Green, 0.08f, 0.05f, 0.05f, 0.1f, 0.15f, 0.1f)); // a toy mouse
        }

        private static void CatTree(List<ColoredMesh> p)
        {
            p.Add(Box(Green, 0.7f, 0.06f, 0.7f, 0f, 0.03f, 0f));
            p.Add(Post(Cream, 0.07f, 1.25f, 0f, 0.06f, 0f));
            p.Add(Post(Orange, 0.3f, 0.05f, 0.18f, 0.6f, 0f));
            p.Add(Post(Orange, 0.28f, 0.05f, 0f, 1.3f, 0f));
            p.Add(Blob(Cream, 0.24f, 0.05f, 0.24f, 0f, 1.37f, 0f));
            // A ball on a string, to bat at.
            p.Add(Post(Dark, 0.008f, 0.25f, 0.4f, 0.35f, 0f));
            p.Add(Blob(Green, 0.06f, 0.06f, 0.06f, 0.4f, 0.31f, 0f));
        }

        private static void FishRug(List<ColoredMesh> p)
        {
            // Outline in XY (nose toward +X), then laid flat.
            var outline = new List<V3>();
            float[] xy =
            {
                0.6f, 0f, 0.5f, 0.2f, 0.25f, 0.32f, -0.05f, 0.3f, -0.3f, 0.14f, -0.6f, 0.36f, -0.52f, 0f,
                -0.6f, -0.36f, -0.3f, -0.14f, -0.05f, -0.3f, 0.25f, -0.32f, 0.5f, -0.2f
            };
            for (int i = 0; i + 1 < xy.Length; i += 2) outline.Add(new V3(xy[i] + 0.06f, xy[i + 1], 0f));
            p.Add(new ColoredMesh(Shapes.Extrude(outline, 0.03f).Transformed(One, 0f, 90f, new V3(0f, 0.015f, 0f)), Orange));
            p.Add(Post(Black, 0.035f, 0.035f, 0.42f, 0f, -0.1f));
            p.Add(Box(Green, 0.05f, 0.032f, 0.5f, 0.1f, 0.016f, 0f));   // stripes
            p.Add(Box(Green, 0.05f, 0.032f, 0.4f, -0.1f, 0.016f, 0f));
        }

        private static void MonsterTruck(List<ColoredMesh> p)
        {
            p.Add(Box(Orange, 0.36f, 0.16f, 0.74f, 0f, 0.45f, 0f));
            p.Add(Box(Green, 0.32f, 0.17f, 0.3f, 0f, 0.61f, -0.06f));
            p.Add(Box(Dark, 0.33f, 0.1f, 0.2f, 0f, 0.62f, -0.06f));        // windows
            p.Add(Box(Green, 0.38f, 0.04f, 0.5f, 0f, 0.5f, 0f));           // a green stripe
            p.Add(Box(Dark, 0.3f, 0.06f, 0.6f, 0f, 0.34f, 0f));            // chassis
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    p.Add(Wheel(Dark, 0.19f, 0.16f, 0.25f * x, 0.19f, 0.25f * z));
                    p.Add(Wheel(Orange, 0.08f, 0.17f, 0.25f * x, 0.19f, 0.25f * z));
                }
            }
        }

        private static void DinoToy(List<ColoredMesh> p)
        {
            p.Add(Blob(Green, 0.2f, 0.22f, 0.3f, 0f, 0.36f, 0f));
            p.Add(Blob(Green, 0.14f, 0.13f, 0.2f, 0f, 0.62f, 0.26f));
            p.Add(Blob(Cream, 0.15f, 0.16f, 0.22f, 0f, 0.3f, 0.06f));     // belly
            var tail = new List<V3> { new V3(0f, 0.32f, -0.2f), new V3(0f, 0.22f, -0.45f), new V3(0f, 0.1f, -0.66f), new V3(0f, 0.04f, -0.8f) };
            p.Add(new ColoredMesh(Shapes.Tube(tail, new List<float> { 0.15f, 0.1f, 0.05f, 0f }, 12), Green));
            for (int s = -1; s <= 1; s += 2)
            {
                p.Add(Post(Green, 0.075f, 0.22f, 0.12f * s, 0f, 0.02f));
                p.Add(Blob(White, 0.035f, 0.04f, 0.03f, 0.085f * s, 0.68f, 0.38f));
                p.Add(Blob(Black, 0.018f, 0.022f, 0.018f, 0.09f * s, 0.685f, 0.405f));
                p.Add(Blob(Green, 0.04f, 0.09f, 0.04f, 0.16f * s, 0.42f, 0.22f)); // little arms
            }
            float[] spineZ = { 0.2f, 0.02f, -0.16f, -0.36f, -0.54f };
            float[] spineY = { 0.72f, 0.56f, 0.5f, 0.32f, 0.18f };
            for (int i = 0; i < spineZ.Length; i++)
                p.Add(new ColoredMesh(Shapes.Cone(0.05f, 0.11f, 10).Transformed(One, 0f, 0f, new V3(0f, spineY[i], spineZ[i])), Orange));
        }

        private static void RedtailPlush(List<ColoredMesh> p)
        {
            foreach (ColoredMesh part in RedtailModel.Build())
                p.Add(new ColoredMesh(part.Mesh.Transformed(new V3(0.8f, 0.8f, 0.8f), 0f, 0f, V3.Zero), part.Color));
        }

        private static void TrampolineRug(List<ColoredMesh> p)
        {
            p.Add(Post(Green, 0.8f, 0.07f, 0f, 0f, 0f));
            p.Add(Post(Orange, 0.66f, 0.03f, 0f, 0.06f, 0f));
            // A star of stripes on top, so it reads as "bouncy".
            p.Add(Box(Green, 1.0f, 0.02f, 0.1f, 0f, 0.1f, 0f));
            p.Add(Box(Green, 0.1f, 0.02f, 1.0f, 0f, 0.1f, 0f));
        }

        // ---- Building blocks ----

        /// <summary>A box: size (w, h, d), centered at (cx, cy, cz).</summary>
        private static ColoredMesh Box(Rgb color, float w, float h, float d, float cx, float cy, float cz)
        {
            var outline = new List<V3>
            {
                new V3(-w / 2f, -h / 2f, 0f), new V3(w / 2f, -h / 2f, 0f), new V3(w / 2f, h / 2f, 0f), new V3(-w / 2f, h / 2f, 0f)
            };
            return new ColoredMesh(Shapes.Extrude(outline, d).Transformed(One, 0f, 0f, new V3(cx, cy, cz)), color);
        }

        /// <summary>An upright round post: its bottom center is at (x, y, z).</summary>
        private static ColoredMesh Post(Rgb color, float radius, float height, float x, float y, float z)
        {
            return new ColoredMesh(Shapes.Cylinder(radius, height, 16).Transformed(One, 0f, 0f, new V3(x, y, z)), color);
        }

        private static ColoredMesh Blob(Rgb color, float rx, float ry, float rz, float x, float y, float z)
        {
            return new ColoredMesh(Shapes.Ellipsoid(new V3(rx, ry, rz), 18, 10).Transformed(One, 0f, 0f, new V3(x, y, z)), color);
        }

        /// <summary>A wheel whose axle runs along X, centered at (x, y, z).</summary>
        private static ColoredMesh Wheel(Rgb color, float radius, float width, float x, float y, float z)
        {
            ShapeMesh upright = Shapes.Cylinder(radius, width, 18).Transformed(One, 0f, 0f, new V3(0f, -width / 2f, 0f));
            ShapeMesh alongZ = upright.Transformed(One, 0f, 90f, V3.Zero);
            return new ColoredMesh(alongZ.Transformed(One, 90f, 0f, new V3(x, y, z)), color);
        }

        /// <summary>Moves everything so the lowest point is on y = 0 and it is centered on X and Z.</summary>
        private static List<ColoredMesh> OnTheFloor(List<ColoredMesh> parts)
        {
            if (parts.Count == 0) return parts;
            V3 min = parts[0].Mesh.Min, max = parts[0].Mesh.Max;
            foreach (ColoredMesh part in parts)
            {
                V3 a = part.Mesh.Min, b = part.Mesh.Max;
                min = new V3(Math.Min(min.X, a.X), Math.Min(min.Y, a.Y), Math.Min(min.Z, a.Z));
                max = new V3(Math.Max(max.X, b.X), Math.Max(max.Y, b.Y), Math.Max(max.Z, b.Z));
            }
            var shift = new V3(-(min.X + max.X) / 2f, -min.Y, -(min.Z + max.Z) / 2f);
            var moved = new List<ColoredMesh>();
            foreach (ColoredMesh part in parts)
                moved.Add(new ColoredMesh(part.Mesh.Transformed(One, 0f, 0f, shift), part.Color));
            return moved;
        }
    }
}
