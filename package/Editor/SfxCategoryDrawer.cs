using GrygTools;
using GrygTools.Audio;
using GrygToolsUtils;
using System.Collections.Generic;
using UnityEditor;

namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxCategoryAttribute))]
	public class SfxCategoryDrawer : SearchablePropertyDrawerBase<SfxCategory>
	{
		protected override void OnSelect(SerializedProperty property, SfxCategory obj)
		{
			property.intValue = obj.Id;
		}
		
		protected override bool TypeCheck(SerializedProperty property, out string error)
		{
			error = null;
			if (property.type == "int")
			{
				return true;
			}
			error = "Sfx category searchable property must be used on an int value";
			return false;
		}

		protected override bool IndexComparison(SerializedProperty property, SfxCategory obj)
		{
			return obj.Id == property.intValue;
		}

		protected override string GetButtonText(SerializedProperty property)
		{
			if (!optionsDictionary.ContainsKey(property.intValue) && property.intValue == 0)
			{
				return "";
			}
			return $"{property.intValue} - {nameDictionary[property.intValue]}";
		}

		protected override void Populate()
		{
			if (nameDictionary.Count <= 0 ||  GrygAudioSettings.GetOrCreateSettings().SfxCategories.Count != optionsList.Count)
			{
				optionsDictionary.Clear();
				foreach (SfxCategory category in GrygAudioSettings.GetOrCreateSettings().SfxCategories)
				{
					optionsDictionary.Add(category.Id, category);
				}

				optionsList.Clear();
				optionsList.AddRange(optionsDictionary.Values);
				optionsList.Sort((a, b) => a.Id < b.Id ? -1 : 1);
				List<string> sfxCategoryNames = new List<string>();
				nameDictionary.Clear();
				foreach (SfxCategory category in optionsList)
				{
					nameDictionary.Add(category.Id, category.Name);
				}
			}
		}
	}
}
