using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class MenuItemViewModel : CultureAwareViewModel
    {
        public MenuItemViewModel(string? header, RelayCommand? relayCommand)
        {
            if (string.IsNullOrEmpty(header) && relayCommand == null)
            {
                IsSeparator = true;
            }
            else
            {
                Header = (header ?? string.Empty).Replace("_", "__", StringComparison.Ordinal);
                Command = relayCommand;
            }

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
        }

        public MenuItemViewModel(string header, bool isEnabled)
        {
            Header = (header ?? string.Empty).Replace("_", "__", StringComparison.Ordinal);
            Command = null;
            IsEnabled = isEnabled;
        }

        public MenuItemViewModel(string header, bool isCheckable, RelayCommand relayCommand)
            : this(header, relayCommand)
        {
            IsCheckable = isCheckable;
        }

        public ObservableCollection<MenuItemViewModel> Children { get; } = [];

        [ObservableProperty]
        private bool isSeparator = false;

        [ObservableProperty]
        private string header = string.Empty;

        [ObservableProperty]
        private bool isChecked = false;

        [ObservableProperty]
        private bool isCheckable = false;

        [ObservableProperty]
        private bool isEnabled = true;

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double mainFormFontSize;

        public ICommand? Command { get; }
    }
}
