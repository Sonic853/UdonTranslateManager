
using System.Text;
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
        public void ReadPoFile() => ReadPoFile(false);
        public void ReadPoFileForce() => ReadPoFile(true);
        public void ReadPoFile(bool force = false) => ReadPoFile(
            poFile,
            ref dataDictionary,
            ref msgids,
            ref msgstrs,
            ref _language,
            ref lastTranslator,
            ref dictionaryLoaded,
            force
        );
        static void ReadPoFile(
            TextAsset poFile,
            ref DataDictionary dataDictionary,
            ref string[] msgids,
            ref string[] msgstrs,
            ref string _language,
            ref string lastTranslator,
            ref bool dictionaryLoaded,
            bool force = false
        )
        {
            if (poFile == null)
            {
                Debug.LogError("poFile is null");
                return;
            }
            if (!force && msgids.Length > 0 && msgstrs.Length > 0)
            {
                if (!dictionaryLoaded) LoadDictionary(msgids, msgstrs, ref dataDictionary, ref dictionaryLoaded);
                return;
            }
            dataDictionary.Clear();
            dictionaryLoaded = false;
            msgids = new string[0];
            msgstrs = new string[0];
            var lines = poFile.text.Split('\n');
            var msgidIndex = -1;
            var msgstrIndex = -1;
            var msgid = "";
            var lastmsgid = "";
            var msgstr = "";
            // for (var i = 0; i < lines.Length; i++)
            foreach (var line in lines)
            {
                // var line = lines[i];
                // Debug.Log(line);
                if (line.StartsWith("msgid \""))
                {
                    // 将\\n替换为\n
                    // Debug.Log(line.Substring(7, line.Length - 14));
                    var text = line.Substring(7, line.LastIndexOf('"') - 7);
                    // UdonArrayPlus.Add(ref msgids, Decode(text, 0, text.Length));
                    msgid = Decode(text, 0, text.Length);
                    lastmsgid = msgstr = null;
                    msgidIndex++;
                    // msgidIndex = msgids.Length - 1;
                    // msgstrIndex = -1;
                }
                else if (line.StartsWith("msgstr \""))
                {
                    var text = line.Substring(8, line.LastIndexOf('"') - 8);
                    // UdonArrayPlus.Add(ref msgstrs, Decode(text, 0, text.Length));
                    msgstr = Decode(text, 0, text.Length);
                    if (dataDictionary.ContainsKey(msgid))
                    {
                        dataDictionary.SetValue(msgid, msgstr);
                    }
                    else
                    {
                        dataDictionary.Add(msgid, msgstr);
                    }
                    lastmsgid = msgid;
                    msgid = null;
                    msgstrIndex++;
                    // msgstrIndex = msgstrs.Length - 1;
                    // msgidIndex = -1;
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
                    if (msgid != null || msgid == "" && msgstr == null && msgidIndex != 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgid += Decode(text, 0, text.Length);
                        // msgids[msgidIndex] += Decode(text, 0, text.Length);
                    }
                    else if (msgstr != null || msgstr == "" && msgid == null && msgstrIndex != 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgstr += Decode(text, 0, text.Length);
                        if (!string.IsNullOrEmpty(lastmsgid) && dataDictionary.ContainsKey(lastmsgid))
                        {
                            dataDictionary.SetValue(lastmsgid, msgstr);
                        }
                        // msgstrs[msgstrIndex] += Decode(text, 0, text.Length);
                    }
                }
            }
            dictionaryLoaded = true;
            LoadArray(ref msgids, ref msgstrs, dataDictionary);
            if (msgids.Length != msgstrs.Length)
            {
                Debug.LogError("msgids.Length != msgstrs.Length");
                return;
            }
        }
        public void LoadDictionary() => LoadDictionary(msgids, msgstrs, ref dataDictionary, ref dictionaryLoaded);
        static void LoadDictionary(string[] msgids, string[] msgstrs, ref DataDictionary dataDictionary, ref bool dictionaryLoaded)
        {
            dataDictionary.Clear();
            for (var i = 0; i < msgids.Length; i++)
            {
                dataDictionary.Add(msgids[i], msgstrs[i]);
            }
            dictionaryLoaded = true;
        }
        public void LoadArray() => LoadArray(ref msgids, ref msgstrs, dataDictionary);
        static void LoadArray(ref string[] msgids, ref string[] msgstrs, DataDictionary dataDictionary)
        {
            msgids = new string[dataDictionary.Count];
            msgstrs = new string[dataDictionary.Count];
            var msgidKeys = dataDictionary.GetKeys();
            for (var i = 0; i < msgidKeys.Count; i++)
            {
                var msgidKey = msgidKeys[i];
                msgids[i] = msgidKey.String;
                if (!dataDictionary.TryGetValue(msgidKey, out var value)) { continue; }
                msgstrs[i] = value.String;
            }
        }
        static string Decode(string source, int startIndex, int count, string newLine = "\n")
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
            // if (!dictionaryLoaded)
            // {
            //     var msgidsLength = msgids.Length;
            //     for (int i = 0; i < msgidsLength; i++)
            //     {
            //         if (msgids[i] == text)
            //         {
            //             return msgstrs[i];
            //         }
            //     }
            //     return text;
            // }
            if (!dataDictionary.TryGetValue(text, out var value)) { return text; }
            return value.String;
        }
        public string GetOriginalText(string text)
        {
            if (!dataDictionary.ContainsValue(text)) { return text; }
            var keys = dataDictionary.GetKeys();
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!dataDictionary.TryGetValue(key, out var value)
                || value.TokenType != TokenType.String
                || value.String != text) { continue; }
                return key.String;
            }
            return text;
        }
    }
}
