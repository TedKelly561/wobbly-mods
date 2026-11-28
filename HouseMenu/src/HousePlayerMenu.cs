using UnityEngine;
using WobblyMods.Core;
using WobblyMods.HouseMenu.Build;
using WobblyMods.HouseMenu.Icons;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Builds one player's house menu (pages, tabs and buttons from Core's menu model) and does
    /// what each button means. Everything uses this player's own undo history, camera and status,
    /// so split-screen players never undo or move each other's things.
    /// Drawing is done by HouseMenuWindow; anything that touches the game goes through GameBindings.
    /// </summary>
    internal sealed class HousePlayerMenu
    {
        private const float MoveStep = 0.5f;

        // Skylar's favorites first: orange and green.
        private static readonly Color[] Palette =
        {
            new Color(1f, 0.5f, 0f),      // orange
            new Color(0.2f, 0.75f, 0.2f), // green
            new Color(1f, 0.75f, 0.3f),   // light orange
            new Color(0.5f, 0.9f, 0.3f),  // lime green
            Color.red, Color.blue, Color.yellow, Color.black, Color.white
        };

        private readonly PlayerSession _session;
        private int _colorIndex;
        private MenuItem _colorItem;
        private MenuItem _furItem;
        private int _furIndex;
        private readonly MenuItem[] _saveItems = new MenuItem[HouseSave.Slots];
        private readonly MenuItem[] _loadItems = new MenuItem[HouseSave.Slots];

        private HousePlayerMenu(PlayerSession session)
        {
            _session = session;
            _session.Status = "Look at something in your house, then pick a button.";
        }

        /// <summary>Pass to SessionManager: builds the menu for each player who joins.</summary>
        public static MenuPage BuildRootPage(PlayerSession session)
        {
            var menu = new HousePlayerMenu(session);
            JustBuilt[session] = menu;
            return menu.Build();
        }

        // The session's menu doesn't exist yet while its pages are being built, so anything that needs to listen
        // to the menu is hooked up a moment later, in Attach.
        private static readonly System.Collections.Generic.Dictionary<PlayerSession, HousePlayerMenu> JustBuilt =
            new System.Collections.Generic.Dictionary<PlayerSession, HousePlayerMenu>();

        /// <summary>Pass to SessionManager.PlayerJoined: refreshes the save-slot labels each time this player's menu opens.</summary>
        public static void Attach(PlayerSession session)
        {
            HousePlayerMenu menu;
            if (!JustBuilt.TryGetValue(session, out menu)) return;
            JustBuilt.Remove(session);
            session.Menu.OpenChanged += open => { if (open) menu.RefreshSlotLabels(); };
        }

        /// <summary>Undo for this player (the menu button and the F2 backup key).</summary>
        public static void UndoLast(PlayerSession session)
        {
            string undone = session.Undo.UndoLast();
            session.Status = undone != null ? "Undid: " + undone : "Nothing to undo.";
        }

        private int Player
        {
            get { return _session.PlayerIndex; }
        }

        private MenuPage Build()
        {
            var root = new MenuPage("House Menu");

            // Every tab is a picture grid, 3 across (the parent's choice). Items show a photo of the real thing
            // (Icons/IconCache, keyed by catalog id); actions show a symbol ("sym:..." keys, Icons/SymbolDrawings).
            foreach (string category in FurnitureCatalog.Categories)
            {
                MenuTab tab = root.AddTab(category).AsGrid();
                tab.Icon = TabTheme.IconFor(category);
                if (category == "Furniture")
                {
                    // "Copy this" is a tool (parent's request, 2026-11-21): pick it, the menu closes, a + on screen
                    // names what will be copied, tap RB to copy it into "My copies". Empty slots show a dashed box.
                    MenuItem copyTool = tab.Add("Copy this", () => { _session.Status = Fun.ToggleTool(Player, Fun.Tool.Copy); });
                    copyTool.Icon = SymbolIcons.Key("camera");
                    copyTool.ClosesMenu = true;
                    var copies = new MenuPage("My copies");
                    MenuTab copyGrid = copies.MainTab().AsGrid();
                    for (int i = 0; i < WorldCopy.Slots; i++)
                    {
                        int slot = i;
                        Icons.IconCache.SetFallback(WorldCopy.IconKey(slot), SymbolIcons.Key("ghostblock"));
                        copyGrid.Add("Copy " + (slot + 1), () =>
                        {
                            string message;
                            GameObject placed = WorldCopy.Paste(Player, slot, out message);
                            _session.Status = message;
                            if (placed != null) _session.Undo.Push("placed a copy", () => ObjectEdits.Remove(placed));
                        }).Icon = WorldCopy.IconKey(slot);
                    }
                    tab.AddSubPage("My copies", copies).Icon = SymbolIcons.Key("load");
                    // All the rest of the game's furniture, one page per room (a photo of one item is the room's picture).
                    foreach (string[] room in FurnitureCatalog.Rooms)
                    {
                        var page = new MenuPage(room[0]);
                        MenuTab grid = page.MainTab().AsGrid();
                        string firstShown = null;
                        foreach (CatalogItem item in FurnitureCatalog.InCategory(room[0]))
                        {
                            if (HiddenItems.IsHidden(item.Id)) continue;
                            CatalogItem captured = item;
                            string icon = item.Symbol != null ? SymbolIcons.Key(item.Symbol) : item.Id;
                            grid.Add(item.DisplayName, () => Spawn(captured)).Icon = icon;
                            if (firstShown == null) firstShown = icon;
                        }
                        // A room with nothing left to place (every item hidden) gets no tile: it would open an empty page.
                        if (firstShown == null) continue;
                        // The room's picture is one of its items; if that one is hidden, use the first that isn't.
                        tab.AddSubPage(room[0], page).Icon = HiddenItems.IsHidden(room[1]) ? firstShown : room[1];
                    }
                }
                if (category == "Toys")
                {
                    tab.AddSubPage("Fun", FunPage()).Icon = SymbolIcons.Key("star");
                }
                if (category == "Animals")
                {
                    // Skylar's catfish rides: turn the car, boat or helicopter she's in into a giant catfish.
                    tab.AddSubPage("Dress up", DressUpPage()).Icon = SymbolIcons.Key("shirt");
                    tab.Add("Catfish car!", () => { _session.Status = CatfishRide.Toggle(Player); }).Icon = SymbolIcons.Key("fish");
                    tab.Add("Turn catfish", () => { _session.Status = CatfishRide.Flip(Player); }).Icon = SymbolIcons.Key("turn");
                }
                foreach (CatalogItem item in FurnitureCatalog.InCategory(category))
                {
                    if (HiddenItems.IsHidden(item.Id)) continue;
                    CatalogItem captured = item;
                    tab.Add(item.DisplayName, () => Spawn(captured)).Icon = item.Symbol != null ? SymbolIcons.Key(item.Symbol) : item.Id;
                }
            }

            // The tab row only fits about five tabs at the menu's font size (MaxPanelWidth 560, fontSize 26),
            // so Undo and Reset house moved from their own tab into Change, making room for Build.
            MenuTab change = root.AddTab("Change").AsGrid();
            change.Icon = TabTheme.IconFor("Change");
            // Skylar's request: no-clip in her own world, like the sandbox has.
            // Parent's request: give every player a million.
            change.Add("Million!", () => { _session.Status = Powers.GiveMoney(1000000); }).Icon = SymbolIcons.Key("star");
            change.Add("No-clip (fly through walls)", () => { _session.Status = Powers.ToggleNoClip(Player); }).Icon = SymbolIcons.Key("ghost");
            change.Add("Undo", () => UndoLast(_session)).Icon = SymbolIcons.Key("undo");
            change.Add("Left", () => Nudge(CameraRelative(Vector3.left))).Icon = SymbolIcons.Key("left");
            change.Add("Right", () => Nudge(CameraRelative(Vector3.right))).Icon = SymbolIcons.Key("right");
            // Closer = toward the camera (the camera's back direction); Farther = away from it.
            change.Add("Closer", () => Nudge(CameraRelative(Vector3.back))).Icon = SymbolIcons.Key("closer");
            change.Add("Farther", () => Nudge(CameraRelative(Vector3.forward))).Icon = SymbolIcons.Key("farther");
            change.Add("Up", () => Nudge(Vector3.up)).Icon = SymbolIcons.Key("up");
            change.Add("Down", () => Nudge(Vector3.down)).Icon = SymbolIcons.Key("down");
            // The paint drop shows the color the next press will use.
            _colorItem = change.Add("New color", Recolor);
            _colorItem.Icon = SymbolIcons.Key("paint", Palette[_colorIndex]);
            change.Add("Copy", CopyTarget).Icon = SymbolIcons.Key("copy");
            change.Add("Turn", () => WithTarget(t => { ObjectEdits.Turn(t.transform, 45f, _session.Undo); return "Turned " + t.name + "."; })).Icon = SymbolIcons.Key("turn");
            change.Add("Bigger", () => WithTarget(t => ObjectEdits.Resize(t, 1.25f, _session.Undo) ? "Bigger!" : "That's as big as it goes.")).Icon = SymbolIcons.Key("plus");
            change.Add("Smaller", () => WithTarget(t => ObjectEdits.Resize(t, 0.8f, _session.Undo) ? "Smaller!" : "That's as small as it goes.")).Icon = SymbolIcons.Key("minus");

            // Save and put back everything placed with the menu (it would vanish when the game closes).
            var savePage = new MenuPage("Save house");
            var loadPage = new MenuPage("Load house");
            savePage.MainTab().AsGrid();
            loadPage.MainTab().AsGrid();
            for (int i = 1; i <= HouseSave.Slots; i++)
            {
                int slot = i;
                string digit = SymbolIcons.Key(slot.ToString());
                _saveItems[slot - 1] = savePage.MainTab().Add("House " + slot, () =>
                {
                    _session.Status = HouseSave.Save(slot);
                    RefreshSlotLabels();
                    _session.Menu.Back();
                });
                _saveItems[slot - 1].Icon = digit;

                // Loading replaces what's placed now, so it asks first.
                var confirmLoad = new MenuPage("Load house " + slot + "?");
                confirmLoad.MainTab().AsGrid(2);
                confirmLoad.MainTab().Add("No, keep what's here", () => _session.Menu.Back()).Icon = SymbolIcons.Key("no");
                confirmLoad.MainTab().Add("Yes, load house " + slot, () =>
                {
                    _session.Status = HouseSave.Load(slot, Player, _session.Undo);
                    _session.Menu.Back();
                    _session.Menu.Back();
                }).Icon = SymbolIcons.Key("yes");
                _loadItems[slot - 1] = loadPage.MainTab().AddSubPage("House " + slot, confirmLoad);
                _loadItems[slot - 1].Icon = digit;
            }
            // "(empty)" / "(replace)" on the slot tiles: set now, again right after this player saves, and each time
            // this player's menu opens (see Attach: another player may have saved, or the shared folder come back).
            RefreshSlotLabels();
            change.AddSubPage("Save house", savePage).Icon = SymbolIcons.Key("save");
            change.AddSubPage("Load house", loadPage).Icon = SymbolIcons.Key("load");

            // Reset asks first, so a stray A press can't wipe everything.
            var confirmReset = new MenuPage("Reset house?");
            MenuTab confirm = confirmReset.MainTab().AsGrid(2);
            confirm.Add("No, keep it", () => _session.Menu.Back()).Icon = SymbolIcons.Key("no");
            confirm.Add("Yes, reset", () =>
            {
                ResetHouse();
                _session.Menu.Back();
            }).Icon = SymbolIcons.Key("yes");

            change.AddSubPage("Reset house", confirmReset).Icon = SymbolIcons.Key("broom");

            // Sandbox build mode: blocks on a grid, with its own per-player undo (BuildMode.Core).
            BuildMenu.AddTo(root, _session);

            return root;
        }

        private void Spawn(CatalogItem item)
        {
            _session.Status = "Placing " + item.DisplayName + "...";
            Plugin.Log.LogInfo("Menu pick: player " + (Player + 1) + " picked " + item.Id + " (" + item.DisplayName + ").");
            PlayerSession session = _session;
            GameBindings.Spawn(item, Player, (placed, problem) =>
            {
                if (placed == null)
                {
                    session.Status = "Couldn't place " + item.DisplayName + ".";
                    Plugin.Log.LogWarning("Menu pick FAILED: player " + (Player + 1) + " couldn't place " + item.Id + ": " + problem);
                    if (problem != null && problem.Contains("couldn't place")) HiddenItems.Hide(item.Id, problem);
                    return;
                }

                session.Undo.Push("placed " + item.DisplayName, () => ObjectEdits.Remove(placed));
                session.Status = "Placed " + item.DisplayName + ".";
            });
        }

        /// <summary>Puts "(replace)" or "(empty)" on the Save house and Load house slot tiles, from the files as they are now.</summary>
        private void RefreshSlotLabels()
        {
            for (int i = 0; i < HouseSave.Slots; i++)
            {
                bool used = HouseSave.SlotExists(i + 1);
                if (_saveItems[i] != null) _saveItems[i].Label = "House " + (i + 1) + (used ? " (replace)" : " (empty)");
                if (_loadItems[i] != null) _loadItems[i].Label = "House " + (i + 1) + (used ? "" : " (empty)");
            }
        }

        private void Nudge(Vector3 direction)
        {
            GameObject target = CurrentTarget();
            if (target == null)
            {
                return;
            }

            ObjectEdits.Move(target.transform, direction * MoveStep, _session.Undo);
            _session.Status = "Moved " + target.name + ".";
        }

        /// <summary>
        /// Makes another of the thing the player is looking at (or the last thing they placed), right beside it,
        /// at the same size, facing and color. Handy for rows of plants or lamps.
        /// </summary>
        private void CopyTarget()
        {
            // The thing she is looking at, or (like every other Change button) the last thing she placed: aiming a
            // third-person camera is hard at 5, and without that Copy seemed to do nothing. Only things placed from
            // the menu's own tiles can be made again here; world copies come from "Copy this" / "My copies".
            GameObject target = CurrentTarget();
            if (target == null) return; // CurrentTarget already said "Place something first..."
            CatalogItem item = FurnitureCatalog.Find(GameBindings.CatalogIdOf(target));
            if (item == null)
            {
                _session.Status = "Copy works on things you placed from this menu. Look at one, then press Copy.";
                return;
            }

            float scale = GameBindings.ScaleOf(target);
            Color color;
            bool hasColor = GameBindings.ColorOf(target, out color);

            // Beside it: one object-width to the player's right.
            Bounds b;
            float width = Tanks.FishTank.TryGetBounds(target, out b) ? Mathf.Max(b.size.x, b.size.z) : 1.5f;
            Camera cam = GameBindings.CameraFor(Player);
            Vector3 right = cam != null ? new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized : Vector3.right;
            Vector3 position = target.transform.position + right * (width + 0.2f);

            PlayerSession session = _session;
            _session.Status = "Copying " + item.DisplayName + "...";
            GameBindings.SpawnAt(item, Player, position, target.transform.rotation, (placed, problem) =>
            {
                if (placed == null)
                {
                    session.Status = "Couldn't copy " + item.DisplayName + ".";
                    return;
                }
                if (!Mathf.Approximately(scale, 1f))
                {
                    placed.transform.localScale *= scale;
                    GameBindings.NoteScale(placed, scale);
                }
                if (hasColor) ObjectEdits.ApplyColor(placed, color);
                session.Undo.Push("copied " + item.DisplayName, () => ObjectEdits.Remove(placed));
                session.Status = "Copied " + item.DisplayName + ".";
            });
        }

        /// <summary>Runs an edit on the thing the player is looking at (or the last thing they placed).</summary>
        private void WithTarget(System.Func<GameObject, string> edit)
        {
            GameObject target = CurrentTarget();
            if (target == null) return;
            _session.Status = edit(target);
        }

        /// <summary>The fun page: catfish world, parade, pet, bubble gun, paint, gravity (Fun.cs).</summary>
        private MenuPage FunPage()
        {
            var page = new MenuPage("Fun");
            MenuTab grid = page.MainTab().AsGrid();
            // Tools: picking one closes the menu; then tap RB to shoot (Fun.Fire, from Plugin.Update).
            MenuItem bubble = grid.Add("Bubble gun", () => { _session.Status = Fun.ToggleTool(Player, Fun.Tool.Bubble); });
            bubble.Icon = SymbolIcons.Key("bubble");
            bubble.ClosesMenu = true;
            MenuItem paint = null;
            paint = grid.Add("Paint brush", () =>
            {
                _session.Status = Fun.ToggleTool(Player, Fun.Tool.Paint);
                paint.Icon = SymbolIcons.Key("paint", Fun.NextPaint(Player));
            });
            paint.Icon = SymbolIcons.Key("paint", Fun.NextPaint(Player));
            paint.ClosesMenu = true;
            grid.Add("Catfish swarm", () => { _session.Status = Fun.Swarm(Player); }).Icon = SymbolIcons.Key("swarm");
            grid.Add("Clear swarm", () => { _session.Status = Fun.ClearSwarm(); }).Icon = SymbolIcons.Key("trash");
            grid.Add("Redtail pet", () => { _session.Status = Fun.TogglePet(Player); }).Icon = SymbolIcons.Key("fish");
            grid.Add("Catfish parade", () => { _session.Status = Fun.ToggleParade(Player); }).Icon = SymbolIcons.Key("parade");
            grid.Add("Every car a catfish", () => { _session.Status = Fun.ToggleCatfishWorld(); }).Icon = SymbolIcons.Key("redtail");
            grid.Add("Floatier (less gravity)", () => { _session.Status = Fun.Gravity(1); }).Icon = SymbolIcons.Key("up");
            grid.Add("Heavier (more gravity)", () => { _session.Status = Fun.Gravity(-1); }).Icon = SymbolIcons.Key("down");
            grid.Add("Normal gravity", () => { _session.Status = Fun.Gravity(0); }).Icon = SymbolIcons.Key("tofloor");
            return page;
        }

        /// <summary>Skylar's costumes: she stays a Wobbly, with ears, tails, whiskers and fur color added (Outfits).</summary>
        private MenuPage DressUpPage()
        {
            var page = new MenuPage("Dress up");
            MenuTab grid = page.MainTab().AsGrid();
            foreach (string id in Outfits.Ids)
            {
                string captured = id;
                grid.Add(Outfits.NiceName(id), () => { _session.Status = Outfits.Wear(Player, captured); }).Icon = SymbolIcons.Key(id);
            }
            _furItem = grid.Add("Fur color", () =>
            {
                Color color = Palette[_furIndex];
                _furIndex = (_furIndex + 1) % Palette.Length;
                _furItem.Icon = SymbolIcons.Key("paint", Palette[_furIndex]);
                _session.Status = Outfits.SetFur(Player, color);
            });
            _furItem.Icon = SymbolIcons.Key("paint", Palette[_furIndex]);
            grid.Add("Turn costume", () => { _session.Status = Outfits.Turn(Player); }).Icon = SymbolIcons.Key("turn");
            grid.Add("Bigger costume", () => { _session.Status = Outfits.Resize(Player, 1.15f); }).Icon = SymbolIcons.Key("plus");
            grid.Add("Smaller costume", () => { _session.Status = Outfits.Resize(Player, 1f / 1.15f); }).Icon = SymbolIcons.Key("minus");
            grid.Add("Take off", () => { _session.Status = Outfits.TakeOff(Player); }).Icon = SymbolIcons.Key("no");
            return page;
        }

        private void Recolor()
        {
            GameObject target = CurrentTarget();
            if (target == null)
            {
                return;
            }

            Color color = Palette[_colorIndex];
            _colorIndex = (_colorIndex + 1) % Palette.Length;
            _colorItem.Icon = SymbolIcons.Key("paint", Palette[_colorIndex]);
            ObjectEdits.SetColor(target, color, _session.Undo);
            _session.Status = "Changed the color of " + target.name + ".";
        }

        /// <summary>Undoes every change this player made (not other players' changes).</summary>
        private void ResetHouse()
        {
            int undone = _session.Undo.UndoAll();
            _session.Status = undone > 0
                ? "Reset the house (" + undone + " changes undone)."
                : "The house is already normal.";
        }

        /// <summary>
        /// The thing this player is looking at if it can be changed, otherwise the last thing they placed.
        /// Aiming a third-person camera is hard at 5, so "the last thing I placed" is the easy default.
        /// </summary>
        private GameObject CurrentTarget()
        {
            GameObject target = GameBindings.FindEditableInView(Player) ?? GameBindings.LastSpawnedBy(Player);
            if (target == null)
            {
                _session.Status = "Place something first, then you can move it or color it.";
            }
            return target;
        }

        /// <summary>Turns a direction from this player's camera point of view into a flat world direction.</summary>
        private Vector3 CameraRelative(Vector3 local)
        {
            Camera cam = GameBindings.CameraFor(Player);
            if (cam == null)
            {
                return local;
            }

            Vector3 forward = cam.transform.forward;
            forward = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 right = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;

            if (local == Vector3.left) return -right;
            if (local == Vector3.right) return right;
            if (local == Vector3.forward) return forward;
            if (local == Vector3.back) return -forward;
            return local;
        }
    }
}
