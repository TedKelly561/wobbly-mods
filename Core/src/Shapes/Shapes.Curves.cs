using System;
using System.Collections.Generic;

namespace WobblyMods.Core.Shapes
{
    public static partial class Shapes
    {
        /// <summary>Cubic Bezier: steps + 1 points from p0 to p3 inclusive (the ends are exact).</summary>
        public static List<V3> Bezier(V3 p0, V3 p1, V3 p2, V3 p3, int steps)
        {
            if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps), "steps must be >= 1");

            var points = new List<V3>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                if (i == 0) { points.Add(p0); continue; }
                if (i == steps) { points.Add(p3); continue; }

                float t = i / (float)steps;
                float u = 1f - t;
                float a = u * u * u;
                float b = 3f * u * u * t;
                float c = 3f * u * t * t;
                float d = t * t * t;
                points.Add(new V3(
                    a * p0.X + b * p1.X + c * p2.X + d * p3.X,
                    a * p0.Y + b * p1.Y + c * p2.Y + d * p3.Y,
                    a * p0.Z + b * p1.Z + c * p2.Z + d * p3.Z));
            }
            return points;
        }

        /// <summary>
        /// A closed tube along a path. Each path point gets a ring of <paramref name="segments"/> vertices
        /// (a single point when its radius is 0). Rings are turned with a rotation-minimizing (parallel
        /// transport) frame, so the tube does not twist or pinch on curves. Vertex order: all the side
        /// rings / points in path order, then the start cap, then the end cap (caps only where the end
        /// radius is above 0).
        /// </summary>
        public static ShapeMesh Tube(IList<V3> path, IList<float> radii, int segments = 10)
        {
            if (path == null || path.Count < 2) throw new ArgumentException("path needs at least 2 points", nameof(path));
            if (radii == null || radii.Count != path.Count) throw new ArgumentException("radii must have one value per path point", nameof(radii));
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), "segments must be >= 3");

            bool anyPositive = false;
            for (int i = 0; i < radii.Count; i++)
            {
                if (radii[i] < 0f) throw new ArgumentException("radii must not be negative", nameof(radii));
                if (radii[i] > 0f) anyPositive = true;
            }
            if (!anyPositive) throw new ArgumentException("at least one radius must be above 0", nameof(radii));

            int n = path.Count;

            // Tangents: average of the two neighbouring segment directions (one-sided at the ends).
            var tangents = new V3[n];
            for (int i = 0; i < n; i++)
            {
                V3 before = i > 0 ? (path[i] - path[i - 1]).Normalized : V3.Zero;
                V3 after = i < n - 1 ? (path[i + 1] - path[i]).Normalized : V3.Zero;
                V3 t = (before + after).Normalized;
                if (t.Length < 0.5f) t = after.Length > 0.5f ? after : before;
                if (t.Length < 0.5f) t = i > 0 ? tangents[i - 1] : V3.Up;
                tangents[i] = t;
            }

            // Parallel transport: carry the first ring's "normal" along the path.
            var normals = new V3[n];
            V3 helper = Math.Abs(V3.Dot(tangents[0], V3.Up)) > 0.9f ? V3.Right : V3.Up;
            normals[0] = V3.Cross(helper, tangents[0]).Normalized;
            for (int i = 1; i < n; i++)
            {
                V3 axis = V3.Cross(tangents[i - 1], tangents[i]);
                float s = axis.Length;
                float c = V3.Dot(tangents[i - 1], tangents[i]);
                V3 nrm = normals[i - 1];
                if (s > 1e-6f) nrm = RotateVector(nrm, axis / s, (float)Math.Atan2(s, c));
                nrm = (nrm - tangents[i] * V3.Dot(nrm, tangents[i])).Normalized; // keep it exactly perpendicular
                normals[i] = nrm;
            }

            var mesh = new ShapeMesh();
            var ringStart = new int[n];
            var pointIndex = new int[n];

            for (int i = 0; i < n; i++)
            {
                if (radii[i] > 1e-6f)
                {
                    ringStart[i] = mesh.Vertices.Count;
                    pointIndex[i] = -1;
                    AddRing(mesh, path[i], normals[i], tangents[i], radii[i], segments, false, false);
                }
                else
                {
                    ringStart[i] = -1;
                    pointIndex[i] = mesh.AddVertex(path[i], i == 0 ? -tangents[i] : tangents[i]);
                }
            }

            // Sides. Triangles are wound so that Cross(b - a, c - a) points away from the tube axis.
            for (int i = 1; i < n; i++)
            {
                bool prevRing = ringStart[i - 1] >= 0;
                bool curRing = ringStart[i] >= 0;
                for (int j = 0; j < segments; j++)
                {
                    if (prevRing && curRing)
                    {
                        int p0 = ringStart[i - 1] + j, p1 = p0 + 1;
                        int p2 = ringStart[i] + j, p3 = p2 + 1;
                        mesh.AddTriangle(p0, p1, p2);
                        mesh.AddTriangle(p1, p3, p2);
                    }
                    else if (prevRing)
                    {
                        mesh.AddTriangle(ringStart[i - 1] + j, ringStart[i - 1] + j + 1, pointIndex[i]);
                    }
                    else if (curRing)
                    {
                        mesh.AddTriangle(pointIndex[i - 1], ringStart[i] + j + 1, ringStart[i] + j);
                    }
                }
            }

            // Flat caps where an end has a radius.
            if (ringStart[0] >= 0) AddCap(mesh, path[0], normals[0], tangents[0], radii[0], segments, true);
            if (ringStart[n - 1] >= 0) AddCap(mesh, path[n - 1], normals[n - 1], tangents[n - 1], radii[n - 1], segments, false);

            return mesh;
        }

        // Adds segments + 1 vertices around center (the last repeats the first exactly). With flatNormal the
        // vertices carry the cap's normal instead of the outward direction.
        private static void AddRing(ShapeMesh mesh, V3 center, V3 normal, V3 tangent, float radius, int segments,
                                    bool flatNormal, bool startCap)
        {
            V3 binormal = V3.Cross(tangent, normal); // (normal, binormal, tangent) is right-handed
            V3 capNormal = startCap ? -tangent : tangent;
            for (int j = 0; j <= segments; j++)
            {
                float theta = 2f * (float)Math.PI * (j % segments) / segments;
                V3 dir = normal * (float)Math.Cos(theta) + binormal * (float)Math.Sin(theta);
                mesh.AddVertex(center + dir * radius, flatNormal ? capNormal : dir);
            }
        }

        private static void AddCap(ShapeMesh mesh, V3 center, V3 normal, V3 tangent, float radius, int segments, bool startCap)
        {
            V3 capNormal = startCap ? -tangent : tangent;
            int centerIdx = mesh.AddVertex(center, capNormal);
            int ringStart = mesh.Vertices.Count;
            AddRing(mesh, center, normal, tangent, radius, segments, true, startCap);
            for (int j = 0; j < segments; j++)
            {
                if (startCap) mesh.AddTriangle(centerIdx, ringStart + j + 1, ringStart + j);
                else mesh.AddTriangle(centerIdx, ringStart + j, ringStart + j + 1);
            }
        }

        private static V3 RotateVector(V3 v, V3 axis, float angle)
        {
            // Rodrigues' rotation formula; axis must be unit length.
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return v * cos + V3.Cross(axis, v) * sin + axis * (V3.Dot(axis, v) * (1f - cos));
        }

        /// <summary>
        /// A solid slab from a polygon in the XY plane (Z is ignored), from z = -thickness/2 to +thickness/2.
        /// The polygon must be star-shaped around its centroid (the average of its points); it may be given
        /// clockwise or counter-clockwise. Front and back are fanned from the centroid (one triangle per
        /// outline edge). Faces and sides have their own vertices, so every face is flat shaded.
        /// </summary>
        public static ShapeMesh Extrude(IList<V3> outline, float thickness)
        {
            if (outline == null || outline.Count < 3) throw new ArgumentOutOfRangeException(nameof(outline), "outline needs at least 3 points");
            if (thickness <= 0f) throw new ArgumentOutOfRangeException(nameof(thickness), "thickness must be > 0");

            int count = outline.Count;

            // Orientation by the signed area; work on a counter-clockwise copy.
            float area2 = 0f;
            for (int i = 0; i < count; i++)
            {
                V3 a = outline[i], b = outline[(i + 1) % count];
                area2 += a.X * b.Y - b.X * a.Y;
            }
            var pts = new List<V3>(count);
            if (area2 >= 0f) { for (int i = 0; i < count; i++) pts.Add(outline[i]); }
            else { for (int i = count - 1; i >= 0; i--) pts.Add(outline[i]); }

            float cx = 0f, cy = 0f;
            for (int i = 0; i < count; i++) { cx += pts[i].X; cy += pts[i].Y; }
            cx /= count;
            cy /= count;

            float h = thickness / 2f;
            V3 front = V3.Forward;   // +Z
            V3 back = -V3.Forward;   // -Z
            var mesh = new ShapeMesh();

            int frontCenter = mesh.AddVertex(new V3(cx, cy, h), front);
            int frontStart = mesh.Vertices.Count;
            for (int i = 0; i < count; i++) mesh.AddVertex(new V3(pts[i].X, pts[i].Y, h), front);

            int backCenter = mesh.AddVertex(new V3(cx, cy, -h), back);
            int backStart = mesh.Vertices.Count;
            for (int i = 0; i < count; i++) mesh.AddVertex(new V3(pts[i].X, pts[i].Y, -h), back);

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                float dx = pts[j].X - pts[i].X, dy = pts[j].Y - pts[i].Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-9f) continue; // repeated point: nothing to build on this edge

                // Front and back fans. Counter-clockwise seen from +Z gives a +Z normal on the front;
                // the back is wound the other way.
                mesh.AddTriangle(frontCenter, frontStart + i, frontStart + j);
                mesh.AddTriangle(backCenter, backStart + j, backStart + i);

                // Side quad with its own four vertices and one flat outward normal.
                V3 sideNormal = new V3(dy / len, -dx / len, 0f);
                int topI = mesh.AddVertex(new V3(pts[i].X, pts[i].Y, h), sideNormal);
                int topJ = mesh.AddVertex(new V3(pts[j].X, pts[j].Y, h), sideNormal);
                int botI = mesh.AddVertex(new V3(pts[i].X, pts[i].Y, -h), sideNormal);
                int botJ = mesh.AddVertex(new V3(pts[j].X, pts[j].Y, -h), sideNormal);
                mesh.AddTriangle(topI, botI, topJ);
                mesh.AddTriangle(topJ, botI, botJ);
            }

            return mesh;
        }
    }
}
