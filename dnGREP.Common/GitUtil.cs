using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dnGREP.Common
{
    public static class GitUtil
    {
        private static bool? isGitInstalled = null;
        public static bool IsGitInstalled
        {
            get
            {
                if (!isGitInstalled.HasValue)
                {
                    isGitInstalled = CheckGitInstalled();
                }
                return isGitInstalled.Value;
            }
        }

        private static bool CheckGitInstalled()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process proc = new();
            proc.StartInfo = startInfo;
            try
            {
                proc.Start();
                if (!proc.StandardOutput.EndOfStream)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    return !string.IsNullOrEmpty(line) && line.StartsWith("git ", StringComparison.CurrentCulture);
                }
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            return false;
        }

        public static Gitignore GetGitignore(string path)
        {
            List<string> list = [];

            if (IsGitInstalled)
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = "git",
                    Arguments = "status --short --ignored",
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process proc = new();
                proc.StartInfo = startInfo;
                try
                {
                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string? line = proc.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(line) && line.StartsWith("!! ", StringComparison.OrdinalIgnoreCase))
                            list.Add(line[3..].Trim('"'));
                    }
                }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
            }

            return new Gitignore(path, list);
        }

        public static Gitignore GetGitignore(List<string> paths)
        {
            Gitignore results = new();

            foreach (var path in paths)
            {
                results.Merge(GetGitignore(path));
            }

            return results;
        }
    }

    public class Gitignore
    {
        private const char gitSeparatorChar = '/';
        private const string gitSeparator = "/";
        private readonly HashSet<string> directories = [];
        private readonly HashSet<string> files = [];

        public Gitignore()
        {
        }

        public Gitignore(string path, List<string> list)
        {
            // add .git to the directories to ignore
            string git = Path.Combine(path, ".git");
            if (Directory.Exists(git))
            {
                directories.Add(git);
            }
            // add .gitignore to the files to ignore
            string gitignore = Path.Combine(path, ".gitignore");
            if (File.Exists(gitignore))
            {
                files.Add(gitignore);
            }

            foreach (var item in list.Where(s => !s.StartsWith("..", StringComparison.OrdinalIgnoreCase) &&
                    s.EndsWith(gitSeparator, StringComparison.CurrentCulture))
                .Select(s => Path.Combine(path, s.Replace(gitSeparatorChar, Path.DirectorySeparatorChar)
                    .TrimEnd(Path.DirectorySeparatorChar))))
            {
                directories.Add(item);
            }

            foreach (var item in list.Where(s => !s.StartsWith("..", StringComparison.OrdinalIgnoreCase) &&
                   !s.EndsWith(gitSeparator, StringComparison.CurrentCulture))
                .Select(s => Path.Combine(path, s.Replace(gitSeparatorChar, Path.DirectorySeparatorChar)
                    .TrimEnd(Path.DirectorySeparatorChar))))
            {
                files.Add(item);
            }
        }

        public void Merge(Gitignore other)
        {
            foreach(var item in other.Directories)
            {
                directories.Add(item);
            }

            foreach (var item in other.Files)
            {
                files.Add(item);
            }
        }

        public bool IsEmpty => directories.Count == 0 && files.Count == 0;
        public ISet<string> Directories => directories;
        public ISet<string> Files => files;
    }
}
