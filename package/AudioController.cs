using GrygTools.AssetManagement;
using GrygToolsUtils;
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
		public const float MaxMusicVolume = 1f;

		public const string MasterVolumeName = "MasterVolume";

		private const float VolumeLogScalar = 20f;
		private const float VolumeZeroEquivalent = 0.00001f;

		private const uint MaxConcurrent = 100;
		private const uint PerSfxMaxConcurrent = 5;
		public const float MinTimeSinceLastPlay = 0.01f;
		
		public delegate void SfxEndCallback();
		
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
		
#region music fields
		private Transform musicPoolTransform = null;
		private MusicTrackComponent musicCompTemplate = null;
		
		private readonly Dictionary<int, MusicTrackComponent> musicDictionary = new Dictionary<int, MusicTrackComponent>();
		private MusicTrackComponent playingTrack = null;

		private AudioMixerGroup musicGroup;
#endregion
		
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
				if (category.isMusicGroup)
				{
					musicGroup = groups[0];
				}
				else
				{
					sfxCategoryToGroup.Add(category.id, groups[0]);
				}
			}
			
			LoadVolumeFromSettings();
			
			Transform trans = transform;
			GameObject sfxPoolObj = new GameObject("SfxPool");
			sfxPoolTransform = sfxPoolObj.transform;
			sfxPoolTransform.parent = trans;
			
			GameObject musicPoolObj = new GameObject("MusicPool");
			musicPoolTransform = musicPoolObj.transform;
			musicPoolTransform.parent = trans;
			
			GameObject sfxObjTemplate = new GameObject("sfxTemplate");
			sfxObjTemplate.transform.parent = transform;
			sfxCompTemplate = sfxObjTemplate.AddComponent<SfxComponent>();
			sfxCompTemplate.Source.volume = 1f;
			sfxCompTemplate.Source.playOnAwake = false;
			sfxCompTemplate.Source.spatialize = false;
			sfxCompTemplate.Source.spatialBlend = 0;
			
			GameObject musicObjTemplate = new GameObject("musicTemplate");
			musicObjTemplate.transform.parent = transform;
			musicCompTemplate = musicObjTemplate.AddComponent<MusicTrackComponent>();
			musicCompTemplate.Source.spatialize = false;
			musicCompTemplate.Source.priority = 0;
			musicCompTemplate.Source.spatialBlend = 0;
			
			foreach (MusicPriorityCategory musicCategory in AudioSettings.musicCategories)
			{
				musicDictionary.Add(musicCategory.priority, Instantiate(musicCompTemplate, musicPoolObj.transform).Init(musicCategory));
			}
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

		public void LoadAudioConfig(IEnumerable<AudioClipConfig> configs)
		{
			foreach (AudioClipConfig config in configs)
			{
				LoadAudioConfig(config);
			}
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

		public async Task LoadAudioConfigAsync(IEnumerable<AudioClipConfig> configs)
		{
			List<Task> loadTasks = new List<Task>();
			foreach (AudioClipConfig config in configs)
			{
				loadTasks.Add(LoadAudioConfigAsync(config));
			}
			await Task.WhenAll(loadTasks);
		}

		public async Task LoadAudioConfigAsync(AudioClipConfig config)
		{
			List<Task<AudioClip>> loadTasks = new List<Task<AudioClip>>();
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
				loadTasks.Add(AddressableManager.Instance.LoadAssetReferenceAsync<AudioClip>(entry.reference));
			}
			await Task.WhenAll(loadTasks);

			for (int i = 0; i < loadTasks.Count; i++)
			{
				Task<AudioClip> task = loadTasks[i];
				AudioClipConfigEntry entry = config.Entries[i];
				if (!audioClipLoadRefCounts.TryAdd(entry.reference, 1))
				{
					audioClipLoadRefCounts[entry.reference]++;
				}

				concurrentMaxesDictionary[entry.key] =
					entry.maxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.maxSimultaneous;
				minimumTimeSinceLastPlayDictionary[entry.key] = entry.minTimeBetweenPlays;
			
				task.Result.LoadAudioData();
			
				if (clipsListDictionary.TryGetValue(entry.key, out List<AudioClip> value))
				{
					value.Add(task.Result);
				}
				else
				{
					clipsListDictionary[entry.key] = new List<AudioClip>(){task.Result};
				}
			}
		}
		
		public void UnloadAudioConfig(AudioClipConfig config)
		{
			InternalUnloadClipList(config.Entries);
		}
		
		public void UnloadAudioConfig(IEnumerable<AudioClipConfig> configs)
		{
			foreach (AudioClipConfig config in configs)
			{
				UnloadAudioConfig(config);
			}
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
		
#region music
		public void PlayTrack(MusicConfig config, SfxEndCallback onEndCallback = null)
		{
			if (config == null || !config.IsSet())
			{
				Debug.LogWarning("MusicConfig was null or no track was set.");
				return;
			}
			PlayTrack(config.TrackName, config.Priority, config.TrackVolume, config.Looping, config.CrossFadeTime, onEndCallback, config.StartOffset);
		}
		
		public void PlayTrack(string clipName, int priority, float vol = 1f, bool loop = true, float crossFadeTime = 0,
			SfxEndCallback onEndCallback = null, float startOffset = 0f)
		{
			InternalPlayTrack(clipName, priority, vol, loop, crossFadeTime, onEndCallback, startOffset);
		}
		
		private void InternalPlayTrack(string clipName, int priority, float vol = 1f, bool loop = true,
			float crossFadeTime = 0, SfxEndCallback onEndCallback = null, float startOffset = 0f)
		{
			if (TryGetClipFromName(clipName, out AudioClip clip))
			{
				if(musicDictionary.TryGetValue(priority, out MusicTrackComponent targetComponent))
				{
					//Check if track is playing or waiting to play at that priority, try to resume the track if waiting on priority
					if (targetComponent.TrackName == clipName)
					{
						if (targetComponent.IsPlaying() || targetComponent.IsWaitingOnPriority)
						{
							ResumeNextPriority();
							return;
						}
					}

					//If there is no track playing or the priority attempting to play is not busy play this track now
					if (playingTrack == null || !playingTrack.IsBusy)
					{
						targetComponent.PlayTrack(musicGroup, clip, clipName, crossFadeTime / 2, vol, loop, onEndCallback, true, startOffset);
						playingTrack = targetComponent;
					}
					else if(priority >= playingTrack.Priority)
					{
						targetComponent.SetTrackData(musicGroup, clip, clipName, vol, loop, onEndCallback, startOffset);
						
						playingTrack.FadeOut(crossFadeTime / 2, () =>
						{
							playingTrack.SuspendTrack();
							ResumeNextPriority(crossFadeTime / 2);
						});
					}
					else // set data, do not transition to track
					{
						targetComponent.SetTrackData(musicGroup, clip, clipName, vol, loop, onEndCallback, startOffset);
					}
				}
			}
		}
		
		public void SuspendAllMusic()
		{
			playingTrack = null;
			foreach (KeyValuePair<int,MusicTrackComponent> pair in musicDictionary)
			{
				if (pair.Value.IsPlaying())
				{
					pair.Value.SuspendTrack();
				}
			}
		}

		public void ResumeMusic()
		{
			ResumeNextPriority();
		}

		internal void ResumeNextPriority(float fadeTime = 0f)
		{
			
			for (int i = AudioSettings.musicCategories.Count - 1; i >= 0; i--)
			{
				if (musicDictionary.TryGetValue(AudioSettings.musicCategories[i].priority, out MusicTrackComponent track))
				{
					//If a valid track is already playing then there is no need to resume
					if (track.IsPlaying())
					{
						return;
					}
					
					if (track.IsWaitingOnPriority)
					{
						track.Unpause(fadeTime != 0 ? fadeTime : track.FadeInTime);
						playingTrack = track;
						return;
					}
				}
			}
		}

		public void StopTrack(MusicConfig config, bool fadeOut = false)
		{
			if (config == null || !config.IsSet())
			{
				Debug.LogWarning("Music config is null or no track is set.");
				return;
			}

			StopTrack(config.TrackName, config.CrossFadeTime/2);
		}

		public void StopTrack(string trackName, float fadeTime = 0f)
		{
			foreach (KeyValuePair<int,MusicTrackComponent> pair in musicDictionary)
			{
				MusicTrackComponent track = pair.Value;
				if (track.TrackName == trackName)
				{
					if (track == playingTrack)
					{
						if (fadeTime > 0f)
						{
							track.FadeOut(fadeTime, () =>
							{
								track.StopTrack();
								ResumeNextPriority(fadeTime);
							});

						}
						else
						{
							track.StopTrack();
							ResumeNextPriority();
						}
					}
					else
					{
						track.StopTrack();	
					}

					return;
				}
			}
		}
		
		public void StopTrackByPriority(int priority)
		{
			if (musicDictionary.TryGetValue(priority, out MusicTrackComponent track))
			{
				track.StopTrack();
				if (playingTrack == track)
				{
					track.FadeOut(track.FadeOutTime, () =>
					{
						track.StopTrack();
						ResumeNextPriority();
					});
				}
			}
		}

		public void StopAllTracks()
		{
			foreach (KeyValuePair<int,MusicTrackComponent> pair in musicDictionary)
			{
				pair.Value.StopTrack();
			}
		}

		public void SetTrackPosition(string trackName, float position)
		{
			foreach (MusicTrackComponent trackComponent in musicDictionary.Values)
			{
				if (trackComponent.TrackName.Equals(trackName))
				{
					trackComponent.SetPosition(position);
				}
			}
		}

		public void SetTrackPosition(MusicConfig config, float position)
		{
			SetTrackPosition(config.Priority, position);
		}
		
		public void SetTrackPosition(int trackType, float position)
		{
			if (musicDictionary.TryGetValue(trackType, out MusicTrackComponent track))
			{
				track.SetPosition(position);
			}
		}

		public void SetPlayingTrackPosition(float position)
		{
			playingTrack.SetPosition(position);
		}
#endregion music
	}
}