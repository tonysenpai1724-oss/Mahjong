using System.Collections;
using System.Collections.Generic;
using MahjongOut3D.Data;
using MahjongOut3D.TileSystem;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Renders a screen-space shard burst using UI images sliced from the matched tile textures.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchUiShardEffect : MonoBehaviour
    {
        private const int OverlaySortingOrder = 500;
        private const float DefaultPixelsPerUnit = 100f;
        private const float MinTextureCropNormalizedSize = 0.18f;
        private const float MaxTextureCropNormalizedSize = 0.38f;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private readonly List<RuntimeShard> activeShards = new List<RuntimeShard>();
        private readonly Dictionary<string, Sprite[]> cachedSprites = new Dictionary<string, Sprite[]>();
        private readonly List<Sprite> generatedRuntimeSprites = new List<Sprite>();

        private RectTransform rootRect;
        private Canvas overlayCanvas;
        private Coroutine animationRoutine;

        /// <summary>
        /// Creates a runtime UI shard overlay under the supplied parent transform.
        /// </summary>
        public static MatchUiShardEffect Create(Transform parent)
        {
            GameObject overlayObject = new GameObject(
                "Match UI Shard Effect",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(MatchUiShardEffect));

            if (parent != null && parent.root != null)
            {
                overlayObject.transform.SetParent(parent.root, false);
            }

            overlayObject.transform.localPosition = Vector3.zero;
            overlayObject.transform.localRotation = Quaternion.identity;
            overlayObject.transform.localScale = Vector3.one;

            MatchUiShardEffect effect = overlayObject.GetComponent<MatchUiShardEffect>();
            effect.InitializeOverlay();
            return effect;
        }

        /// <summary>
        /// Plays a split-left split-right shard burst using the two matched tile visuals.
        /// </summary>
        public bool Play(MahjongTile firstTile, MahjongTile secondTile, Camera worldCamera, TileAnimationSettings settings)
        {
            if (firstTile == null || secondTile == null || worldCamera == null || settings == null)
            {
                return false;
            }

            InitializeOverlay();
            RefreshOverlayLayout();

            bool spawnedAnyShard = false;
            spawnedAnyShard |= SpawnTileShards(firstTile, worldCamera, settings, -1f);
            spawnedAnyShard |= SpawnTileShards(secondTile, worldCamera, settings, 1f);

            if (!spawnedAnyShard)
            {
                return false;
            }

            if (animationRoutine == null)
            {
                animationRoutine = StartCoroutine(AnimateShards(settings));
            }

            return true;
        }

        /// <summary>
        /// Prepares the overlay so first-use playback has a valid full-screen layout.
        /// </summary>
        public void Prewarm()
        {
            InitializeOverlay();
            RefreshOverlayLayout();
        }

        /// <summary>
        /// Clears every active shard immediately.
        /// </summary>
        public void Clear()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            for (int index = activeShards.Count - 1; index >= 0; index--)
            {
                DestroyShard(activeShards[index]);
            }

            activeShards.Clear();
        }

        private void OnDestroy()
        {
            Clear();

            foreach (KeyValuePair<string, Sprite[]> entry in cachedSprites)
            {
                Sprite[] sprites = entry.Value;
                if (sprites == null)
                {
                    continue;
                }

                for (int index = 0; index < sprites.Length; index++)
                {
                    if (sprites[index] != null)
                    {
                        Destroy(sprites[index]);
                    }
                }
            }

            cachedSprites.Clear();

            for (int index = generatedRuntimeSprites.Count - 1; index >= 0; index--)
            {
                if (generatedRuntimeSprites[index] != null)
                {
                    Destroy(generatedRuntimeSprites[index]);
                }
            }

            generatedRuntimeSprites.Clear();
        }

        private void InitializeOverlay()
        {
            if (overlayCanvas != null && rootRect != null)
            {
                return;
            }

            overlayCanvas = GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = OverlaySortingOrder;
            overlayCanvas.pixelPerfect = false;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                rootRect = gameObject.GetComponent<RectTransform>();
            }

            if (rootRect == null)
            {
                return;
            }

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
            rootRect.anchoredPosition = Vector2.zero;

            RefreshOverlayLayout();
        }

        private void RefreshOverlayLayout()
        {
            if (rootRect == null)
            {
                return;
            }

            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
            Canvas.ForceUpdateCanvases();
        }

        private bool SpawnTileShards(MahjongTile tile, Camera worldCamera, TileAnimationSettings settings, float horizontalDirection)
        {
            if (tile == null || rootRect == null)
            {
                return false;
            }

            Texture2D sourceTexture = ResolveSourceTexture(tile);
            if (sourceTexture == null)
            {
                sourceTexture = Texture2D.whiteTexture;
            }

            if (!TryConvertWorldToAnchoredPosition(worldCamera, tile.transform.position, out Vector2 centerPosition))
            {
                return false;
            }

            Vector2 effectSize = EstimateTileScreenSize(tile, worldCamera, sourceTexture, settings.MatchUiShardScale);

            Sprite[] irregularSprites = settings.MatchUiShardSprites;
            if (irregularSprites != null && irregularSprites.Length > 0)
            {
                return SpawnIrregularShards(tile, centerPosition, effectSize, sourceTexture, settings, irregularSprites, horizontalDirection);
            }

            int rows = Mathf.Max(1, settings.MatchUiShardRows);
            int columns = Mathf.Max(1, settings.MatchUiShardColumns);
            Sprite[] sprites = GetOrCreateSprites(sourceTexture, rows, columns);
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            float shardWidth = effectSize.x / columns;
            float shardHeight = effectSize.y / rows;
            float left = centerPosition.x - (effectSize.x * 0.5f);
            float top = centerPosition.y + (effectSize.y * 0.5f);
            int shardIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (shardIndex >= sprites.Length)
                    {
                        break;
                    }

                    Sprite sprite = sprites[shardIndex];
                    if (sprite == null)
                    {
                        shardIndex++;
                        continue;
                    }

                    GameObject shardObject = new GameObject("UI Shard", typeof(RectTransform), typeof(Image));
                    shardObject.transform.SetParent(rootRect, false);

                    RectTransform shardRect = shardObject.GetComponent<RectTransform>();
                    Image shardImage = shardObject.GetComponent<Image>();
                    shardImage.sprite = sprite;
                    shardImage.raycastTarget = false;
                    shardRect.sizeDelta = new Vector2(shardWidth, shardHeight);
                    shardRect.anchorMin = new Vector2(0.5f, 0.5f);
                    shardRect.anchorMax = new Vector2(0.5f, 0.5f);
                    shardRect.pivot = new Vector2(0.5f, 0.5f);

                    float shardX = left + ((column + 0.5f) * shardWidth);
                    float shardY = top - ((row + 0.5f) * shardHeight);
                    shardRect.anchoredPosition = new Vector2(shardX, shardY);

                    float normalizedColumn = columns > 1 ? (column / (columns - 1f)) : 0.5f;
                    float normalizedRow = rows > 1 ? (row / (rows - 1f)) : 0.5f;
                    float lateralBias = Mathf.Lerp(-0.22f, 0.22f, normalizedColumn);
                    float waterfallBias = Mathf.Lerp(1f, 0.6f, normalizedRow);
                    float horizontalSpeed = Random.Range(settings.MatchUiBurstSpeedMin, settings.MatchUiBurstSpeedMax) * horizontalDirection;
                    float horizontalScatter = lateralBias * settings.MatchUiBurstSpeedMin * 0.4f;
                    float verticalSpeed = Random.Range(settings.MatchUiUpwardSpeedMin, settings.MatchUiUpwardSpeedMax) * waterfallBias;
                    float lifetime = settings.MatchUiLifetimeSeconds * Random.Range(0.92f, 1.08f);
                    float spin = Random.Range(settings.MatchUiSpinSpeedMin, settings.MatchUiSpinSpeedMax);
                    if (Random.value < 0.5f)
                    {
                        spin = -spin;
                    }

                    RuntimeShard shard = new RuntimeShard
                    {
                        Rect = shardRect,
                        Image = shardImage,
                        Velocity = new Vector2(horizontalSpeed + horizontalScatter, verticalSpeed),
                        Gravity = settings.MatchUiGravity,
                        GravityDelay = settings.MatchUiGravityDelaySeconds * Random.Range(0.9f, 1.15f),
                        RotationSpeed = spin,
                        Lifetime = Mathf.Max(0.05f, lifetime),
                        FadeStartTime = Mathf.Clamp01(settings.MatchUiFadeStartNormalized) * Mathf.Max(0.05f, lifetime),
                        BaseScale = Vector3.one,
                    };

                    activeShards.Add(shard);
                    shardIndex++;
                }
            }

            return shardIndex > 0;
        }

        private bool SpawnIrregularShards(MahjongTile tile, Vector2 centerPosition, Vector2 effectSize, Texture2D sourceTexture, TileAnimationSettings settings, Sprite[] irregularSprites, float horizontalDirection)
        {
            if (tile == null || irregularSprites == null || irregularSprites.Length == 0)
            {
                return false;
            }

            int shardCount = Mathf.Max(1, settings.MatchUiShardRows * settings.MatchUiShardColumns);
            float totalArea = Mathf.Max(1f, effectSize.x * effectSize.y);
            float averageShardArea = totalArea / Mathf.Max(1, shardCount * 0.9f);
            float spawnWidth = effectSize.x * 0.038f;
            float spawnHeight = effectSize.y * 0.028f;
            float emissionDuration = Mathf.Max(0.01f, settings.MatchUiEmissionDurationSeconds);
            int spawnedCount = 0;
            float streamCenterX = centerPosition.x + (horizontalDirection * effectSize.x * 0.075f);

            for (int index = 0; index < shardCount; index++)
            {
                float emissionProgress = shardCount > 1 ? index / (float)(shardCount - 1) : 0.5f;
                float lineOffset = Mathf.Lerp(-1f, 1f, emissionProgress) * effectSize.x * 0.028f;
                float waveOffset = Mathf.Sin(emissionProgress * Mathf.PI) * effectSize.x * 0.012f * horizontalDirection;

                Sprite sprite = irregularSprites[Random.Range(0, irregularSprites.Length)];
                if (sprite == null)
                {
                    continue;
                }

                Rect spriteRect = sprite.rect;
                float aspect = spriteRect.height > 0.01f ? spriteRect.width / spriteRect.height : 1f;
                float areaScale = Random.Range(0.82f, 1.22f);
                float width = Mathf.Sqrt(Mathf.Max(16f, averageShardArea * aspect * areaScale));
                float height = width / Mathf.Max(0.25f, aspect);

                Sprite textureSprite = CreateRandomTextureCropSprite(sourceTexture);
                if (textureSprite == null)
                {
                    continue;
                }

                GameObject shardObject = new GameObject("UI Irregular Shard", typeof(RectTransform));
                shardObject.transform.SetParent(rootRect, false);

                RectTransform shardRect = shardObject.GetComponent<RectTransform>();
                shardRect.anchorMin = new Vector2(0.5f, 0.5f);
                shardRect.anchorMax = new Vector2(0.5f, 0.5f);
                shardRect.pivot = new Vector2(0.5f, 0.5f);
                shardRect.sizeDelta = new Vector2(width, height);

                float sideBias = horizontalDirection * Random.Range(effectSize.x * 0.02f, effectSize.x * 0.07f);
                float spawnX = streamCenterX + sideBias + lineOffset + waveOffset + Random.Range(-spawnWidth, spawnWidth);
                float spawnY = centerPosition.y + Random.Range(-spawnHeight, spawnHeight);
                shardRect.anchoredPosition = new Vector2(spawnX, spawnY);
                shardRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-28f, 28f));

                Image maskImage = shardObject.AddComponent<Image>();
                maskImage.sprite = sprite;
                maskImage.raycastTarget = false;

                Mask mask = shardObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                GameObject surfaceObject = new GameObject("Surface", typeof(RectTransform), typeof(Image));
                surfaceObject.transform.SetParent(shardRect, false);
                RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
                surfaceRect.anchorMin = Vector2.zero;
                surfaceRect.anchorMax = Vector2.one;
                surfaceRect.offsetMin = Vector2.zero;
                surfaceRect.offsetMax = Vector2.zero;

                Image surfaceImage = surfaceObject.GetComponent<Image>();
                surfaceImage.sprite = textureSprite;
                surfaceImage.raycastTarget = false;
                surfaceImage.preserveAspect = false;

                float horizontalSpeed = Random.Range(settings.MatchUiBurstSpeedMin, settings.MatchUiBurstSpeedMax) * horizontalDirection;
                float horizontalScatter = Random.Range(-settings.MatchUiBurstSpeedMin * 0.62f, settings.MatchUiBurstSpeedMin * 0.62f);
                float verticalSpeed = Random.Range(settings.MatchUiUpwardSpeedMin, settings.MatchUiUpwardSpeedMax);
                float lifetime = settings.MatchUiLifetimeSeconds * Random.Range(0.92f, 1.08f);
                float spin = Random.Range(settings.MatchUiSpinSpeedMin, settings.MatchUiSpinSpeedMax);
                if (Random.value < 0.5f)
                {
                    spin = -spin;
                }

                float spawnDelay = Mathf.Lerp(0f, emissionDuration, index / Mathf.Max(1f, shardCount - 1f));
                spawnDelay += Random.Range(0f, emissionDuration * 0.15f);

                RuntimeShard shard = new RuntimeShard
                {
                    Root = shardObject,
                    Rect = shardRect,
                    Image = surfaceImage,
                    Velocity = new Vector2(horizontalSpeed + horizontalScatter, verticalSpeed),
                    Gravity = settings.MatchUiGravity,
                    GravityDelay = settings.MatchUiGravityDelaySeconds * Random.Range(0.9f, 1.15f),
                    RotationSpeed = spin,
                    Lifetime = Mathf.Max(0.05f, lifetime),
                    FadeStartTime = Mathf.Clamp01(settings.MatchUiFadeStartNormalized) * Mathf.Max(0.05f, lifetime),
                    BaseScale = Vector3.one * Random.Range(0.9f, 1.12f),
                    SpawnDelay = spawnDelay,
                    GeneratedSprite = textureSprite,
                };

                SetShardVisible(shard, false);

                activeShards.Add(shard);
                spawnedCount++;
            }

            return spawnedCount > 0;
        }

        private IEnumerator AnimateShards(TileAnimationSettings settings)
        {
            while (activeShards.Count > 0)
            {
                float deltaTime = settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (deltaTime <= 0f)
                {
                    yield return null;
                    continue;
                }

                for (int index = activeShards.Count - 1; index >= 0; index--)
                {
                    RuntimeShard shard = activeShards[index];
                    if (shard == null || shard.Rect == null || shard.Image == null)
                    {
                        activeShards.RemoveAt(index);
                        continue;
                    }

                    shard.Age += deltaTime;

                    if (!shard.IsVisible)
                    {
                        if (shard.Age < shard.SpawnDelay)
                        {
                            continue;
                        }

                        shard.Age = 0f;
                        shard.IsVisible = true;
                        SetShardVisible(shard, true);
                    }

                    if (shard.Age >= shard.GravityDelay)
                    {
                        shard.Velocity += Vector2.down * (shard.Gravity * deltaTime);
                    }
                    shard.Rect.anchoredPosition += shard.Velocity * deltaTime;
                    shard.Rect.Rotate(0f, 0f, shard.RotationSpeed * deltaTime, Space.Self);

                    float lifetimeProgress = Mathf.Clamp01(shard.Age / shard.Lifetime);
                    float fadeProgress = shard.Age <= shard.FadeStartTime
                        ? 0f
                        : Mathf.InverseLerp(shard.FadeStartTime, shard.Lifetime, shard.Age);

                    shard.Rect.localScale = Vector3.LerpUnclamped(shard.BaseScale, shard.BaseScale * 0.82f, lifetimeProgress * 0.65f);
                    Color color = shard.Image.color;
                    color.a = 1f - fadeProgress;
                    shard.Image.color = color;

                    if (shard.Age >= shard.Lifetime)
                    {
                        DestroyShard(shard);
                        activeShards.RemoveAt(index);
                    }
                }

                yield return null;
            }

            animationRoutine = null;
        }

        private void DestroyShard(RuntimeShard shard)
        {
            if (shard == null)
            {
                return;
            }

            if (shard.Root != null)
            {
                Destroy(shard.Root);
            }

            if (shard.GeneratedSprite != null)
            {
                generatedRuntimeSprites.Remove(shard.GeneratedSprite);
                Destroy(shard.GeneratedSprite);
            }
        }

        private void SetShardVisible(RuntimeShard shard, bool isVisible)
        {
            if (shard == null || shard.Root == null)
            {
                return;
            }

            shard.Root.SetActive(isVisible);
        }

        private bool TryConvertWorldToAnchoredPosition(Camera worldCamera, Vector3 worldPosition, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;
            if (rootRect == null || worldCamera == null)
            {
                return false;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f)
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPoint, null, out anchoredPosition);
        }

        private Vector2 EstimateTileScreenSize(MahjongTile tile, Camera worldCamera, Texture2D sourceTexture, float sizeScale)
        {
            Vector3 tileSize = tile != null ? tile.GetPlacementSize() : Vector3.one;
            float widthWorld = Mathf.Max(0.35f, Mathf.Max(tileSize.x, tileSize.z));
            float heightWorld = Mathf.Max(0.35f, tileSize.y);
            Vector3 worldCenter = tile != null ? tile.transform.position : Vector3.zero;

            Vector3 leftScreen = worldCamera.WorldToScreenPoint(worldCenter - (worldCamera.transform.right * (widthWorld * 0.5f)));
            Vector3 rightScreen = worldCamera.WorldToScreenPoint(worldCenter + (worldCamera.transform.right * (widthWorld * 0.5f)));
            Vector3 downScreen = worldCamera.WorldToScreenPoint(worldCenter - (worldCamera.transform.up * (heightWorld * 0.5f)));
            Vector3 upScreen = worldCamera.WorldToScreenPoint(worldCenter + (worldCamera.transform.up * (heightWorld * 0.5f)));

            float widthPixels = Mathf.Abs(rightScreen.x - leftScreen.x);
            float heightPixels = Mathf.Abs(upScreen.y - downScreen.y);

            if (widthPixels <= 1f || heightPixels <= 1f)
            {
                float fallbackWidth = Mathf.Clamp(Screen.width * 0.06f, 72f, 180f);
                float aspect = sourceTexture != null && sourceTexture.height > 0
                    ? sourceTexture.width / (float)sourceTexture.height
                    : 1f;
                float fallbackHeight = fallbackWidth / Mathf.Max(0.5f, aspect);
                return new Vector2(fallbackWidth, fallbackHeight) * Mathf.Max(0.1f, sizeScale);
            }

            return new Vector2(widthPixels, heightPixels) * Mathf.Max(0.1f, sizeScale);
        }

        private Sprite[] GetOrCreateSprites(Texture2D texture, int rows, int columns)
        {
            if (texture == null)
            {
                return null;
            }

            string cacheKey = texture.GetEntityId() + "_" + rows + "x" + columns;
            if (cachedSprites.TryGetValue(cacheKey, out Sprite[] sprites))
            {
                return sprites;
            }

            sprites = new Sprite[rows * columns];
            float width = texture.width / (float)columns;
            float height = texture.height / (float)rows;
            int spriteIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float x = column * width;
                    float y = texture.height - ((row + 1) * height);
                    Rect rect = new Rect(x, y, width, height);
                    sprites[spriteIndex] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), DefaultPixelsPerUnit, 0, SpriteMeshType.FullRect);
                    spriteIndex++;
                }
            }

            cachedSprites.Add(cacheKey, sprites);
            return sprites;
        }

        private Sprite CreateRandomTextureCropSprite(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            float normalizedWidth = Random.Range(MinTextureCropNormalizedSize, MaxTextureCropNormalizedSize);
            float normalizedHeight = Random.Range(MinTextureCropNormalizedSize, MaxTextureCropNormalizedSize);
            float maxX = Mathf.Max(0f, 1f - normalizedWidth);
            float maxY = Mathf.Max(0f, 1f - normalizedHeight);
            float normalizedX = Random.Range(0f, maxX);
            float normalizedY = Random.Range(0f, maxY);

            Rect rect = new Rect(
                normalizedX * texture.width,
                normalizedY * texture.height,
                Mathf.Max(4f, normalizedWidth * texture.width),
                Mathf.Max(4f, normalizedHeight * texture.height));

            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), DefaultPixelsPerUnit, 0, SpriteMeshType.FullRect);
            generatedRuntimeSprites.Add(sprite);
            return sprite;
        }

        private static Texture2D ResolveSourceTexture(MahjongTile tile)
        {
            if (tile == null)
            {
                return null;
            }

            Texture2D bodyTexture = ResolveRendererTexture(tile.MeshRenderer);
            if (bodyTexture != null)
            {
                return bodyTexture;
            }

            if (tile.PieceTexture != null)
            {
                return tile.PieceTexture;
            }

            if (tile.FillTexture != null)
            {
                return tile.FillTexture;
            }

            return null;
        }

        private static Texture2D ResolveRendererTexture(Renderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty(BaseMapId))
            {
                Texture2D texture = material.GetTexture(BaseMapId) as Texture2D;
                if (texture != null)
                {
                    return texture;
                }
            }

            if (material.HasProperty(MainTexId))
            {
                Texture2D texture = material.GetTexture(MainTexId) as Texture2D;
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        private sealed class RuntimeShard
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public Vector3 BaseScale;
            public float Gravity;
            public float GravityDelay;
            public float RotationSpeed;
            public float Lifetime;
            public float FadeStartTime;
            public float Age;
            public float SpawnDelay;
            public bool IsVisible;
            public Sprite GeneratedSprite;
        }
    }
}
