using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using dnGREP.Common;
using SevenZip;
using System.IO;
using System.Reflection;

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

		public string Description
		{
			get { return "Searches inside archive files. Archives supported include: 7z, zip, rar, gzip. Search only."; }
		}

		public List<string> SupportedFileExtensions
		{
			get { return new List<string> ( new string[] { "7z", "zip", "rar", "gzip" }); }
		}


        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
			SevenZipExtractor extractor = new SevenZipExtractor(file);
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText();
			plainTextEngine.Initialize(new GrepEngineInitParams(showLinesInContext, linesBefore, linesAfter, fuzzyMatchThreshold));
			string tempFolder = Utils.FixFolderName(Utils.GetTempFolder()) + "dnGREP-Archive\\" + Utils.GetHash(file) + "\\";
			
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			try
			{
				extractor.ExtractArchive(tempFolder);
				foreach (string archiveFileName in Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories))
				{
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(archiveFileName, new GrepEngineInitParams(showLinesInContext, linesBefore, linesAfter, fuzzyMatchThreshold));
                    var innerFileResults = engine.Search(archiveFileName, searchPattern, searchType, searchOptions, encoding);
					
                    using (FileStream reader = File.Open(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader streamReader = new StreamReader(reader))
                    {
                        foreach (var result in innerFileResults)
                        {
                            if (!result.HasSearchResults)
                                result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, linesBefore, linesAfter);
                        }
                    }
                    searchResults.AddRange(innerFileResults);
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
				logger.Log<Exception>(LogLevel.Error, "Failed to search inside archive.", ex);
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
