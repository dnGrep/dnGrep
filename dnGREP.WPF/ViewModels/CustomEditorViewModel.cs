using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using dnGREP.WPF.MVHelpers;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public partial class CustomEditorViewModel : CultureAwareViewModel
    {
        public CustomEditorViewModel(CustomEditor editor, bool isDefault)
        {
            original = editor;
            Label = editor.Label;
            Path = editor.Path;
            Args = editor.Args;
            EscapeQuotes = editor.EscapeQuotes;
            Extensions = editor.Extensions;
            IsDefault = originalIsDefault = isDefault;

            EscapeQuotesLabel = TranslationSource.Format(Resources.Options_CustomEditorEscapeQuotes, Match);

            int count = TranslationSource.CountPlaceholders(Resources.Options_CustomEditorHelp);
            if (count == 5)
            {
                CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                    File, Line, Pattern, Match, Column);
            }
            else
            {
                CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                    File, Line, Pattern, Match, Column, Page);
            }

            CustomEditorTemplates = [.. ConfigurationTemplate.EditorConfigurationTemplates];

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        private const string File = "%file";
        private const string Page = "%page";
        private const string Line = "%line";
        private const string Pattern = "%pattern";
        private const string Match = "%match";
        private const string Column = "%column";
        private static readonly string ellipsis = char.ConvertFromUtf32(0x2026);
        private static readonly char[] separators = [',', ';', ' '];

        private readonly CustomEditor original;
        private readonly bool originalIsDefault;
        private CustomEditor? changed = null;

        public bool IsChanged =>
            Label != original.Label ||
            Path != original.Path ||
            Args != original.Args ||
            EscapeQuotes != original.EscapeQuotes ||
            GrepSettings.CleanExtensions(Extensions) != original.Extensions ||
            IsDefault != originalIsDefault;

        public string ExtensionSummary => string.IsNullOrEmpty(Extensions) ? "*" : Extensions;

        [ObservableProperty]
        private string label;

        [ObservableProperty]
        private string path;

        [ObservableProperty]
        private string args;

        [ObservableProperty]
        private bool escapeQuotes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExtensionSummary))]
        private string extensions;

        [ObservableProperty]
        private bool isDefault;

        [ObservableProperty]
        private string escapeQuotesLabel = string.Empty;

        [ObservableProperty]
        private string customEditorHelp = string.Empty;

        [ObservableProperty]
        private string applicationFontFamily;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private ConfigurationTemplate? customEditorTemplate = null;
        partial void OnCustomEditorTemplateChanged(ConfigurationTemplate? value)
        {
            ApplyCustomEditorTemplate(value);
        }

        public KeyValuePair<string, ConfigurationTemplate?>[] CustomEditorTemplates { get; }

        public ICommand EditCustomEditorCommand => new RelayCommand(
            p => EditCustomEditor());

        public ICommand DeleteCustomEditorCommand => new RelayCommand(
            p => OptionsViewModel.OptionsMessenger.NotifyColleagues("DeleteCustomEditor", this));

        public ICommand BrowseEditorCommand => new RelayCommand(
            p => BrowseToEditor());

        public ICommand SaveCommand => new RelayCommand(
            p => CommitChanges(),
            q => IsChanged);

        internal bool EditCustomEditor()
        {
            CustomEditorWindow dlg = new()
            {
                DataContext = this
            };
            var result = dlg.ShowDialog();
            return result ?? false;
        }

        public CustomEditor Save()
        {
            return IsChanged && changed != null ? changed : original;
        }

        private void CommitChanges()
        {
            // extensions are saved comma separated without spaces
            changed = new(Label, Path, Args, EscapeQuotes, GrepSettings.CleanExtensions(Extensions));

            if (IsDefault != originalIsDefault)
            {
                OptionsViewModel.OptionsMessenger.NotifyColleagues("SetAsDefault", this);
            }
        }

        private void BrowseToEditor()
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Path = dlg.FileName;
            }
        }

        private void ApplyCustomEditorTemplate(ConfigurationTemplate? template)
        {
            if (template != null)
            {
                Label = template.Label;
                Path = ellipsis + template.ExeFileName;
                Args = template.Arguments;

                Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    UIServices.SetBusyState();
                    string fullPath = ConfigurationTemplate.FindExePath(template);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        Label = template.Label;
                        Path = fullPath;
                        Args = template.Arguments;
                    }
                }, DispatcherPriority.ApplicationIdle);
            }
        }
    }
}
