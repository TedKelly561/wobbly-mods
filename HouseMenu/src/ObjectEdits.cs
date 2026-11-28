using System.Collections.Generic;
using UnityEngine;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// The actual changes to house objects. Each one records how to undo itself.
    /// </summary>
    internal static class ObjectEdits
    {
        public static void Move(Transform target, Vector3 offset, UndoStack undo)
        {
            if (target == null)
            {
                return;
            }

            Vector3 before = target.position;
            target.position = before + offset;
            undo.Push("moved " + target.name, () =>
            {
                if (target != null)
                {
                    target.position = before;
                }
            });
        }

        public static void SetColor(GameObject target, Color color, UndoStack undo)
        {
            var saved = new List<KeyValuePair<Material, Color>>();
            var swapped = new List<KeyValuePair<Renderer, Material[]>>();
            Paint(target, color, saved, swapped);

            Color oldColor;
            bool hadColor = GameBindings.ColorOf(target, out oldColor);
            GameBindings.NoteColor(target, true, color);
            undo.Push("recolored " + target.name, () =>
            {
                GameBindings.NoteColor(target, hadColor, oldColor);
                foreach (KeyValuePair<Material, Color> pair in saved)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.color = pair.Value;
                    }
                }
                foreach (KeyValuePair<Renderer, Material[]> pair in swapped)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.sharedMaterials = pair.Value;
                    }
                }
            });
        }

        private static Shader _plainShader;

        /// <summary>
        /// Colors every part of an item. Two ways, because of what the materials are:
        /// - Things this mod builds (lamps, tanks, model-pack furniture) use our plain material, whose color can
        ///   simply be set. material.color makes a per-object copy first, so other objects that share the material
        ///   are not changed. This is how it always worked.
        /// - Things that carry the game's own materials (catalog props and world copies, since they became static
        ///   copies) ignore a color that is set on them (test 2026-11-22, as the paint brush found). They get the
        ///   paint brush's plain material in that color instead; the plain color replaces any texture.
        /// saved and swapped (either may be null) collect what is needed to undo it.
        /// </summary>
        private static void Paint(GameObject target, Color color, List<KeyValuePair<Material, Color>> saved,
                                  List<KeyValuePair<Renderer, Material[]>> swapped)
        {
            if (_plainShader == null) _plainShader = Fun.PaintMaterial(color).shader;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                Material first = renderer.sharedMaterial;
                // A part the paint was already swapped onto stays on the swap way, so the shared paint material itself is never changed.
                bool ours = first == null || (first.shader == _plainShader && !Fun.IsPaint(first));
                if (ours)
                {
                    Material material = renderer.material;
                    if (material == null || material.renderQueue >= 3000)
                    {
                        continue; // skip see-through things like tank glass, so they stay see-through
                    }

                    if (saved != null) saved.Add(new KeyValuePair<Material, Color>(material, material.color));
                    material.color = color;
                    continue;
                }

                if (renderer is ParticleSystemRenderer) continue;
                Material[] old = renderer.sharedMaterials;
                var now = new Material[old.Length];
                bool any = false;
                for (int i = 0; i < old.Length; i++)
                {
                    // Glass and other see-through parts stay as they are.
                    bool seeThrough = old[i] != null && old[i].renderQueue >= 3000;
                    now[i] = seeThrough ? old[i] : Fun.PaintMaterial(color);
                    any |= !seeThrough;
                }
                if (!any) continue;
                if (swapped != null) swapped.Add(new KeyValuePair<Renderer, Material[]>(renderer, old));
                renderer.sharedMaterials = now;
            }
        }

        /// <summary>Turns an item around its up axis by the given degrees.</summary>
        public static void Turn(Transform target, float degrees, UndoStack undo)
        {
            if (target == null) return;
            Quaternion before = target.rotation;
            target.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * before;
            undo.Push("turned " + target.name, () =>
            {
                if (target != null) target.rotation = before;
            });
        }

        /// <summary>Makes an item bigger or smaller (factor &gt; 1 bigger), between 0.25x and 4x its own size.</summary>
        public static bool Resize(GameObject target, float factor, UndoStack undo)
        {
            if (target == null) return false;
            float oldFactor = GameBindings.ScaleOf(target);
            float newFactor = Mathf.Clamp(oldFactor * factor, 0.25f, 4f);
            if (Mathf.Approximately(newFactor, oldFactor)) return false;

            Vector3 before = target.transform.localScale;
            target.transform.localScale = before * (newFactor / oldFactor);
            GameBindings.NoteScale(target, newFactor);
            undo.Push("resized " + target.name, () =>
            {
                if (target == null) return;
                target.transform.localScale = before;
                GameBindings.NoteScale(target, oldFactor);
            });
            return true;
        }

        /// <summary>Paints an item without recording undo (used when loading a saved house).</summary>
        public static void ApplyColor(GameObject target, Color color)
        {
            Paint(target, color, null, null);
            GameBindings.NoteColor(target, true, color);
        }

        public static void Remove(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            GameBindings.Forget(target);
            GameSpawner.Despawn(target);
        }
    }
}
