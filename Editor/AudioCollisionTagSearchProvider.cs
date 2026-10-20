using GrygTools.Audio.SfxHandlers;
using GrygTools.Utils.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace GrygTools.Audio
{
	public static class AudioCollisionTagSearchProvider
	{
		private static Dictionary<int, string> optionsDictionary = new ();
		private static List<string> optionsList = new ();
		private static Dictionary<object, string> nameDictionary = new();
		private static int idHash => "AudioCollisionTagSearch".GetHashCode();
		
		
		private static GrygAudioSettings m_Settings;
		private static GrygAudioSettings Settings
		{
			get
			{
				if (m_Settings == null)
				{
					m_Settings = GrygAudioSettings.GetOrCreateSettings();
				}
				return m_Settings;
			}
		}
		
		private static bool TypeCheck(SerializedProperty property, out string error)
		{
			error = null;
			if (property.type == "AudioCollisionTag")
			{
				return true;
			}
			error = "AudioCollisionTag searchable property must be used on AudioCollisionTag values";
			return false;
		}
		
		private static bool IndexComparison(SerializedProperty property, string stringValue)
		{
			return stringValue == property.FindPropertyRelative("_value").stringValue;
		}
		
		private static void Populate()
		{
			if (nameDictionary.Count <= 0 || Settings.CollisionTags.Count != optionsList.Count)
			{
				optionsDictionary.Clear();
				optionsList.Clear();
				nameDictionary.Clear();
				foreach (string collisionTag in Settings.CollisionTags)
				{
					optionsDictionary.Add(collisionTag.GetHashCode(), collisionTag);
				}
				optionsDictionary.Add("-".GetHashCode(), "-");
				
				optionsList.AddRange(optionsDictionary.Values);
				foreach (string collisionTag in optionsList)
				{
					nameDictionary.Add(collisionTag, collisionTag);
				}
			}
		}
		
		private static void OnItemSelected(SearchColumn column, SearchColumnEventArgs args, string choice)
		{
			Debug.Log($"Applied {choice} to search item: {args.item.id}");
			SearchColumnEventArgs updateArgs = new SearchColumnEventArgs(
					item: args.item,
					context: args.context,
					column: args.column
				);
			updateArgs.value = choice;
			column.setter(updateArgs);
			EditorUtility.SetDirty(args.item.ToObject());
		}
		
		[SearchColumnProvider("AudioCollisionTag")]
		public static void InitializeCollisionSFxHandlerColumn(SearchColumn column)
		{
			column.drawer = args =>
			{
				Populate();

				void OnSelect(int i)
				{
					OnItemSelected(column, args, optionsList[i]);
					SearchService.RefreshWindows();
					ScheduleSearchWindowRefresh();
				}

				if (EditorGUILayout.DropdownButton(new GUIContent(args.value?.ToString() ?? "Select"), FocusType.Passive))
				{
					var currentValue = args.value?.ToString();
					int currentIndex = Array.IndexOf(Settings.CollisionTagsStrings, currentValue);
					List<string> strings = new();
					foreach (AudioCollisionTag tag in Settings.CollisionTags)
					{
						strings.Add(tag.ToString());
					}
					strings.Add("-");
					if(currentIndex == -1)
					{
						currentIndex = strings.Count - 1;
					}
					
					SearchablePopup.Show(args.rect, nameDictionary.Values.ToArray(), currentIndex, OnSelect);
				}
				return args.value;
			};

			column.setter = args =>
			{
				foreach (SearchItem item in args.context.selection)
				{
					var handler = item.ToObject<CollisionSfxHandler>();
					if(handler != null)
					{
						handler.AudioCollisionTag = new AudioCollisionTag(args.value?.ToString() ?? string.Empty);
						EditorUtility.SetDirty(handler);
						EditorUtility.SetDirty(handler.gameObject);
					}
				}
			};
		}
		private static void ScheduleSearchWindowRefresh()
		{
			EditorApplication.update += UpdateSearchWindows;
		}

		private static void UpdateSearchWindows()
		{
			SearchService.RefreshWindows();
			EditorApplication.update -= UpdateSearchWindows; // Remove the update handler after the update
		}
	}
}
