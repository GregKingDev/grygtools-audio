using System.Collections.Generic;
using UnityEngine;

namespace GrygTools.Audio
{
	public class OnStartAudioConfigLoader : MonoBehaviour
	{
		[SerializeField]
		private bool useAsync = false;
		[SerializeField]
		private bool unloadOnDestroy = false;
		[SerializeField]
		private List<AudioClipConfig> audioConfigs;

		private async void Start()
		{
			if (useAsync)
			{
				await AudioController.Instance.LoadAudioConfigAsync(audioConfigs);
			}
			else
			{
				AudioController.Instance.LoadAudioConfig(audioConfigs);
			}
		}

		private void OnDestroy()
		{
			if (unloadOnDestroy)
			{
				foreach (AudioClipConfig config in audioConfigs)
				{
					AudioController.Instance.UnloadAudioConfig(config);
				}
			}
		}
	}
}