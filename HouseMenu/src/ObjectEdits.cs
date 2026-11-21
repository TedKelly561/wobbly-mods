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
            // material.color creates a per-object material copy, so other objects that
            // share a material are not changed. The object must use a shader with a "_Color" property.
            var saved = new List<KeyValuePair<Material, Color>>();
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.material;
                if (material == null || material.renderQueue >= 3000)
                {
                    continue; // skip see-through things like tank glass, so they stay see-through
                }

                saved.Add(new KeyValuePair<Material, Color>(material, material.color));
                material.color = color;
            }

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
            });
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
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.material;
                if (material == null || material.renderQueue >= 3000) continue;
                material.color = color;
            }
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
