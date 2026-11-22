using System;
using System.Collections.Generic;
using UnityEngine;
using WobblyMods.HouseMenu.Icons;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Scripts-free copies of game objects: their meshes, materials and solid shape, with no game scripts, physics
    /// bodies or network objects, so a copy can't break the game's own object and can't fall through the floor.
    ///
    /// Two uses:
    /// 1. "Copy this" (a tool: pick it, the menu closes, a crosshair shows what will be copied, tap RB): any real
    ///    thing in the world goes into one of nine "My copies" slots, shown as a photo; picking a slot places it.
    /// 2. Catalog props (beds, wardrobe, toys...): the game's network spawner stopped answering for all 26 of them
    ///    (menu self-test, 2026-11-21), so the menu now places a static copy made from the prop's prefab.
    ///
    /// Copies last until the game closes (world copies aren't in house saves yet). Slots are shared by all players.
    /// </summary>
    internal static class WorldCopy
    {
        public const int Slots = 9;
        private const float MaxSize = 12f;      // bigger than this is a building or the ground (test: the giant TV is 8.3 m)
        private const int MaxRenderers = 80;
        private const float Reach = 30f;

        private static readonly GameObject[] Templates = new GameObject[Slots];
        private static readonly string[] Names = new string[Slots];
        private static readonly float[] Masses = new float[Slots];   // > 0 = the original was a loose thing, so copies are loose too
        private static readonly int[] Pasted = new int[Slots];
        private static readonly string[] Reasons = new string[8];

        /// <summary>Why nothing can be copied right now (for the text under the +), or null.</summary>
        public static string LastReason(int player)
        {
            return Reasons[player & 7];
        }
        private static int _next;
        private static Transform _shelf;

        public static string IconKey(int slot)
        {
            return "worldcopy" + (slot + 1);
        }

        /// <summary>The name of what "Copy this" would copy right now, or null. For the crosshair label.</summary>
        public static string TargetName(int player)
        {
            string reason;
            GameObject root = FindTarget(player, out reason);
            Reasons[player & 7] = root != null ? null : reason;
            return root != null ? Nice(root.name) : null;
        }

        /// <summary>Copies what that player is aiming at into the next slot. Returns a message; every try is logged.</summary>
        public static string Copy(int player)
        {
            try
            {
                string reason;
                GameObject root = FindTarget(player, out reason);
                if (root == null)
                {
                    Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " nothing copied: " + reason);
                    return reason;
                }
                GameObject template = BuildTemplate(root, false, out reason);
                if (template == null)
                {
                    Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " couldn't copy '" + Path(root.transform) + "': " + reason);
                    return reason;
                }

                if (_shelf == null)
                {
                    var shelf = new GameObject("Wobbly copies shelf");
                    UnityEngine.Object.DontDestroyOnLoad(shelf);
                    shelf.SetActive(false);
                    _shelf = shelf.transform;
                }
                template.transform.SetParent(_shelf, true);

                int slot = _next;
                _next = (_next + 1) % Slots;
                if (Templates[slot] != null) UnityEngine.Object.Destroy(Templates[slot]);
                Templates[slot] = template;
                Names[slot] = Nice(root.name);
                // Skylar's request (2026-11-22): if the original moves around (a tennis ball, a floaty), so does the copy.
                Rigidbody body = root.GetComponentInChildren<Rigidbody>();
                Masses[slot] = body != null ? Mathf.Clamp(body.mass, 0.2f, 60f) : 0f;
                Pasted[slot] = 0;
                IconCache.PictureOf(IconKey(slot), template, TabTheme.Light(TabTheme.Furniture, 0.6f));
                Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " copied '" + Path(root.transform) + "' into slot " +
                                   (slot + 1) + " (" + template.transform.childCount + " pieces, " + (Masses[slot] > 0f ? "loose, mass " + Masses[slot].ToString("0.0") : "fixed") + ").");
                return "Copied the " + Names[slot] + "! It's in My copies, picture " + (slot + 1) + ".";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Copy this failed: " + e);
                return "That couldn't be copied.";
            }
        }

        /// <summary>Places a copy from that slot in front of the player. Returns the new object, or null with a message.</summary>
        public static GameObject Paste(int player, int slot, out string message)
        {
            if (slot < 0 || slot >= Slots || Templates[slot] == null)
            {
                message = "That one is empty. Pick Copy this, aim at something and tap RB.";
                return null;
            }

            Bounds size;
            float longest = MeshBounds(Templates[slot], true, out size) ? Mathf.Max(size.size.x, size.size.z) : 1f;
            Vector3 position;
            Quaternion rotation;
            if (!GameBindings.PlacementFor(player, 1f, 2f + longest, out position, out rotation))
            {
                message = "Couldn't find where you are.";
                return null;
            }

            // Each one a little to the side of the last, so several copies don't land inside each other.
            int n = Pasted[slot]++;
            position += rotation * new Vector3(((n % 5) - 2) * (longest + 0.15f), 0f, -(n / 5 % 3) * (longest + 0.15f));

            GameObject copy = UnityEngine.Object.Instantiate(Templates[slot], position, rotation);
            copy.name = Names[slot];
            copy.SetActive(true);
            GameBindings.Register(copy, player);
            GameBindings.RestOnFloor(copy, position.y);
            if (Masses[slot] > 0f)
            {
                var body = copy.AddComponent<Rigidbody>();
                body.mass = Masses[slot];
                body.drag = 0.1f;
                body.angularDrag = 0.4f;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            Plugin.Log.LogInfo("Copy this: player " + (player + 1) + " placed copy " + (slot + 1) + " (" + Names[slot] + ") at " + copy.transform.position.ToString("0.0") + ".");
            message = "Placed the " + Names[slot] + ".";
            return copy;
        }

        /// <summary>A static, scripts-free copy of a prop prefab (for catalog items), origin at its bottom center. Null with a reason on failure.</summary>
        public static GameObject StaticCopy(GameObject prefab, out string reason)
        {
            return BuildTemplate(prefab, true, out reason);
        }

        /// <summary>
        /// What the middle of that player's view points at, as "the whole thing". From the F3 probes (2026-11-21):
        /// the house's one big mesh collider is often hit a little in front of the furniture, so anything
        /// building-sized is skipped and the next hit is tried; colliders often sit on a child with nothing to
        /// see, so we climb to the physics body, or to the first parent that has something to see.
        /// </summary>
        private static GameObject FindTarget(int player, out string reason)
        {
            reason = "Point the + at the thing, close up, then tap RB.";
            Camera cam = GameBindings.CameraFor(player);
            if (cam == null) { reason = "Couldn't find your camera."; return null; }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, Reach);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Type character = GameReflection.FindType("PlayerCharacter");

            // Looking past the building is only allowed a short way: the mansion's big collider sits a little in
            // front of its furniture (F3 probe: 0.7 m). Without this limit, aiming at a house from OUTSIDE copied
            // a treadmill from inside it (test 2026-11-22).
            const float LookPast = 1.5f;
            float buildingAt = -1f;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (character != null && hit.collider.GetComponentInParent(character) != null) continue; // no Wobblies
                if (hit.collider.GetComponentInParent<FloatBubble>() != null || hit.collider.GetComponentInParent<Follower>() != null) continue;
                if (buildingAt >= 0f && hit.distance - buildingAt > LookPast) break;

                GameObject root = PickRoot(hit.collider);
                if (root != null) return root;
                if (buildingAt < 0f)
                {
                    buildingAt = hit.distance;
                    reason = "Too big to copy: that's a whole building! Point the + at one thing.";
                }
            }
            if (buildingAt >= 0f) return null;

            // The exact middle found nothing (thin things like lamps are easy to miss): the nearest furniture-sized
            // thing within a narrow cone, as long as nothing is in the way of seeing it.
            Vector3 eye = cam.transform.position, look = cam.transform.forward;
            GameObject best = null;
            float bestAngle = 10f;
            foreach (Collider c in Physics.OverlapSphere(eye + look * 7f, 8f))
            {
                if (c == null || c.isTrigger) continue;
                if (character != null && c.GetComponentInParent(character) != null) continue;
                Vector3 to = c.bounds.center - eye;
                if (to.magnitude > 14f) continue;
                float angle = Vector3.Angle(look, to);
                if (angle >= bestAngle) continue;
                GameObject root = PickRoot(c);
                if (root == null) continue;
                RaycastHit seen;
                if (Physics.Raycast(eye, to.normalized, out seen, to.magnitude + 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    !seen.collider.transform.IsChildOf(root.transform) &&
                    !(character != null && seen.collider.GetComponentInParent(character) != null)) continue; // something is in the way
                best = root;
                bestAngle = angle;
            }
            return best;
        }

        private static GameObject PickRoot(Collider hit)
        {
            Transform start = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
            // Nothing to see here (a "Collider" child, or a group whose meshes are in a sibling): climb a little.
            for (int i = 0; i < 4 && start != null && !Sized(start.gameObject); i++) start = start.parent;
            if (start == null || !FurnitureSized(start.gameObject)) return null;
            if (hit.attachedRigidbody != null) return start.gameObject;

            // A static prop: climb only while the parent is still the same object (hardly any bigger).
            Bounds mine;
            MeshBounds(start.gameObject, false, out mine);
            for (Transform up = start.parent; up != null; up = up.parent)
            {
                Bounds theirs;
                if (!FurnitureSized(up.gameObject) || !MeshBounds(up.gameObject, false, out theirs)) break;
                if (theirs.size.magnitude > mine.size.magnitude * 1.25f + 0.2f) break;
                start = up;
                mine = theirs;
            }
            return start.gameObject;
        }

        private static bool Sized(GameObject go)
        {
            Bounds b;
            return MeshBounds(go, false, out b);
        }

        private static bool FurnitureSized(GameObject go)
        {
            if (go.GetComponentsInChildren<Renderer>().Length > MaxRenderers) return false;
            Bounds b;
            if (!MeshBounds(go, false, out b)) return false;
            return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) <= MaxSize;
        }

        /// <summary>
        /// One child per mesh plus the solid colliders. prefab = the source is a prefab asset (nothing in it is
        /// "active in the scene", and physics bounds don't exist), so everything is worked out from the meshes.
        /// </summary>
        private static GameObject BuildTemplate(GameObject source, bool prefab, out string reason)
        {
            reason = null;
            Bounds bounds;
            if (!MeshBounds(source, prefab, out bounds))
            {
                reason = "There's nothing to see there to copy.";
                return null;
            }

            // Origin at the bottom center, facing the way the source faces (flat), so it places nicely.
            var template = new GameObject("Copy of " + source.name);
            Vector3 flatForward = source.transform.forward; flatForward.y = 0f;
            template.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            template.transform.rotation = flatForward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flatForward.normalized, Vector3.up) : Quaternion.identity;

            // Only the detailed version of each mesh (the far-away LOD versions would overlap it).
            var lowDetail = new HashSet<Renderer>();
            foreach (LODGroup group in source.GetComponentsInChildren<LODGroup>(prefab))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (Renderer r in lods[i].renderers) if (r != null) lowDetail.Add(r);
                }
            }

            int meshes = 0, batched = 0;
            foreach (Renderer r in source.GetComponentsInChildren<Renderer>(prefab))
            {
                if (lowDetail.Contains(r) || !r.enabled || !Shown(r.transform, source.transform, prefab) || r is ParticleSystemRenderer) continue;
                Mesh mesh = MeshOf(r);
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
            foreach (Collider c in source.GetComponentsInChildren<Collider>(prefab))
            {
                if (c.isTrigger || !c.enabled || !Shown(c.transform, source.transform, prefab)) continue;
                try
                {
                    GameObject piece = Piece(template.transform, c.transform, "solid");
                    BoxCollider box = c as BoxCollider;
                    SphereCollider sphere = c as SphereCollider;
                    CapsuleCollider capsule = c as CapsuleCollider;
                    MeshCollider meshCollider = c as MeshCollider;
                    if (box != null) { var n = piece.AddComponent<BoxCollider>(); n.center = box.center; n.size = box.size; }
                    else if (sphere != null) { var n = piece.AddComponent<SphereCollider>(); n.center = sphere.center; n.radius = sphere.radius; }
                    else if (capsule != null)
                    {
                        var n = piece.AddComponent<CapsuleCollider>();
                        n.center = capsule.center; n.radius = capsule.radius; n.height = capsule.height; n.direction = capsule.direction;
                    }
                    else if (meshCollider != null && meshCollider.sharedMesh != null)
                    {
                        // The game's meshes are locked, so a mesh collider can't be rebuilt: a box around that mesh.
                        var n = piece.AddComponent<BoxCollider>();
                        n.center = meshCollider.sharedMesh.bounds.center;
                        n.size = meshCollider.sharedMesh.bounds.size;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(piece);
                        continue;
                    }
                    solids++;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Copy: skipped a collider on " + c.name + ": " + e.Message);
                }
            }
            if (solids == 0)
            {
                // Nothing solid came across: one box around the whole thing, so it isn't a ghost.
                var piece = new GameObject("solid");
                piece.transform.position = bounds.center;
                piece.transform.SetParent(template.transform, true);
                piece.AddComponent<BoxCollider>().size = bounds.size;
            }
            return template;
        }

        private static Mesh MeshOf(Renderer r)
        {
            SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
            if (skinned != null) return skinned.sharedMesh;
            MeshFilter mf = r is MeshRenderer ? r.GetComponent<MeshFilter>() : null;
            return mf != null ? mf.sharedMesh : null;
        }

        /// <summary>Switched on, counting only the objects between it and the thing being copied (works for prefabs too).</summary>
        private static bool Shown(Transform t, Transform root, bool prefab)
        {
            if (!prefab) return t.gameObject.activeInHierarchy;
            for (; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == root) break;
            }
            return true;
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

        /// <summary>
        /// The space an object's meshes take up, worked out from the meshes themselves (each mesh's own box, moved
        /// by its transform), so it is right for prefab assets too, where Renderer.bounds can't be trusted.
        /// </summary>
        private static bool MeshBounds(GameObject go, bool includeInactive, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(includeInactive))
            {
                if (r is ParticleSystemRenderer) continue;
                Mesh mesh = MeshOf(r);
                if (mesh == null) continue;
                Bounds local = mesh.bounds;
                Matrix4x4 m = r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 world = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; } else bounds.Encapsulate(world);
                }
            }
            return any;
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && path.Length < 200; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        /// <summary>"Prop_Couch_14" becomes "couch".</summary>
        private static string Nice(string name)
        {
            string n = Core.PropRooms.NiceName(name.Replace("(Clone)", "").Replace("Prop_", ""));
            return string.IsNullOrEmpty(n) || n == "thing" ? "thing" : n;
        }

        /// <summary>
        /// For grown-ups (F3): describes the things in the middle of player 1's view: each parent up the tree with
        /// its part count, size, and whether its meshes are glued into the scenery's combined mesh.
        /// </summary>
        public static string Probe(int player)
        {
            try
            {
                Camera cam = GameBindings.CameraFor(player);
                if (cam == null) return "Probe: no camera.";
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(ray, Reach);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                Type character = GameReflection.FindType("PlayerCharacter");
                string reason;
                GameObject would = FindTarget(player, out reason);
                var sb = new System.Text.StringBuilder("Probe (F3). Copy this would take: " + (would != null ? "'" + Path(would.transform) + "'" : "nothing (" + reason + ")") + ". Nearest first:");
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
                        int batched = 0, combined = 0;
                        foreach (Renderer r in rs)
                        {
                            if (r.isPartOfStaticBatch) batched++;
                            Mesh mesh = MeshOf(r);
                            if (mesh != null && mesh.name.StartsWith("Combined Mesh", StringComparison.Ordinal)) combined++;
                        }
                        Bounds b;
                        string size = MeshBounds(t.gameObject, false, out b) ? b.size.x.ToString("0.0") + "x" + b.size.y.ToString("0.0") + "x" + b.size.z.ToString("0.0") : "no size";
                        sb.Append("\n    up ").Append(level).Append(": '").Append(t.name).Append("' parts ").Append(rs.Length).Append(", size ").Append(size)
                          .Append(", static-batched ").Append(batched).Append(", combined-mesh ").Append(combined).Append(", children ").Append(t.childCount);
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
    }
}
