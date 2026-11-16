using System.Collections.Generic;
using WobblyMods.BuildMode;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu.Build
{
    /// <summary>
    /// The "Build" tab of one player's menu. Uses only the menu's existing controls (up/down, A, B),
    /// so it needs no new controller buttons: while the tab is showing, a see-through ghost block sits
    /// on the grid a couple of metres in front of the player, and each button acts on that spot.
    /// Labels show the current choice ("Block: Wall") and update when cycled.
    /// </summary>
    internal sealed class BuildMenu
    {
        private readonly PlayerSession _session;
        private readonly BuildPlayer _player;
        private readonly BuildWorld _world;
        private readonly HashSet<MenuPage> _pages = new HashSet<MenuPage>();
        private readonly MenuItem[] _saveItems = new MenuItem[BuildWorld.SaveSlots];
        private readonly MenuItem[] _loadItems = new MenuItem[BuildWorld.SaveSlots];
        private MenuTab _tab;
        private MenuPage _root;
        private MenuItem _blockItem;
        private MenuItem _materialItem;
        private MenuItem _paintItem;
        private MenuItem _rotateItem;
        private MenuItem _levelItem;
        private MenuItem _modeItem;

        private BuildMenu(PlayerSession session)
        {
            _session = session;
            _world = BuildWorld.Instance;
            _player = _world.Register(session);
            _player.MenuOnBuild = IsShowing;
        }

        /// <summary>Adds the Build tab to a player's root page.</summary>
        public static void AddTo(MenuPage root, PlayerSession session)
        {
            new BuildMenu(session).Build(root);
        }

        private PlayerBuildState State
        {
            get { return _player.State; }
        }

        private void Build(MenuPage root)
        {
            _root = root;
            _tab = root.AddTab("Build");
            _tab.Add("Place block", () => Say(_world.Place(_player)));
            _blockItem = _tab.Add("", () => { State.CycleBlock(1); Say("Block: " + State.Block.DisplayName + "."); });
            _materialItem = _tab.Add("", () => { State.CycleMaterial(1); Say("Material: " + State.Material.DisplayName + "."); });
            _paintItem = _tab.Add("", () => { State.CyclePaint(1); Say("Paint: " + PaintName() + "."); });
            _rotateItem = _tab.Add("", () => { State.RotateClockwise(); Say("Turned to " + State.Rotation.Degrees + " degrees."); });
            // Shows the level; picking it goes back to the floor.
            _levelItem = _tab.Add("", () => Say(_world.ChangeLevel(_player, -_player.Level)));
            _tab.Add("Raise", () => Say(_world.ChangeLevel(_player, 1)));
            _tab.Add("Lower", () => Say(_world.ChangeLevel(_player, -1)));
            _tab.Add("Remove block here", () => Say(_world.RemoveHere(_player)));
            _tab.Add("Undo build", () => Say(_world.Undo(_player)));
            _tab.Add("Redo build", () => Say(_world.Redo(_player)));
            _modeItem = _tab.Add("", () =>
            {
                _player.KeepGhost = !_player.KeepGhost;
                Say(_player.KeepGhost
                    ? "Build mode on: the ghost stays when the menu is closed."
                    : "Build mode off: the ghost shows only on this tab.");
            });

            // Save: pick a slot. Load and Clear ask first, like Reset house.
            var savePage = Page("Save build");
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                int slot = i + 1;
                _saveItems[i] = savePage.MainTab().Add("", () =>
                {
                    Say(_world.Save(slot));
                    RefreshLabels();
                    _session.Menu.Back();
                });
            }
            savePage.MainTab().Add("Back", () => _session.Menu.Back());
            _tab.AddSubPage("Save build", savePage);

            var loadPage = Page("Load build");
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                int slot = i + 1;
                var confirm = Page("Load Build " + slot + "? It replaces this build.");
                confirm.MainTab().Add("No, keep this one", () => _session.Menu.Back());
                confirm.MainTab().Add("Yes, load it", () =>
                {
                    Say(_world.Load(slot));
                    _session.Menu.Back();
                    _session.Menu.Back();
                });
                _loadItems[i] = loadPage.MainTab().AddSubPage("", confirm);
            }
            loadPage.MainTab().Add("Back", () => _session.Menu.Back());
            _tab.AddSubPage("Load build", loadPage);

            var clearPage = Page("Clear the whole build?");
            clearPage.MainTab().Add("No, keep it", () => _session.Menu.Back());
            clearPage.MainTab().Add("Yes, clear it", () =>
            {
                Say(_world.ClearAll(_player));
                _session.Menu.Back();
            });
            _tab.AddSubPage("Clear build", clearPage);

            RefreshLabels();
        }

        private MenuPage Page(string title)
        {
            var page = new MenuPage(title);
            _pages.Add(page);
            return page;
        }

        /// <summary>Is this player's menu open on the Build tab (or one of its pages)?</summary>
        private bool IsShowing()
        {
            MenuModel menu = _session.Menu;
            if (menu == null || !menu.IsOpen)
            {
                return false;
            }
            if (menu.CurrentPage == _root)
            {
                return menu.CurrentTab == _tab;
            }
            return _pages.Contains(menu.CurrentPage);
        }

        private void Say(string message)
        {
            _session.Status = message;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            _blockItem.Label = "Block: " + State.Block.DisplayName;
            _materialItem.Label = "Material: " + State.Material.DisplayName;
            _paintItem.Label = "Paint: " + PaintName();
            _rotateItem.Label = "Rotate (" + State.Rotation.Degrees + ")";
            _levelItem.Label = _player.Level == 0 ? "Level: floor" : "Level: " + (_player.Level > 0 ? "+" : "") + _player.Level;
            _modeItem.Label = _player.KeepGhost ? "Build mode: on" : "Build mode: off";
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                bool used = BuildWorld.SlotExists(i + 1);
                _saveItems[i].Label = "Build " + (i + 1) + (used ? " (replace)" : " (empty)");
                _loadItems[i].Label = "Build " + (i + 1) + (used ? "" : " (empty)");
            }
        }

        private string PaintName()
        {
            int i = State.PaintIndex;
            return i < 0 || i >= BuildLooks.Paints.Length ? "Natural" : BuildLooks.Paints[i].Name;
        }
    }
}
