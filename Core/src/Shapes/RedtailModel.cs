using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
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

            // 1. Body: Tube along Z from nose (z=0.5) to tail base (z=-0.3).
            // Radii: 0.03 at z=0.5, 0.1 at z=0.42, 0.14 at z=0.25, tapering to 0.04 at z=-0.3.
            var bodyPath = new List<V3> {
                new V3(0f, 0f, 0.5f),
                new V3(0f, 0f, 0.42f),
                new V3(0f, 0f, 0.25f),
                new V3(0f, 0f, 0.1f),
                new V3(0f, 0f, -0.1f),
                new V3(0f, 0f, -0.3f)
            };
            var bodyRadii = new List<float> { 0.03f, 0.1f, 0.14f, 0.12f, 0.06f, 0.04f };
            var bodyMesh = Shapes.Tube(bodyPath, bodyRadii, 14);
            // Flatten Y by 0.8 to make it broader/flat-headed look
            var bodyTransformed = bodyMesh.Transformed(new V3(1f, 0.8f, 1f), 0f, 0f, V3.Zero);
            parts.Add(new ColoredMesh(bodyTransformed, Body));

            // 2. Belly: Ellipsoid under the front two-thirds.
            // Sticking out below and slightly to sides. Center around z=0.15, y=-0.1.
            // Slightly wider than the head and tucked up under it, so a cream band shows low on the sides and below.
            var bellyMesh = Shapes.Ellipsoid(new V3(0.14f, 0.07f, 0.25f), 16, 10);
            var bellyTransformed = bellyMesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(0f, -0.07f, 0.15f));
            parts.Add(new ColoredMesh(bellyTransformed, Belly));

            // 3. Tail fin: Extrude of a forked outline in XY, then turn with yaw 90.
            // Outline in XY: x is length along fish (mapped to -Z after yaw), y is height.
            // Must reach from z=-0.28 to z=-0.5.
            // In XY plane before rotation: x corresponds to distance from tail base.
            // Tail base is at z=-0.3. Tip at z=-0.5. So length along fish axis is 0.2.
            // Let's define outline in local X (along body) and Y (height).
            // Points: (0.28, -0.02), (0.5, -0.15), (0.44, 0.0), (0.5, 0.15), (0.28, 0.02)
            // After yaw 90: X becomes Z' = -X_local? No.
            // Transformed(..., 90f, ...) description: "yaw 90 turns +Z into +X".
            // Standard Yaw about Y: x' = x*cos(y) + z*sin(y), z' = -x*sin(y) + z*cos(y).
            // If we have a point (x, 0, z) in local space.
            // We want the "length" of the fin (which is along local X in the outline definition) to become Z in world space?
            // The prompt says: "make the outline in XY (x is the length along the fish...), then turn it with Transformed(..., 90f...) so x becomes -Z".
            // Let's check Transformed logic.
            // R_yaw * [x, y, z]^T.
            // If we want local X to map to world -Z:
            // World Z = -local X.
            // From matrix: z' = -sy*x + cy*z. If sy=1 (yaw 90), z' = -x. Correct.
            // So local X becomes -Z. Local Y stays Y. Local Z becomes X.
            // The outline is in XY plane (local Z=0).
            // So after rotation: World X = local Z * sy = 0? No, wait.
            // x' = cy*x + sy*z. If z=0, x' = cy*x. At yaw 90, cy=0. So x'=0.
            // This means the fin would be flat against the YZ plane at X=0?
            // The prompt says "thickness becomes X".
            // Extrude creates a slab from z=-t/2 to z=t/2 in local space.
            // Local Z becomes World X (since x' = sy*z = 1*z = z).
            // So the thickness is along World X. This matches "sticking out to +/-X" if we offset it?
            // No, the tail fin is at the back of the fish, centered on X=0.
            // So local Z (thickness) becomes World X. The fin lies in Y-Z plane.
            // Local X (length) becomes World -Z.
            // Let's verify coordinates.
            // Outline points (x_out, y_out, 0).
            // After yaw 90:
            // W_X = x_out * 0 + z_local * 1 = 0? Wait.
            // Matrix row 0: [cy, 0, sy]. With yaw 90: [0, 0, 1].
            // So W_X = z_local.
            // Matrix row 2: [-sy, 0, cy]. With yaw 90: [-1, 0, 0].
            // So W_Z = -x_out.
            // The outline X (length) maps to -Z.
            // The outline Y (height) maps to Y.
            // The Extrude thickness (local Z) maps to W_X.
            // This works perfectly for a tail fin centered at X=0.
            
            var tailOutline = new List<V3> {
                new V3(0.28f, -0.02f, 0f),
                new V3(0.5f, -0.15f, 0f),
                new V3(0.44f, 0.0f, 0f),
                new V3(0.5f, 0.15f, 0f),
                new V3(0.28f, 0.02f, 0f)
            };
            // Centroid check: X avg = (0.28+0.5+0.44+0.5+0.28)/5 = 2.0/5 = 0.4. Y avg approx 0.
            // Star-shaped around centroid? Yes, convex-ish forked shape.
            var tailMesh = Shapes.Extrude(tailOutline, 0.015f);
            // Apply yaw 90 to orient it correctly along the fish's Z axis.
            // The extruded mesh is in local XY (thickness along Z).
            // We want thickness along World X.
            // Transformed with yaw 90: Local Z -> World X. Correct.
            // Local X -> World -Z.
            // The outline X range was 0.28 to 0.5.
            // So World Z range is -0.5 to -0.28. This matches the requirement (tail tip at -0.5).
            var tailTransformed = tailMesh.Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero);
            parts.Add(new ColoredMesh(tailTransformed, Tail));

            // 4. Back fin (dorsal): Extrude of rounded triangle on top of body around z=0.0.
            // Outline in XY (x along length, y height).
            // Triangle shape: base at x=0, tip at x=0.2? Or symmetric?
            // "Rounded triangle". Let's make a small fin.
            // Outline in XY: x along the fish (yaw 90 turns +x into -Z), y is up. A swept-back triangle
            // centred on z = 0 (x from -0.15 to 0.15 becomes z from +0.15 to -0.15) whose base sits in the back.
            var dorsalOutline = new List<V3> {
                new V3(-0.15f, 0.06f, 0f),
                new V3(0.05f, 0.22f, 0f),   // tip, swept back toward the tail
                new V3(0.15f, 0.06f, 0f)
            };
            var dorsalMesh = Shapes.Extrude(dorsalOutline, 0.01f);
            // Yaw 90: the thickness goes along X and the fin lies in the Y-Z plane, standing on the back.
            var dorsalTransformed = dorsalMesh.Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero);
            parts.Add(new ColoredMesh(dorsalTransformed, Body));

            // Small Tail-colored fin behind it (z -0.2).
            var analOutline = new List<V3> {
                new V3(0f, 0.04f, 0f),
                new V3(0.1f, 0.15f, 0f),
                new V3(0.2f, 0.04f, 0f)
            };
            var analMesh = Shapes.Extrude(analOutline, 0.01f);
            // Offset to z=-0.2 (which is local x=0.2 in the extruded mesh before rotation? No.)
            // After yaw 90, local X becomes -Z.
            // We want World Z = -0.2. So Local X should be 0.2.
            // The outline X range is 0 to 0.2.
            // So the fin will span World Z from -0.2 to 0? No, -0.2 to 0 is wrong direction.
            // Local X=0 -> World Z=0. Local X=0.2 -> World Z=-0.2.
            // So it spans z=0 to z=-0.2. We want it at z=-0.2.
            // Let's shift the outline X values.
            var analOutlineShifted = new List<V3> {
                new V3(0.2f, 0.04f, 0f),
                new V3(0.3f, 0.15f, 0f),
                new V3(0.4f, 0.04f, 0f)
            };
            var analMeshShifted = Shapes.Extrude(analOutlineShifted, 0.01f);
            // The small Tail-coloured fin sits on the back behind the dorsal fin (z from -0.2 to -0.4).
            var analTransformed = analMeshShifted.Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero);
            // Actually, prompt says "smaller Tail-colored fin behind it (z -0.2)". It doesn't specify Y offset.
            // Let's place it slightly below Z axis to look natural.
            parts.Add(new ColoredMesh(analTransformed, Tail));

            // 5. Side fins (pectoral): Just behind the head, sticking out to +X and -X.
            // Head is around z=0.25. Behind means lower Z? Or higher Z?
            // Nose is +0.5. Head center ~0.25. "Behind" usually means towards tail (lower Z).
            // Let's place at z=0.1.
            // Sticking out to +/-X.
            // Build left and right separately with positive scale.
            
            // Fin shape: small triangle/quad in XY plane (x along body, y up).
            // Flat fins sticking out sideways. Outline in XY: x is how far out, y is along the body
            // (pitch 90 turns y into +Z and the thickness into the vertical). The base starts inside the
            // head (x = 0 at the offset) and the tip is swept back toward the tail. The left one is built
            // with mirrored coordinates, not a negative scale.
            var pecRightOutline = new List<V3> {
                new V3(0f, 0.04f, 0f),
                new V3(0f, -0.04f, 0f),
                new V3(0.19f, -0.09f, 0f)
            };
            var pecLeftOutline = new List<V3> {
                new V3(0f, 0.04f, 0f),
                new V3(0f, -0.04f, 0f),
                new V3(-0.19f, -0.09f, 0f)
            };
            var pecRightMesh = Shapes.Extrude(pecRightOutline, 0.01f);
            var pecLeftMesh = Shapes.Extrude(pecLeftOutline, 0.01f);
            var pecRight = pecRightMesh.Transformed(new V3(1f, 1f, 1f), 0f, 90f, new V3(0.09f, -0.06f, 0.1f));
            parts.Add(new ColoredMesh(pecRight, Tail));
            var pecLeft = pecLeftMesh.Transformed(new V3(1f, 1f, 1f), 0f, 90f, new V3(-0.09f, -0.06f, 0.1f));
            parts.Add(new ColoredMesh(pecLeft, Tail));

            // 6. Whiskers (barbels): 2 long from mouth corners, 2 short under chin.
            // Mouth at nose tip z=0.5. Corners at x=+/-0.08, y=-0.05?
            // Sweeping out and back.
            
            // Long whisker Right: Start (0.08, -0.05, 0.45), End (-0.2, -0.1, -0.3)
            var longWhiskerRightPath = Shapes.Bezier(
                new V3(0.08f, -0.05f, 0.45f),
                new V3(0.15f, -0.08f, 0.2f),
                new V3(0.1f, -0.1f, -0.1f),
                new V3(-0.2f, -0.12f, -0.3f),
                10
            );
            var longWhiskerRightRadii = new List<float>(new float[longWhiskerRightPath.Count]);
            for(int i=0; i<longWhiskerRightRadii.Count; i++) longWhiskerRightRadii[i] = 0.008f;
            var whisker1 = Shapes.Tube(longWhiskerRightPath, longWhiskerRightRadii, 6);
            parts.Add(new ColoredMesh(whisker1, Whisker));

            // Long whisker Left: Mirror X.
            var longWhiskerLeftPath = new List<V3>();
            foreach(var p in longWhiskerRightPath) {
                longWhiskerLeftPath.Add(new V3(-p.X, p.Y, p.Z));
            }
            var whisker2 = Shapes.Tube(longWhiskerLeftPath, longWhiskerRightRadii, 6);
            parts.Add(new ColoredMesh(whisker2, Whisker));

            // Short whiskers under chin (z=0.45, y=-0.1).
            var shortWhiskerRightPath = Shapes.Bezier(
                new V3(0.05f, -0.1f, 0.45f),
                new V3(0.08f, -0.12f, 0.3f),
                new V3(0.06f, -0.12f, 0.2f),
                new V3(-0.05f, -0.11f, 0.1f),
                8
            );
            var shortRadii = new List<float>(new float[shortWhiskerRightPath.Count]);
            for(int i=0; i<shortRadii.Count; i++) shortRadii[i] = 0.006f;
            var whisker3 = Shapes.Tube(shortWhiskerRightPath, shortRadii, 6);
            parts.Add(new ColoredMesh(whisker3, Whisker));

            var shortWhiskerLeftPath = new List<V3>();
            foreach(var p in shortWhiskerRightPath) {
                shortWhiskerLeftPath.Add(new V3(-p.X, p.Y, p.Z));
            }
            var whisker4 = Shapes.Tube(shortWhiskerLeftPath, shortRadii, 6);
            parts.Add(new ColoredMesh(whisker4, Whisker));

            // 7. Eyes: Small white Ellipsoids high on each side of the head.
            // Head center z=0.25. Eyes at z=0.3? High Y.
            var eyeMesh = Shapes.Ellipsoid(new V3(0.015f, 0.015f, 0.02f), 8, 6);
            
            // Right Eye
            var eyeRight = eyeMesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(0.1f, 0.08f, 0.3f));
            parts.Add(new ColoredMesh(eyeRight, EyeWhite));
            
            // Left Eye
            var eyeLeft = eyeMesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(-0.1f, 0.08f, 0.3f));
            parts.Add(new ColoredMesh(eyeLeft, EyeWhite));

            // Pupils: Black, slightly further out (larger X).
            var pupilMesh = Shapes.Ellipsoid(new V3(0.008f, 0.008f, 0.01f), 6, 4);
            
            var pupilRight = pupilMesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(0.12f, 0.08f, 0.3f));
            parts.Add(new ColoredMesh(pupilRight, Pupil));

            var pupilLeft = pupilMesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, new V3(-0.12f, 0.08f, 0.3f));
            parts.Add(new ColoredMesh(pupilLeft, Pupil));

            return parts;
        }
    }
}
