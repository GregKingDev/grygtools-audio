using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditorInternal;

namespace GrygTools.Audio
{
    [CustomEditor(typeof(AudioTagCollisionConfig))]
    public class AudioTagCollisionConfigEditor : Editor
    {
        private ReorderableList collisionsList;

        private void OnEnable()
        {
            var collisionsProperty = serializedObject.FindProperty("m_CollisionSfx");
            AudioTagCollisionConfig config = (AudioTagCollisionConfig)target;
            if (collisionsProperty != null)
            {
                collisionsList = new ReorderableList(serializedObject, collisionsProperty, true, true, true, true)
                {
                    drawHeaderCallback = (Rect rect) =>
                    {
                        EditorGUI.LabelField(rect, "Audio Collision SFX");
                    },
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        var element = collisionsProperty.GetArrayElementAtIndex(index);
                        var tag1 = element.FindPropertyRelative("AudioCollisionTag1").FindPropertyRelative("_value").stringValue;
                        var tag2 = element.FindPropertyRelative("AudioCollisionTag2").FindPropertyRelative("_value").stringValue;
                        var sfxKey = element.FindPropertyRelative("SfxKey").stringValue;
                        
                        
                        bool duplicate = config.Duplicates.Contains((new AudioCollisionTag(tag1), new AudioCollisionTag(tag2)))
                            || config.Duplicates.Contains((new AudioCollisionTag(tag2), new AudioCollisionTag(tag1)));
                        
                        var oldColor = GUI.color;
                        if(duplicate)
                        {
                            GUI.color = Color.lightPink;
                        }
                        string label = $"{tag1} - {tag2} -> {sfxKey}";
                        EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
                        GUI.color = oldColor;
                    },
                    elementHeightCallback = (int index) =>
                    {
                        var element = collisionsProperty.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
                    }
                };
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.TextArea("List of collisions pairs and their associated SFX keys. Duplicate pairs will be highlighted in pink.", GUI.skin.box);
            serializedObject.Update();
            AudioTagCollisionConfig config = (AudioTagCollisionConfig)target;
            
            if (collisionsList != null)
            {
                collisionsList.DoLayoutList();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CollisionSfx"));
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}