using System.Windows;
using dnGREP.Common;

namespace dnGREP.WPF.Services
{
    public class PasswordService : IPassword
    {
        public static object lockObject = new();

        public string RequestPassword(string subject, string details, bool isRetry)
        {
            lock (lockObject)
            {
                string password = string.Empty;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // All code inside this lambda runs on the UI thread
                    PasswordDialog dlg = new PasswordDialog();
                    dlg.ViewModel.Subject = subject;
                    dlg.ViewModel.Details = details;
                    dlg.ViewModel.IsRetry = isRetry;

                    var result = dlg.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        password = dlg.ViewModel.Password;
                    }
                });

                return password;
            }
        }
    }
}
