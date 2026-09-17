using System;
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

        /// <summary>
        /// Returns the active fill textures filtered by category names.
        /// When the selection is empty, all active fill textures are returned.
        /// </summary>
        public List<Texture2D> GetActiveFillTextures(IList<string> categoryNames)
        {
            return GetActiveTextures(fillCategories, categoryNames);
        }

        private static List<Texture2D> GetActiveTextures(List<MahjongMaterialCategory> categories)
        {
            return GetActiveTextures(categories, null);
        }

        private static List<Texture2D> GetActiveTextures(List<MahjongMaterialCategory> categories, IList<string> categoryNames)
        {
            List<Texture2D> resolvedTextures = new List<Texture2D>();
            HashSet<Texture2D> seenTextures = new HashSet<Texture2D>();
            HashSet<string> categoryLookup = BuildCategoryLookup(categoryNames);
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

                if (categoryLookup != null)
                {
                    string categoryName = string.IsNullOrWhiteSpace(category.CategoryName) ? string.Empty : category.CategoryName.Trim();
                    if (!categoryLookup.Contains(categoryName))
                    {
                        continue;
                    }
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

        private static HashSet<string> BuildCategoryLookup(IList<string> categoryNames)
        {
            if (categoryNames == null || categoryNames.Count == 0)
            {
                return null;
            }

            HashSet<string> lookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < categoryNames.Count; index++)
            {
                string categoryName = categoryNames[index];
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    lookup.Add(categoryName.Trim());
                }
            }

            return lookup.Count > 0 ? lookup : null;
        }
    }
}
