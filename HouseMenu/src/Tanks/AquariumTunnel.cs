using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Skylar's aquarium tunnel (2026-11-25, after seeing real redtail catfish at the aquarium in San Francisco):
    /// a glass tunnel you walk (or drive) through, inside a big tank, with redtails swimming overhead and beside
    /// you, a sunken log across the top, and plants. Also a planted wall tank like the one she leaned on.
    /// Everything is look-only except the walkway, the glass (so nobody walks into the water) and the frame.
    /// Local space: the tunnel runs along Z, centered on the placement point; +Y is up.
    /// </summary>
    internal static class AquariumTunnel
    {
        private const float Length = 14f;      // along the tunnel
        private const float Width = 12f;       // the whole tank
        private const float Height = 7f;
        private const float Radius = 2.6f;     // the tunnel's inside: roomy for Wobblies, and a catfish car fits
        private const int Segments = 12;

        private static readonly Color Water = new Color(0.25f, 0.7f, 0.75f, 0.2f);
        private static readonly Color Sand = new Color(0.86f, 0.78f, 0.58f);
        private static readonly Color Wood = new Color(0.42f, 0.3f, 0.2f);
        private static readonly Color Stone = new Color(0.3f, 0.34f, 0.36f);
        private static readonly Color Leaf = new Color(0.25f, 0.72f, 0.22f);
        private static readonly Color LeafDark = new Color(0.12f, 0.5f, 0.2f);

        public static GameObject Build(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Aquarium tunnel");
            root.transform.SetPositionAndRotation(position, rotation);
            Transform t = root.transform;
            var rng = new System.Random(7);
            Material water = TankMaterials.Tinted(Water);
            Material sand = TankMaterials.Solid(Sand);
            Material stone = TankMaterials.Solid(Stone);

            // The walkway through the middle, and the tank's sandy floor on both sides.
            FishTank.Box(t, "walkway", new Vector3(0f, 0.06f, 0f), new Vector3(Radius * 2f - 0.2f, 0.12f, Length + 2f), TankMaterials.Solid(Wood), true);
            float sideWidth = Width / 2f - Radius;
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "sand", new Vector3(s * (Radius + sideWidth / 2f), 0.1f, 0f), new Vector3(sideWidth, 0.2f, Length), sand, true);
            }

            // The glass arch: one see-through mesh, and a ring of thin solid boxes so it can be leaned on.
            var arch = new GameObject("glass arch");
            arch.transform.SetParent(t, false);
            arch.AddComponent<MeshFilter>().sharedMesh = ArchMesh();
            arch.AddComponent<MeshRenderer>().sharedMaterial = water;
            for (int i = 0; i < Segments; i++)
            {
                float a0 = Mathf.PI * i / Segments, a1 = Mathf.PI * (i + 1) / Segments, mid = (a0 + a1) / 2f;
                var solid = new GameObject("arch solid");
                solid.transform.SetParent(t, false);
                solid.transform.localPosition = new Vector3(Mathf.Cos(mid) * (Radius + 0.08f), Mathf.Sin(mid) * (Radius + 0.08f), 0f);
                solid.transform.localRotation = Quaternion.Euler(0f, 0f, mid * Mathf.Rad2Deg + 90f);
                solid.AddComponent<BoxCollider>().size = new Vector3(2f * Radius * Mathf.Sin((a1 - a0) / 2f) + 0.12f, 0.16f, Length);
            }

            // The tank's outside glass (sides and a water surface on top), and stone end walls around each doorway.
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "side glass", new Vector3(s * Width / 2f, Height / 2f, 0f), new Vector3(0.15f, Height, Length), water, true);
                // End wall: one piece each side of the doorway, and one above it.
                float z = s * Length / 2f;
                for (int x = -1; x <= 1; x += 2)
                {
                    FishTank.Box(t, "end wall", new Vector3(x * (Radius + sideWidth / 2f + 0.1f), Height / 2f, z), new Vector3(sideWidth - 0.2f, Height, 0.3f), stone, true);
                }
                FishTank.Box(t, "end wall top", new Vector3(0f, (Height + Radius) / 2f + 0.15f, z), new Vector3(Radius * 2f + 0.4f, Height - Radius - 0.3f, 0.3f), stone, true);
                var portal = new GameObject("doorway arch");
                portal.transform.SetParent(t, false);
                portal.transform.localPosition = new Vector3(0f, 0f, z);
                portal.AddComponent<MeshFilter>().sharedMesh = PortalMesh();
                portal.AddComponent<MeshRenderer>().sharedMaterial = stone;
            }
            FishTank.Box(t, "water surface", new Vector3(0f, Height - 0.05f, 0f), new Vector3(Width, 0.06f, Length), water, false);

            // A sunken log lying across the top of the tunnel, like the real one.
            Log(t, new Vector3(-4.6f, 2.2f, -2.5f), new Vector3(4.4f, 4.3f, 1.5f), 0.55f);
            Log(t, new Vector3(3.6f, 0.5f, 4.5f), new Vector3(5.4f, 2.6f, 2.2f), 0.3f);

            // Plants on the sand, both sides.
            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (Radius + 0.7f + (float)rng.NextDouble() * (sideWidth - 1.2f));
                float z = ((float)rng.NextDouble() - 0.5f) * (Length - 1.5f);
                Plant(t, new Vector3(x, 0.2f, z), 0.9f + (float)rng.NextDouble() * 2.2f, rng);
            }

            // The redtails. Big ones cross right over the tunnel; smaller ones cruise along the sides.
            float[] sizes = { 3.0f, 2.6f, 2.2f, 2.4f, 1.8f, 2.0f, 1.5f, 1.6f, 1.3f, 1.2f };
            for (int i = 0; i < sizes.Length; i++)
            {
                bool overhead = i < 6;
                GameObject fish = ShapeModels.Redtail();
                var holder = new GameObject("redtail");
                holder.transform.SetParent(t, false);
                fish.transform.SetParent(holder.transform, false);
                fish.transform.localScale = Vector3.one * sizes[i];
                Swim swim = holder.AddComponent<Swim>();
                swim.Model = fish.transform;
                swim.Angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                swim.Bob = 0.25f;
                if (overhead)
                {
                    swim.Center = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.5f, Radius + 0.9f + i * 0.5f, ((float)rng.NextDouble() - 0.5f) * 3f);
                    swim.RadiusX = 3.2f + (float)rng.NextDouble() * 1.6f;
                    swim.RadiusZ = 3.5f + (float)rng.NextDouble() * 2.3f;
                    swim.Speed = (i % 2 == 0 ? 1f : -1f) * (0.16f + (float)rng.NextDouble() * 0.12f);
                }
                else
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    swim.Center = new Vector3(side * (Radius + sideWidth / 2f), 1.2f + (float)rng.NextDouble() * 1.2f, 0f);
                    swim.RadiusX = sideWidth / 2f - 0.9f;
                    swim.RadiusZ = Length / 2f - 1.6f;
                    swim.Speed = side * (0.2f + (float)rng.NextDouble() * 0.12f);
                }
            }
            return root;
        }

        /// <summary>A planted wall tank: a dark frame, glass, sand, lots of plants, branches and a few little fish.</summary>
        public static GameObject BuildPlanted(Vector3 position, Quaternion rotation)
        {
            const float w = 5f, h = 2.2f, d = 1.4f, lift = 0.9f;
            var root = new GameObject("Planted tank");
            root.transform.SetPositionAndRotation(position, rotation);
            Transform t = root.transform;
            var rng = new System.Random(11);
            Material frame = TankMaterials.Solid(new Color(0.14f, 0.11f, 0.1f));
            Material glass = TankMaterials.Tinted(new Color(0.55f, 0.9f, 0.75f, 0.16f));

            FishTank.Box(t, "stand", new Vector3(0f, lift / 2f, 0f), new Vector3(w + 0.3f, lift, d + 0.2f), frame, true);
            FishTank.Box(t, "back", new Vector3(0f, lift + h / 2f, -d / 2f), new Vector3(w + 0.3f, h + 0.3f, 0.12f), frame, true);
            FishTank.Box(t, "top", new Vector3(0f, lift + h + 0.1f, 0f), new Vector3(w + 0.3f, 0.2f, d + 0.2f), frame, true);
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "end", new Vector3(s * (w / 2f + 0.08f), lift + h / 2f, 0f), new Vector3(0.14f, h, d + 0.2f), frame, true);
            }
            FishTank.Box(t, "glass", new Vector3(0f, lift + h / 2f, d / 2f), new Vector3(w, h, 0.06f), glass, true);
            FishTank.Box(t, "sand", new Vector3(0f, lift + 0.08f, 0f), new Vector3(w, 0.16f, d), TankMaterials.Solid(Sand), false);
            var glow = new GameObject("tank light");
            glow.transform.SetParent(t, false);
            glow.transform.localPosition = new Vector3(0f, lift + h - 0.2f, 0f);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.75f, 1f, 0.8f);
            light.range = 6f;
            light.intensity = 1.4f;

            for (int i = 0; i < 34; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * (w - 0.5f);
                float z = ((float)rng.NextDouble() - 0.5f) * (d - 0.4f);
                // Tall bushy ones at the back, shorter in front, like a real planted tank.
                float tall = z < 0f ? 0.9f + (float)rng.NextDouble() * 1.0f : 0.35f + (float)rng.NextDouble() * 0.6f;
                Plant(t, new Vector3(x, lift + 0.16f, z), tall, rng);
            }
            Log(t, new Vector3(-1.9f, lift + 0.2f, 0.1f), new Vector3(-0.4f, lift + 1.5f, -0.3f), 0.07f);
            Log(t, new Vector3(-0.9f, lift + 0.25f, 0.3f), new Vector3(0.8f, lift + 1.1f, 0f), 0.06f);
            Log(t, new Vector3(1.9f, lift + 0.2f, -0.2f), new Vector3(0.9f, lift + 1.4f, 0.2f), 0.06f);

            Color[] fishColors = { new Color(1f, 0.45f, 0.1f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.25f, 0.25f), new Color(1f, 0.9f, 0.2f) };
            for (int i = 0; i < 9; i++)
            {
                var holder = new GameObject("little fish");
                holder.transform.SetParent(t, false);
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(holder.transform, false);
                body.transform.localScale = new Vector3(0.05f, 0.09f, 0.2f);
                body.GetComponent<Renderer>().sharedMaterial = TankMaterials.Solid(fishColors[i % fishColors.Length]);
                Swim swim = holder.AddComponent<Swim>();
                swim.Center = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.5f, lift + 0.5f + (float)rng.NextDouble() * (h - 0.9f), 0f);
                swim.RadiusX = 1.0f + (float)rng.NextDouble() * 1.1f;
                swim.RadiusZ = 0.2f + (float)rng.NextDouble() * 0.25f;
                swim.Speed = (i % 2 == 0 ? 1f : -1f) * (0.5f + (float)rng.NextDouble() * 0.5f);
                swim.Angle = (float)rng.NextDouble() * 6.28f;
                swim.Bob = 0.06f;
            }
            return root;
        }

        /// <summary>
        /// The half-full tank she watched: sand and rocks under the water, branches reaching up out of it with
        /// hanging leaves, and little silver fish swimming just under the surface.
        /// </summary>
        public static GameObject BuildMangrove(Vector3 position, Quaternion rotation)
        {
            const float w = 5f, h = 3f, d = 1.8f, lift = 0.8f, waterTop = 1.4f;
            var root = new GameObject("Mangrove tank");
            root.transform.SetPositionAndRotation(position, rotation);
            Transform t = root.transform;
            var rng = new System.Random(23);
            Material frame = TankMaterials.Solid(new Color(0.14f, 0.11f, 0.1f));
            Material glass = TankMaterials.Tinted(new Color(0.8f, 0.95f, 1f, 0.1f));
            Material water = TankMaterials.Tinted(new Color(0.45f, 0.75f, 0.6f, 0.22f));
            Material rock = TankMaterials.Solid(new Color(0.42f, 0.42f, 0.4f));
            Material leaf = TankMaterials.Solid(Leaf);

            FishTank.Box(t, "stand", new Vector3(0f, lift / 2f, 0f), new Vector3(w + 0.3f, lift, d + 0.2f), frame, true);
            FishTank.Box(t, "back", new Vector3(0f, lift + h / 2f, -d / 2f), new Vector3(w + 0.3f, h, 0.12f), frame, true);
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "end glass", new Vector3(s * w / 2f, lift + h / 2f, 0f), new Vector3(0.06f, h, d), glass, true);
            }
            FishTank.Box(t, "front glass", new Vector3(0f, lift + h / 2f, d / 2f), new Vector3(w, h, 0.06f), glass, true);
            FishTank.Box(t, "sand", new Vector3(0f, lift + 0.1f, 0f), new Vector3(w - 0.1f, 0.2f, d - 0.1f), TankMaterials.Solid(new Color(0.9f, 0.82f, 0.5f)), false);
            FishTank.Box(t, "water", new Vector3(0f, lift + waterTop / 2f + 0.1f, 0f), new Vector3(w - 0.14f, waterTop - 0.2f, d - 0.14f), water, false);

            for (int i = 0; i < 9; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(stone.GetComponent<Collider>());
                stone.name = "rock";
                stone.transform.SetParent(t, false);
                float size = 0.25f + (float)rng.NextDouble() * 0.4f;
                stone.transform.localPosition = new Vector3((i < 5 ? 1f : -1f) * (1.0f + (float)rng.NextDouble() * 1.3f), lift + 0.2f + size * 0.3f, ((float)rng.NextDouble() - 0.5f) * (d - 0.6f));
                stone.transform.localScale = new Vector3(size * 1.3f, size * 0.8f, size);
                stone.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, 0f);
                stone.GetComponent<Renderer>().sharedMaterial = rock;
            }

            // Mangrove branches: roots spread on the sand, a stem up through the surface, and drooping leafy twigs.
            float[] stems = { -1.9f, -0.5f, 0.6f, 1.9f };
            foreach (float x in stems)
            {
                float z = -0.4f + (float)rng.NextDouble() * 0.5f;
                Vector3 crown = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 0.4f, lift + 2.3f + (float)rng.NextDouble() * 0.4f, z);
                Vector3 knee = new Vector3(x, lift + 1.0f, z);
                Log(t, knee, crown, 0.05f);
                for (int r = 0; r < 4; r++)
                {
                    float a = r * 1.57f + (float)rng.NextDouble();
                    Log(t, new Vector3(x + Mathf.Cos(a) * 0.5f, lift + 0.2f, z + Mathf.Sin(a) * 0.35f), knee, 0.03f);
                }
                for (int b = 0; b < 4; b++)
                {
                    float a = b * 1.57f + (float)rng.NextDouble();
                    Vector3 tip = crown + new Vector3(Mathf.Cos(a) * 0.7f, -0.5f - (float)rng.NextDouble() * 0.5f, Mathf.Sin(a) * 0.3f);
                    Log(t, crown, tip, 0.02f);
                    for (int l = 0; l < 6; l++)
                    {
                        GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Object.DestroyImmediate(bit.GetComponent<Collider>());
                        bit.name = "leaf";
                        bit.transform.SetParent(t, false);
                        bit.transform.localPosition = Vector3.Lerp(crown, tip, (l + 1) / 6f) + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.16f, -0.05f - (float)rng.NextDouble() * 0.12f, ((float)rng.NextDouble() - 0.5f) * 0.16f);
                        bit.transform.localScale = new Vector3(0.12f, 0.05f, 0.09f);
                        bit.GetComponent<Renderer>().sharedMaterial = leaf;
                    }
                }
            }
            for (int i = 0; i < 6; i++) Plant(t, new Vector3(((float)rng.NextDouble() - 0.5f) * (w - 1f), lift + 0.2f, 0.2f + (float)rng.NextDouble() * 0.5f), 0.4f + (float)rng.NextDouble() * 0.4f, rng);

            // Slim silver fish just under the surface.
            Material silver = TankMaterials.Solid(new Color(0.82f, 0.86f, 0.88f));
            for (int i = 0; i < 7; i++)
            {
                var holder = new GameObject("silver fish");
                holder.transform.SetParent(t, false);
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(holder.transform, false);
                body.transform.localScale = new Vector3(0.04f, 0.05f, 0.3f);
                body.GetComponent<Renderer>().sharedMaterial = silver;
                Swim swim = holder.AddComponent<Swim>();
                swim.Center = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.2f, lift + waterTop - 0.12f - (float)rng.NextDouble() * 0.15f, 0.15f);
                swim.RadiusX = 1.2f + (float)rng.NextDouble() * 0.9f;
                swim.RadiusZ = 0.3f + (float)rng.NextDouble() * 0.3f;
                swim.Speed = (i % 2 == 0 ? 1f : -1f) * (0.35f + (float)rng.NextDouble() * 0.3f);
                swim.Angle = (float)rng.NextDouble() * 6.28f;
                swim.Bob = 0.02f;
            }
            return root;
        }

        // ---- Pieces ----

        /// <summary>A leafy water plant: a few long leaves fanning up from one spot, swaying.</summary>
        private static void Plant(Transform parent, Vector3 at, float tall, System.Random rng)
        {
            var plant = new GameObject("plant");
            plant.transform.SetParent(parent, false);
            plant.transform.localPosition = at;
            plant.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            Material leaf = TankMaterials.Solid(rng.Next(3) == 0 ? LeafDark : Leaf);
            int leaves = 3 + rng.Next(4);
            for (int i = 0; i < leaves; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(blade.GetComponent<Collider>());
                blade.name = "leaf";
                blade.transform.SetParent(plant.transform, false);
                float lean = 8f + (float)rng.NextDouble() * 26f;
                float h = tall * (0.6f + (float)rng.NextDouble() * 0.4f);
                blade.transform.localRotation = Quaternion.Euler(lean, i * 360f / leaves, 0f);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, h / 2f, 0f);
                blade.transform.localScale = new Vector3(0.05f + tall * 0.04f, h, 0.16f + tall * 0.07f);
                blade.GetComponent<Renderer>().sharedMaterial = leaf;
            }
            Sway sway = plant.AddComponent<Sway>();
            sway.Amount = 5f;
            sway.Speed = 0.5f + (float)rng.NextDouble() * 0.5f;
            sway.Phase = (float)rng.NextDouble() * 6f;
        }

        /// <summary>A branch or sunken log between two points.</summary>
        private static void Log(Transform parent, Vector3 from, Vector3 to, float radius)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(log.GetComponent<Collider>());
            log.name = "log";
            log.transform.SetParent(parent, false);
            Vector3 along = to - from;
            log.transform.localPosition = (from + to) / 2f;
            log.transform.localRotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
            log.transform.localScale = new Vector3(radius * 2f, along.magnitude / 2f, radius * 2f); // a Unity cylinder is 2 tall
            log.GetComponent<Renderer>().sharedMaterial = TankMaterials.Solid(Wood);
        }

        private static Mesh _arch, _portal;

        /// <summary>The half-pipe of glass, seen from inside and outside.</summary>
        private static Mesh ArchMesh()
        {
            if (_arch != null) return _arch;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < Segments; i++)
            {
                float a0 = Mathf.PI * i / Segments, a1 = Mathf.PI * (i + 1) / Segments;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * Radius, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * Radius;
                Vector3 n = new Vector3(Mathf.Cos((a0 + a1) / 2f), Mathf.Sin((a0 + a1) / 2f), 0f);
                Vector3 back = Vector3.back * (Length / 2f), front = Vector3.forward * (Length / 2f);
                Quad(verts, normals, tris, p0 + back, p1 + back, p1 + front, p0 + front, n);
            }
            _arch = ToMesh("Aquarium arch", verts, normals, tris);
            return _arch;
        }

        /// <summary>The stone around a doorway: fills between the round arch and the square hole in the end wall.</summary>
        private static Mesh PortalMesh()
        {
            if (_portal != null) return _portal;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            float edge = Radius + 0.2f;
            for (int i = 0; i < Segments; i++)
            {
                float a0 = Mathf.PI * i / Segments, a1 = Mathf.PI * (i + 1) / Segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f), d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                // Out to the square: stretch each direction until it touches x = +-edge or y = edge.
                Vector3 s0 = d0 * (edge / Mathf.Max(Mathf.Abs(d0.x), Mathf.Abs(d0.y)));
                Vector3 s1 = d1 * (edge / Mathf.Max(Mathf.Abs(d1.x), Mathf.Abs(d1.y)));
                Quad(verts, normals, tris, d0 * Radius, d1 * Radius, s1, s0, Vector3.forward);
            }
            _portal = ToMesh("Aquarium doorway", verts, normals, tris);
            return _portal;
        }

        private static void Quad(List<Vector3> verts, List<Vector3> normals, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++) normals.Add(normal);
            // Both sides, so it shows from inside and outside.
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1); tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        }

        private static Mesh ToMesh(string name, List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            var m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
