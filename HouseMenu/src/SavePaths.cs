using System;
using System.IO;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Where Skylar's saved houses and builds live. Default: BepInEx\config (as before). The config entry
    /// [Saves] SaveFolder can point both PCs (the office laptop and REMOTEWS2 in the living room) at the same
    /// cloud-synced folder, e.g. %OneDrive%\WobblyLifeSaves, so her houses and builds follow her (parent's request).
    /// Environment variables like %OneDrive% are expanded. If the folder can't be used, the default is used and logged.
    /// </summary>
    internal static class SavePaths
    {
        private static string _root;

        public static void Init(string configured)
        {
            _root = BepInEx.Paths.ConfigPath;
            if (string.IsNullOrEmpty(configured) || configured.Trim().Length == 0)
            {
                Plugin.Log.LogInfo("Saves folder: " + _root + " (default).");
                return;
            }

            try
            {
                string expanded = Environment.ExpandEnvironmentVariables(configured.Trim());
                Directory.CreateDirectory(expanded);
                _root = expanded;
                Plugin.Log.LogInfo("Saves folder: " + _root + " (from config).");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Saves folder '" + configured + "' can't be used (" + e.Message + "); using " + _root + ".");
            }
        }

        /// <summary>The folder for one kind of save, e.g. "WobblyHouse" or "WobblyBuilds".</summary>
        public static string Folder(string kind)
        {
            return Path.Combine(_root ?? BepInEx.Paths.ConfigPath, kind);
        }
    }
}
