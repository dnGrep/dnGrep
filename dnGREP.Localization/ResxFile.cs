using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Windows;
using NLog;

namespace dnGREP.Localization
{
    internal class ResxFile
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, string> resources = new();

        public void ReadFile(string filePath)
        {
            resources.Clear();
            IetfLanguageTag = string.Empty;

            try
            {
                if (Path.GetExtension(filePath).Equals(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        int pos = fileName.IndexOf(".");
                        if (pos < 0 && fileName.Contains("resourcesresx"))
                        {
                            // try the file name format used by Transifex downloads:
                            // for_use_dngrep-application_resourcesresx_he.resx
                            // for_use_dngrep-application_resourcesresx_he_IL.resx
                            // get the first underscore after the first 'resx'
                            pos = fileName.IndexOf("resx");
                            if (pos > -1)
                            {
                                pos = fileName.IndexOf("_", pos);
                            }
                        }
                        if (pos < 0)
                        {
                            // try the file name format used by Weblate downloads:
                            // dngrep-dngrep-application-de.resx
                            // dngrep-dngrep-application-zh_Hans.resx
                            // dngrep-dngrep-application-zh_Hans (1).resx
                            // get the last dash in the filename
                            pos = fileName.LastIndexOf("-");
                        }
                        if (pos > -1)
                        {
                            var tag = fileName[(pos + 1)..].Replace('_', '-');
                            if (!string.IsNullOrEmpty(tag) && tag.Contains('('))
                            {
                                // remove file numbers: 'he (1)'
                                pos = tag.IndexOf("(");
                                if (pos > -1)
                                {
                                    tag = tag[..pos].Trim();
                                }
                            }
                            if (!string.IsNullOrEmpty(tag))
                            {
                                IetfLanguageTag = tag;

                                using ResXResourceReader rsxr = new(filePath);
                                foreach (DictionaryEntry d in rsxr)
                                {
                                    var key = d.Key.ToString();
                                    var value = d.Value?.ToString();
                                    if (key != null && value != null)
                                    {
                                        resources.Add(key, value);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(TranslationSource.Format(Properties.Resources.MessageBox_ResourcesFile0IsNotAResxFile, filePath),
                        Properties.Resources.MessageBox_DnGrep + " " + Properties.Resources.MessageBox_LoadResources,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load resources file '{filePath}'");
                MessageBox.Show(TranslationSource.Format(Properties.Resources.MessageBox_CouldNotLoadResourcesFile0, filePath) + ex.Message,
                    Properties.Resources.MessageBox_DnGrep + " " + Properties.Resources.MessageBox_LoadResources,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(IetfLanguageTag) && resources.Count > 0;

        public string IetfLanguageTag { get; private set; } = string.Empty;


        public IDictionary<string, string> Resources => resources;
    }
}
