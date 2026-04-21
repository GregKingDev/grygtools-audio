using GrygToolsUtils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	[Serializable]
	public class SfxCategory
	{
		[ReadOnly]
		public int Id;
		public string Name;
		public string TargetGroupName;
		public bool IsMusicGroup;
		[Range(0f, 1f)]
		public float Volume = 1;
	}
	
	[Serializable]
	public class MusicPriorityCategory
	{
		[ReadOnly][Tooltip("Higher values take precendence in playing, if 0 is playing and 1 is requested 0 will be stopped and 1 started. Upon stopping 1 0 will resume")]
		public int Priority;
		public string Name;
	}
	
	public class GrygAudioSettings : ScriptableObject
	{
		public const string AudioSettingsPath = "Assets/Resources/AudioSettings.asset";

		[SerializeField]
		[Range(0f, 1f)]
		public float MasterVolume = 1;

		[SerializeField]
		public List<SfxCategory> SfxCategories;
		
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
				settings.SfxCategories = new List<SfxCategory>();
				settings.MasterVolume = 1f;
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
			foreach (SfxCategory sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					return sfxCategory.Volume;
				}
			}
			return 1;
		}

		public void SetCategoryVolume(int id, float volume)
		{
			foreach (SfxCategory sfxCategory in SfxCategories)
			{
				if (sfxCategory.Id == id)
				{
					sfxCategory.Volume = volume;
				}
			}
		}

		public SfxCategory GetCategoryData(int id)
		{
			foreach (SfxCategory category in SfxCategories)
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
				AudioController.Instance.SetVolume(MasterVolume);
				foreach (SfxCategory category in SfxCategories)
				{
					AudioController.Instance.SetSfxVolume(category.Id, category.Volume);
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