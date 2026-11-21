using System.Collections.Generic;
using UnityEngine;
using WobblyMods.BuildMode;

namespace WobblyMods.HouseMenu.Build
{
    internal enum PartShape
    {
        /// <summary>A box: drawn, and solid unless <see cref="BlockPart.Collider"/> is false.</summary>
        Box,

        /// <summary>A roof wedge: drawn only (its <see cref="Ramp"/> part makes it walkable).</summary>
        Wedge,

        /// <summary>An invisible tilted slab players walk up (stairs and roof slopes).</summary>
        Ramp,
    }

    /// <summary>
    /// One piece of a block, in block-local CELL units (multiply by GridSettings.CellSize), before the
    /// block's yaw. Local space follows BuildMode.Core's README: the pivot is the bottom centre of the
    /// anchor cell, so the anchor cell spans x and z from -0.5 to 0.5 and y from 0 to 1, and the
    /// footprint extends toward +X (width), +Y and +Z (depth).
    /// </summary>
    internal struct BlockPart
    {
        public PartShape Shape;
        public Vector3 Center;
        public Vector3 Size;

        /// <summary>Ramp only: tilt around local X in degrees (negative = rises toward +Z).</summary>
        public float TiltX;

        /// <summary>Drawn with glass whatever the block's material (window panes).</summary>
        public bool Glass;

        /// <summary>Box only: false for drawn-only boxes (stair steps, which use one ramp instead).</summary>
        public bool Collider;
    }

    /// <summary>
    /// Block shapes built from boxes and one wedge mesh, keyed by BuildCatalog block id.
    /// Kept deliberately simple for v1: no textures, no face culling between neighbours.
    /// </summary>
    internal static class BuildShapes
    {
        private const float WallThickness = 0.2f;

        private static readonly Dictionary<string, BlockPart[]> Cache = new Dictionary<string, BlockPart[]>();
        private static Mesh _cube;
        private static Mesh _wedge;

        /// <summary>The parts of a block type (cached per type id).</summary>
        public static BlockPart[] PartsFor(BlockType type)
        {
            BlockPart[] parts;
            if (!Cache.TryGetValue(type.Id, out parts))
            {
                parts = Make(type).ToArray();
                Cache[type.Id] = parts;
            }
            return parts;
        }

