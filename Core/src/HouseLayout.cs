using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WobblyMods.Core
{
    /// <summary>One thing placed in the house: which catalog item, where, which way, how big, what color.</summary>
    public sealed class HouseItem
    {
        public string Id;          // catalog id, e.g. "bed", "fishtank"
        public float X, Y, Z;      // world position (metres)
        public float Yaw;          // facing, degrees around up
        public float Scale = 1f;   // size multiplier on top of the item's own size
        public bool HasColor;      // false = the item's own colors
        public float R, G, B;      // paint color (0-1), used when HasColor

        public HouseItem Copy()
        {
            return (HouseItem)MemberwiseClone();
        }
    }

    /// <summary>
    /// Everything placed in the house, so it can be saved and put back next time (Skylar's tanks and furniture
    /// otherwise vanish when the game closes). Pure C#, no Unity, so it is unit-tested.
    ///
    /// Text format, one item per line, invariant culture:
    ///   wobbly-house 1
    ///   item &lt;id&gt; &lt;x&gt; &lt;y&gt; &lt;z&gt; &lt;yaw&gt; &lt;scale&gt; [&lt;r&gt; &lt;g&gt; &lt;b&gt;]
    /// Blank lines and lines starting with # are ignored. Bad lines are skipped with a warning; only a missing or
    /// newer header fails the whole load.
    /// </summary>
    public sealed class HouseLayout
    {
        public const string Header = "wobbly-house";
        public const int FormatVersion = 1;

        public readonly List<HouseItem> Items = new List<HouseItem>();

        public string Save()
        {
            var sb = new StringBuilder();
            sb.Append(Header).Append(' ').Append(FormatVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (HouseItem it in Items)
            {
                if (string.IsNullOrEmpty(it.Id) || it.Id.IndexOf(' ') >= 0)
                {
                    continue; // ids are single words; anything else can't be read back
                }
                sb.Append("item ").Append(it.Id)
                  .Append(' ').Append(F(it.X)).Append(' ').Append(F(it.Y)).Append(' ').Append(F(it.Z))
                  .Append(' ').Append(F(it.Yaw)).Append(' ').Append(it.Scale.ToString("R", CultureInfo.InvariantCulture)); // full precision: tiny scales must not save as 0
                if (it.HasColor)
                {
                    sb.Append(' ').Append(F(it.R)).Append(' ').Append(F(it.G)).Append(' ').Append(F(it.B));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Reads a saved house. Returns null (with an error) only for a missing or newer header.</summary>
        public static HouseLayout Load(string text, List<string> warnings, out string error)
        {
            error = null;
            var layout = new HouseLayout();
            if (text == null)
            {
                error = "no text";
                return null;
            }

            var reader = new StringReader(text);
            string line;
            int lineNo = 0;
            bool sawHeader = false;
            while ((line = reader.ReadLine()) != null)
            {
                lineNo++;
                string t = line.Trim();
                if (t.Length == 0 || t[0] == '#')
                {
                    continue;
                }

                string[] parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (!sawHeader)
                {
                    int version;
                    if (parts.Length != 2 || parts[0] != Header ||
                        !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out version))
                    {
                        error = "not a house file (missing '" + Header + "' header)";
                        return null;
                    }
                    if (version > FormatVersion)
                    {
                        error = "house file is from a newer version (" + version + ")";
                        return null;
                    }
                    sawHeader = true;
                    continue;
                }

                HouseItem item;
                string why;
                if (TryParseItem(parts, out item, out why))
                {
                    layout.Items.Add(item);
                }
                else if (warnings != null)
                {
                    warnings.Add("line " + lineNo + ": " + why);
                }
            }

            if (!sawHeader)
            {
                error = "empty house file";
                return null;
            }
            return layout;
        }

        private static bool TryParseItem(string[] p, out HouseItem item, out string why)
        {
            item = null;
            why = null;
            if (p[0] != "item")
            {
                why = "unknown line '" + p[0] + "'";
                return false;
            }
            if (p.Length != 7 && p.Length != 10)
            {
                why = "expected 7 or 10 fields, got " + p.Length;
                return false;
            }

            var it = new HouseItem { Id = p[1] };
            float[] v = new float[p.Length - 2];
            for (int i = 2; i < p.Length; i++)
            {
                float f;
                if (!float.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out f) ||
                    float.IsNaN(f) || float.IsInfinity(f))
                {
                    why = "bad number '" + p[i] + "'";
                    return false;
                }
                v[i - 2] = f;
            }

            it.X = v[0]; it.Y = v[1]; it.Z = v[2]; it.Yaw = v[3]; it.Scale = v[4];
            if (it.Scale <= 0f || it.Scale > 100f)
            {
                why = "scale out of range";
                return false;
            }
            if (v.Length == 8)
            {
                it.HasColor = true;
                it.R = Clamp01(v[5]); it.G = Clamp01(v[6]); it.B = Clamp01(v[7]);
            }
            item = it;
            return true;
        }

        private static float Clamp01(float f)
        {
            return f < 0f ? 0f : f > 1f ? 1f : f;
        }

        private static string F(float f)
        {
            return f.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
