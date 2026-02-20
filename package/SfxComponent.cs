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
		private AudioSource source = null;
		public AudioSource Source => source;
		
		private string sfxName = string.Empty;
		public string SfxName => sfxName;
		
		private int requestingObjHash = 0;
		public int RequestingObjHash => requestingObjHash;
		
		private float sfxDelayTimer = 0f;
		private float sfxTimer = 0f;
		private SfxState state = SfxState.Idle;
		
		private int category = 1;
		public int Category => category;
		
		private Action callback;

		private bool isBusy = false;
		public bool IsBusy => isBusy;
		
		private void Awake()
		{
			if (source == null)
			{
				if (!TryGetComponent(out source))
				{
					source = gameObject.AddComponent<AudioSource>();
				}
			}
		}
		
		internal void SetBusy(bool busy)
		{
			isBusy = busy;
		}
		
		internal void PlaySfx(AudioMixerGroup sfxGroup, AudioClip clip, string clipName, GameObject requestingObj, float vol,
			bool looping, float delay, Action cb, int category, float pitch = 1f)
		{
			sfxName = clipName;
			if (requestingObj != null)
			{
				requestingObjHash = requestingObj.GetHashCode();
				transform.parent = requestingObj.transform;
				source.loop = looping;
			}
			else
			{
				requestingObjHash = 0;
				source.loop = false;
			}
			
			source.clip = clip;
			this.category = category;
			source.volume = vol;
			source.outputAudioMixerGroup = sfxGroup;
			source.pitch = pitch;
			callback = cb;
			sfxDelayTimer = delay;
			sfxTimer = 0f;
			
			if (sfxDelayTimer <= 0)
			{
				InternalPlaySfx();
			}
			else
			{
				state = SfxState.Waiting;
			}
			
			AudioController.Instance.IncrementClipCount(this);
		}
		
		private void InternalPlaySfx()
		{
			state = SfxState.Playing;
			source.Play();
			sfxTimer = source.clip.length;
		}
		
		private void Update()
		{
			if (isBusy)
			{
				if (state == SfxState.Waiting)
				{
					sfxDelayTimer -= Time.unscaledDeltaTime;
					if (sfxDelayTimer <= 0)
					{
						InternalPlaySfx();
					}	
				}
				else if (state == SfxState.Playing)
				{
					sfxTimer -= Time.unscaledDeltaTime;
					if (sfxTimer <= 0)
					{
						if (source.loop)
						{
							sfxTimer = source.clip.length + sfxTimer;
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
			state = SfxState.Idle;
			AudioController.Instance.ReturnSfxObject(this);
			callback?.Invoke();
		}

		internal void StopSfx()
		{
			source.Stop();
			AudioController.Instance.DecrementClipCount(this);
			AudioController.Instance.ReturnSfxObject(this);
			state = SfxState.Idle;
		}
		
		public void Pause()
		{
			if (state is SfxState.Playing or SfxState.Waiting)
			{
				state = SfxState.Paused;
				source.Pause();
			}
		}

		public void Unpause()
		{
			if (state == SfxState.Paused)
			{
				if (sfxDelayTimer > 0f)
				{
					state = SfxState.Waiting;
				}
				else if (sfxTimer > 0)
				{
					state = SfxState.Playing;
					source.UnPause();
				}
			}
		}
		
		private void OnDestroy()
		{
			source.Stop();
			state = SfxState.Destroyed;
			AudioController.Instance.RemoveSfxCompOnDestroy(this);
			if (isBusy)
			{
				AudioController.Instance.DecrementClipCount(this);
			}
		}
	}
}