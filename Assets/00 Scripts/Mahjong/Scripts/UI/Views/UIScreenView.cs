using UnityEngine;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Represents a toggleable UI screen backed by a CanvasGroup.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIScreenView : MonoBehaviour
    {
        [SerializeField] private UIScreenType screenType;
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>
        /// Gets the screen type represented by this view.
        /// </summary>
        public UIScreenType ScreenType => screenType;

        /// <summary>
        /// Shows or hides the screen.
        /// </summary>
        /// <param name="isVisible">True to show the screen; otherwise false.</param>
        public virtual void SetVisible(bool isVisible)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;
            }

            gameObject.SetActive(isVisible);
        }
    }
}
