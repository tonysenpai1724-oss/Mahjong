using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds gameplay-first bridge silhouettes with two supports, a clear arch opening, and a top deck.
    /// Each shell is a complete bridge nested inside another so generated levels still peel from outside to inside.
    /// </summary>
    internal static class BridgeLevelShapeGenerator
    {
        private static readonly Vector3Int[] NeighborDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        private const int SupportWidth = 3;
        private const int MinGapWidth = 1;
        private const int MaxGapWidth = 7;
        private const int MinDepth = 4;
        private const int MaxDepth = 6;
        private const int MinHeight = 5;
        private const int MaxHeight = 8;
        private const float TargetTileRangeBias = 0.61f;
        private const float OversizePenalty = 0.16f;
        private const float WidthPenalty = 0.35f;
        private const float DepthPenalty = 0.6f;
        private const float HeightPenalty = 0.45f;
        private const float OpeningReward = 0.08f;

        private readonly struct BridgePlan
        {
            public BridgePlan(int gapWidth, int height, int depth, int deckThickness)
            {
                GapWidth = Mathf.Max(1, gapWidth);
                Height = Mathf.Max(3, height);
                Depth = Mathf.Max(1, depth);
                DeckThickness = Mathf.Clamp(deckThickness, 1, Mathf.Max(1, Height - 2));
            }

            public int GapWidth { get; }

            public int Height { get; }

            public int Depth { get; }

            public int DeckThickness { get; }

            public int Width => GapWidth + (SupportWidth * 2);

            public int ClearanceHeight => Mathf.Max(1, Height - DeckThickness);
        }

        public static VoxelGridSize BuildGridSize(int layerCount)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            int width = Mathf.Max(11, (safeLayerCount * 3) + 8);
            int height = Mathf.Max(6, (safeLayerCount * 2) + 5);
            int depth = Mathf.Max(5, safeLayerCount + 4);
            return new VoxelGridSize(width, height, depth);
        }

        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            VoxelGridSize gridSize = BuildGridSize(targetLayerCount);
            List<BridgePlan> plans = SelectBridgePlans(targetLayerCount, minTileCount, maxTileCount, gridSize);
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(plans.Count);

            for (int index = 0; index < plans.Count; index++)
            {
                HashSet<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(plans[index], gridSize);
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(occupiedCoordinates);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }
            }

            shells.Reverse();
            return shells;
        }

        private static List<BridgePlan> SelectBridgePlans(int targetLayerCount, int minTileCount, int maxTileCount, VoxelGridSize gridSize)
        {
            int shellCount = Mathf.Max(1, targetLayerCount);
            int desiredTileCount = Mathf.RoundToInt(Mathf.Lerp(Mathf.Max(2, minTileCount), Mathf.Max(minTileCount, maxTileCount), TargetTileRangeBias));
            List<BridgePlan> bestPlans = null;
            float bestScore = float.MaxValue;
            List<BridgePlan> bestFallbackPlans = null;
            float bestFallbackScore = float.MaxValue;

            int maxInnerGapWidth = Mathf.Min(MaxGapWidth, gridSize.Width - (SupportWidth * 2) - ((shellCount - 1) * 2));
            int maxInnerHeight = Mathf.Min(MaxHeight, gridSize.Height - (shellCount - 1));
            int maxInnerDepth = Mathf.Min(MaxDepth, gridSize.Depth - (shellCount - 1));

            for (int gapWidth = MinGapWidth; gapWidth <= maxInnerGapWidth; gapWidth += 2)
            {
                for (int height = MinHeight; height <= maxInnerHeight; height++)
                {
                    for (int depth = MinDepth; depth <= maxInnerDepth; depth++)
                    {
                        for (int deckThickness = 1; deckThickness <= Mathf.Min(2, height - 2); deckThickness++)
                        {
                            BridgePlan innerPlan = new BridgePlan(gapWidth, height, depth, deckThickness);
                            List<BridgePlan> candidatePlans = BuildNestedBridgePlans(innerPlan, shellCount, gridSize);
                            if (candidatePlans.Count != shellCount)
                            {
                                continue;
                            }

                            int totalTileCount = 0;
                            int totalOpeningVolume = 0;
                            bool valid = true;

                            for (int index = 0; index < candidatePlans.Count; index++)
                            {
                                HashSet<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(candidatePlans[index], gridSize);
                                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(occupiedCoordinates);
                                totalTileCount += shell.Count;
                                totalOpeningVolume += candidatePlans[index].GapWidth * candidatePlans[index].ClearanceHeight * candidatePlans[index].Depth;
                                valid &= shell.Count >= 2 && candidatePlans[index].ClearanceHeight >= 2;
                            }

                            float score = GetDistanceToTileRange(totalTileCount, minTileCount, maxTileCount, desiredTileCount);
                            if (totalTileCount > maxTileCount)
                            {
                                score += (totalTileCount - maxTileCount) * OversizePenalty;
                            }

                            score += gapWidth * WidthPenalty;
                            score += depth * DepthPenalty;
                            score += height * HeightPenalty;
                            score -= totalOpeningVolume * OpeningReward;

                            if (score < bestFallbackScore)
                            {
                                bestFallbackScore = score;
                                bestFallbackPlans = candidatePlans;
                            }

                            if (!valid)
                            {
                                continue;
                            }

                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestPlans = candidatePlans;
                            }
                        }
                    }
                }
            }

            if (bestPlans != null)
            {
                return bestPlans;
            }

            if (bestFallbackPlans != null)
            {
                return bestFallbackPlans;
            }

            return BuildNestedBridgePlans(new BridgePlan(MinGapWidth, MinHeight, MinDepth, 1), shellCount, gridSize);
        }

        private static List<BridgePlan> BuildNestedBridgePlans(BridgePlan innerPlan, int shellCount, VoxelGridSize gridSize)
        {
            List<BridgePlan> plans = new List<BridgePlan>(shellCount);
            BridgePlan currentPlan = innerPlan;

            for (int shellIndex = 0; shellIndex < shellCount; shellIndex++)
            {
                if (!CanFit(currentPlan, gridSize))
                {
                    break;
                }

                plans.Add(currentPlan);
                if (shellIndex >= shellCount - 1)
                {
                    continue;
                }

                currentPlan = ExpandBridgePlan(currentPlan);
            }

            return plans;
        }

        private static BridgePlan ExpandBridgePlan(BridgePlan currentPlan)
        {
            return new BridgePlan(
                currentPlan.GapWidth + 2,
                currentPlan.Height + 1,
                currentPlan.Depth + 1,
                Mathf.Min(2, currentPlan.DeckThickness + (currentPlan.Height >= 6 ? 1 : 0)));
        }

        private static bool CanFit(BridgePlan plan, VoxelGridSize gridSize)
        {
            return plan.Width <= gridSize.Width
                && plan.Height <= gridSize.Height
                && plan.Depth <= gridSize.Depth
                && plan.GapWidth >= 1
                && plan.ClearanceHeight >= 2;
        }

        private static HashSet<Vector3Int> BuildOccupiedCoordinates(BridgePlan plan, VoxelGridSize gridSize)
        {
            HashSet<Vector3Int> occupiedCoordinates = new HashSet<Vector3Int>();
            int startX = Mathf.Max(0, (gridSize.Width - plan.Width) / 2);
            int startY = Mathf.Max(0, (gridSize.Height - plan.Height) / 2);
            int startZ = Mathf.Max(0, (gridSize.Depth - plan.Depth) / 2);
            int deckStartY = plan.Height - plan.DeckThickness;
            int rightSupportStartX = plan.Width - SupportWidth;
            int shoulderStartY = Mathf.Max(1, deckStartY - 2);
            int centerZStart = Mathf.Max(0, (plan.Depth / 2) - 1);
            int centerZEnd = Mathf.Min(plan.Depth - 1, centerZStart + 1);

            for (int localX = 0; localX < plan.Width; localX++)
            {
                bool isLeftSupport = localX < SupportWidth;
                bool isRightSupport = localX >= rightSupportStartX;
                bool isInnerShoulderLeft = localX == SupportWidth;
                bool isInnerShoulderRight = localX == rightSupportStartX - 1;

                for (int localY = 0; localY < plan.Height; localY++)
                {
                    bool isDeck = localY >= deckStartY;
                    bool isShoulder = localY >= shoulderStartY
                        && localY < deckStartY
                        && (isInnerShoulderLeft || isInnerShoulderRight);
                    bool isBraceRow = localY == deckStartY - 1;

                    for (int localZ = 0; localZ < plan.Depth; localZ++)
                    {
                        bool isCenterBrace = isBraceRow
                            && localX >= SupportWidth
                            && localX < rightSupportStartX
                            && localZ >= centerZStart
                            && localZ <= centerZEnd;

                        if (!isDeck && !isLeftSupport && !isRightSupport && !isShoulder && !isCenterBrace)
                        {
                            continue;
                        }

                        occupiedCoordinates.Add(new Vector3Int(startX + localX, startY + localY, startZ + localZ));
                    }
                }
            }

            return occupiedCoordinates;
        }

        private static List<ProceduralLevelBatchGenerator.TilePlacementData> ExtractSurfaceShell(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = coordinate + NeighborDirections[directionIndex];
                    if (occupiedCoordinates.Contains(neighbor))
                    {
                        continue;
                    }

                    shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                    {
                        Coordinate = coordinate,
                        FacingDirection = ToGridDirection(NeighborDirections[directionIndex]),
                        SurfaceSlotIndex = -1,
                    });
                }
            }

            return shell;
        }

        private static VoxelGridDirection ToGridDirection(Vector3Int offset)
        {
            if (offset == Vector3Int.left)
            {
                return VoxelGridDirection.Left;
            }

            if (offset == Vector3Int.right)
            {
                return VoxelGridDirection.Right;
            }

            if (offset == Vector3Int.down)
            {
                return VoxelGridDirection.Down;
            }

            if (offset == Vector3Int.up)
            {
                return VoxelGridDirection.Up;
            }

            if (offset == new Vector3Int(0, 0, -1))
            {
                return VoxelGridDirection.Back;
            }

            return VoxelGridDirection.Forward;
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
