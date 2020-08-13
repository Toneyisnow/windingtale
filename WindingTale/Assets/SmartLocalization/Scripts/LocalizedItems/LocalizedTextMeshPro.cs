using UnityEngine;
using TMPro;

namespace SmartLocalization.Editor
{
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class LocalizedTextMeshPro : LocalizedBase 
	{			
		private void Start () 
		{	
			//Subscribe to the change language event
			var languageManager = LanguageManager.Instance;
			LanguageManager.Instance.OnChangeLanguage += OnChangeLanguage;
			
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
		
		public void OnChangeLanguage(LanguageManager languageManager)
		{
			var text = LanguageManager.Instance.GetTextValue(localizedKey);
			GetComponent<TextMeshProUGUI>().text = text;
		}
	}
}