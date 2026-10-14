using GrygTools.Utils.Extensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace GrygTools.Audio
{
    [CustomEditor(typeof(AudioTagCollisionConfig))]
    public class AudioTagCollisionConfigEditor : Editor
    {
        private ReorderableList impulseRangeList;
        private Dictionary<string, ReorderableList> collisionsListsPerRange = new();
        private Dictionary<int, bool> foldoutStates = new();
        private AudioTagCollisionConfig config;

        private void OnEnable()
        {
            config = (AudioTagCollisionConfig)target;
            var impulseRangeProperty = serializedObject.FindProperty("m_ImpulseRangeCollisionSfx");
            
            if (impulseRangeProperty != null)
            {
                impulseRangeList = new ReorderableList(serializedObject, impulseRangeProperty, true, true, true, true)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Impulse Range Collision SFX"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        EditorGUI.indentLevel++;
                        if (!foldoutStates.ContainsKey(index))
                            foldoutStates[index] = false;

                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";

                        if (!collisionsListsPerRange.ContainsKey(key))
                        {
                            CreateNestedList(key, collisionsProperty, index);
                        }

                        float y = rect.y;
                        
                        // Draw foldout and range property
                        foldoutStates[index] = EditorGUI.Foldout(new Rect(rect.x, y, 15, EditorGUIUtility.singleLineHeight), foldoutStates[index], "");
                        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width - 15, EditorGUIUtility.singleLineHeight), rangeProperty);
                        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        
                        // Draw nested list if expanded
                        if (foldoutStates[index] && collisionsListsPerRange.TryGetValue(key, out var list))
                        {
                            float indent = EditorGUI.indentLevel * 15;
                            list.DoList(new Rect(rect.x + indent, y, rect.width - indent, list.GetHeight()));
                        }
                        EditorGUI.indentLevel--;
                    },
                    elementHeightCallback = index =>
                    {
                        if (!foldoutStates.ContainsKey(index))
                            foldoutStates[index] = false;

                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";
                        
                        if (!collisionsListsPerRange.ContainsKey(key))
                        {
                            CreateNestedList(key, collisionsProperty, index);
                        }

                        float height = EditorGUI.GetPropertyHeight(rangeProperty) + EditorGUIUtility.standardVerticalSpacing;
                        
                        if (foldoutStates[index] && collisionsListsPerRange.TryGetValue(key, out var list))
                            height += list.GetHeight();
                        
                        return height;
                    }
                };
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.TextArea("Collision pairs organized by impulse range. Duplicate pairs will be highlighted in pink.", GUI.skin.box);
            
            serializedObject.Update();
            
            if (impulseRangeList != null)
            {
                impulseRangeList.DoLayoutList();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateNestedList(string key, SerializedProperty collisionsProperty, int impulseRangeIndex)
        {
            var list = new ReorderableList(serializedObject, collisionsProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Audio Collision SFX"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = collisionsProperty.GetArrayElementAtIndex(index);
                    var tag1 = element.FindPropertyRelative("AudioCollisionTag1").FindPropertyRelative("_value").stringValue;
                    var tag2 = element.FindPropertyRelative("AudioCollisionTag2").FindPropertyRelative("_value").stringValue;
                    var sfxKey = element.FindPropertyRelative("SfxConfig").FindPropertyRelative("m_SfxName").stringValue;
                    
                    // duplicate check needs to be fixed up. it will need to be remade into a dictionary to be able to check for duplicates properly.
                    // The dictionary will need to be keyed off the ImpulseRangeCollisionSfx entries then they'll need to be found here and used to find the right dupe list
                    bool duplicate = false;
                    if( config.DuplicatesPerRange.Count > impulseRangeIndex)
                    {
                        var duplicatesList = config.DuplicatesPerRange[impulseRangeIndex];
                        duplicate = duplicatesList.Contains((new AudioCollisionTag(tag1), new AudioCollisionTag(tag2)))
                            || duplicatesList.Contains((new AudioCollisionTag(tag2), new AudioCollisionTag(tag1)));
                    }
                    
                    var oldColor = GUI.color;
                    if (duplicate)
                        GUI.color = Color.lightPink;
                    
                    string label = $"{tag1} - {tag2} -> {sfxKey}";
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
                    EditorGUI.indentLevel--;
                    GUI.color = oldColor;
                },
                elementHeightCallback = index =>
                {
                    var element = collisionsProperty.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
                }
            };
            
            collisionsListsPerRange[key] = list;
        }
    }
}