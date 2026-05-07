#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrygTools.Audio
{
	public class AudioKeysUtility
	{
		public static IReadOnlyCollection<string> AudioClipKeys => s_AudioClipKeys;
		private static HashSet<string> s_AudioClipKeys = new HashSet<string>();
		private static bool m_IsDirty = true;
		
		public static void SetKeysDirty()
		{
			m_IsDirty = true;
		}
		
		[MenuItem("GrygTools/Audio/Check Clip Configs")]
		public static void ValidateClipConfigs()
		{
			RefreshClipKeys(true);
		}
		
		public static void RefreshClipKeys(bool useLogging = false)
		{
			if (!m_IsDirty)
			{
				return;
			}
			
			string[] audioConfigGuids = AssetDatabase.FindAssets("t:AudioClipConfig");
			int problemCount = 0;
			int problemConfigCount = 0;
			
			void ColorLog(object message, UnityEngine.Object context = null)
			{
				if (useLogging)
				{
					Debug.Log($"<color=orange>{message}</color>", context);
				}
			}
			s_AudioClipKeys.Clear();
			foreach (string audioConfigGuid in audioConfigGuids)
			{
				bool problemFound = false;
				string configPath = AssetDatabase.GUIDToAssetPath(audioConfigGuid);
				AudioClipConfig audioConfig = AssetDatabase.LoadAssetAtPath<AudioClipConfig>(configPath);
				foreach (AudioClipConfigEntry entry in audioConfig.Entries)
				{	
					//If block is a bit weird but I only wanted one console message per kvp
					if (string.IsNullOrEmpty(entry.Key))
					{
						if ((entry.Reference == null || !entry.Reference.RuntimeKeyIsValid()))
						{
							ColorLog($"No key and bad ClipReference", audioConfig);
							problemCount++;
							problemFound = true;
						}
						else
						{
							ColorLog($"Key is empty", audioConfig);
							problemCount++;
							problemFound = true;
						}
					}
					else if(entry.Reference == null || !entry.Reference.RuntimeKeyIsValid())
					{
						ColorLog($"Bad clip for key <b>{entry.Key}</b>", audioConfig);
						problemCount++;
						problemFound = true;
					}
					s_AudioClipKeys.Add(entry.Key);
				}

				if (problemFound)
				{
					problemConfigCount++;
				}
			}
			ColorLog($"{audioConfigGuids.Length} Configs checked. Found {problemCount} problems across {problemConfigCount} files");
			m_IsDirty = false;
		}
	}
}
#endif