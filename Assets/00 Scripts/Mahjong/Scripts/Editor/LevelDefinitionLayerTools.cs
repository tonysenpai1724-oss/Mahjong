using System.Collections.Generic;
using MahjongOut3D.LevelSystem;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Provides editor utilities for backfilling and refreshing layer metadata on level assets.
    /// </summary>
    public static class LevelDefinitionLayerTools
    {
        private const string LayerCountPropertyName = "<LayerCount>k__BackingField";
        private const string TilesPropertyName = "<Tiles>k__BackingField";
        private const string UseSurfaceTilePlacementPropertyName = "<UseSurfaceTilePlacement>k__BackingField";

        /// <summary>
        /// Refreshes layer metadata for every LevelDefinition asset in the project.
        /// </summary>
        [MenuItem("Tools/Mahjong Out 3D/Levels/Refresh Layer Counts")]
        public static void RefreshAllLevelLayerCountsMenu()
        {
            int refreshedCount = RefreshAllLevelLayerCounts();
            EditorUtility.DisplayDialog("Refresh Layer Counts", $"Refreshed {refreshedCount} level assets.", "OK");
        }

        /// <summary>
        /// Refreshes layer metadata for every LevelDefinition asset in the project.
        /// </summary>
        /// <returns>Number of processed level assets.</returns>
        public static int RefreshAllLevelLayerCounts()
        {
            string[] levelGuids = AssetDatabase.FindAssets("t:LevelDefinition");
            int refreshedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int index = 0; index < levelGuids.Length; index++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(levelGuids[index]);
                    LevelDefinition definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                    if (definition == null)
                    {
                        continue;
                    }

                    if (RefreshLevelDefinition(definition))
                    {
                        refreshedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return refreshedCount;
        }

        /// <summary>
        /// Refreshes layer metadata for one level asset.
        /// </summary>
        /// <param name="definition">Level asset to refresh.</param>
        /// <returns>True when the asset was processed; otherwise false.</returns>
        public static bool RefreshLevelDefinition(LevelDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(definition);
            SerializedProperty layerCountProperty = serializedObject.FindProperty(LayerCountPropertyName);
            SerializedProperty tilesProperty = serializedObject.FindProperty(TilesPropertyName);
            SerializedProperty useSurfaceTilePlacementProperty = serializedObject.FindProperty(UseSurfaceTilePlacementPropertyName);
            if (layerCountProperty == null || tilesProperty == null || useSurfaceTilePlacementProperty == null)
            {
                return false;
            }

            int resolvedLayerCount = useSurfaceTilePlacementProperty.boolValue
                ? RefreshSurfaceLevelTiles(tilesProperty, definition.LayoutOverride)
                : RefreshFlatLevelTiles(tilesProperty);

            layerCountProperty.intValue = Mathf.Max(0, resolvedLayerCount);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return true;
        }

        private static int RefreshFlatLevelTiles(SerializedProperty tilesProperty)
        {
            if (tilesProperty == null || !tilesProperty.isArray || tilesProperty.arraySize == 0)
            {
                return 0;
            }

            for (int index = 0; index < tilesProperty.arraySize; index++)
            {
                SerializedProperty tileProperty = tilesProperty.GetArrayElementAtIndex(index);
                tileProperty.FindPropertyRelative("surfaceShellIndex").intValue = 0;
            }

            return 1;
        }

        private static int RefreshSurfaceLevelTiles(SerializedProperty tilesProperty, VoxelGridLayoutSettings layoutOverride)
        {
            List<SurfaceTileEntry> entries = BuildEntries(tilesProperty);
            if (entries.Count == 0)
            {
                return 0;
            }

            List<SurfaceTileEntry> remainingEntries = new List<SurfaceTileEntry>(entries);
            int layerIndex = 0;
            while (remainingEntries.Count > 0)
            {
                List<SurfaceTileEntry> exposedEntries = new List<SurfaceTileEntry>();
                for (int index = 0; index < remainingEntries.Count; index++)
                {
                    SurfaceTileEntry candidate = remainingEntries[index];
                    if (IsEntryExposed(candidate, remainingEntries, layoutOverride))
                    {
                        exposedEntries.Add(candidate);
                    }
                }

                if (exposedEntries.Count == 0)
                {
                    for (int index = 0; index < remainingEntries.Count; index++)
                    {
                        remainingEntries[index].TileProperty.FindPropertyRelative("surfaceShellIndex").intValue = layerIndex;
                    }

                    layerIndex++;
                    break;
                }

                HashSet<int> exposedIndexes = new HashSet<int>();
                for (int index = 0; index < exposedEntries.Count; index++)
                {
                    exposedEntries[index].TileProperty.FindPropertyRelative("surfaceShellIndex").intValue = layerIndex;
                    exposedIndexes.Add(exposedEntries[index].OriginalIndex);
                }

                remainingEntries.RemoveAll(entry => exposedIndexes.Contains(entry.OriginalIndex));
                layerIndex++;
            }

            return layerIndex;
        }

        private static List<SurfaceTileEntry> BuildEntries(SerializedProperty tilesProperty)
        {
            List<SurfaceTileEntry> entries = new List<SurfaceTileEntry>();
            if (tilesProperty == null || !tilesProperty.isArray)
            {
                return entries;
            }

            for (int index = 0; index < tilesProperty.arraySize; index++)
            {
                SerializedProperty tileProperty = tilesProperty.GetArrayElementAtIndex(index);
                if (tileProperty == null)
                {
                    continue;
                }

                entries.Add(new SurfaceTileEntry(
                    index,
                    tileProperty,
                    tileProperty.FindPropertyRelative("localPosition").vector3Value,
                    ResolveFacingDirection(tileProperty.FindPropertyRelative("localEulerAngles").vector3Value)));
            }

            return entries;
        }

        private static bool IsEntryExposed(SurfaceTileEntry candidate, List<SurfaceTileEntry> remainingEntries, VoxelGridLayoutSettings layoutOverride)
        {
            Vector3 outwardNormal = ((Vector3)VoxelGridDirections.GetOffset(candidate.FacingDirection)).normalized;
            Vector2 columnTolerance = GetSurfaceColumnTolerance(candidate.FacingDirection, layoutOverride);
            float depthEpsilon = GetDepthEpsilon(layoutOverride);
            float candidateDepth = Vector3.Dot(candidate.LocalPosition, outwardNormal);

            for (int index = 0; index < remainingEntries.Count; index++)
            {
                SurfaceTileEntry blocker = remainingEntries[index];
                if (blocker.OriginalIndex == candidate.OriginalIndex || blocker.FacingDirection != candidate.FacingDirection)
                {
                    continue;
                }

                float blockerDepth = Vector3.Dot(blocker.LocalPosition, outwardNormal);
                if (blockerDepth <= candidateDepth + depthEpsilon)
                {
                    continue;
                }

                if (SharesSurfaceColumn(candidate.FacingDirection, candidate.LocalPosition, blocker.LocalPosition, columnTolerance))
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector2 GetSurfaceColumnTolerance(VoxelGridDirection facingDirection, VoxelGridLayoutSettings layoutOverride)
        {
            Vector3 step = layoutOverride != null ? layoutOverride.CellStep : Vector3.one;
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return new Vector2(Mathf.Max(0.05f, step.y * 0.35f), Mathf.Max(0.05f, step.z * 0.35f));

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return new Vector2(Mathf.Max(0.05f, step.x * 0.35f), Mathf.Max(0.05f, step.z * 0.35f));

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return new Vector2(Mathf.Max(0.05f, step.x * 0.35f), Mathf.Max(0.05f, step.y * 0.35f));
            }
        }

        private static float GetDepthEpsilon(VoxelGridLayoutSettings layoutOverride)
        {
            Vector3 step = layoutOverride != null ? layoutOverride.CellStep : Vector3.one;
            float minStep = Mathf.Min(Mathf.Abs(step.x), Mathf.Abs(step.y), Mathf.Abs(step.z));
            return Mathf.Max(0.01f, minStep * 0.1f);
        }

        private static bool SharesSurfaceColumn(VoxelGridDirection facingDirection, Vector3 firstPosition, Vector3 secondPosition, Vector2 columnTolerance)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Mathf.Abs(firstPosition.y - secondPosition.y) <= columnTolerance.x
                        && Mathf.Abs(firstPosition.z - secondPosition.z) <= columnTolerance.y;

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return Mathf.Abs(firstPosition.x - secondPosition.x) <= columnTolerance.x
                        && Mathf.Abs(firstPosition.z - secondPosition.z) <= columnTolerance.y;

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Abs(firstPosition.x - secondPosition.x) <= columnTolerance.x
                        && Mathf.Abs(firstPosition.y - secondPosition.y) <= columnTolerance.y;
            }
        }

        private static VoxelGridDirection ResolveFacingDirection(Vector3 localEulerAngles)
        {
            Vector3 normal = Quaternion.Euler(localEulerAngles) * Vector3.up;
            Vector3 absoluteNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (absoluteNormal.x >= absoluteNormal.y && absoluteNormal.x >= absoluteNormal.z)
            {
                return normal.x >= 0f ? VoxelGridDirection.Right : VoxelGridDirection.Left;
            }

            if (absoluteNormal.y >= absoluteNormal.x && absoluteNormal.y >= absoluteNormal.z)
            {
                return normal.y >= 0f ? VoxelGridDirection.Up : VoxelGridDirection.Down;
            }

            return normal.z >= 0f ? VoxelGridDirection.Forward : VoxelGridDirection.Back;
        }

        private readonly struct SurfaceTileEntry
        {
            public SurfaceTileEntry(int originalIndex, SerializedProperty tileProperty, Vector3 localPosition, VoxelGridDirection facingDirection)
            {
                OriginalIndex = originalIndex;
                TileProperty = tileProperty;
                LocalPosition = localPosition;
                FacingDirection = facingDirection;
            }

            public int OriginalIndex { get; }

            public SerializedProperty TileProperty { get; }

            public Vector3 LocalPosition { get; }

            public VoxelGridDirection FacingDirection { get; }
        }
    }
}
