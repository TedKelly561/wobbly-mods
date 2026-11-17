using UnityEngine;
using WobblyMods.Core;
using WobblyMods.HouseMenu.Build;

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

        private HousePlayerMenu(PlayerSession session)
        {
            _session = session;
            _session.Status = "Look at something in your house, then pick a button.";
        }

        /// <summary>Pass to SessionManager: builds the menu for each player who joins.</summary>
        public static MenuPage BuildRootPage(PlayerSession session)
        {
            return new HousePlayerMenu(session).Build();
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

            foreach (string category in FurnitureCatalog.Categories)
            {
                MenuTab tab = root.AddTab(category);
                foreach (CatalogItem item in FurnitureCatalog.InCategory(category))
                {
                    CatalogItem captured = item;
                    tab.Add(item.DisplayName, () => Spawn(captured)).Icon = item.Id;
                }
            }

            // The tab row only fits about five tabs at the menu's font size (MaxPanelWidth 560, fontSize 26),
            // so Undo and Reset house moved from their own tab into Change, making room for Build.
            MenuTab change = root.AddTab("Change");
            // Skylar's request: no-clip in her own world, like the sandbox has.
            change.Add("No-clip (fly through walls)", () => { _session.Status = Powers.ToggleNoClip(Player); });
            change.Add("Undo", () => UndoLast(_session));
            change.Add("Left", () => Nudge(CameraRelative(Vector3.left)));
            change.Add("Right", () => Nudge(CameraRelative(Vector3.right)));
            change.Add("Closer", () => Nudge(CameraRelative(Vector3.forward)));
            change.Add("Farther", () => Nudge(CameraRelative(Vector3.back)));
            change.Add("Up", () => Nudge(Vector3.up));
            change.Add("Down", () => Nudge(Vector3.down));
            change.Add("New color", Recolor);
            change.Add("Copy", CopyTarget);
            change.Add("Turn", () => WithTarget(t => { ObjectEdits.Turn(t.transform, 45f, _session.Undo); return "Turned " + t.name + "."; }));
            change.Add("Bigger", () => WithTarget(t => ObjectEdits.Resize(t, 1.25f, _session.Undo) ? "Bigger!" : "That's as big as it goes."));
            change.Add("Smaller", () => WithTarget(t => ObjectEdits.Resize(t, 0.8f, _session.Undo) ? "Smaller!" : "That's as small as it goes."));

            // Save and put back everything placed with the menu (it would vanish when the game closes).
            var savePage = new MenuPage("Save house");
            var loadPage = new MenuPage("Load house");
            for (int i = 1; i <= HouseSave.Slots; i++)
            {
                int slot = i;
                savePage.MainTab().Add("House " + slot + (HouseSave.SlotExists(slot) ? " (replace)" : " (empty)"), () =>
                {
                    _session.Status = HouseSave.Save(slot);
                    _session.Menu.Back();
                });

                // Loading replaces what's placed now, so it asks first.
                var confirmLoad = new MenuPage("Load house " + slot + "?");
                confirmLoad.MainTab().Add("No, keep what's here", () => _session.Menu.Back());
                confirmLoad.MainTab().Add("Yes, load house " + slot, () =>
                {
                    _session.Status = HouseSave.Load(slot, Player, _session.Undo);
                    _session.Menu.Back();
                    _session.Menu.Back();
                });
                loadPage.MainTab().AddSubPage("House " + slot + (HouseSave.SlotExists(slot) ? "" : " (empty)"), confirmLoad);
            }
            change.AddSubPage("Save house", savePage);
            change.AddSubPage("Load house", loadPage);

            // Reset asks first, so a stray A press can't wipe everything.
            var confirmReset = new MenuPage("Reset house?");
            MenuTab confirm = confirmReset.MainTab();
            confirm.Add("No, keep it", () => _session.Menu.Back());
            confirm.Add("Yes, reset", () =>
            {
                ResetHouse();
                _session.Menu.Back();
            });

            change.AddSubPage("Reset house", confirmReset);

            // Sandbox build mode: blocks on a grid, with its own per-player undo (BuildMode.Core).
            BuildMenu.AddTo(root, _session);

            return root;
        }

        private void Spawn(CatalogItem item)
        {
            _session.Status = "Placing " + item.DisplayName + "...";
            PlayerSession session = _session;
            GameBindings.Spawn(item, Player, (placed, problem) =>
            {
                if (placed == null)
                {
                    session.Status = "Couldn't place " + item.DisplayName + ".";
                    Plugin.Log.LogWarning("Player " + (Player + 1) + " couldn't place " + item.Id + ": " + problem);
                    return;
                }

                session.Undo.Push("placed " + item.DisplayName, () => ObjectEdits.Remove(placed));
                session.Status = "Placed " + item.DisplayName + ".";
            });
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
            GameObject target = CurrentTarget();
            if (target == null) return;
            CatalogItem item = FurnitureCatalog.Find(GameBindings.CatalogIdOf(target));
            if (item == null)
            {
                _session.Status = "That can't be copied.";
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

        private void Recolor()
        {
            GameObject target = CurrentTarget();
            if (target == null)
            {
                return;
            }

            Color color = Palette[_colorIndex];
            _colorIndex = (_colorIndex + 1) % Palette.Length;
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
