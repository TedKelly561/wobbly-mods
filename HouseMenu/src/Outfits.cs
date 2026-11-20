using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using WobblyMods.HouseMenu.Tanks;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// "Dress up" (Skylar's request, 2026-11-19): she stays a Wobbly but wears an animal costume: cat, bunny,
    /// puppy, redtail catfish or T-Rex. Parent's choice: a Wobbly in a costume, not a full animal body.
    ///
    /// How: simple visual-only shapes (no colliders, no physics) are attached to the character's own head and hip
    /// bones (Player/Wobbly/Hip/Chest/Head, confirmed in research), so they move and wobble with her, and her body
    /// color is set with the game's own CharacterCustomize.SetCharacterColor (put back by "Take off").
    /// Shapes are measured in "head units" (1 = about the head's radius, from the character's height).
    ///
    /// Bone axes aren't known, so the costume is placed using world-up and the head bone's forward at the moment
    /// she puts it on; "Turn costume" turns it a quarter turn if the face is on the wrong side, and that choice is
    /// remembered in the config for everyone. Local look only (other PCs see the color, not the parts).
    /// </summary>
    internal static class Outfits
    {
        public static readonly string[] Ids = { "cat", "bunny", "puppy", "redtail", "trex" };

        private static readonly Color Pink = new Color(1f, 0.55f, 0.7f);
        private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f);
        private static readonly Color RedTail = new Color(1f, 0.3f, 0.05f);
        private static readonly Color Black = new Color(0.03f, 0.03f, 0.03f);
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.05f);

        private sealed class Worn
        {
            public string Id;
            public Component Character;
            public object Customize;
            public object OriginalColor;   // boxed Color from GetCharacterColor, or null
            public Color? Fur;             // Skylar's own color pick, or null for the costume's own
            public readonly List<GameObject> Roots = new List<GameObject>();
        }

        private static readonly Dictionary<int, Worn> Wearing = new Dictionary<int, Worn>();
        private static ConfigEntry<int> _quarterTurns;
        private static ConfigEntry<float> _size;
        private static ConfigEntry<float> _headHeight;
        private static Mesh _cone;

        // Set while building: where new parts go.
        private static Transform _into;
        private static Color _fur;
        private static int _layer;

        public static void Init(ConfigFile config)
        {
            _quarterTurns = config.Bind("Outfits", "FacingQuarterTurns", 2,
                "Quarter turns added to costumes so the face is at the front. The menu's 'Turn costume' changes this.");
            _size = config.Bind("Outfits", "CostumeSize", 1f, "Costume size multiplier. The menu's Bigger and Smaller change this.");
            _headHeight = config.Bind("Outfits", "HeadHeight", 0f,
                "Moves head pieces up (+) or down (-), in head units, if ears and eyes sit too high or low.");
        }

        public static string NiceName(string id)
        {
            switch (id)
            {
                case "cat": return "cat";
                case "bunny": return "bunny";
                case "puppy": return "puppy";
                case "redtail": return "redtail catfish";
                case "trex": return "T-Rex";
                default: return id;
            }
        }

        private static Color DefaultFur(string id)
        {
            switch (id)
            {
                case "cat": return new Color(0.06f, 0.06f, 0.07f);
                case "bunny": return Color.white;
                case "puppy": return new Color(0.6f, 0.4f, 0.22f);
                case "redtail": return new Color(0.13f, 0.13f, 0.15f);
                default: return new Color(0.25f, 0.62f, 0.2f);
            }
        }

        /// <summary>Puts a costume on (replacing any other). Returns a message for the menu.</summary>
        public static string Wear(int player, string id)
        {
            Worn old;
            Color? keepFur = null;
            object keepOriginal = null;
            if (Wearing.TryGetValue(player, out old))
            {
                // Same costume again keeps her color pick; the true original body color is kept across changes.
                if (old.Id == id) keepFur = old.Fur;
                if (old.Character != null) keepOriginal = old.OriginalColor;
                RemoveParts(old);
                Wearing.Remove(player);
            }

            try
            {
                Worn worn = Build(player, id, keepFur, keepOriginal);
                if (worn == null) return "Couldn't find your Wobbly to dress up.";
                Wearing[player] = worn;
                return "You're a " + NiceName(id) + "!";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Dress up (" + id + ") failed: " + e);
                return "The costume got stuck.";
            }
        }

        public static string TakeOff(int player)
        {
            Worn worn;
            if (!Wearing.TryGetValue(player, out worn)) return "You aren't wearing a costume.";
            RemoveParts(worn);
            if (worn.Character != null && worn.OriginalColor != null)
            {
                SetBodyColor(worn.Customize, (Color)worn.OriginalColor);
            }
            Wearing.Remove(player);
            return "Costume off.";
        }

        /// <summary>Skylar picks the fur color (bunny: "she picks").</summary>
        public static string SetFur(int player, Color color)
        {
            Worn worn;
            if (!Wearing.TryGetValue(player, out worn)) return "Put a costume on first.";
            worn.Fur = color;
            return Rewear(player, "New fur color!");
        }

        public static string Turn(int player)
        {
            _quarterTurns.Value = (_quarterTurns.Value + 1) % 4;
            Plugin.Log.LogInfo("Dress up: FacingQuarterTurns = " + _quarterTurns.Value);
            return Rewear(player, "Turned the costume.");
        }

        public static string Resize(int player, float factor)
        {
            _size.Value = Mathf.Clamp(_size.Value * factor, 0.4f, 3f);
            Plugin.Log.LogInfo("Dress up: Size = " + _size.Value.ToString("0.00"));
            return Rewear(player, factor > 1f ? "Bigger costume!" : "Smaller costume!");
        }

        private static string Rewear(int player, string message)
        {
            Worn worn;
            if (!Wearing.TryGetValue(player, out worn)) return "Put a costume on first.";
            Color? fur = worn.Fur;
            string id = worn.Id;
            worn.Fur = fur;
            string result = Wear(player, id);
            return result.StartsWith("You're", StringComparison.Ordinal) ? message : result;
        }

        private static Worn Build(int player, string id, Color? fur, object original)
        {
            List<object> locals = GameReflection.LocalPlayerControllers();
            if (player < 0 || player >= locals.Count) return null;
            Component character = (GameReflection.Call(locals[player], "GetPlayerCharacter")
                                   ?? GameReflection.Get(locals[player], "playerCharacter")) as Component;
            if (character == null) return null;

            Transform head = FindBone(character.transform, "Player/Wobbly/Hip/Chest/Head", "Head");
            Transform hip = FindBone(character.transform, "Player/Wobbly/Hip", "Hip");
            if (head == null || hip == null)
            {
                Plugin.Log.LogWarning("Dress up: bones not found (head " + (head != null) + ", hip " + (hip != null) + ").");
                return null;
            }

            // Size and place from the BONES only. Test 20 showed renderer bounds are useless here: the skinned
            // mesh's bounds are far bigger than the body and included our own parts, so the costume grew and
            // climbed on every press. The head bone's own collider (ragdoll) gives the head's real center and size.
            int layer = character.gameObject.layer;
            Renderer first = character.GetComponentInChildren<Renderer>();
            if (first != null) layer = first.gameObject.layer;

            float spine = Vector3.Distance(head.position, hip.position);
            if (spine < 0.05f || spine > 10f) spine = 0.77f;
            Vector3 headCenter = head.position + Vector3.up * spine * 0.3f;
            float headRadius = spine * 0.42f;
            string measured = "guessed from the bones";
            Collider headCollider = head.GetComponent<Collider>();
            if (headCollider != null && !headCollider.isTrigger)
            {
                float scale = Mathf.Max(head.lossyScale.x, Mathf.Max(head.lossyScale.y, head.lossyScale.z));
                SphereCollider sphere = headCollider as SphereCollider;
                CapsuleCollider capsule = headCollider as CapsuleCollider;
                BoxCollider box = headCollider as BoxCollider;
                float r = 0f;
                Vector3 c = head.position;
                if (sphere != null) { r = sphere.radius * scale; c = head.TransformPoint(sphere.center); }
                else if (capsule != null) { r = Mathf.Max(capsule.radius, capsule.height * 0.5f) * scale; c = head.TransformPoint(capsule.center); }
                else if (box != null) { r = Mathf.Max(box.size.x, Mathf.Max(box.size.y, box.size.z)) * 0.5f * scale; c = head.TransformPoint(box.center); }
                if (r > spine * 0.15f && r < spine * 1.5f)
                {
                    headRadius = r;
                    headCenter = c;
                    measured = headCollider.GetType().Name;
                }
            }
            float unit = headRadius * _size.Value;
            headCenter += Vector3.up * _headHeight.Value * headRadius;

            Vector3 forward = head.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) { forward = head.up; forward.y = 0f; }
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Quaternion facing = Quaternion.AngleAxis(90f * _quarterTurns.Value, Vector3.up) * Quaternion.LookRotation(forward.normalized, Vector3.up);

            var worn = new Worn { Id = id, Character = character, Fur = fur };
            worn.Customize = GameReflection.Call(character, "GetPlayerCharacterCustomize");
            worn.OriginalColor = original ?? GameReflection.Call(worn.Customize, "GetCharacterColor");

            _fur = fur ?? DefaultFur(id);
            _layer = layer;
            Transform headRoot = Root(worn, "Wobbly costume (head)", head, headCenter, facing, unit);
            Transform hipRoot = Root(worn, "Wobbly costume (body)", hip, hip.position, facing, unit);

            switch (id)
            {
                case "cat": Cat(headRoot, hipRoot); break;
                case "bunny": Bunny(headRoot, hipRoot); break;
                case "puppy": Puppy(headRoot, hipRoot); break;
                case "redtail": Redtail(headRoot, hipRoot); break;
                default: TRex(headRoot, hipRoot); break;
            }

            bool colored = SetBodyColor(worn.Customize, _fur);
            Plugin.Log.LogInfo("Dress up: player " + (player + 1) + " is a " + NiceName(id) + ". Head radius " + headRadius.ToString("0.00") +
                               " (" + measured + "), head center " + (headCenter.y - head.position.y).ToString("0.00") + " above the head bone, spine " +
                               spine.ToString("0.00") + ", size " + _size.Value.ToString("0.00") + ", unit " + unit.ToString("0.00") +
                               ", head forward " + head.forward.ToString("0.0") + ", head up " + head.up.ToString("0.0") +
                               ", quarter turns " + _quarterTurns.Value + ", body color " + (colored ? "set" : "NOT set") + ".");
            return worn;
        }

        // ---- The costumes. Units: 1 = head radius. +Z is the face side, +Y is up. ----

        private static void Cat(Transform head, Transform hip)
        {
            _into = head;
            for (int s = -1; s <= 1; s += 2)
            {
                Cone(new Vector3(0.5f * s, 0.7f, 0f), new Vector3(0.65f, 0.85f, 0.4f), new Vector3(0, 0, -15f * s), _fur);
                Cone(new Vector3(0.5f * s, 0.74f, 0.1f), new Vector3(0.36f, 0.5f, 0.26f), new Vector3(0, 0, -15f * s), Pink);
                Ball(new Vector3(0.36f * s, 0.15f, 0.86f), Vector3.one * 0.38f, Yellow);
                Ball(new Vector3(0.36f * s, 0.15f, 1.02f), new Vector3(0.09f, 0.3f, 0.09f), Black);
                Whiskers(s, Color.white, 0.4f);
            }
            Ball(new Vector3(0f, -0.12f, 1.0f), new Vector3(0.18f, 0.12f, 0.12f), Pink);

            _into = Holder(hip, "tail", new Vector3(0f, 0f, -0.8f), 14f, 2.2f);
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                Ball(new Vector3(0f, 1.7f * t * t, -0.1f - 0.9f * t + 0.5f * t * t), Vector3.one * (0.36f - 0.12f * t), _fur);
            }
        }

        private static void Bunny(Transform head, Transform hip)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                // Big floppy ears: up, then the top half flops over to the side. They sway a little.
                _into = Holder(head, "ear", new Vector3(0.4f * s, 0.75f, 0f), 6f, 1.3f + 0.2f * s);
                Part(PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f), new Vector3(0.36f, 0.7f, 0.2f), new Vector3(0, 0, -6f * s), _fur);
                Part(PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0.08f), new Vector3(0.2f, 0.55f, 0.12f), new Vector3(0, 0, -6f * s), Pink);
                Part(PrimitiveType.Capsule, new Vector3(0.42f * s, 1.28f, 0.05f), new Vector3(0.34f, 0.55f, 0.2f), new Vector3(0, 0, -80f * s), _fur);
                Part(PrimitiveType.Capsule, new Vector3(0.78f * s, 1.0f, 0.1f), new Vector3(0.32f, 0.4f, 0.2f), new Vector3(0, 0, -160f * s), _fur);
            }
            _into = head;
            Ball(new Vector3(0f, -0.1f, 1.0f), new Vector3(0.18f, 0.13f, 0.12f), Pink);
            Part(PrimitiveType.Cube, new Vector3(-0.09f, -0.42f, 0.94f), new Vector3(0.16f, 0.22f, 0.06f), Vector3.zero, Color.white);
            Part(PrimitiveType.Cube, new Vector3(0.09f, -0.42f, 0.94f), new Vector3(0.16f, 0.22f, 0.06f), Vector3.zero, Color.white);
            for (int s = -1; s <= 1; s += 2) Whiskers(s, Color.white, 0.3f);

            _into = hip;
            Ball(new Vector3(0f, 0.1f, -1.0f), Vector3.one * 0.6f, Color.white);
        }

        private static void Puppy(Transform head, Transform hip)
        {
            Color dark = Color.Lerp(_fur, Color.black, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                _into = Holder(head, "ear", new Vector3(0.95f * s, 0.55f, 0f), 5f, 1.6f + 0.3f * s);
                Ball(new Vector3(0.05f * s, -0.5f, 0f), new Vector3(0.3f, 1.05f, 0.62f), dark);
            }
            _into = head;
            Ball(new Vector3(0f, -0.25f, 0.82f), new Vector3(0.75f, 0.52f, 0.65f), Cream);
            Ball(new Vector3(0f, -0.1f, 1.14f), new Vector3(0.26f, 0.19f, 0.17f), Black);
            Ball(new Vector3(0f, -0.52f, 0.98f), new Vector3(0.2f, 0.06f, 0.3f), Pink);
            Ball(new Vector3(0.45f, 0.35f, 0.7f), new Vector3(0.5f, 0.55f, 0.3f), dark); // eye patch

            _into = Holder(hip, "tail", new Vector3(0f, 0.2f, -0.8f), 30f, 9f);
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                Ball(new Vector3(0f, 0.9f * t, -0.1f - 0.45f * t), Vector3.one * (0.3f - 0.1f * t), i == 4 ? Cream : _fur);
            }
        }

        private static void Redtail(Transform head, Transform hip)
        {
            _into = head;
            Ball(new Vector3(0f, -0.48f, 0.5f), new Vector3(1.35f, 0.5f, 1.05f), Cream);   // the white mouth band
            for (int s = -1; s <= 1; s += 2)
            {
                // Long whiskers (barbels) sweeping out and back, and two short ones on the chin.
                Part(PrimitiveType.Cylinder, new Vector3(1.15f * s, -0.38f, 0.75f), new Vector3(0.06f, 0.65f, 0.06f), new Vector3(0, 20f * s, 90f - 12f * s), _fur);
                Part(PrimitiveType.Cylinder, new Vector3(0.28f * s, -0.95f, 0.75f), new Vector3(0.05f, 0.3f, 0.05f), new Vector3(10f, 0, 8f * s), Cream);
            }
            Ball(new Vector3(0f, 0.95f, -0.2f), new Vector3(0.12f, 0.75f, 0.9f), _fur);     // back fin
            Ball(new Vector3(0f, 1.3f, -0.4f), new Vector3(0.1f, 0.35f, 0.5f), RedTail);

            _into = hip;
            Ball(new Vector3(0f, 0.25f, 0.5f), new Vector3(1.15f, 1.3f, 0.7f), Cream);      // white belly
            for (int s = -1; s <= 1; s += 2)
            {
                Ball(new Vector3(1.0f * s, 0.1f, 0.25f), new Vector3(0.6f, 0.12f, 0.42f), RedTail, new Vector3(0, 0, -25f * s));
            }
            _into = Holder(hip, "tail", new Vector3(0f, 0.1f, -0.8f), 22f, 3.5f);
            Ball(new Vector3(0f, 0f, -0.4f), new Vector3(0.5f, 0.6f, 1.0f), _fur);
            Ball(new Vector3(0f, 0.55f, -1.25f), new Vector3(0.14f, 0.95f, 0.6f), RedTail, new Vector3(40f, 0, 0));
            Ball(new Vector3(0f, -0.55f, -1.25f), new Vector3(0.14f, 0.95f, 0.6f), RedTail, new Vector3(-40f, 0, 0));
            Ball(new Vector3(0f, 0f, -1.0f), new Vector3(0.15f, 0.6f, 0.5f), RedTail);
        }

        private static void TRex(Transform head, Transform hip)
        {
            Color spikes = new Color(1f, 0.5f, 0.05f); // orange and green: Skylar's colors
            _into = head;
            Ball(new Vector3(0f, -0.1f, 0.95f), new Vector3(1.0f, 0.62f, 1.35f), _fur);     // snout
            Ball(new Vector3(0f, -0.5f, 0.85f), new Vector3(0.85f, 0.3f, 1.1f), Color.Lerp(_fur, Color.black, 0.25f)); // jaw
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    Cone(new Vector3(0.36f * s - 0.03f * s * i, -0.3f, 0.95f + 0.2f * i), new Vector3(0.14f, 0.24f, 0.14f), new Vector3(180f, 0, 0), Color.white);
                }
                Ball(new Vector3(0.48f * s, 0.42f, 0.7f), Vector3.one * 0.3f, Yellow);
                Ball(new Vector3(0.52f * s, 0.42f, 0.83f), new Vector3(0.1f, 0.22f, 0.08f), Black);
                Ball(new Vector3(0.2f * s, 0.12f, 1.55f), Vector3.one * 0.09f, Black);       // nostrils
            }
            for (int i = 0; i < 3; i++)
            {
                Cone(new Vector3(0f, 0.9f - 0.12f * i, 0.25f - 0.45f * i), new Vector3(0.32f, 0.5f, 0.32f), new Vector3(-20f - 25f * i, 0, 0), spikes);
            }

            _into = hip;
            for (int i = 0; i < 2; i++)
            {
                Cone(new Vector3(0f, 0.8f - 0.6f * i, -0.75f), new Vector3(0.34f, 0.5f, 0.34f), new Vector3(-75f, 0, 0), spikes);
            }
            _into = Holder(hip, "tail", new Vector3(0f, 0f, -0.7f), 10f, 1.6f);
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                Ball(new Vector3(0f, -0.35f * t, -0.2f - 2.0f * t), Vector3.one * (0.85f - 0.62f * t), _fur);
                if (i > 0 && i < 6) Cone(new Vector3(0f, -0.35f * t + 0.35f - 0.25f * t, -0.2f - 2.0f * t), Vector3.one * (0.26f - 0.12f * t), Vector3.zero, spikes);
            }
        }

        private static void Whiskers(int side, Color color, float length)
        {
            // The roots bunch together on the muzzle beside the nose; the tips fan out (up, level, down) and away.
            float whiskerLength = 2f * length + 0.4f;
            for (int k = -1; k <= 1; k++)
            {
                Vector3 root = new Vector3(0.2f * side, -0.2f + 0.02f * k, 1.04f);
                float a = 0.38f * k;
                Vector3 dir = new Vector3(side * Mathf.Cos(a), Mathf.Sin(a), 0.1f).normalized;
                // A Unity cylinder is 2 tall, so scale.y is half the length; it is centred on its position.
                Part(PrimitiveType.Cylinder, root + dir * (whiskerLength * 0.5f),
                     new Vector3(0.03f, whiskerLength * 0.5f, 0.03f),
                     Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, color);
            }
        }

        // ---- Building blocks ----

        private static Transform Root(Worn worn, string name, Transform bone, Vector3 position, Quaternion rotation, float unit)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * unit;
            go.transform.SetParent(bone, true);
            worn.Roots.Add(go);
            return go.transform;
        }

        /// <summary>A pivot whose children swing gently from side to side (tails, floppy ears).</summary>
        private static Transform Holder(Transform parent, string name, Vector3 at, float degrees, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var sway = go.AddComponent<Sway>();
            sway.Degrees = degrees;
            sway.Speed = speed;
            sway.AroundForward = name == "ear";
            return go.transform;
        }

        private static void Ball(Vector3 at, Vector3 size, Color color, Vector3? euler = null)
        {
            Part(PrimitiveType.Sphere, at, size, euler ?? Vector3.zero, color);
        }

        private static GameObject Part(PrimitiveType shape, Vector3 at, Vector3 size, Vector3 euler, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            Collider c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c); // visual only: must never push the ragdoll
            return Place(go, at, size, euler, color);
        }

        private static void Cone(Vector3 at, Vector3 size, Vector3 euler, Color color)
        {
            var go = new GameObject("Cone");
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
            go.AddComponent<MeshRenderer>();
            Place(go, at, size, euler, color);
        }

        private static GameObject Place(GameObject go, Vector3 at, Vector3 size, Vector3 euler, Color color)
        {
            go.name = "Costume part";
            go.layer = _layer;
            go.transform.SetParent(_into, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return go;
        }

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        private static Material MaterialFor(Color color)
        {
            Material m;
            if (!Materials.TryGetValue(color, out m) || m == null)
            {
                m = TankMaterials.Solid(color);
                m.name = "Costume";
                Materials[color] = m;
            }
            return m;
        }

        /// <summary>A cone: base (radius 0.5) at y = 0, tip at y = 1. Both sides drawn, so winding can't hide it.</summary>
        private static Mesh ConeMesh()
        {
            if (_cone != null) return _cone;
            const int n = 16;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                var tip = new Vector3(0f, 1f, 0f);
                Vector3 side = new Vector3(Mathf.Cos((a0 + a1) / 2f), 0.5f, Mathf.Sin((a0 + a1) / 2f)).normalized;
                int b = verts.Count;
                verts.Add(p0); verts.Add(tip); verts.Add(p1);
                normals.Add(side); normals.Add(side); normals.Add(side);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(Vector3.zero);
                normals.Add(Vector3.down); normals.Add(Vector3.down); normals.Add(Vector3.down);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
            }
            _cone = new Mesh { name = "Costume cone" };
            _cone.SetVertices(verts);
            _cone.SetNormals(normals);
            _cone.SetTriangles(tris, 0);
            _cone.RecalculateBounds();
            return _cone;
        }

        private static Transform FindBone(Transform root, string path, string name)
        {
            Transform t = root.Find(path);
            if (t != null) return t;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }

        private static bool SetBodyColor(object customize, Color color)
        {
            // CharacterCustomize.SetCharacterColor(color, bool, float) (from research; the bool and float aren't named there).
            return GameReflection.CallWith(customize, "SetCharacterColor", color, false, 0f)
                || GameReflection.CallWith(customize, "SetCharacterColor", color);
        }

        private static void RemoveParts(Worn worn)
        {
            foreach (GameObject root in worn.Roots)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
            }
            worn.Roots.Clear();
        }
    }

    /// <summary>Swings gently from side to side (tails), or flops (ears).</summary>
    internal sealed class Sway : MonoBehaviour
    {
        public float Degrees = 10f;
        public float Speed = 2f;
        public bool AroundForward;
        private Quaternion _rest;
        private bool _started;

        private void Update()
        {
            if (!_started) { _rest = transform.localRotation; _started = true; }
            float a = Mathf.Sin(Time.time * Speed) * Degrees;
            transform.localRotation = _rest * (AroundForward ? Quaternion.Euler(0f, 0f, a) : Quaternion.Euler(0f, a, 0f));
        }
    }
}
