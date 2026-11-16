using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Draws every open player menu with Unity's IMGUI: big buttons, the focused one highlighted.
    /// It only RENDERS Core's MenuModel; navigation lives in the model and is driven by each
    /// player's controller (IPlayerInput). Mouse clicks still work and go through the same model.
    /// Contains no game-specific code; where each player's panel goes comes from GameBindings.
    /// </summary>
    internal sealed class HouseMenuWindow
    {
        private const float Margin = 16f;
        private const float MaxPanelWidth = 560f;
        private const float MaxPanelHeight = 760f;
        private const float TitleHeight = 40f;
        private const float RowHeight = 64f;
        private const float RowSpacing = 4f;
        private const float TabRowHeight = 52f;
        private const float FooterHeight = 110f; // hint + status text

        private static readonly Color FocusColor = Color.yellow;

        private readonly SessionManager _sessions;

        private GUIStyle _bigButton;
        private GUIStyle _text;

        // Mouse clicks are applied after drawing, so the menu doesn't change halfway through an IMGUI pass.
        private PlayerSession _clickSession;
        private int _clickTab = -1;
        private int _clickItem = -1;

        // The game hides and locks the mouse during play. Save its state when a menu opens, put it back when all close.
        private bool _cursorSaved;
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock;

        public HouseMenuWindow(SessionManager sessions)
        {
            _sessions = sessions;
        }

        /// <summary>Call every frame (from Update) to save or restore the mouse state.</summary>
        public void UpdateCursor()
        {
            bool anyOpen = _sessions.AnyOpen;
            if (anyOpen && !_cursorSaved)
            {
                _savedCursorVisible = Cursor.visible;
                _savedCursorLock = Cursor.lockState;
                _cursorSaved = true;
            }
            else if (!anyOpen && _cursorSaved)
            {
                Cursor.visible = _savedCursorVisible;
                Cursor.lockState = _savedCursorLock;
                _cursorSaved = false;
            }
        }

        /// <summary>Call from OnGUI.</summary>
        public void Draw()
        {
            if (!_sessions.AnyOpen)
            {
                return;
            }

            // Every frame, because the game may re-lock the mouse on its own.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureStyles();
            int playerCount = _sessions.Count;
            foreach (PlayerSession session in _sessions.Sessions)
            {
                if (session.IsOpen)
                {
                    DrawPanel(session, GameBindings.ScreenAreaFor(session.PlayerIndex, playerCount), playerCount);
                }
            }

            ApplyPendingClick();
        }

        private void EnsureStyles()
        {
            if (_bigButton != null)
            {
                return;
            }

            _bigButton = new GUIStyle(GUI.skin.button) { fontSize = 26 };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
        }

        private void DrawPanel(PlayerSession session, Rect area, int playerCount)
        {
            MenuModel menu = session.Menu;
            float width = System.Math.Min(MaxPanelWidth, area.width - 2 * Margin);
            float height = System.Math.Min(MaxPanelHeight, area.height - 2 * Margin);
            var panel = new Rect(area.x + Margin, area.y + Margin, width, height);

            string title = menu.CurrentPage.Title;
            if (playerCount > 1)
            {
                title = "Player " + (session.PlayerIndex + 1) + ": " + title;
            }
            GUI.Box(panel, title);

            var inner = new Rect(panel.x + Margin, panel.y + TitleHeight, panel.width - 2 * Margin, panel.height - TitleHeight - Margin);
            GUILayout.BeginArea(inner);

            float listHeight = inner.height - FooterHeight;
            if (menu.CurrentPage.Tabs.Count > 1)
            {
                DrawTabs(session);
                listHeight -= TabRowHeight;
            }
            DrawItems(session, listHeight);

            GUILayout.Space(8);
            GUILayout.Label(menu.Depth > 1 ? "A = pick    B = back" : "A = pick    B = close", _text);
            GUILayout.Label(session.Status ?? string.Empty, _text);
            GUILayout.EndArea();
        }

        private void DrawTabs(PlayerSession session)
        {
            MenuModel menu = session.Menu;
            Color normal = GUI.backgroundColor;

            GUILayout.BeginHorizontal();
            for (int i = 0; i < menu.CurrentPage.Tabs.Count; i++)
            {
                bool current = i == menu.TabIndex;
                string name = menu.CurrentPage.Tabs[i].Title;
                GUI.backgroundColor = current ? FocusColor : normal;
                if (GUILayout.Button(current ? "[ " + name + " ]" : name, _bigButton, GUILayout.Height(TabRowHeight - 8)))
                {
                    QueueClick(session, i, -1);
                }
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = normal;
        }

        private void DrawItems(PlayerSession session, float listHeight)
        {
            MenuModel menu = session.Menu;
            int count = menu.Items.Count;
            int focus = menu.FocusedIndex;

            // Show as many rows as fit (split-screen quarters are small), scrolled so the focused one is visible.
            int visible = System.Math.Max(1, (int)System.Math.Floor(listHeight / (RowHeight + RowSpacing)));
            int first = focus >= visible ? focus - visible + 1 : 0;
            int last = System.Math.Min(count, first + visible);

            Color normal = GUI.backgroundColor;
            for (int i = first; i < last; i++)
            {
                bool focused = i == focus;
                string label = menu.Items[i].Label;
                if (menu.Items[i].SubPage != null)
                {
                    label += "  >";
                }

                GUI.backgroundColor = focused ? FocusColor : normal;
                if (GUILayout.Button(focused ? "> " + label + " <" : label, _bigButton, GUILayout.Height(RowHeight)))
                {
                    QueueClick(session, -1, i);
                }
                GUILayout.Space(RowSpacing);
            }
            GUI.backgroundColor = normal;
        }

        private void QueueClick(PlayerSession session, int tab, int item)
        {
            _clickSession = session;
            _clickTab = tab;
            _clickItem = item;
        }

        private void ApplyPendingClick()
        {
            if (_clickSession == null)
            {
                return;
            }

            PlayerSession session = _clickSession;
            int tab = _clickTab;
            int item = _clickItem;
            _clickSession = null;
            _clickTab = -1;
            _clickItem = -1;

            if (tab >= 0)
            {
                session.Menu.SelectTab(tab);
            }
            if (item >= 0)
            {
                session.Menu.Activate(item);
            }
        }
    }
}
