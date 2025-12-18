using System.Collections.Generic;
using System.Windows;
using dnGREP.Common;

namespace dnGREP.WPF.Services
{
    public class PasswordService : IPassword
    {
        private static readonly object lockObject = new();
        private readonly Dictionary<string, string> passwordCache = [];

        public string RequestPassword(string subject, string details, bool isRetry)
        {
            lock (lockObject)
            {
                string key = subject + "|" + details;
                if (isRetry)
                {
                    // retrying, so previous password was incorrect
                    passwordCache.Remove(key);
                }
                else if (passwordCache.TryGetValue(key, out string? value) &&
                    !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                string password = string.Empty;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // All code inside this lambda runs on the UI thread
                    PasswordDialog dlg = new();
                    dlg.ViewModel.Subject = subject;
                    dlg.ViewModel.Details = details;
                    dlg.ViewModel.IsRetry = isRetry;

                    var result = dlg.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        password = dlg.ViewModel.Password;
                    }
                });

                passwordCache[key] = password;

                return password;
            }
        }
    }
}
