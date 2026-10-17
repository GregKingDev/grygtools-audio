using System;
using UnityEngine;
using UnityEngine.Serialization;
namespace GrygTools.Audio
{
	[Serializable]
	public class AudioTagToTexturePhysMatCollisionSfx
	{
		[FormerlySerializedAs("AudioCollisionTag1")]
		[SerializeField]
		[AudioCollisionTag]
		public AudioCollisionTag AudioCollisionTag;
		
		[SerializeField]
		public TexturesAndMaterialsToSfx[] TexturesAndMaterialsToSfx;
	}
	
	[Serializable]
	public class TexturesAndMaterialsToSfx
	{
		public Texture[] Textures;
		public PhysicsMaterial[] PhysicsMaterials;
		public SfxConfig SfxConfig;
	}
}
