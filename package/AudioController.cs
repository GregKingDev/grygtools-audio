using Cysharp.Threading.Tasks;
using GrygTools.AddressableUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace GrygTools.Audio
{
	public class AudioController : MbSingleton<AudioController>
	{
		public const float MaxSfxVolume = 1f;

		public const string MasterVolumeName = "MasterVolume";

		private const float VolumeLogScalar = 20f;
		private const float VolumeZeroEquivalent = 0.00001f;

		private const uint MaxConcurrent = 100;
		private const uint PerSfxMaxConcurrent = 5;
		public const float MinTimeSinceLastPlay = 0.01f;
		
		private bool isMuted = false;

		private bool isSfxMuted = false;
		
		private SfxComponent sfxCompTemplate = null;
		private Transform sfxPoolTransform = null;
		
		private readonly List<SfxComponent> sfxPool = new List<SfxComponent>();

		private readonly Dictionary<int, AudioMixerGroup> sfxCategoryToGroup = new();
		
		private readonly Dictionary<string, List<AudioClip>> clipsListDictionary =
			new Dictionary<string, List<AudioClip>>();
		
		private readonly Dictionary<Tuple<string, int>, SfxComponent> activeSfxDictionary =
			new Dictionary<Tuple<string, int>, SfxComponent>();

		private readonly Dictionary<AssetReferenceT<AudioClip>, int> audioClipLoadRefCounts = new Dictionary<AssetReferenceT<AudioClip>, int>();
		private readonly Dictionary<string, uint> concurrentCountDictionary = new Dictionary<string, uint>();
		public Dictionary<string, uint> ConcurrentCountDictionary => concurrentCountDictionary;
		private readonly Dictionary<string, uint> concurrentMaxesDictionary = new Dictionary<string, uint>();
		
		private readonly Dictionary<string, float> minimumTimeSinceLastPlayDictionary = new Dictionary<string, float>();
		private readonly Dictionary<string, float> lastPlayedDictionary = new Dictionary<string, float>();

		private readonly Dictionary<int, List<SfxComponent>> activeSfxByCategory = new();
		
		private AudioMixer masterMixer;
		private AudioMixer MasterMixer
		{
			get
			{
				masterMixer ??= (AudioMixer)Resources.Load("MasterMixer");
				return masterMixer;
			}
		}

		private GrygAudioSettings audioSettings;
		private GrygAudioSettings AudioSettings
		{
			get
			{
				audioSettings ??= GrygAudioSettings.GetOrCreateSettings();
				return audioSettings;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		static void OnRuntimeInitialized()
		{
			Instance.StartCoroutine(Instance.DelayedLoadVolumeFromSettings());
		}

		protected override void Init()
		{
			foreach (SfxCategory category in AudioSettings.sfxCategories)
			{
				AudioMixerGroup[] groups = MasterMixer.FindMatchingGroups(category.targetGroupName);
				sfxCategoryToGroup.Add(category.id, groups[0]);
			}
			
			LoadVolumeFromSettings();
			
			Transform trans = transform;
			GameObject sfxPoolObj = new GameObject("SfxPool");
			sfxPoolTransform = sfxPoolObj.transform;
			sfxPoolTransform.parent = trans;
			
			GameObject sfxObjTemplate = new GameObject("sfxTemplate");
			sfxObjTemplate.transform.parent = transform;
			sfxCompTemplate = sfxObjTemplate.AddComponent<SfxComponent>();
			sfxCompTemplate.Source.volume = 1f;
			sfxCompTemplate.Source.playOnAwake = false;
			sfxCompTemplate.Source.spatialize = false;
			sfxCompTemplate.Source.spatialBlend = 0;
		}
		
		private IEnumerator DelayedLoadVolumeFromSettings()
		{
			yield return 0;
			LoadVolumeFromSettings();
		}

		private void LoadVolumeFromSettings()
		{
			SetVolume(AudioSettings.masterVolume);
			foreach (SfxCategory sfxCategory in AudioSettings.sfxCategories)
			{
				SetSfxVolume(sfxCategory.id, sfxCategory.volume);
			}
		}

		internal bool TryGetClipFromName(string key, out AudioClip clip)
		{
			clip = null;
			if (clipsListDictionary.TryGetValue(key, out List<AudioClip> clipList))
			{
				clip = clipList[Random.Range(0, clipList.Count)];
			}
			
			if (clip != null)
			{
				return true;
			}
			
			return false;
		}
		
		private uint GetMaxConcurrent(string key)
		{
			if (concurrentMaxesDictionary.TryGetValue(key, out uint count))
			{
				return count;
			}

			return MaxConcurrent;
		}

		private bool IsAtMaxConcurrent(string clipName)
		{
			if (concurrentCountDictionary.TryGetValue(clipName, out uint currentCount))
			{
				if (GetMaxConcurrent(clipName) <= currentCount)
				{
					return true;
				}
			}
			return false;
		}
		
		private bool CheckTimeBetweenPlays(string key)
		{
			if (minimumTimeSinceLastPlayDictionary.TryGetValue(key, out float timeBetweenPlays))
			{
				if (lastPlayedDictionary.TryGetValue(key, out float lastPlayed))
				{
					if (Time.realtimeSinceStartup - lastPlayed < timeBetweenPlays)
					{
						return false;
					}
				}
			}

			return true;
		}
		
		public void PlaySfx(SfxConfig config, GameObject sourceObject)
		{
			PlaySfx(config.SfxName, config.ForcePlay ? null : sourceObject, config.SfxCategory, config.Looping, 
				Random.Range(config.PitchRandomization.x, config.PitchRandomization.y), config.SfxVolume, config.SfxDelay);
		}

		public void ForcePlaySfx(string clipName, int category, bool loop = false, float pitch = 1, float volume = 1f, float delay = 1f)
		{
			PlaySfx(clipName, null, category, loop, pitch, volume, delay);
		}
		
		public void PlaySfx(string clipName, GameObject sourceObject, int category, bool loop = false, float pitch = 1f, float volume = 1f, float delay = 1f)
		{
			if (TryGetClipFromName(clipName, out AudioClip clip))
			{
				if (IsAtMaxConcurrent(clipName))
				{
					return;
				}
			}

			if (clip == null)
			{
				Debug.LogWarning($"No Audio Clip loaded for sfxName {clipName}");
				return;
			}
			
			if (!CheckTimeBetweenPlays(clipName))
			{
				return;
			}
			
			SfxComponent sfxComp = LeaseSfxComponent();
			lastPlayedDictionary[clipName] = Time.realtimeSinceStartup;

			if (sfxCategoryToGroup.TryGetValue(category, out AudioMixerGroup group))
			{
				sfxComp.PlaySfx(group, clip, clipName, sourceObject, volume, loop, delay, null, category, pitch);
			}
		}

		internal void IncrementClipCount(SfxComponent comp)
		{
			if (concurrentCountDictionary.ContainsKey(comp.SfxName))
			{
				concurrentCountDictionary[comp.SfxName]++;
			}
			else
			{
				concurrentCountDictionary[comp.SfxName] = 1;
			}

			activeSfxDictionary[new Tuple<string, int>(comp.SfxName, comp.RequestingObjHash)] = comp;
			if (activeSfxByCategory.ContainsKey(comp.Category))
			{
				activeSfxByCategory[comp.Category].Add(comp);
			}
			else
			{
				activeSfxByCategory[comp.Category] = new List<SfxComponent>(){comp};
			}
		}
		
		internal void DecrementClipCount(SfxComponent comp)
		{
			if (concurrentCountDictionary[comp.SfxName] > 0)
			{
				concurrentCountDictionary[comp.SfxName]--;
			}

			activeSfxDictionary.Remove(new Tuple<string, int>(comp.SfxName, comp.RequestingObjHash));
			activeSfxByCategory[comp.Category].Remove(comp);
		}
		
		private SfxComponent LeaseSfxComponent()
		{
			for (int i = sfxPool.Count - 1; i >= 0; i--)
			{
				if (!sfxPool[i].IsBusy)
				{
					sfxPool[i].SetBusy(true);
					sfxPool[i].gameObject.SetActive(true);

					return sfxPool[i];
				}
			}

			SfxComponent newComp = Instantiate(sfxCompTemplate, sfxPoolTransform);

			newComp.Source.volume = 1f;
			newComp.Source.spatialize = false;
			newComp.Source.spatialBlend = 0;
			newComp.SetBusy(true);
			sfxPool.Add(newComp);

			return newComp;
		}
		
		internal void ReturnSfxObject(SfxComponent comp)
		{
			Transform sourceTransform = comp.transform;
			sourceTransform.parent = sfxPoolTransform;
			sourceTransform.position = sfxPoolTransform.position;
			comp.SetBusy(false);
		}
		
		internal void RemoveSfxCompOnDestroy(SfxComponent comp)
		{
			sfxPool.Remove(comp);
		}

		public void LoadAudioConfig(AudioClipConfig config)
		{
			foreach (AudioClipConfigEntry entry in config.Entries)
			{
				if (entry.reference == null || !entry.reference.RuntimeKeyIsValid())
				{
					continue;
				}
				if (string.IsNullOrEmpty(entry.key))
				{
					continue;
				}
				var loadedClip = AddressableManager.Instance.LoadAssetReference<AudioClip>(entry.reference);
			
				if (audioClipLoadRefCounts.ContainsKey(entry.reference))
				{
					audioClipLoadRefCounts[entry.reference]++;
				}
				else
				{
					audioClipLoadRefCounts[entry.reference] = 1;
				}
		
				concurrentMaxesDictionary[entry.key] =
					entry.maxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.maxSimultaneous;
				minimumTimeSinceLastPlayDictionary[entry.key] = entry.minTimeBetweenPlays;
		
				loadedClip.LoadAudioData();
		
				if (clipsListDictionary.ContainsKey(entry.key))
				{
					clipsListDictionary[entry.key].Add(loadedClip);
				}
				else
				{
					clipsListDictionary[entry.key] = new List<AudioClip>(){loadedClip};
				}
			}
		}

		public async Task LoadAudioConfigAsync(AudioClipConfig config)
		{
			foreach (AudioClipConfigEntry entry in config.Entries)
			{
				if (entry.reference == null || !entry.reference.RuntimeKeyIsValid())
				{
					continue;
				}
				if (string.IsNullOrEmpty(entry.key))
				{
					continue;
				}
				var loadedClip = await AddressableManager.Instance.LoadAssetReferenceAsync<AudioClip>(entry.reference);
				
				if (audioClipLoadRefCounts.ContainsKey(entry.reference))
				{
					audioClipLoadRefCounts[entry.reference]++;
				}
				else
				{
					audioClipLoadRefCounts[entry.reference] = 1;
				}
			
				concurrentMaxesDictionary[entry.key] =
					entry.maxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.maxSimultaneous;
				minimumTimeSinceLastPlayDictionary[entry.key] = entry.minTimeBetweenPlays;
			
				loadedClip.LoadAudioData();
			
				if (clipsListDictionary.ContainsKey(entry.key))
				{
					clipsListDictionary[entry.key].Add(loadedClip);
				}
				else
				{
					clipsListDictionary[entry.key] = new List<AudioClip>(){loadedClip};
				}
			}
		}
		
		// private async void BuildAndValidateClipList(List<AudioClipConfigEntry> entries)
		// {
		// 	foreach (AudioClipConfigEntry entry in entries)
		// 	{
		// 		if (entry.reference == null || !entry.reference.RuntimeKeyIsValid())
		// 		{
		// 			continue;
		// 		}
		// 		if (string.IsNullOrEmpty(entry.key))
		// 		{
		// 			continue;
		// 		}
		//
		// 		try
		// 		{
		// 			if (AddressableManager.Instance.TryLoadAssetReference(entry.reference, out AudioClip loadedClip))
		// 			{
		// 				if (audioClipLoadRefCounts.ContainsKey(entry.reference))
		// 				{
		// 					audioClipLoadRefCounts[entry.reference]++;
		// 				}
		// 				else
		// 				{
		// 					audioClipLoadRefCounts[entry.reference] = 1;
		// 				}
		//
		// 				concurrentMaxesDictionary[entry.key] =
		// 					entry.maxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.maxSimultaneous;
		// 				minimumTimeSinceLastPlayDictionary[entry.key] = entry.minTimeBetweenPlays;
		//
		// 				loadedClip.LoadAudioData();
		//
		// 				if (clipsListDictionary.ContainsKey(entry.key))
		// 				{
		// 					clipsListDictionary[entry.key].Add(loadedClip);
		// 				}
		// 				else
		// 				{
		// 					clipsListDictionary[entry.key] = new List<AudioClip>(){loadedClip};
		// 				}
		// 			}
		// 		}
		// 		catch (Exception e)
		// 		{
		// 			Debug.LogError($"Failed to load audio addressable. Key:{entry.key} clip:{entry.reference}\nException:{e}");
		// 		}
		// 	}
		// }

		public void UnloadAudioConfig(AudioClipConfig config)
		{
			InternalUnloadClipList(config.Entries);
		}
		
		private void InternalUnloadClipList(List<AudioClipConfigEntry> entries)
		{
			foreach (AudioClipConfigEntry entry in entries)
			{
				if (clipsListDictionary.TryGetValue(entry.key, out List<AudioClip> clipList))
				{
					if (clipList != null)
					{
						if (entry.reference != null)
						{
							if (audioClipLoadRefCounts.ContainsKey(entry.reference))
							{
								audioClipLoadRefCounts[entry.reference]--;
								if (audioClipLoadRefCounts[entry.reference] <= 0)
								{
									if (AddressableManager.Instance.TryGetIfLoaded(entry.reference, out AudioClip loadedClip))
									{
										loadedClip.UnloadAudioData();
									}
									audioClipLoadRefCounts.Remove(entry.reference);
									AddressableManager.Instance.ReleaseAssetReference(entry.reference);
								}
							}
							else
							{
								AddressableManager.Instance.ReleaseAssetReference(entry.reference);
							}
						}

						if (clipList.Count <= 0)
						{
							clipsListDictionary.Remove(entry.key);
						}
					}
				}
			}
		}
		
		public void SetVolume(float newVolume) 
		{
			float adjustedVolume = isMuted ? 0 : Mathf.Clamp(newVolume, VolumeZeroEquivalent, 1);
			MasterMixer.SetFloat(MasterVolumeName, Mathf.Log(adjustedVolume) * VolumeLogScalar);
			AudioSettings.masterVolume = newVolume;
		}

		public float GetCategoryVolume(int category)
		{
			return AudioSettings.GetCategoryVolume(category);
		}
		
		public void SetSfxVolume(int category, float newVolume)
		{
			float adjustedVolume = isSfxMuted ? 0 : Mathf.Clamp(newVolume, VolumeZeroEquivalent, MaxSfxVolume);
			
			AudioSettings.SetCategoryVolume(category, newVolume);
			SfxCategory data = AudioSettings.GetCategoryData(category);
			if (data != null)
			{
				MasterMixer.SetFloat(data.targetGroupName, Mathf.Log(adjustedVolume) * VolumeLogScalar);
			}
		}
	}
}