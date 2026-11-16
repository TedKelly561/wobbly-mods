using System.Collections.Generic;
using Xunit;

namespace WobblyMods.Core.Tests
{
    public class MenuModelTests
    {
        private readonly List<string> _log = new List<string>();

        /// <summary>Root page: tabs "Furniture" (Couch, Lamp, Table, More >), "Toys" (Ball), "Empty". "More" opens a page with "Giant".</summary>
        private MenuModel Build()
        {
            var root = new MenuPage("House");
            MenuTab furniture = root.AddTab("Furniture");
            furniture.Add("Couch", () => _log.Add("couch"));
            furniture.Add("Lamp", () => _log.Add("lamp"));
            furniture.Add("Table", () => _log.Add("table"));
            var more = new MenuPage("More");
            more.MainTab().Add("Giant", () => _log.Add("giant"));
            furniture.AddSubPage("More", more);
            root.AddTab("Toys").Add("Ball", () => _log.Add("ball"));
            root.AddTab("Empty");
            var menu = new MenuModel(root);
            menu.Open();
            return menu;
        }

        [Fact]
        public void StartsClosed_AndIgnoresNavigationWhileClosed()
        {
            var menu = new MenuModel(new MenuPage("x"));
            Assert.False(menu.IsOpen);
            Assert.False(menu.Handle(MenuAction.Down));
            Assert.False(menu.Handle(MenuAction.Select));
            Assert.True(menu.Handle(MenuAction.ToggleMenu));
            Assert.True(menu.IsOpen);
            Assert.True(menu.Handle(MenuAction.ToggleMenu));
            Assert.False(menu.IsOpen);
        }

        [Fact]
        public void DownAndUp_WrapAround()
        {
            MenuModel menu = Build();
            Assert.Equal(0, menu.FocusedIndex);
            menu.Handle(MenuAction.Up);
            Assert.Equal(3, menu.FocusedIndex);
            Assert.Equal("More", menu.FocusedItem.Label);
            menu.Handle(MenuAction.Down);
            Assert.Equal(0, menu.FocusedIndex);
            menu.Handle(MenuAction.Down);
            menu.Handle(MenuAction.Down);
            Assert.Equal("Table", menu.FocusedItem.Label);
        }

        [Fact]
        public void LeftAndRight_SwitchTabsWithWrap_AndResetFocus()
        {
            MenuModel menu = Build();
            menu.Handle(MenuAction.Down);
            menu.Handle(MenuAction.Right);
            Assert.Equal("Toys", menu.CurrentTab.Title);
            Assert.Equal(0, menu.FocusedIndex);
            menu.Handle(MenuAction.Right);
            Assert.Equal("Empty", menu.CurrentTab.Title);
            menu.Handle(MenuAction.Right);
            Assert.Equal("Furniture", menu.CurrentTab.Title);
            menu.Handle(MenuAction.Left);
            Assert.Equal("Empty", menu.CurrentTab.Title);
        }

        [Fact]
        public void EmptyTab_HasNoFocus_AndSelectDoesNothing()
        {
            MenuModel menu = Build();
            menu.SelectTab(2);
            Assert.Equal(-1, menu.FocusedIndex);
            Assert.Null(menu.FocusedItem);
            menu.Handle(MenuAction.Down);
            menu.Handle(MenuAction.Select);
            Assert.Empty(_log);
            Assert.True(menu.IsOpen);
        }

        [Fact]
        public void Select_InvokesFocusedItem()
        {
            MenuModel menu = Build();
            menu.Handle(MenuAction.Down);
            menu.Handle(MenuAction.Select);
            Assert.Equal(new[] { "lamp" }, _log);
            Assert.True(menu.IsOpen);
        }

        [Fact]
        public void ClosesMenuItem_ClosesAfterRunning()
        {
            var root = new MenuPage("x");
            root.MainTab().Add("Go", () => _log.Add("go")).ClosesMenu = true;
            var menu = new MenuModel(root);
            menu.Open();
            menu.Handle(MenuAction.Select);
            Assert.Equal(new[] { "go" }, _log);
            Assert.False(menu.IsOpen);
        }

        [Fact]
        public void Select_OnSubPage_OpensIt_AndBackReturnsWithFocusKept()
        {
            MenuModel menu = Build();
            menu.Handle(MenuAction.Up); // "More"
            menu.Handle(MenuAction.Select);
            Assert.Equal(2, menu.Depth);
            Assert.Equal("More", menu.CurrentPage.Title);
            Assert.Equal("Giant", menu.FocusedItem.Label);
            menu.Handle(MenuAction.Select);
            Assert.Equal(new[] { "giant" }, _log);

            menu.Handle(MenuAction.Back);
            Assert.True(menu.IsOpen);
            Assert.Equal(1, menu.Depth);
            Assert.Equal("More", menu.FocusedItem.Label);
        }

        [Fact]
        public void Back_OnRootPage_Closes()
        {
            MenuModel menu = Build();
            menu.Handle(MenuAction.Back);
            Assert.False(menu.IsOpen);
        }

        [Fact]
        public void Reopen_StartsOnRootPage_KeepingRootTab()
        {
            MenuModel menu = Build();
            menu.Handle(MenuAction.Up);
            menu.Handle(MenuAction.Select); // into "More"
            menu.Handle(MenuAction.ToggleMenu);
            Assert.False(menu.IsOpen);
            menu.Handle(MenuAction.ToggleMenu);
            Assert.Equal(1, menu.Depth);
            Assert.Equal("Furniture", menu.CurrentTab.Title);
        }

        [Fact]
        public void Activate_FocusesAndSelects_ForMouseClicks()
        {
            MenuModel menu = Build();
            menu.Activate(2);
            Assert.Equal(2, menu.FocusedIndex);
            Assert.Equal(new[] { "table" }, _log);
            menu.Activate(99); // ignored
            Assert.Single(_log);
        }

        [Fact]
        public void OpenChanged_FiresOnTransitionsOnly()
        {
            var menu = new MenuModel(new MenuPage("x"));
            var seen = new List<bool>();
            menu.OpenChanged += open => seen.Add(open);
            menu.Open();
            menu.Open();
            menu.Close();
            menu.Close();
            Assert.Equal(new[] { true, false }, seen);
        }
    }
}
