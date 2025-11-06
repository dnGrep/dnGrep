using System;
using System.Collections.Generic;
using System.Linq;
using System.Unicode;

namespace dnGREP.Common
{
    public class StringMap
    {
        private readonly Dictionary<string, string> map = [];

        public Dictionary<string, string> Map => map;

        internal void Load(Dictionary<string, string> dictionary)
        {
            map.Clear();
            foreach (var item in dictionary)
            {
                map[item.Key] = item.Value;
            }
        }

        public void SaveToSettings(string key)
        {
            GrepSettings.Instance.Set(key, map);
        }

        //public string this[string key]
        //{
        //    get => map[key];
        //    set => map[key] = value;
        //}

        //public void Add(string key, string value)
        //{
        //    map.Add(key, value);
        //}

        //public bool ContainsKey(string key)
        //{
        //    return map.ContainsKey(key);
        //}

        //public bool TryGetValue(string key, out string value)
        //{
        //    if (map.TryGetValue(key, out var result) && result is not null)
        //    {
        //        value = result;
        //        return true;
        //    }
        //    value = string.Empty;
        //    return false;
        //}

        public static string Describe(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            if (text.Length == 1)
            {
                UnicodeCharInfo uci = UnicodeInfo.GetCharInfo(text[0]);
                return uci.Name;
            }
            else
            {
                return string.Join(", ",
                    text.ToCharArray().Select(c => UnicodeInfo.GetCharInfo(c).Name));
            }
        }

        public string ReplaceAllKeys(string text)
        {
            foreach (var item in map)
            {
                text = text.Replace(item.Key, item.Value, StringComparison.Ordinal);
            }

            return text;
        }

    }
}
