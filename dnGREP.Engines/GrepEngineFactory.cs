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
		private static Dictionary<string, GrepPlugin> fileTypeEngines = new Dictionary<string, GrepPlugin>();
		private static List<GrepPlugin> plugings = null;
		private static Dictionary<string, string> failedEngines = new Dictionary<string, string>();

		private static void loadPlugins() 
		{
			if (plugings == null)
			{
				plugings = new List<GrepPlugin>();
				foreach (string pluginFile in Directory.GetFiles(Utils.GetCurrentPath() + "\\Plugins", "*.plugin", SearchOption.AllDirectories))
				{
					try
					{
						GrepPlugin plugin = new GrepPlugin(pluginFile);
						if (plugin.LoadPluginSettings())
						{
							plugings.Add(plugin);
						}
					}
					catch (Exception ex)
					{
						failedEngines[Path.GetFileNameWithoutExtension(pluginFile)] = ex.Message;
					}
				}
			}
		}

		public static IGrepEngine GetSearchEngine(string fileName, bool showLinesInContext, int linesBefore, int linesAfter)
		{
			loadPlugins();

			string fileExtension = Path.GetExtension(fileName);
			if (fileExtension.Length > 1)
				fileExtension = fileExtension.Substring(1);

			if (!fileTypeEngines.ContainsKey(fileExtension)) 
			{
				foreach (GrepPlugin plugin in plugings)
				{
					if (plugin.Extensions.Contains(fileExtension))
					{
						fileTypeEngines[fileExtension] = plugin;
					}
				}
			}
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText();
			plainTextEngine.Initialize(showLinesInContext, linesBefore, linesAfter);

			if (fileTypeEngines.ContainsKey(fileExtension) && fileTypeEngines[fileExtension].Enabled)
			{
				if (fileTypeEngines[fileExtension].Engine.FrameworkVersion.CompareTo(plainTextEngine.FrameworkVersion) == 0)
				{
					fileTypeEngines[fileExtension].Engine.Initialize(showLinesInContext, linesBefore, linesAfter);
					return fileTypeEngines[fileExtension].Engine;
				}
				else
				{
					failedEngines[fileTypeEngines[fileExtension].Engine.GetType().Name] = "Plugin developed under outdated framework. Please update the plugin.";
					return plainTextEngine;
				}
			}
			else
				return plainTextEngine;
		}

		public static IGrepEngine GetReplaceEngine(string fileName, bool showLinesInContext, int linesBefore, int linesAfter)
		{
			loadPlugins();

			string fileExtension = Path.GetExtension(fileName);
			if (fileExtension.Length > 1)
				fileExtension = fileExtension.Substring(1);

			if (!fileTypeEngines.ContainsKey(fileExtension))
			{
				foreach (GrepPlugin plugin in plugings)
				{
					if (plugin.Extensions.Contains(fileExtension))
					{
						fileTypeEngines[fileExtension] = plugin;
					}
				}
			}
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText();
			plainTextEngine.Initialize(showLinesInContext, linesBefore, linesAfter);

			if (fileTypeEngines.ContainsKey(fileExtension) && fileTypeEngines[fileExtension].Enabled && !fileTypeEngines[fileExtension].Engine.IsSearchOnly)
			{
				if (fileTypeEngines[fileExtension].Engine.FrameworkVersion.CompareTo(plainTextEngine.FrameworkVersion) == 0)
				{
					fileTypeEngines[fileExtension].Engine.Initialize(showLinesInContext, linesBefore, linesAfter);
					return fileTypeEngines[fileExtension].Engine;
				}
				else
				{
					failedEngines[fileTypeEngines[fileExtension].Engine.GetType().Name] = "Plugin developed under outdated framework. Please update the plugin.";
					return plainTextEngine;
				}
			}
			else
				return plainTextEngine;
		}

		public static void UnloadEngines()
		{
			foreach (string key in fileTypeEngines.Keys)
			{
				fileTypeEngines[key].Engine.Unload();
			}
		}

		public static string GetListOfFailedEngines()
		{
			StringBuilder sb = new StringBuilder();
			foreach (string key in failedEngines.Keys)
			{
				sb.AppendFormat("  * {0} ({1})", key, failedEngines[key]);
			}
			failedEngines.Clear();
			return sb.ToString();
		}
	}
}
