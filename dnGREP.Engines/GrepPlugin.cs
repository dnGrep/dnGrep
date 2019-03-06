using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using NLog;

namespace dnGREP.Engines
{
    public class GrepPlugin
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
        private Type pluginType;

        public IGrepEngine CreateEngine()
        {
            IGrepEngine engine = null;
            try
            {
                if (pluginType != null)
                    engine = (IGrepEngine)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to create engine " + Path.GetFileNameWithoutExtension(DllFilePath), ex);
            }
            return engine;
        }

        public List<string> Extensions { get; private set; }

        /// <summary>
        /// Gets the name of the IGrepEngine type
        /// </summary>
        public string PluginName { get; private set; }

        /// <summary>
        /// Absolute path to DLL file
        /// </summary>
        public string DllFilePath { get; private set; }

        /// <summary>
        /// Absolute path to plugin file
        /// </summary>
        public string PluginFilePath { get; private set; }

        /// <summary>
        /// Gets a flag indicating if this plugin should be loaded
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Returns true if engine supports search only. Returns false is engine supports replace as well.
        /// </summary>
        public bool IsSearchOnly { get; private set; }

        public Version FrameworkVersion
        {
            get { return Assembly.GetAssembly(pluginType).GetName().Version; }
        }

        public GrepPlugin(string pluginFilePath)
        {
            PluginFilePath = pluginFilePath;
            Extensions = new List<string>();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public bool LoadPluginSettings()
        {
            bool result = false;
            if (PluginFilePath != null && File.Exists(PluginFilePath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(PluginFilePath))
                    {
                        string[] tokens = line.Split('=');
                        if (tokens.Length != 2)
                            continue;

                        switch (tokens[0].Trim().ToUpper())
                        {
                            case "FILE":
                                DllFilePath = tokens[1].Trim();
                                break;
                            case "ENABLED":
                                Enabled = Utils.ParseBoolean(tokens[1].Trim(), true);
                                break;
                            case "EXTENSIONS":
                                Extensions.Clear();
                                foreach (string extension in tokens[1].Trim().Split(','))
                                {
                                    Extensions.Add(extension.Trim().ToLower());
                                }
                                break;
                        }
                    }

                    string tempDllFilePath = DllFilePath;
                    if (!File.Exists(tempDllFilePath))
                        DllFilePath = Path.Combine(Path.GetDirectoryName(PluginFilePath), tempDllFilePath);

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
                        PluginName = pluginType.Name;
                        var engine = CreateEngine();
                        if (engine != null)
                        {
                            IsSearchOnly = engine.IsSearchOnly;

                            if (engine is IDisposable disposable)
                                disposable.Dispose();
                        }
                    }

                    result = pluginType != null;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;

            if (loadedAssemblies.ContainsKey(args.Name))
            {
                assembly = loadedAssemblies[args.Name];
            }
            else if (!string.IsNullOrWhiteSpace(PluginFilePath) && !string.IsNullOrWhiteSpace(DllFilePath))
            {
                var name = new AssemblyName(args.Name).Name + ".dll";

                var filePath = Path.Combine(Path.GetDirectoryName(PluginFilePath), Path.GetDirectoryName(DllFilePath), name);
                if (File.Exists(filePath))
                {
                    assembly = Assembly.LoadFile(filePath);

                    if (assembly != null)
                        loadedAssemblies.Add(args.Name, assembly);
                }
            }

            return assembly;
        }

        public void PersistPluginSettings()
        {
            if (PluginFilePath != null && File.Exists(PluginFilePath))
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("File=" + DllFilePath);
                    sb.AppendLine("Enabled=" + Enabled.ToString());
                    sb.Append("Extensions=");
                    foreach (string ext in Extensions)
                    {
                        sb.Append(ext + ",");
                    }
                    Utils.DeleteFile(PluginFilePath);
                    File.WriteAllText(PluginFilePath, sb.ToString());

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
