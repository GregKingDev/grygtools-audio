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
		internal const float MaxSfxVolume = 1f;
		internal const float MaxMusicVolume = 1f;

		internal const string MasterVolumeName = "MasterVolume";

		private const float VolumeLogScalar = 20f;
		private const float VolumeZeroEquivalent = 0.00001f;

		private const uint MaxConcurrent = 100;
		private const uint PerSfxMaxConcurrent = 5;
		public const float MinTimeSinceLastPlay = 0.01f;
		
		public delegate void SfxEndCallback();
		
		private bool m_IsMuted = false;

		private bool m_IsSfxMuted = false;
		
		private SfxComponent m_SfxCompTemplate = null;
		private Transform m_SfxPoolTransform = null;
		
		private readonly List<SfxComponent> m_SfxPool = new List<SfxComponent>();

		private readonly Dictionary<int, AudioMixerGroup> m_SfxCategoryToGroup = new();
		
		private readonly Dictionary<string, ClipLibrary> m_ClipsListDictionary =
			new Dictionary<string, ClipLibrary>();
		
		
		private readonly Dictionary<Tuple<string, int>, SfxComponent> m_ActiveSfxDictionary =
			new Dictionary<Tuple<string, int>, SfxComponent>();

		private readonly Dictionary<AssetReferenceT<AudioClip>, int> m_AudioClipLoadRefCounts = new Dictionary<AssetReferenceT<AudioClip>, int>();
		private readonly Dictionary<string, uint> m_ConcurrentCountDictionary = new Dictionary<string, uint>();
		public Dictionary<string, uint> ConcurrentCountDictionary => m_ConcurrentCountDictionary;
		private readonly Dictionary<string, uint> m_ConcurrentMaxesDictionary = new Dictionary<string, uint>();
		
		private readonly Dictionary<string, float> m_MinimumTimeSinceLastPlayDictionary = new Dictionary<string, float>();
		private readonly Dictionary<string, float> m_LastPlayedDictionary = new Dictionary<string, float>();

		private readonly Dictionary<int, List<SfxComponent>> m_ActiveSfxByCategory = new();
		
#region music fields
		private Transform m_MusicPoolTransform = null;
		private MusicTrackComponent m_MusicCompTemplate = null;
		
		private readonly Dictionary<int, MusicTrackComponent> m_MusicDictionary = new Dictionary<int, MusicTrackComponent>();
		private MusicTrackComponent m_PlayingTrack = null;

		private AudioMixerGroup m_MusicGroup;
