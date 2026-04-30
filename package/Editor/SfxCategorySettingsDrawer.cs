using GrygToolsUtils;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxCategorySettings))]
	public class SfxCategorySettingsDrawer : PropertyDrawer
	{
		private float sliderValue = 1f;
		
		private GrygAudioSettings m_AudioSettings;
		private GrygAudioSettings AudioSettings
		{
			get
			{
				m_AudioSettings ??= GrygAudioSettings.GetOrCreateSettings();
				return m_AudioSettings;
			}
		}
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing*6;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			float runningPos = rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			Rect runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.BeginProperty(runningRect, label, property.FindPropertyRelative("Id"));
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("Id"));
			EditorGUI.EndProperty();

			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("Name"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("Name"));
			EditorGUI.EndProperty();

			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("TargetGroupName"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("TargetGroupName"));
			EditorGUI.EndProperty();

			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("IsMusicGroup"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("IsMusicGroup"));
			EditorGUI.EndProperty();
			
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			int catId = property.FindPropertyRelative("Id").intValue;
			float oldCatVolume = AudioSettings.GetCategoryVolume(catId);
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			
			float originalRectWidth = rect.width;
			runningRect.width = 100f;
			EditorGUI.LabelField(runningRect, new GUIContent("Volume"));
			runningRect = new Rect(runningRect.x + 100, runningPos, originalRectWidth - 100, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			sliderValue = EditorGUI.Slider(runningRect, oldCatVolume, 0f, 1f);
			if (!Mathf.Approximately(sliderValue, oldCatVolume))
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetSfxVolume(catId, sliderValue);
				}
				else
				{
					AudioSettings.SetCategoryVolume(catId, sliderValue);
				}
			}
		}
	}
}
