using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	[Serializable]
	public class MusicConfig
	{
		[SerializeField]
		[Tooltip("Name of track to be played. A name and a clip cannot both be set")]
		private string m_TrackName = string.Empty;
		public string TrackName => m_TrackName;
		
		[SerializeField]
		[Tooltip("Volume to play track at, 1 is normal volume. Is still affected by SoundManager master and music volume")]
		[Range(0f, 1f)]
		private float m_TrackVolume = 1f;
		public float TrackVolume => m_TrackVolume;
		
		[SerializeField]
		[Tooltip("If true track will loop until stopped.")]
		private bool m_Looping = false;
		public bool Looping => m_Looping;
		
		[SerializeField]
		[Tooltip("Time in seconds to fade out the old track and fade in this track")]
		private float m_CrossFadeTime = 0;
		public float CrossFadeTime => m_CrossFadeTime;

		[SerializeField]
		[Tooltip("Priority at which to play the track, higher values play over lower values")]
		private int m_Priority = 0;
		public int Priority => m_Priority;

		[SerializeField]
		[Tooltip("Offset for starting the track, 30 would mean clip starts at 30 seconds in")]
		[Min(0)]
		private float m_StartOffset = 0f;
		public float StartOffset => m_StartOffset;

		public bool IsSet()
		{
			return !string.IsNullOrEmpty(m_TrackName);
		}
	}
}
