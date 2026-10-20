using GrygTools.Utils.Attributes;
using GrygTools.Utils.Extensions;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GrygTools.Audio.SfxHandlers
{
	public enum UnderWaterSfxCollisionMode
	{
		IgnoreCollisionSfx,
		PlayUnderWaterCollisionSfx,
		UseTerrestrialSfx
	}
	
	[RequireComponent(typeof(Collider)), DisallowMultipleComponent]
	public class CollisionSfxHandler : MonoBehaviour
	{
		[SerializeField]
		private AudioCollisionTag m_AudioCollisionTag;
		public AudioCollisionTag AudioCollisionTag 
		{
			get
			{
				return m_AudioCollisionTag;
			}
			internal set
			{
				m_AudioCollisionTag = value;
			} 
		}

		[SerializeField]
		private UnderWaterSfxCollisionMode m_UnderWaterSfxCollisionMode;
		
		private bool m_InWater;
		
		[SerializeField, Tooltip("Collider used to determine speed in entering water"), ReadOnly]
		private Collider m_Collider;

		private void Awake()
		{
			AutoSetCollider();
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
				if(AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig.TryGetSfxKeyForTagTagCollision(impulse.sqrMagnitude, m_AudioCollisionTag, GrygAudioSettings.WaterCollisionTag, out SfxConfig sfxConfig))
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

			if (collision.gameObject.TryGetComponent(out CollisionSfxHandler otherCollisionSfxHandler))
			{
				// Only the higher instance ID will play the SFX to avoid double playing
				if(gameObject.GetInstanceID() > otherCollisionSfxHandler.gameObject.GetInstanceID())
				{
					return;
				}
				Log($"Tag to Tag collision {m_AudioCollisionTag} - {otherCollisionSfxHandler.m_AudioCollisionTag}", gameObject);
				PlaySfxForTagType(otherCollisionSfxHandler.m_AudioCollisionTag, collision.impulse.sqrMagnitude);
				return;
			}
			
			if(collision.gameObject.TryGetComponent(out Terrain terrain))
			{
				var texture = terrain.GetDominantTerrainTexture(collision.contacts[0].point);
				Log($"Terrain Texture collision: {m_AudioCollisionTag} - {(texture != null ? texture.name : "null")}", texture);
				if (AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig
				    .TryGetSfxKeyForTagTextureCollision(collision.impulse.sqrMagnitude, m_AudioCollisionTag,texture, out sfxConfig))
				{
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}

			var colliders = collision.gameObject.GetComponents<Collider>();
			foreach (Collider col in colliders)
			{
				if (AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig.TryGetSfxKeyForTagPhysMatCollision(
					    collision.impulse.sqrMagnitude, m_AudioCollisionTag, col.sharedMaterial, out sfxConfig))
				{
					Log($"Physics Material collision: {m_AudioCollisionTag} - {(col.sharedMaterial != null ? col.sharedMaterial.name : "null")}", col.sharedMaterial);
					AudioController.Instance.PlaySfx(sfxConfig, gameObject);
					return;
				}
			}
		}

		private void PlaySfxForTagType(AudioCollisionTag otherAudioTag, float impulseSquared)
		{
			if (AudioController.Instance.AudioSettings.DefaultAudioTagCollisionConfig.TryGetSfxKeyForTagTagCollision(impulseSquared, m_AudioCollisionTag,
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
