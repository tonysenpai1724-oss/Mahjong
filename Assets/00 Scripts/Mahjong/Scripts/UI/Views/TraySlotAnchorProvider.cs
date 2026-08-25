using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Provides screen-space tray slot anchors from any UI canvas hierarchy.
    /// </summary>
    public sealed class TraySlotAnchorProvider : MonoBehaviour
    {
        [SerializeField] private RectTransform[] traySlotAnchors;
        [SerializeField] private RectTransform previewRoot;
        [SerializeField] private Image capacityWarningImage;

        public Image CapacityWarningImage => capacityWarningImage;

        public RectTransform PreviewRoot
        {
            get
            {
                if (previewRoot != null)
                {
                    return previewRoot;
                }

                if (traySlotAnchors != null && traySlotAnchors.Length > 0 && traySlotAnchors[0] != null)
                {
                    return traySlotAnchors[0].parent as RectTransform;
                }

                return transform as RectTransform;
            }
        }

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
