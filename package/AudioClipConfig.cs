using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace GrygTools.Audio
{
	[CreateAssetMenu(menuName = "GrygTools/AudioConfig")]
	public class AudioClipConfig : ScriptableObject
	{
		[SerializeField]
		private List<AudioClipConfigEntry> m_Entries = new List<AudioClipConfigEntry>();
		public List<AudioClipConfigEntry> Entries => m_Entries;
	}
	
	[Serializable]
	public class AudioClipConfigEntry
	{
		[SerializeField]
		private string m_Key;
		public string Key => m_Key;
		
		[SerializeField]
		private AssetReferenceT<AudioClip> m_Reference;
		public AssetReferenceT<AudioClip> Reference => m_Reference;
		
		[SerializeField]
		[Min(0)]
		private uint m_MaxSimultaneous = 5;
		public uint MaxSimultaneous => m_MaxSimultaneous;
		
		[SerializeField]
		[Min(0f)]
		private float m_MinTimeBetweenPlays = 0.01f;
		public float MinTimeBetweenPlays => m_MinTimeBetweenPlays;
		
		[SerializeField]
		[Min(1f)]
		private int m_Weight = 1;
		public int Weight => m_Weight;
	}
}