using UnityEngine;
using UnityEngine.Rendering;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Swims in a lazy loop around the tank, bobbing up and down, facing the way it's going.
    /// The fish models have no animation, so this moves the holder object. The model's long side is turned
    /// to point along the path (the game's fish may be modeled along X or Z; we check which is longer).
    /// </summary>
    internal sealed class Swim : MonoBehaviour
    {
        public Vector3 Center;
        public float RadiusX = 2f;
        public float RadiusZ = 1.5f;
        public float Speed = 0.3f;
        public float Angle;
        public float Bob = 0.1f;
        public Transform Model;

        private bool _aligned;

        private void Update()
        {
            if (!_aligned)
            {
                AlignModel();
                _aligned = true;
            }

            Angle += Speed * Time.deltaTime;
            float x = Mathf.Cos(Angle) * RadiusX;
            float z = Mathf.Sin(Angle) * RadiusZ;
            float y = Mathf.Sin(Angle * 3f) * Bob;
            transform.localPosition = Center + new Vector3(x, y, z);

            // Direction of travel along the loop.
            float s = Mathf.Sign(Speed);
            Vector3 dir = new Vector3(-Mathf.Sin(Angle) * RadiusX * s, 0f, Mathf.Cos(Angle) * RadiusZ * s);
            if (dir.sqrMagnitude > 0.0001f && transform.parent != null)
            {
                Vector3 worldDir = transform.parent.TransformDirection(dir);
                transform.rotation = Quaternion.LookRotation(worldDir, transform.parent.up);
            }
        }

        /// <summary>If the model is longer along X than Z, turn it 90 degrees so its length points forward.</summary>
        private void AlignModel()
        {
            if (Model == null) return;
            Mesh mesh = null;
            MeshFilter mf = Model.GetComponentInChildren<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            if (mesh == null) return;

            Vector3 size = mesh.bounds.size;
            if (size.x > size.z * 1.2f)
            {
                Model.localRotation = Quaternion.Euler(0, 90f, 0) * Model.localRotation;
            }
        }
    }

    /// <summary>Gently sways a plant back and forth, like it's in a slow current.</summary>
    internal sealed class Sway : MonoBehaviour
    {
        public float Amount = 9f;
        public float Speed = 0.8f;
        public float Phase;

        private Quaternion _rest;
        private bool _started;

        private void Update()
        {
            if (!_started)
            {
                _rest = transform.localRotation;
                _started = true;
            }
            float t = Time.time * Speed + Phase;
            float tiltX = Mathf.Sin(t) * Amount;
            float tiltZ = Mathf.Sin(t * 0.7f + 1.3f) * Amount * 0.5f;
            transform.localRotation = _rest * Quaternion.Euler(tiltX, 0f, tiltZ);
        }
    }

    /// <summary>Simple rising bubbles, used only if the game's own bubble effect can't load.</summary>
    internal sealed class Bubbles : MonoBehaviour
    {
        public float Top = 2.5f;
        public int Count = 14;

        private Transform[] _bubbles;
        private float[] _speeds;

        private void Start()
        {
            Material mat = TankMaterials.Bubble();
            _bubbles = new Transform[Count];
            _speeds = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(b.GetComponent<Collider>());
                b.name = "Bubble";
                b.transform.SetParent(transform, false);
                float size = Random.Range(0.05f, 0.14f);
                b.transform.localScale = Vector3.one * size;
                b.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0f, Top), Random.Range(-0.15f, 0.15f));
                Renderer r = b.GetComponent<Renderer>();
                r.material = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                _bubbles[i] = b.transform;
                _speeds[i] = Random.Range(0.4f, 0.8f);
            }
        }

        private void Update()
        {
            if (_bubbles == null) return;
            for (int i = 0; i < _bubbles.Length; i++)
            {
                Transform b = _bubbles[i];
                Vector3 p = b.localPosition;
                p.y += _speeds[i] * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * 3f + i) * 0.002f;
                if (p.y > Top)
                {
                    p = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
                }
                b.localPosition = p;
            }
        }
    }

    /// <summary>
    /// Materials for the tank. The game's render setup is unknown from outside, so each material tries
    /// several options and keeps the first that exists: the game's own glass, then Unity's Standard shader in
    /// transparent mode, then shaders that are always included.
    /// </summary>
    internal static class TankMaterials
    {
        private static Material _glass;
        private static Material _bubble;
        private static Material _solidTemplate;

        public static string GlassMaterialName { get; private set; }

        public static Material Glass()
        {
            if (_glass != null) return _glass;
            Color tint = new Color(0.7f, 0.9f, 1f, 0.22f);

            // 1. The game's own see-through glass material.
            foreach (Material m in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (m == null || m.shader == null) continue;
                if (m.name.ToLowerInvariant().Contains("glass") && m.renderQueue >= (int)RenderQueue.Transparent)
                {
                    _glass = new Material(m) { name = "Tank Glass (from " + m.name + ")" };
                    SetColor(_glass, tint);
                    GlassMaterialName = _glass.name;
                    return _glass;
                }
            }

            // 2. Standard shader, switched to transparent.
            Shader standard = Shader.Find("Standard");
            if (standard != null)
            {
                _glass = new Material(standard) { name = "Tank Glass (Standard)" };
                _glass.SetFloat("_Mode", 3f);
                _glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _glass.SetInt("_ZWrite", 0);
                _glass.DisableKeyword("_ALPHATEST_ON");
                _glass.EnableKeyword("_ALPHABLEND_ON");
                _glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                _glass.SetFloat("_Glossiness", 0.9f);
                _glass.renderQueue = (int)RenderQueue.Transparent;
                SetColor(_glass, tint);
                GlassMaterialName = _glass.name;
                return _glass;
            }

            // 3. Always-included see-through shader.
            _glass = Fallback(tint, "Tank Glass (fallback)");
            GlassMaterialName = _glass.name;
            return _glass;
        }

        public static Material Bubble()
        {
            if (_bubble != null) return _bubble;
            _bubble = new Material(Glass()) { name = "Tank Bubble" };
            SetColor(_bubble, new Color(0.9f, 0.97f, 1f, 0.45f));
            return _bubble;
        }

        /// <summary>A see-through colored material (water, ponds), based on the glass material.</summary>
        public static Material Tinted(Color color)
        {
            var m = new Material(Glass()) { name = "Tank Tinted" };
            SetColor(m, color);
            return m;
        }

        /// <summary>A solid colored material, copied from Unity's default material (works with any render setup).</summary>
        public static Material Solid(Color color)
        {
            if (_solidTemplate == null)
            {
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _solidTemplate = new Material(probe.GetComponent<Renderer>().sharedMaterial);
                Object.Destroy(probe);
            }
            var m = new Material(_solidTemplate) { name = "Tank Solid" };
            SetColor(m, color);
            return m;
        }

        private static Material Fallback(Color tint, string name)
        {
            Shader shader = Shader.Find("Legacy Shaders/Transparent/Diffuse") ?? Shader.Find("Sprites/Default");
            var m = shader != null ? new Material(shader) : new Material(Solid(tint));
            m.name = name;
            m.renderQueue = (int)RenderQueue.Transparent;
            SetColor(m, tint);
            return m;
        }

        /// <summary>Sets the color under both common property names (built-in and URP shaders).</summary>
        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
    }
}
