using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomEditor(typeof(AudioTagCollisionConfig))]
	public class AudioTagCollisionConfigEditor: Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Fix Ranges", GUILayout.Height(30)))
			{
				var config = (AudioTagCollisionConfig)target;
				config?.FixRanges();
			}
			
			if (GUILayout.Button("Build Lookups", GUILayout.Height(30)))
			{
				var config = (AudioTagCollisionConfig)target;
				config?.BuildLookups();
			}
			
			if (GUILayout.Button("Validate Ranges", GUILayout.Height(30)))
			{
				var config = (AudioTagCollisionConfig)target;
				config?.ValidateRanges();
			}
		}
	}
}

