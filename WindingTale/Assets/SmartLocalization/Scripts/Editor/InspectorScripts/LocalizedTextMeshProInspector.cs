using UnityEditor;
using TMPro;

namespace SmartLocalization.Editor
{
	[CustomEditor(typeof(LocalizedTextMeshPro))]
	public class LocalizedTextMeshProInspector : LocalizedBaseInspector
	{		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
						
			var newText = LanguageManager.GetTextValueInEditor(Obj.localizedKey, CurrentLanguageCode);
			Obj.GetComponent<TextMeshProUGUI>().text = newText;			
		}	
	}
}