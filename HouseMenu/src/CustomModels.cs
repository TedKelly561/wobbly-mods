using System.IO;
using UnityEngine;
using WobblyMods.Visuals;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Our own models (not the game's), packed into AssetBundles by the Unity kit in UnityAssets/
    /// ("Wobbly Mods > Build Bundles"). Bundle "wobbly_animals" holds RedtailCatfish (from the cartoon koi,
    /// recolored) and, later, BlackCat. If the bundle isn't installed yet, the menu says so and nothing breaks.
    /// </summary>
    internal static class CustomModels
    {
        public const string AnimalsBundle = "wobbly_animals";

        /// <summary>Points ModelLibrary at the bundles folder next to our DLLs (or plugins\WobblyMods\bundles).</summary>
        public static void Init()
        {
            string nextToDll = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? "", "bundles");
            string inWobblyMods = Path.Combine(Path.Combine(BepInEx.Paths.PluginPath, "WobblyMods"), "bundles");
            ModelLibrary.BundleFolder = Directory.Exists(nextToDll) ? nextToDll
                                      : Directory.Exists(inWobblyMods) ? inWobblyMods
                                      : nextToDll;
            Plugin.Log.LogInfo("Model bundles folder: " + ModelLibrary.BundleFolder +
                               (Directory.Exists(ModelLibrary.BundleFolder) ? "" : " (not there yet)"));
        }

        /// <summary>The redtail's catalog id, and how far it floats above the floor point it is placed on (meters).</summary>
        public const string RedtailId = "redtail";
        public const float RedtailLift = 0.6f;

        /// <summary>Skylar's boat-sized redtail catfish prop (about 4 m long), plays its own swim animation.</summary>
        public static GameObject Redtail(Vector3 position, Quaternion rotation)
        {
            return Spawn("RedtailCatfish", position, rotation, 4f, RedtailLift);
        }

        private static GameObject Spawn(string asset, Vector3 position, Quaternion rotation, float length, float lift)
        {
            string problem;
            GameObject prefab = ModelLibrary.Load(AnimalsBundle, asset, out problem);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, position, rotation);
                Reskin.MakeVisualOnly(go);
            }
            else if (asset == "RedtailCatfish")
            {
                // No Unity-kit bundle: use the redtail made in code (Core RedtailModel).
                go = ShapeModels.Redtail();
                go.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                Plugin.Log.LogWarning(asset + ": " + problem + " Build it with the Unity kit (UnityAssets/README.md).");
                return null;
            }
            go.name = asset;

            // Size by its longest side, then lift it a little off the floor so it "floats".
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            float longest = any ? Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) : 0f;
            if (longest > 0.001f) go.transform.localScale *= length / longest;
            go.transform.position += Vector3.up * lift;

            // A collider so players can bump into it and climb on (Skylar wants to ride it).
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(length * 0.25f, length * 0.25f, length) / Mathf.Max(go.transform.localScale.x, 0.0001f);

            // The model hangs inside a holder that sits exactly on the floor point, facing as asked. The holder is
            // what the menu tracks, saves and copies. (The lifted model used to be returned itself, so its saved
            // height already had the lift in it and every load added the lift again: it rose 0.6 m each time.)
            // The model is at the very same spot, size and facing as before.
            var holder = new GameObject(asset);
            holder.transform.SetPositionAndRotation(position, rotation);
            go.transform.SetParent(holder.transform, true);
            return holder;
        }
    }
}
