using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxCategorySettings))]
	public class SfxCategorySettingsDrawer : PropertyDrawer
	{
		private float m_SliderValue = 1f;
		private bool m_MuteValue = false;
		private int m_NumberOfElements = 9;
		
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
			return EditorGUIUtility.singleLineHeight * m_NumberOfElements + EditorGUIUtility.standardVerticalSpacing * m_NumberOfElements;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			float runningPos = rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			Rect runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.BeginProperty(runningRect, label, property.FindPropertyRelative("Id"));
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("Id"));
			EditorGUI.EndProperty();
			
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("MixerGroup"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("MixerGroup"));
			EditorGUI.EndProperty();

			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("Name"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("Name"));
			EditorGUI.EndProperty();
			
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("VolumeParameterName"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("VolumeParameterName"));
			EditorGUI.EndProperty();
			
			GUI.enabled = !Application.isPlaying;
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.BeginProperty(rect, label, property.FindPropertyRelative("IsMusicGroup"));
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.PropertyField(runningRect, property.FindPropertyRelative("IsMusicGroup"));
			EditorGUI.EndProperty();
			GUI.enabled = true;
			
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			int catId = property.FindPropertyRelative("Id").intValue;
			float oldCatVolume = AudioSettings.GetCategoryVolume(catId);
			runningRect = new Rect(rect.x, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			
			float originalRectWidth = rect.width;
			runningRect.width = 100f;
			EditorGUI.LabelField(runningRect, new GUIContent("Volume"));
			runningRect = new Rect(runningRect.x + 100, runningPos, originalRectWidth - 100, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			m_SliderValue = EditorGUI.Slider(runningRect, oldCatVolume, 0f, 1f);
			if (!Mathf.Approximately(m_SliderValue, oldCatVolume))
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetSfxVolume(catId, m_SliderValue);
				}
				else
				{
					AudioSettings.SetCategoryVolume(catId, m_SliderValue);
				}
			}
			
			runningPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			runningRect = new Rect(runningRect.x - 100, runningPos, originalRectWidth, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			EditorGUI.LabelField(runningRect, new GUIContent("Mute"));
			runningRect = new Rect(rect.x + 100, runningPos, rect.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			bool oldMuteValue = AudioSettings.GetCategoryMute(catId);
			m_MuteValue = EditorGUI.Toggle(runningRect, oldMuteValue);
			if (m_MuteValue != oldMuteValue)
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetCategoryMute(catId, m_MuteValue);
				}
				else
				{
					AudioSettings.SetCategoryMute(catId, m_MuteValue);
				}
			}
		}
	}
}
