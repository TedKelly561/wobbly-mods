using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu.Icons
{
    /// <summary>
    /// Procedural symbol textures for menu items whose Icon key starts with "sym:".
    ///
    /// Key format: <c>sym:NAME</c>, optionally followed by <c>#RRGGBB</c> (a color, for paint drops and
    /// material cubes) and/or <c>!off</c> (adds a red "switched off" slash). Examples: "sym:undo",
    /// "sym:paint#FF8000", "sym:ghostblock!off". Names are listed in SymbolDrawings.
    ///
    /// Textures are made lazily the first time they're drawn, at most a few per frame so opening a tab full
    /// of new symbols never stalls the game; until then the tile shows its letter placeholder.
    /// </summary>
    internal static class SymbolIcons
    {
        public const string Prefix = "sym:";

        /// <summary>New symbols drawn per frame at most (each is about 16k pixels of simple math).</summary>
        private const int PerFrame = 4;

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> Unknown = new HashSet<string>();
        private static int _frame = -1;
        private static int _madeThisFrame;

        /// <summary>Builds a key: Key("paint", color) = "sym:paint#RRGGBB".</summary>
        public static string Key(string name, Color? color = null, bool off = false)
        {
            string key = Prefix + name;
            if (color.HasValue) key += "#" + TabTheme.Hex(color.Value);
            if (off) key += "!off";
            return key;
        }

        public static bool IsSymbol(string key)
        {
            return key != null && key.StartsWith(Prefix, System.StringComparison.Ordinal);
        }

        /// <summary>The texture for a "sym:" key, or null if it isn't made yet (or the name is unknown).</summary>
        public static Texture2D Get(string key)
        {
            if (!IsSymbol(key)) return null;

            Texture2D tex;
            if (Cache.TryGetValue(key, out tex)) return tex;
            if (Unknown.Contains(key)) return null;

            if (Time.frameCount != _frame)
            {
                _frame = Time.frameCount;
                _madeThisFrame = 0;
            }
            if (_madeThisFrame >= PerFrame) return null;
            _madeThisFrame++;

            try
            {
                tex = Make(key);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Symbol '" + key + "' failed: " + e.Message);
                tex = null;
            }
            if (tex == null)
            {
                Unknown.Add(key);
                return null;
            }
            Cache[key] = tex;
            return tex;
        }

        private static Texture2D Make(string key)
        {
            string name = key.Substring(Prefix.Length);
            bool off = false;
            if (name.EndsWith("!off", System.StringComparison.Ordinal))
            {
                off = true;
                name = name.Substring(0, name.Length - 4);
            }

            Color? tint = null;
            int hash = name.IndexOf('#');
            if (hash >= 0)
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(name.Substring(hash), out c)) tint = c;
                name = name.Substring(0, hash);
            }

            var canvas = new SymbolCanvas();
            if (!SymbolDrawings.Draw(canvas, name, tint))
            {
                Plugin.Log.LogWarning("Unknown menu symbol '" + key + "' (the tile shows a letter instead).");
                return null;
            }
            if (off) canvas.OffSlash();
            return canvas.ToTexture("WobblySymbol " + key);
        }
    }
}
