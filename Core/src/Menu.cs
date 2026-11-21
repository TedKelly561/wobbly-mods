using System;
using System.Collections.Generic;

namespace WobblyMods.Core
{
    /// <summary>
    /// One button in a menu. It either runs an action or opens a sub-page.
    /// Pure data: drawing is done by each mod's renderer.
    /// </summary>
    public sealed class MenuItem
    {
        /// <summary>Text on the button. Keep it short; the players may not read yet.</summary>
        public string Label;

        /// <summary>Optional picture key for the renderer (for example a catalog id). May be null.</summary>
        public string Icon;

        /// <summary>Runs when the item is selected. Null for a sub-page item.</summary>
        public Action OnSelect;

        /// <summary>Opened when the item is selected. Null for an action item.</summary>
        public MenuPage SubPage;

        /// <summary>Close the whole menu after <see cref="OnSelect"/> runs.</summary>
        public bool ClosesMenu;

        public MenuItem(string label, Action onSelect)
        {
            Label = label;
            OnSelect = onSelect;
        }

        public MenuItem(string label, MenuPage subPage)
        {
            Label = label;
            SubPage = subPage;
        }
    }

    /// <summary>What just happened in a menu, so a renderer can give feedback (for example a sound).</summary>
    public enum MenuFeedback
    {
        /// <summary>Focus moved to another item.</summary>
        Moved,
        /// <summary>Another tab is showing.</summary>
        TabChanged,
        /// <summary>An action item was picked (raised just before its action runs).</summary>
        Selected,
        /// <summary>A sub-page opened.</summary>
        PageOpened,
        /// <summary>Went back from a sub-page to the page before.</summary>
        PageBack,
        /// <summary>The menu opened.</summary>
        Opened,
        /// <summary>The menu closed.</summary>
        Closed
    }

    /// <summary>
    /// A category inside a page. Shown either as a plain list (<see cref="Columns"/> = 1, the default)
    /// or as a grid of picture tiles (<see cref="Columns"/> above 1, see <see cref="AsGrid"/>).
    /// TabPrev/TabNext (LB/RB taps) switch tabs; on a plain list Left/Right also switch tabs.
    /// </summary>
    public sealed class MenuTab
    {
        /// <summary>Columns used by <see cref="AsGrid"/> when none are given.</summary>
        public const int DefaultGridColumns = 3;

        private readonly List<MenuItem> _items = new List<MenuItem>();
        private int _columns = 1;

        public string Title;

        /// <summary>Optional picture key for the tab button (like <see cref="MenuItem.Icon"/>). May be null.</summary>
        public string Icon;

        /// <summary>
        /// 1 = plain list (Up/Down move, Left/Right switch tabs). More than 1 = grid with that many
        /// columns (Up/Down move a row, Left/Right move along the row). Values below 1 count as 1.
        /// </summary>
        public int Columns
        {
            get { return _columns; }
            set { _columns = value < 1 ? 1 : value; }
        }

        /// <summary>True when the tab is shown as a grid (more than one column).</summary>
        public bool IsGrid
        {
            get { return _columns > 1; }
        }

        /// <summary>Shows this tab as a grid. Returns the tab so calls can be chained.</summary>
        public MenuTab AsGrid(int columns = DefaultGridColumns)
        {
            Columns = columns;
            return this;
        }

        public MenuTab(string title)
        {
            Title = title;
        }

        public IList<MenuItem> Items
        {
            get { return _items; }
        }

        /// <summary>Adds an action item. Returns it so callers can set Icon or ClosesMenu.</summary>
        public MenuItem Add(string label, Action onSelect)
        {
            var item = new MenuItem(label, onSelect);
            _items.Add(item);
            return item;
        }

        /// <summary>Adds an item that opens a sub-page.</summary>
        public MenuItem AddSubPage(string label, MenuPage subPage)
        {
            var item = new MenuItem(label, subPage);
            _items.Add(item);
            return item;
        }
    }

    /// <summary>
    /// A screen of the menu: one or more tabs, each with a list of items.
    /// A page with one tab is a plain list (no tab row needs drawing).
    /// </summary>
    public sealed class MenuPage
    {
        private readonly List<MenuTab> _tabs = new List<MenuTab>();

        public string Title;

        public MenuPage(string title)
        {
            Title = title;
        }

        public IList<MenuTab> Tabs
        {
            get { return _tabs; }
        }

        public MenuTab AddTab(string title)
        {
            var tab = new MenuTab(title);
            _tabs.Add(tab);
            return tab;
        }

        /// <summary>The first tab, created if the page has none. Handy for single-list pages.</summary>
        public MenuTab MainTab()
        {
            return _tabs.Count > 0 ? _tabs[0] : AddTab(Title);
        }
    }

