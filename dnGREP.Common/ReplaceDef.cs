using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dnGREP.Common
{
    public class ReplaceDef
    {
        public ReplaceDef(string originalFile, IEnumerable<GrepSearchResult.GrepMatch> replaceItems)
        {
            OrginalFile = originalFile;
            BackupName = Guid.NewGuid().ToString() + Path.GetExtension(originalFile);
            ReplaceItems = replaceItems;
        }

        public string OrginalFile { get; private set; }
        public string BackupName { get; private set; }
        public IEnumerable<GrepSearchResult.GrepMatch> ReplaceItems { get; private set; }
    }

    //public class BaseSearchReplaceCriteria
    //{
    //    internal BaseSearchReplaceCriteria(SearchType searchType, string searchFor, GrepSearchOption options, int codePage)
    //    {
    //        SearchType = searchType;
    //        SearchPattern = searchFor;
    //        SearchOptions = options;
    //        CodePage = codePage;
    //    }

    //    public SearchType SearchType { get; private set; }
    //    public string SearchPattern { get; private set; }
    //    public GrepSearchOption SearchOptions { get; private set; }
    //    public int CodePage { get; private set; }
    //}

    //public class SearchCriteria : BaseSearchReplaceCriteria
    //{
    //    public SearchCriteria(IEnumerable<string> files, SearchType searchType, string searchFor, GrepSearchOption options, int codePage)
    //        : base(searchType, searchFor, options, codePage)
    //    {
    //        SearchFiles = files;
    //    }

    //    public IEnumerable<string> SearchFiles { get; private set; }
    //}

    //public class ReplaceCriteria : BaseSearchReplaceCriteria
    //{
    //    public ReplaceCriteria(IEnumerable<ReplaceDef> replaceFiles, SearchType searchType, string searchFor, string replaceText, GrepSearchOption options, int codePage)
    //        : base(searchType, searchFor, options, codePage)
    //    {
    //        ReplaceFiles = replaceFiles;
    //        ReplaceText = replaceText;
    //    }

    //    public IEnumerable<ReplaceDef> ReplaceFiles { get; private set; }
    //    public string ReplaceText { get; private set; }
    //}

}
