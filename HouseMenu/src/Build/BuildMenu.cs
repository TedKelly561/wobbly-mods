using System.Collections.Generic;
using WobblyMods.BuildMode;
using WobblyMods.Core;
using WobblyMods.HouseMenu.Icons;
using UnityEngine;

namespace WobblyMods.HouseMenu.Build
{
    /// <summary>
    /// The "Build" tab of one player's menu. Uses only the menu's existing controls (up/down, A, B),
    /// so it needs no new controller buttons: while the tab is showing, a see-through ghost block sits
    /// on the grid a couple of metres in front of the player, and each button acts on that spot.
    /// Labels show the current choice ("Block: Wall") and update when cycled; so do the symbols on the tiles
    /// (the current block's shape, a cube in the current material, a drop of the current paint).
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
        private MenuItem _sizeItem;
        private MenuItem _autoTurnItem;
        private MenuItem _roofItem;
        private MenuItem _placeItem;
        private MenuPage _kitPage;
        private readonly Dictionary<MenuItem, RoomKit> _kitItems = new Dictionary<MenuItem, RoomKit>();
        private RoomKit _lastKit = RoomKit.SmallRoom;

        private BuildMenu(PlayerSession session)
        {
            _session = session;
            _world = BuildWorld.Instance;
            _player = _world.Register(session);
            _player.MenuOnBuild = IsShowing;
            _player.KitPreview = KitToPreview;
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
            _tab = root.AddTab("Build").AsGrid();
            _tab.Icon = TabTheme.IconFor("Build");
            _placeItem = _tab.Add("Place block", () => Say(_world.Place(_player)));
            _placeItem.Icon = SymbolIcons.Key("brick");
            BuildKitPage();
            _blockItem = _tab.Add("", () => { State.CycleBlock(1); Say("Block: " + State.Block.DisplayName + "."); });
            _materialItem = _tab.Add("", () => { State.CycleMaterial(1); Say("Material: " + State.Material.DisplayName + "."); });
            _paintItem = _tab.Add("", () => { State.CyclePaint(1); Say("Paint: " + PaintName() + "."); });
            _rotateItem = _tab.Add("", () =>
            {
                _player.AutoTurn = false; // turning by hand switches auto-turn off
                State.RotateClockwise();
                Say("Turned to " + State.Rotation.Degrees + " degrees (auto-turn off).");
            });
            _autoTurnItem = _tab.Add("", () =>
            {
                _player.AutoTurn = !_player.AutoTurn;
                Say(_player.AutoTurn ? "Auto-turn on: pieces follow the way you face." : "Auto-turn off: use Rotate.");
            });
            // Shows the level; picking it goes back to the floor.
            _levelItem = _tab.Add("", () => Say(_world.ChangeLevel(_player, -_player.Level)));
            _sizeItem = _tab.Add("", () => Say(_world.CyclePieceSize()));
            _tab.Add("Raise", () => Say(_world.ChangeLevel(_player, 1))).Icon = SymbolIcons.Key("up");
            _tab.Add("Lower", () => Say(_world.ChangeLevel(_player, -1))).Icon = SymbolIcons.Key("down");
            _tab.Add("Remove block here", () => Say(_world.RemoveHere(_player))).Icon = SymbolIcons.Key("trash");
            _tab.Add("Undo build", () => Say(_world.Undo(_player))).Icon = SymbolIcons.Key("undo");
            _tab.Add("Redo build", () => Say(_world.Redo(_player))).Icon = SymbolIcons.Key("redo");
            _modeItem = _tab.Add("", () =>
            {
                _player.KeepGhost = !_player.KeepGhost;
                Say(_player.KeepGhost
                    ? "Build mode on: the ghost stays when the menu is closed."
                    : "Build mode off: the ghost shows only on this tab.");
            });

            // Save: pick a slot. Load and Clear ask first, like Reset house.
            var savePage = Page("Save build");
            savePage.MainTab().AsGrid();
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                int slot = i + 1;
                _saveItems[i] = savePage.MainTab().Add("", () =>
                {
                    Say(_world.Save(slot));
                    RefreshLabels();
                    _session.Menu.Back();
                });
                _saveItems[i].Icon = SymbolIcons.Key(slot.ToString());
            }
            savePage.MainTab().Add("Back", () => _session.Menu.Back()).Icon = SymbolIcons.Key("back");
            _tab.AddSubPage("Save build", savePage).Icon = SymbolIcons.Key("save");

