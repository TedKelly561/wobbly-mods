using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using WobblyMods.BuildMode;
using WobblyMods.Core;

namespace WobblyMods.HouseMenu.Build
{
    /// <summary>One split-screen player's build-mode state: their selection, level and ghost.</summary>
    internal sealed class BuildPlayer
    {
        public BuildPlayer(PlayerSession session, BuildCatalog catalog)
        {
            Session = session;
            State = new PlayerBuildState(session.PlayerIndex, catalog, BuildLooks.PaintColors());
        }

        public readonly PlayerSession Session;
        public readonly PlayerBuildState State;

        /// <summary>Cells up (+) or down (-) from the floor in front of the player.</summary>
        public int Level;

        /// <summary>"Build mode: on": keep showing the ghost while the menu is closed or on another tab.</summary>
        public bool KeepGhost;

        /// <summary>True while this player's menu shows the Build tab or one of its pages (set by BuildMenu).</summary>
        public Func<bool> MenuOnBuild;

        public bool HasCursor;

        /// <summary>Set if drawing the ghost threw, so the error is logged once instead of every frame.</summary>
        public bool GhostFailed;

        public GameObject Ghost;
        public MeshRenderer GhostRenderer;
        public string GhostTypeId;

        public int Index
        {
            get { return Session.PlayerIndex; }
        }

        public bool WantsGhost
        {
            get { return !GhostFailed && (KeepGhost || (MenuOnBuild != null && MenuOnBuild())); }
        }
    }

    /// <summary>
    /// The one shared build in the world (every split-screen player builds into the same one, each with
    /// their own undo and redo from BuildMode.Core). Owns the model, the chunk renderer, each player's
    /// ghost, and save/load. Separate from the house menu's spawned props and their UndoStack.
    /// Call <see cref="Update"/> every frame from the plugin.
    /// </summary>
    internal sealed class BuildWorld
    {
        /// <summary>How far in front of the character the ghost sits, in metres.</summary>
        private const float GhostDistance = 2.5f;
        private const int MaxLevel = 12;
        private const int MaxStack = 16;
        public const int SaveSlots = 3;

        private static BuildWorld _instance;

        private readonly BuildCatalog _catalog = BuildCatalog.CreateDefault();
        private readonly BuildRenderer _renderer = new BuildRenderer();
        private readonly Dictionary<int, BuildPlayer> _players = new Dictionary<int, BuildPlayer>();
        private readonly Dictionary<string, Mesh> _ghostMeshes = new Dictionary<string, Mesh>();
        private readonly List<int> _gone = new List<int>();
        private BuildModel _model;
        private BuildEditor _editor;
        private GridSettings _grid = new GridSettings();

        // Until the first block goes down, the grid's height follows the ground in front of the player,
        // so blocks sit on the floor instead of up to a metre above or below it.
        private bool _gridLocked;

        private BuildWorld()
        {
            _model = new BuildModel(_catalog);
            _editor = new BuildEditor(_model);
        }

        public static BuildWorld Instance
        {
            get { return _instance ?? (_instance = new BuildWorld()); }
        }

        public BuildCatalog Catalog
        {
            get { return _catalog; }
        }

        public int BlockCount
        {
            get { return _model.Count; }
        }

        /// <summary>Called when a player's menu is built. Replaces any older state for that player number.</summary>
        public BuildPlayer Register(PlayerSession session)
        {
            BuildPlayer old;
            if (_players.TryGetValue(session.PlayerIndex, out old))
            {
                DestroyGhost(old);
            }
            var player = new BuildPlayer(session, _catalog);
            _players[session.PlayerIndex] = player;
            return player;
        }

        /// <summary>Every frame: drop players who left, redraw dirty chunks, move ghosts.</summary>
        public void Update(SessionManager sessions)
        {
            _gone.Clear();
            foreach (KeyValuePair<int, BuildPlayer> pair in _players)
            {
                if (sessions.Get(pair.Key) != pair.Value.Session)
                {
                    _gone.Add(pair.Key);
                }
            }
            foreach (int i in _gone)
            {
                DestroyGhost(_players[i]);
                _players.Remove(i);
            }

            GridPos near = GridPos.Zero;
            foreach (BuildPlayer p in _players.Values)
            {
                try
                {
                    UpdateGhost(p);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Build: ghost for player " + (p.Index + 1) + " failed: " + e);
                    p.GhostFailed = true;
                    DestroyGhost(p);
                }
                if (p.HasCursor)
                {
                    near = p.State.Cursor;
                }
            }

            _renderer.Update(_model, _grid, near);
        }

