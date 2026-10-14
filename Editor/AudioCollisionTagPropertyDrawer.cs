using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(AudioCollisionTagAttribute))]
	public class AudioCollisionTagDrawer : PropertyDrawer
	{
		private static GrygAudioSettings m_Settings;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			if (fieldInfo.FieldType != typeof(AudioCollisionTag)
			    && fieldInfo.FieldType != typeof(List<AudioCollisionTag>)
			    && fieldInfo.FieldType != typeof(AudioCollisionTag[]))
			{
				EditorGUI.LabelField(position, label.text, "Use [AudioCollisionTag] with AudioCollisionTag type only.");
				return;
			}
			if (m_Settings == null)
			{
				m_Settings = GrygAudioSettings.GetOrCreateSettings();
			}
			var currentValue = property.FindPropertyRelative("_value").stringValue;
			int currentIndex = Array.IndexOf(m_Settings.CollisionTagsStrings, currentValue);
			List<string> strings = new();
			foreach (AudioCollisionTag tag in m_Settings.CollisionTags)
			{
				strings.Add(tag.ToString());
			}
			strings.Add("-");
			if(currentIndex == -1)
			{
				currentIndex = strings.Count - 1;
			}
			int newIndex = EditorGUI.Popup(position, "Audio Collision Tag:", currentIndex, strings.ToArray());

			if (newIndex != currentIndex)
			{
				property.FindPropertyRelative("_value").stringValue = strings[newIndex];
			}
		}
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