        private static List<BlockPart> Make(BlockType type)
        {
            var p = new List<BlockPart>();
            float w = type.Size.X;
            float h = type.Size.Y;
            float d = type.Size.Z;
            float cx = (w - 1f) * 0.5f; // footprint centre along local X
            float cz = (d - 1f) * 0.5f; // and Z
            float left = -0.5f;
            float right = w - 0.5f;

            switch (type.Id)
            {
                case BuildCatalog.Ids.Floor:
                    p.Add(Box(cx, 0.05f, cz, w, 0.1f, d));
                    break;

                case BuildCatalog.Ids.RoofFlat:
                    p.Add(Box(cx, 0.075f, cz, w, 0.15f, d));
                    break;

                case BuildCatalog.Ids.Wall:
                    p.Add(Box(cx, h * 0.5f, cz, w, h, WallThickness));
                    break;

                case BuildCatalog.Ids.HalfWall:
                    p.Add(Box(cx, h * 0.25f, cz, w, h * 0.5f, WallThickness));
                    break;

                case BuildCatalog.Ids.Pillar:
                    p.Add(Box(cx, h * 0.5f, cz, 0.3f, h, 0.3f));
                    break;

                case BuildCatalog.Ids.CornerWall:
                {
                    // An L through the cell centre: one arm to the +X edge, one to the +Z edge, so a wall
                    // in the next cell along X and one in the next cell along Z meet it with no gap.
                    const float half = WallThickness * 0.5f;
                    float armX = 0.5f + half;          // from x = -half to the +X edge
                    float armZ = 0.5f - half;          // from z = +half to the +Z edge
                    p.Add(Box(0.5f - armX * 0.5f, h * 0.5f, 0f, armX, h, WallThickness));
                    p.Add(Box(0f, h * 0.5f, 0.5f - armZ * 0.5f, WallThickness, h, armZ));
                    break;
                }

                case BuildCatalog.Ids.Beam:
                    p.Add(Box(cx, h - 0.1f, cz, w, 0.2f, 0.25f));
                    break;

                case BuildCatalog.Ids.Window:
                {
                    // Frame in the block's material, with a glass pane in the middle.
                    const float sill = 0.35f;
                    const float lintel = 0.2f;
                    const float post = 0.15f;
                    float openH = h - sill - lintel;
                    p.Add(Box(cx, sill * 0.5f, cz, w, sill, WallThickness));
                    p.Add(Box(cx, h - lintel * 0.5f, cz, w, lintel, WallThickness));
                    p.Add(Box(left + post * 0.5f, sill + openH * 0.5f, cz, post, openH, WallThickness));
                    p.Add(Box(right - post * 0.5f, sill + openH * 0.5f, cz, post, openH, WallThickness));
                    BlockPart pane = Box(cx, sill + openH * 0.5f, cz, w - 2f * post, openH, 0.04f);
                    pane.Glass = true;
                    p.Add(pane);
                    break;
                }

                case BuildCatalog.Ids.Door:
                {
                    // An open doorway (no door panel in v1), so players can walk through.
                    // UNCERTAIN: opening size (0.7 m x 1.75 m at 1 m cells) is a guess at what fits a Wobbly character.
                    const float post = 0.15f;
                    float openTop = Mathf.Min(1.75f, h - 0.1f);
                    p.Add(Box(left + post * 0.5f, h * 0.5f, cz, post, h, WallThickness));
                    p.Add(Box(right - post * 0.5f, h * 0.5f, cz, post, h, WallThickness));
                    p.Add(Box(cx, (openTop + h) * 0.5f, cz, w - 2f * post, h - openTop, WallThickness));
                    break;
                }

                case BuildCatalog.Ids.Stairs:
                {
                    // Four solid steps climbing toward +Z (drawn only), plus one ramp collider to walk on.
                    const int steps = 4;
                    float stepD = d / steps;
                    for (int i = 0; i < steps; i++)
                    {
                        float stepH = h * (i + 1) / steps;
                        BlockPart s = Box(cx, stepH * 0.5f, -0.5f + stepD * (i + 0.5f), w, stepH, stepD);
                        s.Collider = false;
                        p.Add(s);
                    }
                    p.Add(Ramp(cx, w, h, d));
                    break;
                }

                case BuildCatalog.Ids.RoofSlope:
                    p.Add(new BlockPart { Shape = PartShape.Wedge, Center = new Vector3(cx, h * 0.5f, cz), Size = new Vector3(w, h, d) });
                    p.Add(Ramp(cx, w, h, d));
                    break;

                default:
                    // A block type added later without a shape here: a plain box of its footprint.
                    p.Add(Box(cx, h * 0.5f, cz, w, h, d));
                    break;
            }
            return p;
        }

        private static BlockPart Box(float x, float y, float z, float sx, float sy, float sz)
        {
            return new BlockPart
            {
                Shape = PartShape.Box,
                Center = new Vector3(x, y, z),
                Size = new Vector3(sx, sy, sz),
                Collider = true,
            };
        }

        /// <summary>A thin slab whose top runs from the footprint's low front edge (z = -0.5, y = 0) to its high back edge.</summary>
        private static BlockPart Ramp(float cx, float w, float h, float d)
        {
            const float thickness = 0.1f;
            float angle = Mathf.Atan2(h, d) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(h * h + d * d);
            Quaternion tilt = Quaternion.Euler(-angle, 0f, 0f);
            Vector3 mid = new Vector3(cx, h * 0.5f, -0.5f + d * 0.5f);
            Vector3 center = mid - tilt * Vector3.up * (thickness * 0.5f); // top face on the slope line
            return new BlockPart
            {
                Shape = PartShape.Ramp,
                Center = center,
                Size = new Vector3(w, thickness, length),
                TiltX = -angle,
            };
        }

