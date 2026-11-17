using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Saves everything placed with the house menu (furniture, tanks, lamps, catfish...) with its spot, facing,
    /// size and color, and puts it all back later. Things the menu places otherwise vanish when the game closes.
    /// Files: BepInEx\config\WobblyHouse\house{slot}.txt (format: Core/src/HouseLayout.cs).
    /// </summary>
    internal static class HouseSave
    {
        public const int Slots = 3;

        public static string SlotPath(int slot)
        {
            return Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, "WobblyHouse"), "house" + slot + ".txt");
        }

        public static bool SlotExists(int slot)
        {
            try
            {
                return File.Exists(SlotPath(slot));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Save(int slot)
        {
            HouseLayout layout = GameBindings.Snapshot();
            if (layout.Items.Count == 0)
            {
                return "Nothing placed yet to save.";
            }
            try
            {
                string path = SlotPath(slot);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, layout.Save());
                Plugin.Log.LogInfo("House saved to " + path + " (" + layout.Items.Count + " things).");
                return "Saved house " + slot + " (" + layout.Items.Count + " things).";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Saving house " + slot + " failed: " + e);
                return "Couldn't save the house.";
            }
        }

        /// <summary>
        /// Removes what the menu placed, then places everything from the slot. Networked props arrive a moment
        /// later, so the message counts what was started; the log says what didn't come back.
        /// </summary>
        public static string Load(int slot, int playerIndex, UndoStack undo)
        {
            string text;
            try
            {
                string path = SlotPath(slot);
                if (!File.Exists(path)) return "House " + slot + " is empty.";
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Loading house " + slot + " failed: " + e);
                return "Couldn't read house " + slot + ".";
            }

            var warnings = new List<string>();
            string error;
            HouseLayout layout = HouseLayout.Load(text, warnings, out error);
            if (layout == null)
            {
                Plugin.Log.LogWarning("House " + slot + ": " + error);
                return "House " + slot + " couldn't be read.";
            }
            foreach (string w in warnings) Plugin.Log.LogWarning("House " + slot + ": " + w);

            // Start fresh: remove what's placed now. This can't be undone, so the menu asks first.
            foreach (GameObject go in GameBindings.AllPlaced())
            {
                ObjectEdits.Remove(go);
            }
            undo.Clear();

            int started = 0, unknown = 0;
            foreach (HouseItem saved in layout.Items)
            {
                CatalogItem item = FurnitureCatalog.Find(saved.Id);
                if (item == null)
                {
                    unknown++;
                    Plugin.Log.LogWarning("House " + slot + ": no catalog item '" + saved.Id + "' any more; skipped.");
                    continue;
                }

                HouseItem s = saved;
                Vector3 position = new Vector3(s.X, s.Y, s.Z);
                Quaternion rotation = Quaternion.Euler(0f, s.Yaw, 0f);
                GameBindings.SpawnAt(item, playerIndex, position, rotation, (placed, problem) =>
                {
                    if (placed == null)
                    {
                        Plugin.Log.LogWarning("House " + slot + ": couldn't put back " + s.Id + ": " + problem);
                        return;
                    }
                    if (!Mathf.Approximately(s.Scale, 1f))
                    {
                        placed.transform.localScale *= s.Scale;
                        GameBindings.NoteScale(placed, s.Scale);
                    }
                    if (s.HasColor)
                    {
                        ObjectEdits.ApplyColor(placed, new Color(s.R, s.G, s.B));
                    }
                });
                started++;
            }

            Plugin.Log.LogInfo("House " + slot + ": putting back " + started + " things (" + unknown + " unknown).");
            return "Putting back house " + slot + " (" + started + " things).";
        }
    }
}
