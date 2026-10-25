using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxConfig))]
	public class SfxConfigDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var nameProp = property.FindPropertyRelative("m_SfxName");
			string sfxName = string.IsNullOrEmpty(nameProp.stringValue) ? "NOT-SET" : nameProp.stringValue;

			var newLabel = new GUIContent($"Sfx Config - {sfxName}", label.tooltip);
			EditorGUI.PropertyField(position, property, newLabel, true);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
}
