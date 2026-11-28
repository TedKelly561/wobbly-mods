using UnityEngine;

namespace WobblyMods.Visuals
{
    /// <summary>
    /// Swishes a tail side to side. Faster movement means a faster, bigger wag; standing still
    /// gives a slow, gentle wag. Works on any model with a tail part, with no animation needed:
    /// the catfish's red tail, or the cat's tail.
    ///
    /// Add it to our model after Reskin.Apply, then call Setup with the tail part and the
    /// game object whose speed drives the wag (the vehicle or the player).
    /// </summary>
    public class TailWag : MonoBehaviour
    {
        // How the wag feels. Tuned for a boat-sized catfish; tweak per model.
        public float IdleAngle = 8f;
        public float IdleSpeed = 1.5f;
        public float MaxAngle = 30f;
        public float MaxWagSpeed = 9f;
        public float SpeedForMaxWag = 12f;
        public Vector3 WagAxis = Vector3.up;

        private Transform _tail;
        private Rigidbody _speedSource;
        private Transform _positionSource;
        private Quaternion _restRotation;
        private Vector3 _lastPosition;
        private float _phase;

        /// <param name="tail">The part to swing. Its pivot should be where the tail meets the body.</param>
        /// <param name="mover">The game object whose speed drives the wag. Its Rigidbody is used if it has one.</param>
        public void Setup(Transform tail, GameObject mover)
        {
            _tail = tail;
            _restRotation = tail != null ? tail.localRotation : Quaternion.identity;
            _speedSource = mover != null ? mover.GetComponentInParent<Rigidbody>() : null;
            _positionSource = mover != null ? mover.transform : null;
            _lastPosition = _positionSource != null ? _positionSource.position : Vector3.zero;
        }

        private void Update()
        {
            if (_tail == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            float speed = CurrentSpeed(dt);
            float t = Mathf.Clamp01(speed / SpeedForMaxWag);
            float angle = Mathf.Lerp(IdleAngle, MaxAngle, t);
            float wagSpeed = Mathf.Lerp(IdleSpeed, MaxWagSpeed, t);

            _phase += wagSpeed * dt;
            if (_phase > Mathf.PI * 2f)
            {
                _phase -= Mathf.PI * 2f;
            }

            _tail.localRotation = _restRotation * Quaternion.AngleAxis(Mathf.Sin(_phase) * angle, WagAxis);
        }

        private float CurrentSpeed(float dt)
        {
            if (_speedSource != null)
            {
                return _speedSource.velocity.magnitude;
            }
            if (_positionSource == null)
            {
                return 0f;
            }

            Vector3 now = _positionSource.position;
            float speed = (now - _lastPosition).magnitude / dt;
            _lastPosition = now;
            return speed;
        }

        private void OnDisable()
        {
            if (_tail != null)
            {
                _tail.localRotation = _restRotation;
            }
        }
    }
}
