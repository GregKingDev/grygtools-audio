using GrygTools.Audio;
using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public struct ImpactSfxConfig
{
	public float MinVelocity;
	public float MaxVelocity;
	public SfxConfig SfxConfig;
}

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

public struct TagsToSfx
{
	[Tag]
	public string[] Tags;
	public SfxConfig SfxConfig;
}

public class ImpactSfxHandler : MonoBehaviour
{
	[Header("Sfx checks are done in order of PhysicMaterial, Texture, Tag. If no match is found the default Sfx will be played.")]
	[SerializeField]
	private PhysicMaterialsToSfx[] m_PhysicMaterialSfx;
	private Dictionary<PhysicsMaterial, SfxConfig> m_PhysicMaterialSfxLookup = new Dictionary<PhysicsMaterial, SfxConfig>();
	
	[SerializeField]
	private TexturesToSfx[] m_TextureSfx;
	private Dictionary<Texture, SfxConfig> m_TextureSfxLookup = new Dictionary<Texture, SfxConfig>();
	
	[SerializeField]
	private TagsToSfx[] m_TagSfx;
	private Dictionary<string, SfxConfig> m_TagSfxLookup = new Dictionary<string, SfxConfig>();
	
	[SerializeField]
	private ImpactSfxConfig m_WaterImpactSfx;
	private bool m_InWater = false;
	
	[SerializeField]
	private SfxConfig m_DefaultImpactSfx;

	private void Awake()
	{
		BuildLookup();
	}

	private void OnTriggerEnter(Collider other)
	{
		// The water layer mask is configured in the grygtools audio settings
		if (!m_InWater && ((1 << other.gameObject.layer) & GrygAudioSettings.WaterLayerMask) != 0)
		{
			AudioController.Instance.PlaySfx(m_WaterImpactSfx.SfxConfig, gameObject);
			Debug.Log("Collision with water detected, ignoring impact sound.");
			m_InWater = true;
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
			if(m_PhysicMaterialSfxLookup.TryGetValue(collision.collider.sharedMaterial, out sfxConfig) 
			   && sfxConfig.IsSet())
			{
				AudioController.Instance.PlaySfx(sfxConfig, gameObject);
				return;
			}
		}
		
		if(!string.IsNullOrEmpty(collision.gameObject.tag))
		{
			Log($"Tag: {collision.gameObject.tag}", collision.gameObject);
			if(m_TagSfxLookup.TryGetValue(collision.gameObject.tag, out sfxConfig) && sfxConfig.IsSet())
			{
				AudioController.Instance.PlaySfx(sfxConfig, gameObject);
				return;
			}
		}
		
		if(m_DefaultImpactSfx.IsSet())
		{
			AudioController.Instance.PlaySfx(m_DefaultImpactSfx, gameObject);
			return;
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
				if(!m_PhysicMaterialSfxLookup.TryAdd(physicMaterial, sfx.SfxConfig))
				{
					Debug.LogWarning($"Failed to add physics material {physicMaterial} to lookup dictionary. Duplicate Key.");
				}
			}
		}
		
		foreach (TexturesToSfx sfx in m_TextureSfx)
		{
			foreach (Texture texture in sfx.Textures)
			{
				if(!m_TextureSfxLookup.TryAdd(texture, sfx.SfxConfig))
				{
					Debug.LogWarning($"Failed to add texture {texture} to lookup dictionary. Duplicate Key.");
				}
			}
		}
		
		foreach (TagsToSfx sfx in m_TagSfx)
		{
			foreach (string tag in sfx.Tags)
			{
				if(!m_TagSfxLookup.TryAdd(tag, sfx.SfxConfig))
				{
					Debug.LogWarning($"Failed to add tag {tag} to lookup dictionary. Duplicate Key.");
				}
			}
		}
	}

	private void OnValidate()
	{
		BuildLookup();
	}
	
	[Conditional("IMPACT_SFX_DEBUG")]
	private void Log(string message, UnityEngine.Object context = null)
	{
		Debug.Log($"IMPACT SFX: {message}", context);
	}
}
