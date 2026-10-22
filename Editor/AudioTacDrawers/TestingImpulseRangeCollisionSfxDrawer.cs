using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
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
}
