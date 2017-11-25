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
        
        public static string GetHtml(string url, int delay, int retryCount = 0)
        {
            HttpWebRequest req = null;
            int counter = 0;

            while (true)
            {
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
                    using (var sr = new StreamReader(resSt))
                    {
                        html = sr.ReadToEnd();
                    }

                    return html;
                }
                catch (Exception)
                {
                    counter++;
                    if (retryCount < counter) throw;
                }
                finally
                {
                    if (req != null) req.Abort();
                    Task.Delay(delay).Wait();
                }
            }
        }
    }
}
