using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Renders a per-pixel outline state texture before URP executes the fullscreen outline pass.
    /// Every renderer receives a default white state unless it overrides the values through a MaterialPropertyBlock.
    /// </summary>
    public sealed class OutlineStateRenderSystem
    {
        private const string OutlineStateTextureName = "_OutlineStateTex";
        private const string OutlineWriterShaderName = "Hidden/Mahjong/OutlineStateWriter";
        private static readonly ShaderTagId UniversalForwardShaderTag = new ShaderTagId("UniversalForward");
        private static readonly ShaderTagId UniversalForwardOnlyShaderTag = new ShaderTagId("UniversalForwardOnly");
        private static readonly ShaderTagId SrpDefaultUnlitShaderTag = new ShaderTagId("SRPDefaultUnlit");
        private static readonly ShaderTagId LightweightForwardShaderTag = new ShaderTagId("LightweightForward");
        private static readonly int OutlineStateTexturePropertyId = Shader.PropertyToID(OutlineStateTextureName);
        private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>(4)
        {
            UniversalForwardShaderTag,
            UniversalForwardOnlyShaderTag,
            SrpDefaultUnlitShaderTag,
            LightweightForwardShaderTag,
        };

        private static readonly Dictionary<EntityId, RenderTexture> StateTexturesByCamera = new Dictionary<EntityId, RenderTexture>(4);
        private static Material outlineWriterMaterial;
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
            Application.quitting += CleanupAllTextures;
            isInitialized = true;
        }

        private static void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!IsSupportedCamera(camera) || !EnsureResources())
            {
                return;
            }

            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                return;
            }

            CullingResults cullingResults = context.Cull(ref cullingParameters);
            RenderTexture stateTexture = GetOrCreateStateTexture(camera);
            if (stateTexture == null)
            {
                return;
            }

            context.SetupCameraProperties(camera);

            CommandBuffer commandBuffer = CommandBufferPool.Get("Outline State Prepass");
            commandBuffer.SetRenderTarget(stateTexture);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            DrawOpaqueState(context, camera, cullingResults);

            commandBuffer.SetGlobalTexture(OutlineStateTexturePropertyId, stateTexture);
            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        private static void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (StateTexturesByCamera.TryGetValue(camera.GetEntityId(), out RenderTexture stateTexture) && stateTexture != null)
            {
                Shader.SetGlobalTexture(OutlineStateTexturePropertyId, stateTexture);
            }
        }

        private static void DrawOpaqueState(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
        {
            SortingSettings sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };

            DrawingSettings drawingSettings = new DrawingSettings(ShaderTags[0], sortingSettings)
            {
                perObjectData = PerObjectData.None,
                overrideMaterial = outlineWriterMaterial,
                overrideMaterialPassIndex = 0,
            };

            for (int index = 1; index < ShaderTags.Count; index++)
            {
                drawingSettings.SetShaderPassName(index, ShaderTags[index]);
            }

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, ~0);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private static bool EnsureResources()
        {
            if (outlineWriterMaterial != null)
            {
                return true;
            }

            Shader shader = Shader.Find(OutlineWriterShaderName);
            if (shader == null)
            {
                Debug.LogWarning("OutlineStateRenderSystem could not find Hidden/Mahjong/OutlineStateWriter shader. Ensure it is included in Graphics Settings for player builds.");
                return false;
            }

            outlineWriterMaterial = new Material(shader);
            outlineWriterMaterial.hideFlags = HideFlags.HideAndDontSave;
            return true;
        }

        private static bool IsSupportedCamera(Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            return camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView;
        }

        private static RenderTexture GetOrCreateStateTexture(Camera camera)
        {
            EntityId cameraId = camera.GetEntityId();
            int width = Mathf.Max(1, camera.pixelWidth);
            int height = Mathf.Max(1, camera.pixelHeight);

            if (StateTexturesByCamera.TryGetValue(cameraId, out RenderTexture texture) && texture != null)
            {
                if (texture.width == width && texture.height == height)
                {
                    return texture;
                }

                texture.Release();
                Object.Destroy(texture);
                StateTexturesByCamera.Remove(cameraId);
            }

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 1,
                sRGB = false,
                useMipMap = false,
                autoGenerateMips = false,
            };

            RenderTexture stateTexture = new RenderTexture(descriptor)
            {
                name = $"OutlineStateTex_{cameraId}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            stateTexture.Create();
            StateTexturesByCamera[cameraId] = stateTexture;
            return stateTexture;
        }

        private static void CleanupAllTextures()
        {
            foreach (KeyValuePair<EntityId, RenderTexture> pair in StateTexturesByCamera)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.Release();
                Object.Destroy(pair.Value);
            }

            StateTexturesByCamera.Clear();

            if (outlineWriterMaterial != null)
            {
                Object.Destroy(outlineWriterMaterial);
                outlineWriterMaterial = null;
            }
        }
    }
}
