using System;
using UnityEngine;

namespace GrygTools.Audio
{
	[Serializable]
	public class MusicConfig
	{
		[SerializeField]
		[Tooltip("Name of track to be played. A name and a clip cannot both be set")]
		private string trackName = string.Empty;
		public string TrackName => trackName;
		
		[SerializeField]
		[Tooltip("Volume to play track at, 1 is normal volume. Is still affected by SoundManager master and music volume")]
		[Range(0f, 1f)]
		private float trackVolume = 1f;
		public float TrackVolume => trackVolume;
		
		[SerializeField]
		[Tooltip("If true track will loop until stopped.")]
		private bool looping = false;
		public bool Looping => looping;
		
		[SerializeField]
		[Tooltip("Time in seconds to fade out the old track and fade in this track")]
		private float crossFadeTime = 0;
		public float CrossFadeTime => crossFadeTime;

		[SerializeField]
		[Tooltip("Priority at which to play the track, higher values play over lower values")]
		private int priority = 0;
		public int Priority => priority;

		[SerializeField]
		[Tooltip("Offset for starting the track, 30 would mean clip starts at 30 seconds in")]
		[Min(0)]
		private float startOffset = 0f;
		public float StartOffset => startOffset;

		public bool IsSet()
		{
			return !string.IsNullOrEmpty(trackName);
		}
	}
}
