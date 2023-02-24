using System;
using System.Collections.Generic;
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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, GrepPlugin> fileTypeEngines = new();
        private static List<GrepPlugin>? plugins = null;
        private static readonly List<GrepPlugin> disabledPlugins = new();
        private static readonly Dictionary<string, string> poolKeys = new();
        private static readonly List<IGrepEngine> loadedEngines = new();
        private static readonly Dictionary<string, Queue<IGrepEngine>> pooledEngines = new();
        private static readonly Dictionary<string, string> failedEngines = new();
        private static readonly object lockObj = new();

        public static IEnumerable<GrepPlugin> AllPlugins
        {
            get
            {
                LoadPlugins();
                if (plugins == null)
                    return Enumerable.Empty<GrepPlugin>();

                return plugins.Concat(disabledPlugins);
            }
        }

        private static void LoadPlugins()
        {
            lock (lockObj)
            {
                if (plugins == null)
                {
                    plugins = new List<GrepPlugin>();
                    disabledPlugins.Clear();
                    string pluginPath = Path.Combine(Utils.GetCurrentPath(), "Plugins");
                    if (Directory.Exists(pluginPath))
                    {
                        foreach (string pluginFile in Directory.GetFiles(pluginPath, "*.plugin", SearchOption.AllDirectories))
                        {
                            try
                            {
                                GrepPlugin plugin = new(pluginFile);
                                if (plugin.LoadPluginSettings())
                                {
                                    if (FrameworkVersionsAreCompatible(plugin.FrameworkVersion, FrameworkVersion))
                                    {
                                        if (plugin.Enabled)
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
                                            disabledPlugins.Add(plugin);
                                            logger.Debug(string.Format("Plugin skipped, not enabled: {0}", plugin.DllFilePath));
                                        }
                                    }
                                    else
                                    {
                                        logger.Error(string.Format("Plugin '{0}' developed under outdated framework. Please update the plugin.", Path.GetFileNameWithoutExtension(pluginFile)));
                                    }
                                }
                                else
                                {
                                    logger.Error(string.Format("Plugin {0} failed to load", plugin.DllFilePath));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Failed to initialize " + Path.GetFileNameWithoutExtension(pluginFile) + " engine.");
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

        public static void ReloadPlugins()
        {
            if (plugins == null)
            {
                LoadPlugins();
                return;
            }

            lock (lockObj)
            {
                var pluginList = new List<GrepPlugin>();
                pluginList.AddRange(plugins);
                pluginList.AddRange(disabledPlugins);

                plugins.Clear();
                disabledPlugins.Clear();
                poolKeys.Clear();
                fileTypeEngines.Clear();
                UnloadEngines();

                foreach (var plugin in pluginList)
                {
                    if (plugin.LoadPluginSettings())
                    {
                        if (plugin.Enabled)
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
                            disabledPlugins.Add(plugin);
                            logger.Debug(string.Format("Plugin skipped, not enabled: {0}", plugin.DllFilePath));
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

        public static IGrepEngine GetSearchEngine(string fileName, GrepEngineInitParams param, FileFilter filter, SearchType searchType)
        {
            LoadPlugins();

            string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');

            lock (lockObj)
            {
                if (searchType == SearchType.Hex)
                {
                    return GetHexEngine(param, filter);
                }

                IGrepEngine? poolEngine = FetchFromPool(fileExtension);
                if (poolEngine != null)
                {
                    poolEngine.Initialize(param, filter);
                    return poolEngine;
                }

                if (ArchiveDirectory.Extensions.Contains(fileExtension))
                {
                    return GetArchiveEngine(fileExtension, param, filter);
                }

                if (fileTypeEngines.TryGetValue(fileExtension, out GrepPlugin? plugin))
                {
                    IGrepEngine? engine = plugin.CreateEngine();
                    if (engine != null && engine.Initialize(param, filter))
                    {
                        if (engine is IGrepPluginEngine pluginEngine)
                        {
                            pluginEngine.PreviewPlainText = fileTypeEngines[fileExtension].PreviewPlainText;
                        }

                        loadedEngines.Add(engine);
                        logger.Debug(string.Format("Using plugin: {0} for extension {1}", engine.ToString(), fileExtension));
                        return engine;
                    }
                    else
                    {
                        logger.Debug(string.Format("File type engines failed to initialize: {0}, using plainTextEngine", fileExtension));
                        failedEngines[plugin.PluginName] = "Failed to initialize the plugin. See error log for details.";
                        return GetPlainTextEngine(fileExtension, param, filter);
                    }
                }
                else
                {
                    logger.Debug(string.Format("File type engines has no key for: {0}, using plainTextEngine", fileExtension));
                    return GetPlainTextEngine(fileExtension, param, filter);
                }
            }
        }

        public static IGrepEngine GetReplaceEngine(string fileName, GrepEngineInitParams param, FileFilter filter)
        {
            LoadPlugins();

            string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');

            lock (lockObj)
            {
                if (fileTypeEngines.TryGetValue(fileExtension, out GrepPlugin? plugin) && !plugin.IsSearchOnly)
                {
                    IGrepEngine? engine = plugin.CreateEngine();
                    if (engine != null && engine.Initialize(param, filter))
                    {
                        loadedEngines.Add(engine);
                        return engine;
                    }
                    else
                    {
                        failedEngines[plugin.PluginName] = "Failed to initialize the plugin. See error log for details.";
                        return GetPlainTextEngine(fileExtension, param, filter);
                    }
                }
                else
                    return GetPlainTextEngine(fileExtension, param, filter);
            }
        }

        private static IGrepEngine GetPlainTextEngine(string fileExtension, GrepEngineInitParams param, FileFilter filter)
        {
            if (!poolKeys.ContainsKey(fileExtension))
            {
                poolKeys.Add(fileExtension, "GrepEnginePlainText");
            }

            IGrepEngine? poolEngine = FetchFromPool(fileExtension);
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

        private static IGrepEngine GetArchiveEngine(string fileExtension, GrepEngineInitParams param, FileFilter filter)
        {
            if (!poolKeys.ContainsKey(fileExtension))
            {
                poolKeys.Add(fileExtension, "GrepArchiveEngine");
            }

            IGrepEngine? poolEngine = FetchFromPool(fileExtension);
            if (poolEngine != null)
            {
                poolEngine.Initialize(param, filter);
                return poolEngine;
            }

            IGrepEngine engine = new ArchiveEngine();
            loadedEngines.Add(engine);
            engine.Initialize(param, filter);
            return engine;
        }

        private static IGrepEngine GetHexEngine(GrepEngineInitParams param, FileFilter filter)
        {
            IGrepEngine engine = new GrepEngineHex();
            loadedEngines.Add(engine);
            engine.Initialize(param, filter);
            return engine;
        }

        private static IGrepEngine? FetchFromPool(string fileExtension)
        {
            IGrepEngine? engine = null;
            if (poolKeys.TryGetValue(fileExtension, out string? poolKey))
            {
                if (GrepEngineFactory.pooledEngines.TryGetValue(poolKey, out Queue<IGrepEngine>? pooledEngines))
                {
                    if (pooledEngines.Count > 0)
                    {
                        engine = pooledEngines.Dequeue();
                    }
                }
            }
            return engine;
        }

        public static void ReturnToPool(string fileName, IGrepEngine engine)
        {
            lock (lockObj)
            {
                string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
                if (poolKeys.TryGetValue(fileExtension, out string? poolKey))
                {
                    if (!GrepEngineFactory.pooledEngines.TryGetValue(poolKey, out Queue<IGrepEngine>? pooledEngines))
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
                        if (engine is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }

                loadedEngines.Clear();
            }
        }

        public static Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        /// <summary>
        /// Tells if two grep-engine/plugin versions are compatible.
        /// </summary>
        private static bool FrameworkVersionsAreCompatible(Version? version1, Version? version2)
        {
            return version1 != null && version2 != null && (version1.Major == version2.Major);
        }

        public static string GetListOfFailedEngines()
        {
            StringBuilder sb = new();
            foreach (string key in failedEngines.Keys)
            {
                sb.AppendFormat("  * {0} ({1})", key, failedEngines[key]);
            }
            failedEngines.Clear();
            return sb.ToString();
        }
    }
}
