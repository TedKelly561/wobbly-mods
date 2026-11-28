using System;
using System.Linq;
using System.Reflection;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Fun powers that work in the main world, not just the sandbox (Skylar's request: no-clip in her world).
    ///
    /// No-clip lives on the player's own movement code, PlayerCharacterMovement (CONFIRMED in Scout report 2:
    /// field bool bNoClip, noClipRigidbody, SimulateMovementNoClip, and the RPC ClientSetNoClipEnabled). The
    /// sandbox only adds a button for it. The public method that switches it wasn't in the Scout report (the
    /// list was cut off), so we search PlayerCharacterMovement for a method with "NoClip" in its name that takes
    /// one bool, preferring a Server... one (it tells every copy of the game), and log which one we used.
    /// </summary>
    internal static class Powers
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static bool _loggedMethods;

        /// <summary>Turns no-clip on or off for that player. Returns a message for the menu.</summary>
        public static string ToggleNoClip(int playerIndex)
        {
            object movement = MovementOf(playerIndex);
            if (movement == null)
            {
                return "Couldn't find your character.";
            }

            object current = GameReflection.Get(movement, "bNoClip");
            bool isOn = current is bool && (bool)current;
            bool want = !isOn;

            MethodInfo setter = FindSetter(movement.GetType());
            if (setter == null)
            {
                Plugin.Log.LogWarning("No-clip: no switch method found on " + movement.GetType().Name + ".");
                return "No-clip isn't available here.";
            }

            try
            {
                setter.Invoke(movement, new object[] { want });
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                Plugin.Log.LogWarning("No-clip: " + setter.Name + " failed: " + inner.GetType().Name + ": " + inner.Message);
                return "No-clip didn't work.";
            }

            object after = GameReflection.Get(movement, "bNoClip");
            bool nowOn = after is bool ? (bool)after : want;
            Plugin.Log.LogInfo("No-clip for player " + (playerIndex + 1) + " via " + setter.Name + "(" + want + "): now " +
                               (nowOn ? "on" : "off") + ".");
            return nowOn ? "No-clip ON: fly through walls! Pick it again to land." : "No-clip OFF.";
        }

        /// <summary>
        /// Gives every player the game knows about (all split-screen players, and anyone in the game) the amount, using
        /// the game's own money call: PlayerControllerEmployment.UpdateMoney(int) (Scout report 2), the call jobs use
        /// to pay players. It syncs and saves. Logs each player's money before and after, to confirm it adds.
        /// </summary>
        public static string GiveMoney(int amount)
        {
            object gameInstance = GameReflection.GetStatic("GameInstance", "Instance");
            var players = new System.Collections.Generic.List<object>();
            System.Collections.IEnumerable all = GameReflection.Call(gameInstance, "GetPlayerControllers") as System.Collections.IEnumerable;
            if (all != null)
            {
                foreach (object c in all) if (c != null) players.Add(c);
            }
            if (players.Count == 0) players.AddRange(GameReflection.LocalPlayerControllers());

            Type employmentType = GameReflection.FindType("PlayerControllerEmployment");
            int given = 0;
            foreach (object controller in players)
            {
                object employment = GameReflection.Call(controller, "GetPlayerControllerEmployment")
                                    ?? GameReflection.Get(controller, "playerControllerEmployment");
                UnityEngine.Component comp = controller as UnityEngine.Component;
                if (employment == null && comp != null && employmentType != null)
                {
                    employment = comp.GetComponentInChildren(employmentType, true) ?? comp.GetComponentInParent(employmentType);
                }
                if (employment == null) continue;

                object before = GameReflection.Call(employment, "GetLocalMoney");
                if (GameReflection.CallWith(employment, "UpdateMoney", amount))
                {
                    given++;
                    object after = GameReflection.Call(employment, "GetLocalMoney");
                    Plugin.Log.LogInfo("Money: " + (comp != null ? comp.name : "player") + " " + before + " -> " + after +
                                       " (UpdateMoney(" + amount + ")).");
                }
            }

            if (given == 0)
            {
                Plugin.Log.LogWarning("Money: no player's PlayerControllerEmployment found (" + players.Count + " players).");
                return "Couldn't find anyone's wallet.";
            }
            return "Everyone got " + amount.ToString("N0") + "!";
        }

        /// <summary>That player's PlayerCharacterMovement: controller → character → movement.</summary>
        private static object MovementOf(int playerIndex)
        {
            var locals = GameReflection.LocalPlayerControllers();
            if (playerIndex < 0 || playerIndex >= locals.Count) return null;
            object character = GameReflection.Call(locals[playerIndex], "GetPlayerCharacter")
                               ?? GameReflection.Get(locals[playerIndex], "playerCharacter");
            return GameReflection.Call(character, "GetPlayerCharacterMovement")
                   ?? GameReflection.Get(character, "playerCharacterMovement");
        }

        /// <summary>
        /// A method named like *NoClip* that takes exactly one bool. Prefers names starting with "Server",
        /// then "Set", and skips the network message handler (ClientSetNoClipEnabled takes reader/info, not a bool).
        /// </summary>
        private static MethodInfo FindSetter(Type type)
        {
            MethodInfo[] candidates = type.GetMethods(Any)
                .Where(m => m.Name.IndexOf("NoClip", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            if (!_loggedMethods)
            {
                _loggedMethods = true;
                Plugin.Log.LogInfo("No-clip methods on " + type.Name + ": " +
                    string.Join(", ", candidates.Select(m => m.Name + "(" +
                        string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name).ToArray()) + ")").ToArray()));
            }

            Func<MethodInfo, bool> oneBool = m =>
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(bool);

            return candidates.Where(oneBool).OrderBy(m =>
                    m.Name.StartsWith("Server", StringComparison.Ordinal) ? 0 :
                    m.Name.StartsWith("Set", StringComparison.Ordinal) ? 1 : 2)
                .FirstOrDefault();
        }
    }
}
