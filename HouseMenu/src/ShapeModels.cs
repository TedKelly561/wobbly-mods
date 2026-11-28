using System.Collections.Generic;
using UnityEngine;
using WobblyMods.Core.Shapes;
using WobblyMods.HouseMenu.Tanks;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Turns models made in code with the shape library (Core/src/Shapes, e.g. RedtailModel) into Unity objects:
    /// one child per colored part. Visual only (no colliders). This is how Skylar's redtail gets into the game
    /// without the Unity kit.
    /// </summary>
    internal static class ShapeModels
    {
        private static readonly Dictionary<string, Mesh[]> MeshCache = new Dictionary<string, Mesh[]>();

        /// <summary>Skylar's redtail catfish: 1 unit long, nose toward +Z, belly down.</summary>
        public static GameObject Redtail()
        {
            return Build("Redtail catfish", RedtailModel.Build());
        }

        public static GameObject Build(string name, IList<ColoredMesh> parts)
        {
            Mesh[] meshes;
            if (!MeshCache.TryGetValue(name, out meshes))
            {
                meshes = new Mesh[parts.Count];
                for (int i = 0; i < parts.Count; i++) meshes[i] = ToMesh(parts[i].Mesh, name + " part " + i);
                MeshCache[name] = meshes;
            }

            var root = new GameObject(name);
            for (int i = 0; i < parts.Count; i++)
            {
                var child = new GameObject("part " + i);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                Rgb c = parts[i].Color;
                child.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color32(c.R, c.G, c.B, 255));
            }
            return root;
        }

        /// <summary>Like Build, but solid: each part gets a mesh collider (our meshes are made here, so they can be).</summary>
        public static GameObject BuildSolid(string name, IList<ColoredMesh> parts)
        {
            GameObject root = Build(name, parts);
            foreach (Transform child in root.transform)
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) child.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            return root;
        }

        private static Mesh ToMesh(ShapeMesh shape, string name)
        {
            var verts = new List<Vector3>(shape.Vertices.Count);
            var normals = new List<Vector3>(shape.Normals.Count);
            foreach (V3 v in shape.Vertices) verts.Add(new Vector3(v.X, v.Y, v.Z));
            foreach (V3 n in shape.Normals) normals.Add(new Vector3(n.X, n.Y, n.Z));

            // The library's rule (cross(b-a, c-a) points outward) is Unity's front face. The reversed copies are a
            // safety net: if that ever turned out wrong in game, parts would look a bit dark instead of invisible.
            var tris = new List<int>(shape.Triangles.Count * 2);
            tris.AddRange(shape.Triangles);
            for (int i = 0; i + 2 < shape.Triangles.Count; i += 3)
            {
                tris.Add(shape.Triangles[i]); tris.Add(shape.Triangles[i + 2]); tris.Add(shape.Triangles[i + 1]);
            }

            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static readonly Dictionary<Color32, Material> Materials = new Dictionary<Color32, Material>();

        private static Material MaterialFor(Color32 color)
        {
            Material m;
            if (!Materials.TryGetValue(color, out m) || m == null)
            {
                m = TankMaterials.Solid(color);
                m.name = "Shape model";
                m.enableInstancing = true; // sixty trees share six shapes: let the graphics card draw each shape in one go
                Materials[color] = m;
            }
            return m;
        }
    }
}
