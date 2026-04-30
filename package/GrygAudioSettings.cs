using GrygToolsUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrygTools.Audio
{
	[Serializable]
	public class SfxCategorySettings
	{
		[ReadOnly]
		public int Id;
		public string Name;
		public string TargetGroupName;
		public bool IsMusicGroup;
	}

	[Serializable]
	public class VolumeSettings
	{
		[Range(0f, 1f)]
		public float Volume;
		public bool IsMuted;
	}
	
	[Serializable]
	public class MusicPriorityCategory
	{
		[ReadOnly][Tooltip("Higher values take precedence in playing, if 0 is playing and 1 is requested 0 will be stopped and 1 started. Upon stopping 1 0 will resume")]
		public int Priority;
		public string Name;
	}
	
	public class GrygAudioSettings : ScriptableObject
	{
		private const string MuteKey = "Mute";
		private const string VolumeKey = "Volume";
		private const string MasterMuteKey = "MasterMute";
		private const string MasterVolumeKey = "MasterVolume";
		
		public const string AudioSettingsPath = "Assets/Resources/AudioSettings.asset";

		[SerializeField]
		public List<SfxCategorySettings> SfxCategories;
		
		[SerializeField]
		public List<MusicPriorityCategory> MusicCategories;

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
				settings.SfxCategories = new List<SfxCategorySettings>();
				
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

		public float GetMasterVolume()
		{
			return PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
		}

		public void SetMasterVolume(float volume)
		{
			PlayerPrefs.SetFloat(MasterVolumeKey, volume);
		}
		
		public void SetMasterMute(bool isMuted)
		{
			PlayerPrefs.SetInt(MasterMuteKey, isMuted ? 1 : 0);
		}
		
		public bool GetMasterMute()
		{
			return PlayerPrefs.GetInt(MasterMuteKey, 0) == 1;
		}
		
		public void SetCategoryMute(int id, bool isMuted)
		{
			foreach (SfxCategorySettings sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					PlayerPrefs.SetInt(GetMuteKey(id), isMuted ? 1 : 0);
				}
			}
		}
		
		public bool GetCategoryMute(int id)
		{
			foreach (SfxCategorySettings sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					return PlayerPrefs.GetInt(GetMuteKey(id), 0) == 1;
				}
			}
			return false;
		}
		
		public float GetCategoryVolume(int id)
		{
			foreach (SfxCategorySettings sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					return PlayerPrefs.GetFloat(GetVolumeKey(id), 1f);;
				}
			}
			return 1;
		}

		public void SetCategoryVolume(int id, float volume)
		{
			foreach (SfxCategorySettings sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					PlayerPrefs.SetFloat(GetVolumeKey(id), volume);
				}
			}
		}

		public SfxCategorySettings GetCategoryData(int id)
		{
			foreach (SfxCategorySettings category in SfxCategories)
			{
				if (category.Id == id)
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
				AudioController.Instance.SetMasterVolume(GetMasterVolume());
				foreach (SfxCategorySettings category in SfxCategories)
				{
					AudioController.Instance.SetSfxVolume(category.Id, PlayerPrefs.GetFloat(GetVolumeKey(category), 1f));
				}
			}
		}
		
		private string GetVolumeKey(int id)
		{
			return $"{VolumeKey}{id.ToString()}";
		}
		
		private string GetVolumeKey(SfxCategorySettings categorySettings)
		{
			return $"{VolumeKey}{categorySettings.Id.ToString()}";
		}
		
		private string GetMuteKey(int id)
		{
			return $"{MuteKey}{id.ToString()}";
		}
		
		private string GetMuteKey(SfxCategorySettings categorySettings)
		{
			return $"{MuteKey}{categorySettings.Id.ToString()}";
		}
		
		internal void RunSfxValidation()
		{
			HashSet<int> ids = new();
			List<int> sfxIndecesToBeRemoved = new();
			List<int> musicIndecesToBeToggled = new();
			int musicGroupCount = 0;
			int highestId = 0;
			for (int i = 0; i < SfxCategories.Count; i++)
			{
				if(SfxCategories[i].IsMusicGroup)
				{
					musicGroupCount++;
					if (musicGroupCount > 1)
					{
						musicIndecesToBeToggled.Add(i);
						Debug.LogError("Audio Categories list already contains a music group. Adjusting music group settings.");
					}
				}
				if (!ids.Add(SfxCategories[i].Id))
				{
					Debug.LogError($"Audio Categories list already contains ID {SfxCategories[i].Id}. Adjusting Ids.");
					sfxIndecesToBeRemoved.Add(i);
				}
				highestId = Math.Max(highestId, SfxCategories[i].Id);
			}

			if (musicGroupCount < 1)
			{
				Debug.LogError($"No music group found, you will not be able to use gryg tools to play music without a music group set.");
			}

			for(int i = 0; i < sfxIndecesToBeRemoved.Count; i++)
			{
				highestId++;
				SfxCategories[sfxIndecesToBeRemoved[i]].Id = highestId;
			}

			for (int i = 0; i < musicIndecesToBeToggled.Count; i++)
			{
				SfxCategories[musicIndecesToBeToggled[i]].IsMusicGroup = false;
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
			for (int i = 0; i < MusicCategories.Count; i++)
			{
				if (!ids.Add(MusicCategories[i].Priority))
				{
					Debug.LogError($"Audio Categories list already contains ID {MusicCategories[i].Priority}. Adjusting Ids.");
					musicIndecesToBeRemoved.Add(i);
				}
				highestId = Math.Max(highestId, MusicCategories[i].Priority);
			}

			for(int i = 0; i < musicIndecesToBeRemoved.Count; i++)
			{
				highestId++;
				MusicCategories[musicIndecesToBeRemoved[i]].Priority = highestId;
			}
			
			if (musicIndecesToBeRemoved.Count > 0)
			{
				UnityEditor.SettingsService.NotifySettingsProviderChanged();
			}
		}
#endif
	}
}