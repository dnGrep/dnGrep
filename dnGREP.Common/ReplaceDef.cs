using System;
using System.Collections.Generic;
using System.IO;

namespace dnGREP.Common
{
    public class ReplaceDef
    {
        public ReplaceDef(string originalFile, IEnumerable<GrepMatch> replaceItems)
        {
            OrginalFile = originalFile;
            BackupName = Guid.NewGuid().ToString() + Path.GetExtension(originalFile);
            ReplaceItems = replaceItems;
        }

        public string OrginalFile { get; private set; }
        public string BackupName { get; private set; }
        public IEnumerable<GrepMatch> ReplaceItems { get; private set; }
    }
}
