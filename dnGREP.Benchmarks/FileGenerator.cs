using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace dnGREP.Benchmarks
{
    /// <summary>
    /// Writes a set of large test files to a folder, so dnGrep can be tried on them outside the benchmarks.
    /// File sizes can vary within a range, and each file can be placed in its own zip archive.
    /// </summary>
    public static class FileGenerator
    {
        private const double MB = 1024.0 * 1024.0;

        public static int Run(string[] args)
        {
            Dictionary<string, string> options = PeakMemoryRunner.ParseArgs(args);
            if (options.ContainsKey("help"))
            {
                PrintUsage();
                return 0;
            }

            if (!options.TryGetValue("out", out string? outFolder) || string.IsNullOrWhiteSpace(outFolder))
            {
                Console.Error.WriteLine("The --out folder is required.");
                Console.Error.WriteLine();
                PrintUsage();
                return 1;
            }

            int files = PeakMemoryRunner.GetInt(options, "files", 8);
            int length = PeakMemoryRunner.GetInt(options, "length", 80);
            LineEnding eol = PeakMemoryRunner.GetEnum(options, "eol", LineEnding.CRLF);
            MatchDensity density = PeakMemoryRunner.GetEnum(options, "density", MatchDensity.Sparse);
            int mb = PeakMemoryRunner.GetInt(options, "mb", TestData.DefaultSizeMB);
            int minMB = PeakMemoryRunner.GetInt(options, "min-mb", mb);
            int maxMB = PeakMemoryRunner.GetInt(options, "max-mb", mb);
            int sizeSteps = PeakMemoryRunner.GetInt(options, "sizes", 4);
            bool zip = options.ContainsKey("zip");
            string prefix = options.GetValueOrDefault("prefix", "file");

            if (files < 1 || minMB < 1 || maxMB < minMB)
            {
                Console.Error.WriteLine("Use --files of at least 1 and 1 <= --min-mb <= --max-mb.");
                return 1;
            }

            int steps = minMB == maxMB ? 1 : Math.Clamp(sizeSteps, 1, files);
            int SizeFor(int index) => steps == 1
                ? minMB
                : minMB + (int)((long)(maxMB - minMB) * (index % steps) / (steps - 1));

            string folder = Path.GetFullPath(outFolder);
            Directory.CreateDirectory(folder);

            long totalMB = 0;
            int largestMB = 0;
            for (int i = 0; i < files; i++)
            {
                totalMB += SizeFor(i);
                largestMB = Math.Max(largestMB, SizeFor(i));
            }

            // a zip only needs room for the largest uncompressed file while it is being compressed
            long neededMB = zip ? largestMB : totalMB;
            if (!HasFreeSpace(folder, neededMB, out double freeGB))
            {
                Console.Error.WriteLine($"Not enough free disk space in {folder}: about {neededMB / 1024.0:N1} GB needed, {freeGB:N1} GB free.");
                return 1;
            }

            Console.WriteLine($"Writing {files} file(s) to {folder}");
            Console.WriteLine($"  size {(minMB == maxMB ? $"{minMB} MB" : $"{minMB}-{maxMB} MB in {steps} sizes")}, total about {totalMB / 1024.0:N1} GB before compression, " +
                $"line length {(length <= 0 ? "single line" : length.ToString(CultureInfo.InvariantCulture))}, {eol}, density {density}{(zip ? ", each in a zip" : string.Empty)}");

            string extension = zip ? ".zip" : ".txt";
            Dictionary<int, (string Path, long Matches)> templates = [];
            List<string> manifest = ["file,sizeBytes,matches,source"];
            long totalMatches = 0, totalBytes = 0;

            for (int i = 0; i < files; i++)
            {
                int sizeMB = SizeFor(i);
                string name = $"{prefix}{i:D3}";
                string target = Path.Combine(folder, name + extension);
                long matches;
                string source;

                // files of the same size have the same content, so copy rather than generate again
                if (templates.TryGetValue(sizeMB, out var template))
                {
                    File.Copy(template.Path, target, true);
                    matches = template.Matches;
                    source = "copied";
                }
                else
                {
                    matches = zip
                        ? GenerateZip(target, name + ".txt", sizeMB, length, eol, density)
                        : TestData.GenerateFile(target, sizeMB, length, eol, density);
                    templates[sizeMB] = (target, matches);
                    source = "generated";
                }

                long bytes = new FileInfo(target).Length;
                totalMatches += matches;
                totalBytes += bytes;
                manifest.Add(string.Join(',', Path.GetFileName(target), bytes.ToString(CultureInfo.InvariantCulture),
                    matches.ToString(CultureInfo.InvariantCulture), source));
                Console.WriteLine($"  [{i + 1}/{files}] {Path.GetFileName(target)}  {bytes / MB,8:N1} MB  {matches,10:N0} matches  ({source})");
            }

            string manifestPath = Path.Combine(folder, "manifest.csv");
            File.WriteAllLines(manifestPath, manifest, new UTF8Encoding(false));

            Console.WriteLine();
            Console.WriteLine($"Done: {files} file(s), {totalBytes / MB / 1024:N2} GB on disk, {totalMatches:N0} matches of \"{TestData.SearchText}\" expected in total.");
            Console.WriteLine($"The per-file counts are in {manifestPath}");
            return 0;
        }

        private static long GenerateZip(string zipPath, string entryName, int sizeMB, int length, LineEnding eol, MatchDensity density)
        {
            string temp = zipPath + ".tmp";
            try
            {
                long matches = TestData.GenerateFile(temp, sizeMB, length, eol, density);

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(temp, entryName, CompressionLevel.Fastest);
                return matches;
            }
            finally
            {
                File.Delete(temp);
            }
        }

        private static bool HasFreeSpace(string folder, long neededMB, out double freeGB)
        {
            freeGB = double.NaN;
            try
            {
                string? root = Path.GetPathRoot(folder);
                if (string.IsNullOrEmpty(root))
                    return true;

                DriveInfo drive = new(root);
                freeGB = drive.AvailableFreeSpace / MB / 1024;
                return drive.AvailableFreeSpace > neededMB * 1024 * 1024 * 1.05;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                // network paths have no drive information, so let the writes report any problem
                return true;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("""
                Usage: dnGREP.Benchmarks generate --out FOLDER [options]

                  --out FOLDER    folder to write the files to (required, created if missing)
                  --files N       number of files (default 8)
                  --mb N          size of each file in MB (default 64, or DNGREP_BENCH_MB)
                  --min-mb N      smallest file size in MB, to vary the sizes (default --mb)
                  --max-mb N      largest file size in MB (default --mb)
                  --sizes K       number of distinct sizes between --min-mb and --max-mb (default 4)
                  --length N      line length in chars; 0 = whole file is one line (default 80)
                  --eol lf|crlf   line ending (default crlf)
                  --density D     None | Last | Sparse | Dense (default Sparse)
                  --zip           put each file in its own .zip archive
                  --prefix TEXT   file name prefix (default "file")

                Files of the same size are copies of each other. The files contain random lowercase
                words and the text "needle" at the chosen density. manifest.csv in the output folder
                lists the expected number of matches for "needle" in each file.

                Example, similar to the reported scenario (about 75 GB):
                  generate --out D:\data --files 300 --min-mb 100 --max-mb 400 --sizes 7
                """);
        }
    }
}
