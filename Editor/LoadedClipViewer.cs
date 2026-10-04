using UnityEditor;
using UnityEngine;
namespace GrygTools.Audio
{
	public class LoadedClipViewer : EditorWindow
	{
		private Vector2 m_ScrollPosition;

		[MenuItem("GrygTools/Audio/Loaded Clip Viewer")]
		private static void ShowWindow()
		{
			GetWindow<LoadedClipViewer>("Loaded Clip Viewer");
		}

		private void OnGUI()
		{
			AudioController controller = AudioController.ActiveInstance;
			if (controller == null)
			{
				EditorGUILayout.HelpBox("AudioController is not running. Enter Play Mode to view loaded clips.", MessageType.Info);
				return;
			}

			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(150));
			EditorGUILayout.LabelField("Count", EditorStyles.boldLabel, GUILayout.Width(50));
			EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			DrawLineSeparator();
			foreach (var pair in controller.ClipsListDictionary)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(pair.Key, GUILayout.Width(150));

				uint concurrentCount = controller.ConcurrentCountDictionary.TryGetValue(pair.Key, out uint count) ? count : 0;
				uint maxConcurrent = controller.ConcurrentMaxesDictionary.TryGetValue(pair.Key, out uint maxCount) ? maxCount : AudioController.MaxConcurrent;
				EditorGUILayout.LabelField($"{concurrentCount}/{maxConcurrent}", GUILayout.Width(50));

				if (pair.Value == null || pair.Value.Clips.Count == 0)
				{
					EditorGUILayout.LabelField("No clips loaded");
				}
				else
				{
					EditorGUILayout.BeginVertical();
					foreach (var clipEntry in pair.Value.Clips)
					{
						EditorGUILayout.ObjectField(clipEntry.Clip, typeof(AudioClip), false);
					}
					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndHorizontal();

				DrawLineSeparator();
			}

			EditorGUILayout.EndScrollView();

			Repaint();
		}
		
		private void DrawLineSeparator()
		{
			var dividingLineRect = EditorGUILayout.BeginHorizontal();
			Handles.color = Color.gray;
			Handles.DrawLine(new Vector2(dividingLineRect.x - 15, dividingLineRect.y),
				new Vector2(dividingLineRect.width + 15, dividingLineRect.y));
			EditorGUILayout.EndHorizontal();
		}
	}
}
