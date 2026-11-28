using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    /// <summary>
    /// Rainforest giants for the top floor of Skylar's big aquarium: a leaning trunk with wide buttress roots, a few
    /// big branches, a canopy of leaf clumps, hanging vines and blossoms. Six variants, each always the same
    /// (seeded), about 8.5 m tall with a canopy about 6 m across; the game scales and turns them for variety.
    ///
    /// For the frame rate each tree is only FOUR meshes (wood, light leaves, dark leaves, blossoms): everything of
    /// one color is merged, so sixty trees cost about as much to draw as one busy fish tank.
    /// Stands on y = 0 at the origin.
    /// </summary>
    public static class RainforestTree
    {
        public const int Variants = 6;

        public static readonly Rgb Wood = new Rgb(104, 76, 52);
        public static readonly Rgb Leaf = new Rgb(64, 160, 58);
        public static readonly Rgb LeafDark = new Rgb(30, 110, 52);
        public static readonly Rgb Blossom = new Rgb(255, 150, 60);

        private static readonly V3 One = new V3(1f, 1f, 1f);

        private sealed class Plan
        {
            public readonly ShapeMesh Wood = new ShapeMesh(), Leaf = new ShapeMesh(), LeafDark = new ShapeMesh(), Blossom = new ShapeMesh();
            public readonly List<V3> Perches = new List<V3>();
            public float Height;
        }

        private static readonly Dictionary<int, Plan> Plans = new Dictionary<int, Plan>();

        /// <summary>The tree as four colored meshes (blossoms may be empty on some variants: then three).</summary>
        public static List<ColoredMesh> Build(int variant)
        {
            Plan p = PlanFor(variant);
            var parts = new List<ColoredMesh> { new ColoredMesh(p.Wood, Wood), new ColoredMesh(p.Leaf, Leaf), new ColoredMesh(p.LeafDark, LeafDark) };
            if (p.Blossom.Triangles.Count > 0) parts.Add(new ColoredMesh(p.Blossom, Blossom));
            return parts;
        }

        /// <summary>Spots on top of the big branches where a bird can sit.</summary>
        public static List<V3> Perches(int variant)
        {
            return new List<V3>(PlanFor(variant).Perches);
        }

        public static float Height(int variant)
        {
            return PlanFor(variant).Height;
        }

        private static Plan PlanFor(int variant)
        {
            variant = ((variant % Variants) + Variants) % Variants;
            Plan plan;
            lock (Plans)
            {
                if (!Plans.TryGetValue(variant, out plan))
                {
                    plan = Grow(variant);
                    Plans[variant] = plan;
                }
            }
            return plan;
        }

        private static Plan Grow(int variant)
        {
            var rng = new Random(9100 + variant * 37);
            var p = new Plan();
            Func<float> r01 = () => (float)rng.NextDouble();

            // The trunk: thick at the foot, leaning a little, on a gentle curve.
            float tall = 5.4f + r01() * 1.2f;
            float leanA = r01() * 6.28f, lean = 0.3f + r01() * 0.7f;
            var top = new V3((float)Math.Cos(leanA) * lean, tall, (float)Math.Sin(leanA) * lean);
            List<V3> trunk = Shapes.Bezier(V3.Zero, new V3(-top.X * 0.4f, tall * 0.35f, -top.Z * 0.4f), new V3(top.X * 0.3f, tall * 0.7f, top.Z * 0.3f), top, 6);
            var trunkR = new List<float>();
            for (int i = 0; i < trunk.Count; i++) trunkR.Add(0.56f - 0.26f * i / (trunk.Count - 1));
            p.Wood.Append(Shapes.Tube(trunk, trunkR, 10));

            // Buttress roots: fins spreading from the trunk to the ground.
            int roots = 5 + rng.Next(3);
            for (int i = 0; i < roots; i++)
            {
                float a = i * 6.28f / roots + r01() * 0.5f, reach = 1.5f + r01() * 0.9f, up = 1.5f + r01() * 0.9f;
                var foot = new V3((float)Math.Cos(a) * reach, 0.03f, (float)Math.Sin(a) * reach);
                var hip = new V3((float)Math.Cos(a) * 0.3f, up, (float)Math.Sin(a) * 0.3f);
                var mid = new V3((float)Math.Cos(a) * reach * 0.45f, up * 0.3f, (float)Math.Sin(a) * reach * 0.45f);
                p.Wood.Append(Shapes.Tube(new List<V3> { hip, mid, foot }, new List<float> { 0.2f, 0.17f, 0.07f }, 7));
            }

            // Big branches reaching out and up from the top of the trunk, each ending in clumps of leaves.
            int branches = 4 + rng.Next(2);
            var clumps = new List<V3> { top + new V3(0f, 1.3f, 0f) };
            for (int i = 0; i < branches; i++)
            {
                float a = i * 6.28f / branches + r01() * 0.7f, reach = 2.0f + r01() * 1.3f, rise = 0.7f + r01() * 1.4f;
                var from = V3.Lerp(trunk[trunk.Count - 3], top, r01());
                var tip = top + new V3((float)Math.Cos(a) * reach, rise, (float)Math.Sin(a) * reach);
                var elbow = V3.Lerp(from, tip, 0.5f) + new V3(0f, 0.35f, 0f);
                p.Wood.Append(Shapes.Tube(new List<V3> { from, elbow, tip }, new List<float> { 0.2f, 0.14f, 0.06f }, 7));
                p.Perches.Add(elbow + new V3(0f, 0.16f, 0f));
                clumps.Add(tip);
                clumps.Add(V3.Lerp(elbow, tip, 0.55f) + new V3((r01() - 0.5f) * 1.2f, 0.6f + r01() * 0.5f, (r01() - 0.5f) * 1.2f));
            }

            // The canopy: one rounded clump of leaves at each branch tip, in two greens.
            float highest = 0f;
            for (int i = 0; i < clumps.Count; i++)
            {
                var radii = new V3(1.3f + r01() * 0.8f, 0.7f + r01() * 0.45f, 1.3f + r01() * 0.8f);
                ShapeMesh clump = Shapes.Ellipsoid(radii, 10, 6).Transformed(One, r01() * 360f, 0f, clumps[i]);
                (i % 3 == 1 ? p.LeafDark : p.Leaf).Append(clump);
                highest = Math.Max(highest, clumps[i].Y + radii.Y);
                // Blossoms on top of some clumps (not every tree flowers).
                if (variant % 2 == 0 && i % 2 == 0)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        var at = clumps[i] + new V3((r01() - 0.5f) * radii.X, radii.Y * 0.85f, (r01() - 0.5f) * radii.Z);
                        p.Blossom.Append(Shapes.Ellipsoid(new V3(0.2f, 0.14f, 0.2f), 6, 4).Transformed(One, 0f, 0f, at));
                    }
                }
            }

            // Vines hanging from the canopy, with a leaf or two on the way down.
            int vines = 4 + rng.Next(3);
            for (int i = 0; i < vines; i++)
            {
                V3 from = clumps[1 + rng.Next(clumps.Count - 1)] + new V3((r01() - 0.5f) * 1.6f, -0.5f, (r01() - 0.5f) * 1.6f);
                float drop = 1.6f + r01() * 2.4f;
                var end = new V3(from.X + (r01() - 0.5f) * 0.4f, Math.Max(1.9f, from.Y - drop), from.Z + (r01() - 0.5f) * 0.4f);
                var belly = V3.Lerp(from, end, 0.5f) + new V3((r01() - 0.5f) * 0.3f, 0f, (r01() - 0.5f) * 0.3f);
                p.LeafDark.Append(Shapes.Tube(new List<V3> { from, belly, end }, new List<float> { 0.04f, 0.035f, 0.02f }, 5));
                p.Leaf.Append(Shapes.Ellipsoid(new V3(0.16f, 0.05f, 0.1f), 6, 4).Transformed(One, r01() * 360f, 0f, belly));
                p.Leaf.Append(Shapes.Ellipsoid(new V3(0.14f, 0.05f, 0.09f), 6, 4).Transformed(One, r01() * 360f, 0f, end));
            }
            p.Height = Math.Max(Math.Max(p.Wood.Max.Y, p.Leaf.Max.Y), Math.Max(p.LeafDark.Max.Y, p.Blossom.Triangles.Count > 0 ? p.Blossom.Max.Y : 0f));
            return p;
        }

        /// <summary>
        /// A vine gate to walk through: two thin posts, a branch across the top (at y = height), and a curtain of
        /// vines hanging from it down to about waist height. Spans x from -width/2 to +width/2. Three colors.
        /// </summary>
        public static List<ColoredMesh> VineGate(int variant, float width, float height)
        {
            var rng = new Random(4400 + variant * 13);
            Func<float> r01 = () => (float)rng.NextDouble();
            ShapeMesh wood = new ShapeMesh(), leaf = new ShapeMesh(), dark = new ShapeMesh();
            float half = width / 2f;
            foreach (int s in new[] { -1, 1 })
            {
                wood.Append(Shapes.Tube(new List<V3> { new V3(s * half, 0f, 0f), new V3(s * (half - 0.1f), height * 0.6f, 0.05f * s), new V3(s * (half - 0.05f), height, 0f) },
                                        new List<float> { 0.1f, 0.08f, 0.07f }, 7));
            }
            wood.Append(Shapes.Tube(new List<V3> { new V3(-half - 0.2f, height, 0f), new V3(0f, height + 0.18f, 0f), new V3(half + 0.2f, height, 0f) }, new List<float> { 0.07f, 0.09f, 0.07f }, 7));
            int strands = Math.Max(6, (int)(width * 4f));
            for (int i = 0; i < strands; i++)
            {
                float x = -half + 0.2f + (width - 0.4f) * i / (strands - 1);
                float bottom = 0.75f + r01() * 1.1f;
                var from = new V3(x, height + 0.05f, (r01() - 0.5f) * 0.2f);
                var end = new V3(x + (r01() - 0.5f) * 0.3f, bottom, (r01() - 0.5f) * 0.35f);
                var belly = V3.Lerp(from, end, 0.5f) + new V3((r01() - 0.5f) * 0.25f, 0f, (r01() - 0.5f) * 0.25f);
                dark.Append(Shapes.Tube(new List<V3> { from, belly, end }, new List<float> { 0.035f, 0.03f, 0.018f }, 5));
                int leaves = 2 + rng.Next(3);
                for (int k = 0; k < leaves; k++)
                {
                    V3 at = V3.Lerp(from, end, 0.25f + 0.75f * k / leaves);
                    (k % 2 == 0 ? leaf : dark).Append(Shapes.Ellipsoid(new V3(0.15f, 0.045f, 0.1f), 6, 4).Transformed(One, r01() * 360f, 0f, at));
                }
            }
            return new List<ColoredMesh> { new ColoredMesh(wood, Wood), new ColoredMesh(leaf, Leaf), new ColoredMesh(dark, LeafDark) };
        }
    }

    /// <summary>
    /// A parrot for the rainforest: a round body, a head with a hooked beak, and a long tail. About 0.5 long before
    /// scaling, beak toward +Z, sitting on y = 0. The wings are separate (so the game can flap them): each wing's
    /// own origin is its shoulder.
    /// </summary>
    public static class BirdModel
    {
        private static readonly V3 One = new V3(1f, 1f, 1f);
        private static readonly Rgb Beak = new Rgb(255, 205, 60), White = new Rgb(250, 250, 250), Black = new Rgb(10, 10, 10);

        public static List<ColoredMesh> Body(Rgb main, Rgb chest)
        {
            var parts = new List<ColoredMesh>();
            parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.085f, 0.1f, 0.15f), 10, 6).Transformed(One, 0f, -18f, new V3(0f, 0.13f, 0f)), main));
            parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.07f, 0.075f, 0.1f), 8, 5).Transformed(One, 0f, 0f, new V3(0f, 0.11f, 0.05f)), chest));
            var head = new ShapeMesh();
            head.Append(Shapes.Ellipsoid(new V3(0.07f, 0.07f, 0.075f), 10, 6).Transformed(One, 0f, 0f, new V3(0f, 0.26f, 0.1f)));
            parts.Add(new ColoredMesh(head, main));
            // The beak: a cone tipped forward and down.
            parts.Add(new ColoredMesh(Shapes.Cone(0.035f, 0.09f, 8).Transformed(One, 0f, 115f, new V3(0f, 0.255f, 0.165f)), Beak));
            var eyes = new ShapeMesh();
            var pupils = new ShapeMesh();
            foreach (int s in new[] { -1, 1 })
            {
                eyes.Append(Shapes.Ellipsoid(new V3(0.016f, 0.022f, 0.022f), 6, 4).Transformed(One, 0f, 0f, new V3(s * 0.062f, 0.275f, 0.125f)));
                pupils.Append(Shapes.Ellipsoid(new V3(0.008f, 0.012f, 0.012f), 6, 4).Transformed(One, 0f, 0f, new V3(s * 0.074f, 0.275f, 0.13f)));
            }
            parts.Add(new ColoredMesh(eyes, White));
            parts.Add(new ColoredMesh(pupils, Black));
            // The tail: a long flat feather behind, and two feet.
            var tail = new List<V3> { new V3(-0.045f, 0f, 0f), new V3(0.045f, 0f, 0f), new V3(0.02f, -0.3f, 0f), new V3(-0.02f, -0.3f, 0f) };
            parts.Add(new ColoredMesh(Shapes.Extrude(tail, 0.02f).Transformed(One, 0f, 70f, new V3(0f, 0.09f, -0.12f)), chest));
            var feet = new ShapeMesh();
            foreach (int s in new[] { -1, 1 }) feet.Append(Shapes.Cylinder(0.012f, 0.06f, 6).Transformed(One, 0f, 0f, new V3(s * 0.035f, 0f, 0.01f)));
            parts.Add(new ColoredMesh(feet, Beak));
            return parts;
        }

        /// <summary>One wing, lying flat, reaching out along X from its shoulder at the origin. side = -1 left, +1 right.</summary>
        public static List<ColoredMesh> Wing(Rgb color, Rgb tip, int side)
        {
            float s = side < 0 ? -1f : 1f;
            var inner = new List<V3> { new V3(0f, -0.08f, 0f), new V3(0.17f * s, -0.07f, 0f), new V3(0.17f * s, 0.07f, 0f), new V3(0f, 0.1f, 0f) };
            var outer = new List<V3> { new V3(0.17f * s, -0.07f, 0f), new V3(0.34f * s, -0.03f, 0f), new V3(0.33f * s, 0.03f, 0f), new V3(0.17f * s, 0.07f, 0f) };
            return new List<ColoredMesh>
            {
                new ColoredMesh(Shapes.Extrude(inner, 0.018f).Transformed(One, 0f, 90f, V3.Zero), color),
                new ColoredMesh(Shapes.Extrude(outer, 0.014f).Transformed(One, 0f, 90f, V3.Zero), tip)
            };
        }
    }
}
