using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a full pagoda silhouette where each generated shell is a complete pagoda nested inside another.
    /// Layer count therefore means the number of enclosing pagoda shells, not the number of visible rooftop tiers.
    /// </summary>
    internal static class PagodaLevelShapeGenerator
    {
        private const float TargetTileRangeBias = 0.64f;
        private const float OversizePenalty = 0.2f;
        private const float BaseSizeReward = 5f;
        private const float TierReward = 16f;
        private const float ShellReward = 12f;
        private const float MismatchPenalty = 12f;

        private readonly struct PagodaPlan
        {
            public PagodaPlan(int baseColumnCount, int baseRowCount, int tierCount, int shellIndex)
            {
                BaseColumnCount = Mathf.Max(3, baseColumnCount);
                BaseRowCount = Mathf.Max(3, baseRowCount);
                TierCount = Mathf.Max(2, tierCount);
                ShellIndex = Mathf.Max(0, shellIndex);
            }

            public int BaseColumnCount { get; }

            public int BaseRowCount { get; }

            public int TierCount { get; }

            public int ShellIndex { get; }
        }

        private readonly struct TierPlan
        {
            public TierPlan(int columnCount, int rowCount, int heightCount)
            {
                ColumnCount = Mathf.Max(2, columnCount);
                RowCount = Mathf.Max(2, rowCount);
                HeightCount = Mathf.Max(1, heightCount);
            }

            public int ColumnCount { get; }

            public int RowCount { get; }

            public int HeightCount { get; }
        }

        public static VoxelGridSize BuildGridSize(int layerCount)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            int width = Mathf.Max(10, (safeLayerCount * 2) + 6);
            int height = Mathf.Max(8, (safeLayerCount * 2) + 5);
            int depth = Mathf.Max(10, (safeLayerCount * 2) + 6);
            return new VoxelGridSize(width, height, depth);
        }

        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            int shellCount = Mathf.Max(1, targetLayerCount);
            List<PagodaPlan> plans = SelectPagodaPlans(shellCount, metrics, minTileCount, maxTileCount);
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(plans.Count);

            for (int index = 0; index < plans.Count; index++)
            {
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = BuildPagodaShell(plans[index], metrics);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }
            }

            return shells;
        }

        private static List<PagodaPlan> SelectPagodaPlans(
            int shellCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            int desiredTileCount = Mathf.RoundToInt(Mathf.Lerp(Mathf.Max(2, minTileCount), Mathf.Max(minTileCount, maxTileCount), TargetTileRangeBias));
            float bestScore = float.MaxValue;
            int bestCapacity = 0;
            List<PagodaPlan> bestPlans = null;

            for (int tierCount = 3; tierCount <= 5; tierCount++)
            {
                int minBaseColumnCount = tierCount + shellCount + 1;
                int minBaseRowCount = tierCount + shellCount;
                int maxBaseColumnCount = tierCount + shellCount + 4;
                int maxBaseRowCount = tierCount + shellCount + 3;

                for (int baseColumnCount = minBaseColumnCount; baseColumnCount <= maxBaseColumnCount; baseColumnCount++)
                {
                    for (int baseRowCount = minBaseRowCount; baseRowCount <= maxBaseRowCount; baseRowCount++)
                    {
                        List<PagodaPlan> candidatePlans = BuildNestedPagodaPlans(baseColumnCount, baseRowCount, tierCount, shellCount);
                        if (candidatePlans.Count != shellCount)
                        {
                            continue;
                        }

                        int totalTileCount = 0;
                        for (int shellIndex = 0; shellIndex < candidatePlans.Count; shellIndex++)
                        {
                            totalTileCount += BuildPagodaShell(candidatePlans[shellIndex], metrics).Count;
                        }

                        float footprintMismatch = GetFootprintMismatch(candidatePlans[0], metrics);
                        float score = GetDistanceToTileRange(totalTileCount, minTileCount, maxTileCount, desiredTileCount);
                        score += footprintMismatch * MismatchPenalty;
                        score += Mathf.Max(0, totalTileCount - maxTileCount) * OversizePenalty;
                        score -= candidatePlans[0].BaseColumnCount * BaseSizeReward;
                        score -= candidatePlans[0].BaseRowCount * (BaseSizeReward * 0.8f);
                        score -= tierCount * TierReward;
                        score -= candidatePlans.Count * ShellReward;

                        bool isBetter = score + 0.0001f < bestScore;
                        bool isCloserTie = Mathf.Abs(score - bestScore) <= 0.0001f
                            && Mathf.Abs(totalTileCount - desiredTileCount) < Mathf.Abs(bestCapacity - desiredTileCount);
                        if (!isBetter && !isCloserTie)
                        {
                            continue;
                        }

                        bestScore = score;
                        bestCapacity = totalTileCount;
                        bestPlans = candidatePlans;
                    }
                }
            }

            if (bestPlans != null)
            {
                return bestPlans;
            }

            return BuildNestedPagodaPlans(8 + shellCount, 7 + shellCount, 4, shellCount);
        }

        private static List<PagodaPlan> BuildNestedPagodaPlans(int outerBaseColumnCount, int outerBaseRowCount, int tierCount, int shellCount)
        {
            List<PagodaPlan> plans = new List<PagodaPlan>(shellCount);

            for (int shellIndex = 0; shellIndex < shellCount; shellIndex++)
            {
                int baseColumnCount = outerBaseColumnCount - (shellIndex * 2);
                int baseRowCount = outerBaseRowCount - (shellIndex * 2);
                if (!CanBuildFullPagoda(baseColumnCount, baseRowCount, tierCount))
                {
                    break;
                }

                plans.Add(new PagodaPlan(baseColumnCount, baseRowCount, tierCount, shellIndex));
            }

            return plans;
        }

        private static bool CanBuildFullPagoda(int baseColumnCount, int baseRowCount, int tierCount)
        {
            return baseColumnCount - (tierCount - 1) >= 2
                && baseRowCount - (tierCount - 1) >= 2;
        }

        private static List<TierPlan> BuildTierPlans(PagodaPlan pagodaPlan)
        {
            List<TierPlan> tierPlans = new List<TierPlan>(pagodaPlan.TierCount);
            int currentColumnCount = pagodaPlan.BaseColumnCount;
            int currentRowCount = pagodaPlan.BaseRowCount;

            for (int tierIndex = 0; tierIndex < pagodaPlan.TierCount; tierIndex++)
            {
                tierPlans.Add(new TierPlan(
                    currentColumnCount,
                    currentRowCount,
                    ResolveTierHeightCount(tierIndex, pagodaPlan.TierCount, pagodaPlan.ShellIndex)));

                currentColumnCount = Mathf.Max(2, currentColumnCount - 1);
                currentRowCount = Mathf.Max(2, currentRowCount - 1);
            }

            return tierPlans;
        }

        private static int ResolveTierHeightCount(int tierIndex, int tierCount, int shellIndex)
        {
            int baseHeightCount;
            if (tierIndex == 0)
            {
                baseHeightCount = 2;
            }
            else if (tierIndex < tierCount - 1)
            {
                baseHeightCount = 2;
            }
            else
            {
                baseHeightCount = 1;
            }

            int shellHeightReduction = Mathf.Min(shellIndex, 1);
            return Mathf.Max(1, baseHeightCount - shellHeightReduction);
        }

        private static List<ProceduralLevelBatchGenerator.TilePlacementData> BuildPagodaShell(
            PagodaPlan pagodaPlan,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            List<TierPlan> tierPlans = BuildTierPlans(pagodaPlan);
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            float verticalStep = Mathf.Max(metrics.FaceWidth, metrics.FaceHeight);
            float totalHeight = 0f;

            for (int index = 0; index < tierPlans.Count; index++)
            {
                totalHeight += GetTierWorldHeight(tierPlans[index], verticalStep, metrics.Thickness);
            }

            float currentBottom = -totalHeight * 0.5f;
            for (int tierIndex = 0; tierIndex < tierPlans.Count; tierIndex++)
            {
                TierPlan tierPlan = tierPlans[tierIndex];
                TierPlan? nextTierPlan = tierIndex + 1 < tierPlans.Count ? tierPlans[tierIndex + 1] : (TierPlan?)null;
                float tierHeight = GetTierWorldHeight(tierPlan, verticalStep, metrics.Thickness);
                float centerY = currentBottom + (tierHeight * 0.5f);
                AddTierSurface(shell, tierPlan, nextTierPlan, centerY, tierIndex == 0, metrics, verticalStep);
                currentBottom += tierHeight;
            }

            return shell;
        }

        private static void AddTierSurface(
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell,
            TierPlan tierPlan,
            TierPlan? nextTierPlan,
            float centerY,
            bool addBottomFace,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            float verticalStep)
        {
            float widthSpan = tierPlan.ColumnCount * metrics.FaceWidth;
            float depthSpan = tierPlan.RowCount * metrics.FaceHeight;
            float heightSpan = GetTierWorldHeight(tierPlan, verticalStep, metrics.Thickness);
            float xOffset = Mathf.Max(0f, (widthSpan - metrics.Thickness) * 0.5f);
            float yOffset = Mathf.Max(0f, (heightSpan - metrics.Thickness) * 0.5f);
            float zOffset = Mathf.Max(0f, (depthSpan - metrics.Thickness) * 0.5f);

            for (int verticalIndex = 0; verticalIndex < tierPlan.HeightCount; verticalIndex++)
            {
                float localY = centerY + GetCenteredVerticalCoordinate(verticalIndex, tierPlan.HeightCount, heightSpan, metrics.FaceWidth, metrics.Thickness);
                for (int depthIndex = 0; depthIndex < tierPlan.RowCount; depthIndex++)
                {
                    float localZ = GetCenteredPanelCoordinate(depthIndex, tierPlan.RowCount, metrics.FaceHeight);
                    shell.Add(CreatePlacement(new Vector3(-xOffset, localY, localZ), VoxelGridDirection.Left));
                    shell.Add(CreatePlacement(new Vector3(xOffset, localY, localZ), VoxelGridDirection.Right));
                }
            }

            for (int verticalIndex = 0; verticalIndex < tierPlan.HeightCount; verticalIndex++)
            {
                float localY = centerY + GetCenteredVerticalCoordinate(verticalIndex, tierPlan.HeightCount, heightSpan, metrics.FaceHeight, metrics.Thickness);
                for (int widthIndex = 0; widthIndex < tierPlan.ColumnCount; widthIndex++)
                {
                    float localX = GetInsetPanelCoordinate(widthIndex, tierPlan.ColumnCount, metrics.FaceWidth, metrics.FaceWidth * 0.5f);
                    shell.Add(CreatePlacement(new Vector3(localX, localY, -zOffset), VoxelGridDirection.Back));
                    shell.Add(CreatePlacement(new Vector3(localX, localY, zOffset), VoxelGridDirection.Forward));
                }
            }

            for (int widthIndex = 0; widthIndex < tierPlan.ColumnCount; widthIndex++)
            {
                float localX = GetInsetPanelCoordinate(widthIndex, tierPlan.ColumnCount, metrics.FaceWidth, metrics.FaceWidth * 0.5f);
                for (int depthIndex = 0; depthIndex < tierPlan.RowCount; depthIndex++)
                {
                    float localZ = GetInsetPanelCoordinate(depthIndex, tierPlan.RowCount, metrics.FaceHeight, metrics.FaceHeight * 0.5f);
                    if (!IsCoveredByTierAbove(localX, localZ, nextTierPlan, metrics))
                    {
                        shell.Add(CreatePlacement(new Vector3(localX, centerY + yOffset, localZ), VoxelGridDirection.Up));
                    }

                    if (addBottomFace)
                    {
                        shell.Add(CreatePlacement(new Vector3(localX, centerY - yOffset, localZ), VoxelGridDirection.Down));
                    }
                }
            }
        }

        private static bool IsCoveredByTierAbove(
            float localX,
            float localZ,
            TierPlan? nextTierPlan,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            if (!nextTierPlan.HasValue)
            {
                return false;
            }

            TierPlan plan = nextTierPlan.Value;
            float tileHalfWidth = metrics.FaceWidth * 0.5f;
            float tileHalfDepth = metrics.FaceHeight * 0.5f;
            float upperHalfWidth = (plan.ColumnCount * metrics.FaceWidth * 0.5f) + (metrics.Thickness * 0.5f);
            float upperHalfDepth = (plan.RowCount * metrics.FaceHeight * 0.5f) + (metrics.Thickness * 0.5f);

            float tileMinX = localX - tileHalfWidth;
            float tileMaxX = localX + tileHalfWidth;
            float tileMinZ = localZ - tileHalfDepth;
            float tileMaxZ = localZ + tileHalfDepth;

            bool overlapsX = tileMaxX > -upperHalfWidth && tileMinX < upperHalfWidth;
            bool overlapsZ = tileMaxZ > -upperHalfDepth && tileMinZ < upperHalfDepth;
            return overlapsX && overlapsZ;
        }

        private static ProceduralLevelBatchGenerator.TilePlacementData CreatePlacement(Vector3 localPosition, VoxelGridDirection facingDirection)
        {
            return new ProceduralLevelBatchGenerator.TilePlacementData
            {
                Coordinate = Vector3Int.zero,
                FacingDirection = facingDirection,
                SurfaceSlotIndex = -1,
                CustomLocalPosition = localPosition,
                UseCustomLocalPosition = true,
                ApplyShellCompaction = true,
            };
        }

        private static float GetCenteredPanelCoordinate(int index, int count, float step)
        {
            return (index - ((count - 1) * 0.5f)) * step;
        }

        private static float GetInsetPanelCoordinate(int index, int count, float step, float inset)
        {
            if (count <= 1)
            {
                return 0f;
            }

            float fullHalfSpan = ((count - 1) * step) * 0.5f;
            float insetHalfSpan = Mathf.Max(0f, fullHalfSpan - inset);
            float resolvedStep = (insetHalfSpan * 2f) / (count - 1);
            return -insetHalfSpan + (index * resolvedStep);
        }

        private static float GetCenteredVerticalCoordinate(int index, int count, float availableSpan, float tileSpan, float faceThickness)
        {
            if (count <= 1)
            {
                return 0f;
            }

            float halfRange = Mathf.Max(0f, (availableSpan - faceThickness - tileSpan) * 0.5f);
            float step = (halfRange * 2f) / (count - 1);
            return -halfRange + (index * step);
        }

        private static float GetTierWorldHeight(TierPlan tierPlan, float verticalStep, float thickness)
        {
            return (tierPlan.HeightCount * verticalStep) + (Mathf.Max(0.01f, thickness) * 1.1f);
        }

        private static float GetFootprintMismatch(PagodaPlan plan, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            float widthSpan = plan.BaseColumnCount * metrics.FaceWidth;
            float depthSpan = plan.BaseRowCount * metrics.FaceHeight;
            float longestSide = Mathf.Max(widthSpan, depthSpan);
            return longestSide <= 0.01f ? 0f : Mathf.Abs(widthSpan - depthSpan) / longestSide;
        }

        private static float GetDistanceToTileRange(int tileCount, int minTileCount, int maxTileCount, int targetTileCount)
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
    }
}
