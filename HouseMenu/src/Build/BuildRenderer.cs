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

        // Which block each collider belongs to (by the collider's instance id), so "remove the piece I'm
        // looking at" removes the piece the aim ray really hit. Kept in step with the chunks as they're redrawn.
        private readonly BlockPickMap<int> _pick = new BlockPickMap<int>();
        private readonly List<int> _hitIds = new List<int>();
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
            _pick.Clear();
        }

        /// <summary>
        /// From the things an aim ray went through (sorted nearest first): the first one that is a piece of
        /// the build. Anything else in the way (the ground, a prop, the player's own body) is skipped.
        /// </summary>
        public bool FirstBlockHit(RaycastHit[] hitsNearestFirst, BuildModel model, out int blockId)
        {
            _hitIds.Clear();
            for (int i = 0; i < hitsNearestFirst.Length; i++)
            {
                Collider c = hitsNearestFirst[i].collider;
                if (c != null) _hitIds.Add(c.GetInstanceID());
            }
            return _pick.FirstBlock(_hitIds, model, out blockId);
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
            _pick.Clear();
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

        /// <summary>True if a collider's position and size are sane numbers (no NaN/infinity, not huge or flat).</summary>
        private static bool SafeCollider(Vector3 position, Vector3 size)
        {
            bool ok = Finite(position) && Finite(size) &&
                      size.x > 0.001f && size.y > 0.001f && size.z > 0.001f &&
                      size.x < 100f && size.y < 100f && size.z < 100f &&
                      position.sqrMagnitude < 1e10f;
            if (!ok)
            {
                Plugin.Log.LogWarning("Build: skipped a collider with position " + position + " size " + size + ".");
            }
            return ok;
        }

        private static bool Finite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        private void RebuildChunk(BuildModel model, GridSettings grid, ChunkPos chunk)
        {
            ChunkView old;
            if (_chunks.TryGetValue(chunk, out old))
            {
                DestroyView(old);
                _chunks.Remove(chunk);
            }
            // The old colliders go with the old view (also when the chunk was only half drawn before).
            _pick.RemoveChunk(chunk);

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
            int colliders = 0;
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
                        Vector3 center = blockM.MultiplyPoint3x4(part.Center);
                        Vector3 s = yaw * Vector3.Scale(part.Size, cellScale);
                        Vector3 size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                        // A collider with a bad size or position can crash the physics engine, so skip it.
                        if (SafeCollider(center, size))
                        {
                            BoxCollider box = view.Root.AddComponent<BoxCollider>();
                            box.center = center;
                            box.size = size;
                            _pick.Add(chunk, box.GetInstanceID(), _blocks[b].Id);
                            colliders++;
                        }
                    }
                    else if (part.Shape == PartShape.Ramp)
                    {
                        Vector3 position = blockM.MultiplyPoint3x4(part.Center);
                        Vector3 size = Vector3.Scale(part.Size, cellScale);
                        if (SafeCollider(position, size))
                        {
                            // A tilted collider needs its own transform.
                            var ramp = new GameObject("Ramp");
                            ramp.transform.SetParent(view.Root.transform, false);
                            ramp.transform.position = position;
                            ramp.transform.rotation = yaw * Quaternion.Euler(part.TiltX, 0f, 0f);
                            BoxCollider box = ramp.AddComponent<BoxCollider>();
                            box.size = size;
                            _pick.Add(chunk, box.GetInstanceID(), _blocks[b].Id);
                            colliders++;
                        }
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
            Plugin.Log.LogInfo("Build: drew " + chunk + ": " + _blocks.Count + " blocks, " + colliders + " colliders.");
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

        /// <summary>
        /// One see-through mesh for a whole room kit, in kit-local space (pivot = bottom centre of the
        /// kit's anchor cell, facing +Z). Place it at the anchor's pivot and turn it by the kit's facing.
        /// </summary>
        public static Mesh BuildKitGhostMesh(IList<KitPiece> pieces, BuildCatalog catalog, float cellSize, string name)
        {
            var list = new List<CombineInstance>();
            Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * cellSize);
            int vertices = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                BlockType type = catalog.GetBlock(pieces[i].TypeId);
                if (type == null) continue;
                Matrix4x4 place = Matrix4x4.TRS(
                    new Vector3(pieces[i].Cell.X, pieces[i].Cell.Y, pieces[i].Cell.Z),
                    Quaternion.Euler(0f, pieces[i].LocalRotation.Degrees, 0f),
                    Vector3.one);
                BlockPart[] parts = BuildShapes.PartsFor(type);
                for (int j = 0; j < parts.Length; j++)
                {
                    Mesh mesh = BuildShapes.MeshFor(parts[j].Shape);
                    if (mesh == null) continue;
                    vertices += mesh.vertexCount;
                    list.Add(new CombineInstance
                    {
                        mesh = mesh,
                        transform = scale * place * Matrix4x4.TRS(parts[j].Center, Quaternion.identity, parts[j].Size),
                    });
                }
            }
            var combined = new Mesh { name = name };
            if (vertices > 65000)
            {
                combined.indexFormat = IndexFormat.UInt32;
            }
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
