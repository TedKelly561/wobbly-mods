namespace WobblyMods.Core
{
    /// <summary>
    /// Detects a quick tap: the button was pressed and released within MaxSeconds, and the other button
    /// was never pressed meanwhile (so the LB+RB menu combo never counts as a tap).
    /// </summary>
    public sealed class TapWatcher
    {
        public float MaxSeconds = 0.35f;

        private float _downAt = -1f;
        private bool _spoiled;

        /// <returns>True on the frame a valid tap ends.</returns>
        public bool Update(bool down, bool otherDown, float now)
        {
            if (down)
            {
                if (_downAt < 0f)
                {
                    _downAt = now;
                    _spoiled = otherDown;
                }
                if (otherDown) _spoiled = true;
                return false;
            }

            bool tapped = _downAt >= 0f && !_spoiled && now - _downAt <= MaxSeconds;
            _downAt = -1f;
            _spoiled = false;
            return tapped;
        }
    }

}
