using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays a simple runtime level selector.
    /// </summary>
    public sealed class LevelSelectView : UIScreenView
    {
        [SerializeField] private Text levelLabel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Text unlockedInfoLabel;

        /// <summary>
        /// Binds level navigation callbacks.
        /// </summary>
        public void Bind(Action onPreviousPressed, Action onNextPressed, Action onPlayPressed)
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveAllListeners();
                previousButton.onClick.AddListener(() => onPreviousPressed?.Invoke());
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => onNextPressed?.Invoke());
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => onPlayPressed?.Invoke());
            }
        }

        /// <summary>
        /// Updates the visible level index text.
        /// </summary>
        public void SetLevelInfo(int currentLevelIndex, int highestUnlockedLevel)
        {
            if (levelLabel != null)
            {
                levelLabel.text = $"Level {currentLevelIndex + 1}";
            }

            if (unlockedInfoLabel != null)
            {
                unlockedInfoLabel.text = $"Unlocked: {Mathf.Max(1, highestUnlockedLevel)}";
            }
        }
    }
}
