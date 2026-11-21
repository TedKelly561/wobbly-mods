using System;
using System.Collections.Generic;
using UnityEngine;
using WobblyMods.HouseMenu.Tanks;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// The fun page (family's picks, 2026-11-21): every car a catfish, a catfish parade, a redtail pet, the bubble
    /// gun, paint the world, adjustable gravity. Everything here is local, switchable off, and uses only Unity
    /// physics and our own visual objects, never the game's scripts.
    /// </summary>
    internal static class Fun
    {
        // ---------- Gravity ----------

        private static readonly float[] GravitySteps = { 1f, 0.7f, 0.5f, 0.35f, 0.25f, 0.15f, 0.08f, 0.04f };
        private static Vector3? _normalGravity;
        private static int _gravityStep;

        /// <summary>Floatier (+1), heavier (-1) or back to normal (0). Fine steps, so the fun spot can be found.</summary>
        public static string Gravity(int change)
        {
            if (_normalGravity == null) _normalGravity = Physics.gravity;
            _gravityStep = change == 0 ? 0 : Mathf.Clamp(_gravityStep + change, 0, GravitySteps.Length - 1);
            Physics.gravity = _normalGravity.Value * GravitySteps[_gravityStep];
            int percent = Mathf.RoundToInt(GravitySteps[_gravityStep] * 100f);
            Plugin.Log.LogInfo("Fun: gravity " + percent + "% (" + Physics.gravity.y.ToString("0.00") + ").");
            return _gravityStep == 0 ? "Normal gravity." : "Gravity " + percent + "%. Jump!";
        }

        // ---------- Paint the world ----------

        private static readonly Color[] Paints =
        {
            new Color(1f, 0.5f, 0.05f), new Color(0.25f, 0.67f, 0.24f), new Color(1f, 0.85f, 0.1f), new Color(0.95f, 0.3f, 0.6f),
            new Color(0.25f, 0.55f, 0.95f), new Color(0.6f, 0.3f, 0.8f), Color.white, new Color(0.1f, 0.1f, 0.1f)
        };
        private static readonly int[] PaintIndex = new int[8];

        public static Color NextPaint(int player)
        {
            return Paints[PaintIndex[player & 7] % Paints.Length];
        }

        /// <summary>Paints whatever that player is looking at with their next color.</summary>
        public static string Paint(int player)
        {
            try
            {
                Collider hit = LookedAt(player, 40f);
                if (hit == null) return "Point the middle of your screen at something.";
                GameObject root = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Renderer inParent = hit.GetComponentInParent<Renderer>();
                    if (inParent == null) return "That can't be painted.";
                    renderers = new[] { inParent };
                }
                if (renderers.Length > 150) return "That's too big to paint.";

                Color color = NextPaint(player);
                PaintIndex[player & 7]++;
                int painted = 0;
                foreach (Renderer r in renderers)
                {
                    if (r is ParticleSystemRenderer || r.isPartOfStaticBatch) continue;
                    foreach (Material m in r.materials) // instances: only this object changes
                    {
                        if (m == null || m.renderQueue >= 3000) continue;
                        TankMaterials.SetColor(m, color);
                        if (m.HasProperty("_Tint")) m.SetColor("_Tint", color);
                        painted++;
                    }
                }
                Plugin.Log.LogInfo("Fun: player " + (player + 1) + " painted " + root.name + " (" + painted + " materials).");
                return painted > 0 ? "Painted!" : "That one is glued into the scenery. Try another.";
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Fun: paint failed: " + e.Message);
                return "That can't be painted.";
            }
        }

        // ---------- Bubble gun ----------

        public static string BubbleGun(int player)
        {
            try
            {
                Collider hit = LookedAt(player, 40f);
                if (hit == null) return "Point the middle of your screen at something.";
                List<Rigidbody> bodies = BodiesOf(hit);
                if (bodies.Count == 0) return "That's stuck down. Try something loose, like a car or a ball.";
                if (bodies[0].GetComponentInParent<FloatBubble>() != null || FloatBubble.Holds(bodies[0])) return "It's already in a bubble!";

                var go = new GameObject("Wobbly bubble");
                go.AddComponent<FloatBubble>().Begin(bodies);
                Plugin.Log.LogInfo("Fun: player " + (player + 1) + " bubbled " + bodies[0].transform.root.name + " (" + bodies.Count + " bodies).");
                return "Bubble!";
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Fun: bubble failed: " + e.Message);
                return "The bubble popped too soon.";
            }
        }

        /// <summary>
        /// All the physics bodies that make up the thing a collider belongs to: a whole Wobbly (every ragdoll part),
        /// a whole vehicle, or one loose prop.
        /// </summary>
        internal static List<Rigidbody> BodiesOf(Collider hit)
        {
            var bodies = new List<Rigidbody>();
            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null) return bodies;

            Component owner = null;
            foreach (string typeName in new[] { "PlayerCharacter", "PlayerVehicle" })
            {
                Type type = GameReflection.FindType(typeName);
                if (type != null) owner = hit.GetComponentInParent(type);
                if (owner != null) break;
            }
            if (owner != null) bodies.AddRange(owner.GetComponentsInChildren<Rigidbody>());
            if (bodies.Count == 0) bodies.Add(rb);
            bodies.RemoveAll(b => b == null || b.isKinematic);
            return bodies;
        }

        /// <summary>The first solid thing in the middle of that player's view that isn't the player.</summary>
        internal static Collider LookedAt(int player, float distance)
        {
            Camera cam = GameBindings.CameraFor(player);
            if (cam == null) return null;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, distance);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Component me = null;
            Type character = GameReflection.FindType("PlayerCharacter");
            List<object> locals = GameReflection.LocalPlayerControllers();
            if (player >= 0 && player < locals.Count)
            {
                me = (GameReflection.Call(locals[player], "GetPlayerCharacter") ?? GameReflection.Get(locals[player], "playerCharacter")) as Component;
            }
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.GetComponentInParent<FloatBubble>() != null || hit.collider.GetComponentInParent<Follower>() != null) continue;
                if (me != null && character != null && hit.collider.GetComponentInParent(character) == me) continue; // not yourself
                return hit.collider;
            }
            return null;
        }

        // ---------- Catfish world: every vehicle is a catfish ----------

        private static bool _catfishWorld;
        private static float _nextWorldSweep;

        public static string ToggleCatfishWorld()
        {
            _catfishWorld = !_catfishWorld;
            if (_catfishWorld)
            {
                int n = CatfishRide.ApplyToAll();
                _nextWorldSweep = Time.unscaledTime + 2f;
                return n > 0 ? "Every car is a catfish! (" + n + ")" : "Catfish world is on. Cars turn into catfish as they show up.";
            }
            int back = CatfishRide.RestoreAll();
            return "Back to normal cars. (" + back + ")";
        }

        // ---------- Parade and pet: redtails that follow a player ----------

        private static readonly Follower[] Parades = new Follower[8];
        private static readonly Follower[] Pets = new Follower[8];

        public static string ToggleParade(int player)
        {
            return ToggleFollower(Parades, player, 7, 2.4f, 2.8f, 0.9f, "Catfish parade! They follow you.", "The parade swam home.");
        }

        public static string TogglePet(int player)
        {
            return ToggleFollower(Pets, player, 1, 0.55f, 1.1f, 0.75f, "Your redtail pet is following you!", "Your redtail went for a nap.");
        }

        private static string ToggleFollower(Follower[] slots, int player, int count, float length, float spacing, float hover, string on, string off)
        {
            int i = player & 7;
            if (slots[i] != null)
            {
                UnityEngine.Object.Destroy(slots[i].gameObject);
                slots[i] = null;
                return off;
            }
            if (GameBindings.CharacterPosition(player) == null) return "Couldn't find your Wobbly.";
            var go = new GameObject("Wobbly redtail followers (player " + (player + 1) + ")");
            slots[i] = go.AddComponent<Follower>();
            slots[i].Begin(player, count, length, spacing, hover);
            Plugin.Log.LogInfo("Fun: player " + (player + 1) + " has " + count + " redtail follower(s).");
            return on;
        }

        /// <summary>Call once per frame from the plugin.</summary>
        public static void Tick()
        {
            if (_catfishWorld && Time.unscaledTime >= _nextWorldSweep)
            {
                _nextWorldSweep = Time.unscaledTime + 2f;
                try { CatfishRide.ApplyToAll(); }
                catch (Exception e) { Plugin.Log.LogWarning("Fun: catfish world sweep failed: " + e.Message); _catfishWorld = false; }
            }
        }
    }

    /// <summary>
    /// A bubble around something: it floats up gently for a few seconds, then pops and drops. Works on anything
    /// with a physics body (props, vehicles, Wobblies). Gravity is put back exactly as it was.
    /// </summary>
    internal sealed class FloatBubble : MonoBehaviour
    {
        private const float Seconds = 6f;
        private static readonly HashSet<Rigidbody> Held = new HashSet<Rigidbody>();

        private readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        private readonly List<bool> _hadGravity = new List<bool>();
        private Transform _ball;
        private float _popAt;
        private float _radius;

        public static bool Holds(Rigidbody body)
        {
            return Held.Contains(body);
        }

        public void Begin(List<Rigidbody> bodies)
        {
            Bounds b = new Bounds(bodies[0].worldCenterOfMass, Vector3.zero);
            foreach (Rigidbody rb in bodies)
            {
                _bodies.Add(rb);
                _hadGravity.Add(rb.useGravity);
                rb.useGravity = false;
                Held.Add(rb);
                foreach (Collider c in rb.GetComponentsInChildren<Collider>())
                {
                    if (!c.isTrigger && c.enabled) b.Encapsulate(c.bounds);
                }
            }
            _radius = Mathf.Clamp(b.extents.magnitude * 1.05f, 0.4f, 12f);

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider col = ball.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col); // look only: it must never push anything
            ball.name = "bubble";
            ball.transform.SetParent(transform, false);
            ball.GetComponent<Renderer>().sharedMaterial = TankMaterials.Bubble();
            _ball = ball.transform;
            transform.position = b.center;
            _ball.localScale = Vector3.one * _radius * 2f;
            _popAt = Time.time + Seconds;
            FmodSounds.TryPlay(MenuSound.Pop);
        }

        private void FixedUpdate()
        {
            float left = _popAt - Time.time;
            Vector3 center = Vector3.zero;
            int alive = 0;
            foreach (Rigidbody rb in _bodies)
            {
                if (rb == null) continue;
                // Drift up, slower near the end, and calm any spinning: a gentle float, not a launch.
                Vector3 want = new Vector3(rb.velocity.x * 0.96f, left > 1.5f ? 1.6f : 0.3f, rb.velocity.z * 0.96f);
                rb.velocity = Vector3.Lerp(rb.velocity, want, 0.12f);
                rb.angularVelocity *= 0.97f;
                center += rb.worldCenterOfMass;
                alive++;
            }
            if (alive == 0 || left <= 0f)
            {
                Pop();
                return;
            }
            transform.position = center / alive;
            float wobble = 1f + 0.04f * Mathf.Sin(Time.time * 5f);
            _ball.localScale = new Vector3(_radius * 2f * wobble, _radius * 2f / wobble, _radius * 2f * wobble);
        }

        private void Pop()
        {
            Release();
            FmodSounds.TryPlay(MenuSound.Pop);
            Destroy(gameObject);
        }

        private void Release()
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                if (_bodies[i] == null) continue;
                _bodies[i].useGravity = _hadGravity[i];
                Held.Remove(_bodies[i]);
            }
            _bodies.Clear();
        }

        private void OnDestroy()
        {
            Release(); // also when the scene unloads: never leave something weightless
        }
    }

    /// <summary>
    /// Redtails that follow a player like ducklings: each swims along the path the player took, a set distance
    /// behind the one in front. Look only (no colliders), so they can't push or block anything.
    /// </summary>
    internal sealed class Follower : MonoBehaviour
    {
        private readonly List<Vector3> _trail = new List<Vector3>();
        private readonly List<Transform> _fish = new List<Transform>();
        private int _player;
        private float _spacing, _hover, _length;

        public void Begin(int player, int count, float length, float spacing, float hover)
        {
            _player = player;
            _spacing = spacing;
            _hover = hover;
            _length = length;
            Vector3 start = GameBindings.CharacterPosition(player) ?? Vector3.zero;
            _trail.Add(start);
            for (int i = 0; i < count; i++)
            {
                GameObject fish = ShapeModels.Redtail();
                fish.transform.SetParent(transform, false);
                // A little size variety in a parade, biggest first.
                fish.transform.localScale = Vector3.one * length * (count > 1 ? 1f - 0.07f * i : 1f);
                fish.transform.position = start + Vector3.up * hover - Vector3.forward * spacing * (i + 1);
                _fish.Add(fish.transform);
            }
        }

        private void Update()
        {
            Vector3? where = GameBindings.CharacterPosition(_player);
            if (where == null) return;
            Vector3 head = where.Value;
            if ((head - _trail[0]).sqrMagnitude > 0.04f) _trail.Insert(0, head);
            int keep = Mathf.CeilToInt((_fish.Count + 2) * _spacing / 0.2f) + 10;
            if (_trail.Count > keep) _trail.RemoveRange(keep, _trail.Count - keep);

            // A teleport or respawn: bring everyone along instead of swimming across the map.
            if ((_fish[0].position - head).sqrMagnitude > 60f * 60f)
            {
                _trail.Clear();
                _trail.Add(head);
                foreach (Transform f in _fish) f.position = head + Vector3.up * _hover;
            }

            for (int i = 0; i < _fish.Count; i++)
            {
                Vector3 target = PointBehind(_spacing * (i + 1)) + Vector3.up * (_hover + 0.08f * Mathf.Sin(Time.time * 2.2f + i));
                Transform f = _fish[i];
                Vector3 before = f.position;
                f.position = Vector3.Lerp(before, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
                Vector3 moved = f.position - before;
                Vector3 look = moved.sqrMagnitude > 1e-6f ? moved : head - f.position;
                look.y *= 0.3f;
                if (look.sqrMagnitude > 1e-6f)
                {
                    Quaternion face = Quaternion.LookRotation(look.normalized, Vector3.up) *
                                      Quaternion.Euler(0f, 8f * Mathf.Sin(Time.time * 7f + i * 1.3f), 0f); // tail wiggle
                    f.rotation = Quaternion.Slerp(f.rotation, face, 1f - Mathf.Exp(-8f * Time.deltaTime));
                }
            }
        }

        /// <summary>The point that far back along the path the player took.</summary>
        private Vector3 PointBehind(float distance)
        {
            for (int i = 0; i + 1 < _trail.Count; i++)
            {
                float step = Vector3.Distance(_trail[i], _trail[i + 1]);
                if (step >= distance && step > 1e-5f) return Vector3.Lerp(_trail[i], _trail[i + 1], distance / step);
                distance -= step;
            }
            // Not walked far enough yet: line up behind the last known point.
            Vector3 last = _trail[_trail.Count - 1];
            Vector3 back = _trail.Count > 1 ? (last - _trail[_trail.Count - 2]).normalized : Vector3.back;
            return last + back * distance;
        }
    }

    /// <summary>Skylar's trampoline rug: anything with a physics body that touches it gets bounced up.</summary>
    internal sealed class Trampoline : MonoBehaviour
    {
        public float Speed = 11f;
        private readonly Dictionary<Rigidbody, float> _recent = new Dictionary<Rigidbody, float>();

        private void OnTriggerEnter(Collider other)
        {
            Bounce(other);
        }

        private void OnTriggerStay(Collider other)
        {
            Bounce(other);
        }

        private void Bounce(Collider other)
        {
            if (other == null || other.isTrigger || other.attachedRigidbody == null) return;
            List<Rigidbody> bodies = Fun.BodiesOf(other);
            if (bodies.Count == 0) return;
            float last;
            if (_recent.TryGetValue(bodies[0], out last) && Time.time - last < 0.35f) return;
            if (_recent.Count > 200) _recent.Clear();
            _recent[bodies[0]] = Time.time;

            float up = Speed * transform.lossyScale.y; // Bigger rug, bigger bounce
            foreach (Rigidbody rb in bodies)
            {
                Vector3 v = rb.velocity;
                if (v.y < up) rb.velocity = new Vector3(v.x, up, v.z);
            }
            FmodSounds.TryPlay(MenuSound.Pop);
        }
    }
}
