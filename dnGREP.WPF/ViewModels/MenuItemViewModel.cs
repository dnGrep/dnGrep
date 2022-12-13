using System.Collections.ObjectModel;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class MenuItemViewModel : CultureAwareViewModel
    {
        public MenuItemViewModel(string header, RelayCommand relayCommand)
        {
            if (string.IsNullOrEmpty(header) && relayCommand == null)
            {
                IsSeparator = true;
            }
            else
            {
                Header = header;
                command = relayCommand;
            }

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
        }

        public MenuItemViewModel(string header, bool isCheckable, RelayCommand relayCommand)
            : this(header, relayCommand)
        {
            IsCheckable = isCheckable;
        }

        public ObservableCollection<MenuItemViewModel> Children { get; } = new ObservableCollection<MenuItemViewModel>();


        private bool isSeparator = false;
        public bool IsSeparator
        {
            get { return isSeparator; }
            set
            {
                if (isSeparator == value)
                {
                    return;
                }

                isSeparator = value;
                OnPropertyChanged(nameof(IsSeparator));
            }
        }

        private string header = string.Empty;
        public string Header
        {
            get { return header; }
            set
            {
                if (header == value)
                {
                    return;
                }

                header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        private bool isChecked = false;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value)
                {
                    return;
                }

                isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        private bool isCheckable = false;
        public bool IsCheckable
        {
            get { return isCheckable; }
            set
            {
                if (isCheckable == value)
                {
                    return;
                }

                isCheckable = value;
                OnPropertyChanged(nameof(IsCheckable));
            }
        }

        private bool isEnabled = true;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                {
                    return;
                }

                isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        private RelayCommand command;
        public ICommand Command
        {
            get { return command; }
        }

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private double mainFormfontSize;
        public double MainFormFontSize
        {
            get { return mainFormfontSize; }
            set
            {
                if (mainFormfontSize == value)
                    return;

                mainFormfontSize = value;
                base.OnPropertyChanged(nameof(MainFormFontSize));
            }
        }

    }
}
