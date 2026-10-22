using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
namespace GrygTools.Audio
{
	[Serializable]
	internal class ImpulseRangeCollisionSfx
	{
		[SerializeField,MinAndMaxRangeVec2(0f, 10000)]
		internal Vector2 m_ImpulseRange = new Vector2(0, 10000);
		[SerializeField]
		private AudioTagCollisionSfx[] m_CollisionSfx;
		internal AudioTagCollisionSfx[] CollisionSfx => m_CollisionSfx;

		[FormerlySerializedAs("m_TextureCollisionSfx")]
		[SerializeField]
		private AudioTagToTexturePhysMatCollisionSfx[] m_TextureAndMaterialCollisionSfx;
		internal AudioTagToTexturePhysMatCollisionSfx[] TextureAndMaterialCollisionSfx => m_TextureAndMaterialCollisionSfx;
	}
	
	[CreateAssetMenu(fileName = "AudioTagCollisionConfig", menuName = "GrygTools/Audio Tag Collision Config")]
	public class AudioTagCollisionConfig : ScriptableObject
	{
		//TODO: should the texture and physmat mappings be to tags instead of sfx?
		
		
		[SerializeField]
		private ImpulseRangeCollisionSfx[] m_ImpulseRangeCollisionSfx;

#region Tag Lookups  
		private readonly List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> m_PerRangeTagTagLookup = new List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>>();
		internal List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> PerRangeTagTagLookup => m_PerRangeTagTagLookup;

		private readonly List<Dictionary<(AudioCollisionTag,Texture), SfxConfig>> m_PerRangeTagTextureLookup = new List<Dictionary<(AudioCollisionTag,Texture), SfxConfig>>();
		internal List<Dictionary<(AudioCollisionTag,Texture), SfxConfig>> PerRangeTagTextureLookup => m_PerRangeTagTextureLookup;

		private readonly List<Dictionary<(AudioCollisionTag,PhysicsMaterial), SfxConfig>> m_PerRangeTagPhysMatLookup = new List<Dictionary<(AudioCollisionTag,PhysicsMaterial), SfxConfig>>();
		internal List<Dictionary<(AudioCollisionTag,PhysicsMaterial), SfxConfig>> PerRangeTagPhysMatLookup => m_PerRangeTagPhysMatLookup;
#endregion

#region Duplicate Management
		internal List<HashSet<(AudioCollisionTag, AudioCollisionTag)>> TagToTagDuplicatesPerRange
		{
			get; private set;
		} = new List<HashSet<(AudioCollisionTag, AudioCollisionTag)>>();
		
		internal List<HashSet<AudioCollisionTag>> TagDuplicatesPerRange
		{
			get; private set;
		} = new List<HashSet<AudioCollisionTag>> ();
		
		internal List<HashSet<(AudioCollisionTag, Texture)>> TagToTextureDuplicatesPerRange
		{
			get; private set;
		} = new List<HashSet<(AudioCollisionTag, Texture)>>();
		
		internal List<HashSet<(AudioCollisionTag, PhysicsMaterial)>> TagToPhysicsMaterialDuplicatesPerRange
		{
			get; private set;
		} = new List<HashSet<(AudioCollisionTag, PhysicsMaterial)>>();
#endregion

		private void OnValidate()
		{
			ValidatePairs();
			ValidateRanges();
		}

