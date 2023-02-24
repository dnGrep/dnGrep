using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using dnGREP.Common;
using NLog;

namespace dnGREP.Engines
{
    public class GrepPlugin
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
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
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create engine " + Path.GetFileNameWithoutExtension(DllFilePath));
            }
            return engine;
        }

        public string Name { get; private set; } = string.Empty;

        public List<string> DefaultExtensions { get; private set; }

        public List<string> Extensions { get; private set; }

        /// <summary>
        /// Gets the name of the IGrepEngine type
        /// </summary>
        public string PluginName { get; private set; } = string.Empty;

        /// <summary>
        /// Absolute path to DLL file
        /// </summary>
        public string DllFilePath { get; private set; } = string.Empty;

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

        public Version? FrameworkVersion => pluginType != null ? Assembly.GetAssembly(pluginType)?.GetName()?.Version : null;

        public GrepPlugin(string pluginFilePath)
        {
            PluginFilePath = pluginFilePath;
            DefaultExtensions = new List<string>();
            Extensions = new List<string>();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public bool LoadPluginSettings()
        {
            bool result = false;
            if (File.Exists(PluginFilePath))
            {
                try
                {
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
                        }
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

                    IList<string>? defaultExtensions = null;
                    if (pluginType != null)
                    {
                        PluginName = pluginType.Name;
                        var engine = CreateEngine();
                        if (engine != null)
                        {
                            defaultExtensions = engine.DefaultFileExtensions;
                            IsSearchOnly = engine.IsSearchOnly;

                            if (engine is IDisposable disposable)
                                disposable.Dispose();
                        }
                    }

                    GetEnabledFromSettings(Name);
                    GetPreviewPlainTextFromSettings(Name);
                    GetExtensionsFromSettings(Name, defaultExtensions ?? new List<string>());

                    result = pluginType != null;
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    throw;
                }
            }
            return result;
        }

        private void GetEnabledFromSettings(string name)
        {
            Enabled = true;
            if (!string.IsNullOrEmpty(name))
            {
                string key = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name) + "Enabled";
                if (GrepSettings.Instance.ContainsKey(key))
                    Enabled = GrepSettings.Instance.Get<bool>(key);
            }
        }

        private void GetPreviewPlainTextFromSettings(string name)
        {
            PreviewPlainText = true;
            if (!string.IsNullOrEmpty(name))
            {
                string key = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name) + "PreviewText";
                if (GrepSettings.Instance.ContainsKey(key))
                    PreviewPlainText = GrepSettings.Instance.Get<bool>(key);
            }
        }

        private void GetExtensionsFromSettings(string name, IList<string> defaultExtensions)
        {
            var list = GrepSettings.Instance.GetExtensionList(name, defaultExtensions);

            DefaultExtensions.Clear();
            Extensions.Clear();

            DefaultExtensions.AddRange(defaultExtensions);
            Extensions.AddRange(list);
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            Assembly? assembly = null;

            if (loadedAssemblies.ContainsKey(args.Name))
            {
                assembly = loadedAssemblies[args.Name];
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
