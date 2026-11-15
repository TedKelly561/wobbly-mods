using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// BepInEx entry point. Handles keys and hands everything else to HouseMenuWindow.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "wobblymods.housemenu";
        public const string PluginName = "Wobbly House Menu";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private ConfigEntry<KeyCode> _menuKey;
        private ConfigEntry<KeyCode> _undoKey;
        private HouseMenuWindow _menu;

        private void Awake()
        {
            Log = Logger;
            _menuKey = Config.Bind("Controls", "MenuKey", KeyCode.F1,
                "Opens and closes the house menu.");
            _undoKey = Config.Bind("Controls", "UndoKey", KeyCode.F2,
                "Undoes the most recent house change.");

            _menu = new HouseMenuWindow();
            Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(_menuKey.Value))
            {
                _menu.Toggle();
            }

            if (Input.GetKeyDown(_undoKey.Value))
            {
                _menu.UndoLast();
            }
        }

        private void OnGUI()
        {
            _menu.Draw();
        }
    }
}
