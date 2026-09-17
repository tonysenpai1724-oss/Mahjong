using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays pause actions during gameplay.
    /// </summary>
    public sealed class PauseMenuView : UIScreenView
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        /// <summary>
        /// Binds pause-menu callbacks.
        /// </summary>
        public void Bind(Action onResume, Action onRetry, Action onHome)
        {
            BindButton(resumeButton, onResume);
            BindButton(retryButton, onRetry);
            BindButton(homeButton, onHome);
        }

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
