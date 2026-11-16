using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WobblyMods.BuildMode
{
    /// <summary>What came back from loading a build file.</summary>
    public sealed class LoadResult
    {
        internal LoadResult()
        {
            Warnings = new List<string>();
            Name = "";
        }

        /// <summary>False only if the file couldn't be read at all (see <see cref="Error"/>). Warnings still mean success.</summary>
        public bool Success
        {
            get { return Error == null; }
        }

        /// <summary>Why loading failed, or null.</summary>
        public string Error { get; internal set; }

        /// <summary>Lines that were skipped or odd, with line numbers. Show a short summary to the player.</summary>
        public List<string> Warnings { get; private set; }

        /// <summary>Blocks skipped (unknown ids, bad lines, overlaps).</summary>
        public int SkippedBlocks { get; internal set; }

        public int LoadedBlocks { get; internal set; }

        /// <summary>The file's format version.</summary>
        public int FormatVersion { get; internal set; }

        public string Name { get; internal set; }

        /// <summary>Grid the build was made on (cell size and world origin). Defaults if the file had none.</summary>
        public GridSettings Grid { get; internal set; }

        /// <summary>The blocks read, in file order. Already placed into the model for <see cref="BuildSerializer.Read"/>.</summary>
        public List<BlockSpec> Blocks { get; internal set; }

        /// <summary>The loaded build (null if <see cref="Success"/> is false).</summary>
        public BuildModel Model { get; internal set; }
    }

    /// <summary>
    /// Reads and writes builds in a small, versioned, human-readable text format:
    ///
    /// <code>
    /// wobblybuild 1
    /// # comments start with #
    /// name Skylar's castle
    /// cell 1 0 0 0                      (cell size, then grid origin x y z, in metres)
    /// block wall brick 0 0 0 1 #FFFFFFFF (type material x y z quarterTurns paint)
    /// end
    /// </code>
    ///
    /// Ids are the catalog's stable ids. Numbers are written with the invariant culture and floats
    /// with round-trip ("R") formatting, so save -> load -> save gives identical text.
    /// Unknown keywords, unknown ids, bad lines and overlaps are skipped with a warning; only a
    /// missing header or a newer format version stops loading.
    /// </summary>
    public static class BuildSerializer
    {
        public const string Magic = "wobblybuild";

        /// <summary>Bump when the format changes in a way old readers can't skip. Keep reading old versions.</summary>
        public const int FormatVersion = 1;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---------- Writing ----------

        public static string Write(BuildModel model, GridSettings grid, string name)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb, Inv))
            {
                Write(model, grid, name, writer);
            }
            return sb.ToString();
        }

        /// <summary>Writes blocks oldest first. Uses "\n" line endings on every platform.</summary>
        public static void Write(BuildModel model, GridSettings grid, string name, TextWriter writer)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            grid = grid ?? new GridSettings();

            Line(writer, Magic + " " + FormatVersion.ToString(Inv));
            Line(writer, "# Wobbly Mods build mode save. Edit with care: one block per line.");
            Line(writer, "name " + Escape(name ?? ""));
            Line(writer, "cell " + F(grid.CellSize) + " " + F(grid.Origin.X) + " " + F(grid.Origin.Y) + " " + F(grid.Origin.Z));
            foreach (PlacedBlock block in model.BlocksInPlacementOrder())
            {
                BlockSpec s = block.Spec;
                Line(writer, "block " + s.Type.Id + " " + s.Material.Id + " "
                    + I(s.Anchor.X) + " " + I(s.Anchor.Y) + " " + I(s.Anchor.Z) + " "
                    + I(s.Rotation.QuarterTurns) + " " + s.Paint.ToHex());
            }
            Line(writer, "end");
        }

        // ---------- Reading ----------

        /// <summary>Reads a build file into a new model. Never throws on bad file content.</summary>
        public static LoadResult Read(string text, BuildCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }
            LoadResult result = Parse(text ?? "", catalog);
            if (!result.Success)
            {
                return result;
            }
            var model = new BuildModel(catalog);
            PlaceAll(model, result);
            result.Model = model;
            return result;
        }

        /// <summary>
        /// Reads only the block list (nothing placed), for pasting a saved build into an existing
        /// one at an offset with <see cref="BuildEditor.PlaceMany"/> as a single undo step.
        /// </summary>
        public static LoadResult ReadBlocks(string text, BuildCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }
            return Parse(text ?? "", catalog);
        }

        private static void PlaceAll(BuildModel model, LoadResult result)
        {
            // A file can be bigger than the default cap; loading shouldn't silently cut a build.
            if (result.Blocks.Count > model.MaxBlocks)
            {
                result.Warnings.Add("Build has " + result.Blocks.Count + " blocks; only the first " + model.MaxBlocks + " were loaded.");
            }
            foreach (BlockSpec spec in result.Blocks)
            {
                int blocking;
                BuildResult r = model.CanPlace(spec.Type, spec.Anchor, spec.Rotation, out blocking);
                if (r == BuildResult.Ok)
                {
                    model.TryPlace(spec);
                    result.LoadedBlocks++;
                }
                else
                {
                    result.SkippedBlocks++;
                    if (r != BuildResult.LimitReached)
                    {
                        result.Warnings.Add("Skipped " + spec + ": " + r + ".");
                    }
                }
            }
        }

        private static LoadResult Parse(string text, BuildCatalog catalog)
        {
            var result = new LoadResult();
            result.Grid = new GridSettings();
            result.Blocks = new List<BlockSpec>();

            var reader = new StringReader(text);
            string raw;
            int lineNo = 0;
            bool headerSeen = false;
            bool endSeen = false;
            var unknownBlocks = new HashSet<string>();
            var unknownMaterials = new HashSet<string>();

            while ((raw = reader.ReadLine()) != null)
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string keyword;
                string rest;
                SplitFirst(line, out keyword, out rest);

                if (!headerSeen)
                {
                    int version;
                    if (keyword != Magic || !int.TryParse(rest, NumberStyles.Integer, Inv, out version) || version < 1)
                    {
                        result.Error = "Not a build file (first line should be '" + Magic + " <version>').";
                        return result;
                    }
                    if (version > FormatVersion)
                    {
                        result.Error = "This build was saved by a newer version of the mod (format " + version
                            + "; this one reads up to " + FormatVersion + "). Update the mod to load it.";
                        return result;
                    }
                    result.FormatVersion = version;
                    headerSeen = true;
                    continue;
                }

                if (endSeen)
                {
                    result.Warnings.Add("Line " + lineNo + ": text after 'end' ignored.");
                    break;
                }

                switch (keyword)
                {
                    case "name":
                        result.Name = Unescape(rest);
                        break;

                    case "cell":
                        ParseCell(rest, lineNo, result);
                        break;

                    case "block":
                        BlockSpec spec = ParseBlock(rest, lineNo, catalog, result, unknownBlocks, unknownMaterials);
                        if (spec != null)
                        {
                            result.Blocks.Add(spec);
                        }
                        else
                        {
                            result.SkippedBlocks++;
                        }
                        break;

                    case "end":
                        endSeen = true;
                        break;

                    default:
                        // A newer minor feature we don't know: skip it rather than fail.
                        result.Warnings.Add("Line " + lineNo + ": unknown entry '" + keyword + "' ignored.");
                        break;
                }
            }

            if (!headerSeen)
            {
                result.Error = "The build file is empty.";
                return result;
            }
            if (!endSeen)
            {
                result.Warnings.Add("The file has no 'end' line, so it may be cut off. Loaded what was there.");
            }
            return result;
        }

        private static void ParseCell(string rest, int lineNo, LoadResult result)
        {
            string[] p = SplitFields(rest);
            float size, x, y, z;
            if (p.Length != 4 || !TryF(p[0], out size) || !TryF(p[1], out x) || !TryF(p[2], out y) || !TryF(p[3], out z)
                || !(size > 0f) || float.IsInfinity(size))
            {
                result.Warnings.Add("Line " + lineNo + ": bad 'cell' line, using 1 m cells at the origin.");
                return;
            }
            result.Grid = new GridSettings(size, new Vec3(x, y, z));
        }

        private static BlockSpec ParseBlock(string rest, int lineNo, BuildCatalog catalog, LoadResult result,
            HashSet<string> unknownBlocks, HashSet<string> unknownMaterials)
        {
            string[] p = SplitFields(rest);
            if (p.Length < 7)
            {
                result.Warnings.Add("Line " + lineNo + ": block line has too few values, skipped.");
                return null;
            }

            BlockType type = catalog.GetBlock(p[0]);
            if (type == null)
            {
                // Warn once per id with the first line, so a big build doesn't flood the list.
                if (unknownBlocks.Add(p[0]))
                {
                    result.Warnings.Add("Line " + lineNo + ": unknown block '" + p[0] + "', skipped (and any others like it).");
                }
                return null;
            }
            BuildMaterial material = catalog.GetMaterial(p[1]);
            if (material == null)
            {
                if (unknownMaterials.Add(p[1]))
                {
                    result.Warnings.Add("Line " + lineNo + ": unknown material '" + p[1] + "', skipped (and any others like it).");
                }
                return null;
            }

            int x, y, z, turns;
            PaintColor paint;
            if (!TryI(p[2], out x) || !TryI(p[3], out y) || !TryI(p[4], out z) || !TryI(p[5], out turns)
                || turns < 0 || turns > 3 || !PaintColor.TryParseHex(p[6], out paint))
            {
                result.Warnings.Add("Line " + lineNo + ": block line has a bad number or colour, skipped.");
                return null;
            }
            // Extra fields (p.Length > 7) are reserved for future versions and ignored.
            return new BlockSpec(type, material, paint, new GridPos(x, y, z), new Rotation(turns));
        }

        // ---------- Helpers ----------

        private static void Line(TextWriter w, string s)
        {
            w.Write(s);
            w.Write('\n');
        }

        private static string F(float v)
        {
            return v.ToString("R", Inv);
        }

        private static string I(int v)
        {
            return v.ToString(Inv);
        }

        private static bool TryF(string s, out float v)
        {
            return float.TryParse(s, NumberStyles.Float, Inv, out v) && !float.IsNaN(v);
        }

        private static bool TryI(string s, out int v)
        {
            return int.TryParse(s, NumberStyles.Integer, Inv, out v);
        }

        private static void SplitFirst(string line, out string keyword, out string rest)
        {
            int space = line.IndexOf(' ');
            if (space < 0)
            {
                keyword = line;
                rest = "";
            }
            else
            {
                keyword = line.Substring(0, space);
                rest = line.Substring(space + 1);
            }
        }

        private static string[] SplitFields(string rest)
        {
            return rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Keeps the name on one line and exact: backslash, newline, CR and tab are escaped, and
        /// whitespace at either end is written as \uXXXX so trimming the line can't eat it.
        /// </summary>
        private static string Escape(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool atEdge = i == 0 || i == s.Length - 1;
                if (c == '\\' || c == '\n' || c == '\r' || c == '\t')
                {
                    sb.Append('\\').Append(c == '\n' ? 'n' : c == '\r' ? 'r' : c == '\t' ? 't' : '\\');
                }
                else if ((atEdge && char.IsWhiteSpace(c)) || char.IsControl(c))
                {
                    sb.Append("\\u").Append(((int)c).ToString("X4", Inv));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string Unescape(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\' || i + 1 >= s.Length)
                {
                    sb.Append(c);
                    continue;
                }
                char n = s[++i];
                int code;
                if (n == 'u' && i + 4 < s.Length
                    && int.TryParse(s.Substring(i + 1, 4), NumberStyles.AllowHexSpecifier, Inv, out code))
                {
                    sb.Append((char)code);
                    i += 4;
                }
                else
                {
                    sb.Append(n == 'n' ? '\n' : n == 'r' ? '\r' : n == 't' ? '\t' : n);
                }
            }
            return sb.ToString();
        }
    }
}
