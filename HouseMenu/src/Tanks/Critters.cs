using UnityEngine;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Cute little animals built from simple shapes, in Skylar's orange and green. The game has no frog,
    /// lizard or turtle animals (its frog/crocodile hats are clothing that needs a character to wear them),
    /// so these are ours. Each faces +Z (forward). All are visual-only (no colliders).
    /// </summary>
    internal static class Critters
    {
        public static readonly Color Orange = new Color(1f, 0.5f, 0f);
        public static readonly Color Green = new Color(0.2f, 0.72f, 0.2f);
        public static readonly Color Lime = new Color(0.55f, 0.9f, 0.25f);
        private static readonly Color EyeWhite = new Color(0.97f, 0.97f, 0.97f);
        private static readonly Color EyeBlack = new Color(0.05f, 0.05f, 0.05f);
        private static readonly Color Belly = new Color(0.95f, 0.9f, 0.6f);

        /// <summary>A round frog with big eyes. Hops (see Hopper).</summary>
        public static GameObject Frog(Color skin, float size)
        {
            var root = new GameObject("Frog");
            Transform body = Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0, 0.45f, 0), new Vector3(1f, 0.75f, 1.15f));
            Part(root.transform, PrimitiveType.Sphere, Belly, new Vector3(0, 0.38f, 0.12f), new Vector3(0.8f, 0.55f, 0.95f));
            // Eyes on top.
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0.28f * side, 0.82f, 0.32f), Vector3.one * 0.34f);
                Part(root.transform, PrimitiveType.Sphere, EyeWhite, new Vector3(0.3f * side, 0.86f, 0.42f), Vector3.one * 0.22f);
                Part(root.transform, PrimitiveType.Sphere, EyeBlack, new Vector3(0.31f * side, 0.87f, 0.5f), Vector3.one * 0.1f);
                // Back legs.
                Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0.45f * side, 0.2f, -0.3f), new Vector3(0.4f, 0.3f, 0.6f));
                // Front feet.
                Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0.3f * side, 0.08f, 0.45f), new Vector3(0.22f, 0.12f, 0.3f));
            }
            // Smile.
            Part(root.transform, PrimitiveType.Cube, EyeBlack, new Vector3(0, 0.42f, 0.62f), new Vector3(0.45f, 0.03f, 0.03f));
            root.transform.localScale = Vector3.one * size;
            return root;
        }

        /// <summary>A turtle with a domed shell. Plods slowly (see Wander).</summary>
        public static GameObject Turtle(Color shell, Color skin, float size)
        {
            var root = new GameObject("Turtle");
            Part(root.transform, PrimitiveType.Sphere, shell, new Vector3(0, 0.35f, 0), new Vector3(1.1f, 0.6f, 1.3f));
            Part(root.transform, PrimitiveType.Cylinder, Belly, new Vector3(0, 0.18f, 0), new Vector3(1.05f, 0.04f, 1.25f));
            // Shell spots.
            Part(root.transform, PrimitiveType.Sphere, Lime, new Vector3(0, 0.63f, 0), new Vector3(0.4f, 0.12f, 0.45f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root.transform, PrimitiveType.Sphere, Lime, new Vector3(0.3f * side, 0.55f, 0.25f), new Vector3(0.25f, 0.1f, 0.25f));
                Part(root.transform, PrimitiveType.Sphere, Lime, new Vector3(0.3f * side, 0.55f, -0.25f), new Vector3(0.25f, 0.1f, 0.25f));
                // Legs.
                Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0.48f * side, 0.12f, 0.38f), new Vector3(0.25f, 0.22f, 0.3f));
                Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0.48f * side, 0.12f, -0.38f), new Vector3(0.25f, 0.22f, 0.3f));
            }
            // Head with eyes.
            Transform head = Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0, 0.32f, 0.78f), new Vector3(0.38f, 0.34f, 0.42f));
            head.name = "Head";
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root.transform, PrimitiveType.Sphere, EyeBlack, new Vector3(0.12f * side, 0.4f, 0.95f), Vector3.one * 0.08f);
            }
            // Tail.
            Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0, 0.2f, -0.7f), new Vector3(0.12f, 0.1f, 0.25f));
            root.transform.localScale = Vector3.one * size;
            return root;
        }

        /// <summary>A long lizard with a wagging tail. Scurries and pauses (see Wander).</summary>
        public static GameObject Lizard(Color skin, Color stripe, float size)
        {
            var root = new GameObject("Lizard");
            Part(root.transform, PrimitiveType.Capsule, skin, new Vector3(0, 0.18f, 0), new Vector3(0.32f, 0.5f, 0.32f), Quaternion.Euler(90, 0, 0));
            Part(root.transform, PrimitiveType.Capsule, stripe, new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.42f, 0.12f), Quaternion.Euler(90, 0, 0));
            // Head.
            Part(root.transform, PrimitiveType.Sphere, skin, new Vector3(0, 0.2f, 0.6f), new Vector3(0.3f, 0.22f, 0.38f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root.transform, PrimitiveType.Sphere, EyeWhite, new Vector3(0.1f * side, 0.3f, 0.66f), Vector3.one * 0.1f);
                Part(root.transform, PrimitiveType.Sphere, EyeBlack, new Vector3(0.12f * side, 0.31f, 0.7f), Vector3.one * 0.05f);
                // Legs, splayed out.
                Part(root.transform, PrimitiveType.Capsule, skin, new Vector3(0.25f * side, 0.08f, 0.25f), new Vector3(0.08f, 0.12f, 0.08f), Quaternion.Euler(0, 0, 70 * side));
                Part(root.transform, PrimitiveType.Capsule, skin, new Vector3(0.25f * side, 0.08f, -0.25f), new Vector3(0.08f, 0.12f, 0.08f), Quaternion.Euler(0, 0, 70 * side));
            }
            // Tail: its own pivot so it can wag.
            var tailPivot = new GameObject("Tail").transform;
            tailPivot.SetParent(root.transform, false);
            tailPivot.localPosition = new Vector3(0, 0.15f, -0.45f);
            Part(tailPivot, PrimitiveType.Capsule, skin, new Vector3(0, 0, -0.35f), new Vector3(0.14f, 0.38f, 0.14f), Quaternion.Euler(90, 0, 0));
            Part(tailPivot, PrimitiveType.Capsule, stripe, new Vector3(0, 0.03f, -0.7f), new Vector3(0.08f, 0.2f, 0.08f), Quaternion.Euler(90, 0, 0));
            root.transform.localScale = Vector3.one * size;
            return root;
        }

        private static Transform Part(Transform parent, PrimitiveType type, Color color, Vector3 pos, Vector3 scale)
        {
            return Part(parent, type, color, pos, scale, Quaternion.identity);
        }

        private static Transform Part(Transform parent, PrimitiveType type, Color color, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            GameObject p = GameObject.CreatePrimitive(type);
            Object.Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = rot;
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = TankMaterials.Shared(color);
            return p.transform;
        }
    }

    /// <summary>
    /// Wanders around a rectangle inside the tank: walks to a random spot, pauses, picks another.
    /// Turtles are slow with long pauses; lizards are quick with short pauses and a wagging tail.
    /// Positions are in the tank's local space; Floor is the height to walk on.
    /// </summary>
    internal sealed class Wander : MonoBehaviour
    {
        public float HalfWidth = 1.5f;
        public float HalfDepth = 1f;
        public float Floor;
        public float Speed = 0.3f;
        public float TurnSpeed = 90f;
        public float PauseMin = 1f;
        public float PauseMax = 3f;
        public float TailWag;      // degrees; 0 = no tail
        public float Bob;          // walking bounce height

        private Vector3 _target;
        private float _pauseUntil;
        private Transform _tail;
        private float _walkTime;

        private void Start()
        {
            _tail = transform.Find("Tail");
            PickTarget();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.localPosition;
            Vector3 flat = new Vector3(pos.x, 0, pos.z);
            Vector3 to = new Vector3(_target.x, 0, _target.z) - flat;

            bool walking = Time.time >= _pauseUntil && to.magnitude > 0.05f;
            if (walking)
            {
                Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, want, TurnSpeed * dt);
                float facing = Vector3.Dot(transform.localRotation * Vector3.forward, to.normalized);
                if (facing > 0.7f)
                {
                    flat += to.normalized * Mathf.Min(Speed * dt, to.magnitude);
                    _walkTime += dt;
                }
            }
            else if (Time.time >= _pauseUntil)
            {
                _pauseUntil = Time.time + Random.Range(PauseMin, PauseMax);
                PickTarget();
            }

            float bob = walking ? Mathf.Abs(Mathf.Sin(_walkTime * 10f)) * Bob : 0f;
            transform.localPosition = new Vector3(flat.x, Floor + bob, flat.z);

            if (_tail != null && TailWag > 0f)
            {
                float rate = walking ? 12f : 3f;
                _tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * rate) * TailWag, 0);
            }
        }

        private void PickTarget()
        {
            _target = new Vector3(Random.Range(-HalfWidth, HalfWidth), Floor, Random.Range(-HalfDepth, HalfDepth));
        }
    }

    /// <summary>Hops around like a frog: crouch, leap in an arc to a nearby spot, sit for a bit.</summary>
    internal sealed class Hopper : MonoBehaviour
    {
        public float HalfWidth = 1.5f;
        public float HalfDepth = 1f;
        public float Floor;
        public float HopDistance = 0.6f;
        public float HopHeight = 0.4f;
        public float HopTime = 0.5f;
        public float SitMin = 1.5f;
        public float SitMax = 4f;

        private Vector3 _from;
        private Vector3 _to;
        private float _hopStart = -1f;
        private float _nextHop;
        private Vector3 _restScale;

        private void Start()
        {
            _restScale = transform.localScale;
            _nextHop = Time.time + Random.Range(0.5f, SitMax);
        }

        private void Update()
        {
            if (_hopStart < 0f)
            {
                // Sitting. A little breathing squash, then hop when it's time.
                float breathe = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
                transform.localScale = new Vector3(_restScale.x, _restScale.y * breathe, _restScale.z);
                if (Time.time >= _nextHop) StartHop();
                return;
            }

            float t = (Time.time - _hopStart) / HopTime;
            if (t >= 1f)
            {
                transform.localPosition = _to;
                transform.localScale = _restScale;
                _hopStart = -1f;
                _nextHop = Time.time + Random.Range(SitMin, SitMax);
                return;
            }

            Vector3 p = Vector3.Lerp(_from, _to, t);
            p.y = Floor + Mathf.Sin(t * Mathf.PI) * HopHeight;
            transform.localPosition = p;
            float stretch = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            transform.localScale = new Vector3(_restScale.x / stretch, _restScale.y * stretch, _restScale.z);
        }

        private void StartHop()
        {
            _from = transform.localPosition;
            float angle = Random.Range(0f, 360f);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 to = _from + dir * HopDistance;
            to.x = Mathf.Clamp(to.x, -HalfWidth, HalfWidth);
            to.z = Mathf.Clamp(to.z, -HalfDepth, HalfDepth);
            to.y = Floor;
            _to = to;
            Vector3 face = new Vector3(_to.x - _from.x, 0, _to.z - _from.z);
            if (face.sqrMagnitude > 0.0001f) transform.localRotation = Quaternion.LookRotation(face, Vector3.up);
            _hopStart = Time.time;
        }
    }
}
