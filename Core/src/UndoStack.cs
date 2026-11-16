using System;
using System.Collections.Generic;

namespace WobblyMods.Core
{
    /// <summary>
    /// Records how to reverse each change, so a menu's Undo and Reset buttons can put things back.
    /// Each player gets their own UndoStack (see <see cref="PlayerSession"/>), so one person's
    /// undo never removes someone else's work.
    /// </summary>
    public sealed class UndoStack
    {
        private sealed class Entry
        {
            public string Description;
            public Action Revert;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        /// <summary>How many changes can be undone.</summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        /// <summary>Description of the change the next <see cref="UndoLast"/> would revert, or null.</summary>
        public string PeekDescription
        {
            get { return _entries.Count == 0 ? null : _entries[_entries.Count - 1].Description; }
        }

        /// <summary>Records a change and how to reverse it.</summary>
        public void Push(string description, Action revert)
        {
            if (revert == null)
            {
                throw new ArgumentNullException("revert");
            }
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
            return entry.Description ?? string.Empty;
        }

        /// <summary>Reverts every recorded change, newest first. Returns how many were undone.</summary>
        public int UndoAll()
        {
            int count = 0;
            while (_entries.Count > 0)
            {
                UndoLast();
                count++;
            }
            return count;
        }

        /// <summary>Forgets every recorded change without reverting anything.</summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
