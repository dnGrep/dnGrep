using System;

namespace dnGREP.Engines
{
    [Serializable]
    internal class GenericPluginException : Exception
    {
        public GenericPluginException()
        {
        }

        public GenericPluginException(string? message)
            : base(message)
        {
        }

        public GenericPluginException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}