#endregion
		
		private AudioMixer m_MasterMixer;
		private AudioMixer MasterMixer
		{
			get
			{
				m_MasterMixer ??= (AudioMixer)Resources.Load("MasterMixer");
				return m_MasterMixer;
			}
		}

		private GrygAudioSettings m_AudioSettings;
		private GrygAudioSettings AudioSettings
		{
			get
			{
				m_AudioSettings ??= GrygAudioSettings.GetOrCreateSettings();
				return m_AudioSettings;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		static void OnRuntimeInitialized()
		{
			Instance.StartCoroutine(Instance.DelayedLoadVolumeFromSettings());
		}

		protected override void Init()
		{
			foreach (SfxCategorySettings category in AudioSettings.SfxCategories)
			{
				AudioMixerGroup[] groups = MasterMixer.FindMatchingGroups(category.TargetGroupName);
				if (category.IsMusicGroup)
				{
					m_MusicGroup = groups[0];
				}
				else
				{
					m_SfxCategoryToGroup.Add(category.Id, groups[0]);
				}
			}
			
			LoadVolumeFromSettings();
			
			Transform trans = transform;
			GameObject sfxPoolObj = new GameObject("SfxPool");
			m_SfxPoolTransform = sfxPoolObj.transform;
			m_SfxPoolTransform.parent = trans;
			
			GameObject musicPoolObj = new GameObject("MusicPool");
			m_MusicPoolTransform = musicPoolObj.transform;
			m_MusicPoolTransform.parent = trans;
			
			GameObject sfxObjTemplate = new GameObject("sfxTemplate");
			sfxObjTemplate.transform.parent = transform;
			m_SfxCompTemplate = sfxObjTemplate.AddComponent<SfxComponent>();
			m_SfxCompTemplate.Source.volume = 1f;
			m_SfxCompTemplate.Source.playOnAwake = false;
			m_SfxCompTemplate.Source.spatialize = false;
			m_SfxCompTemplate.Source.spatialBlend = 0;
			
			GameObject musicObjTemplate = new GameObject("musicTemplate");
			musicObjTemplate.transform.parent = transform;
			m_MusicCompTemplate = musicObjTemplate.AddComponent<MusicTrackComponent>();
			m_MusicCompTemplate.Source.spatialize = false;
			m_MusicCompTemplate.Source.priority = 0;
			m_MusicCompTemplate.Source.spatialBlend = 0;
			
			foreach (MusicPriorityCategory musicCategory in AudioSettings.MusicCategories)
			{
				m_MusicDictionary.Add(musicCategory.Priority, Instantiate(m_MusicCompTemplate, musicPoolObj.transform).Init(musicCategory));
			}
		}
		
		private IEnumerator DelayedLoadVolumeFromSettings()
		{
			yield return 0;
			LoadVolumeFromSettings();
		}

		private void LoadVolumeFromSettings()
		{
			SetMasterVolume(AudioSettings.GetMasterVolume());
			foreach (SfxCategorySettings sfxCategory in AudioSettings.SfxCategories)
			{
				SetSfxVolume(sfxCategory.Id, AudioSettings.GetCategoryVolume(sfxCategory.Id));
			}
		}

		internal bool TryGetClipFromName(string key, out AudioClip clip)
		{
			clip = null;
			if (m_ClipsListDictionary.TryGetValue(key, out ClipLibrary clipLibrary))
			{
				clip = clipLibrary.GetClip();
			}
			
			if (clip != null)
			{
				return true;
			}
			
			return false;
		}
		
		private uint GetMaxConcurrent(string key)
		{
			if (m_ConcurrentMaxesDictionary.TryGetValue(key, out uint count))
			{
				return count;
			}

			return MaxConcurrent;
		}

		private bool IsAtMaxConcurrent(string clipName)
		{
			if (m_ConcurrentCountDictionary.TryGetValue(clipName, out uint currentCount))
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
			if (m_MinimumTimeSinceLastPlayDictionary.TryGetValue(key, out float timeBetweenPlays))
			{
				if (m_LastPlayedDictionary.TryGetValue(key, out float lastPlayed))
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
			m_LastPlayedDictionary[clipName] = Time.realtimeSinceStartup;

			if (m_SfxCategoryToGroup.TryGetValue(category, out AudioMixerGroup group))
			{
				sfxComp.PlaySfx(group, clip, clipName, sourceObject, volume, loop, delay, null, category, pitch);
			}
		}

		internal void IncrementClipCount(SfxComponent comp)
		{
			if (m_ConcurrentCountDictionary.ContainsKey(comp.SfxName))
			{
				m_ConcurrentCountDictionary[comp.SfxName]++;
			}
			else
			{
				m_ConcurrentCountDictionary[comp.SfxName] = 1;
			}

			m_ActiveSfxDictionary[new Tuple<string, int>(comp.SfxName, comp.RequestingObjHash)] = comp;
			if (m_ActiveSfxByCategory.ContainsKey(comp.Category))
			{
				m_ActiveSfxByCategory[comp.Category].Add(comp);
			}
			else
			{
				m_ActiveSfxByCategory[comp.Category] = new List<SfxComponent>(){comp};
			}
		}
		
		internal void DecrementClipCount(SfxComponent comp)
		{
			if (m_ConcurrentCountDictionary[comp.SfxName] > 0)
			{
				m_ConcurrentCountDictionary[comp.SfxName]--;
			}

			m_ActiveSfxDictionary.Remove(new Tuple<string, int>(comp.SfxName, comp.RequestingObjHash));
			m_ActiveSfxByCategory[comp.Category].Remove(comp);
		}
		
		private SfxComponent LeaseSfxComponent()
		{
			for (int i = m_SfxPool.Count - 1; i >= 0; i--)
			{
				if (!m_SfxPool[i].IsBusy)
				{
					m_SfxPool[i].SetBusy(true);
					m_SfxPool[i].gameObject.SetActive(true);

					return m_SfxPool[i];
				}
			}

			SfxComponent newComp = Instantiate(m_SfxCompTemplate, m_SfxPoolTransform);

			newComp.Source.volume = 1f;
			newComp.Source.spatialize = false;
			newComp.Source.spatialBlend = 0;
			newComp.SetBusy(true);
			m_SfxPool.Add(newComp);

			return newComp;
		}
		
		internal void ReturnSfxObject(SfxComponent comp)
		{
			Transform sourceTransform = comp.transform;
			sourceTransform.parent = m_SfxPoolTransform;
			sourceTransform.position = m_SfxPoolTransform.position;
			comp.SetBusy(false);
		}
		
		internal void RemoveSfxCompOnDestroy(SfxComponent comp)
		{
			m_SfxPool.Remove(comp);
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
				if (entry.Reference == null || !entry.Reference.RuntimeKeyIsValid())
				{
					continue;
				}
				if (string.IsNullOrEmpty(entry.Key))
				{
					continue;
				}
				var loadedClip = AddressableManager.Instance.LoadAssetReference<AudioClip>(entry.Reference);
			
				if (m_AudioClipLoadRefCounts.ContainsKey(entry.Reference))
				{
					m_AudioClipLoadRefCounts[entry.Reference]++;
				}
				else
				{
					m_AudioClipLoadRefCounts[entry.Reference] = 1;
				}
		
				m_ConcurrentMaxesDictionary[entry.Key] =
					entry.MaxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.MaxSimultaneous;
				m_MinimumTimeSinceLastPlayDictionary[entry.Key] = entry.MinTimeBetweenPlays;
		
				loadedClip.LoadAudioData();
		
				if (!m_ClipsListDictionary.TryGetValue(entry.Key, out ClipLibrary clipLibrary))
				{
					clipLibrary = new();
					m_ClipsListDictionary[entry.Key] = clipLibrary;
				}
				clipLibrary.AddClip(entry.Weight, loadedClip);
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
				if (entry.Reference == null || !entry.Reference.RuntimeKeyIsValid())
				{
					continue;
				}
				if (string.IsNullOrEmpty(entry.Key))
				{
					continue;
				}
				loadTasks.Add(AddressableManager.Instance.LoadAssetReferenceAsync<AudioClip>(entry.Reference));
			}
			await Task.WhenAll(loadTasks);

			for (int i = 0; i < loadTasks.Count; i++)
			{
				Task<AudioClip> task = loadTasks[i];
				AudioClipConfigEntry entry = config.Entries[i];
				if (!m_AudioClipLoadRefCounts.TryAdd(entry.Reference, 1))
				{
					m_AudioClipLoadRefCounts[entry.Reference]++;
				}

				m_ConcurrentMaxesDictionary[entry.Key] =
					entry.MaxSimultaneous <= 0 ? PerSfxMaxConcurrent : entry.MaxSimultaneous;
				m_MinimumTimeSinceLastPlayDictionary[entry.Key] = entry.MinTimeBetweenPlays;
			
				task.Result.LoadAudioData();
			
				if (!m_ClipsListDictionary.TryGetValue(entry.Key, out ClipLibrary clipLibrary))
				{
					clipLibrary = new();
					m_ClipsListDictionary[entry.Key] = clipLibrary;
				}
				clipLibrary.AddClip(entry.Weight, task.Result);
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
				if (m_ClipsListDictionary.TryGetValue(entry.Key, out ClipLibrary clipLibrary))
				{
					if (clipLibrary != null)
					{
						if (entry.Reference != null)
						{
							if (m_AudioClipLoadRefCounts.ContainsKey(entry.Reference))
							{
								m_AudioClipLoadRefCounts[entry.Reference]--;
								if (m_AudioClipLoadRefCounts[entry.Reference] <= 0)
								{
									if (AddressableManager.Instance.TryGetIfLoaded(entry.Reference, out AudioClip loadedClip))
									{
										loadedClip.UnloadAudioData();
									}
									m_AudioClipLoadRefCounts.Remove(entry.Reference);
									AddressableManager.Instance.ReleaseAssetReference(entry.Reference);
								}
							}
							else
							{
								AddressableManager.Instance.ReleaseAssetReference(entry.Reference);
							}
						}

						if (clipLibrary.Count <= 0)
						{
							m_ClipsListDictionary.Remove(entry.Key);
						}
					}
				}
			}
		}
		
		public void SetMasterVolume(float newVolume) 
		{
			float adjustedVolume = m_IsMuted ? 0 : Mathf.Clamp(newVolume, VolumeZeroEquivalent, 1);
			MasterMixer.SetFloat(MasterVolumeName, Mathf.Log(adjustedVolume) * VolumeLogScalar);
			AudioSettings.SetMasterVolume(newVolume);
		}

		public float GetCategoryVolume(int category)
		{
			return AudioSettings.GetCategoryVolume(category);
		}
		
		public void SetSfxVolume(int category, float newVolume)
		{
			float adjustedVolume = m_IsSfxMuted ? 0 : Mathf.Clamp(newVolume, VolumeZeroEquivalent, MaxSfxVolume);
			
			AudioSettings.SetCategoryVolume(category, newVolume);
			SfxCategorySettings data = AudioSettings.GetCategoryData(category);
			if (data != null)
			{
				MasterMixer.SetFloat(data.TargetGroupName, Mathf.Log(adjustedVolume) * VolumeLogScalar);
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
				if(m_MusicDictionary.TryGetValue(priority, out MusicTrackComponent targetComponent))
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
					if (m_PlayingTrack == null || !m_PlayingTrack.IsBusy)
					{
						targetComponent.PlayTrack(m_MusicGroup, clip, clipName, crossFadeTime / 2, vol, loop, onEndCallback, true, startOffset);
						m_PlayingTrack = targetComponent;
					}
					else if(priority >= m_PlayingTrack.Priority)
					{
						targetComponent.SetTrackData(m_MusicGroup, clip, clipName, vol, loop, onEndCallback, startOffset);
						
						m_PlayingTrack.FadeOut(crossFadeTime / 2, () =>
						{
							m_PlayingTrack.SuspendTrack();
							ResumeNextPriority(crossFadeTime / 2);
						});
					}
					else // set data, do not transition to track
					{
						targetComponent.SetTrackData(m_MusicGroup, clip, clipName, vol, loop, onEndCallback, startOffset);
					}
				}
			}
		}
		
		public void SuspendAllMusic()
		{
			m_PlayingTrack = null;
			foreach (KeyValuePair<int,MusicTrackComponent> pair in m_MusicDictionary)
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
			
			for (int i = AudioSettings.MusicCategories.Count - 1; i >= 0; i--)
			{
				if (m_MusicDictionary.TryGetValue(AudioSettings.MusicCategories[i].Priority, out MusicTrackComponent track))
				{
					//If a valid track is already playing then there is no need to resume
					if (track.IsPlaying())
					{
						return;
					}
					
					if (track.IsWaitingOnPriority)
					{
						track.Unpause(fadeTime != 0 ? fadeTime : track.FadeInTime);
						m_PlayingTrack = track;
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
			foreach (KeyValuePair<int,MusicTrackComponent> pair in m_MusicDictionary)
			{
				MusicTrackComponent track = pair.Value;
				if (track.TrackName == trackName)
				{
					if (track == m_PlayingTrack)
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
			if (m_MusicDictionary.TryGetValue(priority, out MusicTrackComponent track))
			{
				track.StopTrack();
				if (m_PlayingTrack == track)
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
			foreach (KeyValuePair<int,MusicTrackComponent> pair in m_MusicDictionary)
			{
				pair.Value.StopTrack();
			}
		}

		public void SetTrackPosition(string trackName, float position)
		{
			foreach (MusicTrackComponent trackComponent in m_MusicDictionary.Values)
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
			if (m_MusicDictionary.TryGetValue(trackType, out MusicTrackComponent track))
			{
				track.SetPosition(position);
			}
		}

		public void SetPlayingTrackPosition(float position)
		{
			m_PlayingTrack.SetPosition(position);
		}
#endregion music
	}
}