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

        public static readonly CatalogItem[] Items =
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
            Item("toilet",     "Toilet",         "Kitchen",   Sandbox + "Props/Toilet.prefab"),

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
            // ---- All the rest of the game's furniture (parent's request, 2026-11-19), from the real prop list
            // (Switchyard doc "Wobbly Life furniture prop list", 606 props). Shown as room pages inside the
            // Furniture tab (Rooms below). These come from other game modes, so most aren't network objects:
            // the spawner tries the network way first, then places a local copy. Sizes are untested.
            Item("fridge",     "Fridge",         "Kitchen", Trash + "Trash_Fridge.prefab"),
            Item("ovengas",    "Gas oven",       "Kitchen", Trash + "Trash_Oven_Gas.prefab"),
            Item("ovengrey",   "Grey oven",      "Kitchen", Trash + "Trash_Oven_Gray.prefab"),
            Item("washer",     "Washing machine", "Kitchen", Trash + "Trash_Washing_Machine.prefab"),
            Item("cupboard1",  "Cupboard",       "Kitchen", Trash + "Trash_Cupboard_1.prefab"),
            Item("cupboard2",  "Cupboard 2",     "Kitchen", Trash + "Trash_Cupboard_2.prefab"),
            Item("cupboard3",  "Cupboard 3",     "Kitchen", Trash + "Trash_Cupboard_3.prefab"),
            Item("table1",     "Table",          "Kitchen", Trash + "Trash_Table_1.prefab"),
            Item("table2",     "Table 2",        "Kitchen", Trash + "Trash_Table_2.prefab"),
            Item("plate",      "Plate",          "Kitchen", Sandbox + "Props/Plate_2.prefab"),
            Item("papercup",   "Paper cup",      "Kitchen", Sandbox + "Props/Food_PaperCup.prefab"),

            Item("sofa",       "Sofa",           "Living room", Trash + "Trash_Sofa_Old.prefab"),
            Item("armchair1",  "Chair",          "Living room", Trash + "Trash_Chair_1.prefab"),
            Item("armchair2",  "Chair 2",        "Living room", Trash + "Trash_Chair_2.prefab"),
            Item("armchair3",  "Chair 3",        "Living room", Trash + "Trash_Chair_3.prefab"),
            Item("armchair4",  "Chair 4",        "Living room", Trash + "Trash_Chair_4.prefab"),
            Item("footstool",  "Foot stool",     "Living room", Trash + "Trash_FootStool.prefab"),
            Item("tv1",        "TV",             "Living room", Trash + "Trash_Tv_1.prefab"),
            Item("tv2",        "TV 2",           "Living room", Trash + "Trash_Tv_2.prefab"),
            Item("tv3",        "TV 3",           "Living room", Trash + "Trash_Tv_3.prefab"),
            Item("tv4",        "TV 4",           "Living room", Trash + "Trash_Tv_4.prefab"),
            Item("tvtable",    "TV table",       "Living room", Trash + "Trash_Tv_Stand.prefab"),
            Item("coffeetable", "Coffee table",  "Living room", Trash + "Trash_Table_3.prefab"),
            Item("sidetable",  "Side table",     "Living room", Trash + "Trash_Table_4.prefab"),
            Item("clock",      "Grandfather clock", "Living room", Trash + "Trash_GrandfatherClock.prefab"),
            Item("piano",      "Piano",          "Living room", Trash + "Broken Grand Piano.prefab"),
            Item("dresser",    "Dresser",        "Living room", Trash + "Trash_Dresser.prefab"),
            Item("shelf1",     "Shelf 1",        "Living room", Trash + "Trash_Shelf_1.prefab"),
            Item("shelf2",     "Shelf 2",        "Living room", Trash + "Trash_Shelf_2.prefab"),
            Item("shelf3",     "Shelf 3",        "Living room", Trash + "Trash_Shelf_3.prefab"),
            Item("drawers1",   "Small drawers",  "Living room", Trash + "Trash_Draw_1.prefab"),
            Item("potplant",   "Pot plant",      "Living room", Lobby + "Arcade/Arcade_Plant.prefab"),
            Item("trophies",   "Trophy cabinet", "Living room", Lobby + "Arcade/LobbyTrophyCabinet.prefab"),
            Item("neonstar",   "Neon star",      "Living room", Lobby + "Arcade/NeonStar.prefab"),
            Item("speaker",    "Big speaker",    "Living room", Lobby + "Arcade/Animated Large Speaker.prefab"),

            Item("cabinbed",   "Cabin bed",      "Kids' room", Hide + "Village/Bed_LogCabbin.prefab"),
            Item("racetrack",  "Toy race track", "Kids' room", Sandbox + "Props/Track.prefab"),
            Item("basketball", "Basketball",     "Kids' room", Hide + "Quarry/Dynamic Props/BasketBall.prefab"),
            Item("hoop",       "Basketball hoop", "Kids' room", Hide + "Quarry/Static/BasketBallHoop.prefab"),
            Item("crayons",    "Giant crayons",  "Kids' room", Hide + "ArtStudio/BoxedCrayons.prefab"),
            Item("easel",      "Giant easel",    "Kids' room", Hide + "ArtStudio/EaselLarge.prefab"),
            Item("painting",   "Giant painting", "Kids' room", Hide + "ArtStudio/CanvasPainted.prefab"),
            Item("paintbrush", "Giant paint brush", "Kids' room", Hide + "ArtStudio/PaintBrush_Large.prefab"),
            Item("paintorange", "Orange paint",  "Kids' room", Hide + "ArtStudio/PaintTin_Orange.prefab"),
            Item("paintgreen", "Green paint",    "Kids' room", Hide + "ArtStudio/PaintTin_Green.prefab"),
            Item("statue",     "Wobbly statue",  "Kids' room", Hide + "ArtStudio/WobblyBust_Large.prefab"),
            Item("cowride",    "Cow ride",       "Kids' room", Lobby + "Arcade/CowRideMachine.prefab"),

            Item("doghouse",   "Dog house",      "Garden", Hide + "Village/DogHouse.prefab"),
            Item("chickens",   "Chicken coop",   "Garden", Hide + "Village/ChickenCoop.prefab"),
            Item("shed",       "Shed",           "Garden", Hide + "Village/Shed.prefab"),
            Item("stable",     "Stable",         "Garden", Hide + "Village/Stable.prefab"),
            Item("bandstand",  "Band stand",     "Garden", Hide + "Village/BandStand.prefab"),
            Item("sweetcorn",  "Sweetcorn",      "Garden", Hide + "Village/SweetCorn_Plant.prefab"),
            Item("treehouse",  "Tree house",     "Garden", Hide + "Forrest/TreeHouse.prefab"),
            Item("treeshort",  "Short tree",     "Garden", Hide + "Forrest/Tree_Short.prefab"),
            Item("treetall",   "Tall tree",      "Garden", Hide + "Forrest/Tree_Tall.prefab"),
            Item("ropebridge", "Rope bridge",    "Garden", Hide + "Forrest/RopeBridge_Short.prefab"),
            Item("fence",      "Fence",          "Garden", Hide + "Quarry/Static/FenceBlock.prefab"),
            Item("woodpile",   "Wood pile",      "Garden", Hide + "Quarry/Static/Prop_WoodPile_Small.prefab"),

            Item("slidelong",  "Long slide",     "Playground", Lobby + "PlayZone/LongSlide.prefab"),
            Item("slideshort", "Short slide",    "Playground", Lobby + "PlayZone/ShortSlide.prefab"),
            Item("slidespiral", "Spiral slide",  "Playground", Lobby + "PlayZone/SpiralSlide.prefab"),
            Item("swing",      "Big swing",      "Playground", Lobby + "PlayZone/LargeSwing.prefab"),
            Item("trampgreen", "Green trampoline", "Playground", Run + "Trampoline_Green.prefab"),
            Item("trampyellow", "Yellow trampoline", "Playground", Run + "Trampoline_Yellow.prefab"),
            Item("trampred",   "Red trampoline", "Playground", Run + "Trampoline_Red.prefab"),
            Item("trampblue",  "Blue trampoline", "Playground", Run + "Trampoline_Blue.prefab"),
            Item("foambridge", "Foam bridge",    "Playground", Lobby + "PlayZone/FoamBridge.prefab"),
            Item("foamtunnel", "Foam tunnel",    "Playground", Lobby + "PlayZone/FoamCubeTunnel.prefab"),
            Item("steppingstone", "Stepping stone", "Playground", Lobby + "PlayZone/FoamSteppingStone.prefab"),
            Item("turret",     "Castle tower",   "Playground", Lobby + "PlayZone/CastleTurret.prefab"),
            Item("castle2",    "Foam castle 2",  "Playground", Sandbox + "FoamBlock/FoamCastle_2.prefab"),
            Item("bigslide",   "Giant slide",    "Playground", Run + "LargeSlide.prefab"),

            // Skylar's animal tanks (task T24). Built from simple shapes and the game's own models.
            new CatalogItem
            {
                Id = "fishtank", DisplayName = "Giant fish tank", Category = "Animals",
                Build = Tanks.FishTank.Build, Distance = 7f
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

        private const string Trash = "Assets/Content/Arcade/Trash Man/Prefabs/Props/Dynamic Objects/";
        private const string Hide = "Assets/Content/Arcade/Hide And Seek/Prefabs/Props/";
        private const string Run = "Assets/Content/Arcade/Wobble Run/Prefabs/Props/";
        private const string Lobby = "Arcade/ArcadeLobby/Prefabs/Props/";

        /// <summary>
        /// Room pages inside the Furniture tab (the tab row only fits five tabs). Each room's tile shows a photo
        /// of one of its items, so Skylar can pick a room without reading: { room, item id for the picture }.
        /// </summary>
        public static readonly string[][] Rooms =
        {
            new[] { "Kitchen", "fridge" },
            new[] { "Living room", "sofa" },
            new[] { "Kids' room", "crayons" },
            new[] { "Garden", "doghouse" },
            new[] { "Playground", "slidelong" },
        };

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
