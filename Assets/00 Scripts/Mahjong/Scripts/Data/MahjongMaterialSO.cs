using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Data
{
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Mahjong Material", fileName = "MahjongMaterial")]
    public sealed class MahjongMaterialSO : ScriptableObject
    {
        [SerializeField] private Material pieceBaseMaterial;
        [SerializeField] private List<MahjongMaterialCategory> pieceCategories = new List<MahjongMaterialCategory>();
        [SerializeField] private Material fillBaseMaterial;
        [SerializeField] private List<MahjongMaterialCategory> fillCategories = new List<MahjongMaterialCategory>();

        /// <summary>
        /// Gets or sets the shared piece base material used by all tiles.
        /// </summary>
        public Material PieceBaseMaterial
        {
            get => pieceBaseMaterial;
            set => pieceBaseMaterial = value;
        }

        /// <summary>
        /// Gets the configured piece texture categories.
        /// </summary>
        public List<MahjongMaterialCategory> PieceCategories => pieceCategories;

        /// <summary>
        /// Gets or sets the shared fill base material used by all tiles.
        /// </summary>
        public Material FillBaseMaterial
        {
            get => fillBaseMaterial;
            set => fillBaseMaterial = value;
        }

        /// <summary>
        /// Gets the configured fill texture categories.
        /// </summary>
        public List<MahjongMaterialCategory> FillCategories => fillCategories;

        /// <summary>
        /// Returns the active piece textures, flattening all configured categories.
        /// </summary>
        public List<Texture2D> GetActivePieceTextures()
        {
            return GetActiveTextures(pieceCategories);
        }

        /// <summary>
        /// Returns the active fill textures, flattening all configured categories.
        /// </summary>
        public List<Texture2D> GetActiveFillTextures()
        {
            return GetActiveTextures(fillCategories);
        }

        private static List<Texture2D> GetActiveTextures(List<MahjongMaterialCategory> categories)
        {
            List<Texture2D> resolvedTextures = new List<Texture2D>();
            HashSet<Texture2D> seenTextures = new HashSet<Texture2D>();
            if (categories == null)
            {
                return resolvedTextures;
            }

            for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
            {
                MahjongMaterialCategory category = categories[categoryIndex];
                if (category?.Textures == null)
                {
                    continue;
                }

                for (int textureIndex = 0; textureIndex < category.Textures.Count; textureIndex++)
                {
                    Texture2D texture = category.Textures[textureIndex];
                    if (texture != null && seenTextures.Add(texture))
                    {
                        resolvedTextures.Add(texture);
                    }
                }
            }

            return resolvedTextures;
        }
    }
}
