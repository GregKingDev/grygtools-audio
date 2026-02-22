using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GrygTools.Audio
{
	public class OnStartAudioConfigLoader : MonoBehaviour
	{
		[SerializeField]
		private bool useAsync = false;
		[SerializeField]
		private List<AudioClipConfig> audioConfigs;

		private async void Start()
		{
			List<Task> tasks = new();
			foreach (AudioClipConfig config in audioConfigs)
			{
				if (useAsync)
				{
					tasks.Add(AudioController.Instance.LoadAudioConfigAsync(config));
				}
				else
				{
					AudioController.Instance.LoadAudioConfig(config);
				}
				
			}
			if (useAsync)
			{
				await Task.WhenAll(tasks);
			}
		}
	}
}