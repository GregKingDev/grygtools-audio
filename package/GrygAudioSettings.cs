using GrygToolsUtils;
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
		public bool isMusicGroup;
		[Range(0f, 1f)]
		public float volume = 1;
	}
	
	[Serializable]
	public class MusicPriorityCategory
	{
		[ReadOnly][Tooltip("Higher values take precendence in playing, if 0 is playing and 1 is requested 0 will be stopped and 1 started. Upon stopping 1 0 will resume")]
		public int priority;
		public string name;
	}
	
	public class GrygAudioSettings : ScriptableObject
	{
		public const string AudioSettingsPath = "Assets/Resources/AudioSettings.asset";

		[SerializeField]
		[Range(0f, 1f)]
		public float masterVolume = 1;

		[SerializeField]
		public List<SfxCategory> sfxCategories;
		
		[SerializeField]
		public List<MusicPriorityCategory> musicCategories;

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
		
		internal void RunSfxValidation()
		{
			HashSet<int> ids = new();
			List<int> sfxIndecesToBeRemoved = new();
			List<int> musicIndecesToBeToggled = new();
			int musicGroupCount = 0;
			int highestId = 0;
			for (int i = 0; i < sfxCategories.Count; i++)
			{
				if(sfxCategories[i].isMusicGroup)
				{
					musicGroupCount++;
					if (musicGroupCount > 1)
					{
						musicIndecesToBeToggled.Add(i);
						Debug.LogError("Audio Categories list already contains a music group. Adjusting music group settings.");
					}
				}
				if (!ids.Add(sfxCategories[i].id))
				{
					Debug.LogError($"Audio Categories list already contains ID {sfxCategories[i].id}. Adjusting Ids.");
					sfxIndecesToBeRemoved.Add(i);
				}
				highestId = Math.Max(highestId, sfxCategories[i].id);
			}

			if (musicGroupCount < 1)
			{
				Debug.LogError($"No music group found, you will not be able to use gryg tools to play music without a music group set.");
			}

			for(int i = 0; i < sfxIndecesToBeRemoved.Count; i++)
			{
				highestId++;
				sfxCategories[sfxIndecesToBeRemoved[i]].id = highestId;
			}

			for (int i = 0; i < musicIndecesToBeToggled.Count; i++)
			{
				sfxCategories[musicIndecesToBeToggled[i]].isMusicGroup = false;
			}
			
			if (sfxIndecesToBeRemoved.Count > 0 || musicIndecesToBeToggled.Count > 0)
			{
				UnityEditor.SettingsService.NotifySettingsProviderChanged();
			}
		}

		internal void RunMusicValidation()
		{
			HashSet<int> ids = new();
			List<int> musicIndecesToBeRemoved = new();
			int highestId = 0;
			for (int i = 0; i < musicCategories.Count; i++)
			{
				if (!ids.Add(musicCategories[i].priority))
				{
					Debug.LogError($"Audio Categories list already contains ID {musicCategories[i].priority}. Adjusting Ids.");
					musicIndecesToBeRemoved.Add(i);
				}
				highestId = Math.Max(highestId, musicCategories[i].priority);
			}

			for(int i = 0; i < musicIndecesToBeRemoved.Count; i++)
			{
				highestId++;
				musicCategories[musicIndecesToBeRemoved[i]].priority = highestId;
			}
			
			if (musicIndecesToBeRemoved.Count > 0)
			{
				UnityEditor.SettingsService.NotifySettingsProviderChanged();
			}
		}
#endif
	}
}