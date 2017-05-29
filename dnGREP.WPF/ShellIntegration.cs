using System;
using System.Runtime.InteropServices;

namespace dnGREP.WPF
{
    class ShellIntegration
    {
        public static void ShowFileProperties(string filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.Size = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.Verb = "properties";
            info.File = filename;
            info.Show = SW_SHOW;
            info.Mask = SEE_MASK_INVOKEIDLIST;
            ShellExecuteEx(ref info);
        }

        //public static void OpenFolder(string filename)
        //{
        //    SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
        //    info.Size = System.Runtime.InteropServices.Marshal.SizeOf(info);
        //    info.Verb = "explore";
        //    info.File = filename;
        //    info.Show = SW_SHOWNORMAL;
        //    info.Class = "folder";
        //    info.Mask = SEE_MASK_IDLIST | SEE_MASK_CLASSNAME;
        //    ShellExecuteEx(ref info);
        //}

        private const int SW_SHOW = 5;
        private const int SW_SHOWNORMAL = 1;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        private const uint SEE_MASK_IDLIST = 4;
        private const uint SEE_MASK_CLASSNAME = 1;

        [DllImport("shell32.dll")]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [Serializable]
        public struct SHELLEXECUTEINFO
        {
            public int Size;
            public uint Mask;
            public IntPtr hwnd;
            public string Verb;
            public string File;
            public string Parameters;
            public string Directory;
            public uint Show;
            public IntPtr InstApp;
            public IntPtr IDList;
            public string Class;
            public IntPtr hkeyClass;
            public uint HotKey;
            public IntPtr Icon;
            public IntPtr Monitor;
        }
    }
}
