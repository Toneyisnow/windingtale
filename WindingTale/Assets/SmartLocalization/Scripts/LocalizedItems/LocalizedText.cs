using UnityEngine;
using UnityEngine.UI;

namespace SmartLocalization.Editor
{
	[RequireComponent(typeof(Text))]
	public class LocalizedText : LocalizedBase 
	{
		private void Start () 
		{	
			//Subscribe to the change language event
			var languageManager = LanguageManager.Instance;
			languageManager.OnChangeLanguage += OnChangeLanguage;
			
			//Run the method one first time
			OnChangeLanguage(languageManager);
		}

		private void OnDestroy()
		{
			if(LanguageManager.HasInstance)
			{
				LanguageManager.Instance.OnChangeLanguage -= OnChangeLanguage;
			}
		}

		private void OnChangeLanguage(LanguageManager languageManager)
		{
			GetComponent<Text>().text = LanguageManager.Instance.GetTextValue(localizedKey);
		}
	}
}