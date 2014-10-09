.NET implementation of GREP tool with GUI. Requires Microsoft .NET *4.0* to run ([http://msdn.microsoft.com/en-us/netframework/aa569263.aspx]). Version 2.0+ is implemented using WPF framework.

Check [Introduction user guide] for screenshots and details.

## Features
   * [ShellIntegration Shell integration] (ability to search from explorer)
   * Plain text/[RegularExpressions regex]/[XPath] search (including case-insensitive search)
   * [Phonetic] search (using Bitap and Needleman-Wunch algorithms)
   * File move/copy/delete actions
   * Search inside archives (via plug-in)
   * Search MS Word documents (via plug-in)
   * Search PDF documents (via plug-in)
   * [Undo] functionality
   * Optional integration with text editor (like notepad++)
   * [Bookmarks] (ability to save regex searches for the future)
   * Pattern test form
   * Search result highlighting
   * Search result preview
   * Does not require installation (can be run from USB drive)
   * and more...

http://dngrep.googlecode.com/hg/Images/grep-main.jpg

_Main Screen_

http://dngrep.googlecode.com/hg/Images/options-window.jpg

_Options_

http://dngrep.googlecode.com/hg/Images/shell-integration.jpg

_Shell Integration_

### dnGREP 2.7.2 is out

   * Fixed lost settings when opening options (issue 170)
   * Fixed lost settings when opening test window (issue 173)

### dnGREP 2.7.1 Beta 2 is out

   * Moved [Introduction help file] to Google Code wiki and linked it from the tool
   * Fixed missing file path in Win XP and Win 7 (issue 164)
   * Fixed bug with multiline search (issue 165)
   * Fixed preview windows appearing even after show preview is unchecked (issue 166)
   * Multiline related fixes (issue 136 and issue 167)

### dnGREP 2.7.0 Beta is out
   * Updated UI (including Windows 8 styling)
   * Brought back "context lines" (issue 142)
   * Improved memory usage and performance with large search results (issue 123)
   * Improved XPath search highlighting
   * Added support for shebang based file selection (issue 138) - e.g. "Paths that match" and "Paths to ignore" can now contain patterns such as "#!*python;#!*sh"
   * Added ability to start search from [CommandLine command line] by passing in query as a last argument (issue 159) - e.g. {{{dnGrep.exe "c:\path" "\s\w+"}}}
   * Added keyword "%pattern" for alternate editor setup (issue 158)
   * Implemented option to stop after first match (issue 150)
   * Added CSV copy to clipboard (issue 144)
   * Added menu option to always allow opening with either custom or default editor (issue 152)
   * Refactored to use MVVM pattern and migrated to XUnit for unit testing
   * Fix for the test tool (issue 136)
   * Misc bug fixes (issue 141, issue 149)


### dnGREP 2.6.3 is out
Thanks to all beta testers for helping me find all of the bugs with the 2.6 release. I've called it 2.6.3 to differentiate binaries from the beta builds. Below is a cumulative list of changes in 2.6.3 from 2.5:
   * Significantly improved search performance (~5-10 times) and reduced memory footprint (issue 123)
   * Improved start-up time (issue 105)
   * Preview window with "sticky" behaviour (issue 116)
   * Increased results space by collapsing file filters
   * Added regex and xpath validation (issue 131)
   * Disable file filters when single file is selected (issue 115)
   * Added restore of window size (issue 104)
   * Added ability to exclude folders from search path (issue 103)
   * Improved open file location by selecting file as well
   * Enabled advance >> button when search for is not used (issue 119)
   * Added ability to generate GUIDs (using $(guid) in replace)
   * Minor enhancement to pattern test window (issue 77)
   * Improved shell integration (issue 96, issue 97)
   * Misc bug fixes (issue 100, issue 106, issue 99, issue 111, issue 93, issue 94, issue 95, issue 114, issue 117, issue 121, issue 123, issue 124, issue 127, issue 128, issue 129)
   * Fixed misc. issues in the first Beta (thanks everyone who tested it)

### dnGREP 2.6.0 Beta2 is out
   * Significantly improved search performance (~5-10 times) and reduced memory footprint (issue 123)
   * Fixed misc. issues in the first Beta (thanks everyone who tested it)
   * Misc. improvements based on feedback (issue 121, issue 123, issue 124)

### dnGREP 2.6.0 Beta is out
   * Improved start-up time (issue 105)
   * Preview window (issue 116)
   * Increased results space by collapsing file filters
   * Disable file filters when single file is selected (issue 115)
   * Added restore of window size (issue 104)
   * Enabled advance >> button when search for is not used (issue 119)
   * Added ability to generate GUIDs (using $(guid) in replace)
   * Minor enhancement to pattern test window (issue 77)
   * Improved shell integration (issue 96, issue 97)
   * Misc bug fixes (issue 100, issue 106, issue 99, issue 111, issue 93, issue 94, issue 95, issue 114, issue 117)

### Release 2.5.0 is available for download
   * Added info about search pattern and folder path to window title (issue 83)
   * Added description of replacement patterns to help and hint (issue 88)
   * Enabled searching in folders with ";" and "," in the name (issue 68)
   * Made the line number column expandable (issue 85)
   * Implemented better regex validation in main window and test (issue 78)
   * Changed prerequisites to .NET 4.0 Client Profile to to reduce size
   * Fixed issues related to Unix style line breaks
   * Misc. defect fixes (issue 75, issue 80, issue 81, issue 82, issue 87)

### Release 2.4.0 is available for download
   * Upgraded to .NET 4.0 to improve "blurry" fonts on Windows XP - to disable ClearType go to Options > User Interface and uncheck the appropriate checkbox (issue 39)
   * Fixed XPath search
   * Added drop down list for previous search paths (issue 71)
   * Added functionality to clear old searches from drop downs (issue 70)
   * Added option to have results tree be always expanded (issue 70)

### Release 2.3.1 is available for download
   * Bug fix related to file search (certain files were skipped from search)
   * Added splash screen to address WPF related slow start (issue 64)

### Release 2.3.0 is available for download
   * Added history to filter text fields (via drop down field) (issue 62)
   * Improved Cancel button performance when searching large directories (issue 57)
   * Added context menu to copy path to clipboard (in addition to Ctrl + C hotkey) (issue 61)
   * Implemented line separator when "show in context" is used (issue 58)
   * Misc. bug fixes (issue 55, issue 56)
   * Misc. Windows 7 bug fixes (issue 56, issue 50)
   * Implemented new installer 

### Release 2.2.0 is available for download
   * Added option to exclude binary files from search - used algorithm from winGrep (issue 41) 
   * Extended tree view to support copy to clipboard via Ctrl + C (issue 42) and drag-and-drop to external editor (issue 54)
   * Added quick access to past search and replace patterns similar to other search tools (issue 37)
   * Converted search window from tool-window to standard window to add minimize and maximize buttons (issue 22, issue 47)
   * Extended "files that match" pattern to support commas along with semi-colons to provide multiple patterns (issue 49)
   * Misc. defect fixes (issue 44, issue 45, issue 51, issue 52, issue 53)

### Release 2.1.0 is available for download
   * Implemented "whole word" search functionality (issue 35)
   * Added ability to specify file patterns to "exclude" (e.g. "`*`.jpg;`*`.gif;`*`.png") (issue 32)
   * Added support for unix-style wildcards in asterisk search (e.g. "t`[`a-z`]`st-fil?.`*`") (issue 33)
   * Fixed UI issues with Options window (issue 39, issue 38)
   * Added "Search Params" options tab which currently allows to customize phonetic search "fuzzyness"
   * Implemented feature to copy file names returned in search into clipboard (issue 16)
   * Number of misc. defect fixes (issue 36, issue 40)

### Release 2.0.0 is available for download
   * Version 2.0 is a complete re-write of the front-end using WPF framework and now requires .NET 3.5 SP1
   * Includes a new type of search - phonetic (based on agrep algorithm)
   * Added search result highlighting
   * Added ability to drag and drop files and folders from Explorer into dnGREP (issue 33)
   * Added ability to delete nodes from the result (issue 34)
   * Fixed inconsistency between test form and main search (issue 31)
   * Number of other misc. improvements and enhancements

### Release 0.13.0 is available for download
   * Added PDF plug-in to search inside PDF documents (uses xpdf engine) (issue 28)
   * Implemented ability to open files inside archive (issue 27)
   * Misc. fixes related to Windows 7
     * File/Folder select dialog (issue 26)
     * Menu text layout

### Release 0.12.0 is available for download
   * Added MS Word plug-in to search inside word documents (requires Office 2003 or 2007 installed) (issue 20)
   * Implemented "preview" functionality to show results as they are found (issue 21)
   * Implemented ability to search inside one file as well as to select multiple files to search in. To enable this with Shell integration go to Options, un-check Shell Integration option and then check it back on (issue 19)
   * Added ability to type folder or file path in text box (issue 23)
   * Increased search and replace text-box maximum length from 32767 to 327670 (issue 24)

### Release 0.11.0 is available for download
   * Added plug-in framework for future enhancements
   * Added archive plug-in to search inside 7z, rar, and zip archives (utilizes 7zip engine for extracting files) (issue 18)
   * Fixed exception when search returns no files (issue 17)
   * Enabled replace with empty strings (issue 15)

### Release 0.10.3 is available for download
   * Fixed shell integration on drive level (dnGREP shortcut when right clicking on drive letter in Windows Explorer). To enable this go to Options, un-check Shell Integration option and then check it back on (issue 14)
   * Added help file
   * Added setup solution
   * Fixed defect with XPath search

### Release 0.10.2 is available for download
   * Added ability to search on file name pattern only without searching file content (issue 12). Requires an option checked:
http://dngrep.googlecode.com/svn/images/options-window-new-option-file-search.jpg
   * Misc bug fixes (issue 10, issue 11, issue 13)

### Release 0.10.1 is available for download
   * Implemented "context view" (ability to see N number of lines before the matched result and M lines after) (issue 9)
   * Added "Case Insensitive" search to Regex search
   * Added display of number of matches in the result tree

### Release 0.10.0 is available for download
   * Added XPath search (for XML files)
   * Added advance actions ">>" (Copy, Move, Delete files; generate CSV report)
   * Added search in results (search previously matched files)
   * Added ability to specify file pattern as regex

### Release 0.9.4 is available for download
   * Added option to open containing folder (issue 8)

### Release 0.9.3 is available for download
   * Added ability to see file path in result window (issue 6)
   * Changed default behavior of Enter key when in textbox with multi-line mode (issue 7)

### Release 0.9.2 is available for download
   * Added automatic check for updates (controlled via Options)

### Release 0.9.1 is available for download
   * Fixed issue 1, issue 2
   * Renames project to dnGREP for better searchability (issue 4)
   * Added bookmarks
   * Updated UI (returned back the old textbox layout) 

### Release 0.9.0 is available for download
   * Added multiline search capability
   * Added warning when replacing read-only files
   * Added running time information on the status strip
   * When Open Folder is clicked and no folders are currently selected, the clipboard is read for folder path
   * Fixed read-only search bug

### Release 0.8.4 is available for download
   * Added window to test regular expressions
   * Added regex cheat-sheet (thanks to http://www.addedbytes.com/cheat-sheets/regular-expressions-cheat-sheet/)

### Release 0.8.3 is available for download
   * Fixed issues when replacing text in ANSI files by implementing GetFileEncoding() (thanks to [http://csharpfeeds.com/post/6949/Detecting_Text_Encoding_for_StreamReader.aspx])

### Release 0.8.2 is available for download
   * Implemented case-insensitive plain text search (thanks to http://www.codeproject.com/KB/string/fastestcscaseinsstringrep.aspx) 
   * Added ability to filter by multiple text pattern (separated by semicolon) 
   * Fixed defect when replacing read-only files 
   * Fixed defect with maintaining file attributes after replacement
