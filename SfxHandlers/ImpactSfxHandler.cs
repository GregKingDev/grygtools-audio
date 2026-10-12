using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GrygTools.Audio.SfxHandlers
{
	[Serializable]
	public class ImpactSfxConfig
	{
		[MinAndMaxRangeVec2(0f, 10000)]
		public Vector2 ImpulseRange = new(0, 10000);
		public float MinVelocity;
		public float MaxVelocity;
		public SfxConfig SfxConfig;
		
		public float ImpulseLowerBoundSqr => ImpulseRange.x * ImpulseRange.x;
		public float ImpulseUpperBoundSqr => ImpulseRange.y * ImpulseRange.y;
	}
	
	[RequireComponent(typeof(Collider))]
	public class ImpactSfxHandler : MonoBehaviour
	{
		[Header("Sfx checks are done in order of PhysicMaterial, Texture, Tag. If no match is found the default Sfx will be played.")]
		[SerializeField]
		private PhysicMaterialsToSfx[] m_PhysicMaterialSfx;
		private readonly Dictionary<PhysicsMaterial, SfxConfig> m_PhysicMaterialSfxLookup = new Dictionary<PhysicsMaterial, SfxConfig>();

		[SerializeField]
		private TexturesToSfx[] m_TextureSfx;
		private readonly Dictionary<Texture, SfxConfig> m_TextureSfxLookup = new Dictionary<Texture, SfxConfig>();

		[SerializeField]
		private TagsToSfx[] m_TagSfx;
		private readonly Dictionary<string, SfxConfig> m_TagSfxLookup = new Dictionary<string, SfxConfig>();

		[SerializeField]
		private ImpactSfxConfig m_WaterImpactSfx;
		private bool m_InWater;

		[SerializeField]
		private SfxConfig m_DefaultImpactSfx;
		
		[SerializeField, Tooltip("Collider used to determine speed in entering water"), ReadOnly]
		private Collider m_Collider;

		private void Awake()
		{
			BuildLookup();
		}

		private void OnTriggerEnter(Collider other)
		{
			// The water layer mask is configured in the grygtools audio settings
			if (!m_InWater && ((1 << other.gameObject.layer) & GrygAudioSettings.WaterLayerMask) != 0)
			{
				Vector3 impulse = m_Collider.attachedRigidbody.mass * m_Collider.attachedRigidbody.linearVelocity;
				Log($"Entering water with impulse: {impulse}. Thresholds: {m_WaterImpactSfx.ImpulseLowerBoundSqr} " +
					$"- {m_WaterImpactSfx.ImpulseUpperBoundSqr}", gameObject);
				if (m_WaterImpactSfx.ImpulseLowerBoundSqr < impulse.sqrMagnitude &&
				    m_WaterImpactSfx.ImpulseUpperBoundSqr > impulse.sqrMagnitude)
				{
					AudioController.Instance.PlaySfx(m_WaterImpactSfx.SfxConfig, gameObject);
					Debug.Log("Collision with water detected, ignoring impact sound.");
					m_InWater = true;
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (m_InWater && ((1 << other.gameObject.layer) & GrygAudioSettings.WaterLayerMask) != 0)
			{
				m_InWater = false;
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			SfxConfig sfxConfig;
			
			if (collision.collider.TryGetComponent(out Terrain terrain))
			{
				var texture = InternalUtils.GetDominantTerrainTexture(collision.contacts[0].point, terrain);
				Log($"Dominant Terrain Texture: {(texture != null ? texture.name : "null")}", texture);

				if (m_TextureSfxLookup.TryGetValue(texture, out sfxConfig) && sfxConfig.IsSet())
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}

			if (collision.collider.sharedMaterial != null)
			{
				Log($"Shared Material: {collision.collider.sharedMaterial.name}", collision.collider.sharedMaterial);
				if (m_PhysicMaterialSfxLookup.TryGetValue(collision.collider.sharedMaterial, out sfxConfig)
				    && sfxConfig.IsSet())
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}

			if (!string.IsNullOrEmpty(collision.gameObject.tag))
			{
				Log($"Tag: {collision.gameObject.tag}", collision.gameObject);
				if (m_TagSfxLookup.TryGetValue(collision.gameObject.tag, out sfxConfig) && sfxConfig.IsSet())
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}

			if (m_DefaultImpactSfx.IsSet())
			{
				AudioController.Instance.PlaySfx(m_DefaultImpactSfx, gameObject);
			}
		}

		private void BuildLookup()
		{
			m_PhysicMaterialSfxLookup.Clear();
			m_TextureSfxLookup.Clear();
			m_TagSfxLookup.Clear();

			foreach (PhysicMaterialsToSfx sfx in m_PhysicMaterialSfx)
			{
				foreach (PhysicsMaterial physicMaterial in sfx.PhysicMaterials)
				{
					if (!m_PhysicMaterialSfxLookup.TryAdd(physicMaterial, sfx.SfxConfig))
					{
						Debug.LogWarning($"Failed to add physics material {physicMaterial} to lookup dictionary. Duplicate Key.");
					}
				}
			}

			foreach (TexturesToSfx sfx in m_TextureSfx)
			{
				foreach (Texture texture in sfx.Textures)
				{
					if (!m_TextureSfxLookup.TryAdd(texture, sfx.SfxConfig))
					{
						Debug.LogWarning($"Failed to add texture {texture} to lookup dictionary. Duplicate Key.");
					}
				}
			}

			foreach (TagsToSfx sfx in m_TagSfx)
			{
				foreach (string tagName in sfx.Tags)
				{
					if (!m_TagSfxLookup.TryAdd(tagName, sfx.SfxConfig))
					{
						Debug.LogWarning($"Failed to add tag {tagName} to lookup dictionary. Duplicate Key.");
					}
				}
			}
		}

		private void OnValidate()
		{
			BuildLookup();
			m_Collider = GetComponent<Collider>();
		}

		private void Reset()
		{
			m_Collider = GetComponent<Collider>();
			if(GrygAudioSettings.WaterLayerMask == 0)
			{
				Debug.LogWarning("Water Layer Mask is not set in Gryg Audio Settings. Please set it to the appropriate layer for water objects.");
			}
		}

		[Conditional("IMPACT_SFX_DEBUG")]
		private void Log(string message, Object context = null)
		{
			Debug.Log($"IMPACT SFX: {message}", context);
		}
	}
}