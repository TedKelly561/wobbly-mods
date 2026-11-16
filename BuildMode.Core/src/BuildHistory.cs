using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// One undoable change. Actions refer to blocks by id (ids are never reused), so when two
    /// players edit the same build, one player's undo never touches another player's blocks.
    /// If the target was changed by someone else meanwhile, Apply/Revert return Conflict.
    /// </summary>
    internal abstract class BuildAction
    {
        public abstract string Description { get; }

        /// <summary>Do (first time) or redo.</summary>
        public abstract BuildResult Apply(BuildModel model);

        public abstract BuildResult Revert(BuildModel model);
    }

    internal sealed class PlaceAction : BuildAction
    {
        private readonly BlockSpec _spec;
        private int _id;

        public PlaceAction(BlockSpec spec)
        {
            _spec = spec;
        }

        public override string Description
        {
            get { return "Place " + _spec.Type.DisplayName; }
        }

        public override BuildResult Apply(BuildModel model)
        {
            PlacedBlock placed;
            if (_id == 0)
            {
                BuildResult r = model.TryPlace(_spec, out placed);
                if (r == BuildResult.Ok)
                {
                    _id = placed.Id;
                }
                return r;
            }
            BuildResult restored = model.TryRestore(_id, _spec, out placed);
            return restored == BuildResult.Ok ? BuildResult.Ok : BuildResult.Conflict;
        }

        public override BuildResult Revert(BuildModel model)
        {
            // Someone else already removed it: nothing of ours to take away.
            return model.Remove(_id) ? BuildResult.Ok : BuildResult.Conflict;
        }
    }

    internal sealed class RemoveAction : BuildAction
    {
        private readonly int _id;
        private BlockSpec _spec;

        public RemoveAction(int id)
        {
            _id = id;
        }

        public override string Description
        {
            get { return "Remove " + (_spec == null ? "block" : _spec.Type.DisplayName); }
        }

        public override BuildResult Apply(BuildModel model)
        {
            PlacedBlock removed;
            if (!model.Remove(_id, out removed))
            {
                return _spec == null ? BuildResult.NotFound : BuildResult.Conflict;
            }
            _spec = removed.Spec; // the look at removal time, so undo restores any repaint too
            return BuildResult.Ok;
        }

        public override BuildResult Revert(BuildModel model)
        {
            PlacedBlock placed;
            return model.TryRestore(_id, _spec, out placed) == BuildResult.Ok ? BuildResult.Ok : BuildResult.Conflict;
        }
    }

    internal sealed class LookAction : BuildAction
    {
        private readonly int _id;
        private readonly BuildMaterial _newMaterial;
        private readonly PaintColor _newPaint;
        private BuildMaterial _oldMaterial;
        private PaintColor _oldPaint;

        public LookAction(int id, BuildMaterial material, PaintColor paint)
        {
            _id = id;
            _newMaterial = material;
            _newPaint = paint;
        }

        public override string Description
        {
            get { return "Paint"; }
        }

        public override BuildResult Apply(BuildModel model)
        {
            PlacedBlock block = model.GetBlock(_id);
            if (block == null)
            {
                return _oldMaterial == null ? BuildResult.NotFound : BuildResult.Conflict;
            }
            _oldMaterial = block.Spec.Material;
            _oldPaint = block.Spec.Paint;
            return model.SetLook(_id, _newMaterial, _newPaint);
        }

        public override BuildResult Revert(BuildModel model)
        {
            return model.SetLook(_id, _oldMaterial, _oldPaint) == BuildResult.Ok ? BuildResult.Ok : BuildResult.Conflict;
        }
    }

    /// <summary>
    /// Several actions as one undo step (fills, pasting a saved build). Only the parts that
    /// succeeded are reverted, and a redo that partly conflicts keeps the parts that worked.
    /// </summary>
    internal sealed class CompoundAction : BuildAction
    {
        private readonly string _description;
        private readonly List<BuildAction> _parts;
        private readonly List<BuildAction> _applied = new List<BuildAction>();

        public CompoundAction(string description, List<BuildAction> parts)
        {
            _description = description;
            _parts = parts;
        }

        public override string Description
        {
            get { return _description; }
        }

        public int AppliedCount
        {
            get { return _applied.Count; }
        }

        /// <summary>First apply: keeps only the parts that worked, so redo doesn't retry blocked cells.</summary>
        public BuildResult ApplyFirstTime(BuildModel model)
        {
            BuildResult r = Apply(model);
            _parts.Clear();
            _parts.AddRange(_applied);
            return r;
        }

        public override BuildResult Apply(BuildModel model)
        {
            _applied.Clear();
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i].Apply(model) == BuildResult.Ok)
                {
                    _applied.Add(_parts[i]);
                }
            }
            return _applied.Count > 0 ? BuildResult.Ok : BuildResult.Conflict;
        }

        public override BuildResult Revert(BuildModel model)
        {
            int reverted = 0;
            for (int i = _applied.Count - 1; i >= 0; i--)
            {
                if (_applied[i].Revert(model) == BuildResult.Ok)
                {
                    reverted++;
                }
            }
            _applied.Clear();
            return reverted > 0 ? BuildResult.Ok : BuildResult.Conflict;
        }
    }

    /// <summary>One player's undo and redo stacks. Get it from <see cref="BuildEditor.HistoryFor"/>.</summary>
    public sealed class BuildHistory
    {
        private readonly List<BuildAction> _undo = new List<BuildAction>();
        private readonly List<BuildAction> _redo = new List<BuildAction>();

        internal BuildHistory(int limit)
        {
            Limit = limit;
        }

        /// <summary>Oldest steps are forgotten past this many.</summary>
        public int Limit { get; private set; }

        public int UndoCount
        {
            get { return _undo.Count; }
        }

        public int RedoCount
        {
            get { return _redo.Count; }
        }

        /// <summary>What Undo would do next, for the menu ("Undo: Place Wall"), or null.</summary>
        public string NextUndoDescription
        {
            get { return _undo.Count == 0 ? null : _undo[_undo.Count - 1].Description; }
        }

        public string NextRedoDescription
        {
            get { return _redo.Count == 0 ? null : _redo[_redo.Count - 1].Description; }
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        internal void Record(BuildAction action)
        {
            _undo.Add(action);
            if (_undo.Count > Limit)
            {
                _undo.RemoveAt(0);
            }
            _redo.Clear();
        }

        internal BuildResult Undo(BuildModel model, out string description)
        {
            description = null;
            if (_undo.Count == 0)
            {
                return BuildResult.NothingToUndo;
            }
            BuildAction action = Pop(_undo);
            description = action.Description;
            BuildResult r = action.Revert(model);
            if (r == BuildResult.Ok)
            {
                _redo.Add(action);
            }
            // On conflict the step is dropped: the world no longer matches it.
            return r;
        }

        internal BuildResult Redo(BuildModel model, out string description)
        {
            description = null;
            if (_redo.Count == 0)
            {
                return BuildResult.NothingToRedo;
            }
            BuildAction action = Pop(_redo);
            description = action.Description;
            BuildResult r = action.Apply(model);
            if (r == BuildResult.Ok)
            {
                _undo.Add(action);
            }
            return r;
        }

        private static BuildAction Pop(List<BuildAction> stack)
        {
            BuildAction a = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return a;
        }
    }

    /// <summary>Result of an editor call, with a short message for the player's screen.</summary>
    public struct EditResult
    {
        public EditResult(BuildResult result, int blocksChanged, string message)
        {
            Result = result;
            BlocksChanged = blocksChanged;
            Message = message;
        }

        public readonly BuildResult Result;
        public readonly int BlocksChanged;
        public readonly string Message;

        public bool Ok
        {
            get { return Result == BuildResult.Ok; }
        }

        public override string ToString()
        {
            return Result + ": " + Message;
        }
    }

    /// <summary>
    /// The front door for building. Every change made through here is recorded in the acting
    /// player's own history, so split-screen players each undo only their own work.
    /// Player numbers are whatever the Unity layer uses (0 to 3 for split screen).
    /// </summary>
    public sealed class BuildEditor
    {
        public const int DefaultHistoryLimit = 200;

        private readonly Dictionary<int, BuildHistory> _histories = new Dictionary<int, BuildHistory>();
        private readonly int _historyLimit;

        public BuildEditor(BuildModel model) : this(model, DefaultHistoryLimit)
        {
        }

        public BuildEditor(BuildModel model, int historyLimit)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (historyLimit < 1)
            {
                throw new ArgumentOutOfRangeException("historyLimit");
            }
            Model = model;
            _historyLimit = historyLimit;
        }

        public BuildModel Model { get; private set; }

        /// <summary>That player's history (created on first use).</summary>
        public BuildHistory HistoryFor(int player)
        {
            BuildHistory h;
            if (!_histories.TryGetValue(player, out h))
            {
                h = new BuildHistory(_historyLimit);
                _histories.Add(player, h);
            }
            return h;
        }

        /// <summary>Forget every player's history (do this after loading a different build).</summary>
        public void ClearAllHistory()
        {
            foreach (BuildHistory h in _histories.Values)
            {
                h.Clear();
            }
        }

        public EditResult Place(int player, BlockSpec spec)
        {
            if (spec == null)
            {
                return new EditResult(BuildResult.Invalid, 0, "Pick a block first");
            }
            var action = new PlaceAction(spec);
            BuildResult r = action.Apply(Model);
            if (r != BuildResult.Ok)
            {
                return new EditResult(r, 0, MessageFor(r));
            }
            HistoryFor(player).Record(action);
            return new EditResult(r, 1, action.Description);
        }

        /// <summary>Removes the block at a cell (Body layer first, then Floor).</summary>
        public EditResult RemoveAt(int player, GridPos cell)
        {
            PlacedBlock block = Model.GetBlockAt(cell);
            if (block == null)
            {
                return new EditResult(BuildResult.NotFound, 0, MessageFor(BuildResult.NotFound));
            }
            return RemoveBlock(player, block.Id);
        }

        public EditResult RemoveBlock(int player, int blockId)
        {
            var action = new RemoveAction(blockId);
            BuildResult r = action.Apply(Model);
            if (r != BuildResult.Ok)
            {
                return new EditResult(r, 0, MessageFor(r));
            }
            HistoryFor(player).Record(action);
            return new EditResult(r, 1, action.Description);
        }

        /// <summary>Changes material and colour of the block at a cell.</summary>
        public EditResult PaintAt(int player, GridPos cell, BuildMaterial material, PaintColor paint)
        {
            PlacedBlock block = Model.GetBlockAt(cell);
            if (block == null)
            {
                return new EditResult(BuildResult.NotFound, 0, MessageFor(BuildResult.NotFound));
            }
            if (material == null)
            {
                return new EditResult(BuildResult.Invalid, 0, MessageFor(BuildResult.Invalid));
            }
            if (ReferenceEquals(block.Spec.Material, material) && block.Spec.Paint == paint)
            {
                return new EditResult(BuildResult.Ok, 0, "Already that colour");
            }
            var action = new LookAction(block.Id, material, paint);
            BuildResult r = action.Apply(Model);
            if (r != BuildResult.Ok)
            {
                return new EditResult(r, 0, MessageFor(r));
            }
            HistoryFor(player).Record(action);
            return new EditResult(r, 1, action.Description);
        }

        /// <summary>
        /// Fills the box between two corner cells (inclusive, any order) with copies of a block,
        /// stepping by its rotated footprint so multi-cell blocks tile without overlapping.
        /// Cells that are already taken are skipped. One undo step for the whole fill.
        /// </summary>
        public EditResult Fill(int player, BlockType type, BuildMaterial material, PaintColor paint, Rotation rotation, GridPos cornerA, GridPos cornerB)
        {
            if (type == null || material == null)
            {
                return new EditResult(BuildResult.Invalid, 0, MessageFor(BuildResult.Invalid));
            }
            GridPos min = new GridPos(Math.Min(cornerA.X, cornerB.X), Math.Min(cornerA.Y, cornerB.Y), Math.Min(cornerA.Z, cornerB.Z));
            GridPos max = new GridPos(Math.Max(cornerA.X, cornerB.X), Math.Max(cornerA.Y, cornerB.Y), Math.Max(cornerA.Z, cornerB.Z));

            // Rotated footprint extent, and the offset from the footprint's min corner to its anchor.
            GridPos fMin, fMax;
            Footprint.GetBounds(type, GridPos.Zero, rotation, out fMin, out fMax);
            int stepX = fMax.X - fMin.X + 1;
            int stepY = fMax.Y - fMin.Y + 1;
            int stepZ = fMax.Z - fMin.Z + 1;

            long volume = (long)(max.X - min.X + 1) * (max.Y - min.Y + 1) * (max.Z - min.Z + 1);
            if (volume / ((long)stepX * stepY * stepZ) > Model.MaxBlocks - Model.Count)
            {
                return new EditResult(BuildResult.LimitReached, 0, MessageFor(BuildResult.LimitReached));
            }

            var parts = new List<BuildAction>();
            for (int y = min.Y; y + stepY - 1 <= max.Y; y += stepY)
            {
                for (int z = min.Z; z + stepZ - 1 <= max.Z; z += stepZ)
                {
                    for (int x = min.X; x + stepX - 1 <= max.X; x += stepX)
                    {
                        GridPos anchor = new GridPos(x, y, z) - fMin;
                        parts.Add(new PlaceAction(new BlockSpec(type, material, paint, anchor, rotation)));
                    }
                }
            }
            return RecordCompound(player, "Fill " + type.DisplayName, parts);
        }

        /// <summary>
        /// Places a list of blocks (e.g. a saved build loaded at an offset) as one undo step.
        /// Blocks that would overlap are skipped.
        /// </summary>
        public EditResult PlaceMany(int player, IEnumerable<BlockSpec> specs, string description)
        {
            var parts = new List<BuildAction>();
            foreach (BlockSpec spec in specs)
            {
                if (spec != null)
                {
                    parts.Add(new PlaceAction(spec));
                }
            }
            return RecordCompound(player, description ?? "Place blocks", parts);
        }

        /// <summary>
        /// Removes every block in the build as ONE undo step for this player (a "clear build" button
        /// that can still be undone). Blocks keep their ids, so undo puts them all back and other
        /// players' histories keep working. Returns NotFound when the build is already empty.
        /// </summary>
        public EditResult RemoveAll(int player, string description)
        {
            List<PlacedBlock> blocks = Model.BlocksInPlacementOrder();
            if (blocks.Count == 0)
            {
                return new EditResult(BuildResult.NotFound, 0, MessageFor(BuildResult.NotFound));
            }
            var parts = new List<BuildAction>(blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
            {
                parts.Add(new RemoveAction(blocks[i].Id));
            }
            return RecordCompound(player, description ?? "Clear build", parts);
        }

        public EditResult Undo(int player)
        {
            string description;
            BuildResult r = HistoryFor(player).Undo(Model, out description);
            return new EditResult(r, r == BuildResult.Ok ? 1 : 0,
                r == BuildResult.Ok ? "Undo: " + description : MessageFor(r));
        }

        public EditResult Redo(int player)
        {
            string description;
            BuildResult r = HistoryFor(player).Redo(Model, out description);
            return new EditResult(r, r == BuildResult.Ok ? 1 : 0,
                r == BuildResult.Ok ? "Redo: " + description : MessageFor(r));
        }

        private EditResult RecordCompound(int player, string description, List<BuildAction> parts)
        {
            if (parts.Count == 0)
            {
                return new EditResult(BuildResult.Invalid, 0, "Nothing to place");
            }
            var compound = new CompoundAction(description, parts);
            BuildResult r = compound.ApplyFirstTime(Model);
            if (r != BuildResult.Ok)
            {
                return new EditResult(BuildResult.Overlap, 0, MessageFor(BuildResult.Overlap));
            }
            HistoryFor(player).Record(compound);
            return new EditResult(r, compound.AppliedCount, description);
        }

        /// <summary>Short, friendly messages. Kids read these, so keep them simple.</summary>
        public static string MessageFor(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Ok: return "Done";
                case BuildResult.Overlap: return "Something is in the way";
                case BuildResult.OutOfBounds: return "Too high or too low";
                case BuildResult.LimitReached: return "The build is full";
                case BuildResult.NotFound: return "Nothing there";
                case BuildResult.NothingToUndo: return "Nothing to undo";
                case BuildResult.NothingToRedo: return "Nothing to redo";
                case BuildResult.Conflict: return "Someone else changed that";
                default: return "Can't do that";
            }
        }
    }
}
