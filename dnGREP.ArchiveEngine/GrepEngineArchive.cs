using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using NLog;
using SevenZip;

namespace dnGREP.Engines.Archive
{
	public class GrepEngineArchive : GrepEngineBase, IGrepEngine
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public GrepEngineArchive() : base() { }

		public GrepEngineArchive(GrepEngineInitParams param)
			:
			base(param)
		{}

		public bool IsSearchOnly
		{
			get { return true; }
		}

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
			SevenZipExtractor extractor = new SevenZipExtractor(file);
			string tempFolder = Utils.FixFolderName(Utils.GetTempFolder()) + "dnGREP-Archive\\" + Utils.GetHash(file) + "\\";
            FileFilter filter = FileFilter.ChangePath(tempFolder);

            // if the search pattern(s) only match archive files, need to include an 'any' file type  to search inside the archive.  
            // otherwise, keep the original pattern set so the user can specify what types of files to search inside the archive.
            var patterns = Utils.SplitPath(FileFilter.NamePatternToInclude).ToList();
            bool hasNonArchivePattern = patterns.Where(p => !Utils.IsArchiveExtension(Path.GetExtension(p))).Any();
            if (!hasNonArchivePattern)
            {
                patterns.Add(FileFilter.IsRegex ? ".*" : "*.*");
                filter = filter.ChangeIncludePattern(string.Join(";", patterns.ToArray()));
            }
			
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			try
			{
				extractor.ExtractArchive(tempFolder);
                foreach (var innerFileName in Utils.GetFileListEx(filter))
				{
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(innerFileName, initParams, FileFilter);
                    var innerFileResults = engine.Search(innerFileName, searchPattern, searchType, searchOptions, encoding);

                    if (innerFileResults.Count > 0)
                    {
                        using (FileStream reader = File.Open(innerFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader streamReader = new StreamReader(reader))
                        {
                            foreach (var result in innerFileResults)
                            {
                                if (Utils.CancelSearch)
                                    break;

                                if (!result.HasSearchResults)
                                    result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                            }
                        }
                        searchResults.AddRange(innerFileResults);
                    }

                    if (Utils.CancelSearch)
                        break;
                }

				foreach (GrepSearchResult result in searchResults)
				{
					result.FileNameDisplayed = file + "\\" + result.FileNameDisplayed.Substring(tempFolder.Length);
					result.FileNameReal = file;
					result.ReadOnly = true;
                }
			}
			catch (Exception ex)
			{
				logger.Log<Exception>(LogLevel.Error, string.Format("Failed to search inside archive '{0}'", file), ex);
			}
			return searchResults;
		}

		public void Unload()
		{
			//Do nothing
		}

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			throw new Exception("The method or operation is not supported.");
		}

		public Version FrameworkVersion
		{
			get
			{
                return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version;
			}
		}

		public override void OpenFile(OpenFileArgs args)
		{
			SevenZipExtractor extractor = new SevenZipExtractor(args.SearchResult.FileNameReal);
			
			string tempFolder = Utils.FixFolderName(Utils.GetTempFolder()) + "dnGREP-Archive\\" + Utils.GetHash(args.SearchResult.FileNameReal) + "\\";

			if (!Directory.Exists(tempFolder))
			{
				Directory.CreateDirectory(tempFolder);
				try
				{
					extractor.ExtractArchive(tempFolder);					
				}
				catch
				{
					args.UseBaseEngine = true;
				}
			}
			GrepSearchResult newResult = new GrepSearchResult();
			newResult.FileNameReal = args.SearchResult.FileNameReal;
			newResult.FileNameDisplayed = args.SearchResult.FileNameDisplayed;
			OpenFileArgs newArgs = new OpenFileArgs(newResult, args.Pattern, args.LineNumber, args.UseCustomEditor, args.CustomEditor, args.CustomEditorArgs);
			newArgs.SearchResult.FileNameDisplayed = tempFolder + args.SearchResult.FileNameDisplayed.Substring(args.SearchResult.FileNameReal.Length + 1);
			Utils.OpenFile(newArgs);
		}
	}
}
