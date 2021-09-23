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

        private readonly Dictionary<string, string> resources = new Dictionary<string, string>();

        public void ReadFile(string filePath)
        {
            resources.Clear();
            IetfLanguateTag = string.Empty;

            try
            {
                if (Path.GetExtension(filePath).Equals(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        int pos = fileName.IndexOf(".");
                        if (pos > -1)
                        {
                            var tag = fileName.Substring(pos + 1);
                            if (!string.IsNullOrEmpty(tag))
                            {
                                IetfLanguateTag = tag;

                                using (ResXResourceReader rsxr = new ResXResourceReader(filePath))
                                {
                                    // Iterate through the resources and display the contents to the console.
                                    foreach (DictionaryEntry d in rsxr)
                                    {
                                        resources.Add(d.Key.ToString(), d.Value.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(TranslationSource.Format(Properties.Resources.MessageBox_ResourcesFile0IsNotAResxFile, filePath),
                        Properties.Resources.MessageBox_DnGrep + "  " + Properties.Resources.MessageBox_LoadResources, 
                        MessageBoxButton.OK, MessageBoxImage.Error, 
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex, $"Failed to load resources file '{filePath}'");
                MessageBox.Show(TranslationSource.Format(Properties.Resources.MessageBox_CouldNotLoadResourcesFile0, filePath) + ex.Message,
                    Properties.Resources.MessageBox_DnGrep + "  " + Properties.Resources.MessageBox_LoadResources, 
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(IetfLanguateTag) && resources.Count > 0;

        public string IetfLanguateTag { get; private set; } = string.Empty;


        public IDictionary<string, string> Resources => resources;
    }
}
