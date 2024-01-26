using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnGREP.Common;
using NLog;

namespace dnGREP.Engines
{
    public class GrepPlugin
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, Assembly> loadedAssemblies = [];
        private Type? pluginType;

        public IGrepEngine? CreateEngine()
        {
            IGrepEngine? engine = null;
            try
            {
                if (pluginType != null)
                {
                    engine = Activator.CreateInstance(pluginType) as IGrepEngine;
                }
                else if (!string.IsNullOrEmpty(Application))
                {
                    engine = new GenericPluginEngine(Name, Application, Arguments, WorkingDirectory);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create engine " + Path.GetFileNameWithoutExtension(DllFilePath));
            }
            return engine;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        public List<string> DefaultExtensions { get; private set; }

        public List<string> Extensions { get; private set; }

        /// <summary>
        /// Gets the name of the IGrepEngine type
        /// </summary>
        public string PluginTypeName { get; private set; } = string.Empty;

        /// <summary>
        /// Absolute path to DLL file
        /// </summary>
        public string DllFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the name of the application to call in the GenericPlugin
        /// </summary>
        public string Application { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the command arguments for the GenericPlugin
        /// </summary>
        public string Arguments { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the working directory for the GenericPlugin
        /// </summary>
        public string WorkingDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// Absolute path to plugin file
        /// </summary>
        public string PluginFilePath { get; private set; }

        /// <summary>
        /// Gets a flag indicating if this plugin should be loaded
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Gets a flag indicating if this plugin should create a temporary plain text file for the Preview window
        /// </summary>
        public bool PreviewPlainText { get; private set; }

        /// <summary>
        /// Returns true if engine supports search only. Returns false is engine supports replace as well.
        /// </summary>
        public bool IsSearchOnly { get; private set; }

        public Version? FrameworkVersion => pluginType != null ?
            Assembly.GetAssembly(pluginType)?.GetName()?.Version :
            Assembly.GetAssembly(typeof(GenericPluginEngine))?.GetName()?.Version;

        public GrepPlugin(string pluginFilePath)
        {
            PluginFilePath = pluginFilePath;
            DefaultExtensions = [];
            Extensions = [];
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static readonly char[] csvSeparators = [',', ';', ' '];

        public bool LoadPluginSettings()
        {
            bool result = false;
            if (File.Exists(PluginFilePath))
            {
                try
                {
                    List<string>? defaultExtensions = null;

                    foreach (string line in File.ReadAllLines(PluginFilePath))
                    {
                        string[] tokens = line.Split('=');
                        if (tokens.Length != 2)
                            continue;

                        switch (tokens[0].Trim())
                        {
                            case "Name":
                                Name = tokens[1].Trim();
                                break;
                            case "File":
                                DllFilePath = tokens[1].Trim();
                                break;
                            case "Application":
                                Application = tokens[1].Trim();
                                break;
                            case "Arguments":
                                Arguments = tokens[1].Trim();
                                break;
                            case "WorkingDirectory":
                                WorkingDirectory = tokens[1].Trim();
                                break;
                            case "Extensions":
                                defaultExtensions = [.. tokens[1].Split(csvSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                                break;
                            default:
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(Name))
                    {
                        logger.Error($"The plugin {PluginFilePath} must have a Name property");
                        return false;
                    }

                    if (!string.IsNullOrEmpty(Application))
                    {
                        if (string.IsNullOrEmpty(WorkingDirectory))
                        {
                            WorkingDirectory = Path.GetDirectoryName(PluginFilePath) ?? string.Empty;
                        }

                        if (!File.Exists(Application))
                        {
                            string fullPath = NativeMethods.PathFindOnPath(Application, WorkingDirectory);
                            if (!string.IsNullOrEmpty(fullPath))
                            {
                                Application = fullPath;
                            }
                        }

                        if (!File.Exists(Application))
                        {
                            logger.Error($"The {Name} plugin application [{Application}] could not be found");
                            return false;
                        }

                        PluginTypeName = $"GenericPluginEngine{Name}";
                        IsSearchOnly = true;
                    }

                    string tempDllFilePath = DllFilePath;
                    if (!File.Exists(tempDllFilePath))
                    {
                        DllFilePath = Path.Combine(
                            Path.GetDirectoryName(PluginFilePath) ?? string.Empty, tempDllFilePath);
                    }

                    if (File.Exists(DllFilePath))
                    {
                        Assembly assembly = Assembly.LoadFile(DllFilePath);
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            if (type.GetInterface("IGrepEngine") != null)
                            {
                                pluginType = type;
                                break;
                            }
                        }
                    }

                    if (pluginType != null)
                    {
                        PluginTypeName = pluginType.Name;
                        var engine = CreateEngine();
                        if (engine != null)
                        {
                            defaultExtensions = engine.DefaultFileExtensions;
                            IsSearchOnly = engine.IsSearchOnly;

                            if (engine is IDisposable disposable)
                                disposable.Dispose();
                        }
                    }

                    GrepSettings.Instance.ConvertExtensionsToV3(Name, defaultExtensions ?? []);

                    PluginConfiguration? cfg =
                        GrepSettings.Instance.Get<List<PluginConfiguration>>(GrepSettings.Key.Plugins)
                        .FirstOrDefault(r => r.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)) ??
                        GrepSettings.Instance.AddNewPluginConfig(Name);

                    Enabled = cfg.Enabled;
                    PreviewPlainText = cfg.PreviewText;
                    GetExtensionsFromSettings(cfg, defaultExtensions ?? []);

                    result = pluginType != null || !string.IsNullOrEmpty(Application);

                    if (result && cfg.Enabled)
                    {
                        // keep a list of all extensions handled by a plugin
                        foreach (var ext in cfg.ExtensionList)
                        {
                            Utils.AllPluginExtensions.Add('.' + ext.ToLower());
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    throw;
                }
            }
            return result;
        }

        private void GetExtensionsFromSettings(PluginConfiguration cfg, List<string> defaultExtensions)
        {
            if (string.IsNullOrEmpty(cfg.Extensions) && defaultExtensions.Count > 0)
            {
                cfg = new(cfg.Name, cfg.Enabled, cfg.PreviewText, GrepSettings.CleanExtensions(defaultExtensions));
                GrepSettings.Instance.UpdatePluginConfig(cfg);
            }

            DefaultExtensions.Clear();
            Extensions.Clear();

            DefaultExtensions.AddRange(defaultExtensions);
            Extensions.AddRange(cfg.ExtensionList);
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            Assembly? assembly = null;

            if (loadedAssemblies.TryGetValue(args.Name, out Assembly? value))
            {
                assembly = value;
            }
            else if (!string.IsNullOrWhiteSpace(PluginFilePath) && !string.IsNullOrWhiteSpace(DllFilePath))
            {
                var name = new AssemblyName(args.Name).Name + ".dll";

                var filePath = Path.Combine(Path.GetDirectoryName(PluginFilePath) ?? string.Empty,
                    Path.GetDirectoryName(DllFilePath) ?? string.Empty, name);
                if (File.Exists(filePath))
                {
                    assembly = Assembly.LoadFile(filePath);

                    if (assembly != null)
                        loadedAssemblies.Add(args.Name, assembly);
                }
            }

            return assembly;
        }
    }
}
