using System.Diagnostics;
using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.TileSystem;
using TigerForge;
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
        [SerializeField] private AudioSource ambientSource;

        public  static AudioManager persistentInstance;
        private AudioClip activeGameplayMusic;
        private bool hasStartedEarlyAudio;

        /// <summary>
        /// Gets a value indicating whether audio output is muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the audio manager.
        /// </summary>
        public override int InitializationOrder => 60;

        private void Awake()
        {
            if (persistentInstance != null && persistentInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            persistentInstance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }

            DontDestroyOnLoad(gameObject);
            StartEarlyAudio();
             UnityEngine.Debug.Log("AudioManager: Awake complete.");
        }

        /// <summary>
        /// Creates fallback audio sources and applies saved audio settings.
        /// </summary>
        protected override void OnInitialize()
        {
            EnsureAudioSources();

            if (Context != null)
            {
                Context.EventBus.Subscribe<TileTappedEvent>(HandleTileTapped);
                Context.EventBus.Subscribe<LevelGeneratedEvent>(HandleLevelGenerated);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameFlowStateChanged -= HandleGameFlowStateChanged;
                GameManager.Instance.GameFlowStateChanged += HandleGameFlowStateChanged;
            }

            EventManager.StartListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, HandleSettingsChanged);
            ApplyLegacyAudioSettings();
            PlayMenuMusic();
            RefreshGameplayAudioState();
        }

        private void StartEarlyAudio()
        {
            if (hasStartedEarlyAudio)
            {
                return;
            }

            hasStartedEarlyAudio = true;
            EnsureAudioSources();
            ApplyLegacyAudioSettings();
            PlayMenuMusic();
            UnityEngine.Debug.Log("AudioManager: Started early audio playback.");
        }

        private void EnsureAudioSources()
        {
            if (audioSettings == null)
            {
                audioSettings = Resources.Load<MahjongAudioSettings>("MahjongAudioSettings");
            }

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

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

        }

        protected override void OnShutdown()
        {
            if (Context != null)
            {
                Context.EventBus.Unsubscribe<TileTappedEvent>(HandleTileTapped);
                Context.EventBus.Unsubscribe<LevelGeneratedEvent>(HandleLevelGenerated);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameFlowStateChanged -= HandleGameFlowStateChanged;
            }
            EventManager.StopListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, HandleSettingsChanged);

            if (musicSource != null)
            {
                musicSource.Stop();
            }

            if (ambientSource != null)
            {
                ambientSource.Stop();
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

            if (ambientSource != null)
            {
                ambientSource.mute = isMuted;
            }
        }

        private void HandleSettingsChanged()
        {
            ApplyLegacyAudioSettings();
        }

        private void ApplyLegacyAudioSettings()
        {
            bool musicEnabled = IGameSettingController.Instance == null || IGameSettingController.Instance.GetSetting(EGameSetting.Music);
            bool soundEnabled = IGameSettingController.Instance == null || IGameSettingController.Instance.GetSetting(EGameSetting.Sound);

            if (musicSource != null)
            {
                musicSource.mute = !musicEnabled;
            }

            if (sfxSource != null)
            {
                sfxSource.mute = !soundEnabled;
            }

            if (ambientSource != null)
            {
                ambientSource.mute = !soundEnabled;
            }

            IsMuted = !musicEnabled && !soundEnabled;
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
            PlaySfx(audioSettings != null && audioSettings.TileMatchClip != null ? audioSettings.TileMatchClip : audioSettings != null ? audioSettings.MatchClip : null);
        }

        /// <summary>
        /// Plays the mismatch sound effect.
        /// </summary>
        public void PlayMismatch()
        {
            PlayTileSmash();
            UnityEngine.Debug.Log("AudioManager: Played mismatch sound effect.");
        }

        /// <summary>
        /// Plays the tile smash impact sound effect.
        /// </summary>
        public void PlayTileSmash()
        {
            PlaySfx(audioSettings != null && audioSettings.TileSmashClip != null ? audioSettings.TileSmashClip : audioSettings != null ? audioSettings.MismatchClip : null);
        }

        /// <summary>
        /// Plays the tile appear sound effect.
        /// </summary>
        public void PlayTileAppear()
        {
            PlaySfx(audioSettings != null ? audioSettings.TileAppearClip : null);
        }

        /// <summary>
        /// Plays the gameplay win sound effect.
        /// </summary>
        public void PlayWin()
        {
            PlaySfx(audioSettings != null && audioSettings.LevelCompletedClip != null ? audioSettings.LevelCompletedClip : audioSettings != null ? audioSettings.WinClip : null);
        }

        /// <summary>
        /// Plays the gameplay lose sound effect.
        /// </summary>
        public void PlayLose()
        {
            PlaySfx(audioSettings != null && audioSettings.LevelFailedClip != null ? audioSettings.LevelFailedClip : audioSettings != null ? audioSettings.LoseClip : null);
        }

        public void PlayFlipTile()
        {
            PlaySfx(audioSettings != null ? audioSettings.FlipTileClip : null);
        }

        public void PlayCombo(int combo)
        {
            if (audioSettings == null)
            {
                return;
            }

            if (combo < 5 || combo % 5 != 0)
            {
                return;
            }

            AudioClip clip = combo >= 35 ? audioSettings.LegendaryClip
                : combo >= 30 ? audioSettings.BrilliantClip
                : combo >= 25 ? audioSettings.UnbelievableClip
                : combo >= 20 ? audioSettings.WellDoneClip
                : combo >= 15 ? audioSettings.ExcellentClip
                : combo >= 10 ? audioSettings.GreatClip
                : combo >= 5 ? audioSettings.GoodClip
                : null;
            PlaySfx(clip);
        }

        private void HandleTileTapped(TileTappedEvent eventData)
        {
            if (audioSettings != null && audioSettings.MahjongTapClip != null)
            {
                PlaySfx(audioSettings.MahjongTapClip);
            }
        }

        private void HandleLevelGenerated(LevelGeneratedEvent eventData)
        {
            RefreshGameplayAudioState();
        }

        private void HandleGameFlowStateChanged(GameFlowStateChangedEvent eventData)
        {
            RefreshGameplayAudioState();
        }

        private void RefreshGameplayAudioState()
        {
            bool isGameplay = GameManager.Instance != null && GameManager.Instance.CurrentFlowState == GameFlowState.Gameplay;
            if (isGameplay)
            {
                PlayGameplayMusic();
            }
            else
            {
                StopGameplayMusic();
                PlayMenuMusic();
            }
        }

        private void PlayMenuMusic()
        {
            PlayMusic(audioSettings != null ? audioSettings.MenuMusicClip : null);
            StopAmbient();
        }
        public void PlayUISfx()
        {
            PlaySfx(audioSettings != null ? audioSettings.uiClikClip : null);
        }

        private void PlayGameplayMusic()
        {
            if (audioSettings == null || audioSettings.GameplayMusicClips == null || audioSettings.GameplayMusicClips.Length == 0)
            {
                return;
            }

            AudioClip[] clips = audioSettings.GameplayMusicClips;
            AudioClip selectedClip = clips[Random.Range(0, clips.Length)];
            if (selectedClip == null)
            {
                return;
            }

            activeGameplayMusic = selectedClip;
            PlayMusic(selectedClip);
            PlayAmbient();
        }

        private void StopGameplayMusic()
        {
            activeGameplayMusic = null;
            if (musicSource != null)
            {
                musicSource.Stop();
            }
            StopAmbient();
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        private void PlayAmbient()
        {
            if (audioSettings == null || audioSettings.NatureClip == null || ambientSource == null)
            {
                return;
            }

            ambientSource.clip = audioSettings.NatureClip;
            ambientSource.loop = true;
            if (!ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }

        private void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
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
