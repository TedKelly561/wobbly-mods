using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>
    /// Remembers which block each solid part of the drawn build belongs to, so "remove the piece I'm
    /// looking at" can ask the thing the aim ray actually hit instead of guessing a cell from the hit
    /// point. (Guessing went wrong: a floor and a wall share a cell, and thin floors and roofs sit at the
    /// edge of theirs, so looking at the floor by a wall removed the wall.)
    ///
    /// TKey is whatever names a solid part in the game layer (the Unity layer uses the collider's
    /// instance id). Entries are grouped by chunk, because the renderer rebuilds one chunk at a time.
    /// </summary>
    public sealed class BlockPickMap<TKey>
    {
        private readonly Dictionary<TKey, int> _blocks = new Dictionary<TKey, int>();
        private readonly Dictionary<ChunkPos, List<TKey>> _byChunk = new Dictionary<ChunkPos, List<TKey>>();

        /// <summary>How many solid parts are known.</summary>
        public int Count
        {
            get { return _blocks.Count; }
        }

        /// <summary>Notes that a solid part drawn by this chunk belongs to this block.</summary>
        public void Add(ChunkPos chunk, TKey part, int blockId)
        {
            List<TKey> parts;
            if (!_byChunk.TryGetValue(chunk, out parts))
            {
                parts = new List<TKey>();
                _byChunk.Add(chunk, parts);
            }
            if (!_blocks.ContainsKey(part))
            {
                parts.Add(part);
            }
            _blocks[part] = blockId;
        }

        /// <summary>Forgets every part of one chunk (call before the chunk is redrawn or when it empties).</summary>
        public void RemoveChunk(ChunkPos chunk)
        {
            List<TKey> parts;
            if (!_byChunk.TryGetValue(chunk, out parts))
            {
                return;
            }
            for (int i = 0; i < parts.Count; i++)
            {
                _blocks.Remove(parts[i]);
            }
            _byChunk.Remove(chunk);
        }

        /// <summary>Forgets everything (the build was replaced, or the scene took the drawn objects away).</summary>
        public void Clear()
        {
            _blocks.Clear();
            _byChunk.Clear();
        }

        /// <summary>The block a solid part belongs to. False for anything that isn't part of the build.</summary>
        public bool TryGetBlock(TKey part, out int blockId)
        {
            return _blocks.TryGetValue(part, out blockId);
        }

        /// <summary>
        /// From the things an aim ray went through, nearest first: the first one that is a build piece
        /// still in the model. Things that aren't build pieces (the ground, a prop, the player's own
        /// body) are skipped, and so is a part whose block was removed a moment ago and not redrawn yet.
        /// </summary>
        public bool FirstBlock(IList<TKey> hitsNearestFirst, BuildModel model, out int blockId)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            blockId = 0;
            if (hitsNearestFirst == null)
            {
                return false;
            }
            for (int i = 0; i < hitsNearestFirst.Count; i++)
            {
                int id;
                if (_blocks.TryGetValue(hitsNearestFirst[i], out id) && model.GetBlock(id) != null)
                {
                    blockId = id;
                    return true;
                }
            }
            return false;
        }
    }
}
