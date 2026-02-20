using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrygTools.Audio
{
	public class OnStartAudioConfigLoader : MonoBehaviour
	{
		[SerializeField]
		private List<AudioClipConfig> audioConfigs;

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(1f);
			foreach (AudioClipConfig config in audioConfigs)
			{
				AudioController.Instance.LoadAudioConfig(config);
			}
		}
	}
}