using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace dnGREP.Common
{
	public class PublishedVersionExtractor
	{
		
		public delegate void VersionExtractorHandler(object sender, PackageVersion files);
		public event VersionExtractorHandler RetrievedVersion;
		public class PackageVersion
		{
			public PackageVersion(string version)
			{
				Version = version;
			}
			public string Version;
		}

		public void StartWebRequest()
		{
            Task.Factory.StartNew<PackageVersion>(() =>
                {
                    Thread.Sleep(1000);
                    HttpWebRequest webRequest;
                    webRequest = (HttpWebRequest)WebRequest.Create("http://code.google.com/feeds/p/dngrep/downloads/basic");
                    webRequest.Method = "GET";
                    var webResponse = webRequest.GetResponse();

                    XmlDocument response = new XmlDocument();
                    try
                    {
                        using (HttpWebResponse resp = webResponse as HttpWebResponse)
                        {
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                XmlTextReader reader = new XmlTextReader(resp.GetResponseStream());
                                response.Load(reader);
                                reader.Close();
                            }
                        }
                        return new PackageVersion(extractVersion(response));
                    }
                    catch (Exception ex)
                    {
                        return new PackageVersion("0.0.0.0");
                    }
                }).ContinueWith(task =>
                {
                    var version = task.Result;
                    if (RetrievedVersion != null)
                        RetrievedVersion(this, version);
                });
		}

		private string extractVersion(XmlDocument doc)
		{
			if (doc != null)
			{				
				XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
				XmlNodeList nodes = doc.GetElementsByTagName("title");
				Version version = new Version();
				foreach (XmlNode node in nodes)
				{
					Regex versionRe = new Regex(@"d?nGREP\s+(?<version>[\d\.]+)\.\w+");

					if (versionRe.IsMatch(node.InnerText))
					{
						Version tempVersion = new Version(versionRe.Match(node.InnerText).Groups["version"].Value);
						if (version == null || version.CompareTo(tempVersion) < 0)
							version = tempVersion;
					}
				}
				return version.ToString();
			}
			else
			{
				return null;
			}
		}

		public static bool IsUpdateNeeded(string currentVersion, string publishedVersion)
		{
			if (string.IsNullOrEmpty(currentVersion) ||
				string.IsNullOrEmpty(publishedVersion))
			{
				return false;
			}
			else
			{
				try
				{
					Version cVersion = new Version(currentVersion);
					Version pVersion = new Version(publishedVersion);
					if (cVersion.CompareTo(pVersion) < 0)
						return true;
					else
						return false;
				}
				catch (Exception ex)
				{
					return false;
				}
			}
		}
	}
}
