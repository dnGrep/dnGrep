using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Alphaleonis.Win32.Filesystem;

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
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process proc = new Process())
            {
                proc.StartInfo = startInfo;
                try
                {
                    proc.Start();
                    if (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        return !string.IsNullOrEmpty(line) && line.StartsWith("git ", StringComparison.CurrentCulture);
                    }
                }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
            }
            return false;
        }

        public static Gitignore GetGitignore(string path)
        {
            List<string> list = new List<string>();

            if (IsGitInstalled)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --short --ignored",
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = startInfo;
                    try
                    {
                        proc.Start();
                        while (!proc.StandardOutput.EndOfStream)
                        {
                            string line = proc.StandardOutput.ReadLine();
                            if (line.StartsWith("!! ", StringComparison.OrdinalIgnoreCase))
                                list.Add(line.Substring(3).Trim('"'));
                        }
                    }
                    catch (InvalidOperationException) { }
                    catch (Win32Exception) { }
                }
            }

            return new Gitignore(path, list);
        }

        public static Gitignore GetGitignore(IList<string> paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            Gitignore results = new Gitignore();

            foreach (var path in paths)
            {
                results.Merge(GetGitignore(path));
            }

            return results;
        }
    }

    public class Gitignore
    {
        private const string gitSeparator = "/";
        private HashSet<string> directories = new HashSet<string>();
        private HashSet<string> files = new HashSet<string>();

        public Gitignore()
        {
        }

        public Gitignore(string path, IList<string> list)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (list == null)
                throw new ArgumentNullException(nameof(list));

            foreach (var item in list.Where(s => !s.StartsWith("..", StringComparison.OrdinalIgnoreCase) &&
                    s.EndsWith(gitSeparator, StringComparison.CurrentCulture))
                .Select(s => Path.Combine(path, s.Replace(gitSeparator, Path.DirectorySeparator)
                    .TrimEnd(Path.DirectorySeparatorChar))))
            {
                if (!directories.Contains(item))
                    directories.Add(item);
            }

            foreach (var item in list.Where(s => !s.StartsWith("..", StringComparison.OrdinalIgnoreCase) &&
                   !s.EndsWith(gitSeparator, StringComparison.CurrentCulture))
                .Select(s => Path.Combine(path, s.Replace(gitSeparator, Path.DirectorySeparator)
                    .TrimEnd(Path.DirectorySeparatorChar))))
            {
                if (!files.Contains(item))
                    files.Add(item);
            }
        }

        public void Merge(Gitignore other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            foreach(var item in other.Directories)
            {
                if (!directories.Contains(item))
                    directories.Add(item);
            }

            foreach (var item in other.Files)
            {
                if (!files.Contains(item))
                    files.Add(item);
            }
        }

        public bool IsEmpty => directories.Count == 0 && files.Count == 0;
        public ISet<string> Directories => directories;
        public ISet<string> Files => files;
    }
}
