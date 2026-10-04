using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace GrygTools.Audio
{
	[Serializable]
	public class TextureToFootstepSfx
	{
		[SerializeField]
		private Texture m_Texture;
		public Texture Texture => m_Texture;
		
		[SerializeField]
		private SfxConfig m_SfxConfig;
		public SfxConfig SfxConfig => m_SfxConfig;
	}
	
	[Serializable]
	public class PhysicsMaterialToFootstepSfx
	{
		[SerializeField]
		private PhysicsMaterial m_PhysicsMaterial;
		public PhysicsMaterial PhysicsMaterial => m_PhysicsMaterial;
		
		[SerializeField]
		private SfxConfig m_SfxConfig;
		public SfxConfig SfxConfig => m_SfxConfig;
		private readonly Dictionary<string, SfxConfig> m_SfxConfigLookup = new Dictionary<string, SfxConfig>();
	}
	
	[Serializable]
	public class TagToFootstepSfx
	{
		[SerializeField, Tag]
		private string m_Tag;
		public string Tag => m_Tag;
		
		[SerializeField]
		private SfxConfig m_SfxConfig;
		public SfxConfig SfxConfig => m_SfxConfig;
		private readonly Dictionary<string, SfxConfig> m_SfxConfigLookup = new Dictionary<string, SfxConfig>();
	}
	
	public class FootstepSfxHandler : MonoBehaviour
	{
		[Header("Function must be PlaySfx and string parameter of event must match The set EventName")]
		[ReadOnly, TextArea(3, 8)]
		public string m_DevNotes = "This class has multiple ways to determine which footstep sound to play.\n" +
			"First a terrain texture is checked for, if found the splat map is used to determine the dominant texture and looks for a corresponding sfx for the texture.\n" +
			"If no terrain texture is found, the physics material of the collider is checked for a corresponding sfx.\n" +
			"If no physics material is found, the tag of the collider is checked for a corresponding sfx.\n" + 
			"If neither a matching texture nor a matching PhysicsMaterial is found, it will play the m_DefaultFootstepSfx if it is set.";
		
		[SerializeField]
		private string m_EventName = "Footstep";
		
		[SerializeField] 
		private LayerMask m_TerrainLayer;
		
		[SerializeField]
		private List<TextureToFootstepSfx> m_TextureToSfxList = new List<TextureToFootstepSfx>();
		[SerializeField]
		private List<PhysicsMaterialToFootstepSfx> m_PhysicsMaterialToSfxList = new List<PhysicsMaterialToFootstepSfx>();
		[SerializeField]
		private List<TagToFootstepSfx> m_TagToSfxList = new List<TagToFootstepSfx>();
		[SerializeField]
		private SfxConfig m_DefaultFootstepSfx;
		
		private readonly Dictionary<PhysicsMaterial, SfxConfig> m_PhysicsMaterialLookup = new Dictionary<PhysicsMaterial, SfxConfig>();
		private readonly Dictionary<Texture, SfxConfig> m_TextureLookup = new Dictionary<Texture, SfxConfig>();
		private readonly Dictionary<string, SfxConfig> m_TagLookup = new Dictionary<string, SfxConfig>();

		private void Awake()
		{
			BuildLookup();
		}

		public void PlaySfx(string eventName)
		{
			if (eventName != m_EventName)
			{
				return;
			}

			if (m_PhysicsMaterialLookup.Count > 0 || m_TextureLookup.Count > 0 || m_TagLookup.Count > 0)
			{
				if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f, m_TerrainLayer)
				    && hit.collider != null)
				{
					SfxConfig sfxConfig;
					if (hit.collider.TryGetComponent(out Terrain terrain))
					{
						var texture = GetDominantTerrainTexture(hit, terrain);
						Log($"Dominant Terrain Texture: {(texture != null ? texture.name : "null")}", texture);

						if (m_TextureLookup.TryGetValue(texture, out sfxConfig) && sfxConfig.IsSet())
						{
							AudioController.Instance.PlaySfx(sfxConfig, gameObject);
							return;
						}
					}

					Log($"Physics Material: {(hit.collider.sharedMaterial == null ? "null" : hit.collider.sharedMaterial.name)}", hit.collider.sharedMaterial);
					if (hit.collider.sharedMaterial != null && m_PhysicsMaterialLookup.TryGetValue(hit.collider.sharedMaterial, out sfxConfig) && sfxConfig.IsSet())
					{
						AudioController.Instance.PlaySfx(sfxConfig, gameObject);
						return;
					}

					Log($"Tag: {(hit.collider == null ? "null" : hit.collider.tag)}", hit.collider);
					if (!string.IsNullOrEmpty(hit.collider.tag) && m_TagLookup.TryGetValue(hit.collider.tag, out sfxConfig) && sfxConfig.IsSet())
					{
						AudioController.Instance.PlaySfx(sfxConfig, gameObject);
						return;
					}
				}
			}
			if (m_DefaultFootstepSfx != null && m_DefaultFootstepSfx.IsSet())
			{
				AudioController.Instance.PlaySfx(m_DefaultFootstepSfx, gameObject);
			}
		}
		
		private Texture2D GetDominantTerrainTexture(RaycastHit hit, Terrain terrain)
		{
			if (terrain == null) return null;

			TerrainData tData = terrain.terrainData;
			Vector3 terrainPos = terrain.transform.position;

			float localX = hit.point.x - terrainPos.x;
			float localZ = hit.point.z - terrainPos.z;

			int mapX = Mathf.FloorToInt((localX / tData.size.x) * tData.alphamapWidth);
			int mapZ = Mathf.FloorToInt((localZ / tData.size.z) * tData.alphamapHeight);

			float[,,] alphaMaps = tData.GetAlphamaps(mapX, mapZ, 1, 1);

			int dominantLayer = 0;
			float maxWeight = 0f;

			for (int i = 0; i < alphaMaps.GetLength(2); i++)
			{
				if (alphaMaps[0, 0, i] > maxWeight)
				{
					maxWeight = alphaMaps[0, 0, i];
					dominantLayer = i;
				}
			}
			
			if (dominantLayer == -1) return null;

			if (tData.terrainLayers != null && dominantLayer < tData.terrainLayers.Length)
			{
				TerrainLayer currentLayer = tData.terrainLayers[dominantLayer];
        
				if (currentLayer != null)
				{
					return currentLayer.diffuseTexture;
				}
			}

			return null;
		}
		
		private void BuildLookup()
		{
			m_PhysicsMaterialLookup.Clear();
			foreach (PhysicsMaterialToFootstepSfx materialToSfx in m_PhysicsMaterialToSfxList)
			{
				if (materialToSfx.SfxConfig != null && materialToSfx.PhysicsMaterial != null)
				{
					m_PhysicsMaterialLookup[materialToSfx.PhysicsMaterial] = materialToSfx.SfxConfig;
				}
			}
			
			m_TextureLookup.Clear();
			foreach (TextureToFootstepSfx textureToSfx in m_TextureToSfxList)
			{
				if (textureToSfx.SfxConfig != null && textureToSfx.Texture != null)
				{
					m_TextureLookup[textureToSfx.Texture] = textureToSfx.SfxConfig;
				}
			}
			
			m_TagLookup.Clear();
			foreach (TagToFootstepSfx tagToSfx in m_TagToSfxList)
			{
				if (tagToSfx.SfxConfig != null && !string.IsNullOrEmpty(tagToSfx.Tag))
				{
					m_TagLookup[tagToSfx.Tag] = tagToSfx.SfxConfig;
				}
			}
		}

		private void OnValidate()
		{
			BuildLookup();
		}
		
		[Conditional("Footstep_Debug")]
		private void Log(string message, UnityEngine.Object context = null)
		{
				Debug.Log(message, context);
		}
	}
}
