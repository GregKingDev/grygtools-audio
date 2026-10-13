using System;
using UnityEngine;
namespace GrygTools.Audio
{
	[Serializable]
	public class AudioCollisionSfx
	{
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag1;
		
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag2;
		
		[SerializeField]
		[SfxKeyProperty]
		public string SfxKey;
		
		public override string ToString()
		{
			return $"{AudioCollisionTag1} - {AudioCollisionTag2} -> {SfxKey}";
		}
	}
}

