using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Gameplay;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Routes one-shot music and SFX feedback for gameplay interactions.
    /// </summary>
    public sealed class AudioManager : ManagerBehaviour
    {
        [SerializeField] private MahjongAudioSettings audioSettings;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        /// <summary>
        /// Gets a value indicating whether audio output is muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the audio manager.
        /// </summary>
        public override int InitializationOrder => 60;

        /// <summary>
        /// Creates fallback audio sources and applies saved audio settings.
        /// </summary>
        protected override void OnInitialize()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (Context.Services.TryGet(out SaveManager saveManager) && saveManager.CurrentSave != null)
            {
                musicSource.mute = !saveManager.CurrentSave.musicEnabled;
                sfxSource.mute = !saveManager.CurrentSave.soundEnabled;
                IsMuted = sfxSource.mute && musicSource.mute;
            }
        }

        /// <summary>
        /// Updates the runtime mute state.
        /// </summary>
        /// <param name="isMuted">New mute state.</param>
        public void SetMuted(bool isMuted)
        {
            IsMuted = isMuted;
            if (musicSource != null)
            {
                musicSource.mute = isMuted;
            }

            if (sfxSource != null)
            {
                sfxSource.mute = isMuted;
            }
        }

        /// <summary>
        /// Plays the tile selection sound effect.
        /// </summary>
        public void PlaySelect()
        {
            PlaySfx(audioSettings != null ? audioSettings.SelectClip : null);
        }

        /// <summary>
        /// Plays the successful match sound effect.
        /// </summary>
        public void PlayMatch()
        {
            PlaySfx(audioSettings != null ? audioSettings.MatchClip : null);
        }

        /// <summary>
        /// Plays the mismatch sound effect.
        /// </summary>
        public void PlayMismatch()
        {
            PlaySfx(audioSettings != null ? audioSettings.MismatchClip : null);
        }

        /// <summary>
        /// Plays the gameplay win sound effect.
        /// </summary>
        public void PlayWin()
        {
            PlaySfx(audioSettings != null ? audioSettings.WinClip : null);
        }

        /// <summary>
        /// Plays the gameplay lose sound effect.
        /// </summary>
        public void PlayLose()
        {
            PlaySfx(audioSettings != null ? audioSettings.LoseClip : null);
        }

        /// <summary>
        /// Plays the power-up sound associated with the given type.
        /// </summary>
        /// <param name="powerUpType">Power-up type to play.</param>
        public void PlayPowerUp(PowerUpType powerUpType)
        {
            if (audioSettings == null)
            {
                return;
            }

            switch (powerUpType)
            {
                case PowerUpType.Hint:
                    PlaySfx(audioSettings.HintClip);
                    break;
                case PowerUpType.Undo:
                    PlaySfx(audioSettings.UndoClip);
                    break;
                case PowerUpType.Shuffle:
                    PlaySfx(audioSettings.ShuffleClip);
                    break;
                case PowerUpType.Bomb:
                    PlaySfx(audioSettings.BombClip);
                    break;
                case PowerUpType.XRay:
                    PlaySfx(audioSettings.XRayClip);
                    break;
            }
        }

        /// <summary>
        /// Plays a one-shot clip through the shared SFX source.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null || sfxSource.mute)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}