            var loadPage = Page("Load build");
            loadPage.MainTab().AsGrid();
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                int slot = i + 1;
                var confirm = Page("Load Build " + slot + "? It replaces this build.");
                confirm.MainTab().AsGrid(2);
                confirm.MainTab().Add("No, keep this one", () => _session.Menu.Back()).Icon = SymbolIcons.Key("no");
                confirm.MainTab().Add("Yes, load it", () =>
                {
                    Say(_world.Load(slot));
                    _session.Menu.Back();
                    _session.Menu.Back();
                }).Icon = SymbolIcons.Key("yes");
                _loadItems[i] = loadPage.MainTab().AddSubPage("", confirm);
                _loadItems[i].Icon = SymbolIcons.Key(slot.ToString());
            }
            loadPage.MainTab().Add("Back", () => _session.Menu.Back()).Icon = SymbolIcons.Key("back");
            _tab.AddSubPage("Load build", loadPage).Icon = SymbolIcons.Key("load");

            var clearPage = Page("Clear the whole build?");
            clearPage.MainTab().AsGrid(2);
            clearPage.MainTab().Add("No, keep it", () => _session.Menu.Back()).Icon = SymbolIcons.Key("no");
            clearPage.MainTab().Add("Yes, clear it", () =>
            {
                Say(_world.ClearAll(_player));
                _session.Menu.Back();
            }).Icon = SymbolIcons.Key("yes");
            _tab.AddSubPage("Clear build", clearPage).Icon = SymbolIcons.Key("broom");

            RefreshLabels();
        }

        /// <summary>
        /// "Room kits...": whole rooms, floors, wall lines and stairs in one press, in front of the player
        /// and facing their way, in the chosen material and paint. The ghost shows the focused kit.
        /// </summary>
        private void BuildKitPage()
        {
            _kitPage = Page("Room kits");
            MenuTab kits = _kitPage.MainTab().AsGrid();
            foreach (RoomKit kit in RoomKits.All)
            {
                RoomKit k = kit;
                MenuItem item = kits.Add(RoomKits.DisplayName(k), () =>
                {
                    _lastKit = k;
                    Say(_world.PlaceKit(_player, k));
                });
                item.Icon = SymbolIcons.Key(KitSymbol(k));
                _kitItems[item] = k;
            }
            _roofItem = kits.Add("", () =>
            {
                _player.KitRoof = !_player.KitRoof;
                Say(_player.KitRoof ? "Rooms get a flat roof." : "Rooms have no roof (open top).");
            });
            kits.Add("Undo build", () => Say(_world.Undo(_player))).Icon = SymbolIcons.Key("undo");
            kits.Add("Back", () => _session.Menu.Back()).Icon = SymbolIcons.Key("back");
            _tab.AddSubPage("Room kits...", _kitPage).Icon = SymbolIcons.Key("room");
        }

        /// <summary>The kit under the cursor on the Room kits page (or the last one, on its other buttons).</summary>
        private RoomKit? KitToPreview()
        {
            MenuModel menu = _session.Menu;
            if (menu == null || !menu.IsOpen || menu.CurrentPage != _kitPage)
            {
                return null;
            }
            MenuItem focused = menu.FocusedItem;
            RoomKit kit;
            if (focused != null && _kitItems.TryGetValue(focused, out kit))
            {
                _lastKit = kit;
                return kit;
            }
            return _lastKit;
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
            _sizeItem.Label = "Piece size: " + _world.PieceSize + " m";
            _autoTurnItem.Label = _player.AutoTurn ? "Auto-turn: on" : "Auto-turn: off";
            _roofItem.Label = _player.KitRoof ? "Roof on rooms: yes" : "Roof on rooms: no";

            // Symbols follow the current choices.
            string block = BlockSymbol(State.Block.Id);
            _placeItem.Icon = SymbolIcons.Key(block);
            _blockItem.Icon = SymbolIcons.Key(block);
            Color natural = BuildLooks.NaturalColor(State.Material);
            _materialItem.Icon = SymbolIcons.Key("block", natural);
            int paint = State.PaintIndex;
            _paintItem.Icon = SymbolIcons.Key("paint", paint < 0 || paint >= BuildLooks.Paints.Length
                ? natural
                : (Color)new Color32(BuildLooks.Paints[paint].Color.R, BuildLooks.Paints[paint].Color.G, BuildLooks.Paints[paint].Color.B, 255));
            _rotateItem.Icon = SymbolIcons.Key("turn");
            _autoTurnItem.Icon = SymbolIcons.Key("autoturn", null, !_player.AutoTurn);
            _levelItem.Icon = SymbolIcons.Key("tofloor");
            _modeItem.Icon = SymbolIcons.Key("ghostblock", null, !_player.KeepGhost);
            _sizeItem.Icon = SymbolIcons.Key("size");
            _roofItem.Icon = SymbolIcons.Key("roofflat", null, !_player.KitRoof);
            for (int i = 0; i < BuildWorld.SaveSlots; i++)
            {
                bool used = BuildWorld.SlotExists(i + 1);
                _saveItems[i].Label = "Build " + (i + 1) + (used ? " (replace)" : " (empty)");
                _loadItems[i].Label = "Build " + (i + 1) + (used ? "" : " (empty)");
            }
        }

        /// <summary>The symbol for a block type id (BuildCatalog.Ids).</summary>
        private static string BlockSymbol(string id)
        {
            switch (id)
            {
                case BuildCatalog.Ids.Floor: return "floor";
                case BuildCatalog.Ids.Wall: return "wall";
                case BuildCatalog.Ids.HalfWall: return "halfwall";
                case BuildCatalog.Ids.RoofSlope: return "roof";
                case BuildCatalog.Ids.RoofFlat: return "roofflat";
                case BuildCatalog.Ids.Stairs: return "stairs";
                case BuildCatalog.Ids.Door: return "door";
                case BuildCatalog.Ids.Window: return "window";
                case BuildCatalog.Ids.Pillar: return "pillar";
                case BuildCatalog.Ids.Beam: return "beam";
                case BuildCatalog.Ids.CornerWall: return "corner";
                default: return "brick";
            }
        }

        private static string KitSymbol(RoomKit kit)
        {
            switch (kit)
            {
                case RoomKit.SmallRoom: return "room";
                case RoomKit.BigRoom: return "bigroom";
                case RoomKit.Floor3x3: return "floor";
                case RoomKit.Floor5x5: return "bigfloor";
                case RoomKit.WallLine3: return "wall";
                case RoomKit.WallLine5: return "longwall";
                case RoomKit.StairsUp: return "stairs";
                default: return "room";
            }
        }

        private string PaintName()
        {
            int i = State.PaintIndex;
            return i < 0 || i >= BuildLooks.Paints.Length ? "Natural" : BuildLooks.Paints[i].Name;
        }
    }
}
