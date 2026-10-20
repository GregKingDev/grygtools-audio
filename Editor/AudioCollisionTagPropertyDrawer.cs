using GrygTools.Utils.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(AudioCollisionTag))]
	public class AudioCollisionTagDrawer : PropertyDrawer
	{
		private int idHash => "AudioCollisionTag".GetHashCode();
		private Dictionary<int, string> optionsDictionary = new ();
		private List<string> optionsList = new ();
		private Dictionary<object, string> nameDictionary = new();
		
		private static GrygAudioSettings m_Settings;

		private void OnSelect(SerializedProperty property, string stringValue)
		{
			var thing = property.FindPropertyRelative("_value");
			thing.stringValue = stringValue == "-" ? string.Empty : stringValue;
		}
		
		private bool TypeCheck(SerializedProperty property, out string error)
		{
			error = null;
			if (property.type == "AudioCollisionTag")
			{
				return true;
			}
			error = "AudioCollisionTag searchable property must be used on AudioCollisionTag values";
			return false;
		}

		private bool IndexComparison(SerializedProperty property, string stringValue)
		{
			return stringValue == property.FindPropertyRelative("_value").stringValue;
		}

		private void Populate()
		{
			if (nameDictionary.Count <= 0 ||  m_Settings.CollisionTags.Count != optionsList.Count)
			{
				optionsDictionary.Clear();
				optionsList.Clear();
				nameDictionary.Clear();
				foreach (string collisionTag in m_Settings.CollisionTags)
				{
					optionsDictionary.Add(collisionTag.GetHashCode(), collisionTag);
				}
				optionsDictionary.Add("-".GetHashCode(), "-");
				
				optionsList.AddRange(optionsDictionary.Values);
				foreach (string collisionTag in optionsList)
				{
					nameDictionary.Add(collisionTag, collisionTag);
				}
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
	        if (m_Settings == null)
			{
				m_Settings = GrygAudioSettings.GetOrCreateSettings();
			}
	        
            if (!TypeCheck(property, out string errorString))
            {
                GUIStyle errorStyle = "CN EntryErrorIconSmall";
                Rect r = new Rect(position);
                r.width = errorStyle.fixedWidth;
                position.xMin = r.xMax;
                GUI.Label(r, "", errorStyle);
                GUI.Label(position,  errorString);
                return;
            }

            Populate();
        
            int id = GUIUtility.GetControlID(idHash, FocusType.Keyboard, position);
            
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, id, label);

	        var currentValue = property.FindPropertyRelative("_value").stringValue;
            var buttonText = new GUIContent(currentValue);
            
            if (EditorUtils.DropdownButton(id, position, buttonText))
            {
                Action<int> onSelect = i =>
                {
                    OnSelect(property, optionsList[i]);
                    property.serializedObject.ApplyModifiedProperties();
                };
            
                int index = 0;
                if (!string.IsNullOrEmpty(currentValue))
                {
                    index = optionsList.FindIndex(0, obj => IndexComparison(property, obj));
                }
            
                SearchablePopup.Show(position, nameDictionary.Values.ToArray(), index, onSelect);
            }
            EditorGUI.EndProperty();
        }

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) * 1;	
		}
	}
}
