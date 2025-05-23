﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NLog;

namespace dnGREP.WPF
{
    public class ThemedHighlightingManager : IHighlightingDefinitionReferenceResolver
    {
        public static ThemedHighlightingManager Instance { get; } = new();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly object lockObj = new();
        private readonly List<SyntaxDefinition> syntaxDefinitions = [];
        private readonly Dictionary<string, string> extensionToNameMap = [];
        private readonly HashSet<string> loadQueue = [];

        private readonly Dictionary<string, Lazy<IHighlightingDefinition?>> normalHighlightingsByName = [];
        private readonly Dictionary<string, Lazy<IHighlightingDefinition?>> invertedHighlightingsByName = [];

        private ThemedHighlightingManager()
        {
        }

        /// <summary>
        /// Gets the list of highlighting names
        /// </summary>
        public IEnumerable<string> HighlightingNames
        {
            get
            {
                lock (lockObj)
                {
                    return syntaxDefinitions
                        .Where(r => !string.IsNullOrEmpty(r.Name) && r.Extensions.Length != 0)
                        .Select(r => r.Name);
                }
            }
        }

        /// <summary>
        /// Gets a highlighting definition by name.
        /// Returns null if the definition is not found.
        /// </summary>
        public IHighlightingDefinition? GetDefinition(string name)
        {
            lock (lockObj)
            {
                IHighlightingDefinition? highlighting = null;

                if (loadQueue.Contains(name))
                {
                    throw new InvalidOperationException("Tried to create a highlighting definition recursively. Make sure the are no cyclic references between the highlighting definitions.");
                }

                loadQueue.Add(name);

                bool invertColors = (bool)Application.Current.Resources["PreviewText.SyntaxColor.Invert"];
                if (invertColors)
                {
                    if (invertedHighlightingsByName.TryGetValue(name, out Lazy<IHighlightingDefinition?>? definition))
                        highlighting = definition.Value;
                }
                else
                {
                    if (normalHighlightingsByName.TryGetValue(name, out Lazy<IHighlightingDefinition?>? definition))
                        highlighting = definition.Value;
                }

                loadQueue.Remove(name);

                return highlighting;
            }
        }

        /// <summary>
        /// Gets a highlighting definition by extension.
        /// Returns null if the definition is not found.
        /// </summary>
        public IHighlightingDefinition? GetDefinitionByExtension(string extension)
        {
            lock (lockObj)
            {
                string key = extension.ToLowerInvariant();
                if (extensionToNameMap.TryGetValue(key, out string? name))
                {
                    return GetDefinition(name);
                }
            }
            return null;
        }

        public void Initialize()
        {
            InitializeUserSyntaxDefinitions();
            InitializeSyntaxDefinitions();
        }

        private void InitializeSyntaxDefinitions()
        {
            var type = typeof(ThemedHighlightingManager);
            foreach (string name in type.Assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".xshd", StringComparison.Ordinal)))
            {
                using var stream = type.Assembly.GetManifestResourceStream(name);
                if (stream != null)
                {
                    using var reader = new XmlTextReader(stream);
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    if (!string.IsNullOrEmpty(xshd.Name) &&
                        !syntaxDefinitions.Any(sd => sd.Name == xshd.Name)) // exclude user defined syntaxes
                    {
                        var extensions = xshd.Extensions
                            .Select(s => s.TrimStart('*').ToLowerInvariant()).ToArray();
                        var syntax = new SyntaxDefinition(xshd.Name, name, extensions);
                        syntaxDefinitions.Add(syntax);

                        RegisterHighlighting(syntax);

                        foreach (var extension in extensions)
                        {
                            if (!extensionToNameMap.ContainsKey(extension))
                            {
                                extensionToNameMap.Add(extension, xshd.Name);
                            }
                        }
                    }
                }
            }
        }

        private void InitializeUserSyntaxDefinitions()
        {
            string dataFolder = DirectoryConfiguration.Instance.DataDirectory;
            if (!Directory.Exists(dataFolder))
            {
                return;
            }

            foreach (string fileName in Directory.GetFiles(dataFolder, "*.xshd", SearchOption.AllDirectories))
            {
                try
                {
                    using TextReader textReader = new StreamReader(fileName);
                    using XmlReader reader = XmlReader.Create(textReader);
                    XshdSyntaxDefinition xshd = HighlightingLoader.LoadXshd(reader);
                    if (string.IsNullOrEmpty(xshd.Name))
                    {
                        logger.Error($"Failed to load user syntax file '{fileName}': SyntaxDefinition name is missing.");
                    }
                    else
                    {
                        var existing = syntaxDefinitions.FirstOrDefault(s => s.Name == xshd.Name);
                        if (existing != null)
                        {
                            syntaxDefinitions.Remove(existing);
                        }
                        var extensions = xshd.Extensions
                            .Select(s => s.TrimStart('*').ToLowerInvariant()).ToArray();
                        var syntax = new SyntaxDefinition(xshd.Name, fileName, extensions);
                        syntaxDefinitions.Add(syntax);

                        RegisterHighlighting(syntax);

                        foreach (var extension in extensions)
                        {
                            if (!extensionToNameMap.ContainsKey(extension))
                            {
                                extensionToNameMap.Add(extension, xshd.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to load user syntax file '{fileName}': {ex.Message}");
                }
            }
        }

        private void RegisterHighlighting(SyntaxDefinition syntax)
        {
            if (syntax == null)
                return;

            lock (lockObj)
            {
                if (!normalHighlightingsByName.ContainsKey(syntax.Name))
                {
                    normalHighlightingsByName.Add(syntax.Name,
                        new Lazy<IHighlightingDefinition?>(() => LoadHighlightingDefinition(syntax, false)));
                }

                if (!invertedHighlightingsByName.ContainsKey(syntax.Name))
                {
                    invertedHighlightingsByName.Add(syntax.Name,
                        new Lazy<IHighlightingDefinition?>(() => LoadHighlightingDefinition(syntax, true)));
                }
            }
        }

        private IHighlightingDefinition? LoadHighlightingDefinition(SyntaxDefinition syntax, bool invertColors)
        {
            IHighlightingDefinition? highlighting;
            if (syntax.IsEmbeddedResource)
            {
                highlighting = LoadResourceHighlightingDefinition(syntax.FileName);
            }
            else
            {
                highlighting = LoadFileHighlightingDefinition(syntax.FileName);
            }

            if (highlighting != null && invertColors)
            {
                ColorInverter.TranslateThemeColors(highlighting);
            }

            return highlighting;
        }

        private IHighlightingDefinition? LoadFileHighlightingDefinition(string fileName)
        {
            try
            {
                using TextReader textReader = new StreamReader(fileName);
                using XmlReader reader = XmlReader.Create(textReader);
                return HighlightingLoader.Load(reader, this);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load user syntax file [{fileName}]: {ex.Message}");
            }
            return null;
        }

        private IHighlightingDefinition? LoadResourceHighlightingDefinition(string resourceName)
        {
            try
            {
                var type = typeof(ThemedHighlightingManager);
                using var stream = type.Assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new XmlTextReader(stream);
                    return HighlightingLoader.Load(reader, this);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load syntax file [{resourceName}]: {ex.Message}");
            }
            return null;
        }
    }

    public class SyntaxDefinition(string name, string fileName, string[] extensions)
    {
        public string Name { get; private set; } = name;
        public string FileName { get; private set; } = fileName;
        public bool IsEmbeddedResource { get; private set; } = !Path.IsPathRooted(fileName);
        public string[] Extensions { get; private set; } = extensions ?? [];
    }

}
