using System;
using UnityEngine;
using UnityEngine.Audio;
namespace GrygTools.Audio
{
	public class MusicTrackComponent : MonoBehaviour
	{
		[Flags]
		public enum MusicState
		{
			Idle = 1,
			FadingIn = 2,
			Playing = 4,
			Paused = 8,
			FadingOut =	16,
			WaitingOnPriority = 32
		}
		
		private AudioSource source = null;
		public AudioSource Source => source;
		
		private int priority = 1;
		public int Priority => priority;
		
		private string trackName = string.Empty;
		public string TrackName => trackName;
		
		private bool isBusy = false;
		public bool IsBusy => isBusy;
		
		private AudioController.SfxEndCallback callback;
		private AudioController.SfxEndCallback fadeoutCallback;
		
		private float trackTimer = 0f;
		private float fadeInTimer = 0f;
		private float fadeOutTimer = 0f;
		private float fadeInTime = 0f;
		public float FadeInTime => fadeInTime;

		private float fadeOutTime = 0f;
		public float FadeOutTime => fadeOutTime;

		private float targetVolume = 1f;
		private MusicState state = MusicState.Idle;
		public MusicState State => state;
		private bool looping = false;
		private bool resumeNextOnEnd = false;
		
		private AudioController sm;
		
		internal bool IsWaitingOnPriority => state == MusicState.WaitingOnPriority;
		
		private void Awake()
		{
			if (source == null)
			{
				if (!TryGetComponent(out source))
				{
					source = gameObject.AddComponent<AudioSource>();
				}
			}
			sm = AudioController.Instance;
		}
		
		internal MusicTrackComponent Init(MusicPriorityCategory priorityCategory)
		{
			this.priority = priorityCategory.priority;
			return this;
		}
		
		internal void SetBusy(bool busy)
		{
			isBusy = busy;
		}
		
		internal void PlayTrack(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, 
			float fadeInlength, float vol, bool loop, AudioController.SfxEndCallback cb, bool resumeNextOnEnd, float offset)
		{
			offset = Mathf.Clamp(offset, 0, clip.length);

			trackName = clipName;
			source.clip = clip;
			targetVolume = vol;
			source.volume = vol;
			source.loop = loop;
			looping = loop;
			source.outputAudioMixerGroup = sfxGroup;
			callback = cb;
			this.resumeNextOnEnd = resumeNextOnEnd;
			trackTimer = clip.length - offset;
			
			if (trackTimer < fadeInTime)
			{
				fadeInTimer = fadeInTime = trackTimer;
			}
			else
			{
				fadeInTimer = fadeInTime = fadeInlength;
			}
			
			if (fadeInTimer > 0f)
			{
				state = MusicState.FadingIn;
			}
			else
			{
				state = MusicState.Playing;
			}

			source.time = offset;
			isBusy = true;
			source.Play();
		}
		
		internal void SetTrackData(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, 
			float vol, bool loop, AudioController.SfxEndCallback callback, float offset)
		{
			offset = Mathf.Clamp(offset, 0, clip.length);
			
			state = MusicState.WaitingOnPriority;
			isBusy = true;
			trackName = clipName;
			source.clip = clip;
			targetVolume = vol;
			source.volume = vol;
			source.loop = loop;
			looping = loop;
			source.time = offset;
			this.callback = callback;
			source.outputAudioMixerGroup = sfxGroup;
			trackTimer = clip.length - offset;
		}
		
		internal void SetPosition(float timePosition)
		{
			if (source.clip != null && source.clip.length > timePosition)
			{
				timePosition = Mathf.Clamp(timePosition, 0, source.clip.length);
				source.time = timePosition;
				trackTimer = source.clip.length - timePosition;
			}
		}
		
		internal void FadeOut(float fadeTime, AudioController.SfxEndCallback fadeCallback)
		{
			if (source != null && source.isPlaying)
			{
				fadeOutTime = fadeTime < trackTimer  ? fadeTime : trackTimer;
				fadeOutTimer = fadeOutTime;
				fadeoutCallback = fadeCallback;
				state = MusicState.FadingOut;
			}
			else
			{
				fadeCallback?.Invoke();
			}
		}
		
		private void Update()
		{
			if (isBusy && state != MusicState.Idle)
			{
				if (state == MusicState.Playing)
				{
					trackTimer -= Time.unscaledDeltaTime;
					if (trackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (trackTimer <= fadeOutTime && fadeOutTime < 0f)
					{
						state = MusicState.FadingOut;
					}
				}
				else if (state == MusicState.FadingIn)
				{
					trackTimer -= Time.unscaledDeltaTime;
					fadeInTimer -= Time.unscaledDeltaTime;
					source.volume = Mathf.Clamp((fadeInTime - fadeInTimer) / fadeInTime * targetVolume, 0, targetVolume);
					if (trackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (fadeInTimer <= 0f)
					{
						state = MusicState.Playing;
					}
				}
				else if (state == MusicState.FadingOut)
				{
					trackTimer -= Time.unscaledDeltaTime;
					fadeOutTimer -= Time.unscaledDeltaTime;
					source.volume = Mathf.Clamp(fadeOutTimer / fadeOutTime * targetVolume, targetVolume, 1);
					if (trackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (fadeOutTimer <= 0f)
					{
						SuspendTrack();
						fadeoutCallback?.Invoke();
					}
				}
			}
		}
		
		private void OnFinishedPlaying()
		{
			if (looping)
			{
				fadeInTime = fadeOutTime = fadeOutTimer = fadeInTimer = 0;
				trackTimer = source.clip.length;
			}
			else
			{
				state = MusicState.Idle;
				if (resumeNextOnEnd)
				{
					sm.ResumeNextPriority(fadeOutTime);					
				}
				
				isBusy = false;
			}
			callback?.Invoke();
		}

		internal void SuspendTrack()
		{
			source.Pause();
			state = MusicState.WaitingOnPriority;
		}
		
		internal void StopTrack()
		{
			if (IsPlaying() || state == MusicState.WaitingOnPriority)
			{
				source.Stop();
				trackName = string.Empty;
			}

			isBusy = false;
			state = MusicState.Idle;
		}

		internal bool IsPlaying()
		{
			return isBusy && ((MusicState.FadingIn | MusicState.FadingOut | MusicState.Playing) & state) != 0;
		}

		internal bool IsFadingOut()
		{
			return state == MusicState.FadingOut;
		}
		
		internal void Unpause(float fade = 0f)
		{
			if (state == MusicState.Paused || state == MusicState.WaitingOnPriority)
			{
				if (fade > 0)
				{
					fadeInTime = fadeInTimer = fade < trackTimer ? fade : trackTimer;
					state = MusicState.FadingIn;
				}
				else if (fadeInTimer > 0)
				{
					state = MusicState.FadingIn;
				}
				else if (fadeOutTimer > 0)
				{
					state = MusicState.FadingOut;
				}
				else
				{
					state = MusicState.Playing;
					source.volume = targetVolume;
				}
				
				if (trackTimer > 0)
				{
					if (!source.isPlaying)
					{
						source.Play();
					}
					source.UnPause();
				}
			}
		}

		internal void AudioConfigurationChanged()
		{
			if ((state == MusicState.Playing || state == MusicState.FadingIn) 
			    && !source.isPlaying)
			{
				source.time = source.clip.length - trackTimer;
				source.Play();
			}
		}
	}
}
