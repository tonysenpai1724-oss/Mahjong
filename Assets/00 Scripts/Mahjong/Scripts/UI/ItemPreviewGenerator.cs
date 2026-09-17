using UnityEngine;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Captures isolated 3D objects into transparent preview textures.
    /// Mirrors the Dice ItemPreviewGenerator pattern for Mahjong UI previews.
    /// </summary>
    public sealed class ItemPreviewGenerator : MonoBehaviour
    {
        private const int DefaultPreviewLayer = 31;
        private const int DefaultRenderTextureSize = 512;
        private const string DefaultPreviewLayerName = "3d";

        public Camera previewCamera;
        public RenderTexture renderTexture;
        public string previewLayerName = DefaultPreviewLayerName;
        public Transform previewRoot;
        public Vector3 previewLocalPosition = Vector3.zero;
        public Vector3 previewLocalScale = Vector3.one;
        public Vector3 previewLocalEulerAngles = Vector3.zero;
        [Min(64)] public int maxCameraCaptureSize = DefaultRenderTextureSize;

        [Header("Crop")]
        public bool cropTransparentPixels = true;
        [Range(0f, 0.5f)] public float cropPaddingPercent = 0.08f;
        [Range(0f, 1f)] public float alphaThreshold = 0.02f;

        private bool ownsRenderTexture;
        private static ItemPreviewGenerator instance;

        public static ItemPreviewGenerator Resolve()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<ItemPreviewGenerator>(FindObjectsInactive.Include);
            if (instance != null)
            {
                return instance;
            }

            GameObject generatorObject = new GameObject("Mahjong Item Preview Generator", typeof(ItemPreviewGenerator));
            generatorObject.hideFlags = HideFlags.HideAndDontSave;
            instance = generatorObject.GetComponent<ItemPreviewGenerator>();
            instance.EnsurePreviewResources();
            return instance;
        }

        public Texture2D Capture(GameObject previewPrefab)
        {
            if (previewPrefab == null)
            {
                return null;
            }

            EnsurePreviewResources();
            if (previewCamera == null || renderTexture == null)
            {
                return null;
            }

            Transform root = previewRoot != null ? previewRoot : transform;
            GameObject previewContainer = new GameObject($"{previewPrefab.name} Preview");
            previewContainer.transform.SetParent(root, false);
            previewContainer.transform.localPosition = previewLocalPosition;
            previewContainer.transform.localScale = previewLocalScale;
            previewContainer.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);

            GameObject previewObject = Instantiate(previewPrefab, previewContainer.transform);
            previewObject.SetActive(true);
            previewObject.transform.localPosition = Vector3.zero;
            previewObject.transform.localRotation = Quaternion.identity;

            SetLayerRecursively(previewObject, GetPreviewLayer());
            DisablePreviewPhysics(previewObject);
            CenterPreviewObject(previewContainer.transform, root);

            Texture2D texture = Capture();
            Destroy(previewContainer);
            return texture;
        }

        public Texture2D Capture(Component previewPrefab)
        {
            return previewPrefab != null ? Capture(previewPrefab.gameObject) : null;
        }

        public Texture2D CaptureExisting(GameObject previewObject, bool temporarilyShowRenderers = false)
        {
            return CaptureExisting(previewObject, null, temporarilyShowRenderers);
        }

        public Texture2D CaptureExisting(GameObject previewObject, Camera sourceCamera, bool temporarilyShowRenderers = false)
        {
            if (previewObject == null)
            {
                return null;
            }

            EnsurePreviewResources();
            if (previewCamera == null || renderTexture == null)
            {
                return null;
            }

            LayerSnapshot[] layerSnapshots = CaptureLayerSnapshots(previewObject.transform);
            bool restoreRendererVisibility = temporarilyShowRenderers && !IsAnyRendererVisible(previewObject);
            int previewLayer = GetPreviewLayer();

            try
            {
                if (restoreRendererVisibility)
                {
                    SetRenderersVisible(previewObject, true);
                }

                SetLayerRecursively(previewObject, previewLayer);
                return sourceCamera != null ? CaptureFromCamera(sourceCamera, previewLayer) : Capture();
            }
            finally
            {
                RestoreLayerSnapshots(layerSnapshots);
                if (restoreRendererVisibility)
                {
                    SetRenderersVisible(previewObject, false);
                }
            }
        }

        public Texture2D CaptureFromCamera(Camera sourceCamera)
        {
            return CaptureFromCamera(sourceCamera, GetPreviewLayer());
        }

        private Texture2D CaptureFromCamera(Camera sourceCamera, int previewLayer)
        {
            if (sourceCamera == null)
            {
                return Capture();
            }

            EnsurePreviewResources();
            if (previewCamera == null)
            {
                return null;
            }

            Vector2Int captureSize = ResolveCameraCaptureSize(sourceCamera);
            EnsureRenderTextureSize(captureSize.x, captureSize.y);

            previewCamera.CopyFrom(sourceCamera);
            previewCamera.enabled = false;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << previewLayer;
            return Capture(false);
        }

        public Texture2D Capture()
        {
            return Capture(true);
        }

        private Texture2D Capture(bool normalizeAlpha)
        {
            EnsurePreviewResources();
            if (previewCamera == null || renderTexture == null)
            {
                return null;
            }

            if (!renderTexture.IsCreated())
            {
                renderTexture.Create();
            }

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture currentCameraRT = previewCamera.targetTexture;
            CameraClearFlags currentClearFlags = previewCamera.clearFlags;
            Color currentBackgroundColor = previewCamera.backgroundColor;

            previewCamera.targetTexture = renderTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(
                currentBackgroundColor.r,
                currentBackgroundColor.g,
                currentBackgroundColor.b,
                0f);
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, previewCamera.backgroundColor);

            previewCamera.Render();

            Texture2D texture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGBA32,
                false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            texture.ReadPixels(
                new Rect(0, 0, renderTexture.width, renderTexture.height),
                0,
                0);

            texture.Apply();
            RenderTexture.active = currentRT;
            previewCamera.targetTexture = currentCameraRT;
            previewCamera.clearFlags = currentClearFlags;
            previewCamera.backgroundColor = currentBackgroundColor;

            if (normalizeAlpha)
            {
                NormalizeCapturedAlpha(texture, previewCamera.backgroundColor);
            }

            return cropTransparentPixels ? CropToVisiblePixels(texture) : texture;
        }

        public static Sprite CreateSprite(Texture2D texture)
        {
            return InventoryItemPreview.CreateSprite(texture);
        }

        private void EnsurePreviewResources()
        {
            if (previewRoot == null)
            {
                previewRoot = transform;
            }

            if (previewCamera == null)
            {
                GameObject cameraObject = new GameObject("Preview Camera", typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.transform.SetParent(transform, false);
                cameraObject.transform.localPosition = Vector3.zero;
                cameraObject.transform.localRotation = Quaternion.identity;
                previewCamera = cameraObject.GetComponent<Camera>();
                previewCamera.enabled = false;
                previewCamera.orthographic = false;
                previewCamera.nearClipPlane = 0.01f;
                previewCamera.farClipPlane = 200f;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = Color.clear;
                previewCamera.cullingMask = 1 << DefaultPreviewLayer;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(DefaultRenderTextureSize, DefaultRenderTextureSize, 24, RenderTextureFormat.ARGB32)
                {
                    name = "MahjongItemPreviewRT",
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                ownsRenderTexture = true;
            }
        }

        private void EnsureRenderTextureSize(int width, int height)
        {
            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height)
            {
                return;
            }

            if (renderTexture != null && !ownsRenderTexture)
            {
                return;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
            }

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "MahjongItemPreviewRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            ownsRenderTexture = true;
        }

        private Vector2Int ResolveCameraCaptureSize(Camera sourceCamera)
        {
            int width = Mathf.Max(1, sourceCamera != null && sourceCamera.pixelWidth > 0 ? sourceCamera.pixelWidth : Screen.width);
            int height = Mathf.Max(1, sourceCamera != null && sourceCamera.pixelHeight > 0 ? sourceCamera.pixelHeight : Screen.height);
            int maxSize = Mathf.Max(64, maxCameraCaptureSize);
            int longestSide = Mathf.Max(width, height);
            if (longestSide <= maxSize)
            {
                return new Vector2Int(width, height);
            }

            float scale = maxSize / (float)longestSide;
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }

        private void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
            {
                return;
            }

            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private int GetPreviewLayer()
        {
            string resolvedLayerName = string.IsNullOrWhiteSpace(previewLayerName)
                ? DefaultPreviewLayerName
                : previewLayerName;
            int namedPreviewLayer = LayerMask.NameToLayer(resolvedLayerName);
            if (namedPreviewLayer >= 0)
            {
                return namedPreviewLayer;
            }

            for (int layer = 0; layer < 32; layer++)
            {
                string layerName = LayerMask.LayerToName(layer);
                if (!string.IsNullOrEmpty(layerName) && string.Equals(layerName, resolvedLayerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return layer;
                }
            }

            Debug.LogWarning($"[Mahjong] Preview layer '{resolvedLayerName}' was not found. Falling back to layer {DefaultPreviewLayer}.");
            return DefaultPreviewLayer;
        }

        private void DisablePreviewPhysics(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (Rigidbody rigidbody in target.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }
        }

        private void CenterPreviewObject(Transform previewContainer, Transform root)
        {
            if (previewContainer == null || !TryGetRendererBounds(previewContainer, out Bounds bounds))
            {
                return;
            }

            Vector3 desiredCenter = root != null ? root.TransformPoint(previewLocalPosition) : transform.TransformPoint(previewLocalPosition);
            previewContainer.position += desiredCenter - bounds.center;
        }

        private bool TryGetRendererBounds(Transform target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bounds = default;

            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private void NormalizeCapturedAlpha(Texture2D texture, Color backgroundColor)
        {
            if (texture == null)
            {
                return;
            }

            Color32[] pixels = texture.GetPixels32();
            byte alphaThresholdByte = (byte)Mathf.RoundToInt(alphaThreshold * 255f);
            bool hasVisibleAlpha = false;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > alphaThresholdByte)
                {
                    hasVisibleAlpha = true;
                    break;
                }
            }

            if (hasVisibleAlpha)
            {
                return;
            }

            Color32 background = backgroundColor;
            bool changed = false;
            const int colorThreshold = 4;

            for (int i = 0; i < pixels.Length; i++)
            {
                int diff = Mathf.Abs(pixels[i].r - background.r)
                    + Mathf.Abs(pixels[i].g - background.g)
                    + Mathf.Abs(pixels[i].b - background.b);

                pixels[i].a = diff > colorThreshold ? (byte)255 : (byte)0;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private Texture2D CropToVisiblePixels(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            Color32[] pixels = source.GetPixels32();
            int width = source.width;
            int height = source.height;
            byte threshold = (byte)Mathf.RoundToInt(alphaThreshold * 255f);

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= threshold)
                    {
                        continue;
                    }

                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return source;
            }

            int contentWidth = maxX - minX + 1;
            int contentHeight = maxY - minY + 1;
            int padding = Mathf.RoundToInt(Mathf.Max(contentWidth, contentHeight) * cropPaddingPercent);
            int cropSize = Mathf.Max(contentWidth, contentHeight) + padding * 2;
            cropSize = Mathf.Min(cropSize, Mathf.Max(width, height));

            int centerX = Mathf.RoundToInt((minX + maxX) * 0.5f);
            int centerY = Mathf.RoundToInt((minY + maxY) * 0.5f);
            int maxStartX = Mathf.Max(0, width - cropSize);
            int maxStartY = Mathf.Max(0, height - cropSize);
            int startX = Mathf.Clamp(centerX - cropSize / 2, 0, maxStartX);
            int startY = Mathf.Clamp(centerY - cropSize / 2, 0, maxStartY);
            if (startX == 0 && startY == 0 && cropSize == width && cropSize == height)
            {
                return source;
            }

            Texture2D cropped = new Texture2D(cropSize, cropSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color32[] croppedPixels = new Color32[cropSize * cropSize];
            for (int y = 0; y < cropSize; y++)
            {
                int sourceY = startY + y;
                if (sourceY < 0 || sourceY >= height)
                {
                    continue;
                }

                int sourceX = Mathf.Max(0, startX);
                int destinationX = sourceX - startX;
                int copyWidth = Mathf.Min(cropSize - destinationX, width - sourceX);
                if (copyWidth > 0)
                {
                    System.Array.Copy(
                        pixels,
                        (sourceY * width) + sourceX,
                        croppedPixels,
                        (y * cropSize) + destinationX,
                        copyWidth);
                }
            }

            cropped.SetPixels32(croppedPixels);
            cropped.Apply();
            Destroy(source);
            return cropped;
        }

        private static bool IsAnyRendererVisible(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer != null && renderer.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetRenderersVisible(GameObject target, bool isVisible)
        {
            if (target == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }

        private static LayerSnapshot[] CaptureLayerSnapshots(Transform root)
        {
            if (root == null)
            {
                return new LayerSnapshot[0];
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            LayerSnapshot[] snapshots = new LayerSnapshot[children.Length];
            for (int index = 0; index < children.Length; index++)
            {
                GameObject gameObject = children[index].gameObject;
                snapshots[index] = new LayerSnapshot(gameObject, gameObject.layer);
            }

            return snapshots;
        }

        private static void RestoreLayerSnapshots(LayerSnapshot[] snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            for (int index = 0; index < snapshots.Length; index++)
            {
                LayerSnapshot snapshot = snapshots[index];
                if (snapshot.GameObject != null)
                {
                    snapshot.GameObject.layer = snapshot.Layer;
                }
            }
        }

        private void OnDestroy()
        {
            if (renderTexture != null && ownsRenderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private readonly struct LayerSnapshot
        {
            public LayerSnapshot(GameObject gameObject, int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }

            public int Layer { get; }
        }
    }
}
