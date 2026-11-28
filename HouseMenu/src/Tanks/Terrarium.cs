using UnityEngine;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Frog, lizard and turtle tanks for Skylar (task T24): a walk-around glass terrarium (5 x 3 x 2.5 m)
    /// with an orange rim and green base, a floor to match the animal, the game's own plants, rocks, and our
    /// little shape-built animals (Critters.cs) that hop, scurry or plod around.
    /// </summary>
    internal static class Terrarium
    {
        private const float Width = 5f;
        private const float Depth = 3f;
        private const float Height = 2.5f;
        private const float Glass = 0.06f;
        private const float FloorTop = 0.45f;
        private const string Plants = "Assets/Content/Game/Prefabs/Environment/Vegitation/";

        private static readonly Color Soil = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color Sand = new Color(0.93f, 0.8f, 0.55f);
        private static readonly Color Water = new Color(0.25f, 0.6f, 0.85f, 0.6f);
        private static readonly Color LilyPad = new Color(0.3f, 0.65f, 0.25f);
        private static readonly Color Grey = new Color(0.55f, 0.55f, 0.55f);

        public static GameObject BuildFrogTank(Vector3 position, Quaternion rotation)
        {
            var rng = new System.Random();
            Transform root = Shell("Skylar Frog Tank", position, rotation, Soil);

            // A pond with lily pads.
            GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pond.name = "Pond";
            Object.Destroy(pond.GetComponent<Collider>());
            pond.transform.SetParent(root, false);
            pond.transform.localPosition = new Vector3(Width * 0.18f, FloorTop + 0.01f, 0);
            pond.transform.localScale = new Vector3(2f, 0.02f, 1.5f);
            pond.GetComponent<Renderer>().sharedMaterial = TankMaterials.SharedTinted(Water);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(Width * 0.18f + Rand(rng, -0.6f, 0.6f), FloorTop + 0.04f, Rand(rng, -0.45f, 0.45f));
                Flat(root, "Lily pad", p, 0.35f, LilyPad);
            }

            AddPlants(root, rng, 9);
            AddRocks(root, rng, 5);

            Color[] skins = { Critters.Green, Critters.Orange, Critters.Lime, Critters.Green };
            for (int i = 0; i < skins.Length; i++)
            {
                GameObject frog = Critters.Frog(skins[i], 0.35f + (float)rng.NextDouble() * 0.1f);
                frog.transform.SetParent(root, false);
                frog.transform.localPosition = new Vector3(Rand(rng, -1.8f, 1.8f), FloorTop, Rand(rng, -1f, 1f));
                Hopper hop = frog.AddComponent<Hopper>();
                hop.HalfWidth = Width / 2 - 0.4f;
                hop.HalfDepth = Depth / 2 - 0.4f;
                hop.Floor = FloorTop;
            }
            return root.gameObject;
        }

        public static GameObject BuildLizardTank(Vector3 position, Quaternion rotation)
        {
            var rng = new System.Random();
            Transform root = Shell("Skylar Lizard Tank", position, rotation, Sand);

            // A warm basking rock under an orange heat lamp.
            GameObject bask = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bask.name = "Basking rock";
            Object.Destroy(bask.GetComponent<Collider>());
            bask.transform.SetParent(root, false);
            bask.transform.localPosition = new Vector3(-Width * 0.25f, FloorTop + 0.1f, 0);
            bask.transform.localScale = new Vector3(1.3f, 0.5f, 0.9f);
            bask.GetComponent<Renderer>().sharedMaterial = TankMaterials.Shared(new Color(0.75f, 0.45f, 0.25f));
            FishTank.Box(root, "Heat lamp", new Vector3(-Width * 0.25f, Height - 0.15f, 0), new Vector3(0.5f, 0.15f, 0.5f),
                         TankMaterials.Shared(Critters.Orange), false);
            var lampLight = new GameObject("Heat lamp light").AddComponent<Light>();
            lampLight.transform.SetParent(root, false);
            lampLight.transform.localPosition = new Vector3(-Width * 0.25f, Height - 0.4f, 0);
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.6f, 0.2f);
            lampLight.range = 3f;
            lampLight.intensity = 1.2f;

            AddPlants(root, rng, 5);
            AddRocks(root, rng, 8);

            Color[] skins = { Critters.Green, Critters.Orange, Critters.Lime };
            Color[] stripes = { Critters.Orange, Critters.Green, Critters.Orange };
            for (int i = 0; i < skins.Length; i++)
            {
                GameObject lizard = Critters.Lizard(skins[i], stripes[i], 0.55f + (float)rng.NextDouble() * 0.15f);
                lizard.transform.SetParent(root, false);
                lizard.transform.localPosition = new Vector3(Rand(rng, -1.8f, 1.8f), FloorTop, Rand(rng, -1f, 1f));
                Wander w = lizard.AddComponent<Wander>();
                w.HalfWidth = Width / 2 - 0.5f;
                w.HalfDepth = Depth / 2 - 0.5f;
                w.Floor = FloorTop;
                w.Speed = 1.1f;
                w.TurnSpeed = 300f;
                w.PauseMin = 0.4f;
                w.PauseMax = 2.5f;
                w.TailWag = 25f;
            }
            return root.gameObject;
        }

        public static GameObject BuildTurtleTank(Vector3 position, Quaternion rotation)
        {
            var rng = new System.Random();
            Transform root = Shell("Skylar Turtle Tank", position, rotation, Sand);

            // Half the tank is a shallow pool.
            Vector3 poolCenter = new Vector3(Width * 0.25f, FloorTop + 0.02f, 0);
            FishTank.Box(root, "Pool", poolCenter, new Vector3(Width * 0.45f, 0.04f, Depth - 0.3f), TankMaterials.SharedTinted(Water), false);

            AddPlants(root, rng, 6);
            AddRocks(root, rng, 6);

            Color[] shells = { Critters.Green, Critters.Orange, Critters.Green };
            for (int i = 0; i < shells.Length; i++)
            {
                GameObject turtle = Critters.Turtle(shells[i], Critters.Lime, 0.45f + (float)rng.NextDouble() * 0.15f);
                turtle.transform.SetParent(root, false);
                turtle.transform.localPosition = new Vector3(Rand(rng, -1.8f, 1.8f), FloorTop, Rand(rng, -1f, 1f));
                Wander w = turtle.AddComponent<Wander>();
                w.HalfWidth = Width / 2 - 0.5f;
                w.HalfDepth = Depth / 2 - 0.5f;
                w.Floor = FloorTop;
                w.Speed = 0.25f;
                w.TurnSpeed = 45f;
                w.PauseMin = 2f;
                w.PauseMax = 5f;
                w.Bob = 0.02f;
            }
            return root.gameObject;
        }

        // ---------- shared pieces ----------

        /// <summary>Glass walls (walk around, not through), orange rim, green base, and a floor layer.</summary>
        private static Transform Shell(string name, Vector3 position, Quaternion rotation, Color floor)
        {
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(position, rotation);

            Material glass = TankMaterials.Glass();
            float h = Height;
            FishTank.Box(root, "Glass Front", new Vector3(0, h / 2, -Depth / 2), new Vector3(Width, h, Glass), glass, true);
            FishTank.Box(root, "Glass Back", new Vector3(0, h / 2, Depth / 2), new Vector3(Width, h, Glass), glass, true);
            FishTank.Box(root, "Glass Left", new Vector3(-Width / 2, h / 2, 0), new Vector3(Glass, h, Depth), glass, true);
            FishTank.Box(root, "Glass Right", new Vector3(Width / 2, h / 2, 0), new Vector3(Glass, h, Depth), glass, true);

            Material orange = TankMaterials.Shared(Critters.Orange);
            const float rim = 0.14f;
            FishTank.Box(root, "Rim Front", new Vector3(0, h, -Depth / 2), new Vector3(Width + rim, rim, rim), orange, true);
            FishTank.Box(root, "Rim Back", new Vector3(0, h, Depth / 2), new Vector3(Width + rim, rim, rim), orange, true);
            FishTank.Box(root, "Rim Left", new Vector3(-Width / 2, h, 0), new Vector3(rim, rim, Depth + rim), orange, true);
            FishTank.Box(root, "Rim Right", new Vector3(Width / 2, h, 0), new Vector3(rim, rim, Depth + rim), orange, true);
            FishTank.Box(root, "Base", new Vector3(0, 0.1f, 0), new Vector3(Width + 0.25f, 0.2f, Depth + 0.25f),
                         TankMaterials.Shared(Critters.Green), true);
            FishTank.Box(root, "Floor", new Vector3(0, (0.2f + FloorTop) / 2, 0), new Vector3(Width - 0.15f, FloorTop - 0.2f, Depth - 0.15f),
                         TankMaterials.Shared(floor), false);
            return root;
        }

        private static void AddPlants(Transform root, System.Random rng, int count)
        {
            string[] plants =
            {
                Plants + "Details/RedPlant.prefab", Plants + "Details/YellowPlant.prefab",
                Plants + "CatTail.prefab", Plants + "Details/CarpetGrass.prefab"
            };
            for (int i = 0; i < count; i++)
            {
                Vector3 p = new Vector3(Rand(rng, -Width / 2 + 0.4f, Width / 2 - 0.4f), FloorTop, Rand(rng, -Depth / 2 + 0.4f, Depth / 2 - 0.4f));
                // Keep plants mostly toward the back and sides, so the animals stay visible.
                if (p.z < 0) p.z = -p.z * 0.6f;
                GameObject plant = FishTank.Place(root, plants[i % plants.Length], 0.6f + (float)rng.NextDouble() * 0.8f, p, rng);
                if (plant != null)
                {
                    Sway sway = plant.AddComponent<Sway>();
                    sway.Amount = 4f;
                    sway.Speed = 0.5f + (float)rng.NextDouble() * 0.4f;
                    sway.Phase = (float)rng.NextDouble() * 6.28f;
                }
            }
        }

        private static void AddRocks(Transform root, System.Random rng, int count)
        {
            Color[] colors = { Grey, Critters.Orange, new Color(0.4f, 0.4f, 0.45f) };
            for (int i = 0; i < count; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock";
                Object.Destroy(rock.GetComponent<Collider>());
                rock.transform.SetParent(root, false);
                float s = 0.15f + (float)rng.NextDouble() * 0.3f;
                rock.transform.localScale = new Vector3(s * 1.3f, s * 0.7f, s);
                rock.transform.localPosition = new Vector3(Rand(rng, -Width / 2 + 0.3f, Width / 2 - 0.3f), FloorTop + s * 0.15f,
                                                           Rand(rng, -Depth / 2 + 0.3f, Depth / 2 - 0.3f));
                rock.GetComponent<Renderer>().sharedMaterial = TankMaterials.Shared(colors[i % colors.Length]);
            }
        }

        private static void Flat(Transform root, string name, Vector3 pos, float size, Color color)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = pos;
            disc.transform.localScale = new Vector3(size, 0.01f, size);
            disc.GetComponent<Renderer>().sharedMaterial = TankMaterials.Shared(color);
        }

        private static float Rand(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
