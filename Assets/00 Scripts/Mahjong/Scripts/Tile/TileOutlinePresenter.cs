using UnityEngine;
using UnityEngine.Rendering;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Toggles one or more outline-related objects or behaviours for a tile.
    /// Spawns an inverted-hull clone that renders with the outline material.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TileOutlinePresenter : MonoBehaviour
    {
        private const string GeneratedOutlineName = "InvertedHullOutlineRuntime";
        private static readonly int OutlineScalePropertyId = Shader.PropertyToID("_outline_scale");
        private static readonly int OutlineColorPropertyId = Shader.PropertyToID("_outlineColor");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [SerializeField] private Behaviour[] outlineBehaviours;
        [SerializeField] private GameObject[] outlineObjects;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private float outlineScale = 1.1f;
        [SerializeField] private bool showOutlineOnCreate = true;
        [SerializeField] private Color hintOutlineColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color blockedOutlineColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float hintBlinkSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float hintBlinkStrength = 0.3f;

        /// <summary>
        /// Gets the scale multiplier used by the inverted hull outline shader.
        /// </summary>
        public float OutlineScale => outlineScale;

        private GameObject generatedOutlineObject;
        private Material generatedOutlineRuntimeMaterial;
        private Color defaultOutlineColor = Color.black;
        private bool isHintHighlighted;
        private bool isBlockedHighlighted;
        private bool isOutlineVisible = true;

        private void Awake()
        {
            EnsureGeneratedOutline();
            ApplyOutlineAppearance();
            isOutlineVisible = showOutlineOnCreate;
            SetGeneratedOutlineVisible(showOutlineOnCreate);
        }

        private void OnDestroy()
        {
            if (generatedOutlineRuntimeMaterial != null)
            {
                Destroy(generatedOutlineRuntimeMaterial);
            }
        }

        private void Update()
        {
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Shows or hides every configured outline target.
        /// </summary>
        /// <param name="isVisible">True to show outlines; otherwise false.</param>
        public void SetOutlineVisible(bool isVisible)
        {
            EnsureGeneratedOutline();
            isOutlineVisible = isVisible;

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

            SetGeneratedOutlineVisible(isVisible && (!isHintHighlighted || IsHintBlinkVisible()));
        }

        /// <summary>
        /// Enables or disables hint-specific outline feedback.
        /// </summary>
        /// <param name="isHighlighted">True to blink the outline yellow; otherwise false.</param>
        public void SetHintHighlighted(bool isHighlighted)
        {
            isHintHighlighted = isHighlighted;
            ApplyOutlineAppearance();
        }

        /// <summary>
        /// Enables or disables blocked-tap outline feedback.
        /// </summary>
        /// <param name="isHighlighted">True to show the blocked outline; otherwise false.</param>
        public void SetBlockedHighlighted(bool isHighlighted)
        {
            isBlockedHighlighted = isHighlighted;
            ApplyOutlineAppearance();
        }

        private void EnsureGeneratedOutline()
        {
            if (generatedOutlineObject != null || outlineMaterial == null)
            {
                return;
            }

            Transform existingTransform = transform.Find(GeneratedOutlineName);
            if (existingTransform != null)
            {
                generatedOutlineObject = existingTransform.gameObject;

                MeshRenderer existingRenderer = generatedOutlineObject.GetComponent<MeshRenderer>();
                if (existingRenderer != null && existingRenderer.sharedMaterial != null)
                {
                    generatedOutlineRuntimeMaterial = existingRenderer.sharedMaterial;
                    defaultOutlineColor = ResolveOutlineColor(generatedOutlineRuntimeMaterial);
                }

                return;
            }

            MeshFilter sourceMeshFilter = GetComponent<MeshFilter>();
            MeshRenderer sourceMeshRenderer = GetComponent<MeshRenderer>();
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null || sourceMeshRenderer == null)
            {
                return;
            }

            generatedOutlineObject = new GameObject(GeneratedOutlineName);
            generatedOutlineObject.hideFlags = HideFlags.DontSave;
            generatedOutlineObject.transform.SetParent(transform, false);
            generatedOutlineObject.transform.localPosition = Vector3.zero;
            generatedOutlineObject.transform.localRotation = Quaternion.identity;
            generatedOutlineObject.transform.localScale = Vector3.one;
            generatedOutlineObject.layer = gameObject.layer;

            MeshFilter outlineMeshFilter = generatedOutlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer outlineMeshRenderer = generatedOutlineObject.AddComponent<MeshRenderer>();
            generatedOutlineRuntimeMaterial = new Material(outlineMaterial)
            {
                name = outlineMaterial.name + " Runtime"
            };
            defaultOutlineColor = ResolveOutlineColor(generatedOutlineRuntimeMaterial);
            ApplyOutlineScale(generatedOutlineRuntimeMaterial);
            ApplyOutlineColor(generatedOutlineRuntimeMaterial, defaultOutlineColor);

            int subMeshCount = Mathf.Max(1, sourceMeshFilter.sharedMesh.subMeshCount);
            Material[] outlineMaterials = new Material[subMeshCount];
            for (int index = 0; index < subMeshCount; index++)
            {
                outlineMaterials[index] = generatedOutlineRuntimeMaterial;
            }

            outlineMeshRenderer.sharedMaterials = outlineMaterials;
            outlineMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineMeshRenderer.receiveShadows = false;
            outlineMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outlineMeshRenderer.allowOcclusionWhenDynamic = false;
            outlineMeshRenderer.sortingLayerID = sourceMeshRenderer.sortingLayerID;
            outlineMeshRenderer.sortingOrder = sourceMeshRenderer.sortingOrder - 1;
        }

        private void SetGeneratedOutlineVisible(bool isVisible)
        {
            if (generatedOutlineObject != null)
            {
                generatedOutlineObject.SetActive(isVisible);
            }
        }

        private void ApplyOutlineAppearance()
        {
            if (generatedOutlineRuntimeMaterial == null)
            {
                return;
            }

            ApplyOutlineScale(generatedOutlineRuntimeMaterial);

            if (isBlockedHighlighted)
            {
                ApplyOutlineColor(generatedOutlineRuntimeMaterial, blockedOutlineColor);
                SetGeneratedOutlineVisible(isOutlineVisible);
                return;
            }

            if (!isHintHighlighted)
            {
                ApplyOutlineColor(generatedOutlineRuntimeMaterial, defaultOutlineColor);
                SetGeneratedOutlineVisible(isOutlineVisible);
                return;
            }

            ApplyOutlineColor(generatedOutlineRuntimeMaterial, hintOutlineColor);
            SetGeneratedOutlineVisible(isOutlineVisible && IsHintBlinkVisible());
        }

        private bool IsHintBlinkVisible()
        {
            float blinkT = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hintBlinkSpeed * Mathf.PI * 2f);
            return blinkT >= Mathf.Clamp01(hintBlinkStrength);
        }

        private void ApplyOutlineScale(Material targetMaterial)
        {
            if (targetMaterial != null && targetMaterial.HasProperty(OutlineScalePropertyId))
            {
                targetMaterial.SetFloat(OutlineScalePropertyId, outlineScale);
            }
        }

        private Color ResolveOutlineColor(Material targetMaterial)
        {
            if (targetMaterial != null && targetMaterial.HasProperty(OutlineColorPropertyId))
            {
                return targetMaterial.GetColor(OutlineColorPropertyId);
            }

            if (targetMaterial != null && targetMaterial.HasProperty(BaseColorPropertyId))
            {
                return targetMaterial.GetColor(BaseColorPropertyId);
            }

            if (targetMaterial != null && targetMaterial.HasProperty(ColorPropertyId))
            {
                return targetMaterial.GetColor(ColorPropertyId);
            }

            return Color.black;
        }

        private void ApplyOutlineColor(Material targetMaterial, Color color)
        {
            if (targetMaterial == null)
            {
                return;
            }

            if (targetMaterial.HasProperty(OutlineColorPropertyId))
            {
                targetMaterial.SetColor(OutlineColorPropertyId, color);
            }

            if (targetMaterial.HasProperty(BaseColorPropertyId))
            {
                targetMaterial.SetColor(BaseColorPropertyId, color);
            }

            if (targetMaterial.HasProperty(ColorPropertyId))
            {
                targetMaterial.SetColor(ColorPropertyId, color);
            }
        }
    }
}
