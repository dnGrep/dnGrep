using dnGREP.Everything;

namespace dnGREP.Common
{
    /// <summary>
    /// This class maps the text entered into the 'Search in Folder' box on the main window.
    /// Values are cached to reduce the number of times the path is split and evaluated.
    /// </summary>
    public class PathSearchText
    {
        private string fileOrFolderPath;
        /// <summary>
        /// Gets or sets the raw text entered in the Folder text box
        /// </summary>
        public string FileOrFolderPath
        {
            get => fileOrFolderPath;
            set
            {
                if (value == fileOrFolderPath)
                    return;

                baseFolder = null;
                isValidPath = null;
                fileOrFolderPath = value;
            }
        }

        /// <summary>
        /// Gets or sets the current file search type
        /// </summary>
        public FileSearchType TypeOfFileSearch { get; set; } = FileSearchType.Asterisk;

        public override string ToString()
        {
            return $"{FileOrFolderPath} - {TypeOfFileSearch}";
        }

        private string baseFolder;
        /// <summary>
        /// Gets the base folder of one or many files or folders. 
        /// If the FileOrFolderPath contains multiple paths, it returns the first one.
        /// </summary>
        public string BaseFolder
        {
            get
            {
                if (baseFolder == null)
                {
                    if (TypeOfFileSearch == FileSearchType.Everything)
                        baseFolder = EverythingSearch.GetBaseFolder(FileOrFolderPath);
                    else
                        baseFolder = Utils.GetBaseFolder(FileOrFolderPath);
                }
                return baseFolder;
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
                    return EverythingSearch.GetFilePattern(FileOrFolderPath);
                }
                return string.Empty;
            }
        }

        private bool? isValidPath;
        /// <summary>
        /// Gets a flag indicating the path or set of paths are valid
        /// </summary>
        public bool IsValidPath
        {
            get
            {
                if (!isValidPath.HasValue)
                {
                    if (TypeOfFileSearch == FileSearchType.Everything)
                        isValidPath = !string.IsNullOrWhiteSpace(FileOrFolderPath);
                    else
                        isValidPath = !string.IsNullOrWhiteSpace(FileOrFolderPath) && 
                            Utils.IsPathValid(fileOrFolderPath);
                }
                return isValidPath.Value;
            }
        }

    }
}
