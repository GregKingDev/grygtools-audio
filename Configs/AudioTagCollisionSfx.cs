using System;
using UnityEngine;
using UnityEngine.Serialization;
namespace GrygTools.Audio
{
	[Serializable]
	public class AudioTagCollisionSfx
	{
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag1;
		
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag2;
		
		[FormerlySerializedAs("SfxKey")]
		[SerializeField]
		public SfxConfig SfxConfig;
		
		public override string ToString()
		{
			return $"{AudioCollisionTag1} - {AudioCollisionTag2} -> {SfxConfig.SfxName}";
		}
	}
}

