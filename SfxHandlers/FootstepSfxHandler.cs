using GrygTools.Utils.Attributes;
using GrygTools.Utils.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
		private AudioTagCollisionResponseData[] m_TagCollisionSfx;
		
		[SerializeField]
		private SfxConfig m_DefaultFootstepSfx;
		
		[SerializeField]
		private AudioTagCollisionConfig m_AudioConfigOverride;
		
		private readonly Dictionary<AudioCollisionTag, SfxConfig> m_TagToSfxLookup = new Dictionary<AudioCollisionTag, SfxConfig>();
		private readonly RaycastHit[] m_RayHits = new RaycastHit[1];

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
			if (eventName != m_EventName || m_TagToSfxLookup.Count == 0)
			{
				return;
			}
			
			AudioTagCollisionConfig tagConfig = m_AudioConfigOverride != null ? 
				m_AudioConfigOverride : AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig;
			
			if (Physics.RaycastNonAlloc(m_CastOrigin.position + m_CastOffset, Vector3.down, m_RayHits, m_CastDistance, m_CastLayers) > 0
			    && m_RayHits[0].collider != null)
			{
				SfxConfig sfxConfig;
				if (m_RayHits[0].collider.TryGetComponent(out CollisionSfxHandler collisionSfxHandler)
				    && m_TagToSfxLookup.TryGetValue(collisionSfxHandler.AudioCollisionTag, out sfxConfig))
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
				
				if (m_RayHits[0].collider.TryGetComponent(out Terrain terrain))
				{
					var texture = terrain.GetDominantTerrainTexture(m_RayHits[0].point);
					Log($"Dominant Terrain Texture: {(texture != null ? texture.name : "null")}", texture);
					if (AudioController.Instance.TryGetAudioTagForTexture(texture, out AudioCollisionTag textureTag, tagConfig)
					    && m_TagToSfxLookup.TryGetValue(textureTag, out sfxConfig))
					{
						AudioController.Instance.PlaySfx(sfxConfig, gameObject);
						return;
					}
				}

				Log($"Physics Material: {(m_RayHits[0].collider.sharedMaterial == null ? "null" : m_RayHits[0].collider.sharedMaterial.name)}", m_RayHits[0].collider.sharedMaterial);
				if (m_RayHits[0].collider.sharedMaterial != null 
				    && AudioController.Instance.TryGetAudioTagForPhysicsMaterial(m_RayHits[0].collider.sharedMaterial,
					    out AudioCollisionTag physMatTag, tagConfig) && m_TagToSfxLookup.TryGetValue(physMatTag, out sfxConfig))
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}

				Log($"Tag: {(m_RayHits[0].collider == null ? "null" : m_RayHits[0].collider.tag)}", m_RayHits[0].collider);
				if (!string.IsNullOrEmpty(m_RayHits[0].collider.tag) 
				    && AudioController.Instance.TryGetAudioTagForUnityTag(m_RayHits[0].collider.tag, out AudioCollisionTag audioTag, tagConfig)
				    && m_TagToSfxLookup.TryGetValue(audioTag, out sfxConfig))
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}
			
			if (m_DefaultFootstepSfx != null && m_DefaultFootstepSfx.IsSet())
			{
				AudioController.Instance.PlaySfx(m_DefaultFootstepSfx, gameObject);
			}
		}
		
		private void BuildLookup()
		{
			m_TagToSfxLookup.Clear();
			foreach (var tagCollisionSfx in m_TagCollisionSfx)
			{
				if(tagCollisionSfx == null) continue;
				if(tagCollisionSfx.SfxConfig == null) continue;

				foreach (AudioCollisionTag otherTag in tagCollisionSfx.OtherTags)
				{
					if(!m_TagToSfxLookup.TryAdd(otherTag, tagCollisionSfx.SfxConfig))
					{
						Debug.LogWarning($"Duplicate tag: {otherTag}", this);
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
