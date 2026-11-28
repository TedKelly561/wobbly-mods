using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// One split-screen player's build-mode selection: chosen block, material, paint, rotation and
    /// cursor cell. Designed for controllers: every method is one button or stick press.
    /// Keep one per player next to that player's camera; pass <see cref="PlayerIndex"/> to <see cref="BuildEditor"/>.
    /// </summary>
    public sealed class PlayerBuildState
    {
        private readonly BuildCatalog _catalog;
        private readonly PaintColor[] _palette;
        private int _blockIndex;
        private int _materialIndex;
        private int _paintIndex = -1; // -1 = material's default colour

        public PlayerBuildState(int playerIndex, BuildCatalog catalog) : this(playerIndex, catalog, null)
        {
        }

        /// <summary>
        /// Same, with the player's own paint palette (e.g. favourite colours first). Null or empty uses
        /// <see cref="PaintColor.Palette"/>. The list is copied, so later changes to it have no effect.
        /// </summary>
        public PlayerBuildState(int playerIndex, BuildCatalog catalog, IList<PaintColor> palette)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }
            if (catalog.Blocks.Count == 0 || catalog.Materials.Count == 0)
            {
                throw new ArgumentException("Catalog needs at least one block and one material.");
            }
            PlayerIndex = playerIndex;
            _catalog = catalog;
            if (palette == null || palette.Count == 0)
            {
                _palette = (PaintColor[])PaintColor.Palette.Clone();
            }
            else
            {
                _palette = new PaintColor[palette.Count];
                palette.CopyTo(_palette, 0);
            }
        }

        public int PlayerIndex { get; private set; }

        /// <summary>The cell the ghost preview sits in. The Unity layer usually sets this from an aim raycast.</summary>
        public GridPos Cursor { get; set; }

        public Rotation Rotation { get; set; }

        public BlockType Block
        {
            get { return _catalog.Blocks[_blockIndex]; }
        }

        public BuildMaterial Material
        {
            get { return _catalog.Materials[_materialIndex]; }
        }

        /// <summary>The chosen paint, or the material's default if none chosen.</summary>
        public PaintColor Paint
        {
            get { return _paintIndex < 0 ? Material.DefaultColor : _palette[_paintIndex]; }
        }

        /// <summary>The paint colours <see cref="CyclePaint"/> steps through (after "material default").</summary>
        public IList<PaintColor> Palette
        {
            get { return Array.AsReadOnly(_palette); }
        }

        /// <summary>Index into <see cref="Palette"/> of the chosen paint, or -1 for the material's default colour.</summary>
        public int PaintIndex
        {
            get { return _paintIndex; }
        }

        /// <summary>Selects by id (e.g. from a picture menu). Returns false if the id is unknown.</summary>
        public bool SelectBlock(string id)
        {
            int i = _catalog.Blocks.IndexOf(_catalog.GetBlock(id));
            if (i < 0)
            {
                return false;
            }
            _blockIndex = i;
            return true;
        }

        public bool SelectMaterial(string id)
        {
            int i = _catalog.Materials.IndexOf(_catalog.GetMaterial(id));
            if (i < 0)
            {
                return false;
            }
            _materialIndex = i;
            return true;
        }

        /// <summary>Next/previous block (bumpers). Wraps around.</summary>
        public void CycleBlock(int step)
        {
            _blockIndex = Wrap(_blockIndex + step, _catalog.Blocks.Count);
        }

        public void CycleMaterial(int step)
        {
            _materialIndex = Wrap(_materialIndex + step, _catalog.Materials.Count);
        }

        /// <summary>Cycles through "material default" then each palette colour.</summary>
        public void CyclePaint(int step)
        {
            int slots = _palette.Length + 1;
            _paintIndex = Wrap(_paintIndex + 1 + step, slots) - 1;
        }

        public void RotateClockwise()
        {
            Rotation = Rotation.Clockwise();
        }

        public void RotateCounterClockwise()
        {
            Rotation = Rotation.CounterClockwise();
        }

        /// <summary>
        /// Moves the cursor relative to where the camera faces (D-pad): forward/right steps are
        /// turned by the camera yaw snapped to 90 degrees, so "up" always moves away from the player.
        /// </summary>
        public void MoveCursor(int rightSteps, int upSteps, int forwardSteps, float cameraYawDegrees)
        {
            Rotation facing = Rotation.FromDegrees(cameraYawDegrees);
            GridPos local = new GridPos(rightSteps, upSteps, forwardSteps);
            Cursor = Cursor + facing.Apply(local);
        }

        /// <summary>What would be placed at the cursor right now.</summary>
        public BlockSpec CurrentSpec()
        {
            return new BlockSpec(Block, Material, Paint, Cursor, Rotation);
        }

        private static int Wrap(int value, int count)
        {
            int m = value % count;
            return m < 0 ? m + count : m;
        }
    }
}
