using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WobblyMods.HouseMenu.Tanks
{
    /// <summary>
    /// Skylar's giant walk-around fish tank (task T24), built at runtime from simple shapes plus the game's
    /// own models (addresses CONFIRMED in Scout report 2): fishing fish, seahorses, seaweed, coral, shells,
    /// starfish, a castle turret, and the "Fish Bubbles" effect.
    ///
    /// Glass walls keep their colliders, so players walk around the tank, not through it. Everything inside
    /// is visual-only. Colors follow her favorites: orange rim, green base.
    /// It's one object, so the menu's undo removes the whole tank.
    /// </summary>
    internal static class FishTank
    {
        public const float Width = 8f;
        public const float Depth = 5f;
        public const float Height = 3.5f;
        private const float Glass = 0.08f;
        private const float SandHeight = 0.3f;

        private const string FishPath = "Assets/Content/Game/Prefabs/Props/Fishing_Props/Fish/Static/";
        private const string PlantPath = "Assets/Content/Game/Prefabs/Environment/Vegitation/Details/";
        private const string OceanPath = "Assets/Content/Game/Prefabs/Props/Artifacts/Ocean/";

        private static readonly Color Orange = new Color(1f, 0.5f, 0f);
        private static readonly Color Green = new Color(0.2f, 0.7f, 0.2f);
        private static readonly Color SandColor = new Color(0.93f, 0.82f, 0.6f);

        // name, length in meters
        private static readonly KeyValuePair<string, float>[] FishList =
        {
            Pair("ClownFish", 0.6f), Pair("ClownFish", 0.6f), Pair("Angel Fish", 0.8f), Pair("Pufferfish", 0.7f),
            Pair("Parrot Fish", 0.9f), Pair("LionFish", 0.8f), Pair("Blue Dragon", 0.7f), Pair("Wobbly Fish", 0.9f),
            Pair("SeaHorse_Green", 0.6f), Pair("SeaHorse_Yellow", 0.6f), Pair("JellyFish", 0.7f), Pair("StingRay", 1.2f)
        };

        // Fish whose model faces backwards compared with the others. Add names here if more swim backwards.
        private static readonly string[] BackwardFish = { "Pufferfish" };

        private static KeyValuePair<string, float> Pair(string name, float size)
        {
            return new KeyValuePair<string, float>(name, size);
        }

        /// <summary>A tabletop-sized copy of the giant tank (same contents, scaled down).</summary>
        public static GameObject BuildSmall(Vector3 position, Quaternion rotation)
        {
            GameObject tank = Build(position, rotation);
            tank.name = "Skylar Small Fish Tank";
            tank.transform.localScale = Vector3.one * 0.3f;
            return tank;
        }

        /// <summary>Builds the whole tank. position is the floor point under the tank's center.</summary>
        public static GameObject Build(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Skylar Fish Tank");
            root.transform.SetPositionAndRotation(position, rotation);
            var rng = new System.Random();

            BuildGlassAndFrame(root.transform);
            BuildFloor(root.transform, rng);
            BuildDecorations(root.transform, rng);
            BuildFish(root.transform, rng);
            BuildBubbles(root.transform);

            Plugin.Log.LogInfo("Fish tank built (glass material: " + TankMaterials.GlassMaterialName + ").");
            return root;
        }

        // ---------- glass and frame ----------

        private static void BuildGlassAndFrame(Transform root)
        {
            Material glass = TankMaterials.Glass();
            float h = Height;
            // Four glass walls (keep colliders so people walk around, not through).
            Box(root, "Glass Front", new Vector3(0, h / 2, -Depth / 2), new Vector3(Width, h, Glass), glass, true);
            Box(root, "Glass Back", new Vector3(0, h / 2, Depth / 2), new Vector3(Width, h, Glass), glass, true);
            Box(root, "Glass Left", new Vector3(-Width / 2, h / 2, 0), new Vector3(Glass, h, Depth), glass, true);
            Box(root, "Glass Right", new Vector3(Width / 2, h / 2, 0), new Vector3(Glass, h, Depth), glass, true);

            // Orange rim along the top, green base along the bottom.
            Material orange = TankMaterials.Shared(Orange);
            Material green = TankMaterials.Shared(Green);
            const float rim = 0.18f;
            Box(root, "Rim Front", new Vector3(0, h, -Depth / 2), new Vector3(Width + rim, rim, rim), orange, true);
            Box(root, "Rim Back", new Vector3(0, h, Depth / 2), new Vector3(Width + rim, rim, rim), orange, true);
            Box(root, "Rim Left", new Vector3(-Width / 2, h, 0), new Vector3(rim, rim, Depth + rim), orange, true);
            Box(root, "Rim Right", new Vector3(Width / 2, h, 0), new Vector3(rim, rim, Depth + rim), orange, true);
            Box(root, "Base", new Vector3(0, 0.1f, 0), new Vector3(Width + 0.3f, 0.2f, Depth + 0.3f), green, true);
        }

        // ---------- sand and rocks ----------

        private static void BuildFloor(Transform root, System.Random rng)
        {
            Box(root, "Sand", new Vector3(0, 0.2f + SandHeight / 2, 0), new Vector3(Width - 0.2f, SandHeight, Depth - 0.2f),
                TankMaterials.Shared(SandColor), false);

            Color[] rockColors = { Orange, new Color(0.55f, 0.55f, 0.55f), new Color(0.85f, 0.4f, 0.1f), new Color(0.4f, 0.4f, 0.45f) };
            for (int i = 0; i < 9; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock " + i;
                Object.Destroy(rock.GetComponent<Collider>());
                rock.transform.SetParent(root, false);
                float size = 0.25f + (float)rng.NextDouble() * 0.45f;
                rock.transform.localScale = new Vector3(size * 1.3f, size * 0.7f, size);
                rock.transform.localPosition = RandomFloorPoint(rng, 0.5f) + Vector3.up * (size * 0.2f);
                rock.GetComponent<Renderer>().sharedMaterial = TankMaterials.Shared(rockColors[i % rockColors.Length]);
            }
        }

        // ---------- plants, coral, shells, castle ----------

        private static void BuildDecorations(Transform root, System.Random rng)
        {
            float floorY = 0.2f + SandHeight;

            // Seaweed that sways with the current.
            for (int i = 0; i < 10; i++)
            {
                string name = i % 2 == 0 ? "Seaweed 1" : "Seaweed 2";
                GameObject weed = Place(root, PlantPath + name + ".prefab", 1.2f + (float)rng.NextDouble() * 1.2f,
                                        RandomFloorPoint(rng, 0.4f), rng); // already at sand level (the floor height was added twice before)
                if (weed != null)
                {
                    Sway sway = weed.AddComponent<Sway>();
                    sway.Phase = (float)rng.NextDouble() * 6.28f;
                    sway.Speed = 0.6f + (float)rng.NextDouble() * 0.5f;
                }
            }

            Place(root, OceanPath + "Artifact_Ocean_Coral_Static.prefab", 1.0f, new Vector3(-Width * 0.3f, floorY, Depth * 0.25f), rng);
            Place(root, OceanPath + "Artifact_Ocean_Coral_Static.prefab", 0.8f, new Vector3(Width * 0.32f, floorY, -Depth * 0.2f), rng);
            Place(root, OceanPath + "Artifact_Ocean_ClamShell_Static.prefab", 0.6f, new Vector3(Width * 0.15f, floorY, Depth * 0.3f), rng);
            Place(root, FishPath + "Starfish_Static.prefab", 0.4f, new Vector3(-Width * 0.1f, floorY + 0.02f, -Depth * 0.3f), rng);
            Place(root, FishPath + "Starfish_Static.prefab", 0.4f, new Vector3(Width * 0.38f, floorY + 0.02f, Depth * 0.35f), rng);
            Place(root, FishPath + "Giant Crab_Static.prefab", 0.6f, new Vector3(-Width * 0.38f, floorY, -Depth * 0.3f), rng);

            // Treasure by the castle (Skylar asked for a treasure chest; the game has gold coins and bars).
            const string Gold = "Assets/Content/Game/Prefabs/Props/Treasure/Money/";
            Place(root, Gold + "Treasure_Gold_Coin_Static.prefab", 0.35f, new Vector3(0.9f, floorY, -0.6f), rng);
            Place(root, Gold + "Treasure_Gold_Coin_Static.prefab", 0.3f, new Vector3(1.1f, floorY, -0.3f), rng);
            Place(root, Gold + "Treasure_Gold_Ingot_Static.prefab", 0.4f, new Vector3(0.7f, floorY, -0.25f), rng);

            // The castle: the centerpiece the fish swim around.
            GameObject castle = Place(root, "Arcade/ArcadeLobby/Prefabs/Props/PlayZone/CastleTurret.prefab", 1.8f,
                                      new Vector3(0, floorY, 0), rng);
            if (castle != null)
            {
                castle.transform.localRotation = Quaternion.identity;
            }
        }

        // ---------- fish ----------

        private static void BuildFish(Transform root, System.Random rng)
        {
            float floorY = 0.2f + SandHeight;
            for (int i = 0; i < FishList.Length; i++)
            {
                KeyValuePair<string, float> f = FishList[i];
                GameObject fish = Place(root, FishPath + f.Key + "_Static.prefab", f.Value, Vector3.zero, rng);
                if (fish == null) continue;

                Swim swim = fish.AddComponent<Swim>();
                swim.Center = new Vector3(0, floorY + 0.6f + (float)rng.NextDouble() * (Height - floorY - 1.2f), 0);
                swim.RadiusX = Width * (0.22f + (float)rng.NextDouble() * 0.2f);
                swim.RadiusZ = Depth * (0.18f + (float)rng.NextDouble() * 0.17f);
                swim.Speed = (0.25f + (float)rng.NextDouble() * 0.3f) * (rng.Next(2) == 0 ? 1f : -1f);
                swim.Angle = (float)rng.NextDouble() * 6.28f;
                swim.Bob = 0.08f + (float)rng.NextDouble() * 0.12f;
                swim.Model = fish.transform.GetChild(0);
                // Models that face the other way from the rest (seen in game: the pufferfish swam backwards).
                swim.Flip = System.Array.IndexOf(BackwardFish, f.Key) >= 0;
            }
        }

        // ---------- bubbles ----------

        private static void BuildBubbles(Transform root)
        {
            float floorY = 0.2f + SandHeight;
            Vector3[] spots = { new Vector3(-Width * 0.35f, floorY, 0), new Vector3(Width * 0.25f, floorY, Depth * 0.1f) };
            foreach (Vector3 spot in spots)
            {
                string problem;
                GameObject effect = GameSpawner.SpawnLocal("Assets/Content/Game/Prefabs/Particles/Shrinking Machine and Lab/Fish Bubbles.prefab",
                                                           root.TransformPoint(spot), root.rotation, out problem);
                if (effect != null)
                {
                    effect.transform.SetParent(root, true);
                }
                else
                {
                    // Our own simple bubbles if the game's effect can't load.
                    var stream = new GameObject("Bubbles");
                    stream.transform.SetParent(root, false);
                    stream.transform.localPosition = spot;
                    stream.AddComponent<Bubbles>().Top = Height - floorY - 0.2f;
                }
            }
        }

        // ---------- helpers ----------

        /// <summary>
        /// Loads one of the game's models, makes it visual-only, sizes it so its longest side is `size` meters,
        /// and puts it at localPos inside the tank. The model sits inside a holder object, so swimming and
        /// swaying move the holder while the model keeps its own orientation. Returns the holder, or null.
        /// </summary>
        internal static GameObject Place(Transform root, string address, float size, Vector3 localPos, System.Random rng)
        {
            string problem;
            GameObject model = GameSpawner.SpawnLocal(address, Vector3.zero, Quaternion.identity, out problem);
            if (model == null)
            {
                Plugin.Log.LogWarning("Fish tank: couldn't load " + address + ": " + problem);
                return null;
            }

            MakeVisualOnly(model);
            var holder = new GameObject(model.name.Replace("(Clone)", "").Trim());
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);

            model.transform.SetParent(holder.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds b;
            if (TryGetBounds(model, out b))
            {
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (longest > 0.001f)
                {
                    model.transform.localScale = model.transform.localScale * (size / longest);
                }
                // Rest the model's bottom on the holder's origin.
                if (TryGetBounds(model, out b))
                {
                    float bottom = b.min.y - holder.transform.position.y;
                    model.transform.position -= new Vector3(0, bottom, 0);
                }
            }
            return holder;
        }

        internal static void MakeVisualOnly(GameObject model)
        {
            foreach (Joint j in model.GetComponentsInChildren<Joint>(true)) Object.Destroy(j);
            foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(rb);
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        }

        internal static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        private static Vector3 RandomFloorPoint(System.Random rng, float margin)
        {
            float x = ((float)rng.NextDouble() - 0.5f) * (Width - 2 * margin);
            float z = ((float)rng.NextDouble() - 0.5f) * (Depth - 2 * margin);
            return new Vector3(x, 0.2f + SandHeight, z);
        }

        internal static GameObject Box(Transform root, string name, Vector3 localPos, Vector3 size, Material material, bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (!keepCollider) Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(root, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = size;
            Renderer r = box.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = material.renderQueue >= (int)RenderQueue.Transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
            return box;
        }
    }
}
