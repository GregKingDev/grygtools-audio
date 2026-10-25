using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(AudioTagCollisionSfxInfo))]
	internal class AudioTagCollisionSfxInfoDrawer : PropertyDrawer
	{
		private const string k_AudioTagName = "AudioTag";
		private static readonly Regex s_RangeIndex = new Regex(@"m_ImpulseRanges\.Array\.data\[(\d+)\]");

		private static bool IsDuplicate(SerializedProperty property, string tagName)
		{
			if (string.IsNullOrEmpty(tagName)) return false;
			if (property.serializedObject.targetObject is not AudioTagCollisionConfig config) return false;

			var match = s_RangeIndex.Match(property.propertyPath);
			if (!match.Success) return false;

			int rangeIndex = int.Parse(match.Groups[1].Value);
			var tag = new AudioCollisionTag(tagName);

			// Same tag listed more than once in this range
			if (rangeIndex < config.TagDuplicatesPerRange.Count
			    && config.TagDuplicatesPerRange[rangeIndex].Contains(tag))
				return true;

			// Any of this element's own tag-to-tag pairs duplicated in this range
			if (rangeIndex >= config.TagToTagDuplicatesPerRange.Count) return false;
			var pairDupes = config.TagToTagDuplicatesPerRange[rangeIndex];
			if (pairDupes.Count == 0) return false;

			SerializedProperty responses = property.FindPropertyRelative("TagToTagCollisionData");
			for (int i = 0; i < responses.arraySize; i++)
			{
				SerializedProperty others = responses.GetArrayElementAtIndex(i).FindPropertyRelative("OtherTags");
				for (int j = 0; j < others.arraySize; j++)
				{
					string otherName = others.GetArrayElementAtIndex(j).FindPropertyRelative("_value")?.stringValue;
					if (string.IsNullOrEmpty(otherName)) continue;
					if (pairDupes.Contains((tag, new AudioCollisionTag(otherName))))
						return true;
				}
			}
			return false;
		}
		
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
