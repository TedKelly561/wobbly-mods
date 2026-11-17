using UnityEngine;
using WobblyMods.HouseMenu.Tanks;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Glowing decorations in Skylar's colors (orange and green): floor lamps, a night light and a string of
    /// party lights. Built from simple shapes plus real Unity lights, so they light up the room. Visual-only,
    /// except the lamp pole, which players bump into.
    /// </summary>
    internal static class Decor
    {
        private static readonly Color Orange = new Color(1f, 0.5f, 0f);
        private static readonly Color Green = new Color(0.25f, 0.85f, 0.25f);
        private static readonly Color Pole = new Color(0.25f, 0.25f, 0.28f);

        public static GameObject OrangeLamp(Vector3 position, Quaternion rotation)
        {
            return FloorLamp("Orange lamp", Orange, position, rotation);
        }

        public static GameObject GreenLamp(Vector3 position, Quaternion rotation)
        {
            return FloorLamp("Green lamp", Green, position, rotation);
        }

        /// <summary>A small soft glow, nice next to a bed.</summary>
        public static GameObject NightLight(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Night light");
            root.transform.SetPositionAndRotation(position, rotation);
            Part(root.transform, PrimitiveType.Cylinder, Pole, new Vector3(0, 0.05f, 0), new Vector3(0.25f, 0.05f, 0.25f), false);
            Glow(root.transform, Green, new Vector3(0, 0.22f, 0), 0.28f, 3f, 0.6f);
            return root;
        }

        /// <summary>A row of bulbs on a wire, alternating orange and green, between two little posts.</summary>
        public static GameObject PartyLights(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Party lights");
            root.transform.SetPositionAndRotation(position, rotation);
            const float width = 4f;
            const float height = 2.2f;
            Part(root.transform, PrimitiveType.Cylinder, Pole, new Vector3(-width / 2, height / 2, 0), new Vector3(0.08f, height / 2, 0.08f), true);
            Part(root.transform, PrimitiveType.Cylinder, Pole, new Vector3(width / 2, height / 2, 0), new Vector3(0.08f, height / 2, 0.08f), true);

            const int bulbs = 9;
            for (int i = 0; i < bulbs; i++)
            {
                float t = i / (float)(bulbs - 1);
                float x = Mathf.Lerp(-width / 2, width / 2, t);
                float sag = Mathf.Sin(t * Mathf.PI) * 0.35f; // the wire droops in the middle
                Color c = i % 2 == 0 ? Orange : Green;
                // Only two real lights (lights are costly); the rest just glow.
                bool realLight = i == 2 || i == 6;
                Glow(root.transform, c, new Vector3(x, height - sag, 0), 0.16f, realLight ? 4f : 0f, 0.8f);
            }
            return root;
        }

        private static GameObject FloorLamp(string name, Color color, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(position, rotation);
            Part(root.transform, PrimitiveType.Cylinder, Pole, new Vector3(0, 0.04f, 0), new Vector3(0.5f, 0.04f, 0.5f), true);
            Part(root.transform, PrimitiveType.Cylinder, Pole, new Vector3(0, 0.85f, 0), new Vector3(0.06f, 0.8f, 0.06f), true);
            Glow(root.transform, color, new Vector3(0, 1.75f, 0), 0.5f, 7f, 1.3f);
            return root;
        }

        /// <summary>A glowing ball, with a real point light if range &gt; 0.</summary>
        private static void Glow(Transform root, Color color, Vector3 pos, float size, float range, float intensity)
        {
            Transform ball = Part(root, PrimitiveType.Sphere, color, pos, Vector3.one * size, false);
            Material m = ball.GetComponent<Renderer>().material;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
            }
            if (range > 0f)
            {
                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.localPosition = pos;
                light.type = LightType.Point;
                light.color = color;
                light.range = range;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
            }
        }

        private static Transform Part(Transform parent, PrimitiveType type, Color color, Vector3 pos, Vector3 scale, bool keepCollider)
        {
            GameObject p = GameObject.CreatePrimitive(type);
            if (!keepCollider) Object.Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().material = TankMaterials.Solid(color);
            return p.transform;
        }
    }
}
