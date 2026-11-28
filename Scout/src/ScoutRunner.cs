using UnityEngine;

namespace WobblyMods.Scout
{
    /// <summary>
    /// A hidden object that survives scene loads and drives Scout every frame, so Scout keeps working
    /// even if the game destroys BepInEx's own object.
    /// </summary>
    internal sealed class ScoutRunner : MonoBehaviour
    {
        public ScoutPlugin Owner;

        private void Update()
        {
            if (Owner != null)
            {
                Owner.Tick();
            }
        }
    }
}
