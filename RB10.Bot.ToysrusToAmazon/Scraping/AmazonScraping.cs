using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;
using static RB10.Bot.ToysrusToAmazon.Scraping.ToysrusScraping;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class AmazonScraping
    {
        public class ToyInformation
        {
            public string ToyName { get; set; }
            public int ToysrusPrice { get; set; }
            public string Asin { get; set; }
            public int AmazonPrice { get; set; }
        }

        public int Delay { get; set; }

        public delegate void ExecutingStateEventHandler(object sender, ExecutingStateEventArgs e);
        public event ExecutingStateEventHandler ExecutingStateChanged;

        public List<ToyInformation> Run(List<ToysrusScraping.ToyInformation> toysrusToyInformations)
        {
            //string html = System.IO.File.ReadAllText(@"C:\Users\Higashi\Desktop\debug.txt");

            //var parser = new HtmlParser();
            //var doc = parser.Parse(html);

            //var result = doc.GetElementById("result_0");
            //var asin = result.GetAttribute("data-asin");

            //var price = result.GetElementsByClassName("a-offscreen");

            List<ToyInformation> ret = new List<ToyInformation>();
            foreach (var toysrusToyInformation in toysrusToyInformations)
            {
                var keyWord = string.Join("+", toysrusToyInformation.ToyName.Replace("　", " ").Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                string html = Utils.GetHtml($"https://www.amazon.co.jp/s/field-keywords={keyWord}", Delay);
                var parser = new HtmlParser();
                var doc = parser.Parse(html);

                var result = doc.GetElementById("result_0");
                if (result == null) continue;

                var asin = result.GetAttribute("data-asin");
                var priceTag = result.GetElementsByClassName("a-price-whole").First() as AngleSharp.Dom.Html.IHtmlSpanElement;
                var price = Convert.ToInt32(priceTag.InnerHtml.Replace(@",", ""));

                if (toysrusToyInformation.Price < price)
                {
                    ret.Add(new ToyInformation { ToyName = toysrusToyInformation.ToyName, ToysrusPrice = toysrusToyInformation.Price, Asin = asin, AmazonPrice = price });
                }
            }

            return ret;
        }

        protected void Notify(string info, string message, NotifyStatus reportState, ProcessStatus processState = ProcessStatus.Start)
        {
            if (ExecutingStateChanged != null)
            {
                var eventArgs = new ExecutingStateEventArgs()
                {
                    Info = info,
                    Message = message,
                    NotifyStatus = reportState,
                    ProcessStatus = processState
                };
                ExecutingStateChanged.Invoke(this, eventArgs);
            }
        }

        private const string MY_AWS_ACCESS_KEY_ID = "AKIAIW5VMOY47U46SOHA";
        private const string MY_AWS_SECRET_KEY = "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";
        private const string DESTINATION = "ecs.amazonaws.jp";
        private const string ASSOCIATE_TAG = "baggio10cod02-22";

        private void GetAmazon(string keyword)
        {
            var helper = new Helper.SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION, ASSOCIATE_TAG);

            IDictionary<string, string> request = new Dictionary<string, String>
            {
                ["Service"] = "AWSECommerceService",
                ["Operation"] = "ItemSearch",
                ["SearchIndex"] = "All",
                ["ResponseGroup"] = "Medium",
                ["Keywords"] = keyword
            };
            var requestUrl = helper.Sign(request);
            System.Xml.Linq.XDocument xml = System.Xml.Linq.XDocument.Load(requestUrl);

            System.Xml.Linq.XNamespace ex = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";
            var query = xml.Descendants(ex + "Item");

            var elem = query.First();
            var asin = elem.Element(ex + "ASIN");
            var offerSummary = elem.Element(ex + "OfferSummary");
            var price = offerSummary.Element(ex + "LowestNewPrice").Element(ex + "Amount");
        }
    }
}
