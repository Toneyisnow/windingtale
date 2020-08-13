
namespace SmartLocalization.Editor
{
	using NUnit.Framework;
	using System.Collections.Generic;

	[TestFixture]
	public class LanguageDictionaryHelperTests  
	{
		[Test]
		public void AddToStringDictionary_UniqueNames()
		{
			Dictionary<string,string> testDictionary = new Dictionary<string, string>();
			string tempKey = "TestKey";
			string tempValue = "temp";
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             LocalizedObject.GetFullKey(tempKey, LocalizedObjectType.String), 
			                                             tempValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             LocalizedObject.GetFullKey(tempKey, LocalizedObjectType.Audio), 
			                                             tempValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             LocalizedObject.GetFullKey(tempKey, LocalizedObjectType.GameObject), 
			                                             tempValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             LocalizedObject.GetFullKey(tempKey, LocalizedObjectType.Texture), 
			                                             tempValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             LocalizedObject.GetFullKey(tempKey, LocalizedObjectType.String), 
			                                             tempValue);
		}

		[Test]
		public void AddToObjectDictionary_UniqueNames()
		{
			var testDictionary = new Dictionary<string, LocalizedObject>();
			string tempKey = "TestKey";
			LocalizedObject tempStringValue = new LocalizedObject();

			tempStringValue.ObjectType = LocalizedObjectType.String;
			tempStringValue.TextValue = "TEMP";

			LocalizedObject tempAudioValue = new LocalizedObject(tempStringValue);
			tempAudioValue.ObjectType = LocalizedObjectType.Audio;

			LocalizedObject tempPrefabValue = new LocalizedObject(tempStringValue);
			tempPrefabValue.ObjectType = LocalizedObjectType.GameObject;

			LocalizedObject tempTextureValue = new LocalizedObject(tempStringValue);
			tempTextureValue.ObjectType = LocalizedObjectType.Texture;

			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             tempKey, 
			                                             tempStringValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             tempKey, 
			                                             new LocalizedObject(tempStringValue));
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             tempKey, 
			                                             tempAudioValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             tempKey, 
			                                             tempPrefabValue);
			LanguageDictionaryHelper.AddNewKeyPersistent(testDictionary, 
			                                             tempKey, 
			                                             tempTextureValue);
		}
	}
}