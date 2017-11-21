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
    class AmazonScraping : ScrapingBase
    {
        public class ToyInformation : ToysrusScraping.ToyInformation
        {
            public string Asin { get; set; }
            public int AmazonPrice { get; set; }
            public string AmazonImageUrl { get; set; }
        }

        public int Delay { get; set; }

        public List<ToyInformation> Run(List<ToysrusScraping.ToyInformation> toysrusToyInformations)
        {
            List<ToyInformation> ret = new List<ToyInformation>();
            foreach (var toysrusToyInformation in toysrusToyInformations)
            {
                try
                {
                    var toy = GetAmazonUsingScraping(toysrusToyInformation.ToyName);

                    if (toy.asin != null && toysrusToyInformation.Price < toy.price)
                    {
                        ToyInformation toyInformation = new ToyInformation();
                        toyInformation.Url = toysrusToyInformation.Url;
                        toyInformation.ToyName = toysrusToyInformation.ToyName;
                        toyInformation.Price = toysrusToyInformation.Price;
                        toyInformation.OnlineStock = toysrusToyInformation.OnlineStock;
                        toyInformation.StoreStockCount = toysrusToyInformation.StoreStockCount;
                        toyInformation.StoreLessStockCount = toysrusToyInformation.StoreLessStockCount;
                        toyInformation.ImageUrl = toysrusToyInformation.ImageUrl;
                        toyInformation.Asin = toy.asin;
                        toyInformation.AmazonPrice = toy.price;
                        toyInformation.AmazonImageUrl = toy.imageUrl;
                        ret.Add(toyInformation);

                        Notify($"Amazon：【{toysrusToyInformation.ToyName}】の取得を行いました。", NotifyStatus.Information);
                    }
                }
                catch (Exception ex)
                {
                    Notify($"Amazon：{ex.ToString()}", NotifyStatus.Exception);
                }
            }

            return ret;
        }

        private (string asin, int price, string imageUrl) GetAmazonUsingScraping(string toyName)
        {
            var keyword = string.Join("+", toyName.Replace("　", " ").Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            string html = Utils.GetHtml($"https://www.amazon.co.jp/s/field-keywords={keyword}", Delay);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            var result = doc.GetElementById("result_0");
            if (result == null) return (null, 0, null);

            var asin = result.GetAttribute("data-asin");
            int price = 0;
            var priceTag = result.GetElementsByClassName("a-price-whole").FirstOrDefault() as AngleSharp.Dom.Html.IHtmlSpanElement;
            if(priceTag != null)
            {
                price = priceTag != null ? Convert.ToInt32(priceTag.InnerHtml.Replace(@",", "")) : 0;
            }
            else
            {
                priceTag = result.GetElementsByClassName("a-size-base a-color-price s-price a-text-bold").FirstOrDefault() as AngleSharp.Dom.Html.IHtmlSpanElement;
                price = priceTag != null ? Convert.ToInt32(priceTag.InnerHtml.Replace(@",", "").Replace(@"￥", "").Trim()) : 0;
            }

            var image = result.GetElementsByClassName("s-access-image cfMarker").FirstOrDefault();
            var imageElem = image as AngleSharp.Dom.Html.IHtmlImageElement;

            return (asin, price, imageElem.Source);
        }

        private const string MY_AWS_ACCESS_KEY_ID = "";
        private const string MY_AWS_SECRET_KEY = "";
        private const string DESTINATION = "ecs.amazonaws.jp";
        private const string ASSOCIATE_TAG = "baggio10cod02-22";

        private (string asin, int price) GetAmazonUsingAPI(string toyName)
        {
            try
            {
                var keyword = toyName.Replace("　", " ");
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

                System.Xml.Linq.XNamespace ns = xml.Root.Name.Namespace;
                var errorMessageNodes = xml.Descendants(ns + "Message").ToList();
                if (errorMessageNodes.Any())
                {
                    var message = errorMessageNodes[0].Value;
                    return (null, 0);
                }
                var item = xml.Descendants(ns + "Item").FirstOrDefault();
                var asin = item?.Descendants(ns + "ASIN").FirstOrDefault()?.Value;
                var offerSummary = item?.Descendants(ns + "OfferSummary").FirstOrDefault();
                var price = offerSummary?.Descendants(ns + "LowestNewPrice").FirstOrDefault()?.Descendants(ns + "Amount").FirstOrDefault()?.Value;

                return (asin, price != null ? Convert.ToInt32(price) : 0);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Task.Delay(Delay).Wait();
            }
        }
    }
}
