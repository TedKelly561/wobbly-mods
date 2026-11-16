using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WobblyMods.BuildMode;

namespace WobblyMods.HouseMenu.Build
{
    /// <summary>
    /// Turns the BuildModel into Unity objects, one chunk at a time. Each 16 x 16 x 16-cell chunk is
    /// one GameObject holding a merged mesh per look (material + paint, via Mesh.CombineMeshes) and a
    /// BoxCollider per solid box part, so players can stand on floors and walk up stairs. Only dirty
    /// chunks are rebuilt (BuildModel.TakeDirtyChunks), a few per frame, so big builds don't lag.
    ///
    /// The build is local (not networked) in v1: only this computer sees it, which is all the family
    /// needs for same-computer split screen.
    /// </summary>
    internal sealed class BuildRenderer
    {
        private const int ChunksPerFrame = 3;

        private sealed class ChunkView
        {
            public GameObject Root;
            public readonly List<Mesh> Meshes = new List<Mesh>();
        }

        private sealed class LookGroup
        {
            public Material Material;
            public bool Transparent;
            public readonly List<CombineInstance> Parts = new List<CombineInstance>();
        }

        private readonly Dictionary<ChunkPos, ChunkView> _chunks = new Dictionary<ChunkPos, ChunkView>();
        private readonly List<ChunkPos> _dirty = new List<ChunkPos>();
        private readonly List<PlacedBlock> _blocks = new List<PlacedBlock>();
        private readonly Dictionary<string, LookGroup> _groups = new Dictionary<string, LookGroup>();
        private GameObject _root;

        /// <summary>
        /// Rebuilds a few dirty chunks, nearest to <paramref name="nearCell"/> first.
        /// If the scene changed (Unity destroyed our objects), starts again and redraws everything.
        /// UNCERTAIN: the build then shows in whatever scene is loaded; v1 doesn't remember which world it belongs to.
        /// </summary>
        public void Update(BuildModel model, GridSettings grid, GridPos nearCell)
        {
            if (_root == null)
            {
                bool hadChunks = _chunks.Count > 0;
                ForgetAll();
                _root = new GameObject("Wobbly Build");
                if (hadChunks)
                {
                    model.MarkAllDirty();
                }
            }

            _dirty.Clear();
            model.TakeDirtyChunks(_dirty, ChunksPerFrame, nearCell);
            for (int i = 0; i < _dirty.Count; i++)
            {
                try
                {
                    RebuildChunk(model, grid, _dirty[i]);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Build: drawing " + _dirty[i] + " failed: " + e);
                }
            }
        }

        /// <summary>Destroys every chunk object (before loading another build or clearing).</summary>
        public void DestroyAll()
        {
            foreach (ChunkView view in _chunks.Values)
            {
                DestroyView(view);
            }
            _chunks.Clear();
        }

        private void ForgetAll()
        {
            // The GameObjects are already gone with the old scene; free the meshes we made.
            foreach (ChunkView view in _chunks.Values)
            {
                foreach (Mesh m in view.Meshes)
                {
                    if (m != null) Object.Destroy(m);
                }
            }
            _chunks.Clear();
        }

        private static void DestroyView(ChunkView view)
        {
            foreach (Mesh m in view.Meshes)
            {
                if (m != null) Object.Destroy(m);
            }
            view.Meshes.Clear();
            if (view.Root != null) Object.Destroy(view.Root);
        }

