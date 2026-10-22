using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomEditor(typeof(TestingAudioTagCollisionConfig))]
	public class TestingAudioTagCollisionConfigEditor: Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Fix Ranges", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.FixRanges();
			}
			
			if (GUILayout.Button("Build Lookups", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.BuildLookups();
			}
			
			if (GUILayout.Button("Validate Ranges", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.ValidateRanges();
			}
		}
	}
	
	[CustomPropertyDrawer(typeof(TestingImpulseRangeCollisionSfx))]
	internal class TestingImpulseRangeCollisionSfxDrawer : PropertyDrawer
	{
		private const string k_ImpulseRangeName = "m_ImpulseRange";
		private const string k_CollisionSfxName = "m_CollisionSfx";

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_ImpulseRangeName), true) + EditorGUIUtility.standardVerticalSpacing;
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_CollisionSfxName), true);
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty impulseRange = property.FindPropertyRelative(k_ImpulseRangeName);
			SerializedProperty collisionSfx = property.FindPropertyRelative(k_CollisionSfxName);
			Vector2 range = impulseRange.vector2Value;

			Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginProperty(position, label, property);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"Impulse {range.x} - {range.y}", true);
			EditorGUI.EndProperty();

			if (!property.isExpanded)
			{
				return;
			}

			EditorGUI.indentLevel++;
			float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			float rangeHeight = EditorGUI.GetPropertyHeight(impulseRange, true);
			EditorGUI.PropertyField(new Rect(position.x, y, position.width, rangeHeight), impulseRange, true);
			y += rangeHeight + EditorGUIUtility.standardVerticalSpacing;

			float sfxHeight = EditorGUI.GetPropertyHeight(collisionSfx, true);
			EditorGUI.PropertyField(new Rect(position.x, y, position.width, sfxHeight), collisionSfx, true);
			EditorGUI.indentLevel--;
		}
	}
	
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
	
	[CustomPropertyDrawer(typeof(AudioTagCollisionSfxInfo))]
	internal class AudioTagCollisionSfxInfoDrawer : PropertyDrawer
	{
		private const string k_AudioTagName = "AudioTag";

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
	
	[CustomPropertyDrawer(typeof(AudioTagMappings))]
	internal class AudioTagMappingsDrawer : PropertyDrawer
	{
		private const string k_AudioTagName = "AudioTag";

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

