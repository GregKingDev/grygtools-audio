using GrygTools.Utils.Attributes;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GrygTools.Audio.SfxHandlers
{
	// [Serializable]
	// public class CollisionRange
	// {
	// 	[MinAndMaxRangeVec2(0f, 10000)]
	// 	private Vector2 m_ImpulseRange = new Vector2(0, 10000);
	// 	public Vector2 ImpulseRange { get => m_ImpulseRange;  internal set => m_ImpulseRange = value; }
	// 	public float ImpulseLowerBoundSqr => m_ImpulseRange.x * m_ImpulseRange.x;
	// 	public float ImpulseUpperBoundSqr => m_ImpulseRange.y * m_ImpulseRange.y;
	// 	
	// 	[Header("Sfx checks are done in order of PhysicMaterial, Texture, Tag. If no match is found the default Sfx will be played.")]
	// 	[SerializeField]
	// 	internal PhysicsMaterialsToSfx[] m_PhysicMaterialSfx;
	// 	public readonly Dictionary<PhysicsMaterial, SfxConfig> PhysicsMaterialSfxLookup = new Dictionary<PhysicsMaterial, SfxConfig>();
	//
	// 	[SerializeField]
	// 	internal TexturesToSfx[] m_TextureSfx;
	// 	public readonly Dictionary<Texture, SfxConfig> TextureSfxLookup = new Dictionary<Texture, SfxConfig>();
	//
	// 	[SerializeField]
	// 	internal TagsToSfx[] m_TagSfx;
	// 	public readonly Dictionary<string, SfxConfig> TagSfxLookup = new Dictionary<string, SfxConfig>();
	// 	
	// 	[SerializeField]
	// 	public SfxConfig m_WaterCollisionSfx;
	// 	
	// 	[SerializeField]
	// 	public SfxConfig m_DefaultCollisionSfx;
	// }
	
	public enum UnderWaterSfxCollisionMode
	{
		IgnoreCollisionSfx,
		PlayUnderWaterCollisionSfx,
		UseTerrestrialSfx
	}
	
	[RequireComponent(typeof(Collider))]
	public class CollisionSfxHandler : MonoBehaviour
	{
		// [SerializeField, AudioCollisionTag]
		// private AudioCollisionTag m_AudioCollisionTag;
		//
		// [SerializeField]
		// private CollisionRange[] m_CollisionRanges;
		
		[SerializeField]
		private AudioCollisionTag m_AudioCollisionTag;

		[SerializeField]
		private UnderWaterSfxCollisionMode m_UnderWaterSfxCollisionMode;
		
		private bool m_InWater;
		
		// [SerializeField, ConditionalSerializedField(nameof(m_UnderWaterSfxCollisionMode), UnderWaterSfxCollisionMode.PlayUnderWaterCollisionSfx)]
		// private CollisionRange[] m_UnderWaterCollisionRanges;
		
		[SerializeField, Tooltip("Collider used to determine speed in entering water"), ReadOnly]
		private Collider m_Collider;

		private void Awake()
		{
			AutoSetCollider();
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
				m_InWater = true;
				if(m_Collider == null || m_Collider.attachedRigidbody == null)
				{
					return;
				}
				
				Vector3 impulse = m_Collider.attachedRigidbody.mass * m_Collider.attachedRigidbody.linearVelocity;
				Log($"Water: Entering water with impulse sqr: {impulse.sqrMagnitude}.", gameObject);
				if(AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig.TryGetSfxKeyForCollision(impulse.sqrMagnitude, m_AudioCollisionTag, GrygAudioSettings.WaterCollisionTag, out SfxConfig sfxConfig))
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
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

			if(m_InWater && m_UnderWaterSfxCollisionMode == UnderWaterSfxCollisionMode.IgnoreCollisionSfx)
			{
				return;
			}

			//TODO this needs to be expanded to handle PhysicMaterial and Texture based SFX as well, not just AudioCollisionTag. Possibly unity tag as well.
			if (collision.gameObject.TryGetComponent(out CollisionSfxHandler otherCollisionSfxHandler))
			{
				// Only the higher instance ID will play the SFX to avoid double playing
				if(gameObject.GetInstanceID() > otherCollisionSfxHandler.gameObject.GetInstanceID())
				{
					return;
				}
				PlaySfxForTagType(otherCollisionSfxHandler.m_AudioCollisionTag, collision.impulse.sqrMagnitude);
			}
		}

		public void PlaySfxForTagType(AudioCollisionTag otherAudioTag, float impulseSquared)
		{
			if (AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig.TryGetSfxKeyForCollision(impulseSquared, m_AudioCollisionTag,
				    otherAudioTag, out SfxConfig sfxConfig))
			{
				AudioController.Instance.PlaySfx(sfxConfig, gameObject);
			}
		}

		private void AutoSetCollider()
		{
			if (m_Collider == null)
			{
				m_Collider = GetComponent<Collider>();
			}
		}

		private void OnValidate()
		{
			AutoSetCollider();
		}

		private void Reset()
		{
			AutoSetCollider();
			if(GrygAudioSettings.WaterLayerMask == 0)
			{
				Debug.LogWarning("Water Layer Mask is not set in Gryg Audio Settings. Please set it to the appropriate layer for water objects.");
			}
		}

		[Conditional("COLLISION_SFX_DEBUG")]
		private void Log(string message, Object context = null)
		{
			Debug.Log($"COLLISION SFX: {message}", context);
		}
	}
}