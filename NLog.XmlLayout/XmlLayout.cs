using System;
using System.Collections.Generic;
using System.Text;
using NLog.LayoutRenderers;
using System.Xml;
using System.Collections;
using System.IO;
using System.Globalization;


namespace NLog.XmlLayout
{
	[LayoutRenderer("xml")] 
	public class XmlLayoutRenderer : LayoutRenderer 
	{
		private string elementName;

		public string ElementName
		{
			get { return elementName; }
			set { elementName = value; }
		}

		protected override void Append(StringBuilder builder, LogEventInfo logEvent)
		{
            using (StringWriter stringWriter = new UTF8StringWriter(builder))
			using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
			{
				writer.Formatting = Formatting.Indented;
				// Create the eventNode element
				writer.WriteStartElement((elementName == null ? logEvent.Level.ToString() : elementName));
				
				if (elementName != null)
				{
					WriteToAttribute(writer, "type", logEvent.Level.ToString());
				}

				WriteToAttribute(writer, "time", logEvent.TimeStamp.ToString("M/d/yyyy HH:mm:ss tt"));
				writer.WriteStartElement("Message");
				writer.WriteString(logEvent.Message);
				writer.WriteEndElement();
				if (logEvent.Parameters != null)
				{
					foreach (object obj in logEvent.Parameters)
					{
						writer.WriteStartElement("Parameter");
						if (obj is KeyValuePair<string, string>)
						{
							KeyValuePair<string, string> p = (KeyValuePair<string, string>)obj;
							WriteToAttribute(writer, "name", p.Key);
							WriteToAttribute(writer, "value", p.Value);
						}
                        else if (obj is Exception)
                        {
                            Exception ex = obj as Exception;
                            string msg = ex.GetType().ToString() + ex.StackTrace;
                            WriteToAttribute(writer, "value", msg);                            
                        }
						else
						{
							WriteToAttribute(writer, "value", obj.ToString());
						}
						writer.WriteEndElement();
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
            :base()
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