    /// <summary>
    /// The controller-navigable state of one player's menu: open/closed, which page,
    /// which tab and which item has focus. No drawing here.
    ///
    /// TabPrev/TabNext switch tabs (wrapping), Select runs the focused item or opens its sub-page,
    /// Back returns to the previous page or closes the menu on the first page, ToggleMenu opens/closes.
    ///
    /// On a plain list tab (Columns = 1): Up/Down move focus (wrapping), Left/Right switch tabs (wrapping),
    /// exactly as before grids existed.
    ///
    /// On a grid tab (Columns &gt; 1), items fill rows left to right:
    /// - Left/Right step to the previous/next item in reading order: past the end of a row they continue on
    ///   the next row, and the last item wraps to the first (so pushing one way visits everything).
    ///   They never switch tabs on a grid.
    /// - Up/Down move to the item above/below in the same column, wrapping from the bottom of that column
    ///   to its top. A column that's shorter (the last row is partial) wraps sooner. A grid with a single
    ///   row ignores Up/Down.
    /// </summary>
    public sealed class MenuModel
    {
        private sealed class Frame
        {
            public MenuPage Page;
            public int Tab;
            public int Focus;
        }

        private readonly List<Frame> _stack = new List<Frame>();
        private bool _open;

        public MenuModel(MenuPage root)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }
            _stack.Add(new Frame { Page = root });
        }

        /// <summary>Raised after the menu opens or closes. The argument is the new IsOpen.</summary>
        public event Action<bool> OpenChanged;

        /// <summary>
        /// Raised when something visible happens (focus moved, tab changed, item picked, page opened...).
        /// The item is the one involved (the newly focused or picked item), or null. Only raised when something
        /// actually changed, so a press that does nothing gives no feedback.
        /// </summary>
        public event Action<MenuFeedback, MenuItem> Feedback;

        public bool IsOpen
        {
            get { return _open; }
        }

        public MenuPage RootPage
        {
            get { return _stack[0].Page; }
        }

        public MenuPage CurrentPage
        {
            get { return Top.Page; }
        }

        /// <summary>1 on the first page, 2 inside a sub-page, and so on.</summary>
        public int Depth
        {
            get { return _stack.Count; }
        }

        public int TabIndex
        {
            get { return Top.Tab; }
        }

        /// <summary>The current tab, or null if the page has no tabs.</summary>
        public MenuTab CurrentTab
        {
            get
            {
                IList<MenuTab> tabs = Top.Page.Tabs;
                return tabs.Count == 0 ? null : tabs[Clamp(Top.Tab, tabs.Count)];
            }
        }

        /// <summary>The first page's current tab (the tab a sub-page was opened from), or null.</summary>
        public MenuTab RootTab
        {
            get
            {
                Frame root = _stack[0];
                IList<MenuTab> tabs = root.Page.Tabs;
                return tabs.Count == 0 ? null : tabs[Clamp(root.Tab, tabs.Count)];
            }
        }

        /// <summary>Columns of the current tab: 1 for a plain list (or no tab).</summary>
        public int Columns
        {
            get
            {
                MenuTab tab = CurrentTab;
                return tab == null ? 1 : tab.Columns;
            }
        }

        /// <summary>Items in the current tab (empty if none).</summary>
        public IList<MenuItem> Items
        {
            get
            {
                MenuTab tab = CurrentTab;
                return tab == null ? (IList<MenuItem>)new MenuItem[0] : tab.Items;
            }
        }

        /// <summary>Index of the focused item in <see cref="Items"/>, or -1 when the tab is empty.</summary>
        public int FocusedIndex
        {
            get
            {
                int count = Items.Count;
                return count == 0 ? -1 : Clamp(Top.Focus, count);
            }
        }

        public MenuItem FocusedItem
        {
            get
            {
                int i = FocusedIndex;
                return i < 0 ? null : Items[i];
            }
        }

        private Frame Top
        {
            get { return _stack[_stack.Count - 1]; }
        }

        /// <summary>Opens the menu on its first page. The first page keeps its last tab and focus.</summary>
        public void Open()
        {
            if (_open)
            {
                return;
            }
            PopToRoot();
            SetOpen(true);
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }
            PopToRoot();
            SetOpen(false);
        }

        public void Toggle()
        {
            if (_open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Applies one action. Returns true if the menu used it (so the game should ignore it).
        /// While closed, only ToggleMenu is used.
        /// </summary>
        public bool Handle(MenuAction action)
        {
            if (action == MenuAction.ToggleMenu)
            {
                Toggle();
                return true;
            }

            if (!_open)
            {
                return false;
            }

            bool grid = Columns > 1;
            switch (action)
            {
                case MenuAction.Up:
                    if (grid) MoveRow(-1); else MoveFocus(-1);
                    break;
                case MenuAction.Down:
                    if (grid) MoveRow(1); else MoveFocus(1);
                    break;
                case MenuAction.Left:
                    if (grid) MoveFocus(-1); else MoveTab(-1);
                    break;
                case MenuAction.Right:
                    if (grid) MoveFocus(1); else MoveTab(1);
                    break;
                case MenuAction.TabPrev:
                    MoveTab(-1);
                    break;
                case MenuAction.TabNext:
                    MoveTab(1);
                    break;
                case MenuAction.Select:
                    SelectFocused();
                    break;
                case MenuAction.Back:
                    Back();
                    break;
            }
            return true;
        }

        /// <summary>Moves focus by delta items (reading order on a grid), wrapping around the ends.</summary>
        public void MoveFocus(int delta)
        {
            int count = Items.Count;
            if (count == 0)
            {
                return;
            }
            SetFocus(Wrap(FocusedIndex + delta, count));
        }

        /// <summary>
        /// Grid move: delta rows up (negative) or down (positive), staying in the same column and wrapping
        /// within that column. On a plain list this is the same as <see cref="MoveFocus"/>.
        /// </summary>
        public void MoveRow(int delta)
        {
            int count = Items.Count;
            if (count == 0)
            {
                return;
            }
            int columns = Columns;
            if (columns <= 1)
            {
                MoveFocus(delta);
                return;
            }

            int focus = FocusedIndex;
            int column = focus % columns;
            int row = focus / columns;
            // Rows that have an item in this column (the last row may be partial).
            int rowsInColumn = (count - 1 - column) / columns + 1;
            if (rowsInColumn <= 1)
            {
                return;
            }
            SetFocus(Wrap(row + delta, rowsInColumn) * columns + column);
        }

        /// <summary>Switches tab by delta, wrapping. Focus goes to the new tab's first item.</summary>
        public void MoveTab(int delta)
        {
            int count = Top.Page.Tabs.Count;
            if (count <= 1)
            {
                return;
            }
            Top.Tab = Wrap(Clamp(Top.Tab, count) + delta, count);
            Top.Focus = 0;
            Raise(MenuFeedback.TabChanged, FocusedItem);
        }

        /// <summary>Jumps to a tab (for mouse clicks on the tab row).</summary>
        public void SelectTab(int index)
        {
            int count = Top.Page.Tabs.Count;
            if (index < 0 || index >= count || index == Top.Tab)
            {
                return;
            }
            Top.Tab = index;
            Top.Focus = 0;
            Raise(MenuFeedback.TabChanged, FocusedItem);
        }

        /// <summary>Focuses an item and selects it (for mouse clicks on a button).</summary>
        public void Activate(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                return;
            }
            Top.Focus = index;
            SelectFocused();
        }

        private void SetFocus(int index)
        {
            if (index == FocusedIndex)
            {
                return;
            }
            Top.Focus = index;
            Raise(MenuFeedback.Moved, FocusedItem);
        }

        /// <summary>Runs the focused item, or opens its sub-page.</summary>
        public void SelectFocused()
        {
            MenuItem item = FocusedItem;
            if (item == null)
            {
                return;
            }

            if (item.SubPage != null)
            {
                _stack.Add(new Frame { Page = item.SubPage });
                Raise(MenuFeedback.PageOpened, item);
                return;
            }

            Raise(MenuFeedback.Selected, item);
            if (item.OnSelect != null)
            {
                item.OnSelect();
            }

            if (item.ClosesMenu)
            {
                Close();
            }
        }

        /// <summary>Goes back one page, or closes the menu on the first page.</summary>
        public void Back()
        {
            if (_stack.Count > 1)
            {
                _stack.RemoveAt(_stack.Count - 1);
                Raise(MenuFeedback.PageBack, FocusedItem);
            }
            else
            {
                Close();
            }
        }

        private void PopToRoot()
        {
            if (_stack.Count > 1)
            {
                _stack.RemoveRange(1, _stack.Count - 1);
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;
            Action<bool> handler = OpenChanged;
            if (handler != null)
            {
                handler(open);
            }
            Raise(open ? MenuFeedback.Opened : MenuFeedback.Closed, null);
        }

        private void Raise(MenuFeedback feedback, MenuItem item)
        {
            Action<MenuFeedback, MenuItem> handler = Feedback;
            if (handler != null)
            {
                handler(feedback, item);
            }
        }

        private static int Wrap(int value, int count)
        {
            int r = value % count;
            return r < 0 ? r + count : r;
        }

        private static int Clamp(int value, int count)
        {
            if (value < 0) return 0;
            if (value >= count) return count - 1;
            return value;
        }
    }
}
