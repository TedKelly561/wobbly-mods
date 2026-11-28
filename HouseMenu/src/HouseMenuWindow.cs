using System.Collections.Generic;
using UnityEngine;
using WobblyMods.Core;
using WobblyMods.HouseMenu.Icons;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Draws every open player menu with Unity's IMGUI, made for a child who can't read yet:
    /// - a row of colored tab buttons, each with a symbol (sofa, ball, paw, wand, hammer) and a small word,
    ///   switched with quick LB/RB taps;
    /// - a grid of big square picture tiles (a photo of the real item, or a symbol for actions), with a small
    ///   word underneath;
    /// - the panel, tab stripe and tiles use the current tab's color (sub-pages keep the color of the tab they
    ///   were opened from); the focused tile gets a thick, gently pulsing yellow glow.
    /// It only RENDERS Core's MenuModel; navigation lives in the model and is driven by each player's controller.
    /// Mouse clicks still work and go through the same model. The panel fits each player's own screen area
    /// (split-screen halves and quarters), and the grid scrolls so the focused row is always visible.
    /// </summary>
    internal sealed class HouseMenuWindow
    {
        private const float Margin = 12f;
        private const float MaxPanelWidth = 720f;
        private const float MaxPanelHeight = 900f;
        private const float PanelBorder = 6f;
        private const float Pad = 10f;
        private const float TitleHeight = 26f;
        private const float FooterHeight = 70f;
        private const float Gap = 12f;
        private const float MaxTile = 200f;
        private const float MinTile = 56f;
        private const float BumperWidth = 34f;

        private static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.09f, 0.9f);
        private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color AButton = new Color(0.25f, 0.72f, 0.25f);
        private static readonly Color BButton = new Color(0.88f, 0.25f, 0.22f);

        private sealed class Scroll
        {
            public MenuPage Page;
            public int Tab;
            public int FirstRow;
        }

        private readonly SessionManager _sessions;
        private readonly Dictionary<PlayerSession, Scroll> _scroll = new Dictionary<PlayerSession, Scroll>();

        private GUIStyle _text;
        private GUIStyle _letter;
        private readonly Dictionary<FitKey, int> _fitted = new Dictionary<FitKey, int>();

        // Small and quiet in the panel's corner, so a grown-up can tell which version is running.
        private static readonly string VersionText = "v" + Plugin.PluginVersion;
        private static readonly Color VersionColor = new Color(1f, 1f, 1f, 0.4f);

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
            if (_text != null)
            {
                return;
            }

            _text = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
                fontSize = 16,
                padding = new RectOffset(2, 2, 0, 0)
            };
            _text.normal.textColor = Color.white;
            _letter = new GUIStyle(_text) { fontStyle = FontStyle.Bold };
        }

        // ---------- Panel ----------

        private void DrawPanel(PlayerSession session, Rect area, int playerCount)
        {
            MenuModel menu = session.Menu;
            MenuTab rootTab = menu.RootTab;
            Color tabColor = TabTheme.ColorFor(rootTab != null ? rootTab.Title : null);

            float width = Mathf.Min(MaxPanelWidth, area.width - 2 * Margin);
            float height = Mathf.Min(MaxPanelHeight, area.height - 2 * Margin);
            var panel = new Rect(area.x + Margin, area.y + Margin, width, height);
            Fill(panel, PanelColor, 14f);
            Outline(panel, tabColor, PanelBorder, 14f);

            var inner = new Rect(panel.x + PanelBorder + Pad, panel.y + PanelBorder + 4f,
                                 panel.width - 2 * (PanelBorder + Pad), panel.height - 2 * PanelBorder - 4f - Pad);
            float y = inner.y;

            // Title (small: for grown-ups). Pages deeper than the first show their own title ("Reset house?").
            string title = menu.Depth > 1 ? menu.CurrentPage.Title : menu.RootPage.Title;
            if (playerCount > 1)
            {
                title = "Player " + (session.PlayerIndex + 1) + ":  " + title;
            }
            Label(new Rect(inner.x, y, inner.width, TitleHeight), title, 16, TabTheme.Light(tabColor, 0.3f), TextAnchor.MiddleLeft);
            y += TitleHeight;

            if (menu.CurrentPage.Tabs.Count > 1)
            {
                float tabHeight = Mathf.Clamp(inner.height * 0.16f, 46f, 92f);
                DrawTabs(session, new Rect(inner.x, y, inner.width, tabHeight));
                y += tabHeight;
                Fill(new Rect(inner.x, y, inner.width, 5f), tabColor, 2f); // stripe joining the tab to its tiles
                y += 5f + Gap;
            }
            else if (menu.Depth > 1)
            {
                Fill(new Rect(inner.x, y, inner.width, 5f), tabColor, 2f);
                y += 5f + Gap;
            }

            var items = new Rect(inner.x, y, inner.width, inner.yMax - FooterHeight - y);
            if (items.height > 20f)
            {
                if (menu.Columns > 1)
                {
                    DrawGrid(session, items, tabColor);
                }
                else
                {
                    DrawList(session, items, tabColor);
                }
            }

            DrawFooter(session, new Rect(inner.x, inner.yMax - FooterHeight, inner.width, FooterHeight));
        }

        // ---------- Tabs ----------

        private void DrawTabs(PlayerSession session, Rect row)
        {
            MenuModel menu = session.Menu;
            IList<MenuTab> tabs = menu.CurrentPage.Tabs;

            // "LB" and "RB" at the ends, so grown-ups can explain "tap the bumpers".
            Bumper(new Rect(row.x, row.y + row.height * 0.25f, BumperWidth - 4f, row.height * 0.5f), "LB");
            Bumper(new Rect(row.xMax - BumperWidth + 4f, row.y + row.height * 0.25f, BumperWidth - 4f, row.height * 0.5f), "RB");

            float x = row.x + BumperWidth;
            float w = (row.width - 2 * BumperWidth - (tabs.Count - 1) * 6f) / tabs.Count;
            for (int i = 0; i < tabs.Count; i++)
            {
                MenuTab tab = tabs[i];
                bool current = i == menu.TabIndex;
                Color color = TabTheme.ColorFor(tab.Title);
                var r = new Rect(x + i * (w + 6f), row.y, w, row.height);
                if (!current)
                {
                    r = new Rect(r.x + 3f, r.y + 6f, r.width - 6f, r.height - 6f); // the current tab stands taller
                    color = Color.Lerp(color, Color.black, 0.35f);
                }

                Fill(r, color, 10f);
                if (current)
                {
                    Outline(r, Color.white, 4f, 10f);
                }

                float wordHeight = Mathf.Clamp(r.height * 0.26f, 13f, 22f);
                string iconKey = tab.Icon ?? TabTheme.IconFor(tab.Title);
                Texture2D icon = SymbolIcons.Get(iconKey);
                var iconRect = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - wordHeight - 6f);
                if (icon != null)
                {
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 0f, current ? Color.white : new Color(1f, 1f, 1f, 0.7f), 0f, 0f);
                }
                else
                {
                    Letter(iconRect, tab.Title);
                }
                Label(new Rect(r.x, r.yMax - wordHeight - 2f, r.width, wordHeight), tab.Title,
                      Mathf.RoundToInt(Mathf.Clamp(wordHeight * 0.72f, 10f, 16f)), Color.white, TextAnchor.MiddleCenter);

                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    QueueClick(session, i, -1);
                }
            }
        }

        private void Bumper(Rect r, string text)
        {
            Fill(r, new Color(0.25f, 0.25f, 0.28f, 1f), 8f);
            Outline(r, new Color(1f, 1f, 1f, 0.6f), 2f, 8f);
            Label(r, text, 13, Color.white, TextAnchor.MiddleCenter);
        }

        // ---------- Grid of tiles ----------

        private void DrawGrid(PlayerSession session, Rect area, Color tabColor)
        {
            MenuModel menu = session.Menu;
            IList<MenuItem> items = menu.Items;
            int count = items.Count;
            if (count == 0) return;

            int columns = menu.Columns;
            int rows = (count + columns - 1) / columns;

            // Square tiles, as big as fit across; at least two rows on screen when there's room for decent tiles.
            float tile = Mathf.Min(MaxTile, (area.width - (columns - 1) * Gap) / columns);
            int visibleRows = Mathf.Max(1, Mathf.FloorToInt((area.height + Gap) / (tile + Gap)));
            if (visibleRows < 2 && rows >= 2 && (area.height - Gap) / 2f >= MinTile)
            {
                tile = (area.height - Gap) / 2f;
                visibleRows = 2;
            }
            tile = Mathf.Min(tile, area.height);
            visibleRows = Mathf.Min(visibleRows, rows);

            int focus = menu.FocusedIndex;
            int first = FirstVisibleRow(session, focus < 0 ? 0 : focus / columns, visibleRows, rows);

            float gridWidth = columns * tile + (columns - 1) * Gap;
            float x0 = area.x + (area.width - gridWidth) * 0.5f;
            Rect focusedRect = new Rect();
            for (int r = first; r < first + visibleRows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int i = r * columns + c;
                    if (i >= count) break;
                    var rect = new Rect(x0 + c * (tile + Gap), area.y + (r - first) * (tile + Gap), tile, tile);
                    if (i == focus)
                    {
                        focusedRect = rect; // drawn last, so its glow sits on top of its neighbours
                        continue;
                    }
                    DrawTile(items[i], rect, tabColor, false);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) QueueClick(session, -1, i);
                }
            }
            if (focus >= 0 && focus / columns >= first && focus / columns < first + visibleRows)
            {
                DrawTile(items[focus], focusedRect, tabColor, true);
                if (GUI.Button(focusedRect, GUIContent.none, GUIStyle.none)) QueueClick(session, -1, focus);
            }

            // Little arrows when there are more rows above or below.
            float arrow = 26f;
            if (first > 0)
            {
                Symbol(new Rect(area.xMax - arrow, area.y - 4f, arrow, arrow), "sym:up", Color.white);
            }
            if (first + visibleRows < rows)
            {
                Symbol(new Rect(area.xMax - arrow, area.y + visibleRows * (tile + Gap) - Gap - arrow + 4f, arrow, arrow), "sym:down", Color.white);
            }
        }

        /// <summary>Keeps the focused row on screen, scrolling as little as possible; resets on a new page or tab.</summary>
        private int FirstVisibleRow(PlayerSession session, int focusRow, int visibleRows, int rows)
        {
            Scroll s;
            if (!_scroll.TryGetValue(session, out s))
            {
                s = new Scroll();
                _scroll[session] = s;
            }
            MenuModel menu = session.Menu;
            if (s.Page != menu.CurrentPage || s.Tab != menu.TabIndex)
            {
                s.Page = menu.CurrentPage;
                s.Tab = menu.TabIndex;
                s.FirstRow = 0;
            }
            if (focusRow < s.FirstRow) s.FirstRow = focusRow;
            if (focusRow >= s.FirstRow + visibleRows) s.FirstRow = focusRow - visibleRows + 1;
            s.FirstRow = Mathf.Clamp(s.FirstRow, 0, Mathf.Max(0, rows - visibleRows));
            return s.FirstRow;
        }

        private void DrawTile(MenuItem item, Rect rect, Color tabColor, bool focused)
        {
            float radius = Mathf.Clamp(rect.width * 0.1f, 6f, 18f);
            if (focused)
            {
                // Thick bright glow that gently pulses: easy to spot for a 5-year-old.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                float grow = rect.width * 0.04f;
                rect = new Rect(rect.x - grow, rect.y - grow, rect.width + 2 * grow, rect.height + 2 * grow);
                Fill(Expand(rect, 12f), new Color(TabTheme.Glow.r, TabTheme.Glow.g, TabTheme.Glow.b, 0.25f + 0.2f * pulse), radius + 12f);
                Fill(Expand(rect, 7f), TabTheme.Glow, radius + 7f);
            }

            float labelHeight = Mathf.Clamp(rect.height * 0.2f, 16f, 30f);
            var picture = new Rect(rect.x, rect.y, rect.width, rect.height - labelHeight);

            bool isSymbol;
            Texture2D tex = PictureFor(item, out isSymbol);
            // Photos carry their own light background; symbols are white on the tab color; letters too.
            Fill(rect, tex != null && !isSymbol ? TabTheme.Light(tabColor, 0.6f) : tabColor, radius);
            if (tex != null)
            {
                float inset = isSymbol ? rect.width * 0.14f : 3f;
                GUI.DrawTexture(Expand(picture, -inset), tex, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, isSymbol ? 0f : radius * 0.7f);
            }
            else
            {
                Letter(Expand(picture, -rect.width * 0.08f), item.Label);
            }

            // The small word underneath, on a darker strip of the tab color.
            var strip = new Rect(rect.x, rect.yMax - labelHeight, rect.width, labelHeight);
            Fill(strip, TabTheme.Dark(tabColor, 0.35f), radius);
            Fill(new Rect(strip.x, strip.y, strip.width, Mathf.Min(radius, strip.height)), TabTheme.Dark(tabColor, 0.35f), 0f);
            FittedLabel(Expand(strip, -3f), item.Label, Mathf.RoundToInt(Mathf.Clamp(rect.width * 0.11f, 11f, 18f)));

            if (item.SubPage != null)
            {
                // "There's more inside": a small arrow in the corner.
                float s = Mathf.Clamp(rect.width * 0.2f, 16f, 30f);
                Symbol(new Rect(rect.xMax - s - 3f, rect.y + 3f, s, s), "sym:right", Color.white);
            }

            Outline(rect, focused ? Color.white : TabTheme.Dark(tabColor, 0.3f), focused ? 3f : 2f, radius);
        }

        // ---------- Plain list (pages left as Columns = 1) ----------

        private void DrawList(PlayerSession session, Rect area, Color tabColor)
        {
            MenuModel menu = session.Menu;
            IList<MenuItem> items = menu.Items;
            int count = items.Count;
            if (count == 0) return;

            const float rowHeight = 64f;
            const float spacing = 6f;
            int visible = Mathf.Max(1, Mathf.FloorToInt((area.height + spacing) / (rowHeight + spacing)));
            int focus = menu.FocusedIndex;
            int first = FirstVisibleRow(session, Mathf.Max(0, focus), Mathf.Min(visible, count), count);
            for (int i = first; i < Mathf.Min(count, first + visible); i++)
            {
                var r = new Rect(area.x + 8f, area.y + (i - first) * (rowHeight + spacing), area.width - 16f, rowHeight);
                bool focused = i == focus;
                if (focused) Fill(Expand(r, 6f), TabTheme.Glow, 14f);
                Fill(r, tabColor, 10f);
                bool isSymbol;
                Texture2D tex = PictureFor(items[i], out isSymbol);
                var icon = new Rect(r.x + 6f, r.y + 6f, rowHeight - 12f, rowHeight - 12f);
                if (tex != null) GUI.DrawTexture(icon, tex, ScaleMode.ScaleToFit);
                else Letter(icon, items[i].Label);
                string label = items[i].Label + (items[i].SubPage != null ? "  >" : "");
                Label(new Rect(icon.xMax + 10f, r.y, r.xMax - icon.xMax - 16f, r.height), label, 20, Color.white, TextAnchor.MiddleLeft);
                Outline(r, focused ? Color.white : TabTheme.Dark(tabColor, 0.3f), focused ? 3f : 2f, 10f);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) QueueClick(session, -1, i);
            }
        }

        // ---------- Footer: button hints and the status line ----------

        private void DrawFooter(PlayerSession session, Rect r)
        {
            MenuModel menu = session.Menu;
            float s = 30f;
            float y = r.y + 4f;
            float x = r.x;

            // A = pick (check), B = back (arrow) or close (X): shown as the controller's colored buttons.
            x = Hint(x, y, s, "A", AButton, "sym:yes");
            x = Hint(x + 14f, y, s, "B", BButton, menu.Depth > 1 ? "sym:back" : "sym:no");

            // The plugin version, at the right end of the hint row (clear of the hints and the status line below).
            if (r.xMax - 70f > x + 8f)
            {
                Label(new Rect(r.xMax - 70f, y, 70f, s), VersionText, 11, VersionColor, TextAnchor.LowerRight);
            }

            Label(new Rect(r.x, y + s + 4f, r.width, r.yMax - y - s - 4f), session.Status ?? string.Empty, 15,
                  new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperLeft, true);
        }

        private float Hint(float x, float y, float size, string letter, Color color, string symbol)
        {
            var button = new Rect(x, y, size, size);
            Fill(button, color, size * 0.5f);
            Label(button, letter, 16, Color.white, TextAnchor.MiddleCenter);
            Symbol(new Rect(button.xMax + 4f, y, size, size), symbol, Color.white);
            return button.xMax + 4f + size;
        }

        // ---------- Drawing helpers ----------

        /// <summary>The item's photo or symbol texture, or null for the letter placeholder.</summary>
        private static Texture2D PictureFor(MenuItem item, out bool isSymbol)
        {
            isSymbol = false;
            string key = item.Icon;
            if (key == null) return null;
            if (SymbolIcons.IsSymbol(key))
            {
                isSymbol = true;
                return SymbolIcons.Get(key);
            }

            Texture2D photo = IconCache.Get(key);
            if (photo != null) return photo;

            string fallback = IconCache.FallbackFor(key);
            if (fallback != null)
            {
                isSymbol = true;
                return SymbolIcons.Get(fallback);
            }
            return null;
        }

        private static Rect Expand(Rect r, float by)
        {
            return new Rect(r.x - by, r.y - by, r.width + 2 * by, r.height + 2 * by);
        }

        private static void Fill(Rect r, Color color, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
        }

        private static void Outline(Rect r, Color color, float width, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, width, radius);
        }

        private static void Symbol(Rect r, string key, Color tint)
        {
            Texture2D tex = SymbolIcons.Get(key);
            if (tex != null && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true, 0f, tint, 0f, 0f);
            }
        }

        /// <summary>The placeholder: the word's first letter, big and white.</summary>
        private void Letter(Rect r, string word)
        {
            string letter = "?";
            if (!string.IsNullOrEmpty(word))
            {
                foreach (char ch in word)
                {
                    if (char.IsLetterOrDigit(ch))
                    {
                        letter = char.ToUpperInvariant(ch).ToString();
                        break;
                    }
                }
            }
            _letter.fontSize = Mathf.RoundToInt(Mathf.Clamp(r.height * 0.7f, 14f, 120f));
            DrawShadowed(r, letter, _letter, Color.white);
        }

        private void Label(Rect r, string text, int size, Color color, TextAnchor anchor, bool wrap = false)
        {
            _text.fontSize = size;
            _text.alignment = anchor;
            _text.wordWrap = wrap;
            DrawShadowed(r, text, _text, color);
            _text.wordWrap = false;
        }

        /// <summary>A label shrunk (down to size 10) until it fits on one line; clipped if it still doesn't.</summary>
        private void FittedLabel(Rect r, string text, int size)
        {
            _text.alignment = TextAnchor.MiddleCenter;
            _text.wordWrap = false;

            // The size that fits is worked out once per word, width and starting size, then remembered: measuring
            // every tile's word again on every draw (several times a frame, for every open menu) was wasted work.
            var key = new FitKey(text, Mathf.RoundToInt(r.width), size);
            int fitted;
            if (!_fitted.TryGetValue(key, out fitted))
            {
                var content = new GUIContent(text);
                _text.fontSize = size;
                while (_text.fontSize > 10 && _text.CalcSize(content).x > r.width)
                {
                    _text.fontSize--;
                }
                fitted = _text.fontSize;
                if (_fitted.Count > 4000) _fitted.Clear(); // far more than the menu has; just a safety net
                _fitted[key] = fitted;
            }
            _text.fontSize = fitted;
            DrawShadowed(r, text, _text, Color.white);
        }

        /// <summary>What a fitted label's size depends on: the word, the room it has (whole pixels) and the size it starts from.</summary>
        private struct FitKey : System.IEquatable<FitKey>
        {
            public readonly string Text;
            public readonly int Width;
            public readonly int Size;

            public FitKey(string text, int width, int size)
            {
                Text = text ?? string.Empty;
                Width = width;
                Size = size;
            }

            public bool Equals(FitKey other)
            {
                return Width == other.Width && Size == other.Size && Text == other.Text;
            }

            public override bool Equals(object obj)
            {
                return obj is FitKey && Equals((FitKey)obj);
            }

            public override int GetHashCode()
            {
                return (Text.GetHashCode() * 397 ^ Width) * 397 ^ Size;
            }
        }

        private static void DrawShadowed(Rect r, string text, GUIStyle style, Color color)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = Shadow;
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        // ---------- Mouse ----------

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
