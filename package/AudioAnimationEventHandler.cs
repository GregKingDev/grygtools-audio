using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	[Serializable]
	public class AnimationSfx
	{
		[SerializeField]
		private string m_EventName;
		public string EventName => m_EventName;
		
		[SerializeField]
		private SfxConfig m_SfxConfig;
		public SfxConfig SfxConfig => m_SfxConfig;
	}
	
	public class AudioAnimationEventHandler : MonoBehaviour
	{
		[SerializeField]
		private List<AnimationSfx> m_SfxConfigs = new List<AnimationSfx>();
		public List<AnimationSfx> SfxConfigs => m_SfxConfigs;
		
		private Dictionary<string, SfxConfig> m_SfxConfigLookup = new Dictionary<string, SfxConfig>();

		private void Awake()
		{
			BuildLookup();
		}

		public void PlayAudioClip(string eventName)
		{
			AudioController.Instance.PlaySfx(m_SfxConfigLookup[eventName], gameObject);
		}
		
		private void BuildLookup()
		{
			m_SfxConfigLookup.Clear();
			foreach (AnimationSfx animationSfx in m_SfxConfigs)
			{
				if (animationSfx.SfxConfig != null && !string.IsNullOrEmpty(animationSfx.EventName))
				{
					m_SfxConfigLookup[animationSfx.EventName] = animationSfx.SfxConfig;
				}
			}
		}

		private void OnValidate()
		{
			BuildLookup();
		}
	}
}
