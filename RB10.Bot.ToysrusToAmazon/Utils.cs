using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RB10.Bot.ToysrusToAmazon
{
    class Utils
    {
        private const int TIME_OUT = 100000;

        public static string GetHtml(string url, int delay)
        {
            HttpWebRequest req = null;

            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                //req.Method = "GET";
                req.Timeout = TIME_OUT;
                req.UserAgent = Properties.Settings.Default.UserAgent;
                //req.Proxy = null;
                //req.MaximumAutomaticRedirections = 1000;

                // html取得文字列
                string html = "";

                using (var res = (HttpWebResponse)req.GetResponse())
                using (var resSt = res.GetResponseStream())
                using (var sr = new StreamReader(resSt, Encoding.UTF8))
                {
                    html = sr.ReadToEnd();
                }

                return html;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (req != null) req.Abort();
                Task.Delay(delay).Wait();
            }
        }
    }
}
