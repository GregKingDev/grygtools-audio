using GrygTools.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Audio
{
	[Serializable]
	public class AnimationSfx
	{
		[SerializeField]
		public string eventName;
		[SerializeField]
		public SfxConfig sfxConfig;
	}
	
	public class AudioAnimationEventHandler : MonoBehaviour
	{
		[SerializeField]
		List<AnimationSfx> sfxConfigs = new List<AnimationSfx>();
		
		private Dictionary<string, SfxConfig> sfxConfigLookup = new Dictionary<string, SfxConfig>();

		private void Awake()
		{
			BuildLookup();
		}

		public void PlayAudioClip(string eventName)
		{
			AudioController.Instance.PlaySfx(sfxConfigLookup[eventName], gameObject);
		}
		
		private void BuildLookup()
		{
			sfxConfigLookup.Clear();
			foreach (AnimationSfx animationSfx in sfxConfigs)
			{
				if (animationSfx.sfxConfig != null && !string.IsNullOrEmpty(animationSfx.eventName))
				{
					sfxConfigLookup[animationSfx.eventName] = animationSfx.sfxConfig;
				}
			}
		}

		private void OnValidate()
		{
			BuildLookup();
		}
	}
}
