using GrygToolsUtils;
using UnityEditor;

namespace GrygTools.Audio
{
	[CustomPropertyDrawer(typeof(SfxCategoryAttribute))]
	public class SfxCategoryDrawer : SearchablePropertyDrawerBase<SfxCategorySettings>
	{
		protected override void OnSelect(SerializedProperty property, SfxCategorySettings obj)
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

		protected override bool IndexComparison(SerializedProperty property, SfxCategorySettings obj)
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
				foreach (SfxCategorySettings category in GrygAudioSettings.GetOrCreateSettings().SfxCategories)
				{
					optionsDictionary.Add(category.Id, category);
				}

				optionsList.Clear();
				optionsList.AddRange(optionsDictionary.Values);
				optionsList.Sort((a, b) => a.Id < b.Id ? -1 : 1);
				nameDictionary.Clear();
				
				foreach (SfxCategorySettings category in optionsList)
				{
					nameDictionary.Add(category.Id, category.Name);
				}
			}
		}
	}
}
