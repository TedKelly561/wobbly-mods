using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    /// <summary>A costume: parts for the head (head space) and for the body (body space). 1 unit = the Wobbly's head radius.</summary>
    public sealed class Costume
    {
        public List<ColoredMesh> Head = new List<ColoredMesh>();
        public List<ColoredMesh> Body = new List<ColoredMesh>();
    }

    public static class CostumeModels
    {
        public static readonly string[] Ids = { "cat", "bunny", "puppy", "trex" };

        private static readonly Rgb Pink = new Rgb(255, 140, 180);
        private static readonly Rgb Cream = new Rgb(245, 235, 205);
        private static readonly Rgb White = new Rgb(255, 255, 255);
        private static readonly Rgb Black = new Rgb(10, 10, 10);
        private static readonly Rgb Orange = new Rgb(255, 128, 13);
        private static readonly V3 One = new V3(1f, 1f, 1f);

        public static Costume Build(string id, Rgb fur)
        {
            switch (id)
            {
                case "cat": return BuildCat(fur);
                case "bunny": return BuildBunny(fur);
                case "puppy": return BuildPuppy(fur);
                case "trex": return BuildTrex(fur);
                default: throw new ArgumentException("Unknown costume id: " + id, nameof(id));
            }
        }

        /// <summary>The fur colour times 0.6.</summary>
        private static Rgb Darker(Rgb c)
        {
            return new Rgb((byte)(c.R * 0.6f), (byte)(c.G * 0.6f), (byte)(c.B * 0.6f));
        }

        /// <summary>A mesh placed with Transformed (scale, then pitch about X, then yaw about Y, then offset), with its colour.</summary>
        private static ColoredMesh Part(ShapeMesh mesh, Rgb color, V3 scale, float yawDegrees, float pitchDegrees, V3 offset)
        {
            return new ColoredMesh(mesh.Transformed(scale, yawDegrees, pitchDegrees, offset), color);
        }

        /// <summary>A mesh just moved (no scale, no rotation), with its colour.</summary>
        private static ColoredMesh Part(ShapeMesh mesh, Rgb color, V3 offset)
        {
            return new ColoredMesh(mesh.Transformed(One, 0f, 0f, offset), color);
        }

        /// <summary>A tube along a Bezier curve whose radius goes from r0 at p0 to r1 at p3.</summary>
        private static ShapeMesh Pipe(V3 p0, V3 p1, V3 p2, V3 p3, int steps, float r0, float r1, int segments)
        {
            List<V3> path = Shapes.Bezier(p0, p1, p2, p3, steps);
            var radii = new List<float>();
            for (int i = 0; i < path.Count; i++) radii.Add(r0 + (r1 - r0) * i / (path.Count - 1));
            return Shapes.Tube(path, radii, segments);
        }

        /// <summary>A stand-in Wobbly for pictures only: head sphere, body, two legs, two arms, two black eyes.</summary>
        public static List<ColoredMesh> StandIn(Rgb fur)
        {
            var parts = new List<ColoredMesh>();
            parts.Add(Part(Shapes.Ellipsoid(new V3(1f, 1f, 1f), 28, 16), fur, new V3(0f, 2.1f, 0f)));
            parts.Add(Part(Shapes.Ellipsoid(new V3(0.75f, 0.9f, 0.6f), 24, 14), fur, new V3(0f, 0.75f, 0f)));
            for (int s = -1; s <= 1; s += 2)
            {
                parts.Add(new ColoredMesh(Pipe(new V3(0.3f * s, 0.1f, 0f), new V3(0.3f * s, -0.1f, 0f), new V3(0.3f * s, -0.3f, 0f), new V3(0.3f * s, -0.55f, 0f), 6, 0.2f, 0.18f, 10), fur));
                parts.Add(new ColoredMesh(Pipe(new V3(0.65f * s, 1.15f, 0f), new V3(0.85f * s, 1.0f, 0f), new V3(0.95f * s, 0.8f, 0f), new V3(1.0f * s, 0.55f, 0f), 6, 0.16f, 0.13f, 10), fur));
                parts.Add(Part(Shapes.Ellipsoid(new V3(0.08f, 0.08f, 0.08f), 12, 8), Black, new V3(0.3f * s, 2.2f, 0.93f)));
            }
            return parts;
        }

        /// <summary>The stand-in plus the costume: head parts moved up to the head, body parts to the hip.</summary>
        public static List<ColoredMesh> Dressed(string id, Rgb fur)
        {
            Costume c = Build(id, fur);
            List<ColoredMesh> parts = StandIn(fur);
            foreach (ColoredMesh p in c.Head) parts.Add(new ColoredMesh(p.Mesh.Transformed(One, 0f, 0f, new V3(0f, 2.1f, 0f)), p.Color));
            foreach (ColoredMesh p in c.Body) parts.Add(new ColoredMesh(p.Mesh.Transformed(One, 0f, 0f, new V3(0f, 0.55f, 0f)), p.Color));
            return parts;
        }

        private static Costume BuildCat(Rgb fur)
        {
            var costume = new Costume();
            float earTilt = 15f;
            V3 earBase = new V3(0.55f, 0.75f, 0f);

            // Ears: two pointed cones, tilted outward 15 degrees
            for (int s = -1; s <= 1; s += 2)
            {
                // Outer ear (fur color)
                var outerEar = Shapes.Cone(0.18f, 0.45f, 16);
                // To tilt outward: pitch tilts +Y toward +Z, yaw 90 turns +Z into +X.
                // For right side (+X), we want the cone to point somewhat +X and +Z (outward/up).
                // Using the rule: pitch = a, yaw = +90 for +X side.
                costume.Head.Add(Part(outerEar, fur, One, 90f, earTilt, new V3(s * earBase.X, earBase.Y, earBase.Z)));

                // Inner ear (pink cone, smaller, facing forward)
                var innerEar = Shapes.Cone(0.12f, 0.35f, 16);
                // Positioned slightly lower and further forward inside the outer ear
                costume.Head.Add(Part(innerEar, Pink, One, 90f, earTilt, new V3(s * (earBase.X - 0.05f), earBase.Y - 0.05f, earBase.Z + 0.1f)));
            }

            // Nose: small pink nose on the face front low
            var nose = Shapes.Ellipsoid(new V3(0.06f, 0.04f, 0.05f), 12, 8);
            costume.Head.Add(Part(nose, Pink, One, 0f, 0f, new V3(0f, -0.15f, 0.98f)));

            // Whiskers: three thin white whiskers per side fanning out from (+/-0.45, -0.2, 0.85)
            for (int s = -1; s <= 1; s += 2)
            {
                // Fan angles: roughly -15, 0, +15 degrees in X-Z plane relative to forward
                float[] whiskerAngles = { -0.26f, 0f, 0.26f }; // approx tan(15 deg)
                
                for (int i = 0; i < 3; i++)
                {
                    float angle = whiskerAngles[i];
                    // Base is fixed. Tip moves out in X and slightly down/back in Z/Y
                    V3 p0 = new V3(s * 0.45f, -0.2f, 0.85f);
                    // Control points to create a slight curve outward
                    float xOff = s * (0.3f + i * 0.1f);
                    float yOff = -0.1f - i * 0.05f;
                    float zOff = 0.15f - i * 0.02f;
                    
                    V3 p1 = new V3(p0.X + xOff * 0.5f, p0.Y + yOff * 0.5f, p0.Z + zOff * 0.5f);
                    V3 p2 = new V3(p0.X + xOff * 0.8f, p0.Y + yOff * 0.8f, p0.Z + zOff * 0.8f);
                    V3 p3 = new V3(p0.X + xOff, p0.Y + yOff, p0.Z + zOff);

                    var whiskerMesh = Pipe(p0, p1, p2, p3, 6, 0.02f, 0.01f, 8);
                    costume.Head.Add(new ColoredMesh(whiskerMesh, White));
                }
            }

            // Tail: long tail in BODY list, Tube along a Bezier from lower back up and curling
            // Body space: origin at hip center (0, 0.2, 0) roughly. Back is -Z.
            // Start near lower back (0, 0.1, -0.5), go up and curl.
            // Starts sunk into the lower back, sweeps out behind, then curls up and forward over itself.
            V3 tailStart = new V3(0f, -0.1f, -0.5f);
            V3 tailCtrl1 = new V3(0f, -0.2f, -1.3f);
            V3 tailCtrl2 = new V3(0f, 0.9f, -1.6f);
            V3 tailEnd = new V3(0f, 1.3f, -0.95f);

            var tailMesh = Pipe(tailStart, tailCtrl1, tailCtrl2, tailEnd, 16, 0.16f, 0.08f, 12);
            costume.Body.Add(new ColoredMesh(tailMesh, fur));

            return costume;
        }

        private static Costume BuildBunny(Rgb fur)
        {
            var costume = new Costume();
            
            // --- HEAD PARTS ---

            // Pink nose: small ellipsoid near the face center, slightly forward
            var noseMesh = Shapes.Ellipsoid(new V3(0.12f, 0.08f, 0.1f), 12, 8);
            costume.Head.Add(Part(noseMesh, Pink, new V3(0f, -0.15f, 0.95f)));

            // White teeth: two small cones or cylinders below the mouth area (0, -0.5, 0.9)
            // Using Cone for a pointed tooth shape. Base at y=0, tip at y=height.
            // We want them to stick out slightly from the face.
            var toothMesh = Shapes.Cone(0.04f, 0.12f, 8);
            for (int s = -1; s <= 1; s += 2)
            {
                // Position: x offset by s, y slightly below mouth center, z near face surface
                costume.Head.Add(Part(toothMesh, White, new V3(s * 0.06f, -0.58f, 0.92f)));
            }

            // Ears: long, slightly flat, going up from the head and flopping outward and down in the top third.
            // The paths are already in head space; the only transform is flattening Z (scale 0.5 about the origin).
            V3 flat = new V3(1f, 1f, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                V3 p0 = new V3(s * 0.4f, 0.75f, 0f);   // sunk into the top of the head
                V3 p1 = new V3(s * 0.4f, 2.0f, 0f);
                V3 p2 = new V3(s * 0.75f, 2.55f, 0f);
                V3 p3 = new V3(s * 1.05f, 2.0f, 0f);   // tip hangs outward and down
                costume.Head.Add(Part(Pipe(p0, p1, p2, p3, 16, 0.2f, 0.15f, 12), fur, flat, 0f, 0f, V3.Zero));

                // Pink inner strip on the front of the ear: a thinner tube pushed toward +Z.
                V3 q = new V3(0f, 0f, 0.2f);
                costume.Head.Add(Part(Pipe(p0 + q, p1 + q, p2 + q, p3 + q, 16, 0.1f, 0.08f, 10), Pink, flat, 0f, 0f, V3.Zero));
            }

            // --- BODY PARTS ---

            // Round white tail on the lower back
            // Body space: origin at hip center (0, 0.2, 0) in head space? No, body parts are in BODY space.
            // "Body parts are in BODY space: origin at the hip center... back is -Z."
            // So tail should be at negative Z in body space.
            var tailMesh = Shapes.Ellipsoid(new V3(0.35f, 0.35f, 0.35f), 16, 10);
            costume.Body.Add(Part(tailMesh, White, new V3(0f, -0.2f, -0.6f)));

            return costume;
        }

        private static Costume BuildPuppy(Rgb fur)
        {
            var costume = new Costume();
            Rgb darkerFur = Darker(fur);
            V3 one = One;

            // --- HEAD PARTS ---

            // Two floppy ears: flattened ellipsoids hanging down the sides.
            // Radii (0.18, 0.6, 0.38). Base at (+/-0.95, 0.35, 0).
            // To hang down, we need to tilt them outward and downward.
            // The ellipsoid is centered at origin, tall in Y.
            // We want the long axis (Y) to point somewhat down and out.
            // Using pitch/yaw:
            // Start with Y-axis up. Pitch -90 makes it point +Z. Yaw 90 makes it point +X.
            // We want it to point down (-Y) and out (+/-X).
            // Let's use a combination of scale and rotation.
            // Actually, simpler: Scale Y by 0.6 (flattened), X/Z by 0.18/0.38.
            // Then rotate so the long axis points down-out.
            // Pitch -90 tips +Y to +Z. Yaw 90 turns +Z to +X. Result: +Y becomes +X. Not what we want.
            // Let's try: Pitch 90 tips +Y to -Z. Yaw 90 turns -Z to +X.
            // We want the ear to hang down (-Y) and out (+/-X).
            // If we pitch by some angle a, the +Y axis tilts toward +Z.
            // The instruction says: "To TILT a Y-axis part (cone, cylinder) outward sideways by an angle a: use pitch = a and yaw = +90 for the +X side".
            // This tips the top (+Y) towards +Z, then yaw 90 turns that +Z direction to +X.
            // So the top of the part points in the +X direction.
            // We want the ear to hang DOWN. So we need the "top" of the ellipsoid (which is at +Y initially) to point DOWN (-Y).
            // Let's just use Pitch = -90 to make it point +Z, then Yaw = 180 to make it point -Z? No.
            // Let's think about the vector (0,1,0).
            // Pitch p: (0, cos(p), sin(p)).
            // Yaw y: The X component becomes sin(y)*sin(p)? No.
            // Transformed(scale, yaw, pitch, offset):
            // 1. Scale.
            // 2. Pitch about X: (x, y*cos(p)-z*sin(p), y*sin(p)+z*cos(p)). For (0,1,0) -> (0, cos(p), sin(p)).
            // 3. Yaw about Y: (x*cos(y)+z*sin(y), y, -x*sin(y)+z*cos(y)).
            // Apply to (0, cos(p), sin(p)):
            // x' = 0*cos(y) + sin(p)*sin(y) = sin(p)*sin(y)
            // y' = cos(p)
            // z' = -0*sin(y) + sin(p)*cos(y) = sin(p)*cos(y)
            // We want the ear to hang down, so we want the main axis (originally Y) to point roughly (-X, -Y, 0) or similar.
            // Actually, ears hang DOWN and OUT.
            // Let's try Pitch = -45 degrees, Yaw = 90 degrees for right ear (+X side).
            // p = -45, y = 90.
            // x' = sin(-45)*sin(90) = -0.707 * 1 = -0.707.
            // y' = cos(-45) = 0.707.
            // z' = sin(-45)*cos(90) = -0.707 * 0 = 0.
            // So the top of the ear points in direction (-0.7, 0.7, 0). This is up and left. Not hanging down.
            
            // Let's try Pitch = 45 degrees, Yaw = -90 degrees for right ear?
            // p = 45, y = -90.
            // x' = sin(45)*sin(-90) = 0.707 * -1 = -0.707.
            // y' = cos(45) = 0.707.
            // z' = sin(45)*cos(-90) = 0.
            // Still up.

            // We want the top of the ear (originally +Y) to point DOWN (-Y).
            // So we need y' to be negative. cos(p) < 0 => p in (90, 270).
            // Let's try Pitch = 135 degrees.
            // p = 135, y = 90.
            // x' = sin(135)*sin(90) = 0.707 * 1 = 0.707.
            // y' = cos(135) = -0.707.
            // z' = sin(135)*cos(90) = 0.
            // Direction (0.7, -0.7, 0). This points down and right (+X). Perfect for the right ear.
            // For the left ear (-X side), we want it to point down and left (-X).
            // p = 135, y = -90.
            // x' = sin(135)*sin(-90) = 0.707 * -1 = -0.707.
            // y' = cos(135) = -0.707.
            // z' = sin(135)*cos(-90) = 0.
            // Direction (-0.7, -0.7, 0). Perfect.

            // Ear shape: Ellipsoid radii (0.18, 0.6, 0.38).
            // We want it to be somewhat flat against the head but floppy.
            // Scale: X=0.5, Y=1.2, Z=0.5? Or just use the radii as scale?
            // The ellipsoid is centered at origin. We will offset it to (+/-0.95, 0.35, 0).
            // To ensure it reaches below y = -0.3, we need the bottom of the ear to be low.
            // With pitch 135, the "top" (originally +Y) points down-left/right. The "bottom" (originally -Y) points up-right/left.
            // Wait, if the top points down, the ear hangs down from its attachment point.
            // Attachment is at (+/-0.95, 0.35, 0).
            // The ear extends along the vector (x', y', z') scaled by Y-radius.
            // Length of Y-axis in local space is 0.6. Scaled by 1.2 -> 0.72.
            // So it extends 0.72 units in direction (0.7, -0.7, 0).
            // Delta Y = 0.72 * -0.707 = -0.51.
            // End Y = 0.35 - 0.51 = -0.16. This is not below -0.3.
            // We need more length or more downward tilt.
            // Let's increase the Y scale of the ear to make it longer. Say Scale Y = 1.5.
            // Length = 0.6 * 1.5 = 0.9.
            // Delta Y = 0.9 * -0.707 = -0.636.
            // End Y = 0.35 - 0.636 = -0.286. Close to -0.3.
            // Let's try Pitch = 150 degrees.
            // p = 150, y = 90.
            // x' = sin(150)*sin(90) = 0.5 * 1 = 0.5.
            // y' = cos(150) = -0.866.
            // z' = sin(150)*cos(90) = 0.
            // Direction (0.5, -0.866, 0).
            // Length 0.9. Delta Y = 0.9 * -0.866 = -0.78.
            // End Y = 0.35 - 0.78 = -0.43. This is well below -0.3.
            
            // The ellipsoid is centred on its own origin, so the centre sits lower than the attachment point:
            // the ear hangs from about (+/-0.95, 0.35) down the side of the head, its inner face sunk into it.
            // Pitch -12 with yaw +/-90 leans the top toward the head and the bottom outward.
            for (int s = -1; s <= 1; s += 2)
            {
                var earMesh = Shapes.Ellipsoid(new V3(0.18f, 0.6f, 0.38f), 20, 12);
                costume.Head.Add(Part(earMesh, darkerFur, one, s * 90f, -12f, new V3(s * 0.98f, -0.05f, 0f)));
            }

            // Muzzle: Ellipsoid radii (0.5, 0.35, 0.35) at (0, -0.35, 0.8).
            var muzzleMesh = Shapes.Ellipsoid(new V3(0.5f, 0.35f, 0.35f), 20, 12);
            costume.Head.Add(Part(muzzleMesh, Cream, one, 0f, 0f, new V3(0f, -0.35f, 0.8f)));

            // Nose: Black, on top-front of muzzle.
            // Muzzle center is (0, -0.35, 0.8). Top-front would be slightly up and forward from center.
            // Let's place it at (0, -0.15, 0.95).
            var noseMesh = Shapes.Ellipsoid(new V3(0.12f, 0.08f, 0.1f), 16, 8);
            costume.Head.Add(Part(noseMesh, Black, one, 0f, 0f, new V3(0f, -0.15f, 0.95f)));

            // Tongue: Small pink, under the muzzle.
            // Under the muzzle center (0, -0.35, 0.8) means lower Y and maybe slightly forward/back.
            // Let's place it at (0, -0.6, 0.7).
            var tongueMesh = Shapes.Ellipsoid(new V3(0.1f, 0.05f, 0.15f), 12, 8);
            costume.Head.Add(Part(tongueMesh, Pink, one, 0f, 0f, new V3(0f, -0.6f, 0.7f)));

            // --- BODY PARTS ---

            // Tail: Short tail pointing up and back with a cream tip.
            // Body space: origin at hip center (0, 0.2, 0) in body coords? No, body parts are in BODY space.
            // "Body parts are in BODY space: origin at the hip center...".
            // Tail starts near the back (-Z). Points up (+Y) and back (-Z).
            // Let's use a Bezier curve for the tail.
            // Start at (0, 0.1, -0.4) [near hip/back].
            // Control points to curve up and back.
            // End at (0, 0.5, -0.6).
            // Starts sunk into the lower back, points back and up. Fur-coloured tube with a cream tip.
            var tailPath = Shapes.Bezier(
                new V3(0f, 0.0f, -0.5f),
                new V3(0f, 0.0f, -0.9f),
                new V3(0f, 0.3f, -1.0f),
                new V3(0f, 0.75f, -1.05f),
                10
            );
            var tailRadii = new List<float>();
            for (int i = 0; i < tailPath.Count; i++)
                tailRadii.Add(0.11f - 0.04f * i / (tailPath.Count - 1));
            costume.Body.Add(new ColoredMesh(Shapes.Tube(tailPath, tailRadii, 10), fur));

            V3 tailEnd = tailPath[tailPath.Count - 1];
            costume.Body.Add(Part(Shapes.Ellipsoid(new V3(0.1f, 0.12f, 0.1f), 16, 8), Cream, one, 0f, 0f, tailEnd));

            return costume;
        }

        private static Costume BuildTrex(Rgb fur)
        {
            var costume = new Costume();
            Rgb darkerFur = Darker(fur);
            V3 one = One;

            // --- HEAD PARTS (Head Space) ---

            // 1. Big Snout: Ellipsoid at (0, -0.25, 0.9)
            // Radii (0.7, 0.45, 0.9). Center is at z=0.9, extends to z=0.9+0.9=1.8 and z=0.9-0.9=0.
            // Top of snout is at y = -0.25 + 0.45 = 0.2. Bottom is at y = -0.25 - 0.45 = -0.7.
            // It touches the head surface (radius 1) near z=1, y=-0.25? Distance from origin to center is 0.9.
            // The snout overlaps the face area slightly but we must not cover the middle of the face.
            // The prompt says "big snout ... at (0, -0.25, 0.9)". We place it exactly there.
            costume.Head.Add(Part(Shapes.Ellipsoid(new V3(0.7f, 0.45f, 0.9f), 20, 12), fur, one, 0f, 0f, new V3(0f, -0.25f, 0.9f)));

            // 2. Darker Lower Jaw: Ellipsoid below the snout.
            // To look like a jaw, it should be under the main snout volume.
            // Let's place it slightly lower and smaller to define the chin/lower lip area.
            costume.Head.Add(Part(Shapes.Ellipsoid(new V3(0.5f, 0.2f, 0.6f), 16, 8), darkerFur, one, 0f, 0f, new V3(0f, -0.45f, 0.7f)));

            // 3. Nostrils: Two small dark ellipsoids on the snout top.
            // Snout top is around y=0.2. We want them on the "top" surface.
            // Since the snout is an ellipsoid centered at (0, -0.25, 0.9), its top vertex is at (0, 0.2, 0.9).
            // Nostrils should be slightly forward and up/down? Usually on the side or top front.
            // Let's put them on the upper-front part of the snout.
            float nostrilY = 0.15f;
            float nostrilZ = 1.3f; // Near the tip (z=1.8) but inside? No, tip is z=1.8. Let's put them at z=1.4.
            // Actually, let's place them on the surface of the snout ellipsoid roughly.
            // Point on ellipsoid: x=0.3, y=0.15, z=sqrt(0.9^2 - 0.3^2/0.7^2 * ... ) complicated.
            // Just place them visually near the top-front.
            for (int s = -1; s <= 1; s += 2)
            {
                costume.Head.Add(Part(Shapes.Ellipsoid(new V3(0.04f, 0.03f, 0.05f), 8, 6), Black, one, 0f, 0f, new V3(s * 0.25f, nostrilY, nostrilZ)));
            }

            // 4. Teeth: Row of white Cone teeth along each side pointing down.
            // "Along each side" implies x = +/- some value.
            // Snout width at y=-0.25 (center) is radius X=0.7.
            // Let's place teeth along the lower edge of the snout or jaw.
            // The prompt says "along each side pointing down".
            // We'll create a helper to add teeth.
            AddTrexTeeth(costume.Head, fur);

            // 5. Orange Spikes on Head: Row from top of head down the back.
            // Head is sphere radius 1 at origin. Top is (0,1,0). Back is -Z.
            // Path along the surface from top to back? Or just a row of cones placed along an arc?
            // "from the top of the head down the back of the head".
            // Let's place them along the Z-axis negative direction on the top/back ridge.
            // Positions: (0, y, z) where x=0.
            // Start at top (0, 1, 0). Move towards -Z.
            // We need to tilt them outward? "Orange spikes ... in a row". Usually they point up/out.
            // Let's place them along the curve of the head surface.
            // Center points: (0, cos(theta), sin(theta)) for theta from 90 deg (top) to 180 deg (back)?
            // Actually, let's just place them at specific coordinates on the head surface.
            // Top: (0, 1, 0). Back: (0, 0, -1).
            // Let's do 5 spikes.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4.0f; // 0 to 1
                float angle = 90f * t; // 0 to 90 degrees from Y axis towards -Z?
                // Let's use spherical coords. Start at (0,1,0). End at (0,0,-1).
                // y = cos(t * PI/2), z = -sin(t * PI/2). x=0.
                float y = (float)Math.Cos(t * Math.PI / 2f);
                float z = -(float)Math.Sin(t * Math.PI / 2f);
                
                V3 pos = new V3(0f, y, z);
                
                // The spike should point outward from the surface.
                // Normal at (0, y, z) on unit sphere is (0, y, z).
                // We need to orient a Cone (which points +Y by default) to align with this normal.
                // Target direction: V3(0, y, z). Current direction: V3(0, 1, 0).
                // Axis of rotation: Cross(Up, Normal).
                // If y is close to 1 (top), cross is small. At top, spike points Up.
                // Let's calculate yaw/pitch.
                // To point +Y to V3(0, y, z):
                // Pitch about X? No, that moves Y towards Z.
                // If we pitch by -alpha where sin(alpha)=z, cos(alpha)=y?
                // Pitch -theta moves Y towards -Z.
                // So pitch = -angle (in degrees). Angle = atan2(z, y) in radians -> degrees.
                float angleDeg = (float)(Math.Atan2(Math.Abs(z), y) * 180f / Math.PI);
                
                // Since x=0, we don't need yaw for the main tilt, but let's be safe.
                // If z is negative, we are going back. Pitch -angleDeg tips Y towards -Z.
                costume.Head.Add(Part(Shapes.Cone(0.06f, 0.25f, 8), Orange, one, 0f, -angleDeg, pos));
            }

            // --- BODY PARTS (Body Space) ---

            // Body origin is hip center. Axes: Up +Y, Forward +Z (face direction? No, Wobbly face is +Z in head space).
            // In body space, the prompt says "back is -Z". So front is +Z.
            // The tail goes back (-Z) and slightly down.

            // 1. Orange Spikes down the back.
            // Back is -Z. We place them along the spine on the back side.
            // Spine runs from hip (0,0,0) up to neck? And then tail?
            // "down the back" usually means from shoulders/neck towards tail.
            // Let's place them along the Z axis negative direction, slightly elevated (Y > 0).
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5.0f;
                float z = -t * 1.2f; // Go back 1.2 units
                float y = 0.3f + t * 0.2f; // Slightly rising or flat? Let's say slightly up then down for tail base.
                if (i > 3) y -= (t - 0.6f) * 0.5f; // Drop off near tail
                
                V3 pos = new V3(0f, y, z);
                
                // Spike points outward from back surface. Back normal is roughly -Z?
                // If it's on the "back" of the body ellipsoid, the normal has a -Z component.
                // Let's just make them point Up and slightly Back? Or just Up?
                // "Orange spikes down the back". Usually they stick up/out from the spine.
                // Let's have them point Up (+Y) but tilted slightly towards the back (-Z) to follow the curve?
                // Or just straight Up. Straight Up is safer and looks like a ridge.
                costume.Body.Add(Part(Shapes.Cone(0.05f, 0.2f, 8), Orange, one, 0f, 0f, pos));
            }

            // 2. Thick Tail: Tube, radius 0.45 tapering to 0.08, about 2.2 long.
            // Must reach beyond z = -1.8 in body space.
            // Start at hip/neck base? Usually tail starts near the lower back.
            // Let's start at (0, 0.1, -0.5) and go to (0, -0.2, -2.7).
            // Length approx 2.2.
            V3 tailStart = new V3(0f, 0.1f, -0.5f);
            V3 tailMid1 = new V3(0f, 0.0f, -1.2f);
            V3 tailMid2 = new V3(0f, -0.1f, -2.0f);
            V3 tailEnd = new V3(0f, -0.2f, -2.7f); // Beyond -1.8
            
            costume.Body.Add(new ColoredMesh(Pipe(tailStart, tailMid1, tailMid2, tailEnd, 12, 0.45f, 0.08f, 12), fur));

            // 3. Small orange spikes on top of the tail.
            // Along the tube path.
            float[] tailZ = { -0.5f, -1.0f, -1.6f, -2.2f };
            foreach (float tz in tailZ)
            {
                V3 pos = new V3(0f, 0.15f, tz); // On top of the tail (radius ~0.3 at that point)
                costume.Body.Add(Part(Shapes.Cone(0.04f, 0.15f, 8), Orange, one, 0f, 0f, pos));
            }

            return costume;
        }

        private static void AddTrexTeeth(List<ColoredMesh> headParts, Rgb fur)
        {
            // Teeth along each side of the snout/jaw.
            // Snout center (0, -0.25, 0.9). Width X=0.7.
            // Let's place teeth at x = +/- 0.4, varying z from front to back of snout.
            // And y slightly below the snout center line to hang down.
            
            int numTeeth = 6;
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < numTeeth; i++)
                {
                    float t = i / (numTeeth - 1f); // 0 to 1
                    // Z from 1.5 (front) to 0.5 (back of snout)
                    float z = 1.5f - t * 1.0f;
                    // Y: slightly below the jaw line, pointing down.
                    // Jaw bottom is around y=-0.6. Teeth hang from there.
                    float y = -0.4f - t * 0.2f; 
                    
                    V3 pos = new V3(s * 0.45f, y, z);
                    
                    // Cone points +Y by default. We want it to point DOWN (-Y).
                    // Pitch 180 degrees tips Y to -Y.
                    headParts.Add(Part(Shapes.Cone(0.03f, 0.12f, 6), White, One, 0f, 180f, pos));
                }
            }
        }

    }
}
