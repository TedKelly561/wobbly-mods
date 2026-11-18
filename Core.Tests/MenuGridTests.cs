using System.Collections.Generic;
using Xunit;

namespace WobblyMods.Core.Tests
{
    /// <summary>Grid navigation (picture tiles, 3 across), TabPrev/TabNext, and feedback events.</summary>
    public class MenuGridTests
    {
        /// <summary>
        /// Root page with tabs "Grid" (count items in a grid of the given columns), "List" (A, B) and "Second grid" (X).
        /// Layout for 8 items, 3 columns:
        ///   0 1 2
        ///   3 4 5
        ///   6 7
        /// </summary>
        private static MenuModel Build(int count, int columns = 3)
        {
            var root = new MenuPage("House");
            MenuTab grid = root.AddTab("Grid").AsGrid(columns);
            for (int i = 0; i < count; i++)
            {
                grid.Add("Item " + i, () => { });
            }
            MenuTab list = root.AddTab("List");
            list.Add("A", () => { });
            list.Add("B", () => { });
            root.AddTab("Second grid").AsGrid().Add("X", () => { });
            var menu = new MenuModel(root);
            menu.Open();
            return menu;
        }

        private static void Press(MenuModel menu, params MenuAction[] actions)
        {
            foreach (MenuAction a in actions) menu.Handle(a);
        }

        [Fact]
        public void Tabs_DefaultToPlainList_AndAsGridDefaultsToThreeColumns()
        {
            var tab = new MenuTab("t");
            Assert.Equal(1, tab.Columns);
            Assert.False(tab.IsGrid);
            Assert.Same(tab, tab.AsGrid());
            Assert.Equal(3, tab.Columns);
            Assert.True(tab.IsGrid);
            tab.Columns = 0;
            Assert.Equal(1, tab.Columns);
        }

        [Fact]
        public void DownAndUp_MoveByARow()
        {
            MenuModel menu = Build(8);
            Assert.Equal(3, menu.Columns);
            Press(menu, MenuAction.Right); // 1
            Press(menu, MenuAction.Down);
            Assert.Equal(4, menu.FocusedIndex);
            Press(menu, MenuAction.Down);
            Assert.Equal(7, menu.FocusedIndex);
            Press(menu, MenuAction.Up);
            Assert.Equal(4, menu.FocusedIndex);
        }

        [Fact]
        public void DownAndUp_WrapWithinTheColumn()
        {
            MenuModel menu = Build(8);
            Press(menu, MenuAction.Up); // from 0 to the bottom of column 0
            Assert.Equal(6, menu.FocusedIndex);
            Press(menu, MenuAction.Down); // past the bottom back to the top
            Assert.Equal(0, menu.FocusedIndex);
        }

        [Fact]
        public void PartialLastRow_ShortColumnWrapsSooner()
        {
            MenuModel menu = Build(8);
            Press(menu, MenuAction.Right, MenuAction.Right); // 2 (column 2 has only rows 0 and 1)
            Press(menu, MenuAction.Down);
            Assert.Equal(5, menu.FocusedIndex);
            Press(menu, MenuAction.Down); // no item 8: wraps to the top of column 2
            Assert.Equal(2, menu.FocusedIndex);
            Press(menu, MenuAction.Up);  // up from the top goes to the bottom of column 2, not past the end
            Assert.Equal(5, menu.FocusedIndex);
        }

        [Fact]
        public void LeftAndRight_StepInReadingOrder_AcrossRowsAndAroundTheEnds()
        {
            MenuModel menu = Build(8);
            Press(menu, MenuAction.Right, MenuAction.Right, MenuAction.Right);
            Assert.Equal(3, menu.FocusedIndex); // end of row 0 continues on row 1
            Press(menu, MenuAction.Left);
            Assert.Equal(2, menu.FocusedIndex);
            Press(menu, MenuAction.Left, MenuAction.Left, MenuAction.Left);
            Assert.Equal(7, menu.FocusedIndex); // before the first item is the last one
            Press(menu, MenuAction.Right);
            Assert.Equal(0, menu.FocusedIndex);
            Assert.Equal("Grid", menu.CurrentTab.Title); // never switched tabs
        }

        [Fact]
        public void SingleRowGrid_IgnoresUpDown()
        {
            MenuModel menu = Build(2);
            Press(menu, MenuAction.Right);
            Press(menu, MenuAction.Down);
            Assert.Equal(1, menu.FocusedIndex);
            Press(menu, MenuAction.Up);
            Assert.Equal(1, menu.FocusedIndex);
        }

