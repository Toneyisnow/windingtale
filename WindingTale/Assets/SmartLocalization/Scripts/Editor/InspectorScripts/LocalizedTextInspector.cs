using UnityEditor;
using UnityEngine.UI;

namespace SmartLocalization.Editor
{	
	[CustomEditor(typeof(LocalizedText))]
	public class LocalizedTextInspector : LocalizedBaseInspector
	{		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			var newText = LanguageManager.GetTextValueInEditor(Obj.localizedKey, CurrentLanguageCode);
			Obj.GetComponent<Text>().text = newText;			
		}		
	}
}