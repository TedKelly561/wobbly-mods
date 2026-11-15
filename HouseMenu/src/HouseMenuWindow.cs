using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Big-button menu drawn with Unity's IMGUI. Contains no game-specific code;
    /// anything that touches the game goes through GameBindings.
    /// </summary>
    internal sealed class HouseMenuWindow
    {
        private const float MoveStep = 0.5f;

        private static readonly Color[] Palette =
        {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.black, Color.white, Color.magenta
        };

        private readonly UndoStack _undo = new UndoStack();
        private readonly Rect _panel = new Rect(16, 16, 560, 760);

        private bool _open;
        private int _categoryIndex;
        private int _colorIndex;
        private string _status = "Look at something in your house, then press a button.";

        private GUIStyle _bigButton;
        private GUIStyle _text;

        public void Toggle()
        {
            _open = !_open;
        }

        public void UndoLast()
        {
            string undone = _undo.UndoLast();
            _status = undone != null ? "Undid: " + undone : "Nothing to undo.";
        }

        public void Draw()
        {
            if (!_open)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(_panel, "House Menu");

            GUILayout.BeginArea(new Rect(_panel.x + 16, _panel.y + 40, _panel.width - 32, _panel.height - 56));
            DrawCategoryTabs();
            DrawItemButtons();
            GUILayout.Space(12);
            DrawChangeButtons();
            GUILayout.Space(12);
            DrawUndoButtons();
            GUILayout.Space(12);
            GUILayout.Label(_status, _text);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_bigButton != null)
            {
                return;
            }

            _bigButton = new GUIStyle(GUI.skin.button) { fontSize = 26 };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
        }

        private void DrawCategoryTabs()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < FurnitureCatalog.Categories.Length; i++)
            {
                string name = FurnitureCatalog.Categories[i];
                string label = i == _categoryIndex ? "[ " + name + " ]" : name;
                if (GUILayout.Button(label, _bigButton))
                {
                    _categoryIndex = i;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawItemButtons()
        {
            string category = FurnitureCatalog.Categories[_categoryIndex];
            foreach (CatalogItem item in FurnitureCatalog.InCategory(category))
            {
                if (GUILayout.Button(item.DisplayName, _bigButton, GUILayout.Height(64)))
                {
                    Spawn(item);
                }
            }
        }

        private void DrawChangeButtons()
        {
            GUILayout.Label("Change the thing you're looking at:", _text);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Left", _bigButton)) Nudge(CameraRelative(Vector3.left));
            if (GUILayout.Button("Right", _bigButton)) Nudge(CameraRelative(Vector3.right));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Closer", _bigButton)) Nudge(CameraRelative(Vector3.forward));
            if (GUILayout.Button("Farther", _bigButton)) Nudge(CameraRelative(Vector3.back));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Up", _bigButton)) Nudge(Vector3.up);
            if (GUILayout.Button("Down", _bigButton)) Nudge(Vector3.down);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("New color", _bigButton)) Recolor();
        }

        private void DrawUndoButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Undo", _bigButton)) UndoLast();
            if (GUILayout.Button("Reset house", _bigButton)) ResetHouse();
            GUILayout.EndHorizontal();
        }

        private void Spawn(CatalogItem item)
        {
            GameObject placed = GameBindings.Spawn(item);
            if (placed == null)
            {
                _status = GameBindings.SpawnReady
                    ? "Could not place " + item.DisplayName + "."
                    : "Spawning isn't connected to the game yet.";
                return;
            }

            _undo.Push("placed " + item.DisplayName, () => ObjectEdits.Remove(placed));
            _status = "Placed " + item.DisplayName + ".";
        }

        private void Nudge(Vector3 direction)
        {
            GameObject target = CurrentTarget();
            if (target == null)
            {
                return;
            }

            ObjectEdits.Move(target.transform, direction * MoveStep, _undo);
            _status = "Moved " + target.name + ".";
        }

        private void Recolor()
        {
            GameObject target = CurrentTarget();
            if (target == null)
            {
                return;
            }

            Color color = Palette[_colorIndex];
            _colorIndex = (_colorIndex + 1) % Palette.Length;
            ObjectEdits.SetColor(target, color, _undo);
            _status = "Changed the color of " + target.name + ".";
        }

        private void ResetHouse()
        {
            int undone = _undo.UndoAll();
            _status = undone > 0 ? "Reset the house (" + undone + " changes undone)." : "The house is already normal.";
        }

        /// <summary>Returns the object the player is looking at, or null with a status message.</summary>
        private GameObject CurrentTarget()
        {
            GameObject target = GameBindings.FindTargetInView();
            if (target == null)
            {
                _status = "Look at something in your house first.";
                return null;
            }

            if (!GameBindings.IsEditable(target))
            {
                _status = "That can't be changed yet.";
                return null;
            }

            return target;
        }

        /// <summary>Turns a direction from the camera's point of view into a flat world direction.</summary>
        private static Vector3 CameraRelative(Vector3 local)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return local;
            }

            Vector3 forward = cam.transform.forward;
            forward = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 right = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;

            if (local == Vector3.left) return -right;
            if (local == Vector3.right) return right;
            if (local == Vector3.forward) return forward;
            if (local == Vector3.back) return -forward;
            return local;
        }
    }
}
