using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Drives scale, tint, glow and visibility feedback for a Mahjong tile.
    /// </summary>
    public sealed class TileVisualController : MonoBehaviour
    {
        [SerializeField] private TileVisualSettings settings;
        [SerializeField] private MeshRenderer[] targetRenderers;
        [SerializeField] private TileOutlinePresenter outlinePresenter;
        [SerializeField] private Transform scaleRoot;

        private bool hasInitializedScaleState;
        private Color[] cachedBaseColors;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseScale;
        private Vector3 currentScale;
        private Vector3 targetScale;
        private float scaleXSmoothVelocity;
        private float scaleYSmoothVelocity;
        private float scaleZSmoothVelocity;

        /// <summary>
        /// Applies visual feedback for the specified tile state.
        /// </summary>
        /// <param name="state">New tile state.</param>
        /// <param name="instant">True to snap immediately; otherwise smooth scale transitions.</param>
        public void ApplyState(TileState state, bool instant)
        {
            EnsureInitialized();

            bool shouldRender = state != TileState.Hidden && state != TileState.Removed;
            SetRenderersVisible(shouldRender);
            SetOutlineVisible(state == TileState.Selected);

            targetScale = GetTargetScale(state);
            if (instant)
            {
                currentScale = targetScale;
                ApplyScale(currentScale);
            }

            ApplyMaterialFeedback(state);
        }

        /// <summary>
        /// Gets the primary renderer used by the tile visual.
        /// </summary>
        /// <returns>Primary renderer when available; otherwise null.</returns>
        public MeshRenderer GetPrimaryRenderer()
        {
            EnsureInitialized();
            return targetRenderers != null && targetRenderers.Length > 0 ? targetRenderers[0] : null;
        }

        /// <summary>
        /// Gets the outline presenter used by this tile visual.
        /// </summary>
        /// <returns>Outline presenter when available; otherwise null.</returns>
        public TileOutlinePresenter GetOutlinePresenter()
        {
            return outlinePresenter;
        }

        /// <summary>
        /// Caches references and base material colors once the component wakes.
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Smooths scale feedback every frame.
        /// </summary>
        private void LateUpdate()
        {
            EnsureInitialized();

            float deltaTime = GetDeltaTime();
            currentScale = new Vector3(
                Mathf.SmoothDamp(currentScale.x, targetScale.x, ref scaleXSmoothVelocity, GetScaleSmoothing(), Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(currentScale.y, targetScale.y, ref scaleYSmoothVelocity, GetScaleSmoothing(), Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(currentScale.z, targetScale.z, ref scaleZSmoothVelocity, GetScaleSmoothing(), Mathf.Infinity, deltaTime));

            ApplyScale(currentScale);
        }

        /// <summary>
        /// Auto-caches editor references when values change in the inspector.
        /// </summary>
        private void OnValidate()
        {
            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }

            if (outlinePresenter == null)
            {
                outlinePresenter = GetComponentInChildren<TileOutlinePresenter>(true);
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<MeshRenderer>(true);
            }
        }

        /// <summary>
        /// Ensures cached renderers, colors and scale state are available.
        /// </summary>
        private void EnsureInitialized()
        {
            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<MeshRenderer>(true);
            }

            if (outlinePresenter == null)
            {
                outlinePresenter = GetComponentInChildren<TileOutlinePresenter>(true);
            }

            if (!hasInitializedScaleState)
            {
                baseScale = scaleRoot.localScale;
                currentScale = baseScale;
                targetScale = baseScale;
                hasInitializedScaleState = true;
            }

            if (cachedBaseColors == null || cachedBaseColors.Length != targetRenderers.Length)
            {
                cachedBaseColors = new Color[targetRenderers.Length];
                for (int index = 0; index < targetRenderers.Length; index++)
                {
                    cachedBaseColors[index] = ResolveBaseColor(targetRenderers[index]);
                }
            }
        }

        /// <summary>
        /// Resolves the scale multiplier associated with a tile state.
        /// </summary>
        /// <param name="state">Tile state to evaluate.</param>
        /// <returns>Target local scale vector.</returns>
        private Vector3 GetTargetScale(TileState state)
        {
            float scaleMultiplier = GetVisibleScaleMultiplier();
            if (state == TileState.Selected)
            {
                scaleMultiplier = GetSelectedScaleMultiplier();
            }
            else if (state == TileState.Matched)
            {
                scaleMultiplier = GetMatchedScaleMultiplier();
            }

            return baseScale * scaleMultiplier;
        }

        /// <summary>
        /// Applies state-based tint and glow using material property blocks.
        /// </summary>
        /// <param name="state">Current tile state.</param>
        private void ApplyMaterialFeedback(TileState state)
        {
            for (int index = 0; index < targetRenderers.Length; index++)
            {
                MeshRenderer renderer = targetRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                Color baseColor = cachedBaseColors[index];
                Color tintColor = baseColor;
                Color emissionColor = Color.black;

                if (state == TileState.Selected)
                {
                    tintColor = Color.Lerp(baseColor, GetSelectedTintColor(), GetSelectedTintStrength());
                    emissionColor = GetSelectedEmissionColor() * GetSelectedEmissionIntensity();
                }
                else if (state == TileState.Matched)
                {
                    tintColor = baseColor;
                    emissionColor = GetMatchedEmissionColor() * GetMatchedEmissionIntensity();
                }

                ApplyColorProperty(renderer, GetBaseColorProperty(), tintColor);
                ApplyColorProperty(renderer, GetSecondaryBaseColorProperty(), tintColor);
                ApplyColorProperty(renderer, GetEmissionColorProperty(), emissionColor);
            }
        }

        /// <summary>
        /// Sets a color property when the renderer shader exposes it.
        /// </summary>
        /// <param name="renderer">Renderer to update.</param>
        /// <param name="propertyName">Shader property name.</param>
        /// <param name="color">Color value to assign.</param>
        private void ApplyColorProperty(Renderer renderer, string propertyName, Color color)
        {
            if (string.IsNullOrWhiteSpace(propertyName) || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(propertyName))
            {
                return;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(propertyName, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// Resolves the base tint color from the renderer shared material.
        /// </summary>
        /// <param name="renderer">Renderer to inspect.</param>
        /// <returns>Base color for later highlight blending.</returns>
        private Color ResolveBaseColor(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return Color.white;
            }

            if (!string.IsNullOrWhiteSpace(GetBaseColorProperty()) && renderer.sharedMaterial.HasProperty(GetBaseColorProperty()))
            {
                return renderer.sharedMaterial.GetColor(GetBaseColorProperty());
            }

            if (!string.IsNullOrWhiteSpace(GetSecondaryBaseColorProperty()) && renderer.sharedMaterial.HasProperty(GetSecondaryBaseColorProperty()))
            {
                return renderer.sharedMaterial.GetColor(GetSecondaryBaseColorProperty());
            }

            return Color.white;
        }

        /// <summary>
        /// Shows or hides the outline visual.
        /// </summary>
        /// <param name="isVisible">True to show outline; otherwise false.</param>
        private void SetOutlineVisible(bool isVisible)
        {
            if (outlinePresenter != null)
            {
                outlinePresenter.SetOutlineVisible(isVisible);
            }
        }

        /// <summary>
        /// Shows or hides every configured renderer.
        /// </summary>
        /// <param name="isVisible">True to render; otherwise false.</param>
        private void SetRenderersVisible(bool isVisible)
        {
            for (int index = 0; index < targetRenderers.Length; index++)
            {
                MeshRenderer renderer = targetRenderers[index];
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }

        /// <summary>
        /// Applies the computed local scale to the configured scale root.
        /// </summary>
        /// <param name="scaleValue">Scale value to assign.</param>
        private void ApplyScale(Vector3 scaleValue)
        {
            if (scaleRoot != null)
            {
                scaleRoot.localScale = scaleValue;
            }
        }

        /// <summary>
        /// Gets the scale smoothing duration.
        /// </summary>
        /// <returns>Scale smoothing duration.</returns>
        private float GetScaleSmoothing()
        {
            return settings != null ? settings.ScaleSmoothing : 0.06f;
        }

        /// <summary>
        /// Gets the visible scale multiplier.
        /// </summary>
        /// <returns>Visible scale multiplier.</returns>
        private float GetVisibleScaleMultiplier()
        {
            return settings != null ? settings.VisibleScaleMultiplier : 1f;
        }

        /// <summary>
        /// Gets the selected scale multiplier.
        /// </summary>
        /// <returns>Selected scale multiplier.</returns>
        private float GetSelectedScaleMultiplier()
        {
            return settings != null ? settings.SelectedScaleMultiplier : 1.05f;
        }

        /// <summary>
        /// Gets the matched scale multiplier.
        /// </summary>
        /// <returns>Matched scale multiplier.</returns>
        private float GetMatchedScaleMultiplier()
        {
            return settings != null ? settings.MatchedScaleMultiplier : 1.02f;
        }

        /// <summary>
        /// Gets the selected tint color.
        /// </summary>
        /// <returns>Selected tint color.</returns>
        private Color GetSelectedTintColor()
        {
            return settings != null ? settings.SelectedTintColor : new Color(1f, 0.95f, 0.8f, 1f);
        }

        /// <summary>
        /// Gets the selected emission color.
        /// </summary>
        /// <returns>Selected emission color.</returns>
        private Color GetSelectedEmissionColor()
        {
            return settings != null ? settings.SelectedEmissionColor : new Color(0.95f, 0.8f, 0.35f, 1f);
        }

        /// <summary>
        /// Gets the matched emission color.
        /// </summary>
        /// <returns>Matched emission color.</returns>
        private Color GetMatchedEmissionColor()
        {
            return settings != null ? settings.MatchedEmissionColor : new Color(0.45f, 1f, 0.8f, 1f);
        }

        /// <summary>
        /// Gets the selected tint blend strength.
        /// </summary>
        /// <returns>Selected tint strength.</returns>
        private float GetSelectedTintStrength()
        {
            return settings != null ? settings.SelectedTintStrength : 0.18f;
        }

        /// <summary>
        /// Gets the selected emission intensity.
        /// </summary>
        /// <returns>Selected emission intensity.</returns>
        private float GetSelectedEmissionIntensity()
        {
            return settings != null ? settings.SelectedEmissionIntensity : 1.35f;
        }

        /// <summary>
        /// Gets the matched emission intensity.
        /// </summary>
        /// <returns>Matched emission intensity.</returns>
        private float GetMatchedEmissionIntensity()
        {
            return settings != null ? settings.MatchedEmissionIntensity : 1.75f;
        }

        /// <summary>
        /// Gets the base color property name.
        /// </summary>
        /// <returns>Base color property name.</returns>
        private string GetBaseColorProperty()
        {
            return settings != null ? settings.BaseColorProperty : "_BaseColor";
        }

        /// <summary>
        /// Gets the secondary base color property name.
        /// </summary>
        /// <returns>Secondary base color property name.</returns>
        private string GetSecondaryBaseColorProperty()
        {
            return settings != null ? settings.SecondaryBaseColorProperty : "_Color";
        }

        /// <summary>
        /// Gets the emission color property name.
        /// </summary>
        /// <returns>Emission color property name.</returns>
        private string GetEmissionColorProperty()
        {
            return settings != null ? settings.EmissionColorProperty : "_EmissionColor";
        }

        /// <summary>
        /// Gets the active time step used by the visual controller.
        /// </summary>
        /// <returns>Current delta time.</returns>
        private float GetDeltaTime()
        {
            if (settings != null && settings.UseUnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }

            return Time.deltaTime;
        }
    }
}
