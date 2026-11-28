using System;
using UnityEngine;
using WobblyMods.Core;
using Object = UnityEngine.Object;

namespace WobblyMods.HouseMenu
{
    internal enum MenuSound
    {
        Tick,   // focus moved
        Pop,    // picked something / placed a piece (happy rising blip)
        Swish,  // back, close, undo
        Chime   // tab changed, menu opened (two rising notes)
    }

    /// <summary>
    /// Fun little menu sounds, made in code (AudioClip.Create), played on a hidden 2D AudioSource.
    ///
    /// UNCERTAIN in game: Wobbly Life plays its audio through FMOD (FMODUnity is loaded). Unity's own audio may be
    /// switched off in the game's settings ("Disable Unity Audio"), and its cameras may carry FMOD's listener
    /// instead of Unity's AudioListener. So:
    /// - The first sound is checked a moment later (is the AudioSource actually playing?) and the result is logged
    ///   once: "Unity audio works" or "seems off, the menu will be silent".
    /// - If no enabled AudioListener exists, a hidden one is added (2D sounds don't care where it is). If the game
    ///   adds its own later, ours is switched off to avoid two listeners.
    /// - Nothing here ever throws into the game; every failure is logged once and sounds just stop.
    /// Turn sounds off with the config entry [Sounds] MenuSounds = false.
    /// </summary>
    internal static class MenuSounds
    {
        private const int Rate = 44100;
        private const float MinGapSeconds = 0.06f; // one sound per action: e.g. "pick" then "back" plays only the pop

        private static bool _enabled;
        private static float _volume = 0.6f;
        private static bool _broken;
        private static GameObject _host;
        private static AudioSource _source;
        private static AudioListener _ourListener;
        private static AudioClip[] _clips;
        private static float _lastPlayed = -10f;
        private static float _nextListenerCheck;
        private static float _checkAt = -1f;
        private static bool _checked;

        public static void Init(bool enabled, float volume)
        {
            _enabled = enabled;
            _volume = Mathf.Clamp01(volume);
            if (!_enabled)
            {
                Plugin.Log.LogInfo("Menu sounds are off (config).");
            }
        }

        /// <summary>Hooks one player's menu so its moves, picks and tab changes make sounds.</summary>
        public static void Attach(PlayerSession session)
        {
            session.Menu.Feedback += OnFeedback;
        }

        public static void OnFeedback(MenuFeedback feedback, MenuItem item)
        {
            switch (feedback)
            {
                case MenuFeedback.Moved:
                    Play(MenuSound.Tick);
                    break;
                case MenuFeedback.TabChanged:
                case MenuFeedback.Opened:
                    Play(MenuSound.Chime);
                    break;
                case MenuFeedback.Selected:
                    Play(item != null && item.Icon != null && item.Icon.StartsWith("sym:undo", StringComparison.Ordinal)
                        ? MenuSound.Swish : MenuSound.Pop);
                    break;
                case MenuFeedback.PageOpened:
                    Play(MenuSound.Pop);
                    break;
                case MenuFeedback.PageBack:
                case MenuFeedback.Closed:
                    Play(MenuSound.Swish);
                    break;
            }
        }

