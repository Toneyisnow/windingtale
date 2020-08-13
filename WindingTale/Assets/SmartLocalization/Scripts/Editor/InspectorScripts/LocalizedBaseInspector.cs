//Voloshin Maxim
//Alexey Menshikh

using System;
using UnityEditor;
using UnityEngine;

namespace SmartLocalization.Editor
{
    public class LocalizedBaseInspector : UnityEditor.Editor 
    {
        private string[] _languageCodes;
        private int _selectedLanguageIdx;

        protected LocalizedBase Obj;

        private void Awake()
        {
            Obj = (LocalizedBase)target;
        }
        
        protected string CurrentLanguageCode
        {
            get
            {
                return _languageCodes[_selectedLanguageIdx];
            }
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();
            
            if (_languageCodes == null)
            {
                _languageCodes = LanguageManager.GetAvailableLanguageCodes();

                var defaultCode = LanguageManager.HasInstance 
                    ? LanguageManager.Instance.CurrentlyLoadedCulture.languageCode : LanguageManager.DefaultLanguage;
                
                _selectedLanguageIdx = Array.IndexOf(_languageCodes, defaultCode);                
            }
                  
            var buttonsPerLine = (int)(EditorGUIUtility.currentViewWidth - 20) / 40;

            _selectedLanguageIdx = GUILayout.SelectionGrid(_selectedLanguageIdx, _languageCodes, buttonsPerLine);
            
            EditorGUI.BeginChangeCheck();
            
            var selectedKey = LocalizedKeySelector.SelectKeyGUI(Obj.localizedKey, true, LocalizedObjectType.String);
            if (EditorGUI.EndChangeCheck() && selectedKey != null)
            {
                Undo.RecordObject(Obj, "Set localization key");
                Obj.localizedKey = selectedKey;
            }
        }
    }
}