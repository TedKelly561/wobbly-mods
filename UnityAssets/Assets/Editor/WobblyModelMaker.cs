// Unity Editor tools for Skylar's models. Menu: "Wobbly Mods".
//
// 1. "Make Redtail Catfish from selected model": select the imported koi model (Assets/Models/Koi/...), run this.
//    It makes Assets/Bundles/wobbly_animals/RedtailCatfish.prefab with redtail colors: dark grey body,
//    cream-white belly, bright orange-red tail and fins, dark whiskers.
// 2. "Make Black Cat from selected model": same for a cat model. Black fur, yellow eyes.
// 3. "Build Bundles": packs every prefab under Assets/Bundles/<bundleName>/ into an AssetBundle named <bundleName>,
//    for Windows 64-bit, into BuiltBundles/. If the game is installed in the default Steam folder, it also copies
//    them into BepInEx\plugins\WobblyMods\bundles\ (where the mod's ModelLibrary looks).
//
// Colors are picked from each material's name (for example "belly", "fin", "tail", "eye"). If a model's material
// names don't match, open the made prefab and adjust its material colors by hand, then Build Bundles again.
//
// NOTE: written without a Unity Editor to test against. If something doesn't compile, the Console shows the line.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WobblyModelMaker
{
    private const string BundleRoot = "Assets/Bundles";
    private const string AnimalsBundle = "wobbly_animals";
    private const string ShaderName = "WobblyMods/SimpleLit";
    private const string GameBundleFolder =
        @"C:\Program Files (x86)\Steam\steamapps\common\Wobbly Life\BepInEx\plugins\WobblyMods\bundles";

    // Redtail catfish colors.
    private static readonly Color DarkBody = new Color(0.18f, 0.18f, 0.2f);
    private static readonly Color Cream = new Color(0.95f, 0.92f, 0.82f);
    private static readonly Color RedOrange = new Color(1f, 0.33f, 0.08f);
    private static readonly Color Whisker = new Color(0.1f, 0.1f, 0.1f);

    // Cat colors.
    private static readonly Color Black = new Color(0.06f, 0.06f, 0.07f);
    private static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);

    [MenuItem("Wobbly Mods/Make Redtail Catfish from selected model")]
    public static void MakeRedtail()
    {
        MakeFromSelection("RedtailCatfish", name =>
        {
            if (Has(name, "belly", "under", "white", "light")) return Cream;
            if (Has(name, "tail", "fin")) return RedOrange;
            if (Has(name, "whisk", "barbel", "trim", "yellow")) return Whisker;
            if (Has(name, "eye", "pupil")) return Color.black;
            return DarkBody;
        });
    }

    [MenuItem("Wobbly Mods/Make Black Cat from selected model")]
    public static void MakeBlackCat()
    {
        MakeFromSelection("BlackCat", name =>
        {
            if (Has(name, "pupil")) return Color.black;
            if (Has(name, "eye", "iris")) return Yellow;
            if (Has(name, "nose", "tongue", "mouth")) return new Color(0.9f, 0.5f, 0.6f);
            return Black;
        });
    }

    [MenuItem("Wobbly Mods/Build Bundles")]
    public static void BuildBundles()
    {
        // Name every prefab by the folder it's in: Assets/Bundles/<bundle>/<Prefab>.prefab
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { BundleRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string bundle = Path.GetFileName(Path.GetDirectoryName(path)).ToLowerInvariant();
            AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(bundle, "");
        }
        // Make sure our shader goes into the animals bundle too.
        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
        {
            AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)).SetAssetBundleNameAndVariant(AnimalsBundle, "");
        }

        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "BuiltBundles");
        Directory.CreateDirectory(outDir);
        BuildPipeline.BuildAssetBundles(outDir, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        Debug.Log("Wobbly Mods: bundles built in " + outDir);

        if (Directory.Exists(Path.GetDirectoryName(GameBundleFolder)))
        {
            Directory.CreateDirectory(GameBundleFolder);
            foreach (string file in Directory.GetFiles(outDir))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".manifest") || name == "BuiltBundles") continue;
                File.Copy(file, Path.Combine(GameBundleFolder, name), true);
            }
            Debug.Log("Wobbly Mods: copied bundles to " + GameBundleFolder);
        }
        else
        {
            Debug.Log("Wobbly Mods: game folder not found; copy the files in BuiltBundles (not the .manifest files) to " +
                      @"BepInEx\plugins\WobblyMods\bundles\ yourself.");
        }
    }

    // ---------- helpers ----------

    private static void MakeFromSelection(string prefabName, System.Func<string, Color> colorFor)
    {
        GameObject source = Selection.activeObject as GameObject;
        if (source == null)
        {
            EditorUtility.DisplayDialog("Wobbly Mods", "Select the imported model in the Project window first.", "OK");
            return;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Wobbly Mods", "Shader " + ShaderName + " not found. Is Assets/Shaders imported?", "OK");
            return;
        }

        string folder = BundleRoot + "/" + AnimalsBundle;
        Directory.CreateDirectory(folder);
        string materialFolder = folder + "/Materials";
        Directory.CreateDirectory(materialFolder);
        AssetDatabase.Refresh();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = prefabName;
        var made = new Dictionary<Material, Material>();
        var report = new List<string>();

        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material original = mats[i];
                if (original == null) continue;
                Material copy;
                if (!made.TryGetValue(original, out copy))
                {
                    copy = new Material(shader) { name = prefabName + "_" + original.name };
                    if (original.HasProperty("_MainTex") && original.mainTexture != null)
                    {
                        copy.SetTexture("_MainTex", original.mainTexture);
                    }
                    Color c = colorFor(original.name.ToLowerInvariant());
                    copy.SetColor("_Color", c);
                    AssetDatabase.CreateAsset(copy, materialFolder + "/" + copy.name + ".mat");
                    made[original] = copy;
                    report.Add(original.name + " -> " + ColorUtility.ToHtmlStringRGB(c));
                }
                mats[i] = copy;
            }
            r.sharedMaterials = mats;
        }

        string animationNote = SetUpAnimation(instance, source, folder, prefabName);

        string prefabPath = folder + "/" + prefabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();

        Debug.Log("Wobbly Mods: made " + prefabPath + ". " + animationNote + " Material colors:\n  " + string.Join("\n  ", report.ToArray()));
        EditorUtility.DisplayDialog("Wobbly Mods", "Made " + prefabPath +
            "\n\nCheck it in the Scene view. If a part has the wrong color, select the prefab and change that " +
            "material's color, then use Wobbly Mods > Build Bundles.", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    /// <summary>
    /// Makes the model play its own animation in game: prefers a clip named swim, walk or idle.
    /// A legacy Animation component plays automatically. For an Animator, makes a controller with that clip.
    /// </summary>
    private static string SetUpAnimation(GameObject instance, GameObject source, string folder, string prefabName)
    {
        var clips = new List<AnimationClip>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)))
        {
            AnimationClip clip = o as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__")) clips.Add(clip);
        }
        if (clips.Count == 0) return "No animation clips found (it will be still; the mod can wag the tail instead).";

        AnimationClip best = clips[0];
        foreach (string want in new[] { "swim", "walk", "idle" })
        {
            AnimationClip match = clips.Find(c => c.name.ToLowerInvariant().Contains(want));
            if (match != null) { best = match; break; }
        }

        Animation legacy = instance.GetComponentInChildren<Animation>();
        if (legacy != null)
        {
            legacy.clip = best;
            legacy.playAutomatically = true;
            legacy.wrapMode = WrapMode.Loop;
            return "Legacy animation set to '" + best.name + "'.";
        }

        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        string controllerPath = folder + "/" + prefabName + ".controller";
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, best);
        animator.runtimeAnimatorController = controller;
        return "Animator plays '" + best.name + "' (" + clips.Count + " clips found).";
    }

    private static bool Has(string name, params string[] words)
    {
        foreach (string w in words)
        {
            if (name.Contains(w)) return true;
        }
        return false;
    }
}
