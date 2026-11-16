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
    /// </summary>
    internal static class GameBindings
    {
        private static readonly List<GameObject> Spawned = new List<GameObject>();

        /// <summary>The most recently placed item that still exists, or null.</summary>
        public static GameObject LastSpawned
        {
            get
            {
                for (int i = Spawned.Count - 1; i >= 0; i--)
                {
                    if (Spawned[i] != null)
                    {
                        return Spawned[i];
                    }
                }
                return null;
            }
        }

        /// <summary>True once Spawn is connected to the game's own spawning code.</summary>
        public static bool SpawnReady
        {
            get { return false; } // TODO(decompile): set to true once Spawn is implemented
        }

        /// <summary>Creates a copy of the catalog item in front of the player. Returns null if not possible.</summary>
        public static GameObject Spawn(CatalogItem item)
        {
            if (!SpawnReady || item.PrefabName == null)
            {
                return null;
            }

            // TODO(decompile): find the game's prefab for item.PrefabName, instantiate it
            // at the spot the camera is looking, then register it below.
            GameObject placed = null;
            if (placed != null)
            {
                Spawned.Add(placed);
            }
            return placed;
        }

        /// <summary>
        /// Returns the nearest editable object along the line from the screen center, or null.
        /// The camera is third-person, so the first thing hit is usually her own character.
        /// That's why this checks every hit instead of just the first one, and walks up from
        /// the part that was hit (a cushion, a leg) to the whole object that can be edited.
        /// </summary>
        public static GameObject FindEditableInView()
        {
            Camera cam = Camera.main;
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
            return Spawned.Contains(target);
        }

        public static void Forget(GameObject target)
        {
            Spawned.Remove(target);
        }
    }
}
