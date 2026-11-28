using System;
using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// PLACEHOLDER until the controller adapter is written (PROJECT_NOTES.md tasks T5 and T18).
    ///
    /// Drives one player's menu from the keyboard: arrow keys move, Enter picks, Backspace goes
    /// back, Q/E switch tabs, the menu key (F1 by default) opens and closes. It's a backup for a grown-up at the
    /// keyboard and lets the menu be tried before we know how the game reads controllers.
    ///
    /// The real adapter will be one IPlayerInput per controller (legacy Input, the new Input
    /// System, or Rewired, whichever the game uses), mapping D-pad/stick, A, B and Start.
    /// </summary>
    internal sealed class KeyboardPlayerInput : IPlayerInput
    {
        private readonly Func<KeyCode> _toggleKey;

        /// <param name="toggleKey">Read every frame, so a changed config value applies at once.</param>
        public KeyboardPlayerInput(Func<KeyCode> toggleKey)
        {
            _toggleKey = toggleKey;
        }

        public bool WasPressed(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Up: return Input.GetKeyDown(KeyCode.UpArrow);
                case MenuAction.Down: return Input.GetKeyDown(KeyCode.DownArrow);
                case MenuAction.Left: return Input.GetKeyDown(KeyCode.LeftArrow);
                case MenuAction.Right: return Input.GetKeyDown(KeyCode.RightArrow);
                case MenuAction.Select: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                case MenuAction.Back: return Input.GetKeyDown(KeyCode.Backspace);
                case MenuAction.ToggleMenu: return Input.GetKeyDown(_toggleKey());
                case MenuAction.TabPrev: return Input.GetKeyDown(KeyCode.Q);
                case MenuAction.TabNext: return Input.GetKeyDown(KeyCode.E);
                default: return false;
            }
        }
    }
}
