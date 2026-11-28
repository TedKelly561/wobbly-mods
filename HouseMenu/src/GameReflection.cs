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

        // Looking a method up by name is slow and makes garbage, and some callers ask every frame (followers, the
        // tool crosshair, costume powers). A class's methods never change while the game runs, so each answer
        // (found or not) is kept, by class, name and number of arguments.
        private struct MethodKey : IEquatable<MethodKey>
        {
            public Type Type;
            public string Name;
            public int Arguments;

            public bool Equals(MethodKey other)
            {
                return Type == other.Type && Arguments == other.Arguments && Name == other.Name;
            }

            public override bool Equals(object obj)
            {
                return obj is MethodKey && Equals((MethodKey)obj);
            }

            public override int GetHashCode()
            {
                return Type.GetHashCode() ^ (Name.GetHashCode() * 31) ^ Arguments;
            }
        }

        private static readonly Dictionary<MethodKey, MethodInfo> MethodCache = new Dictionary<MethodKey, MethodInfo>();

        /// <summary>The first method with that name and that many arguments (as before, just remembered), or null.</summary>
        private static MethodInfo FindMethod(Type type, string method, int arguments)
        {
            var key = new MethodKey { Type = type, Name = method, Arguments = arguments };
            MethodInfo m;
            if (MethodCache.TryGetValue(key, out m)) return m;
            m = type.GetMethods(Any).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == arguments);
            MethodCache[key] = m;
            return m;
        }

        // The local players are asked for many times in one frame; the game is only asked once per frame.
        private static List<object> _locals;
        private static int _localsFrame = -1;

        /// <summary>
        /// The game's local player controllers in split-screen order, or an empty list.
        /// GameInstance.Instance.GetLocalPlayerControllers() (CONFIRMED in research).
        /// </summary>
        public static List<object> LocalPlayerControllers()
        {
            int frame = UnityEngine.Time.frameCount;
            if (_locals != null && _localsFrame == frame) return _locals;

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
            _locals = result;
            _localsFrame = frame;
            return result;
        }

        /// <summary>
        /// Player N's camera: PlayerController.GetGameplayCamera().GetCamera() (CONFIRMED in research).
        /// Returns null if not available.
        /// </summary>
        public static UnityEngine.Camera CameraOf(object playerController)
        {
            object gameplayCamera = Call(playerController, "GetGameplayCamera") ?? Get(playerController, "gameplayCamera");
            return Call(gameplayCamera, "GetCamera") as UnityEngine.Camera;
        }

        /// <summary>Player N's PlayerControllerInputManager (method or field, both seen in Scout report 1).</summary>
        public static object InputManagerOf(object playerController)
        {
            return Call(playerController, "GetPlayerControllerInputManager") ?? Get(playerController, "controllerInputManager");
        }

        /// <summary>
        /// Pauses or resumes one player's game controls and camera, and shows or hides their cursor, using the game's own
        /// per-player calls (Scout report 1): DisableGameplayInput(handle, bResetTransformInput) /
        /// EnableGameplayInput(handle), ShowCursor(handle) / HideCursor(handle).
        /// Each pause must be matched by a resume with the same handle. Returns false if the calls aren't found.
        /// </summary>
        public static bool SetGameplayPaused(object playerController, object handle, bool paused)
        {
            object input = InputManagerOf(playerController);
            if (input == null) return false;

            bool ok = paused
                ? CallWith(input, "DisableGameplayInput", handle, true)
                : CallWith(input, "EnableGameplayInput", handle);
            CallWith(input, paused ? "DisableGameplayCameraInput" : "EnableGameplayCameraInput", handle);
            CallWith(input, paused ? "ShowCursor" : "HideCursor", handle);
            return ok;
        }

        /// <summary>Calls a method with exactly these arguments. Returns false if it isn't found or throws.</summary>
        public static bool CallWith(object target, string method, params object[] args)
        {
            if (target == null) return false;
            try
            {
                MethodInfo m = FindMethod(target.GetType(), method, args.Length);
                if (m == null) return false;
                m.Invoke(m.IsStatic ? null : target, args);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Player N's Rewired player: PlayerController.GetPlayerControllerInputManager().player
        /// (CONFIRMED in research). Returns null if not available.
        /// </summary>
        public static object RewiredPlayerOf(object playerController)
        {
            object inputManager = InputManagerOf(playerController);
            return Call(inputManager, "GetRewiredPlayer") ?? Get(inputManager, "player");
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
                MethodInfo m = FindMethod(target.GetType(), method, 0);
                return m == null ? null : m.Invoke(m.IsStatic ? null : target, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads a static property or field, searching parent classes too. Needed because many game
        /// singletons (like GameInstance.Instance) are declared on a generic parent class.
        /// </summary>
        public static object GetStatic(string typeName, string member)
        {
            Type type = FindType(typeName);
            try
            {
                for (Type t = type; t != null; t = t.BaseType)
                {
                    PropertyInfo p = t.GetProperty(member, Any | BindingFlags.DeclaredOnly);
                    if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(null, null);
                    FieldInfo f = t.GetField(member, Any | BindingFlags.DeclaredOnly);
                    if (f != null && f.IsStatic) return f.GetValue(null);
                }
            }
            catch (Exception)
            {
                // fall through
            }
            return null;
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
