using System;
using System.Collections.Generic;
using System.Globalization;

namespace WobblyMods.Core.Shapes
{
    public sealed class ShapeMesh
    {
        public readonly List<V3> Vertices = new List<V3>();
        public readonly List<V3> Normals = new List<V3>();
        public readonly List<int> Triangles = new List<int>();

        public int AddVertex(V3 position, V3 normal)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            return Vertices.Count - 1;
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public void Append(ShapeMesh other)
        {
            int offset = Vertices.Count;
            for (int i = 0; i < other.Vertices.Count; i++)
            {
                Vertices.Add(other.Vertices[i]);
                Normals.Add(other.Normals[i]);
            }
            for (int i = 0; i < other.Triangles.Count; i++)
            {
                Triangles.Add(other.Triangles[i] + offset);
            }
        }

        public ShapeMesh Transformed(V3 scale, float yawDegrees, float pitchDegrees, V3 offset)
        {
            var result = new ShapeMesh();
            
            // Precompute rotation matrices components
            float yawRad = yawDegrees * (float)Math.PI / 180f;
            float pitchRad = pitchDegrees * (float)Math.PI / 180f;
            
            float cy = (float)Math.Cos(yawRad);
            float sy = (float)Math.Sin(yawRad);
            float cp = (float)Math.Cos(pitchRad);
            float sp = (float)Math.Sin(pitchRad);

            // Rotation matrix for Pitch (X) then Yaw (Y)
            // Pitch about X:
            // [1  0    0 ]
            // [0  cp  -sp]
            // [0  sp   cp]
            // Yaw about Y:
            // [ cy  0  sy]
            // [ 0   1   0]
            // [-sy  0  cy]
            
            // Combined R = Yaw * Pitch
            // R[0,0] = cy*1 + 0*0 + sy*0 = cy
            // R[0,1] = cy*0 + 0*cp + sy*sp = sy*sp
            // R[0,2] = cy*0 + 0*-sp + sy*cp = sy*cp
            
            // R[1,0] = 0*1 + 1*0 + 0*0 = 0
            // R[1,1] = 0*0 + 1*cp + 0*sp = cp
            // R[1,2] = 0*0 + 1*-sp + 0*cp = -sp
            
            // R[2,0] = -sy*1 + 0*0 + cy*0 = -sy
            // R[2,1] = -sy*0 + 0*cp + cy*sp = cy*sp
            // R[2,2] = -sy*0 + 0*-sp + cy*cp = cy*cp

            float r00 = cy;
            float r01 = sy * sp;
            float r02 = sy * cp;
            
            float r10 = 0f;
            float r11 = cp;
            float r12 = -sp;
            
            float r20 = -sy;
            float r21 = cy * sp;
            float r22 = cy * cp;

            // Inverse scale for normals (transpose of inverse is just 1/scale for diagonal)
            float invScaleX = 1f / scale.X;
            float invScaleY = 1f / scale.Y;
            float invScaleZ = 1f / scale.Z;

            for (int i = 0; i < Vertices.Count; i++)
            {
                V3 v = Vertices[i];
                V3 n = Normals[i];

                // Apply Scale
                float sx = v.X * scale.X;
                float sy2 = v.Y * scale.Y;
                float sz = v.Z * scale.Z;

                // Apply Rotation to Position
                float rx = r00 * sx + r01 * sy2 + r02 * sz;
                float ry = r10 * sx + r11 * sy2 + r12 * sz;
                float rz = r20 * sx + r21 * sy2 + r22 * sz;

                // Apply Rotation to Normal (using inverse scale logic implicitly by rotating normalized normal)
                // Normals are already normalized. We rotate them.
                float nx = n.X;
                float ny = n.Y;
                float nz = n.Z;

                float rnX = r00 * nx + r01 * ny + r02 * nz;
                float rnY = r10 * nx + r11 * ny + r12 * nz;
                float rnZ = r20 * nx + r21 * ny + r22 * nz;

                // Apply Offset
                rx += offset.X;
                ry += offset.Y;
                rz += offset.Z;

                result.AddVertex(new V3(rx, ry, rz), new V3(rnX, rnY, rnZ).Normalized);
            }

            for (int i = 0; i < Triangles.Count; i++)
            {
                result.Triangles.Add(Triangles[i]);
            }

            return result;
        }

        /// <summary>
        /// Looks straight down from a point: how far below it is the first surface of this mesh?
        /// Returns false if there is nothing within maxDrop. (For standing a bird on a branch.)
        /// </summary>
        public bool TryDropOnto(V3 from, float maxDrop, out float drop)
        {
            drop = float.MaxValue;
            var down = new V3(0f, -1f, 0f);
            for (int i = 0; i + 2 < Triangles.Count; i += 3)
            {
                V3 a = Vertices[Triangles[i]], b = Vertices[Triangles[i + 1]], c = Vertices[Triangles[i + 2]];
                V3 e1 = b - a, e2 = c - a, h = V3.Cross(down, e2);
                float det = V3.Dot(e1, h);
                if (Math.Abs(det) < 1e-9f) continue;
                V3 s = from - a;
                float u = V3.Dot(s, h) / det;
                if (u < 0f || u > 1f) continue;
                V3 q = V3.Cross(s, e1);
                float v = V3.Dot(down, q) / det;
                if (v < 0f || u + v > 1f) continue;
                float t = V3.Dot(e2, q) / det;
                if (t > 0f && t < drop) drop = t;
            }
            return drop <= maxDrop;
        }

        public V3 Min
        {
            get
            {
                if (Vertices.Count == 0) return V3.Zero;
                float minX = Vertices[0].X, minY = Vertices[0].Y, minZ = Vertices[0].Z;
                for (int i = 1; i < Vertices.Count; i++)
                {
                    var v = Vertices[i];
                    if (v.X < minX) minX = v.X;
                    if (v.Y < minY) minY = v.Y;
                    if (v.Z < minZ) minZ = v.Z;
                }
                return new V3(minX, minY, minZ);
            }
        }

        public V3 Max
        {
            get
            {
                if (Vertices.Count == 0) return V3.Zero;
                float maxX = Vertices[0].X, maxY = Vertices[0].Y, maxZ = Vertices[0].Z;
                for (int i = 1; i < Vertices.Count; i++)
                {
                    var v = Vertices[i];
                    if (v.X > maxX) maxX = v.X;
                    if (v.Y > maxY) maxY = v.Y;
                    if (v.Z > maxZ) maxZ = v.Z;
                }
                return new V3(maxX, maxY, maxZ);
            }
        }
    }
}
