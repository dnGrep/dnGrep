namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base SevenZip exception class.
    /// </summary>
    [Serializable]
    public class SevenZipException : Exception
    {
        /// <summary>
        /// The message for thrown user exceptions.
        /// </summary>
        internal const string USER_EXCEPTION_MESSAGE = "The extraction was successful but" +
            "some exceptions were thrown in your events. Check UserExceptions for details.";

        /// <summary>
        /// Initializes a new instance of the SevenZipException class
        /// </summary>
        public SevenZipException() : base("SevenZip unknown exception.") { }

        /// <summary>
        /// Initializes a new instance of the SevenZipException class
        /// </summary>
        /// <param name="defaultMessage">Default exception message</param>
        public SevenZipException(string defaultMessage)
            : base(defaultMessage) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipException class
        /// </summary>
        /// <param name="defaultMessage">Default exception message</param>
        /// <param name="message">Additional detailed message</param>
        public SevenZipException(string defaultMessage, string message)
            : base(defaultMessage + " Message: " + message) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipException class
        /// </summary>
        /// <param name="defaultMessage">Default exception message</param>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occurred</param>
        public SevenZipException(string defaultMessage, string message, Exception inner)
            : base(
                defaultMessage + (defaultMessage.EndsWith(" ", StringComparison.CurrentCulture) ? "" : " Message: ") +
                message, inner)
        { }

        /// <summary>
        /// Initializes a new instance of the SevenZipException class
        /// </summary>
        /// <param name="defaultMessage">Default exception message</param>
        /// <param name="inner">Inner exception occurred</param>
        public SevenZipException(string defaultMessage, Exception inner)
            : base(defaultMessage, inner) { }
    }
}
