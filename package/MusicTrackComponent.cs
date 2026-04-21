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
		
		private AudioSource m_Source = null;
		public AudioSource Source => m_Source;
		
		private int m_Priority = 1;
		public int Priority => m_Priority;
		
		private string m_TrackName = string.Empty;
		public string TrackName => m_TrackName;
		
		private bool m_IsBusy = false;
		public bool IsBusy => m_IsBusy;
		
		private AudioController.SfxEndCallback m_Callback;
		private AudioController.SfxEndCallback m_FadeoutCallback;
		
		private float m_TrackTimer = 0f;
		private float m_FadeInTimer = 0f;
		private float m_FadeOutTimer = 0f;
		private float m_FadeInTime = 0f;
		public float FadeInTime => m_FadeInTime;

		private float m_FadeOutTime = 0f;
		public float FadeOutTime => m_FadeOutTime;

		private float m_TargetVolume = 1f;
		private MusicState m_State = MusicState.Idle;
		public MusicState State => m_State;
		private bool m_Looping = false;
		private bool m_ResumeNextOnEnd = false;
		
		private AudioController m_AudioController;
		
		internal bool IsWaitingOnPriority => m_State == MusicState.WaitingOnPriority;
		
		private void Awake()
		{
			if (m_Source == null)
			{
				if (!TryGetComponent(out m_Source))
				{
					m_Source = gameObject.AddComponent<AudioSource>();
				}
			}
			m_AudioController = AudioController.Instance;
		}
		
		internal MusicTrackComponent Init(MusicPriorityCategory priorityCategory)
		{
			this.m_Priority = priorityCategory.Priority;
			return this;
		}
		
		internal void SetBusy(bool busy)
		{
			m_IsBusy = busy;
		}
		
		internal void PlayTrack(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, 
			float fadeInlength, float vol, bool loop, AudioController.SfxEndCallback cb, bool resumeNextOnEnd, float offset)
		{
			offset = Mathf.Clamp(offset, 0, clip.length);

			m_TrackName = clipName;
			m_Source.clip = clip;
			m_TargetVolume = vol;
			m_Source.volume = vol;
			m_Source.loop = loop;
			m_Looping = loop;
			m_Source.outputAudioMixerGroup = sfxGroup;
			m_Callback = cb;
			this.m_ResumeNextOnEnd = resumeNextOnEnd;
			m_TrackTimer = clip.length - offset;
			
			if (m_TrackTimer < m_FadeInTime)
			{
				m_FadeInTimer = m_FadeInTime = m_TrackTimer;
			}
			else
			{
				m_FadeInTimer = m_FadeInTime = fadeInlength;
			}
			
			if (m_FadeInTimer > 0f)
			{
				m_State = MusicState.FadingIn;
			}
			else
			{
				m_State = MusicState.Playing;
			}

			m_Source.time = offset;
			m_IsBusy = true;
			m_Source.Play();
		}
		
		internal void SetTrackData(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, 
			float vol, bool loop, AudioController.SfxEndCallback callback, float offset)
		{
			offset = Mathf.Clamp(offset, 0, clip.length);
			
			m_State = MusicState.WaitingOnPriority;
			m_IsBusy = true;
			m_TrackName = clipName;
			m_Source.clip = clip;
			m_TargetVolume = vol;
			m_Source.volume = vol;
			m_Source.loop = loop;
			m_Looping = loop;
			m_Source.time = offset;
			this.m_Callback = callback;
			m_Source.outputAudioMixerGroup = sfxGroup;
			m_TrackTimer = clip.length - offset;
		}
		
		internal void SetPosition(float timePosition)
		{
			if (m_Source.clip != null && m_Source.clip.length > timePosition)
			{
				timePosition = Mathf.Clamp(timePosition, 0, m_Source.clip.length);
				m_Source.time = timePosition;
				m_TrackTimer = m_Source.clip.length - timePosition;
			}
		}
		
		internal void FadeOut(float fadeTime, AudioController.SfxEndCallback fadeCallback)
		{
			if (m_Source != null && m_Source.isPlaying)
			{
				m_FadeOutTime = fadeTime < m_TrackTimer  ? fadeTime : m_TrackTimer;
				m_FadeOutTimer = m_FadeOutTime;
				m_FadeoutCallback = fadeCallback;
				m_State = MusicState.FadingOut;
			}
			else
			{
				fadeCallback?.Invoke();
			}
		}
		
		private void Update()
		{
			if (m_IsBusy && m_State != MusicState.Idle)
			{
				if (m_State == MusicState.Playing)
				{
					m_TrackTimer -= Time.unscaledDeltaTime;
					if (m_TrackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (m_TrackTimer <= m_FadeOutTime && m_FadeOutTime < 0f)
					{
						m_State = MusicState.FadingOut;
					}
				}
				else if (m_State == MusicState.FadingIn)
				{
					m_TrackTimer -= Time.unscaledDeltaTime;
					m_FadeInTimer -= Time.unscaledDeltaTime;
					m_Source.volume = Mathf.Clamp((m_FadeInTime - m_FadeInTimer) / m_FadeInTime * m_TargetVolume, 0, m_TargetVolume);
					if (m_TrackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (m_FadeInTimer <= 0f)
					{
						m_State = MusicState.Playing;
					}
				}
				else if (m_State == MusicState.FadingOut)
				{
					m_TrackTimer -= Time.unscaledDeltaTime;
					m_FadeOutTimer -= Time.unscaledDeltaTime;
					m_Source.volume = Mathf.Clamp(m_FadeOutTimer / m_FadeOutTime * m_TargetVolume, m_TargetVolume, 1);
					if (m_TrackTimer <= 0)
					{
						OnFinishedPlaying();
					}
					else if (m_FadeOutTimer <= 0f)
					{
						SuspendTrack();
						m_FadeoutCallback?.Invoke();
					}
				}
			}
		}
		
		private void OnFinishedPlaying()
		{
			if (m_Looping)
			{
				m_FadeInTime = m_FadeOutTime = m_FadeOutTimer = m_FadeInTimer = 0;
				m_TrackTimer = m_Source.clip.length;
			}
			else
			{
				m_State = MusicState.Idle;
				if (m_ResumeNextOnEnd)
				{
					m_AudioController.ResumeNextPriority(m_FadeOutTime);					
				}
				
				m_IsBusy = false;
			}
			m_Callback?.Invoke();
		}

		internal void SuspendTrack()
		{
			m_Source.Pause();
			m_State = MusicState.WaitingOnPriority;
		}
		
		internal void StopTrack()
		{
			if (IsPlaying() || m_State == MusicState.WaitingOnPriority)
			{
				m_Source.Stop();
				m_TrackName = string.Empty;
			}

			m_IsBusy = false;
			m_State = MusicState.Idle;
		}

		internal bool IsPlaying()
		{
			return m_IsBusy && ((MusicState.FadingIn | MusicState.FadingOut | MusicState.Playing) & m_State) != 0;
		}

		internal bool IsFadingOut()
		{
			return m_State == MusicState.FadingOut;
		}
		
		internal void Unpause(float fade = 0f)
		{
			if (m_State == MusicState.Paused || m_State == MusicState.WaitingOnPriority)
			{
				if (fade > 0)
				{
					m_FadeInTime = m_FadeInTimer = fade < m_TrackTimer ? fade : m_TrackTimer;
					m_State = MusicState.FadingIn;
				}
				else if (m_FadeInTimer > 0)
				{
					m_State = MusicState.FadingIn;
				}
				else if (m_FadeOutTimer > 0)
				{
					m_State = MusicState.FadingOut;
				}
				else
				{
					m_State = MusicState.Playing;
					m_Source.volume = m_TargetVolume;
				}
				
				if (m_TrackTimer > 0)
				{
					if (!m_Source.isPlaying)
					{
						m_Source.Play();
					}
					m_Source.UnPause();
				}
			}
		}

		internal void AudioConfigurationChanged()
		{
			if ((m_State == MusicState.Playing || m_State == MusicState.FadingIn) 
			    && !m_Source.isPlaying)
			{
				m_Source.time = m_Source.clip.length - m_TrackTimer;
				m_Source.Play();
			}
		}
	}
}
