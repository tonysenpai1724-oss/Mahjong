using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays gameplay progress, currency and power-up actions.
    /// </summary>
    public sealed class GameplayHudView : UIScreenView
    {
        [SerializeField] private Text coinText;
        [SerializeField] private Text progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button bombButton;
        [SerializeField] private Button xrayButton;

        /// <summary>
        /// Binds HUD buttons to runtime callbacks.
        /// </summary>
        public void Bind(Action onPause, Action onHint, Action onUndo, Action onShuffle, Action onBomb, Action onXRay)
        {
            BindButton(pauseButton, onPause);
            BindButton(hintButton, onHint);
            BindButton(undoButton, onUndo);
            BindButton(shuffleButton, onShuffle);
            BindButton(bombButton, onBomb);
            BindButton(xrayButton, onXRay);
        }

        /// <summary>
        /// Updates the visible coin counter.
        /// </summary>
        public void SetCoins(int coins)
        {
            if (coinText != null)
            {
                coinText.text = coins.ToString();
            }
        }

        /// <summary>
        /// Updates the visible progress indicator.
        /// </summary>
        public void SetProgress(int remainingTiles, int totalTiles, float completionRatio)
        {
            if (progressText != null)
            {
                progressText.text = $"{totalTiles - remainingTiles}/{totalTiles}";
            }

            if (progressSlider != null)
            {
                progressSlider.value = Mathf.Clamp01(completionRatio);
            }
        }

        /// <summary>
        /// Binds a UI button when the reference exists.
        /// </summary>
        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
