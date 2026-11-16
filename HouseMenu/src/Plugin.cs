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
        public const string PluginVersion = "0.2.0";

        internal static ManualLogSource Log;

        private ConfigEntry<KeyCode> _menuKey;
        private ConfigEntry<KeyCode> _undoKey;
        private SessionManager _sessions;
        private PlayerSession _keyboardPlayer;
        private HouseMenuWindow _window;

        private void Awake()
        {
            Log = Logger;
            _menuKey = Config.Bind("Controls", "MenuKey", KeyCode.F1,
                "Keyboard backup: opens and closes player 1's house menu.");
            _undoKey = Config.Bind("Controls", "UndoKey", KeyCode.F2,
                "Keyboard backup: undoes player 1's most recent house change.");

            _sessions = new SessionManager(HousePlayerMenu.BuildRootPage);

            // PLACEHOLDER (T5/T18): one keyboard-driven player until the controller adapter exists.
            // The adapter will Join one session per connected controller instead.
            _keyboardPlayer = _sessions.Join(0, new KeyboardPlayerInput(() => _menuKey.Value));

            _window = new HouseMenuWindow(_sessions);
            Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            _sessions.Update();

            if (Input.GetKeyDown(_undoKey.Value))
            {
                HousePlayerMenu.UndoLast(_keyboardPlayer);
            }

            _window.UpdateCursor();
        }

        private void OnGUI()
        {
            _window.Draw();
        }
    }
}
