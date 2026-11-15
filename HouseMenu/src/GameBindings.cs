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

        /// <summary>Returns the object at the center of the screen, or null.</summary>
        public static GameObject FindTargetInView()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return null;
            }

            RaycastHit hit;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out hit, 30f))
            {
                return hit.transform.gameObject;
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
