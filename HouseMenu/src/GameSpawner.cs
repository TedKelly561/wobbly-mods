using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Spawns and removes props using the game's own systems (names CONFIRMED in Scout reports 1 and 2):
    /// - Networked: NetworkPrefab.SpawnNetworkPrefab(string address, Action&lt;HawkNetworkBehaviour&gt; onComplete,
    ///   Vector3? position, Quaternion? rotation, HawkConnection owner, bool bUseChunkSystem, bool bSendTransform,
    ///   bool bCheckChunk, bool bMarkAsReplaced). Only the host may spawn; in local split screen this PC is the host.
    /// - Local (for plain models that aren't network objects): Addressables.LoadAssetAsync&lt;GameObject&gt;(address)
    ///   .WaitForCompletion(), then Object.Instantiate.
    /// - Remove: HawkNetworkBehaviour.networkObject.Destroy() for networked objects, else Object.Destroy.
    /// Everything goes through reflection, so a wrong name reports an error instead of crashing.
    /// </summary>
    internal static class GameSpawner
    {
        private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>Receives the game's callback. Its parameter is object, so it fits Action&lt;HawkNetworkBehaviour&gt;.</summary>
        private sealed class Relay
        {
            private readonly Action<GameObject> _done;
            private bool _called;

            public Relay(Action<GameObject> done)
            {
                _done = done;
            }

            public void Invoke(object spawned)
            {
                if (_called) return;
                _called = true;
                Component c = spawned as Component;
                _done(c != null ? c.gameObject : null);
            }
        }

        /// <summary>
        /// Starts a networked spawn. Returns false (with a reason) if it couldn't even start.
        /// done is called later with the spawned object, or null if the game couldn't spawn it.
        /// </summary>
        public static bool TrySpawnNetworked(string address, Vector3 position, Quaternion rotation,
                                             Action<GameObject> done, out string problem)
        {
            problem = null;
            Type networkPrefab = GameReflection.FindType("NetworkPrefab");
            Type behaviour = GameReflection.FindType("HawkNetworking.HawkNetworkBehaviour");
            if (networkPrefab == null || behaviour == null)
            {
                problem = "the game's network spawner wasn't found";
                return false;
            }

            MethodInfo spawn = networkPrefab.GetMethods(AnyStatic).FirstOrDefault(m =>
                m.Name == "SpawnNetworkPrefab" && m.GetParameters().Length == 9 &&
                m.GetParameters()[0].ParameterType == typeof(string));
            if (spawn == null)
            {
                problem = "SpawnNetworkPrefab(string, ...) wasn't found";
                return false;
            }

            try
            {
                var relay = new Relay(done);
                Type callbackType = typeof(Action<>).MakeGenericType(behaviour);
                Delegate callback = Delegate.CreateDelegate(callbackType, relay, typeof(Relay).GetMethod("Invoke"));
                spawn.Invoke(null, new object[]
                {
                    address, callback, position, rotation, null,
                    true,  // bUseChunkSystem: stays with the world chunk it's in
                    true,  // bSendTransform
                    true,  // bCheckChunk
                    false  // bMarkAsReplaced
                });
                return true;
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                problem = "the game's spawner failed: " + inner.GetType().Name + ": " + inner.Message;
                return false;
            }
        }

        /// <summary>
        /// Loads a prop by its address and places a STATIC, scripts-free copy of it (WorldCopy.StaticCopy): what it
        /// looks like and its solid shape, with no physics body or network object. Used for catalog props since the
        /// menu self-test (2026-11-21) showed the game's network spawner never answers for them.
        /// </summary>
        public static GameObject SpawnStatic(string address, Vector3 position, Quaternion rotation, out string problem)
        {
            problem = null;
            Type addressables = GameReflection.FindType("UnityEngine.AddressableAssets.Addressables");
            MethodInfo load = addressables == null ? null : addressables.GetMethods(AnyStatic).FirstOrDefault(m =>
                m.Name == "LoadAssetAsync" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(object));
            if (load == null)
            {
                problem = "Addressables.LoadAssetAsync wasn't found";
                return null;
            }
            try
            {
                object handle = load.MakeGenericMethod(typeof(GameObject)).Invoke(null, new object[] { address });
                MethodInfo wait = handle.GetType().GetMethod("WaitForCompletion", BindingFlags.Public | BindingFlags.Instance);
                GameObject prefab = wait == null ? null : wait.Invoke(handle, null) as GameObject;
                if (prefab == null)
                {
                    problem = "the model couldn't be loaded";
                    return null;
                }
                GameObject copy = WorldCopy.StaticCopy(prefab, out problem);
                if (copy == null) return null;
                copy.name = prefab.name;
                copy.transform.SetPositionAndRotation(position, rotation);
                return copy;
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                problem = "loading failed: " + inner.GetType().Name + ": " + inner.Message;
                return null;
            }
        }

        /// <summary>Loads a model by its address and places a plain (non-networked) copy. Returns null with a reason on failure.</summary>
        public static GameObject SpawnLocal(string address, Vector3 position, Quaternion rotation, out string problem)
        {
            problem = null;
            Type addressables = GameReflection.FindType("UnityEngine.AddressableAssets.Addressables");
            MethodInfo load = addressables == null ? null : addressables.GetMethods(AnyStatic).FirstOrDefault(m =>
                m.Name == "LoadAssetAsync" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(object));
            if (load == null)
            {
                problem = "Addressables.LoadAssetAsync wasn't found";
                return null;
            }

            try
            {
                object handle = load.MakeGenericMethod(typeof(GameObject)).Invoke(null, new object[] { address });
                MethodInfo wait = handle.GetType().GetMethod("WaitForCompletion", BindingFlags.Public | BindingFlags.Instance);
                GameObject prefab = wait == null ? null : wait.Invoke(handle, null) as GameObject;
                if (prefab == null)
                {
                    problem = "the model couldn't be loaded";
                    return null;
                }
                return UnityEngine.Object.Instantiate(prefab, position, rotation);
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                problem = "loading failed: " + inner.GetType().Name + ": " + inner.Message;
                return null;
            }
        }

        /// <summary>Removes an object the way the game does for networked objects, else a plain delete.</summary>
        public static void Despawn(GameObject target)
        {
            if (target == null) return;

            Type behaviour = GameReflection.FindType("HawkNetworking.HawkNetworkBehaviour");
            Component networked = behaviour != null ? target.GetComponent(behaviour) : null;
            if (networked != null)
            {
                object networkObject = GameReflection.Get(networked, "networkObject");
                if (networkObject != null && GameReflection.CallWith(networkObject, "Destroy"))
                {
                    return;
                }
            }
            UnityEngine.Object.Destroy(target);
        }
    }
}
