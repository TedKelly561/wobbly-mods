using System;
using System.Collections.Generic;
using UnityEngine;
using WobblyMods.HouseMenu.Icons;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// "Copy this" (parent's request, 2026-11-21): the game's intact furniture (fridges, sofas, tables, baths...)
    /// exists only inside the world's houses and shops, not as props the menu can load by name. So: look at any
    /// real thing in the world, press "Copy this", and the menu keeps a copy in one of nine "My copies" slots
    /// (shown as a photo). Picking a slot places a copy in front of you, e.g. back at home.
    ///
    /// A copy is the thing's LOOK and SHAPE only: its meshes, materials and solid colliders. No game scripts,
    /// physics bodies or network objects are copied, so a copy can't break the game's own object, but it also
    /// doesn't open, switch on or move by itself. Copies last until the game closes (they aren't in house saves yet).
    /// The slots are shared by all players.
    /// </summary>
    internal static class WorldCopy
    {
        public const int Slots = 9;
        private const float MaxSize = 8f;       // bigger than this is a building or the ground, not furniture
        private const int MaxRenderers = 80;

        private static readonly GameObject[] Templates = new GameObject[Slots];
        private static readonly string[] Names = new string[Slots];
        private static int _next;
        private static Transform _shelf;

        public static string IconKey(int slot)
        {
            return "worldcopy" + (slot + 1);
        }

        /// <summary>Copies what that player is looking at into the next slot. Returns a message for the menu.</summary>
        public static string Copy(int player)
        {
            try
            {
                Camera cam = GameBindings.CameraFor(player);
                if (cam == null) return "Couldn't find your camera.";

                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                Type character = GameReflection.FindType("PlayerCharacter");

                string reason = "Point the middle of your screen at the thing, close up, then press this.";
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.collider.isTrigger) continue;
                    if (character != null && hit.collider.GetComponentInParent(character) != null) continue; // not yourself

                    GameObject root = PickRoot(hit.collider, out reason);
                    if (root == null) break; // the first solid thing in view decides (don't copy through walls)

                    GameObject template = BuildTemplate(root, out reason);
                    if (template == null) break;

                    int slot = _next;
                    _next = (_next + 1) % Slots;
                    if (Templates[slot] != null) UnityEngine.Object.Destroy(Templates[slot]);
                    Templates[slot] = template;
                    Names[slot] = Nice(root.name);
                    IconCache.PictureOf(IconKey(slot), template, TabTheme.Light(TabTheme.Furniture, 0.6f));
                    Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " copied '" + Path(root.transform) + "' into slot " +
                                       (slot + 1) + " (" + template.transform.childCount + " pieces).");
                    return "Copied the " + Names[slot] + "! It's in My copies, picture " + (slot + 1) + ".";
                }
                Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " nothing copied: " + reason);
                return reason;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Copy this failed: " + e);
                return "That couldn't be copied.";
            }
        }

        /// <summary>
        /// For grown-ups (F3): describes the thing in the middle of player 1's view, so we can learn why real
        /// furniture can't be copied yet: each parent up the tree with its part count, size, and whether its meshes
        /// are glued into the scenery's combined mesh.
        /// </summary>
        public static string Probe(int player)
        {
            try
            {
                Camera cam = GameBindings.CameraFor(player);
                if (cam == null) return "Probe: no camera.";
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                Type character = GameReflection.FindType("PlayerCharacter");
                var sb = new System.Text.StringBuilder("Probe (F3), nearest first:");
                int shown = 0;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.collider.isTrigger) continue;
                    if (character != null && hit.collider.GetComponentInParent(character) != null) continue;
                    sb.Append("\n  HIT ").Append(hit.collider.GetType().Name).Append(" at ").Append(hit.distance.ToString("0.0")).Append(" m, body: ")
                      .Append(hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.name : "none");
                    int level = 0;
                    for (Transform t = hit.collider.transform; t != null && level < 6; t = t.parent, level++)
                    {
                        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                        int batched = 0, combined = 0, readable = 0;
                        foreach (Renderer r in rs)
                        {
                            if (r.isPartOfStaticBatch) batched++;
                            MeshFilter mf = r.GetComponent<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null)
                            {
                                if (mf.sharedMesh.name.StartsWith("Combined Mesh", StringComparison.Ordinal)) combined++;
                                if (mf.sharedMesh.isReadable) readable++;
                            }
                        }
                        Bounds b;
                        string size = TryBounds(t.gameObject, false, out b) ? b.size.x.ToString("0.0") + "x" + b.size.y.ToString("0.0") + "x" + b.size.z.ToString("0.0") : "no size";
                        sb.Append("\n    up ").Append(level).Append(": '").Append(t.name).Append("' parts ").Append(rs.Length).Append(", size ").Append(size)
                          .Append(", static-batched ").Append(batched).Append(", combined-mesh ").Append(combined).Append(", readable ").Append(readable)
                          .Append(", children ").Append(t.childCount);
                    }
                    if (++shown >= 3) break;
                }
                return shown == 0 ? "Probe (F3): nothing in the middle of the view." : sb.ToString();
            }
            catch (Exception e)
            {
                return "Probe failed: " + e.Message;
            }
        }

        /// <summary>Places a copy from that slot in front of the player. Returns the new object, or null with a message.</summary>
        public static GameObject Paste(int player, int slot, out string message)
        {
            if (slot < 0 || slot >= Slots || Templates[slot] == null)
            {
                message = "That one is empty. Look at something and pick Copy this.";
                return null;
            }

            Bounds size;
            float longest = TryBounds(Templates[slot], true, out size) ? Mathf.Max(size.size.x, size.size.z) : 1f;
            Vector3 position;
            Quaternion rotation;
            if (!GameBindings.PlacementFor(player, 1f, 2f + longest, out position, out rotation))
            {
                message = "Couldn't find where you are.";
                return null;
            }

            GameObject copy = UnityEngine.Object.Instantiate(Templates[slot], position, rotation);
            copy.name = Names[slot];
            copy.SetActive(true);
            GameBindings.Register(copy, player);
            GameBindings.RestOnFloor(copy, position.y);
            message = "Placed the " + Names[slot] + ".";
            return copy;
        }

        /// <summary>
        /// From the collider that was hit, the object that is "the whole thing": the nearest physics body if there
        /// is one, otherwise as far up the parents as it stays furniture-sized.
        /// </summary>
        private static GameObject PickRoot(Collider hit, out string reason)
        {
            reason = null;
            Transform best = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
            if (!FurnitureSized(best.gameObject))
            {
                // A body can be a whole vehicle or building piece; fall back to just the part that was hit.
                best = hit.transform;
                if (!FurnitureSized(best.gameObject))
                {
                    reason = "That's too big to copy (or it's part of the building).";
                    return null;
                }
            }
            for (Transform up = best.parent; up != null; up = up.parent)
            {
                if (!FurnitureSized(up.gameObject)) break;
                best = up;
            }
            return best.gameObject;
        }

        private static bool FurnitureSized(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || renderers.Length > MaxRenderers) return false;
            Bounds b;
            if (!TryBounds(go, false, out b)) return false;
            return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) <= MaxSize;
        }

        /// <summary>A scripts-free copy: one child per mesh, plus the solid colliders. Kept switched off on a hidden shelf.</summary>
        private static GameObject BuildTemplate(GameObject source, out string reason)
        {
            reason = null;
            Bounds bounds;
            if (!TryBounds(source, false, out bounds))
            {
                reason = "There's nothing to see there to copy.";
                return null;
            }

            if (_shelf == null)
            {
                var shelf = new GameObject("Wobbly copies shelf");
                UnityEngine.Object.DontDestroyOnLoad(shelf);
                shelf.SetActive(false);
                _shelf = shelf.transform;
            }

            // Origin at the bottom center, facing the way the source faces (flat), so it places nicely.
            var template = new GameObject("Copy of " + source.name);
            Vector3 flatForward = source.transform.forward; flatForward.y = 0f;
            template.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            template.transform.rotation = flatForward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flatForward.normalized, Vector3.up) : Quaternion.identity;

            // Only the detailed version of each mesh (the far-away LOD versions would overlap it).
            var lowDetail = new HashSet<Renderer>();
            foreach (LODGroup group in source.GetComponentsInChildren<LODGroup>())
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (Renderer r in lods[i].renderers) if (r != null) lowDetail.Add(r);
                }
            }

            int meshes = 0, batched = 0;
            foreach (Renderer r in source.GetComponentsInChildren<Renderer>())
            {
                if (lowDetail.Contains(r)) continue;
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                Mesh mesh = null;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
                if (r is MeshRenderer && mf != null) mesh = mf.sharedMesh;
                else if (skinned != null) mesh = skinned.sharedMesh;
                if (mesh == null) continue;
                if (r.isPartOfStaticBatch || mesh.name.StartsWith("Combined Mesh", StringComparison.Ordinal))
                {
                    batched++; // glued into the scenery's big combined mesh: can't be taken out on its own
                    continue;
                }

                GameObject piece = Piece(template.transform, r.transform, "mesh");
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                piece.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
                meshes++;
            }
            if (meshes == 0)
            {
                UnityEngine.Object.Destroy(template);
                reason = batched > 0 ? "That one is glued into the building. Try a different one."
                                     : "There's nothing to see there to copy.";
                return null;
            }

            int solids = 0;
            foreach (Collider c in source.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                try
                {
                    GameObject piece = Piece(template.transform, c.transform, "solid");
                    BoxCollider box = c as BoxCollider;
                    SphereCollider sphere = c as SphereCollider;
                    CapsuleCollider capsule = c as CapsuleCollider;
                    if (box != null) { var n = piece.AddComponent<BoxCollider>(); n.center = box.center; n.size = box.size; }
                    else if (sphere != null) { var n = piece.AddComponent<SphereCollider>(); n.center = sphere.center; n.radius = sphere.radius; }
                    else if (capsule != null)
                    {
                        var n = piece.AddComponent<CapsuleCollider>();
                        n.center = capsule.center; n.radius = capsule.radius; n.height = capsule.height; n.direction = capsule.direction;
                    }
                    else
                    {
                        // Mesh (or other) colliders: a box around it. Mesh colliders can't be rebuilt from the
                        // game's locked meshes, and a box is plenty for furniture.
                        piece.transform.rotation = Quaternion.identity;
                        piece.transform.localScale = Vector3.one;
                        piece.transform.position = c.bounds.center;
                        piece.AddComponent<BoxCollider>().size = c.bounds.size;
                    }
                    solids++;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Copy this: skipped a collider on " + c.name + ": " + e.Message);
                }
            }
            if (solids == 0)
            {
                // Nothing solid came across: one box around the whole thing, so it isn't a ghost.
                GameObject piece = new GameObject("solid");
                piece.transform.position = bounds.center;
                piece.transform.SetParent(template.transform, true);
                piece.AddComponent<BoxCollider>().size = bounds.size;
            }

            template.transform.SetParent(_shelf, true);
            return template;
        }

        private static GameObject Piece(Transform template, Transform like, string name)
        {
            var piece = new GameObject(name);
            piece.layer = like.gameObject.layer;
            piece.transform.position = like.position;
            piece.transform.rotation = like.rotation;
            piece.transform.localScale = like.lossyScale;
            piece.transform.SetParent(template, true);
            return piece;
        }

        private static bool TryBounds(GameObject go, bool includeInactive, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(includeInactive))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && path.Length < 200; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        /// <summary>"Prop_Fridge_01 (3)" becomes "fridge".</summary>
        private static string Nice(string name)
        {
            string n = Core.PropRooms.NiceName(name.Replace("(Clone)", "").Replace("Prop_", ""));
            return string.IsNullOrEmpty(n) || n == "thing" ? "thing" : n;
        }
    }
}
