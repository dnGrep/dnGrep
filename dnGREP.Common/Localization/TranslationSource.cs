using System;

namespace dnGREP.Common
{
    public class TranslationSource
    {
        public static EventHandler<QueryStringEventArgs> QueryString;

        public static TranslationSource Instance { get; } = new TranslationSource();

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException("key");

                QueryStringEventArgs args = new QueryStringEventArgs(key);
                QueryString?.Invoke(this, args);

                return args.Value ?? $"#{key}";
            }
        }
    }

    public class QueryStringEventArgs : EventArgs
    {
        public QueryStringEventArgs(string key)
        {
            Key = key;
        }
        public string Key { get; private set; }
        public string Value { get; set; }
    }

}
