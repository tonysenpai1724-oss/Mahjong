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
            EditorGUILayout.HelpBox("Generator now writes surface-wrapped levels. Each exposed face is split into tile slots, and outer shells are generated before inner shells so the block peels from outside to inside.", MessageType.Info);
            EditorGUILayout.HelpBox("Easy keeps matched pairs close together on the same face when possible. Normal, Hard, and SuperHard progressively spread pair members across different faces and shell layers.", MessageType.Info);
            EditorGUILayout.HelpBox("SuperHard levels are written with runtime difficulty Expert to stay compatible with the existing enum.", MessageType.Info);
            EditorGUILayout.HelpBox("Write Mode: New Gen sẽ giữ level cũ và tạo level mới với số tiếp theo. Overwrite Matching sẽ chỉ cập nhật level trùng tên generate, không xoá sạch catalog cũ.", MessageType.Warning);

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
                var generatedAssets = generator.GenerateAssets(generator.WriteMode);
                Debug.Log($"Generated {generatedAssets.Count} procedural levels from {generator.name} using {generator.WriteMode}.", generator);
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
