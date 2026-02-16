using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF.ViewModels
{
    public partial class PasswordViewModel : CultureAwareViewModel
    {
        public PasswordViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
            IsCaps = Keyboard.IsKeyToggled(Key.CapsLock);
        }

        internal void SavePassword(string password)
        {
            Password = password;
        }

        public string Password { get; private set; } = string.Empty;

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string subject = string.Empty;

        [ObservableProperty]
        private string details = string.Empty;

        partial void OnDetailsChanged(string value)
        {
            HasDetails = !string.IsNullOrWhiteSpace(value);
        }

        [ObservableProperty]
        private bool hasDetails;

        [ObservableProperty]
        private bool isRetry; 

        [ObservableProperty]
        private bool isCaps; 

    }
}
