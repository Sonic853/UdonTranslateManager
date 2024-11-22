﻿
using System.Text;
using Koyashiro.GenericDataContainer;
using UdonLab;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace Sonic853.Translate
{
    public class TranslatePo : UdonSharpBehaviour
    {
        [SerializeField] private TextAsset poFile;
        public string language;
        [TextArea(3, 10)]
        [HideInInspector]
        [SerializeField] private string[] msgids = new string[0];
        public string[] Msgids => msgids;
        [TextArea(3, 10)]
        [HideInInspector]
        [SerializeField] private string[] msgstrs = new string[0];
        public string[] Msgstrs => msgstrs;
        [HideInInspector]
        [SerializeField] public string _language;
        [HideInInspector] public string lastTranslator = "anonymous";
        bool dictionaryLoaded = false;
        DataDictionary dataDictionary = new DataDictionary();
        public void ReadPoFile(bool force = false)
        {
            if (poFile == null)
            {
                Debug.LogError("poFile is null");
                return;
            }
            if (!force && msgids.Length > 0 && msgstrs.Length > 0)
            {
                if (!dictionaryLoaded) LoadDictionary();
                return;
            }
            dictionaryLoaded = false;
            msgids = new string[0];
            msgstrs = new string[0];
            var lines = poFile.text.Split('\n');
            var msgidIndex = -1;
            var msgstrIndex = -1;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Debug.Log(line);
                if (line.StartsWith("msgid \""))
                {
                    // 将\\n替换为\n
                    // Debug.Log(line.Substring(7, line.Length - 14));
                    var text = line.Substring(7, line.LastIndexOf('"') - 7);
                    UdonArrayPlus.Add(ref msgids, Decode(text, 0, text.Length));
                    msgidIndex = msgids.Length - 1;
                    msgstrIndex = -1;
                }
                else if (line.StartsWith("msgstr \""))
                {
                    var text = line.Substring(8, line.LastIndexOf('"') - 8);
                    UdonArrayPlus.Add(ref msgstrs, Decode(text, 0, text.Length));
                    msgstrIndex = msgstrs.Length - 1;
                    msgidIndex = -1;
                }
                // 找到符合"Language: 的行，然后获取语言，同时去除后面的换行
                else if (line.StartsWith("\"Language: ") && msgstrIndex == 0)
                {
                    _language = line.Substring(11, line.LastIndexOf('"') - 11);
                    // 找到并去除\n
                    _language = _language.IndexOf("\\n") == -1 ? _language : _language.Substring(0, _language.LastIndexOf("\\n"));
                }
                // 找到符合"Last-Translator: 的行，然后获取语言，同时去除后面的换行
                else if (line.StartsWith("\"Last-Translator: ") && msgstrIndex == 0)
                {
                    lastTranslator = line.Substring(20, line.LastIndexOf('"') - 20);
                    // 找到并去除\n
                    lastTranslator = lastTranslator.IndexOf("\\n") == -1 ? lastTranslator : lastTranslator.Substring(0, lastTranslator.LastIndexOf("\\n"));
                    // 将<和>替换为＜和＞
                    lastTranslator = lastTranslator.Replace("<", "＜").Replace(">", "＞");
                }
                else if (line.StartsWith("\""))
                {
                    if (msgidIndex != -1 && msgidIndex != 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgids[msgidIndex] += Decode(text, 0, text.Length);
                    }
                    else if (msgstrIndex != -1 && msgstrIndex != 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgstrs[msgstrIndex] += Decode(text, 0, text.Length);
                    }
                }
            }
            if (msgids.Length != msgstrs.Length)
            {
                Debug.LogError("msgids.Length != msgstrs.Length");
                return;
            }
            LoadDictionary();
        }
        public void LoadDictionary()
        {
            dataDictionary.Clear();
            for (var i = 0; i < msgids.Length; i++)
            {
                dataDictionary.Add(msgids[i], msgstrs[i]);
            }
            dictionaryLoaded = true;
        }
        string Decode(string source, int startIndex, int count, string newLine = "\n")
        {
            var builder = new StringBuilder();
            for (var endIndex = startIndex + count; startIndex < endIndex; startIndex++)
            {
                var c = source[startIndex];
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (++startIndex < endIndex)
                {
                    c = source[startIndex];
                    switch (c)
                    {
                        case '\\':
                        case '"':
                            builder.Append(c);
                            continue;
                        case 't':
                            builder.Append('\t');
                            continue;
                        case 'r':
                            var index = startIndex;
                            if (++index + 1 < endIndex && source[index] == '\\' && source[++index] == 'n')
                                startIndex = index;
                            // "\r" and "\r\n" are both accepted as new line
                            builder.Append(newLine);
                            continue;
                        case 'n':
                            builder.Append(newLine);
                            continue;
                    }
                }

                // invalid escape sequence
                return builder.ToString();
            }

            return builder.ToString();
        }
        public string GetText(string text)
        {
            if (!dictionaryLoaded)
            {
                var msgidsLength = msgids.Length;
                for (int i = 0; i < msgidsLength; i++)
                {
                    if (msgids[i] == text)
                    {
                        return msgstrs[i];
                    }
                }
                return text;
            }
            if (!dataDictionary.ContainsKey(text)) { return text; }
            if (!dataDictionary.TryGetValue(text, out var value)) { return text; }
            return value.String;
        }
        public string GetOriginalText(string text)
        {
            return text;
        }
    }
}