		private void ValidatePairs()
		{
			//Range lookups
			m_PerRangeTagTagLookup.Clear();
			m_PerRangeTagTextureLookup.Clear();
			m_PerRangeTagPhysMatLookup.Clear();
			//Duplicate trackers
			TagDuplicatesPerRange.Clear();
			TagToTagDuplicatesPerRange.Clear();
			TagToTextureDuplicatesPerRange.Clear();
			TagToPhysicsMaterialDuplicatesPerRange.Clear();
			
			HashSet<(AudioCollisionTag, AudioCollisionTag)> uniquePairs = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
			HashSet<(AudioCollisionTag, Texture)> uniqueTexturePairs = new HashSet<(AudioCollisionTag, Texture)>();
			HashSet<(AudioCollisionTag, PhysicsMaterial)> uniquePhysicsMaterialPairs = new HashSet<(AudioCollisionTag, PhysicsMaterial)>();
			
			//Iterate each Impulse Range
			foreach (ImpulseRangeCollisionSfx collisionSfxRange in m_ImpulseRangeCollisionSfx)
			{
				uniquePairs.Clear();
				uniqueTexturePairs.Clear();
				uniquePhysicsMaterialPairs.Clear();
				
				var rangeTagDuplicates = new HashSet<AudioCollisionTag>();
				TagDuplicatesPerRange.Add(rangeTagDuplicates);
				var rangeTagToTagDuplicates = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
				TagToTagDuplicatesPerRange.Add(rangeTagToTagDuplicates);
				var rangeTextureDuplicates = new HashSet<(AudioCollisionTag, Texture)>();
				TagToTextureDuplicatesPerRange.Add(rangeTextureDuplicates);
				var rangePhysicsMaterialDuplicates = new HashSet<(AudioCollisionTag, PhysicsMaterial)>();
				TagToPhysicsMaterialDuplicatesPerRange.Add(rangePhysicsMaterialDuplicates);
				
				var tagTagSfxDict = new Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>();
				m_PerRangeTagTagLookup.Add(tagTagSfxDict);
				
				var tagTextureSfxDict = new Dictionary<(AudioCollisionTag, Texture), SfxConfig>();
				m_PerRangeTagTextureLookup.Add(tagTextureSfxDict);
				
				var tagPhysicsMaterialSfxDict = new Dictionary<(AudioCollisionTag, PhysicsMaterial), SfxConfig>();
				m_PerRangeTagPhysMatLookup.Add(tagPhysicsMaterialSfxDict);
				
				foreach (AudioTagCollisionSfx collisionSfx in collisionSfxRange.CollisionSfx)
				{
					//Special case for if both items are the same
					if(collisionSfx.AudioCollisionTag1 == collisionSfx.AudioCollisionTag2)
					{
						if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2)))
						{
							tagTagSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
						}
						else
						{
							rangeTagToTagDuplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
							Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
						}
					}
					else if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2))
					         && uniquePairs.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1)))
					{
						tagTagSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
					}
					else
					{
						rangeTagToTagDuplicates.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2));
						rangeTagToTagDuplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
						Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
					}
				}

				HashSet<AudioCollisionTag> uniqueTags = new HashSet<AudioCollisionTag>();
				//Iterate each AudioTagToTexturePhysMatCollisionSfx entry in the range and validate duplicates for tags, textures and physics materials
				foreach (AudioTagToTexturePhysMatCollisionSfx entry in collisionSfxRange.TextureAndMaterialCollisionSfx)
				{
					if (!uniqueTags.Add(entry.AudioCollisionTag))
					{
						rangeTagDuplicates.Add(entry.AudioCollisionTag);
					}
					foreach (TexturesAndMaterialsToSfx texturesAndMaterialsToSfx in entry.TexturesAndMaterialsToSfx)
					{
						foreach (Texture texture in texturesAndMaterialsToSfx.Textures)
						{
							if (!uniqueTexturePairs.Add((entry.AudioCollisionTag, texture)))
							{
								rangeTextureDuplicates.Add((entry.AudioCollisionTag, texture));
								Debug.LogWarning($"Duplicate texture found for tag {entry.AudioCollisionTag}: {texture}. Only the first occurrence will be used.", this);
							}
							else
							{
								tagTextureSfxDict.Add((entry.AudioCollisionTag, texture), texturesAndMaterialsToSfx.SfxConfig);
							}
						}
						foreach (PhysicsMaterial physicsMaterial in texturesAndMaterialsToSfx.PhysicsMaterials)
						{
							if (!uniquePhysicsMaterialPairs.Add((entry.AudioCollisionTag, physicsMaterial)))
							{
								rangeTagDuplicates.Add(entry.AudioCollisionTag);
								Debug.LogWarning($"Duplicate physics material found for tag {entry.AudioCollisionTag}: {physicsMaterial}. Only the first occurrence will be used.", this);
							}
							if (!rangePhysicsMaterialDuplicates.Add((entry.AudioCollisionTag, physicsMaterial)))
							{
								tagPhysicsMaterialSfxDict.Add((entry.AudioCollisionTag, physicsMaterial), texturesAndMaterialsToSfx.SfxConfig);
							}
						}
					}
				}
			}
		}
		
		internal bool TryGetSfxKeyForTagTagCollision(float impulse, AudioCollisionTag tag1, AudioCollisionTag tag2, out SfxConfig sfxConfig)
		{
			sfxConfig = null;
			if(TryGetTagTagCollisionSfxDictForImpulse(impulse, out var collisionSfxDict))
			{
				if (collisionSfxDict.TryGetValue((tag1, tag2), out sfxConfig) || collisionSfxDict.TryGetValue((tag2, tag1), out sfxConfig))
				{
					return true;
				}
			}
			return false;
		}
		
		internal bool TryGetSfxKeyForTagTextureCollision(float impulse, AudioCollisionTag tag, Texture texture, out SfxConfig sfxConfig)
		{
			sfxConfig = null;
			if(TryGetTagTextureCollisionSfxDictForImpulse(impulse, out var collisionSfxDict))
			{
				if (collisionSfxDict.TryGetValue((tag, texture), out sfxConfig))
				{
					return true;
				}
			}
			return false;
		}
		
		internal bool TryGetSfxKeyForTagPhysMatCollision(float impulse, AudioCollisionTag tag, PhysicsMaterial physMat, out SfxConfig sfxConfig)
		{
			sfxConfig = null;
			if(TryGetTagPhysMatCollisionSfxDictForImpulse(impulse, out var collisionSfxDict))
			{
				if (collisionSfxDict.TryGetValue((tag, physMat), out sfxConfig))
				{
					return true;
				}
			}
			return false;
		}
		
		internal bool TryGetTagTagCollisionSfxDictForImpulse(float impulse, out Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig> collisionSfxDict)
		{
			for (int i = 0; i < m_ImpulseRangeCollisionSfx.Length; i++)
			{
				var range = m_ImpulseRangeCollisionSfx[i];
				if (impulse >= range.m_ImpulseRange.x && impulse <= range.m_ImpulseRange.y)
				{
					collisionSfxDict = m_PerRangeTagTagLookup[i];
					return true;
				}
			}
			collisionSfxDict = null;
			return false;
		}
		
		internal bool TryGetTagTextureCollisionSfxDictForImpulse(float impulse, out Dictionary<(AudioCollisionTag, Texture), SfxConfig> collisionSfxDict)
		{
			for (int i = 0; i < m_ImpulseRangeCollisionSfx.Length; i++)
			{
				var range = m_ImpulseRangeCollisionSfx[i];
				if (impulse >= range.m_ImpulseRange.x && impulse <= range.m_ImpulseRange.y)
				{
					collisionSfxDict = m_PerRangeTagTextureLookup[i];
					return true;
				}
			}
			collisionSfxDict = null;
			return false;
		}
		
		internal bool TryGetTagPhysMatCollisionSfxDictForImpulse(float impulse, out Dictionary<(AudioCollisionTag, PhysicsMaterial), SfxConfig> collisionSfxDict)
		{
			for (int i = 0; i < m_ImpulseRangeCollisionSfx.Length; i++)
			{
				var range = m_ImpulseRangeCollisionSfx[i];
				if (impulse >= range.m_ImpulseRange.x && impulse <= range.m_ImpulseRange.y)
				{
					collisionSfxDict = m_PerRangeTagPhysMatLookup[i];
					return true;
				}
			}
			collisionSfxDict = null;
			return false;
		}
		
		private void ValidateRanges()
		{
			for(int i = 0; i < m_ImpulseRangeCollisionSfx.Length - 1; i++)
			{
				if (i > 0)
				{
					for (int j = i + 1; j < m_ImpulseRangeCollisionSfx.Length; j++)
					{
						if(m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.x > m_ImpulseRangeCollisionSfx[j].m_ImpulseRange.x
						   && m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.x < m_ImpulseRangeCollisionSfx[j].m_ImpulseRange.y)
						{
							Debug.LogWarning(
								$"CollisionRange at index {i} has an impulse range that overlaps with another range. Please ensure that each SFX has a unique impulse range.");
							continue;
						}
						if (m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.y > m_ImpulseRangeCollisionSfx[j].m_ImpulseRange.x
						    && m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.y < m_ImpulseRangeCollisionSfx[j].m_ImpulseRange.y)
						{
							Debug.LogWarning(
								$"CollisionRange at index {i} has an impulse range that overlaps with another range. Please ensure that each SFX has a unique impulse range.");
						}
					}
				}
			}
		}

		internal void FixRanges()
		{
			bool changes = false;
			Array.Sort(m_ImpulseRangeCollisionSfx, (a, b) => a.m_ImpulseRange.x.CompareTo(b.m_ImpulseRange.x));
			for(int i = 0; i < m_ImpulseRangeCollisionSfx.Length; i++)
			{
				if(i > 0 && m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.x < m_ImpulseRangeCollisionSfx[i - 1].m_ImpulseRange.y)	
				{
					if (m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.y < m_ImpulseRangeCollisionSfx[i - 1].m_ImpulseRange.y)
					{
						m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.y = m_ImpulseRangeCollisionSfx[i - 1].m_ImpulseRange.y + 1;
					}
					m_ImpulseRangeCollisionSfx[i].m_ImpulseRange.x = m_ImpulseRangeCollisionSfx[i - 1].m_ImpulseRange.y;
					Debug.LogWarning($"CollisionRange at index {i} has an impulse range that overlaps with the previous SFX. Adjusting");
					changes = true;
				}
			}

			if (changes)
			{
				FixRanges();
			}
		}
	}
}
