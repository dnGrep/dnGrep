using System;
using System.Collections.Generic;
using System.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class ScriptTargetDefinition
    {
        public string Target { get; set; }

        public int Priority { get; set; } = int.MaxValue;

        public string Description { get; set; } = null;

        public string ValueHint { get; set; } = null;

        public Type ValueType { get; set; } = null;

        public bool AllowNullValue { get; set; } = false;

        public List<ScriptValueDefinition> Values { get; } = new List<ScriptValueDefinition>();

        public List<ScriptingCompletionData> CompletionData { get; } = new List<ScriptingCompletionData>();

        public bool IsValueCommand => Values.Count > 0;

        public void Initialize()
        {
            if (ValueType == typeof(bool))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = "False" });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = "True" });
            }
            else if (ValueType == typeof(FileSearchType))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileSearchType.Regex.ToString(), Description = "Main_PatternType_Regex" });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileSearchType.Asterisk.ToString(), Description = "Main_PatternType_Asterisk" });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileSearchType.Everything.ToString(), Description = "Main_PatternType_Everything" });
            }
            else if (ValueType == typeof(FileSizeFilter))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileSizeFilter.No.ToString(), Description = "Main_AllSizes" });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileSizeFilter.Yes.ToString(), Description = "Main_SizeIs" });
            }
            else if (ValueType == typeof(FileDateFilter))
            {
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileDateFilter.All.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileDateFilter.Created.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileDateFilter.Modified.ToString() });
            }
            else if (ValueType == typeof(FileTimeRange))
            {
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = FileTimeRange.All.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = FileTimeRange.Dates.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = FileTimeRange.Hours.ToString() });
            }
            else if (ValueType == typeof(SearchType))
            {
                Values.Add(new ScriptValueDefinition { Priority = 3, Value = SearchType.Regex.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 4, Value = SearchType.PlainText.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = SearchType.XPath.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = SearchType.Soundex.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = SearchType.Hex.ToString() });
            }
            else if (ValueType == typeof(SortType))
            {
                Values.Add(new ScriptValueDefinition { Priority = 5, Value = SortType.FileNameOnly.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 4, Value = SortType.FileTypeAndName.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 6, Value = SortType.FileNameDepthFirst.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 3, Value = SortType.FileNameBreadthFirst.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = SortType.Size.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = SortType.Date.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = SortType.MatchCount.ToString() });
            }
            else if (ValueType == typeof(ListSortDirection))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = ListSortDirection.Ascending.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = ListSortDirection.Descending.ToString() });
            }
            else if (ValueType == typeof(ReportMode))
            {
                Values.Add(new ScriptValueDefinition { Priority = 2, Value = ReportMode.FullLine.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = ReportMode.Groups.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = ReportMode.Matches.ToString() });
            }
            else if (ValueType == typeof(UniqueScope))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = UniqueScope.PerFile.ToString() });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = UniqueScope.Global.ToString() });
            }

            foreach (var value in Values)
            {
                CompletionData.Add(new ScriptingCompletionData(value));
            }
        }

        public override string ToString()
        {
            return $"{Priority} {Target} V:{Values.Count} {ValueType?.Name}";
        }
    }
}