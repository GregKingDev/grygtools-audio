#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrygTools.Audio
{
	public class GrygAudioSettingsProvider : SettingsProvider
	{
		private SerializedObject m_CustomSettings;

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
			EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("MasterVolume"));
			
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