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
	public class ImpactRange
	{
		[MinAndMaxRangeVec2(0f, 10000)]
		private Vector2 m_ImpulseRange = new Vector2(0, 10000);
		public Vector2 ImpulseRange { get => m_ImpulseRange;  internal set => m_ImpulseRange = value; }
		public float ImpulseLowerBoundSqr => m_ImpulseRange.x * m_ImpulseRange.x;
		public float ImpulseUpperBoundSqr => m_ImpulseRange.y * m_ImpulseRange.y;
		
		[Header("Sfx checks are done in order of PhysicMaterial, Texture, Tag. If no match is found the default Sfx will be played.")]
		[SerializeField]
		internal PhysicsMaterialsToSfx[] m_PhysicMaterialSfx;
		public readonly Dictionary<PhysicsMaterial, SfxConfig> PhysicsMaterialSfxLookup = new Dictionary<PhysicsMaterial, SfxConfig>();

		[SerializeField]
		internal TexturesToSfx[] m_TextureSfx;
		public readonly Dictionary<Texture, SfxConfig> TextureSfxLookup = new Dictionary<Texture, SfxConfig>();

		[SerializeField]
		internal TagsToSfx[] m_TagSfx;
		public readonly Dictionary<string, SfxConfig> TagSfxLookup = new Dictionary<string, SfxConfig>();
		
		[SerializeField]
		public SfxConfig m_WaterImpactSfx;
		
		[SerializeField]
		public SfxConfig m_DefaultImpactSfx;
	}
	
	public enum UnderWaterImpactMode
	{
		IgnoreImpactSfx,
		PlayUnderWaterImpactSfx,
		UseTerrestrialSfx
	}
	
	[RequireComponent(typeof(Collider))]
	public class ImpactSfxHandler : MonoBehaviour
	{
		[SerializeField]
		private ImpactRange[] m_ImpactRanges;

		[SerializeField]
		private UnderWaterImpactMode m_UnderWaterImpactMode;
		
		private bool m_InWater;
		
		[SerializeField, ConditionalSerializedField(nameof(m_UnderWaterImpactMode), UnderWaterImpactMode.PlayUnderWaterImpactSfx)]
		private ImpactRange[] m_UnderWaterImpactRanges;
		
		[SerializeField, Tooltip("Collider used to determine speed in entering water"), ReadOnly]
		private Collider m_Collider;

		private void Awake()
		{
			RangeValidation();
		}

		private bool MeetsRange(float value, Vector2 range)
		{
			return value >= range.x && value <= range.y;
		}
		

		private void OnTriggerEnter(Collider other)
		{
			// The water layer mask is configured in the grygtools audio settings
			if (!m_InWater && ((1 << other.gameObject.layer) & GrygAudioSettings.WaterLayerMask) != 0)
			{
				if(m_Collider == null || m_Collider.attachedRigidbody == null)
				{
					return;
				}
				
				Vector3 impulse = m_Collider.attachedRigidbody.mass * m_Collider.attachedRigidbody.linearVelocity;
				Log($"Water: Entering water with impulse sqr: {impulse.sqrMagnitude}.", gameObject);
				for (int i = 0; i < m_ImpactRanges.Length; i++)
				{
					if(MeetsRange(impulse.sqrMagnitude, m_ImpactRanges[i].ImpulseRange))
					{
						AudioController.Instance.PlaySfx(m_ImpactRanges[i].m_WaterImpactSfx, gameObject);
						Log($"Water: Collision between within range {m_ImpactRanges[i].ImpulseRange}. Playing SFX {m_ImpactRanges[i].m_WaterImpactSfx.SfxName}", gameObject);
						m_InWater = true;
						return;
					}
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

			if(m_InWater && m_UnderWaterImpactMode == UnderWaterImpactMode.IgnoreImpactSfx)
			{
				return;
			}

			ImpactRange[] ranges = m_InWater ? m_UnderWaterImpactRanges : m_ImpactRanges;
			
			for (int i = 0; i < ranges.Length; i++)
			{
				if (MeetsRange(collision.impulse.sqrMagnitude, ranges[i].ImpulseRange))
				{
					if (collision.collider.TryGetComponent(out Terrain terrain))
					{
						var texture = InternalUtils.GetDominantTerrainTexture(collision.contacts[0].point, terrain);
						Log($"Dominant Terrain Texture: {(texture != null ? texture.name : "null")}", texture);

						if (ranges[i].TextureSfxLookup.TryGetValue(texture, out sfxConfig) && sfxConfig.IsSet())
						{
							AudioController.Instance.PlaySfx(sfxConfig, gameObject);
							return;
						}
					}

					if (collision.collider.sharedMaterial != null)
					{
						Log($"Shared Material: {collision.collider.sharedMaterial.name}", collision.collider.sharedMaterial);
						if (ranges[i].PhysicsMaterialSfxLookup.TryGetValue(collision.collider.sharedMaterial, out sfxConfig)
						    && sfxConfig.IsSet())
						{
							AudioController.Instance.PlaySfx(sfxConfig, gameObject);
							return;
						}
					}

					if (!string.IsNullOrEmpty(collision.gameObject.tag))
					{
						Log($"Tag: {collision.gameObject.tag}", collision.gameObject);
						if (ranges[i].TagSfxLookup.TryGetValue(collision.gameObject.tag, out sfxConfig) && sfxConfig.IsSet())
						{
							AudioController.Instance.PlaySfx(sfxConfig, gameObject);
							return;
						}
					}

					if (ranges[i].m_DefaultImpactSfx.IsSet())
					{
						AudioController.Instance.PlaySfx(ranges[i].m_DefaultImpactSfx, gameObject);
					}
				}
			}
		}

		private void RangeValidation()
		{
			ValidateRanges(m_ImpactRanges);
			ValidateRanges(m_UnderWaterImpactRanges);
		}

		private void ValidateRanges(ImpactRange[] ranges)
		{
			foreach (ImpactRange impactRange in ranges)
			{
				impactRange.PhysicsMaterialSfxLookup.Clear();
				impactRange.TextureSfxLookup.Clear();
				impactRange.TagSfxLookup.Clear();

				
				foreach (PhysicsMaterialsToSfx sfx in impactRange.m_PhysicMaterialSfx)
				{
					foreach (PhysicsMaterial physicMaterial in sfx.PhysicsMaterials)
					{
						if (!impactRange.PhysicsMaterialSfxLookup.TryAdd(physicMaterial, sfx.SfxConfig))
						{
							Debug.LogWarning($"Failed to add physics material {physicMaterial} to lookup dictionary. Duplicate Key.");
						}
					}
				}

				foreach (TexturesToSfx sfx in impactRange.m_TextureSfx)
				{
					foreach (Texture texture in sfx.Textures)
					{
						if (!impactRange.TextureSfxLookup.TryAdd(texture, sfx.SfxConfig))
						{
							Debug.LogWarning($"Failed to add texture {texture} to lookup dictionary. Duplicate Key.");
						}
					}
				}

				foreach (TagsToSfx sfx in impactRange.m_TagSfx)
				{
					foreach (string tagName in sfx.Tags)
					{
						if (!impactRange.TagSfxLookup.TryAdd(tagName, sfx.SfxConfig))
						{
							Debug.LogWarning($"Failed to add tag {tagName} to lookup dictionary. Duplicate Key.");
						}
					}
				}
			}

			Array.Sort(m_ImpactRanges, (a, b) => a.ImpulseRange.x.CompareTo(b.ImpulseRange.x));
			for(int i = 0; i < m_ImpactRanges.Length; i++)
			{
				if(i > 0 && m_ImpactRanges[i].ImpulseRange.x < m_ImpactRanges[i - 1].ImpulseRange.y)	
				{
					Debug.LogWarning($"ImpactRange at index {i} has an impulse range that overlaps with the previous SFX. Adjusting. Please ensure that each SFX has a unique impulse range.");
					m_ImpactRanges[i].ImpulseRange = new Vector2(m_ImpactRanges[i - 1].ImpulseRange.y, m_ImpactRanges[i].ImpulseRange.y);
				}
			}
		}

		private void OnValidate()
		{
			RangeValidation();
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