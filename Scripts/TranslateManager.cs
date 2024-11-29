
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
        public string currentLanguage = "zh-CN";
        [SerializeField] TranslatePo[] translates;
        [SerializeField] Text[] texts;
        string[] originalTexts = new string[0];
        [SerializeField] TMP_Text[] tMP_Texts;
        string[] originalTMP_Texts = new string[0];
        TranslatePo currentTranslatePo;
        bool loadedTranslate = false;
        public bool LoadedTranslate => loadedTranslate;
        [SerializeField] UdonBehaviour[] sendFunctions;
        public static TranslateManager Instance()
        {
            var obj = GameObject.Find("TranslateManager");
            if (obj == null)
            {
                Debug.LogError("TranslateManager not found");
                return null;
            }
            return (TranslateManager)obj.GetComponent(typeof(UdonBehaviour));
        }
        void Start()
        {
            LoadOriginalText();
            currentLanguage = VRCPlayerApi.GetCurrentLanguage() ?? currentLanguage;
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
                originalTexts.SetValue(texts[i] == null ? "" : texts[i].text, i);
            }
            for (int i = 0; i < tMP_Texts.Length; i++)
            {
                originalTMP_Texts.SetValue(tMP_Texts[i] == null ? "" : tMP_Texts[i].text, i);
            }
        }
        public void LoadTranslate() => LoadTranslate(currentLanguage);
        public void LoadTranslate(string _currentLanguage, bool loadUI = true)
        {
            Debug.Log($"LoadTranslate {_currentLanguage}");
            loadedTranslate = false;
            foreach (var translate in translates)
            {
                if (translate.Msgids.Length == 0 || translate.Msgstrs.Length == 0) translate.ReadPoFile();
                if (translate.language == _currentLanguage)
                {
                    currentTranslatePo = translate;
                    loadedTranslate = true;
                    if (loadUI) TranslateUI();
                    return;
                }
            }
            if (translates.Length > 0 && currentTranslatePo == null)
            {
                currentTranslatePo = translates[0];
                loadedTranslate = currentTranslatePo != null;
            }
            if (loadUI) TranslateUI();
        }
        public void TranslateUI()
        {
            if (originalTexts.Length == 0 || originalTMP_Texts.Length == 0) LoadOriginalText();
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].text = GetText(originalTexts[i]);
            }
            for (int i = 0; i < tMP_Texts.Length; i++)
            {
                tMP_Texts[i].text = GetText(originalTMP_Texts[i]);
            }
            foreach (var sendFunction in sendFunctions)
            {
                sendFunction.SendCustomEvent("TranslateUI");
            }
        }
        public string GetText(string text) => loadedTranslate ? currentTranslatePo.GetText(text) : text;
    }
}
