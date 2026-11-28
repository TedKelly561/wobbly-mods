using System;
using System.IO;

namespace WobblyMods.Core
{
    /// <summary>
    /// Careful file writing for saves (Skylar's houses). Pure C#, no Unity, so it is unit-tested.
    /// - The new text is written to a temporary file first and only then moved into place, so a crash or a
    ///   power cut in the middle of a save can't leave half a file where the old save was.
    /// - Before an existing save is replaced, it is kept as a backup: house1.bak1 is the newest, then .bak2,
    ///   .bak3... A grown-up can put one back by renaming it to house1.txt.
    /// </summary>
    public static class SaveFiles
    {
        /// <summary>The backup file for a save: generation 1 is the newest (house1.txt -> house1.bak1).</summary>
        public static string BackupPath(string path, int generation)
        {
            return Path.ChangeExtension(path, ".bak" + generation);
        }

        /// <summary>
        /// Saves text to path. If a file is already there, it becomes backup 1, the old backup 1 becomes
        /// backup 2, and so on; the oldest (number = generations) is dropped. Throws if the folder can't be written.
        /// </summary>
        public static void WriteWithBackups(string path, string text, int generations)
        {
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // The new save first, complete, beside the old one. Nothing old is touched until this has worked.
            string temp = path + ".tmp";
            File.WriteAllText(temp, text);

            if (File.Exists(path))
            {
                // Oldest first: .bak2 -> .bak3, .bak1 -> .bak2, then a copy of the save itself -> .bak1.
                for (int g = generations; g >= 2; g--)
                {
                    string from = BackupPath(path, g - 1);
                    if (!File.Exists(from)) continue;
                    string to = BackupPath(path, g);
                    if (File.Exists(to)) File.Delete(to);
                    File.Move(from, to);
                }
                if (generations >= 1)
                {
                    File.Copy(path, BackupPath(path, 1), true);
                }
                File.Delete(path);
            }
            File.Move(temp, path);
        }

        /// <summary>
        /// Which of two copies of a save to load: the one that exists, or the more recently written one when
        /// both do (the first wins a tie). Either path may be null. Returns null when neither file exists.
        /// </summary>
        public static string Newer(string first, string second)
        {
            bool hasFirst = !string.IsNullOrEmpty(first) && File.Exists(first);
            bool hasSecond = !string.IsNullOrEmpty(second) && File.Exists(second);
            if (hasFirst && hasSecond)
            {
                if (string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase))
                {
                    return first;
                }
                return File.GetLastWriteTimeUtc(second) > File.GetLastWriteTimeUtc(first) ? second : first;
            }
            return hasFirst ? first : hasSecond ? second : null;
        }
    }
}
