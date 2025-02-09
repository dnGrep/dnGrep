using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dnGREP.Common
{
    public class CompareFolders
    {
        public static DirectoryCompareResult GetFolderDifferences(
            string relativeToSourceFolder, string sourceFolder,
            string relativeToTargetFolder, string targetFolder,
            string namePattern, SearchOption searchOption)
        {
            DirectoryCompareResult result = new();

            DirectoryInfo source = new(sourceFolder);
            DirectoryInfo target = new(targetFolder);

            if (!source.Exists)
            {
                return result;
            }

            if (!target.Exists)
            {
                result.SourceOnly.AddRange(GetFiles(relativeToSourceFolder, source, namePattern, searchOption));
                return result;
            }

            var sourceFiles = GetFiles(relativeToSourceFolder, source, namePattern, searchOption);
            var targetFiles = GetFiles(relativeToTargetFolder, target, namePattern, searchOption);

            var sourceFileSet = new HashSet<RelativeFileInfo>(sourceFiles);
            var targetFileSet = new HashSet<RelativeFileInfo>(targetFiles);

            foreach (RelativeFileInfo sourceFile in sourceFiles)
            {
                if (targetFileSet.Contains(sourceFile))
                {
                    var targetFile = targetFileSet.First(f => f.RelativeName.Equals(sourceFile.RelativeName, StringComparison.OrdinalIgnoreCase));

                    if (Utils.HashSHA256(sourceFile.FileInfo.FullName) == Utils.HashSHA256(targetFile.FileInfo.FullName))
                    {
                        result.Identical.Add(sourceFile);
                    }
                    else
                    {
                        result.Conflicting.Add(sourceFile);
                    }
                }
                else
                {
                    result.SourceOnly.Add(sourceFile);
                }
            }

            return result;
        }


        private static List<RelativeFileInfo> GetFiles(string relativeToFolder,
            DirectoryInfo directory, string namePattern, SearchOption searchOption)
        {
            List<RelativeFileInfo> files = [];
            string[] patterns = namePattern.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
            foreach (string pattern in patterns)
            {
                foreach (FileInfo file in directory.GetFiles(pattern, searchOption))
                {
                    files.Add(new RelativeFileInfo(relativeToFolder, file));
                }
            }

            return files;
        }
    }

    public class DirectoryCompareResult
    {
        public List<RelativeFileInfo> Identical { get; private set; } = [];
        public List<RelativeFileInfo> Conflicting { get; private set; } = [];
        public List<RelativeFileInfo> SourceOnly { get; private set; } = [];

        public void Merge(DirectoryCompareResult other)
        {
            Identical.AddRange(other.Identical);
            Conflicting.AddRange(other.Conflicting);
            SourceOnly.AddRange(other.SourceOnly);
        }
    }

    public class RelativeFileInfo(string relativeToFolder, FileInfo fileInfo) : IEquatable<RelativeFileInfo>
    {
        public FileInfo FileInfo { get; private set; } = fileInfo;
        public string RelativeName { get; private set; } = Path.GetRelativePath(relativeToFolder, fileInfo.FullName);

        public override bool Equals(object? obj)
        {
            return Equals(obj as RelativeFileInfo);
        }
        public override int GetHashCode()
        {
            return RelativeName.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(RelativeFileInfo? other)
        {
            if (other == null)
            {
                return false;
            }

            return RelativeName.Equals(other.RelativeName, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return RelativeName;
        }
    }
}