        // ---------- Aiming ----------

        /// <summary>
        /// Works out the snapped cell in front of the player: GhostDistance ahead of their character
        /// (along their camera's flat forward), dropped onto whatever is below, plus their Level.
        /// </summary>
        public bool RefreshCursor(BuildPlayer p)
        {
            p.HasCursor = false;
            Camera cam = GameBindings.CameraFor(p.Index);
            if (cam == null)
            {
                return false;
            }

            // UNCERTAIN: GetPlayerPosition may be the hips rather than the feet; the downward ray below
            // finds the real floor either way, starting a little above the character.
            Vector3 origin = GameBindings.CharacterPosition(p.Index) ?? cam.transform.position;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            Vector3 spot = origin + forward * GhostDistance;

            float surfaceY;
            RaycastHit hit;
            Vector3 rayStart = new Vector3(spot.x, origin.y + 1.2f, spot.z);
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                surfaceY = hit.point.y;
            }
            else
            {
                surfaceY = origin.y;
            }

            if (!_gridLocked)
            {
                // Round to a centimetre so the saved origin stays tidy.
                _grid = new GridSettings(_grid.CellSize, new Vec3(0f, Mathf.Round(surfaceY * 100f) / 100f, 0f));
            }

            float cell = _grid.CellSize;
            GridPos c = _grid.WorldToCell(new Vec3(spot.x, surfaceY + cell * 0.5f, spot.z));
            // Sit on top of whatever is already built there, so pressing Place again stacks
            // (the ray can miss a thin wall and hit the floor beside it).
            for (int up = 0; up < MaxStack; up++)
            {
                var above = new GridPos(c.X, c.Y + up, c.Z);
                if (_model.CanPlace(p.State.Block, above, p.State.Rotation) != BuildResult.Overlap)
                {
                    c = above;
                    break;
                }
            }
            p.State.Cursor = new GridPos(c.X, c.Y + p.Level, c.Z);
            p.HasCursor = true;
            return true;
        }

        private void UpdateGhost(BuildPlayer p)
        {
            if (!p.WantsGhost || !RefreshCursor(p))
            {
                if (p.Ghost != null) p.Ghost.SetActive(false);
                return;
            }

            BlockType type = p.State.Block;
            if (p.Ghost == null)
            {
                p.Ghost = new GameObject("Build ghost P" + (p.Index + 1));
                p.Ghost.AddComponent<MeshFilter>();
                p.GhostRenderer = p.Ghost.AddComponent<MeshRenderer>();
                p.GhostRenderer.shadowCastingMode = ShadowCastingMode.Off;
                p.GhostRenderer.receiveShadows = false;
                p.GhostTypeId = null;
            }
            if (p.GhostTypeId != type.Id)
            {
                p.Ghost.GetComponent<MeshFilter>().sharedMesh = GhostMesh(type);
                p.GhostTypeId = type.Id;
            }

            p.Ghost.transform.position = BuildRenderer.ToUnity(_grid.BlockPivot(p.State.Cursor));
            p.Ghost.transform.rotation = Quaternion.Euler(0f, p.State.Rotation.Degrees, 0f);
            bool fits = _model.CanPlace(type, p.State.Cursor, p.State.Rotation) == BuildResult.Ok;
            p.GhostRenderer.sharedMaterial = BuildLooks.Ghost(fits);
            if (!p.Ghost.activeSelf) p.Ghost.SetActive(true);
        }

        private Mesh GhostMesh(BlockType type)
        {
            string key = type.Id + "@" + _grid.CellSize;
            Mesh mesh;
            if (!_ghostMeshes.TryGetValue(key, out mesh) || mesh == null)
            {
                mesh = BuildRenderer.BuildGhostMesh(type, _grid.CellSize);
                _ghostMeshes[key] = mesh;
            }
            return mesh;
        }

        private static void DestroyGhost(BuildPlayer p)
        {
            if (p.Ghost != null)
            {
                UnityEngine.Object.Destroy(p.Ghost);
            }
            p.Ghost = null;
            p.GhostRenderer = null;
            p.GhostTypeId = null;
        }

        /// <summary>
        /// Quick build without the menu (parent's request: "repeat last piece"). Only while that player's
        /// Build mode is on and their menu is closed: tap RB = place the current piece at the ghost,
        /// tap LB = undo their last piece. Returns a message for the log, or null if it didn't apply.
        /// </summary>
        public string QuickAction(int playerIndex, bool place)
        {
            BuildPlayer p;
            if (!_players.TryGetValue(playerIndex, out p) || !p.KeepGhost || p.Session.IsOpen)
            {
                return null;
            }
            return place ? Place(p) : Undo(p);
        }

