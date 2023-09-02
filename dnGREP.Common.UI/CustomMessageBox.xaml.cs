using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace dnGREP.Common.UI
{
    /// <summary>
    /// A message box with additional button options and customizations
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        private CustomMessageBox()
        {
            InitializeComponent();

            cancelButton.Visibility = Visibility.Collapsed;
            okButton.Visibility = Visibility.Collapsed;
            noButton.Visibility = Visibility.Collapsed;
            yesButton.Visibility = Visibility.Collapsed;
            yesToAllButton.Visibility = Visibility.Collapsed;
            noToAllButton.Visibility = Visibility.Collapsed;
            doNotAskAgainCheckbox.Visibility = Visibility.Collapsed;
            doNotAskAgainCheckbox.IsChecked = false;

            SourceInitialized += (s, e) => NativeMethods.RemoveIcon(this);
        }

        public MessageBoxResultEx MessageBoxResultEx { get; private set; } = MessageBoxResultEx.None;

        public bool OnceOnly => doNotAskAgainCheckbox.IsChecked ?? false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                switch (btn.Name)
                {
                    case nameof(okButton):
                        MessageBoxResultEx = MessageBoxResultEx.OK;
                        break;
                    case nameof(cancelButton):
                        MessageBoxResultEx = MessageBoxResultEx.Cancel;
                        break;
                    case nameof(noButton):
                        MessageBoxResultEx = MessageBoxResultEx.No;
                        break;
                    case nameof(yesButton):
                        MessageBoxResultEx = MessageBoxResultEx.Yes;
                        break;
                    case nameof(yesToAllButton):
                        MessageBoxResultEx = MessageBoxResultEx.YesToAll;
                        break;
                    case nameof(noToAllButton):
                        MessageBoxResultEx = MessageBoxResultEx.NoToAll;
                        break;
                }
            }
            Close();
        }

        private void SetButtonVisibility(MessageBoxButtonEx button, MessageBoxResultEx defaultResult)
        {
            switch (button)
            {
                case MessageBoxButtonEx.OK:
                    okButton.Visibility = Visibility.Visible;
                    okButton.IsDefault = true;
                    break;
                case MessageBoxButtonEx.OKCancel:
                    cancelButton.Visibility = Visibility.Visible;
                    okButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResultEx.OK:
                        default:
                            okButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.Cancel:
                            cancelButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButtonEx.YesNo:
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResultEx.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.No:
                            noButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButtonEx.YesNoCancel:
                    cancelButton.Visibility = Visibility.Visible;
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResultEx.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.No:
                            noButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.Cancel:
                            cancelButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButtonEx.YesAllNoAll:
                    noToAllButton.Visibility = Visibility.Visible;
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    yesToAllButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResultEx.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.YesToAll:
                            yesToAllButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.No:
                            noButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.NoToAll:
                            noToAllButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButtonEx.YesAllNoAllCancel:
                    cancelButton.Visibility = Visibility.Visible;
                    noToAllButton.Visibility = Visibility.Visible;
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    yesToAllButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResultEx.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.YesToAll:
                            yesToAllButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.No:
                            noButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.NoToAll:
                            noToAllButton.IsDefault = true;
                            break;
                        case MessageBoxResultEx.Cancel:
                            cancelButton.IsDefault = true;
                            break;
                    }
                    break;
            }
        }

        private void SetButtonText(MessageBoxCustoms customs)
        {
            if (!string.IsNullOrEmpty(customs.DoNotAskAgainCheckboxText))
            {
                doNotAskAgainCheckbox.Content = customs. DoNotAskAgainCheckboxText;
            }
            if (!string.IsNullOrEmpty(customs.OKButtonText))
            {
                okButton.Content = customs.OKButtonText;
            }
            if (!string.IsNullOrEmpty(customs.CancelButtonText))
            {
                cancelButton.Content = customs.CancelButtonText;
            }
            if (!string.IsNullOrEmpty(customs.NoButtonText))
            {
                noButton.Content = customs.NoButtonText;
            }
            if (!string.IsNullOrEmpty(customs.YesButtonText))
            {
                yesButton.Content = customs.YesButtonText;
            }
            if (!string.IsNullOrEmpty(customs.NoToAllButtonText))
            {
                noToAllButton.Content = customs.NoToAllButtonText;
            }
            if (!string.IsNullOrEmpty(customs.YesToAllButtonText))
            {
                yesToAllButton.Content = customs.YesToAllButtonText;
            }
        }

        public static CustomMessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, string.Empty, MessageBoxButtonEx.OK, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }
        public static CustomMessageBoxResult Show(Window owner, string messageBoxText)
        {
            return Show(owner, messageBoxText, string.Empty, MessageBoxButtonEx.OK, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButtonEx.OK, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }
        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return Show(owner, messageBoxText, caption, MessageBoxButtonEx.OK, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButtonEx button)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }
        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButtonEx button)
        {
            return Show(owner, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResultEx.None, MessageBoxOptions.None);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon)
        {
            return Show(messageBoxText, caption, button, icon, MessageBoxResultEx.None, MessageBoxOptions.None);
        }
        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon)
        {
            return Show(owner, messageBoxText, caption, button, icon, MessageBoxResultEx.None, MessageBoxOptions.None);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }
        public static CustomMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult,
            MessageBoxOptions options)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, options);
        }

        public static CustomMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult,
            MessageBoxOptions options)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxCustoms.None, options);
        }

        public static CustomMessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult,
            MessageBoxCustoms customs, MessageBoxOptions options)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, customs, options);
        }

        public static CustomMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButtonEx button, MessageBoxImage icon, MessageBoxResultEx defaultResult,
            MessageBoxCustoms customs, MessageBoxOptions options)
        {
            var dlg = new CustomMessageBox();

            if (options.HasFlag(MessageBoxOptions.RtlReading))
            {
                dlg.FlowDirection = FlowDirection.RightToLeft;
            }
            if (options.HasFlag(MessageBoxOptions.RightAlign))
            {
                dlg.mbText.HorizontalAlignment = HorizontalAlignment.Right;
            }

            dlg.mbText.Text = messageBoxText;
            dlg.Title = caption;
            dlg.SetButtonVisibility(button, defaultResult);
            dlg.SetButtonText(customs);

            if (customs.ShowDoNotAskAgainCheckbox)
            {
                dlg.doNotAskAgainCheckbox.Visibility = Visibility.Visible;
            }
            if (customs.Icon != null)
            {
                dlg.mbIcon.Source = customs.Icon;
            }
            else if (icon == MessageBoxImage.None)
            {
                dlg.mbIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                dlg.mbIcon.Source = NativeMethods.GetIcon(icon);
            }


            if (owner != null)
            {
                dlg.Owner = owner;
            }
            else
            {
                var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                if (activeWindow != null)
                {
                    dlg.Owner = activeWindow;
                }
            }

            if (dlg.Owner != null)
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dlg.ShowDialog();

            return new CustomMessageBoxResult(dlg.MessageBoxResultEx, dlg.OnceOnly);
        }

        #region Native Methods
        static class NativeMethods
        {
            public static ImageSource GetIcon(MessageBoxImage image)
            {
                SHSTOCKICONINFO sii = new()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO))
                };

                Marshal.ThrowExceptionForHR(PInvoke.SHGetStockIconInfo(ToSHSTOCKICONID(image),
                        SHGSI_FLAGS.SHGSI_ICON,
                        ref sii));

                using Icon icon = System.Drawing.Icon.FromHandle(sii.hIcon);

                return ToImageSource(icon);
            }

            private static SHSTOCKICONID ToSHSTOCKICONID(MessageBoxImage image)
            {
                return image switch
                {
                    MessageBoxImage.Error => SHSTOCKICONID.SIID_ERROR,
                    MessageBoxImage.Warning => SHSTOCKICONID.SIID_WARNING,
                    MessageBoxImage.Question => SHSTOCKICONID.SIID_HELP,
                    _ => SHSTOCKICONID.SIID_INFO,
                };
            }

            private static ImageSource ToImageSource(Icon icon)
            {
                HGDIOBJ hBitmap = new(IntPtr.Zero);
                try
                {
                    hBitmap = new(icon.ToBitmap().GetHbitmap());

                    ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));

                    return wpfBitmap;
                }
                finally
                {
                    if (hBitmap != IntPtr.Zero)
                    {
                        PInvoke.DeleteObject(hBitmap);
                    }
                }
            }

            public static void RemoveIcon(Window window)
            {
                var hwnd = new HWND(new WindowInteropHelper(window).Handle);

                // Change the extended window style to not show a window icon
                var extendedStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, extendedStyle | (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME);

                PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, IntPtr.Zero);
                PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, IntPtr.Zero);
            }
        }
        #endregion
    }

    public class CustomMessageBoxResult
    {
        public CustomMessageBoxResult(MessageBoxResultEx result, bool doNotAskAgain)
        {
            Result = result;
            DoNotAskAgain = doNotAskAgain;
        }

        public MessageBoxResultEx Result { get; private set; }
        public bool DoNotAskAgain { get; private set; }
    }


    public enum MessageBoxResultEx
    {
        /// <summary>The message box returns no result.</summary>
        None = 0,
        /// <summary>The result value of the message box is OK.</summary>
        OK,
        /// <summary>The result value of the message box is Cancel.</summary>
        Cancel,
        /// <summary>The result value of the message box is Yes.</summary>
        Yes,
        /// <summary>The result value of the message box is No.</summary>
        No,
        /// <summary>The result value of the message box is Yes to All.</summary>
        YesToAll,
        /// <summary>The result value of the message box is No to All.</summary>
        NoToAll,
    }

    public enum MessageBoxButtonEx
    {
        /// <summary>The message box displays an OK button.</summary>
        OK = 0,
        /// <summary>The message box displays OK and Cancel buttons.</summary>
        OKCancel,
        /// <summary>The message box displays Yes, No, and Cancel buttons.</summary>
        YesNoCancel,
        /// <summary>The message box displays Yes and No buttons.</summary>
        YesNo,
        /// <summary>The message box displays Yes, Yes to all, No, No to all, and Cancel buttons.</summary>
        YesAllNoAllCancel,
        /// <summary>The message box displays Yes, Yes to all, No, and No to all buttons.</summary>
        YesAllNoAll,
    }

    public class MessageBoxCustoms
    {
        public static readonly MessageBoxCustoms None = new();
        public static readonly MessageBoxCustoms DoNotAskAgain = new() { ShowDoNotAskAgainCheckbox = true };

        /// <summary>
        /// Gets or sets a custom icon for the message box (see class for example)
        /// </summary>
        /// <example>
        /// var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        /// bitmapImage.BeginInit();
        /// bitmapImage.UriSource = new Uri("pack://application:,,,/dnGrep;component/images/dnGrep48.png");
        /// bitmapImage.EndInit();
        /// var customs = new MessageBoxCustoms() { Icon = bitmapImage };
        /// </example>
        public ImageSource? Icon { get; set; }
        public bool ShowDoNotAskAgainCheckbox { get; set; }
        public string DoNotAskAgainCheckboxText { get; set; } = string.Empty;
        public string OKButtonText { get; set; } = string.Empty;
        public string CancelButtonText { get; set; } = string.Empty;
        public string NoButtonText { get; set; } = string.Empty;
        public string YesButtonText { get; set; } = string.Empty;
        public string NoToAllButtonText { get; set; } = string.Empty;
        public string YesToAllButtonText { get; set; } = string.Empty;
    }


}
