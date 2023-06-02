namespace dnGREP.Common
{
    public class MostRecentlyUsed
    {
        public MostRecentlyUsed() { }

        public MostRecentlyUsed(string stringValue, bool isPinned)
        {
            StringValue = stringValue;
            IsPinned = isPinned;
        }

        public string StringValue { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }
}
