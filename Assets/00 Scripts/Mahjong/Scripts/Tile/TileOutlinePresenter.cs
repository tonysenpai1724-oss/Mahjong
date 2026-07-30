using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Toggles one or more outline-related objects or behaviours for a tile.
    /// </summary>
    public sealed class TileOutlinePresenter : MonoBehaviour
    {
        [SerializeField] private Behaviour[] outlineBehaviours;
        [SerializeField] private GameObject[] outlineObjects;

        /// <summary>
        /// Shows or hides every configured outline target.
        /// </summary>
        /// <param name="isVisible">True to show outlines; otherwise false.</param>
        public void SetOutlineVisible(bool isVisible)
        {
            if (outlineBehaviours != null)
            {
            for (int index = 0; index < outlineBehaviours.Length; index++)
            {
                Behaviour behaviour = outlineBehaviours[index];
                if (behaviour != null)
                {
                    behaviour.enabled = isVisible;
                }
            }
            }

            if (outlineObjects != null)
            {
            for (int index = 0; index < outlineObjects.Length; index++)
            {
                GameObject outlineObject = outlineObjects[index];
                if (outlineObject != null)
                {
                    outlineObject.SetActive(isVisible);
                }
            }
            }
        }
    }
}
