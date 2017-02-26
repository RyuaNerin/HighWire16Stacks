using System;
using System.IO;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace FFXIVBuff
{
    internal static class GithubLastestRealease
    {
        [JsonObject(MemberSerialization.OptIn)]
        private class LastestRealease
        {
            [JsonProperty("tag_name")]
            public string tag_name { get; set; }

            [JsonProperty("html_url")]
            public string html_url { get; set; }
        }

        public static string CheckNewVersion(string owner, string repository, Predicate<string> isNewVersion)
        {
            try
            {
                LastestRealease last;

                var req = HttpWebRequest.Create(string.Format("https://api.github.com/repos/{0}/{1}/releases", owner, repository)) as HttpWebRequest;
                req.UserAgent = Assembly.GetExecutingAssembly().FullName;
                req.Timeout = 5000;
                using (var res = req.GetResponse())
                using (var stream = res.GetResponseStream())
                {
                    var sReader = new StreamReader(stream);

                    last = JsonConvert.DeserializeObject<LastestRealease>(sReader.ReadToEnd());
                }

                return isNewVersion(last.tag_name) ? last.html_url : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
