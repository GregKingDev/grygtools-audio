#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrygTools.Audio
{
	public class GrygAudioSettingsProvider : SettingsProvider
	{
		private SerializedObject m_CustomSettings;
		
		private float m_MasterSliderValue = 1f;
		private bool m_MasterMuteValue = false;
		
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
			// Master volume slider
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Master Volume", GUILayout.Width(100));
			float oldMasterVolume = AudioSettings.GetMasterVolume();
			m_MasterSliderValue = EditorGUILayout.Slider(oldMasterVolume, 0, 1);
			
			if (!Mathf.Approximately(m_MasterSliderValue, oldMasterVolume))
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetMasterVolume(m_MasterSliderValue);
				}
				else
				{
					AudioSettings.SetMasterVolume(m_MasterSliderValue);
				}
			}
			EditorGUILayout.EndHorizontal();
			
			//Master volume mute
			EditorGUILayout.BeginHorizontal();
			bool oldMasterMute = AudioSettings.GetMasterMute();
			m_MasterMuteValue = EditorGUILayout.Toggle("Master Mute", oldMasterMute);
			if(m_MasterMuteValue != oldMasterMute)
			{
				if (Application.isPlaying)
				{
					AudioController.Instance.SetMasterMute(m_MasterMuteValue);
				}
				else
				{
					AudioSettings.SetMasterMute(m_MasterMuteValue);
				}
			}
			EditorGUILayout.EndHorizontal();
			
			
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