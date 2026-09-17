using System;
using System.Collections.Generic;
using MahjongOut3D.TileSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongOut3D.LevelSystem.ArrowOutGeneration
{
    /// <summary>
    /// Entry point used to generate gameplay-first Mahjong levels from imported meshes.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Arrow Out Mesh Generator", fileName = "ArrowOutMeshLevelGenerator")]
    public sealed class ArrowOutMeshLevelGenerator : ScriptableObject
    {
        [SerializeField] private LevelCatalog targetCatalog;
        [SerializeField] private VoxelGridLayoutSettings layoutOverride;
        [SerializeField] private ArrowOutLevelGeneratorProfile profile;
        [SerializeField] private string outputFolder = "Assets/00 Scripts/Mahjong/Generated ArrowOut Levels";
        [SerializeField] private bool overwriteExistingAssets = true;
        [SerializeField] private bool replaceCatalogEntries;
        [SerializeField] private List<Request> requests = new List<Request>();

        /// <summary>
        /// Gets the requests configured on this asset.
        /// </summary>
        public IReadOnlyList<Request> Requests => requests;

        /// <summary>
        /// Generates all configured level assets.
        /// </summary>
        public List<LevelDefinition> GenerateAssets()
        {
#if UNITY_EDITOR
            if (profile == null)
            {
                throw new InvalidOperationException("ArrowOutMeshLevelGenerator requires a generator profile.");
            }

            if (requests == null || requests.Count == 0)
            {
                return new List<LevelDefinition>();
            }

            ArrowOutLevelGeneratorPipeline pipeline = ArrowOutLevelGeneratorPipeline.CreateDefault();
            List<LevelDefinition> generatedAssets = new List<LevelDefinition>();
            List<string> validationNotes = new List<string>();

            if (replaceCatalogEntries && targetCatalog != null)
            {
                targetCatalog.Levels.Clear();
            }

            int totalRequestCount = requests.Count;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int index = 0; index < totalRequestCount; index++)
                {
                    Request request = requests[index];
                    float progress = totalRequestCount <= 0 ? 1f : (index + 1f) / totalRequestCount;
                    string requestName = request != null ? request.LevelName : $"Request {index + 1}";
                    EditorUtility.DisplayProgressBar("Arrow Out Generation", $"Generating {requestName}", progress);

                    if (request == null || !request.HasSource)
                    {
                        continue;
                    }

                    ArrowOutGeneratedLevel generatedLevel = pipeline.Generate(request, profile, layoutOverride, index);
                    ArrowOutGeneratedLevelValidationReport report = ArrowOutGeneratedLevelValidator.Validate(generatedLevel);
                    LevelDefinition levelAsset = ArrowOutLevelDefinitionAssetWriter.WriteAsset(generatedLevel, outputFolder, overwriteExistingAssets);
                    generatedAssets.Add(levelAsset);

                    if (request.AddToCatalog && targetCatalog != null)
                    {
                        targetCatalog.Levels.Add(levelAsset);
                    }

                    if (report.Warnings.Count > 0)
                    {
                        validationNotes.Add(report.ToMultilineString());
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            if (targetCatalog != null)
            {
                EditorUtility.SetDirty(targetCatalog);
            }
            AssetDatabase.SaveAssets();

            if (validationNotes.Count > 0)
            {
                Debug.Log($"Arrow Out generation finished with {validationNotes.Count} validation note(s):\n- {string.Join("\n- ", validationNotes)}");
            }

            return generatedAssets;
#else
            throw new InvalidOperationException("Mesh-driven asset generation is editor-only.");
#endif
        }

        /// <summary>
        /// Stores one mesh generation job.
        /// </summary>
        [Serializable]
        public sealed class Request
        {
            [SerializeField] private string levelName = "ArrowOut Level";
            [SerializeField] private MahjongTile tilePrefab;
            [SerializeField] private Mesh mesh;
            [SerializeField] private LevelShapeType shape = LevelShapeType.Custom;
            [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
            [SerializeField] private int seedOffset;
            [SerializeField] private int targetPairCountOverride;
            [SerializeField] private bool addToCatalog = true;

            public string LevelName => string.IsNullOrWhiteSpace(levelName) ? "ArrowOut Level" : levelName.Trim();
            public MahjongTile TilePrefab => tilePrefab;
            public Mesh Mesh => ResolveSourceMesh();
            public LevelShapeType Shape => shape;
            public LevelDifficulty Difficulty => difficulty;
            public int SeedOffset => seedOffset;
            public int TargetPairCountOverride => Mathf.Max(0, targetPairCountOverride);
            public bool AddToCatalog => addToCatalog;
            public bool HasSource => tilePrefab != null || mesh != null;

            private Mesh ResolveSourceMesh()
            {
                if (tilePrefab != null)
                {
                    MeshFilter meshFilter = tilePrefab.GetComponentInChildren<MeshFilter>(true);
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        return meshFilter.sharedMesh;
                    }

                    SkinnedMeshRenderer skinnedMeshRenderer = tilePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
                    {
                        return skinnedMeshRenderer.sharedMesh;
                    }
                }

                return mesh;
            }
        }
    }
}
