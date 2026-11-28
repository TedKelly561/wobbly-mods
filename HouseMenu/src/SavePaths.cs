using System;
using System.IO;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Where Skylar's saved houses and builds live. Default: BepInEx\config (as before). The config entry
    /// [Saves] SaveFolder can point both PCs (the office laptop and REMOTEWS2 in the living room) at the same
    /// cloud-synced folder, e.g. %OneDrive%\WobblyLifeSaves, so her houses and builds follow her (parent's request).
    /// Environment variables like %OneDrive% are expanded.
    ///
    /// The shared folder is checked again on EVERY save and load, not just once when the game starts: a cloud
    /// drive letter may not be there yet at launch and appear a minute later (or the other way round). While it
    /// can't be reached, saves go to BepInEx\config on this computer, so her work is never lost, and the log says so.
    /// </summary>
    internal static class SavePaths
    {
        private static string _shared;          // the configured folder (expanded), or null = none configured
        private static bool _sharedWasOk = true; // last answer, so the log only speaks when it changes

        public static void Init(string configured)
        {
            _shared = null;
            _sharedWasOk = true;
            if (string.IsNullOrEmpty(configured) || configured.Trim().Length == 0)
            {
                Plugin.Log.LogInfo("Saves folder: " + LocalRoot + " (default).");
                return;
            }

            try
            {
                _shared = Environment.ExpandEnvironmentVariables(configured.Trim());
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Saves folder '" + configured + "' can't be used (" + e.Message + "); using " + LocalRoot + ".");
                return;
            }
            if (SharedReachable())
            {
                Plugin.Log.LogInfo("Saves folder: " + _shared + " (from config).");
            }
        }

        /// <summary>This computer's own saves folder (BepInEx\config). Always there.</summary>
        public static string LocalRoot
        {
            get { return BepInEx.Paths.ConfigPath; }
        }

        /// <summary>True if [Saves] SaveFolder names a shared folder (whether or not it can be reached right now).</summary>
        public static bool HasShared
        {
            get { return _shared != null; }
        }

        /// <summary>Checks, right now, whether the shared folder can be used. Logs only when the answer changes.</summary>
        public static bool SharedReachable()
        {
            if (_shared == null) return false;
            bool ok;
            string why = null;
            try
            {
                Directory.CreateDirectory(_shared);
                ok = true;
            }
            catch (Exception e)
            {
                ok = false;
                why = e.Message;
            }

            if (ok != _sharedWasOk)
            {
                if (ok) Plugin.Log.LogInfo("Saves folder: " + _shared + " can be reached again.");
                else Plugin.Log.LogWarning("Saves folder '" + _shared + "' can't be reached (" + why + "). Until it can, saves go to " +
                                           LocalRoot + " on this computer.");
            }
            _sharedWasOk = ok;
            return ok;
        }

        /// <summary>
        /// The folder for one kind of save, e.g. "WobblyHouse" or "WobblyBuilds": in the shared folder if it can be
        /// reached right now, otherwise on this computer.
        /// </summary>
        public static string Folder(string kind)
        {
            return Path.Combine(SharedReachable() ? _shared : LocalRoot, kind);
        }

        /// <summary>The folder for one kind of save in the shared folder, or null if none is set or it can't be reached right now.</summary>
        public static string SharedFolder(string kind)
        {
            return SharedReachable() ? Path.Combine(_shared, kind) : null;
        }

        /// <summary>The folder for one kind of save on this computer (the fallback, and the default when no shared folder is set).</summary>
        public static string LocalFolder(string kind)
        {
            return Path.Combine(LocalRoot, kind);
        }
    }
}
