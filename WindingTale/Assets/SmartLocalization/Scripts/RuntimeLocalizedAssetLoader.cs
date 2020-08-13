using System;
using UnityEngine;

namespace SmartLocalization
{
	internal class RuntimeLocalizedAssetLoader : ILocalizedAssetLoader 
	{
		private static readonly Type GameObjectType = typeof(GameObject);
		private static readonly Type AudioClipType  = typeof(AudioClip);
		private static readonly Type SpriteType     = typeof(Sprite);
		private static readonly Type TextureType    = typeof(Texture);
		private static readonly Type TextAssetType  = typeof(TextAsset);
		private static readonly Type FontType       = typeof(Font);
	
		public T LoadAsset<T>(string assetKey, string languageCode) where T : UnityEngine.Object
		{
			var loadedObject = Resources.Load<T>(GetAssetFolderPath(typeof(T), languageCode) + "/" + assetKey);
			
			return loadedObject != null ? loadedObject : default(T);
		}
		
		string GetAssetFolderPath(Type assetType, string languageCode)
		{			
			if(assetType == GameObjectType)
			{
				return LanguageRuntimeData.PrefabsFolderPath(languageCode);
			}
			else if(assetType == AudioClipType)
			{
				return LanguageRuntimeData.AudioFilesFolderPath(languageCode);
			}
			else if(assetType == SpriteType || assetType == TextureType)
			{
				return LanguageRuntimeData.TexturesFolderPath(languageCode);
			}
			else if(assetType == TextAssetType)
			{
				return LanguageRuntimeData.TextAssetsFolderPath(languageCode);
			}
			else if(assetType == FontType)
			{
				return LanguageRuntimeData.FontsFolderPath(languageCode);
			}			
			
			return string.Empty;
		}
	}
}