        [Fact]
        public void EmptyGrid_NavigationDoesNothing()
        {
            MenuModel menu = Build(0);
            Press(menu, MenuAction.Down, MenuAction.Right, MenuAction.Up, MenuAction.Left);
            Assert.Equal(-1, menu.FocusedIndex);
            Assert.Equal("Grid", menu.CurrentTab.Title);
        }

        [Fact]
        public void TabPrevAndNext_SwitchTabs_WrapAndResetFocus()
        {
            MenuModel menu = Build(8);
            Press(menu, MenuAction.Down);
            Assert.True(menu.Handle(MenuAction.TabNext));
            Assert.Equal("List", menu.CurrentTab.Title);
            Assert.Equal(0, menu.FocusedIndex);
            Press(menu, MenuAction.TabNext);
            Assert.Equal("Second grid", menu.CurrentTab.Title);
            Press(menu, MenuAction.TabNext);
            Assert.Equal("Grid", menu.CurrentTab.Title);
            Press(menu, MenuAction.TabPrev);
            Assert.Equal("Second grid", menu.CurrentTab.Title);
        }

        [Fact]
        public void TabActions_IgnoredWhileClosed()
        {
            MenuModel menu = Build(8);
            menu.Close();
            Assert.False(menu.Handle(MenuAction.TabNext));
            Assert.False(menu.Handle(MenuAction.TabPrev));
            menu.Open();
            Assert.Equal("Grid", menu.CurrentTab.Title);
        }

        [Fact]
        public void ListTab_KeepsLeftRightForTabs()
        {
            MenuModel menu = Build(8);
            Press(menu, MenuAction.TabNext); // "List"
            Assert.Equal(1, menu.Columns);
            Press(menu, MenuAction.Down);
            Assert.Equal("B", menu.FocusedItem.Label);
            Press(menu, MenuAction.Right);
            Assert.Equal("Second grid", menu.CurrentTab.Title);
        }

        [Fact]
        public void SubPage_UsesItsOwnColumns_AndRootTabStaysTheOpeningTab()
        {
            var root = new MenuPage("House");
            MenuTab grid = root.AddTab("Change").AsGrid();
            var confirm = new MenuPage("Sure?");
            confirm.MainTab().AsGrid(2);
            confirm.MainTab().Add("No", () => { });
            confirm.MainTab().Add("Yes", () => { });
            grid.AddSubPage("Reset", confirm);
            var menu = new MenuModel(root);
            menu.Open();
            menu.Handle(MenuAction.Select);
            Assert.Equal(2, menu.Columns);
            Assert.Equal("Change", menu.RootTab.Title);
            menu.Handle(MenuAction.Right);
            Assert.Equal("Yes", menu.FocusedItem.Label);
            menu.Handle(MenuAction.Right);
            Assert.Equal("No", menu.FocusedItem.Label);
        }

        [Fact]
        public void Feedback_ReportsOnlyRealChanges()
        {
            MenuModel menu = Build(2);
            var seen = new List<MenuFeedback>();
            menu.Feedback += (f, item) => seen.Add(f);

            menu.Handle(MenuAction.Down);     // single row: nothing happens, no feedback
            menu.Handle(MenuAction.Right);    // Moved
            menu.Handle(MenuAction.TabNext);  // TabChanged
            menu.Handle(MenuAction.Select);   // Selected
            menu.Handle(MenuAction.Back);     // Closed
            menu.Handle(MenuAction.ToggleMenu); // Opened
            Assert.Equal(new[]
            {
                MenuFeedback.Moved, MenuFeedback.TabChanged, MenuFeedback.Selected,
                MenuFeedback.Closed, MenuFeedback.Opened
            }, seen);
        }

        [Fact]
        public void Feedback_PageOpenAndBack_CarryTheItem()
        {
            var root = new MenuPage("x");
            var sub = new MenuPage("sub");
            sub.MainTab().Add("Inside", () => { });
            MenuItem opener = root.MainTab().AddSubPage("More", sub);
            var menu = new MenuModel(root);
            menu.Open();
            var seen = new List<KeyValuePair<MenuFeedback, MenuItem>>();
            menu.Feedback += (f, item) => seen.Add(new KeyValuePair<MenuFeedback, MenuItem>(f, item));

            menu.Handle(MenuAction.Select);
            menu.Handle(MenuAction.Back);
            Assert.Equal(2, seen.Count);
            Assert.Equal(MenuFeedback.PageOpened, seen[0].Key);
            Assert.Same(opener, seen[0].Value);
            Assert.Equal(MenuFeedback.PageBack, seen[1].Key);
            Assert.Same(opener, seen[1].Value);
        }
    }
}
