using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Plays the GAME'S OWN menu sounds through FMOD. Test 14 showed Unity audio is off in Wobbly Life (it uses FMOD),
    /// so our made-in-code sounds were silent.
    ///
    /// How: FMODUnity.RuntimeManager.StudioSystem → getBankList → each bank's getEventList → getPath gives every
    /// sound event's path (like "event:/UI/Click"). Menu-ish ones are picked by name for each MenuSound and played
    /// with RuntimeManager.PlayOneShot(path, position). Every path with "ui" or "menu" in it is logged once, so a
    /// better pick can be set in the config ([Sounds] FmodTick / FmodPick / FmodBack / FmodTab).
    /// Reflection only (no FMOD DLL reference); any failure is logged once and the menu stays silent.
    /// </summary>
    internal static class FmodSounds
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static;

        private static bool _tried;
        private static bool _ready;
        private static MethodInfo _playOneShot;
        private static readonly string[] Chosen = new string[4];
        private static readonly string[] Overrides = new string[4];

        // Keywords per MenuSound (Tick, Pop, Swish, Chime), best first. Paths that also contain "ui"/"menu" win.
        private static readonly string[][] Keywords =
        {
            new[] { "hover", "highlight", "navigate", "scroll", "cursor", "tick", "move" },
            new[] { "confirm", "accept", "select", "click", "press", "pop", "buy" },
            new[] { "back", "cancel", "close", "return", "whoosh", "swoosh", "swish" },
            new[] { "open", "tab", "page", "pause", "notification", "chime", "unlock" }
        };

        /// <summary>Config overrides (empty = choose automatically).</summary>
        public static void SetOverrides(string tick, string pick, string back, string tab)
        {
            Overrides[0] = tick; Overrides[1] = pick; Overrides[2] = back; Overrides[3] = tab;
        }

        /// <summary>Plays any FMOD event path at a place (costume powers). True if it was handed to FMOD.</summary>
        public static bool TryPlayAt(string path, Vector3 at)
        {
            EnsureSetup();
            if (_playOneShot == null || string.IsNullOrEmpty(path)) return false;
            try
            {
                _playOneShot.Invoke(null, new object[] { path, at });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool _loopTried;
        private static MethodInfo _createInstance;
        private static MethodInfo _attachInstance;

        /// <summary>
        /// Starts a looping FMOD event that follows a transform, the way the game's ClothingJetpack plays
        /// "event:/Objects/Objects_UFOJetpack": RuntimeManager.CreateInstance, AttachInstanceToGameObject, start.
        /// Returns the (boxed) EventInstance to hand back to StopLoop, or null if it couldn't start.
        /// </summary>
        public static object StartLoop(string path, Transform follow)
        {
            if (string.IsNullOrEmpty(path) || follow == null) return null;
            try
            {
                if (!_loopTried)
                {
                    _loopTried = true;
                    Type runtime = GameReflection.FindType("FMODUnity.RuntimeManager");
                    if (runtime != null)
                    {
                        _createInstance = runtime.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                        if (_createInstance != null)
                        {
                            Type ev = _createInstance.ReturnType;
                            _attachInstance = runtime.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m =>
                                m.Name == "AttachInstanceToGameObject" && m.GetParameters().Length == 3 &&
                                m.GetParameters()[0].ParameterType == ev &&
                                m.GetParameters()[1].ParameterType == typeof(Transform) &&
                                m.GetParameters()[2].ParameterType == typeof(Rigidbody));
                        }
                    }
                    Plugin.Log.LogInfo("Looping sounds (FMOD): CreateInstance " + (_createInstance != null ? "found" : "MISSING") +
                                       ", attach " + (_attachInstance != null ? "found" : "MISSING") + ".");
                }
                if (_createInstance == null) return null;

                object instance = _createInstance.Invoke(null, new object[] { path });
                if (instance == null || !IsValid(instance)) return null;
                if (_attachInstance != null) _attachInstance.Invoke(null, new object[] { instance, follow, null });
                MethodInfo start = instance.GetType().GetMethod("start", Type.EmptyTypes);
                if (start != null) start.Invoke(instance, null);
                return instance;
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                Plugin.Log.LogWarning("Looping sounds (FMOD): couldn't start " + path + ": " + inner.Message);
                return null;
            }
        }

        /// <summary>Stops (with fade-out unless immediate) and releases what StartLoop returned. Null is fine.</summary>
        public static void StopLoop(object instance, bool immediate = false)
        {
            if (instance == null) return;
            try
            {
                if (!IsValid(instance)) return;
                Type t = instance.GetType();
                MethodInfo stop = t.GetMethods().FirstOrDefault(m => m.Name == "stop" && m.GetParameters().Length == 1);
                if (stop != null)
                {
                    // FMOD.Studio.STOP_MODE: ALLOWFADEOUT = 0, IMMEDIATE = 1
                    object mode = Enum.ToObject(stop.GetParameters()[0].ParameterType, immediate ? 1 : 0);
                    stop.Invoke(instance, new[] { mode });
                }
                MethodInfo release = t.GetMethod("release", Type.EmptyTypes);
                if (release != null) release.Invoke(instance, null);
            }
            catch (Exception)
            {
                // the sound is going away anyway
            }
        }

        private static bool IsValid(object instance)
        {
            MethodInfo isValid = instance.GetType().GetMethod("isValid", Type.EmptyTypes);
            if (isValid == null) return true;
            object v = isValid.Invoke(instance, null);
            return !(v is bool) || (bool)v;
        }

        /// <summary>True if the sound was handed to FMOD.</summary>
        public static bool TryPlay(MenuSound sound)
        {
            EnsureSetup();
            if (!_ready) return false;

            string path = Chosen[(int)sound] ?? Chosen[(int)MenuSound.Pop];
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                Camera cam = Camera.main;
                Vector3 at = cam != null ? cam.transform.position : Vector3.zero;
                _playOneShot.Invoke(null, new object[] { path, at });
                return true;
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                Plugin.Log.LogWarning("Menu sounds (FMOD): playing " + path + " failed: " + inner.Message + ". FMOD sounds off.");
                _ready = false;
                return false;
            }
        }

        private static void EnsureSetup()
        {
            if (!_tried)
            {
                _tried = true;
                try
                {
                    _ready = Setup();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Menu sounds (FMOD): setup failed: " + e.GetType().Name + ": " + e.Message);
                    _ready = false;
                }
            }
        }

        private static bool Setup()
        {
            Type runtime = GameReflection.FindType("FMODUnity.RuntimeManager");
            if (runtime == null)
            {
                Plugin.Log.LogWarning("Menu sounds (FMOD): FMODUnity.RuntimeManager not found.");
                return false;
            }

            _playOneShot = runtime.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m =>
                m.Name == "PlayOneShot" && m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(string) &&
                m.GetParameters()[1].ParameterType == typeof(Vector3));
            if (_playOneShot == null)
            {
                Plugin.Log.LogWarning("Menu sounds (FMOD): PlayOneShot(string, Vector3) not found.");
                return false;
            }

            List<string> paths = AllEventPaths(runtime);
            List<string> uiPaths = paths.Where(p => Has(p, "ui") || Has(p, "menu")).ToList();
            Plugin.Log.LogInfo("Menu sounds (FMOD): " + paths.Count + " sound events, " + uiPaths.Count + " look like menu sounds:");
            foreach (string p in uiPaths.Take(120)) Plugin.Log.LogInfo("  " + p);

            for (int i = 0; i < 4; i++)
            {
                Chosen[i] = !string.IsNullOrEmpty(Overrides[i]) ? Overrides[i] : Pick(uiPaths, paths, Keywords[i]);
            }
            Plugin.Log.LogInfo("Menu sounds (FMOD): move=" + (Chosen[0] ?? "-") + "  pick=" + (Chosen[1] ?? "-") +
                               "  back=" + (Chosen[2] ?? "-") + "  tab=" + (Chosen[3] ?? "-"));
            return Chosen.Any(c => !string.IsNullOrEmpty(c));
        }

        /// <summary>Every event path in every loaded bank.</summary>
        private static List<string> AllEventPaths(Type runtime)
        {
            var result = new List<string>();
            object system = GetStatic(runtime, "StudioSystem");
            if (system == null)
            {
                Plugin.Log.LogWarning("Menu sounds (FMOD): StudioSystem not available.");
                return result;
            }

            Array banks = CallOut(system, "getBankList") as Array;
            if (banks == null) return result;
            foreach (object bank in banks)
            {
                Array events = CallOut(bank, "getEventList") as Array;
                if (events == null) continue;
                foreach (object ev in events)
                {
                    string path = CallOut(ev, "getPath") as string;
                    if (!string.IsNullOrEmpty(path)) result.Add(path);
                }
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static string Pick(List<string> uiPaths, List<string> all, string[] words)
        {
            foreach (string w in words)
            {
                string hit = uiPaths.FirstOrDefault(p => Has(p, w));
                if (hit != null) return hit;
            }
            foreach (string w in words)
            {
                string hit = all.FirstOrDefault(p => Has(p, w) && !Has(p, "music") && !Has(p, "ambience"));
                if (hit != null) return hit;
            }
            return null;
        }

        private static bool Has(string path, string word)
        {
            return path.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Calls a method whose only parameter is an out parameter, and returns that value.</summary>
        private static object CallOut(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethods(Any).FirstOrDefault(x =>
                x.Name == method && x.GetParameters().Length == 1 && x.GetParameters()[0].IsOut);
            if (m == null) return null;
            object[] args = { null };
            m.Invoke(target, args);
            return args[0];
        }

        private static object GetStatic(Type type, string member)
        {
            PropertyInfo p = type.GetProperty(member, Any);
            if (p != null) return p.GetValue(null, null);
            FieldInfo f = type.GetField(member, Any);
            return f != null ? f.GetValue(null) : null;
        }
    }
}
