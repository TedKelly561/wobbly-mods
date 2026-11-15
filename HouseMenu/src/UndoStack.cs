using System;
using System.Collections.Generic;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Records how to reverse each house change, so the menu's Undo and Reset buttons can restore the house.
    /// No Unity types here, so this file can be tested on its own.
    /// </summary>
    internal sealed class UndoStack
    {
        private sealed class Entry
        {
            public string Description;
            public Action Revert;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public int Count
        {
            get { return _entries.Count; }
        }

        public void Push(string description, Action revert)
        {
            _entries.Add(new Entry { Description = description, Revert = revert });
        }

        /// <summary>Reverts the most recent change. Returns its description, or null if there was nothing to undo.</summary>
        public string UndoLast()
        {
            if (_entries.Count == 0)
            {
                return null;
            }

            int last = _entries.Count - 1;
            Entry entry = _entries[last];
            _entries.RemoveAt(last);
            entry.Revert();
            return entry.Description;
        }

        /// <summary>Reverts every recorded change, newest first. Returns how many were undone.</summary>
        public int UndoAll()
        {
            int count = 0;
            while (UndoLast() != null)
            {
                count++;
            }
            return count;
        }
    }
}
