using System;
using System.IO;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// What the automatic updater last did on this PC, so the menu can tell the family when updates have stopped
    /// working (otherwise they keep playing an old version and nobody knows). The updater
    /// (tools\Update-WobblyMods.ps1) writes BepInEx\plugins\WobblyMods\update-status.txt after each run, next to
    /// installed-commit.txt. Two lines:
    ///   time=2026-11-28 16:37:24
    ///   result=up to date (38e9abb)      or  installed 38e9abb: ...   or  waiting: ...   or  FAILED: ...
    /// The file is read once, the first time it's asked for (the updater doesn't run while the game does).
    /// Nothing here ever throws: a missing or odd file just means "nothing to report".
    /// </summary>
    internal static class UpdateStatus
    {
        public const string FileName = "update-status.txt";

        private const int ShortLength = 160;

        private static bool _read;
        private static string _time;
        private static string _result;

        /// <summary>The status file, in the same folder the updater installs the mod into.</summary>
        public static string FilePath
        {
            get { return Path.Combine(Path.Combine(BepInEx.Paths.PluginPath, "WobblyMods"), FileName); }
        }

        /// <summary>
        /// Null when the last update went fine or there is no status file (the updater isn't set up here, or
        /// hasn't run since this was added). Otherwise one short plain sentence saying what went wrong.
        /// </summary>
        public static string Problem
        {
            get
            {
                try
                {
                    Read();
                    if (_result == null || !_result.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    string why = FirstSentence(_result.Substring("FAILED".Length).TrimStart(':', ' '));
                    string when = string.IsNullOrEmpty(_time) ? "" : " (" + _time + ")";
                    return why.Length == 0
                        ? "The mods could not update themselves" + when + "."
                        : "The mods could not update themselves" + when + ": " + why;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>One line about the last update, whatever it was. Never null.</summary>
        public static string Summary
        {
            get
            {
                try
                {
                    Read();
                    if (_result == null)
                    {
                        return "No update record on this PC yet.";
                    }
                    string when = string.IsNullOrEmpty(_time) ? "" : _time + ": ";
                    return "Last mod update, " + when + Shorten(_result);
                }
                catch (Exception)
                {
                    return "No update record on this PC yet.";
                }
            }
        }

        /// <summary>Reads the file again (for a "check again" button; normally not needed).</summary>
        public static void Reload()
        {
            _read = false;
            _time = null;
            _result = null;
        }

        private static void Read()
        {
            if (_read)
            {
                return;
            }
            _read = true;
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                {
                    return;
                }
                Parse(File.ReadAllText(path), out _time, out _result);
            }
            catch (Exception)
            {
                // Unreadable right now: nothing to report.
                _time = null;
                _result = null;
            }
        }

        /// <summary>Picks time= and result= out of the file's text. Either can come back null.</summary>
        internal static void Parse(string text, out string time, out string result)
        {
            time = null;
            result = null;
            if (text == null)
            {
                return;
            }
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    time = line.Substring(5).Trim();
                }
                else if (line.StartsWith("result=", StringComparison.OrdinalIgnoreCase))
                {
                    result = line.Substring(7).Trim();
                }
            }
            if (result != null && result.Length == 0)
            {
                result = null;
            }
        }

        /// <summary>The updater puts what went wrong in the first sentence and the details after it.</summary>
        private static string FirstSentence(string text)
        {
            int end = text.IndexOf(". ", StringComparison.Ordinal);
            if (end >= 0)
            {
                text = text.Substring(0, end + 1);
            }
            return Shorten(text);
        }

        private static string Shorten(string text)
        {
            return text.Length <= ShortLength ? text : text.Substring(0, ShortLength - 3) + "...";
        }
    }
}
