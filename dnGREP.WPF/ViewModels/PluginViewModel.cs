using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class PluginViewModel : CultureAwareViewModel
    {
        public PluginViewModel(PluginConfiguration plugin, List<string> defaultExtensions)
        {
            original = plugin;
            Name = plugin.Name;
            IsEnabled = plugin.Enabled;
            PreviewTextEnabled = plugin.PreviewText;
            MappedExtensions = plugin.Extensions;
            ApplyStringMap = plugin.ApplyStringMap;
            DefaultExtensions = GrepSettings.CleanExtensions(defaultExtensions);
        }

        public bool IsChanged => IsEnabled != original.Enabled ||
            PreviewTextEnabled != original.PreviewText ||
            GrepSettings.CleanExtensions(MappedExtensions) != original.Extensions || 
            ApplyStringMap != original.ApplyStringMap;

        public PluginConfiguration original;

        public string DefaultExtensions { get; private set; }

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool previewTextEnabled;

        [ObservableProperty]
        private bool applyStringMap;

        [ObservableProperty]
        private string mappedExtensions = string.Empty;

        [ObservableProperty]
        private string customExtensions = string.Empty;


        private RelayCommand? resetExtensionsCommand;
        public RelayCommand ResetExtensions => resetExtensionsCommand ??= new RelayCommand(
            p => MappedExtensions = DefaultExtensions,
            q => !MappedExtensions.Equals(DefaultExtensions, StringComparison.Ordinal));

        internal PluginConfiguration Save()
        {
            return IsChanged ? new(Name, IsEnabled, PreviewTextEnabled,
                GrepSettings.CleanExtensions(MappedExtensions), ApplyStringMap) : original;
        }
    }
}
