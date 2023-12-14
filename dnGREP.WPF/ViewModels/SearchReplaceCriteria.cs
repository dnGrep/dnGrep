using System;
using System.Collections.Generic;
using System.Linq;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class SearchReplaceCriteria(MainViewModel vm, PauseCancelToken pauseCancelToken)
    {
        public void AddSearchFiles(IEnumerable<string> files)
        {
            SearchInFiles = files;
        }

        public void AddReplaceFiles(IEnumerable<ReplaceDef> files)
        {
            ReplaceFiles = files;
        }

        public GrepOperation Operation { get; private set; } = vm.CurrentGrepOperation;
        public FileSizeFilter UseFileSizeFilter { get; private set; } = vm.UseFileSizeFilter;
        public int SizeFrom { get; private set; } = vm.SizeFrom;
        public int SizeTo { get; private set; } = vm.SizeTo;
        public FileDateFilter UseFileDateFilter { get; private set; } = vm.UseFileDateFilter;
        public FileTimeRange TypeOfTimeRangeFilter { get; private set; } = vm.TypeOfTimeRangeFilter;
        public DateTime? StartDate { get; private set; } = vm.StartDate;
        public DateTime? EndDate { get; private set; } = vm.EndDate;
        public int HoursFrom { get; private set; } = vm.HoursFrom;
        public int HoursTo { get; private set; } = vm.HoursTo;
        public FileSearchType TypeOfFileSearch { get; private set; } = vm.TypeOfFileSearch;
        public string FilePattern { get; private set; } = vm.FilePattern;
        public string FilePatternIgnore { get; private set; } = vm.FilePatternIgnore;
        public string IgnoreFilterFile { get; private set; } = vm.IgnoreFilter.FilePath;
        public bool UseGitIgnore { get; private set; } = vm.UseGitignore;
        public bool IncludeArchive { get; private set; } = vm.IncludeArchive;
        public bool IncludeBinary { get; private set; } = vm.IncludeBinary;
        public bool IncludeHidden { get; private set; } = vm.IncludeHidden;
        public bool IncludeSubfolder { get; private set; } = vm.IncludeSubfolder;
        public int MaxSubfolderDepth { get; private set; } = vm.MaxSubfolderDepth;
        public bool FollowSymlinks { get; private set; } = vm.FollowSymlinks;
        public int CodePage { get; private set; } = vm.CodePage;
        public SearchType TypeOfSearch { get; private set; } = vm.TypeOfSearch;
        public string SearchFor { get; private set; } = vm.SearchFor;
        public string ReplaceWith { get; private set; } = vm.ReplaceWith;
        public bool SkipRemoteCloudStorageFiles { get; private set; } = vm.SkipRemoteCloudStorageFiles;

        public IEnumerable<string> SearchInFiles { get; private set; } = Enumerable.Empty<string>();
        public IEnumerable<ReplaceDef> ReplaceFiles { get; private set; } = Enumerable.Empty<ReplaceDef>();
        public PauseCancelToken PauseCancelToken { get; private set; } = pauseCancelToken;
    }
}
