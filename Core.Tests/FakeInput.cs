using System.Collections.Generic;

namespace WobblyMods.Core.Tests
{
    /// <summary>Scripted input: Press() queues actions that count as pressed for the next Update only.</summary>
    internal sealed class FakeInput : IPlayerInput
    {
        private readonly HashSet<MenuAction> _pressed = new HashSet<MenuAction>();

        public void Press(params MenuAction[] actions)
        {
            foreach (MenuAction a in actions) _pressed.Add(a);
        }

        public void EndFrame() => _pressed.Clear();

        public bool WasPressed(MenuAction action) => _pressed.Contains(action);
    }
}
