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

        private const string DefaultCategoryRootFolderPath = "Assets/00 Scripts/Mahjong/Tex/Object Fill";

        private static readonly (string CategoryName, string TextureFolderPath)[] LegacyCategorySources =
        {
            ("Number", "Assets/00 Scripts/Mahjong/Tex/Number"),
            ("Flower", "Assets/00 Scripts/Mahjong/Tex/Flower"),
        };

        /// <summary>
        /// Reloads the serialized source-category list from the default root folder.
        /// </summary>
        public int ReloadCategories()
        {
            categories.Clear();

            foreach (SourceCategory discoveredCategory in DiscoverDefaultSourceCategories())
            {
                categories.Add(discoveredCategory);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return categories.Count;
        }

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

            resolvedCategories.AddRange(DiscoverDefaultSourceCategories());
            if (resolvedCategories.Count > 0)
            {
                return resolvedCategories;
            }

            for (int legacyIndex = 0; legacyIndex < LegacyCategorySources.Length; legacyIndex++)
            {
                (string categoryName, string textureFolderPath) = LegacyCategorySources[legacyIndex];
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

        private static List<SourceCategory> DiscoverDefaultSourceCategories()
        {
            List<SourceCategory> discoveredCategories = new List<SourceCategory>();
            DefaultAsset rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultCategoryRootFolderPath);
            if (rootFolder == null)
            {
                return discoveredCategories;
            }

            string rootPath = AssetDatabase.GetAssetPath(rootFolder);
            string[] subFolderGuids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { rootPath });
            Array.Sort(subFolderGuids, CompareTextureGuidsByPath);

            for (int guidIndex = 0; guidIndex < subFolderGuids.Length; guidIndex++)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(subFolderGuids[guidIndex]);
                if (string.IsNullOrEmpty(folderPath) || string.Equals(folderPath, rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    continue;
                }

                string relativePath = folderPath.Substring(rootPath.Length).Trim('/');
                if (relativePath.Length == 0 || relativePath.Contains("/"))
                {
                    continue;
                }

                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
                if (folderAsset == null)
                {
                    continue;
                }

                discoveredCategories.Add(new SourceCategory
                {
                    categoryName = folderAsset.name,
                    textureFolder = folderAsset,
                });
            }

            return discoveredCategories;
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
