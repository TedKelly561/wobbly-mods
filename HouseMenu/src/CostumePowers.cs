using System;
using System.Reflection;
using UnityEngine;

namespace WobblyMods.HouseMenu
{
    /// <summary>
    /// Every costume gives the jetpack boost and the moon-boot double jump (Ted's request, 2026-11-19), without
    /// wearing the game's jetpack or boots. It copies what the game's own ClothingJetpack and ClothingMoonBoots do
    /// (decompiled from Game.dll, 2026-11-19):
    /// - Jetpack: while Jump is held and there is fuel, after each movement step
    ///   PlayerBody.SetRagdollVelocityLerpY(6, fixedDeltaTime * 5). 3.5 s of fuel, refills 1 s per second, and
    ///   after running dry it waits until 1 s is back.
    /// - Moon boots: after a normal jump (PlayerCharacterMovement.onJumpCallback), letting go of Jump and pressing it
    ///   again in the air (at least 0.1 s after the jump) gives PlayerBody.SetRagdollVelocityY(15). Then a short cooldown.
    /// Both hook the movement's own delegates (onPostMovement, onJumpCallback), like the game's clothing does, so the
    /// push lands after the game's own movement step. It sits on the costume's body root, so taking the costume off
    /// (which destroys that root) removes the powers too. Only local players wear costumes, and the game simulates
    /// a local player's body on this PC, so nothing needs sending over the network. It respects the game's switch
    /// for clothing abilities (PlayerController.bServerAllowedCustomsClothingAbilities) when that field is found,
    /// and does nothing while the player's game controls are paused (menu open).
    /// </summary>
    internal sealed class CostumePowers : MonoBehaviour
    {
        private const float JetSpeed = 6f;
        private const float JetFuelMax = 3.5f;
        private const float JetRefillPerSecond = 1f;
        private const float JetRestartFuel = 1f;
        private const float MoonJumpSpeed = 15f;
        private const float MoonMinAirTime = 0.1f;
        private const float MoonCooldown = 1.2f;

        private const string JetSound = "event:/Objects_Space/Objects_Space_FlightSuit_Rocket_Burst_Back";
        private const string MoonSound = "event:/Objects_Space/Objects_Space_FlightSuit_Rocket_Burst_Top";

        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private object _controller;
        private Component _movement;
        private object _rewired;
        private object _inputManager;
        private object _customize;
        private object _interactor;
        private MethodInfo _hasEnteredAction;
        private bool _pressedAgain;
        private bool _broken;

        private FieldInfo _postField;
        private FieldInfo _jumpField;
        private Delegate _postHandler;
        private Delegate _jumpHandler;

        private MethodInfo _getButton;
        private MethodInfo _inputEnabled;
        private MethodInfo _getBody;
        private MethodInfo _isGrounded;
        private MethodInfo _canJump;
        private MethodInfo _lerpY;
        private MethodInfo _setY;
        private FieldInfo _allowedField;

        private float _fuel = JetFuelMax;
        private bool _refilling;
        private bool _jetOn;

        private bool _watchingJump;
        private float _jumpTime;
        private bool _letGo;
        private float _cooldownUntil;

        public static void Give(GameObject costumeRoot, object playerController, Component character, int player)
        {
            var powers = costumeRoot.AddComponent<CostumePowers>();
            string result = powers.Hook(playerController, character);
            Plugin.Log.LogInfo("Dress up: player " + (player + 1) + " powers: " + result);
        }

        private string Hook(object playerController, Component character)
        {
            _controller = playerController;
            _movement = (GameReflection.Call(character, "GetPlayerCharacterMovement")
                         ?? GameReflection.Get(character, "playerCharacterMovement")) as Component;
            if (_movement == null) return "no PlayerCharacterMovement, no powers.";

            _inputManager = GameReflection.InputManagerOf(playerController);
            _rewired = GameReflection.RewiredPlayerOf(playerController);
            if (_rewired == null) return "no Rewired player, no powers.";

            Type rewiredType = _rewired.GetType();
            _getButton = rewiredType.GetMethod("GetButton", new[] { typeof(string) });
            _inputEnabled = _inputManager != null ? _inputManager.GetType().GetMethod("IsGameplayInputEnabled", Any, null, Type.EmptyTypes, null) : null;
            Type movementType = _movement.GetType();
            _getBody = movementType.GetMethod("GetPlayerBody", Any, null, Type.EmptyTypes, null);
            _isGrounded = movementType.GetMethod("IsGrounded", Any, null, Type.EmptyTypes, null);
            _canJump = movementType.GetMethod("CanJump", Any, null, Type.EmptyTypes, null);
            object body = _getBody != null ? _getBody.Invoke(_movement, null) : null;
            if (body != null)
            {
                _lerpY = body.GetType().GetMethod("SetRagdollVelocityLerpY", Any, null, new[] { typeof(float), typeof(float) }, null);
                _setY = body.GetType().GetMethod("SetRagdollVelocityY", Any, null, new[] { typeof(float) }, null);
            }
            _allowedField = playerController.GetType().GetField("bServerAllowedCustomsClothingAbilities", Any);
            // The per-character switch the game's own clothing obeys (off in jobs, races, minigames):
            // CharacterCustomize.bAllowedCustoms, set by ServerSetAllowCustoms.
            _customize = GameReflection.Call(character, "GetPlayerCharacterCustomize");
            // Seated or in a vehicle: the game's clothing ignores Jump while HasEnteredAction() is true.
            _interactor = GameReflection.Call(playerController, "GetPlayerControllerInteractor");
            _hasEnteredAction = _interactor != null ? _interactor.GetType().GetMethod("HasEnteredAction", Any, null, Type.EmptyTypes, null) : null;

            _postField = movementType.GetField("onPostMovement", Any);
            _jumpField = movementType.GetField("onJumpCallback", Any);
            _postHandler = Subscribe(_postField, "OnPostMovement");
            _jumpHandler = Subscribe(_jumpField, "OnJump");

            bool jet = _postHandler != null && _lerpY != null && _getButton != null;
            bool moon = _jumpHandler != null && _setY != null && _getButton != null && _isGrounded != null;
            return "jetpack " + (jet ? "on" : "MISSING") + ", moon jump " + (moon ? "on" : "MISSING") +
                   (_allowedField == null ? " (game's ability switch not found, always allowed)" : "") + ".";
        }

