using System.Collections.Generic;
using UnityEngine;

namespace GrygTools.Audio
{
	public class ClipLibrary
	{
		private List<(int,AudioClip)> m_Clips = new List<(int,AudioClip)>();
		private int m_TotalWeight = 0;
		public int Count => m_Clips.Count;
		public void AddClip(int weight, AudioClip clip)
		{
			if (m_Clips.Count > 0)
			{
				m_Clips.Add((weight + m_Clips[^1].Item1, clip));
			}
			else
			{
				m_Clips.Add((weight, clip));
			}
			m_TotalWeight += weight;
		}

		public AudioClip GetClip()
		{
			int roll = Random.Range(0, m_TotalWeight);
			foreach ((int, AudioClip) tuple in m_Clips)
			{
				if (roll < tuple.Item1)
				{
					return tuple.Item2;
				}
			}
			return null;
		}
	}
}
