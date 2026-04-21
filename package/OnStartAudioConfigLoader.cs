using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	public class OnStartAudioConfigLoader : MonoBehaviour
	{
		[SerializeField]
		private bool m_UseAsync = false;
		[SerializeField]
		private bool m_UnloadOnDestroy = false;
		[SerializeField]
		private List<AudioClipConfig> m_AudioConfigs;

		private async void Start()
		{
			if (m_UseAsync)
			{
				await AudioController.Instance.LoadAudioConfigAsync(m_AudioConfigs);
			}
			else
			{
				AudioController.Instance.LoadAudioConfig(m_AudioConfigs);
			}
		}

		private void OnDestroy()
		{
			if (m_UnloadOnDestroy)
			{
				foreach (AudioClipConfig config in m_AudioConfigs)
				{
					AudioController.Instance.UnloadAudioConfig(config);
				}
			}
		}
	}
}