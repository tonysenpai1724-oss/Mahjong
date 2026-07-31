using MahjongOut3D.LevelSystem.ArrowOutGeneration;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Custom inspector for the new gameplay-first Arrow Out style generator.
    /// </summary>
    [CustomEditor(typeof(ArrowOutMeshLevelGenerator))]
    public sealed class ArrowOutMeshLevelGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Each request should point to a MahjongTile prefab. The generator reads the prefab's mesh, converts it to a sparse shell, then carves holes and splits it into clusters.", MessageType.Info);
            EditorGUILayout.HelpBox("This generator keeps only sparse shell voxels, carves pockets and tunnels, then separates the result into gameplay clusters connected by thin bridges.", MessageType.Info);
            EditorGUILayout.HelpBox("It is intentionally not mesh-faithful when that would create a boring solid block. Gameplay peel flow wins over silhouette accuracy.", MessageType.Warning);
            EditorGUILayout.HelpBox("The generator now shows a progress bar and skips a full AssetDatabase refresh, so a normal run should finish quickly instead of looking frozen.", MessageType.None);

            if (GUILayout.Button("Generate Arrow Out Levels"))
            {
                Generate((ArrowOutMeshLevelGenerator)target);
            }
        }

        private static void Generate(ArrowOutMeshLevelGenerator generator)
        {
            try
            {
                var results = generator.GenerateAssets();
                Debug.Log($"Generated {results.Count} Arrow Out level assets from {generator.name}.", generator);
                EditorUtility.SetDirty(generator);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, generator);
                EditorUtility.DisplayDialog("Arrow Out Generation Failed", exception.Message, "OK");
            }
        }
    }
}
