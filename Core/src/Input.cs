namespace WobblyMods.Core
{
    /// <summary>
    /// Everything a player can do to a menu. A game-specific adapter maps controller buttons
    /// (or keys) to these, so the menu logic never knows how the game reads controllers.
    /// Suggested controller mapping: D-pad / left stick = Up/Down/Left/Right, A = Select,
    /// B = Back, Start or Select/Back button = ToggleMenu.
    /// </summary>
    public enum MenuAction
    {
        Up,
        Down,
        Left,
        Right,
        Select,
        Back,
        ToggleMenu
    }

    /// <summary>
    /// One local player's input device (usually one controller).
    /// Implemented per game once we know how it reads controllers (legacy Input,
    /// the new Input System, or Rewired). See PROJECT_NOTES.md tasks T5 and T18.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>
        /// True only on the frame the action was pressed (an edge, not "held").
        /// For a stick, report one press when it crosses the threshold, and optionally
        /// repeat while held, so a menu moves one step at a time.
        /// </summary>
        bool WasPressed(MenuAction action);
    }
}
