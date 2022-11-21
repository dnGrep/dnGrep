using System;
using System.Collections.Generic;
using System.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using Newtonsoft.Json;

namespace dnGREP.WPF
{
    public class ScriptTargetDefinition
    {
        public string Target { get; set; }

        public int Priority { get; set; } = int.MaxValue;

        public string DescriptionKey { get; set; } = null;

        public string ValueHintKey { get; set; } = null;

        public string Type { get; set; } = null;

        public bool AllowNullValue { get; set; } = false;

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
        public Type ValueType => ScriptCommandDefinition.FromSimpleName(Type);

        [JsonIgnore]
        public List<ScriptingCompletionData> CompletionData { get; } = new List<ScriptingCompletionData>();

        [JsonIgnore]
        public bool IsValueCommand => Values.Count > 0;

        public void Initialize()
        {
            if (Type == "bool")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = "False" });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = "True" });
            }
            else if (Type == "FileSearchType")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileSearchType.Regex.ToString(), DescriptionKey = "Main_PatternType_Regex" });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileSearchType.Asterisk.ToString(), DescriptionKey = "Main_PatternType_Asterisk" });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileSearchType.Everything.ToString(), DescriptionKey = "Main_PatternType_Everything" });
            }
            else if (Type == "FileSizeFilter")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileSizeFilter.No.ToString(), DescriptionKey = "Main_AllSizes" });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileSizeFilter.Yes.ToString(), DescriptionKey = "Main_SizeIs" });
            }
            else if (Type == "FileDateFilter")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileDateFilter.All.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileDateFilter.Created.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileDateFilter.Modified.ToString() });
            }
            else if (Type == "FileTimeRange")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileTimeRange.All.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileTimeRange.Dates.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileTimeRange.Hours.ToString() });
            }
            else if (Type == "SearchType")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = SearchType.Regex.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = SearchType.PlainText.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = SearchType.XPath.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 3, Value = SearchType.Soundex.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 4, Value = SearchType.Hex.ToString() });
            }
            else if (Type == "SortType")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = SortType.FileNameOnly.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = SortType.FileTypeAndName.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = SortType.FileNameDepthFirst.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 3, Value = SortType.FileNameBreadthFirst.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 4, Value = SortType.Size.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 5, Value = SortType.Date.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 6, Value = SortType.MatchCount.ToString() });
            }
            else if (Type == "ListSortDirection")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = ListSortDirection.Ascending.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = ListSortDirection.Descending.ToString() });
            }
            else if (Type == "ReportMode")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = ReportMode.FullLine.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = ReportMode.Groups.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = ReportMode.Matches.ToString() });
            }
            else if (Type == "UniqueScope")
            {
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = UniqueScope.PerFile.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = UniqueScope.Global.ToString() });
            }

            foreach (var value in Values)
            {
                CompletionData.Add(new ScriptingCompletionData(value));
            }
        }

        public override string ToString()
        {
            return $"{Priority} {Target} V:{Values.Count} {Type}";
        }
    }
}