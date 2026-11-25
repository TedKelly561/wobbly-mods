using System.Collections.Generic;

namespace WobblyMods.HouseMenu
{
    /// <summary>One thing the menu can place.</summary>
    internal sealed class CatalogItem
    {
        public string Id;
        public string DisplayName;
        public string Category;

        /// <summary>The game's Addressables address for the prop (from Scout's 02_props.txt).</summary>
        public string Address;

        /// <summary>Size multiplier. 1 = normal size.</summary>
        public float Scale = 1f;

        /// <summary>
        /// True for plain models that aren't network objects (e.g. the static fishing fish). They're placed
        /// as local copies instead of through the game's network spawner.
        /// </summary>
        public bool Local;

        /// <summary>
        /// For things we build ourselves (like the fish tank) instead of loading one game prop.
        /// Given the floor point and facing, returns the built object.
        /// </summary>
        public System.Func<UnityEngine.Vector3, UnityEngine.Quaternion, UnityEngine.GameObject> Build;

        /// <summary>How far in front of the player to place it, in meters. 0 = automatic from Scale.</summary>
        public float Distance;
    }

    /// <summary>
    /// The list of things the menu offers. Addresses are CONFIRMED in Scout report 2 (02_props.txt).
    /// The sandbox props are the ones the game's own sandbox menu spawns, so they should be network-ready.
    /// </summary>
    internal static class FurnitureCatalog
    {
        private const string Sandbox = "Assets/Content/Arcade/Sandbox/Prefabs/Props/";
        private const string Fish = "Assets/Content/Game/Prefabs/Props/Fishing_Props/Fish/Static/";

        public static readonly string[] Categories = { "Furniture", "Toys", "Animals" };

