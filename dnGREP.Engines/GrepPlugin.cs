using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using dnGREP.Common;
using System.Reflection;

namespace dnGREP.Engines
{
	public class GrepPlugin
	{
		private IGrepEngine engine;

		public IGrepEngine Engine
		{
			get { return engine; }
			set { engine = value; }
		}
		private List<string> extensions = new List<string>();

		public List<string> Extensions
		{
			get { return extensions; }
		}
		private string dllFilePath;

		/// <summary>
		/// Relative path to DLL file
		/// </summary>
		public string DllFilePath
		{
			get { return dllFilePath; }
			set { dllFilePath = value; }
		}
		private string pluginFilePath;

		/// <summary>
		/// Absolute path to plugin file
		/// </summary>
		public string PluginFilePath
		{
			get { return pluginFilePath; }
			set { pluginFilePath = value; }
		}

		private bool enabled = true;

		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		public GrepPlugin(string pluginFilePath)
		{
			PluginFilePath = pluginFilePath;
		}

		public bool LoadPluginSettings()
		{
			if (pluginFilePath != null && File.Exists(pluginFilePath))
			{
				try
				{
					foreach (string line in File.ReadAllLines(pluginFilePath))
					{
						string[] tokens = line.Split('=');
						if (tokens.Length != 2)
							continue;

						switch (tokens[0].Trim().ToUpper())
						{
							case "FILE":
								DllFilePath = tokens[1].Trim();
								break;
							case "ENABLED":
								Enabled = Utils.ParseBoolean(tokens[1].Trim(), true);
								break;
							case "EXTENSIONS":
								Extensions.Clear();
								foreach (string extension in tokens[1].Trim().Split(','))
								{
									Extensions.Add(extension.Trim().ToLower());
								}
								break;
						}
					}

					string tempDllFilePath = DllFilePath;
					if (!File.Exists(tempDllFilePath))
						tempDllFilePath = Path.GetDirectoryName(pluginFilePath) + "\\" + tempDllFilePath;

					if (File.Exists(tempDllFilePath))
					{
						List<string> domainSearchPaths = new List<string>();
						if (System.AppDomain.CurrentDomain.RelativeSearchPath != null)
							domainSearchPaths = new List<string>(System.AppDomain.CurrentDomain.RelativeSearchPath.Split(';', ','));

						if (!domainSearchPaths.Contains(Path.GetDirectoryName(tempDllFilePath)))
							AppDomain.CurrentDomain.AppendPrivatePath(Path.GetDirectoryName(tempDllFilePath));

						Assembly assembly = Assembly.LoadFile(tempDllFilePath);
						Type[] types = assembly.GetTypes();
						foreach (Type type in types)
						{
							if (type.GetInterface("IGrepEngine") != null)
							{
								IGrepEngine engine = (IGrepEngine)Activator.CreateInstance(type);
								Engine = engine;
								break;
							}
						}
					}
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}
			if (Engine != null)
				return true;
			else
				return false;
		}

		public void PersistPluginSettings()
		{
			if (pluginFilePath != null && File.Exists(pluginFilePath))
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("File=" + DllFilePath);
					sb.AppendLine("Enabled=" + Enabled.ToString());
					sb.Append("Extensions=");
					foreach (string ext in Extensions)
					{
						sb.Append(ext + ",");
					}
					Utils.DeleteFile(pluginFilePath);
					File.WriteAllText(pluginFilePath, sb.ToString());
					
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}
		}
	}
}
