using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.Visuals
{
    /// <summary>
    /// Puts a different look on a game object without changing how it works.
    /// The game object's own renderers are hidden (not removed), and our model is attached
    /// as a visual-only child. Physics, colliders, seats, driving and floating stay the game's.
    ///
    /// Used for: the cat (on the player), the catfish boat and car (on game vehicles).
    /// Call Restore() on the returned handle to put the original look back, e.g. from undo.
    /// </summary>
    public static class Reskin
    {
        public sealed class Handle
        {
            private readonly List<Renderer> _hidden;
            private GameObject _model;

            internal Handle(GameObject model, List<Renderer> hidden)
            {
                _model = model;
                _hidden = hidden;
            }

            /// <summary>The attached model, or null after Restore.</summary>
            public GameObject Model
            {
                get { return _model; }
            }

            public bool IsApplied
            {
                get { return _model != null; }
            }

            /// <summary>Removes our model and shows the game's original look again.</summary>
            public void Restore()
            {
                foreach (Renderer r in _hidden)
                {
                    if (r != null)
                    {
                        r.enabled = true;
                    }
                }
                _hidden.Clear();

                if (_model != null)
                {
                    Object.Destroy(_model);
                    _model = null;
                }
            }
        }

        /// <summary>
        /// Hides the target's renderers and attaches a copy of modelPrefab to it.
        /// </summary>
        /// <param name="target">The game object to reskin (a vehicle, or the player's body root).</param>
        /// <param name="modelPrefab">Our model. Any colliders or rigidbodies on it are removed, so it stays visual-only.</param>
        /// <param name="localPosition">Offset from the target, to line the model up.</param>
        /// <param name="localEuler">Rotation, in degrees, to face the model forward.</param>
        /// <param name="scale">Uniform size. 1 = the model's own size.</param>
        public static Handle Apply(GameObject target, GameObject modelPrefab, Vector3 localPosition, Vector3 localEuler, float scale)
        {
            var hidden = new List<Renderer>();
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    hidden.Add(r);
                }
            }

            GameObject model = Object.Instantiate(modelPrefab, target.transform);
            model.name = modelPrefab.name + " (reskin)";
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(localEuler);
            model.transform.localScale = Vector3.one * scale;
            MakeVisualOnly(model);

            return new Handle(model, hidden);
        }

        /// <summary>
        /// Removes physics parts from our model, so the game object's own physics are the only ones.
        /// Rigidbodies go first, because joints and colliders can depend on them.
        /// </summary>
        public static void MakeVisualOnly(GameObject model)
        {
            foreach (Joint j in model.GetComponentsInChildren<Joint>(true))
            {
                Object.DestroyImmediate(j);
            }
            foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(rb);
            }
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
        }

        /// <summary>Finds the first child whose name contains the text (case-insensitive), e.g. "tail".</summary>
        public static Transform FindPart(Transform root, string nameContains)
        {
            string wanted = nameContains.ToLowerInvariant();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLowerInvariant().Contains(wanted))
                {
                    return t;
                }
            }
            return null;
        }
    }
}
