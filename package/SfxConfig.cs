using GrygToolsUtils;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	[Serializable]
	public class SfxConfig 
	{
		[SerializeField]
		private string m_SfxName = string.Empty;
		public string SfxName => m_SfxName;

		[SerializeField]
		[SfxCategory]
		private int m_SfxCategory = 1;
		public int SfxCategory => m_SfxCategory;

		[SerializeField]
		[Range(0f, 1f)]
		private float m_SfxVolume = 1f;
		public float SfxVolume => m_SfxVolume;
		
		[SerializeField]
		[Min(0)]
		private float m_SfxDelay = 0;
		public float SfxDelay => m_SfxDelay;
		
		[SerializeField]
		private bool m_Looping = false;
		public bool Looping => m_Looping;
		
		[SerializeField]
		private bool m_ForcePlay = false;
		public bool ForcePlay => m_ForcePlay;
		
		[SerializeField]
		[MinMaxRange(0.5f, 1.5f)]
		private Vector2 m_PitchRandomization = new Vector2(1, 1);
		public Vector2 PitchRandomization => m_PitchRandomization;

		public bool IsSet()
		{
			return !string.IsNullOrEmpty(m_SfxName);
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
			m_SfxName = source.m_SfxName;
			m_SfxCategory = source.m_SfxCategory;
			m_SfxVolume = source.m_SfxVolume;
			m_SfxDelay = source.m_SfxDelay;
			m_Looping = source.m_Looping;
			m_ForcePlay = source.m_ForcePlay;
			m_PitchRandomization = source.m_PitchRandomization;
			m_PitchRandomization = source.m_PitchRandomization;
		}
	}
}