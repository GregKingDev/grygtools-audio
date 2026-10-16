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
        private class RangeListsPair
        {
            public ReorderableList CollisionsList;
            public ReorderableList TextureCollisionsList;
        }

        private ReorderableList impulseRangeList;
        private Dictionary<string, RangeListsPair> listsPerRange = new();
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
                        var textureCollisionsProperty = element.FindPropertyRelative("m_TextureCollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";

                        if (!listsPerRange.ContainsKey(key))
                        {
                            CreateNestedLists(key, collisionsProperty, textureCollisionsProperty, index);
                        }

                        float y = rect.y;
                        
                        // Draw foldout and range property
                        foldoutStates[index] = EditorGUI.Foldout(new Rect(rect.x, y, 15, EditorGUIUtility.singleLineHeight), foldoutStates[index], "");
                        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width - 15, EditorGUIUtility.singleLineHeight), rangeProperty);
                        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        
                        // Draw nested lists if expanded
                        if (foldoutStates[index] && listsPerRange.TryGetValue(key, out var listPair))
                        {
                            float indent = EditorGUI.indentLevel * 15;
                            listPair.CollisionsList.DoList(new Rect(rect.x + indent, y, rect.width - indent, listPair.CollisionsList.GetHeight()));
                            y += listPair.CollisionsList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                            listPair.TextureCollisionsList.DoList(new Rect(rect.x + indent, y, rect.width - indent, listPair.TextureCollisionsList.GetHeight()));
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
                        var textureCollisionsProperty = element.FindPropertyRelative("m_TextureCollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";
                        
                        if (!listsPerRange.ContainsKey(key))
                        {
                            CreateNestedLists(key, collisionsProperty, textureCollisionsProperty, index);
                        }

                        float height = EditorGUI.GetPropertyHeight(rangeProperty) + EditorGUIUtility.standardVerticalSpacing;
                        
                        if (foldoutStates[index] && listsPerRange.TryGetValue(key, out var listPair))
                        {
                            height += listPair.CollisionsList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                            height += listPair.TextureCollisionsList.GetHeight();
                        }
                        
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
            
            if (GUILayout.Button("Fix Ranges", GUILayout.Height(30)))
            {
                config.FixRanges();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateNestedLists(string key, SerializedProperty collisionsProperty, SerializedProperty textureCollisionsProperty, int impulseRangeIndex)
        {
            var collisionsList = new ReorderableList(serializedObject, collisionsProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Audio Collision SFX"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = collisionsProperty.GetArrayElementAtIndex(index);
                    string tag1, tag2, sfxKey = null;
                    if (element == null)
                    {
                        tag1 = string.Empty;
                        tag2 = string.Empty;
                        sfxKey = string.Empty;
                    }
                    else
                    {
                        tag1 = element.FindPropertyRelative("AudioCollisionTag1")?.FindPropertyRelative("_value").stringValue;
                        tag2 = element.FindPropertyRelative("AudioCollisionTag2")?.FindPropertyRelative("_value").stringValue;
                        sfxKey = element.FindPropertyRelative("SfxConfig")?.FindPropertyRelative("m_SfxName").stringValue;                        
                    }
                    
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

            var textureCollisionsList = new ReorderableList(serializedObject, textureCollisionsProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Texture Collision SFX"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = textureCollisionsProperty.GetArrayElementAtIndex(index);
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(rect, element, true);
                    EditorGUI.indentLevel--;
                },
                elementHeightCallback = index =>
                {
                    var element = textureCollisionsProperty.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
                }
            };
            
            listsPerRange[key] = new RangeListsPair { CollisionsList = collisionsList, TextureCollisionsList = textureCollisionsList };
        }
    }
}