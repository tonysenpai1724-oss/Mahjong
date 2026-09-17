using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Drives per-object outline state for the fullscreen outline pipeline.
    /// Legacy inverted-hull helpers are disabled so the screen-space outline remains the only source of outlines.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TileOutlinePresenter : MonoBehaviour
    {
        private const string MatchIndicatorObjectName = "Quad";
        private const string GeneratedOutlineObjectName = "InvertedHullOutlineRuntime";
        private static readonly int OutlineStateColorPropertyId = Shader.PropertyToID("_OutlineStateColor");
        private static readonly int OutlineStateEnabledPropertyId = Shader.PropertyToID("_OutlineStateEnabled");

        [SerializeField] private Behaviour[] outlineBehaviours;
        [SerializeField] private GameObject[] outlineObjects;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private float outlineScale = 1.1f;
        [SerializeField] private bool showOutlineOnCreate = true;
        [Header("Outline Colors")]
        [SerializeField] private Color defaultOutlineColor = Color.white;
        [SerializeField] private Color hintOutlineColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color blockedOutlineColor = new Color(1f, 0.2f, 0.2f, 1f);
        [Header("Hint Blink")]
        [SerializeField] private float hintBlinkSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float hintBlinkStrength = 0.3f;
        [SerializeField] private MeshRenderer[] targetRenderers;

        /// <summary>
        /// Gets the authored outline scale multiplier kept for backward compatibility.
        /// </summary>
        public float OutlineScale => outlineScale;

        /// <summary>
        /// Gets or sets the default outline color used when no temporary state is active.
        /// </summary>
        public Color DefaultOutlineColor
        {
            get => defaultOutlineColor;
            set
            {
                defaultOutlineColor = value;
                ApplyOutlineAppearance();
            }
        }

        /// <summary>
        /// Gets or sets the hint outline color.
        /// </summary>
        public Color HintOutlineColor
        {
            get => hintOutlineColor;
            set
            {
                hintOutlineColor = value;
                ApplyOutlineAppearance();
            }
        }

        /// <summary>
        /// Gets or sets the blocked outline color.
        /// </summary>
        public Color BlockedOutlineColor
        {
            get => blockedOutlineColor;
            set
            {
                blockedOutlineColor = value;
                ApplyOutlineAppearance();
            }
        }

        private readonly List<MeshRenderer> rendererCache = new List<MeshRenderer>(8);
        private MaterialPropertyBlock propertyBlock;
        private bool isHintHighlighted;
        private bool isBlockedHighlighted;
        private bool isOutlineVisible = true;

        private void Awake()
        {
            EnsureInitialized();
            isOutlineVisible = showOutlineOnCreate;
            ApplyOutlineAppearance();
        }

        private void Update()
        {
            ApplyOutlineAppearance();
        }

        private void OnValidate()
        {
            EnsureInitialized();
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Shows or hides the screen-space outline state written by this tile.
        /// </summary>
        public void SetOutlineVisible(bool isVisible)
        {
            isOutlineVisible = isVisible;
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Enables or disables hint-specific outline feedback.
        /// </summary>
        public void SetHintHighlighted(bool isHighlighted)
        {
            isHintHighlighted = isHighlighted;
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Enables or disables blocked-tap outline feedback.
        /// </summary>
        public void SetBlockedHighlighted(bool isHighlighted)
        {
            isBlockedHighlighted = isHighlighted;
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Overrides the default outline color at runtime.
        /// </summary>
        public void SetDefaultOutlineColor(Color color)
        {
            DefaultOutlineColor = color;
        }

        /// <summary>
        /// Overrides the hint outline color at runtime.
        /// </summary>
        public void SetHintOutlineColor(Color color)
        {
            HintOutlineColor = color;
        }

        /// <summary>
        /// Overrides the blocked outline color at runtime.
        /// </summary>
        public void SetBlockedOutlineColor(Color color)
        {
            BlockedOutlineColor = color;
        }

        private void EnsureInitialized()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            DisableLegacyOutlineTargets();
            RefreshTargetRenderers();
        }

        private void DisableLegacyOutlineTargets()
        {
            if (outlineBehaviours != null)
            {
                for (int index = 0; index < outlineBehaviours.Length; index++)
                {
                    Behaviour behaviour = outlineBehaviours[index];
                    if (behaviour != null)
                    {
                        behaviour.enabled = false;
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
                        outlineObject.SetActive(false);
                    }
                }
            }
        }

        private void RefreshTargetRenderers()
        {
            rendererCache.Clear();
            AddRenderers(rendererCache, targetRenderers);
            AddRenderers(rendererCache, GetComponentsInChildren<MeshRenderer>(true));
            targetRenderers = rendererCache.ToArray();
        }

        private void ApplyOutlineAppearance()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                RefreshTargetRenderers();
            }

            Color stateColor = ResolveOutlineColor();
            float stateEnabled = ResolveOutlineEnabled() ? 1f : 0f;

            for (int index = 0; index < targetRenderers.Length; index++)
            {
                MeshRenderer renderer = targetRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(OutlineStateColorPropertyId, stateColor);
                propertyBlock.SetFloat(OutlineStateEnabledPropertyId, stateEnabled);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private Color ResolveOutlineColor()
        {
            if (isBlockedHighlighted)
            {
                return blockedOutlineColor;
            }

            if (isHintHighlighted)
            {
                return hintOutlineColor;
            }

            return defaultOutlineColor;
        }

        private bool ResolveOutlineEnabled()
        {
            if (!isOutlineVisible)
            {
                return false;
            }

            if (!isHintHighlighted)
            {
                return true;
            }

            return IsHintBlinkVisible();
        }

        private bool IsHintBlinkVisible()
        {
            float blinkT = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * hintBlinkSpeed * Mathf.PI * 2f));
            return blinkT >= Mathf.Clamp01(hintBlinkStrength);
        }

        private static void AddRenderers(List<MeshRenderer> destination, MeshRenderer[] source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Length; index++)
            {
                MeshRenderer renderer = source[index];
                if (renderer == null || destination.Contains(renderer))
                {
                    continue;
                }

                string rendererObjectName = renderer.gameObject.name;
                if (rendererObjectName == MatchIndicatorObjectName || rendererObjectName == GeneratedOutlineObjectName)
                {
                    continue;
                }

                destination.Add(renderer);
            }
        }
    }
}
