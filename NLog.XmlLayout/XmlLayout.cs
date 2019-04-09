using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using NLog.LayoutRenderers;

namespace NLog.XmlLayout
{
    [LayoutRenderer("xml")]
    public class XmlLayoutRenderer : LayoutRenderer
    {
        public string ElementName { get; set; }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            using (StringWriter stringWriter = new UTF8StringWriter(builder))
            using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
            {
                writer.Formatting = Formatting.Indented;
                // Create the eventNode element
                writer.WriteStartElement(ElementName ?? logEvent.Level.ToString());

                if (ElementName != null)
                {
                    WriteToAttribute(writer, "type", logEvent.Level.ToString());
                }

                WriteToAttribute(writer, "time", logEvent.TimeStamp.ToString("M/d/yyyy HH:mm:ss.fff"));
                writer.WriteStartElement("Message");
                writer.WriteString(logEvent.Message);
                writer.WriteEndElement();
                if (logEvent.Parameters != null)
                {
                    foreach (object obj in logEvent.Parameters)
                    {
                        if (obj is Exception ex)
                        {
                            string msg = ex.GetType().ToString() + Environment.NewLine + ex.StackTrace;
                            WriteToElement(writer, "Exception", msg);
                        }
                        else
                        {
                            writer.WriteStartElement("Parameter");
                            if (obj is KeyValuePair<string, string> p)
                            {
                                WriteToAttribute(writer, "name", p.Key);
                                WriteToAttribute(writer, "value", p.Value);
                            }
                            else
                            {
                                WriteToAttribute(writer, "value", obj.ToString());
                            }
                            writer.WriteEndElement();
                        }
                    }
                }
                if (logEvent.Exception != null)
                {
                    Serialization.SerializeException(logEvent.Exception, writer);
                }
                writer.WriteEndElement();
                stringWriter.Flush();
            }
        }

        internal static void WriteToElement(XmlTextWriter writer, string localName, string content)
        {
            writer.WriteStartElement(localName);
            writer.WriteString(content);
            writer.WriteEndElement();
        }

        internal static void WriteToAttribute(XmlTextWriter writer, string attribute, string value)
        {
            WriteToAttribute(writer, attribute, value, false);
        }

        internal static void WriteToAttribute(XmlTextWriter writer, string attribute, DateTime value)
        {
            WriteToAttribute(writer, attribute, value.ToString(DateTimeFormatInfo.InvariantInfo.ShortDatePattern + " " + DateTimeFormatInfo.InvariantInfo.ShortTimePattern), false);
        }

        internal static void WriteToAttribute(XmlTextWriter writer, string attribute, string value, bool writeIfEmptyValue)
        {
            if (writeIfEmptyValue || !string.IsNullOrEmpty(value))
                writer.WriteAttributeString(attribute, value);
        }
    }

    public class UTF8StringWriter : StringWriter
    {
        public UTF8StringWriter()
            : base()
        {
        }

        public UTF8StringWriter(StringBuilder sb)
            : base(sb)
        {
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
