using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(AudioTagMappings))]
	internal class AudioTagMappingsDrawer : PropertyDrawer
	{
		private const string k_AudioTagName = "AudioTag";
		private static readonly Regex s_MappingIndex = new Regex(@"m_AudioTagMappings\.Array\.data\[(\d+)\]");

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			foreach (SerializedProperty child in GetChildren(property))
			{
				height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
			}
			return height;
		}
		
		private static bool IsDuplicate(SerializedProperty property, string tagName)
		{
			if (property.serializedObject.targetObject is not AudioTagCollisionConfig config) return false;

			if (!string.IsNullOrEmpty(tagName) && config.TagDuplicates.Contains(new AudioCollisionTag(tagName)))
				return true;

			if (config.TextureToTagDuplicates.Count > 0)
			{
				SerializedProperty textures = property.FindPropertyRelative("Textures");
				for (int i = 0; i < textures.arraySize; i++)
				{
					var texture = textures.GetArrayElementAtIndex(i).objectReferenceValue as Texture;
					if (texture != null && config.TextureToTagDuplicates.Contains(texture))
						return true;
				}
			}

			if (config.PhysMatToTagDuplicates.Count > 0)
			{
				SerializedProperty mats = property.FindPropertyRelative("PhysicsMaterials");
				for (int i = 0; i < mats.arraySize; i++)
				{
					var mat = mats.GetArrayElementAtIndex(i).objectReferenceValue as PhysicsMaterial;
					if (mat != null && config.PhysMatToTagDuplicates.Contains(mat))
						return true;
				}
			}

			if (config.UnityTagToTagDuplicates.Count > 0)
			{
				SerializedProperty unityTags = property.FindPropertyRelative("UnityTags");
				for (int i = 0; i < unityTags.arraySize; i++)
				{
					string unityTag = unityTags.GetArrayElementAtIndex(i).stringValue;
					if (!string.IsNullOrEmpty(unityTag) && config.UnityTagToTagDuplicates.Contains(unityTag))
						return true;
				}
			}

			return false;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			string tagName = property.FindPropertyRelative(k_AudioTagName)?.FindPropertyRelative("_value")?.stringValue;
			string displayName = string.IsNullOrEmpty(tagName) ? "None" : tagName;

			Color oldColor = GUI.color;
			if (IsDuplicate(property, tagName))
			{
				GUI.color = Color.hotPink;
			}

			try
			{
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
				foreach (SerializedProperty child in GetChildren(property))
				{
					float childHeight = EditorGUI.GetPropertyHeight(child, true);
					EditorGUI.PropertyField(new Rect(position.x, y, position.width, childHeight), child, true);
					y += childHeight + EditorGUIUtility.standardVerticalSpacing;
				}
				EditorGUI.indentLevel--;
			}
			finally
			{
				GUI.color = oldColor;
			}
		}

		private static List<SerializedProperty> GetChildren(SerializedProperty property)
		{
			var children = new List<SerializedProperty>();
			SerializedProperty end = property.GetEndProperty();
			SerializedProperty iterator = property.Copy();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
			{
				children.Add(iterator.Copy());
				enterChildren = false;
			}
			return children;
		}
	}
}