        public static void Play(MenuSound sound)
        {
            if (!_enabled) return;
            float now = Time.unscaledTime;
            if (now - _lastPlayed < MinGapSeconds) return;
            _lastPlayed = now;

            // The game's own sounds through FMOD first (Unity audio is off in Wobbly Life: test 14).
            if (FmodSounds.TryPlay(sound)) return;
            if (_broken) return;

            try
            {
                if (!EnsureReady()) return;
                EnsureListener(now);
                AudioClip clip = _clips[(int)sound];
                if (!_checked && _checkAt < 0f)
                {
                    // The first sound is played with Play() (not PlayOneShot) so isPlaying can be checked shortly after.
                    _source.clip = clip;
                    _source.volume = _volume;
                    _source.Play();
                    _checkAt = now + 0.001f; // checked on the next frame (Tick runs early in Update)
                }
                else
                {
                    _source.PlayOneShot(clip, _volume);
                }
            }
            catch (Exception e)
            {
                _broken = true;
                Plugin.Log.LogWarning("Menu sounds switched off: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>Call every frame: finishes the one-time "does Unity audio work?" check.</summary>
        public static void Tick()
        {
            if (_checked || _checkAt < 0f || _broken || Time.unscaledTime < _checkAt) return;
            _checked = true;
            try
            {
                bool playing = _source != null && (_source.isPlaying || _source.timeSamples > 0);
                AudioConfiguration config = AudioSettings.GetConfiguration();
                string details = "output " + AudioSettings.outputSampleRate + " Hz, speakers " + config.speakerMode +
                                 ", listener: " + (_ourListener != null && _ourListener.enabled ? "ours (the game has none)" : "the game's");
                if (playing)
                {
                    Plugin.Log.LogInfo("Menu sounds: Unity audio works (" + details + ").");
                }
                else
                {
                    Plugin.Log.LogWarning("Menu sounds: Unity audio seems to be off in this game (it uses FMOD), so the menu " +
                                          "will probably be silent (" + details + ").");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Menu sounds: couldn't check Unity audio: " + e.Message);
            }
        }

        private static bool EnsureReady()
        {
            if (_source != null && _clips != null) return true;

            _host = new GameObject("WobblyMenuSounds") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(_host);
            _source = _host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;      // 2D: same in every split-screen view
            _source.ignoreListenerPause = true;
            _source.bypassReverbZones = true;
            _source.priority = 32;

            _clips = new[]
            {
                Make("WobblyTick", ToneTick(0.035f)),
                Make("WobblyPop", TonePop(0.16f)),
                Make("WobblySwish", ToneSwish(0.22f)),
                Make("WobblyChime", ToneChime(0.42f))
            };
            return true;
        }

        private static void EnsureListener(float now)
        {
            if (now < _nextListenerCheck) return;
            _nextListenerCheck = now + 5f;

            bool gameHasOne = false;
            foreach (AudioListener l in Object.FindObjectsOfType<AudioListener>())
            {
                if (l != _ourListener && l.enabled && l.gameObject.activeInHierarchy)
                {
                    gameHasOne = true;
                    break;
                }
            }

            if (gameHasOne)
            {
                if (_ourListener != null && _ourListener.enabled) _ourListener.enabled = false;
            }
            else
            {
                if (_ourListener == null)
                {
                    _ourListener = _host.AddComponent<AudioListener>();
                    Plugin.Log.LogInfo("Menu sounds: no Unity AudioListener in the scene, added a hidden one for menu sounds.");
                }
                _ourListener.enabled = true;
            }
        }

        private static AudioClip Make(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        // ---------- The sounds (mono, -1..1) ----------

        /// <summary>Soft tick: a very short high blip that dies away fast.</summary>
        private static float[] ToneTick(float seconds)
        {
            var s = new float[(int)(Rate * seconds)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 140f) * Mathf.Clamp01(t * 2000f);
                s[i] = 0.35f * env * Mathf.Sin(2f * Mathf.PI * 1700f * t);
            }
            return s;
        }

        /// <summary>Happy pop: a quick upward sweep with a round tone.</summary>
        private static float[] TonePop(float seconds)
        {
            var s = new float[(int)(Rate * seconds)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float k = t / seconds;
                float freq = Mathf.Lerp(420f, 1250f, Mathf.Sqrt(k));
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Clamp01(t * 400f) * Mathf.Pow(1f - k, 2f);
                s[i] = 0.5f * env * (Mathf.Sin(phase) + 0.25f * Mathf.Sin(2f * phase));
            }
            return s;
        }

        /// <summary>Swish: soft noise that sweeps from bright to dull, rising and fading.</summary>
        private static float[] ToneSwish(float seconds)
        {
            var s = new float[(int)(Rate * seconds)];
            var random = new System.Random(7);
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.35f, 0.04f, k); // one-pole low-pass, closing over time
                low += cutoff * (noise - low);
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.7f));
                s[i] = 0.9f * env * low;
            }
            return s;
        }

        /// <summary>Two-note chime going up (C6 then E6), bell-like.</summary>
        private static float[] ToneChime(float seconds)
        {
            var s = new float[(int)(Rate * seconds)];
            AddBell(s, 0f, 1046.5f, 0.32f);
            AddBell(s, 0.11f, 1318.5f, 0.32f);
            return s;
        }

        private static void AddBell(float[] s, float start, float freq, float amp)
        {
            int from = (int)(start * Rate);
            for (int i = from; i < s.Length; i++)
            {
                float t = (i - from) / (float)Rate;
                float env = Mathf.Clamp01(t * 600f) * Mathf.Exp(-t * 11f);
                s[i] += amp * env * (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2.01f * t));
            }
        }
    }
}
