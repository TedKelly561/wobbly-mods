using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    /// <summary>
    /// Skylar's redtail catfish, made in code from the shape library (no Unity kit needed): broad round head,
    /// dark back, cream belly and lower lip, bright orange-red forked tail, whiskers. Length exactly 1 along Z:
    /// nose at z = +0.5, tail tip at z = -0.5, centered on X, belly down. First draft by the local worker (job 512);
    /// rounded out by the PM with the preview renderer.
    /// Outlines for fins are drawn in XY and turned with yaw 90, which maps outline x to -Z (so x = distance
    /// toward the tail) and the slab's thickness to X.
    /// </summary>
    public static class RedtailModel
    {
        public static readonly Rgb Body = new Rgb(40, 40, 46);
        public static readonly Rgb Belly = new Rgb(245, 235, 205);
        public static readonly Rgb Tail = new Rgb(255, 77, 13);
        public static readonly Rgb Whisker = new Rgb(60, 60, 66);
        public static readonly Rgb EyeWhite = new Rgb(255, 255, 255);
        public static readonly Rgb Pupil = new Rgb(0, 0, 0);

        public static List<ColoredMesh> Build()
        {
            var parts = new List<ColoredMesh>();
            V3 one = new V3(1f, 1f, 1f);

            // Body: a smooth tube, rounded nose, broad head, tapering to the tail. Slightly flattened.
            float[] z = { 0.5f, 0.49f, 0.47f, 0.44f, 0.40f, 0.34f, 0.26f, 0.16f, 0.05f, -0.06f, -0.16f, -0.25f, -0.32f };
            float[] r = { 0f, 0.045f, 0.08f, 0.11f, 0.135f, 0.155f, 0.165f, 0.16f, 0.14f, 0.115f, 0.085f, 0.06f, 0.045f };
            var path = new List<V3>();
            var radii = new List<float>();
            for (int i = 0; i < z.Length; i++) { path.Add(new V3(0f, 0f, z[i])); radii.Add(r[i]); }
            parts.Add(new ColoredMesh(Shapes.Tube(path, radii, 24).Transformed(new V3(1f, 0.85f, 1f), 0f, 0f, V3.Zero), Body));

            // Cream belly under the front two thirds, and the cream lower lip.
            parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.15f, 0.1f, 0.3f), 24, 14).Transformed(one, 0f, 0f, new V3(0f, -0.07f, 0.13f)), Belly));
            parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.105f, 0.035f, 0.07f), 16, 8).Transformed(one, 0f, 0f, new V3(0f, -0.035f, 0.435f)), Belly));

            // Tail fin: forked, with rounded lobes.
            parts.Add(Fin(Tail, 0.02f,
                0.26f, -0.035f, 0.36f, -0.12f, 0.44f, -0.17f, 0.49f, -0.165f, 0.5f, -0.13f, 0.47f, -0.06f, 0.44f, 0f,
                0.47f, 0.06f, 0.5f, 0.13f, 0.49f, 0.165f, 0.44f, 0.17f, 0.36f, 0.12f, 0.26f, 0.035f));

            // Back fin (rounded, swept back), and the small red fin behind it.
            parts.Add(Fin(Body, 0.016f,
                -0.12f, 0.1f, -0.08f, 0.2f, -0.02f, 0.27f, 0.05f, 0.3f, 0.1f, 0.28f, 0.13f, 0.2f, 0.14f, 0.1f));
            parts.Add(Fin(Tail, 0.014f,
                0.19f, 0.03f, 0.22f, 0.13f, 0.27f, 0.16f, 0.31f, 0.13f, 0.33f, 0.03f));

            // Side fins: rounded paddles lying flat, just behind the head. Built separately, never mirrored by scale.
            for (int s = -1; s <= 1; s += 2)
            {
                var outline = new List<V3>
                {
                    new V3(0f, 0.05f, 0f), new V3(0.09f * s, 0.06f, 0f), new V3(0.15f * s, 0.02f, 0f),
                    new V3(0.16f * s, -0.05f, 0f), new V3(0.11f * s, -0.1f, 0f), new V3(0f, -0.05f, 0f)
                };
                parts.Add(new ColoredMesh(Shapes.Extrude(outline, 0.014f).Transformed(one, 0f, 90f, new V3(0.1f * s, -0.08f, 0.15f)), Tail));
            }

            // Whiskers: two long ones sweeping out and back from the mouth corners, two short ones under the chin.
            for (int s = -1; s <= 1; s += 2)
            {
                parts.Add(new ColoredMesh(Barbel(0.012f, 14,
                    new V3(0.1f * s, -0.03f, 0.44f), new V3(0.25f * s, -0.01f, 0.42f),
                    new V3(0.29f * s, -0.1f, 0.22f), new V3(0.24f * s, -0.17f, 0.02f)), Whisker));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                parts.Add(new ColoredMesh(Barbel(0.008f, 8,
                    new V3(0.04f * s, -0.06f, 0.44f), new V3(0.06f * s, -0.12f, 0.43f),
                    new V3(0.07f * s, -0.16f, 0.38f), new V3(0.07f * s, -0.18f, 0.32f)), Whisker));
            }

            // Eyes: big and friendly, high on each side of the head.
            for (int s = -1; s <= 1; s += 2)
            {
                parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.036f, 0.036f, 0.036f), 14, 8).Transformed(one, 0f, 0f, new V3(0.1f * s, 0.075f, 0.37f)), EyeWhite));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.02f, 0.022f, 0.02f), 10, 6).Transformed(one, 0f, 0f, new V3(0.12f * s, 0.082f, 0.388f)), Pupil));
            }

            return parts;
        }

        /// <summary>
        /// A light version for crowds (the catfish swarm: 100 at once): dark body, cream belly, red tail, three
        /// parts and few triangles. Same size and direction as Build(): length 1 along Z, nose at +0.5.
        /// </summary>
        public static List<ColoredMesh> BuildSimple()
        {
            var parts = new List<ColoredMesh>();
            float[] z = { 0.5f, 0.44f, 0.3f, 0.1f, -0.12f, -0.3f };
            float[] r = { 0f, 0.11f, 0.165f, 0.15f, 0.08f, 0.045f };
            var path = new List<V3>();
            var radii = new List<float>();
            for (int i = 0; i < z.Length; i++) { path.Add(new V3(0f, 0f, z[i])); radii.Add(r[i]); }
            parts.Add(new ColoredMesh(Shapes.Tube(path, radii, 8).Transformed(new V3(1f, 0.85f, 1f), 0f, 0f, V3.Zero), Body));
            parts.Add(new ColoredMesh(Shapes.Ellipsoid(new V3(0.14f, 0.09f, 0.28f), 8, 5).Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(0f, -0.07f, 0.13f)), Belly));
            parts.Add(Fin(Tail, 0.03f, 0.26f, -0.035f, 0.44f, -0.17f, 0.5f, -0.13f, 0.44f, 0f, 0.5f, 0.13f, 0.44f, 0.17f, 0.26f, 0.035f));
            return parts;
        }

        /// <summary>A flat fin standing in the Y-Z plane. Pairs are (distance toward the tail, height).</summary>
        private static ColoredMesh Fin(Rgb color, float thickness, params float[] xy)
        {
            var outline = new List<V3>();
            for (int i = 0; i + 1 < xy.Length; i += 2) outline.Add(new V3(xy[i], xy[i + 1], 0f));
            return new ColoredMesh(Shapes.Extrude(outline, thickness).Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero), color);
        }

        /// <summary>A whisker: a tube along a Bezier curve, tapering to a third of its width.</summary>
        private static ShapeMesh Barbel(float radius, int steps, V3 p0, V3 p1, V3 p2, V3 p3)
        {
            List<V3> path = Shapes.Bezier(p0, p1, p2, p3, steps);
            var radii = new List<float>();
            for (int i = 0; i < path.Count; i++) radii.Add(radius * (1f - 0.65f * i / (path.Count - 1)));
            return Shapes.Tube(path, radii, 8);
        }
    }
}
