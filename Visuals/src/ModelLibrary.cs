using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WobblyMods.Visuals
{
    /// <summary>
    /// Loads our own models (cat, redtail catfish, building pieces) from Unity AssetBundles.
    /// Each bundle is loaded once and kept, so spawning many copies stays cheap.
    ///
    /// Bundles must be built with the same Unity version the game uses (Scout reports it).
    /// Put them in BepInEx\plugins\WobblyMods\bundles\.
    /// </summary>
    public static class ModelLibrary
    {
        private static readonly Dictionary<string, AssetBundle> Bundles = new Dictionary<string, AssetBundle>();

        /// <summary>Folder holding the bundle files. Set once at startup by the plugin.</summary>
        public static string BundleFolder { get; set; }

        /// <summary>Returns the model prefab, or null (with a reason) if the bundle or asset is missing.</summary>
        public static GameObject Load(string bundleName, string assetName, out string problem)
        {
            problem = null;
            AssetBundle bundle = GetBundle(bundleName, out problem);
            if (bundle == null)
            {
                return null;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
            if (prefab == null)
            {
                problem = "Model '" + assetName + "' is not in bundle '" + bundleName + "'.";
            }
            return prefab;
        }

        private static AssetBundle GetBundle(string bundleName, out string problem)
        {
            problem = null;
            AssetBundle bundle;
            if (Bundles.TryGetValue(bundleName, out bundle) && bundle != null)
            {
                return bundle;
            }

            if (string.IsNullOrEmpty(BundleFolder))
            {
                problem = "Bundle folder is not set.";
                return null;
            }

            string path = Path.Combine(BundleFolder, bundleName);
            if (!File.Exists(path))
            {
                problem = "Bundle file not found: " + path;
                return null;
            }

            bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                problem = "Bundle could not be loaded (was it built with the game's Unity version?): " + path;
                return null;
            }

            Bundles[bundleName] = bundle;
            return bundle;
        }
    }
}
