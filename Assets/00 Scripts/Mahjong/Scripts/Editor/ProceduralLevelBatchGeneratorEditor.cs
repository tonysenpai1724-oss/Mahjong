using MahjongOut3D.LevelSystem;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Draws the inspector controls used to generate many procedural level assets with one click.
    /// </summary>
    [CustomEditor(typeof(ProceduralLevelBatchGenerator))]
    public sealed class ProceduralLevelBatchGeneratorEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the generator inspector and its action buttons.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Generator now supports multiple layered silhouettes per difficulty tier. Outer shells are generated before inner shells so the block peels from outside to inside.", MessageType.Info);
            EditorGUILayout.HelpBox("SuperHard levels are written with runtime difficulty Expert to stay compatible with the existing enum.", MessageType.Info);

            if (GUILayout.Button("Generate Levels"))
            {
                GenerateLevels((ProceduralLevelBatchGenerator)target);
            }
        }

        /// <summary>
        /// Runs the batch generator and pings the configured level catalog afterwards.
        /// </summary>
        private static void GenerateLevels(ProceduralLevelBatchGenerator generator)
        {
            try
            {
                var generatedAssets = generator.GenerateAssets();
                Debug.Log($"Generated {generatedAssets.Count} procedural levels from {generator.name}.", generator);
                EditorUtility.SetDirty(generator);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, generator);
                EditorUtility.DisplayDialog("Level Generation Failed", exception.Message, "OK");
            }
        }
    }
}
