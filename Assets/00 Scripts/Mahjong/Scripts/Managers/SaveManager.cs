using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Bridges Mahjong runtime save calls into the project's legacy persistence systems.
    /// </summary>
    public sealed class SaveManager : ManagerBehaviour
    {
        private const string SelectedSkinKey = "mahjong_selected_skin";

        /// <summary>
        /// Gets a value indicating whether the player profile has been loaded.
        /// </summary>
        public bool HasLoadedProfile { get; private set; }

        /// <summary>
        /// Gets the currently loaded save data.
        /// </summary>
        public PlayerSaveData CurrentSave { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the save manager.
        /// </summary>
        public override int InitializationOrder => 5;

        /// <summary>
        /// Loads or creates the player save data during bootstrap.
        /// </summary>
        protected override void OnInitialize()
        {
            LoadProfile();
        }

        /// <summary>
        /// Marks the runtime profile as loaded.
        /// </summary>
        public void MarkProfileLoaded()
        {
            HasLoadedProfile = true;
        }

        /// <summary>
        /// Loads the player save file or creates a new one when none exists.
        /// </summary>
        public void LoadProfile()
        {
            RefreshSnapshot();
            HasLoadedProfile = true;
        }

        /// <summary>
        /// Writes the current save data to disk as JSON.
        /// </summary>
        public void SaveProfile()
        {
            RefreshSnapshot();
        }

        /// <summary>
        /// Adds coins to the player profile and saves the change.
        /// </summary>
        /// <param name="amount">Amount of coins to add.</param>
        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (IPlayerResource.Instance == null)
            {
                MahjongRuntimeLogger.LogWarning("Unable to add coins because legacy player resource is unavailable.");
                return;
            }

            IPlayerResource.Instance.AddResource(new System.Collections.Generic.List<GameResource>
            {
                new CommonResource(ECommonResource.Coin, amount)
            }, EResourceFrom.GameDrop);

            SaveProfile();
        }

        /// <summary>
        /// Attempts to spend coins from the player profile.
        /// </summary>
        /// <param name="amount">Amount of coins to spend.</param>
        /// <returns>True when the coins were spent; otherwise false.</returns>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (IPlayerResource.Instance == null)
            {
                MahjongRuntimeLogger.LogWarning("Unable to spend coins because legacy player resource is unavailable.");
                return false;
            }

            var cost = new System.Collections.Generic.List<GameResource>
            {
                new CommonResource(ECommonResource.Coin, -amount)
            };

            if (!IPlayerResource.Instance.CheckListResource(cost))
            {
                return false;
            }

            IPlayerResource.Instance.AddResource(cost, EResourceFrom.SpendIngame);
            SaveProfile();
            return true;
        }

        /// <summary>
        /// Updates the selected skin id and saves the change.
        /// </summary>
        /// <param name="skinId">Selected skin identifier.</param>
        public void SetSkin(string skinId)
        {
            EnsureSaveData();
            CurrentSave.selectedSkin = string.IsNullOrWhiteSpace(skinId) ? "Default" : skinId;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveLocalData(SelectedSkinKey, CurrentSave.selectedSkin);
            }

            SaveProfile();
        }

        /// <summary>
        /// Updates runtime audio settings and saves the change.
        /// </summary>
        /// <param name="musicEnabled">True when music is enabled.</param>
        /// <param name="soundEnabled">True when sound is enabled.</param>
        public void SetSettings(bool musicEnabled, bool soundEnabled)
        {
            EnsureSaveData();
            CurrentSave.musicEnabled = musicEnabled;
            CurrentSave.soundEnabled = soundEnabled;

            if (IGameSettingController.Instance != null)
            {
                ApplyLegacySetting(EGameSetting.Music, musicEnabled);
                ApplyLegacySetting(EGameSetting.Sound, soundEnabled);
            }

            SaveProfile();
        }

        /// <summary>
        /// Clears runtime save state during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            HasLoadedProfile = false;
            CurrentSave = null;
        }

        /// <summary>
        /// Ensures a valid in-memory save object exists.
        /// </summary>
        private void EnsureSaveData()
        {
            if (CurrentSave == null)
            {
                RefreshSnapshot();
            }
        }

        private void RefreshSnapshot()
        {
            if (CurrentSave == null)
            {
                CurrentSave = new PlayerSaveData();
            }

            long coins = IPlayerResource.Instance != null
                ? IPlayerResource.Instance.GetCommonResource(ECommonResource.Coin)
                : 0;

            long clampedCoins = coins < 0L ? 0L : coins;
            CurrentSave.coins = clampedCoins > int.MaxValue ? int.MaxValue : (int)clampedCoins;
            CurrentSave.musicEnabled = IGameSettingController.Instance == null || IGameSettingController.Instance.GetSetting(EGameSetting.Music);
            CurrentSave.soundEnabled = IGameSettingController.Instance == null || IGameSettingController.Instance.GetSetting(EGameSetting.Sound);
            CurrentSave.selectedSkin = ReadSelectedSkin();
        }

        private static string ReadSelectedSkin()
        {
            if (GameManager.Instance == null)
            {
                return "Default";
            }

            string skinId = GameManager.Instance.GetLocalData(SelectedSkinKey);
            return string.IsNullOrWhiteSpace(skinId) ? "Default" : skinId;
        }

        private static void ApplyLegacySetting(EGameSetting setting, bool enabled)
        {
            if (IGameSettingController.Instance == null)
            {
                return;
            }

            if (IGameSettingController.Instance.GetSetting(setting) != enabled)
            {
                IGameSettingController.Instance.ToggleSetting(setting);
            }
        }
    }
}
