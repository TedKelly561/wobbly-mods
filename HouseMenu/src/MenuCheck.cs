using System;
using System.Collections.Generic;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// The menu self-test (parent's idea, 2026-11-21), for a grown-up to run once after an update, with
    /// [Debug] MenuSelfTest = true: about 15 seconds after the world has loaded it places every menu item once in
    /// front of player 1, waits, checks it is still there and hasn't fallen away, removes it, and logs
    /// "Menu check: X ok / Y failed: ids". Failed items go on the hidden list (HiddenItems); items that pass come off it.
    /// </summary>
    internal static class MenuCheck
    {
        private const float StartDelay = 15f, AnswerTimeout = 5f, SettleSeconds = 1.5f;

        private static bool _enabled, _done;
        private static float _readySince = -1f, _stepAt;
        private static int _index = -1;
        private static CatalogItem[] _items;
        private static GameObject _placed;
        private static Vector3 _placedAt;
        private static bool _answered;
        private static string _problem;
        private static int _state; // 0 start next, 1 waiting for the spawn answer, 2 letting it settle
        private static readonly List<string> Ok = new List<string>(), Failed = new List<string>();

        public static void Init(bool enabled)
        {
            _enabled = enabled;
            if (enabled) Plugin.Log.LogInfo("Menu check: the self-test is ON; it starts about 15 s after the world loads.");
        }

        public static void Tick()
        {
            if (!_enabled || _done) return;
            try
            {
                Step();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Menu check: stopped: " + e);
                _done = true;
            }
        }

        private static void Step()
        {
            if (GameBindings.CharacterPosition(0) == null)
            {
                // Player 1 is gone (left the world, or the game is loading). An item that was in the middle of its
                // test is NOT a failure: whatever was placed went away with the world. Tidy up and test the same
                // item again once player 1 is back. (It used to be written down as "nothing appeared" and hidden.)
                if (_state != 0)
                {
                    Plugin.Log.LogInfo("Menu check: interrupted while testing " + _items[_index].Id + "; it will be tested again.");
                    if (_placed != null)
                    {
                        try { ObjectEdits.Remove(_placed); }
                        catch (Exception) { /* it is going away with the world anyway */ }
                    }
                    _placed = null;
                    _state = 0;
                    _index--;
                }
                _readySince = -1f;
                return;
            }
            if (_readySince < 0f) _readySince = Time.unscaledTime;
            if (Time.unscaledTime - _readySince < StartDelay) return;

            if (_items == null)
            {
                _items = FurnitureCatalog.Items;
                Plugin.Log.LogInfo("Menu check: testing " + _items.Length + " items in front of player 1.");
            }

            if (_state == 0)
            {
                _index++;
                if (_index >= _items.Length) { Finish(); return; }
                CatalogItem item = _items[_index];
                _placed = null; _answered = false; _problem = null;
                _stepAt = Time.unscaledTime;
                _state = 1;
                GameBindings.Spawn(item, 0, (placed, problem) =>
                {
                    _answered = true;
                    _placed = placed;
                    _problem = problem;
                    if (placed != null) _placedAt = placed.transform.position;
                    _stepAt = Time.unscaledTime;
                });
            }
            else if (_state == 1)
            {
                if (_answered) { _state = 2; return; }
                if (Time.unscaledTime - _stepAt > AnswerTimeout) Record(false, "no answer from the spawner in " + AnswerTimeout + " s");
            }
            else if (Time.unscaledTime - _stepAt >= SettleSeconds)
            {
                if (_placed == null) Record(false, _problem ?? "nothing appeared (or it was destroyed straight away)");
                else if (!_placed.activeInHierarchy) Record(false, "it switched itself off");
                else if (_placed.GetComponentsInChildren<Renderer>().Length == 0) Record(false, "nothing to see (no renderers)");
                else if ((_placed.transform.position - _placedAt).magnitude > 6f) Record(false, "it moved " + (_placed.transform.position - _placedAt).magnitude.ToString("0.0") + " m (fell or flew away)");
                else Record(true, null);
            }
        }

        private static void Record(bool ok, string why)
        {
            CatalogItem item = _items[_index];
            if (ok)
            {
                Ok.Add(item.Id);
                HiddenItems.Unhide(item.Id); // it works now: let it back into the menu if it was hidden before
            }
            else
            {
                Failed.Add(item.Id);
                Plugin.Log.LogWarning("Menu check: FAILED " + item.Id + " (" + item.DisplayName + "): " + why);
                HiddenItems.Hide(item.Id, "self-test: " + why);
            }
            if (_placed != null)
            {
                try { ObjectEdits.Remove(_placed); }
                catch (Exception e) { Plugin.Log.LogWarning("Menu check: couldn't remove " + item.Id + ": " + e.Message); }
            }
            _placed = null;
            _state = 0;
        }

        private static void Finish()
        {
            _done = true;
            Plugin.Log.LogInfo("Menu check: " + Ok.Count + " ok / " + Failed.Count + " failed" +
                               (Failed.Count > 0 ? ": " + string.Join(", ", Failed.ToArray()) : "") +
                               ". Set [Debug] MenuSelfTest back to false.");
        }
    }
}
