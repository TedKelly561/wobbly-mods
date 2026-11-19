using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    public static class Shapes
    {
        public static ShapeMesh Ellipsoid(V3 radii, int segments = 16, int rings = 10)
        {
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), "segments must be >= 3");
            if (rings < 2) throw new ArgumentOutOfRangeException(nameof(rings), "rings must be >= 2");
            if (radii.X <= 0 || radii.Y <= 0 || radii.Z <= 0) throw new ArgumentOutOfRangeException(nameof(radii), "Radii must be > 0");

            var mesh = new ShapeMesh();
            
            // Generate vertices
            for (int r = 0; r <= rings; r++)
            {
                float phi = (float)Math.PI * r / rings;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                for (int s = 0; s <= segments; s++)
                {
                    float theta = 2f * (float)Math.PI * s / segments;
                    float sinTheta = (float)Math.Sin(theta);
                    float cosTheta = (float)Math.Cos(theta);

                    // Position on sphere scaled by radii
                    float x = radii.X * sinPhi * cosTheta;
                    float y = radii.Y * cosPhi;
                    float z = radii.Z * sinPhi * sinTheta;

                    // The two poles must be exactly one point (no -0.0 / tiny float error from sin(pi)).
                    V3 pos = (r == 0 || r == rings)
                        ? new V3(0f, r == 0 ? radii.Y : -radii.Y, 0f)
                        : new V3(x, y, z);

                    // Normal for ellipsoid at origin is proportional to (x/rx^2, y/ry^2, z/rz^2)
                    float nx = pos.X / (radii.X * radii.X);
                    float ny = pos.Y / (radii.Y * radii.Y);
                    float nz = pos.Z / (radii.Z * radii.Z);
                    V3 norm = new V3(nx, ny, nz).Normalized;

                    mesh.AddVertex(pos, norm);
                }
            }

            // Generate triangles
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int p0 = r * (segments + 1) + s;
                    int p1 = p0 + 1;
                    int p2 = (r + 1) * (segments + 1) + s;
                    int p3 = p2 + 1;

                    // Winding: outward normal check. 
                    // For sphere-like shapes, (p0, p1, p2) and (p1, p3, p2) usually work if ordered correctly.
                    // Let's verify winding for standard UV mapping order.
                    // Top-left, Top-right, Bottom-left, Bottom-right
                    // Triangle 1: p0, p1, p2 ? 
                    // Normal at p0 is outward. Cross(p1-p0, p2-p0) should point roughly same direction as normal.
                    
                    // At the top pole (r == 0) p0 and p1 are the same point; at the bottom pole
                    // (r == rings - 1) p2 and p3 are the same point. Skip those zero-area triangles.
                    if (r != 0) mesh.AddTriangle(p0, p1, p2);
                    if (r != rings - 1) mesh.AddTriangle(p1, p3, p2);
                }
            }

            return mesh;
        }

        public static ShapeMesh Cone(float radius, float height, int segments = 16)
        {
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), "segments must be >= 3");
            if (radius <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Size", "Radius and height must be > 0");

            var mesh = new ShapeMesh();
            
            // Tip at y=height, Base at y=0
            V3 tip = new V3(0f, height, 0f);
            int tipIdx = mesh.AddVertex(tip, V3.Up); // Approx normal for tip? Actually tip is singular. Let's use Up or Zero. 
            // Better: The normal at the tip is undefined. We'll set it to Up arbitrarily, it won't matter much for lighting if it's a point.
            
            // Base vertices
            int baseStartIdx = mesh.Vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float theta = 2f * (float)Math.PI * i / segments;
                float x = radius * (float)Math.Cos(theta);
                float z = radius * (float)Math.Sin(theta);
                
                V3 pos = new V3(x, 0f, z);
                // Normal for side of cone at base edge points outward and slightly up/down depending on slope.
                // For the cap, normal is Down (0,-1,0).
                mesh.AddVertex(pos, new V3(0f, -1f, 0f));
            }

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int p0 = tipIdx;
                int p1 = baseStartIdx + i;
                int p2 = baseStartIdx + i + 1;
                
                // Winding: Cross(p1-p0, p2-p0) should point outward.
                // p0 is top. p1, p2 are on circle.
                // If we go counter-clockwise around Y axis (standard), p1->p2 is CCW.
                // Vector from tip to base is down.
                // Let's check: Center of triangle is roughly (x,y,z). Normal should point away from axis.
                mesh.AddTriangle(p0, p2, p1); // Reverse order to ensure outward normal for standard CCW ring
            }

            // Base cap triangles
            int centerIdx = mesh.Vertices.Count;
            mesh.AddVertex(new V3(0f, 0f, 0f), new V3(0f, -1f, 0f));
            
            for (int i = 0; i < segments; i++)
            {
                int p0 = centerIdx;
                int p1 = baseStartIdx + i;
                int p2 = baseStartIdx + i + 1;
                
                // Normal is Down. Cross(p1-p0, p2-p0) should point Down (negative Y).
                // p1, p2 are CCW around center.
                // Cross(Left, Right) with Up normal points Out? 
                // Standard: (p0, p1, p2) with normal pointing towards viewer.
                // Here we want normal to point Down (-Y).
                // If viewed from below, CCW is p1->p2.
                mesh.AddTriangle(p0, p1, p2);
            }

            return mesh;
        }

        public static ShapeMesh Cylinder(float radius, float height, int segments = 12)
        {
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), "segments must be >= 3");
            if (radius <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Size", "Radius and height must be > 0");

            var mesh = new ShapeMesh();

            // Bottom Cap Center
            int bottomCenterIdx = mesh.Vertices.Count;
            mesh.AddVertex(new V3(0f, 0f, 0f), new V3(0f, -1f, 0f));

            // Top Cap Center
            int topCenterIdx = mesh.Vertices.Count;
            mesh.AddVertex(new V3(0f, height, 0f), new V3(0f, 1f, 0f));

            // Side vertices: first the whole bottom ring, then the whole top ring.
            int sideStartIdx = mesh.Vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float theta = 2f * (float)Math.PI * i / segments;
                float c = (float)Math.Cos(theta);
                float s = (float)Math.Sin(theta);
                mesh.AddVertex(new V3(radius * c, 0f, radius * s), new V3(c, 0f, s).Normalized);
            }
            for (int i = 0; i <= segments; i++)
            {
                float theta = 2f * (float)Math.PI * i / segments;
                float c = (float)Math.Cos(theta);
                float s = (float)Math.Sin(theta);
                mesh.AddVertex(new V3(radius * c, height, radius * s), new V3(c, 0f, s).Normalized);
            }

            int botRingStart = sideStartIdx;
            int topRingStart = sideStartIdx + segments + 1;

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int p0 = botRingStart + i;
                int p1 = botRingStart + i + 1;
                int p2 = topRingStart + i;
                int p3 = topRingStart + i + 1;

                // Winding: Outward.
                // Normal at side points radially out.
                mesh.AddTriangle(p0, p2, p1);
                mesh.AddTriangle(p1, p2, p3);
            }

            // Bottom Cap triangles
            for (int i = 0; i < segments; i++)
            {
                int p0 = bottomCenterIdx;
                int p1 = botRingStart + i;
                int p2 = botRingStart + i + 1;
                
                // Normal Down. Viewed from below, CCW is p1->p2.
                mesh.AddTriangle(p0, p1, p2);
            }

            // Top Cap triangles
            for (int i = 0; i < segments; i++)
            {
                int p0 = topCenterIdx;
                int p1 = topRingStart + i + 1;
                int p2 = topRingStart + i;
                
                // Normal Up. Viewed from above, CCW is p2->p1 (reverse of bottom).
                mesh.AddTriangle(p0, p1, p2);
            }

            return mesh;
        }
    }
}
