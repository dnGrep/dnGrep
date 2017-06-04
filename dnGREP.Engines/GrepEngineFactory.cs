using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnGREP.Common;
using NLog;

namespace dnGREP.Engines
{
    public class GrepEngineFactory
    {
        private static Dictionary<string, GrepPlugin> fileTypeEngines = new Dictionary<string, GrepPlugin>();
        private static List<GrepPlugin> plugings = null;
        private static Dictionary<string, string> failedEngines = new Dictionary<string, string>();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void loadPlugins()
        {
            if (plugings == null)
            {
                plugings = new List<GrepPlugin>();
                if (Directory.Exists(Utils.GetCurrentPath() + "\\Plugins"))
                {
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
                            logger.Log<Exception>(LogLevel.Error, "Failed to initialize " + Path.GetFileNameWithoutExtension(pluginFile) + " engine.", ex);
                        }
                    }
                }
            }
        }

        public static IGrepEngine GetSearchEngine(string fileName, GrepEngineInitParams param, FileFilter filter)
        {
            Debug.Assert(param != null);
            Debug.Assert(filter != null);

            loadPlugins();

            string fileExtension = Path.GetExtension(fileName).ToLower();
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
            plainTextEngine.Initialize(param, filter);

            if (fileTypeEngines.ContainsKey(fileExtension) && fileTypeEngines[fileExtension].Enabled)
            {
                if (FrameworkVersionsAreCompatible(fileTypeEngines[fileExtension].Engine.FrameworkVersion, plainTextEngine.FrameworkVersion))
                {
                    if (fileTypeEngines[fileExtension].Engine.Initialize(param, filter))
                    {
                        return fileTypeEngines[fileExtension].Engine;
                    }
                    else
                    {
                        failedEngines[fileTypeEngines[fileExtension].Engine.GetType().Name] = "Failed to initialize the plugin. See error log for details.";
                        return plainTextEngine;
                    }
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

        /// <summary>
        /// Tells if two grep-engine/plugin versions are compatible.
        /// </summary>
        private static bool FrameworkVersionsAreCompatible(Version version1, Version version2)
        {
            return (version1.Major == version2.Major);
        }

        public static IGrepEngine GetReplaceEngine(string fileName, GrepEngineInitParams param, FileFilter filter)
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
            plainTextEngine.Initialize(param, filter);

            if (fileTypeEngines.ContainsKey(fileExtension) && fileTypeEngines[fileExtension].Enabled && !fileTypeEngines[fileExtension].Engine.IsSearchOnly)
            {
                if (FrameworkVersionsAreCompatible(fileTypeEngines[fileExtension].Engine.FrameworkVersion, plainTextEngine.FrameworkVersion))
                {
                    if (fileTypeEngines[fileExtension].Engine.Initialize(param, filter))
                    {
                        return fileTypeEngines[fileExtension].Engine;
                    }
                    else
                    {
                        failedEngines[fileTypeEngines[fileExtension].Engine.GetType().Name] = "Failed to initialize the plugin. See error log for details.";
                        return plainTextEngine;
                    }
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
