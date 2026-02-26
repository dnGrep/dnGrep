using System;

namespace dnGREP.Everything
{
    /// <summary>
    /// Exception thrown when an Everything SDK operation fails.
    /// </summary>
    public class EverythingException : Exception
    {
        public uint ErrorCode { get; }

        public EverythingException(uint errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public EverythingException(uint errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}