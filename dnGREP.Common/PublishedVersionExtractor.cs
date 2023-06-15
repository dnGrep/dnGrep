using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dnGREP.Common
{
    public class PublishedVersionExtractor
    {
        public static async Task<string> QueryLatestVersion()
        {
            string page = "https://api.github.com/repos/dnGrep/dnGrep/releases";

            using var client = new HttpClient();
            // TLS 1.2 is required for GitHub connection after 2/1/2018
            // https://githubengineering.com/crypto-deprecation-notice/
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "http://developer.github.com/v3/#user-agent-required");
            using HttpResponseMessage response = await client.GetAsync(page);
            using HttpContent content = response.Content;
            var text = await content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject(text) is JArray json)
            {
                return ExtractVersion(json);
            }
            return string.Empty;
        }

        private static string ExtractVersion(JArray json)
        {
            if (json == null)
                return string.Empty;

            //"tag_name": "v2.9.24.0"

            string? latest = string.Empty;
            try
            {
                latest = json.Children()
                    .Where(r => !r?.Value<bool>("prerelease") ?? false)
                    .Select(r => r?.Value<string>("tag_name"))
                    .FirstOrDefault();
            }
            catch (Exception) { }

            if (!string.IsNullOrEmpty(latest))
                latest = latest.TrimStart('v');

            return latest ?? string.Empty;
        }

        public static bool IsUpdateNeeded(string currentVersion, string publishedVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(publishedVersion))
            {
                return false;
            }
            else
            {
                try
                {
                    Version cVersion = new(currentVersion);
                    Version pVersion = new(publishedVersion);
                    if (cVersion.CompareTo(pVersion) < 0)
                        return true;
                    else
                        return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
