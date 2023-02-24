using System.Collections.Generic;

namespace dnGREP.Everything
{
    public static class EverythingKeywords
    {
        public static readonly List<string> PathPrefixes = new()
        {
            "parent:",
            "infolder:",
            "nosubfolders:",
        };


        public static readonly List<string> Wildcards = new()
        {
            "*",
            "?",
        };

        public static readonly List<string> UnicodeChars = new()
        {
            "#<n>:", // Literal unicode character <n> in decimal.
            "#x<n>:", // Literal unicode character <n> in hexadecimal.
        };

        public static readonly List<string> Macros = new()
        {
            "quot:", // Literal double quote "
            "apos:", // Literal apostrophe '
            "amp:", // Literal ampersand &
            "lt:", // Literal less than <
            "gt:", // Literal greater than >
            "audio:", // Search for audio files.
            "zip:", // Search for compressed files.
            "doc:", // Search for document files.
            "exe:", // Search for executable files.
            "pic:", // Search for picture files.
            "video:", // Search for video files.
        };

        public static readonly List<string> Modifiers = new()
        {
            "ascii:", // Enable or disable fast ASCII case comparisons.
            "utf8:",
            "noascii:",
            "case:", // Match or ignore case.
            "nocase:",
            "diacritics:", // Match or ignore accent marks.
            "nodiacritics:",
            "file:", // Match files only.
            "files:",
            "nofileonly:",
            "folder:", // Match folders only.
            "folders:",
            "nofolderonly:",
            "path:", // Match the full path and file name or just the filename.
            "nopath:",
            "regex:", // Enable or disable regex.
            "noregex:",
            "wfn:", // Match the whole filename or match anywhere in the filename.
            "wholefilename:",
            "nowfn:",
            "nowholefilename:",
            "wholeword:", // Match whole words or match anywhere in the filename.
            "ww:",
            "nowholeword:",
            "noww:",
            "wildcards:", // Enable or disable wildcards.
            "nowildcards:",
        };

        public static readonly List<string> Functions = new()
        {
            "album:", //<album>  Search for the ID3 or FLAC album.
            "artist:", //<artist> Search for the ID3 or FLAC artist.
            "attrib:", //<attributes>
            "attributes:", //<attributes> Search for files and folders with the specified file attributes.
            "bitdepth:", //<bitdepth> Find images with the specified bits per pixel.
            "child:",  //<filename>  Search for folders that contain a child file or folder with a matching filename.
            "childcount:", //<count> Search for folders that contain the specified number of subfolders and files.
            "childfilecount:", //<count>  Search for folders that contain the specified number of files.
            "childfoldercount:", //<count>    Search for folders that contain the specified number of subfolders.
            "comment:", //<comment>   Search for the ID3 or FLAC comment.
            "content:", //
            "ansicontent:", //
            "utf8content:", //
            "utf16content:", //
            "utf16becontent:", // Search file content.
            "count:", //<max> Limit the number of results to max.
            "dateaccessed:", //<date>
            "da:", //<date>   Search for files and folders with the specified date accessed.
            "datecreated:", //<date>
            "dc:", //<date>   Search for files and folders with the specified date created.
            "datemodified:", //<date>
            "dm:", //<date>   Search for files and folders with the specified date modified.
            "daterun:", //<date>
            "dr:", //<date>   Search for files and folders with the specified date run.
            "depth:", //<count>
            "parents:", //<count> Search for files and folders with the specified folder depth.
            "dimension:", //<width>x<height>  Find images with the specified width and height.
            "dupe:", //
            "namepartdupe:", //
            "attribdupe:", //
            "dadupe:", //
            "dcdupe:", //
            "dmdupe:", //
            "sizedupe:", // Search for duplicated files.
            "empty:", // Search for empty folders.
            "endwith:", //<text>  Filenames (including extension) ending with text.
            "ext:", //<list>  Search for files with a matching extension in the specified semicolon delimited extension list.
            "filelist:", //<list> Search for a list of file names in the specified pipe (|) delimited file list.
            "filelistfilename:", //<filename> Search for files and folders belonging to the file list filename.
            "frn:", //<frnlist>   Search for files and folders with the specified semicolon delimited File Reference Numbers.
            "fsi:", //<index> Search for files and folders in the specified zero based internal file system index.
            "genre:", //<genre> Search for the ID3 or FLAC genre.
            "height:", //<height> Search for images with the specified height in pixels.
            "len:", //<length> Search for files and folders that match the specified filename length.
            "orientation:", //<type> Search for images with the specified orientation(landscape or portrait).
            "parent:", //<path>
            "infolder:", //<path>
            "nosubfolders:", //<path> Search for files and folders in the specified path, excluding subfolders.
            "recentchange:", //<date>
            "rc:", //<date> Search for files and folders with the specified recently changed date.
            "root:", // Search for files and folders with no parent folder.
            "runcount:", //<count> Search for files and folders with the specified run count.
            "shell:", //<name> Search for a known shell folder name, including subfolders and files.
            "size:", //<size> Search for files with the specified size in bytes.
            "startwith:", //<text> Search for filenames starting with text.
            "title:", //<title> Search for the ID3 or FLAC title.
            "type:", //<type> Search for files and folders with the specified type.
            "width:", //<width> Search for images with the specified width in pixels.
        };

        public static readonly List<string> SizeConstraints = new()
        {
            "empty",    //
            "tiny",     //  0 KB < size <= 10 KB
            "small",    //  10 KB < size <= 100 KB
            "medium",   //  100 KB < size <= 1 MB
            "large",    //  1 MB < size <= 16 MB
            "huge",     //  16 MB < size <= 128 MB
            "gigantic", //  size > 128 MB
            "unknown",  //
        };

        public static readonly List<string> ID3Tags = new()
        {
            "track:",  //<track> Track number or track range.
            "year:",   //<year>  Year or year range.
            "title:",  //<title> Song title.
            "artist:", //<artist> Song Artist.
            "album:",  //<album>  Album name.
            "comment:",//<comment> Track comment.
            "genre:",  //<genre>  Track genre.
        };

        public static readonly List<string> ImageInfo = new()
        {
            "width:",  //<width>   The width of the image in pixels.
            "height:",  //<height> The height of the image in pixels.
            "dimensions:",  //<width>x<height> The width and height of the image in pixels. Use a x to separate the width and height.
            "orientation:",  //<type>  <type> can landscape or portrait.
            "bitdepth:",  //<bitdepth> Find images with the specified bits per pixel.
        };

        public static readonly List<string> DuplicatedFiles = new()
        {
            "dupe:",  // Find files and folders with the same filename.
            "attribdupe:",  // Find files and folders with the same attributes. Sort by attributes for the best results.
            "dadupe:",  // Find files and folders with the same date accessed. Sort by date accessed for the best results.
            "dcdupe:",  // Find files and folders with the same date created. Sort by date created for the best results.
            "dmdupe:",  // Find files and folders with the same date modified. Sort by date modified for the best results.
            "namepartdupe:",  // Find files and folders with the same name part (excluding extension).
            "sizedupe:",  // Find files and folders with the same size. Sort by size for the best results.
        };
    }
}
