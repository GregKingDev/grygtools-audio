using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(AudioTagCollisionResponseData))]
	internal class AudioTagCollisionResponseDataDrawer : PropertyDrawer
	{
		private const string k_OtherTagsName = "OtherTags";
		private const string k_SfxConfigName = "SfxConfig";

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_OtherTagsName), true) + EditorGUIUtility.standardVerticalSpacing;
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_SfxConfigName), true);
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty otherTags = property.FindPropertyRelative(k_OtherTagsName);
			SerializedProperty sfxConfig = property.FindPropertyRelative(k_SfxConfigName);
			string sfxName = sfxConfig.FindPropertyRelative("m_SfxName")?.stringValue;
			string displayName = string.IsNullOrEmpty(sfxName) ? "None" : sfxName;

			Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginProperty(position, label, property);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayName, true);
			EditorGUI.EndProperty();

			if (!property.isExpanded)
			{
				return;
			}

			EditorGUI.indentLevel++;
			float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			float tagsHeight = EditorGUI.GetPropertyHeight(otherTags, true);
			EditorGUI.PropertyField(new Rect(position.x, y, position.width, tagsHeight), otherTags, true);
			y += tagsHeight + EditorGUIUtility.standardVerticalSpacing;

			float sfxHeight = EditorGUI.GetPropertyHeight(sfxConfig, true);
			EditorGUI.PropertyField(new Rect(position.x, y, position.width, sfxHeight), sfxConfig, true);
			EditorGUI.indentLevel--;
		}
	}
}
