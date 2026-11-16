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
                if (material == null)
                {
                    continue;
                }

                saved.Add(new KeyValuePair<Material, Color>(material, material.color));
                material.color = color;
            }

            undo.Push("recolored " + target.name, () =>
            {
                foreach (KeyValuePair<Material, Color> pair in saved)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.color = pair.Value;
                    }
                }
            });
        }

        public static void Remove(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            GameBindings.Forget(target);
            Object.Destroy(target);
        }
    }
}
