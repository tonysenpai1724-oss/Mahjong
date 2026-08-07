using System;
using System.Collections.Generic;
using MahjongOut3D.Data;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Editor-only generator that syncs fill textures grouped by category folders into the material library.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Editor/Fill Material Generator", fileName = "MahjongFillMaterialGenerator")]
    public sealed class MahjongFillMaterialGenerator : ScriptableObject
    {
        [Serializable]
        public sealed class SourceCategory
        {
            public string categoryName;
            public DefaultAsset textureFolder;
        }

        [SerializeField] private MahjongMaterialSO targetMaterialLibrary;
        [SerializeField] private Material fillBaseMaterial;
        [SerializeField] private List<SourceCategory> categories = new List<SourceCategory>();

        private static readonly (string CategoryName, string TextureFolderPath)[] DefaultCategorySources =
        {
            ("Number", "Assets/00 Scripts/Mahjong/Tex/Number"),
            ("Flower", "Assets/00 Scripts/Mahjong/Tex/Flower"),
        };

        /// <summary>
        /// Syncs categorized fill textures into the target material library.
        /// </summary>
        public int Generate()
        {
            if (targetMaterialLibrary == null)
            {
                throw new InvalidOperationException("Target material library is not assigned.");
            }

            Material resolvedBaseMaterial = ResolveBaseMaterial();
            if (resolvedBaseMaterial == null)
            {
                throw new InvalidOperationException("Fill base material is missing. Assign one in the generator or on MahjongMaterialSO.");
            }

            List<MahjongMaterialCategory> generatedCategories = new List<MahjongMaterialCategory>();
            int textureCount = 0;

            List<SourceCategory> sourceCategories = ResolveSourceCategories();
            for (int categoryIndex = 0; categoryIndex < sourceCategories.Count; categoryIndex++)
            {
                SourceCategory sourceCategory = sourceCategories[categoryIndex];
                if (sourceCategory == null || sourceCategory.textureFolder == null)
                {
                    continue;
                }

                string textureFolderPath = AssetDatabase.GetAssetPath(sourceCategory.textureFolder);
                if (string.IsNullOrEmpty(textureFolderPath) || !AssetDatabase.IsValidFolder(textureFolderPath))
                {
                    continue;
                }

                string categoryName = string.IsNullOrWhiteSpace(sourceCategory.categoryName)
                    ? sourceCategory.textureFolder.name
                    : sourceCategory.categoryName.Trim();
                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolderPath });
                Array.Sort(textureGuids, CompareTextureGuidsByPath);

                MahjongMaterialCategory generatedCategory = new MahjongMaterialCategory
                {
                    CategoryName = categoryName,
                };

                for (int textureIndex = 0; textureIndex < textureGuids.Length; textureIndex++)
                {
                    string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[textureIndex]);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture == null)
                    {
                        continue;
                    }

                    generatedCategory.Textures.Add(texture);
                    textureCount++;
                }

                if (generatedCategory.Textures.Count > 0)
                {
                    generatedCategories.Add(generatedCategory);
                }
            }

            targetMaterialLibrary.FillBaseMaterial = resolvedBaseMaterial;
            targetMaterialLibrary.FillCategories.Clear();
            targetMaterialLibrary.FillCategories.AddRange(generatedCategories);
            EditorUtility.SetDirty(targetMaterialLibrary);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return textureCount;
        }

        private List<SourceCategory> ResolveSourceCategories()
        {
            List<SourceCategory> resolvedCategories = new List<SourceCategory>();
            if (categories != null)
            {
                for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
                {
                    SourceCategory sourceCategory = categories[categoryIndex];
                    if (sourceCategory?.textureFolder != null)
                    {
                        resolvedCategories.Add(sourceCategory);
                    }
                }
            }

            if (resolvedCategories.Count > 0)
            {
                return resolvedCategories;
            }

            for (int defaultIndex = 0; defaultIndex < DefaultCategorySources.Length; defaultIndex++)
            {
                (string categoryName, string textureFolderPath) = DefaultCategorySources[defaultIndex];
                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(textureFolderPath);
                if (folderAsset == null)
                {
                    continue;
                }

                resolvedCategories.Add(new SourceCategory
                {
                    categoryName = categoryName,
                    textureFolder = folderAsset,
                });
            }

            return resolvedCategories;
        }

        private Material ResolveBaseMaterial()
        {
            if (fillBaseMaterial != null)
            {
                return fillBaseMaterial;
            }

            return targetMaterialLibrary != null ? targetMaterialLibrary.FillBaseMaterial : null;
        }

        private static int CompareTextureGuidsByPath(string leftGuid, string rightGuid)
        {
            string leftPath = AssetDatabase.GUIDToAssetPath(leftGuid);
            string rightPath = AssetDatabase.GUIDToAssetPath(rightGuid);
            return string.Compare(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
