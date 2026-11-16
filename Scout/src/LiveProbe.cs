using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace WobblyMods.Scout
{
    /// <summary>
    /// Looks at the running game through the names found in docs/research/game-internals.md.
    /// Everything goes through reflection, so Scout builds without the game's DLLs, and any
    /// name that turns out to be wrong is reported as "not found" instead of crashing.
    /// </summary>
    internal static class LiveProbe
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static;

        /// <summary>Local players, their cameras (split screen) and their Rewired players.</summary>
        public static void AppendPlayers(StringBuilder sb)
        {
            sb.AppendLine("== Local players (GameInstance) ==");
            object gameInstance = GetStatic("GameInstance", "Instance");
            if (gameInstance == null)
            {
                sb.AppendLine("  GameInstance.Instance not found or null (normal on the main menu).");
                return;
            }

            object gamemode = Call(gameInstance, "GetGamemode");
            sb.AppendLine("  Gamemode: " + Describe(gamemode));

            IEnumerable locals = Call(gameInstance, "GetLocalPlayerControllers") as IEnumerable;
            if (locals == null)
            {
                sb.AppendLine("  GetLocalPlayerControllers() not found or returned null.");
                return;
            }

            int index = 0;
            foreach (object controller in locals)
            {
                sb.AppendLine("  Local player " + index + ": " + Describe(controller) + "  type=" + TypeOf(controller));

                object character = Call(controller, "GetPlayerCharacter");
                sb.AppendLine("    character: " + Describe(character) + "  type=" + TypeOf(character));

                object gameplayCamera = Call(controller, "GetGameplayCamera");
                Camera cam = Call(gameplayCamera, "GetCamera") as Camera;
                if (cam != null)
                {
                    sb.AppendLine("    camera: " + cam.name + "  rect=" + cam.rect + "  pixelRect=" + cam.pixelRect);
                }
                else
                {
                    sb.AppendLine("    camera: not found (GameplayCamera=" + Describe(gameplayCamera) + ")");
                }

                object inputManager = Call(controller, "GetPlayerControllerInputManager");
                object rewiredPlayer = Get(inputManager, "player") ?? Call(character, "GetRewiredPlayer");
                sb.AppendLine("    rewired player: " + DescribeRewiredPlayer(rewiredPlayer));
                index++;
            }
            sb.AppendLine("  Local player count: " + index);
        }

        /// <summary>Rewired actions (the button names the game uses) and connected controllers.</summary>
        public static void AppendRewired(StringBuilder sb)
        {
            sb.AppendLine("== Rewired ==");
            Type reInput = FindType("Rewired.ReInput");
            if (reInput == null)
            {
                sb.AppendLine("  Rewired.ReInput not found.");
                return;
            }

            object ready = GetStatic(reInput, "isReady");
            sb.AppendLine("  isReady: " + Describe(ready));

            object mapping = GetStatic(reInput, "mapping");
            IEnumerable actions = Get(mapping, "Actions") as IEnumerable;
            if (actions != null)
            {
                sb.AppendLine("  Actions (id, name, type, category id):");
                foreach (object a in actions)
                {
                    sb.AppendLine("    " + Describe(Get(a, "id")) + "  " + Describe(Get(a, "name")) +
                                  "  " + Describe(Get(a, "type")) + "  cat=" + Describe(Get(a, "categoryId")));
                }
            }
            else
            {
                sb.AppendLine("  ReInput.mapping.Actions not available.");
            }

            object controllers = GetStatic(reInput, "controllers");
            IEnumerable joysticks = Get(controllers, "Joysticks") as IEnumerable;
            if (joysticks != null)
            {
                sb.AppendLine("  Joysticks:");
                foreach (object j in joysticks)
                {
                    sb.AppendLine("    id=" + Describe(Get(j, "id")) + "  name=" + Describe(Get(j, "name")) +
                                  "  hardware=" + Describe(Get(j, "hardwareName")));
                }
            }

            object players = GetStatic(reInput, "players");
            IEnumerable playerList = Get(players, "Players") as IEnumerable;
            if (playerList != null)
            {
                sb.AppendLine("  Rewired players:");
                foreach (object p in playerList)
                {
                    sb.AppendLine("    " + DescribeRewiredPlayer(p));
                }
            }
        }

        /// <summary>Managers the mods will use, and whether each exists right now.</summary>
        public static void AppendManagers(StringBuilder sb)
        {
            sb.AppendLine("== Managers (Instance present?) ==");
            string[] names =
            {
                "GameInstance", "PetManager", "ClothingManager", "SaveGameManager", "WorldManager",
                "WeatherSystem", "RewardManager", "AchievementManager"
            };
            foreach (string n in names)
            {
                Type t = FindType(n);
                object inst = t == null ? null : GetStatic(t, "Instance");
                sb.AppendLine("  " + n + ": " + (t == null ? "type not found" : inst == null ? "no instance" : "present"));
            }

            object hawk = GetStatic("HawkNetworkManager", "DefaultInstance");
            sb.AppendLine("  HawkNetworkManager.DefaultInstance: " + (hawk == null ? "null" : "present") +
                          "  IsServer=" + Describe(Call(hawk, "IsServer")));

            object pets = Call(GetStatic("PetManager", "Instance"), "GetAllPets");
            IEnumerable petList = pets as IEnumerable;
            if (petList != null)
            {
                sb.AppendLine("  Pets:");
                foreach (object p in petList)
                {
                    sb.AppendLine("    " + Describe(p) + "  type=" + TypeOf(p));
                }
            }
        }

        /// <summary>Every Addressables key that looks like a prefab: what a spawner can place.</summary>
        public static string BuildPropReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== Addressable prefab keys (spawnable things) ==");
            Type addressables = FindType("UnityEngine.AddressableAssets.Addressables");
            IEnumerable locators = GetStatic(addressables, "ResourceLocators") as IEnumerable;
            if (locators == null)
            {
                sb.AppendLine("Addressables.ResourceLocators not available.");
                return sb.ToString();
            }

            var keys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object locator in locators)
            {
                IEnumerable locatorKeys = Get(locator, "Keys") as IEnumerable;
                if (locatorKeys == null) continue;
                foreach (object k in locatorKeys)
                {
                    string s = k as string;
                    if (s != null && s.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        keys.Add(s);
                    }
                }
            }

            sb.AppendLine("Count: " + keys.Count);
            foreach (string k in keys)
            {
                sb.AppendLine(k);
            }
            return sb.ToString();
        }

        // ---------- reflection helpers (all null-safe) ----------

        private static string DescribeRewiredPlayer(object p)
        {
            if (p == null) return "null";
            return "id=" + Describe(Get(p, "id")) + "  name=" + Describe(Get(p, "name")) +
                   "  descriptiveName=" + Describe(Get(p, "descriptiveName"));
        }

        private static object Call(object target, string method)
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

        private static object Get(object target, string member)
        {
            if (target == null) return null;
            return GetMember(target.GetType(), target, member);
        }

        private static object GetStatic(string typeName, string member)
        {
            return GetStatic(FindType(typeName), member);
        }

        private static object GetStatic(Type type, string member)
        {
            return type == null ? null : GetMember(type, null, member);
        }

        private static object GetMember(Type type, object target, string member)
        {
            try
            {
                for (Type t = type; t != null; t = t.BaseType)
                {
                    PropertyInfo p = t.GetProperty(member, Any | BindingFlags.DeclaredOnly);
                    if (p != null && p.GetIndexParameters().Length == 0)
                    {
                        return p.GetValue(p.GetGetMethod(true).IsStatic ? null : target, null);
                    }
                    FieldInfo f = t.GetField(member, Any | BindingFlags.DeclaredOnly);
                    if (f != null)
                    {
                        return f.GetValue(f.IsStatic ? null : target);
                    }
                }
            }
            catch (Exception)
            {
                // fall through
            }
            return null;
        }

        internal static Type FindType(string name)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(name, false);
                    if (t != null) return t;
                }
                catch (Exception)
                {
                    // skip assemblies that can't be inspected
                }
            }
            return null;
        }

        private static string Describe(object o)
        {
            if (o == null) return "null";
            UnityEngine.Object uo = o as UnityEngine.Object;
            if (uo != null) return uo ? uo.name : "destroyed";
            return o.ToString();
        }

        private static string TypeOf(object o)
        {
            return o == null ? "-" : o.GetType().FullName;
        }
    }
}
