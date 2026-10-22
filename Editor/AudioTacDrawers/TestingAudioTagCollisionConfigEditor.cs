using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	[CustomEditor(typeof(TestingAudioTagCollisionConfig))]
	public class TestingAudioTagCollisionConfigEditor: Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Fix Ranges", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.FixRanges();
			}
			
			if (GUILayout.Button("Build Lookups", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.BuildLookups();
			}
			
			if (GUILayout.Button("Validate Ranges", GUILayout.Height(30)))
			{
				var config = (TestingAudioTagCollisionConfig)target;
				config?.ValidateRanges();
			}
		}
	}
}

