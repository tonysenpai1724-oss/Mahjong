using MahjongOut3D.Managers;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Adds quick debug controls for loading specific generated levels from the LevelManager inspector.
    /// </summary>
    [CustomEditor(typeof(LevelManager))]
    public sealed class LevelManagerEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the default inspector plus debug level-loading controls.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Level Loader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Nhập level index rồi bấm Load Level để test nhanh khi đang Play.", MessageType.Info);

            SerializedProperty inspectorLevelIndexProperty = serializedObject.FindProperty("inspectorLevelIndex");
            if (inspectorLevelIndexProperty != null)
            {
                EditorGUILayout.PropertyField(inspectorLevelIndexProperty, new GUIContent("Level Index"));
            }

            serializedObject.ApplyModifiedProperties();

            LevelManager levelManager = (LevelManager)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Load Level"))
                    {
                        bool loaded = levelManager.LoadInspectorLevel();
                        if (!loaded)
                        {
                            Debug.LogWarning($"Could not load level index {levelManager.InspectorLevelIndex}. Check the LevelCatalog entries.", levelManager);
                        }
                    }

                    if (GUILayout.Button("Hint"))
                    {
                        MatchManager matchManager = Object.FindFirstObjectByType<MatchManager>(FindObjectsInactive.Exclude);
                        if (matchManager == null)
                        {
                            Debug.LogWarning("Could not find an active MatchManager in the scene.", levelManager);
                        }
                        else if (!matchManager.UseHint())
                        {
                            Debug.LogWarning("Hint failed. There may be no valid exposed pair right now.", levelManager);
                        }
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Nút Load Level và Hint chỉ hoạt động trong Play Mode.", MessageType.None);
            }
        }
    }
}
