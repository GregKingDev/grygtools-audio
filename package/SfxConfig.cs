using System;
using UnityEngine;

namespace GrygTools.Audio
{
	[Serializable]
	public class SfxConfig 
	{
		[SerializeField]
		private string sfxName = string.Empty;
		public string SfxName => sfxName;

		[SerializeField]
		[SfxCategory]
		private int sfxCategory = 1;
		public int SfxCategory => sfxCategory;

		[SerializeField]
		[Range(0f, 1f)]
		private float sfxVolume = 1f;
		public float SfxVolume => sfxVolume;
		
		[SerializeField]
		[Min(0)]
		private float sfxDelay = 0;
		public float SfxDelay => sfxDelay;
		
		[SerializeField]
		private bool looping = false;
		public bool Looping => looping;
		
		[SerializeField]
		private bool forcePlay = false;
		public bool ForcePlay => forcePlay;
		
		[SerializeField]
		[MinMaxRange(0.5f, 1.5f)]
		private Vector2 pitchRandomization = new Vector2(1, 1);
		public Vector2 PitchRandomization => pitchRandomization;

		public bool IsSet()
		{
			return !string.IsNullOrEmpty(sfxName);
		}

		public void PlaySfx(GameObject sourceObject)
		{
			AudioController.Instance.PlaySfx(this, sourceObject);
		}

		public void ForcePlaySfx()
		{
			AudioController.Instance.PlaySfx(this, null);
		}

		public SfxConfig()
		{
		}

		public SfxConfig(SfxConfig source)
		{
			sfxName = source.sfxName;
			sfxCategory = source.sfxCategory;
			sfxVolume = source.sfxVolume;
			sfxDelay = source.sfxDelay;
			looping = source.looping;
			forcePlay = source.forcePlay;
			pitchRandomization = source.pitchRandomization;
			pitchRandomization = source.pitchRandomization;
		}
	}
}