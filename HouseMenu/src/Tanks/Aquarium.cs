using System;
using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Skylar's aquarium (parent's design choices, 2026-11-25): ONE building the Wobblies walk around in.
    /// Seen from above, with the entrance toward the player (+Z):
    ///
    ///   back wall  [ jellyfish ] [====== open ocean window ======] [ touch pool ]
    ///              [   aisle   ] [ Amazon flooded forest + tunnel ] [   aisle    ]
    ///   front      [ coral reef along the left wall ]  lobby  [ gift shop ]   entrance
    ///
    /// Every exhibit has a picture sign (a little model of the animal on a plaque, since Skylar can't read yet).
    /// A big floor button beside the Amazon tank feeds the redtails. The gift shop has redtail plushes that are
    /// loose, so she can carry one home. Fish are look-only; walls, glass, floors and counters are solid.
    /// </summary>
    internal static class Aquarium
    {
        private const float HalfX = 22f, HalfZ = 20f, WallH = 8f, Thick = 0.4f;
        private const string FishPath = "Assets/Content/Game/Prefabs/Props/Fishing_Props/Fish/Static/";

        private static readonly Color WallColor = new Color(0.78f, 0.86f, 0.9f);
        private static readonly Color FloorColor = new Color(0.32f, 0.36f, 0.42f);
        private static readonly Color Trim = new Color(1f, 0.5f, 0.05f);       // orange, Skylar's color
        private static readonly Color Dark = new Color(0.08f, 0.08f, 0.14f);
        private static readonly Color SandColor = new Color(0.9f, 0.82f, 0.6f);

        public static GameObject Build(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Skylar's aquarium");
            root.transform.SetPositionAndRotation(position, rotation);
            Transform t = root.transform;
            var rng = new System.Random(2026);
            int parts = 0;
            // Each area is built on its own, so one that fails (say a fish model that won't load) can't stop the rest.
            parts += Safely("shell", () => Shell(t));
            var redtails = new List<Swim>();
            parts += Safely("Amazon", () =>
            {
                var amazon = new GameObject("Amazon flooded forest");
                amazon.transform.SetParent(t, false);
                AquariumTunnel.BuildInto(amazon.transform, true, redtails, true);
                Sign(t, new Vector3(4.6f, 0f, 9.2f), 180f, sign => { GameObject f = ShapeModels.Redtail(); f.transform.SetParent(sign, false); f.transform.localScale = Vector3.one * 1.1f; f.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); });
            });
            parts += Safely("feeding button", () => Feeding(t, redtails));
            parts += Safely("coral reef", () => CoralReef(t, rng));
            parts += Safely("jellyfish", () => Jellyfish(t, rng));
            parts += Safely("open ocean", () => OpenOcean(t, rng));
            parts += Safely("touch pool", () => TouchPool(t, rng));
            parts += Safely("gift shop", () => GiftShop(t));
            Plugin.Log.LogInfo("Aquarium: built " + parts + " of 8 areas, " + redtails.Count + " redtails, " + t.GetComponentsInChildren<Renderer>().Length + " things to draw.");
            return root;
        }

        internal static int Safely(string what, Action build)
        {
            try
            {
                build();
                return 1;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Aquarium: the " + what + " failed: " + e);
                return 0;
            }
        }

        // ---------- The building ----------

        private static void Shell(Transform t)
        {
            Material wall = TankMaterials.Solid(WallColor);
            Material floor = TankMaterials.Solid(FloorColor);
            Material trim = TankMaterials.Solid(Trim);

            FishTank.Box(t, "floor", new Vector3(0f, -0.05f, 0f), new Vector3(HalfX * 2f, 0.3f, HalfZ * 2f), floor, true);
            FishTank.Box(t, "left wall", new Vector3(-HalfX, WallH / 2f, 0f), new Vector3(Thick, WallH, HalfZ * 2f), wall, true);
            FishTank.Box(t, "right wall", new Vector3(HalfX, WallH / 2f, 0f), new Vector3(Thick, WallH, HalfZ * 2f), wall, true);
            FishTank.Box(t, "back wall", new Vector3(0f, WallH / 2f, -HalfZ), new Vector3(HalfX * 2f, WallH, Thick), wall, true);
            // Front wall with a wide doorway in the middle.
            const float door = 4f, doorH = 5f;
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "front wall", new Vector3(s * (HalfX + door) / 2f, WallH / 2f, HalfZ), new Vector3(HalfX - door, WallH, Thick), wall, true);
                FishTank.Box(t, "door post", new Vector3(s * door, doorH / 2f, HalfZ + 0.1f), new Vector3(0.5f, doorH, 0.7f), trim, true);
            }
            FishTank.Box(t, "over the door", new Vector3(0f, (WallH + doorH) / 2f, HalfZ), new Vector3(door * 2f, WallH - doorH, Thick), wall, true);
            FishTank.Box(t, "door top", new Vector3(0f, doorH + 0.25f, HalfZ + 0.1f), new Vector3(door * 2f + 0.5f, 0.5f, 0.7f), trim, true);
            // The sign over the door: a big redtail, so she knows what's inside.
            GameObject mascot = ShapeModels.Redtail();
            mascot.transform.SetParent(t, false);
            mascot.transform.localPosition = new Vector3(0f, doorH + 1.6f, HalfZ + 0.6f);
            mascot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            mascot.transform.localScale = Vector3.one * 5f;

            // Roof, open above the Amazon tank so daylight falls into the water.
            float tankX = AquariumTunnel.TankWidth / 2f, tankZ = AquariumTunnel.TankLength / 2f;
            Material roof = TankMaterials.Solid(new Color(0.55f, 0.62f, 0.68f));
            float sideX = HalfX - tankX, endZ = HalfZ - tankZ;
            FishTank.Box(t, "roof", new Vector3(-(tankX + sideX / 2f), WallH + 0.15f, 0f), new Vector3(sideX, 0.3f, HalfZ * 2f), roof, true);
            FishTank.Box(t, "roof", new Vector3(tankX + sideX / 2f, WallH + 0.15f, 0f), new Vector3(sideX, 0.3f, HalfZ * 2f), roof, true);
            FishTank.Box(t, "roof", new Vector3(0f, WallH + 0.15f, tankZ + endZ / 2f), new Vector3(tankX * 2f, 0.3f, endZ), roof, true);
            FishTank.Box(t, "roof", new Vector3(0f, WallH + 0.15f, -(tankZ + endZ / 2f)), new Vector3(tankX * 2f, 0.3f, endZ), roof, true);

            // Lights, so it isn't gloomy under the roof.
            Lamp(t, new Vector3(0f, 6.5f, 14f), Color.white, 22f, 1.3f);
            Lamp(t, new Vector3(-14f, 6.5f, 2f), new Color(0.8f, 0.95f, 1f), 20f, 1.1f);
            Lamp(t, new Vector3(14f, 6.5f, 2f), new Color(0.8f, 0.95f, 1f), 20f, 1.1f);
            Lamp(t, new Vector3(0f, 6.5f, -11f), new Color(0.7f, 0.85f, 1f), 20f, 1.1f);
            // A front desk in the lobby.
            FishTank.Box(t, "front desk", new Vector3(-6f, 0.6f, 15f), new Vector3(4f, 1.0f, 1.2f), trim, true);
        }

        internal static void Lamp(Transform t, Vector3 at, Color color, float range, float intensity)
        {
            var go = new GameObject("lamp");
            go.transform.SetParent(t, false);
            go.transform.localPosition = at;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // ---------- Exhibits ----------

        /// <summary>A long reef tank along the left wall: bright corals and rocks with the game's reef fish.</summary>
        private static void CoralReef(Transform t, System.Random rng)
        {
            const float x0 = -HalfX + 0.2f, depth = 4f, z0 = 1f, z1 = 19f, h = 5f, lift = 0.7f;
            float cx = x0 + depth / 2f, cz = (z0 + z1) / 2f, len = z1 - z0;
            Material glass = TankMaterials.Tinted(new Color(0.3f, 0.75f, 1f, 0.16f));
            FishTank.Box(t, "reef base", new Vector3(cx, lift / 2f, cz), new Vector3(depth, lift, len), TankMaterials.Solid(Trim), true);
            FishTank.Box(t, "reef glass", new Vector3(x0 + depth, lift + h / 2f, cz), new Vector3(0.1f, h, len), glass, true);
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "reef end", new Vector3(cx, lift + h / 2f, cz + s * len / 2f), new Vector3(depth, h, 0.3f), TankMaterials.Solid(WallColor), true);
            }
            FishTank.Box(t, "reef sand", new Vector3(cx, lift + 0.1f, cz), new Vector3(depth - 0.2f, 0.2f, len - 0.4f), TankMaterials.Solid(SandColor), false);
            Lamp(t, new Vector3(cx + 0.5f, lift + h - 0.5f, cz), new Color(0.6f, 0.9f, 1f), 12f, 1.6f);

            Color[] coral = { new Color(1f, 0.35f, 0.45f), new Color(1f, 0.6f, 0.1f), new Color(0.75f, 0.35f, 0.9f), new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.85f, 0.7f), new Color(1f, 0.5f, 0.75f) };
            for (int i = 0; i < 34; i++)
            {
                var at = new Vector3(x0 + 0.5f + (float)rng.NextDouble() * (depth - 1f), lift + 0.2f, z0 + 0.8f + (float)rng.NextDouble() * (len - 1.6f));
                Material m = TankMaterials.Solid(coral[rng.Next(coral.Length)]);
                int kind = rng.Next(3);
                if (kind == 0)
                {
                    // Brain coral: a squashed ball.
                    float r = 0.4f + (float)rng.NextDouble() * 0.6f;
                    Shape(t, PrimitiveType.Sphere, at + Vector3.up * r * 0.3f, new Vector3(r * 1.3f, r * 0.8f, r * 1.3f), Quaternion.identity, m);
                }
                else if (kind == 1)
                {
                    // Branching coral: a few fingers.
                    for (int b = 0; b < 5; b++)
                    {
                        float tall = 0.6f + (float)rng.NextDouble() * 1.2f;
                        Quaternion lean = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 50f, 0f, ((float)rng.NextDouble() - 0.5f) * 50f);
                        Shape(t, PrimitiveType.Capsule, at + lean * new Vector3(0f, tall / 2f, 0f), new Vector3(0.16f, tall / 2f, 0.16f), lean, m);
                    }
                }
                else
                {
                    // Tube sponge: an upright cylinder.
                    float tall = 0.5f + (float)rng.NextDouble() * 1.0f;
                    Shape(t, PrimitiveType.Cylinder, at + Vector3.up * tall / 2f, new Vector3(0.35f, tall / 2f, 0.35f), Quaternion.identity, m);
                }
            }
            string[] fish = { "ClownFish", "ClownFish", "ClownFish", "Angel Fish", "Angel Fish", "LionFish", "Parrot Fish", "Parrot Fish", "Blue Dragon", "Pufferfish", "SeaHorse_Green", "SeaHorse_Yellow" };
            foreach (string name in fish)
            {
                GameFish(t, name, 0.6f + (float)rng.NextDouble() * 0.5f,
                    new Vector3(cx, lift + 1.2f + (float)rng.NextDouble() * (h - 2f), cz + ((float)rng.NextDouble() - 0.5f) * 6f),
                    depth / 2f - 0.8f, 3f + (float)rng.NextDouble() * 4.5f, (rng.Next(2) == 0 ? 1f : -1f) * (0.3f + (float)rng.NextDouble() * 0.3f), 0.15f, rng);
            }
            Sign(t, new Vector3(x0 + depth + 1.2f, 0f, z0 - 0.2f), 90f, sign => MiniFish(sign, "ClownFish", 0.9f));
        }

        /// <summary>A dark corner with tall glowing tanks of drifting jellyfish.</summary>
        private static void Jellyfish(Transform t, System.Random rng)
        {
            // Dark floor and wall panels, so the glow shows.
            Material dark = TankMaterials.Solid(Dark);
            FishTank.Box(t, "dark floor", new Vector3(-18f, 0.12f, -13.5f), new Vector3(7.6f, 0.06f, 12.6f), dark, false);
            FishTank.Box(t, "dark wall", new Vector3(-HalfX + 0.3f, WallH / 2f, -13.5f), new Vector3(0.2f, WallH, 12.6f), dark, false);
            FishTank.Box(t, "dark wall", new Vector3(-18f, WallH / 2f, -HalfZ + 0.3f), new Vector3(7.6f, WallH, 0.2f), dark, false);
            Color[] glow = { new Color(0.7f, 0.3f, 1f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.3f, 0.7f) };
            float[] zs = { -9.5f, -13.5f, -17.5f };
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3(-19.2f, 0f, zs[i]);
                Material glass = TankMaterials.Tinted(new Color(glow[i].r, glow[i].g, glow[i].b, 0.16f));
                GameObject tube = Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 3.2f, new Vector3(2.4f, 2.6f, 2.4f), Quaternion.identity, glass);
                tube.AddComponent<CapsuleCollider>(); // solid, so nobody walks through the tank
                FishTank.Box(t, "jelly base", at + Vector3.up * 0.3f, new Vector3(2.7f, 0.6f, 2.7f), dark, true);
                Lamp(t, at + Vector3.up * 3.2f, glow[i], 7f, 2.2f);
                for (int j = 0; j < 4; j++)
                {
                    // Drifting slowly up and down in a tiny circle.
                    GameFish(t, "JellyFish", 0.7f + (float)rng.NextDouble() * 0.4f, at + Vector3.up * (1.6f + j * 0.9f),
                             0.5f, 0.5f, (j % 2 == 0 ? 1f : -1f) * (0.25f + (float)rng.NextDouble() * 0.2f), 0.45f, rng);
                }
            }
            Sign(t, new Vector3(-15.6f, 0f, -8.2f), 90f, sign => MiniFish(sign, "JellyFish", 0.9f));
        }

        /// <summary>A huge window in the back wall with the game's big ocean fish.</summary>
        private static void OpenOcean(Transform t, System.Random rng)
        {
            const float halfW = 13f, depth = 5.6f, h = 6.5f, lift = 0.6f;
            float zGlass = -HalfZ + 0.2f + depth, cz = -HalfZ + 0.2f + depth / 2f;
            Material glass = TankMaterials.Tinted(new Color(0.1f, 0.4f, 0.85f, 0.2f));
            FishTank.Box(t, "ocean base", new Vector3(0f, lift / 2f, cz), new Vector3(halfW * 2f, lift, depth), TankMaterials.Solid(Trim), true);
            FishTank.Box(t, "ocean glass", new Vector3(0f, lift + h / 2f, zGlass), new Vector3(halfW * 2f, h, 0.12f), glass, true);
            FishTank.Box(t, "ocean back", new Vector3(0f, lift + h / 2f, -HalfZ + 0.35f), new Vector3(halfW * 2f, h, 0.1f), TankMaterials.Solid(new Color(0.05f, 0.2f, 0.45f)), false);
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "ocean end", new Vector3(s * halfW, lift + h / 2f, cz), new Vector3(0.3f, h, depth), TankMaterials.Solid(WallColor), true);
            }
            Lamp(t, new Vector3(0f, lift + h - 0.6f, cz), new Color(0.5f, 0.75f, 1f), 18f, 1.8f);
            // A few rocks on the bottom.
            Material rock = TankMaterials.Solid(new Color(0.35f, 0.37f, 0.42f));
            for (int i = 0; i < 8; i++)
            {
                float r = 0.6f + (float)rng.NextDouble() * 1.1f;
                Shape(t, PrimitiveType.Sphere, new Vector3(((float)rng.NextDouble() - 0.5f) * (halfW * 2f - 3f), lift + r * 0.25f, cz + ((float)rng.NextDouble() - 0.5f) * (depth - 2f)),
                      new Vector3(r * 1.4f, r * 0.7f, r), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), rock);
            }
            string[] big = { "Marlin", "Tuna", "Tuna", "Saw Fish", "SunFish", "StingRay", "StingRay", "Grouper", "Salmon", "Cod" };
            float[] size = { 3.6f, 2.4f, 2.1f, 3.2f, 2.6f, 2.2f, 1.8f, 1.8f, 1.3f, 1.3f };
            for (int i = 0; i < big.Length; i++)
            {
                GameFish(t, big[i], size[i], new Vector3(((float)rng.NextDouble() - 0.5f) * 6f, lift + 1.3f + (float)rng.NextDouble() * (h - 2.6f), cz),
                         halfW - 3f - (float)rng.NextDouble() * 4f, depth / 2f - 1.4f, (i % 2 == 0 ? 1f : -1f) * (0.14f + (float)rng.NextDouble() * 0.14f), 0.3f, rng);
            }
            for (int i = 0; i < 10; i++)
            {
                GameFish(t, "Sardine", 0.5f, new Vector3(-5f + (i % 5) * 0.5f, lift + 3.5f + (i / 5) * 0.4f, cz), 6.5f + (i % 5) * 0.15f, 1.3f, 0.4f, 0.12f, rng);
            }
            Sign(t, new Vector3(9.5f, 0f, zGlass + 1.4f), 0f, sign => MiniFish(sign, "Marlin", 1.2f));
        }

        /// <summary>A low open pool to lean over, with starfish, crabs, urchins and a lobster on the sand.</summary>
        private static void TouchPool(Transform t, System.Random rng)
        {
            var c = new Vector3(18f, 0f, -13.5f);
            const float w = 5.5f, d = 8f, rim = 0.75f;
            Material stone = TankMaterials.Solid(new Color(0.6f, 0.62f, 0.66f));
            for (int s = -1; s <= 1; s += 2)
            {
                FishTank.Box(t, "pool rim", c + new Vector3(s * w / 2f, rim / 2f, 0f), new Vector3(0.4f, rim, d + 0.4f), stone, true);
                FishTank.Box(t, "pool rim", c + new Vector3(0f, rim / 2f, s * d / 2f), new Vector3(w - 0.4f, rim, 0.4f), stone, true);
            }
            FishTank.Box(t, "pool sand", c + new Vector3(0f, 0.2f, 0f), new Vector3(w - 0.4f, 0.2f, d - 0.4f), TankMaterials.Solid(SandColor), true);
            FishTank.Box(t, "pool water", c + new Vector3(0f, rim - 0.12f, 0f), new Vector3(w - 0.4f, 0.04f, d - 0.4f), TankMaterials.Tinted(new Color(0.4f, 0.85f, 0.95f, 0.18f)), false);
            string[] crawlers = { "Starfish", "Starfish", "Starfish", "Giant Crab", "Spider Crab", "SeaUrchin", "SeaUrchin", "Lobster", "Cockle", "Oyster", "Sea Cucumber", "Prawn" };
            foreach (string name in crawlers)
            {
                var at = c + new Vector3(((float)rng.NextDouble() - 0.5f) * (w - 1.4f), 0.32f, ((float)rng.NextDouble() - 0.5f) * (d - 1.4f));
                bool walks = name.Contains("Crab") || name == "Lobster" || name == "Prawn";
                // Crabs and the lobster wander slowly; the rest sit still.
                GameFish(t, name, 0.5f + (float)rng.NextDouble() * 0.3f, at, walks ? 0.6f : 0f, walks ? 0.9f : 0f, walks ? 0.15f : 0f, 0f, rng);
            }
            Lamp(t, c + Vector3.up * 3.5f, new Color(1f, 0.95f, 0.8f), 9f, 1.4f);
            Sign(t, c + new Vector3(-w / 2f - 1.3f, 0f, d / 2f - 0.5f), 90f, sign => MiniFish(sign, "Starfish", 0.9f));
        }

        /// <summary>Shelves and a table of redtail plushes. The ones on the table are loose: take one home.</summary>
        private static void GiftShop(Transform t)
        {
            var c = new Vector3(16.5f, 0f, 14f);
            Material wood = TankMaterials.Solid(new Color(0.75f, 0.55f, 0.35f));
            Material green = TankMaterials.Solid(new Color(0.25f, 0.67f, 0.24f));
            FishTank.Box(t, "shop rug", c + new Vector3(0f, 0.11f, 0f), new Vector3(9f, 0.04f, 9f), green, false);
            FishTank.Box(t, "shop counter", c + new Vector3(-3f, 0.55f, 3f), new Vector3(3f, 1.1f, 1.1f), TankMaterials.Solid(Trim), true);
            FishTank.Box(t, "shop table", c + new Vector3(0f, 0.45f, -1f), new Vector3(4f, 0.9f, 2.2f), wood, true);
            // Shelves on the right wall, with plushes that stay put (decoration).
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 1.0f + shelf * 1.3f;
                FishTank.Box(t, "shelf", new Vector3(HalfX - 0.8f, y, c.z), new Vector3(1.0f, 0.12f, 8f), wood, true);
                for (int i = 0; i < 6; i++)
                {
                    GameObject plush = ShapeModels.Redtail();
                    plush.transform.SetParent(t, false);
                    plush.transform.localPosition = new Vector3(HalfX - 0.85f, y + 0.24f, c.z - 3.2f + i * 1.3f);
                    plush.transform.localRotation = Quaternion.Euler(0f, -90f + (i % 2 == 0 ? 15f : -15f), 0f);
                    plush.transform.localScale = Vector3.one * (0.7f + 0.15f * ((i + shelf) % 3));
                }
            }
            // Loose plushes on the table: grab one, carry it, take it home.
            for (int i = 0; i < 8; i++)
            {
                GameObject plush = ShapeModels.Redtail();
                plush.name = "Redtail plush";
                plush.transform.SetParent(t, false);
                plush.transform.localPosition = c + new Vector3(-1.5f + (i % 4) * 1.0f, 1.15f, -1.5f + (i / 4) * 1.0f);
                plush.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
                plush.transform.localScale = Vector3.one * 0.8f;
                var capsule = plush.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.16f;
                capsule.height = 0.95f;
                var body = plush.AddComponent<Rigidbody>();
                body.mass = 0.6f;
                body.angularDrag = 1.5f;
            }
            Sign(t, c + new Vector3(-4.2f, 0f, -3.5f), 180f, sign => { GameObject f = ShapeModels.Redtail(); f.transform.SetParent(sign, false); f.transform.localScale = Vector3.one * 0.9f; f.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); });
        }

        /// <summary>A big orange floor button beside the Amazon tank: step on it and the redtails come to eat.</summary>
        private static void Feeding(Transform t, List<Swim> redtails)
        {
            float glassX = AquariumTunnel.TankWidth / 2f;
            var at = new Vector3(glassX + 1.6f, 0.1f, 2f);
            GameObject button = Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 0.06f, new Vector3(1.6f, 0.07f, 1.6f), Quaternion.identity, TankMaterials.Solid(Trim));
            Shape(t, PrimitiveType.Cylinder, at + Vector3.up * 0.02f, new Vector3(2.0f, 0.03f, 2.0f), Quaternion.identity, TankMaterials.Solid(new Color(0.25f, 0.67f, 0.24f)));
            var zone = button.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 8f, 0f);
            zone.size = new Vector3(1f, 16f, 1f); // in the button's own (squashed) scale: about 1.1 m tall
            Feeder feeder = button.AddComponent<Feeder>();
            feeder.Fish = redtails;
            feeder.Tank = t;
            feeder.FeedPoint = new Vector3(glassX - 1.4f, 3.2f, 2f); // just inside the glass, at eye level
            // A fish-food picture sign: a jar of pellets (a little tub with brown balls) next to a redtail.
            Sign(t, at + new Vector3(1.6f, 0f, 0f), -90f, sign =>
            {
                Shape(sign, PrimitiveType.Cylinder, new Vector3(-0.25f, 0f, 0.05f), new Vector3(0.3f, 0.2f, 0.3f), Quaternion.identity, TankMaterials.Solid(Trim));
                for (int i = 0; i < 5; i++) Shape(sign, PrimitiveType.Sphere, new Vector3(0.1f + (i % 3) * 0.14f, 0.15f - (i / 3) * 0.16f, 0.05f), Vector3.one * 0.1f, Quaternion.identity, TankMaterials.Solid(new Color(0.45f, 0.3f, 0.15f)));
            });
        }

        // ---------- Building blocks ----------

        /// <summary>A look-only shape (no collider) under a parent.</summary>
        internal static GameObject Shape(Transform parent, PrimitiveType kind, Vector3 at, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(kind);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// One of the game's own fish, swimming a lazy loop around center (radius 0 = it sits still). Returns null
        /// (and logs) if that fish model can't be loaded, so a missing fish never breaks an exhibit.
        /// </summary>
        internal static GameObject GameFish(Transform t, string name, float size, Vector3 center, float radiusX, float radiusZ, float speed, float bob, System.Random rng)
        {
            GameObject fish = FishTank.Place(t, FishPath + name + "_Static.prefab", size, center, rng);
            if (fish == null) return null;
            if (radiusX <= 0f && radiusZ <= 0f) return fish;
            Swim swim = fish.AddComponent<Swim>();
            swim.Center = center;
            swim.RadiusX = radiusX;
            swim.RadiusZ = radiusZ;
            swim.Speed = speed;
            swim.Angle = (float)rng.NextDouble() * 6.28f;
            swim.Bob = bob;
            swim.Model = fish.transform.childCount > 0 ? fish.transform.GetChild(0) : null;
            swim.Flip = name == "Pufferfish";
            return fish;
        }

        /// <summary>
        /// A picture sign for someone who can't read yet: a post with an orange plaque, and whatever fill adds in
        /// front of it (a small model of the animal). yaw = which way the plaque faces.
        /// </summary>
        internal static void Sign(Transform t, Vector3 at, float yaw, Action<Transform> fill)
        {
            var sign = new GameObject("picture sign");
            sign.transform.SetParent(t, false);
            sign.transform.localPosition = at;
            sign.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            FishTank.Box(sign.transform, "post", new Vector3(0f, 0.6f, 0f), new Vector3(0.14f, 1.2f, 0.14f), TankMaterials.Solid(new Color(0.2f, 0.2f, 0.24f)), true);
            FishTank.Box(sign.transform, "plaque", new Vector3(0f, 1.55f, 0f), new Vector3(1.5f, 0.9f, 0.1f), TankMaterials.Solid(Trim), false);
            FishTank.Box(sign.transform, "plaque face", new Vector3(0f, 1.55f, 0.06f), new Vector3(1.34f, 0.74f, 0.04f), TankMaterials.Solid(new Color(0.85f, 0.95f, 1f)), false);
            var picture = new GameObject("picture");
            picture.transform.SetParent(sign.transform, false);
            picture.transform.localPosition = new Vector3(0f, 1.55f, 0.18f);
            try { fill(picture.transform); }
            catch (Exception e) { Plugin.Log.LogWarning("Aquarium: a sign's picture failed: " + e.Message); }
        }

        /// <summary>
        /// A small model of one of the game's fish on a sign: long side across the plaque, shrunk to fit on it,
        /// centered, with its back just touching the plaque. Everything is measured in the sign's OWN space from
        /// the model's meshes; the earlier versions measured in world space, which goes wrong whenever the building
        /// isn't lined up with the world (test 26: "their placement still needs work").
        /// </summary>
        internal static void MiniFish(Transform sign, string name, float size)
        {
            GameObject fish = FishTank.Place(sign, FishPath + name + "_Static.prefab", size, Vector3.zero, new System.Random(1));
            if (fish == null) return;
            Transform f = fish.transform;
            f.localPosition = Vector3.zero;
            f.localRotation = Quaternion.identity;
            Bounds b;
            if (!SignBounds(sign, fish, out b)) return;
            if (b.size.z > b.size.x * 1.1f)
            {
                f.localRotation = Quaternion.Euler(0f, 90f, 0f);
                if (!SignBounds(sign, fish, out b)) return;
            }
            // Fit the plaque's face (about 1.3 wide and 0.7 tall).
            float fit = Mathf.Min(1f, Mathf.Min(1.2f / Mathf.Max(0.01f, b.size.x), 0.62f / Mathf.Max(0.01f, b.size.y)));
            if (fit < 0.999f)
            {
                f.localScale *= fit;
                if (!SignBounds(sign, fish, out b)) return;
            }
            // The sign's origin is 0.18 in front of the plaque: center it there, back of the animal 0.02 off the plaque.
            f.localPosition -= new Vector3(b.center.x, b.center.y, b.min.z + 0.1f);
        }

        /// <summary>The box round an object's meshes, measured in the sign's own space.</summary>
        private static bool SignBounds(Transform sign, GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
                Mesh mesh = mf != null ? mf.sharedMesh : (skinned != null ? skinned.sharedMesh : null);
                if (mesh == null) continue;
                Bounds m = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = sign.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; } else bounds.Encapsulate(local);
                }
            }
            return any;
        }
    }

    /// <summary>
    /// The feeding button: when a Wobbly (anything with a physics body) steps on it, food drops into the tank and
    /// the redtails leave their loops to circle the food for a while, then go back to swimming.
    /// </summary>
    internal sealed class Feeder : MonoBehaviour
    {
        public List<Swim> Fish;
        public Transform Tank;
        public Vector3 FeedPoint;

        private const float FeedSeconds = 14f;
        private float _until = -1f;
        private Vector3[] _centers;
        private float[] _rx, _rz;
        private readonly List<GameObject> _food = new List<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger || other.attachedRigidbody == null || _until > 0f || Fish == null || Fish.Count == 0) return;
            _centers = new Vector3[Fish.Count];
            _rx = new float[Fish.Count];
            _rz = new float[Fish.Count];
            for (int i = 0; i < Fish.Count; i++)
            {
                if (Fish[i] == null) continue;
                _centers[i] = Fish[i].Center;
                _rx[i] = Fish[i].RadiusX;
                _rz[i] = Fish[i].RadiusZ;
            }
            Material pellet = TankMaterials.Solid(new Color(0.45f, 0.3f, 0.15f));
            for (int i = 0; i < 14; i++)
            {
                GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(bit.GetComponent<Collider>());
                bit.name = "fish food";
                bit.transform.SetParent(Tank, false);
                bit.transform.localPosition = FeedPoint + new Vector3(Mathf.Cos(i * 2.4f) * 0.5f, 3f + (i % 4) * 0.25f, Mathf.Sin(i * 2.4f) * 0.9f);
                bit.transform.localScale = Vector3.one * 0.14f;
                bit.GetComponent<Renderer>().sharedMaterial = pellet;
                _food.Add(bit);
            }
            _until = Time.time + FeedSeconds;
            FmodSounds.TryPlay(MenuSound.Pop);
            Plugin.Log.LogInfo("Aquarium: feeding time (" + Fish.Count + " redtails).");
        }

        private void Update()
        {
            if (_until <= 0f) return;
            float left = _until - Time.time;
            bool eating = left > 2.5f;
            for (int i = 0; i < Fish.Count; i++)
            {
                if (Fish[i] == null) continue;
                // Glide toward a tight circle round the food, then back out to the usual loop.
                Vector3 wantCenter = eating ? FeedPoint + new Vector3(0f, (i % 3 - 1) * 0.6f, 0f) : _centers[i];
                float wantX = eating ? 0.5f + (i % 4) * 0.2f : _rx[i], wantZ = eating ? 1.2f + (i % 3) * 0.5f : _rz[i];
                float k = 1f - Mathf.Exp(-1.2f * Time.deltaTime);
                Fish[i].Center = Vector3.Lerp(Fish[i].Center, wantCenter, k);
                Fish[i].RadiusX = Mathf.Lerp(Fish[i].RadiusX, wantX, k);
                Fish[i].RadiusZ = Mathf.Lerp(Fish[i].RadiusZ, wantZ, k);
            }
            // The food sinks slowly and is eaten (shrinks away) as the fish arrive.
            foreach (GameObject bit in _food)
            {
                if (bit == null) continue;
                Vector3 p = bit.transform.localPosition;
                if (p.y > FeedPoint.y - 0.5f) bit.transform.localPosition = p + Vector3.down * 0.5f * Time.deltaTime;
                if (left < FeedSeconds - 5f) bit.transform.localScale *= 1f - 0.7f * Time.deltaTime;
            }
            if (left > 0f) return;
            for (int i = 0; i < Fish.Count; i++)
            {
                if (Fish[i] == null) continue;
                Fish[i].Center = _centers[i];
                Fish[i].RadiusX = _rx[i];
                Fish[i].RadiusZ = _rz[i];
            }
            foreach (GameObject bit in _food) if (bit != null) Destroy(bit);
            _food.Clear();
            _until = -1f;
        }
    }
}
