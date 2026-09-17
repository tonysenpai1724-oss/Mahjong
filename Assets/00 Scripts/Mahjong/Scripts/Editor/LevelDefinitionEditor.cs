using System;
using System.Collections.Generic;
using MahjongOut3D.Data;
using MahjongOut3D.LevelSystem;
using UnityEditor;
using UnityEngine;

namespace MahjongOut3D.Editor
{
    /// <summary>
    /// Draws a friendlier inspector for level assets, including a checklist for fill categories.
    /// </summary>
    [CustomEditor(typeof(LevelDefinition))]
    public sealed class LevelDefinitionEditor : UnityEditor.Editor
    {
        private const string FillCategoryPropertyName = "<FillCategoryNames>k__BackingField";
        private const string LayerCountPropertyName = "<LayerCount>k__BackingField";

        /// <summary>
        /// Draws the inspector and replaces the raw fill category string list with a checklist.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, FillCategoryPropertyName, LayerCountPropertyName);
            DrawLayerCountInspector();
            DrawLayerRefreshButtons();
            DrawFillCategoryInspector(serializedObject.FindProperty(FillCategoryPropertyName));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayerCountInspector()
        {
            LevelDefinition definition = target as LevelDefinition;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Layer Count", definition != null ? definition.GetResolvedLayerCount() : 0);
            }
        }

        private void DrawLayerRefreshButtons()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Layer Count"))
                {
                    if (LevelDefinitionLayerTools.RefreshLevelDefinition(target as LevelDefinition))
                    {
                        serializedObject.Update();
                    }
                }

                if (GUILayout.Button("Refresh All Levels"))
                {
                    LevelDefinitionLayerTools.RefreshAllLevelLayerCounts();
                }
            }
        }

        private static void DrawFillCategoryInspector(SerializedProperty fillCategoryProperty)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fill Categories", EditorStyles.boldLabel);

            if (fillCategoryProperty == null || !fillCategoryProperty.isArray)
            {
                EditorGUILayout.HelpBox("Không tìm thấy property FillCategoryNames để vẽ checklist category.", MessageType.Warning);
                return;
            }

            List<string> availableCategoryNames = GetAvailableFillCategoryNames();
            List<string> selectedCategoryNames = GetPropertyValues(fillCategoryProperty);

            if (availableCategoryNames.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa tìm thấy category nào trong MahjongMaterialSO. Tạo/sync category trước rồi quay lại tick ở đây.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Tick category muốn dùng cho riêng level này. Nếu để trống, runtime sẽ dùng toàn bộ fill category đang active.", MessageType.Info);

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
            }

            List<string> missingSelections = GetMissingSelections(selectedCategoryNames, availableCategoryNames);
            if (missingSelections.Count <= 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Một số category đã chọn không còn tồn tại trong MahjongMaterialSO. Mình vẫn giữ chúng để không mất cấu hình cũ.", MessageType.Warning);
            EditorGUILayout.LabelField("Missing / Legacy", EditorStyles.boldLabel);

            for (int index = 0; index < missingSelections.Count; index++)
            {
                string missingName = missingSelections[index];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(missingName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Remove", GUILayout.Width(80f)))
                    {
                        RemoveIgnoreCase(selectedCategoryNames, missingName);
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
                string selectedName = selectedNames[index];
                if (!ContainsIgnoreCase(availableNames, selectedName))
                {
                    missingSelections.Add(selectedName);
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
