using GrygTools.Utils.Attributes;
using UnityEngine;
namespace GrygTools.Audio.SfxHandlers
{
	[System.Serializable]
	public struct PhysicMaterialsToSfx
	{
		public PhysicsMaterial[] PhysicMaterials;
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
