using System;
using UnityEngine;
using UnityEngine.Serialization;
namespace GrygTools.Audio
{
	[Serializable]
	public class AudioTagToTextureCollisionSfx
	{
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag1;
		
		[SerializeField]
		public Texture[] Textures;
		
		[SerializeField]
		public PhysicsMaterial[] PhysicsMaterials;
		
		[FormerlySerializedAs("SfxKey")]
		[SerializeField]
		public SfxConfig SfxConfig;
	}
}
