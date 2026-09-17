using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Plays a short, tier-colored glow burst on the tray and slot images at combo milestones.
    /// </summary>
    public sealed class ComboTrayGlowController : MonoBehaviour
    {
        private const string BaseMapProperty = "_BaseMap";
        private const string BaseColorProperty = "_BaseColor";
        private const string GlowColorProperty = "_GlowColor";
        private const string GradientColorProperty = "_GradientColor";
        private const string GlowModeProperty = "_GlowMode";
        private const string EffectIntensityProperty = "_EffectIntensity";
        private const string RainbowModeProperty = "_RainbowMode";
        private const string RainbowSpeedProperty = "_RainbowSpeed";
        private const string RainbowAngleProperty = "_RainbowAngle";
        private const string RainbowCyclesProperty = "_RainbowCycles";
        private const string RainbowStrengthProperty = "_RainbowStrength";
        private const string FillTintStrengthProperty = "_FillTintStrength";
        private const string BackgroundColorStrengthProperty = "_BackgroundColorStrength";
        private const string GradientStrengthProperty = "_GradientStrength";
        private const string EdgeGlowIntensityProperty = "_EdgeGlowIntensity";
        private const string BloomIntensityProperty = "_BloomIntensity";

        [SerializeField] private TraySlotAnchorProvider traySlotAnchorProvider;
        [SerializeField] private Material glowMaterial;
        [Header("Combo Milestone Colors")]
        [SerializeField] private Color combo5Color = new Color(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private Color combo10Color = new Color(0.65f, 0.25f, 1f, 1f);
        [SerializeField] private Color combo15Color = new Color(1f, 0.72f, 0.12f, 1f);
        [SerializeField] private Color combo20Color = new Color(1f, 0.2f, 0.3f, 1f);
        [SerializeField, Min(5)] private int comboStep = 5;
        [SerializeField, Min(0.1f)] private float milestoneDurationSeconds = 1f;
        [Header("Effect Strength")]
        [SerializeField, Range(0f, 1f)] private float trayTintStrength = 1f;
        [SerializeField, Range(0f, 1f)] private float trayColorStrength = 1f;
        [SerializeField, Range(0f, 1f)] private float trayGradientStrength = 0.45f;
        [SerializeField, Min(0f)] private float slotEdgeGlowIntensity = 2.4f;
        [SerializeField, Min(0f)] private float slotBloomIntensity = 0.5f;
        [Header("Rainbow RGB")]
       [SerializeField, Range(0f, 1f)]
private float rainbowStrength = 1f;

[SerializeField, Min(0f)]
private float rainbowSpeed = 1.35f;

[SerializeField, Range(0f, 360f)]
private float rainbowAngle = 18f;

[SerializeField, Range(0.25f, 2f)]
private float rainbowCycles = 0.75f;

        private readonly List<GlowTarget> glowTargets = new List<GlowTarget>();
        private Coroutine milestoneRoutine;
        private bool isSetup;

        private void Awake()
        {
            CacheGlowTargets();
            RestoreBaseState();
        }

        private void OnEnable()
        {
            CacheGlowTargets();
        }

        private void OnDisable()
        {
            StopMilestoneRoutine();
            RestoreBaseState();
        }

        private void OnDestroy()
        {
            StopMilestoneRoutine();
            RestoreBaseState();
            DestroyRuntimeMaterials();
        }

        /// <summary>
        /// Plays the milestone effect only for combo 5, 10, 15, and so on.
        /// </summary>
        public void SetCombo(int combo)
        {
            CacheGlowTargets();
            if (!isSetup)
            {
                return;
            }

            if (combo <= 0)
            {
                ResetCombo();
                return;
            }

            if (combo % Mathf.Max(1, comboStep) != 0)
            {
                return;
            }

            int milestone = combo;
            int tier = Mathf.Max(1, milestone / Mathf.Max(1, comboStep));

            StopMilestoneRoutine();
            milestoneRoutine = StartCoroutine(PlayMilestoneRoutine(tier));
        }

        /// <summary>
        /// Stops the active burst and restores the original materials immediately.
        /// </summary>
        public void ResetCombo()
        {
            StopMilestoneRoutine();
            RestoreBaseState();
        }

        private IEnumerator PlayMilestoneRoutine(int tier)
        {
            Color primaryColor = ResolveTierColor(tier);
            Color secondaryColor = ResolveTierColor(tier + 1);
            float duration = Mathf.Max(0.1f, milestoneDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float fadeEnvelope = Mathf.Sin(normalizedTime * Mathf.PI);
                float pulse = 0.82f + (0.18f * (0.5f + (0.5f * Mathf.Sin(normalizedTime * Mathf.PI * 4f))));
                float intensity = fadeEnvelope * pulse;
                Color glowColor = Color.Lerp(primaryColor, secondaryColor, normalizedTime * 0.35f);
                ApplyEffect(glowColor, intensity, tier);
                yield return null;
            }

            RestoreBaseState();
            milestoneRoutine = null;
        }

        private void ApplyEffect(Color glowColor, float intensity, int tier)
        {
            for (int index = 0; index < glowTargets.Count; index++)
            {
                GlowTarget target = glowTargets[index];
                if (target.Image == null || target.RuntimeMaterial == null)
                {
                    continue;
                }

                target.Image.material = target.RuntimeMaterial;
                target.RuntimeMaterial.SetColor(GlowColorProperty, glowColor);
                target.RuntimeMaterial.SetColor(GradientColorProperty, ResolveTierColor(2));
                target.RuntimeMaterial.SetFloat(EffectIntensityProperty, intensity);
//                 target.RuntimeMaterial.SetFloat(
//     RainbowModeProperty,
//     tier >= 4 ? 1f : 0f
// );
                 target.RuntimeMaterial.SetFloat(RainbowModeProperty, tier > 4 ? 1f : 0f);
                target.RuntimeMaterial.SetFloat(RainbowStrengthProperty, rainbowStrength);
                target.RuntimeMaterial.SetFloat(RainbowSpeedProperty, rainbowSpeed);
                target.RuntimeMaterial.SetFloat(RainbowAngleProperty, rainbowAngle);
                target.RuntimeMaterial.SetFloat(RainbowCyclesProperty, rainbowCycles);

                if (target.IsTrayBackground)
                {
                    target.RuntimeMaterial.SetFloat(GlowModeProperty, 0f);
                    target.RuntimeMaterial.SetFloat(FillTintStrengthProperty, trayTintStrength);
                    target.RuntimeMaterial.SetFloat(BackgroundColorStrengthProperty, trayColorStrength);
                    target.RuntimeMaterial.SetFloat(GradientStrengthProperty, trayGradientStrength);
                    target.RuntimeMaterial.SetFloat(EdgeGlowIntensityProperty, 0f);
                    target.RuntimeMaterial.SetFloat(BloomIntensityProperty, 0.28f);
                }
                else
                {
                    target.RuntimeMaterial.SetFloat(GlowModeProperty, 1f);
                    target.RuntimeMaterial.SetFloat(FillTintStrengthProperty, 0f);
                    target.RuntimeMaterial.SetFloat(GradientStrengthProperty, 0.86f);
                    target.RuntimeMaterial.SetFloat(EdgeGlowIntensityProperty, slotEdgeGlowIntensity);
                    target.RuntimeMaterial.SetFloat(BloomIntensityProperty, slotBloomIntensity);
                }
            }
        }

        private void CacheGlowTargets()
        {
            if (isSetup)
            {
                return;
            }

            if (traySlotAnchorProvider == null)
            {
                traySlotAnchorProvider = FindAnyObjectByType<TraySlotAnchorProvider>(FindObjectsInactive.Include);
            }

            RectTransform slotRoot = traySlotAnchorProvider != null ? traySlotAnchorProvider.PreviewRoot : null;
            if (slotRoot == null || glowMaterial == null)
            {
                return;
            }

            Image trayBackground = slotRoot.parent != null ? slotRoot.parent.GetComponent<Image>() : null;
            AddGlowTarget(trayBackground, true);

            Image capacityWarningImage = traySlotAnchorProvider.CapacityWarningImage;
            Image[] slotImages = slotRoot.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < slotImages.Length; index++)
            {
                Image slotImage = slotImages[index];
                if (slotImage == null || slotImage.transform.parent != slotRoot || slotImage == capacityWarningImage)
                {
                    continue;
                }

                AddGlowTarget(slotImage, false);
            }

            isSetup = glowTargets.Count > 0;
        }

        private void AddGlowTarget(Image image, bool isTrayBackground)
        {
            if (image == null)
            {
                return;
            }

            Material runtimeMaterial = Instantiate(glowMaterial);
            runtimeMaterial.name = $"{glowMaterial.name} ({image.name} Runtime)";
            runtimeMaterial.SetTexture(BaseMapProperty, image.sprite != null ? image.sprite.texture : null);
            runtimeMaterial.SetColor(BaseColorProperty, image.color);
            glowTargets.Add(new GlowTarget(image, image.material, runtimeMaterial, isTrayBackground));
        }

        private void RestoreBaseState()
        {
            for (int index = 0; index < glowTargets.Count; index++)
            {
                GlowTarget target = glowTargets[index];
                if (target.Image != null)
                {
                    target.Image.material = target.OriginalMaterial;
                }
            }
        }

        private void DestroyRuntimeMaterials()
        {
            for (int index = 0; index < glowTargets.Count; index++)
            {
                if (glowTargets[index].RuntimeMaterial != null)
                {
                    Destroy(glowTargets[index].RuntimeMaterial);
                }
            }

            glowTargets.Clear();
            isSetup = false;
        }

        private void StopMilestoneRoutine()
        {
            if (milestoneRoutine != null)
            {
                StopCoroutine(milestoneRoutine);
                milestoneRoutine = null;
            }
        }

        private Color ResolveTierColor(int tier)
        {
            switch (tier)
            {
                case 1:
                    return combo5Color;
                case 2:
                    return combo10Color;
                case 3:
                    return combo15Color;
                case 4:
                    return combo20Color;
                default:
                    return combo20Color;
            }
        }

        private readonly struct GlowTarget
        {
            public GlowTarget(Image image, Material originalMaterial, Material runtimeMaterial, bool isTrayBackground)
            {
                Image = image;
                OriginalMaterial = originalMaterial;
                RuntimeMaterial = runtimeMaterial;
                IsTrayBackground = isTrayBackground;
            }

            public Image Image { get; }
            public Material OriginalMaterial { get; }
            public Material RuntimeMaterial { get; }
            public bool IsTrayBackground { get; }
        }
    }
}
