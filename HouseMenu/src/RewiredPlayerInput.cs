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
        public string Toggle;      // button held to open/close the menu; empty = no controller toggle
        public float ToggleHoldSeconds = 1f; // 0 = a normal press toggles
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
        private bool _toggleFired;              // already toggled during this hold
        private float _nextRepeatTime;

        private static readonly MenuAction[] Directions = { MenuAction.Up, MenuAction.Down, MenuAction.Left, MenuAction.Right };
        private static readonly Dictionary<string, bool> ActionExists = new Dictionary<string, bool>();

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
        /// Toggle by holding a button (the family chose: hold Map for 1 second). A quick tap still does the
        /// button's normal game job. Fires once per hold.
        /// </summary>
        private void UpdateToggle(object player)
        {
            if (_names.ToggleHoldSeconds <= 0f)
            {
                if (ButtonDown(player, _names.Toggle)) _pressed.Add(MenuAction.ToggleMenu);
                return;
            }

            if (!ButtonHeld(player, _names.Toggle))
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
            if (!exists)
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
