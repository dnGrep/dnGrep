using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            onceCheckbox.Visibility = Visibility.Collapsed;
            onceCheckbox.IsChecked = false;

            SourceInitialized += (s, e) => NativeMethods.RemoveIcon(this);
        }

        public MessageBoxResultEx MessageBoxResultEx { get; private set; } = MessageBoxResultEx.None;

        public bool OnceOnly => onceCheckbox.IsChecked ?? false;

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
            if (!string.IsNullOrEmpty(customs.OnceCheckboxText))
            {
                onceCheckbox.Content = customs.OnceCheckboxText;
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
        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption,
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

        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption,
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

        public static CustomMessageBoxResult Show(Window owner, string messageBoxText, string caption,
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

            if (customs.ShowOnceCheckbox)
            {
                dlg.onceCheckbox.Visibility = Visibility.Visible;
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
                SHSTOCKICONINFO sii = new SHSTOCKICONINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO))
                };

                Marshal.ThrowExceptionForHR(SHGetStockIconInfo(ToSHSTOCKICONID(image),
                        SHGSI.SHGSI_ICON,
                        ref sii));

                using (Icon icon = System.Drawing.Icon.FromHandle(sii.hIcon))
                {
                    return ToImageSource(icon);
                }
            }

            private static SHSTOCKICONID ToSHSTOCKICONID(MessageBoxImage image)
            {
                switch (image)
                {
                    default:
                    case MessageBoxImage.Information:
                        return SHSTOCKICONID.SIID_INFO;
                    case MessageBoxImage.Error:
                        return SHSTOCKICONID.SIID_ERROR;
                    case MessageBoxImage.Warning:
                        return SHSTOCKICONID.SIID_WARNING;
                    case MessageBoxImage.Question:
                        return SHSTOCKICONID.SIID_HELP;
                }
            }

            private static ImageSource ToImageSource(Icon icon)
            {
                IntPtr hBitmap = IntPtr.Zero;
                try
                {
                    hBitmap = icon.ToBitmap().GetHbitmap();

                    ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));

                    return wpfBitmap;
                }
                finally
                {
                    if (hBitmap != IntPtr.Zero && !DeleteObject(hBitmap))
                    {
                        throw new Win32Exception();
                    }
                }
            }

            public static void RemoveIcon(Window window)
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;

                // Change the extended window style to not show a window icon
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

                SendMessage(hwnd, WM_SETICON, ICON_BIG, IntPtr.Zero);
                SendMessage(hwnd, WM_SETICON, ICON_SMALL, IntPtr.Zero);
            }

            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_DLGMODALFRAME = 0x0001;
            private const int WM_SETICON = 0x0080;
            private const int ICON_BIG = 0x0;
            private const int ICON_SMALL = 0x1;

            private enum SHSTOCKICONID : uint
            {
                SIID_HELP = 23,
                SIID_WARNING = 78,
                SIID_INFO = 79,
                SIID_ERROR = 80,
            }

            [Flags]
            private enum SHGSI : uint
            {
                SHGSI_ICONLOCATION = 0,
                SHGSI_ICON = 0x000000100,
                SHGSI_SYSICONINDEX = 0x000004000,
                SHGSI_LINKOVERLAY = 0x000008000,
                SHGSI_SELECTED = 0x000010000,
                SHGSI_LARGEICON = 0x000000000,
                SHGSI_SMALLICON = 0x000000001,
                SHGSI_SHELLICONSIZE = 0x000000004
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct SHSTOCKICONINFO
            {
                public uint cbSize;
                public IntPtr hIcon;
                public int iSysIconIndex;
                public int iIcon;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260/*MAX_PATH*/)]
                public string szPath;
            }

            [DllImport("Shell32.dll", SetLastError = false)]
            private static extern int SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);

            [DllImport("gdi32.dll", SetLastError = true)]
            private static extern bool DeleteObject(IntPtr hObject);

            [DllImport("user32.dll")]
            private static extern int GetWindowLong(IntPtr hwnd, int index);

            [DllImport("user32.dll")]
            private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, int wParam, IntPtr lParam);
        }
        #endregion
    }

    public class CustomMessageBoxResult
    {
        public CustomMessageBoxResult(MessageBoxResultEx result, bool onceOnly)
        {
            Result = result;
            OnceOnly = onceOnly;
        }

        public MessageBoxResultEx Result { get; private set; }
        public bool OnceOnly { get; private set; }
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
        public static MessageBoxCustoms None = new MessageBoxCustoms();
        public static MessageBoxCustoms Once = new MessageBoxCustoms { ShowOnceCheckbox = true };

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
        public ImageSource Icon { get; set; }
        public bool ShowOnceCheckbox { get; set; }
        public string OnceCheckboxText { get; set; }
        public string OKButtonText { get; set; }
        public string CancelButtonText { get; set; }
        public string NoButtonText { get; set; }
        public string YesButtonText { get; set; }
        public string NoToAllButtonText { get; set; }
        public string YesToAllButtonText { get; set; }
    }


}
