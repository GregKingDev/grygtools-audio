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
		private LayerMask m_CastLayers = ~0;

		[SerializeField]
		private Transform m_CastOrigin;
		
		[SerializeField]
		private Vector3 m_CastOffset = new Vector3(0f, 0.5f, 0f);
		
		[SerializeField]
		private float m_CastDistance = 2f;
		
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
		RaycastHit[] m_RayHits = new RaycastHit[1];

		private void Awake()
		{
			BuildLookup();
		}

		private void Reset()
		{
			if(m_CastOrigin == null)
			{
				m_CastOrigin = transform;
			}
		}

		public void PlaySfx(string eventName)
		{
			if (eventName != m_EventName)
			{
				return;
			}

			if (m_PhysicsMaterialLookup.Count > 0 || m_TextureLookup.Count > 0 || m_TagLookup.Count > 0)
			{
				if (Physics.RaycastNonAlloc(m_CastOrigin.position + m_CastOffset, Vector3.down, m_RayHits, m_CastDistance, m_CastLayers) > 0
				    && m_RayHits[0].collider != null)
				{
					SfxConfig sfxConfig;
					if (m_RayHits[0].collider.TryGetComponent(out Terrain terrain))
					{
						var texture = InternalUtils.GetDominantTerrainTexture(m_RayHits[0].point, terrain);
						Log($"Dominant Terrain Texture: {(texture != null ? texture.name : "null")}", texture);

						if (m_TextureLookup.TryGetValue(texture, out sfxConfig) && sfxConfig.IsSet())
						{
							AudioController.Instance.PlaySfx(sfxConfig, gameObject);
							return;
						}
					}

					Log($"Physics Material: {(m_RayHits[0].collider.sharedMaterial == null ? "null" : m_RayHits[0].collider.sharedMaterial.name)}", m_RayHits[0].collider.sharedMaterial);
					if (m_RayHits[0].collider.sharedMaterial != null && m_PhysicsMaterialLookup.TryGetValue(m_RayHits[0].collider.sharedMaterial, out sfxConfig) && sfxConfig.IsSet())
					{
						AudioController.Instance.PlaySfx(sfxConfig, gameObject);
						return;
					}

					Log($"Tag: {(m_RayHits[0].collider == null ? "null" : m_RayHits[0].collider.tag)}", m_RayHits[0].collider);
					if (!string.IsNullOrEmpty(m_RayHits[0].collider.tag) && m_TagLookup.TryGetValue(m_RayHits[0].collider.tag, out sfxConfig) && sfxConfig.IsSet())
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
		
		[Conditional("FOOTSTEP_SFX_DEBUG")]
		private void Log(string message, UnityEngine.Object context = null)
		{
				Debug.Log($"FOOTSTEP SFX: {message}", context);
		}
	}
}
