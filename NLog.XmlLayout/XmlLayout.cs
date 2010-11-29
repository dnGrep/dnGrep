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
			using (StringWriter stringWriter = new StringWriter(builder))
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
}
