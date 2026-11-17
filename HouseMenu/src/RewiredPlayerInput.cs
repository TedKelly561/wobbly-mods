using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Rewired action names used for the menu. Set from the config file.
    /// Each can be a comma-separated list; the first one with input wins (e.g. "UIVertical,Vertical"
    /// reads the UI stick and falls back to the movement stick if the game has UI controls off).
    /// </summary>
    internal sealed class RewiredMenuActions
    {
        public string Vertical;    // axis: up is positive
        public string Horizontal;  // axis: right is positive
        public string Select;      // button
        public string Back;        // button
        public string Toggle;      // button or "A+B" combo (comma list of choices); empty = no controller toggle
        public float ToggleHoldSeconds = 0f; // 0 = toggles as soon as the button or combo is pressed
        public string QuickPlace = "UIRB";    // build mode: quick tap places a piece at the ghost
        public string QuickUndo = "UILB";     // build mode: quick tap undoes the last piece
    }

    /// <summary>
    /// One split-screen player's controller, read through the game's Rewired player for that player
    /// (PlayerControllerInputManager.player, confirmed in research). Reflection keeps this building
    /// without Rewired's DLL.
    ///
    /// Action names come from the config (Scout 0.2 lists the real ones). Each name is checked once
    /// against ReInput.mapping; a missing name is switched off with one log line instead of failing.
    ///
    /// Call Tick() once per frame before the sessions update.
    /// </summary>
    internal sealed class RewiredPlayerInput : IPlayerInput
    {
        private const float StickThreshold = 0.5f;
        private const float RepeatDelay = 0.45f;
        private const float RepeatInterval = 0.15f;

        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Func<object> _rewiredPlayer;
        private readonly RewiredMenuActions _names;
        private readonly HashSet<MenuAction> _pressed = new HashSet<MenuAction>();

        private int _heldDirection = -1; // index into Directions, -1 = none
        private float _toggleHeldSince = -1f;  // when the toggle button went down, -1 = not held
        private readonly TapWatcher _placeTap = new TapWatcher();
        private readonly TapWatcher _undoTap = new TapWatcher();

        /// <summary>True on the frame a quick tap of the build "place" button (RB) finished.</summary>
        public bool QuickPlaceTapped { get; private set; }

        /// <summary>True on the frame a quick tap of the build "undo" button (LB) finished.</summary>
        public bool QuickUndoTapped { get; private set; }
        private bool _toggleFired;              // already toggled during this hold
        private float _nextRepeatTime;

        private static readonly MenuAction[] Directions = { MenuAction.Up, MenuAction.Down, MenuAction.Left, MenuAction.Right };
        private static readonly Dictionary<string, bool> ActionExists = new Dictionary<string, bool>();
        private static readonly HashSet<string> SeenPressed = new HashSet<string>();

        public RewiredPlayerInput(Func<object> rewiredPlayer, RewiredMenuActions names)
        {
            _rewiredPlayer = rewiredPlayer;
            _names = names;
        }

        public bool WasPressed(MenuAction action)
        {
            return _pressed.Contains(action);
        }

        /// <summary>Reads the controller once for this frame.</summary>
        public void Tick()
        {
            _pressed.Clear();
            object player = _rewiredPlayer();
            if (player == null)
            {
                _heldDirection = -1;
                return;
            }

            if (ButtonDown(player, _names.Select)) _pressed.Add(MenuAction.Select);
            if (ButtonDown(player, _names.Back)) _pressed.Add(MenuAction.Back);
            UpdateToggle(player);

            // Quick taps for build mode. Holding both bumpers (the menu combo) cancels both taps.
            bool place = ButtonHeld(player, _names.QuickPlace);
            bool undo = ButtonHeld(player, _names.QuickUndo);
            float t = Time.unscaledTime;
            QuickPlaceTapped = _placeTap.Update(place, undo, t);
            QuickUndoTapped = _undoTap.Update(undo, place, t);

            int direction = CurrentDirection(Axis(player, _names.Vertical), Axis(player, _names.Horizontal));
            float now = Time.unscaledTime;

            if (direction < 0)
            {
                _heldDirection = -1;
            }
            else if (direction != _heldDirection)
            {
                _heldDirection = direction;
                _nextRepeatTime = now + RepeatDelay;
                _pressed.Add(Directions[direction]);
            }
            else if (now >= _nextRepeatTime)
            {
                _nextRepeatTime = now + RepeatInterval;
                _pressed.Add(Directions[direction]);
            }
        }

        /// <summary>
        /// Opens/closes the menu. The toggle setting is a list of choices separated by commas; each choice is
        /// one button or a combo joined with "+", e.g. "UILB+UIRB" (the family's choice: LB and RB together).
        /// The toggle fires once when a choice has been held for ToggleHoldSeconds (0 = as soon as it's pressed),
        /// and again only after it's released.
        /// </summary>
        private void UpdateToggle(object player)
        {
            if (!AnyChoiceHeld(player, _names.Toggle))
            {
                _toggleHeldSince = -1f;
                _toggleFired = false;
                return;
            }

            float now = Time.unscaledTime;
            if (_toggleHeldSince < 0f)
            {
                _toggleHeldSince = now;
            }
            if (!_toggleFired && now - _toggleHeldSince >= _names.ToggleHoldSeconds)
            {
                _toggleFired = true;
                _pressed.Add(MenuAction.ToggleMenu);
            }
        }

        /// <summary>True if every button of at least one choice ("A+B" or "C") is held right now.</summary>
        private static bool AnyChoiceHeld(object player, string choices)
        {
            foreach (string choice in Split(choices))
            {
                if (choice.Length == 0) continue;
                bool all = true;
                foreach (string part in choice.Split('+'))
                {
                    string action = part.Trim();
                    if (action.Length == 0 || !Usable(action))
                    {
                        all = false;
                        break;
                    }
                    object result = Invoke(player, "GetButton", action);
                    if (!(result is bool) || !(bool)result)
                    {
                        all = false;
                        break;
                    }
                    if (SeenPressed.Add(action))
                    {
                        Plugin.Log.LogInfo("Menu button '" + action + "' pressed (first time seen).");
                    }
                }
                if (all) return true;
            }
            return false;
        }

        /// <summary>The strongest stick/D-pad direction past the threshold, or -1.</summary>
        private static int CurrentDirection(float vertical, float horizontal)
        {
            if (Mathf.Abs(vertical) < StickThreshold && Mathf.Abs(horizontal) < StickThreshold) return -1;
            if (Mathf.Abs(vertical) >= Mathf.Abs(horizontal)) return vertical > 0 ? 0 : 1;
            return horizontal < 0 ? 2 : 3;
        }

        private static bool ButtonDown(object player, string actions)
        {
            return AnyButton(player, actions, "GetButtonDown");
        }

        private static bool ButtonHeld(object player, string actions)
        {
            return AnyButton(player, actions, "GetButton");
        }

        private static bool AnyButton(object player, string actions, string method)
        {
            foreach (string action in Split(actions))
            {
                if (!Usable(action)) continue;
                object result = Invoke(player, method, action);
                if (result is bool && (bool)result) return true;
            }
            return false;
        }

        /// <summary>The first listed axis that's past the dead zone, or 0.</summary>
        private static float Axis(object player, string actions)
        {
            foreach (string action in Split(actions))
            {
                if (!Usable(action)) continue;
                object result = Invoke(player, "GetAxis", action);
                if (result is float && Mathf.Abs((float)result) >= 0.2f) return (float)result;
            }
            return 0f;
        }

        private static string[] Split(string actions)
        {
            if (string.IsNullOrEmpty(actions)) return new string[0];
            string[] parts = actions.Split(',');
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            return parts;
        }

        private static object Invoke(object player, string method, string action)
        {
            try
            {
                MethodInfo m = player.GetType().GetMethods(Any).FirstOrDefault(x =>
                    x.Name == method && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string));
                return m == null ? null : m.Invoke(player, new object[] { action });
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// True if the action name exists in the game's Rewired setup. Checked once per name, so a wrong
        /// name gives one log line instead of one per frame.
        /// </summary>
        private static bool Usable(string action)
        {
            if (string.IsNullOrEmpty(action)) return false;

            bool exists;
            if (ActionExists.TryGetValue(action, out exists)) return exists;

            Type reInput = GameReflection.FindType("Rewired.ReInput");
            object ready = reInput == null ? null : GetStaticMember(reInput, "isReady");
            if (!(ready is bool) || !(bool)ready)
            {
                return false; // Rewired not ready yet: don't cache, ask again next frame
            }

            object mapping = GetStaticMember(reInput, "mapping");
            object found = null;
            try
            {
                MethodInfo getAction = mapping == null ? null : mapping.GetType().GetMethods(Any).FirstOrDefault(x =>
                    x.Name == "GetAction" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string));
                found = getAction == null ? null : getAction.Invoke(mapping, new object[] { action });
            }
            catch (Exception)
            {
                found = null;
            }

            exists = found != null;
            ActionExists[action] = exists;
            if (exists)
            {
                Plugin.Log.LogInfo("Rewired action '" + action + "' found.");
            }
            else
            {
                Plugin.Log.LogWarning("Rewired action '" + action + "' not found. Set the right name in the config " +
                                      "(Scout's scene report lists the game's actions).");
            }
            return exists;
        }

        private static object GetStaticMember(Type type, string name)
        {
            try
            {
                PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (p != null) return p.GetValue(null, null);
                FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return f != null ? f.GetValue(null) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>Pressed if any of the inputs is pressed: lets player 1 use both keyboard and controller.</summary>
    internal sealed class EitherPlayerInput : IPlayerInput
    {
        private readonly IPlayerInput _a;
        private readonly IPlayerInput _b;

        public EitherPlayerInput(IPlayerInput a, IPlayerInput b)
        {
            _a = a;
            _b = b;
        }

        public bool WasPressed(MenuAction action)
        {
            return _a.WasPressed(action) || _b.WasPressed(action);
        }
    }
}
