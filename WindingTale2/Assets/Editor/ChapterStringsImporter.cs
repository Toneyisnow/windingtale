using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;

namespace WindingTale.EditorTools
{
    /// <summary>
    /// Builds the ChapterStrings-NN localization tables from the CSV files sitting next to
    /// them. The CSVs are generated from Resources/Original/Strings/Maps/Chapter-NN.strings;
    /// this step is what turns them into the StringTable assets the game loads at runtime.
    ///
    /// A collection that does not exist yet is created first (with every project locale),
    /// so a brand new chapter only needs its CSV dropped in the folder.
    /// </summary>
    public static class ChapterStringsImporter
    {
        private const string LocalizationsFolder = "Assets/Resources/Strings/Localizations";

        [UnityEditor.MenuItem("WindingTale/Localization/Import All Chapter Strings CSV")]
        public static void ImportAll()
        {
            foreach (string path in Directory.GetFiles(LocalizationsFolder, "ChapterStrings-*.csv"))
            {
                Import(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [UnityEditor.MenuItem("Assets/WindingTale/Import Chapter Strings CSV", true)]
        private static bool ImportSelectedValidate()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && Regex.IsMatch(Path.GetFileName(path), @"^ChapterStrings-\d+\.csv$");
        }

        [UnityEditor.MenuItem("Assets/WindingTale/Import Chapter Strings CSV")]
        private static void ImportSelected()
        {
            Import(AssetDatabase.GetAssetPath(Selection.activeObject));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Import(string csvPath)
        {
            // "ChapterStrings-02.csv" -> collection "ChapterStrings-02", the same name
            // LocalizationManager.GetConversationString() asks for at runtime.
            string collectionName = Path.GetFileNameWithoutExtension(csvPath);

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(collectionName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(collectionName, LocalizationsFolder);
                Debug.Log($"Created string table collection {collectionName}");
            }

            using (StreamReader reader = new StreamReader(csvPath))
            {
                Csv.ImportInto(reader, collection);
            }

            Debug.Log($"Imported {csvPath} into {collectionName}");
        }
    }
}
