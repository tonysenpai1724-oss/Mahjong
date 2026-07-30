using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Utilities;
using System.IO;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns JSON persistence for player progress, currency, settings and unlock state.
    /// </summary>
    public sealed class SaveManager : ManagerBehaviour
    {
        private string saveFilePath;

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
            saveFilePath = Path.Combine(Application.persistentDataPath, "mahjong_out_3d_save.json");
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
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                CurrentSave = JsonUtility.FromJson<PlayerSaveData>(json);
            }

            if (CurrentSave == null)
            {
                CurrentSave = new PlayerSaveData();
                SaveProfile();
            }

            HasLoadedProfile = true;
            Context?.EventBus.Publish(new SaveDataLoadedEvent(CurrentSave));
        }

        /// <summary>
        /// Writes the current save data to disk as JSON.
        /// </summary>
        public void SaveProfile()
        {
            if (CurrentSave == null)
            {
                CurrentSave = new PlayerSaveData();
            }

            string directory = Path.GetDirectoryName(saveFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(CurrentSave, true);
            File.WriteAllText(saveFilePath, json);
            Context?.EventBus.Publish(new SaveDataLoadedEvent(CurrentSave));
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

            EnsureSaveData();
            CurrentSave.coins += amount;
            SaveProfile();
        }

        /// <summary>
        /// Attempts to spend coins from the player profile.
        /// </summary>
        /// <param name="amount">Amount of coins to spend.</param>
        /// <returns>True when the coins were spent; otherwise false.</returns>
        public bool TrySpendCoins(int amount)
        {
            EnsureSaveData();
            if (amount <= 0)
            {
                return true;
            }

            if (CurrentSave.coins < amount)
            {
                return false;
            }

            CurrentSave.coins -= amount;
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
            SaveProfile();
        }

        /// <summary>
        /// Updates the current level index and saves the change.
        /// </summary>
        /// <param name="levelIndex">Current level index.</param>
        public void SetCurrentLevel(int levelIndex)
        {
            EnsureSaveData();
            CurrentSave.currentLevel = Mathf.Max(0, levelIndex);
            SaveProfile();
        }

        /// <summary>
        /// Marks a level as completed and unlocks the next level.
        /// </summary>
        /// <param name="levelIndex">Completed level index.</param>
        public void MarkLevelCompleted(int levelIndex)
        {
            EnsureSaveData();
            if (!CurrentSave.completedLevels.Contains(levelIndex))
            {
                CurrentSave.completedLevels.Add(levelIndex);
            }

            CurrentSave.highestUnlockedLevel = Mathf.Max(CurrentSave.highestUnlockedLevel, levelIndex + 2);
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
            SaveProfile();
        }

        /// <summary>
        /// Clears runtime save state during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            if (HasLoadedProfile)
            {
                SaveProfile();
            }

            HasLoadedProfile = false;
            CurrentSave = null;
            saveFilePath = null;
        }

        /// <summary>
        /// Ensures a valid in-memory save object exists.
        /// </summary>
        private void EnsureSaveData()
        {
            if (CurrentSave == null)
            {
                MahjongRuntimeLogger.LogWarning("SaveManager recreated an empty save profile because none was loaded.");
                CurrentSave = new PlayerSaveData();
            }
        }
    }
}
