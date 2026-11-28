using System;
using System.Collections.Generic;

namespace WobblyMods.Core
{
    /// <summary>
    /// Everything that belongs to one local (split screen) player: their input, their own
    /// menu, their own undo history, and a status line to show them.
    /// </summary>
    public sealed class PlayerSession
    {
        /// <summary>Actions polled each frame, in this order.</summary>
        private static readonly MenuAction[] PolledActions =
        {
            MenuAction.ToggleMenu, MenuAction.Back, MenuAction.Select,
            MenuAction.Up, MenuAction.Down, MenuAction.Left, MenuAction.Right,
            MenuAction.TabPrev, MenuAction.TabNext
        };

        private readonly UndoStack _undo = new UndoStack();
        private readonly MenuModel _menu;

        /// <param name="playerIndex">0 to 3: which local player (0 = player 1).</param>
        /// <param name="input">This player's controller.</param>
        /// <param name="buildRootPage">
        /// Builds this player's menu. Gets the session, so item actions can use this
        /// player's Undo and Status (and nobody else's).
        /// </param>
        public PlayerSession(int playerIndex, IPlayerInput input, Func<PlayerSession, MenuPage> buildRootPage)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (buildRootPage == null)
            {
                throw new ArgumentNullException("buildRootPage");
            }

            PlayerIndex = playerIndex;
            Input = input;
            _menu = new MenuModel(buildRootPage(this));
        }

        /// <summary>0-based local player number (0 = player 1).</summary>
        public int PlayerIndex { get; private set; }

        public IPlayerInput Input { get; set; }

        public MenuModel Menu
        {
            get { return _menu; }
        }

        public UndoStack Undo
        {
            get { return _undo; }
        }

        public bool IsOpen
        {
            get { return _menu.IsOpen; }
        }

        /// <summary>A short message for this player ("Placed Couch."). Shown by the renderer.</summary>
        public string Status { get; set; }

        /// <summary>Free slot for a mod's own per-player data (for example the next color to use).</summary>
        public object UserData { get; set; }

        /// <summary>
        /// Reads this player's input and drives their menu. Call once per frame.
        /// Returns true if the menu used any input this frame (the game should then ignore it).
        /// </summary>
        public bool Update()
        {
            bool used = false;
            foreach (MenuAction action in PolledActions)
            {
                if (Input.WasPressed(action) && _menu.Handle(action))
                {
                    used = true;
                }
            }
            return used;
        }
    }

    /// <summary>Holds up to four local players and updates each one from its own input.</summary>
    public sealed class SessionManager
    {
        public const int MaxPlayers = 4;

        private readonly PlayerSession[] _slots = new PlayerSession[MaxPlayers];
        private readonly Func<PlayerSession, MenuPage> _buildRootPage;

        /// <param name="buildRootPage">Builds each new player's menu (called once per player).</param>
        public SessionManager(Func<PlayerSession, MenuPage> buildRootPage)
        {
            if (buildRootPage == null)
            {
                throw new ArgumentNullException("buildRootPage");
            }
            _buildRootPage = buildRootPage;
        }

        /// <summary>Raised after a player joins.</summary>
        public event Action<PlayerSession> PlayerJoined;

        /// <summary>Raised after a player leaves (their menu is closed first).</summary>
        public event Action<PlayerSession> PlayerLeft;

        /// <summary>Number of players currently joined.</summary>
        public int Count
        {
            get
            {
                int n = 0;
                foreach (PlayerSession s in _slots)
                {
                    if (s != null) n++;
                }
                return n;
            }
        }

        /// <summary>Joined players in player-index order.</summary>
        public IEnumerable<PlayerSession> Sessions
        {
            get
            {
                foreach (PlayerSession s in _slots)
                {
                    if (s != null) yield return s;
                }
            }
        }

        /// <summary>True if any player's menu is open.</summary>
        public bool AnyOpen
        {
            get
            {
                foreach (PlayerSession s in _slots)
                {
                    if (s != null && s.IsOpen) return true;
                }
                return false;
            }
        }

        /// <summary>The player in that slot (0 to 3), or null.</summary>
        public PlayerSession Get(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
            {
                return null;
            }
            return _slots[playerIndex];
        }

        /// <summary>Adds a player in the given slot (0 to 3). Fails if the slot is taken.</summary>
        public PlayerSession Join(int playerIndex, IPlayerInput input)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException("playerIndex", "Player index must be 0 to " + (MaxPlayers - 1) + ".");
            }
            if (_slots[playerIndex] != null)
            {
                throw new InvalidOperationException("Player " + (playerIndex + 1) + " has already joined.");
            }

            var session = new PlayerSession(playerIndex, input, _buildRootPage);
            _slots[playerIndex] = session;
            Action<PlayerSession> handler = PlayerJoined;
            if (handler != null) handler(session);
            return session;
        }

        /// <summary>Adds a player in the lowest free slot. Returns null if four players have joined.</summary>
        public PlayerSession Join(IPlayerInput input)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_slots[i] == null)
                {
                    return Join(i, input);
                }
            }
            return null;
        }

        /// <summary>Removes a player. Their undo history is kept on the returned session but no longer updated.</summary>
        public PlayerSession Leave(int playerIndex)
        {
            PlayerSession session = Get(playerIndex);
            if (session == null)
            {
                return null;
            }

            session.Menu.Close();
            _slots[playerIndex] = null;
            Action<PlayerSession> handler = PlayerLeft;
            if (handler != null) handler(session);
            return session;
        }

        /// <summary>Updates every player from their own input. Call once per frame.</summary>
        public void Update()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerSession s = _slots[i];
                if (s != null)
                {
                    s.Update();
                }
            }
        }
    }
}
