using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WobblyMods.Core;
using WobblyMods.Core.Shapes;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Furniture from model files (parent's choice, 2026-11-21: a free model pack plus our own), because the game
    /// has almost no intact furniture props to load. Every .obj in plugins\WobblyMods\models\furniture becomes a
    /// menu item: Core's ObjModel reads it, ShapeModels turns it into a Unity object, and it gets solid mesh
    /// colliders (our meshes are made at runtime, so unlike the game's they can be used for collision).
    /// The file name gives the label and the room page (Core PropRooms).
    /// </summary>
    internal static class ModelPack
    {
        public const string IdPrefix = "m-";

        private static readonly Dictionary<string, List<ColoredMesh>> Parsed = new Dictionary<string, List<ColoredMesh>>();
        private static readonly Dictionary<string, Vector3> Pivots = new Dictionary<string, Vector3>();
        private static List<CatalogItem> _items;
        private static float _scale = 1f;

        public static string Folder
        {
            get { return Path.Combine(Path.Combine(Path.Combine(BepInEx.Paths.PluginPath, "WobblyMods"), "models"), "furniture"); }
        }

        public static void Init(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.05f, 50f);
        }

        /// <summary>One catalog item per model file (found once).</summary>
        public static List<CatalogItem> Items()
        {
            if (_items != null) return _items;
            _items = new List<CatalogItem>();
            try
            {
                if (!Directory.Exists(Folder))
                {
                    Plugin.Log.LogInfo("Model pack: no folder at " + Folder + " (no model-pack furniture yet).");
                    return _items;
                }
                string[] files = Directory.GetFiles(Folder, "*.obj");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                var counts = new Dictionary<string, int>();
                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.IndexOf(' ') >= 0) continue; // ids are saved in house files, which split on spaces
                    string room = RoomName(PropRooms.Classify("/Props/" + name));
                    string path = file;
                    _items.Add(new CatalogItem
                    {
                        Id = IdPrefix + name,
                        DisplayName = PropRooms.NiceName(name),
                        Category = room,
                        Build = (position, rotation) => Spawn(path, position, rotation),
                        Distance = 3f
                    });
                    int n;
                    counts[room] = counts.TryGetValue(room, out n) ? n + 1 : 1;
                }
                var summary = new List<string>();
                foreach (KeyValuePair<string, int> pair in counts) summary.Add(pair.Key + " " + pair.Value);
                Plugin.Log.LogInfo("Model pack: " + _items.Count + " models in " + Folder + " (" + string.Join(", ", summary.ToArray()) +
                                   "), scale " + _scale.ToString("0.00") + ".");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Model pack: reading " + Folder + " failed: " + e);
            }
            return _items;
        }

        /// <summary>Room pages that have models, in a fixed order: { page name, id of the item whose photo is the tile }.</summary>
        public static List<string[]> Rooms()
        {
            var rooms = new List<string[]>();
            foreach (string room in new[] { "Kitchen", "Bathroom", "Bedroom", "Living room", "Kids' room", "Garden", "More" })
            {
                foreach (CatalogItem item in Items())
                {
                    if (item.Category == room) { rooms.Add(new[] { room, item.Id }); break; }
                }
            }
            return rooms;
        }

        private static string RoomName(Room room)
        {
            switch (room)
            {
                case Room.Kitchen: return "Kitchen";
                case Room.Bathroom: return "Bathroom";
                case Room.Bedroom: return "Bedroom";
                case Room.Living: return "Living room";
                case Room.Kids: return "Kids' room";
                case Room.Garden: return "Garden";
                default: return "More";
            }
        }

        private static GameObject Spawn(string objPath, Vector3 position, Quaternion rotation)
        {
            string name = Path.GetFileNameWithoutExtension(objPath);
            List<ColoredMesh> parts;
            if (!Parsed.TryGetValue(objPath, out parts))
            {
                parts = Load(objPath);
                Parsed[objPath] = parts;
                if (parts.Count > 0)
                {
                    // Packs often put the origin at a corner. Use the bottom center, so it stands where it's placed.
                    V3 min = parts[0].Mesh.Min, max = parts[0].Mesh.Max;
                    foreach (ColoredMesh p in parts)
                    {
                        V3 a = p.Mesh.Min, b = p.Mesh.Max;
                        min = new V3(Math.Min(min.X, a.X), Math.Min(min.Y, a.Y), Math.Min(min.Z, a.Z));
                        max = new V3(Math.Max(max.X, b.X), Math.Max(max.Y, b.Y), Math.Max(max.Z, b.Z));
                    }
                    Pivots[objPath] = new Vector3((min.X + max.X) * 0.5f, min.Y, (min.Z + max.Z) * 0.5f);
                    Plugin.Log.LogInfo("Model pack: " + name + " is " + ((max.X - min.X) * _scale).ToString("0.00") + " x " +
                                       ((max.Y - min.Y) * _scale).ToString("0.00") + " x " + ((max.Z - min.Z) * _scale).ToString("0.00") +
                                       " m (wide x tall x deep), " + parts.Count + " colours.");
                }
            }
            if (parts.Count == 0) return null;

            GameObject model = ShapeModels.Build("model " + name, parts);
            Vector3 pivot = Pivots[objPath];
            foreach (Transform child in model.transform)
            {
                child.localPosition = -pivot;
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) child.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            model.name = PropRooms.NiceName(name);
            model.transform.localScale = Vector3.one * _scale;
            model.transform.SetPositionAndRotation(position, rotation);
            return model;
        }

        private static List<ColoredMesh> Load(string objPath)
        {
            try
            {
                string obj = File.ReadAllText(objPath);
                string mtl = null;
                string mtlPath = Path.ChangeExtension(objPath, ".mtl");
                foreach (string line in obj.Split('\n'))
                {
                    string t = line.Trim();
                    if (t.StartsWith("mtllib ", StringComparison.Ordinal))
                    {
                        string named = Path.Combine(Path.GetDirectoryName(objPath), t.Substring(7).Trim());
                        if (File.Exists(named)) mtlPath = named;
                        break;
                    }
                }
                if (File.Exists(mtlPath)) mtl = File.ReadAllText(mtlPath);
                List<ColoredMesh> parts = ObjModel.Parse(obj, mtl, new Rgb(200, 200, 200));
                if (parts.Count == 0) Plugin.Log.LogWarning("Model pack: " + Path.GetFileName(objPath) + " has no faces.");
                return parts;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Model pack: " + Path.GetFileName(objPath) + " couldn't be read: " + e.Message);
                return new List<ColoredMesh>();
            }
        }
    }
}
