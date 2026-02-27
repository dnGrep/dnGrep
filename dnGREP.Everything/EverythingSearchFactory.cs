namespace dnGREP.Everything
{
    /// <summary>
    /// Provides the active IEverythingSearch implementation.
    /// Prefers Everything 3 SDK if available, falls back to Everything 1.4 SDK.
    /// </summary>
    public static class EverythingSearchFactory
    {
        private static IEverythingSearch? instance;
        private static readonly object lockObj = new();

        /// <summary>
        /// Gets the current IEverythingSearch instance, creating it on first access.
        /// </summary>
        public static IEverythingSearch Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        instance ??= Create();
                    }
                }
                return instance;
            }
        }

        public static bool IsAvailable => Instance.IsAvailable;

        /// <summary>
        /// Creates and returns the best available IEverythingSearch implementation.
        /// Prefers Everything3 if its DLL is present and the service is running.
        /// </summary>
        private static IEverythingSearch Create()
        {
            var search3 = new EverythingSearch3();
            if (search3.IsAvailable)
                return search3;

            // Fall back to the Everything 1.4 SDK
            return new EverythingSearch();
        }

        /// <summary>
        /// Resets the cached instance, forcing re-detection on next access.
        /// Call this if the Everything service may have started or stopped.
        /// </summary>
        public static void Reset()
        {
            lock (lockObj)
            {
                instance = null;
            }
        }
    }
}