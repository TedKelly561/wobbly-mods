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
    /// Files: <saves folder>\WobblyHouse\house{slot}.txt (see SavePaths) (format: Core/src/HouseLayout.cs).
    ///
    /// Safe saving (Core SaveFiles): the new file is written beside the old one and then swapped in, and the
    /// save it replaces is kept as house{slot}.bak1 (newest), .bak2 and .bak3. To get an older house back, a
    /// grown-up renames one of those to house{slot}.txt.
    ///
    /// Shared folder not there (a cloud drive that isn't connected): the house is saved on this computer instead
    /// (BepInEx\config\WobblyHouse) and the message says so. Loading looks in both places and takes the newer file.
    /// </summary>
    internal static class HouseSave
    {
        public const int Slots = 3;
        private const int Backups = 3;
        private const string Kind = "WobblyHouse";

        // A comment line added to every file saved since things are placed exactly as given. Files WITHOUT it were saved when model-pack
        // furniture carried a hidden half turn and the redtail a hidden lift (see Load); they are corrected once
        // while loading. HouseLayout ignores lines starting with #.
        private const string PlacedAsGivenMark = "# placed-as-given";

        private static string FileName(int slot)
        {
            return "house" + slot + ".txt";
        }

        /// <summary>The slot's file in the shared folder, or null if no shared folder is set or it can't be reached now.</summary>
        private static string SharedPath(int slot)
        {
            string folder = SavePaths.SharedFolder(Kind);
            return folder == null ? null : Path.Combine(folder, FileName(slot));
        }

        /// <summary>The slot's file on this computer.</summary>
        private static string LocalPath(int slot)
        {
            return Path.Combine(SavePaths.LocalFolder(Kind), FileName(slot));
        }

        /// <summary>Where a save would go right now (the shared folder if it can be reached, else this computer).</summary>
        public static string SlotPath(int slot)
        {
            return SharedPath(slot) ?? LocalPath(slot);
        }

        public static bool SlotExists(int slot)
        {
            try
            {
                return SaveFiles.Newer(SharedPath(slot), LocalPath(slot)) != null;
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
            string text = layout.Save() + PlacedAsGivenMark + "\n";
            string things = " (" + layout.Items.Count + " things)";

            // The shared folder first, checked right now (not just when the game started).
            string shared = SharedPath(slot);
            if (shared != null)
            {
                try
                {
                    SaveFiles.WriteWithBackups(shared, text, Backups);
                    Plugin.Log.LogInfo("House saved to " + shared + things + ".");
                    return "Saved house " + slot + things + ".";
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Saving house " + slot + " to " + shared + " failed; saving on this computer instead: " + e);
                }
            }

            // No shared folder, or it can't be used: save on this computer, so her work is never lost.
            try
            {
                string local = LocalPath(slot);
                SaveFiles.WriteWithBackups(local, text, Backups);
                if (SavePaths.HasShared)
                {
                    Plugin.Log.LogWarning("House saved ON THIS COMPUTER ONLY, to " + local + things +
                                          ": the shared saves folder can't be reached. It will be loaded from here (the newer file wins).");
                    return "Saved house " + slot + " on this computer only" + things + ". The shared folder isn't connected.";
                }
                Plugin.Log.LogInfo("House saved to " + local + things + ".");
                return "Saved house " + slot + things + ".";
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
        /// Nothing standing is removed unless the file was read and has at least one thing this computer can build.
        /// </summary>
        public static string Load(int slot, int playerIndex, UndoStack undo)
        {
            string text;
            bool fromThisComputer;
            try
            {
                // Both places, the newer file wins (a save made while the shared folder was away is on this computer).
                string shared = SharedPath(slot);
                string local = LocalPath(slot);
                string path = SaveFiles.Newer(shared, local);
                if (path == null)
                {
                    return SavePaths.HasShared && shared == null
                        ? "House " + slot + " isn't on this computer, and the shared folder isn't connected."
                        : "House " + slot + " is empty.";
                }
                fromThisComputer = SavePaths.HasShared && path == local;
                text = File.ReadAllText(path);
                Plugin.Log.LogInfo("House " + slot + ": reading " + path +
                                   (fromThisComputer ? (shared == null ? " (the shared folder can't be reached)" : " (newer than the one in the shared folder)") : "") + ".");
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

            // Older files: model-pack furniture was saved with a hidden half turn, and the redtail with its lift
            // already added, so both came back wrong on every load. They are now placed exactly as saved; take the
            // old extras out once here so old houses come back the way they looked.
            bool oldFile = text.IndexOf(PlacedAsGivenMark, StringComparison.Ordinal) < 0;

            // Work out what can be built BEFORE touching what's standing.
            var known = new List<KeyValuePair<CatalogItem, HouseItem>>();
            int unknown = 0;
            foreach (HouseItem saved in layout.Items)
            {
                CatalogItem item = FurnitureCatalog.Find(saved.Id);
                if (item == null)
                {
                    unknown++;
                    Plugin.Log.LogWarning("House " + slot + ": no catalog item '" + saved.Id + "' any more; skipped.");
                    continue;
                }
                HouseItem s = saved.Copy();
                if (oldFile)
                {
                    if (s.Id.StartsWith(ModelPack.IdPrefix, StringComparison.Ordinal)) s.Yaw -= 180f;
                    else if (s.Id == CustomModels.RedtailId) s.Y -= CustomModels.RedtailLift * s.Scale; // the lift now grows with Bigger/Smaller
                }
                known.Add(new KeyValuePair<CatalogItem, HouseItem>(item, s));
            }
            if (known.Count == 0)
            {
                Plugin.Log.LogWarning("House " + slot + ": nothing in the file can be built here (" + layout.Items.Count +
                                      " lines, " + unknown + " unknown); what's standing was left alone.");
                return layout.Items.Count == 0 ? "House " + slot + " is empty." : "House " + slot + " has nothing this computer can build.";
            }

            // Start fresh: remove what's placed now. This can't be undone, so the menu asks first.
            foreach (GameObject go in GameBindings.AllPlaced())
            {
                try
                {
                    ObjectEdits.Remove(go);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("House " + slot + ": couldn't remove something that was standing: " + e.Message);
                }
            }
            undo.Clear();

            int started = 0, failed = 0;
            foreach (KeyValuePair<CatalogItem, HouseItem> pair in known)
            {
                CatalogItem item = pair.Key;
                HouseItem s = pair.Value;
                // Each thing on its own: one that goes wrong must not stop the rest of the house coming back.
                try
                {
                    Vector3 position = new Vector3(s.X, s.Y, s.Z);
                    Quaternion rotation = Quaternion.Euler(0f, s.Yaw, 0f);
                    GameBindings.SpawnAt(item, playerIndex, position, rotation, (placed, problem) =>
                    {
                        if (placed == null)
                        {
                            Plugin.Log.LogWarning("House " + slot + ": couldn't put back " + s.Id + ": " + problem);
                            return;
                        }
                        try
                        {
                            if (!Mathf.Approximately(s.Scale, 1f))
                            {
                                placed.transform.localScale *= s.Scale;
                                GameBindings.NoteScale(placed, s.Scale);
                            }
                            if (s.HasColor)
                            {
                                ObjectEdits.ApplyColor(placed, new Color(s.R, s.G, s.B));
                            }
                        }
                        catch (Exception e)
                        {
                            Plugin.Log.LogWarning("House " + slot + ": " + s.Id + " is back, but its size or color couldn't be set: " + e.Message);
                        }
                    });
                    started++;
                }
                catch (Exception e)
                {
                    failed++;
                    Plugin.Log.LogError("House " + slot + ": putting back " + s.Id + " failed: " + e);
                }
            }

            Plugin.Log.LogInfo("House " + slot + ": putting back " + started + " things (" + unknown + " unknown, " + failed + " failed).");
            return "Putting back house " + slot + " (" + started + " things)" + (fromThisComputer ? " from this computer." : ".");
        }
    }
}
