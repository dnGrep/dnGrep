using System;
using System.Runtime.Serialization;

namespace dnGREP.Engines.Pdf
{
    [Serializable]
    public class PdfToTextException : Exception
    {
        /// <summary>
        /// Creates a new PdfToTextException.
        /// </summary>
        public PdfToTextException() : base()
        {
        }

        /// <summary>
        /// Creates a new PdfToTextException.
        /// </summary>
        public PdfToTextException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new PdfToTextException.
        /// </summary>
        public PdfToTextException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Deserializes a PdfToTextException.
        /// </summary>
        protected PdfToTextException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
