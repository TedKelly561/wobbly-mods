using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// The only file that talks to Wobbly Life's own code. Everything else in the mod is generic.
    ///
    /// Each TODO(decompile) needs the real class and method names from the game's
    /// Assembly-CSharp.dll, found with a decompiler and Cinematic Unity Explorer.
    /// Until a TODO is filled in, the menu says the feature isn't connected yet.
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

        /// <summary>True once Spawn is connected to the game's own spawning code.</summary>
        public static bool SpawnReady
        {
            get { return false; } // TODO(decompile): set to true once Spawn is implemented
        }

        /// <summary>Creates a copy of the catalog item in front of that player. Returns null if not possible.</summary>
        public static GameObject Spawn(CatalogItem item, int playerIndex)
        {
            if (!SpawnReady || item.PrefabName == null)
            {
                return null;
            }

            // TODO(decompile): find the game's prefab for item.PrefabName, instantiate it
            // at the spot CameraFor(playerIndex) is looking, then register it below.
            GameObject placed = null;
            if (placed != null)
            {
                Spawned.Add(new Placed { Item = placed, Owner = playerIndex });
            }
            return placed;
        }

        /// <summary>
        /// That player's camera.
        /// TODO(T14): in split screen each player has their own camera. Find how the game
        /// tracks local players and return player N's camera. Camera.main is only right for one player.
        /// </summary>
        public static Camera CameraFor(int playerIndex)
        {
            return Camera.main;
        }

        /// <summary>
        /// The part of the screen that belongs to that player, in IMGUI coordinates (top-left origin).
        /// TODO(T14): use the game's split-screen layout (for example the player's camera pixelRect,
        /// flipped to top-left origin). Until then: 1 player = whole screen, 2 = left/right halves,
        /// 3 or 4 = quarters.
        /// </summary>
        public static Rect ScreenAreaFor(int playerIndex, int playerCount)
        {
            float w = Screen.width;
            float h = Screen.height;
            if (playerCount <= 1)
            {
                return new Rect(0, 0, w, h);
            }
            if (playerCount == 2)
            {
                return new Rect(playerIndex == 0 ? 0 : w / 2, 0, w / 2, h);
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

        public static void Forget(GameObject target)
        {
            Spawned.RemoveAll(p => p.Item == target);
        }
    }
}
