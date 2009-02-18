using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;

namespace dnGREP
{
	public class CurrentPublishedVersionExtractor
	{
		HttpWebRequest webRequest;
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
			webRequest = (HttpWebRequest)WebRequest.Create("http://code.google.com/feeds/p/dngrep/downloads/basic");
			webRequest.Method = "GET";
			webRequest.BeginGetResponse(new AsyncCallback(finishWebRequest), null);
		}

		private void finishWebRequest(IAsyncResult result)
		{
			XmlDocument response = new XmlDocument();
			using (HttpWebResponse resp = (HttpWebResponse)webRequest.EndGetResponse(result))
			{
				if (resp.StatusCode == HttpStatusCode.OK)
				{
					XmlTextReader reader = new XmlTextReader(resp.GetResponseStream());
					response.Load(reader);
					reader.Close();
				}
			}
			if (RetrievedVersion != null)
				RetrievedVersion(this, new PackageVersion(extractVersion(response)));
		}

		private string extractVersion(XmlDocument doc)
		{
			if (doc != null)
			{				
				XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
				XmlNodeList nodes = doc.GetElementsByTagName("title");
				if (nodes.Count != 2)
					return null;
				XmlNode node = nodes[1];
				Regex versionRe = new Regex(@"dnGREP\s(?<version>[\d\.]+)\.zip");
				if (!versionRe.IsMatch(node.InnerText))
					return null;
				return versionRe.Match(node.InnerText).Groups["version"].Value;
			}
			else
			{
				return null;
			}
		}
	}
}