        /// <summary>A unit cube centred on the origin (1 x 1 x 1), flat-shaded.</summary>
        public static Mesh Cube
        {
            get
            {
                if (_cube == null)
                {
                    var b = new MeshBuilder();
                    Vector3 a0 = new Vector3(-0.5f, -0.5f, -0.5f), a1 = new Vector3(0.5f, -0.5f, -0.5f);
                    Vector3 a2 = new Vector3(0.5f, -0.5f, 0.5f), a3 = new Vector3(-0.5f, -0.5f, 0.5f);
                    Vector3 b0 = new Vector3(-0.5f, 0.5f, -0.5f), b1 = new Vector3(0.5f, 0.5f, -0.5f);
                    Vector3 b2 = new Vector3(0.5f, 0.5f, 0.5f), b3 = new Vector3(-0.5f, 0.5f, 0.5f);
                    b.Quad(a0, a1, a2, a3, Vector3.down);
                    b.Quad(b0, b1, b2, b3, Vector3.up);
                    b.Quad(a0, a1, b1, b0, Vector3.back);
                    b.Quad(a3, a2, b2, b3, Vector3.forward);
                    b.Quad(a0, a3, b3, b0, Vector3.left);
                    b.Quad(a1, a2, b2, b1, Vector3.right);
                    _cube = b.ToMesh("Build cube");
                }
                return _cube;
            }
        }

        /// <summary>
        /// A unit roof wedge centred on the origin: 1 x 1 x 1, low edge at z = -0.5 (bottom),
        /// rising to the full height at z = +0.5, with a vertical back face there.
        /// </summary>
        public static Mesh Wedge
        {
            get
            {
                if (_wedge == null)
                {
                    var b = new MeshBuilder();
                    Vector3 f0 = new Vector3(-0.5f, -0.5f, -0.5f), f1 = new Vector3(0.5f, -0.5f, -0.5f);
                    Vector3 k0 = new Vector3(-0.5f, -0.5f, 0.5f), k1 = new Vector3(0.5f, -0.5f, 0.5f);
                    Vector3 t0 = new Vector3(-0.5f, 0.5f, 0.5f), t1 = new Vector3(0.5f, 0.5f, 0.5f);
                    b.Quad(f0, f1, k1, k0, Vector3.down);
                    b.Quad(k0, k1, t1, t0, Vector3.forward);
                    b.Quad(f0, f1, t1, t0, new Vector3(0f, 1f, -1f).normalized);
                    b.Tri(f0, k0, t0, Vector3.left);
                    b.Tri(f1, k1, t1, Vector3.right);
                    _wedge = b.ToMesh("Build wedge");
                }
                return _wedge;
            }
        }

        /// <summary>Unity mesh for a visible part's shape (null for collider-only parts).</summary>
        public static Mesh MeshFor(PartShape shape)
        {
            switch (shape)
            {
                case PartShape.Box: return Cube;
                case PartShape.Wedge: return Wedge;
                default: return null;
            }
        }

        /// <summary>Tiny flat-shaded mesh writer that fixes triangle winding from the wanted normal.</summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _verts = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int> _tris = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                Tri(a, b, c, normal);
                Tri(a, c, d, normal);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                // Unity draws a triangle's front when Cross(b - a, c - a) points toward the viewer.
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                {
                    Vector3 t = b;
                    b = c;
                    c = t;
                }
                int i = _verts.Count;
                _verts.Add(a);
                _verts.Add(b);
                _verts.Add(c);
                _normals.Add(normal);
                _normals.Add(normal);
                _normals.Add(normal);
                _tris.Add(i);
                _tris.Add(i + 1);
                _tris.Add(i + 2);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(_verts);
                m.SetNormals(_normals);
                m.SetTriangles(_tris, 0);
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
