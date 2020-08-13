using UnityEngine;

namespace SmartLocalization
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class LocalizedSprite : LocalizedBase 
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
			var spriteRenderer = GetComponent<SpriteRenderer>();			
			spriteRenderer.sprite = LanguageManager.Instance.GetSprite(localizedKey);
		}
	}
}