        private Delegate Subscribe(FieldInfo field, string handlerName)
        {
            if (field == null || !typeof(Delegate).IsAssignableFrom(field.FieldType)) return null;
            try
            {
                MethodInfo handler = typeof(CostumePowers).GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance);
                Delegate d = Delegate.CreateDelegate(field.FieldType, this, handler);
                field.SetValue(_movement, Delegate.Combine((Delegate)field.GetValue(_movement), d));
                return d;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Dress up powers: couldn't hook " + field.Name + ": " + e.Message);
                return null;
            }
        }

        private void Unsubscribe(FieldInfo field, Delegate d)
        {
            if (field == null || d == null || _movement == null) return;
            try
            {
                field.SetValue(_movement, Delegate.Remove((Delegate)field.GetValue(_movement), d));
            }
            catch (Exception)
            {
                // the character is going away anyway
            }
        }

        private void OnDestroy()
        {
            Unsubscribe(_postField, _postHandler);
            Unsubscribe(_jumpField, _jumpHandler);
        }

        private bool Allowed()
        {
            if (_allowedField != null)
            {
                object v = _allowedField.GetValue(_controller);
                if (v is bool && !(bool)v) return false;
            }
            if (_customize != null)
            {
                object v = GameReflection.Get(_customize, "bAllowedCustoms");
                if (v is bool && !(bool)v) return false;
            }
            if (_hasEnteredAction != null)
            {
                object v = _hasEnteredAction.Invoke(_interactor, null);
                if (v is bool && (bool)v) return false;
            }
            if (_inputEnabled != null)
            {
                object v = _inputEnabled.Invoke(_inputManager, null);
                if (v is bool && !(bool)v) return false;
            }
            return true;
        }

        private bool JumpHeld()
        {
            if (_getButton == null) return false;
            object v = _getButton.Invoke(_rewired, new object[] { "Jump" });
            return v is bool && (bool)v;
        }

        private object Body()
        {
            return _getBody != null ? _getBody.Invoke(_movement, null) : null;
        }

        private void Update()
        {
            if (_movement == null || _broken) return;
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                Plugin.Log.LogWarning("Dress up powers: stopped (" + inner.GetType().Name + ": " + inner.Message + ").");
                _broken = true;
                _jetOn = false;
            }
        }

        private void Tick()
        {
            bool allowed = Allowed();
            bool held = allowed && JumpHeld();

            // Jetpack fuel, as ClothingJetpack: burn while on, refill while off; after running dry, wait for 1 s.
            bool want = held && _fuel > 0f && !(_refilling && _fuel < JetRestartFuel);
            if (want && !_jetOn) FmodSounds.TryPlayAt(JetSound, transform.position);
            _jetOn = want;
            if (_jetOn) { _fuel -= Time.deltaTime; _refilling = false; }
            else { _fuel += Time.deltaTime * JetRefillPerSecond; _refilling = true; }
            _fuel = Mathf.Clamp(_fuel, 0f, JetFuelMax);

            // Moon boots, as ClothingMoonBoots.JumpCheck: let go of Jump, then press it again while in the air.
            if (_watchingJump)
            {
                bool grounded = Call(_isGrounded) && (_canJump == null || Call(_canJump));
                if (grounded || !allowed) { _watchingJump = false; return; }
                // As the game: the second press is remembered, and fires once 0.1 s has passed and the cooldown is over.
                if (!_letGo) { if (!held) _letGo = true; }
                else if (held) _pressedAgain = true;
                if (_pressedAgain && Time.time - _jumpTime >= MoonMinAirTime && Time.time >= _cooldownUntil)
                {
                    object body = Body();
                    if (body != null && _setY != null)
                    {
                        _setY.Invoke(body, new object[] { MoonJumpSpeed });
                        FmodSounds.TryPlayAt(MoonSound, transform.position);
                        _cooldownUntil = Time.time + MoonCooldown;
                    }
                    _watchingJump = false;
                }
            }
        }

        private bool Call(MethodInfo m)
        {
            object v = m != null ? m.Invoke(_movement, null) : null;
            return v is bool && (bool)v;
        }

        // Called by PlayerCharacterMovement after each movement step (same hook as the game's jetpack).
        private void OnPostMovement(Component movement, Rigidbody rigidbody)
        {
            if (!_jetOn || _lerpY == null || _broken) return;
            try
            {
                object body = Body();
                if (body != null) _lerpY.Invoke(body, new object[] { JetSpeed, Time.fixedDeltaTime * 5f });
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Dress up powers: jetpack push failed, jetpack off: " + e.Message);
                _lerpY = null;
            }
        }

        // Called by PlayerCharacterMovement when the wobbly jumps (same hook as the game's moon boots).
        private void OnJump(Component movement, Rigidbody rigidbody)
        {
            if (_broken) return;
            try { if (!Allowed()) return; } catch (Exception) { return; }
            _watchingJump = true;
            _jumpTime = Time.time;
            _letGo = false;
            _pressedAgain = false;
        }
    }
}
