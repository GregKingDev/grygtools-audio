using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrygTools.Audio
{
	[Serializable]
	internal class ImpulseRangeCollisionSfx
	{
		[SerializeField,MinAndMaxRangeVec2(0f, 10000)]
		internal Vector2 m_ImpulseRange = new Vector2(0, 10000);
		[SerializeField]
		private AudioCollisionSfx[] m_CollisionSfx;
		internal AudioCollisionSfx[] CollisionSfx => m_CollisionSfx;
	}
	
	[CreateAssetMenu(fileName = "AudioTagCollisionConfig", menuName = "GrygTools/Audio Tag Collision Config")]
	public class AudioTagCollisionConfig : ScriptableObject
	{
		[SerializeField]
		private ImpulseRangeCollisionSfx[] m_ImpulseRangeCollisionSfx;
		
		private readonly List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> m_CollisionSfxDictPerRange = new List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>>();
		internal List<Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>> CollisionSfxDictPerRange => m_CollisionSfxDictPerRange;

		internal List<HashSet<(AudioCollisionTag, AudioCollisionTag)>> DuplicatesPerRange
		{
			get; private set;
		} = new List<HashSet<(AudioCollisionTag, AudioCollisionTag)>>();

		private void OnValidate()
		{
			ValidatePairs();
			ValidateRanges();
		}

		private void ValidatePairs()
		{
			m_CollisionSfxDictPerRange.Clear();
			DuplicatesPerRange.Clear();
			HashSet<(AudioCollisionTag, AudioCollisionTag)> uniquePairs = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
			
			foreach (ImpulseRangeCollisionSfx collisionSfxRange in m_ImpulseRangeCollisionSfx)
			{
				uniquePairs.Clear();
				var rangeDuplicates = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
				DuplicatesPerRange.Add(rangeDuplicates);
				
				var collisionSfxDict = new Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>();
				m_CollisionSfxDictPerRange.Add(collisionSfxDict);
				
				foreach (AudioCollisionSfx collisionSfx in collisionSfxRange.CollisionSfx)
				{
					//Special case for if both items are the same
					if(collisionSfx.AudioCollisionTag1 == collisionSfx.AudioCollisionTag2)
					{
						if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2)))
						{
							collisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
						}
						else
						{
							rangeDuplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
							Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
						}
					}
					else if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2))
					         && uniquePairs.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1)))
					{
						collisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
					}
					else
					{
						rangeDuplicates.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2));
						rangeDuplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
						Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
					}
				}
			}
		}
		
		internal bool TryGetSfxKeyForCollision(float impulse, AudioCollisionTag tag1, AudioCollisionTag tag2, out SfxConfig sfxConfig)
		{
			sfxConfig = null;
			if(TryGetCollisionSfxDictForImpulse(impulse, out var collisionSfxDict))
			{
				if (collisionSfxDict.TryGetValue((tag1, tag2), out sfxConfig) || collisionSfxDict.TryGetValue((tag2, tag1), out sfxConfig))
				{
					return true;
				}
			}
			return false;
		}
		
		internal bool TryGetCollisionSfxDictForImpulse(float impulse, out Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig> collisionSfxDict)
		{
			for (int i = 0; i < m_ImpulseRangeCollisionSfx.Length; i++)
			{
				var range = m_ImpulseRangeCollisionSfx[i];
				if (impulse >= range.m_ImpulseRange.x && impulse <= range.m_ImpulseRange.y)
				{
					collisionSfxDict = m_CollisionSfxDictPerRange[i];
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
