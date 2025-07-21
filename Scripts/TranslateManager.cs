
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Sonic853.Translate
{
    public class TranslateManager : UdonSharpBehaviour
    {
        public bool InitAtStart = true;
        public string currentLanguage = "en";
        [SerializeField] TranslatePo[] translates;
        [SerializeField] Text[] texts;
        public Text[] Texts => texts;
        string[] originalTexts = new string[0];
        [SerializeField] TMP_Text[] tMP_Texts;
        public TMP_Text[] TMP_Texts => tMP_Texts;
        string[] originalTMP_Texts = new string[0];
        TranslatePo currentTranslatePo;
        bool loadedTranslate = false;
        public bool LoadedTranslate => loadedTranslate;
        [SerializeField] UdonBehaviour[] sendFunctions;
        [SerializeField] bool enableOnLanguageChanged = true;
        public static TranslateManager Instance() => Instance("TranslateManager");
        public static TranslateManager InstanceGlobal() => Instance("TranslateManager.Global");
        public static TranslateManager Instance(string gameObjectName = "TranslateManager")
        {
            var obj = GameObject.Find(gameObjectName);
            if (obj == null)
            {
                Debug.LogError($"TranslateManager: {gameObjectName} not found");
                return null;
            }
            return (TranslateManager)obj.GetComponent(typeof(UdonBehaviour));
        }
        void Start()
        {
            if (InitAtStart) Init();
        }
        public void Init()
        {
            LoadOriginalText();
            currentLanguage = (VRCPlayerApi.GetCurrentLanguage() ?? currentLanguage).Replace("_", "-").ToLower();
            LoadTranslate(currentLanguage);
            TranslateUI();
        }
        void LoadOriginalText()
        {
            if (originalTexts.Length > 0 || originalTMP_Texts.Length > 0) { return; }
            originalTexts = new string[texts.Length];
            originalTMP_Texts = new string[tMP_Texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                // originalTexts.SetValue(texts[i] == null ? "" : texts[i].text, i);
                originalTexts[i] = texts[i] == null ? "" : texts[i].text;
            }
            for (int i = 0; i < tMP_Texts.Length; i++)
            {
                // originalTMP_Texts.SetValue(tMP_Texts[i] == null ? "" : tMP_Texts[i].text, i);
                originalTMP_Texts[i] = tMP_Texts[i] == null ? "" : tMP_Texts[i].text;
            }
        }
        public void AddText(Text text, bool loadUI = true)
        {
            if (originalTexts.Length == 0 && originalTMP_Texts.Length == 0) LoadOriginalText();
            var oldLength = texts.Length;
            Add(ref texts, text, false);
            if (oldLength == texts.Length) { return; }
            Add(ref originalTexts, text.text);
            if (loadUI) TranslateUI();
        }
        public void AddTMP_Text(TMP_Text tMP_Text, bool loadUI = true)
        {
            if (originalTexts.Length == 0 && originalTMP_Texts.Length == 0) LoadOriginalText();
            var oldLength = tMP_Texts.Length;
            Add(ref tMP_Texts, tMP_Text, false);
            if (oldLength == tMP_Texts.Length) { return; }
            Add(ref originalTMP_Texts, tMP_Text.text);
            if (loadUI) TranslateUI();
        }
        public void LoadTranslate() => LoadTranslate(currentLanguage);
        public void LoadTranslate(string _currentLanguage, bool loadUI = true)
        {
            Debug.Log($"LoadTranslate {_currentLanguage}");
            loadedTranslate = false;
            foreach (var translate in translates)
            {
                if (translate.language.Replace("_", "-").ToLower() != _currentLanguage) { continue; }
                LoadTranslate(translate, loadUI);
                loadedTranslate = true;
                return;
            }
            if (translates.Length > 0 && currentTranslatePo == null)
            {
                LoadTranslate(translates[0], loadUI);
                loadedTranslate = currentTranslatePo != null;
            }
            else if (loadUI) TranslateUI();
        }
        public void LoadTranslate(TranslatePo translate, bool loadUI = true)
        {
            Debug.Log($"LoadTranslate File: {translate.language}");
            if (currentTranslatePo == translate) { return; }
            currentTranslatePo = translate;
            if (currentTranslatePo.Msgids.Length == 0 || currentTranslatePo.Msgstrs.Length == 0) currentTranslatePo.ReadPoFile();
            currentLanguage = currentTranslatePo.language.Replace("_", "-").ToLower();
            if (loadUI) TranslateUI();
        }
        public void LoadTranslate0() => LoadTranslate(translates[0]);
        public void LoadTranslate1() => LoadTranslate(translates[1]);
        public void LoadTranslate2() => LoadTranslate(translates[2]);
        public void LoadTranslate3() => LoadTranslate(translates[3]);
        public void TranslateUI()
        {
            if (originalTexts.Length == 0 && originalTMP_Texts.Length == 0) LoadOriginalText();
            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null) { continue; }
                text.text = GetText(originalTexts[i]);
            }
            for (int i = 0; i < tMP_Texts.Length; i++)
            {
                var text = tMP_Texts[i];
                if (text == null) { continue; }
                text.text = GetText(originalTMP_Texts[i]);
            }
            foreach (var sendFunction in sendFunctions)
            {
                sendFunction.SendCustomEvent("TranslateUI");
            }
        }
        public override void OnLanguageChanged(string language)
        {
            if (!enableOnLanguageChanged) { return; }
            // currentLanguage = VRCPlayerApi.GetCurrentLanguage() ?? currentLanguage;
            LoadTranslate(language.Replace("_", "-").ToLower());
        }
        public string GetText(string text) => loadedTranslate ? currentTranslatePo.GetText(text) : text;
        #region Array
        
        static T[] Add<T>(T[] _array, T _value, bool duplicates = true)
        {
            if (!duplicates)
            {
                if (IndexOf(_array, _value) != -1)
                    return _array;
            }
            var length = _array.Length;
            var _newArray = new T[length + 1];
            Array.Copy(_array, _newArray, length);
            // _newArray.SetValue(_value, length);
            _newArray[length] = _value;
            return _newArray;
        }
        static T[] Add<T>(ref T[] _array, T _value, bool duplicates = true) => _array = Add(_array, _value, duplicates);
        static int IndexOf<T>(T[] _array, T _value) => Array.IndexOf(_array, _value);
        #endregion
    }
}
