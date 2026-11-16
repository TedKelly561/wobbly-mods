using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WobblyMods.Scout
{
    /// <summary>
    /// Read-only scout. It changes nothing in the game. It writes text reports to
    /// BepInEx\WobblyScout\ describing the game's code and scenes, so mods can be built
    /// without guessing class names.
    ///
    /// Reports:
    ///   00_summary.txt   Unity version, input system, controllers, loaded assemblies
    ///   01_types.txt     game classes whose names match modding keywords, with their members
    ///   02_props.txt     every Addressables prefab key (what a spawner can place), refreshed each snapshot
    ///   scene_*.txt      a snapshot of each scene: local players and their cameras (split screen),
    ///                    Rewired actions and controllers, managers, objects, components, prefabs
    ///
    /// A scene snapshot is taken automatically 15 seconds after every scene loads,
    /// and again any time F9 is pressed (if the game uses Unity's legacy input).
    /// </summary>
    [BepInPlugin("wobblymods.scout", "Wobbly Scout", "0.3.0")]
    public class ScoutPlugin : BaseUnityPlugin
    {
        private const float SnapshotDelaySeconds = 15f;
        private const int MaxDepth = 6;
        private const int MaxChildrenPerObject = 60;
        private const int MaxMembersPerType = 120;

        // Name fragments worth reporting. Lower case.
        private static readonly string[] Keywords =
        {
            "player", "character", "camera", "input", "controller", "gamepad", "rewired",
            "spawn", "prop", "furniture", "house", "home", "build", "construct", "place",
            "cat", "animal", "pet", "costume", "cloth", "outfit", "hat", "skin", "appearance",
            "network", "save", "sandbox", "vehicle", "splitscreen", "split", "local",
            "ragdoll", "pickup", "grab", "inventory", "job", "mod", "paint", "color", "colour",
            "material", "decor", "customi"
        };

        // Exact class names to always report, even though they match no keyword.
        private static readonly string[] ExactTypes =
        {
            "GameInstance", "GameplayCamera", "DynamicObject", "WorldDynamicObject", "NetworkPrefab",
            "WorldManager", "FreemodeGamemode", "ModGamemode", "UnitySingleton`1", "PlayerBody"
        };

        private readonly List<float> _pendingSnapshots = new List<float>();
        private readonly List<string> _pendingReasons = new List<string>();

        private string _outDir;
        private int _snapshotCount;
        private bool _legacyInputWorks = true;

        private void Awake()
        {
            try
            {
                _outDir = Path.Combine(Paths.BepInExRootPath, "WobblyScout");
                Directory.CreateDirectory(_outDir);

                WriteSafe("00_summary.txt", BuildSummary);
                WriteSafe("01_types.txt", BuildTypeReport);

                SceneManager.sceneLoaded += OnSceneLoaded;

                // Run from our own hidden object: some games destroy BepInEx's object on the first scene
                // load, which stopped Scout 0.2's snapshots.
                var runnerObject = new GameObject("WobblyScoutRunner");
                runnerObject.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(runnerObject);
                runnerObject.AddComponent<ScoutRunner>().Owner = this;
                Logger.LogInfo("Wobbly Scout ready. Reports go to " + _outDir + ". Press F9 for a snapshot.");
            }
            catch (Exception e)
            {
                Logger.LogError("Wobbly Scout failed to start: " + e);
            }
        }

        // No OnDestroy unsubscribe on purpose: if the game destroys BepInEx's object, Scout must keep
        // hearing scene loads. Scout lives for the whole game session.

        /// <summary>Called every frame by ScoutRunner.</summary>
        internal void Tick()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = _pendingSnapshots.Count - 1; i >= 0; i--)
            {
                if (now >= _pendingSnapshots[i])
                {
                    string reason = _pendingReasons[i];
                    _pendingSnapshots.RemoveAt(i);
                    _pendingReasons.RemoveAt(i);
                    TakeSnapshot(reason);
                }
            }
            PollF9();
        }

        private void PollF9()
        {
            if (!_legacyInputWorks)
            {
                return;
            }

            try
            {
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    TakeSnapshot("F9");
                }
            }
            catch (InvalidOperationException)
            {
                // The game uses the new Input System only. Snapshots still happen on scene load.
                _legacyInputWorks = false;
                Logger.LogWarning("Legacy input is disabled in this game; F9 won't work. Automatic snapshots still run.");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _pendingSnapshots.Add(Time.realtimeSinceStartup + SnapshotDelaySeconds);
            _pendingReasons.Add("auto-" + scene.name);
        }

        private void TakeSnapshot(string reason)
        {
            _snapshotCount++;
            string sceneName = SceneManager.GetActiveScene().name;
            string file = string.Format("scene_{0:00}_{1}_{2}.txt", _snapshotCount, Sanitize(sceneName), Sanitize(reason));
            WriteSafe(file, () => BuildSceneReport(reason));
            WriteSafe("02_props.txt", LiveProbe.BuildPropReport);
            Logger.LogInfo("Wobbly Scout snapshot written: " + file);
        }

        // ---------- 00 summary ----------

        private string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== Wobbly Scout summary ==");
            sb.AppendLine("Written: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Product: " + Application.productName + "  Game version: " + Application.version);
            sb.AppendLine("Unity version: " + Application.unityVersion);
            sb.AppendLine("Platform: " + Application.platform);
            sb.AppendLine("Data path: " + Application.dataPath);
            sb.AppendLine("Executable data folder: " + Path.GetFileName(Application.dataPath));
            sb.AppendLine();

            sb.AppendLine("== Input ==");
            sb.AppendLine("Rewired present: " + (FindType("Rewired.ReInput") != null));
            sb.AppendLine("New Input System present: " + (FindType("UnityEngine.InputSystem.InputSystem") != null));
            try
            {
                Input.GetKey(KeyCode.F9);
                sb.AppendLine("Legacy Input enabled: true");
                string[] pads = Input.GetJoystickNames();
                sb.AppendLine("Controllers seen by legacy Input: " + pads.Length);
                for (int i = 0; i < pads.Length; i++)
                {
                    sb.AppendLine("  [" + i + "] '" + pads[i] + "'");
                }
            }
            catch (InvalidOperationException)
            {
                sb.AppendLine("Legacy Input enabled: false (new Input System only)");
            }
            sb.AppendLine();

            sb.AppendLine("== Loaded assemblies (game and third-party first) ==");
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()
                         .OrderBy(a => IsEngineOrSystem(a) ? 1 : 0)
                         .ThenBy(a => a.GetName().Name))
            {
                AssemblyName n = asm.GetName();
                sb.AppendLine((IsEngineOrSystem(asm) ? "  (engine/system) " : "  ") + n.Name + "  " + n.Version);
            }
            return sb.ToString();
        }

        // ---------- 01 types ----------

        private string BuildTypeReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== Game types matching modding keywords ==");
            sb.AppendLine("Keywords: " + string.Join(", ", Keywords));
            sb.AppendLine();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsEngineOrSystem(asm) || IsModLoader(asm))
                {
                    continue;
                }

                Type[] types = GetTypesSafe(asm);
                List<Type> matches = types
                    .Where(t => t != null && (MatchesKeyword(t.Name) || ExactTypes.Contains(t.Name)))
                    .OrderBy(t => t.FullName)
                    .ToList();
                if (matches.Count == 0)
                {
                    continue;
                }

                sb.AppendLine("######## Assembly: " + asm.GetName().Name + " (" + matches.Count + " matching of " + types.Length + " types)");
                foreach (Type t in matches)
                {
                    AppendType(sb, t);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static void AppendType(StringBuilder sb, Type t)
        {
            try
            {
                string kind = t.IsEnum ? "enum" : t.IsInterface ? "interface" : t.IsValueType ? "struct" : "class";
                string baseName = t.BaseType != null ? " : " + t.BaseType.FullName : "";
                sb.AppendLine();
                sb.AppendLine(kind + " " + t.FullName + baseName);

                if (t.IsEnum)
                {
                    sb.AppendLine("  values: " + string.Join(", ", Enum.GetNames(t)));
                    return;
                }

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                           BindingFlags.Static | BindingFlags.DeclaredOnly;
                int count = 0;

                foreach (FieldInfo f in t.GetFields(flags))
                {
                    if (f.Name.Contains("<") || ++count > MaxMembersPerType) continue;
                    sb.AppendLine("  field  " + (f.IsStatic ? "static " : "") + TypeName(f.FieldType) + " " + f.Name);
                }
                foreach (PropertyInfo p in t.GetProperties(flags))
                {
                    if (++count > MaxMembersPerType) continue;
                    sb.AppendLine("  prop   " + TypeName(p.PropertyType) + " " + p.Name);
                }
                foreach (MethodInfo m in t.GetMethods(flags))
                {
                    if (m.IsSpecialName || m.Name.Contains("<") || ++count > MaxMembersPerType) continue;
                    string args = string.Join(", ", m.GetParameters().Select(a => TypeName(a.ParameterType) + " " + a.Name).ToArray());
                    sb.AppendLine("  method " + (m.IsStatic ? "static " : "") + TypeName(m.ReturnType) + " " + m.Name + "(" + args + ")");
                }
                if (count > MaxMembersPerType)
                {
                    sb.AppendLine("  ... " + (count - MaxMembersPerType) + " more members not shown");
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("  (could not read members: " + e.GetType().Name + ")");
            }
        }

        // ---------- scene snapshots ----------

        private string BuildSceneReport(string reason)
        {
            var sb = new StringBuilder();
            sb.AppendLine("== Scene snapshot (" + reason + ") at " + DateTime.Now.ToString("HH:mm:ss") + " ==");

            sb.AppendLine("Loaded scenes:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                sb.AppendLine("  " + s.name + (s == SceneManager.GetActiveScene() ? " (active)" : "") + "  roots=" + s.rootCount);
            }
            sb.AppendLine();

            // Cameras: in split screen there's one per player, each with its own screen rect.
            sb.AppendLine("== Cameras (split screen shows several, each with its own rect) ==");
            foreach (Camera cam in Camera.allCameras)
            {
                sb.AppendLine("  " + PathOf(cam.transform) + "  enabled=" + cam.enabled + "  depth=" + cam.depth +
                              "  rect=" + cam.rect + "  isMain=" + (cam == Camera.main));
            }
            sb.AppendLine();

            // Live game state through the names from the research (reflection, so wrong names just report "not found").
            TryAppend(sb, LiveProbe.AppendPlayers);
            sb.AppendLine();
            TryAppend(sb, LiveProbe.AppendRewired);
            sb.AppendLine();
            TryAppend(sb, LiveProbe.AppendManagers);
            sb.AppendLine();

            // Objects whose name matches a keyword, anywhere in loaded scenes.
            sb.AppendLine("== Scene objects matching keywords ==");
            int matchCount = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.IsValid() || !MatchesKeyword(go.name)) continue;
                if (++matchCount > 400) continue;
                sb.AppendLine("  " + PathOf(go.transform) + "  active=" + go.activeInHierarchy + "  [" + ComponentList(go) + "]");
            }
            if (matchCount > 400) sb.AppendLine("  ... " + (matchCount - 400) + " more not shown");
            sb.AppendLine();

            // Prefabs and other loaded objects not placed in a scene: these are what spawners use.
            sb.AppendLine("== Loaded prefabs / templates not in a scene (root objects) ==");
            var prefabNames = new SortedDictionary<string, int>();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.IsValid() || go.transform.parent != null) continue;
                int n;
                prefabNames.TryGetValue(go.name, out n);
                prefabNames[go.name] = n + 1;
            }
            sb.AppendLine("  count: " + prefabNames.Count);
            foreach (KeyValuePair<string, int> kv in prefabNames)
            {
                sb.AppendLine("  " + kv.Key + (kv.Value > 1 ? "  x" + kv.Value : ""));
            }
            sb.AppendLine();

            // Full hierarchy of the active scene, depth-limited.
            sb.AppendLine("== Active scene hierarchy (depth " + MaxDepth + ") ==");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                AppendHierarchy(sb, root.transform, 0);
            }
            return sb.ToString();
        }

        private static void AppendHierarchy(StringBuilder sb, Transform t, int depth)
        {
            sb.Append(' ', depth * 2);
            sb.AppendLine(t.name + (t.gameObject.activeSelf ? "" : " (inactive)") + "  [" + ComponentList(t.gameObject) + "]");
            if (depth >= MaxDepth)
            {
                if (t.childCount > 0) { sb.Append(' ', (depth + 1) * 2); sb.AppendLine("... " + t.childCount + " children"); }
                return;
            }
            int shown = Math.Min(t.childCount, MaxChildrenPerObject);
            for (int i = 0; i < shown; i++)
            {
                AppendHierarchy(sb, t.GetChild(i), depth + 1);
            }
            if (t.childCount > shown)
            {
                sb.Append(' ', (depth + 1) * 2);
                sb.AppendLine("... " + (t.childCount - shown) + " more children");
            }
        }

        // ---------- helpers ----------

        private void TryAppend(StringBuilder sb, Action<StringBuilder> section)
        {
            try
            {
                section(sb);
            }
            catch (Exception e)
            {
                sb.AppendLine("  (section failed: " + e.GetType().Name + ": " + e.Message + ")");
                Logger.LogWarning("Wobbly Scout section failed: " + e);
            }
        }

        private void WriteSafe(string fileName, Func<string> build)
        {
            try
            {
                File.WriteAllText(Path.Combine(_outDir, fileName), build());
            }
            catch (Exception e)
            {
                Logger.LogError("Wobbly Scout could not write " + fileName + ": " + e);
            }
        }

        private static bool MatchesKeyword(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lower = name.ToLowerInvariant();
            foreach (string k in Keywords)
            {
                if (lower.Contains(k)) return true;
            }
            return false;
        }

        private static string ComponentList(GameObject go)
        {
            try
            {
                return string.Join(", ", go.GetComponents<Component>()
                    .Where(c => c != null && !(c is Transform))
                    .Select(c => c.GetType().Name).ToArray());
            }
            catch (Exception)
            {
                return "?";
            }
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                path = p.name + "/" + path;
            }
            return path;
        }

        private static Type[] GetTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception)
            {
                return new Type[0];
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch (Exception)
                {
                    // ignore assemblies that can't be inspected
                }
            }
            return null;
        }

        private static bool IsEngineOrSystem(Assembly asm)
        {
            string n = asm.GetName().Name;
            return n == "mscorlib" || n == "netstandard" || n.StartsWith("System") || n.StartsWith("Mono.") ||
                   n.StartsWith("UnityEngine") || n.StartsWith("Unity.") || n.StartsWith("Microsoft.");
        }

        private static bool IsModLoader(Assembly asm)
        {
            string n = asm.GetName().Name;
            return n.StartsWith("BepInEx") || n.StartsWith("0Harmony") || n.StartsWith("Harmony") ||
                   n.StartsWith("MonoMod") || n.StartsWith("WobblyMods");
        }

        private static string TypeName(Type t)
        {
            if (t == null) return "?";
            if (!t.IsGenericType) return t.Name;
            string name = t.Name;
            int tick = name.IndexOf('`');
            if (tick > 0) name = name.Substring(0, tick);
            return name + "<" + string.Join(",", t.GetGenericArguments().Select(TypeName).ToArray()) + ">";
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "none";
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            }
            return sb.ToString();
        }
    }
}
