#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrygTools.Audio
{
	public class GrygAudioSettingsProvider : SettingsProvider
	{
		private SerializedObject customSettings;

		public GrygAudioSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
			: base(path, scope)
		{
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			customSettings = GrygAudioSettings.GetSerializedSettings();
		}

		public override void OnGUI(string searchContext)
		{
			EditorGUILayout.PropertyField(customSettings.FindProperty("masterVolume"));
			
			EditorGUILayout.PropertyField(customSettings.FindProperty("sfxCategories"));

			if (GUILayout.Button("Validate") && (customSettings.targetObject is GrygAudioSettings settings))
			{
				settings.RunValidation();
			}
			
			customSettings.ApplyModifiedPropertiesWithoutUndo();
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			return new GrygAudioSettingsProvider("Project/GrygTools/GrygAudio", SettingsScope.Project);
		}
	}
}
#endif