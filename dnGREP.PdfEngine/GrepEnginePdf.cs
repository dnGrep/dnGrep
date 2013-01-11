using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Engines;
using NLog;
using dnGREP.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace dnGREP.Engines.Pdf
{
	/// <summary>
	/// Based on a MicrosoftWordPlugin class for AstroGrep by Curtis Beard. Thank you!
	/// </summary>
	public class GrepEnginePdf : GrepEngineBase, IGrepEngine
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private string pathToPdfToText = "";

		#region Initialization and disposal
		public override bool Initialize(GrepEngineInitParams param)
		{
            base.Initialize(param);
			try
			{
				// Make sure pdftotext.exe exists
				pathToPdfToText = Utils.GetCurrentPath(typeof(GrepEnginePdf)) + "\\pdftotext.exe";
				if (File.Exists(pathToPdfToText))
					return true;
				else
					return false;
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to find pdftotext.exe.", ex);
				return false;
			}
		}

		#endregion

		public bool IsSearchOnly
		{
			get { return true; }
		}

		public string Description
		{
			get { return "Searches inside Acrobat PDF files. File types supported include: pdf. Search only."; }
		}

		public List<string> SupportedFileExtensions
		{
			get { return new List<string>(new string[] { "pdf" }); }
		}

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			try
			{
				// Extract text
				string tempFile = extractText(file);
				if (!File.Exists(tempFile))
					throw new ApplicationException("pdftotext failed to create text file.");

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(tempFile, new GrepEngineInitParams(showLinesInContext, linesBefore, linesAfter, fuzzyMatchThreshold));
				List<GrepSearchResult> results = engine.Search(tempFile, searchPattern, searchType, searchOptions, encoding);

                using (FileStream reader = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader streamReader = new StreamReader(reader))
                {
                    foreach (var result in results)
                    {
                        result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, linesBefore, linesAfter);
                    }
                }

				foreach (GrepSearchResult result in results)
				{
					result.ReadOnly = true;
					result.FileNameDisplayed = file;
					result.FileNameReal = file;
				}

				return results;
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to search inside Pdf file", ex);
				return new List<GrepSearchResult>();
			}
		}

		private string extractText(string pdfFilePath)
		{
			string tempFolder = Utils.GetTempFolder() + "dnGREP-PDF\\";
			if (!Directory.Exists(tempFolder))
				Directory.CreateDirectory(tempFolder);
			string tempFileName = tempFolder + Path.GetFileNameWithoutExtension(pdfFilePath) + ".txt";

			using (Process process = new Process())
			{
				try
				{
					// use command prompt
					process.StartInfo.FileName = pathToPdfToText;
					process.StartInfo.Arguments = string.Format("-layout \"{0}\" \"{1}\"", pdfFilePath, tempFileName);
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.WorkingDirectory = Utils.GetCurrentPath(typeof(GrepEnginePdf));
					process.StartInfo.CreateNoWindow = true;
					// start cmd prompt, execute command
					process.Start();
					process.WaitForExit();

					if (process.ExitCode == 0)
						return tempFileName;
					else
						throw new Exception("pdftotext process exited with error code.");
				}
				catch
				{
					throw;
				}
			}
		}

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public Version FrameworkVersion
		{
			get
			{
				return new Version(2, 7, 0, 0);
			}
		}

		public void Unload()
		{
			//Do nothing
		}

		public override void OpenFile(OpenFileArgs args)
		{
			args.UseCustomEditor = false;
			Utils.OpenFile(args);
		}
	}
}
