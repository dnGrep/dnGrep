using System;
using System.Collections.Generic;
using System.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using Newtonsoft.Json;

namespace dnGREP.WPF
{
    public class ScriptCommandDefinition
    {
        public string Command { get; set; }

        public int Priority { get; set; } = int.MaxValue;

        public string DescriptionKey { get; set; } = null;

        public string ValueHintKey { get; set; } = null;

        public string Type { get; set; } = null;

        public bool AllowNullValue { get; set; } = false;

        public List<ScriptTargetDefinition> Targets { get; } = new List<ScriptTargetDefinition>();

        public List<ScriptValueDefinition> Values { get; } = new List<ScriptValueDefinition>();

        [JsonIgnore]
        public string ValueHint
        {
            get
            {
                if (!string.IsNullOrEmpty(ValueHintKey))
                {
                    return TranslationSource.Instance[ValueHintKey];
                }
                return null;
            }
        }

        [JsonIgnore]
        public Type ValueType => FromSimpleName(Type);

        [JsonIgnore]
        public List<ScriptingCompletionData> CompletionData { get; } = new List<ScriptingCompletionData>();

        [JsonIgnore]
        public bool IsTargetCommand => Targets.Count > 0;

        public void Initialize()
        {
            if (Type == "bool")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = "False" });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = "True" });
            }

            foreach (var target in Targets)
            {
                target.Initialize();

                CompletionData.Add(new ScriptingCompletionData(target));
            }

            foreach (var value in Values)
            {
                CompletionData.Add(new ScriptingCompletionData(value));
            }
        }

        public static Type FromSimpleName(string typeName)
        {
            switch (typeName)
            {
                default:
                case null:
                    return null;
                case "string":
                    return typeof(string);
                case "bool":
                    return typeof(bool);
                case "int":
                    return typeof(int);
                case "double":
                    return typeof(double);
                case "DateTime":
                    return typeof(DateTime);
                case "FileSearchType":
                    return typeof(FileSearchType);
                case "FileSizeFilter":
                    return typeof(FileSizeFilter);
                case "FileDateFilter":
                    return typeof(FileDateFilter);
                case "FileTimeRange":
                    return typeof(FileTimeRange);
                case "SearchType":
                    return typeof(SearchType);
                case "SortType":
                    return typeof(SortType);
                case "ListSortDirection":
                    return typeof(ListSortDirection);
                case "ReportMode":
                    return typeof(ReportMode);
                case "UniqueScope":
                    return typeof(UniqueScope);
            }
        }

        public override string ToString()
        {
            return $"{Priority} {Command} T:{Targets.Count} V:{Values.Count} {Type}";
        }
    }
}
