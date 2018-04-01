using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using NLog;

namespace dnGREP.Engines
{
    public class GrepEngineFactory
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static Dictionary<string, GrepPlugin> fileTypeEngines = new Dictionary<string, GrepPlugin>();
        private static List<GrepPlugin> plugins = null;
        private static Dictionary<string, string> poolKeys = new Dictionary<string, string>();
        private static List<IGrepEngine> loadedEngines = new List<IGrepEngine>();
        private static Dictionary<string, Queue<IGrepEngine>> pooledEngines = new Dictionary<string, Queue<IGrepEngine>>();
        private static Dictionary<string, string> failedEngines = new Dictionary<string, string>();
        private static object lockObj = new object();

        private static void LoadPlugins()
        {
            lock (lockObj)
            {
                if (plugins == null)
                {
                    plugins = new List<GrepPlugin>();
                    string pluginPath = Path.Combine(Utils.GetCurrentPath(), "Plugins");
                    if (Directory.Exists(pluginPath))
                    {
                        foreach (string pluginFile in Directory.GetFiles(pluginPath, "*.plugin", SearchOption.AllDirectories))
                        {
                            try
                            {
                                GrepPlugin plugin = new GrepPlugin(pluginFile);
                                if (plugin.LoadPluginSettings())
                                {
                                    if (plugin.Enabled)
                                    {
                                        if (FrameworkVersionsAreCompatible(plugin.FrameworkVersion, FrameworkVersion))
                                        {
                                            plugins.Add(plugin);

                                            // many file extensions will map to the same pool of engines, 
                                            // so keep a common key for the set of extensions
                                            foreach (string ext in plugin.Extensions)
                                            {
                                                string fileExtension = ext.TrimStart('.');
                                                if (!poolKeys.ContainsKey(fileExtension))
                                                {
                                                    poolKeys.Add(fileExtension, plugin.PluginName);
                                                }
                                            }

                                            logger.Debug(string.Format("Loading plugin: {0} for extensions {1}",
                                                plugin.DllFilePath, string.Join(", ", plugin.Extensions.ToArray())));
                                        }
                                        else
                                        {
                                            logger.Error(string.Format("Plugin '{0}' developed under outdated framework. Please update the plugin.", Path.GetFileNameWithoutExtension(pluginFile)));
                                        }
                                    }
                                    else
                                    {
                                        logger.Debug(string.Format("Plugin skipped, not enabled: {0}", plugin.DllFilePath));
                                    }
                                }
                                else
                                {
                                    logger.Error(string.Format("Plugin {0} failed to load", plugin.DllFilePath));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Log<Exception>(LogLevel.Error, "Failed to initialize " + Path.GetFileNameWithoutExtension(pluginFile) + " engine.", ex);
                            }
                        }
                    }

                    foreach (GrepPlugin plugin in plugins)
                    {
                        foreach (string extension in plugin.Extensions)
                        {
                            if (extension != null)
                            {
                                string fileExtension = extension.TrimStart('.');
                                if (!string.IsNullOrWhiteSpace(fileExtension) && !fileTypeEngines.ContainsKey(fileExtension))
                                {
                                    fileTypeEngines.Add(fileExtension, plugin);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static List<string> GetArchiveExtenstions()
        {
            LoadPlugins();

            if (plugins != null && plugins.Count > 0)
            {
                var archivePlugin = plugins.Where(r => r.Enabled &&
                    (r.Extensions.Contains("zip") || r.Extensions.Contains("gzip") || r.Extensions.Contains("gz") || r.Extensions.Contains("7z")))
                    .Select(r => r).FirstOrDefault();

                if (archivePlugin != null)
                    return archivePlugin.Extensions;
            }

            return new List<string>();
        }

        public static IGrepEngine GetSearchEngine(string fileName, GrepEngineInitParams param, FileFilter filter)
        {
            Debug.Assert(param != null);
            Debug.Assert(filter != null);

            LoadPlugins();

            string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');

            IGrepEngine poolEngine = FetchFromPool(fileExtension);
            if (poolEngine != null)
            {
                poolEngine.Initialize(param, filter);
                return poolEngine;
            }

            if (fileTypeEngines.ContainsKey(fileExtension))
            {
                IGrepEngine engine = fileTypeEngines[fileExtension].CreateEngine();
                if (engine != null && engine.Initialize(param, filter))
                {
                    loadedEngines.Add(engine);
                    logger.Debug(string.Format("Using plugin: {0} for extension {1}", engine.ToString(), fileExtension));
                    return engine;
                }
                else
                {
                    logger.Debug(string.Format("File type engines failed to initialize: {0}, using plainTextEngine", fileExtension));
                    failedEngines[engine.GetType().Name] = "Failed to initialize the plugin. See error log for details.";
                    return GetPlainTextEngine(fileExtension, param, filter);
                }
            }
            else
            {
                logger.Debug(string.Format("File type engines has no key for: {0}, using plainTextEngine", fileExtension));
                return GetPlainTextEngine(fileExtension, param, filter);
            }
        }

        public static IGrepEngine GetReplaceEngine(string fileName, GrepEngineInitParams param, FileFilter filter)
        {
            Debug.Assert(param != null);
            Debug.Assert(filter != null);

            LoadPlugins();

            string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');

            if (fileTypeEngines.ContainsKey(fileExtension) && !fileTypeEngines[fileExtension].IsSearchOnly)
            {
                IGrepEngine engine = fileTypeEngines[fileExtension].CreateEngine();
                if (engine != null && engine.Initialize(param, filter))
                {
                    loadedEngines.Add(engine);
                    return engine;
                }
                else
                {
                    failedEngines[engine.GetType().Name] = "Failed to initialize the plugin. See error log for details.";
                    return GetPlainTextEngine(fileExtension, param, filter);
                }
            }
            else
                return GetPlainTextEngine(fileExtension, param, filter);
        }

        private static IGrepEngine GetPlainTextEngine(string fileExtension, GrepEngineInitParams param, FileFilter filter)
        {
            if (!poolKeys.ContainsKey(fileExtension))
            {
                poolKeys.Add(fileExtension, "GrepEnginePlainText");
            }

            IGrepEngine poolEngine = FetchFromPool(fileExtension);
            if (poolEngine != null)
            {
                poolEngine.Initialize(param, filter);
                return poolEngine;
            }

            IGrepEngine engine = new GrepEnginePlainText();
            loadedEngines.Add(engine);
            engine.Initialize(param, filter);
            return engine;
        }

        private static IGrepEngine FetchFromPool(string fileExtension)
        {
            lock (lockObj)
            {
                IGrepEngine engine = null;
                string poolKey;
                if (poolKeys.TryGetValue(fileExtension, out poolKey))
                {
                    Queue<IGrepEngine> pooledEngines;
                    if (GrepEngineFactory.pooledEngines.TryGetValue(poolKey, out pooledEngines))
                    {
                        if (pooledEngines.Count > 0)
                        {
                            engine = pooledEngines.Dequeue();
                        }
                    }
                }
                return engine;
            }
        }

        public static void ReturnToPool(string fileName, IGrepEngine engine)
        {
            lock (lockObj)
            {
                string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
                string poolKey;
                if (poolKeys.TryGetValue(fileExtension, out poolKey))
                {
                    Queue<IGrepEngine> pooledEngines;

                    if (!GrepEngineFactory.pooledEngines.TryGetValue(poolKey, out pooledEngines))
                    {
                        pooledEngines = new Queue<IGrepEngine>();
                        GrepEngineFactory.pooledEngines.Add(poolKey, pooledEngines);
                    }

                    pooledEngines.Enqueue(engine);
                }
            }
        }

        public static void UnloadEngines()
        {
            lock (lockObj)
            {
                pooledEngines.Clear();

                foreach (IGrepEngine engine in loadedEngines)
                {
                    if (engine != null)
                    {
                        engine.Unload();
                        var disposable = engine as IDisposable;
                        if (disposable != null)
                            disposable.Dispose();
                    }
                }

                loadedEngines.Clear();
            }
        }

        public static Version FrameworkVersion
        {
            get { return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version; }
        }

        /// <summary>
        /// Tells if two grep-engine/plugin versions are compatible.
        /// </summary>
        private static bool FrameworkVersionsAreCompatible(Version version1, Version version2)
        {
            return (version1.Major == version2.Major);
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
