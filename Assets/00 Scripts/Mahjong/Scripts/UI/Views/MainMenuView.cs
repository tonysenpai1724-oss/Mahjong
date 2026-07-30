using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays the main menu actions.
    /// </summary>
    public sealed class MainMenuView : UIScreenView
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;

        /// <summary>
        /// Binds the menu buttons to runtime callbacks.
        /// </summary>
        public void Bind(Action onPlayPressed, Action onLevelSelectPressed)
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => onPlayPressed?.Invoke());
            }

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.RemoveAllListeners();
                levelSelectButton.onClick.AddListener(() => onLevelSelectPressed?.Invoke());
            }
        }
    }
}
