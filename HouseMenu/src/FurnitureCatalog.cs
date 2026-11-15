using System.Collections.Generic;

namespace WobblyMods.HouseMenu
{
    /// <summary>One thing the menu can place. PrefabName stays null until verified in the game's code.</summary>
    internal sealed class CatalogItem
    {
        public string Id;
        public string DisplayName;
        public string Category;

        /// <summary>The game's prefab name. Null means "not verified yet", so the item can't spawn.</summary>
        public string PrefabName;
    }

    /// <summary>
    /// The list of things the menu offers. Add items here. Items are placeholders until
    /// PrefabName is filled in from the game's code (see PROJECT_NOTES.md, task T2).
    /// </summary>
    internal static class FurnitureCatalog
    {
        public static readonly string[] Categories = { "Furniture", "Toys", "Fun" };

        public static readonly CatalogItem[] Items =
        {
            new CatalogItem { Id = "couch",  DisplayName = "Couch",  Category = "Furniture" },
            new CatalogItem { Id = "lamp",   DisplayName = "Lamp",   Category = "Furniture" },
            new CatalogItem { Id = "table",  DisplayName = "Table",  Category = "Furniture" },
            new CatalogItem { Id = "plant",  DisplayName = "Plant",  Category = "Furniture" },
            new CatalogItem { Id = "ball",   DisplayName = "Ball",   Category = "Toys" },
            new CatalogItem { Id = "teddy",  DisplayName = "Teddy",  Category = "Toys" },
            new CatalogItem { Id = "giant",  DisplayName = "Giant ball", Category = "Fun" },
        };

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
