using MahjongOut3D.Core;
using MahjongOut3D.Gameplay;
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
        private const string PowerUpItemKeyPrefix = "mahjongPowerUp";

        /// <summary>
        /// Gets a value indicating whether the player profile has been loaded.
        /// </summary>
        public bool HasLoadedProfile { get; private set; }

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
        /// Marks the shared player save as available for Mahjong runtime calls.
        /// </summary>
        public void LoadProfile()
        {
            HasLoadedProfile = true;
        }

        /// <summary>
        /// Kept for compatibility; Mahjong now writes directly to the shared save systems.
        /// </summary>
        public void SaveProfile()
        {
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
        /// Gets the saved quantity for a gameplay power-up booster.
        /// </summary>
        public int GetPowerUpCount(PowerUpType powerUpType)
        {
            return IPlayerResource.Instance?.GetItemCount(GetPowerUpItemKey(powerUpType)) ?? 0;
        }

        /// <summary>
        /// Saves an absolute quantity for a gameplay power-up booster.
        /// </summary>
        public void SetPowerUpCount(PowerUpType powerUpType, int count)
        {
            IPlayerResource.Instance?.SetItemCount(GetPowerUpItemKey(powerUpType), count);
        }

        /// <summary>
        /// Adds to a gameplay power-up booster count and saves the result.
        /// </summary>
        public void AddPowerUpCount(PowerUpType powerUpType, int amount)
        {
            IPlayerResource.Instance?.AddItemCount(GetPowerUpItemKey(powerUpType), amount);
        }

        /// <summary>
        /// Attempts to consume one or more saved gameplay power-up boosters.
        /// </summary>
        public bool TrySpendPowerUp(PowerUpType powerUpType, int amount = 1)
        {
            if (amount <= 0)
            {
                return true;
            }

            return IPlayerResource.Instance?.TrySpendItem(GetPowerUpItemKey(powerUpType), amount) ?? false;
        }

        /// <summary>
        /// Updates the selected skin id and saves the change.
        /// </summary>
        /// <param name="skinId">Selected skin identifier.</param>
        public void SetSkin(string skinId)
        {
            string selectedSkin = string.IsNullOrWhiteSpace(skinId) ? "Default" : skinId;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveLocalData(SelectedSkinKey, selectedSkin);
            }
        }

        /// <summary>
        /// Updates runtime audio settings and saves the change.
        /// </summary>
        /// <param name="musicEnabled">True when music is enabled.</param>
        /// <param name="soundEnabled">True when sound is enabled.</param>
        public void SetSettings(bool musicEnabled, bool soundEnabled)
        {
            if (IGameSettingController.Instance != null)
            {
                ApplyLegacySetting(EGameSetting.Music, musicEnabled);
                ApplyLegacySetting(EGameSetting.Sound, soundEnabled);
            }
        }

        /// <summary>
        /// Clears runtime save state during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            HasLoadedProfile = false;
        }

        private static string GetPowerUpItemKey(PowerUpType powerUpType)
        {
            return $"{PowerUpItemKeyPrefix}{powerUpType}";
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
