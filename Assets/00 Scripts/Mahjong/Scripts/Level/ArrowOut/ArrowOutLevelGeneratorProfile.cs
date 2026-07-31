using System;
using UnityEngine;

namespace MahjongOut3D.LevelSystem.ArrowOutGeneration
{
    /// <summary>
    /// Stores the gameplay-first tuning used by the Arrow Out style level generator.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Arrow Out Generator Profile", fileName = "ArrowOutLevelGeneratorProfile")]
    public sealed class ArrowOutLevelGeneratorProfile : ScriptableObject
    {
        [SerializeField] private int baseSeed = 20260731;
        [SerializeField] private int surfaceSamplesPerTriangleEdge = 3;
        [SerializeField] private int gridPadding = 2;
        [SerializeField] private DifficultyTuning easy = DifficultyTuning.CreateEasyDefaults();
        [SerializeField] private DifficultyTuning normal = DifficultyTuning.CreateNormalDefaults();
        [SerializeField] private DifficultyTuning hard = DifficultyTuning.CreateHardDefaults();
        [SerializeField] private DifficultyTuning expert = DifficultyTuning.CreateExpertDefaults();

        /// <summary>
        /// Gets the shared seed offset used by every generation request.
        /// </summary>
        public int BaseSeed => baseSeed;

        /// <summary>
        /// Gets how many barycentric subdivisions are sampled per triangle edge.
        /// </summary>
        public int SurfaceSamplesPerTriangleEdge => Mathf.Max(2, surfaceSamplesPerTriangleEdge);

        /// <summary>
        /// Gets the amount of empty border voxels added around the imported mesh.
        /// </summary>
        public int GridPadding => Mathf.Max(1, gridPadding);

        /// <summary>
        /// Resolves the tuning block used for the requested difficulty.
        /// </summary>
        public DifficultyTuning GetDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    return easy;
                case LevelDifficulty.Hard:
                    return hard;
                case LevelDifficulty.Expert:
                    return expert;
                default:
                    return normal;
            }
        }

        /// <summary>
        /// Stores the difficulty-specific controls that shape the peeling flow.
        /// </summary>
        [Serializable]
        public sealed class DifficultyTuning
        {
            [SerializeField] private int targetLongestSide = 12;
            [SerializeField] private int shellThicknessMin = 2;
            [SerializeField] private int shellThicknessMax = 3;
            [SerializeField] private int clusterCountMin = 4;
            [SerializeField] private int clusterCountMax = 6;
            [SerializeField] private int clusterGap = 2;
            [SerializeField] private int pocketCountMin = 3;
            [SerializeField] private int pocketCountMax = 5;
            [SerializeField] private int tunnelCountMin = 1;
            [SerializeField] private int tunnelCountMax = 2;
            [SerializeField] private int bridgeCountMin = 2;
            [SerializeField] private int bridgeCountMax = 4;
            [SerializeField] private int minimumClusterSize = 8;
            [SerializeField] private int targetPairCount = 32;
            [SerializeField] private int maxLocalPairDistance = 5;
            [SerializeField] private float flipRotationChance = 0.2f;

            public int TargetLongestSide => Mathf.Max(6, targetLongestSide);
            public int ShellThicknessMin => Mathf.Max(1, shellThicknessMin);
            public int ShellThicknessMax => Mathf.Max(ShellThicknessMin, shellThicknessMax);
            public int ClusterCountMin => Mathf.Max(2, clusterCountMin);
            public int ClusterCountMax => Mathf.Max(ClusterCountMin, clusterCountMax);
            public int ClusterGap => Mathf.Max(1, clusterGap);
            public int PocketCountMin => Mathf.Max(0, pocketCountMin);
            public int PocketCountMax => Mathf.Max(PocketCountMin, pocketCountMax);
            public int TunnelCountMin => Mathf.Max(0, tunnelCountMin);
            public int TunnelCountMax => Mathf.Max(TunnelCountMin, tunnelCountMax);
            public int BridgeCountMin => Mathf.Max(1, bridgeCountMin);
            public int BridgeCountMax => Mathf.Max(BridgeCountMin, bridgeCountMax);
            public int MinimumClusterSize => Mathf.Max(4, minimumClusterSize);
            public int TargetPairCount => Mathf.Max(4, targetPairCount);
            public int MaxLocalPairDistance => Mathf.Max(1, maxLocalPairDistance);
            public float FlipRotationChance => Mathf.Clamp01(flipRotationChance);

            public static DifficultyTuning CreateEasyDefaults()
            {
                return new DifficultyTuning
                {
                    targetLongestSide = 10,
                    shellThicknessMin = 2,
                    shellThicknessMax = 2,
                    clusterCountMin = 3,
                    clusterCountMax = 4,
                    clusterGap = 2,
                    pocketCountMin = 2,
                    pocketCountMax = 3,
                    tunnelCountMin = 1,
                    tunnelCountMax = 1,
                    bridgeCountMin = 2,
                    bridgeCountMax = 2,
                    minimumClusterSize = 8,
                    targetPairCount = 18,
                    maxLocalPairDistance = 4,
                    flipRotationChance = 0.1f,
                };
            }

            public static DifficultyTuning CreateNormalDefaults()
            {
                return new DifficultyTuning
                {
                    targetLongestSide = 12,
                    shellThicknessMin = 2,
                    shellThicknessMax = 3,
                    clusterCountMin = 4,
                    clusterCountMax = 5,
                    clusterGap = 2,
                    pocketCountMin = 3,
                    pocketCountMax = 5,
                    tunnelCountMin = 1,
                    tunnelCountMax = 2,
                    bridgeCountMin = 2,
                    bridgeCountMax = 3,
                    minimumClusterSize = 10,
                    targetPairCount = 28,
                    maxLocalPairDistance = 5,
                    flipRotationChance = 0.2f,
                };
            }

            public static DifficultyTuning CreateHardDefaults()
            {
                return new DifficultyTuning
                {
                    targetLongestSide = 14,
                    shellThicknessMin = 2,
                    shellThicknessMax = 3,
                    clusterCountMin = 5,
                    clusterCountMax = 7,
                    clusterGap = 3,
                    pocketCountMin = 4,
                    pocketCountMax = 6,
                    tunnelCountMin = 2,
                    tunnelCountMax = 3,
                    bridgeCountMin = 3,
                    bridgeCountMax = 4,
                    minimumClusterSize = 10,
                    targetPairCount = 38,
                    maxLocalPairDistance = 6,
                    flipRotationChance = 0.3f,
                };
            }

            public static DifficultyTuning CreateExpertDefaults()
            {
                return new DifficultyTuning
                {
                    targetLongestSide = 16,
                    shellThicknessMin = 3,
                    shellThicknessMax = 3,
                    clusterCountMin = 6,
                    clusterCountMax = 8,
                    clusterGap = 3,
                    pocketCountMin = 5,
                    pocketCountMax = 7,
                    tunnelCountMin = 2,
                    tunnelCountMax = 4,
                    bridgeCountMin = 3,
                    bridgeCountMax = 5,
                    minimumClusterSize = 12,
                    targetPairCount = 48,
                    maxLocalPairDistance = 6,
                    flipRotationChance = 0.35f,
                };
            }
        }
    }
}
