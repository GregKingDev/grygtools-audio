using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GrygTools.Audio
{
	[CreateAssetMenu(menuName = "GrygTools/AudioConfig")]
	public class AudioClipConfig : ScriptableObject
	{
		[SerializeField]
		public List<AudioClipConfigEntry> Entries = new List<AudioClipConfigEntry>();
	}
	
	[Serializable]
	public class AudioClipConfigEntry
	{
		public string key;
		public AssetReferenceT<AudioClip> reference;
		[Min(0)]
		public uint maxSimultaneous = 5;
		[Min(0f)]
		public float minTimeBetweenPlays = 0.01f;
	}
}