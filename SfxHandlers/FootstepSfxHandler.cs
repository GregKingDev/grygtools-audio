using GrygTools.Utils.Attributes;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GrygTools.Audio.SfxHandlers
{
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
		private List<TexturesToSfx> m_TextureToSfxList = new List<TexturesToSfx>();
		[SerializeField]
		private List<PhysicMaterialsToSfx> m_PhysicsMaterialToSfxList = new List<PhysicMaterialsToSfx>();
		[SerializeField]
		private List<TagsToSfx> m_TagToSfxList = new List<TagsToSfx>();
		
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
					if (m_RayHits[0].collider.sharedMaterial != null 
					    && m_PhysicsMaterialLookup.TryGetValue(m_RayHits[0].collider.sharedMaterial, out sfxConfig) 
					    && sfxConfig.IsSet())
					{
						AudioController.Instance.PlaySfx(sfxConfig, gameObject);
						return;
					}

					Log($"Tag: {(m_RayHits[0].collider == null ? "null" : m_RayHits[0].collider.tag)}", m_RayHits[0].collider);
					if (!string.IsNullOrEmpty(m_RayHits[0].collider.tag) 
					    && m_TagLookup.TryGetValue(m_RayHits[0].collider.tag, out sfxConfig) 
					    && sfxConfig.IsSet())
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
			foreach (PhysicMaterialsToSfx materialToSfx in m_PhysicsMaterialToSfxList)
			{
				foreach (PhysicsMaterial material in materialToSfx.PhysicMaterials)
				{
					if (!m_PhysicsMaterialLookup.TryAdd(material, materialToSfx.SfxConfig))
					{
						Debug.LogWarning($"Duplicate physics material: {material}", this);
					}
				}
			}
			
			m_TextureLookup.Clear();
			foreach (TexturesToSfx textureToSfx in m_TextureToSfxList)
			{
				foreach (Texture texture in textureToSfx.Textures)
				{
					if(!m_TextureLookup.TryAdd(texture, textureToSfx.SfxConfig))
					{
						Debug.LogWarning($"Duplicate texture: {texture}", this);
					}
				}
			}
			
			m_TagLookup.Clear();
			foreach (TagsToSfx tagToSfx in m_TagToSfxList)
			{
				foreach (var tagEntry in tagToSfx.Tags)
				{
					if(!string.IsNullOrEmpty(tagEntry))
					{
						if(!m_TagLookup.TryAdd(tagEntry, tagToSfx.SfxConfig))
						{
							Debug.LogWarning($"Duplicate tag: {tagEntry}", this);
						}
					}
				}
			}
		}

		private void OnValidate()
		{
			BuildLookup();
		}
		
		[Conditional("FOOTSTEP_SFX_DEBUG")]
		private void Log(string message, Object context = null)
		{
				Debug.Log($"FOOTSTEP SFX: {message}", context);
		}
	}
}
