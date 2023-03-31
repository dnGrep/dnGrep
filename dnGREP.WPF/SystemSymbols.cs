using System.Drawing;

namespace dnGREP.WPF
{
    public static class SystemSymbols
    {
        public static string FontFamily { get; }
        public static string DropDownFontFamily { get; }
        public static string WindowChromeFontFamily { get; }
        public static float WindowChromeFontSize { get; }
        public static string DeleteCharacter { get; }
        public static string PinCharacter { get; }
        public static string UnpinCharacter { get; }
        public static string MinimizeCharacter { get; }
        public static string MaximizeCharacter { get; }
        public static string RestoreCharacter { get; }
        public static string CloseCharacter { get; }
        public static string DropDownArrowCharacter { get; }


        static SystemSymbols()
        {
            if (IsFontInstalled("Segoe Fluent Icons"))
            {
                FontFamily = "Segoe Fluent Icons";
                WindowChromeFontFamily = "Segoe Fluent Icons";
                WindowChromeFontSize = 10f;
                DeleteCharacter = char.ConvertFromUtf32(0xEA39);
                PinCharacter = char.ConvertFromUtf32(0xE718);
                UnpinCharacter = char.ConvertFromUtf32(0xE196);
                MinimizeCharacter = char.ConvertFromUtf32(0xE921);
                MaximizeCharacter = char.ConvertFromUtf32(0xE922);
                RestoreCharacter = char.ConvertFromUtf32(0xE923);
                CloseCharacter = char.ConvertFromUtf32(0xE8BB);
            }
            else if (IsFontInstalled("Segoe MDL2 Assets"))
            {
                FontFamily = "Segoe MDL2 Assets";
                WindowChromeFontFamily = "Segoe MDL2 Assets";
                WindowChromeFontSize = 10f;
                DeleteCharacter = char.ConvertFromUtf32(0xEA39);
                PinCharacter = char.ConvertFromUtf32(0xE718);
                UnpinCharacter = char.ConvertFromUtf32(0xE196);
                MinimizeCharacter = char.ConvertFromUtf32(0xE949);
                MaximizeCharacter = char.ConvertFromUtf32(0xE739);
                RestoreCharacter = char.ConvertFromUtf32(0xE923);
                CloseCharacter = char.ConvertFromUtf32(0xE8BB);
            }
            else
            {
                if (IsFontInstalled("Segoe UI Symbol"))
                {
                    FontFamily = "Segoe UI Symbol";
                    DeleteCharacter = char.ConvertFromUtf32(0xE106);
                    PinCharacter = char.ConvertFromUtf32(0xE141);
                    UnpinCharacter = char.ConvertFromUtf32(0xE196);
                }
                else
                {
                    FontFamily = "Wingdings 2";
                    DeleteCharacter = char.ConvertFromUtf32(0x55);
                    PinCharacter = char.ConvertFromUtf32(0xB9);
                    UnpinCharacter = char.ConvertFromUtf32(0xB8);
                }

                WindowChromeFontFamily = "Marlett";
                WindowChromeFontSize = 14;
                MinimizeCharacter = char.ConvertFromUtf32(0x30);
                MaximizeCharacter = char.ConvertFromUtf32(0x31);
                RestoreCharacter = char.ConvertFromUtf32(0x32);
                CloseCharacter = char.ConvertFromUtf32(0x72);
            }

            if (IsFontInstalled("Segoe UI Symbol"))
            {
                DropDownFontFamily = "Segoe UI Symbol";
                DropDownArrowCharacter = char.ConvertFromUtf32(0x23F7); // "⏷";
            }
            else
            {
                DropDownFontFamily = "Marlett";
                DropDownArrowCharacter = char.ConvertFromUtf32(0x75);
            }
        }

        private static bool IsFontInstalled(string familyName)
        {
            using (Font fontTester = new Font(familyName, 12))
            {
                return fontTester.Name == familyName;
            }
        }
    }
}
