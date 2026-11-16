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

        public static readonly string[] Categories = { "Furniture", "Toys", "Fun" };

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

            Item("softtoy",    "Soft toy",       "Toys",      Sandbox + "Props/SoftToy_2.prefab"),
            Item("softtoy2",   "Soft toy 2",     "Toys",      Sandbox + "Props/SoftToy_6.prefab"),
            Item("toybox",     "Toy box",        "Toys",      Sandbox + "Props/ToyBox.prefab"),
            Item("dollshouse", "Doll's house",   "Toys",      Sandbox + "Props/DollsHouse.prefab"),
            Item("drums",      "Drum kit",       "Toys",      Sandbox + "Props/DrumKit.prefab"),
            Item("checkers",   "Checkers",       "Toys",      Sandbox + "Props/Checkers.prefab"),
            Item("snakes",     "Snakes & ladders", "Toys",    Sandbox + "Props/SnakesAndLadders.prefab"),
            Item("darts",      "Dart board",     "Toys",      Sandbox + "Props/DartBoard.prefab"),

            Item("castle",     "Foam castle",    "Fun",       Sandbox + "FoamBlock/FoamCastle_1.prefab"),
            Item("ramp",       "Ramp",           "Fun",       Sandbox + "Stadium/Ramp_1.prefab"),
            new CatalogItem
            {
                Id = "fishtank", DisplayName = "Giant fish tank", Category = "Fun",
                Build = Tanks.FishTank.Build, Distance = 7f
            },
            new CatalogItem
            {
                Id = "giantcatfish", DisplayName = "Giant catfish", Category = "Fun",
                Address = Fish + "CatFish_Static.prefab", Scale = 8f, Local = true
            },
        };

        private static CatalogItem Item(string id, string name, string category, string address)
        {
            return new CatalogItem { Id = id, DisplayName = name, Category = category, Address = address };
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
