using UnityEngine;

namespace WobblyMods.HouseMenu.Icons
{
    /// <summary>
    /// One color per tab (the parent's choice, 2026-11-18): Furniture orange, Toys yellow, Animals green,
    /// Change blue, Build brown/tan. Sub-pages use the color of the tab they were opened from, so a child
    /// always knows "where" they are by color alone.
    /// </summary>
    internal static class TabTheme
    {
        public static readonly Color Furniture = new Color(1f, 0.52f, 0.08f);
        public static readonly Color Toys = new Color(1f, 0.80f, 0.10f);
        public static readonly Color Animals = new Color(0.22f, 0.68f, 0.22f);
        public static readonly Color Change = new Color(0.20f, 0.50f, 0.92f);
        public static readonly Color Build = new Color(0.70f, 0.50f, 0.28f);
        public static readonly Color Other = new Color(0.55f, 0.45f, 0.75f);

        /// <summary>The focused tile's glow: bright yellow-white, obvious on every tab color.</summary>
        public static readonly Color Glow = new Color(1f, 0.97f, 0.45f);

        public static Color ColorFor(string tabTitle)
        {
            switch (tabTitle)
            {
                case "Furniture": return Furniture;
                case "Toys": return Toys;
                case "Animals": return Animals;
                case "Change": return Change;
                case "Build": return Build;
                default: return Other;
            }
        }

        /// <summary>Symbol key for a tab button (drawn by SymbolIcons).</summary>
        public static string IconFor(string tabTitle)
        {
            switch (tabTitle)
            {
                case "Furniture": return "sym:tab-furniture";
                case "Toys": return "sym:tab-toys";
                case "Animals": return "sym:tab-animals";
                case "Change": return "sym:tab-change";
                case "Build": return "sym:tab-build";
                default: return null;
            }
        }

        /// <summary>A lighter version (tile and snapshot backgrounds).</summary>
        public static Color Light(Color c, float amount = 0.55f)
        {
            Color l = Color.Lerp(c, Color.white, amount);
            l.a = 1f;
            return l;
        }

        /// <summary>A darker version (borders, text shadows).</summary>
        public static Color Dark(Color c, float amount = 0.45f)
        {
            Color d = Color.Lerp(c, Color.black, amount);
            d.a = 1f;
            return d;
        }

        public static string Hex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