        private void RebuildChunk(BuildModel model, GridSettings grid, ChunkPos chunk)
        {
            ChunkView old;
            if (_chunks.TryGetValue(chunk, out old))
            {
                DestroyView(old);
                _chunks.Remove(chunk);
            }

            _blocks.Clear();
            model.GetBlocksInChunk(chunk, _blocks);
            if (_blocks.Count == 0)
            {
                return;
            }

            var view = new ChunkView { Root = new GameObject("Build " + chunk) };
            view.Root.transform.SetParent(_root.transform, false);
            _chunks[chunk] = view;

            float cell = grid.CellSize;
            Vector3 cellScale = Vector3.one * cell;
            foreach (LookGroup g in _groups.Values)
            {
                g.Parts.Clear();
            }

            for (int b = 0; b < _blocks.Count; b++)
            {
                BlockSpec spec = _blocks[b].Spec;
                Quaternion yaw = Quaternion.Euler(0f, spec.Rotation.Degrees, 0f);
                Matrix4x4 blockM = Matrix4x4.TRS(ToUnity(grid.BlockPivot(spec.Anchor)), yaw, cellScale);
                BlockPart[] parts = BuildShapes.PartsFor(spec.Type);

                for (int i = 0; i < parts.Length; i++)
                {
                    BlockPart part = parts[i];
                    Mesh mesh = BuildShapes.MeshFor(part.Shape);
                    if (mesh != null)
                    {
                        string key;
                        Material mat = part.Glass ? BuildLooks.PaneGlass(out key) : BuildLooks.MaterialFor(spec.Material, spec.Paint, out key);
                        LookGroup group;
                        if (!_groups.TryGetValue(key, out group))
                        {
                            group = new LookGroup();
                            _groups[key] = group;
                        }
                        group.Material = mat;
                        group.Transparent = part.Glass || spec.Material.IsTransparent;
                        group.Parts.Add(new CombineInstance
                        {
                            mesh = mesh,
                            transform = blockM * Matrix4x4.TRS(part.Center, Quaternion.identity, part.Size),
                        });
                    }

                    if (part.Shape == PartShape.Box && part.Collider)
                    {
                        // Quarter turns keep boxes axis-aligned, so one BoxCollider on the chunk works.
                        BoxCollider box = view.Root.AddComponent<BoxCollider>();
                        box.center = blockM.MultiplyPoint3x4(part.Center);
                        Vector3 s = yaw * Vector3.Scale(part.Size, cellScale);
                        box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    }
                    else if (part.Shape == PartShape.Ramp)
                    {
                        // A tilted collider needs its own transform.
                        var ramp = new GameObject("Ramp");
                        ramp.transform.SetParent(view.Root.transform, false);
                        ramp.transform.position = blockM.MultiplyPoint3x4(part.Center);
                        ramp.transform.rotation = yaw * Quaternion.Euler(part.TiltX, 0f, 0f);
                        BoxCollider box = ramp.AddComponent<BoxCollider>();
                        box.size = Vector3.Scale(part.Size, cellScale);
                    }
                }
            }

            foreach (KeyValuePair<string, LookGroup> pair in _groups)
            {
                LookGroup g = pair.Value;
                if (g.Parts.Count == 0)
                {
                    continue;
                }
                var mesh = new Mesh { name = "Build " + chunk + " " + pair.Key };
                // Big chunks pass Unity's 65k-vertex limit for 16-bit indices.
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(g.Parts.ToArray(), true, true);
                view.Meshes.Add(mesh);

                var go = new GameObject(pair.Key);
                go.transform.SetParent(view.Root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = g.Material;
                if (g.Transparent)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                g.Parts.Clear();
            }
        }

        /// <summary>
        /// A block type's visible parts merged into one mesh in block-local space (pivot at the bottom
        /// centre of the anchor cell), for the ghost preview. The caller owns and caches the mesh.
        /// </summary>
        public static Mesh BuildGhostMesh(BlockType type, float cellSize)
        {
            BlockPart[] parts = BuildShapes.PartsFor(type);
            var list = new List<CombineInstance>();
            Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * cellSize);
            for (int i = 0; i < parts.Length; i++)
            {
                Mesh mesh = BuildShapes.MeshFor(parts[i].Shape);
                if (mesh == null) continue;
                list.Add(new CombineInstance
                {
                    mesh = mesh,
                    transform = scale * Matrix4x4.TRS(parts[i].Center, Quaternion.identity, parts[i].Size),
                });
            }
            var combined = new Mesh { name = "Build ghost " + type.Id };
            combined.CombineMeshes(list.ToArray(), true, true);
            return combined;
        }

        public static Vector3 ToUnity(Vec3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vec3 ToCore(Vector3 v)
        {
            return new Vec3(v.x, v.y, v.z);
        }
    }
}
