using System;

namespace dnGREP.Everything
{
    [Flags]
    public enum RequestFlags : uint
    {
        FileName = 0x00000001,
        Path = 0x00000002,
        FullPathAndFileName = 0x00000004,
        FileExtension = 0x00000008,
        Size = 0x00000010,
        DateCreated = 0x00000020,
        DateModified = 0x00000040,
        DateAccessed = 0x00000080,
        Attributes = 0x00000100,
        FileListFilename = 0x00000200,
        RunCount = 0x00000400,
        DateRun = 0x00000800,
        DateRecentlyChanged = 0x00001000,
        HighlightedFileName = 0x00002000,
        HighlightedPath = 0x00004000,
        HighlightedFullPathAndFileName = 0x00008000,
    }

    public enum SortType : uint
    {
        NameAscending,
        NameDescending,
        PathAscending,
        PathDescending,
        SizeAscending,
        SizeDescending,
        ExtensionAscending,
        ExtensionDescending,
        TypeNameAscending,
        TypeNameDescending,
        DateCreatedAscending,
        DateCreatedDescending,
        DateModifiedAscending,
        DateModifiedDescending,
        AttributesAscending,
        AttributesDescending,
        FileListFileNameAscending,
        FileListFileNameDescending,
        RunCountAscending,
        RunCountDescending,
        DateRecentlyChangedAscending,
        DateRecentlyChangedDescending,
        DateAccessedAscending,
        DateAccessedDescending,
        DateRunAscending,
        DateRunDescending,
    }
}
