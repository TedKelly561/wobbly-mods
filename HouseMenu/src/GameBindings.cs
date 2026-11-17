using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// The only file that talks to Wobbly Life's own code. Everything else in the mod is generic.
    ///
    /// Game names come from Scout reports 1 and 2 (docs/research/), reached through reflection.
    ///
    /// Split screen: methods take a 0-based playerIndex (0 = player 1). TODO(T14) marks
    /// where we still need the game's own per-player camera and screen area.
    /// </summary>
    internal static class GameBindings
    {
        private sealed class Placed
        {
            public GameObject Item;
            public int Owner;
            public string CatalogId;     // for saving the house
            public float ScaleFactor = 1f; // Bigger/Smaller on top of the item's own size
            public bool HasColor;        // painted with "New color"
            public Color Color;
        }

        private static readonly List<Placed> Spawned = new List<Placed>();

        /// <summary>The most recent item this player placed that still exists, or null.</summary>
        public static GameObject LastSpawnedBy(int playerIndex)
        {
            for (int i = Spawned.Count - 1; i >= 0; i--)
            {
                if (Spawned[i].Owner == playerIndex && Spawned[i].Item != null)
                {
                    return Spawned[i].Item;
                }
            }
            return null;
        }

        /// <summary>
        /// Places a copy of the catalog item in front of that player, on the floor and facing them.
        /// The game spawns networked props a moment later, so the result arrives through done:
        /// done(placedObject, null) on success, or done(null, reason) on failure.
        /// </summary>
        public static void Spawn(CatalogItem item, int playerIndex, System.Action<GameObject, string> done)
        {
            if (string.IsNullOrEmpty(item.Address) && item.Build == null)
            {
                done(null, "no address for " + item.DisplayName);
                return;
            }

            Vector3 position;
            Quaternion rotation;
            if (!PlacementFor(playerIndex, item.Scale, item.Distance, out position, out rotation))
            {
                done(null, "couldn't find where player " + (playerIndex + 1) + " is");
                return;
            }
            SpawnAt(item, playerIndex, position, rotation, done);
        }

        /// <summary>Like Spawn, but at an exact spot (used when loading a saved house).</summary>
        public static void SpawnAt(CatalogItem item, int playerIndex, Vector3 position, Quaternion rotation,
                                   System.Action<GameObject, string> done)
        {

            System.Action<GameObject> finish = placed =>
            {
                if (placed == null)
                {
                    done(null, "the game couldn't place " + item.DisplayName);
                    return;
                }
                if (item.Scale != 1f)
                {
                    placed.transform.localScale = placed.transform.localScale * item.Scale;
                }
                Spawned.Add(new Placed { Item = placed, Owner = playerIndex, CatalogId = item.Id });
                done(placed, null);
            };

            string problem;
            if (item.Build != null)
            {
                GameObject built = null;
                try
                {
                    built = item.Build(position, rotation);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Building " + item.Id + " failed: " + e);
                }
                finish(built);
                return;
            }

            if (item.Local)
            {
                finish(GameSpawner.SpawnLocal(item.Address, position, rotation, out problem));
                if (problem != null) Plugin.Log.LogWarning("Spawn " + item.Id + ": " + problem);
                return;
            }

            if (!GameSpawner.TrySpawnNetworked(item.Address, position, rotation, finish, out problem))
            {
                Plugin.Log.LogWarning("Spawn " + item.Id + ": " + problem + ". Trying a local copy instead.");
                string localProblem;
                GameObject local = GameSpawner.SpawnLocal(item.Address, position, rotation, out localProblem);
                if (localProblem != null) Plugin.Log.LogWarning("Spawn " + item.Id + " (local): " + localProblem);
                finish(local);
            }
        }

        /// <summary>
        /// Where to put a new item for that player: a few meters in front of their character (bigger items
        /// further away), dropped onto the floor below, facing the player. The floor check starts just above
        /// the player's height, so it finds the floor inside a house rather than the roof.
        /// </summary>
        public static bool PlacementFor(int playerIndex, float scale, float distanceOverride, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            Camera cam = CameraFor(playerIndex);
            if (cam == null)
            {
                return false;
            }

            Vector3 origin = CharacterPosition(playerIndex) ?? cam.transform.position;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            float distance = distanceOverride > 0f ? distanceOverride : 2.5f + 1.5f * Mathf.Max(1f, scale);
            Vector3 spot = origin + forward * distance;

            RaycastHit hit;
            Vector3 rayStart = new Vector3(spot.x, origin.y + 1.5f, spot.z);
            position = Physics.Raycast(rayStart, Vector3.down, out hit, 10f) ? hit.point + Vector3.up * 0.05f : spot;
            rotation = Quaternion.LookRotation(-forward, Vector3.up);
            return true;
        }

        /// <summary>The player's character position (PlayerCharacter.GetPlayerPosition, from research), or null.</summary>
        internal static Vector3? CharacterPosition(int playerIndex)
        {
            List<object> locals = GameReflection.LocalPlayerControllers();
            if (playerIndex < 0 || playerIndex >= locals.Count) return null;

            object character = GameReflection.Call(locals[playerIndex], "GetPlayerCharacter")
                               ?? GameReflection.Get(locals[playerIndex], "playerCharacter");
            object pos = GameReflection.Call(character, "GetPlayerPosition");
            if (pos is Vector3) return (Vector3)pos;

            Component c = character as Component;
            return c != null ? (Vector3?)c.transform.position : null;
        }

        /// <summary>
        /// That player's camera. Uses the game's own local players (GameInstance, confirmed in research),
        /// and falls back to Camera.main if they can't be read.
        /// TODO(T14): confirm with the Scout report that player order matches controller order.
        /// </summary>
        public static Camera CameraFor(int playerIndex)
        {
            List<object> locals = GameReflection.LocalPlayerControllers();
            if (playerIndex >= 0 && playerIndex < locals.Count)
            {
                Camera cam = GameReflection.CameraOf(locals[playerIndex]);
                if (cam != null)
                {
                    return cam;
                }
            }
            return Camera.main;
        }

        /// <summary>
        /// The part of the screen that belongs to that player, in IMGUI coordinates (top-left origin).
        /// TODO(T14): use the game's split-screen layout (for example the player's camera pixelRect,
        /// flipped to top-left origin). Until then: 1 player = whole screen, 2 = top/bottom halves,
        /// 3 or 4 = quarters.
        /// </summary>
        public static Rect ScreenAreaFor(int playerIndex, int playerCount)
        {
            // Best source: the player's own camera area. Camera pixelRect starts bottom-left,
            // IMGUI starts top-left, so flip y.
            List<object> locals = GameReflection.LocalPlayerControllers();
            if (playerIndex >= 0 && playerIndex < locals.Count)
            {
                Camera cam = GameReflection.CameraOf(locals[playerIndex]);
                if (cam != null)
                {
                    Rect p = cam.pixelRect;
                    return new Rect(p.x, Screen.height - p.y - p.height, p.width, p.height);
                }
            }

            float w = Screen.width;
            float h = Screen.height;
            if (playerCount <= 1)
            {
                return new Rect(0, 0, w, h);
            }
            if (playerCount == 2)
            {
                // The game stacks two players top and bottom (Scout report 2).
                return new Rect(0, playerIndex == 0 ? 0 : h / 2, w, h / 2);
            }
            float x = (playerIndex % 2) * (w / 2);
            float y = (playerIndex / 2) * (h / 2);
            return new Rect(x, y, w / 2, h / 2);
        }

        /// <summary>
        /// Returns the nearest editable object along the line from the center of that player's view, or null.
        /// The camera is third-person, so the first thing hit is usually the player's own character.
        /// That's why this checks every hit instead of just the first one, and walks up from
        /// the part that was hit (a cushion, a leg) to the whole object that can be edited.
        /// </summary>
        public static GameObject FindEditableInView(int playerIndex)
        {
            Camera cam = CameraFor(playerIndex);
            if (cam == null)
            {
                return null;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                for (Transform t = hit.transform; t != null; t = t.parent)
                {
                    if (IsEditable(t.gameObject))
                    {
                        return t.gameObject;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Only objects the menu is allowed to change. Spawned items are always allowed.
        /// TODO(decompile): allow the house's own furniture once it is identified, and never walls, floors or the player.
        /// </summary>
        public static bool IsEditable(GameObject target)
        {
            foreach (Placed p in Spawned)
            {
                if (p.Item == target)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Remembers a size change on a placed item (for saving the house).</summary>
        public static void NoteScale(GameObject target, float factor)
        {
            Placed p = Find(target);
            if (p != null) p.ScaleFactor = factor;
        }

        public static float ScaleOf(GameObject target)
        {
            Placed p = Find(target);
            return p != null ? p.ScaleFactor : 1f;
        }

        /// <summary>Remembers a paint color on a placed item (hasColor false = back to its own colors).</summary>
        public static void NoteColor(GameObject target, bool hasColor, Color color)
        {
            Placed p = Find(target);
            if (p == null) return;
            p.HasColor = hasColor;
            p.Color = color;
        }

        public static bool ColorOf(GameObject target, out Color color)
        {
            Placed p = Find(target);
            color = p != null ? p.Color : Color.white;
            return p != null && p.HasColor;
        }

        /// <summary>Everything placed with the menu that still exists, for "Save house".</summary>
        public static Core.HouseLayout Snapshot()
        {
            var layout = new Core.HouseLayout();
            foreach (Placed p in Spawned)
            {
                if (p.Item == null || string.IsNullOrEmpty(p.CatalogId)) continue;
                Transform t = p.Item.transform;
                layout.Items.Add(new Core.HouseItem
                {
                    Id = p.CatalogId,
                    X = t.position.x, Y = t.position.y, Z = t.position.z,
                    Yaw = t.eulerAngles.y,
                    Scale = p.ScaleFactor,
                    HasColor = p.HasColor, R = p.Color.r, G = p.Color.g, B = p.Color.b
                });
            }
            return layout;
        }

        /// <summary>Every placed item that still exists (for clearing before a load).</summary>
        public static List<GameObject> AllPlaced()
        {
            var list = new List<GameObject>();
            foreach (Placed p in Spawned)
            {
                if (p.Item != null) list.Add(p.Item);
            }
            return list;
        }

        private static Placed Find(GameObject target)
        {
            foreach (Placed p in Spawned)
            {
                if (p.Item == target) return p;
            }
            return null;
        }

        public static void Forget(GameObject target)
        {
            Spawned.RemoveAll(p => p.Item == target);
        }
    }
}
