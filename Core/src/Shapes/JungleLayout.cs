using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    /// <summary>Where one rainforest giant stands on the top floor of the big aquarium.</summary>
    public struct TreeSpot
    {
        public float X, Z, Scale, Yaw;
        public int Variant;
    }

    /// <summary>A vine gate over a path. Yaw 0 = it spans along X; yaw 90 = along Z.</summary>
    public struct GateSpot
    {
        public float X, Z, Yaw;
        public int Variant;
    }

    public enum JungleLook { Plank, DarkWood, Orange, Green, Unseen }

    /// <summary>
    /// One box of the treehouse. Tilts are in degrees the way the game engine turns things: TiltZ lifts the +X end,
    /// TiltX drops the +Z end. Only one of the two is ever used on a box.
    /// </summary>
    public sealed class JungleBox
    {
        public string Name;
        public V3 Center, Size;
        public float TiltX, TiltZ;
        public JungleLook Look;
        public bool Solid;      // players and things bump into it
        public bool Slippery;   // a slide
        public bool Trigger;    // not solid: a zone that pushes whoever is in it down the slide

        /// <summary>A point given in the box's own axes (before tilting), as a point on the floor plan.</summary>
        public V3 Point(float x, float y, float z)
        {
            double az = TiltZ * Math.PI / 180.0, ax = TiltX * Math.PI / 180.0;
            // About Z: +X turns toward +Y.
            float x1 = (float)(x * Math.Cos(az) - y * Math.Sin(az));
            float y1 = (float)(x * Math.Sin(az) + y * Math.Cos(az));
            // About X: +Y turns toward +Z (so +Z turns down).
            float y2 = (float)(y1 * Math.Cos(ax) - z * Math.Sin(ax));
            float z2 = (float)(y1 * Math.Sin(ax) + z * Math.Cos(ax));
            return new V3(Center.X + x1, Center.Y + y2, Center.Z + z2);
        }

        /// <summary>The middle of the top face at the box's +X end (sx = 1), -X end (sx = -1), or likewise along Z.</summary>
        public V3 TopEdge(int sx, int sz)
        {
            return Point(sx * Size.X / 2f, Size.Y / 2f, sz * Size.Z / 2f);
        }

        public V3[] Corners()
        {
            var c = new V3[8];
            for (int i = 0; i < 8; i++)
                c[i] = Point(((i & 1) == 0 ? -1 : 1) * Size.X / 2f, ((i & 2) == 0 ? -1 : 1) * Size.Y / 2f, ((i & 4) == 0 ? -1 : 1) * Size.Z / 2f);
            return c;
        }

        /// <summary>The box as a mesh (for the floor-plan picture).</summary>
        public ShapeMesh ToMesh()
        {
            var mesh = new ShapeMesh();
            // For each face: its normal axis and two edge axes u, v with u x v = normal.
            int[][] faces =
            {
                new[] { 0, 1, 1, 2 }, new[] { 0, -1, 2, 1 }, new[] { 1, 1, 2, 0 }, new[] { 1, -1, 0, 2 }, new[] { 2, 1, 0, 1 }, new[] { 2, -1, 1, 0 }
            };
            float[] half = { Size.X / 2f, Size.Y / 2f, Size.Z / 2f };
            foreach (int[] f in faces)
            {
                var corner = new V3[4];
                int[][] signs = { new[] { -1, -1 }, new[] { 1, -1 }, new[] { 1, 1 }, new[] { -1, 1 } };
                for (int k = 0; k < 4; k++)
                {
                    var local = new float[3];
                    local[f[0]] = f[1] * half[f[0]];
                    local[f[2]] = signs[k][0] * half[f[2]];
                    local[f[3]] = signs[k][1] * half[f[3]];
                    corner[k] = Point(local[0], local[1], local[2]);
                }
                V3 normal = V3.Cross(corner[1] - corner[0], corner[2] - corner[0]).Normalized;
                int start = mesh.Vertices.Count;
                for (int k = 0; k < 4; k++) mesh.AddVertex(corner[k], normal);
                mesh.Triangles.Add(start); mesh.Triangles.Add(start + 1); mesh.Triangles.Add(start + 2);
                mesh.Triangles.Add(start); mesh.Triangles.Add(start + 2); mesh.Triangles.Add(start + 3);
            }
            return mesh;
        }
    }

    public struct JungleRope
    {
        public V3 From, To;
        public float Radius;
    }

    /// <summary>A spot for a bird to sit, with the way it faces.</summary>
    public struct BirdSpot
    {
        public V3 At;
        public float Yaw;
    }

    public sealed class TreehousePlan
    {
        public readonly List<JungleBox> Boxes = new List<JungleBox>();
        public readonly List<JungleRope> Ropes = new List<JungleRope>();
        public readonly List<BirdSpot> Birds = new List<BirdSpot>();
        public JungleBox Ramp, Slide, Bridge1, Bridge2, DeckA, DeckB;
        /// <summary>The way down the slide (a direction, length 1).</summary>
        public V3 SlideDown;
    }

    /// <summary>
    /// The layout of the jungle on the top floor of Skylar's big aquarium: where the trees, vine gates and the
    /// treehouse go. Plain numbers with no game engine in them, so tests can check what nobody can see from the
    /// cloud: that every tree fits under the glass roof, that no tree blocks a path, that the ramp and the slide
    /// meet their platforms. Everything is measured from the middle of the top floor, with y = 0 at the floor.
    /// </summary>
    public static class JungleLayout
    {
        public const float HalfX = 26f, HalfZ = 26f;          // the glass walls
        public const float LagoonX = 6f, LagoonZ = 7f;        // the open water in the middle
        public const float WallHeight = 8f, RoofRise = 11f;   // glass walls, then a glass pyramid
        public const float Deck = 3.0f;                       // how high the treehouse platforms are
        public const float HutRoofTop = Deck + 2.56f;
        public const float GateWidth = 3.3f, GateHeight = 4.4f;
        public const float TreeAX = 11f, TreeAZ = 20f, TreeBX = 20f, TreeBZ = 20f;
        public const float Spacing = 3.4f;
        public const float SmallestTree = 0.42f;

        /// <summary>How high the glass roof is above the floor at this spot.</summary>
        public static float Roof(float x, float z)
        {
            float out01 = Math.Max(Math.Abs(x) / HalfX, Math.Abs(z) / HalfZ);
            return WallHeight + RoofRise * (1f - Math.Min(1f, out01));
        }

        /// <summary>Is this spot on the top floor free for a tree, a bush or a flower?</summary>
        public static bool Free(float x, float z)
        {
            if (Math.Abs(x) > HalfX - 2f || Math.Abs(z) > HalfZ - 2f) return false;
            if (Math.Abs(x) < LagoonX + 2.6f && Math.Abs(z) < LagoonZ + 2.9f) return false; // the lagoon and the walk round it
            if (x > 16f && z > -13f && z < 13f) return false;                              // the ramp and the way from it
            if (x < -15f && Math.Abs(z) < 8f) return false;                                // where the bounce pad lands you
            if (x > -15f && x < -8.6f && z > 1f && z < 7f) return false;                   // the way from there to the lagoon
            if (x > -5f && x < 24.5f && z > 16.5f) return false;                           // the treehouse, its ramp and bridge
            if (x > 17.5f && x < 22.5f && z > 9.5f && z < 17f) return false;               // the slide and its landing
            if (x > -5f && x < 1f && z > 9.9f && z < 17f) return false;                    // the way from the lagoon to the treehouse ramp
            if (Sq(x + 19f, z + 15f) < 3.6f * 3.6f) return false;                          // frog home
            if (Sq(x - 12f, z + 19f) < 3.6f * 3.6f) return false;                          // lizard home
            if (Sq(x + 12f, z - 19f) < 3.6f * 3.6f) return false;                          // turtle home
            if (Math.Abs(Math.Abs(x) - 9.5f) < 1.4f && Math.Abs(z) < 3f) return false;     // the benches
            return true;
        }

        private static float Sq(float x, float z) { return x * x + z * z; }

        public static List<GateSpot> Gates()
        {
            float[][] g =
            {
                new[] { -7.9f, -5f, 0f }, new[] { -7.9f, 5.5f, 0f }, new[] { 7.9f, -5f, 0f }, new[] { 7.9f, 5f, 0f },
                new[] { -4f, -8.9f, 90f }, new[] { 4f, -8.9f, 90f }, new[] { -4.5f, 8.9f, 90f }, new[] { 5.6f, 8.9f, 90f },
                new[] { 17.5f, 10.5f, 90f }, new[] { -2f, 13.5f, 0f }, new[] { -12f, 4f, 90f }
            };
            var list = new List<GateSpot>();
            for (int i = 0; i < g.Length; i++) list.Add(new GateSpot { X = g[i][0], Z = g[i][1], Yaw = g[i][2], Variant = i % 3 });
            return list;
        }

        /// <summary>Where a gate's two posts stand.</summary>
        public static void GatePosts(GateSpot gate, out V3 a, out V3 b)
        {
            V3 along = Turn(new V3(GateWidth / 2f, 0f, 0f), gate.Yaw);
            a = new V3(gate.X - along.X, 0f, gate.Z - along.Z);
            b = new V3(gate.X + along.X, 0f, gate.Z + along.Z);
        }

        /// <summary>Turns a point about the up axis the way the game engine does (yaw 90 turns +Z into +X).</summary>
        public static V3 Turn(V3 p, float yawDegrees)
        {
            double a = yawDegrees * Math.PI / 180.0;
            return new V3((float)(p.X * Math.Cos(a) + p.Z * Math.Sin(a)), p.Y, (float)(-p.X * Math.Sin(a) + p.Z * Math.Cos(a)));
        }

        /// <summary>
        /// The biggest size (up to wanted) at which this tree, standing here and turned this way, stays inside the
        /// glass: under the roof and inside the walls. 0 = it doesn't fit even at the smallest size.
        /// </summary>
        public static float FitScale(int variant, float x, float z, float yaw, float wanted)
        {
            IReadOnlyList<V3> points = RainforestTree.Points(variant);
            for (float scale = wanted; scale >= SmallestTree; scale *= 0.95f)
            {
                bool fits = true;
                for (int i = 0; i < points.Count && fits; i++)
                {
                    V3 p = Turn(points[i] * scale, yaw);
                    float px = x + p.X, pz = z + p.Z;
                    if (Math.Abs(px) > HalfX - 0.35f || Math.Abs(pz) > HalfZ - 0.35f || p.Y > Roof(px, pz) - 0.25f) fits = false;
                }
                if (fits) return scale;
            }
            return 0f;
        }

        private static readonly Dictionary<int, List<TreeSpot>> TreeCache = new Dictionary<int, List<TreeSpot>>();

        /// <summary>
        /// The trees: always the same for the same count. The first two are the treehouse's own trees. Smaller near
        /// the glass walls (where the roof is low), giants toward the middle.
        /// </summary>
        public static List<TreeSpot> Trees(int count)
        {
            count = Math.Max(2, Math.Min(90, count));
            lock (TreeCache)
            {
                List<TreeSpot> cached;
                if (TreeCache.TryGetValue(count, out cached)) return new List<TreeSpot>(cached);
            }

            var spots = new List<TreeSpot> { TreehouseTree(TreeAX, TreeAZ), TreehouseTree(TreeBX, TreeBZ) };
            List<GateSpot> gates = Gates();
            var rng = new Random(20261128);
            for (int tries = 0; tries < 40000 && spots.Count < count; tries++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * (HalfX * 2f - 5f), z = ((float)rng.NextDouble() - 0.5f) * (HalfZ * 2f - 5f);
                int variant = rng.Next(RainforestTree.Variants);
                float wanted = 0.8f + (float)rng.NextDouble() * 0.45f, yaw = (float)rng.NextDouble() * 360f;
                if (!Free(x, z)) continue;
                bool crowded = false;
                foreach (TreeSpot other in spots)
                {
                    // Small trees may stand closer together than giants.
                    float need = Spacing * Math.Max(0.75f, Math.Min(1f, (other.Scale + wanted) / 2f));
                    if (Sq(other.X - x, other.Z - z) < need * need) { crowded = true; break; }
                }
                if (crowded) continue;
                bool inGate = false;
                foreach (GateSpot gate in gates)
                {
                    V3 a, b;
                    GatePosts(gate, out a, out b);
                    if (DistanceToSegment(x, z, a.X, a.Z, b.X, b.Z) < 1.6f) { inGate = true; break; }
                }
                if (inGate) continue;
                float scale = FitScale(variant, x, z, yaw, wanted);
                if (scale <= 0f) continue;
                spots.Add(new TreeSpot { X = x, Z = z, Scale = scale, Yaw = yaw, Variant = variant });
            }
            lock (TreeCache) TreeCache[count] = new List<TreeSpot>(spots);
            return spots;
        }

        /// <summary>
        /// A treehouse tree: of all the shapes and turns, the one whose leaves start highest once it has been sized
        /// to fit under the roof, so the hut sits below the leaves and not inside them.
        /// </summary>
        private static TreeSpot TreehouseTree(float x, float z)
        {
            var best = new TreeSpot { X = x, Z = z, Scale = 0f };
            float bestClear = -1f;
            for (int variant = 0; variant < RainforestTree.Variants; variant++)
            {
                for (float yaw = 0f; yaw < 360f; yaw += 30f)
                {
                    float scale = FitScale(variant, x, z, yaw, 1.2f);
                    float clear = RainforestTree.CanopyBottom(variant) * scale;
                    if (clear > bestClear)
                    {
                        bestClear = clear;
                        best = new TreeSpot { X = x, Z = z, Scale = scale, Yaw = yaw, Variant = variant };
                    }
                }
            }
            return best;
        }

        public static float DistanceToSegment(float px, float pz, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az, len2 = dx * dx + dz * dz;
            float t = len2 < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, ((px - ax) * dx + (pz - az) * dz) / len2));
            return (float)Math.Sqrt(Sq(px - (ax + dx * t), pz - (az + dz * t)));
        }

        private static TreehousePlan _treehouse;

        /// <summary>
        /// The treehouse: a ramp up to a hut on a platform round the first tree, a rope bridge across to a platform
        /// round the second tree, and a slide back down.
        /// </summary>
        public static TreehousePlan Treehouse()
        {
            if (_treehouse != null) return _treehouse;
            var plan = new TreehousePlan();
            var a = new V3(TreeAX, 0f, TreeAZ);
            var b = new V3(TreeBX, 0f, TreeBZ);
            const float halfA = 2.2f, halfB = 2.0f;

            // The two platforms.
            plan.DeckA = Add(plan, "treehouse deck", a + new V3(0f, Deck - 0.1f, 0f), new V3(halfA * 2f, 0.2f, halfA * 2f), JungleLook.Plank, true);
            plan.DeckB = Add(plan, "treehouse deck", b + new V3(0f, Deck - 0.1f, 0f), new V3(halfB * 2f, 0.2f, halfB * 2f), JungleLook.Plank, true);

            // The hut on the first platform: four posts and a stepped roof in her colors. The roof is not solid:
            // a solid roof would shove the camera about when you walk under it.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Add(plan, "hut post", a + new V3(sx * 1.95f, Deck + 1.08f, sz * 1.95f), new V3(0.16f, 2.16f, 0.16f), JungleLook.DarkWood, true);
            Add(plan, "hut roof", a + new V3(0f, Deck + 2.25f, 0f), new V3(4.9f, 0.18f, 4.9f), JungleLook.Orange, false);
            Add(plan, "hut roof", a + new V3(0f, Deck + 2.47f, 0f), new V3(3.3f, 0.18f, 3.3f), JungleLook.Green, false);

            // Rails round both platforms, with gaps for the ramp (west of the first), the bridge (between them) and
            // the slide (south of the second).
            Rail(plan, a + new V3(0f, Deck, halfA - 0.05f), halfA * 2f, true);
            Rail(plan, a + new V3(0f, Deck, -halfA + 0.05f), halfA * 2f, true);
            Rail(plan, a + new V3(halfA - 0.05f, Deck, 1.5f), 1.4f, false);
            Rail(plan, a + new V3(halfA - 0.05f, Deck, -1.5f), 1.4f, false);
            Rail(plan, a + new V3(-halfA + 0.05f, Deck, 1.6f), 1.2f, false);
            Rail(plan, a + new V3(-halfA + 0.05f, Deck, -1.6f), 1.2f, false);
            Rail(plan, b + new V3(0f, Deck, halfB - 0.05f), halfB * 2f, true);
            Rail(plan, b + new V3(halfB - 0.05f, Deck, 0f), halfB * 2f, false);
            Rail(plan, b + new V3(-halfB + 0.05f, Deck, 1.4f), 1.2f, false);
            Rail(plan, b + new V3(-halfB + 0.05f, Deck, -1.4f), 1.2f, false);
            Rail(plan, b + new V3(1.35f, Deck, -halfB + 0.05f), 1.3f, true);
            Rail(plan, b + new V3(-1.35f, Deck, -halfB + 0.05f), 1.3f, true);

            // The ramp up from the floor to the first platform: no steeper than the building's own ramps.
            float x0 = -2.6f, x1 = a.X - halfA, run = x1 - x0;
            float angle = (float)(Math.Atan2(Deck, run) * 180.0 / Math.PI), length = (float)Math.Sqrt(run * run + Deck * Deck);
            float cos = (float)Math.Cos(angle * Math.PI / 180.0), sin = (float)Math.Sin(angle * Math.PI / 180.0);
            // The box's top face runs exactly from the floor at x0 to the platform at x1.
            var rampMid = new V3((x0 + x1) / 2f + 0.1f * sin, Deck / 2f - 0.1f * cos, a.Z);
            plan.Ramp = Add(plan, "treehouse ramp", rampMid, new V3(length, 0.2f, 1.8f), JungleLook.Plank, true);
            plan.Ramp.TiltZ = angle;
            for (int s = -1; s <= 1; s += 2)
            {
                JungleBox side = Add(plan, "ramp rail", new V3((x0 + x1) / 2f - 0.45f * sin, Deck / 2f + 0.45f * cos, a.Z + s * 0.9f), new V3(length, 0.9f, 0.1f), JungleLook.DarkWood, true);
                side.TiltZ = angle;
            }

            // The rope bridge. What you walk on is two smooth unseen boards in a shallow V (planks with gaps could
            // catch a Wobbly's feet); the planks you see lie on them.
            float bx0 = a.X + halfA, bx1 = b.X - halfB, span = bx1 - bx0, sag = 0.2f, halfSpan = span / 2f;
            float sagAngle = (float)(Math.Atan2(sag, halfSpan) * 180.0 / Math.PI), board = (float)Math.Sqrt(halfSpan * halfSpan + sag * sag);
            float sc = (float)Math.Cos(sagAngle * Math.PI / 180.0), ss = (float)Math.Sin(sagAngle * Math.PI / 180.0);
            plan.Bridge1 = Add(plan, "bridge walk", new V3(bx0 + halfSpan / 2f - 0.05f * ss, Deck - sag / 2f - 0.05f * sc, a.Z), new V3(board, 0.1f, 1.5f), JungleLook.Unseen, true);
            plan.Bridge1.TiltZ = -sagAngle;
            plan.Bridge2 = Add(plan, "bridge walk", new V3(bx1 - halfSpan / 2f + 0.05f * ss, Deck - sag / 2f - 0.05f * sc, a.Z), new V3(board, 0.1f, 1.5f), JungleLook.Unseen, true);
            plan.Bridge2.TiltZ = sagAngle;
            const int planks = 8;
            for (int i = 0; i < planks; i++)
            {
                float f = (i + 0.5f) / planks, down = sag * (1f - Math.Abs(2f * f - 1f));
                JungleBox plank = Add(plan, "bridge plank", new V3(bx0 + span * f, Deck - down - 0.045f, a.Z), new V3(span / planks - 0.08f, 0.08f, 1.4f), JungleLook.Plank, false);
                plank.TiltZ = f < 0.5f ? -sagAngle : sagAngle;
            }
            for (int s = -1; s <= 1; s += 2)
            {
                float z = a.Z + s * 0.75f;
                var from = new V3(bx0, Deck + 1.0f, z);
                var to = new V3(bx1, Deck + 1.0f, z);
                V3 mid = V3.Lerp(from, to, 0.5f) + new V3(0f, -0.28f, 0f);
                plan.Ropes.Add(new JungleRope { From = from, To = mid, Radius = 0.04f });
                plan.Ropes.Add(new JungleRope { From = mid, To = to, Radius = 0.04f });
                for (int i = 0; i <= 4; i++)
                {
                    float f = i / 4f, dip = 1f - Math.Abs(2f * f - 1f);
                    V3 top = V3.Lerp(from, to, f) + new V3(0f, -0.28f * dip, 0f);
                    plan.Ropes.Add(new JungleRope { From = new V3(top.X, Deck - sag * dip - 0.05f, z), To = top, Radius = 0.025f });
                }
                // What actually stops a Wobbly: an unseen wall along each side.
                Add(plan, "bridge side", new V3((bx0 + bx1) / 2f, Deck + 0.4f, z), new V3(span, 1.4f, 0.08f), JungleLook.Unseen, true);
            }

            // The slide down from the second platform, with a zone on it that keeps you sliding.
            float zTop = b.Z - halfB, zEnd = 12.4f, sRun = zTop - zEnd, endHeight = 0.25f, sDrop = Deck - endHeight;
            float sAngle = (float)(Math.Atan2(sDrop, sRun) * 180.0 / Math.PI), sLength = (float)Math.Sqrt(sRun * sRun + sDrop * sDrop);
            float cs = (float)Math.Cos(sAngle * Math.PI / 180.0), sn = (float)Math.Sin(sAngle * Math.PI / 180.0);
            // Top face from the platform edge (zTop, Deck) down to (zEnd, endHeight).
            var slideMid = new V3(b.X, (Deck + endHeight) / 2f - 0.075f * cs, (zTop + zEnd) / 2f + 0.075f * sn);
            plan.Slide = Add(plan, "slide", slideMid, new V3(1.4f, 0.15f, sLength), JungleLook.Orange, true);
            plan.Slide.TiltX = -sAngle;
            plan.Slide.Slippery = true;
            for (int s = -1; s <= 1; s += 2)
            {
                JungleBox lip = Add(plan, "slide side", new V3(b.X + s * 0.75f, (Deck + endHeight) / 2f + 0.2f * cs, (zTop + zEnd) / 2f - 0.2f * sn), new V3(0.1f, 0.55f, sLength), JungleLook.Green, true);
                lip.TiltX = -sAngle;
                lip.Slippery = true;
            }
            JungleBox push = Add(plan, "slide push", new V3(b.X, (Deck + endHeight) / 2f + 0.7f * cs, (zTop + zEnd) / 2f - 0.7f * sn), new V3(1.3f, 1.4f, sLength - 0.4f), JungleLook.Unseen, false);
            push.TiltX = -sAngle;
            push.Trigger = true;
            plan.SlideDown = new V3(0f, -sn, -cs);

            // Parrots keeping watch: on the hut roof and on the rails.
            plan.Birds.Add(new BirdSpot { At = a + new V3(1.0f, HutRoofTop, 0.6f), Yaw = 200f });
            plan.Birds.Add(new BirdSpot { At = a + new V3(-2.0f, Deck + 2.34f, -2.0f), Yaw = 20f });
            plan.Birds.Add(new BirdSpot { At = b + new V3(halfB - 0.05f, Deck + 1.0f, 0.8f), Yaw = 270f });
            plan.Birds.Add(new BirdSpot { At = b + new V3(-0.6f, Deck + 1.0f, halfB - 0.05f), Yaw = 180f });
            _treehouse = plan;
            return plan;
        }

        private static JungleBox Add(TreehousePlan plan, string name, V3 center, V3 size, JungleLook look, bool solid)
        {
            var box = new JungleBox { Name = name, Center = center, Size = size, Look = look, Solid = solid };
            plan.Boxes.Add(box);
            return box;
        }

        /// <summary>A waist-high fence piece standing on a platform: two bars you see and one unseen wall that stops you.</summary>
        private static void Rail(TreehousePlan plan, V3 at, float length, bool alongX)
        {
            Add(plan, "treehouse rail", at + new V3(0f, 0.95f, 0f), alongX ? new V3(length, 0.1f, 0.1f) : new V3(0.1f, 0.1f, length), JungleLook.DarkWood, false);
            Add(plan, "treehouse rail", at + new V3(0f, 0.5f, 0f), alongX ? new V3(length, 0.08f, 0.08f) : new V3(0.08f, 0.08f, length), JungleLook.DarkWood, false);
            Add(plan, "rail solid", at + new V3(0f, 0.6f, 0f), alongX ? new V3(length, 1.2f, 0.1f) : new V3(0.1f, 1.2f, length), JungleLook.Unseen, true);
        }
    }
}
