using System;
using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Skylar's BIG aquarium (2026-11-27): five floors, about seven times the floor space of the first one.
    ///
    /// Ideas borrowed from real aquariums: one giant tank rising through the middle of the building that you see
    /// again and again from different depths as you go up (Osaka's Kaiyukan), and a bright planted wetland under
    /// glass at the top (the Florida Aquarium's dome in Tampa). Each floor is its own underwater world with its
    /// own light:
    ///
    ///   5  Rainforest lagoon   daylight under a glass roof; the top of the river tank is an open lagoon with
    ///                          mangrove trees; palms, flowers, butterflies, frogs, lizards and turtles
    ///   4  Rivers, bays and beaches   light and sandy: stingray bay, touch pools, river fish, mangrove roots
    ///   3  Jellyfish and ice          the darkest floor: glowing jellyfish tubes, an ice cave, a glow reef
    ///   2  Coral reef                 bright turquoise with ripples of light on the floor
    ///   1  The deep ocean             dark blue: the open-ocean window, the deep sea, the lobby and gift shop,
    ///                                 and the tunnel under the river
    ///
    /// The RIVER in the middle is the redtails' flooded forest: 36 redtails from the tunnel at the bottom up to
    /// the mangrove roots hanging into the water at the top.
    ///
    /// Interactive (parent's picks): fish come over to a Wobbly at the glass and follow her (TankMind); a feeding
    /// button at every big tank; she can dive into the river from the top floor and float down among the redtails
    /// (SwimZone), walking out through a door at the bottom; touch-pool animals and gift-shop toys are loose.
    /// Getting around: wide ramps (right side), and bounce pads that launch her up a floor (left side).
    /// Local space: entrance toward +Z; floor f is at y = f * 8.
    /// </summary>
    internal static class BigAquarium
    {
        private const float HX = 26f, HZ = 26f, FH = 8f;
        private const int Floors = 5;
        private const float TX = AquariumTunnel.TankWidth / 2f, TZ = AquariumTunnel.TankLength / 2f;
        private const float Top = FH * (Floors - 1);                 // the lagoon's water level, and the top floor
        private const float PadX = -24f;
        private const string Plants = "Assets/Content/Game/Prefabs/Environment/Vegitation/";

        private static readonly Color Orange = new Color(1f, 0.5f, 0.05f);
        private static readonly Color Green = new Color(0.25f, 0.67f, 0.24f);
        private static readonly Color Sand = new Color(0.9f, 0.82f, 0.6f);
        private static readonly Color Outside = new Color(0.78f, 0.86f, 0.9f);
        private static readonly Color Wood = new Color(0.42f, 0.3f, 0.2f);
        private static readonly Color Leaf = new Color(0.25f, 0.72f, 0.22f);

        // The feel of each floor: floor color, wall color.
        private static readonly Color[] FloorColor =
        {
            new Color(0.1f, 0.17f, 0.3f), new Color(0.78f, 0.72f, 0.52f), new Color(0.03f, 0.03f, 0.07f),
            new Color(0.82f, 0.76f, 0.58f), new Color(0.55f, 0.42f, 0.28f)
        };
        private static readonly Color[] WallColor =
        {
            new Color(0.04f, 0.07f, 0.18f), new Color(0.16f, 0.58f, 0.68f), new Color(0.04f, 0.03f, 0.09f),
            new Color(0.72f, 0.88f, 0.8f), new Color(0.8f, 0.9f, 0.85f)
        };

        private static int _fishWanted, _fishLoaded;

        private static float PadZ(int floor) { return floor % 2 == 0 ? -4f : 4f; }

        public static GameObject Build(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Skylar's big aquarium");
            root.transform.SetPositionAndRotation(position, rotation);
            Transform t = root.transform;
            var rng = new System.Random(5150);
            _fishWanted = 0; _fishLoaded = 0;
            int ok = 0;
            ok += Aquarium.Safely("big aquarium shell", () => Shell(t));
            ok += Aquarium.Safely("big aquarium ramps", () => Ramps(t));
            ok += Aquarium.Safely("big aquarium bounce pads", () => BouncePads(t));
            ok += Aquarium.Safely("big aquarium river", () => River(t, rng));
            ok += Aquarium.Safely("big aquarium floor 1", () => DeepOcean(t, rng));
            ok += Aquarium.Safely("big aquarium gift shop", () => GiftShop(t, rng));
            ok += Aquarium.Safely("big aquarium floor 2", () => CoralReef(t, rng));
            ok += Aquarium.Safely("big aquarium floor 3", () => JellyAndIce(t, rng));
            ok += Aquarium.Safely("big aquarium floor 4", () => RiversAndBeaches(t, rng));
            ok += Aquarium.Safely("big aquarium floor 5", () => Rainforest(t, rng));
            Plugin.Log.LogInfo("Big aquarium: built " + ok + " of 10 parts; " + _fishLoaded + " of " + _fishWanted + " animals and plants loaded; " +
                               t.GetComponentsInChildren<Renderer>().Length + " things to draw, " + t.GetComponentsInChildren<Light>().Length + " lights.");
            return root;
        }

        // =====================================================================================================
        // The building
        // =====================================================================================================

        private static void Shell(Transform t)
        {
            Material outside = TankMaterials.Solid(Outside);
            float wallTop = Top; // solid walls up to the top floor; glass above

            FishTank.Box(t, "ground", new Vector3(0f, -0.05f, 0f), new Vector3(HX * 2f, 0.3f, HZ * 2f), TankMaterials.Solid(FloorColor[0]), true);
            for (int f = 1; f < Floors; f++)
            {
                var holes = new List<Rect> { Rect.MinMaxRect(-TX, -TZ, TX, TZ) };
                holes.Add(f < Floors - 1 ? Rect.MinMaxRect(18f, -11f, HX, 8f) : Rect.MinMaxRect(22f, -8f, HX, 8f)); // the ramps
                holes.Add(Rect.MinMaxRect(-25.5f, PadZ(f - 1) - 2.2f, -20.3f, PadZ(f - 1) + 2.2f));                    // above the bounce pad below
                Slab(t, f * FH, holes, TankMaterials.Solid(FloorColor[f]));
            }

            FishTank.Box(t, "back wall", new Vector3(0f, wallTop / 2f, -HZ), new Vector3(HX * 2f + 0.4f, wallTop, 0.4f), outside, true);
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "side wall", new Vector3(s * HX, wallTop / 2f, 0f), new Vector3(0.4f, wallTop, HZ * 2f), outside, true);
            }
            const float door = 4f, doorH = 5.2f;
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "front wall", new Vector3(s * (HX + door) / 2f, wallTop / 2f, HZ), new Vector3(HX - door, wallTop, 0.4f), outside, true);
                FishTank.Box(t, "door post", new Vector3(s * door, doorH / 2f, HZ + 0.15f), new Vector3(0.6f, doorH, 0.8f), TankMaterials.Solid(Orange), true);
            }
            FishTank.Box(t, "over the door", new Vector3(0f, (wallTop + doorH) / 2f, HZ), new Vector3(door * 2f, wallTop - doorH, 0.4f), outside, true);
            FishTank.Box(t, "door top", new Vector3(0f, doorH + 0.3f, HZ + 0.15f), new Vector3(door * 2f + 0.6f, 0.6f, 0.8f), TankMaterials.Solid(Orange), true);
            GameObject mascot = ShapeModels.Redtail();
            mascot.transform.SetParent(t, false);
            mascot.transform.localPosition = new Vector3(0f, doorH + 3f, HZ + 0.9f);
            mascot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            mascot.transform.localScale = Vector3.one * 9f;

            // Inside walls in each floor's own color (thin panels just inside the outer walls).
            for (int f = 0; f < Floors - 1; f++)
            {
                Material m = TankMaterials.Solid(WallColor[f]);
                float y = f * FH + (FH - 0.5f) / 2f, h = FH - 0.5f, inset = 0.27f;
                FishTank.Box(t, "inside wall", new Vector3(0f, y, -HZ + inset), new Vector3(HX * 2f - 0.5f, h, 0.1f), m, false);
                FishTank.Box(t, "inside wall", new Vector3(-HX + inset, y, 0f), new Vector3(0.1f, h, HZ * 2f - 0.5f), m, false);
                FishTank.Box(t, "inside wall", new Vector3(HX - inset, y, 0f), new Vector3(0.1f, h, HZ * 2f - 0.5f), m, false);
                if (f == 0)
                {
                    for (int s = -1; s <= 1; s += 2)
                        FishTank.Box(t, "inside wall", new Vector3(s * (HX + door + 0.4f) / 2f, y, HZ - inset), new Vector3(HX - door - 0.6f, h, 0.1f), m, false);
                }
                else
                {
                    FishTank.Box(t, "inside wall", new Vector3(0f, y, HZ - inset), new Vector3(HX * 2f - 0.5f, h, 0.1f), m, false);
                }
            }

            // The top floor is a glass house: glass walls and a glass pyramid roof, so daylight pours in.
            Material glass = TankMaterials.Tinted(new Color(0.75f, 0.92f, 1f, 0.12f));
            float gy = Top + FH / 2f;
            FishTank.Box(t, "glass wall", new Vector3(0f, gy, -HZ), new Vector3(HX * 2f, FH, 0.2f), glass, true);
            FishTank.Box(t, "glass wall", new Vector3(0f, gy, HZ), new Vector3(HX * 2f, FH, 0.2f), glass, true);
            FishTank.Box(t, "glass wall", new Vector3(-HX, gy, 0f), new Vector3(0.2f, FH, HZ * 2f), glass, true);
            FishTank.Box(t, "glass wall", new Vector3(HX, gy, 0f), new Vector3(0.2f, FH, HZ * 2f), glass, true);
            var roof = new GameObject("glass roof");
            roof.transform.SetParent(t, false);
            roof.AddComponent<MeshFilter>().sharedMesh = PyramidMesh();
            roof.AddComponent<MeshRenderer>().sharedMaterial = glass;
        }

        /// <summary>A floor with rectangular holes in it (x = Rect.x, z = Rect.y). Its top is at height y.</summary>
        private static void Slab(Transform t, float y, List<Rect> holes, Material material)
        {
            // Cut the floor into a grid along every hole edge, then lay one long box per run of solid cells.
            List<float> xs = Cuts(-HX, HX, holes, true), zs = Cuts(-HZ, HZ, holes, false);
            for (int i = 0; i + 1 < xs.Count; i++)
            {
                int j = 0;
                while (j + 1 < zs.Count)
                {
                    if (!Solid(xs, zs, i, j, holes)) { j++; continue; }
                    int k = j;
                    while (k + 2 < zs.Count && Solid(xs, zs, i, k + 1, holes)) k++;
                    float x0 = xs[i], x1 = xs[i + 1], z0 = zs[j], z1 = zs[k + 1];
                    FishTank.Box(t, "floor", new Vector3((x0 + x1) / 2f, y - 0.25f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.5f, z1 - z0), material, true);
                    j = k + 1;
                }
            }
        }

        private static bool Solid(List<float> xs, List<float> zs, int i, int j, List<Rect> holes)
        {
            var mid = new Vector2((xs[i] + xs[i + 1]) / 2f, (zs[j] + zs[j + 1]) / 2f);
            foreach (Rect h in holes) if (h.Contains(mid)) return false;
            return true;
        }

        /// <summary>The sorted grid lines along one direction: the two outer edges plus every hole edge between them.</summary>
        private static List<float> Cuts(float min, float max, List<Rect> holes, bool alongX)
        {
            var all = new List<float> { min, max };
            foreach (Rect h in holes)
            {
                all.Add(Mathf.Clamp(alongX ? h.xMin : h.yMin, min, max));
                all.Add(Mathf.Clamp(alongX ? h.xMax : h.yMax, min, max));
            }
            all.Sort();
            var cuts = new List<float>();
            foreach (float v in all) if (cuts.Count == 0 || v - cuts[cuts.Count - 1] > 0.01f) cuts.Add(v);
            return cuts;
        }

        /// <summary>
        /// Wide ramps on the right side, two flights per floor with a landing halfway. Gentle enough for a
        /// catfish car.
        /// </summary>
        private static void Ramps(Transform t)
        {
            Material deck = TankMaterials.Solid(Orange), rail = TankMaterials.Solid(Green);
            float angle = Mathf.Atan2(FH / 2f, 16f) * Mathf.Rad2Deg, length = Mathf.Sqrt(16f * 16f + (FH / 2f) * (FH / 2f));
            for (int f = 0; f < Floors - 1; f++)
            {
                float y = f * FH;
                // Flight A: from this floor at z = +8 up to the landing at z = -8. Flight B: back up to the next floor.
                GameObject a = FishTank.Box(t, "ramp", new Vector3(20f, y + FH / 4f - 0.15f, 0f), new Vector3(4f, 0.3f, length + 0.3f), deck, true);
                a.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                GameObject b = FishTank.Box(t, "ramp", new Vector3(24f, y + FH * 0.75f - 0.15f, 0f), new Vector3(4f, 0.3f, length + 0.3f), deck, true);
                b.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                FishTank.Box(t, "ramp landing", new Vector3(22f, y + FH / 2f - 0.15f, -9.5f), new Vector3(8f, 0.3f, 3f), deck, true);
                // A rail between the two flights, following each one.
                GameObject ra = FishTank.Box(t, "ramp rail", new Vector3(22f, y + FH / 4f + 0.45f, 0f), new Vector3(0.15f, 0.9f, length), rail, true);
                ra.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                GameObject rb = FishTank.Box(t, "ramp rail", new Vector3(22.15f, y + FH * 0.75f + 0.45f, 0f), new Vector3(0.15f, 0.9f, length), rail, true);
                rb.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                GameObject rc = FishTank.Box(t, "ramp rail", new Vector3(18f, y + FH / 4f + 0.45f, 0f), new Vector3(0.15f, 0.9f, length), rail, true);
                rc.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                FishTank.Box(t, "ramp rail", new Vector3(18f, y + FH / 2f + 0.45f, -9.5f), new Vector3(0.15f, 0.9f, 3f), rail, true);
            }
            // Rails round the openings in the floors, so nobody walks off the edge into the ramp well.
            for (int f = 1; f < Floors; f++)
            {
                float y = f * FH + 0.5f;
                if (f < Floors - 1)
                {
                    FishTank.Box(t, "rail", new Vector3(22f, y, -11f), new Vector3(8f, 1f, 0.15f), rail, true);
                    // Along the aisle side too: without it you could walk under the rising ramp and drop into the well.
                    FishTank.Box(t, "rail", new Vector3(17.9f, y, -1.5f), new Vector3(0.15f, 1f, 19f), rail, true);
                }
                else
                {
                    FishTank.Box(t, "rail", new Vector3(22f, y, 0f), new Vector3(0.15f, 1f, 16f), rail, true);
                    FishTank.Box(t, "rail", new Vector3(24f, y, -8f), new Vector3(4f, 1f, 0.15f), rail, true);
                }
            }
        }

        /// <summary>Bounce pads on the left side: step on one and it throws you up through the hole to the next floor.</summary>
        private static void BouncePads(Transform t)
        {
            for (int f = 0; f < Floors - 1; f++)
            {
                var at = new Vector3(PadX, f * FH + (f == 0 ? 0.1f : 0f), PadZ(f));
                Aquarium.Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 0.04f, new Vector3(2.6f, 0.04f, 2.6f), Quaternion.identity, TankMaterials.Solid(Green));
                GameObject pad = Aquarium.Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 0.1f, new Vector3(2.0f, 0.08f, 2.0f), Quaternion.identity, TankMaterials.Solid(Orange));
                // An up arrow standing beside it, so it reads as "this goes up".
                Aquarium.Shape(t, PrimitiveType.Cube, at + new Vector3(-1.2f, 1.2f, 0f), new Vector3(0.12f, 1.6f, 0.5f), Quaternion.identity, TankMaterials.Solid(Orange));
                Aquarium.Shape(t, PrimitiveType.Cube, at + new Vector3(-1.2f, 2.1f, 0f), new Vector3(0.13f, 0.9f, 0.9f), Quaternion.Euler(45f, 0f, 0f), TankMaterials.Solid(Orange));
                var zone = pad.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.center = new Vector3(0f, 7f, 0f);
                zone.size = new Vector3(0.9f, 14f, 0.9f); // the pad is squashed flat, so this is about 1.1 m tall
                Launcher launcher = pad.AddComponent<Launcher>();
                launcher.Building = t;
                launcher.Rise = FH;
            }
        }

        // =====================================================================================================
        // The river: the redtails' flooded forest, from the tunnel at the bottom to the mangrove lagoon at the top
        // =====================================================================================================

        private static void River(Transform t, System.Random rng)
        {
            var river = new GameObject("Redtail river");
            river.transform.SetParent(t, false);
            Transform r = river.transform;
            var redtails = new List<Swim>();
            AquariumTunnel.BuildInto(r, true, redtails, false);

            // Glass all the way up. On the right side at the bottom there's a way out for swimmers.
            Material glass = TankMaterials.Tinted(new Color(0.25f, 0.7f, 0.75f, 0.16f));
            FishTank.Box(r, "river glass", new Vector3(-TX, Top / 2f, 0f), new Vector3(0.15f, Top, TZ * 2f), glass, true);
            const float d0 = 2.6f, d1 = 5.6f, dh = 3.3f;
            FishTank.Box(r, "river glass", new Vector3(TX, Top / 2f, (-TZ + d0) / 2f), new Vector3(0.15f, Top, d0 + TZ), glass, true);
            FishTank.Box(r, "river glass", new Vector3(TX, Top / 2f, (d1 + TZ) / 2f), new Vector3(0.15f, Top, TZ - d1), glass, true);
            FishTank.Box(r, "river glass", new Vector3(TX, (Top + dh) / 2f, (d0 + d1) / 2f), new Vector3(0.15f, Top - dh, d1 - d0), glass, true);
            Material orange = TankMaterials.Solid(Orange);
            FishTank.Box(r, "way out", new Vector3(TX, dh / 2f, d0), new Vector3(0.4f, dh, 0.25f), orange, true);
            FishTank.Box(r, "way out", new Vector3(TX, dh / 2f, d1), new Vector3(0.4f, dh, 0.25f), orange, true);
            FishTank.Box(r, "way out", new Vector3(TX, dh + 0.12f, (d0 + d1) / 2f), new Vector3(0.4f, 0.25f, d1 - d0 + 0.25f), orange, true);
            float low = AquariumTunnel.TankHeight;
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(r, "river glass", new Vector3(0f, (Top + low) / 2f, s * TZ), new Vector3(TX * 2f, Top - low, 0.15f), glass, true);
            }
            FishTank.Box(r, "lagoon surface", new Vector3(0f, Top - 0.1f, 0f), new Vector3(TX * 2f - 0.2f, 0.05f, TZ * 2f - 0.2f), TankMaterials.Tinted(new Color(0.3f, 0.75f, 0.7f, 0.2f)), false);

            // More redtails, all the way up, in all sizes.
            for (int i = 0; i < 26; i++)
            {
                GameObject fish = ShapeModels.Redtail();
                var holder = new GameObject("redtail");
                holder.transform.SetParent(r, false);
                fish.transform.SetParent(holder.transform, false);
                fish.transform.localScale = Vector3.one * (1.3f + (float)rng.NextDouble() * 2.2f);
                Swim swim = holder.AddComponent<Swim>();
                swim.Model = fish.transform;
                swim.Center = new Vector3(((float)rng.NextDouble() - 0.5f) * 2f, low + 1.5f + i * ((Top - low - 3.5f) / 25f), ((float)rng.NextDouble() - 0.5f) * 2.5f);
                swim.RadiusX = 2.4f + (float)rng.NextDouble() * 2.2f;
                swim.RadiusZ = 3.0f + (float)rng.NextDouble() * 2.6f;
                swim.Speed = (i % 2 == 0 ? 1f : -1f) * (0.15f + (float)rng.NextDouble() * 0.15f);
                swim.Angle = (float)rng.NextDouble() * 6.28f;
                swim.Bob = 0.4f;
                redtails.Add(swim);
            }
            string[] others = { "Pike", "Carp", "Bream", "Trout", "Rainbow Trout", "Eel", "Carp", "Salmon", "Pike", "Bream", "CatFish", "CatFish", "Trout", "Eel" };
            for (int i = 0; i < others.Length; i++)
            {
                Animal(r, others[i], 0.9f + (float)rng.NextDouble() * 0.7f, new Vector3(0f, low + 2f + i * 1.6f, 0f),
                       3.6f + (float)rng.NextDouble() * 1.2f, 4.6f + (float)rng.NextDouble() * 1.3f, (i % 2 == 0 ? -1f : 1f) * (0.28f + (float)rng.NextDouble() * 0.2f), 0.3f, rng, null);
            }

            // Drowned trees standing the whole height of the river.
            float[][] tall = { new[] { -4.2f, 4.6f }, new[] { 4.4f, -4.8f }, new[] { -4.5f, -2.5f } };
            foreach (float[] p in tall)
            {
                AquariumTunnel.Log(r, new Vector3(p[0], 0.2f, p[1]), new Vector3(p[0] + 0.6f, Top - 2.5f, p[1] + 0.5f), 0.3f);
            }

            // The mangroves: trees standing in the lagoon at the top, their arching roots hanging far down into the
            // water where the redtails swim between them (Skylar: the mangroves belong in the redtails' tank).
            Material leaf = TankMaterials.Solid(Leaf), leafDark = TankMaterials.Solid(new Color(0.12f, 0.5f, 0.2f)), blossom = TankMaterials.Solid(new Color(1f, 0.85f, 0.95f));
            float[][] trees = { new[] { -4.4f, -5.2f }, new[] { 4.3f, -5.4f }, new[] { -4.6f, 0.4f }, new[] { 4.5f, 1.2f }, new[] { -4.2f, 5.5f }, new[] { 3.4f, 5.6f }, new[] { 0.2f, -5.8f } };
            foreach (float[] p in trees)
            {
                var knee = new Vector3(p[0], Top + 0.9f, p[1]);
                var crown = knee + new Vector3(((float)rng.NextDouble() - 0.5f) * 1.2f, 3.2f + (float)rng.NextDouble() * 1.4f, ((float)rng.NextDouble() - 0.5f) * 1.2f);
                AquariumTunnel.Log(r, knee, crown, 0.2f);
                int roots = 7 + rng.Next(3);
                for (int i = 0; i < roots; i++)
                {
                    float a = i * 6.28f / roots + (float)rng.NextDouble() * 0.5f, reach = 1.2f + (float)rng.NextDouble() * 1.5f;
                    var elbow = new Vector3(p[0] + Mathf.Cos(a) * reach * 0.6f, Top - 0.4f, p[1] + Mathf.Sin(a) * reach * 0.6f);
                    var foot = new Vector3(Mathf.Clamp(p[0] + Mathf.Cos(a) * reach, -TX + 0.3f, TX - 0.3f), Top - 4f - (float)rng.NextDouble() * 6f,
                                           Mathf.Clamp(p[1] + Mathf.Sin(a) * reach, -TZ + 0.3f, TZ - 0.3f));
                    AquariumTunnel.Log(r, knee, elbow, 0.09f);   // the arch above the water
                    AquariumTunnel.Log(r, elbow, foot, 0.07f);   // the root going down into the river
                }
                for (int i = 0; i < 12; i++)
                {
                    var at = crown + new Vector3(((float)rng.NextDouble() - 0.5f) * 3f, ((float)rng.NextDouble() - 0.3f) * 1.6f, ((float)rng.NextDouble() - 0.5f) * 3f);
                    Aquarium.Shape(r, PrimitiveType.Sphere, at, new Vector3(1.2f, 0.55f, 1.1f), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), i % 3 == 0 ? leafDark : leaf);
                    if (i % 4 == 0) Aquarium.Shape(r, PrimitiveType.Sphere, at + Vector3.up * 0.32f, Vector3.one * 0.22f, Quaternion.identity, blossom);
                }
            }
            // Lily pads between them, and shafts of light going down into the deep water.
            Material pad = TankMaterials.Solid(new Color(0.12f, 0.5f, 0.2f)), ray = TankMaterials.Tinted(new Color(1f, 1f, 0.75f, 0.06f));
            for (int i = 0; i < 18; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * (TX * 2f - 1.5f), z = ((float)rng.NextDouble() - 0.5f) * (TZ * 2f - 1.5f);
                float size = 0.5f + (float)rng.NextDouble() * 0.7f;
                Aquarium.Shape(r, PrimitiveType.Cylinder, new Vector3(x, Top - 0.06f, z), new Vector3(size, 0.012f, size), Quaternion.identity, pad);
                if (i % 5 == 0) Aquarium.Shape(r, PrimitiveType.Sphere, new Vector3(x, Top + 0.02f, z), Vector3.one * 0.25f, Quaternion.identity, TankMaterials.Solid(new Color(1f, 0.55f, 0.75f)));
                if (i % 3 == 0)
                {
                    GameObject shaft = FishTank.Box(r, "light ray", new Vector3(x, Top - 8f, z), new Vector3(0.6f, 15.5f, 0.6f), ray, false);
                    shaft.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 4f);
                }
            }

            // Swim with the redtails: inside the river everything sinks slowly, like a diver.
            var water = new GameObject("river water");
            water.transform.SetParent(r, false);
            var zone = water.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, Top / 2f, 0f);
            zone.size = new Vector3(TX * 2f - 0.5f, Top - 0.3f, TZ * 2f - 0.5f);
            water.AddComponent<SwimZone>();

            // The fish notice visitors on every floor, and each floor has a feeding button on the left of the river.
            TankMind mind = river.AddComponent<TankMind>();
            mind.Fish = redtails;
            mind.Interior = new Bounds(new Vector3(0f, Top / 2f, 0f), new Vector3(TX * 2f - 2.6f, Top - 2.5f, TZ * 2f - 2.6f));
            mind.Reach = 9f;
            mind.Spread = 3.2f;     // the redtails circle a few meters out from her
            mind.KeepLoop = 0.7f;   // and keep most of their lazy loops
            for (int f = 0; f < Floors - 1; f++)
            {
                FeedButton(r, mind, new Vector3(-TX - 1.7f, f * FH + (f == 0 ? 0.1f : 0f), 0f), new Vector3(-TX + 1.8f, f * FH + 3.4f, 0f));
            }
            Aquarium.Sign(t, new Vector3(TX + 1.3f, 0.1f, d1 + 1.4f), -90f, sign => { GameObject f = ShapeModels.Redtail(); f.transform.SetParent(sign, false); f.transform.localScale = Vector3.one * 1.1f; f.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); });

            // The top floor: a diving board over the lagoon.
            FishTank.Box(t, "diving board", new Vector3(0f, Top + 0.45f, TZ - 1.2f), new Vector3(1.6f, 0.14f, 4.4f), orange, true);
            FishTank.Box(t, "diving board stand", new Vector3(0f, Top + 0.2f, TZ + 0.9f), new Vector3(1.8f, 0.4f, 0.6f), TankMaterials.Solid(Green), true);
        }

        // =====================================================================================================
        // Floor 1: the deep ocean, the lobby and the gift shop
        // =====================================================================================================

        private static void DeepOcean(Transform t, System.Random rng)
        {
            const float y = 0.1f;
            // The open ocean: one huge window across the back wall.
            Tank ocean = Tank.Make(t, "open ocean", new Vector3(0f, y, -HZ + 3.1f), 0f, 40f, 5.4f, 6.6f, new Color(0.1f, 0.4f, 0.85f, 0.2f), new Color(0.04f, 0.16f, 0.4f), new Color(0.5f, 0.75f, 1f), 1.3f);
            ocean.Rocks(9, rng);
            ocean.Fish(new[] { "Marlin", "Tuna", "Tuna", "Saw Fish", "SunFish", "StingRay", "StingRay", "Grouper", "Salmon", "Cod", "FlyingFish", "FlyingFish" },
                       new[] { 3.8f, 2.5f, 2.2f, 3.4f, 2.8f, 2.3f, 1.9f, 1.9f, 1.3f, 1.3f, 0.9f, 0.9f }, 0.16f, rng);
            ocean.School("Sardine", 14, 0.5f, rng);
            ocean.Finish(t, "Marlin", rng);

            // The deep sea: dark, with only a few glows.
            Tank deep = Tank.Make(t, "deep sea", new Vector3(-HX + 2.8f, y, -16.5f), 90f, 12f, 4.6f, 5.5f, new Color(0.05f, 0.1f, 0.3f, 0.3f), new Color(0.01f, 0.02f, 0.06f), new Color(0.2f, 0.9f, 0.8f), 0.7f);
            deep.Rocks(7, rng);
            deep.Fish(new[] { "Angler Fish", "Angler Fish", "Squid", "Squid", "Snail Fish", "Eel" }, new[] { 1.2f, 0.9f, 1.4f, 1.0f, 0.8f, 1.5f }, 0.14f, rng);
            deep.Crawlers(new[] { "Sea Spider", "Giant Crab", "Sea Cucumber" }, 0.9f, rng);
            deep.Finish(t, "Angler Fish", rng);

            // A kelp forest with cold-water fish.
            Tank kelp = Tank.Make(t, "kelp forest", new Vector3(-HX + 2.8f, y, 17f), 90f, 13f, 4.6f, 6.2f, new Color(0.1f, 0.5f, 0.35f, 0.22f), new Color(0.03f, 0.2f, 0.16f), new Color(0.6f, 1f, 0.7f), 1.0f);
            kelp.Kelp(16, rng);
            kelp.Rocks(5, rng);
            kelp.Fish(new[] { "Cod", "Cod", "Haddock", "Haddock", "Herring", "Herring", "Herring", "Grouper", "Flounder" }, new[] { 1.2f, 1.0f, 0.9f, 0.9f, 0.6f, 0.6f, 0.6f, 1.6f, 0.9f }, 0.25f, rng);
            kelp.Finish(t, "Cod", rng);

            // The junk reef: what people lost at sea, and who lives in it now.
            Tank junk = Tank.Make(t, "junk reef", new Vector3(HX - 2.8f, y, -18.5f), -90f, 10f, 4.6f, 5.5f, new Color(0.15f, 0.45f, 0.6f, 0.22f), new Color(0.05f, 0.18f, 0.3f), new Color(0.8f, 0.9f, 1f), 1.0f);
            junk.Crawlers(new[] { "Boot", "ShoppingTrolly", "Boot", "Lobster", "Spider Crab", "Oyster" }, 1.1f, rng);
            junk.Fish(new[] { "Eel", "Pufferfish", "Wobbly Fish", "Wobbly Fish" }, new[] { 1.4f, 0.8f, 0.9f, 0.8f }, 0.25f, rng);
            junk.Finish(t, "Boot", rng);

            // Light: dark blue, with slow ripples on the floor as if the sea were overhead. The lobby is a bit warmer.
            Ripples(t, new Vector3(-14f, FH - 0.7f, 0f), new Color(0.3f, 0.6f, 1f), 7.5f);
            Ripples(t, new Vector3(4f, FH - 0.7f, -15f), new Color(0.3f, 0.6f, 1f), 7.5f);
            Ripples(t, new Vector3(-6f, FH - 0.7f, 17f), new Color(0.45f, 0.7f, 1f), 6.5f);
            Aquarium.Lamp(t, new Vector3(0f, 5.5f, 21f), new Color(1f, 0.85f, 0.65f), 12f, 0.9f);
            FishTank.Box(t, "front desk", new Vector3(-8f, 0.65f, 20f), new Vector3(5f, 1.1f, 1.3f), TankMaterials.Solid(Orange), true);
        }

        /// <summary>The gift shop: lots of different toys, and every one of them is loose, on tables and low shelves.</summary>
        private static void GiftShop(Transform t, System.Random rng)
        {
            var c = new Vector3(12f, 0.1f, 19f);
            Material wood = TankMaterials.Solid(new Color(0.75f, 0.55f, 0.35f));
            FishTank.Box(t, "shop rug", c + new Vector3(0f, 0.02f, 0f), new Vector3(11f, 0.04f, 10f), TankMaterials.Solid(Green), false);
            FishTank.Box(t, "shop counter", c + new Vector3(-4.2f, 0.55f, 3.6f), new Vector3(2.4f, 1.1f, 1.1f), TankMaterials.Solid(Orange), true);
            Aquarium.Lamp(t, c + new Vector3(0f, 5f, 0f), new Color(1f, 0.9f, 0.7f), 10f, 1.0f);
            // Three low tables (low enough for a Wobbly to reach) and a shelf along the wall.
            var tables = new[] { c + new Vector3(-2.5f, 0f, -1.5f), c + new Vector3(2f, 0f, -1.5f), c + new Vector3(0f, 0f, 2.2f) };
            foreach (Vector3 at in tables) FishTank.Box(t, "shop table", at + Vector3.up * 0.3f, new Vector3(3.4f, 0.6f, 2f), wood, true);
            FishTank.Box(t, "shop shelf", new Vector3(c.x, 0.35f, HZ - 1.1f), new Vector3(10f, 0.7f, 1.2f), wood, true);

            string[] plush = { "ClownFish", "Starfish", "JellyFish", "SeaHorse_Yellow", "SeaHorse_Green", "Pufferfish", "Lobster", "Angel Fish", "Wobbly Fish", "StingRay", "Squid", "Giant Crab", "Blue Dragon", "LionFish" };
            int n = 0;
            Func<Vector3> spot = () =>
            {
                int i = n++;
                if (i < 30)
                {
                    Vector3 table = tables[i % 3];
                    int k = i / 3;
                    return table + new Vector3(-1.3f + (k % 4) * 0.85f, 0.75f + (k / 8) * 0.4f, -0.6f + ((k / 4) % 2) * 1.1f);
                }
                return new Vector3(c.x - 4.5f + (i - 30) * 0.9f, 0.85f, HZ - 1.1f);
            };
            foreach (string name in plush)
            {
                GameObject toy = Animal(t, name, 0.55f, spot(), 0f, 0f, 0f, 0f, rng, null);
                if (toy != null) Loose(toy, 0.5f);
            }
            for (int i = 0; i < 6; i++)
            {
                GameObject toy = ShapeModels.Redtail();
                toy.name = "Redtail plush";
                toy.transform.SetParent(t, false);
                toy.transform.localPosition = spot() + Vector3.up * 0.15f;
                toy.transform.localRotation = Quaternion.Euler(0f, i * 61f, 0f);
                toy.transform.localScale = Vector3.one * (0.6f + 0.1f * (i % 3));
                Loose(toy, 0.6f);
            }
            foreach (string id in new[] { "dinotoy", "monstertruck", "dinotoy", "monstertruck", "catbed", "beanbag" })
            {
                GameObject toy = ShapeModels.Build("Skylar " + id, WobblyMods.Core.Shapes.SkylarFurniture.Build(id));
                toy.name = WobblyMods.Core.Shapes.SkylarFurniture.NiceName(id);
                toy.transform.SetParent(t, false);
                toy.transform.localPosition = spot();
                toy.transform.localScale = Vector3.one * 0.6f;
                Loose(toy, 0.8f);
            }
            Aquarium.Sign(t, c + new Vector3(-5.2f, 0f, -4f), 180f, sign => { GameObject f = ShapeModels.Redtail(); f.transform.SetParent(sign, false); f.transform.localScale = Vector3.one * 0.9f; f.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); });
        }

        // =====================================================================================================
        // Floor 2: the coral reef
        // =====================================================================================================

        private static void CoralReef(Transform t, System.Random rng)
        {
            const float y = FH;
            Color water = new Color(0.25f, 0.8f, 1f, 0.14f), back = new Color(0.1f, 0.5f, 0.75f), lamp = new Color(0.7f, 0.95f, 1f);
            string[] reef = { "ClownFish", "ClownFish", "ClownFish", "Angel Fish", "Angel Fish", "LionFish", "Parrot Fish", "Parrot Fish", "Blue Dragon", "Pufferfish", "Wobbly Fish", "Angel Fish", "ClownFish", "Parrot Fish" };

            Tank big = Tank.Make(t, "great reef", new Vector3(0f, y, -HZ + 3.1f), 0f, 40f, 5.4f, 6.4f, water, back, lamp, 1.6f);
            big.Corals(46, rng);
            big.Fish(reef, null, 0.3f, rng);
            big.Fish(new[] { "StingRay", "Grouper", "Parrot Fish", "LionFish" }, new[] { 1.6f, 1.5f, 1.0f, 0.9f }, 0.2f, rng);
            big.Crawlers(new[] { "Starfish", "Starfish", "SeaUrchin", "Giant Crab" }, 0.6f, rng);
            big.Finish(t, "Parrot Fish", rng);

            Tank front = Tank.Make(t, "reef wall", new Vector3(0f, y, HZ - 3.1f), 180f, 40f, 5.4f, 6.4f, water, back, lamp, 1.6f);
            front.Corals(40, rng);
            front.Fish(reef, null, 0.3f, rng);
            front.Crawlers(new[] { "Starfish", "Lobster", "SeaUrchin", "Starfish" }, 0.6f, rng);
            front.Finish(t, "Angel Fish", rng);

            Tank clown = Tank.Make(t, "clownfish garden", new Vector3(-HX + 2.8f, y, 17f), 90f, 13f, 4.6f, 5.6f, water, back, lamp, 1.4f);
            clown.Anemones(12, rng);
            clown.Fish(new[] { "ClownFish", "ClownFish", "ClownFish", "ClownFish", "ClownFish", "ClownFish", "ClownFish", "Blue Dragon" }, null, 0.4f, rng);
            clown.Finish(t, "ClownFish", rng);

            Tank horses = Tank.Make(t, "seahorse meadow", new Vector3(-HX + 2.8f, y, -17f), 90f, 13f, 4.6f, 5.6f, new Color(0.3f, 0.85f, 0.7f, 0.14f), new Color(0.1f, 0.5f, 0.45f), lamp, 1.4f);
            horses.Plants(18, 1.6f, rng);
            horses.Fish(new[] { "SeaHorse_Green", "SeaHorse_Yellow", "SeaHorse_Green", "SeaHorse_Yellow", "SeaHorse_Green", "SeaHorse_Yellow", "Pufferfish" }, null, 0.18f, rng);
            horses.Finish(t, "SeaHorse_Yellow", rng);

            Tank lion = Tank.Make(t, "lionfish rocks", new Vector3(HX - 2.8f, y, -18.5f), -90f, 10f, 4.6f, 5.6f, water, back, lamp, 1.4f);
            lion.Rocks(8, rng);
            lion.Corals(10, rng);
            lion.Fish(new[] { "LionFish", "LionFish", "LionFish", "Pufferfish", "Pufferfish" }, null, 0.2f, rng);
            lion.Finish(t, "LionFish", rng);

            Tank puff = Tank.Make(t, "puffer bay", new Vector3(HX - 2.8f, y, 18.5f), -90f, 10f, 4.6f, 5.6f, water, back, lamp, 1.4f);
            puff.Corals(14, rng);
            puff.Fish(new[] { "Pufferfish", "Pufferfish", "Pufferfish", "Angel Fish", "Angel Fish", "Wobbly Fish" }, null, 0.3f, rng);
            puff.Finish(t, "Pufferfish", rng);

            // Bright, shallow-water light with lively ripples everywhere.
            Ripples(t, new Vector3(-14f, y + FH - 0.7f, 0f), new Color(0.6f, 1f, 0.95f), 11.0f);
            Ripples(t, new Vector3(0f, y + FH - 0.7f, -14f), new Color(0.6f, 1f, 0.95f), 11.0f);
            Ripples(t, new Vector3(0f, y + FH - 0.7f, 15f), new Color(0.6f, 1f, 0.95f), 11.0f);
            Ripples(t, new Vector3(12f, y + FH - 0.7f, 14f), new Color(0.7f, 1f, 0.9f), 10.0f);
        }

        // =====================================================================================================
        // Floor 3: jellyfish and ice (the dark floor)
        // =====================================================================================================

        private static void JellyAndIce(Transform t, System.Random rng)
        {
            const float y = 2f * FH;
            Material dark = TankMaterials.Solid(new Color(0.02f, 0.02f, 0.05f));
            Color[] glow = { new Color(0.7f, 0.3f, 1f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.3f, 0.7f), new Color(0.3f, 1f, 0.8f) };
            float[][] tubes =
            {
                new[] { -19f, -19f }, new[] { -12f, -21f }, new[] { -21.5f, -12.5f }, new[] { -19f, 19f },
                new[] { -12f, 21f }, new[] { -21.5f, 12.5f }, new[] { -11.5f, -12.5f }, new[] { -11.5f, 12.5f }
            };
            for (int i = 0; i < tubes.Length; i++)
            {
                var at = new Vector3(tubes[i][0], y, tubes[i][1]);
                Color c = glow[i % glow.Length];
                GameObject tube = Aquarium.Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 3.4f, new Vector3(2.6f, 2.8f, 2.6f), Quaternion.identity, TankMaterials.Tinted(new Color(c.r, c.g, c.b, 0.16f)));
                tube.AddComponent<CapsuleCollider>(); // solid, so nobody walks through the tank
                FishTank.Box(t, "jelly base", at + Vector3.up * 0.3f, new Vector3(2.9f, 0.6f, 2.9f), dark, true);
                Aquarium.Lamp(t, at + Vector3.up * 3.4f, c, 6f, 1.6f);
                for (int j = 0; j < 5; j++)
                {
                    Animal(t, "JellyFish", 0.6f + (float)rng.NextDouble() * 0.5f, at + Vector3.up * (1.5f + j * 0.9f), 0.5f, 0.5f,
                           (j % 2 == 0 ? 1f : -1f) * (0.22f + (float)rng.NextDouble() * 0.2f), 0.5f, rng, null);
                }
            }
            Aquarium.Sign(t, new Vector3(-8.5f, y, 9.5f), 90f, sign => Aquarium.MiniFish(sign, "JellyFish", 0.9f));

            // The ice cave: pale blue and cold.
            Tank ice = Tank.Make(t, "ice cave", new Vector3(8f, y, -HZ + 3.1f), 0f, 22f, 5.4f, 6.2f, new Color(0.6f, 0.9f, 1f, 0.16f), new Color(0.55f, 0.8f, 0.95f), new Color(0.75f, 0.95f, 1f), 1.2f);
            ice.Ice(14, rng);
            ice.Fish(new[] { "Arctic Char", "Arctic Char", "Arctic Cod", "Arctic Cod", "Ice Fish", "Ice Fish", "Ice Cube Fish", "Snail Fish", "Haddock", "Herring", "Herring" }, null, 0.22f, rng);
            ice.Crawlers(new[] { "Frozen Boot", "Ice Cube" }, 0.8f, rng);
            ice.Finish(t, "Ice Fish", rng);

            // The glow reef: night creatures.
            Tank night = Tank.Make(t, "glow reef", new Vector3(8f, y, HZ - 3.1f), 180f, 22f, 5.4f, 6.2f, new Color(0.3f, 0.2f, 0.7f, 0.2f), new Color(0.03f, 0.02f, 0.1f), new Color(0.6f, 0.3f, 1f), 1.0f);
            night.Corals(22, rng);
            night.Fish(new[] { "Blue Dragon", "Blue Dragon", "Squid", "Squid", "JellyFish", "JellyFish", "Angler Fish", "Eel" }, null, 0.18f, rng);
            night.Crawlers(new[] { "Sea Cucumber", "SeaUrchin", "Starfish" }, 0.6f, rng);
            night.Finish(t, "Squid", rng);

            Tank crab = Tank.Make(t, "crab cave", new Vector3(HX - 2.8f, y, -18.5f), -90f, 10f, 4.6f, 5.2f, new Color(0.2f, 0.35f, 0.5f, 0.22f), new Color(0.03f, 0.05f, 0.1f), new Color(1f, 0.5f, 0.3f), 0.8f);
            crab.Rocks(10, rng);
            crab.Crawlers(new[] { "Giant Crab", "Giant Crab", "Spider Crab", "Spider Crab", "Sea Spider", "Lobster", "Lobster", "Prawn" }, 1.0f, rng);
            crab.Finish(t, "Giant Crab", rng);
            // Hardly any light out here: only a faint, slow ripple, so the glowing tanks stand out.
            Ripples(t, new Vector3(8f, y + FH - 0.7f, 0f), new Color(0.3f, 0.3f, 0.9f), 1.5f); // kept faint: this floor is meant to be dark
        }

        // =====================================================================================================
        // Floor 4: rivers, bays and beaches (light and sandy)
        // =====================================================================================================

        private static void RiversAndBeaches(Transform t, System.Random rng)
        {
            const float y = 3f * FH;
            Color lamp = new Color(1f, 0.97f, 0.85f);

            // Stingray bay: a big shallow pool to lean over, with rays gliding round it.
            var bay = new Vector3(-3f, y, -18.5f);
            Pool(t, bay, 26f, 9f, rng, pool =>
            {
                for (int i = 0; i < 7; i++)
                    Animal(pool, "StingRay", 1.4f + (float)rng.NextDouble() * 0.9f, new Vector3(((float)rng.NextDouble() - 0.5f) * 6f, 0.5f, 0f),
                           6f + (float)rng.NextDouble() * 5f, 2.2f + (float)rng.NextDouble() * 1.4f, (i % 2 == 0 ? 1f : -1f) * (0.2f + (float)rng.NextDouble() * 0.15f), 0.03f, rng, null);
                for (int i = 0; i < 3; i++)
                    Animal(pool, "Flounder", 0.9f, new Vector3(-8f + i * 8f, 0.34f, ((float)rng.NextDouble() - 0.5f) * 5f), 0f, 0f, 0f, 0f, rng, null);
            });
            Aquarium.Sign(t, bay + new Vector3(11f, 0f, 6.2f), 0f, sign => Aquarium.MiniFish(sign, "StingRay", 1.0f));

            // Two touch pools. These animals are loose: reach in and pick one up.
            string[] touch = { "Starfish", "Starfish", "Starfish", "Giant Crab", "Spider Crab", "SeaUrchin", "SeaUrchin", "Lobster", "Cockle", "Oyster", "Sea Cucumber", "Prawn" };
            foreach (float px in new[] { -13f, 2f })
            {
                var c = new Vector3(px, y, 16f);
                Pool(t, c, 9f, 5.5f, rng, pool =>
                {
                    foreach (string name in touch)
                    {
                        GameObject animal = Animal(pool, name, 0.5f + (float)rng.NextDouble() * 0.3f,
                                                   new Vector3(((float)rng.NextDouble() - 0.5f) * 7f, 0.5f, ((float)rng.NextDouble() - 0.5f) * 3.6f), 0f, 0f, 0f, 0f, rng, null);
                        if (animal != null) Loose(animal, 0.4f);
                    }
                });
                Aquarium.Sign(t, c + new Vector3(3.6f, 0f, -4f), 180f, sign => Aquarium.MiniFish(sign, "Starfish", 0.9f));
            }

            // River tanks with mangrove roots, and the planted and mangrove tanks.
            Tank river = Tank.Make(t, "river bend", new Vector3(-HX + 2.8f, y, -17f), 90f, 13f, 4.6f, 5.8f, new Color(0.45f, 0.75f, 0.5f, 0.16f), new Color(0.25f, 0.45f, 0.3f), lamp, 1.3f);
            river.Roots(5, rng);
            river.Plants(12, 1.4f, rng);
            river.Fish(new[] { "Pike", "Pike", "Trout", "Rainbow Trout", "Rainbow Trout", "Salmon", "Salmon_Pink", "Carp", "Bream", "Eel" }, null, 0.25f, rng);
            river.Finish(t, "Rainbow Trout", rng);

            Tank cats = Tank.Make(t, "catfish cousins", new Vector3(HX - 2.8f, y, -18.5f), -90f, 10f, 4.6f, 5.8f, new Color(0.5f, 0.7f, 0.45f, 0.16f), new Color(0.3f, 0.4f, 0.25f), lamp, 1.3f);
            cats.Roots(4, rng);
            cats.Rocks(5, rng);
            cats.Fish(new[] { "CatFish", "CatFish", "CatFish", "CatFish", "Carp", "Bream" }, new[] { 1.4f, 1.2f, 1.0f, 0.9f, 1.0f, 0.8f }, 0.2f, rng);
            cats.Finish(t, "CatFish", rng);

            Adopt(t, AquariumTunnel.BuildPlanted(t.TransformPoint(new Vector3(-HX + 1.4f, y, 17f)), t.rotation * Quaternion.Euler(0f, 90f, 0f)));
            Adopt(t, AquariumTunnel.BuildMangrove(t.TransformPoint(new Vector3(13f, y, HZ - 1.5f)), t.rotation * Quaternion.Euler(0f, 180f, 0f)));
            Adopt(t, AquariumTunnel.BuildMangrove(t.TransformPoint(new Vector3(HX - 1.5f, y, 18.5f)), t.rotation * Quaternion.Euler(0f, -90f, 0f)));

            // A strip of beach by the bay, and flowers in planters.
            Greenery(t, "Resort_PalmTree.prefab", new Vector3(-17f, y, -11.5f), 4.5f, rng);
            Greenery(t, "Resort_PalmTree.prefab", new Vector3(11.5f, y, -11.5f), 4.0f, rng);
            for (int i = 0; i < 10; i++)
            {
                var at = new Vector3(-16f + i * 3.2f, y, 9.6f + (i % 2) * 0.6f);
                if (Mathf.Abs(at.x) < TX + 1.5f) continue;
                Flower(t, at, rng);
            }
            Aquarium.Lamp(t, new Vector3(-12f, y + 6.5f, 4f), lamp, 13f, 1.1f);
            Aquarium.Lamp(t, new Vector3(10f, y + 6.5f, -13f), lamp, 13f, 1.1f);
            Ripples(t, new Vector3(-3f, y + FH - 0.7f, -17f), new Color(1f, 1f, 0.85f), 9.0f);
            Ripples(t, new Vector3(-6f, y + FH - 0.7f, 15f), new Color(1f, 1f, 0.85f), 9.0f);
        }

        // =====================================================================================================
        // Floor 5: the rainforest lagoon under the glass roof
        // =====================================================================================================

        private static void Rainforest(Transform t, System.Random rng)
        {
            const float y = Top;
            // Trees and bushes all round the lagoon, kept clear of the ramp and the bounce-pad landing.
            float[][] palms = { new[] { -20f, -20f }, new[] { 14f, -21f }, new[] { -21f, 20f }, new[] { 14f, 21f }, new[] { -10f, -22f }, new[] { 2f, 22f }, new[] { -22f, -9f } };
            foreach (float[] p in palms) Greenery(t, "Resort_PalmTree.prefab", new Vector3(p[0], y, p[1]), 5f + (float)rng.NextDouble() * 2f, rng);
            float[][] bananas = { new[] { -15f, -15f }, new[] { 12f, -14f }, new[] { -15f, 16f }, new[] { 10f, 16f }, new[] { -23f, 12f } };
            foreach (float[] p in bananas) Greenery(t, "BananaTree.prefab", new Vector3(p[0], y, p[1]), 3.2f + (float)rng.NextDouble(), rng);
            for (int i = 0; i < 16; i++)
            {
                var at = Around(rng);
                Greenery(t, i % 3 == 0 ? "Details/RedPlant.prefab" : "JungleBush.prefab", new Vector3(at.x, y, at.y), 1.2f + (float)rng.NextDouble() * 0.8f, rng);
            }
            for (int i = 0; i < 12; i++)
            {
                var at = Around(rng);
                Greenery(t, i % 2 == 0 ? "Details/Flower.prefab" : "Details/Flower Yellow.prefab", new Vector3(at.x, y, at.y), 0.9f, rng);
            }
            for (int i = 0; i < 26; i++)
            {
                var at = Around(rng);
                Flower(t, new Vector3(at.x, y, at.y), rng);
            }

            // Wildlife: the frog, lizard and turtle homes, and butterflies over the flowers.
            Adopt(t, Terrarium.BuildFrogTank(t.TransformPoint(new Vector3(-19f, y, -15f)), t.rotation * Quaternion.Euler(0f, 90f, 0f)));
            Adopt(t, Terrarium.BuildLizardTank(t.TransformPoint(new Vector3(12f, y, -19f)), t.rotation));
            Adopt(t, Terrarium.BuildTurtleTank(t.TransformPoint(new Vector3(-12f, y, 19f)), t.rotation * Quaternion.Euler(0f, 180f, 0f)));
            Color[] wings = { new Color(1f, 0.5f, 0.05f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.9f, 0.2f), new Color(0.9f, 0.3f, 0.7f), Color.white };
            for (int i = 0; i < 18; i++)
            {
                var at = Around(rng);
                var holder = new GameObject("butterfly");
                holder.transform.SetParent(t, false);
                Material m = TankMaterials.Solid(wings[i % wings.Length]);
                var body = new GameObject("wings");
                body.transform.SetParent(holder.transform, false);
                Transform left = Aquarium.Shape(body.transform, PrimitiveType.Cube, new Vector3(-0.11f, 0f, 0f), new Vector3(0.22f, 0.012f, 0.2f), Quaternion.identity, m).transform;
                Transform right = Aquarium.Shape(body.transform, PrimitiveType.Cube, new Vector3(0.11f, 0f, 0f), new Vector3(0.22f, 0.012f, 0.2f), Quaternion.identity, m).transform;
                Flap flap = body.AddComponent<Flap>();
                flap.Left = left; flap.Right = right; flap.Phase = i;
                Swim swim = holder.AddComponent<Swim>();
                swim.Center = new Vector3(at.x, y + 1.6f + (float)rng.NextDouble() * 2.5f, at.y);
                swim.RadiusX = 1.5f + (float)rng.NextDouble() * 2.5f;
                swim.RadiusZ = 1.5f + (float)rng.NextDouble() * 2.5f;
                swim.Speed = (i % 2 == 0 ? 1f : -1f) * (0.5f + (float)rng.NextDouble() * 0.5f);
                swim.Angle = (float)rng.NextDouble() * 6.28f;
                swim.Bob = 0.6f;
            }
            // Benches by the water.
            Material wood = TankMaterials.Solid(Wood);
            FishTank.Box(t, "bench", new Vector3(-9.5f, y + 0.35f, 0f), new Vector3(0.9f, 0.7f, 4f), wood, true);
            FishTank.Box(t, "bench", new Vector3(9.5f, y + 0.35f, 0f), new Vector3(0.9f, 0.7f, 4f), wood, true);
            Aquarium.Sign(t, new Vector3(3.2f, y, TZ + 1.6f), 0f, sign => { GameObject f = ShapeModels.Redtail(); f.transform.SetParent(sign, false); f.transform.localScale = Vector3.one * 1.1f; f.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); });
        }

        /// <summary>A random spot on the top floor that isn't the lagoon, the ramp, the bounce landing or right at the wall.</summary>
        private static Vector2 Around(System.Random rng)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * (HX * 2f - 4f), z = ((float)rng.NextDouble() - 0.5f) * (HZ * 2f - 4f);
                if (Mathf.Abs(x) < TX + 2.5f && Mathf.Abs(z) < TZ + 2.8f) continue; // lagoon and the walk round it
                if (x > 16f && z > -13f && z < 13f) continue;                        // ramp
                if (x < -15f && Mathf.Abs(z) < 8f) continue;                         // bounce pad landing
                return new Vector2(x, z);
            }
            return new Vector2(-18f, 22f);
        }

        // =====================================================================================================
        // Building blocks
        // =====================================================================================================

        /// <summary>One of the game's animals. Counts what loaded, for the log. swims = list to add its Swim to.</summary>
        private static GameObject Animal(Transform space, string name, float size, Vector3 center, float rx, float rz, float speed, float bob, System.Random rng, List<Swim> swims)
        {
            _fishWanted++;
            GameObject fish = Aquarium.GameFish(space, name, size, center, rx, rz, speed, bob, rng);
            if (fish == null) return null;
            _fishLoaded++;
            Swim swim = fish.GetComponent<Swim>();
            if (swim != null && swims != null) swims.Add(swim);
            return fish;
        }

        /// <summary>One of the game's own plants or trees (look-only), about that tall.</summary>
        private static void Greenery(Transform t, string prefab, Vector3 at, float size, System.Random rng)
        {
            _fishWanted++;
            GameObject plant = FishTank.Place(t, Plants + prefab, size, at, rng);
            if (plant != null) _fishLoaded++;
        }

        /// <summary>A flower made from shapes: a stem and five petals round a yellow middle.</summary>
        private static void Flower(Transform t, Vector3 at, System.Random rng)
        {
            Color[] petals = { new Color(1f, 0.5f, 0.05f), new Color(1f, 0.35f, 0.55f), new Color(0.75f, 0.4f, 0.95f), Color.white, new Color(1f, 0.85f, 0.2f), new Color(0.95f, 0.2f, 0.2f) };
            float tall = 0.5f + (float)rng.NextDouble() * 0.7f;
            var flower = new GameObject("flower");
            flower.transform.SetParent(t, false);
            flower.transform.localPosition = at;
            Aquarium.Shape(flower.transform, PrimitiveType.Cylinder, new Vector3(0f, tall / 2f, 0f), new Vector3(0.05f, tall / 2f, 0.05f), Quaternion.identity, TankMaterials.Solid(Leaf));
            Aquarium.Shape(flower.transform, PrimitiveType.Sphere, new Vector3(0.12f, tall * 0.4f, 0f), new Vector3(0.3f, 0.05f, 0.14f), Quaternion.Euler(0f, 0f, 25f), TankMaterials.Solid(Leaf));
            Material petal = TankMaterials.Solid(petals[rng.Next(petals.Length)]);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.2566f;
                Aquarium.Shape(flower.transform, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.13f, tall, Mathf.Sin(a) * 0.13f), new Vector3(0.18f, 0.06f, 0.18f), Quaternion.identity, petal);
            }
            Aquarium.Shape(flower.transform, PrimitiveType.Sphere, new Vector3(0f, tall + 0.02f, 0f), Vector3.one * 0.12f, Quaternion.identity, TankMaterials.Solid(new Color(1f, 0.85f, 0.1f)));
            Sway sway = flower.AddComponent<Sway>();
            sway.Amount = 4f;
            sway.Speed = 0.6f + (float)rng.NextDouble() * 0.5f;
            sway.Phase = (float)rng.NextDouble() * 6f;
        }

        /// <summary>A low open pool with a stone rim, sand and a water surface. fill gets the pool's own space (origin at its floor center).</summary>
        private static void Pool(Transform t, Vector3 center, float w, float d, System.Random rng, Action<Transform> fill)
        {
            const float rim = 0.75f;
            var pool = new GameObject("pool");
            pool.transform.SetParent(t, false);
            pool.transform.localPosition = center;
            Transform p = pool.transform;
            Material stone = TankMaterials.Solid(new Color(0.62f, 0.64f, 0.68f));
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(p, "pool rim", new Vector3(s * w / 2f, rim / 2f, 0f), new Vector3(0.4f, rim, d + 0.4f), stone, true);
                FishTank.Box(p, "pool rim", new Vector3(0f, rim / 2f, s * d / 2f), new Vector3(w - 0.4f, rim, 0.4f), stone, true);
            }
            FishTank.Box(p, "pool sand", new Vector3(0f, 0.15f, 0f), new Vector3(w - 0.4f, 0.3f, d - 0.4f), TankMaterials.Solid(Sand), true);
            FishTank.Box(p, "pool water", new Vector3(0f, rim - 0.12f, 0f), new Vector3(w - 0.4f, 0.04f, d - 0.4f), TankMaterials.Tinted(new Color(0.4f, 0.85f, 0.95f, 0.16f)), false);
            fill(p);
        }

        /// <summary>Keeps something that was built as its own object (a terrarium, a small tank) as part of the building.</summary>
        private static void Adopt(Transform t, GameObject made)
        {
            if (made != null) made.transform.SetParent(t, true);
        }

        /// <summary>
        /// Makes something loose: a box-shaped solid and a physics body, so it can be pushed, picked up and carried
        /// (as far as the game's hands allow).
        /// </summary>
        private static void Loose(GameObject go, float mass)
        {
            Bounds b = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds m = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = go.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!any) { b = new Bounds(local, Vector3.zero); any = true; } else b.Encapsulate(local);
                }
            }
            var box = go.AddComponent<BoxCollider>();
            if (any) { box.center = b.center; box.size = Vector3.Max(b.size, Vector3.one * 0.05f); }
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.angularDrag = 1.5f;
        }

        /// <summary>A big floor button in front of a tank: step on it and the tank's fish come to eat.</summary>
        private static void FeedButton(Transform space, TankMind mind, Vector3 at, Vector3 feedPoint)
        {
            Aquarium.Shape(space, PrimitiveType.Cylinder, at + Vector3.up * 0.03f, new Vector3(1.9f, 0.03f, 1.9f), Quaternion.identity, TankMaterials.Solid(Green));
            GameObject button = Aquarium.Shape(space, PrimitiveType.Cylinder, at + Vector3.up * 0.08f, new Vector3(1.5f, 0.07f, 1.5f), Quaternion.identity, TankMaterials.Solid(Orange));
            // A little pile of brown food pellets on it, as its picture.
            for (int i = 0; i < 5; i++)
                Aquarium.Shape(space, PrimitiveType.Sphere, at + new Vector3(Mathf.Cos(i * 1.26f) * 0.25f, 0.2f, Mathf.Sin(i * 1.26f) * 0.25f), Vector3.one * 0.16f, Quaternion.identity, TankMaterials.Solid(new Color(0.45f, 0.3f, 0.15f)));
            var zone = button.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 8f, 0f);
            zone.size = new Vector3(1f, 16f, 1f);
            FeedPad pad = button.AddComponent<FeedPad>();
            pad.Mind = mind;
            pad.FeedPoint = feedPoint;
        }

        /// <summary>
        /// A spotlight pointing down that paints slowly moving ripples of light on the floor, like sunlight through
        /// water (parent's request). Spotlights always shine through a pattern, so this needs no special shader.
        /// </summary>
        private static void Ripples(Transform t, Vector3 at, Color color, float intensity)
        {
            // Two lights in the same place turning opposite ways: where their patterns cross, the bright spots
            // slide and flicker like real light through water. Test 26: one faint light was "hard to tell". A
            // spotlight is much weaker at the floor than its number suggests (it fades with distance), so these
            // are several times brighter than before, with a longer reach.
            for (int layer = 0; layer < 2; layer++)
            {
                var go = new GameObject("ripple light");
                go.transform.SetParent(t, false);
                go.transform.localPosition = at + new Vector3(layer * 0.8f, 0f, layer * 0.6f);
                Light light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.spotAngle = 130f;
                light.range = FH * 1.9f;
                light.color = color;
                light.intensity = intensity * RippleStrength * (layer == 0 ? 1f : 0.75f);
                light.shadows = LightShadows.None;
                light.cookie = CausticCookie();
                light.renderMode = LightRenderMode.ForcePixel;
                RippleLight ripple = go.AddComponent<RippleLight>();
                ripple.Speed = (layer == 0 ? 1f : -1f) * (7f + (Mathf.Abs(at.x * 7f + at.z * 3f) % 5f));
            }
        }

        /// <summary>How strong the floor ripples are (config [Aquarium] RippleStrength; 1 = as designed).</summary>
        public static float RippleStrength = 1f;

        private static Texture2D _cookie;

        /// <summary>A web of bright wobbly lines fading to black at the edge, like the light pattern on a pool floor.</summary>
        private static Texture2D CausticCookie()
        {
            if (_cookie != null) return _cookie;
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Wobbly ripples", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int yy = 0; yy < n; yy++)
            {
                for (int xx = 0; xx < n; xx++)
                {
                    float u = xx / (float)n * 9f, v = yy / (float)n * 9f;
                    float a = Mathf.Sin(u + 1.6f * Mathf.Sin(v * 0.9f)) + Mathf.Sin(v * 1.1f + 1.4f * Mathf.Sin(u * 0.8f + 1.3f));
                    float b = Mathf.Sin((u + v) * 0.75f + 1.2f * Mathf.Sin((u - v) * 0.6f));
                    float lines = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(a * 1.3f + b)), 3f) + 0.6f * Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(b * 1.7f - a * 0.6f)), 4f);
                    float dx = xx / (float)n - 0.5f, dy = yy / (float)n - 0.5f;
                    float edge = Mathf.Clamp01((0.5f - Mathf.Sqrt(dx * dx + dy * dy)) / 0.12f);
                    byte value = (byte)(255f * Mathf.Clamp01((0.04f + 0.96f * lines) * edge));
                    px[yy * n + xx] = new Color32(value, value, value, value);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            _cookie = tex;
            return tex;
        }

        private static Mesh _pyramid;

        private static Mesh PyramidMesh()
        {
            if (_pyramid != null) return _pyramid;
            float baseY = Top + FH;
            var apex = new Vector3(0f, baseY + 11f, 0f);
            Vector3[] corners = { new Vector3(-HX, baseY, -HZ), new Vector3(HX, baseY, -HZ), new Vector3(HX, baseY, HZ), new Vector3(-HX, baseY, HZ) };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                Vector3 n = Vector3.Cross(b - a, apex - a).normalized;
                if (n.y < 0f) n = -n;
                int k = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(apex);
                normals.Add(n); normals.Add(n); normals.Add(n);
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
                tris.Add(k); tris.Add(k + 2); tris.Add(k + 1);
            }
            _pyramid = new Mesh { name = "Aquarium glass roof" };
            _pyramid.SetVertices(verts);
            _pyramid.SetNormals(normals);
            _pyramid.SetTriangles(tris, 0);
            _pyramid.RecalculateBounds();
            return _pyramid;
        }

        // =====================================================================================================
        // A wall tank: a window into a tank that stands against a wall
        // =====================================================================================================

        /// <summary>
        /// One exhibit tank. Its own space: origin on the floor at the middle of the tank, +Z toward the visitors,
        /// X along the glass. Build with Make, add what lives in it, then call Finish (the fish's "mind", the feeding
        /// button and the picture sign).
        /// </summary>
        private sealed class Tank
        {
            private const float Lift = 0.6f;
            private Transform _t;
            private float _w, _d, _h;
            private readonly List<Swim> _swims = new List<Swim>();

            public static Tank Make(Transform building, string name, Vector3 at, float yaw, float w, float d, float h, Color water, Color back, Color lampColor, float lampPower)
            {
                var holder = new GameObject(name);
                holder.transform.SetParent(building, false);
                holder.transform.localPosition = at;
                holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                var tank = new Tank { _t = holder.transform, _w = w, _d = d, _h = h };
                Transform t = holder.transform;
                Material frame = TankMaterials.Solid(new Color(0.1f, 0.1f, 0.13f));
                FishTank.Box(t, "base", new Vector3(0f, Lift / 2f, 0f), new Vector3(w, Lift, d), TankMaterials.Solid(Orange), true);
                FishTank.Box(t, "glass", new Vector3(0f, Lift + h / 2f, d / 2f), new Vector3(w, h, 0.12f), TankMaterials.Tinted(water), true);
                FishTank.Box(t, "back", new Vector3(0f, Lift + h / 2f, -d / 2f + 0.06f), new Vector3(w, h, 0.1f), TankMaterials.Solid(back), false);
                FishTank.Box(t, "top", new Vector3(0f, Lift + h + 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.2f), frame, true);
                for (int s = -1; s <= 1; s += 2)
                    FishTank.Box(t, "end", new Vector3(s * w / 2f, Lift + h / 2f, 0f), new Vector3(0.3f, h, d), frame, true);
                FishTank.Box(t, "sand", new Vector3(0f, Lift + 0.1f, 0f), new Vector3(w - 0.3f, 0.2f, d - 0.2f), TankMaterials.Solid(Sand), false);
                // The tank's own light, well inside it, so each exhibit glows in its own color.
                int lamps = Mathf.Max(1, Mathf.RoundToInt(w / 14f));
                for (int i = 0; i < lamps; i++)
                    Aquarium.Lamp(t, new Vector3((i + 0.5f) * w / lamps - w / 2f, Lift + h - 0.8f, 0f), lampColor, Mathf.Max(d, h) + 3f, lampPower);
                return tank;
            }

            private float Floor { get { return Lift + 0.2f; } }

            private Vector3 OnSand(System.Random rng, float margin)
            {
                return new Vector3(((float)rng.NextDouble() - 0.5f) * (_w - 2f * margin - 0.6f), Floor, ((float)rng.NextDouble() - 0.5f) * (_d - 2f * margin - 0.4f));
            }

            /// <summary>Swimming fish. sizes may be null (then 0.6 to 1.1 m). pace = how fast they go round.</summary>
            public void Fish(string[] names, float[] sizes, float pace, System.Random rng)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    float size = sizes != null ? sizes[i] : 0.6f + (float)rng.NextDouble() * 0.5f;
                    float room = Mathf.Max(0.4f, _d / 2f - 0.5f - size * 0.5f);
                    Animal(_t, names[i], size,
                           new Vector3(((float)rng.NextDouble() - 0.5f) * _w * 0.3f, Floor + 0.8f + (float)rng.NextDouble() * Mathf.Max(0.5f, _h - 2.2f), 0f),
                           _w * (0.2f + (float)rng.NextDouble() * 0.22f), room * (0.5f + (float)rng.NextDouble() * 0.5f),
                           (rng.Next(2) == 0 ? 1f : -1f) * pace * (0.7f + (float)rng.NextDouble() * 0.6f), 0.15f + (float)rng.NextDouble() * 0.2f, rng, _swims);
                }
            }

            /// <summary>A school: many of one small fish following each other round the same loop.</summary>
            public void School(string name, int count, float size, System.Random rng)
            {
                float height = Floor + _h * 0.55f, rx = _w * 0.36f, rz = Mathf.Max(0.5f, _d / 2f - 1.2f);
                for (int i = 0; i < count; i++)
                {
                    GameObject fish = Animal(_t, name, size, new Vector3(0f, height + (i % 3) * 0.35f, 0f), rx + (i % 4) * 0.2f, rz, 0.35f, 0.1f, rng, _swims);
                    if (fish == null) continue;
                    Swim swim = fish.GetComponent<Swim>();
                    if (swim != null) swim.Angle = i * 0.11f; // bunched together
                }
            }

            /// <summary>Animals that sit or shuffle about on the bottom.</summary>
            public void Crawlers(string[] names, float size, System.Random rng)
            {
                foreach (string name in names)
                {
                    bool walks = name.Contains("Crab") || name == "Lobster" || name == "Prawn" || name == "Sea Spider";
                    Animal(_t, name, size * (0.7f + (float)rng.NextDouble() * 0.5f), OnSand(rng, 0.9f), walks ? 0.7f : 0f, walks ? 0.5f : 0f, walks ? 0.15f : 0f, 0f, rng, null);
                }
            }

            public void Rocks(int count, System.Random rng)
            {
                // Rocks sit toward the back and stay low. In test 26 a big rock right at the glass hid a whole tank.
                Material rock = TankMaterials.Solid(new Color(0.34f, 0.36f, 0.42f));
                for (int i = 0; i < count; i++)
                {
                    float r = 0.4f + (float)rng.NextDouble() * 0.7f;
                    Vector3 at = OnSand(rng, 0.6f);
                    at.z = -_d / 2f + 0.5f + (float)rng.NextDouble() * (_d * 0.45f);
                    Aquarium.Shape(_t, PrimitiveType.Sphere, at + Vector3.up * r * 0.2f, new Vector3(r * 1.4f, r * 0.7f, r),
                                   Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, 0f), rock);
                }
            }

            public void Corals(int count, System.Random rng)
            {
                Color[] coral = { new Color(1f, 0.35f, 0.45f), new Color(1f, 0.6f, 0.1f), new Color(0.75f, 0.35f, 0.9f), new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.85f, 0.7f), new Color(1f, 0.5f, 0.75f) };
                for (int i = 0; i < count; i++)
                {
                    Vector3 at = OnSand(rng, 0.5f);
                    at.z = Mathf.Min(at.z, _d * 0.2f); // nothing tall right against the window
                    Material m = TankMaterials.Solid(coral[rng.Next(coral.Length)]);
                    int kind = rng.Next(4);
                    if (kind == 0)
                    {
                        float r = 0.4f + (float)rng.NextDouble() * 0.6f;
                        Aquarium.Shape(_t, PrimitiveType.Sphere, at + Vector3.up * r * 0.3f, new Vector3(r * 1.3f, r * 0.8f, r * 1.3f), Quaternion.identity, m);
                    }
                    else if (kind == 1)
                    {
                        for (int b = 0; b < 5; b++)
                        {
                            float tall = 0.6f + (float)rng.NextDouble() * 1.2f;
                            Quaternion lean = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 50f, 0f, ((float)rng.NextDouble() - 0.5f) * 50f);
                            Aquarium.Shape(_t, PrimitiveType.Capsule, at + lean * new Vector3(0f, tall / 2f, 0f), new Vector3(0.16f, tall / 2f, 0.16f), lean, m);
                        }
                    }
                    else if (kind == 2)
                    {
                        float tall = 0.5f + (float)rng.NextDouble() * 1.0f;
                        Aquarium.Shape(_t, PrimitiveType.Cylinder, at + Vector3.up * tall / 2f, new Vector3(0.35f, tall / 2f, 0.35f), Quaternion.identity, m);
                    }
                    else
                    {
                        // A sea fan: a flat round fan standing on edge.
                        float r = 0.7f + (float)rng.NextDouble() * 0.7f;
                        Aquarium.Shape(_t, PrimitiveType.Sphere, at + Vector3.up * r * 0.55f, new Vector3(r, r * 1.1f, 0.06f), Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f), m);
                    }
                }
            }

            /// <summary>Anemones: a stubby stalk with a ring of waving tentacles, for the clownfish.</summary>
            public void Anemones(int count, System.Random rng)
            {
                Color[] colors = { new Color(1f, 0.55f, 0.75f), new Color(0.8f, 0.5f, 1f), new Color(1f, 0.75f, 0.3f), new Color(0.5f, 0.95f, 0.75f) };
                for (int i = 0; i < count; i++)
                {
                    var anemone = new GameObject("anemone");
                    anemone.transform.SetParent(_t, false);
                    anemone.transform.localPosition = OnSand(rng, 0.7f);
                    Material m = TankMaterials.Solid(colors[rng.Next(colors.Length)]);
                    float size = 0.5f + (float)rng.NextDouble() * 0.5f;
                    Aquarium.Shape(anemone.transform, PrimitiveType.Cylinder, new Vector3(0f, size * 0.25f, 0f), new Vector3(size * 0.7f, size * 0.25f, size * 0.7f), Quaternion.identity, m);
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 0.628f;
                        Quaternion lean = Quaternion.Euler(Mathf.Cos(a) * 28f, 0f, -Mathf.Sin(a) * 28f);
                        Aquarium.Shape(anemone.transform, PrimitiveType.Capsule, new Vector3(Mathf.Sin(a) * size * 0.25f, size * 0.5f, Mathf.Cos(a) * size * 0.25f) + lean * new Vector3(0f, size * 0.3f, 0f),
                                       new Vector3(0.09f, size * 0.32f, 0.09f), lean, m);
                    }
                    Sway sway = anemone.AddComponent<Sway>();
                    sway.Amount = 6f; sway.Speed = 0.7f; sway.Phase = i;
                }
            }

            public void Plants(int count, float tall, System.Random rng)
            {
                for (int i = 0; i < count; i++) AquariumTunnel.Plant(_t, OnSand(rng, 0.5f), tall * (0.5f + (float)rng.NextDouble() * 0.8f), rng);
            }

            /// <summary>Kelp: tall ribbons from the sand nearly to the surface, swaying.</summary>
            public void Kelp(int count, System.Random rng)
            {
                Material m = TankMaterials.Solid(new Color(0.2f, 0.45f, 0.15f)), m2 = TankMaterials.Solid(new Color(0.35f, 0.55f, 0.15f));
                for (int i = 0; i < count; i++)
                {
                    var kelp = new GameObject("kelp");
                    kelp.transform.SetParent(_t, false);
                    kelp.transform.localPosition = OnSand(rng, 0.5f);
                    float tall = _h * (0.6f + (float)rng.NextDouble() * 0.35f);
                    Aquarium.Shape(kelp.transform, PrimitiveType.Cube, new Vector3(0f, tall / 2f, 0f), new Vector3(0.28f, tall, 0.04f), Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f), i % 2 == 0 ? m : m2);
                    Sway sway = kelp.AddComponent<Sway>();
                    sway.Amount = 5f; sway.Speed = 0.4f + (float)rng.NextDouble() * 0.3f; sway.Phase = (float)rng.NextDouble() * 6f;
                }
            }

            /// <summary>Icebergs: white and pale blue blocks, some on the bottom and some hanging from the top.</summary>
            public void Ice(int count, System.Random rng)
            {
                Material white = TankMaterials.Solid(new Color(0.95f, 0.98f, 1f)), blue = TankMaterials.Solid(new Color(0.7f, 0.88f, 1f));
                for (int i = 0; i < count; i++)
                {
                    float size = 0.7f + (float)rng.NextDouble() * 1.3f;
                    Vector3 at = OnSand(rng, 0.6f);
                    at.z = Mathf.Min(at.z, _d * 0.15f);
                    if (i % 3 == 0) at.y = Lift + _h - size * 0.4f; else at.y += size * 0.35f;
                    Aquarium.Shape(_t, PrimitiveType.Cube, at, new Vector3(size, size * (0.6f + (float)rng.NextDouble() * 0.6f), size * 0.8f),
                                   Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f), i % 2 == 0 ? white : blue);
                }
            }

            /// <summary>Mangrove roots: bunches of arching roots coming down from the top of the tank to the sand.</summary>
            public void Roots(int trees, System.Random rng)
            {
                for (int i = 0; i < trees; i++)
                {
                    var top = new Vector3(((float)rng.NextDouble() - 0.5f) * (_w - 2.5f), Lift + _h - 0.2f, ((float)rng.NextDouble() - 0.7f) * (_d - 1.6f));
                    AquariumTunnel.Log(_t, top, top + Vector3.down * 1.0f, 0.14f);
                    int roots = 5 + rng.Next(3);
                    for (int k = 0; k < roots; k++)
                    {
                        float a = k * 6.28f / roots + (float)rng.NextDouble() * 0.6f, reach = 0.6f + (float)rng.NextDouble() * 0.9f;
                        var foot = new Vector3(Mathf.Clamp(top.x + Mathf.Cos(a) * reach, -_w / 2f + 0.4f, _w / 2f - 0.4f), Floor, Mathf.Clamp(top.z + Mathf.Sin(a) * reach, -_d / 2f + 0.3f, _d / 2f - 0.3f));
                        AquariumTunnel.Log(_t, top + Vector3.down * 0.9f, foot, 0.05f);
                    }
                }
            }

            /// <summary>The finishing touches every tank gets: its fish notice visitors, a feeding button, a picture sign.</summary>
            public void Finish(Transform building, string signAnimal, System.Random rng)
            {
                if (_swims.Count > 0)
                {
                    TankMind mind = _t.gameObject.AddComponent<TankMind>();
                    mind.Fish = _swims;
                    mind.Interior = new Bounds(new Vector3(0f, Lift + _h / 2f + 0.2f, 0f), new Vector3(_w - 2.4f, _h - 1.6f, Mathf.Max(0.4f, _d - 1.8f)));
                    mind.Reach = 7f;
                    float bx = -_w / 2f + 2.2f;
                    FeedButton(_t, mind, new Vector3(bx, 0f, _d / 2f + 1.5f), new Vector3(bx, Lift + _h * 0.55f, _d / 2f - 1.1f));
                }
                Aquarium.Sign(_t, new Vector3(_w / 2f - 1.2f, 0f, _d / 2f + 1.1f), 0f, sign => Aquarium.MiniFish(sign, signAnimal, 0.9f));
            }
        }
    }

    // =========================================================================================================
    // Behaviours
    // =========================================================================================================

    /// <summary>Where the visitors are, looked up a few times a second for every tank at once.</summary>
    internal static class Visitors
    {
        private static readonly List<Vector3> Positions = new List<Vector3>();
        private static float _next;

        public static List<Vector3> Now()
        {
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + 0.25f;
                Positions.Clear();
                for (int i = 0; i < 4; i++)
                {
                    Vector3? p = GameBindings.CharacterPosition(i);
                    if (p.HasValue) Positions.Add(p.Value);
                }
            }
            return Positions;
        }
    }

    /// <summary>
    /// What makes a tank's fish "interactive": when a Wobbly stands at the glass (or swims in the tank), the fish
    /// near her leave their loops and gather round the spot closest to her, following as she walks along; when the
    /// tank is fed, they all go to the food. Otherwise they swim their usual loops. Lives on the tank; the fish's
    /// Swim centers are in this object's space.
    /// </summary>
    internal sealed class TankMind : MonoBehaviour
    {
        public List<Swim> Fish;
        public Bounds Interior;          // where fish may gather, in this object's space
        public float Reach = 7f;         // how far from a fish's own loop it will come over
        public float Spread = 0.9f;      // how far from the visitor the gathered fish keep their loops' middles
        public float KeepLoop = 0.45f;   // how much of its own loop size a gathered fish keeps (1 = all of it)

        private Vector3[] _centers;
        private float[] _rx, _rz;
        private float _feedUntil = -1f;
        private Vector3 _feedPoint;
        private readonly List<GameObject> _food = new List<GameObject>();

        public void Feed(Vector3 point)
        {
            if (Time.time < _feedUntil) return;
            _feedPoint = point;
            _feedUntil = Time.time + 14f;
            Material pellet = TankMaterials.Solid(new Color(0.45f, 0.3f, 0.15f));
            for (int i = 0; i < 12; i++)
            {
                GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(bit.GetComponent<Collider>());
                bit.name = "fish food";
                bit.transform.SetParent(transform, false);
                bit.transform.localPosition = point + new Vector3(Mathf.Cos(i * 2.4f) * 0.6f, 1.6f + (i % 4) * 0.25f, Mathf.Sin(i * 2.4f) * 0.4f);
                bit.transform.localScale = Vector3.one * 0.13f;
                bit.GetComponent<Renderer>().sharedMaterial = pellet;
                _food.Add(bit);
            }
            FmodSounds.TryPlay(MenuSound.Pop);
            Plugin.Log.LogInfo("Aquarium: feeding time at " + name + " (" + (Fish != null ? Fish.Count : 0) + " fish).");
        }

        private void Update()
        {
            if (Fish == null || Fish.Count == 0) return;
            if (_centers == null)
            {
                _centers = new Vector3[Fish.Count];
                _rx = new float[Fish.Count];
                _rz = new float[Fish.Count];
                for (int i = 0; i < Fish.Count; i++)
                {
                    if (Fish[i] == null) continue;
                    _centers[i] = Fish[i].Center; _rx[i] = Fish[i].RadiusX; _rz[i] = Fish[i].RadiusZ;
                }
            }

            bool feeding = Time.time < _feedUntil - 2.5f;
            Vector3? target = null;
            float reach = Reach;
            if (feeding)
            {
                target = _feedPoint;
                reach = 14f;
            }
            else
            {
                float best = 5.5f;
                foreach (Vector3 world in Visitors.Now())
                {
                    Vector3 local = transform.InverseTransformPoint(world);
                    Vector3 inside = Interior.ClosestPoint(local);
                    float away = Vector3.Distance(local, inside);
                    if (away < best) { best = away; target = inside; }
                }
            }

            float k = 1f - Mathf.Exp(-0.9f * Time.deltaTime); // unhurried: they drift over, they don't dart
            for (int i = 0; i < Fish.Count; i++)
            {
                Swim fish = Fish[i];
                if (fish == null) continue;
                // When someone is just watching, every other fish stays on its own loop, so the tank still looks alive.
                bool comes = target.HasValue && (feeding || i % 2 == 0) && Vector3.Distance(_centers[i], target.Value) < reach;
                // Gathered fish keep swimming loops, only smaller and moved over: each one's loop sits on a ring
                // round the visitor, so they circle near her and never pile up on one spot (test 26: the redtails
                // "crowd in too close and stop looking like they swim naturally").
                float around = i * 2.4f;
                Vector3 ring = new Vector3(Mathf.Cos(around) * Spread, ((i % 5) - 2) * Spread * 0.35f, Mathf.Sin(around) * Spread);
                Vector3 wantCenter = comes ? Interior.ClosestPoint(target.Value + ring) : _centers[i];
                float wantX = comes ? Mathf.Max(0.6f, _rx[i] * KeepLoop) : _rx[i];
                float wantZ = comes ? Mathf.Max(0.6f, _rz[i] * KeepLoop) : _rz[i];
                fish.Center = Vector3.Lerp(fish.Center, wantCenter, k);
                fish.RadiusX = Mathf.Lerp(fish.RadiusX, wantX, k);
                fish.RadiusZ = Mathf.Lerp(fish.RadiusZ, wantZ, k);
            }

            if (_food.Count > 0)
            {
                float left = _feedUntil - Time.time;
                foreach (GameObject bit in _food)
                {
                    if (bit == null) continue;
                    Vector3 p = bit.transform.localPosition;
                    if (p.y > _feedPoint.y - 0.4f) bit.transform.localPosition = p + Vector3.down * 0.4f * Time.deltaTime;
                    if (left < 9f) bit.transform.localScale *= 1f - 0.7f * Time.deltaTime;
                }
                if (left <= 0f)
                {
                    foreach (GameObject bit in _food) if (bit != null) Destroy(bit);
                    _food.Clear();
                }
            }
        }
    }

    /// <summary>The trigger on a feeding button.</summary>
    internal sealed class FeedPad : MonoBehaviour
    {
        public TankMind Mind;
        public Vector3 FeedPoint;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger || other.attachedRigidbody == null || Mind == null) return;
            Mind.Feed(FeedPoint);
        }
    }

    /// <summary>
    /// The water in the big river tank: anything with a physics body that is inside sinks slowly and drifts, like
    /// a diver, so Skylar can jump in at the top and float down among the redtails. Jumping (or a costume's
    /// jetpack) still pushes her up. Leaving the box gives normal falling back at once.
    /// </summary>
    internal sealed class SwimZone : MonoBehaviour
    {
        private const float Buoyancy = 0.86f;   // how much of gravity the water cancels
        private const float Drag = 1.1f;        // how quickly the water slows things
        private readonly Dictionary<Rigidbody, float> _done = new Dictionary<Rigidbody, float>();

        private void OnTriggerStay(Collider other)
        {
            Rigidbody body = other != null ? other.attachedRigidbody : null;
            if (body == null || body.isKinematic) return;
            // A body with several colliders is only pushed once per physics step.
            float last;
            if (_done.TryGetValue(body, out last) && last == Time.fixedTime) return;
            if (_done.Count > 400) _done.Clear();
            _done[body] = Time.fixedTime;
            body.AddForce(-Physics.gravity * Buoyancy, ForceMode.Acceleration);
            body.velocity *= Mathf.Clamp01(1f - Drag * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// A bounce pad that throws whatever steps on it up to the next floor: straight up through the hole above,
    /// with a little push sideways (toward the middle of the building) so it lands on the floor beside the hole
    /// and not back down it. Works out the throw from the current gravity, so it still works on a low-gravity day.
    /// </summary>
    internal sealed class Launcher : MonoBehaviour
    {
        public Transform Building;
        public float Rise = 8f;
        private readonly Dictionary<Rigidbody, float> _recent = new Dictionary<Rigidbody, float>();
        private readonly List<Rigidbody> _flying = new List<Rigidbody>();
        private Vector3 _throw;
        private float _pushUntil;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger || other.attachedRigidbody == null) return;
            List<Rigidbody> bodies = Fun.BodiesOf(other);
            if (bodies.Count == 0) return;
            float last;
            if (_recent.TryGetValue(bodies[0], out last) && Time.time - last < 1.2f) return;
            if (_recent.Count > 100) _recent.Clear();
            _recent[bodies[0]] = Time.time;

            float g = Mathf.Max(0.5f, Mathf.Abs(Physics.gravity.y));
            float up = Mathf.Sqrt(2f * g * (Rise + 1.8f));
            float timeToFloor = (up - Mathf.Sqrt(Mathf.Max(0f, up * up - 2f * g * Rise))) / g;
            float sideways = 2.2f / Mathf.Max(0.1f, timeToFloor);
            Vector3 toMiddle = Building != null ? Building.TransformDirection(Vector3.right) : Vector3.right;
            _throw = Vector3.up * up + toMiddle * sideways;
            _flying.Clear();
            _flying.AddRange(bodies);
            _pushUntil = Time.time + 0.1f; // held for a few steps, in case the game's own movement fights the first one
            Throw();
            FmodSounds.TryPlay(MenuSound.Pop);
        }

        private void FixedUpdate()
        {
            if (_flying.Count == 0) return;
            if (Time.time > _pushUntil) { _flying.Clear(); return; }
            Throw();
        }

        private void Throw()
        {
            foreach (Rigidbody body in _flying)
            {
                if (body != null && !body.isKinematic) body.velocity = _throw;
            }
        }
    }

    /// <summary>Slowly turns and tilts a ripple light, so the pattern it paints on the floor drifts and shimmers.</summary>
    internal sealed class RippleLight : MonoBehaviour
    {
        public float Speed = 6f;

        private void Update()
        {
            float t = Time.time;
            transform.localRotation = Quaternion.Euler(90f + 3f * Mathf.Sin(t * 0.5f), 3f * Mathf.Cos(t * 0.37f), 0f) * Quaternion.Euler(0f, 0f, t * Speed);
        }
    }

    /// <summary>Butterfly wings.</summary>
    internal sealed class Flap : MonoBehaviour
    {
        public Transform Left, Right;
        public float Phase;

        private void Update()
        {
            float a = 55f * Mathf.Sin(Time.time * 14f + Phase);
            if (Left != null) Left.localRotation = Quaternion.Euler(0f, 0f, -a);
            if (Right != null) Right.localRotation = Quaternion.Euler(0f, 0f, a);
        }
    }
}
