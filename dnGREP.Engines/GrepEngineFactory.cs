using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using dnGREP.Common;
using System.Reflection;

namespace dnGREP.Engines
{
	public class GrepEngineFactory
	{
		private static Dictionary<string, IGrepEngine> fileTypeEngines = new Dictionary<string, IGrepEngine>();
		private static List<IGrepEngine> engines = null;

		private static void loadAssemblies(bool showLinesInContext, int linesBefore, int linesAfter) 
		{
			if (engines == null)
			{
				engines = new List<IGrepEngine>();
				foreach (string assemblyFile in Directory.GetFiles(Utils.GetCurrentPath() + "\\Plugins", "*.dll", SearchOption.AllDirectories))
				{
					try
					{
						Assembly assembly = Assembly.LoadFile(assemblyFile);
						Type[] types = assembly.GetTypes();
						foreach (Type type in types)
						{
							if (type.GetInterface("IGrepEngine") != null)
							{
								IGrepEngine engine = (IGrepEngine)Activator.CreateInstance(type);
								engines.Add(engine);
								break;
							}
						}
					}
					catch (Exception ex)
					{
						//DO NOTHING
					}
				}
			}
		}

		public static IGrepEngine GetSearchEngine(string fileName, bool showLinesInContext, int linesBefore, int linesAfter)
		{
			loadAssemblies(showLinesInContext, linesBefore, linesAfter);

			string fileExtension = Path.GetExtension(fileName);
			if (fileExtension.Length > 1)
				fileExtension = fileExtension.Substring(1);

			if (!fileTypeEngines.ContainsKey(fileExtension)) 
			{
				foreach (IGrepEngine engine in engines)
				{
					if (engine.SupportedFileExtensions.Contains(fileExtension))
					{
						fileTypeEngines[fileExtension] = engine;
					}
				}
			}
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText();
			plainTextEngine.Initialize(showLinesInContext, linesBefore, linesAfter);

			if (fileTypeEngines.ContainsKey(fileExtension))
			{
				fileTypeEngines[fileExtension].Initialize(showLinesInContext, linesBefore, linesAfter);
				return fileTypeEngines[fileExtension];
			}
			else
				return plainTextEngine;
		}

		public static IGrepEngine GetReplaceEngine(string fileName, bool showLinesInContext, int linesBefore, int linesAfter)
		{
			loadAssemblies(showLinesInContext, linesBefore, linesAfter);

			string fileExtension = Path.GetExtension(fileName);
			if (fileExtension.Length > 1)
				fileExtension = fileExtension.Substring(1);

			if (!fileTypeEngines.ContainsKey(fileExtension))
			{
				foreach (IGrepEngine engine in engines)
				{
					if (engine.SupportedFileExtensions.Contains(fileExtension))
					{
						fileTypeEngines[fileExtension] = engine;
					}
				}
			}
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText();
			plainTextEngine.Initialize(showLinesInContext, linesBefore, linesAfter);

			if (fileTypeEngines.ContainsKey(fileExtension) && !fileTypeEngines[fileExtension].IsSearchOnly)
			{
				fileTypeEngines[fileExtension].Initialize(showLinesInContext, linesBefore, linesAfter);
				return fileTypeEngines[fileExtension];
			}
			else
				return plainTextEngine;
		}
	}
}
