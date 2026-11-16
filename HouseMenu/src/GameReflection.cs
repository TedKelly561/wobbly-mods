using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Reaches the game's own classes by name, using the names confirmed in
    /// docs/research/game-internals.md. Reflection keeps the mod building without the
    /// game's DLLs; if a name is wrong, calls return null and callers fall back safely.
    /// Once the game's DLLs are referenced directly (T1), these can become normal calls.
    /// </summary>
    internal static class GameReflection
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static;

        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        /// <summary>
        /// The game's local player controllers in split-screen order, or an empty list.
        /// GameInstance.Instance.GetLocalPlayerControllers() (CONFIRMED in research).
        /// </summary>
        public static List<object> LocalPlayerControllers()
        {
            var result = new List<object>();
            object gameInstance = GetStatic("GameInstance", "Instance");
            IEnumerable locals = Call(gameInstance, "GetLocalPlayerControllers") as IEnumerable;
            if (locals != null)
            {
                foreach (object c in locals)
                {
                    if (c != null) result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// Player N's camera: PlayerController.GetGameplayCamera().GetCamera() (CONFIRMED in research).
        /// Returns null if not available.
        /// </summary>
        public static UnityEngine.Camera CameraOf(object playerController)
        {
            object gameplayCamera = Call(playerController, "GetGameplayCamera");
            return Call(gameplayCamera, "GetCamera") as UnityEngine.Camera;
        }

        /// <summary>
        /// Player N's Rewired player: PlayerController.GetPlayerControllerInputManager().player
        /// (CONFIRMED in research). Returns null if not available.
        /// </summary>
        public static object RewiredPlayerOf(object playerController)
        {
            object inputManager = Call(playerController, "GetPlayerControllerInputManager");
            return Get(inputManager, "player");
        }

        public static object Get(object target, string member)
        {
            if (target == null) return null;
            try
            {
                for (Type t = target.GetType(); t != null; t = t.BaseType)
                {
                    PropertyInfo p = t.GetProperty(member, Any | BindingFlags.DeclaredOnly);
                    if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(target, null);
                    FieldInfo f = t.GetField(member, Any | BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(target);
                }
            }
            catch (Exception)
            {
                // fall through
            }
            return null;
        }

        public static object Call(object target, string method)
        {
            if (target == null) return null;
            try
            {
                MethodInfo m = target.GetType().GetMethods(Any)
                    .FirstOrDefault(x => x.Name == method && x.GetParameters().Length == 0);
                return m == null ? null : m.Invoke(m.IsStatic ? null : target, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object GetStatic(string typeName, string member)
        {
            Type type = FindType(typeName);
            if (type == null) return null;
            try
            {
                PropertyInfo p = type.GetProperty(member, Any);
                if (p != null) return p.GetValue(null, null);
                FieldInfo f = type.GetField(member, Any);
                return f != null ? f.GetValue(null) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Type FindType(string name)
        {
            Type cached;
            if (TypeCache.TryGetValue(name, out cached)) return cached;

            Type found = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    found = asm.GetType(name, false);
                    if (found != null) break;
                }
                catch (Exception)
                {
                    // skip assemblies that can't be inspected
                }
            }
            if (found != null) TypeCache[name] = found; // don't cache misses: the game may load it later
            return found;
        }
    }
}
