using System;
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
        private Dictionary<int, bool> rangeFoldoutStates = new();
        private Dictionary<string, bool> collisionsFoldoutStates = new();
        private Dictionary<string, bool> textureCollisionsFoldoutStates = new();
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
                        if (!rangeFoldoutStates.ContainsKey(index))
                            rangeFoldoutStates[index] = false;

                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        var textureCollisionsProperty = element.FindPropertyRelative("m_TextureAndMaterialCollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";

                        if (!listsPerRange.ContainsKey(key))
                        {
                            CreateNestedLists(key, collisionsProperty, textureCollisionsProperty, index);
                        }

                        float y = rect.y;
                        
                        // Draw foldout and range property
                        bool prevRangeState = rangeFoldoutStates[index];
                        rangeFoldoutStates[index] = EditorGUI.Foldout(new Rect(rect.x, y, 15, EditorGUIUtility.singleLineHeight), rangeFoldoutStates[index], "");
                        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width - 15, EditorGUIUtility.singleLineHeight), rangeProperty);
                        
                        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        
                        // Draw nested lists if expanded
                        if (rangeFoldoutStates[index] && listsPerRange.TryGetValue(key, out var listPair))
                        {
                            float indent = EditorGUI.indentLevel * 15;
                            string collisionsKey = key + "_collisions";
                            string textureCollisionsKey = key + "_textureCollisions";
                            
                            if (!collisionsFoldoutStates.ContainsKey(collisionsKey))
                                collisionsFoldoutStates[collisionsKey] = true;
                            if (!textureCollisionsFoldoutStates.ContainsKey(textureCollisionsKey))
                                textureCollisionsFoldoutStates[textureCollisionsKey] = true;
                            
                            // Draw Audio Collision SFX foldout
                            bool prevCollisionsState = collisionsFoldoutStates[collisionsKey];
                            collisionsFoldoutStates[collisionsKey] = EditorGUI.Foldout(new Rect(rect.x + indent, y, rect.width - indent, EditorGUIUtility.singleLineHeight), collisionsFoldoutStates[collisionsKey], "Tag to Tag Collision SFX");
                            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            
                            if (collisionsFoldoutStates[collisionsKey])
                            {
                                listPair.CollisionsList.DoList(new Rect(rect.x + indent, y, rect.width - indent, listPair.CollisionsList.GetHeight()));
                                y += listPair.CollisionsList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                            }
                            
                            // Draw Texture Collision SFX foldout
                            bool prevTextureState = textureCollisionsFoldoutStates[textureCollisionsKey];
                            textureCollisionsFoldoutStates[textureCollisionsKey] = EditorGUI.Foldout(new Rect(rect.x + indent, y, rect.width - indent, EditorGUIUtility.singleLineHeight), textureCollisionsFoldoutStates[textureCollisionsKey], "Texture&PhysMat Collision SFX");
                            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            
                            if (textureCollisionsFoldoutStates[textureCollisionsKey])
                            {
                                try
                                {
                                    listPair.TextureCollisionsList.DoList(new Rect(rect.x + indent, y, rect.width - indent, listPair.TextureCollisionsList.GetHeight()));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    throw;
                                }
                                
                            }
                        }
                        EditorGUI.indentLevel--;
                    },
                    elementHeightCallback = index =>
                    {
                        if (!rangeFoldoutStates.ContainsKey(index))
                            rangeFoldoutStates[index] = false;

                        var element = impulseRangeProperty.GetArrayElementAtIndex(index);
                        var rangeProperty = element.FindPropertyRelative("m_ImpulseRange");
                        var collisionsProperty = element.FindPropertyRelative("m_CollisionSfx");
                        var textureCollisionsProperty = element.FindPropertyRelative("m_TextureAndMaterialCollisionSfx");
                        string key = $"{impulseRangeProperty.propertyPath}[{index}]";
                        
                        if (!listsPerRange.ContainsKey(key))
                        {
                            CreateNestedLists(key, collisionsProperty, textureCollisionsProperty, index);
                        }

                        float height = EditorGUI.GetPropertyHeight(rangeProperty) + EditorGUIUtility.standardVerticalSpacing;
                        
                        if (rangeFoldoutStates[index] && listsPerRange.TryGetValue(key, out var listPair))
                        {
                            string collisionsKey = key + "_collisions";
                            string textureCollisionsKey = key + "_textureCollisions";
                            
                            if (!collisionsFoldoutStates.ContainsKey(collisionsKey))
                                collisionsFoldoutStates[collisionsKey] = true;
                            if (!textureCollisionsFoldoutStates.ContainsKey(textureCollisionsKey))
                                textureCollisionsFoldoutStates[textureCollisionsKey] = true;
                            
                            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            if (collisionsFoldoutStates[collisionsKey])
                                height += listPair.CollisionsList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                            
                            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            if (textureCollisionsFoldoutStates[textureCollisionsKey])
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
                drawHeaderCallback = rect => { },
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
                    if( config.TagToTagDuplicatesPerRange.Count > impulseRangeIndex)
                    {
                        var duplicatesList = config.TagToTagDuplicatesPerRange[impulseRangeIndex];
                        duplicate = duplicatesList.Contains((new AudioCollisionTag(tag1), new AudioCollisionTag(tag2)))
                            || duplicatesList.Contains((new AudioCollisionTag(tag2), new AudioCollisionTag(tag1)));
                    }
                    
                    var oldColor = GUI.color;
                    if (duplicate)
                    {
                        GUI.color = Color.lightPink;
                    }

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
                drawHeaderCallback = rect => { },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = textureCollisionsProperty.GetArrayElementAtIndex(index);
                    
                    bool isDuplicate = false;
                    if (config.TagDuplicatesPerRange.Count > impulseRangeIndex && 
                        element.FindPropertyRelative("AudioCollisionTag") != null)
                    {
                        var tag = element.FindPropertyRelative("AudioCollisionTag")?.FindPropertyRelative("_value").stringValue;
                        if (config.TagDuplicatesPerRange[impulseRangeIndex].Contains(new AudioCollisionTag(tag)))
                        {
                            isDuplicate = true;
                        }
                    }
                    
                    if (!isDuplicate && config.TagToTextureDuplicatesPerRange.Count > impulseRangeIndex)
                    {
                        var tag = element.FindPropertyRelative("AudioCollisionTag")?.FindPropertyRelative("_value").stringValue;
                        var texturesAndMaterialsArray = element.FindPropertyRelative("TexturesAndMaterialsToSfx");
                        
                        for (int i = 0; i < texturesAndMaterialsArray.arraySize; i++)
                        {
                            var texturesArray = texturesAndMaterialsArray.GetArrayElementAtIndex(i).FindPropertyRelative("Textures");
                            for (int j = 0; j < texturesArray.arraySize; j++)
                            {
                                var texture = texturesArray.GetArrayElementAtIndex(j).objectReferenceValue as Texture;
                                if (texture && config.TagToTextureDuplicatesPerRange[impulseRangeIndex].Contains((new AudioCollisionTag(tag), texture)))
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            if (isDuplicate) break;
                        }
                    }
                    
                    if (!isDuplicate && config.TagToPhysicsMaterialDuplicatesPerRange.Count > impulseRangeIndex)
                    {
                        var tag = element.FindPropertyRelative("AudioCollisionTag")?.FindPropertyRelative("_value").stringValue;
                        var texturesAndMaterialsArray = element.FindPropertyRelative("TexturesAndMaterialsToSfx");
                        
                        for (int i = 0; i < texturesAndMaterialsArray.arraySize; i++)
                        {
                            var materialsArray = texturesAndMaterialsArray.GetArrayElementAtIndex(i).FindPropertyRelative("PhysicsMaterials");
                            for (int j = 0; j < materialsArray.arraySize; j++)
                            {
                                var material = materialsArray.GetArrayElementAtIndex(j).objectReferenceValue as PhysicsMaterial;
                                if (material && config.TagToPhysicsMaterialDuplicatesPerRange[impulseRangeIndex].Contains((new AudioCollisionTag(tag), material)))
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            if (isDuplicate) break;
                        }
                    }
                    
                    var oldColor = GUI.color;
                    if (isDuplicate)
                    {
                        GUI.color = Color.lightPink;
                    }
                    
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(rect, element, true);
                    EditorGUI.indentLevel--;
                    GUI.color = oldColor;
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