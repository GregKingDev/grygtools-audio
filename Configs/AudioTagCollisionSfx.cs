using GrygTools.Utils.Attributes;
using System;
using UnityEngine;
using UnityEngine.Serialization;
namespace GrygTools.Audio
{
	[Serializable]
	internal class AudioTagCollisionSfx
	{
		[SerializeField, AudioCollisionTag]
		internal AudioCollisionTag AudioCollisionTag1;
		
		[SerializeField, AudioCollisionTag]
		internal AudioCollisionTag AudioCollisionTag2;
		
		[FormerlySerializedAs("SfxKey"), SerializeField]
		internal SfxConfig SfxConfig;
		
		public override string ToString()
		{
			return $"{AudioCollisionTag1} - {AudioCollisionTag2} -> {SfxConfig.SfxName}";
		}
	}
	
	[Serializable]
	internal class AudioTagCollisionSfxInfo
	{
		[SerializeField, AudioCollisionTag]
		internal AudioCollisionTag AudioTag;

		[FormerlySerializedAs("OtherTags")]
		[SerializeField]
		internal AudioTagCollisionResponseData[] TagToTagCollisionData;
		
		[Header("Map textures, physical materials and tags back to this Tag")]
		[SerializeField]
		internal Texture2D[] Textures;
		
		[SerializeField]
		internal PhysicsMaterial[] PhysicsMaterials;
		
		[FormerlySerializedAs("TagNames")]
		[SerializeField, Tag]
		internal string[] UnityTags;
		
		public override string ToString()
		{
			return $"{AudioTag}";
		}
	}

	[Serializable]
	internal class AudioTagCollisionResponseData
	{
		[SerializeField]
		[AudioCollisionTag]
		internal AudioCollisionTag[] OtherTags;
		
		[SerializeField]
		internal SfxConfig SfxConfig;
	}
	
	[Serializable]
	internal class TestingImpulseRangeCollisionSfx
	{
		[SerializeField,MinAndMaxRangeVec2(0f, 10000)]
		internal Vector2 m_ImpulseRange = new Vector2(0, 10000);
		[SerializeField]
		private AudioTagCollisionSfxInfo[] m_CollisionSfx;
		internal AudioTagCollisionSfxInfo[] CollisionSfx => m_CollisionSfx;
	}

	[Serializable]
	internal class AudioTagMappings
	{
		[Header("Map textures, physical materials and tags back to this Tag")]
		[SerializeField]
		internal AudioCollisionTag AudioTag;
		
		[SerializeField]
		internal Texture2D[] Textures;
		
		[SerializeField]
		internal PhysicsMaterial[] PhysicsMaterials;
		
		[FormerlySerializedAs("TagNames")]
		[SerializeField, Tag]
		internal string[] UnityTags;
	}
}

