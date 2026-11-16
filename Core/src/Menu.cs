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

    /// <summary>A category inside a page. Left/Right moves between a page's tabs.</summary>
    public sealed class MenuTab
    {
        private readonly List<MenuItem> _items = new List<MenuItem>();

        public string Title;

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
    /// Up/Down move focus (wrapping), Left/Right switch tabs (wrapping), Select runs the
    /// focused item or opens its sub-page, Back returns to the previous page or closes
    /// the menu on the first page, ToggleMenu opens/closes.
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

            switch (action)
            {
                case MenuAction.Up:
                    MoveFocus(-1);
                    break;
                case MenuAction.Down:
                    MoveFocus(1);
                    break;
                case MenuAction.Left:
                    MoveTab(-1);
                    break;
                case MenuAction.Right:
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

        /// <summary>Moves focus by delta items, wrapping around the ends.</summary>
        public void MoveFocus(int delta)
        {
            int count = Items.Count;
            if (count == 0)
            {
                return;
            }
            Top.Focus = Wrap(FocusedIndex + delta, count);
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
                return;
            }

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
