using System.Collections.Generic;
using UnityEngine;
using WobblyMods.BuildMode;
using WobblyMods.HouseMenu.Tanks;

namespace WobblyMods.HouseMenu.Build
{
    /// <summary>One paint choice for the menu: the colour and the word shown on the button.</summary>
    internal struct NamedPaint
    {
        public NamedPaint(string name, byte r, byte g, byte b)
        {
            Name = name;
            Color = new PaintColor(r, g, b);
        }

        public readonly string Name;
        public readonly PaintColor Color;
    }

    /// <summary>
    /// Colours and Unity materials for build blocks. v1 has no textures: each material id gets a
    /// natural colour, and paint tints it. Materials come from TankMaterials (proven in game for the
    /// fish tank): Solid for opaque blocks, Tinted for glass. One material per material + paint,
    /// shared by every chunk.
    /// </summary>
    internal static class BuildLooks
    {
        /// <summary>Skylar's favourites first (PROJECT_NOTES: orange and green), then the rest.</summary>
        public static readonly NamedPaint[] Paints =
        {
            new NamedPaint("Orange", 255, 128, 0),
            new NamedPaint("Green", 51, 191, 51),
            new NamedPaint("Light orange", 255, 191, 77),
            new NamedPaint("Lime", 128, 230, 77),
            new NamedPaint("Red", 230, 57, 70),
            new NamedPaint("Yellow", 255, 214, 10),
            new NamedPaint("Blue", 69, 123, 157),
            new NamedPaint("Purple", 155, 93, 229),
            new NamedPaint("Pink", 255, 143, 171),
            new NamedPaint("Brown", 139, 94, 60),
            new NamedPaint("White", 245, 245, 245),
            new NamedPaint("Grey", 120, 120, 120),
            new NamedPaint("Black", 30, 30, 30),
        };

        private const float GlassAlpha = 0.3f;

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static Material _ghostOk;
        private static Material _ghostBlocked;

        public static PaintColor[] PaintColors()
        {
            var colors = new PaintColor[Paints.Length];
            for (int i = 0; i < Paints.Length; i++)
            {
                colors[i] = Paints[i].Color;
            }
            return colors;
        }

        /// <summary>The material's own colour, used when no paint is chosen.</summary>
        public static Color NaturalColor(BuildMaterial material)
        {
            switch (material.Id)
            {
                case BuildCatalog.Ids.Wood: return new Color(0.66f, 0.46f, 0.28f);
                case BuildCatalog.Ids.Brick: return new Color(0.70f, 0.32f, 0.22f);
                case BuildCatalog.Ids.Stone: return new Color(0.55f, 0.55f, 0.57f);
                case BuildCatalog.Ids.Glass: return new Color(0.75f, 0.9f, 1f, GlassAlpha);
                case BuildCatalog.Ids.Concrete: return new Color(0.76f, 0.76f, 0.73f);
                case BuildCatalog.Ids.RoofTile: return new Color(0.60f, 0.24f, 0.16f);
                case BuildCatalog.Ids.Plaster: return new Color(0.95f, 0.92f, 0.85f);
                default: return new Color(0.7f, 0.7f, 0.7f);
            }
        }

        /// <summary>
        /// The colour a block is drawn in. Paint equal to the material's default (what "no paint" gives)
        /// means the natural colour; real paint mostly wins, with a hint of the material underneath.
        /// Glass keeps its see-through alpha whatever the paint.
        /// </summary>
        public static Color ColorFor(BuildMaterial material, PaintColor paint)
        {
            Color natural = NaturalColor(material);
            Color c = natural;
            if (paint != material.DefaultColor)
            {
                Color p = new Color(paint.R / 255f, paint.G / 255f, paint.B / 255f, 1f);
                c = Color.Lerp(p, natural, 0.15f);
            }
            c.a = material.IsTransparent ? GlassAlpha : 1f;
            return c;
        }

        /// <summary>Shared material for a block look (cached by material id + paint).</summary>
        public static Material MaterialFor(BuildMaterial material, PaintColor paint, out string key)
        {
            key = material.Id + paint.ToHex();
            Material m;
            if (Materials.TryGetValue(key, out m) && m != null)
            {
                return m;
            }
            Color c = ColorFor(material, paint);
            m = material.IsTransparent ? TankMaterials.Tinted(c) : TankMaterials.Solid(c);
            m.name = "Build " + key;
            Materials[key] = m;
            return m;
        }

        /// <summary>Glass for window panes (whatever the frame's material).</summary>
        public static Material PaneGlass(out string key)
        {
            key = "pane";
            Material m;
            if (Materials.TryGetValue(key, out m) && m != null)
            {
                return m;
            }
            m = TankMaterials.Tinted(new Color(0.75f, 0.9f, 1f, GlassAlpha));
            m.name = "Build pane";
            Materials[key] = m;
            return m;
        }

        /// <summary>See-through ghost: green where the block fits, red where something is in the way.</summary>
        public static Material Ghost(bool canPlace)
        {
            if (_ghostOk == null)
            {
                _ghostOk = TankMaterials.Tinted(new Color(0.3f, 1f, 0.3f, 0.35f));
                _ghostOk.name = "Build ghost ok";
            }
            if (_ghostBlocked == null)
            {
                _ghostBlocked = TankMaterials.Tinted(new Color(1f, 0.25f, 0.2f, 0.35f));
                _ghostBlocked.name = "Build ghost blocked";
            }
            return canPlace ? _ghostOk : _ghostBlocked;
        }
    }
}
