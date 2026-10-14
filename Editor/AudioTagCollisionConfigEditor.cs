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
                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";
                        
                        if (!collisionsListsPerRange.ContainsKey(key))
                            CreateNestedList(key, collisionsProperty);
                        
                        float y = rect.y;
                        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), rangeProperty);
                        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        
                        if (collisionsListsPerRange.TryGetValue(key, out var list))
                        {
                            list.DoList(new Rect(rect.x, y, rect.width, list.GetHeight()));
                        }
                    },
                    elementHeightCallback = index =>
                    {
                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";
                        
                        if (!collisionsListsPerRange.ContainsKey(key))
                            CreateNestedList(key, collisionsProperty);
                        
                        float height = EditorGUI.GetPropertyHeight(rangeProperty) + EditorGUIUtility.standardVerticalSpacing;
                        if (collisionsListsPerRange.TryGetValue(key, out var list))
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

        private void CreateNestedList(string key, SerializedProperty collisionsProperty)
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
                    
                    bool duplicate = config.Duplicates.Contains((new AudioCollisionTag(tag1), new AudioCollisionTag(tag2)))
                        || config.Duplicates.Contains((new AudioCollisionTag(tag2), new AudioCollisionTag(tag1)));
                    
                    var oldColor = GUI.color;
                    if (duplicate)
                        GUI.color = Color.lightPink;
                    
                    string label = $"{tag1} - {tag2} -> {sfxKey}";
                    EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
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