using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
namespace GrygTools.Audio
{
	public class OnStartMusicPlayer : MonoBehaviour
	{
		[SerializeField]
		private MusicConfig m_MusicConfig;

		private async void Start()
		{
			await UniTask.WaitForEndOfFrame();
			AudioController.Instance.PlayTrack(m_MusicConfig);
		}
	}
}
