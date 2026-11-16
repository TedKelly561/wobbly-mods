using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// BepInEx entry point. Owns the players (Core's SessionManager), feeds them input each frame,
    /// and hands drawing to HouseMenuWindow.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "wobblymods.housemenu";
        public const string PluginName = "Wobbly House Menu";
        public const string PluginVersion = "0.3.0";

        internal static ManualLogSource Log;

        private ConfigEntry<KeyCode> _menuKey;
        private ConfigEntry<KeyCode> _undoKey;
        private SessionManager _sessions;
        private PlayerSession _keyboardPlayer;
        private HouseMenuWindow _window;
        private KeyboardPlayerInput _keyboard;

        // Controllers: one Rewired input per split-screen player, refreshed once a second.
        private readonly RewiredMenuActions _actions = new RewiredMenuActions();
        private readonly Dictionary<int, RewiredPlayerInput> _controllers = new Dictionary<int, RewiredPlayerInput>();
        private readonly object[] _rewiredPlayers = new object[SessionManager.MaxPlayers];
        private float _nextPlayerSync;

        // The game's own PlayerControllers (by split-screen index), and whether we've paused each one's controls.
        private readonly object[] _playerControllers = new object[SessionManager.MaxPlayers];
        private readonly object[] _pauseHandles = new object[SessionManager.MaxPlayers];
        private readonly object[] _pausedController = new object[SessionManager.MaxPlayers];

        private void Awake()
        {
            Log = Logger;
            _menuKey = Config.Bind("Controls", "MenuKey", KeyCode.F1,
                "Keyboard backup: opens and closes player 1's house menu.");
            _undoKey = Config.Bind("Controls", "UndoKey", KeyCode.F2,
                "Keyboard backup: undoes player 1's most recent house change.");

            // Rewired action names. Defaults are Rewired's standard UI names: a GUESS until
            // Scout's scene report lists the game's real actions (PROJECT_NOTES.md T18).
            _actions.Vertical = Config.Bind("Controller", "VerticalAction", "UIVertical",
                "Rewired axis for menu up/down (up = positive).").Value;
            _actions.Horizontal = Config.Bind("Controller", "HorizontalAction", "UIHorizontal",
                "Rewired axis for menu left/right (right = positive).").Value;
            _actions.Select = Config.Bind("Controller", "SelectAction", "UISubmit",
                "Rewired button that picks the highlighted item (usually A).").Value;
            _actions.Back = Config.Bind("Controller", "BackAction", "UICancel",
                "Rewired button that goes back or closes (usually B).").Value;
            _actions.Toggle = Config.Bind("Controller", "ToggleAction", "",
                "Rewired button that opens and closes the menu. Empty = keyboard only until set.").Value;

            _sessions = new SessionManager(HousePlayerMenu.BuildRootPage);

            // Player 1 always exists, driven by the keyboard backup; controllers are added by SyncPlayers.
            _keyboard = new KeyboardPlayerInput(() => _menuKey.Value);
            _keyboardPlayer = _sessions.Join(0, _keyboard);

            _window = new HouseMenuWindow(_sessions);
            Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextPlayerSync)
            {
                _nextPlayerSync = Time.unscaledTime + 1f;
                SyncPlayers();
            }

            foreach (RewiredPlayerInput controller in _controllers.Values)
            {
                controller.Tick();
            }

            _sessions.Update();
            UpdateGamePause();

            if (Input.GetKeyDown(_undoKey.Value))
            {
                HousePlayerMenu.UndoLast(_keyboardPlayer);
            }

            _window.UpdateCursor();
        }

        /// <summary>
        /// Gives each split-screen player a menu driven by their own controller, and removes menus
        /// for players who left. Player 1 keeps the keyboard as well.
        /// </summary>
        private void SyncPlayers()
        {
            List<object> locals = GameReflection.LocalPlayerControllers();
            int count = System.Math.Min(locals.Count, SessionManager.MaxPlayers);

            for (int i = 0; i < SessionManager.MaxPlayers; i++)
            {
                _playerControllers[i] = i < count ? locals[i] : null;
                _rewiredPlayers[i] = i < count ? GameReflection.RewiredPlayerOf(locals[i]) : null;
            }

            for (int i = 0; i < count; i++)
            {
                if (_controllers.ContainsKey(i) || _rewiredPlayers[i] == null)
                {
                    continue;
                }

                int index = i;
                var controller = new RewiredPlayerInput(() => _rewiredPlayers[index], _actions);
                _controllers[i] = controller;

                if (i == 0)
                {
                    _keyboardPlayer.Input = new EitherPlayerInput(_keyboard, controller);
                }
                else
                {
                    _sessions.Join(i, controller);
                }
                Log.LogInfo("Controller menu ready for player " + (i + 1) + ".");
            }

            // Players who left split screen: drop their controller and menu (player 1 stays).
            var gone = new List<int>();
            foreach (int i in _controllers.Keys)
            {
                if (i >= count) gone.Add(i);
            }
            foreach (int i in gone)
            {
                _controllers.Remove(i);
                if (i == 0)
                {
                    _keyboardPlayer.Input = _keyboard;
                }
                else
                {
                    _sessions.Leave(i);
                }
            }
        }

        /// <summary>
        /// While a player's menu is open, pause that player's game controls (so A doesn't also jump)
        /// and show their cursor, using the game's own per-player calls. Every pause is matched by a resume
        /// with the same handle, including when a player leaves.
        /// </summary>
        private void UpdateGamePause()
        {
            for (int i = 0; i < SessionManager.MaxPlayers; i++)
            {
                PlayerSession session = _sessions.Get(i);
                object controller = _playerControllers[i];
                bool wantPaused = session != null && session.IsOpen && controller != null;

                // Resume if the menu closed, or the paused controller is no longer this player's.
                if (_pausedController[i] != null && (!wantPaused || !ReferenceEquals(_pausedController[i], controller)))
                {
                    GameReflection.SetGameplayPaused(_pausedController[i], _pauseHandles[i], false);
                    _pausedController[i] = null;
                }

                if (wantPaused && _pausedController[i] == null)
                {
                    if (_pauseHandles[i] == null) _pauseHandles[i] = new object();
                    if (GameReflection.SetGameplayPaused(controller, _pauseHandles[i], true))
                    {
                        _pausedController[i] = controller;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < SessionManager.MaxPlayers; i++)
            {
                if (_pausedController[i] != null)
                {
                    GameReflection.SetGameplayPaused(_pausedController[i], _pauseHandles[i], false);
                    _pausedController[i] = null;
                }
            }
        }

        private void OnGUI()
        {
            _window.Draw();
        }
    }
}
