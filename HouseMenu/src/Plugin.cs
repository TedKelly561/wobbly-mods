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
        public const string PluginVersion = "0.38.0";

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
        private int _lastPlayerCount = -1;
        private readonly bool[] _warnedNoRewired = new bool[SessionManager.MaxPlayers];
        private readonly bool[] _warnedNoPause = new bool[SessionManager.MaxPlayers];
        private bool _warnedBuild;

        // The game's own PlayerControllers (by split-screen index), and whether we've paused each one's controls.
        private readonly object[] _playerControllers = new object[SessionManager.MaxPlayers];
        private readonly object[] _pauseHandles = new object[SessionManager.MaxPlayers];
        private readonly object[] _pausedController = new object[SessionManager.MaxPlayers];

        private void Awake()
        {
            Log = Logger;
            CustomModels.Init();
            Outfits.Init(Config);
            HiddenItems.Load();
            // New key names (0.34.0) so the new defaults apply on PCs that saved the old ones.
            Fun.Init(Config.Bind("Fun", "SwarmFishSize", 0.35f,
                "Length in meters of each catfish in the catfish swarm (was 0.22; the family found that a little small).").Value,
                Config.Bind("Fun", "SwarmFishCount", 100, "How many little catfish one press of Catfish swarm makes (1 to 300).").Value);
            MenuCheck.Init(Config.Bind("Debug", "MenuSelfTest", false,
                "Grown-ups only. true = about 15 seconds after the game world loads, place every menu item once in front of " +
                "player 1, check it stays, remove it, and log \"Menu check: ...\". Items that fail are hidden from the menu " +
                "(listed in BepInEx\\config\\WobblyMods.hidden-items-v2.txt). Set back to false afterwards.").Value);
            ModelPack.Init(Config.Bind("Furniture", "ModelSize", 2.4f,
                "Size multiplier for model-pack furniture (the .obj files in plugins\\WobblyMods\\models\\furniture). " +
                "The log prints each model's size in meters the first time it is placed. Small things (blender, toaster, books...) are 1.8 times bigger again; walls and floors always stay at 2.").Value);
            SavePaths.Init(Config.Bind("Saves", "SaveFolder", "",
                "Folder for saved houses and builds. Empty = BepInEx\\config. Point both PCs at the same cloud-synced " +
                "folder (e.g. %OneDrive%\\WobblyLifeSaves) so Skylar's saves follow her.").Value);
            _menuKey = Config.Bind("Controls", "MenuKey", KeyCode.F1,
                "Keyboard backup: opens and closes player 1's house menu.");
            _undoKey = Config.Bind("Controls", "UndoKey", KeyCode.F2,
                "Keyboard backup: undoes player 1's most recent house change.");

            // Rewired action names, CONFIRMED in Scout report 2. Lists fall back to the movement stick
            // in case the game switches its UI controls off during normal play.
            _actions.Vertical = Config.Bind("Controller", "VerticalAction", "UIVertical,Vertical",
                "Rewired axis (or comma list) for menu up/down (up = positive).").Value;
            _actions.Horizontal = Config.Bind("Controller", "HorizontalAction", "UIHorizontal,Horizontal",
                "Rewired axis (or comma list) for menu left/right (right = positive).").Value;
            _actions.Select = Config.Bind("Controller", "SelectAction", "UISubmit",
                "Rewired button that picks the highlighted item (usually A).").Value;
            _actions.Back = Config.Bind("Controller", "BackAction", "UICancel",
                "Rewired button that goes back or closes (usually B).").Value;
            _actions.Toggle = Config.Bind("Controller", "ToggleAction", "UILB+UIRB,Map",
                "Button or combo that opens and closes the menu. Combos use +, choices use commas. " +
                "Family's choice: LB and RB together, or Map (UILB+UIRB,Map). Empty = keyboard only.").Value;
            _actions.QuickPlace = Config.Bind("Controller", "BuildQuickPlaceAction", "UIRB",
                "Build mode (on, menu closed): quick tap places the current piece at the ghost.").Value;
            _actions.QuickUndo = Config.Bind("Controller", "BuildQuickUndoAction", "UILB",
                "Build mode (on, menu closed): quick tap undoes your last piece.").Value;
            _actions.TabNext = Config.Bind("Controller", "TabNextAction", "UIRB",
                "Menu open: quick tap shows the next tab (holding LB+RB to close never switches tabs).").Value;
            _actions.TabPrev = Config.Bind("Controller", "TabPrevAction", "UILB",
                "Menu open: quick tap shows the previous tab.").Value;
            _actions.ToggleHoldSeconds = Config.Bind("Controller", "ToggleHoldSeconds", 1f,
                "Seconds the button or combo must be held. 1 second keeps a quick tap of Map opening the map.").Value;

            FmodSounds.SetOverrides(
                Config.Bind("Sounds", "FmodTick", "", "Game sound (FMOD event path) when moving in the menu. Empty = choose automatically; the log lists the menu sounds found.").Value,
                Config.Bind("Sounds", "FmodPick", "", "Game sound when picking something. Empty = automatic.").Value,
                Config.Bind("Sounds", "FmodBack", "", "Game sound when going back or closing. Empty = automatic.").Value,
                Config.Bind("Sounds", "FmodTab", "", "Game sound when changing tab or opening the menu. Empty = automatic.").Value);
            MenuSounds.Init(
                Config.Bind("Sounds", "MenuSounds", true,
                    "Fun little sounds when moving, picking and switching tabs in the menu. " +
                    "May be silent if the game has Unity audio switched off (it uses FMOD); the log says which.").Value,
                Config.Bind("Sounds", "MenuSoundVolume", 0.6f, "Menu sound volume, 0 to 1.").Value);

            _sessions = new SessionManager(HousePlayerMenu.BuildRootPage);
            _sessions.PlayerJoined += MenuSounds.Attach;

            // Player 1 always exists, driven by the keyboard backup; controllers are added by SyncPlayers.
            _keyboard = new KeyboardPlayerInput(() => _menuKey.Value);
            _keyboardPlayer = _sessions.Join(0, _keyboard);

            _window = new HouseMenuWindow(_sessions);
            Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            MenuSounds.Tick();
            Fun.Tick();
            MenuCheck.Tick();
            if (Input.GetKeyDown(KeyCode.F3)) Log.LogInfo(WorldCopy.Probe(0));
            if (Time.unscaledTime >= _nextPlayerSync)
            {
                _nextPlayerSync = Time.unscaledTime + 1f;
                SyncPlayers();
            }

            foreach (KeyValuePair<int, RewiredPlayerInput> pair in _controllers)
            {
                pair.Value.Tick();
                PlayerSession menu = _sessions.Get(pair.Key);
                bool menuClosed = menu == null || !menu.IsOpen;

                // A fun tool (bubble gun, paint) is in this player's hands: tap RB shoots it, with the menu closed.
                if (pair.Value.QuickPlaceTapped && menuClosed && Fun.HasTool(pair.Key))
                {
                    string shot = Fun.Fire(pair.Key);
                    Log.LogInfo("Player " + (pair.Key + 1) + " fun tool: " + shot);
                    MenuSounds.Play(MenuSound.Pop);
                    continue;
                }
                if (pair.Value.QuickUndoHeld && Build.BuildWorld.Instance != null)
                {
                    string removed = Build.BuildWorld.Instance.RemoveLookedAt(pair.Key);
                    if (removed != null)
                    {
                        Log.LogInfo("Player " + (pair.Key + 1) + " hold-LB remove: " + removed);
                        MenuSounds.Play(MenuSound.Swish);
                    }
                }
                if ((pair.Value.QuickPlaceTapped || pair.Value.QuickUndoTapped) && Build.BuildWorld.Instance != null)
                {
                    string said = Build.BuildWorld.Instance.QuickAction(pair.Key, pair.Value.QuickPlaceTapped);
                    if (said != null)
                    {
                        Log.LogInfo("Player " + (pair.Key + 1) + " quick build: " + said);
                        MenuSounds.Play(pair.Value.QuickPlaceTapped ? MenuSound.Pop : MenuSound.Swish);
                    }
                }
            }

            _sessions.Update();
            UpdateGamePause();

            // Item pictures are made in the background, a little each frame, from the first time a menu opens.
            if (_sessions.AnyOpen) Icons.IconCache.Start();
            Icons.IconCache.Tick();

            try
            {
                Build.BuildWorld.Instance.Update(_sessions);
            }
            catch (System.Exception e)
            {
                if (!_warnedBuild)
                {
                    _warnedBuild = true;
                    Log.LogError("Build mode update failed: " + e);
                }
            }

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
            if (count != _lastPlayerCount)
            {
                _lastPlayerCount = count;
                Log.LogInfo("Local players found: " + count +
                            (GameReflection.FindType("GameInstance") == null ? " (GameInstance class not found)" :
                             GameReflection.GetStatic("GameInstance", "Instance") == null ? " (GameInstance.Instance is null)" : ""));
            }

            for (int i = 0; i < SessionManager.MaxPlayers; i++)
            {
                _playerControllers[i] = i < count ? locals[i] : null;
                _rewiredPlayers[i] = i < count ? GameReflection.RewiredPlayerOf(locals[i]) : null;
            }

            for (int i = 0; i < count; i++)
            {
                if (_controllers.ContainsKey(i))
                {
                    continue;
                }
                if (_rewiredPlayers[i] == null)
                {
                    if (!_warnedNoRewired[i])
                    {
                        _warnedNoRewired[i] = true;
                        Log.LogWarning("Player " + (i + 1) + ": no Rewired player found yet.");
                    }
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
                        Log.LogInfo("Paused player " + (i + 1) + "'s controls while their menu is open.");
                    }
                    else if (!_warnedNoPause[i])
                    {
                        _warnedNoPause[i] = true;
                        Log.LogWarning("Could not pause player " + (i + 1) + "'s controls (input manager not found).");
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
            Fun.DrawHud(System.Math.Max(_lastPlayerCount, 1), i => { PlayerSession m = _sessions.Get(i); return m != null && m.IsOpen; });
        }
    }
}
