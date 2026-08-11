using UnityEngine;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Provides screen-space tray slot anchors from any UI canvas hierarchy.
    /// </summary>
    public sealed class TraySlotAnchorProvider : MonoBehaviour
    {
        [SerializeField] private RectTransform[] traySlotAnchors;

        public bool TryGetTraySlotAnchor(int slotIndex, out RectTransform slotAnchor)
        {
            slotAnchor = null;
            if (traySlotAnchors == null || traySlotAnchors.Length == 0)
            {
                return false;
            }

            int clampedIndex = Mathf.Clamp(slotIndex, 0, traySlotAnchors.Length - 1);
            slotAnchor = traySlotAnchors[clampedIndex];
            return slotAnchor != null;
        }

        public bool TryGetTraySlotScreenPoint(int slotIndex, out Vector2 screenPoint)
        {
            screenPoint = default;
            if (!TryGetTraySlotAnchor(slotIndex, out RectTransform slotAnchor))
            {
                return false;
            }

            Canvas canvas = slotAnchor.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, slotAnchor.position);
            return true;
        }
    }
}
