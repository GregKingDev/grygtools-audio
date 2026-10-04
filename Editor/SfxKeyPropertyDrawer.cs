using GrygTools.Utils.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxKeyProperty))]
	public class SfxKeyPropertyDrawer : PropertyDrawer
	{
		protected int idHash => "SfxKey".GetHashCode();
		protected Dictionary<int, string> optionsDictionary = new ();
		protected List<string> optionsList = new ();
		protected Dictionary<object, string> nameDictionary = new();
		
		protected void OnSelect(SerializedProperty property, string stringValue)
		{
			property.stringValue = stringValue;
		}
		
		protected bool TypeCheck(SerializedProperty property, out string error)
		{
			error = null;
			if (property.type == "string")
			{
				return true;
			}
			error = "SfxKey searchable property must be used on a string value";
			return false;
		}

		protected bool IndexComparison(SerializedProperty property, string stringValue)
		{
			return stringValue == property.stringValue;
		}

		protected void Populate()
		{
			AudioKeysUtility.RefreshClipKeys();
			if (nameDictionary.Count <= 0 ||  AudioKeysUtility.AudioClipKeys.Count != optionsList.Count)
			{
				optionsDictionary.Clear();
				foreach (string clipKey in AudioKeysUtility.AudioClipKeys)
				{
					optionsDictionary.Add(clipKey.GetHashCode(), clipKey);
				}

				optionsList.Clear();
				optionsList.AddRange(optionsDictionary.Values);
				optionsList.Sort((a, b) => string.CompareOrdinal(a, b));
				nameDictionary.Clear();
				
				foreach (string sfxKey in optionsList)
				{
					nameDictionary.Add(sfxKey, sfxKey);
				}
			}
		}

		protected string GetButtonText(SerializedProperty property)
		{
			return "Search for SfxKey";
		}
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
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
            EditorGUI.BeginProperty(position, label, property);
            
            Rect firstLineRect = position;
            firstLineRect.height = EditorGUIUtility.singleLineHeight;
            firstLineRect.xMin += EditorGUI.indentLevel * 15f;

            var buttonText = new GUIContent(GetButtonText(property));
        
            if (DropdownButton(id, firstLineRect, buttonText))
            {
                Action<int> onSelect = i =>
                {
                    OnSelect(property, optionsList[i]);
                    property.serializedObject.ApplyModifiedProperties();
                };
        
                int index = 0;
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    index = optionsList.FindIndex(0, obj => IndexComparison(property, obj));
                }
            
                SearchablePopup.Show(firstLineRect, nameDictionary.Values.ToArray(), index, onSelect);
            }
            
            EditorGUI.EndProperty();
        
            Rect secondLineRect = position;
            secondLineRect.y += EditorGUIUtility.singleLineHeight;
            secondLineRect.height = EditorGUIUtility.singleLineHeight;
            
            EditorGUI.PropertyField(secondLineRect, property, label);
        }

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) * 2;	
		}

		protected static bool DropdownButton(int id, Rect position, GUIContent content)
		{
			Event current = Event.current;
			switch (current.type)
			{
				case EventType.MouseDown:
					if (position.Contains(current.mousePosition) && current.button == 0)
					{
						Event.current.Use();
						return true;
					}
					break;
				case EventType.KeyDown:
					if (GUIUtility.keyboardControl == id && current.character =='\n')
					{
						Event.current.Use();
						return true;
					}
					break;
				case EventType.Repaint:
					EditorStyles.popup.Draw(position, content, id, false);
					break;
			}
			return false;
		} 
	}
}
