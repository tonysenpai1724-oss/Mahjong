using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds stacked pagoda-style shell tiers using the resolved Mahjong tile footprint.
    /// Keeping this in a dedicated class makes the pagoda profile easy to tune without touching cube generation.
    /// </summary>
    internal static class PagodaLevelShapeGenerator
    {
        private const int MaxTilesPerAxis = 12;
        private const float EdgeInsetPadding = 0.001f;
        private const float TierGapMultiplier = 0.75f;

        private readonly struct TierPlan
        {
            public TierPlan(int columnCount, int rowCount, float sideLength, float normalOffset)
            {
                ColumnCount = Mathf.Max(1, columnCount);
                RowCount = Mathf.Max(1, rowCount);
                SideLength = Mathf.Max(0.01f, sideLength);
                NormalOffset = Mathf.Max(0f, normalOffset);
            }

            public int ColumnCount { get; }

            public int RowCount { get; }

            public float SideLength { get; }

            public float NormalOffset { get; }
        }

        public static VoxelGridSize BuildGridSize(int layerCount)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            return new VoxelGridSize(
                Mathf.Max(4, safeLayerCount + 2),
                Mathf.Max(4, safeLayerCount + 2),
                Mathf.Max(4, safeLayerCount + 2));
        }

        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            int tierCount = Mathf.Max(2, targetLayerCount);
            List<TierPlan> tierPlans = SelectTierPlans(tierCount, metrics, minTileCount, maxTileCount);
            if (tierPlans.Count == 0)
            {
                return new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>();
            }

            float[] tierCenters = ResolveTierCenters(tierPlans, metrics.Thickness);
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(tierPlans.Count);
            for (int index = 0; index < tierPlans.Count; index++)
            {
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = BuildTierShell(tierPlans[index], metrics, tierCenters[index]);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }
            }

            return shells;
        }

        private static List<TierPlan> SelectTierPlans(int tierCount, ProceduralLevelBatchGenerator.CubeTileMetrics metrics, int minTileCount, int maxTileCount)
        {
            float safeMinTileCount = Mathf.Max(2, minTileCount);
            float safeMaxTileCount = Mathf.Max(safeMinTileCount, maxTileCount);
            float targetTileCount = (safeMinTileCount + safeMaxTileCount) * 0.5f;

            float bestRangeDistance = float.MaxValue;
            float bestMismatch = float.MaxValue;
            int bestTotalTileCount = int.MaxValue;
            List<TierPlan> bestPlans = null;

            for (int baseColumnCount = 1; baseColumnCount <= MaxTilesPerAxis; baseColumnCount++)
            {
                for (int baseRowCount = 1; baseRowCount <= MaxTilesPerAxis; baseRowCount++)
                {
                    List<TierPlan> candidatePlans = BuildTierPlans(baseColumnCount, baseRowCount, tierCount, metrics);
                    if (candidatePlans.Count == 0)
                    {
                        continue;
                    }

                    int cumulativeTileCount = 0;
                    float candidateRangeDistance = float.MaxValue;
                    for (int index = 0; index < candidatePlans.Count; index++)
                    {
                        cumulativeTileCount += GetShellTileCount(candidatePlans[index]);
                        float distanceToRange = GetDistanceToTileRange(cumulativeTileCount, safeMinTileCount, safeMaxTileCount, targetTileCount);
                        if (distanceToRange < candidateRangeDistance)
                        {
                            candidateRangeDistance = distanceToRange;
                        }
                    }

                    TierPlan basePlan = candidatePlans[0];
                    float panelWidth = basePlan.ColumnCount * metrics.FaceWidth;
                    float panelHeight = basePlan.RowCount * metrics.FaceHeight;
                    float longestSide = Mathf.Max(panelWidth, panelHeight);
                    float mismatch = longestSide <= 0.01f ? 0f : Mathf.Abs(panelWidth - panelHeight) / longestSide;

                    bool isBetterRange = candidateRangeDistance + 0.0001f < bestRangeDistance;
                    bool isSameRangeWithBetterMismatch = Mathf.Abs(candidateRangeDistance - bestRangeDistance) <= 0.0001f && mismatch + 0.0001f < bestMismatch;
                    bool isSameRangeAndMismatchWithFewerTiles = Mathf.Abs(candidateRangeDistance - bestRangeDistance) <= 0.0001f && Mathf.Abs(mismatch - bestMismatch) <= 0.0001f && cumulativeTileCount < bestTotalTileCount;
                    if (!isBetterRange && !isSameRangeWithBetterMismatch && !isSameRangeAndMismatchWithFewerTiles)
                    {
                        continue;
                    }

                    bestRangeDistance = candidateRangeDistance;
                    bestMismatch = mismatch;
                    bestTotalTileCount = cumulativeTileCount;
                    bestPlans = candidatePlans;
                }
            }

            return bestPlans ?? BuildTierPlans(2, 2, tierCount, metrics);
        }

        private static List<TierPlan> BuildTierPlans(int baseColumnCount, int baseRowCount, int tierCount, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            List<TierPlan> plans = new List<TierPlan>(tierCount);
            int columnCount = Mathf.Max(1, baseColumnCount);
            int rowCount = Mathf.Max(1, baseRowCount);

            for (int tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                float sideLength = ResolveTierSideLength(columnCount, rowCount, metrics);
                float normalOffset = ResolveNormalOffset(sideLength, metrics);
                plans.Add(new TierPlan(columnCount, rowCount, sideLength, normalOffset));
                ShrinkTierFootprint(metrics, ref columnCount, ref rowCount);
            }

            return plans;
        }

        private static float[] ResolveTierCenters(List<TierPlan> tierPlans, float thickness)
        {
            float[] centers = new float[tierPlans.Count];
            float currentTop = 0f;
            float gap = Mathf.Max(0.01f, thickness * TierGapMultiplier);

            for (int index = 0; index < tierPlans.Count; index++)
            {
                TierPlan plan = tierPlans[index];
                float centerY = index == 0 ? plan.NormalOffset : currentTop + gap + plan.NormalOffset;
                centers[index] = centerY;
                currentTop = centerY + plan.NormalOffset;
            }

            float minimumY = centers[0] - tierPlans[0].NormalOffset;
            float maximumY = currentTop;
            float centerOffset = -((minimumY + maximumY) * 0.5f);
            for (int index = 0; index < centers.Length; index++)
            {
                centers[index] += centerOffset;
            }

            return centers;
        }

        private static List<ProceduralLevelBatchGenerator.TilePlacementData> BuildTierShell(TierPlan plan, ProceduralLevelBatchGenerator.CubeTileMetrics metrics, float centerY)
        {
            int widthCount = plan.ColumnCount;
            int heightCount = plan.RowCount;
            float normalOffset = plan.NormalOffset;
            float widthAxisStep = Mathf.Max(0.01f, metrics.FaceWidth);
            float heightAxisStep = Mathf.Max(0.01f, metrics.FaceHeight);
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();

            for (int verticalIndex = 0; verticalIndex < widthCount; verticalIndex++)
            {
                float localY = GetCenteredPanelCoordinate(verticalIndex, widthCount, widthAxisStep) + centerY;
                for (int depthIndex = 0; depthIndex < heightCount; depthIndex++)
                {
                    float localZ = GetCenteredPanelCoordinate(depthIndex, heightCount, heightAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(-normalOffset, localY, localZ), VoxelGridDirection.Left));
                    shell.Add(CreateCustomPlacement(new Vector3(normalOffset, localY, localZ), VoxelGridDirection.Right));
                }
            }

            for (int depthIndex = 0; depthIndex < heightCount; depthIndex++)
            {
                float localZ = GetCenteredPanelCoordinate(depthIndex, heightCount, heightAxisStep);
                for (int widthIndex = 0; widthIndex < widthCount; widthIndex++)
                {
                    float localX = GetCenteredPanelCoordinate(widthIndex, widthCount, widthAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(localX, centerY - normalOffset, localZ), VoxelGridDirection.Down));
                    shell.Add(CreateCustomPlacement(new Vector3(localX, centerY + normalOffset, localZ), VoxelGridDirection.Up));
                }
            }

            for (int heightIndex = 0; heightIndex < heightCount; heightIndex++)
            {
                float localY = GetCenteredPanelCoordinate(heightIndex, heightCount, heightAxisStep) + centerY;
                for (int widthIndex = 0; widthIndex < widthCount; widthIndex++)
                {
                    float localX = GetCenteredPanelCoordinate(widthIndex, widthCount, widthAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(localX, localY, -normalOffset), VoxelGridDirection.Back));
                    shell.Add(CreateCustomPlacement(new Vector3(localX, localY, normalOffset), VoxelGridDirection.Forward));
                }
            }

            return shell;
        }

        private static void ShrinkTierFootprint(ProceduralLevelBatchGenerator.CubeTileMetrics metrics, ref int columnCount, ref int rowCount)
        {
            float widthSpan = columnCount * metrics.FaceWidth;
            float depthSpan = rowCount * metrics.FaceHeight;
            if ((widthSpan >= depthSpan && columnCount > 1) || rowCount <= 1)
            {
                columnCount = Mathf.Max(1, columnCount - 1);
                return;
            }

            rowCount = Mathf.Max(1, rowCount - 1);
        }

        private static float ResolveTierSideLength(int columnCount, int rowCount, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            float edgeInset = Mathf.Max(0.01f, metrics.Thickness + EdgeInsetPadding);
            float panelWidth = columnCount * metrics.FaceWidth;
            float panelHeight = rowCount * metrics.FaceHeight;
            return Mathf.Max(panelWidth, panelHeight) + (edgeInset * 2f);
        }

        private static float ResolveNormalOffset(float sideLength, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            float centeredOffset = (sideLength - metrics.Thickness) * 0.55f;
            float inwardRecess = metrics.Thickness * 1f;
            return Mathf.Max(0f, centeredOffset - inwardRecess);
        }

        private static float GetDistanceToTileRange(int tileCount, float minTileCount, float maxTileCount, float targetTileCount)
        {
            if (tileCount >= minTileCount && tileCount <= maxTileCount)
            {
                return Mathf.Abs(tileCount - targetTileCount);
            }

            if (tileCount < minTileCount)
            {
                return (minTileCount - tileCount) + 1000f;
            }

            return (tileCount - maxTileCount) + 1000f;
        }

        private static int GetShellTileCount(TierPlan plan)
        {
            return plan.ColumnCount * plan.RowCount * 6;
        }

        private static float GetCenteredPanelCoordinate(int index, int count, float step)
        {
            return ((index + 0.5f) - (count * 0.5f)) * step;
        }

        private static void GetSquareFaceTileGrid(float tileFaceWidth, float tileFaceHeight, int maxFaceTileCount, out int columnCount, out int rowCount)
        {
            float safeWidth = Mathf.Max(0.01f, tileFaceWidth);
            float safeHeight = Mathf.Max(0.01f, tileFaceHeight);
            int safeMaxFaceTileCount = Mathf.Max(1, maxFaceTileCount);
            float bestMismatch = float.MaxValue;
            int bestColumnCount = 1;
            int bestRowCount = 1;
            int bestTileCount = int.MaxValue;

            for (int candidateColumnCount = 1; candidateColumnCount <= MaxTilesPerAxis; candidateColumnCount++)
            {
                for (int candidateRowCount = 1; candidateRowCount <= MaxTilesPerAxis; candidateRowCount++)
                {
                    int tileCount = candidateColumnCount * candidateRowCount;
                    if (tileCount > safeMaxFaceTileCount)
                    {
                        continue;
                    }

                    float panelWidth = candidateColumnCount * safeWidth;
                    float panelHeight = candidateRowCount * safeHeight;
                    float longestSide = Mathf.Max(panelWidth, panelHeight);
                    float mismatch = longestSide <= 0.01f ? 0f : Mathf.Abs(panelWidth - panelHeight) / longestSide;

                    bool isBetterMismatch = mismatch + 0.0001f < bestMismatch;
                    bool isSameMismatchWithFewerTiles = Mathf.Abs(mismatch - bestMismatch) <= 0.0001f && tileCount < bestTileCount;
                    if (!isBetterMismatch && !isSameMismatchWithFewerTiles)
                    {
                        continue;
                    }

                    bestMismatch = mismatch;
                    bestColumnCount = candidateColumnCount;
                    bestRowCount = candidateRowCount;
                    bestTileCount = tileCount;
                }
            }

            columnCount = bestColumnCount;
            rowCount = bestRowCount;
        }

        private static ProceduralLevelBatchGenerator.TilePlacementData CreateCustomPlacement(Vector3 localPosition, VoxelGridDirection facingDirection)
        {
            return new ProceduralLevelBatchGenerator.TilePlacementData
            {
                Coordinate = Vector3Int.zero,
                FacingDirection = facingDirection,
                SurfaceSlotIndex = -1,
                CustomLocalPosition = localPosition,
                UseCustomLocalPosition = true,
            };
        }
    }
}
