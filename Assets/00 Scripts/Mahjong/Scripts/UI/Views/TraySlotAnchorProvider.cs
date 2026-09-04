using System.Collections;
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
        [SerializeField] private Color matchingSlotOutlineColor = Color.white;
        [SerializeField] private Sprite matchingSlotOutlineSprite;
        [SerializeField, Min(0.01f)] private float matchingSlotBlinkSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float matchingSlotBlinkMinAlpha = 0.45f;
        [SerializeField, Min(0f)] private float matchingSlotPulseScale = 0.035f;

        private Image[] matchingSlotOutlines;
        private Coroutine matchingSlotBlinkRoutine;

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

        public void ShowMatchingSlotOutline(int slotIndex)
        {
            StopMatchingSlotOutline();
            if (!TryGetTraySlotAnchor(slotIndex, out RectTransform slotAnchor) || slotAnchor == null)
            {
                return;
            }

            EnsureMatchingSlotOutlines();
            if (matchingSlotOutlines == null || slotIndex < 0 || slotIndex >= matchingSlotOutlines.Length)
            {
                return;
            }

            Image outline = matchingSlotOutlines[slotIndex];
            if (outline == null)
            {
                return;
            }

            outline.gameObject.SetActive(true);
            outline.rectTransform.localScale = Vector3.one;
            matchingSlotBlinkRoutine = StartCoroutine(BlinkMatchingSlotOutline(outline));
        }

        public void StopMatchingSlotOutline()
        {
            if (matchingSlotBlinkRoutine != null)
            {
                StopCoroutine(matchingSlotBlinkRoutine);
                matchingSlotBlinkRoutine = null;
            }

            if (matchingSlotOutlines == null)
            {
                return;
            }

            for (int index = 0; index < matchingSlotOutlines.Length; index++)
            {
                if (matchingSlotOutlines[index] != null)
                {
                    matchingSlotOutlines[index].gameObject.SetActive(false);
                }
            }
        }

        private void DestroyMatchingSlotOutlines()
        {
            if (matchingSlotOutlines == null)
            {
                return;
            }

            for (int index = 0; index < matchingSlotOutlines.Length; index++)
            {
                Image outline = matchingSlotOutlines[index];
                if (outline != null)
                {
                    Destroy(outline.gameObject);
                }
            }

            matchingSlotOutlines = null;
        }

        private void EnsureMatchingSlotOutlines()
        {
            if (traySlotAnchors == null)
            {
                matchingSlotOutlines = new Image[0];
                return;
            }

            if (matchingSlotOutlines != null && matchingSlotOutlines.Length == traySlotAnchors.Length)
            {
                return;
            }

            DestroyMatchingSlotOutlines();
            matchingSlotOutlines = new Image[traySlotAnchors.Length];
            for (int index = 0; index < matchingSlotOutlines.Length; index++)
            {
                RectTransform slot = traySlotAnchors[index];
                if (slot == null)
                {
                    continue;
                }

                GameObject outlineObject = new GameObject("Matching Slot Outline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform outlineRect = outlineObject.transform as RectTransform;
                outlineRect.SetParent(slot, false);
                outlineRect.anchorMin = Vector2.zero;
                outlineRect.anchorMax = Vector2.one;
                outlineRect.offsetMin = new Vector2(-6f, -6f);
                outlineRect.offsetMax = new Vector2(6f, 6f);
                outlineRect.SetAsLastSibling();

                Image outline = outlineObject.GetComponent<Image>();
                outline.sprite = matchingSlotOutlineSprite;
                outline.type = Image.Type.Simple;
                outline.preserveAspect = false;
                outline.color = matchingSlotOutlineColor;
                outline.raycastTarget = false;
                outlineObject.SetActive(false);
                matchingSlotOutlines[index] = outline;
            }
        }

        private IEnumerator BlinkMatchingSlotOutline(Image outline)
        {
            while (outline != null)
            {
                float pulse = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * matchingSlotBlinkSpeed * Mathf.PI * 2f));
                Color color = matchingSlotOutlineColor;
                color.a = Mathf.Lerp(matchingSlotBlinkMinAlpha, 1f, pulse);
                outline.color = color;
                float scale = 1f + (pulse * matchingSlotPulseScale);
                outline.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private void OnDisable()
        {
            StopMatchingSlotOutline();
        }

        private void OnDestroy()
        {
            DestroyMatchingSlotOutlines();
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