        // ---------- Edits (each in that player's own undo history) ----------

        public string Place(BuildPlayer p)
        {
            if (!RefreshCursor(p))
            {
                return "Can't find where you are.";
            }
            EditResult r = _editor.Place(p.Index, p.State.CurrentSpec());
            if (r.Ok)
            {
                _gridLocked = true;
                return "Placed " + p.State.Block.DisplayName + ".";
            }
            return r.Message + ".";
        }

        /// <summary>Removes the block at the ghost, or the one just under it (the ghost sits on top of what's there).</summary>
        public string RemoveHere(BuildPlayer p)
        {
            if (!RefreshCursor(p))
            {
                return "Can't find where you are.";
            }
            GridPos cell = p.State.Cursor;
            if (_model.GetBlockAt(cell) == null)
            {
                cell = new GridPos(cell.X, cell.Y - 1, cell.Z);
            }
            EditResult r = _editor.RemoveAt(p.Index, cell);
            return r.Ok ? "Removed " + r.Message.Replace("Remove ", "").ToLowerInvariant() + "." : r.Message + ".";
        }

        public string Undo(BuildPlayer p)
        {
            return _editor.Undo(p.Index).Message + ".";
        }

        public string Redo(BuildPlayer p)
        {
            return _editor.Redo(p.Index).Message + ".";
        }

        public string ChangeLevel(BuildPlayer p, int delta)
        {
            p.Level = Mathf.Clamp(p.Level + delta, -MaxLevel, MaxLevel);
            return p.Level == 0 ? "Building on the floor." : "Building " + (p.Level > 0 ? "up " : "down ") + Mathf.Abs(p.Level) + ".";
        }

        /// <summary>
        /// Takes away every block, as one step in this player's undo (so "Undo build" brings it all back).
        /// Other players' undo for those blocks then says "Someone else changed that".
        /// </summary>
        public string ClearAll(BuildPlayer p)
        {
            EditResult r = _editor.RemoveAll(p.Index, "Clear build");
            return r.Ok ? "Cleared " + r.BlocksChanged + " blocks. Undo build brings them back." : "The build is already empty.";
        }

        // ---------- Save and load ----------

        /// <summary>BepInEx\config\WobblyBuilds\build{slot}.txt</summary>
        public static string SlotPath(int slot)
        {
            return Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, "WobblyBuilds"), "build" + slot + ".txt");
        }

        public static bool SlotExists(int slot)
        {
            try
            {
                return File.Exists(SlotPath(slot));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Save(int slot)
        {
            if (_model.Count == 0)
            {
                return "Build something first.";
            }
            string path = SlotPath(slot);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string text = BuildSerializer.Write(_model, _grid, "Build " + slot);
                // Write to a temporary file first so a crash mid-save can't wreck the old save.
                string temp = path + ".tmp";
                File.WriteAllText(temp, text);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                Plugin.Log.LogInfo("Build: saved " + _model.Count + " blocks to " + path);
                return "Saved Build " + slot + " (" + _model.Count + " blocks).";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Build: saving " + path + " failed: " + e);
                return "Couldn't save Build " + slot + ".";
            }
        }

        /// <summary>
        /// Replaces the whole build with a saved one, in the place it was saved (the file keeps the
        /// grid origin). Everyone's build undo is cleared, because it was about the old build.
        /// </summary>
        public string Load(int slot)
        {
            string path = SlotPath(slot);
            string text;
            try
            {
                if (!File.Exists(path))
                {
                    return "Build " + slot + " is empty.";
                }
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Build: reading " + path + " failed: " + e);
                return "Couldn't open Build " + slot + ".";
            }

            LoadResult result = BuildSerializer.Read(text, _catalog);
            if (!result.Success)
            {
                Plugin.Log.LogWarning("Build: " + path + ": " + result.Error);
                return "Couldn't load Build " + slot + ".";
            }
            foreach (string w in result.Warnings)
            {
                Plugin.Log.LogWarning("Build: " + path + ": " + w);
            }

            _renderer.DestroyAll();
            _model = result.Model;
            _editor = new BuildEditor(_model);
            _grid = result.Grid ?? new GridSettings();
            _gridLocked = _model.Count > 0;
            _model.MarkAllDirty();

            string msg = "Loaded Build " + slot + " (" + result.LoadedBlocks + " blocks)";
            return result.SkippedBlocks > 0 ? msg + ", " + result.SkippedBlocks + " skipped." : msg + ".";
        }
    }
}
