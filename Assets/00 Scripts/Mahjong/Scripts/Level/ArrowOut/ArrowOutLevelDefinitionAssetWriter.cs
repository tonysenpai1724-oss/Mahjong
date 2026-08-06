using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongOut3D.LevelSystem.ArrowOutGeneration
{
    /// <summary>
    /// Writes generated Arrow Out layouts into the existing LevelDefinition asset format.
    /// </summary>
    public static class ArrowOutLevelDefinitionAssetWriter
    {
        private const string LevelNameProperty = "<LevelName>k__BackingField";
        private const string GridSizeProperty = "<GridSize>k__BackingField";
        private const string LayoutOverrideProperty = "<LayoutOverride>k__BackingField";
        private const string ShapeProperty = "<Shape>k__BackingField";
        private const string UseSurfaceTilePlacementProperty = "<UseSurfaceTilePlacement>k__BackingField";
        private const string DifficultyProperty = "<Difficulty>k__BackingField";
        private const string TilesProperty = "<Tiles>k__BackingField";

        /// <summary>
        /// Creates or overwrites a LevelDefinition asset from generated tile data.
        /// </summary>
        public static LevelDefinition WriteAsset(ArrowOutGeneratedLevel level, string outputFolder, bool overwriteExistingAsset)
        {
#if UNITY_EDITOR
            if (level == null)
            {
                throw new System.ArgumentNullException(nameof(level));
            }

            EnsureFolder(outputFolder);
            string safeName = SanitizeName(level.LevelName);
            string assetPath = $"{outputFolder}/{safeName}.asset";
            if (!overwriteExistingAsset)
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            LevelDefinition asset = overwriteExistingAsset
                ? AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath)
                : null;

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            FindRequiredProperty(serializedObject, LevelNameProperty).stringValue = level.LevelName;
            WriteGridSize(FindRequiredProperty(serializedObject, GridSizeProperty), level.GridSize);
            FindRequiredProperty(serializedObject, LayoutOverrideProperty).objectReferenceValue = level.LayoutOverride;
            FindRequiredProperty(serializedObject, ShapeProperty).intValue = (int)level.Shape;

            SerializedProperty useSurfaceTilePlacement = serializedObject.FindProperty(UseSurfaceTilePlacementProperty);
            if (useSurfaceTilePlacement != null)
            {
                useSurfaceTilePlacement.boolValue = false;
            }

            FindRequiredProperty(serializedObject, DifficultyProperty).enumValueIndex = (int)level.Difficulty;
            WriteTiles(FindRequiredProperty(serializedObject, TilesProperty), level.Tiles);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
#else
            throw new System.InvalidOperationException("Level asset writing is editor-only.");
#endif
        }

#if UNITY_EDITOR
        private static SerializedProperty FindRequiredProperty(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                return property;
            }

            throw new System.InvalidOperationException($"Could not find serialized property '{propertyName}' on LevelDefinition.");
        }

        private static void WriteGridSize(SerializedProperty property, VoxelGridSize gridSize)
        {
            property.FindPropertyRelative("width").intValue = gridSize.Width;
            property.FindPropertyRelative("height").intValue = gridSize.Height;
            property.FindPropertyRelative("depth").intValue = gridSize.Depth;
        }

        private static void WriteTiles(SerializedProperty property, IReadOnlyList<GeneratedTileData> tiles)
        {
            property.arraySize = tiles.Count;
            for (int index = 0; index < tiles.Count; index++)
            {
                SerializedProperty tileProperty = property.GetArrayElementAtIndex(index);
                GeneratedTileData tile = tiles[index];
                tileProperty.FindPropertyRelative("matchId").intValue = tile.MatchId;
                tileProperty.FindPropertyRelative("gridCoordinate").vector3IntValue = tile.Coordinate;
                tileProperty.FindPropertyRelative("useCustomLocalPosition").boolValue = false;
                tileProperty.FindPropertyRelative("localPosition").vector3Value = Vector3.zero;
                tileProperty.FindPropertyRelative("localEulerAngles").vector3Value = tile.LocalEulerAngles;
            }
        }

        private static string SanitizeName(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
            {
                return "ArrowOutLevel";
            }

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                levelName = levelName.Replace(invalid, '_');
            }

            return levelName.Trim();
        }

        private static void EnsureFolder(string outputFolder)
        {
            if (AssetDatabase.IsValidFolder(outputFolder))
            {
                return;
            }

            string normalized = outputFolder.Replace('\\', '/');
            string[] segments = normalized.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
#endif
    }
}
