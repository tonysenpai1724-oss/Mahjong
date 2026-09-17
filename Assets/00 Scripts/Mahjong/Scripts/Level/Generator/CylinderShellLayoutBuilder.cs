using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric hollow cylinder shells that match the authored preview layouts.
    /// The reference previews use 16 outer segments, 8 middle segments, and 4 inner segments across 6 rows.
    /// </summary>
    internal sealed class CylinderShellLayoutBuilder
    {
        private const int CoreSegmentCount = 16;
        private const int MinimumSegmentCount = 4;
        private const int CoreRowCount = 6;
        private const int MaximumShellCount = 3;
        private const float PreviewOuterCenterDistance = 1.2223f;
        private const float PreviewVerticalStep = 0.72f;

        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public CylinderShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(int targetLayerCount, int minTileCount, int maxTileCount, System.Random random)
        {
            int desiredShellCount = Mathf.Clamp(targetLayerCount, 1, MaximumShellCount);
            List<int> segmentCounts = BuildSegmentCounts(desiredShellCount);
            int totalTileCount = CoreRowCount * Sum(segmentCounts);
            CylinderBlueprint blueprint = new CylinderBlueprint(segmentCounts, CoreRowCount, totalTileCount, 0, GetBalanceDifference(segmentCounts[segmentCounts.Count - 1], CoreRowCount), totalTileCount >= Mathf.Max(2, minTileCount));
            return BuildShells(blueprint);
        }

        private List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(CylinderBlueprint blueprint)
        {
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(blueprint.SegmentCounts.Count);
            float verticalStep = PreviewVerticalStep;
            float outerCenterDistance = PreviewOuterCenterDistance;

            for (int shellIndex = 0; shellIndex < blueprint.SegmentCounts.Count; shellIndex++)
            {
                int segmentCount = blueprint.SegmentCounts[shellIndex];
                float centerDistance = outerCenterDistance / Mathf.Pow(2f, shellIndex);
                shells.Add(BuildShell(segmentCount, blueprint.RowCount, centerDistance, verticalStep, shellIndex, blueprint.SegmentCounts.Count));
            }

            return shells;
        }

        private List<ProceduralLevelBatchGenerator.TilePlacementData> BuildShell(int segmentCount, int rowCount, float centerDistance, float verticalStep, int shellIndex, int totalShellCount)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>(segmentCount * rowCount);

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                float localY = GetCenteredPanelCoordinate(rowIndex, rowCount, verticalStep);

                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    float angleRadians = ((float)segmentIndex / segmentCount) * Mathf.PI * 2f;
                    Vector3 outwardNormal = new Vector3(Mathf.Sin(angleRadians), 0f, Mathf.Cos(angleRadians)).normalized;
                    float yawDegrees = Mathf.Atan2(outwardNormal.x, outwardNormal.z) * Mathf.Rad2Deg;
                    Vector3Int authoredGridCoordinate = GetAuthoredGridCoordinate(totalShellCount, shellIndex, rowIndex, segmentIndex);

                    shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                    {
                        Coordinate = authoredGridCoordinate,
                        FacingDirection = ToFacingDirection(outwardNormal),
                        SurfaceSlotIndex = -1,
                        CustomLocalPosition = (outwardNormal * centerDistance) + (Vector3.up * localY),
                        CustomLocalEulerAngles = new Vector3(0f, yawDegrees + 90f, 90f),
                        UseCustomLocalPosition = true,
                        UseCustomLocalEulerAngles = true,
                        ApplyShellCompaction = false,
                    });
                }
            }

            return shell;
        }

        private static List<int> BuildSegmentCounts(int shellCount)
        {
            List<int> segmentCounts = new List<int>(shellCount);
            int safeShellCount = Mathf.Clamp(shellCount, 1, MaximumShellCount);
            int segmentCount = CoreSegmentCount;
            for (int shellIndex = 0; shellIndex < safeShellCount; shellIndex++)
            {
                segmentCounts.Add(segmentCount);
                if (segmentCount <= MinimumSegmentCount)
                {
                    break;
                }

                segmentCount = Mathf.Max(MinimumSegmentCount, segmentCount / 2);
            }

            return segmentCounts;
        }

        private static Vector3Int GetAuthoredGridCoordinate(int totalShellCount, int shellIndex, int rowIndex, int segmentIndex)
        {
            int safeRowIndex = Mathf.Max(0, rowIndex);

            if (totalShellCount <= 1)
            {
                Vector2Int[] perimeterCoordinates =
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(4, 0),
                    new Vector2Int(4, 1),
                    new Vector2Int(4, 2),
                    new Vector2Int(4, 3),
                    new Vector2Int(4, 4),
                    new Vector2Int(3, 4),
                    new Vector2Int(2, 4),
                    new Vector2Int(1, 4),
                    new Vector2Int(0, 4),
                    new Vector2Int(0, 3),
                    new Vector2Int(0, 2),
                    new Vector2Int(0, 1),
                };

                Vector2Int coordinate = perimeterCoordinates[Mathf.Clamp(segmentIndex, 0, perimeterCoordinates.Length - 1)];
                return new Vector3Int(coordinate.x, safeRowIndex, coordinate.y);
            }

            Vector2Int[][] shellCoordinates =
            {
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(4, 0),
                    new Vector2Int(5, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(3, 1),
                    new Vector2Int(4, 1),
                    new Vector2Int(5, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, 2),
                    new Vector2Int(3, 2),
                },
                new[]
                {
                    new Vector2Int(0, 3),
                    new Vector2Int(1, 3),
                    new Vector2Int(2, 3),
                    new Vector2Int(3, 3),
                    new Vector2Int(4, 3),
                    new Vector2Int(5, 3),
                    new Vector2Int(0, 4),
                    new Vector2Int(1, 4),
                },
                new[]
                {
                    new Vector2Int(0, 5),
                    new Vector2Int(1, 5),
                    new Vector2Int(2, 5),
                    new Vector2Int(3, 5),
                },
            };

            int safeShellIndex = Mathf.Clamp(shellIndex, 0, shellCoordinates.Length - 1);
            Vector2Int[] coordinates = shellCoordinates[safeShellIndex];
            Vector2Int authoredCoordinate = coordinates[Mathf.Clamp(segmentIndex, 0, coordinates.Length - 1)];
            return new Vector3Int(authoredCoordinate.x, safeRowIndex, authoredCoordinate.y);
        }


        private float GetTangentStep()
        {
            return PreviewOuterCenterDistance * Mathf.Sin(Mathf.PI / CoreSegmentCount) * 2f;
        }

        private float GetVerticalStep()
        {
            return PreviewVerticalStep;
        }

        private float GetBalanceDifference(int segmentCount, int rowCount)
        {
            float verticalStep = GetVerticalStep();
            float radius = GetPreviewCenterDistance(segmentCount);
            float totalHeight = rowCount * verticalStep;
            float outerDiameter = (radius * 2f) + tileMetrics.Thickness;
            return Mathf.Abs(totalHeight - outerDiameter) / Mathf.Max(0.01f, Mathf.Max(totalHeight, outerDiameter));
        }

        private static float GetPreviewCenterDistance(int segmentCount)
        {
            float normalizedSegmentRatio = Mathf.Max(1f, segmentCount) / CoreSegmentCount;
            return PreviewOuterCenterDistance * normalizedSegmentRatio;
        }

        private static float GetRingRadius(int segmentCount, float tangentStep)
        {
            int safeSegmentCount = Mathf.Max(3, segmentCount);
            float halfAngle = Mathf.PI / safeSegmentCount;
            float denominator = Mathf.Max(0.0001f, Mathf.Sin(halfAngle) * 2f);
            return tangentStep / denominator;
        }

        private static int Sum(List<int> values)
        {
            int total = 0;
            for (int index = 0; index < values.Count; index++)
            {
                total += values[index];
            }

            return total;
        }

        private static float GetCenteredPanelCoordinate(int index, int count, float step)
        {
            return ((index + 0.5f) - (count * 0.5f)) * step;
        }

        private static VoxelGridDirection ToFacingDirection(Vector3 outwardNormal)
        {
            Vector3 absoluteNormal = new Vector3(Mathf.Abs(outwardNormal.x), Mathf.Abs(outwardNormal.y), Mathf.Abs(outwardNormal.z));
            if (absoluteNormal.x >= absoluteNormal.z)
            {
                return outwardNormal.x >= 0f ? VoxelGridDirection.Right : VoxelGridDirection.Left;
            }

            return outwardNormal.z >= 0f ? VoxelGridDirection.Forward : VoxelGridDirection.Back;
        }

        private readonly struct CylinderBlueprint
        {
            public CylinderBlueprint(List<int> segmentCounts, int rowCount, int totalTileCount, int difference, float balanceDifference, bool meetsMinimumTileCount)
            {
                SegmentCounts = segmentCounts ?? new List<int> { CoreSegmentCount };
                RowCount = Mathf.Max(1, rowCount);
                TotalTileCount = Mathf.Max(2, totalTileCount);
                Difference = Mathf.Max(0, difference);
                BalanceDifference = Mathf.Max(0f, balanceDifference);
                MeetsMinimumTileCount = meetsMinimumTileCount;
            }

            public List<int> SegmentCounts { get; }

            public int RowCount { get; }

            public int TotalTileCount { get; }

            public int Difference { get; }

            public float BalanceDifference { get; }

            public bool MeetsMinimumTileCount { get; }
        }
    }
}
