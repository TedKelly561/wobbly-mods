using System;
using System.Collections.Generic;
using System.IO;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Parent's rule (2026-11-21): "If it's in the menu, it's got to be buildable." Any item that fails to appear is
    /// written to BepInEx\config\WobblyMods.hidden-items-v2.txt (v2: the first list was made when
    /// catalog props still used the network spawner; they are placed a new way now, so they get another chance) and left out of the menu from the next start. A grown-up
    /// can delete a line (or the file) to give an item another chance. One id per line; text after a tab is the reason.
    /// </summary>
    internal static class HiddenItems
    {
        private static readonly HashSet<string> Ids = new HashSet<string>(StringComparer.Ordinal);

        private static string FilePath
        {
            get { return Path.Combine(BepInEx.Paths.ConfigPath, "WobblyMods.hidden-items-v2.txt"); }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string id = line.Split('\t')[0].Trim();
                    if (id.Length > 0 && id[0] != '#') Ids.Add(id);
                }
                if (Ids.Count > 0)
                    Plugin.Log.LogInfo("Menu: " + Ids.Count + " items are hidden because they failed before (" + FilePath + "): " +
                                       string.Join(", ", new List<string>(Ids).ToArray()));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Menu: couldn't read the hidden-items list: " + e.Message);
            }
        }

        public static bool IsHidden(string id)
        {
            return id != null && Ids.Contains(id);
        }

        public static void Hide(string id, string reason)
        {
            if (string.IsNullOrEmpty(id) || !Ids.Add(id)) return;
            try
            {
                File.AppendAllText(FilePath, id + "\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " " + (reason ?? "") + Environment.NewLine);
                Plugin.Log.LogWarning("Menu: " + id + " failed and will be hidden from the menu from the next start.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Menu: couldn't save the hidden-items list: " + e.Message);
            }
        }
    }
}
