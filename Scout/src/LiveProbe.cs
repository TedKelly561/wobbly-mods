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

namespace WobblyMods.Scout
{
    /// <summary>
    /// Scout 0.4: the game's catalogs (costumes, vehicles, pets) with readable addresses, for the T-Rex
    /// dress-up and the monster truck. Reflection only; anything missing is reported, not fatal.
    /// </summary>
    internal static class CatalogProbe
    {
        private const System.Reflection.BindingFlags Any =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;

        public static string Build()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("== Scout catalog (costumes, vehicles, pets) at " + System.DateTime.Now.ToString("HH:mm:ss") + " ==");
            Section(sb, "Clothing (ClothingManager.Instance.GetAllClothingReferences())", Clothing());
            Section(sb, "Vehicles (AllVehiclesAssetReferences.GetAllVehicleReferences())", Vehicles());
            Section(sb, "Pets (PetManager.Instance.GetAllPets())", Pets());
            return sb.ToString();
        }

        private static void Section(System.Text.StringBuilder sb, string title, System.Collections.IEnumerable items)
        {
            sb.AppendLine();
            sb.AppendLine("== " + title + " ==");
            if (items == null)
            {
                sb.AppendLine("  (not available here)");
                return;
            }
            int n = 0;
            bool shapeShown = false;
            foreach (object item in items)
            {
                if (item == null) continue;
                if (!shapeShown)
                {
                    sb.AppendLine("  item type: " + item.GetType().FullName + "  fields: " + Fields(item));
                    shapeShown = true;
                }
                sb.AppendLine("  " + Describe(item));
                n++;
            }
            sb.AppendLine("  count: " + n);
        }

        private static System.Collections.IEnumerable Clothing()
        {
            object manager = Static("ClothingManager", "Instance");
            return Call(manager, "GetAllClothingReferences") as System.Collections.IEnumerable;
        }

        private static System.Collections.IEnumerable Vehicles()
        {
            System.Type t = LiveProbe.FindType("AllVehiclesAssetReferences");
            if (t == null) return null;
            var all = new System.Collections.Generic.List<object>();
            foreach (UnityEngine.Object so in UnityEngine.Resources.FindObjectsOfTypeAll(t))
            {
                System.Collections.IEnumerable refs = Call(so, "GetAllVehicleReferences") as System.Collections.IEnumerable;
                if (refs == null) continue;
                foreach (object r in refs) all.Add(r);
            }
            return all;
        }

        private static System.Collections.IEnumerable Pets()
        {
            return Call(Static("PetManager", "Instance"), "GetAllPets") as System.Collections.IEnumerable;
        }

        /// <summary>One line per item: its id fields plus every AssetReference resolved to an address.</summary>
        private static string Describe(object item)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (System.Reflection.FieldInfo f in item.GetType().GetFields(Any))
            {
                if (f.IsStatic) continue;
                object v;
                try { v = f.GetValue(item); } catch (System.Exception) { continue; }
                if (v == null) continue;
                string typeName = v.GetType().Name;
                if (typeName.Contains("AssetReference"))
                {
                    parts.Add(f.Name + "=" + Resolve(v));
                }
                else if (v is string || v is System.Guid || v.GetType().IsPrimitive || v.GetType().IsEnum)
                {
                    parts.Add(f.Name + "=" + v);
                }
                else if (v is UnityEngine.Object)
                {
                    UnityEngine.Object uo = (UnityEngine.Object)v;
                    parts.Add(f.Name + "=" + (uo ? uo.name : "null"));
                }
            }
            return string.Join("  ", parts.ToArray());
        }

        private static string Fields(object item)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (System.Reflection.FieldInfo f in item.GetType().GetFields(Any))
            {
                if (!f.IsStatic) names.Add(f.FieldType.Name + " " + f.Name);
            }
            return string.Join(", ", names.ToArray());
        }

        /// <summary>An AssetReference's key, and the address/path Addressables knows it by.</summary>
        private static string Resolve(object assetReference)
        {
            object key = Get(assetReference, "RuntimeKey") ?? Get(assetReference, "AssetGUID") ?? Get(assetReference, "m_AssetGUID");
            if (key == null) return "?";
            string where = Locate(key);
            return key + (where != null ? " -> " + where : "");
        }

        private static string Locate(object key)
        {
            System.Type addressables = LiveProbe.FindType("UnityEngine.AddressableAssets.Addressables");
            System.Collections.IEnumerable locators = GetStatic(addressables, "ResourceLocators") as System.Collections.IEnumerable;
            if (locators == null) return null;
            foreach (object locator in locators)
            {
                foreach (System.Reflection.MethodInfo m in locator.GetType().GetMethods(Any))
                {
                    if (m.Name != "Locate" || m.GetParameters().Length != 3) continue;
                    try
                    {
                        object[] args = { key, null, null };
                        object found = m.Invoke(locator, args);
                        System.Collections.IList list = args[2] as System.Collections.IList;
                        if (found is bool && (bool)found && list != null && list.Count > 0)
                        {
                            object loc = list[0];
                            return (Get(loc, "PrimaryKey") ?? Get(loc, "InternalId") ?? loc).ToString();
                        }
                    }
                    catch (System.Exception)
                    {
                        // try the next locator
                    }
                }
            }
            return null;
        }

        private static object Static(string typeName, string member)
        {
            return GetStatic(LiveProbe.FindType(typeName), member);
        }

        private static object GetStatic(System.Type type, string member)
        {
            for (System.Type t = type; t != null; t = t.BaseType)
            {
                try
                {
                    System.Reflection.PropertyInfo p = t.GetProperty(member, Any | System.Reflection.BindingFlags.DeclaredOnly);
                    if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(null, null);
                    System.Reflection.FieldInfo f = t.GetField(member, Any | System.Reflection.BindingFlags.DeclaredOnly);
                    if (f != null && f.IsStatic) return f.GetValue(null);
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
            return null;
        }

        private static object Get(object target, string member)
        {
            if (target == null) return null;
            for (System.Type t = target.GetType(); t != null; t = t.BaseType)
            {
                try
                {
                    System.Reflection.PropertyInfo p = t.GetProperty(member, Any | System.Reflection.BindingFlags.DeclaredOnly);
                    if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(target, null);
                    System.Reflection.FieldInfo f = t.GetField(member, Any | System.Reflection.BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(target);
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
            return null;
        }

        private static object Call(object target, string method)
        {
            if (target == null) return null;
            foreach (System.Reflection.MethodInfo m in target.GetType().GetMethods(Any))
            {
                if (m.Name != method || m.GetParameters().Length != 0) continue;
                try { return m.Invoke(m.IsStatic ? null : target, null); }
                catch (System.Exception) { return null; }
            }
            return null;
        }
    }
}
