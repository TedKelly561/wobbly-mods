using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace WobblyMods.HouseMenu.Icons
{
    /// <summary>
    /// Real pictures of the catalog items for the menu tiles: one 128 x 128 Texture2D per item, made lazily
    /// (started the first time any menu opens) and spread over many frames so the game never freezes.
    ///
    /// How a picture is made:
    /// - Game props (Address): the prefab is loaded with Addressables (async, polled each frame) but NOT
    ///   instantiated, because that would run its networked game scripts. Instead a "mesh-only" copy is built:
    ///   the prefab's hierarchy is walked and only MeshFilter + MeshRenderer pairs are recreated (shared mesh,
    ///   shared materials, local transforms). SkinnedMeshRenderers become static meshes. Lower LOD levels are
    ///   skipped so they don't overlap.
    /// - Our own builds (tanks, lamps, catfish): item.Build is called far below the world (y = -5000),
    ///   photographed and destroyed in the same frame. If Build returns null (for example the catfish bundle
    ///   isn't installed), the tile shows a symbol instead.
    /// - Photo: the preview is moved to layer 31 (<see cref="PreviewLayer"/>), a temporary camera that sees only
    ///   that layer renders it from a 3/4 view over a light tab-colored background, lit by two temporary
    ///   directional lights limited to that layer. The 256 x 256 render is read back and shrunk to 128 x 128.
    ///   Everything temporary is destroyed right after.
    ///
    /// UNCERTAIN in game (not tested yet):
    /// - Layer 31 is assumed unused by Wobbly Life. If the game uses it, other things on that layer that happen to be
    ///   inside the preview camera's view (far below the world, so unlikely) could appear in a picture.
    /// - The game's shaders may depend on global values set by its own cameras (custom lighting, fog); pictures
    ///   could come out darker or flatter than in game. Fog is switched off and ambient light set flat during each
    ///   render, then restored.
    /// - Models are assumed to face +Z (Unity's usual "front"); a model facing the other way is photographed from
    ///   behind.
    /// - Callbacks the game hooks onto every camera (Camera.onPreRender) also run for the preview camera.
    /// Every item is wrapped in try/catch: a failure logs once and the tile shows a symbol for its kind instead.
    /// </summary>
    internal static class IconCache
    {
        /// <summary>Layer used only for preview objects. 31 is the last user layer; believed unused by the game.</summary>
        public const int PreviewLayer = 31;
        public const int Size = 128;

        private const int RenderSize = Size * 2; // rendered at 2x and shrunk, for smooth edges without MSAA
        private const float FrameBudgetSeconds = 0.004f;
        private const int MaxSnapshotsPerFrame = 2;
        private const int MaxLoadsInFlight = 2;
        private const int MaxPreviewNodes = 3000;

        private static readonly Vector3 PreviewSpot = new Vector3(0f, -5000f, 0f);

        // From the object toward the camera: in front (+Z), a bit to the right and above. A friendly 3/4 view.
        private static readonly Vector3 ViewDirection = new Vector3(0.55f, 0.5f, 1f).normalized;

        private sealed class PendingLoad
        {
            public CatalogItem Item;
            public object Handle;
            public float StartedAt;
        }

        private static readonly Dictionary<string, Texture2D> Ready = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>();
        private static readonly HashSet<string> Failed = new HashSet<string>();
        private static readonly Queue<CatalogItem> Waiting = new Queue<CatalogItem>();
        private static readonly List<PendingLoad> Loading = new List<PendingLoad>();
        private static bool _started;
        private static bool _finishedLogged;
        private static bool _tickFailed;
        private static MethodInfo _loadGeneric;
        private static bool _loadSearched;

        public static bool Started
        {
            get { return _started; }
        }

        /// <summary>Starts making pictures (once). Call when a menu first opens.</summary>
        public static void Start()
        {
            if (_started) return;
            _started = true;
            foreach (CatalogItem item in FurnitureCatalog.Items)
            {
                if (item.Symbol != null) continue; // drawn symbol, no photo (and no need to build it just for a picture)
                if (HiddenItems.IsHidden(item.Id)) continue; // not in the menu, so nobody would see its picture
                Waiting.Enqueue(item);
            }
            Plugin.Log.LogInfo("Menu pictures: making " + Waiting.Count + " item pictures in the background (preview layer " +
                               PreviewLayer + ").");
        }

        /// <summary>The picture for a catalog item, or null while it's being made (or if it failed).</summary>
        public static Texture2D Get(string catalogId)
        {
            Texture2D tex;
            return catalogId != null && Ready.TryGetValue(catalogId, out tex) ? tex : null;
        }

        /// <summary>A symbol key to show instead of a picture that couldn't be made, or null (letter placeholder).</summary>
        public static string FallbackFor(string catalogId)
        {
            string key;
            return catalogId != null && Fallbacks.TryGetValue(catalogId, out key) ? key : null;
        }

        /// <summary>Call once per frame (from Update). Does a little work each frame until every picture is made.</summary>
        public static void Tick()
        {
            if (!_started || _tickFailed) return;
            try
            {
                TickCore();
            }
            catch (Exception e)
            {
                // Something unexpected outside the per-item guards: stop for good rather than log every frame.
                _tickFailed = true;
                Plugin.Log.LogError("Menu pictures stopped: " + e);
            }
        }

        private static void TickCore()
        {
            if (Waiting.Count == 0 && Loading.Count == 0)
            {
                if (!_finishedLogged)
                {
                    _finishedLogged = true;
                    Plugin.Log.LogInfo("Menu pictures: " + Ready.Count + " made, " + Failed.Count + " couldn't be made" +
                                       (Failed.Count > 0 ? " (" + string.Join(", ", Failed.ToArray()) + ")" : "") + ".");
                }
                return;
            }

            Stopwatch clock = Stopwatch.StartNew();
            int snapshots = 0;

            // 1. Finish loads that are done.
            for (int i = Loading.Count - 1; i >= 0; i--)
            {
                if (snapshots >= MaxSnapshotsPerFrame || clock.Elapsed.TotalSeconds > FrameBudgetSeconds) return;
                PendingLoad load = Loading[i];
                bool done;
                GameObject prefab;
                string problem;
                if (!PollLoad(load, out done, out prefab, out problem))
                {
                    Loading.RemoveAt(i);
                    Fail(load.Item, problem);
                    continue;
                }
                if (!done) continue;

                Loading.RemoveAt(i);
                snapshots++;
                PictureFromPrefab(load.Item, prefab);
            }

            // 2. Start more work.
            while (Waiting.Count > 0 && clock.Elapsed.TotalSeconds <= FrameBudgetSeconds && snapshots < MaxSnapshotsPerFrame)
            {
                CatalogItem item = Waiting.Peek();
                if (item.Build != null)
                {
                    // Our builds can be big (the giant tank has dozens of parts): one per frame, on a frame of its own.
                    if (snapshots > 0) return;
                    Waiting.Dequeue();
                    PictureFromBuild(item);
                    return;
                }

                if (string.IsNullOrEmpty(item.Address))
                {
                    Waiting.Dequeue();
                    Fail(item, "it has no model");
                    continue;
                }

                if (Loading.Count >= MaxLoadsInFlight) return;
                Waiting.Dequeue();
                string problem;
                object handle = StartLoad(item.Address, out problem);
                if (handle == null)
                {
                    Fail(item, problem);
                    continue;
                }
                Loading.Add(new PendingLoad { Item = item, Handle = handle, StartedAt = Time.unscaledTime });
            }
        }

        // ---------- Loading (Addressables through reflection, like GameSpawner) ----------

        private static object StartLoad(string address, out string problem)
        {
            problem = null;
            if (!_loadSearched)
            {
                _loadSearched = true;
                Type addressables = GameReflection.FindType("UnityEngine.AddressableAssets.Addressables");
                MethodInfo generic = addressables == null ? null : addressables.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "LoadAssetAsync" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 &&
                                         m.GetParameters()[0].ParameterType == typeof(object));
                _loadGeneric = generic == null ? null : generic.MakeGenericMethod(typeof(GameObject));
            }
            if (_loadGeneric == null)
            {
                problem = "Addressables.LoadAssetAsync wasn't found";
                return null;
            }
            try
            {
                return _loadGeneric.Invoke(null, new object[] { address });
            }
            catch (Exception e)
            {
                problem = "loading failed: " + Inner(e);
                return null;
            }
        }

        /// <summary>
        /// Checks a load without blocking (handle.IsDone). Returns false on failure. If the handle has no IsDone
        /// (unexpected Addressables version), falls back to WaitForCompletion, which may pause for a moment.
        /// </summary>
        private static bool PollLoad(PendingLoad load, out bool done, out GameObject prefab, out string problem)
        {
            done = false;
            prefab = null;
            problem = null;
            try
            {
                Type t = load.Handle.GetType();
                PropertyInfo isDone = t.GetProperty("IsDone", BindingFlags.Public | BindingFlags.Instance);
                if (isDone != null)
                {
                    object value = isDone.GetValue(load.Handle, null);
                    if (!(value is bool) || !(bool)value)
                    {
                        if (Time.unscaledTime - load.StartedAt > 30f)
                        {
                            problem = "the model took too long to load";
                            return false;
                        }
                        return true; // still loading
                    }
                    PropertyInfo result = t.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                    prefab = result == null ? null : result.GetValue(load.Handle, null) as GameObject;
                }
                else
                {
                    MethodInfo wait = t.GetMethod("WaitForCompletion", BindingFlags.Public | BindingFlags.Instance);
                    prefab = wait == null ? null : wait.Invoke(load.Handle, null) as GameObject;
                }
                done = true;
                if (prefab == null)
                {
                    problem = "the model couldn't be loaded";
                    return false;
                }
                // The handle is kept (not released): placed copies of the same prop may share the loaded asset.
                return true;
            }
            catch (Exception e)
            {
                problem = "loading failed: " + Inner(e);
                return false;
            }
        }

        // ---------- Making pictures ----------

        private static void PictureFromPrefab(CatalogItem item, GameObject prefab)
        {
            GameObject preview = null;
            try
            {
                preview = MeshOnlyCopy(prefab);
                Texture2D tex = Snapshot(preview, Background(item));
                if (tex == null)
                {
                    Fail(item, "nothing visible to photograph");
                    return;
                }
                tex.name = "WobblyIcon " + item.Id;
                Ready[item.Id] = tex;
            }
            catch (Exception e)
            {
                Fail(item, "picture failed: " + Inner(e));
            }
            finally
            {
                if (preview != null)
                {
                    preview.SetActive(false);
                    Object.Destroy(preview);
                }
            }
        }

        private static void PictureFromBuild(CatalogItem item)
        {
            GameObject built = null;
            try
            {
                built = item.Build(PreviewSpot, Quaternion.identity);
                if (built == null)
                {
                    // e.g. the redtail catfish before its bundle is installed.
                    Fallbacks[item.Id] = item.Category == "Animals" ? SymbolIcons.Key("fish") : SymbolIcons.Key("star");
                    Fail(item, "it couldn't be built here (showing a symbol)");
                    return;
                }
                if (!Mathf.Approximately(item.Scale, 1f))
                {
                    built.transform.localScale *= item.Scale;
                }
                Texture2D tex = Snapshot(built, Background(item));
                if (tex == null)
                {
                    Fallbacks[item.Id] = SymbolIcons.Key("star");
                    Fail(item, "nothing visible to photograph");
                    return;
                }
                tex.name = "WobblyIcon " + item.Id;
                Ready[item.Id] = tex;
            }
            catch (Exception e)
            {
                Fallbacks[item.Id] = SymbolIcons.Key("star");
                Fail(item, "picture failed: " + Inner(e));
            }
            finally
            {
                if (built != null)
                {
                    built.SetActive(false);
                    Object.Destroy(built);
                }
            }
        }

        /// <summary>Makes (or replaces) the picture for any key from a live object, e.g. a "Copy this" slot.</summary>
        public static void PictureOf(string key, GameObject subject, Color background)
        {
            GameObject preview = null;
            try
            {
                preview = MeshOnlyCopy(subject);
                preview.SetActive(true);
                Texture2D tex = Snapshot(preview, background);
                // The picture this one replaces (the slot's last copy) is thrown away, or one would pile up per copy.
                Texture2D old;
                if (Ready.TryGetValue(key, out old) && old != null) Object.Destroy(old);
                if (tex == null) { Ready.Remove(key); return; }
                tex.name = "WobblyIcon " + key;
                Ready[key] = tex;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Menu pictures: " + key + " picture failed: " + Inner(e));
            }
            finally
            {
                if (preview != null)
                {
                    preview.SetActive(false);
                    Object.Destroy(preview);
                }
            }
        }

        /// <summary>The symbol to show for a key while it has no picture (e.g. an empty copy slot).</summary>
        public static void SetFallback(string key, string symbolKey)
        {
            Fallbacks[key] = symbolKey;
        }

        private static Color Background(CatalogItem item)
        {
            return TabTheme.Light(TabTheme.ColorFor(item.Category), 0.6f);
        }

        /// <summary>
        /// A copy of the prefab with ONLY its meshes: no game scripts, colliders, network objects or particles.
        /// Built at the preview spot on the preview layer.
        /// </summary>
        private static GameObject MeshOnlyCopy(GameObject prefab)
        {
            var skip = new HashSet<Renderer>();
            foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (Renderer r in lods[i].renderers)
                    {
                        if (r != null) skip.Add(r);
                    }
                }
            }

            var root = new GameObject("WobblyIconPreview") { layer = PreviewLayer };
            root.transform.position = PreviewSpot;
            root.transform.localScale = prefab.transform.localScale;
            int nodes = 0;
            CopyMeshes(prefab.transform, root, skip, ref nodes);
            return root;
        }

        private static void CopyMeshes(Transform source, GameObject target, HashSet<Renderer> skip, ref int nodes)
        {
            AddMesh(source.gameObject, target, skip);
            foreach (Transform child in source)
            {
                if (!child.gameObject.activeSelf || nodes >= MaxPreviewNodes) continue;
                nodes++;
                var copy = new GameObject(child.name) { layer = PreviewLayer };
                copy.transform.SetParent(target.transform, false);
                copy.transform.localPosition = child.localPosition;
                copy.transform.localRotation = child.localRotation;
                copy.transform.localScale = child.localScale;
                CopyMeshes(child, copy, skip, ref nodes);
            }
        }

        private static void AddMesh(GameObject source, GameObject target, HashSet<Renderer> skip)
        {
            Mesh mesh = null;
            Material[] materials = null;

            MeshRenderer meshRenderer = source.GetComponent<MeshRenderer>();
            MeshFilter filter = source.GetComponent<MeshFilter>();
            if (meshRenderer != null && meshRenderer.enabled && filter != null && filter.sharedMesh != null && !skip.Contains(meshRenderer))
            {
                mesh = filter.sharedMesh;
                materials = meshRenderer.sharedMaterials;
            }
            else
            {
                // Skinned models (animals, characters): shown in their rest pose as a plain mesh.
                // UNCERTAIN: a skinned mesh whose root bone is elsewhere may appear offset or at bind-pose size.
                SkinnedMeshRenderer skinned = source.GetComponent<SkinnedMeshRenderer>();
                if (skinned != null && skinned.enabled && skinned.sharedMesh != null && !skip.Contains(skinned))
                {
                    mesh = skinned.sharedMesh;
                    materials = skinned.sharedMaterials;
                }
            }
            if (mesh == null) return;

            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer copy = target.AddComponent<MeshRenderer>();
            copy.sharedMaterials = materials;
            copy.shadowCastingMode = ShadowCastingMode.Off;
            copy.receiveShadows = false;
        }

        /// <summary>Photographs everything under subject. Returns null if nothing visible was found.</summary>
        private static Texture2D Snapshot(GameObject subject, Color background)
        {
            SetLayer(subject.transform, PreviewLayer);
            Bounds bounds;
            if (!TryGetBounds(subject, out bounds)) return null;

            var rig = new GameObject("WobblyIconRig") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture rt = null;
            Texture2D big = null;
            Camera cam = null;

            bool fog = RenderSettings.fog;
            AmbientMode ambientMode = RenderSettings.ambientMode;
            Color ambientLight = RenderSettings.ambientLight;

            try
            {
                var camObject = new GameObject("Camera");
                camObject.transform.SetParent(rig.transform, false);
                cam = camObject.AddComponent<Camera>();
                cam.enabled = false; // rendered by hand, once
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background;
                cam.cullingMask = 1 << PreviewLayer;
                cam.orthographic = false;
                cam.fieldOfView = 30f;
                cam.allowHDR = false;
                cam.allowMSAA = false;
                cam.useOcclusionCulling = false;
                Frame(cam, bounds);

                AddLight(rig, -ViewDirection + new Vector3(-0.5f, -0.9f, 0.1f), 1.15f);  // key light: from above-left
                AddLight(rig, -ViewDirection + new Vector3(0.9f, 0.2f, 0f), 0.45f);      // soft fill from the right

                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

                rt = RenderTexture.GetTemporary(RenderSize, RenderSize, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                big = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
                big.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0, false);
                return Shrink(big);
            }
            finally
            {
                RenderSettings.fog = fog;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                RenderTexture.active = previousActive;
                if (cam != null) cam.targetTexture = null;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (big != null) Object.Destroy(big);
                Object.Destroy(rig);
            }
        }

        /// <summary>Places the camera on the 3/4 view so the whole bounding box fits with a small margin.</summary>
        private static void Frame(Camera cam, Bounds bounds)
        {
            Quaternion look = Quaternion.LookRotation(-ViewDirection, Vector3.up);
            Vector3 right = look * Vector3.right, up = look * Vector3.up, forward = look * Vector3.forward;
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.88f;

            // For each corner: the camera must be far enough back that the corner fits inside the view.
            float distance = 0.05f;
            Vector3 e = bounds.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                float cx = Mathf.Abs(Vector3.Dot(corner, right));
                float cy = Mathf.Abs(Vector3.Dot(corner, up));
                float cz = Vector3.Dot(corner, forward);
                distance = Mathf.Max(distance, Mathf.Max(cx, cy) / tan - cz);
            }

            cam.transform.rotation = look;
            cam.transform.position = bounds.center - forward * distance;
            float radius = e.magnitude;
            cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.2f);
            cam.farClipPlane = distance + radius * 1.2f + 1f;
        }

        private static void AddLight(GameObject rig, Vector3 direction, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(rig.transform, false);
            go.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << PreviewLayer;
        }

        /// <summary>Bounds of every visible mesh (manual math, so it works the same frame the objects were made).</summary>
        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                MeshRenderer r = filter.GetComponent<MeshRenderer>();
                if (r == null || !r.enabled || filter.sharedMesh == null) continue;
                Bounds local = filter.sharedMesh.bounds;
                Matrix4x4 m = filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 w = m.MultiplyPoint3x4(c);
                    if (!any) { bounds = new Bounds(w, Vector3.zero); any = true; }
                    else bounds.Encapsulate(w);
                }
            }
            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skinned.enabled || skinned.sharedMesh == null) continue;
                if (!any) { bounds = skinned.bounds; any = true; }
                else bounds.Encapsulate(skinned.bounds);
            }
            return any && bounds.size.sqrMagnitude > 1e-8f;
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
            {
                SetLayer(child, layer);
            }
        }

        /// <summary>2x2 box filter down to 128 x 128, fully opaque (the render's alpha isn't meaningful).</summary>
        private static Texture2D Shrink(Texture2D big)
        {
            Color32[] src = big.GetPixels32();
            var dst = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = 2 * x + 2 * y * RenderSize;
                    Color32 a = src[i], b = src[i + 1], c = src[i + RenderSize], d = src[i + RenderSize + 1];
                    dst[x + y * Size] = new Color32(
                        (byte)((a.r + b.r + c.r + d.r + 2) / 4),
                        (byte)((a.g + b.g + c.g + d.g + 2) / 4),
                        (byte)((a.b + b.b + c.b + d.b + 2) / 4),
                        255);
                }
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels32(dst);
            tex.Apply(false, true);
            return tex;
        }

        private static void Fail(CatalogItem item, string problem)
        {
            // No photo: show a symbol for the kind of thing it is rather than a letter (she can't read yet).
            if (!Fallbacks.ContainsKey(item.Id))
            {
                Fallbacks[item.Id] = item.Category == "Animals" ? SymbolIcons.Key("fish")
                                   : item.Category == "Toys" ? TabTheme.IconFor("Toys")
                                   : item.Category == "Furniture" ? TabTheme.IconFor("Furniture")
                                   : SymbolIcons.Key("star");
            }
            if (Failed.Add(item.Id))
            {
                Plugin.Log.LogWarning("Menu picture for " + item.Id + ": " + problem + ".");
            }
        }

        private static string Inner(Exception e)
        {
            Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
            return inner.GetType().Name + ": " + inner.Message;
        }
    }
}