        private static readonly CatalogItem[] GameItems =
        {
            Item("bed",        "Yellow bed",     "Furniture", Sandbox + "Props/Double Bed Yellow.prefab"),
            Item("bunkbed",    "Pink bunk bed",  "Furniture", Sandbox + "Props/BunkBed_Pink.prefab"),
            Item("wardrobe",   "Wardrobe",       "Furniture", Sandbox + "Props/Wardrobe_1.prefab"),
            Item("desk",       "Desk",           "Furniture", Sandbox + "Props/Desk_1.prefab"),
            Item("chair",      "Desk chair",     "Furniture", Sandbox + "Props/DeskChair_1.prefab"),
            Item("lamp",       "Lamp",           "Furniture", Sandbox + "Props/Lamp_2.prefab"),
            Item("desklamp",   "Desk lamp",      "Furniture", Sandbox + "Props/DeskLamp.prefab"),
            Item("shelf",      "Shelf",          "Furniture", Sandbox + "Props/Shelf_4.prefab"),
            Item("curtains",   "Red curtains",   "Furniture", Sandbox + "Props/Curtains_Red.prefab"),
            Item("wardrobe2",  "Wardrobe 2",     "Furniture", Sandbox + "Props/Wardrobe_2.prefab"),
            Item("drawers",    "Drawers",        "Furniture", Sandbox + "Props/Draw_2.prefab"),
            Item("books",      "Books",          "Furniture", Sandbox + "Props/Books_1.prefab"),
            Item("computer",   "Computer",       "Furniture", Sandbox + "Props/Computer.prefab"),
            Item("stereo",     "Stereo",         "Furniture", Sandbox + "Props/Stereo.prefab"),
            Item("tvstand",    "TV stand",       "Furniture", Sandbox + "Stadium/TvStand.prefab"),
            Item("toilet",     "Toilet",         "Furniture",   Sandbox + "Props/Toilet.prefab"),

            // Lights in Skylar's colors (built by us, they really light up the room).
            new CatalogItem { Id = "lamporange", DisplayName = "Orange lamp", Category = "Furniture", Build = Decor.OrangeLamp, Distance = 2.5f },
            new CatalogItem { Id = "lampgreen", DisplayName = "Green lamp", Category = "Furniture", Build = Decor.GreenLamp, Distance = 2.5f },
            new CatalogItem { Id = "nightlight", DisplayName = "Night light", Category = "Furniture", Build = Decor.NightLight, Distance = 2f },
            new CatalogItem { Id = "partylights", DisplayName = "Party lights", Category = "Furniture", Build = Decor.PartyLights, Distance = 3.5f },

            // House plants: the game's own plants and trees, shrunk to indoor size (placed as local copies,
            // since scenery isn't a network object). Sizes are a first guess; Bigger/Smaller adjust them.
            Plant("cactus",     "Cactus",         Plants + "Cactus.prefab", 0.5f),
            Plant("palm",       "Palm tree",      Plants + "Resort_PalmTree.prefab", 0.25f),
            Plant("banana",     "Banana tree",    Plants + "BananaTree.prefab", 0.3f),
            Plant("bush",       "Jungle bush",    Plants + "JungleBush.prefab", 0.5f),
            Plant("flower",     "Flowers",        Plants + "Details/Flower.prefab", 1f),
            Plant("flowery",    "Yellow flowers", Plants + "Details/Flower Yellow.prefab", 1f),
            Plant("redplant",   "Red plant",      Plants + "Details/RedPlant.prefab", 1f),
            Plant("hedge",      "Hedge",          Plants + "Hedge.prefab", 0.5f),

            Item("softtoy",    "Soft toy",       "Toys",      Sandbox + "Props/SoftToy_2.prefab"),
            Item("softtoy2",   "Soft toy 2",     "Toys",      Sandbox + "Props/SoftToy_6.prefab"),
            Item("toybox",     "Toy box",        "Toys",      Sandbox + "Props/ToyBox.prefab"),
            Item("dollshouse", "Doll's house",   "Toys",      Sandbox + "Props/DollsHouse.prefab"),
            Item("drums",      "Drum kit",       "Toys",      Sandbox + "Props/DrumKit.prefab"),
            Item("checkers",   "Checkers",       "Toys",      Sandbox + "Props/Checkers.prefab"),
            Item("snakes",     "Snakes & ladders", "Toys",    Sandbox + "Props/SnakesAndLadders.prefab"),
            Item("darts",      "Dart board",     "Toys",      Sandbox + "Props/DartBoard.prefab"),

            Item("castle",     "Foam castle",    "Toys",       Sandbox + "FoamBlock/FoamCastle_1.prefab"),
            Item("ramp",       "Ramp",           "Toys",       Sandbox + "Stadium/Ramp_1.prefab"),
            new CatalogItem { Id = "goldcoins", DisplayName = "Gold treasure", Category = "Toys",
                              Address = Treasure + "Treasure_Gold_Coin_Static.prefab", Scale = 3f, Local = true, Distance = 2.5f },
            new CatalogItem { Id = "goldbar", DisplayName = "Gold bar", Category = "Toys",
                              Address = Treasure + "Treasure_Gold_Ingot_Static.prefab", Scale = 3f, Local = true, Distance = 2.5f },
            // Skylar's animal tanks (task T24). Built from simple shapes and the game's own models.
            new CatalogItem
            {
                Id = "fishtank", DisplayName = "Giant fish tank", Category = "Animals",
                Build = Tanks.FishTank.Build, Distance = 7f
            },
            // Skylar's aquarium tunnel and planted tank (after the real aquarium visit, 2026-11-25).
            new CatalogItem
            {
                Id = "aquariumtunnel", DisplayName = "Catfish tunnel", Category = "Animals",
                Build = Tanks.AquariumTunnel.Build, Distance = 12f
            },
            new CatalogItem
            {
                Id = "plantedtank", DisplayName = "Planted tank", Category = "Animals",
                Build = Tanks.AquariumTunnel.BuildPlanted, Distance = 4f
            },
            new CatalogItem
            {
                Id = "mangrovetank", DisplayName = "Mangrove tank", Category = "Animals",
                Build = Tanks.AquariumTunnel.BuildMangrove, Distance = 4f
            },
            new CatalogItem
            {
                Id = "smallfishtank", DisplayName = "Small fish tank", Category = "Animals",
                Build = Tanks.FishTank.BuildSmall, Distance = 2.5f
            },
            new CatalogItem
            {
                Id = "frogtank", DisplayName = "Frog tank", Category = "Animals",
                Build = Tanks.Terrarium.BuildFrogTank, Distance = 5f
            },
            new CatalogItem
            {
                Id = "lizardtank", DisplayName = "Lizard tank", Category = "Animals",
                Build = Tanks.Terrarium.BuildLizardTank, Distance = 5f
            },
            new CatalogItem
            {
                Id = "turtletank", DisplayName = "Turtle tank", Category = "Animals",
                Build = Tanks.Terrarium.BuildTurtleTank, Distance = 5f
            },
            new CatalogItem
            {
                // Our own model from the Unity kit; says "couldn't place" until the bundle is installed.
                Id = "redtail", DisplayName = "Redtail catfish", Category = "Animals",
                Build = CustomModels.Redtail, Distance = 5f
            },
            new CatalogItem
            {
                Id = "giantcatfish", DisplayName = "Giant catfish", Category = "Animals",
                Address = Fish + "CatFish_Static.prefab", Scale = 8f, Local = true
            },
        };


