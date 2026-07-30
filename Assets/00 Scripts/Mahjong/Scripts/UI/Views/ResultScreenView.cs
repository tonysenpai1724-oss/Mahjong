using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays win or lose state with primary and secondary actions.
    /// </summary>
    public sealed class ResultScreenView : UIScreenView
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Text primaryButtonText;
        [SerializeField] private Text secondaryButtonText;

        /// <summary>
        /// Applies the visible result content.
        /// </summary>
        public void SetResult(bool isWin)
        {
            if (titleText != null)
            {
                titleText.text = isWin ? "You Win" : "No Moves";
            }

            if (subtitleText != null)
            {
                subtitleText.text = isWin ? "Khối Mahjong đã được tháo hoàn toàn." : "Hãy thử lại hoặc dùng power-up để tiếp tục.";
            }

            if (primaryButtonText != null)
            {
                primaryButtonText.text = isWin ? "Next" : "Retry";
            }

            if (secondaryButtonText != null)
            {
                secondaryButtonText.text = "Home";
            }
        }

        /// <summary>
        /// Binds result-screen button callbacks.
        /// </summary>
        public void Bind(Action onPrimaryPressed, Action onSecondaryPressed)
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveAllListeners();
                primaryButton.onClick.AddListener(() => onPrimaryPressed?.Invoke());
            }

            if (secondaryButton != null)
            {
                secondaryButton.onClick.RemoveAllListeners();
                secondaryButton.onClick.AddListener(() => onSecondaryPressed?.Invoke());
            }
        }
    }
}
