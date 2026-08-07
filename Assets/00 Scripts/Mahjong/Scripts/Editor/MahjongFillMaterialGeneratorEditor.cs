using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Adds a one-click button for syncing categorized fill textures.
    /// </summary>
    [CustomEditor(typeof(MahjongFillMaterialGenerator))]
    public sealed class MahjongFillMaterialGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Generator này gom texture từ từng folder category vào MahjongMaterialSO và gán chung một fill base material để runtime dùng MaterialPropertyBlock.", MessageType.Info);

            if (GUILayout.Button("Generate Fill Materials"))
            {
                Generate((MahjongFillMaterialGenerator)target);
            }
        }

        private static void Generate(MahjongFillMaterialGenerator generator)
        {
            try
            {
                int generatedCount = generator.Generate();
                Debug.Log($"Synced {generatedCount} fill textures from {generator.name}.", generator);
                EditorUtility.SetDirty(generator);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, generator);
                EditorUtility.DisplayDialog("Fill Material Generation Failed", exception.Message, "OK");
            }
        }
    }
}
