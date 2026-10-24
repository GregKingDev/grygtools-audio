using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrygTools.Audio
{
	[CreateAssetMenu(fileName = "NewAudioTagCollisionConfig", menuName = "GrygTools/New Audio Tag Collision Config")]
	public class AudioTagCollisionConfig : ScriptableObject
	{
		[SerializeField]
		private ImpulseRangeCollisionSfx[] m_ImpulseRanges;
		internal ImpulseRangeCollisionSfx[] ImpulseRanges => m_ImpulseRanges;
		
		[SerializeField]
		private AudioTagMappings[] m_AudioTagMappings;
		internal AudioTagMappings[] AudioTagMappings => m_AudioTagMappings;
		
#region lookups
		private readonly List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> m_PerRangeTagTagLookup = new List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>>();
		internal List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> PerRangeTagTagLookup => m_PerRangeTagTagLookup;
		
		private readonly Dictionary<Texture, AudioCollisionTag> m_TextureTagLookup = new Dictionary<Texture, AudioCollisionTag>();
		internal Dictionary<Texture, AudioCollisionTag> TextureTagLookup => m_TextureTagLookup;

		private readonly Dictionary<PhysicsMaterial, AudioCollisionTag> m_PhysMatTagLookup = new Dictionary<PhysicsMaterial, AudioCollisionTag>();
		internal Dictionary<PhysicsMaterial, AudioCollisionTag> PhysMatTagLookup => m_PhysMatTagLookup;
		
		private readonly Dictionary<string, AudioCollisionTag> m_UnityTagLookup = new Dictionary<string, AudioCollisionTag>();
		internal Dictionary<string, AudioCollisionTag> UnityTagLookup => m_UnityTagLookup;
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
		
		internal HashSet<AudioCollisionTag> TagDuplicates
		{
			get; private set;
		} = new HashSet<AudioCollisionTag>();
		
		internal HashSet<Texture> TextureToTagDuplicates
		{
			get; private set;
		} = new HashSet<Texture>();
		
		internal HashSet<PhysicsMaterial> PhysMatToTagDuplicates
		{
			get; private set;
		} = new HashSet<PhysicsMaterial>();
		
		internal HashSet<string> UnityTagToTagDuplicates
		{
			get; private set;
		} = new HashSet<string>();
#endregion

		private void OnDisable()
		{
			ValidateRanges();
			BuildLookups();
		}

		private void OnValidate()
		{
			ValidateRanges();
			BuildLookups();
		}

		internal void BuildLookups()
		{
			//Range lookups
			m_PerRangeTagTagLookup.Clear();
			
			//Mapping lookups
			m_TextureTagLookup.Clear();
			m_PhysMatTagLookup.Clear();
			m_UnityTagLookup.Clear();
			
			//Handle duplicates in the ranges
			TagToTagDuplicatesPerRange.Clear();
			TagDuplicatesPerRange.Clear();
			
			//Handle duplicates in AudioTagMappings 
			TagDuplicates.Clear();
			
			//Handle duplicates in the mappings
			TextureToTagDuplicates.Clear();
			PhysMatToTagDuplicates.Clear();
			UnityTagToTagDuplicates.Clear();
			
			HashSet<AudioCollisionTag> uniqueTags = new HashSet<AudioCollisionTag>();
			HashSet<Texture> uniqueTextureToTag = new HashSet<Texture>();
			HashSet<PhysicsMaterial> uniquePhysMatToTag = new HashSet<PhysicsMaterial>();
			HashSet<string> uniqueUnityTagToTag = new HashSet<string>();
			
			foreach (var range in m_ImpulseRanges)
			{
				// Unique holders to check for duplicates
				
				HashSet<(AudioCollisionTag, AudioCollisionTag)> tagToTagDuplicatesPerRange = 
					new HashSet<(AudioCollisionTag, AudioCollisionTag)> ();
				TagToTagDuplicatesPerRange.Add(tagToTagDuplicatesPerRange);
				
				HashSet<AudioCollisionTag> uniqueTagsPerRange = new HashSet<AudioCollisionTag>();
				HashSet<AudioCollisionTag> duplicateTagsPerRange = new HashSet<AudioCollisionTag>();
				TagDuplicatesPerRange.Add(duplicateTagsPerRange);
				
				
				var tagTagLookup = new Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>();
				m_PerRangeTagTagLookup.Add(tagTagLookup);
				HashSet<(AudioCollisionTag, AudioCollisionTag)> uniqueTagToTag = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
				
				foreach (var sfxInfo in range.CollisionSfx)
				{
					if(!uniqueTagsPerRange.Add(sfxInfo.AudioTag))
					{
						duplicateTagsPerRange.Add(sfxInfo.AudioTag);
					}
					foreach (var otherTagData in sfxInfo.TagToTagCollisionData)
					{
						foreach (var otherTag in otherTagData.OtherTags)
						{
							// special case for if both tags are the same.
							if (sfxInfo.AudioTag == otherTag)
							{
								if (uniqueTagToTag.Add((sfxInfo.AudioTag, otherTag)))
								{
									tagTagLookup[(otherTag, sfxInfo.AudioTag)] = otherTagData.SfxConfig;
								}
								else
								{
									tagToTagDuplicatesPerRange.Add((sfxInfo.AudioTag, otherTag));
								}								
							}
							else
							{
								if (uniqueTagToTag.Add((sfxInfo.AudioTag, otherTag)) && uniqueTagToTag.Add((otherTag, sfxInfo.AudioTag)))
								{
									tagTagLookup[(sfxInfo.AudioTag, otherTag)] = otherTagData.SfxConfig;
									tagTagLookup[(otherTag, sfxInfo.AudioTag)] = otherTagData.SfxConfig;
								}
								else
								{
									tagToTagDuplicatesPerRange.Add((sfxInfo.AudioTag, otherTag));
									tagToTagDuplicatesPerRange.Add((otherTag, sfxInfo.AudioTag));
								}
							}
						}
					}
				}
			}

			foreach (AudioTagMappings audioTagMappings in m_AudioTagMappings)
			{
				if (!uniqueTags.Add(audioTagMappings.AudioTag))
				{
					TagDuplicates.Add(audioTagMappings.AudioTag);
				}

				foreach (Texture2D texture in audioTagMappings.Textures)
				{
					if(!uniqueTextureToTag.Add(texture))
					{
						TextureToTagDuplicates.Add(texture);
					}
					else
					{
						m_TextureTagLookup.Add(texture, audioTagMappings.AudioTag);
					}
				}

				foreach (PhysicsMaterial physicsMaterial in audioTagMappings.PhysicsMaterials)
				{
					if(!uniquePhysMatToTag.Add(physicsMaterial))
					{
						PhysMatToTagDuplicates.Add(physicsMaterial);
					}
					else
					{
						m_PhysMatTagLookup.Add(physicsMaterial, audioTagMappings.AudioTag);
					}
				}

				foreach (string unityTag in audioTagMappings.UnityTags)
				{
					if(!uniqueUnityTagToTag.Add(unityTag))
					{
						UnityTagToTagDuplicates.Add(unityTag);
					}
					else
					{
						m_UnityTagLookup.Add(unityTag, audioTagMappings.AudioTag);
					}
				}
			}

			for (int i = 0; i < TagToTagDuplicatesPerRange.Count; i++)
			{
				if(TagToTagDuplicatesPerRange[i].Count > 0)
				{
					string duplicates = "";
					foreach (var dupes in TagToTagDuplicatesPerRange[i])
					{
						duplicates += $"Pair: {dupes.Item1} - {dupes.Item2}\n";
						
					}
					Logger.LogWarning($"Found {TagToTagDuplicatesPerRange[i].Count} duplicate tag-tag pairs in a range {i}. Pairs: {duplicates}");
				}
			}

			for (int i = 0; i < TagDuplicatesPerRange.Count; i++)
			{
				if(TagDuplicatesPerRange[i].Count > 0)
				{
					Logger.LogWarning($"Found {TagDuplicatesPerRange[i].Count} duplicate tags in a range.");
				}
			}
			
			if(UnityTagToTagDuplicates.Count > 0 || PhysMatToTagDuplicates.Count > 0 || TextureToTagDuplicates.Count > 0 || TagDuplicates.Count > 0)
			{
				Logger.LogWarning("Duplicates found in AudioTagMappings. TagDuplicates: " + TagDuplicates.Count + 
					", TextureToTagDuplicates: " + TextureToTagDuplicates.Count + ", PhysMatToTagDuplicates: " + 
					PhysMatToTagDuplicates.Count + ", UnityTagToTagDuplicates: " + UnityTagToTagDuplicates.Count);
			}
			Logger.Log($"Built {m_PerRangeTagTagLookup.Count} tag-tag lookups, {m_TextureTagLookup.Count} tag-texture lookups, and {m_PhysMatTagLookup.Count} tag-physmat lookups.");
		}
		
		internal bool ValidateRanges()
		{
			var rangesValid = true;
			for(int i = 0; i < m_ImpulseRanges.Length - 1; i++)
			{
				if (i > 0)
				{
					for (int j = i + 1; j < m_ImpulseRanges.Length; j++)
					{
						if(m_ImpulseRanges[i].m_ImpulseRange.x > m_ImpulseRanges[j].m_ImpulseRange.x
						   && m_ImpulseRanges[i].m_ImpulseRange.x < m_ImpulseRanges[j].m_ImpulseRange.y)
						{
							rangesValid = false;
							Debug.LogWarning(
								$"CollisionRange at index {i} has an impulse range that overlaps with another range. Please ensure that each SFX has a unique impulse range.");
							continue;
						}
						if (m_ImpulseRanges[i].m_ImpulseRange.y > m_ImpulseRanges[j].m_ImpulseRange.x
						    && m_ImpulseRanges[i].m_ImpulseRange.y < m_ImpulseRanges[j].m_ImpulseRange.y)
						{
							rangesValid = false;
							Debug.LogWarning(
								$"CollisionRange at index {i} has an impulse range that overlaps with another range. Please ensure that each SFX has a unique impulse range.");
						}
					}
				}
			}
			return rangesValid;
		}
		
		internal void FixRanges()
		{
			bool changes = false;
			Array.Sort(m_ImpulseRanges, (a, b) => a.m_ImpulseRange.x.CompareTo(b.m_ImpulseRange.x));
			for(int i = 0; i < m_ImpulseRanges.Length; i++)
			{
				if(i > 0 && m_ImpulseRanges[i].m_ImpulseRange.x < m_ImpulseRanges[i - 1].m_ImpulseRange.y)	
				{
					if (m_ImpulseRanges[i].m_ImpulseRange.y < m_ImpulseRanges[i - 1].m_ImpulseRange.y)	
					{
						m_ImpulseRanges[i].m_ImpulseRange.y = m_ImpulseRanges[i - 1].m_ImpulseRange.y + 1;
					}
					m_ImpulseRanges[i].m_ImpulseRange.x = m_ImpulseRanges[i - 1].m_ImpulseRange.y;
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