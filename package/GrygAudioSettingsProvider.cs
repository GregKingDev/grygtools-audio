#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrygTools.Audio
{
	public class GrygAudioSettingsProvider : SettingsProvider
	{
		private SerializedObject m_CustomSettings;
		
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

		public GrygAudioSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
			: base(path, scope)
		{
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			m_CustomSettings = GrygAudioSettings.GetSerializedSettings();
		}

		public override void OnGUI(string searchContext)
		{
			// EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("MasterVolume"));
			
			float oldMasterVolume = AudioSettings.GetMasterVolume();
			sliderValue = EditorGUILayout.Slider(oldMasterVolume, 0, 1);
			
			if (!Mathf.Approximately(sliderValue, oldMasterVolume))
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetMasterVolume(sliderValue);
				}
				else
				{
					AudioSettings.SetMasterVolume(sliderValue);
				}
			}
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("SfxCategories"));
			
			if (GUILayout.Button("Validate") && (m_CustomSettings.targetObject is GrygAudioSettings settings))
			{
				settings.RunSfxValidation();
			}
			
			EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("MusicCategories"));
			
			if (GUILayout.Button("Validate") && (m_CustomSettings.targetObject is GrygAudioSettings musicSettings))
			{
				musicSettings.RunMusicValidation();
			}
			
			m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			return new GrygAudioSettingsProvider("Project/GrygTools/GrygAudio", SettingsScope.Project);
		}
	}
}
#endif