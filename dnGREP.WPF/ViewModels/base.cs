virtual public void LoadSettings()
        {
            List<string> fsb = settings.Get<List<string>>(GrepSettings.Key.FastSearchBookmarks);

            string _searchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            FastSearchBookmarks.Clear();
            if (fsb != null)
            {
                foreach (string bookmark in fsb)
                {
                    if (!FastSearchBookmarks.Contains(bookmark))
                        FastSearchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.SearchFor] = _searchFor;

            string _replaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            FastReplaceBookmarks.Clear();
            List<string> frb = settings.Get<List<string>>(GrepSettings.Key.FastReplaceBookmarks);
            if (frb != null)
            {
                foreach (string bookmark in frb)
                {
                    if (!FastReplaceBookmarks.Contains(bookmark))
                        FastReplaceBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.ReplaceWith] = _replaceWith;

            string _filePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
            FastFileMatchBookmarks.Clear();
            List<string> ffmb = settings.Get<List<string>>(GrepSettings.Key.FastFileMatchBookmarks);
            if (ffmb != null)
            {
                foreach (string bookmark in ffmb)
                {
                    if (!FastFileMatchBookmarks.Contains(bookmark))
                        FastFileMatchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.FilePattern] = _filePattern;

            string _filePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            FastFileNotMatchBookmarks.Clear();
            List<string> ffnmb = settings.Get<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks);
            if (ffnmb != null)
            {
                foreach (string bookmark in ffnmb)
                {
                    if (!FastFileNotMatchBookmarks.Contains(bookmark))
                        FastFileNotMatchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.FilePatternIgnore] = _filePatternIgnore;

            string _fileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
            FastPathBookmarks.Clear();
            List<string> pb = settings.Get<List<string>>(GrepSettings.Key.FastPathBookmarks);
            if (pb != null)
            {
                foreach (string bookmark in pb)
                {
                    if (!FastPathBookmarks.Contains(bookmark))
                        FastPathBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.SearchFolder] = _fileOrFolderPath;

            FileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
            SearchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = settings.Get<bool>(GrepSettings.Key.IncludeHidden);
            IncludeBinary = settings.Get<bool>(GrepSettings.Key.IncludeBinary);
            IncludeSubfolder = settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            TypeOfSearch = settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
            FilePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
            FilePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            UseFileSizeFilter = settings.Get<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter);
            CaseSensitive = settings.Get<bool>(GrepSettings.Key.CaseSensitive);
            Multiline = settings.Get<bool>(GrepSettings.Key.Multiline);
            Singleline = settings.Get<bool>(GrepSettings.Key.Singleline);
            StopAfterFirstMatch = settings.Get<bool>(GrepSettings.Key.StopAfterFirstMatch);
            WholeWord = settings.Get<bool>(GrepSettings.Key.WholeWord);
            SizeFrom = settings.Get<int>(GrepSettings.Key.SizeFrom);
            SizeTo = settings.Get<int>(GrepSettings.Key.SizeTo);
            TextFormatting = settings.Get<TextFormattingMode>(GrepSettings.Key.TextFormatting);
            IsOptionsExpanded = settings.Get<bool>(GrepSettings.Key.IsOptionsExpanded);
            IsFiltersExpanded = settings.Get<bool>(GrepSettings.Key.IsFiltersExpanded);
            FileFilters = settings.Get<bool>(GrepSettings.Key.FileFilters);
            PreviewFileContent = settings.Get<bool>(GrepSettings.Key.PreviewFileContent);
        }