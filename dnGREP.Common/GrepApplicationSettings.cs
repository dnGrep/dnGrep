using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;

namespace dnGREP.Common
{
	/// <summary>
	/// Singleton class used to maintain and persist application settings
	/// </summary>
	public class GrepSettings : SerializableDictionary
	{
		public static class Key
		{
			public const string SearchFolder = "SearchFolder";
			public const string SearchFor = "SearchFor";
			public const string ReplaceWith = "ReplaceWith";
            [DefaultValue(true)]
			public const string IncludeHidden = "IncludeHidden";
            [DefaultValue(true)]
			public const string IncludeBinary = "IncludeBinary";
            [DefaultValue(true)]
			public const string IncludeSubfolder = "IncludeSubfolder";
            [DefaultValue(SearchType.Regex)]
			public const string TypeOfSearch = "TypeOfSearch";
            [DefaultValue(FileSearchType.Asterisk)]
			public const string TypeOfFileSearch = "TypeOfFileSearch";
            [DefaultValue("*.*")]
			public const string FilePattern = "FilePattern";
			public const string FilePatternIgnore = "FilePatternIgnore";
            [DefaultValue(FileSizeFilter.No)]
			public const string UseFileSizeFilter = "UseFileSizeFilter";
            public const string CaseSensitive = "CaseSensitive";
            public const string PreviewFileConent = "PreviewFileConent";
			public const string Multiline = "Multiline";
			public const string Singleline = "Singleline";
			public const string WholeWord = "WholeWord";
			public const string SizeFrom = "SizeFrom";
            [DefaultValue(100)]
			public const string SizeTo = "SizeTo";
            [DefaultValue(0.5)]
			public const string FuzzyMatchThreshold = "FuzzyMatchThreshold";
			[DefaultValue(true)]
			public const string EnableUpdateChecking = "EnableUpdateChecking";
            [DefaultValue(true)]
			public const string ShowFilePathInResults = "ShowFilePathInResults";
            [DefaultValue(true)]
			public const string AllowSearchingForFileNamePattern = "AllowSearchingForFileNamePattern";
            [DefaultValue(false)]
			public const string UseCustomEditor = "UseCustomEditor";
			public const string CustomEditor = "CustomEditor";
			public const string CustomEditorArgs = "CustomEditorArgs";
            [DefaultValue(10)]
			public const string UpdateCheckInterval = "UpdateCheckInterval";
            public const string ExpandResults = "ExpandResults";
			public const string LastCheckedVersion = "LastCheckedVersion";
            [DefaultValue(true)]
			public const string IsOptionsExpanded = "IsOptionsExpanded";
            [DefaultValue(true)]
            public const string IsFiltersExpanded = "IsFiltersExpanded";
            public const string FileFilters = "FileFilters";
			public const string FastSearchBookmarks = "FastSearchBookmarks";
			public const string FastReplaceBookmarks = "FastReplaceBookmarks";
			public const string FastFileMatchBookmarks = "FastFileMatchBookmarks";
			public const string FastFileNotMatchBookmarks = "FastFileNotMatchBookmarks";
			public const string FastPathBookmarks = "FastPathBookmarks";
            [DefaultValue(TextFormattingMode.Display)]
            public const string TextFormatting = "TextFormatting";
            [DefaultValue(12)]
            public const string PreviewWindowFont = "PreviewWindowFont";
            [DefaultValue(false)]
            public const string PreviewWindowWrap = "PreviewWindowWrap";
            public const string PreviewWindowSize = "PreviewWindowSize";
            public const string PreviewWindowPosition = "PreviewWindowPosition";
        }		
		
		private static GrepSettings instance;
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private const string storageFileName = "dnGREP.Settings.dat";

		private GrepSettings() { }

