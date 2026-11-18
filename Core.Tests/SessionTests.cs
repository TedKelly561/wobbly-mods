using System;
using Xunit;

namespace WobblyMods.Core.Tests
{
    public class SessionTests
    {
        /// <summary>Each player's menu: "Add" pushes an undoable change to a shared counter array slot.</summary>
        private static MenuPage BuildMenu(PlayerSession s, int[] world)
        {
            var page = new MenuPage("Player " + (s.PlayerIndex + 1));
            MenuTab tab = page.MainTab();
            tab.Add("Add", () =>
            {
                world[s.PlayerIndex]++;
                s.Undo.Push("add", () => world[s.PlayerIndex]--);
            });
            tab.Add("Undo", () => s.Status = s.Undo.UndoLast());
            return page;
        }

        private static void Frame(SessionManager mgr, params FakeInput[] inputs)
        {
            mgr.Update();
            foreach (FakeInput i in inputs) i.EndFrame();
        }

        [Fact]
        public void EachPlayerHasOwnUndo_OneUndoDoesNotAffectOther()
        {
            var world = new int[4];
            var mgr = new SessionManager(s => BuildMenu(s, world));
            var p1 = new FakeInput();
            var p2 = new FakeInput();
            PlayerSession s1 = mgr.Join(p1);
            PlayerSession s2 = mgr.Join(p2);

            p1.Press(MenuAction.ToggleMenu); p2.Press(MenuAction.ToggleMenu); Frame(mgr, p1, p2);
            p1.Press(MenuAction.Select); p2.Press(MenuAction.Select); Frame(mgr, p1, p2);
            p2.Press(MenuAction.Select); Frame(mgr, p1, p2);
            Assert.Equal(1, world[0]);
            Assert.Equal(2, world[1]);

            // Player 1 undoes: only player 1's change goes away.
            p1.Press(MenuAction.Down); Frame(mgr, p1, p2);
            p1.Press(MenuAction.Select); Frame(mgr, p1, p2);
            Assert.Equal(0, world[0]);
            Assert.Equal(2, world[1]);
            Assert.Equal(0, s1.Undo.Count);
            Assert.Equal(2, s2.Undo.Count);

            // Player 1 undoing again with nothing left still doesn't touch player 2.
            p1.Press(MenuAction.Select); Frame(mgr, p1, p2);
            Assert.Equal(2, world[1]);
            Assert.Null(s1.Status);
        }

        [Fact]
        public void EachPlayerHasOwnMenuState()
        {
            var world = new int[4];
            var mgr = new SessionManager(s => BuildMenu(s, world));
            var p1 = new FakeInput();
            var p2 = new FakeInput();
            PlayerSession s1 = mgr.Join(p1);
            PlayerSession s2 = mgr.Join(p2);

            p1.Press(MenuAction.ToggleMenu); Frame(mgr, p1, p2);
            Assert.True(s1.IsOpen);
            Assert.False(s2.IsOpen);
            Assert.True(mgr.AnyOpen);

            p1.Press(MenuAction.Down); Frame(mgr, p1, p2);
            p2.Press(MenuAction.ToggleMenu); Frame(mgr, p1, p2);
            Assert.Equal(1, s1.Menu.FocusedIndex);
            Assert.Equal(0, s2.Menu.FocusedIndex);

            // Player 2 selecting doesn't run player 1's focused item.
            p2.Press(MenuAction.Select); Frame(mgr, p1, p2);
            Assert.Equal(0, world[0]);
            Assert.Equal(1, world[1]);
        }

        [Fact]
        public void ClosedMenu_DoesNotUseInput()
        {
            var world = new int[4];
            var p1 = new FakeInput();
            var s = new PlayerSession(0, p1, x => BuildMenu(x, world));
            p1.Press(MenuAction.Select);
            Assert.False(s.Update());
            Assert.Equal(0, world[0]);
            p1.EndFrame();
            p1.Press(MenuAction.ToggleMenu);
            Assert.True(s.Update());
        }

        [Fact]
        public void TabPrevAndNext_ArePolled_OnlyWhileOpen()
        {
            var p1 = new FakeInput();
            var s = new PlayerSession(0, p1, x =>
            {
                var page = new MenuPage("p");
                page.AddTab("One").AsGrid().Add("a", () => { });
                page.AddTab("Two").AsGrid().Add("b", () => { });
                return page;
            });
            p1.Press(MenuAction.TabNext);
            Assert.False(s.Update()); // closed: a tap is left for build mode
            p1.EndFrame();
            s.Menu.Open();
            p1.Press(MenuAction.TabNext);
            Assert.True(s.Update());
            Assert.Equal("Two", s.Menu.CurrentTab.Title);
            p1.EndFrame();
            p1.Press(MenuAction.TabPrev);
            s.Update();
            Assert.Equal("One", s.Menu.CurrentTab.Title);
        }

        [Fact]
        public void Manager_HoldsUpToFourPlayers_AndReusesFreedSlots()
        {
            var world = new int[4];
            var mgr = new SessionManager(s => BuildMenu(s, world));
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i, mgr.Join(new FakeInput()).PlayerIndex);
            }
            Assert.Null(mgr.Join(new FakeInput()));
            Assert.Equal(4, mgr.Count);

            mgr.Get(1).Menu.Open();
            PlayerSession left = mgr.Leave(1);
            Assert.False(left.IsOpen);
            Assert.Null(mgr.Get(1));
            Assert.Equal(3, mgr.Count);
            Assert.Equal(1, mgr.Join(new FakeInput()).PlayerIndex);
        }

        [Fact]
        public void Manager_JoinSpecificSlot_ValidatesIndex()
        {
            var mgr = new SessionManager(s => new MenuPage("x"));
            Assert.Equal(2, mgr.Join(2, new FakeInput()).PlayerIndex);
            Assert.Throws<InvalidOperationException>(() => mgr.Join(2, new FakeInput()));
            Assert.Throws<ArgumentOutOfRangeException>(() => mgr.Join(4, new FakeInput()));
            Assert.Null(mgr.Get(-1));
            Assert.Null(mgr.Leave(3));
        }

        [Fact]
        public void BuildRootPage_ReceivesItsOwnSession()
        {
            PlayerSession seen = null;
            var mgr = new SessionManager(s => { seen = s; return new MenuPage("x"); });
            PlayerSession joined = mgr.Join(new FakeInput());
            Assert.Same(joined, seen);
        }
    }
}
