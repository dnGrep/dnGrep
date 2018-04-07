using dnGREP.Everything;

namespace dnGREP.Common
{
    /// <summary>
    /// This class maps the text entered into the 'Search in Folder' box on the main window
    /// </summary>
    public class PathSearchText
    {
        /// <summary>
        /// Gets or sets the raw text entered in the Folder text box
        /// </summary>
        public string FileOrFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current file search type
        /// </summary>
        public FileSearchType TypeOfFileSearch { get; set; } = FileSearchType.Asterisk;

        public override string ToString()
        {
            return $"{FileOrFolderPath} - {TypeOfFileSearch}";
        }

        /// <summary>
        /// Gets the FileOrFolderPath with whitespace removed from the individual paths
        /// </summary>
        public string CleanPath
        {
            get
            {
                string path = string.Empty;
                if (TypeOfFileSearch == FileSearchType.Everything)
                    path = FileOrFolderPath.Trim();
                else
                    path = Utils.CleanPath(FileOrFolderPath);
                return path;
            }
        }

        /// <summary>
        /// Gets the base folder of one or many files or folders. 
        /// If the FileOrFolderPath contains multiple paths, it returns the first one.
        /// </summary>
        public string BaseFolder
        {
            get
            {
                string path = string.Empty;
                if (TypeOfFileSearch == FileSearchType.Everything)
                    path = EverythingSearch.GetBaseFolder(CleanPath);
                else
                    path = Utils.GetBaseFolder(CleanPath);
                return path;
            }
        }

        /// <summary>
        /// Gets the search text part of the string, following the base folder
        /// </summary>
        public string FilePattern
        {
            get
            {
                if (TypeOfFileSearch == FileSearchType.Everything)
                {
                    return EverythingSearch.GetFilePattern(CleanPath);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a flag indicating the BaseFolder is valid
        /// </summary>
        public bool IsValidBaseFolder
        {
            get
            {
                return Utils.IsPathValid(BaseFolder);
            }
        }

        /// <summary>
        /// Gets a flag indicating the path or set of paths are valid
        /// </summary>
        public bool IsValidPath
        {
            get
            {
                bool valid = false;
                if (TypeOfFileSearch == FileSearchType.Everything)
                    valid = !string.IsNullOrWhiteSpace(CleanPath);
                else
                    valid = Utils.IsPathValid(CleanPath);
                return valid;
            }
        }

    }
}
