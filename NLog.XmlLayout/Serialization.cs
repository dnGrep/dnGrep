using System;
using System.Collections;
using System.Xml;

namespace NLog.XmlLayout
{
    public class Serialization
    {
        /// <summary>
        /// Serializes exception into XML. If exception or stream is null
        /// does nothing.
        /// </summary>
        /// <param name="currentException"></param>
        /// <param name="writer"></param>
        public static void SerializeException(Exception currentException, XmlTextWriter writer)
        {
            if (currentException == null || writer == null)
                return;

            writer.WriteStartElement("Exception");

            writer.WriteStartElement("Message");
            writer.WriteString(currentException.Message);
            writer.WriteEndElement();

            writer.WriteStartElement("Source");
            writer.WriteString(currentException.Source);
            writer.WriteEndElement();


            if (currentException.StackTrace != null)
            {
                writer.WriteStartElement("Stack");
                writer.WriteString(currentException.StackTrace);
                writer.WriteEndElement();
            }
            if (currentException.Data.Count > 0)
            {
                foreach (DictionaryEntry item in currentException.Data)
                {
                    writer.WriteStartElement("Data");
                    if (item.Key != null)
                        XmlLayoutRenderer.WriteToAttribute(writer, "key", item.Key.ToString());
                    if (item.Value != null)
                        XmlLayoutRenderer.WriteToAttribute(writer, "value", item.Value.ToString());
                    writer.WriteEndElement();
                }
            }

            if (currentException.InnerException != null)
            {
                SerializeException(currentException.InnerException, writer);
            }
            writer.WriteEndElement();
        }
    }
}
