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
                    string room = RoomFor(name);
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
            foreach (string room in new[] { "Kitchen", "Bathroom", "Bedroom", "Living room", "Tables & chairs", "Kids' room", "Garden", "Walls & floors", "More" })
            {
                foreach (CatalogItem item in Items())
                {
                    if (item.Category == room) { rooms.Add(new[] { room, item.Id }); break; }
                }
            }
            return rooms;
        }

        // Name beginnings that decide the room, checked in order (the furniture pack names its files this way,
        // e.g. kitchenFridge, loungeSofa, wallWindow). Anything else is sorted by Core PropRooms' keywords.
        private static readonly string[][] Prefixes =
        {
            new[] { "Walls & floors", "wall", "floor", "doorway", "stairs", "paneling" },
            new[] { "Kitchen", "kitchen", "hood", "toaster", "washer", "dryer", "trashcan" },
            new[] { "Bathroom", "bathroom", "bathtub", "shower", "toilet" },
            new[] { "Bedroom", "bed", "cabinetBed", "pillow", "bear" },
            new[] { "Tables & chairs", "table", "chair", "stool", "bench", "desk", "sideTable" },
            new[] { "Living room", "lounge", "television", "cabinetTelevision", "rug", "lamp", "bookcase", "books", "speaker",
                    "radio", "plant", "pottedPlant", "coatRack", "ceilingFan", "computer", "laptop", "cardboardBox" },
        };

        // Small things look lost at furniture scale (test 2026-11-21: "the blender should have been twice as big"),
        // so they get an extra multiplier. Walls, floors, doorways and stairs ignore the furniture size setting and
        // stay at 2 (walls about 2.6 m), so making furniture bigger doesn't make rooms huge.
        private static readonly string[] SmallThings =
        {
            "kitchenBlender", "toaster", "kitchenCoffeeMachine", "kitchenMicrowave", "books", "computer", "laptop",
            "lampRoundTable", "lampSquareTable", "radio", "speakerSmall", "plantSmall", "pottedPlant", "pillow", "bear", "trashcan"
        };
        private const float SmallThingBoost = 1.8f;
        private const float BuildingPieceScale = 2f;

        private static float ScaleFor(string name)
        {
            if (RoomFor(name) == "Walls & floors") return BuildingPieceScale;
            foreach (string small in SmallThings)
            {
                if (name.StartsWith(small, StringComparison.OrdinalIgnoreCase)) return _scale * SmallThingBoost;
            }
            return _scale;
        }

        private static string RoomFor(string name)
        {
            foreach (string[] rule in Prefixes)
            {
                for (int i = 1; i < rule.Length; i++)
                {
                    if (name.StartsWith(rule[i], StringComparison.OrdinalIgnoreCase)) return rule[0];
                }
            }
            return RoomName(PropRooms.Classify("/Props/" + name));
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
                    float k = ScaleFor(name);
                    Plugin.Log.LogInfo("Model pack: " + name + " is " + ((max.X - min.X) * k).ToString("0.00") + " x " +
                                       ((max.Y - min.Y) * k).ToString("0.00") + " x " + ((max.Z - min.Z) * k).ToString("0.00") +
                                       " m (wide x tall x deep), " + parts.Count + " colours.");
                }
            }
            if (parts.Count == 0) return null;

            GameObject model = ShapeModels.Build("model " + name, parts);
            Vector3 pivot = Pivots[objPath];
            // The pack's models face -Z (seen in the preview pictures); turn them so the front faces the player
            // (and the camera that takes the menu photo). The half turn is on the PARTS, not on the model itself,
            // so the model's own spot and facing are exactly what was asked for. (It used to be on the model:
            // a saved house, or Change > Copy, then asked for that facing again and got another half turn on top,
            // so furniture came back facing the wall.) It looks and stands the same as before.
            Quaternion faceFront = Quaternion.Euler(0f, 180f, 0f);
            foreach (Transform child in model.transform)
            {
                child.localRotation = faceFront;
                child.localPosition = faceFront * -pivot;
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) child.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            model.name = PropRooms.NiceName(name);
            model.transform.localScale = Vector3.one * ScaleFor(name);
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
