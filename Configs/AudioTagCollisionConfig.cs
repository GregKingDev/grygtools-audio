using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace GrygTools.Audio
{
	[CreateAssetMenu(fileName = "AudioTagCollisionConfig", menuName = "GrygTools/Audio Tag Collision Config")]
	public class AudioTagCollisionConfig : ScriptableObject
	{
		[SerializeField]
		private AudioCollisionSfx[] m_CollisionSfx;

		private readonly Dictionary<(AudioCollisionTag, AudioCollisionTag), string> m_CollisionSfxDict = new Dictionary<(AudioCollisionTag, AudioCollisionTag), string>();
		internal Dictionary<(AudioCollisionTag, AudioCollisionTag), string> CollisionSfxDict => m_CollisionSfxDict;
		
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
			foreach (AudioCollisionSfx collisionSfx in m_CollisionSfx)
			{
				//Special case for if both items are the same
				if(collisionSfx.AudioCollisionTag1 == collisionSfx.AudioCollisionTag2)
				{
					if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2)))
					{
						m_CollisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxKey);
					}
					else
					{
						m_Duplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
						Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
					}
				}
				else if (uniquePairs.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2))
				         && uniquePairs.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1)))
				{
					m_CollisionSfxDict.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2), collisionSfx.SfxKey);
				}
				else
				{
					m_Duplicates.Add((collisionSfx.AudioCollisionTag1, collisionSfx.AudioCollisionTag2));
					m_Duplicates.Add((collisionSfx.AudioCollisionTag2, collisionSfx.AudioCollisionTag1));
					Debug.LogWarning($"Duplicate collision pair found: {collisionSfx.AudioCollisionTag1} - {collisionSfx.AudioCollisionTag2}. Only the first occurrence will be used.", this);
				}
			}
		}
		
		public string GetSfxKeyForCollision(AudioCollisionTag tag1, AudioCollisionTag tag2)
		{
			if (m_CollisionSfxDict.TryGetValue((tag1, tag2), out string sfxKey) || m_CollisionSfxDict.TryGetValue((tag2, tag1), out sfxKey))
			{
				return sfxKey;
			}
			else
			{
				Audio.Logger.Log($"No SFX key found for collision between {tag1} and {tag2}.");
				return null;
			}
		}
	}
}
