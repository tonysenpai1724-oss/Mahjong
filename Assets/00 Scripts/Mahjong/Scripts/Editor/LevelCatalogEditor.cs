using System;
using System.Collections.Generic;
using MahjongOut3D.Data;
using MahjongOut3D.LevelSystem;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Draws a catalog inspector that lets designers tweak fill categories per level directly from the catalog.
    /// </summary>
    [CustomEditor(typeof(LevelCatalog))]
    public sealed class LevelCatalogEditor : UnityEditor.Editor
    {
        private const string LevelsPropertyName = "<Levels>k__BackingField";
        private const string FillCategoryPropertyName = "<FillCategoryNames>k__BackingField";

        private readonly Dictionary<string, bool> levelFoldouts = new Dictionary<string, bool>();

        /// <summary>
        /// Draws the default catalog list and an additional per-level fill category editor.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            SerializedProperty levelsProperty = serializedObject.FindProperty(LevelsPropertyName);
            if (levelsProperty == null || !levelsProperty.isArray)
            {
                EditorGUILayout.HelpBox("Không tìm thấy danh sách Levels trong LevelCatalog.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Per-Level Fill Categories", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Chỉnh category cho từng level ngay trong catalog. Thay đổi sẽ ghi trực tiếp vào asset LevelDefinition tương ứng.", MessageType.Info);

            List<string> availableCategoryNames = GetAvailableFillCategoryNames();
            if (availableCategoryNames.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có category nào trong MahjongMaterialSO. Sync/tạo category trước rồi quay lại đây.", MessageType.Info);
                return;
            }

            for (int index = 0; index < levelsProperty.arraySize; index++)
            {
                SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(index);
                if (levelProperty == null)
                {
                    continue;
                }

                LevelDefinition levelDefinition = levelProperty.objectReferenceValue as LevelDefinition;
                DrawLevelEntry(index, levelDefinition, availableCategoryNames);
            }
        }

        private void DrawLevelEntry(int index, LevelDefinition levelDefinition, List<string> availableCategoryNames)
        {
            int layerCount = levelDefinition != null ? levelDefinition.GetResolvedLayerCount() : 0;
            string levelLabel = levelDefinition != null
                ? $"{index + 1}. {levelDefinition.name}   |   Layer {layerCount}   |   {levelDefinition.Shape}"
                : $"{index + 1}. Missing Level";
            string foldoutKey = levelDefinition != null ? AssetDatabase.GetAssetPath(levelDefinition) : $"missing_{index}";
            bool isExpanded = levelFoldouts.TryGetValue(foldoutKey, out bool storedState) && storedState;
            isExpanded = EditorGUILayout.Foldout(isExpanded, levelLabel, true);
            levelFoldouts[foldoutKey] = isExpanded;

            if (!isExpanded)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (levelDefinition == null)
                {
                    EditorGUILayout.HelpBox("Entry này đang null. Gán lại LevelDefinition trong catalog trước đã.", MessageType.Warning);
                    return;
                }

                string levelPath = AssetDatabase.GetAssetPath(levelDefinition);
                EditorGUILayout.ObjectField("Level", levelDefinition, typeof(LevelDefinition), false);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Layer Count", layerCount);
                    EditorGUILayout.EnumPopup("Shape", levelDefinition.Shape);
                    EditorGUILayout.EnumPopup("Difficulty", levelDefinition.Difficulty);
                }
                if (!string.IsNullOrEmpty(levelPath))
                {
                    EditorGUILayout.LabelField("Asset", levelPath);
                }

                SerializedObject levelObject = new SerializedObject(levelDefinition);
                levelObject.Update();
                SerializedProperty fillCategoryProperty = levelObject.FindProperty(FillCategoryPropertyName);
                DrawFillCategoryChecklist(fillCategoryProperty, availableCategoryNames);
                if (levelObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(levelDefinition);
                }
            }

            EditorGUILayout.Space(4f);
        }

        private static void DrawFillCategoryChecklist(SerializedProperty fillCategoryProperty, List<string> availableCategoryNames)
        {
            if (fillCategoryProperty == null || !fillCategoryProperty.isArray)
            {
                EditorGUILayout.HelpBox("Level này không có property FillCategoryNames hợp lệ.", MessageType.Warning);
                return;
            }

            List<string> selectedCategoryNames = GetPropertyValues(fillCategoryProperty);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All"))
                {
                    SetPropertyValues(fillCategoryProperty, availableCategoryNames);
                    selectedCategoryNames = new List<string>(availableCategoryNames);
                }

                if (GUILayout.Button("Clear"))
                {
                    fillCategoryProperty.ClearArray();
                    selectedCategoryNames.Clear();
                }
            }

            for (int categoryIndex = 0; categoryIndex < availableCategoryNames.Count; categoryIndex++)
            {
                string categoryName = availableCategoryNames[categoryIndex];
                bool isSelected = ContainsIgnoreCase(selectedCategoryNames, categoryName);
                bool nextSelected = EditorGUILayout.ToggleLeft(categoryName, isSelected);
                if (nextSelected == isSelected)
                {
                    continue;
                }

                if (nextSelected)
                {
                    selectedCategoryNames.Add(categoryName);
                }
                else
                {
                    RemoveIgnoreCase(selectedCategoryNames, categoryName);
                }

                SetPropertyValues(fillCategoryProperty, selectedCategoryNames);
            }

            List<string> missingSelections = GetMissingSelections(selectedCategoryNames, availableCategoryNames);
            if (missingSelections.Count <= 0)
            {
                return;
            }

            EditorGUILayout.HelpBox("Có category cũ không còn trong MahjongMaterialSO. Mình đang giữ lại để không mất cấu hình level này.", MessageType.Warning);
            for (int index = 0; index < missingSelections.Count; index++)
            {
                string missingSelection = missingSelections[index];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(missingSelection, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Remove", GUILayout.Width(80f)))
                    {
                        RemoveIgnoreCase(selectedCategoryNames, missingSelection);
                        SetPropertyValues(fillCategoryProperty, selectedCategoryNames);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private static List<string> GetAvailableFillCategoryNames()
        {
            HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> resolvedNames = new List<string>();
            string[] materialGuids = AssetDatabase.FindAssets("t:MahjongMaterialSO");

            for (int guidIndex = 0; guidIndex < materialGuids.Length; guidIndex++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(materialGuids[guidIndex]);
                MahjongMaterialSO materialLibrary = AssetDatabase.LoadAssetAtPath<MahjongMaterialSO>(assetPath);
                if (materialLibrary == null || materialLibrary.FillCategories == null)
                {
                    continue;
                }

                for (int categoryIndex = 0; categoryIndex < materialLibrary.FillCategories.Count; categoryIndex++)
                {
                    MahjongMaterialCategory category = materialLibrary.FillCategories[categoryIndex];
                    string categoryName = category?.CategoryName;
                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        continue;
                    }

                    string trimmedName = categoryName.Trim();
                    if (seenNames.Add(trimmedName))
                    {
                        resolvedNames.Add(trimmedName);
                    }
                }
            }

            resolvedNames.Sort(StringComparer.OrdinalIgnoreCase);
            return resolvedNames;
        }

        private static List<string> GetPropertyValues(SerializedProperty property)
        {
            List<string> values = new List<string>();
            if (property == null || !property.isArray)
            {
                return values;
            }

            for (int index = 0; index < property.arraySize; index++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                if (element == null || string.IsNullOrWhiteSpace(element.stringValue))
                {
                    continue;
                }

                string value = element.stringValue.Trim();
                if (!ContainsIgnoreCase(values, value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static void SetPropertyValues(SerializedProperty property, List<string> values)
        {
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values != null ? values.Count : 0;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }
        }

        private static List<string> GetMissingSelections(List<string> selectedNames, List<string> availableNames)
        {
            List<string> missingSelections = new List<string>();
            for (int index = 0; index < selectedNames.Count; index++)
            {
                if (!ContainsIgnoreCase(availableNames, selectedNames[index]))
                {
                    missingSelections.Add(selectedNames[index]);
                }
            }

            return missingSelections;
        }

        private static bool ContainsIgnoreCase(List<string> values, string target)
        {
            if (values == null || string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveIgnoreCase(List<string> values, string target)
        {
            if (values == null || string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            for (int index = values.Count - 1; index >= 0; index--)
            {
                if (string.Equals(values[index], target, StringComparison.OrdinalIgnoreCase))
                {
                    values.RemoveAt(index);
                }
            }
        }
    }
}
