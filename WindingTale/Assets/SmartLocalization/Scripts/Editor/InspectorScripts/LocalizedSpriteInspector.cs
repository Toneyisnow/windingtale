using UnityEngine;
using UnityEditor;

namespace SmartLocalization.Editor
{
	/// <summary>
	/// Editor class for a localized Image
	/// </summary>
	[CustomEditor(typeof(LocalizedSprite))]
	public class LocalizedSpriteInspector : UnityEditor.Editor 
	{
		private string selectedKey = null;
		
		void Awake()
		{
			var textObject = ((LocalizedSprite)target);
			if(textObject != null)
			{
				selectedKey = textObject.localizedKey;
			}
		}
		
		/// <summary>
		/// Override of the OnInspectorGUI method
		/// </summary>
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			selectedKey = LocalizedKeySelector.SelectKeyGUI(selectedKey, true, LocalizedObjectType.Texture);
			
			if(!Application.isPlaying)
			{
				var obj = ((LocalizedSprite)target);
				obj.localizedKey = selectedKey;
			}
		}
	}
} 