        /// <summary>
        /// Room pages inside the Furniture tab (the tab row only fits five tabs): one per room that has model-pack
        /// furniture. Each room's tile shows a photo of one of its items: { room, item id for the picture }.
        /// (The game's own props from other game modes were removed on 2026-11-21: broken or unproven.)
        /// </summary>
        public const string SkylarRoom = "Skylar's things";

        public static List<string[]> Rooms
        {
            get
            {
                var rooms = new List<string[]> { new[] { SkylarRoom, "sky-monstertruck" } };
                rooms.AddRange(ModelPack.Rooms());
                return rooms;
            }
        }

        private const string Plants = "Assets/Content/Game/Prefabs/Environment/Vegitation/";
        private const string Treasure = "Assets/Content/Game/Prefabs/Props/Treasure/Money/";

        private static CatalogItem Plant(string id, string name, string address, float scale)
        {
            return new CatalogItem { Id = id, DisplayName = name, Category = "Furniture", Address = address,
                                     Scale = scale, Local = true, Distance = 2.5f };
        }

        private static CatalogItem Item(string id, string name, string category, string address)
        {
            return new CatalogItem { Id = id, DisplayName = name, Category = category, Address = address };
        }

        private static CatalogItem[] _items;

        /// <summary>Everything the menu can place: the game's proven props and our own builds, plus the model pack.</summary>
        public static CatalogItem[] Items
        {
            get
            {
                if (_items == null)
                {
                    var all = new List<CatalogItem>(GameItems);
                    // Skylar's own pieces, made in code (Core SkylarFurniture): orange and green, cats, dinosaurs,
                    // a monster truck and her redtail. Solid, and they stand where they're put.
                    foreach (string id in WobblyMods.Core.Shapes.SkylarFurniture.Ids)
                    {
                        string made = id;
                        all.Add(new CatalogItem
                        {
                            Id = "sky-" + made,
                            DisplayName = WobblyMods.Core.Shapes.SkylarFurniture.NiceName(made),
                            Category = SkylarRoom,
                            Distance = 2.5f,
                            Build = (position, rotation) =>
                            {
                                UnityEngine.GameObject go = ShapeModels.BuildSolid("Skylar " + made, WobblyMods.Core.Shapes.SkylarFurniture.Build(made));
                                go.name = WobblyMods.Core.Shapes.SkylarFurniture.NiceName(made);
                                go.transform.SetPositionAndRotation(position, rotation);
                                if (made == "trampoline")
                                {
                                    // A thin trigger just above the rug does the bouncing (Fun.cs Trampoline).
                                    var zone = go.AddComponent<UnityEngine.BoxCollider>();
                                    zone.isTrigger = true;
                                    zone.center = new UnityEngine.Vector3(0f, 0.3f, 0f);
                                    zone.size = new UnityEngine.Vector3(1.3f, 0.4f, 1.3f);
                                    go.AddComponent<Trampoline>();
                                }
                                return go;
                            }
                        });
                    }
                    all.AddRange(ModelPack.Items());
                    _items = all.ToArray();
                }
                return _items;
            }
        }

        /// <summary>The catalog item with that id, or null (used when loading a saved house).</summary>
        public static CatalogItem Find(string id)
        {
            foreach (CatalogItem item in Items)
            {
                if (item.Id == id) return item;
            }
            return null;
        }

        public static List<CatalogItem> InCategory(string category)
        {
            var result = new List<CatalogItem>();
            foreach (CatalogItem item in Items)
            {
                if (item.Category == category)
                {
                    result.Add(item);
                }
            }
            return result;
        }
    }
}
