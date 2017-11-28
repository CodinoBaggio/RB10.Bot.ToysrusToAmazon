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
        }

        public int Delay { get; set; }

        private System.Text.RegularExpressions.Regex _priceRangeReg = new System.Text.RegularExpressions.Regex(@".+\-.+");

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

                        Notify($"Amazon：[{toysrusToyInformation.ToyName}]の取得を行いました。", NotifyStatus.Information);
                    }
                }
                catch (Exception ex)
                {
                    Notify($"Amazon：{ex.ToString()}", NotifyStatus.Exception);
                }
            }

            return ret;
        }

        public (string asin, int price, string imageUrl) GetAmazonUsingScraping(string toyName)
        {
            string html = Utils.GetHtml($"https://www.amazon.co.jp/s/?field-keywords={System.Web.HttpUtility.UrlEncode(toyName)}", Delay, 3);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            var noResult = doc.GetElementById("noResultsTitle");
            if(noResult != null)
            {
                return (null, 0, null);
            }

            var atfResults = doc.GetElementById("atfResults");
            if (atfResults == null)
            {
                return (null, 0, null);
            }
            var countReg = new System.Text.RegularExpressions.Regex("result_[0-9]+");
            int count = countReg.Matches(atfResults.InnerHtml).Count;

            AngleSharp.Dom.IElement result = null;
            for (int i = 0; i < count; i++)
            {
                var resultN = doc.GetElementById($"result_{i}");
                if (resultN == null) continue;
                if (resultN.InnerHtml.Contains("Amazonビデオ"))
                {
                    continue;
                }
                else
                {
                    result = resultN;
                    break;
                }              
            }
            if (result == null) return (null, 0, null);

            var asin = result?.GetAttribute("data-asin");
            int price = 0;
            var priceTag = result?.GetElementsByClassName("a-price-whole").FirstOrDefault() as AngleSharp.Dom.Html.IHtmlSpanElement;
            if(priceTag != null)
            {
                string priceText = "";
                if (_priceRangeReg.IsMatch(priceTag.InnerHtml))
                {
                    priceText = priceTag.InnerHtml.Substring(0, priceTag.InnerHtml.IndexOf("-") - 1);
                }
                else
                {
                    priceText = priceTag.InnerHtml;
                }
                price = Convert.ToInt32(priceText.Replace(@",", "").Replace(@"￥", "").Trim());
            }
            else
            {
                priceTag = result?.GetElementsByClassName("a-size-base a-color-price s-price a-text-bold").FirstOrDefault() as AngleSharp.Dom.Html.IHtmlSpanElement;
                if (priceTag == null) priceTag = result?.GetElementsByClassName("a-size-base a-color-price a-text-bold").FirstOrDefault() as AngleSharp.Dom.Html.IHtmlSpanElement;
                if (priceTag != null)
                {
                    string priceText = "";
                    if (_priceRangeReg.IsMatch(priceTag.InnerHtml))
                    {
                        priceText = priceTag.InnerHtml.Substring(0, priceTag.InnerHtml.IndexOf("-") - 1);
                    }
                    else
                    {
                        priceText = priceTag.InnerHtml;
                    }

                    price = priceTag != null ? Convert.ToInt32(priceText.Replace(@",", "").Replace(@"￥", "").Trim()) : 0;
                }
                else
                {
                    price = 0;
                }
            }

            var image = result?.GetElementsByClassName("s-access-image cfMarker").FirstOrDefault();
            var imageElem = image as AngleSharp.Dom.Html.IHtmlImageElement;

            return (asin, price, imageElem.Source);
        }
    }
}
