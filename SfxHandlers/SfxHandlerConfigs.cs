using GrygTools.Utils.Attributes;
using UnityEngine;
namespace GrygTools.Audio.SfxHandlers
{
	[System.Serializable]
	public class PhysicsMaterialsToSfx
	{
		public PhysicsMaterial[] PhysicsMaterials;
		public SfxConfig SfxConfig;
	}

	[System.Serializable]
	public struct TexturesToSfx
	{
		public Texture[] Textures;
		public SfxConfig SfxConfig;
	}

	[System.Serializable]
	public struct TagsToSfx
	{
		[Tag]
		public string[] Tags;
		public SfxConfig SfxConfig;
	}
}
