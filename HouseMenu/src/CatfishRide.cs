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
                return "Get in a car, boat or helicopter first!";
            }

            Ride ride;
            if (Rides.TryGetValue(vehicle, out ride))
            {
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
                               (ride.Invincible ? ", invincible." : ", but invincibility didn't switch on."));
            return "Your " + Nice(vehicle.name) + " is an INVINCIBLE CATFISH!";
        }

        /// <summary>Turns the catfish on the nearest vehicle around (for models that face backwards).</summary>
        public static string Flip(int playerIndex)
        {
            GameObject vehicle = NearestVehicle(playerIndex);
            Ride ride;
            if (vehicle == null || !Rides.TryGetValue(vehicle, out ride) || ride.Fish == null)
            {
                return "No catfish here to turn.";
            }
            ride.Fish.transform.Rotate(0f, 180f, 0f, Space.World);
            return "Turned the catfish around.";
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

            // Size: as long as the vehicle (a bit longer, so it covers it), length along the vehicle's forward.
            fish.transform.SetParent(vehicle.transform, false);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;
            fish.transform.localScale = Vector3.one;

            Bounds fb;
            if (FishTank.TryGetBounds(fish, out fb))
            {
                // Which way is the model long? Turn it so its length runs along the vehicle's forward (local Z).
                Vector3 localSize = fish.transform.InverseTransformVector(fb.size);
                if (Mathf.Abs(localSize.x) > Mathf.Abs(localSize.z) * 1.2f)
                {
                    fish.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                float vehicleLength = Mathf.Max(bounds.size.x, bounds.size.z);
                float fishLength = Mathf.Max(fb.size.x, Mathf.Max(fb.size.y, fb.size.z));
                if (fishLength > 0.001f)
                {
                    fish.transform.localScale = Vector3.one * (vehicleLength * 1.15f / fishLength) / Mathf.Max(0.0001f, vehicle.transform.lossyScale.x);
                }
                // Center the fish on the vehicle.
                if (FishTank.TryGetBounds(fish, out fb))
                {
                    fish.transform.position += bounds.center - fb.center;
                }
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
