using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace dnGREP.WPF
{
    static class ShellIntegration
    {
        unsafe public static void ShowFileProperties(string filename)
        {
            fixed (char* verb = "properties", file = filename)
            {
                SHELLEXECUTEINFOW info = new()
                {
                    cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
                    lpVerb = new(verb),
                    lpFile = new(file),
                    nShow = (int)SHOW_WINDOW_CMD.SW_SHOW,
                    fMask = PInvoke.SEE_MASK_INVOKEIDLIST,
                };
                PInvoke.ShellExecuteEx(ref info);
            }
        }
    }
}
