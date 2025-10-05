using System.Collections.Generic;
using System.Unicode;

namespace dnGREP.Common
{
    public class CharacterMap
    {
        private static readonly List<KeyValuePair<SubstituteChar, SubstituteChar>> list = [];
        public static void LoadSpace()
        {
            UnicodeCharInfo from = UnicodeInfo.GetCharInfo(0xA0);
            UnicodeCharInfo to = UnicodeInfo.GetCharInfo(0x20);

            list.Add(new(
                new (char.ConvertFromUtf32(0xA0), 0xA0, from.Name),
                new (char.ConvertFromUtf32(0x20), 0x20, to.Name)));
        }
    }

    public record SubstituteChar(string Character, int CodePoint, string Name)
    { 
    }

}
