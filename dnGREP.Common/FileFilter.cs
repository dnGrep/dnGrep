using System;

namespace dnGREP.Common
{
    public class FileFilter
    {
        public FileFilter()
        {
            Path = ".";
            NamePatternToInclude = "*.*";
            NamePatternToExclude = string.Empty;
            IgnoreFilterFile = string.Empty;
            MaxSubfolderDepth = -1;
        }

        public static FileFilter Default => new();

        /// <summary>
        /// Stores the file filter parameters 
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <param name="namePatternToInclude">File name pattern. (E.g. *.cs) or regex to include. If null returns empty array. If empty string returns all files.</param>
        /// <param name="namePatternToExclude">File name pattern. (E.g. *.cs) or regex to exclude. If null or empty is ignored.</param>
        /// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
        /// <param name="useGitignore">Use .gitignore file, if present</param>
        /// <param name="useEverything">Use Everything index to search</param>
        /// <param name="includeSubfolders">Include sub folders</param>
        /// <param name="maxSbufolderDepth">Maximum depth of search, where 1 is top level only and -1 is all directories</param>
        /// <param name="includeHidden">Include hidden folders</param>
        /// <param name="includeBinary">Include binary files</param>
        /// <param name="includeArchive">Include search in archives</param>
        /// <param name="followSymlinks">Include search in symbolic links</param>
        /// <param name="sizeFrom">Size in KB</param>
        /// <param name="sizeTo">Size in KB</param>
        /// <param name="dateFilter">Filter by file modified or created date time range</param>
        /// <param name="startTime">start of time range</param>
        /// <param name="endTime">end of time range</param>
        /// <param name="skipRemoteCloudStorageFiles">true to skip cloud  directories</param>
        /// <param name="ignoreFilterFile">File path to a dnGrep ignore file</param>
        public FileFilter(string path, string namePatternToInclude, string namePatternToExclude, bool isRegex,
            bool useGitignore, bool useEverything, bool includeSubfolders,
            int maxSbufolderDepth, bool includeHidden, bool includeBinary, bool includeArchive,
            bool followSymlinks, int sizeFrom, int sizeTo, FileDateFilter dateFilter,
            DateTime? startTime, DateTime? endTime, bool skipRemoteCloudStorageFiles = true,
            string ignoreFilterFile = "")
        {
            Path = path;
            NamePatternToInclude = namePatternToInclude;
            NamePatternToExclude = namePatternToExclude;
            IsRegex = isRegex;
            UseGitIgnore = useGitignore;
            UseEverything = useEverything;
            IncludeSubfolders = includeSubfolders;
            MaxSubfolderDepth = maxSbufolderDepth;
            IncludeHidden = includeHidden;
            IncludeBinary = includeBinary;
            IncludeArchive = includeArchive;
            FollowSymlinks = followSymlinks;
            SizeFrom = sizeFrom;
            SizeTo = sizeTo;
            DateFilter = dateFilter;
            StartTime = startTime;
            EndTime = endTime;
            SkipRemoteCloudStorageFiles = skipRemoteCloudStorageFiles;
            IgnoreFilterFile = ignoreFilterFile;
        }

        public FileFilter ChangePath(string path)
        {
            var f = Clone();
            f.Path = path;
            return f;
        }

        public FileFilter ChangeIncludePattern(string includePattern)
        {
            var f = Clone();
            f.NamePatternToInclude = includePattern;
            return f;
        }

        public FileFilter Clone()
        {
            return new FileFilter(
                Path,
                NamePatternToInclude,
                NamePatternToExclude,
                IsRegex,
                UseGitIgnore,
                UseEverything,
                IncludeSubfolders,
                MaxSubfolderDepth,
                IncludeHidden,
                IncludeBinary,
                IncludeArchive,
                FollowSymlinks,
                SizeFrom,
                SizeTo,
                DateFilter,
                StartTime,
                EndTime,
                SkipRemoteCloudStorageFiles,
                IgnoreFilterFile
                );
        }

        public string Path { get; private set; }
        public string NamePatternToInclude { get; private set; }
        public string NamePatternToExclude { get; private set; }
        public bool UseGitIgnore { get; private set; }
        public string IgnoreFilterFile { get; private set; }
        public bool IsRegex { get; private set; }
        public bool UseEverything { get; private set; }
        public bool IncludeSubfolders { get; private set; }
        public int MaxSubfolderDepth { get; private set; }
        public bool IncludeHidden { get; private set; }
        public bool IncludeBinary { get; private set; }
        public bool IncludeArchive { get; private set; }
        public bool FollowSymlinks { get; private set; }
        public int SizeFrom { get; private set; }
        public int SizeTo { get; private set; }
        public FileDateFilter DateFilter { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public bool SkipRemoteCloudStorageFiles { get; private set; }
    }
}
