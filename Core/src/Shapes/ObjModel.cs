using System;
using System.Collections.Generic;
using System.Globalization;

namespace WobblyMods.Core.Shapes
{
    /// <summary>
    /// Reads a Wavefront .obj model (and its .mtl colours) into coloured shape meshes, one per material.
    /// This is how the free furniture pack gets into the game without the Unity editor.
    ///
    /// .obj files are right-handed with counter-clockwise front faces; the game (Unity) is left-handed. So X is
    /// negated and each face's order is reversed, which keeps the library's rule (cross(b-a, c-a) points outward)
    /// and stops the model from coming out mirrored. Faces with more than three corners are split as a fan.
    /// Normals are flat, per face (the low-poly look). Texture coordinates and the file's own normals are ignored.
    /// Bad lines are skipped, never thrown on: a broken file gives fewer faces, not a crash.
    /// </summary>
    public static class ObjModel
    {
        public static List<ColoredMesh> Parse(string objText, string mtlText, Rgb fallback)
        {
            Dictionary<string, Rgb> colors = ParseMaterials(mtlText);
            var positions = new List<V3>();
            var order = new List<string>();
            var meshes = new Dictionary<string, ShapeMesh>();
            string material = "";
            var corners = new List<int>();

            foreach (string raw in SplitLines(objText))
            {
                string line = raw.Trim();
                if (line.Length < 2 || line[0] == '#') continue;
                string[] t = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (t[0] == "v" && t.Length >= 4)
                {
                    float x, y, z;
                    if (Num(t[1], out x) && Num(t[2], out y) && Num(t[3], out z)) positions.Add(new V3(-x, y, z));
                    else positions.Add(V3.Zero); // keep the numbering of later vertices right
                }
                else if (t[0] == "usemtl")
                {
                    material = t.Length > 1 ? t[1] : "";
                }
                else if (t[0] == "f" && t.Length >= 4)
                {
                    corners.Clear();
                    bool ok = true;
                    for (int i = 1; i < t.Length; i++)
                    {
                        string first = t[i];
                        int slash = first.IndexOf('/');
                        if (slash >= 0) first = first.Substring(0, slash);
                        int index;
                        if (!int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) { ok = false; break; }
                        index = index < 0 ? positions.Count + index : index - 1;
                        if (index < 0 || index >= positions.Count) { ok = false; break; }
                        corners.Add(index);
                    }
                    if (!ok || corners.Count < 3) continue;

                    ShapeMesh mesh;
                    if (!meshes.TryGetValue(material, out mesh))
                    {
                        mesh = new ShapeMesh();
                        meshes[material] = mesh;
                        order.Add(material);
                    }
                    for (int i = 1; i + 1 < corners.Count; i++)
                    {
                        // Reversed order (a, c, b) because X was negated.
                        V3 a = positions[corners[0]], b = positions[corners[i + 1]], c = positions[corners[i]];
                        V3 normal = V3.Cross(b - a, c - a);
                        if (normal.Length < 1e-9f) continue;
                        normal = normal.Normalized;
                        int ia = mesh.AddVertex(a, normal), ib = mesh.AddVertex(b, normal), ic = mesh.AddVertex(c, normal);
                        mesh.AddTriangle(ia, ib, ic);
                    }
                }
            }

            var parts = new List<ColoredMesh>();
            foreach (string name in order)
            {
                if (meshes[name].Triangles.Count == 0) continue;
                Rgb color;
                parts.Add(new ColoredMesh(meshes[name], colors.TryGetValue(name, out color) ? color : fallback));
            }
            return parts;
        }

        /// <summary>Material name to its diffuse colour (the "Kd r g b" line, 0 to 1 each).</summary>
        public static Dictionary<string, Rgb> ParseMaterials(string mtlText)
        {
            var colors = new Dictionary<string, Rgb>();
            string current = null;
            foreach (string raw in SplitLines(mtlText))
            {
                string[] t = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length == 0) continue;
                if (t[0] == "newmtl") current = t.Length > 1 ? t[1] : "";
                else if (t[0] == "Kd" && current != null && t.Length >= 4)
                {
                    float r, g, b;
                    if (Num(t[1], out r) && Num(t[2], out g) && Num(t[3], out b)) colors[current] = new Rgb(Byte(r), Byte(g), Byte(b));
                }
            }
            return colors;
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            return string.IsNullOrEmpty(text) ? new string[0] : text.Split('\n');
        }

        private static bool Num(string s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static byte Byte(float unit)
        {
            if (unit <= 0f) return 0;
            if (unit >= 1f) return 255;
            return (byte)Math.Round(unit * 255f);
        }
    }
}
