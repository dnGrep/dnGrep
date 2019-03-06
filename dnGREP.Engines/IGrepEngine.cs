using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnGREP.Common;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public interface IGrepEngine
    {
        bool Initialize(GrepEngineInitParams param, FileFilter filter);

        /// <summary>
        /// Return true if engine supports search only. Return false is engine supports replace as well.
        /// </summary>
        bool IsSearchOnly { get; }

        FileFilter FileFilter { get; }

        /// <summary>
        /// Searches folder for files whose content matches regex
        /// </summary>
        /// <param name="file">File to search in</param>
        /// <param name="searchPattern"></param>
        /// <param name="searchType"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="isMultiline"></param>
        /// <param name="encoding"></param>
        /// <returns>List of results. If nothing is found returns empty list</returns>
        List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding);

        /// <summary>
        /// Searches an input stream for files whose content matches regex
        /// </summary>
        /// <param name="input">the input stream</param>
        /// <param name="fileName">the file name</param>
        /// <param name="searchPattern"></param>
        /// <param name="searchType"></param>
        /// <param name="searchOptions"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding);

        bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, 
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems);

        /// <summary>
        /// Method gets called when search/replace process is complete
        /// </summary>
        void Unload();

        /// <summary>
        /// Return version of the framework (dgGREP.Engines.dll) that the plugin was compiled against
        /// </summary>
        Version FrameworkVersion { get; }

        /// <summary>
        /// Can be used to provide custom file opening functionality
        /// </summary>
        /// <param name="args"></param>
        void OpenFile(OpenFileArgs args);
    }

    public interface IArchiveEngine
    {
        /// <summary>
        /// Extract a file from an archive to a temp file
        /// </summary>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        string ExtractToTempFile(GrepSearchResult searchResult);
    }
}
