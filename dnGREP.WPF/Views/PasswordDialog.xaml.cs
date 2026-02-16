using System.Windows;
using System.Windows.Input;
using dnGREP.WPF.ViewModels;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for PasswordDialog.xaml
    /// </summary>
    public partial class PasswordDialog : ThemedWindow
    {
        private readonly PasswordViewModel vm = new();

        public PasswordDialog()
        {
            InitializeComponent();

            DataContext = vm;
        }

        public PasswordViewModel ViewModel => vm;

        private void ThemedWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            vm.IsCaps = Keyboard.IsKeyToggled(Key.CapsLock);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            passwordUnmask.Visibility = Visibility.Visible;
            passwordHidden.Visibility = Visibility.Hidden;
            passwordUnmask.Text = passwordHidden.Password;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordUnmask.Visibility = Visibility.Hidden;
            passwordHidden.Visibility = Visibility.Visible;
            passwordHidden.Password = passwordUnmask.Text;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (showCheckbox.IsChecked.HasValue && showCheckbox.IsChecked.Value)
            {
                vm.SavePassword(passwordUnmask.Text);
            }
            else
            {
                vm.SavePassword(passwordHidden.Password);
            }
            DialogResult = true;
            Close();
        }
    }
}
