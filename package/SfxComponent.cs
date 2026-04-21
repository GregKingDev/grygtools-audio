using System;
using UnityEngine;
using UnityEngine.Audio;

namespace GrygTools.Audio
{
	public class SfxComponent : MonoBehaviour
	{
		private enum SfxState
		{
			Idle = 1,
			Waiting = 2,
			Playing = 3,
			Paused = 4,
			Destroyed = 5
		}
		private AudioSource m_Source = null;
		public AudioSource Source => m_Source;
		
		private string m_SfxName = string.Empty;
		public string SfxName => m_SfxName;
		
		private int m_RequestingObjHash = 0;
		public int RequestingObjHash => m_RequestingObjHash;
		
		private float m_SfxDelayTimer = 0f;
		private float m_SfxTimer = 0f;
		private SfxState m_State = SfxState.Idle;
		
		private int m_Category = 1;
		public int Category => m_Category;
		
		private Action m_Callback;

		private bool m_IsBusy = false;
		public bool IsBusy => m_IsBusy;
		
		private void Awake()
		{
			if (m_Source == null)
			{
				if (!TryGetComponent(out m_Source))
				{
					m_Source = gameObject.AddComponent<AudioSource>();
				}
			}
		}
		
		internal void SetBusy(bool busy)
		{
			m_IsBusy = busy;
		}
		
		internal void PlaySfx(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, GameObject requestingObj, float vol,
			bool looping, float delay, Action cb, int category, float pitch = 1f)
		{
			m_SfxName = clipName;
			if (requestingObj != null)
			{
				m_RequestingObjHash = requestingObj.GetHashCode();
				transform.parent = requestingObj.transform;
				m_Source.loop = looping;
			}
			else
			{
				m_RequestingObjHash = 0;
				m_Source.loop = false;
			}
			
			m_Source.clip = clip;
			this.m_Category = category;
			m_Source.volume = vol;
			m_Source.outputAudioMixerGroup = sfxGroup;
			m_Source.pitch = pitch;
			m_Callback = cb;
			m_SfxDelayTimer = delay;
			m_SfxTimer = 0f;
			
			if (m_SfxDelayTimer <= 0)
			{
				InternalPlaySfx();
			}
			else
			{
				m_State = SfxState.Waiting;
			}
			
			AudioController.Instance.IncrementClipCount(this);
		}
		
		private void InternalPlaySfx()
		{
			m_State = SfxState.Playing;
			m_Source.Play();
			m_SfxTimer = m_Source.clip.length;
		}
		
		private void Update()
		{
			if (m_IsBusy)
			{
				if (m_State == SfxState.Waiting)
				{
					m_SfxDelayTimer -= Time.unscaledDeltaTime;
					if (m_SfxDelayTimer <= 0)
					{
						InternalPlaySfx();
					}	
				}
				else if (m_State == SfxState.Playing)
				{
					m_SfxTimer -= Time.unscaledDeltaTime;
					if (m_SfxTimer <= 0)
					{
						if (m_Source.loop)
						{
							m_SfxTimer = m_Source.clip.length + m_SfxTimer;
						}
						else
						{
							OnFinishedPlaying();	
						}
					}
				}
			}
		}
		
		private void OnFinishedPlaying()
		{
			AudioController.Instance.DecrementClipCount(this);
			m_State = SfxState.Idle;
			AudioController.Instance.ReturnSfxObject(this);
			m_Callback?.Invoke();
		}

		internal void StopSfx()
		{
			m_Source.Stop();
			AudioController.Instance.DecrementClipCount(this);
			AudioController.Instance.ReturnSfxObject(this);
			m_State = SfxState.Idle;
		}
		
		public void Pause()
		{
			if (m_State is SfxState.Playing or SfxState.Waiting)
			{
				m_State = SfxState.Paused;
				m_Source.Pause();
			}
		}

		public void Unpause()
		{
			if (m_State == SfxState.Paused)
			{
				if (m_SfxDelayTimer > 0f)
				{
					m_State = SfxState.Waiting;
				}
				else if (m_SfxTimer > 0)
				{
					m_State = SfxState.Playing;
					m_Source.UnPause();
				}
			}
		}
		
		private void OnDestroy()
		{
			m_Source.Stop();
			m_State = SfxState.Destroyed;
			AudioController.Instance.RemoveSfxCompOnDestroy(this);
			if (m_IsBusy)
			{
				AudioController.Instance.DecrementClipCount(this);
			}
		}
	}
}