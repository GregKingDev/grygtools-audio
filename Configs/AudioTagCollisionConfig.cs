using GrygTools.Utils.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace GrygTools.Audio
{
	[Serializable]
	public class ImpulseRangeCollisionSfx
	{
		[SerializeField,MinAndMaxRangeVec2(0f, 10000)]
		private Vector2 m_ImpulseRange = new Vector2(0, 10000);
		[SerializeField]
		private AudioCollisionSfx[] m_CollisionSfx;
	}
	
	[CreateAssetMenu(fileName = "AudioTagCollisionConfig", menuName = "GrygTools/Audio Tag Collision Config")]
	public class AudioTagCollisionConfig : ScriptableObject
	{
		[SerializeField]
		private ImpulseRangeCollisionSfx[] m_ImpulseRangeCollisionSfx;

		private readonly Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig> m_CollisionSfxDict = new Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig>();
		internal Dictionary<(AudioCollisionTag, AudioCollisionTag), SfxConfig> CollisionSfxDict => m_CollisionSfxDict;
		
		private readonly HashSet<(AudioCollisionTag, AudioCollisionTag)> m_Duplicates = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
		internal HashSet<(AudioCollisionTag, AudioCollisionTag)> Duplicates => m_Duplicates;

		private void OnValidate()
		{
			ValidatePairs();
		}

		private void ValidatePairs()
		{
			m_CollisionSfxDict.Clear();
			m_Duplicates.Clear();
			HashSet<(AudioCollisionTag, AudioCollisionTag)> uniquePairs = new HashSet<(AudioCollisionTag, AudioCollisionTag)>();
			// foreach (AudioCollisionSfx collisionSfx in m_CollisionSfx)
			// {
			// 	//Special case for if both items are the same
			// 	if(collisionSfx.AudioCollisionTag1 == collisionSfx.AudioCollisionTag2)
			// 	{
			// 		if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2)))
			// 		{
			// 			m_CollisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
			// 		}
			// 		else
			// 		{
			// 			m_Duplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
			// 			Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
			// 		}
			// 	}
			// 	else if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2))
			// 	         && uniquePairs.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1)))
			// 	{
			// 		m_CollisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxConfig);
			// 	}
			// 	else
			// 	{
			// 		m_Duplicates.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2));
			// 		m_Duplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
			// 		Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
			// 	}
			// }
		}
		
		public bool TryGetSfxKeyForCollision(AudioCollisionTag tag1, AudioCollisionTag tag2, out SfxConfig sfxConfig)
		{
			if (m_CollisionSfxDict.TryGetValue((tag1, tag2), out sfxConfig) || m_CollisionSfxDict.TryGetValue((tag2, tag1), out sfxConfig))
			{
				return true;
			}
			else
			{
				sfxConfig = null;
				return false;
			}
		}
	}
}
