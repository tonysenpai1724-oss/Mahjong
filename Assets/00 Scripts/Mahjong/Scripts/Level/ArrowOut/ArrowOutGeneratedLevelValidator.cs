using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MahjongOut3D.LevelSystem.ArrowOutGeneration
{
    /// <summary>
    /// Summarizes generator quality checks so designers can spot dense or flat layouts.
    /// </summary>
    public sealed class ArrowOutGeneratedLevelValidationReport
    {
        public bool IsValid { get; set; } = true;
        public List<string> Warnings { get; } = new List<string>();

        public string ToMultilineString()
        {
            return string.Join("\n", Warnings);
        }
    }

    /// <summary>
    /// Validates that a generated layout remains sparse, clustered and peel-friendly.
    /// </summary>
    public static class ArrowOutGeneratedLevelValidator
    {
        public static ArrowOutGeneratedLevelValidationReport Validate(ArrowOutGeneratedLevel level)
        {
            ArrowOutGeneratedLevelValidationReport report = new ArrowOutGeneratedLevelValidationReport();
            if (level == null)
            {
                report.IsValid = false;
                report.Warnings.Add("Generated level is null.");
                return report;
            }

            if (level.Tiles.Count < 8)
            {
                report.IsValid = false;
                report.Warnings.Add($"{level.LevelName}: tile count is too low ({level.Tiles.Count}).");
            }

            if (level.FillRatio > 0.42f)
            {
                report.Warnings.Add($"{level.LevelName}: fill ratio is {level.FillRatio:0.00}; level may feel too solid.");
            }

            int nonBridgeClusters = level.Clusters.Count(cluster => !cluster.IsBridge);
            if (nonBridgeClusters < 2)
            {
                report.Warnings.Add($"{level.LevelName}: only {nonBridgeClusters} gameplay cluster found.");
            }

            float averagePairDistance = CalculateAveragePairDistance(level.Tiles);
            if (averagePairDistance > 6f)
            {
                report.Warnings.Add($"{level.LevelName}: average pair distance is {averagePairDistance:0.0}; matches may feel too random.");
            }

            float averageExposure = CalculateAverageExposure(level.Tiles.Select(tile => tile.Coordinate).ToList());
            if (averageExposure < 1.8f)
            {
                report.Warnings.Add($"{level.LevelName}: average exposure is {averageExposure:0.0}; reveal flow may be too flat.");
            }

            return report;
        }

        private static float CalculateAveragePairDistance(IReadOnlyList<GeneratedTileData> tiles)
        {
            Dictionary<int, List<GeneratedTileData>> byMatch = new Dictionary<int, List<GeneratedTileData>>();
            for (int index = 0; index < tiles.Count; index++)
            {
                GeneratedTileData tile = tiles[index];
                if (!byMatch.TryGetValue(tile.MatchId, out List<GeneratedTileData> pair))
                {
                    pair = new List<GeneratedTileData>();
                    byMatch.Add(tile.MatchId, pair);
                }

                pair.Add(tile);
            }

            float total = 0f;
            int count = 0;
            foreach (KeyValuePair<int, List<GeneratedTileData>> entry in byMatch)
            {
                if (entry.Value.Count != 2)
                {
                    continue;
                }

                Vector3Int first = entry.Value[0].Coordinate;
                Vector3Int second = entry.Value[1].Coordinate;
                total += Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) + Mathf.Abs(first.z - second.z);
                count++;
            }

            return count > 0 ? total / count : 0f;
        }

        private static float CalculateAverageExposure(List<Vector3Int> coordinates)
        {
            HashSet<Vector3Int> occupied = new HashSet<Vector3Int>(coordinates);
            if (occupied.Count == 0)
            {
                return 0f;
            }

            float total = 0f;
            foreach (Vector3Int coordinate in occupied)
            {
                int neighborCount = 0;
                foreach (Vector3Int neighborOffset in SparseVoxelShape.GetNeighborOffsets())
                {
                    if (occupied.Contains(coordinate + neighborOffset))
                    {
                        neighborCount++;
                    }
                }

                total += 6 - neighborCount;
            }

            return total / occupied.Count;
        }
    }
}
