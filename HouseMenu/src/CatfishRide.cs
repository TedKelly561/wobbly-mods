using System;
using System.Collections.Generic;
using UnityEngine;
using WobblyMods.HouseMenu.Tanks;
using WobblyMods.Visuals;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// "Catfish ride!" (tasks T20/T21): turns the vehicle a player is in (or right next to) into a giant catfish.
    /// Skylar picked the muscle car (Vehicle_MuscleCar), the big helicopter (Vehicle_HelicopterBig) and the speed boat
    /// (Vehicle_Sea_SpeedBoat), but any vehicle works.
    ///
    /// Only the LOOK changes: the vehicle's own renderers are hidden (never the players riding it) and a visual-only
    /// catfish is attached, sized to the vehicle's length. Driving, flying, floating, seats and collisions stay the game's.
    /// Model: Skylar's redtail from the Unity kit bundle if installed (wobbly_animals/RedtailCatfish), otherwise the
    /// redtail made in code (ShapeModels.Redtail, from Core's RedtailModel), with the game's own fishing catfish
    /// (CatFish_Static) as the last resort.
    /// Catfish rides are also INVINCIBLE (Skylar's request) via the game's own PlayerVehicle.SetIndestructable.
    /// Picking it again on the same vehicle puts the normal look back (and makes it breakable again).
    /// The look is local only (other PCs see the vehicle).
    /// </summary>
    internal static class CatfishRide
    {
        private const float SearchRadius = 8f;
        private const string GameCatfish = "Assets/Content/Game/Prefabs/Props/Fishing_Props/Fish/Static/CatFish_Static.prefab";

        private sealed class Ride
        {
            public GameObject Vehicle;
            public Component PlayerVehicle; // for invincibility (game's own SetIndestructable)
            public bool Invincible;
            public string Shape = "";    // the vehicle's own measurements, for the log
            public bool FromWorldMode;   // changed by "every car is a catfish", not picked by a player
            public GameObject Fish;
            public readonly List<Renderer> Hidden = new List<Renderer>();
        }

        private static readonly Dictionary<GameObject, Ride> Rides = new Dictionary<GameObject, Ride>();

        /// <summary>Catfish on, or back to normal if it's already a catfish. Returns a message for the menu.</summary>
        public static string Toggle(int playerIndex)
        {
            GameObject vehicle = NearestVehicle(playerIndex);
            if (vehicle == null)
            {
                return "Get in a car, boat or helicopter first, then pick Catfish car!";
            }

            Ride ride;
            if (Rides.TryGetValue(vehicle, out ride))
            {
                if (ride.FromWorldMode)
                {
                    // "Every car a catfish" already changed this one. Putting it back was pointless (the next sweep
                    // made it a catfish again two seconds later), so picking it makes it HER catfish: invincible,
                    // and it stays when "every car a catfish" is switched off.
                    ride.FromWorldMode = false;
                    ride.Invincible = SetInvincible(ride, true);
                    Plugin.Log.LogInfo("Catfish ride: " + vehicle.name + " (already a catfish from catfish world) is now her own" +
                                       (ride.Invincible ? ", invincible." : ", but invincibility didn't switch on."));
                    return "Your " + Nice(vehicle.name) + " is an INVINCIBLE CATFISH!";
                }
                Restore(ride);
                Rides.Remove(vehicle);
                return "Back to a normal " + Nice(vehicle.name) + ".";
            }

            try
            {
                ride = Apply(vehicle);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Catfish ride on " + vehicle.name + " failed: " + e);
                ride = null;
            }
            if (ride == null)
            {
                return "The catfish couldn't swim in.";
            }
            Rides[vehicle] = ride;
            ride.Invincible = SetInvincible(ride, true);
            Plugin.Log.LogInfo("Catfish ride: " + vehicle.name + " is now a catfish (" + ride.Fish.name + ")" +
                               (ride.Invincible ? ", invincible" : ", but invincibility didn't switch on") + ". Vehicle: " + ride.Shape + ".");
            return "Your " + Nice(vehicle.name) + " is an INVINCIBLE CATFISH!";
        }

        /// <summary>"Every car is a catfish": turns every vehicle in the loaded world into a catfish (not invincible). Returns how many are new.</summary>
        public static int ApplyToAll()
        {
            Type vehicleType = GameReflection.FindType("PlayerVehicle");
            if (vehicleType == null) return 0;

            // Forget vehicles the game has unloaded.
            var gone = new List<GameObject>();
            foreach (KeyValuePair<GameObject, Ride> pair in Rides) if (pair.Key == null) gone.Add(pair.Key);
            foreach (GameObject g in gone) Rides.Remove(g);

            int added = 0;
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsOfType(vehicleType))
            {
                Component c = o as Component;
                if (c == null || Rides.ContainsKey(c.gameObject)) continue;
                try
                {
                    Ride ride = Apply(c.gameObject);
                    if (ride == null) continue;
                    ride.FromWorldMode = true;
                    Rides[c.gameObject] = ride;
                    added++;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Catfish world: " + c.name + " skipped: " + e.Message);
                }
            }
            if (added > 0) Plugin.Log.LogInfo("Catfish world: " + added + " more vehicles are catfish (" + Rides.Count + " in all).");
            return added;
        }

        /// <summary>Puts back every vehicle that "every car is a catfish" changed (her own invincible rides stay).</summary>
        public static int RestoreAll()
        {
            var back = new List<GameObject>();
            foreach (KeyValuePair<GameObject, Ride> pair in Rides)
            {
                if (pair.Key == null || pair.Value.FromWorldMode) back.Add(pair.Key);
            }
            int restored = 0;
            foreach (GameObject g in back)
            {
                Ride ride = Rides[g];
                Rides.Remove(g);
                if (g == null) continue;
                Restore(ride);
                restored++;
            }
            return restored;
        }

        /// <summary>
        /// Turns the catfish on the nearest vehicle a quarter turn (press again for the next quarter). For vehicles
        /// whose model doesn't point the way they drive. It changes the wiggle's resting direction: turning the fish
        /// itself was undone by the wiggle on the very next frame (test 2026-11-22: "we can't rotate it").
        /// </summary>
        public static string Flip(int playerIndex)
        {
            GameObject vehicle = NearestVehicle(playerIndex);
            Ride ride;
            if (vehicle == null || !Rides.TryGetValue(vehicle, out ride) || ride.Fish == null)
            {
                return "No catfish here to turn.";
            }
            Wiggle wiggle = ride.Fish.GetComponent<Wiggle>();
            if (wiggle != null) wiggle.Turn(90f);
            else ride.Fish.transform.localRotation *= Quaternion.Euler(0f, 90f, 0f);
            Plugin.Log.LogInfo("Catfish ride: turned the catfish on " + vehicle.name + " a quarter turn.");
            return "Turned the catfish.";
        }

        private static Ride Apply(GameObject vehicle)
        {
            // Measure the vehicle (its own renderers, not riders) before hiding anything.
            var ride = new Ride { Vehicle = vehicle };
            Bounds bounds = new Bounds(vehicle.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer r in vehicle.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || IsRider(r) || r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
                ride.Hidden.Add(r);
            }
            if (!any)
            {
                return null;
            }

            GameObject fish = LoadFish();
            if (fish == null)
            {
                return null;
            }
            ride.Fish = fish;
            FishTank.MakeVisualOnly(fish);

            // Size and direction, measured in the VEHICLE'S OWN space (test 2026-11-22: the fish was sideways when
            // the car wasn't lined up with the world, because the old code measured a world-aligned box).
            fish.transform.SetParent(vehicle.transform, false);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;
            fish.transform.localScale = Vector3.one;

            Bounds local;
            if (LocalBounds(vehicle.transform, ride.Hidden, out local))
            {
                // The vehicle's long side is the way it drives: along its local Z, or its local X for models built sideways.
                bool sideways = local.size.x > local.size.z * 1.15f;
                float vehicleLength = Mathf.Max(local.size.x, local.size.z);
                Bounds fishLocal;
                float fishLength = 1f;
                bool fishSideways = false;
                var fishRenderers = new List<Renderer>(fish.GetComponentsInChildren<Renderer>());
                if (LocalBounds(fish.transform, fishRenderers, out fishLocal))
                {
                    fishLength = Mathf.Max(fishLocal.size.x, fishLocal.size.z);
                    fishSideways = fishLocal.size.x > fishLocal.size.z * 1.15f;
                }
                if (sideways != fishSideways) fish.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                if (fishLength > 0.001f) fish.transform.localScale = Vector3.one * (vehicleLength * 1.15f / fishLength);
                // Center the fish on the vehicle (the fish models are centered on their own origin, give or take).
                Vector3 fishCenter = fish.transform.localRotation * (fishLocal.center * fish.transform.localScale.x);
                fish.transform.localPosition = local.center - fishCenter;
ride.Shape = local.size.x.ToString("0.0") + " wide x " + local.size.z.ToString("0.0") + " long" + (sideways ? ", built sideways" : "");
            }

            fish.AddComponent<Wiggle>().Mover = vehicle.transform;

            foreach (Renderer r in ride.Hidden)
            {
                r.enabled = false;
            }
            return ride;
        }

        /// <summary>
        /// Skylar's request: catfish rides can't be wrecked. Uses the game's own switch,
        /// PlayerVehicle.SetIndestructable(object handle, bool) (races use it). The ride itself is the handle, so
        /// turning the catfish off only removes OUR invincibility, never the game's. Returns IsIndestructable().
        /// </summary>
        private static bool SetInvincible(Ride ride, bool on)
        {
            if (ride.PlayerVehicle == null)
            {
                Type vehicleType = GameReflection.FindType("PlayerVehicle");
                ride.PlayerVehicle = vehicleType != null ? ride.Vehicle.GetComponent(vehicleType) : null;
            }
            if (ride.PlayerVehicle == null) return false;

            GameReflection.CallWith(ride.PlayerVehicle, "SetIndestructable", ride, on);
            object now = GameReflection.Call(ride.PlayerVehicle, "IsIndestructable");
            return now is bool && (bool)now;
        }

        private static void Restore(Ride ride)
        {
            if (ride.Invincible && ride.Vehicle != null)
            {
                SetInvincible(ride, false);
            }
            foreach (Renderer r in ride.Hidden)
            {
                if (r != null) r.enabled = true;
            }
            if (ride.Fish != null) UnityEngine.Object.Destroy(ride.Fish);
        }

        /// <summary>Skylar's redtail if its bundle is installed, otherwise the game's own catfish.</summary>
        private static GameObject LoadFish()
        {
            string problem;
            GameObject prefab = ModelLibrary.Load(CustomModels.AnimalsBundle, "RedtailCatfish", out problem);
            if (prefab != null)
            {
                GameObject redtail = UnityEngine.Object.Instantiate(prefab);
                redtail.name = "Redtail catfish ride";
                return redtail;
            }

            // Skylar's redtail made in code (no Unity kit needed). The game's own catfish is the last resort.
            try
            {
                GameObject made = ShapeModels.Redtail();
                made.name = "Redtail catfish ride";
                return made;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Catfish ride: the code-built redtail failed (" + e.Message + "); using the game's catfish.");
            }

            GameObject gameFish = GameSpawner.SpawnLocal(GameCatfish, Vector3.zero, Quaternion.identity, out problem);
            if (gameFish == null)
            {
                Plugin.Log.LogWarning("Catfish ride: couldn't load a catfish model: " + problem);
                return null;
            }
            gameFish.name = "Catfish ride";
            return gameFish;
        }

        /// <summary>The box around those renderers' meshes, measured in root's own space (so root's heading doesn't matter).</summary>
        private static bool LocalBounds(Transform root, List<Renderer> renderers, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                Mesh mesh = null;
                SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (skinned != null) mesh = skinned.sharedMesh;
                else if (mf != null) mesh = mf.sharedMesh;
                if (mesh == null) continue;
                Bounds b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 inRoot = root.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!any) { bounds = new Bounds(inRoot, Vector3.zero); any = true; } else bounds.Encapsulate(inRoot);
                }
            }
            return any;
        }

        /// <summary>The closest game vehicle (PlayerVehicle) to that player's character, within SearchRadius.</summary>
        private static GameObject NearestVehicle(int playerIndex)
        {
            Vector3? where = GameBindings.CharacterPosition(playerIndex);
            Type vehicleType = GameReflection.FindType("PlayerVehicle");
            if (where == null || vehicleType == null) return null;

            GameObject best = null;
            float bestDistance = SearchRadius;
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsOfType(vehicleType))
            {
                Component c = o as Component;
                if (c == null) continue;
                float d = Vector3.Distance(c.transform.position, where.Value);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c.gameObject;
                }
            }
            return best;
        }

        /// <summary>True for renderers that belong to a player character (riders stay visible).</summary>
        private static bool IsRider(Renderer r)
        {
            Type character = GameReflection.FindType("PlayerCharacter");
            return character != null && r.GetComponentInParent(character) != null;
        }

        /// <summary>"Vehicle_MuscleCar(Clone)" → "muscle car".</summary>
        private static string Nice(string name)
        {
            string n = name.Replace("(Clone)", "").Replace("Vehicle_", "").Replace("Sea_", "").Trim();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n.Length; i++)
            {
                char ch = n[i];
                if (ch == '_') { sb.Append(' '); continue; }
                if (i > 0 && char.IsUpper(ch) && !char.IsUpper(n[i - 1])) sb.Append(' ');
                sb.Append(char.ToLowerInvariant(ch));
            }
            string s = sb.ToString().Trim();
            return s.Length > 0 ? s : "vehicle";
        }
    }

    /// <summary>A gentle swim wiggle that gets livelier the faster the vehicle goes.</summary>
    internal sealed class Wiggle : MonoBehaviour
    {
        public Transform Mover;
        private Quaternion _rest;
        private Vector3 _last;
        private float _phase;
        private bool _started;

        /// <summary>Turns the resting direction (the wiggle swings around it).</summary>
        public void Turn(float degrees)
        {
            if (!_started)
            {
                transform.localRotation *= Quaternion.Euler(0f, degrees, 0f);
                return;
            }
            _rest *= Quaternion.Euler(0f, degrees, 0f);
        }

        private void Update()
        {
            if (!_started)
            {
                _rest = transform.localRotation;
                _last = Mover != null ? Mover.position : transform.position;
                _started = true;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 now = Mover != null ? Mover.position : transform.position;
            float speed = (now - _last).magnitude / dt;
            _last = now;
            float t = Mathf.Clamp01(speed / 15f);
            _phase += Mathf.Lerp(1.5f, 9f, t) * dt;
            transform.localRotation = _rest * Quaternion.Euler(0f, Mathf.Sin(_phase) * Mathf.Lerp(3f, 9f, t), 0f);
        }
    }
}