		public static GrepSettings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new GrepSettings();
					instance.Load();
				}
				return instance;
			}
		}

		/// <summary>
		/// Loads settings from default location - baseFolder\\dnGREP.Settings.dat
		/// </summary>
		public void Load()
		{
			Load(Utils.GetDataFolderPath() + "\\" + storageFileName);
		}

		/// <summary>
		/// Load settings from location specified
		/// </summary>
		/// <param name="path">Path to settings file</param>
		public void Load(string path)
		{
			try
			{
				if (!File.Exists(path))
					return;

                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					if (stream == null)
						return;
					XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
					this.Clear();
					SerializableDictionary appData = (SerializableDictionary)serializer.Deserialize(stream);
					foreach (KeyValuePair<string, string> pair in appData)
						this[pair.Key] = pair.Value;
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to load settings", ex);
			}
		}

		/// <summary>
		/// Saves settings to default location - baseFolder\\dnGREP.Settings.dat
		/// </summary>
		public void Save()
		{
			Save(Utils.GetDataFolderPath() + "\\" + storageFileName);
		}

		/// <summary>
		/// Saves settings to location specified
		/// </summary>
		/// <param name="path">Path to settings file</param>
		public void Save(string path)
		{
			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(path)))
					Directory.CreateDirectory(Path.GetDirectoryName(path));

				lock (this)
				{
					// Create temp file in case save crashes
                    using (FileStream stream = File.OpenWrite(path + "~"))
                    using (XmlWriter xmlStream = XmlWriter.Create(stream, new XmlWriterSettings { Indent = false }))
					{
                        if (xmlStream == null)
							return;
						XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
                        serializer.Serialize(xmlStream, this);
					}
					File.Copy(path + "~", path, true);
					Utils.DeleteFile(path + "~");
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to load settings", ex);
			}
		}

		public new string this[string key]
		{
			get { return ContainsKey(key) ? base[key] : null; }
			set { base[key] = value; }
		}

		/// <summary>
		/// Gets value of object in dictionary and deserializes it to specified type
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize from</typeparam>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public T Get<T>(string key)
		{
			string value = this[key];

			if (value == null)
                return getDefaultValue<T>(key);

			try
			{
				if (typeof(T) == typeof(string))
				{
					return (T)Convert.ChangeType(value, typeof(string));
				} 
				else if (typeof(T).IsEnum)
				{
					return (T)Enum.Parse(typeof(T), value);
				}
				else if (!typeof(T).IsPrimitive)
				{
					using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(value)))
					{
						IFormatter formatter = new BinaryFormatter();
						return (T)formatter.Deserialize(stream);
					}
				}
				else
				{
					return (T)Convert.ChangeType(value, typeof(T));
				}
			}
			catch (Exception ex)
			{
                return getDefaultValue<T>(key);
			}
		}

		/// <summary>
		/// Sets value of object in dictionary and serializes it to specified type
		/// </summary>
		/// <typeparam name="T">Type of object to serialize into</typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Set<T>(string key, T value)
		{
			if (value == null)
				return;

			if (typeof(T) == typeof(string))
			{
				this[key] = value.ToString();
			}
			else if (typeof(T).IsEnum)
			{
				this[key] = value.ToString();
			}
			else if (!typeof(T).IsPrimitive)
			{
				using (MemoryStream stream = new MemoryStream())
				{
					IFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, value);
					stream.Position = 0;
					this[key] = Convert.ToBase64String(stream.ToArray());
				}
			}
			else
			{
				this[key] = value.ToString();
			}
		}

        private List<FieldInfo> constantKeys;
        private T getDefaultValue<T>(string key) 
        {
            if (constantKeys == null)
            {
                constantKeys = new List<FieldInfo>();
                FieldInfo[] thisObjectProperties = typeof(Key).GetFields();
                foreach (FieldInfo fi in thisObjectProperties)
                {
                    if (fi.IsLiteral)
                    {
                        constantKeys.Add(fi);
                    }
                }
            }
            FieldInfo info = constantKeys.Find(fi => fi.Name == key);
            if (info == null)
                return default(T);

            DefaultValueAttribute[] attr = info.GetCustomAttributes(typeof(DefaultValueAttribute), false) as DefaultValueAttribute[];
            if (attr != null && attr.Length == 1)
                return (T)attr[0].Value;

            return default(T);
        }
	}

    

	/// <summary>
	/// Serializable generic dictionary 
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	[XmlRoot("dictionary")]
	public class SerializableDictionary: Dictionary<string, string>, IXmlSerializable
	{
		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}
		
		public void ReadXml(System.Xml.XmlReader reader)
		{
			bool wasEmpty = reader.IsEmptyElement;

			if (wasEmpty)
				return;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element)
			{
				string key = reader.GetAttribute("key");
                string value = reader.ReadElementContentAsString();
                this[key] = value;
			}
			reader.ReadEndElement();
		}



		public void WriteXml(System.Xml.XmlWriter writer)
		{
			foreach (var key in this.Keys)
			{
				writer.WriteStartElement("item");
                writer.WriteAttributeString("key", key);
				string value = this[key];
                writer.WriteString(value);
				writer.WriteEndElement();
			}
		}
		#endregion
	}
}
