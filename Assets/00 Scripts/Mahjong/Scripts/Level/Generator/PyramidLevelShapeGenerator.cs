using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds gameplay-first hollow pyramid shells.
    /// The shape is formed by thin clustered bands, tunnels and chambers instead of a filled solid volume.
    /// </summary>
    internal static class PyramidLevelShapeGenerator
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

        private const int MinBaseFootprint = 7;
        private const int MaxBaseFootprint = 15;
        private const float TargetTileRangeBias = 0.58f;
        private const float OversizePenalty = 0.2f;
        private const float SizePenalty = 0.6f;
        private const float RevealReward = 0.18f;

        private enum ClusterKind
        {
            Base,
            Lower,
            Middle,
            Upper,
            Apex,
        }

        private readonly struct ShellPlan
        {
            public ShellPlan(int baseFootprint)
            {
                BaseFootprint = Mathf.Max(3, baseFootprint);
            }

            public int BaseFootprint { get; }

            public int StepCount => ((BaseFootprint - 1) / 2) + 1;
        }

        private sealed class ShellCandidate
        {
            public ShellPlan Plan;
            public HashSet<Vector3Int> Occupied;
            public int TileCount;
            public int LargestInternalVoid;
            public int HiddenFaceCount;
            public int PocketCount;
            public float EmptyRatio;
            public bool IsValid;
        }

        public static VoxelGridSize BuildGridSize(int layerCount)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            int size = Mathf.Max(11, (safeLayerCount * 3) + 7);
            int height = Mathf.Max(6, (safeLayerCount * 2) + 4);
            return new VoxelGridSize(size, height, size);
        }

        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            VoxelGridSize gridSize = BuildGridSize(targetLayerCount);
            List<ShellPlan> plans = SelectShellPlans(targetLayerCount, minTileCount, maxTileCount, gridSize);
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(plans.Count);

            for (int index = 0; index < plans.Count; index++)
            {
                ShellCandidate candidate = BuildShellCandidate(plans[index], gridSize);
                if (candidate == null || candidate.Occupied == null || candidate.Occupied.Count == 0)
                {
                    continue;
                }

                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(candidate.Occupied);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }
            }

            shells.Reverse();
            return shells;
        }

        private static List<ShellPlan> SelectShellPlans(int targetLayerCount, int minTileCount, int maxTileCount, VoxelGridSize gridSize)
        {
            int shellCount = Mathf.Max(1, targetLayerCount);
            int desiredTileCount = Mathf.RoundToInt(Mathf.Lerp(Mathf.Max(2, minTileCount), Mathf.Max(minTileCount, maxTileCount), TargetTileRangeBias));
            float bestScore = float.MaxValue;
            List<ShellPlan> bestPlans = null;
            List<ShellPlan> bestFallbackPlans = null;
            float bestFallbackScore = float.MaxValue;

            int maxInitialFootprint = Mathf.Min(MaxBaseFootprint, gridSize.Width - ((shellCount - 1) * 2));
            if (maxInitialFootprint % 2 == 0)
            {
                maxInitialFootprint--;
            }

            for (int baseFootprint = MinBaseFootprint; baseFootprint <= maxInitialFootprint; baseFootprint += 2)
            {
                List<ShellPlan> candidatePlans = BuildShellPlans(new ShellPlan(baseFootprint), shellCount, gridSize);
                if (candidatePlans.Count != shellCount)
                {
                    continue;
                }

                int totalTileCount = 0;
                int totalHiddenFaces = 0;
                int totalLargestVoid = 0;
                int totalPockets = 0;
                float totalEmptyRatio = 0f;
                bool valid = true;

                for (int index = 0; index < candidatePlans.Count; index++)
                {
                    ShellCandidate candidate = BuildShellCandidate(candidatePlans[index], gridSize);
                    if (candidate == null)
                    {
                        valid = false;
                        break;
                    }

                    totalTileCount += candidate.TileCount;
                    totalHiddenFaces += candidate.HiddenFaceCount;
                    totalLargestVoid += candidate.LargestInternalVoid;
                    totalPockets += candidate.PocketCount;
                    totalEmptyRatio += candidate.EmptyRatio;
                    valid &= candidate.IsValid;
                }

                float score = GetDistanceToTileRange(totalTileCount, minTileCount, maxTileCount, desiredTileCount);
                if (totalTileCount > maxTileCount)
                {
                    score += (totalTileCount - maxTileCount) * OversizePenalty;
                }

                score += baseFootprint * SizePenalty;
                score -= totalHiddenFaces * RevealReward;
                score -= totalLargestVoid * 0.08f;
                score -= totalPockets * 3f;
                score -= totalEmptyRatio * 10f;

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

            if (bestPlans != null)
            {
                return bestPlans;
            }

            if (bestFallbackPlans != null)
            {
                return bestFallbackPlans;
            }

            return BuildShellPlans(new ShellPlan(MinBaseFootprint), shellCount, gridSize);
        }

        private static List<ShellPlan> BuildShellPlans(ShellPlan innerPlan, int shellCount, VoxelGridSize gridSize)
        {
            List<ShellPlan> plans = new List<ShellPlan>(shellCount);
            ShellPlan currentPlan = innerPlan;

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

                currentPlan = ExpandShellPlan(currentPlan, gridSize);
            }

            return plans;
        }

        private static ShellPlan ExpandShellPlan(ShellPlan currentPlan, VoxelGridSize gridSize)
        {
            int nextFootprint = Mathf.Min(currentPlan.BaseFootprint + 2, Mathf.Min(MaxBaseFootprint, gridSize.Width));
            if (nextFootprint % 2 == 0)
            {
                nextFootprint--;
            }

            return new ShellPlan(nextFootprint);
        }

        private static bool CanFit(ShellPlan plan, VoxelGridSize gridSize)
        {
            return plan.BaseFootprint <= gridSize.Width
                && plan.BaseFootprint <= gridSize.Depth
                && plan.StepCount <= gridSize.Height;
        }

        private static ShellCandidate BuildShellCandidate(ShellPlan plan, VoxelGridSize gridSize)
        {
            HashSet<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(plan, gridSize);
            if (occupiedCoordinates.Count == 0)
            {
                return null;
            }

            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(occupiedCoordinates);
            HashSet<Vector3Int> envelopeCoordinates = BuildEnvelopeCoordinates(plan, gridSize);
            AnalyzeVoidSpaces(
                occupiedCoordinates,
                envelopeCoordinates,
                out float emptyRatio,
                out int largestInternalVoid,
                out int hiddenFaceCount,
                out int pocketCount);

            bool isValid = emptyRatio >= 0.5f
                && largestInternalVoid >= Mathf.Max(6, plan.StepCount * 2)
                && hiddenFaceCount >= Mathf.Max(10, plan.BaseFootprint)
                && pocketCount >= 1;

            return new ShellCandidate
            {
                Plan = plan,
                Occupied = occupiedCoordinates,
                TileCount = shell.Count,
                LargestInternalVoid = largestInternalVoid,
                HiddenFaceCount = hiddenFaceCount,
                PocketCount = pocketCount,
                EmptyRatio = emptyRatio,
                IsValid = isValid,
            };
        }

        private static HashSet<Vector3Int> BuildOccupiedCoordinates(ShellPlan plan, VoxelGridSize gridSize)
        {
            HashSet<Vector3Int> occupiedCoordinates = new HashSet<Vector3Int>();

            for (int level = 0; level < plan.StepCount; level++)
            {
                int footprint = GetFootprintAtLevel(plan, level);
                int nextFootprint = level + 1 < plan.StepCount ? GetFootprintAtLevel(plan, level + 1) : 0;
                AddOuterShellLevel(occupiedCoordinates, gridSize, level, footprint, nextFootprint);
                AddClusterLevel(occupiedCoordinates, gridSize, plan, level, footprint);
            }

            return occupiedCoordinates;
        }

        private static HashSet<Vector3Int> BuildEnvelopeCoordinates(ShellPlan plan, VoxelGridSize gridSize)
        {
            HashSet<Vector3Int> envelopeCoordinates = new HashSet<Vector3Int>();
            for (int level = 0; level < plan.StepCount; level++)
            {
                int footprint = GetFootprintAtLevel(plan, level);
                int minX;
                int minZ;
                GetLevelOrigin(gridSize, footprint, out minX, out minZ);
                int maxX = minX + footprint;
                int maxZ = minZ + footprint;

                for (int x = minX; x < maxX; x++)
                {
                    for (int y = 0; y <= level; y++)
                    {
                        for (int z = minZ; z < maxZ; z++)
                        {
                            envelopeCoordinates.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            return envelopeCoordinates;
        }

        private static void AnalyzeVoidSpaces(
            HashSet<Vector3Int> occupiedCoordinates,
            HashSet<Vector3Int> envelopeCoordinates,
            out float emptyRatio,
            out int largestInternalVoid,
            out int hiddenFaceCount,
            out int pocketCount)
        {
            int envelopeVolume = Mathf.Max(1, envelopeCoordinates.Count);
            int emptyCellCount = envelopeVolume - occupiedCoordinates.Count;
            emptyRatio = emptyCellCount / (float)envelopeVolume;
            largestInternalVoid = 0;
            hiddenFaceCount = 0;
            pocketCount = 0;

            if (emptyCellCount <= 0)
            {
                return;
            }

            HashSet<Vector3Int> exteriorReachable = FloodExteriorVoids(occupiedCoordinates, envelopeCoordinates);
            HashSet<Vector3Int> internalVoids = new HashSet<Vector3Int>();
            foreach (Vector3Int coordinate in envelopeCoordinates)
            {
                if (!occupiedCoordinates.Contains(coordinate) && !exteriorReachable.Contains(coordinate))
                {
                    internalVoids.Add(coordinate);
                }
            }

            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    if (internalVoids.Contains(coordinate + NeighborDirections[directionIndex]))
                    {
                        hiddenFaceCount++;
                    }
                }
            }

            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
            Queue<Vector3Int> frontier = new Queue<Vector3Int>();
            foreach (Vector3Int coordinate in internalVoids)
            {
                if (visited.Contains(coordinate))
                {
                    continue;
                }

                int componentSize = 0;
                frontier.Enqueue(coordinate);
                visited.Add(coordinate);

                while (frontier.Count > 0)
                {
                    Vector3Int current = frontier.Dequeue();
                    componentSize++;
                    for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                    {
                        Vector3Int neighbor = current + NeighborDirections[directionIndex];
                        if (!internalVoids.Contains(neighbor) || visited.Contains(neighbor))
                        {
                            continue;
                        }

                        visited.Add(neighbor);
                        frontier.Enqueue(neighbor);
                    }
                }

                if (componentSize > largestInternalVoid)
                {
                    largestInternalVoid = componentSize;
                }

                if (componentSize >= 2 && componentSize <= 12)
                {
                    pocketCount++;
                }
            }
        }

        private static HashSet<Vector3Int> FloodExteriorVoids(HashSet<Vector3Int> occupiedCoordinates, HashSet<Vector3Int> envelopeCoordinates)
        {
            HashSet<Vector3Int> exteriorReachable = new HashSet<Vector3Int>();
            Queue<Vector3Int> frontier = new Queue<Vector3Int>();

            bool initialized = false;
            Vector3Int min = Vector3Int.zero;
            Vector3Int max = Vector3Int.zero;
            foreach (Vector3Int coordinate in envelopeCoordinates)
            {
                if (!initialized)
                {
                    min = coordinate;
                    max = coordinate;
                    initialized = true;
                    continue;
                }

                min = Vector3Int.Min(min, coordinate);
                max = Vector3Int.Max(max, coordinate);
            }

            foreach (Vector3Int coordinate in envelopeCoordinates)
            {
                if (occupiedCoordinates.Contains(coordinate))
                {
                    continue;
                }

                if (coordinate.x == min.x || coordinate.x == max.x
                    || coordinate.y == min.y || coordinate.y == max.y
                    || coordinate.z == min.z || coordinate.z == max.z)
                {
                    exteriorReachable.Add(coordinate);
                    frontier.Enqueue(coordinate);
                }
            }

            while (frontier.Count > 0)
            {
                Vector3Int current = frontier.Dequeue();
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = current + NeighborDirections[directionIndex];
                    if (!envelopeCoordinates.Contains(neighbor)
                        || occupiedCoordinates.Contains(neighbor)
                        || exteriorReachable.Contains(neighbor))
                    {
                        continue;
                    }

                    exteriorReachable.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            return exteriorReachable;
        }

        private static void AddOuterShellLevel(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, int nextFootprint)
        {
            int minX;
            int minZ;
            GetLevelOrigin(gridSize, footprint, out minX, out minZ);
            int maxX = minX + footprint;
            int maxZ = minZ + footprint;

            int innerMinX = 0;
            int innerMinZ = 0;
            int innerMaxX = 0;
            int innerMaxZ = 0;
            if (nextFootprint > 0)
            {
                GetLevelOrigin(gridSize, nextFootprint, out innerMinX, out innerMinZ);
                innerMaxX = innerMinX + nextFootprint;
                innerMaxZ = innerMinZ + nextFootprint;
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    bool isPerimeter = x == minX || x == maxX - 1 || z == minZ || z == maxZ - 1;
                    bool isTerrace = nextFootprint <= 0 || x < innerMinX || x >= innerMaxX || z < innerMinZ || z >= innerMaxZ;
                    if (isPerimeter || isTerrace)
                    {
                        occupiedCoordinates.Add(new Vector3Int(x, level, z));
                    }
                }
            }
        }

        private static void AddClusterLevel(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, ShellPlan plan, int level, int footprint)
        {
            ClusterKind clusterKind = ResolveClusterKind(level, plan.StepCount);
            switch (clusterKind)
            {
                case ClusterKind.Base:
                    AddBaseCluster(occupiedCoordinates, gridSize, level, footprint);
                    break;

                case ClusterKind.Lower:
                    AddLowerCluster(occupiedCoordinates, gridSize, level, footprint);
                    break;

                case ClusterKind.Middle:
                    AddMiddleCluster(occupiedCoordinates, gridSize, level, footprint);
                    break;

                case ClusterKind.Upper:
                    AddUpperCluster(occupiedCoordinates, gridSize, level, footprint);
                    break;

                case ClusterKind.Apex:
                    AddApexCluster(occupiedCoordinates, gridSize, level, footprint);
                    break;
            }
        }

        private static ClusterKind ResolveClusterKind(int level, int stepCount)
        {
            if (level >= stepCount - 1)
            {
                return ClusterKind.Apex;
            }

            float normalizedHeight = stepCount <= 1 ? 1f : level / (float)(stepCount - 1);
            if (normalizedHeight < 0.28f)
            {
                return ClusterKind.Base;
            }

            if (normalizedHeight < 0.52f)
            {
                return ClusterKind.Lower;
            }

            if (normalizedHeight < 0.76f)
            {
                return ClusterKind.Middle;
            }

            return ClusterKind.Upper;
        }

        private static void AddBaseCluster(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            int cornerSize = footprint >= 11 ? 2 : 1;
            AddCornerPods(occupiedCoordinates, gridSize, level, footprint, cornerSize, 1);
            AddSideAnchor(occupiedCoordinates, gridSize, level, footprint, false);
            AddSideAnchor(occupiedCoordinates, gridSize, level, footprint, true);
        }

        private static void AddLowerCluster(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            AddInnerRing(occupiedCoordinates, gridSize, level, footprint, 2, 1, level % 4);
            AddGalleryBars(occupiedCoordinates, gridSize, level, footprint, true);
        }

        private static void AddMiddleCluster(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            AddCrossSpines(occupiedCoordinates, gridSize, level, footprint, level % 2 == 0);
            AddPocketPods(occupiedCoordinates, gridSize, level, footprint);
        }

        private static void AddUpperCluster(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            AddInnerRing(occupiedCoordinates, gridSize, level, footprint, 1, 1, (level + 1) % 4);
            AddPocketPods(occupiedCoordinates, gridSize, level, footprint);
        }

        private static void AddApexCluster(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            if (footprint <= 1)
            {
                AddCenterCell(occupiedCoordinates, gridSize, level, footprint);
                return;
            }

            int center = footprint / 2;
            AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center, center);
            if (footprint >= 3)
            {
                AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center - 1, center);
                AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center + 1, center);
            }
        }

        private static void AddCornerPods(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, int podSize, int inset)
        {
            if (footprint < 5)
            {
                return;
            }

            int min = inset;
            int max = footprint - inset - podSize;
            if (max < min)
            {
                return;
            }

            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, min, min + podSize - 1, min, min + podSize - 1);
            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, max, max + podSize - 1, min, min + podSize - 1);
            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, min, min + podSize - 1, max, max + podSize - 1);
            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, max, max + podSize - 1, max, max + podSize - 1);
        }

        private static void AddSideAnchor(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, bool horizontal)
        {
            if (footprint < 7)
            {
                return;
            }

            int center = footprint / 2;
            if (horizontal)
            {
                AddLocalRect(occupiedCoordinates, gridSize, level, footprint, 2, footprint - 3, center - 1, center - 1);
                return;
            }

            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, center - 1, center - 1, 2, footprint - 3);
        }

        private static void AddLowerClusterRoomRing(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            AddInnerRing(occupiedCoordinates, gridSize, level, footprint, 2, 1, level % 4);
        }

        private static void AddGalleryBars(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, bool leftRight)
        {
            if (footprint < 7)
            {
                return;
            }

            if (leftRight)
            {
                AddLocalRect(occupiedCoordinates, gridSize, level, footprint, 2, 2, 2, footprint - 3);
                AddLocalRect(occupiedCoordinates, gridSize, level, footprint, footprint - 3, footprint - 3, 2, footprint - 3);
                return;
            }

            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, 2, footprint - 3, 2, 2);
            AddLocalRect(occupiedCoordinates, gridSize, level, footprint, 2, footprint - 3, footprint - 3, footprint - 3);
        }

        private static void AddInnerRing(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, int inset, int thickness, int gapDirection)
        {
            int min = inset;
            int max = footprint - inset - 1;
            if (max - min < 2)
            {
                return;
            }

            int center = footprint / 2;
            for (int x = min; x <= max; x++)
            {
                for (int z = min; z <= max; z++)
                {
                    bool onRing = x < min + thickness || x > max - thickness || z < min + thickness || z > max - thickness;
                    if (!onRing)
                    {
                        continue;
                    }

                    bool isGap = false;
                    switch (gapDirection)
                    {
                        case 0:
                            isGap = z == min && Mathf.Abs(x - center) <= 1;
                            break;
                        case 1:
                            isGap = x == max && Mathf.Abs(z - center) <= 1;
                            break;
                        case 2:
                            isGap = z == max && Mathf.Abs(x - center) <= 1;
                            break;
                        default:
                            isGap = x == min && Mathf.Abs(z - center) <= 1;
                            break;
                    }

                    if (!isGap)
                    {
                        AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, x, z);
                    }
                }
            }
        }

        private static void AddCrossSpines(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, bool horizontalFirst)
        {
            if (footprint < 7)
            {
                return;
            }

            int center = footprint / 2;
            if (horizontalFirst)
            {
                for (int x = 2; x <= footprint - 3; x++)
                {
                    if (Mathf.Abs(x - center) <= 1)
                    {
                        continue;
                    }

                    AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, x, center);
                }

                for (int z = 3; z <= footprint - 4; z++)
                {
                    AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center, z);
                }

                return;
            }

            for (int z = 2; z <= footprint - 3; z++)
            {
                if (Mathf.Abs(z - center) <= 1)
                {
                    continue;
                }

                AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center, z);
            }

            for (int x = 3; x <= footprint - 4; x++)
            {
                AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, x, center);
            }
        }

        private static void AddPocketPods(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            if (footprint < 6)
            {
                return;
            }

            int center = footprint / 2;
            AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center - 1, 1);
            AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center + 1, footprint - 2);
        }

        private static void AddCenterCell(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint)
        {
            int center = footprint / 2;
            AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, center, center);
        }

        private static void AddLocalRect(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, int minLocalX, int maxLocalX, int minLocalZ, int maxLocalZ)
        {
            for (int localX = minLocalX; localX <= maxLocalX; localX++)
            {
                for (int localZ = minLocalZ; localZ <= maxLocalZ; localZ++)
                {
                    AddLocalCoordinate(occupiedCoordinates, gridSize, level, footprint, localX, localZ);
                }
            }
        }

        private static void AddLocalCoordinate(HashSet<Vector3Int> occupiedCoordinates, VoxelGridSize gridSize, int level, int footprint, int localX, int localZ)
        {
            if (localX < 0 || localZ < 0 || localX >= footprint || localZ >= footprint)
            {
                return;
            }

            int minX;
            int minZ;
            GetLevelOrigin(gridSize, footprint, out minX, out minZ);
            occupiedCoordinates.Add(new Vector3Int(minX + localX, level, minZ + localZ));
        }

        private static int GetFootprintAtLevel(ShellPlan plan, int level)
        {
            return Mathf.Max(1, plan.BaseFootprint - (level * 2));
        }

        private static void GetLevelOrigin(VoxelGridSize gridSize, int footprint, out int minX, out int minZ)
        {
            minX = Mathf.Max(0, (gridSize.Width - footprint) / 2);
            minZ = Mathf.Max(0, (gridSize.Depth - footprint) / 2);
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
                        UseCustomLocalPosition = false,
                    });
                }
            }

            return shell;
        }

        private static VoxelGridDirection ToGridDirection(Vector3Int offset)
        {
            if (offset == Vector3Int.right)
            {
                return VoxelGridDirection.Right;
            }

            if (offset == Vector3Int.left)
            {
                return VoxelGridDirection.Left;
            }

            if (offset == Vector3Int.up)
            {
                return VoxelGridDirection.Up;
            }

            if (offset == Vector3Int.down)
            {
                return VoxelGridDirection.Down;
            }

            if (offset.z > 0)
            {
                return VoxelGridDirection.Forward;
            }

            return VoxelGridDirection.Back;
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
