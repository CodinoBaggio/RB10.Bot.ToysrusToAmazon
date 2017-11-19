using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
