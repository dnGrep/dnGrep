using System.Collections.Generic;

namespace dnGREP.Everything
{
    public interface IEverythingSearch
    {
        bool IsAvailable { get; }

        int CountMissingFiles { get; }

        List<EverythingFileInfo> FindFiles(string searchString, bool includeHidden);

        string RemovePrefixes(string text);
    }
}