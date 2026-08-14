using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric hollow cylinder shells that match the authored preview layout.
    /// The smallest possible core is the preview cylinder: 16 segments and 6 vertical rows.
    /// </summary>
    internal sealed class CylinderShellLayoutBuilder
    {
        private const int CoreSegmentCount = 16;
        private const int CoreRowCount = 6;
        private const int SegmentStepPerOuterShell = 4;
        private const int MaximumShellCount = 6;

        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public CylinderShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(int targetLayerCount, int minTileCount, int maxTileCount, System.Random random)
        {
            int safeMinTileCount = Mathf.Max(2, minTileCount);
            int safeMaxTileCount = Mathf.Max(safeMinTileCount, maxTileCount);
            int desiredShellCount = Mathf.Clamp(targetLayerCount, 1, MaximumShellCount);
            int targetTileCount = Mathf.Clamp(((safeMinTileCount + safeMaxTileCount) / 2) & ~1, safeMinTileCount, safeMaxTileCount);

            List<CylinderBlueprint> candidates = BuildCandidates(desiredShellCount, safeMinTileCount, safeMaxTileCount, targetTileCount);
            if (candidates.Count == 0)
            {
                candidates = BuildCandidates(1, 2, Mathf.Max(2, safeMaxTileCount), targetTileCount);
            }

            CylinderBlueprint selected = SelectBlueprint(candidates, random);
            return BuildShells(selected);
        }

        private List<CylinderBlueprint> BuildCandidates(int desiredShellCount, int minTileCount, int maxTileCount, int targetTileCount)
        {
            List<CylinderBlueprint> candidates = new List<CylinderBlueprint>();
            int safeDesiredShellCount = Mathf.Clamp(desiredShellCount, 1, MaximumShellCount);

            for (int shellCount = safeDesiredShellCount; shellCount >= 1; shellCount--)
            {
                List<int> segmentCounts = BuildSegmentCounts(shellCount);
                int totalTileCount = CoreRowCount * Sum(segmentCounts);
                if (totalTileCount > maxTileCount)
                {
                    continue;
                }

                float balanceDifference = GetBalanceDifference(segmentCounts[segmentCounts.Count - 1], CoreRowCount);
                bool meetsMinimum = totalTileCount >= minTileCount;
                candidates.Add(new CylinderBlueprint(segmentCounts, CoreRowCount, totalTileCount, Mathf.Abs(totalTileCount - targetTileCount), balanceDifference, meetsMinimum));
            }

            if (candidates.Count > 0)
            {
                return candidates;
            }

            List<int> fallbackSegmentCounts = BuildSegmentCounts(1);
            int fallbackTileCount = CoreRowCount * Sum(fallbackSegmentCounts);
            candidates.Add(new CylinderBlueprint(fallbackSegmentCounts, CoreRowCount, fallbackTileCount, Mathf.Abs(fallbackTileCount - targetTileCount), GetBalanceDifference(fallbackSegmentCounts[0], CoreRowCount), fallbackTileCount >= minTileCount));
            return candidates;
        }

        private List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(CylinderBlueprint blueprint)
        {
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(blueprint.SegmentCounts.Count);
            float tangentStep = GetTangentStep();
            float verticalStep = GetVerticalStep();

            for (int shellIndex = 0; shellIndex < blueprint.SegmentCounts.Count; shellIndex++)
            {
                int segmentCount = blueprint.SegmentCounts[shellIndex];
                float radius = GetRingRadius(segmentCount, tangentStep);
                shells.Add(BuildShell(segmentCount, blueprint.RowCount, radius, verticalStep));
            }

            return shells;
        }

        private List<ProceduralLevelBatchGenerator.TilePlacementData> BuildShell(int segmentCount, int rowCount, float radius, float verticalStep)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>(segmentCount * rowCount);
            float outwardPadding = 0.02f;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                float localY = GetCenteredPanelCoordinate(rowIndex, rowCount, verticalStep);

                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    float angleRadians = ((float)segmentIndex / segmentCount) * Mathf.PI * 2f;
                    Vector3 outwardNormal = new Vector3(Mathf.Sin(angleRadians), 0f, Mathf.Cos(angleRadians)).normalized;
                    float yawDegrees = Mathf.Atan2(outwardNormal.x, outwardNormal.z) * Mathf.Rad2Deg;

                    shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                    {
                        Coordinate = Vector3Int.zero,
                        FacingDirection = ToFacingDirection(outwardNormal),
                        SurfaceSlotIndex = -1,
                        CustomLocalPosition = (outwardNormal * (radius + outwardPadding)) + (Vector3.up * localY),
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
            for (int shellIndex = Mathf.Max(1, shellCount) - 1; shellIndex >= 0; shellIndex--)
            {
                segmentCounts.Add(CoreSegmentCount + (shellIndex * SegmentStepPerOuterShell));
            }

            return segmentCounts;
        }

        private static CylinderBlueprint SelectBlueprint(List<CylinderBlueprint> candidates, System.Random random)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return new CylinderBlueprint(new List<int> { CoreSegmentCount }, CoreRowCount, CoreSegmentCount * CoreRowCount, 0, 0f, true);
            }

            candidates.Sort(CompareBlueprints);
            int topCount = Mathf.Min(3, candidates.Count);
            if (random == null || topCount <= 1)
            {
                return candidates[0];
            }

            int totalWeight = 0;
            for (int index = 0; index < topCount; index++)
            {
                totalWeight += topCount - index;
            }

            int roll = random.Next(0, totalWeight);
            for (int index = 0; index < topCount; index++)
            {
                int weight = topCount - index;
                if (roll < weight)
                {
                    return candidates[index];
                }

                roll -= weight;
            }

            return candidates[0];
        }

        private static int CompareBlueprints(CylinderBlueprint left, CylinderBlueprint right)
        {
            if (left.MeetsMinimumTileCount != right.MeetsMinimumTileCount)
            {
                return right.MeetsMinimumTileCount.CompareTo(left.MeetsMinimumTileCount);
            }

            int differenceComparison = left.Difference.CompareTo(right.Difference);
            if (differenceComparison != 0)
            {
                return differenceComparison;
            }

            int shellCountComparison = right.SegmentCounts.Count.CompareTo(left.SegmentCounts.Count);
            if (shellCountComparison != 0)
            {
                return shellCountComparison;
            }

            int balanceComparison = left.BalanceDifference.CompareTo(right.BalanceDifference);
            if (balanceComparison != 0)
            {
                return balanceComparison;
            }

            return right.TotalTileCount.CompareTo(left.TotalTileCount);
        }

        private float GetTangentStep()
        {
            return Mathf.Max(0.01f, tileMetrics.FaceWidth + inPlaneGap);
        }

        private float GetVerticalStep()
        {
            return Mathf.Max(0.01f, tileMetrics.FaceHeight + inPlaneGap);
        }

        private float GetBalanceDifference(int segmentCount, int rowCount)
        {
            float tangentStep = GetTangentStep();
            float verticalStep = GetVerticalStep();
            float radius = GetRingRadius(segmentCount, tangentStep);
            float totalHeight = rowCount * verticalStep;
            float outerDiameter = (radius * 2f) + tileMetrics.Thickness;
            return Mathf.Abs(totalHeight - outerDiameter) / Mathf.Max(0.01f, Mathf.Max(totalHeight, outerDiameter));
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
