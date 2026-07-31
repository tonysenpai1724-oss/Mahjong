using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MahjongOut3D.LevelSystem.ArrowOutGeneration
{
    /// <summary>
    /// Orchestrates the gameplay-first level generation pipeline.
    /// </summary>
    public sealed class ArrowOutLevelGeneratorPipeline
    {
        private readonly IMeshVoxelizer meshVoxelizer;
        private readonly ISparseShapeCarver shapeCarver;
        private readonly IClusterLayoutPlanner clusterLayoutPlanner;
        private readonly IMatchPlanner matchPlanner;

        public ArrowOutLevelGeneratorPipeline(
            IMeshVoxelizer meshVoxelizer,
            ISparseShapeCarver shapeCarver,
            IClusterLayoutPlanner clusterLayoutPlanner,
            IMatchPlanner matchPlanner)
        {
            this.meshVoxelizer = meshVoxelizer ?? throw new ArgumentNullException(nameof(meshVoxelizer));
            this.shapeCarver = shapeCarver ?? throw new ArgumentNullException(nameof(shapeCarver));
            this.clusterLayoutPlanner = clusterLayoutPlanner ?? throw new ArgumentNullException(nameof(clusterLayoutPlanner));
            this.matchPlanner = matchPlanner ?? throw new ArgumentNullException(nameof(matchPlanner));
        }

        /// <summary>
        /// Creates the default production pipeline.
        /// </summary>
        public static ArrowOutLevelGeneratorPipeline CreateDefault()
        {
            return new ArrowOutLevelGeneratorPipeline(
                new SurfaceSampleMeshVoxelizer(),
                new PocketTunnelShapeCarver(),
                new ClusteredShellLayoutPlanner(),
                new PeelWaveMatchPlanner());
        }

        /// <summary>
        /// Generates one sparse clustered level from the specified request.
        /// </summary>
        public ArrowOutGeneratedLevel Generate(
            ArrowOutMeshLevelGenerator.Request request,
            ArrowOutLevelGeneratorProfile profile,
            VoxelGridLayoutSettings layoutOverride,
            int requestIndex)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            ArrowOutLevelGeneratorProfile.DifficultyTuning tuning = profile.GetDifficulty(request.Difficulty);
            int resolvedSeed = profile.BaseSeed + requestIndex * 7919 + request.SeedOffset;
            System.Random random = new System.Random(resolvedSeed);
            ArrowOutGenerationContext context = new ArrowOutGenerationContext(request, profile, tuning, layoutOverride, random, resolvedSeed);

            SparseVoxelShape shellShape = meshVoxelizer.Voxelize(context);
            SparseVoxelShape carvedShape = shapeCarver.Carve(shellShape, context);
            ClusterLayoutResult layout = clusterLayoutPlanner.Plan(carvedShape, context);
            List<GeneratedTileData> tiles = matchPlanner.PlanMatches(layout, context);
            return ArrowOutGeneratedLevel.Create(context, layout, tiles);
        }
    }

    /// <summary>
    /// Common generation services used by the pipeline steps.
    /// </summary>
    public sealed class ArrowOutGenerationContext
    {
        public ArrowOutGenerationContext(
            ArrowOutMeshLevelGenerator.Request request,
            ArrowOutLevelGeneratorProfile profile,
            ArrowOutLevelGeneratorProfile.DifficultyTuning tuning,
            VoxelGridLayoutSettings layoutOverride,
            System.Random random,
            int seed)
        {
            Request = request;
            Profile = profile;
            Tuning = tuning;
            LayoutOverride = layoutOverride;
            Random = random;
            Seed = seed;
            TargetPairCount = request.TargetPairCountOverride > 0 ? request.TargetPairCountOverride : tuning.TargetPairCount;
            ShellThickness = random.Next(tuning.ShellThicknessMin, tuning.ShellThicknessMax + 1);
            ClusterCount = random.Next(tuning.ClusterCountMin, tuning.ClusterCountMax + 1);
            PocketCount = random.Next(tuning.PocketCountMin, tuning.PocketCountMax + 1);
            TunnelCount = random.Next(tuning.TunnelCountMin, tuning.TunnelCountMax + 1);
            BridgeCount = random.Next(tuning.BridgeCountMin, tuning.BridgeCountMax + 1);
        }

        public ArrowOutMeshLevelGenerator.Request Request { get; }
        public ArrowOutLevelGeneratorProfile Profile { get; }
        public ArrowOutLevelGeneratorProfile.DifficultyTuning Tuning { get; }
        public VoxelGridLayoutSettings LayoutOverride { get; }
        public System.Random Random { get; }
        public int Seed { get; }
        public int TargetPairCount { get; }
        public int ShellThickness { get; }
        public int ClusterCount { get; }
        public int PocketCount { get; }
        public int TunnelCount { get; }
        public int BridgeCount { get; }
    }

    /// <summary>
    /// Stores the generated tile payload before it is exported to a level asset.
    /// </summary>
    public sealed class ArrowOutGeneratedLevel
    {
        private ArrowOutGeneratedLevel()
        {
        }

        public string LevelName { get; private set; }
        public LevelShapeType Shape { get; private set; }
        public LevelDifficulty Difficulty { get; private set; }
        public VoxelGridLayoutSettings LayoutOverride { get; private set; }
        public VoxelGridSize GridSize { get; private set; }
        public IReadOnlyList<ArrowOutClusterData> Clusters { get; private set; }
        public IReadOnlyList<GeneratedTileData> Tiles { get; private set; }
        public float FillRatio { get; private set; }

        public static ArrowOutGeneratedLevel Create(ArrowOutGenerationContext context, ClusterLayoutResult layout, List<GeneratedTileData> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                throw new InvalidOperationException($"Generator created an empty level for '{context.Request.LevelName}'.");
            }

            Vector3Int min = tiles[0].Coordinate;
            Vector3Int max = tiles[0].Coordinate;

            for (int index = 1; index < tiles.Count; index++)
            {
                Vector3Int coordinate = tiles[index].Coordinate;
                min = Vector3Int.Min(min, coordinate);
                max = Vector3Int.Max(max, coordinate);
            }

            Vector3Int normalizeOffset = new Vector3Int(-min.x, -min.y, -min.z);
            for (int index = 0; index < tiles.Count; index++)
            {
                tiles[index].Coordinate += normalizeOffset;
            }

            List<ArrowOutClusterData> normalizedClusters = new List<ArrowOutClusterData>(layout.Clusters.Count);
            for (int index = 0; index < layout.Clusters.Count; index++)
            {
                ArrowOutClusterData cluster = layout.Clusters[index];
                List<Vector3Int> normalizedVoxels = new List<Vector3Int>(cluster.Voxels.Count);
                for (int voxelIndex = 0; voxelIndex < cluster.Voxels.Count; voxelIndex++)
                {
                    normalizedVoxels.Add(cluster.Voxels[voxelIndex] + normalizeOffset);
                }

                normalizedClusters.Add(cluster.WithVoxels(normalizedVoxels));
            }

            max += normalizeOffset;
            VoxelGridSize gridSize = new VoxelGridSize(max.x + 1, max.y + 1, max.z + 1);
            int volume = Mathf.Max(1, gridSize.Volume);

            return new ArrowOutGeneratedLevel
            {
                LevelName = context.Request.LevelName,
                Shape = context.Request.Shape,
                Difficulty = context.Request.Difficulty,
                LayoutOverride = context.LayoutOverride,
                GridSize = gridSize,
                Clusters = normalizedClusters,
                Tiles = tiles,
                FillRatio = tiles.Count / (float)volume,
            };
        }
    }

    /// <summary>
    /// Stores one placed cluster in the final sparse layout.
    /// </summary>
    public sealed class ArrowOutClusterData
    {
        public ArrowOutClusterData(int id, List<Vector3Int> voxels, bool isBridge)
        {
            Id = id;
            Voxels = voxels ?? throw new ArgumentNullException(nameof(voxels));
            IsBridge = isBridge;
        }

        public int Id { get; }
        public List<Vector3Int> Voxels { get; }
        public bool IsBridge { get; }

        public ArrowOutClusterData WithVoxels(List<Vector3Int> voxels)
        {
            return new ArrowOutClusterData(Id, voxels, IsBridge);
        }
    }

    /// <summary>
    /// Stores one generated tile before it becomes a LevelTileDefinition.
    /// </summary>
    public sealed class GeneratedTileData
    {
        public int MatchId;
        public int ClusterId;
        public Vector3Int Coordinate;
        public Vector3 LocalEulerAngles;
    }

    /// <summary>
    /// Stores the placed clusters and occupancy lookup used for pairing.
    /// </summary>
    public sealed class ClusterLayoutResult
    {
        public List<ArrowOutClusterData> Clusters { get; } = new List<ArrowOutClusterData>();
        public Dictionary<Vector3Int, int> ClusterByCoordinate { get; } = new Dictionary<Vector3Int, int>();
        public HashSet<Vector3Int> Occupied { get; } = new HashSet<Vector3Int>();
    }

    public interface IMeshVoxelizer
    {
        SparseVoxelShape Voxelize(ArrowOutGenerationContext context);
    }

    public interface ISparseShapeCarver
    {
        SparseVoxelShape Carve(SparseVoxelShape shellShape, ArrowOutGenerationContext context);
    }

    public interface IClusterLayoutPlanner
    {
        ClusterLayoutResult Plan(SparseVoxelShape sparseShape, ArrowOutGenerationContext context);
    }

    public interface IMatchPlanner
    {
        List<GeneratedTileData> PlanMatches(ClusterLayoutResult layout, ArrowOutGenerationContext context);
    }

    /// <summary>
    /// Sparse voxel container optimized for generator operations.
    /// </summary>
    public sealed class SparseVoxelShape
    {
        private static readonly Vector3Int[] NeighborOffsets =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        private readonly HashSet<Vector3Int> occupied;

        public SparseVoxelShape()
        {
            occupied = new HashSet<Vector3Int>();
        }

        public SparseVoxelShape(IEnumerable<Vector3Int> coordinates)
        {
            occupied = new HashSet<Vector3Int>(coordinates ?? Array.Empty<Vector3Int>());
        }

        public int Count => occupied.Count;
        public IEnumerable<Vector3Int> Coordinates => occupied;

        public bool Add(Vector3Int coordinate)
        {
            return occupied.Add(coordinate);
        }

        public bool Remove(Vector3Int coordinate)
        {
            return occupied.Remove(coordinate);
        }

        public bool Contains(Vector3Int coordinate)
        {
            return occupied.Contains(coordinate);
        }

        public SparseVoxelShape Clone()
        {
            return new SparseVoxelShape(occupied);
        }

        public int GetNeighborCount(Vector3Int coordinate)
        {
            int count = 0;
            for (int index = 0; index < NeighborOffsets.Length; index++)
            {
                if (occupied.Contains(coordinate + NeighborOffsets[index]))
                {
                    count++;
                }
            }

            return count;
        }

        public int GetExposure(Vector3Int coordinate)
        {
            return NeighborOffsets.Length - GetNeighborCount(coordinate);
        }

        public List<List<Vector3Int>> GetConnectedComponents()
        {
            List<List<Vector3Int>> components = new List<List<Vector3Int>>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
            Queue<Vector3Int> frontier = new Queue<Vector3Int>();

            foreach (Vector3Int coordinate in occupied)
            {
                if (visited.Contains(coordinate))
                {
                    continue;
                }

                List<Vector3Int> component = new List<Vector3Int>();
                frontier.Enqueue(coordinate);
                visited.Add(coordinate);

                while (frontier.Count > 0)
                {
                    Vector3Int current = frontier.Dequeue();
                    component.Add(current);

                    for (int index = 0; index < NeighborOffsets.Length; index++)
                    {
                        Vector3Int neighbor = current + NeighborOffsets[index];
                        if (!occupied.Contains(neighbor) || visited.Contains(neighbor))
                        {
                            continue;
                        }

                        visited.Add(neighbor);
                        frontier.Enqueue(neighbor);
                    }
                }

                components.Add(component);
            }

            return components;
        }

        public BoundsInt GetBounds()
        {
            if (occupied.Count == 0)
            {
                return new BoundsInt(Vector3Int.zero, Vector3Int.one);
            }

            bool initialized = false;
            Vector3Int min = Vector3Int.zero;
            Vector3Int max = Vector3Int.zero;
            foreach (Vector3Int coordinate in occupied)
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

            return new BoundsInt(min, max - min + Vector3Int.one);
        }

        public Vector3 GetCentroid()
        {
            if (occupied.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            foreach (Vector3Int coordinate in occupied)
            {
                sum += (Vector3)coordinate;
            }

            return sum / occupied.Count;
        }

        public List<Vector3Int> GetBoundaryCoordinates()
        {
            List<Vector3Int> boundary = new List<Vector3Int>();
            foreach (Vector3Int coordinate in occupied)
            {
                if (GetExposure(coordinate) > 0)
                {
                    boundary.Add(coordinate);
                }
            }

            return boundary;
        }

        public static IEnumerable<Vector3Int> GetNeighborOffsets()
        {
            return NeighborOffsets;
        }
    }

    /// <summary>
    /// Samples only the mesh surface and thickens it inward to avoid creating solid blocks.
    /// </summary>
    public sealed class SurfaceSampleMeshVoxelizer : IMeshVoxelizer
    {
        public SparseVoxelShape Voxelize(ArrowOutGenerationContext context)
        {
            Mesh mesh = context.Request.Mesh;
            if (mesh == null)
            {
                throw new InvalidOperationException($"Request '{context.Request.LevelName}' does not reference a mesh.");
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (vertices == null || vertices.Length == 0 || triangles == null || triangles.Length < 3)
            {
                throw new InvalidOperationException($"Mesh '{mesh.name}' does not contain triangle data.");
            }

            Bounds bounds = mesh.bounds;
            float longestSide = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float cellSize = Mathf.Max(0.05f, longestSide / context.Tuning.TargetLongestSide);
            Vector3 min = bounds.min - Vector3.one * context.Profile.GridPadding * cellSize;
            SparseVoxelShape shape = new SparseVoxelShape();

            for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
            {
                Vector3 a = vertices[triangles[triangleIndex]];
                Vector3 b = vertices[triangles[triangleIndex + 1]];
                Vector3 c = vertices[triangles[triangleIndex + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                int samplesPerEdge = ResolveSamplesPerEdge(a, b, c, cellSize, context.Profile.SurfaceSamplesPerTriangleEdge);
                Vector3Int inwardOffset = GetDominantAxisOffset(-normal);

                for (int u = 0; u <= samplesPerEdge; u++)
                {
                    for (int v = 0; v <= samplesPerEdge - u; v++)
                    {
                        float fu = u / (float)samplesPerEdge;
                        float fv = v / (float)samplesPerEdge;
                        float fw = 1f - fu - fv;
                        Vector3 sample = a * fw + b * fu + c * fv;
                        Vector3Int coordinate = ToGrid(sample, min, cellSize);
                        shape.Add(coordinate);

                        for (int layer = 1; layer < context.ShellThickness; layer++)
                        {
                            shape.Add(coordinate + inwardOffset * layer);
                        }
                    }
                }
            }

            return ThickenSparseShell(shape, context.ShellThickness - 1);
        }

        private static int ResolveSamplesPerEdge(Vector3 a, Vector3 b, Vector3 c, float cellSize, int minimumSamples)
        {
            float maxEdge = Mathf.Max((a - b).magnitude, Mathf.Max((b - c).magnitude, (c - a).magnitude));
            int samples = Mathf.CeilToInt(maxEdge / Mathf.Max(0.01f, cellSize));
            return Mathf.Max(minimumSamples, samples);
        }

        private static Vector3Int ToGrid(Vector3 point, Vector3 min, float cellSize)
        {
            Vector3 shifted = (point - min) / Mathf.Max(0.01f, cellSize);
            return new Vector3Int(
                Mathf.RoundToInt(shifted.x),
                Mathf.RoundToInt(shifted.y),
                Mathf.RoundToInt(shifted.z));
        }

        private static Vector3Int GetDominantAxisOffset(Vector3 direction)
        {
            Vector3 absolute = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            {
                return new Vector3Int(direction.x >= 0f ? 1 : -1, 0, 0);
            }

            if (absolute.y >= absolute.x && absolute.y >= absolute.z)
            {
                return new Vector3Int(0, direction.y >= 0f ? 1 : -1, 0);
            }

            return new Vector3Int(0, 0, direction.z >= 0f ? 1 : -1);
        }

        private static SparseVoxelShape ThickenSparseShell(SparseVoxelShape source, int iterations)
        {
            SparseVoxelShape result = source.Clone();
            if (iterations <= 0)
            {
                return result;
            }

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                List<Vector3Int> additions = new List<Vector3Int>();
                foreach (Vector3Int coordinate in result.Coordinates)
                {
                    foreach (Vector3Int neighborOffset in SparseVoxelShape.GetNeighborOffsets())
                    {
                        Vector3Int neighbor = coordinate + neighborOffset;
                        if (!result.Contains(neighbor))
                        {
                            additions.Add(neighbor);
                        }
                    }
                }

                for (int index = 0; index < additions.Count; index++)
                {
                    result.Add(additions[index]);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Carves windows, pockets and tunnels so the level reads as a sparse peelable object.
    /// </summary>
    public sealed class PocketTunnelShapeCarver : ISparseShapeCarver
    {
        public SparseVoxelShape Carve(SparseVoxelShape shellShape, ArrowOutGenerationContext context)
        {
            SparseVoxelShape result = shellShape.Clone();
            CreateWindows(result, context);
            CreatePockets(result, context);
            CreateTunnels(result, context);
            TrimTinyIslands(result, context.Tuning.MinimumClusterSize / 2);
            EnsureEvenTileCount(result);
            return result;
        }

        private static void CreateWindows(SparseVoxelShape shape, ArrowOutGenerationContext context)
        {
            BoundsInt bounds = shape.GetBounds();
            int axis = 0;
            for (int iteration = 0; iteration < context.PocketCount; iteration++)
            {
                axis = (axis + 1) % 3;
                Vector3Int center = new Vector3Int(
                    context.Random.Next(bounds.xMin, bounds.xMax),
                    context.Random.Next(bounds.yMin, bounds.yMax),
                    context.Random.Next(bounds.zMin, bounds.zMax));

                int radius = context.Random.Next(1, Math.Max(2, context.ShellThickness + 1));
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            Vector3Int offset = new Vector3Int(dx, dy, dz);
                            if (Mathf.Abs(GetAxisValue(offset, axis)) > 0)
                            {
                                continue;
                            }

                            Vector3Int coordinate = center + offset;
                            if (shape.Contains(coordinate) && shape.GetNeighborCount(coordinate) >= 3)
                            {
                                shape.Remove(coordinate);
                            }
                        }
                    }
                }
            }
        }

        private static void CreatePockets(SparseVoxelShape shape, ArrowOutGenerationContext context)
        {
            List<Vector3Int> boundary = shape.GetBoundaryCoordinates();
            for (int pocketIndex = 0; pocketIndex < context.PocketCount && boundary.Count > 0; pocketIndex++)
            {
                Vector3Int start = boundary[context.Random.Next(boundary.Count)];
                Vector3Int direction = SparseVoxelShape.GetNeighborOffsets().ElementAt(context.Random.Next(6));
                int depth = context.Random.Next(1, context.ShellThickness + 2);

                for (int step = 0; step < depth; step++)
                {
                    Vector3Int coordinate = start + direction * step;
                    if (!shape.Contains(coordinate) || shape.GetNeighborCount(coordinate) < 2)
                    {
                        continue;
                    }

                    shape.Remove(coordinate);
                }
            }
        }

        private static void CreateTunnels(SparseVoxelShape shape, ArrowOutGenerationContext context)
        {
            List<Vector3Int> boundary = shape.GetBoundaryCoordinates();
            if (boundary.Count < 2)
            {
                return;
            }

            for (int tunnelIndex = 0; tunnelIndex < context.TunnelCount; tunnelIndex++)
            {
                Vector3Int start = boundary[context.Random.Next(boundary.Count)];
                Vector3Int end = boundary[context.Random.Next(boundary.Count)];
                foreach (Vector3Int coordinate in BuildManhattanPath(start, end))
                {
                    if (shape.Contains(coordinate) && shape.GetNeighborCount(coordinate) >= 2)
                    {
                        shape.Remove(coordinate);
                    }
                }
            }
        }

        private static void TrimTinyIslands(SparseVoxelShape shape, int minimumSize)
        {
            List<List<Vector3Int>> components = shape.GetConnectedComponents();
            for (int index = 0; index < components.Count; index++)
            {
                List<Vector3Int> component = components[index];
                if (component.Count >= minimumSize)
                {
                    continue;
                }

                for (int coordinateIndex = 0; coordinateIndex < component.Count; coordinateIndex++)
                {
                    shape.Remove(component[coordinateIndex]);
                }
            }
        }

        private static void EnsureEvenTileCount(SparseVoxelShape shape)
        {
            if (shape.Count % 2 == 0)
            {
                return;
            }

            Vector3Int removable = shape.GetBoundaryCoordinates().OrderByDescending(shape.GetExposure).FirstOrDefault();
            shape.Remove(removable);
        }

        private static int GetAxisValue(Vector3Int value, int axis)
        {
            switch (axis)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        internal static IEnumerable<Vector3Int> BuildManhattanPath(Vector3Int from, Vector3Int to)
        {
            Vector3Int current = from;
            yield return current;

            while (current.x != to.x)
            {
                current.x += Math.Sign(to.x - current.x);
                yield return current;
            }

            while (current.y != to.y)
            {
                current.y += Math.Sign(to.y - current.y);
                yield return current;
            }

            while (current.z != to.z)
            {
                current.z += Math.Sign(to.z - current.z);
                yield return current;
            }
        }
    }

    /// <summary>
    /// Splits the shell into separated clusters, then reconnects them with sparse bridges.
    /// </summary>
    public sealed class ClusteredShellLayoutPlanner : IClusterLayoutPlanner
    {
        public ClusterLayoutResult Plan(SparseVoxelShape sparseShape, ArrowOutGenerationContext context)
        {
            List<Vector3Int> coordinates = sparseShape.Coordinates.ToList();
            if (coordinates.Count == 0)
            {
                throw new InvalidOperationException($"Sparse shell became empty for '{context.Request.LevelName}'.");
            }

            int clusterCount = Mathf.Clamp(context.ClusterCount, 2, Mathf.Max(2, coordinates.Count / context.Tuning.MinimumClusterSize));
            List<Vector3Int> seeds = ChooseSeeds(coordinates, clusterCount, context.Random);
            Dictionary<int, List<Vector3Int>> assignments = AssignToSeeds(coordinates, seeds);
            List<PlacedCluster> placedClusters = OffsetClusters(assignments, context);
            ResolveOverlaps(placedClusters, context.Tuning.ClusterGap);

            ClusterLayoutResult result = new ClusterLayoutResult();
            for (int index = 0; index < placedClusters.Count; index++)
            {
                PlaceCluster(result, placedClusters[index], isBridge: false);
            }

            List<PlacedCluster> bridgeClusters = CreateBridgeClusters(placedClusters, context);
            for (int index = 0; index < bridgeClusters.Count; index++)
            {
                PlaceCluster(result, bridgeClusters[index], isBridge: true);
            }

            return result;
        }

        private static List<Vector3Int> ChooseSeeds(List<Vector3Int> coordinates, int clusterCount, System.Random random)
        {
            List<Vector3Int> seeds = new List<Vector3Int>();
            seeds.Add(coordinates[random.Next(coordinates.Count)]);

            while (seeds.Count < clusterCount)
            {
                Vector3Int bestCoordinate = coordinates[0];
                int bestDistance = int.MinValue;
                for (int index = 0; index < coordinates.Count; index++)
                {
                    Vector3Int candidate = coordinates[index];
                    int nearestDistance = int.MaxValue;
                    for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
                    {
                        nearestDistance = Mathf.Min(nearestDistance, ManhattanDistance(candidate, seeds[seedIndex]));
                    }

                    if (nearestDistance > bestDistance)
                    {
                        bestDistance = nearestDistance;
                        bestCoordinate = candidate;
                    }
                }

                seeds.Add(bestCoordinate);
            }

            return seeds;
        }

        private static Dictionary<int, List<Vector3Int>> AssignToSeeds(List<Vector3Int> coordinates, List<Vector3Int> seeds)
        {
            Dictionary<int, List<Vector3Int>> assignments = new Dictionary<int, List<Vector3Int>>();
            for (int index = 0; index < seeds.Count; index++)
            {
                assignments[index] = new List<Vector3Int>();
            }

            for (int coordinateIndex = 0; coordinateIndex < coordinates.Count; coordinateIndex++)
            {
                Vector3Int coordinate = coordinates[coordinateIndex];
                int bestSeedIndex = 0;
                int bestDistance = int.MaxValue;

                for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
                {
                    int distance = ManhattanDistance(coordinate, seeds[seedIndex]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestSeedIndex = seedIndex;
                    }
                }

                assignments[bestSeedIndex].Add(coordinate);
            }

            return assignments;
        }

        private static List<PlacedCluster> OffsetClusters(Dictionary<int, List<Vector3Int>> assignments, ArrowOutGenerationContext context)
        {
            Vector3 globalCenter = Vector3.zero;
            int totalCount = 0;
            foreach (KeyValuePair<int, List<Vector3Int>> pair in assignments)
            {
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    globalCenter += (Vector3)pair.Value[index];
                    totalCount++;
                }
            }

            globalCenter /= Mathf.Max(1, totalCount);
            List<PlacedCluster> placedClusters = new List<PlacedCluster>();
            foreach (KeyValuePair<int, List<Vector3Int>> pair in assignments)
            {
                if (pair.Value.Count == 0)
                {
                    continue;
                }

                Vector3 centroid = GetCentroid(pair.Value);
                Vector3 direction = centroid - globalCenter;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = new Vector3(
                        (float)(context.Random.NextDouble() * 2d - 1d),
                        (float)(context.Random.NextDouble() * 2d - 1d),
                        (float)(context.Random.NextDouble() * 2d - 1d));
                }

                direction.Normalize();
                Vector3 scaledOffset = direction * context.Tuning.ClusterGap * 2f;
                Vector3Int offset = new Vector3Int(
                    Mathf.RoundToInt(scaledOffset.x),
                    Mathf.RoundToInt(scaledOffset.y),
                    Mathf.RoundToInt(scaledOffset.z));

                if (offset == Vector3Int.zero)
                {
                    offset = new Vector3Int(context.Tuning.ClusterGap, 0, 0);
                }

                placedClusters.Add(new PlacedCluster(pair.Key, pair.Value, offset));
            }

            return placedClusters;
        }

        private static void ResolveOverlaps(List<PlacedCluster> clusters, int clearance)
        {
            for (int iteration = 0; iteration < 48; iteration++)
            {
                bool moved = false;
                for (int firstIndex = 0; firstIndex < clusters.Count; firstIndex++)
                {
                    for (int secondIndex = firstIndex + 1; secondIndex < clusters.Count; secondIndex++)
                    {
                        PlacedCluster first = clusters[firstIndex];
                        PlacedCluster second = clusters[secondIndex];
                        BoundsInt firstBounds = first.GetPlacedBounds(clearance);
                        BoundsInt secondBounds = second.GetPlacedBounds(clearance);
                        if (!BoundsOverlap(firstBounds, secondBounds))
                        {
                            continue;
                        }

                        Vector3 delta = first.GetPlacedCentroid() - second.GetPlacedCentroid();
                        Vector3Int push = new Vector3Int(
                            delta.x >= 0f ? 1 : -1,
                            delta.y >= 0f ? 1 : -1,
                            delta.z >= 0f ? 1 : -1);

                        if (push == Vector3Int.zero)
                        {
                            push = Vector3Int.right;
                        }

                        first.Offset += push;
                        second.Offset -= push;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }
        }

        private static List<PlacedCluster> CreateBridgeClusters(List<PlacedCluster> clusters, ArrowOutGenerationContext context)
        {
            List<PlacedCluster> bridges = new List<PlacedCluster>();
            if (clusters.Count <= 1)
            {
                return bridges;
            }

            List<(int From, int To)> edges = BuildMinimumSpanningTree(clusters);
            int bridgeBudget = Mathf.Max(1, context.BridgeCount);
            for (int edgeIndex = 0; edgeIndex < edges.Count && bridgeBudget > 0; edgeIndex++, bridgeBudget--)
            {
                PlacedCluster from = clusters[edges[edgeIndex].From];
                PlacedCluster to = clusters[edges[edgeIndex].To];
                List<Vector3Int> bridgePath = BuildBridgePath(from, to);
                if (bridgePath.Count == 0)
                {
                    continue;
                }

                bridges.Add(new PlacedCluster(1000 + bridges.Count, bridgePath, Vector3Int.zero));
            }

            return bridges;
        }

        private static List<(int From, int To)> BuildMinimumSpanningTree(List<PlacedCluster> clusters)
        {
            List<(int From, int To)> edges = new List<(int From, int To)>();
            HashSet<int> visited = new HashSet<int> { 0 };

            while (visited.Count < clusters.Count)
            {
                int bestFrom = -1;
                int bestTo = -1;
                float bestDistance = float.MaxValue;
                foreach (int fromIndex in visited)
                {
                    for (int toIndex = 0; toIndex < clusters.Count; toIndex++)
                    {
                        if (visited.Contains(toIndex))
                        {
                            continue;
                        }

                        float distance = Vector3.Distance(clusters[fromIndex].GetPlacedCentroid(), clusters[toIndex].GetPlacedCentroid());
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestFrom = fromIndex;
                            bestTo = toIndex;
                        }
                    }
                }

                if (bestTo < 0)
                {
                    break;
                }

                visited.Add(bestTo);
                edges.Add((bestFrom, bestTo));
            }

            return edges;
        }

        private static List<Vector3Int> BuildBridgePath(PlacedCluster from, PlacedCluster to)
        {
            List<Vector3Int> fromBoundary = from.GetPlacedBoundary();
            List<Vector3Int> toBoundary = to.GetPlacedBoundary();
            if (fromBoundary.Count == 0 || toBoundary.Count == 0)
            {
                return new List<Vector3Int>();
            }

            Vector3Int bestFrom = fromBoundary[0];
            Vector3Int bestTo = toBoundary[0];
            int bestDistance = ManhattanDistance(bestFrom, bestTo);

            for (int fromIndex = 0; fromIndex < fromBoundary.Count; fromIndex++)
            {
                for (int toIndex = 0; toIndex < toBoundary.Count; toIndex++)
                {
                    int distance = ManhattanDistance(fromBoundary[fromIndex], toBoundary[toIndex]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestFrom = fromBoundary[fromIndex];
                        bestTo = toBoundary[toIndex];
                    }
                }
            }

            List<Vector3Int> result = new List<Vector3Int>();
            foreach (Vector3Int coordinate in PocketTunnelShapeCarver.BuildManhattanPath(bestFrom, bestTo))
            {
                result.Add(coordinate);
            }

            return result;
        }

        private static void PlaceCluster(ClusterLayoutResult result, PlacedCluster cluster, bool isBridge)
        {
            List<Vector3Int> placedVoxels = cluster.GetPlacedVoxels();
            ArrowOutClusterData clusterData = new ArrowOutClusterData(cluster.Id, placedVoxels, isBridge);
            result.Clusters.Add(clusterData);

            for (int index = 0; index < placedVoxels.Count; index++)
            {
                Vector3Int coordinate = placedVoxels[index];
                result.Occupied.Add(coordinate);
                result.ClusterByCoordinate[coordinate] = cluster.Id;
            }
        }

        private static Vector3 GetCentroid(List<Vector3Int> coordinates)
        {
            Vector3 sum = Vector3.zero;
            for (int index = 0; index < coordinates.Count; index++)
            {
                sum += (Vector3)coordinates[index];
            }

            return sum / Mathf.Max(1, coordinates.Count);
        }

        private static int ManhattanDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }

        private static bool BoundsOverlap(BoundsInt first, BoundsInt second)
        {
            return first.xMin < second.xMax
                && first.xMax > second.xMin
                && first.yMin < second.yMax
                && first.yMax > second.yMin
                && first.zMin < second.zMax
                && first.zMax > second.zMin;
        }

        private sealed class PlacedCluster
        {
            public PlacedCluster(int id, List<Vector3Int> voxels, Vector3Int offset)
            {
                Id = id;
                Voxels = voxels;
                Offset = offset;
            }

            public int Id { get; }
            public List<Vector3Int> Voxels { get; }
            public Vector3Int Offset { get; set; }

            public List<Vector3Int> GetPlacedVoxels()
            {
                List<Vector3Int> result = new List<Vector3Int>(Voxels.Count);
                for (int index = 0; index < Voxels.Count; index++)
                {
                    result.Add(Voxels[index] + Offset);
                }

                return result;
            }

            public BoundsInt GetPlacedBounds(int padding)
            {
                List<Vector3Int> placed = GetPlacedVoxels();
                Vector3Int min = placed[0];
                Vector3Int max = placed[0];
                for (int index = 1; index < placed.Count; index++)
                {
                    min = Vector3Int.Min(min, placed[index]);
                    max = Vector3Int.Max(max, placed[index]);
                }

                min -= Vector3Int.one * padding;
                max += Vector3Int.one * padding;
                return new BoundsInt(min, max - min + Vector3Int.one);
            }

            public Vector3 GetPlacedCentroid()
            {
                return GetCentroid(GetPlacedVoxels());
            }

            public List<Vector3Int> GetPlacedBoundary()
            {
                SparseVoxelShape shape = new SparseVoxelShape(GetPlacedVoxels());
                return shape.GetBoundaryCoordinates();
            }
        }
    }

    /// <summary>
    /// Pairs nearby exposed tiles first so each match tends to open the next local pocket.
    /// </summary>
    public sealed class PeelWaveMatchPlanner : IMatchPlanner
    {
        public List<GeneratedTileData> PlanMatches(ClusterLayoutResult layout, ArrowOutGenerationContext context)
        {
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(layout.Occupied);
            List<GeneratedTileData> result = new List<GeneratedTileData>(remaining.Count);
            int nextMatchId = 0;

            while (remaining.Count >= 2)
            {
                List<Vector3Int> frontier = GetFrontier(remaining);
                if (frontier.Count < 2)
                {
                    frontier = remaining.ToList();
                }

                frontier.Sort((left, right) =>
                {
                    int exposureDelta = GetExposure(remaining, right) - GetExposure(remaining, left);
                    if (exposureDelta != 0)
                    {
                        return exposureDelta;
                    }

                    return layout.ClusterByCoordinate[left].CompareTo(layout.ClusterByCoordinate[right]);
                });

                Vector3Int first = frontier[0];
                Vector3Int second = FindBestPairCandidate(first, frontier, layout, context.Tuning.MaxLocalPairDistance);
                int clusterId = layout.ClusterByCoordinate[first];
                Vector3 rotation = context.Random.NextDouble() < context.Tuning.FlipRotationChance ? new Vector3(0f, 180f, 0f) : Vector3.zero;

                result.Add(new GeneratedTileData
                {
                    MatchId = nextMatchId,
                    ClusterId = clusterId,
                    Coordinate = first,
                    LocalEulerAngles = rotation,
                });

                result.Add(new GeneratedTileData
                {
                    MatchId = nextMatchId,
                    ClusterId = layout.ClusterByCoordinate[second],
                    Coordinate = second,
                    LocalEulerAngles = rotation,
                });

                remaining.Remove(first);
                remaining.Remove(second);
                nextMatchId++;
            }

            if (remaining.Count == 1)
            {
                throw new InvalidOperationException($"Generator left an unpaired tile in '{context.Request.LevelName}'.");
            }

            return result;
        }

        private static List<Vector3Int> GetFrontier(HashSet<Vector3Int> remaining)
        {
            List<Vector3Int> frontier = new List<Vector3Int>();
            foreach (Vector3Int coordinate in remaining)
            {
                if (GetExposure(remaining, coordinate) > 0)
                {
                    frontier.Add(coordinate);
                }
            }

            return frontier;
        }

        private static int GetExposure(HashSet<Vector3Int> remaining, Vector3Int coordinate)
        {
            int neighbors = 0;
            foreach (Vector3Int neighborOffset in SparseVoxelShape.GetNeighborOffsets())
            {
                if (remaining.Contains(coordinate + neighborOffset))
                {
                    neighbors++;
                }
            }

            return 6 - neighbors;
        }

        private static Vector3Int FindBestPairCandidate(Vector3Int first, List<Vector3Int> frontier, ClusterLayoutResult layout, int maxLocalPairDistance)
        {
            int sourceCluster = layout.ClusterByCoordinate[first];
            Vector3Int fallback = frontier[1];
            int bestScore = int.MinValue;
            Vector3Int best = fallback;

            for (int index = 1; index < frontier.Count; index++)
            {
                Vector3Int candidate = frontier[index];
                int distance = Mathf.Abs(candidate.x - first.x) + Mathf.Abs(candidate.y - first.y) + Mathf.Abs(candidate.z - first.z);
                int sameClusterBonus = layout.ClusterByCoordinate[candidate] == sourceCluster ? 100 : 0;
                int distanceBonus = distance <= maxLocalPairDistance ? 30 - distance : -distance;
                int exposureBonus = GetExposure(new HashSet<Vector3Int>(frontier), candidate);
                int score = sameClusterBonus + distanceBonus + exposureBonus;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
