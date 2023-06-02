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

            FileInfo fileInfo = new(originalFile);
            fileInfo.Refresh();
            LastWriteTime = fileInfo.LastWriteTime;
        }

        public string OrginalFile { get; private set; }
        public string BackupName { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public IEnumerable<GrepMatch> ReplaceItems { get; private set; }
    }
}
