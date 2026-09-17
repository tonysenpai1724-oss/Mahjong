using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Holds a generated preview sprite for a captured gameplay item.
    /// Mirrors the Dice ItemPreview pattern with a lightweight UI-only target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryItemPreview : MonoBehaviour
    {
        private const float DefaultPixelsPerUnit = 100f;

        [SerializeField] private Image itemPreview;

        private Texture2D previewTexture;
        private Sprite previewSprite;
        private bool ownsTexture;

        public Image ItemPreviewImage => itemPreview;

        public void PrepareForCapture()
        {
            EnsureImage();

            if (itemPreview == null)
            {
                return;
            }

            itemPreview.sprite = null;
            itemPreview.enabled = false;
        }

        public void SetPreview(Texture2D texture)
        {
            ReleasePreview();
            previewTexture = texture;
            ownsTexture = texture != null;
            EnsureImage();

            if (itemPreview == null)
            {
                return;
            }

            if (previewTexture == null)
            {
                itemPreview.sprite = null;
                itemPreview.enabled = false;
                return;
            }

            previewSprite = CreateSprite(previewTexture);
            itemPreview.sprite = previewSprite;
            itemPreview.preserveAspect = true;
            itemPreview.enabled = true;
        }

        public void SetPreview(Sprite sprite)
        {
            ReleasePreview();
            previewSprite = sprite;
            previewTexture = sprite != null ? sprite.texture : null;
            ownsTexture = false;
            EnsureImage();

            if (itemPreview == null)
            {
                return;
            }

            itemPreview.sprite = previewSprite;
            itemPreview.preserveAspect = true;
            itemPreview.enabled = previewSprite != null;
        }

        public void SetPreview(Texture2D texture, Image targetImage)
        {
            if (targetImage != null)
            {
                itemPreview = targetImage;
            }

            SetPreview(texture);
        }

        public static Sprite CreateSprite(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                DefaultPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        private void EnsureImage()
        {
            if (itemPreview != null)
            {
                return;
            }

            itemPreview = GetComponentInChildren<Image>(true);
        }

        private void ReleasePreview()
        {
            if (previewSprite != null)
            {
                Destroy(previewSprite);
                previewSprite = null;
            }

            if (previewTexture != null)
            {
                if (ownsTexture)
                {
                    // Delay destruction so active match effects / shard particles can finish rendering
                    Destroy(previewTexture, 3.5f);
                }

                previewTexture = null;
                ownsTexture = false;
            }
        }

        private void OnDestroy()
        {
            ReleasePreview();
        }
    }
}
