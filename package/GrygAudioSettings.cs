using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrygTools.Audio
{
	[Serializable]
	public class SfxCategory
	{
		[ReadOnly]
		public int id;
		public string name;
		public string targetGroupName;
		[Range(0f, 1f)]
		public float volume = 1;
	}
	
	public class GrygAudioSettings : ScriptableObject
	{
		public const string AudioSettingsPath = "Assets/Resources/AudioSettings.asset";

		[SerializeField]
		[Range(0f, 1f)]
		public float masterVolume = 1;

		[SerializeField]
		public List<SfxCategory> sfxCategories;

		public static GrygAudioSettings GetOrCreateSettings()
		{
			var settings = Resources.Load<GrygAudioSettings>("AudioSettings");
			if (settings == null)
			{
#if UNITY_EDITOR
				if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
				{
					UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
				}
				settings = ScriptableObject.CreateInstance<GrygAudioSettings>();
				settings.sfxCategories = new List<SfxCategory>();
				settings.masterVolume = 1f;
				UnityEditor.AssetDatabase.CreateAsset(settings, AudioSettingsPath);
				UnityEditor.AssetDatabase.SaveAssets();
#endif
			}
			
			if (settings == null)
			{
				Debug.LogError($"Unable to create GrygAudioSettings object, please create at Assets/Resources/AudioSettings.asset or open Project Settings/GrygAudio");
			}
			
			return settings;
		}

		public float GetCategoryVolume(int id)
		{
			foreach (SfxCategory sfxCategory in sfxCategories)
			{
				if (sfxCategory.id == id)
				{
					return sfxCategory.volume;
				}
			}
			return 1;
		}

		public void SetCategoryVolume(int id, float volume)
		{
			foreach (SfxCategory sfxCategory in sfxCategories)
			{
				if (sfxCategory.id == id)
				{
					sfxCategory.volume = volume;
				}
			}
		}

		public SfxCategory GetCategoryData(int id)
		{
			foreach (SfxCategory category in sfxCategories)
			{
				if (category.id == id)
				{
					return category;
				}
			}
			return null;
		}
		
#if UNITY_EDITOR
		public static UnityEditor.SerializedObject GetSerializedSettings()
		{
			return new UnityEditor.SerializedObject(GetOrCreateSettings());
		}

		public void OnValidate()
		{
			if (Application.isPlaying && AudioController.Instance != null)
			{
				AudioController.Instance.SetVolume(masterVolume);
				foreach (SfxCategory category in sfxCategories)
				{
					AudioController.Instance.SetSfxVolume(category.id, category.volume);
				}
			}
		}
		
		public void RunValidation()
		{
			HashSet<int> ids = new();
			List<int> indecesToBeRemoved = new();
			int highestId = 0;
			for (int i = 0; i < sfxCategories.Count; i++)
			{
				if (!ids.Add(sfxCategories[i].id))
				{
					Debug.LogError($"Audio Categories list already contains ID {sfxCategories[i].id}. Adjusting Ids.");
					indecesToBeRemoved.Add(i);
				}
				highestId = Math.Max(highestId, sfxCategories[i].id);
			}

			for(int i = 0; i < indecesToBeRemoved.Count; i++)
			{
				highestId++;
				sfxCategories[indecesToBeRemoved[i]].id = highestId;
			}
			
			if (indecesToBeRemoved.Count > 0)
			{
				UnityEditor.SettingsService.NotifySettingsProviderChanged();
			}
		}
#endif
	}
}