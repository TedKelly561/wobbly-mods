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

        /// <summary>Skylar's boat-sized redtail catfish prop (about 4 m long), plays its own swim animation.</summary>
        public static GameObject Redtail(Vector3 position, Quaternion rotation)
        {
            return Spawn("RedtailCatfish", position, rotation, 4f, 0.6f);
        }

        private static GameObject Spawn(string asset, Vector3 position, Quaternion rotation, float length, float lift)
        {
            string problem;
            GameObject prefab = ModelLibrary.Load(AnimalsBundle, asset, out problem);
            if (prefab == null)
            {
                Plugin.Log.LogWarning(asset + ": " + problem + " Build it with the Unity kit (UnityAssets/README.md).");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, position, rotation);
            go.name = asset;
            Reskin.MakeVisualOnly(go);

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
            return go;
        }
    }
}
