using System;
using System.Collections.Generic;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class SearchReplaceCriteria
    {
        public SearchReplaceCriteria(MainViewModel vm)
        {
            Operation = vm.CurrentGrepOperation;
            UseFileSizeFilter = vm.UseFileSizeFilter;
            SizeFrom = vm.SizeFrom;
            SizeTo = vm.SizeTo;
            UseFileDateFilter = vm.UseFileDateFilter;
            TypeOfTimeRangeFilter = vm.TypeOfTimeRangeFilter;
            StartDate = vm.StartDate;
            EndDate = vm.EndDate;
            HoursFrom = vm.HoursFrom;
            HoursTo = vm.HoursTo;
            TypeOfFileSearch = vm.TypeOfFileSearch;
            FilePattern = vm.FilePattern;
            FilePatternIgnore = vm.FilePatternIgnore;
            IncludeArchive = vm.IncludeArchive;
            IncludeBinary = vm.IncludeBinary;
            IncludeHidden = vm.IncludeHidden;
            IncludeSubfolder = vm.IncludeSubfolder;
            CodePage = vm.CodePage;
            TypeOfSearch = vm.TypeOfSearch;
            SearchFor = vm.SearchFor;
            ReplaceWith = vm.ReplaceWith;
        }

        public void AddSearchFiles(IEnumerable<string> files)
        {
            SearchInFiles = files;
        }

        public void AddReplaceFiles(IEnumerable<ReplaceDef> files)
        {
            ReplaceFiles = files;
        }

        public GrepOperation Operation { get; private set; }
        public FileSizeFilter UseFileSizeFilter { get; private set; }
        public int SizeFrom { get; private set; }
        public int SizeTo { get; private set; }
        public FileDateFilter UseFileDateFilter { get; private set; }
        public FileTimeRange TypeOfTimeRangeFilter { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public int HoursFrom { get; private set; }
        public int HoursTo { get; private set; }
        public FileSearchType TypeOfFileSearch { get; private set; }
        public string FilePattern { get; private set; }
        public string FilePatternIgnore { get; private set; }
        public bool IncludeArchive { get; private set; }
        public bool IncludeBinary { get; private set; }
        public bool IncludeHidden { get; private set; }
        public bool IncludeSubfolder { get; private set; }
        public int CodePage { get; private set; }
        public SearchType TypeOfSearch { get; private set; }
        public string SearchFor { get; private set; }
        public string ReplaceWith { get; private set; }

        public IEnumerable<string> SearchInFiles { get; private set; }
        public IEnumerable<ReplaceDef> ReplaceFiles { get; private set; }
    }
}
