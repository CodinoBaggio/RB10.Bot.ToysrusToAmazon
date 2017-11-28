using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class ToysrusScraping : ScrapingBase
    {
        public class ToyInformation
        {
            public string Url { get; set; }
            public string ToyName { get; set; }
            public int Price { get; set; } = -1;
            public string OnlineStock { get; set; } = "-";
            public int StoreStockCount { get; set; } = -1;
            public int StoreLessStockCount { get; set; } = -1;
            public string ImageUrl { get; set; } = "";
            public string Asin { get; set; }
            public int AmazonPrice { get; set; }
            public string AmazonImageUrl { get; set; }
        }

        public int Delay { get; set; }
        public int AmazonDelay { get; set; }

        private System.Text.RegularExpressions.Regex _numbersReg = new System.Text.RegularExpressions.Regex("全(?<numbers>[0-9]+)件中");
        private System.Text.RegularExpressions.Regex _priceReg = new System.Text.RegularExpressions.Regex(@"(?<price>.*)円 \(税込\)");
        private System.Text.RegularExpressions.Regex _startExtraReg = new System.Text.RegularExpressions.Regex("【.*?】");
        private System.Text.RegularExpressions.Regex _exist = new System.Text.RegularExpressions.Regex("<div class=\"status\">在庫あり</div>");
        private System.Text.RegularExpressions.Regex _lessExist = new System.Text.RegularExpressions.Regex("<div class=\"status\">在庫わずか</div>");
        private System.Text.RegularExpressions.Regex _toyDetailUrlReg = new System.Text.RegularExpressions.Regex("(?<url>" + System.Text.RegularExpressions.Regex.Escape("href=\"https://www.toysrus.co.jp/s/dsg-") + "[0-9]+)");

        public List<ToyInformation> Run(List<string> urls, string searchKeyword)
        {
            // 各カテゴリー内の商品取得
            List<ToyInformation> ret = new List<ToyInformation>();
            foreach (var url in urls)
            {
                var toyInformations = GetToyCollection(url, searchKeyword);
                ret.AddRange(toyInformations);
            }

            return ret;
        }

        private List<ToyInformation> GetToyCollection(string url, string searchKeyword)
        {
            List<ToyInformation> ret = new List<ToyInformation>();

            // 最初のページの商品を取得
            string firstHtml = Utils.GetHtml(ToToysrusSearchUrl(url, searchKeyword, 1), Delay);

            if (firstHtml.Contains("検索結果は0件でした"))
            {
                return ret;
            }

            List<ToyInformation>  firstPageToys = GetToyInPage(firstHtml);
            ret.AddRange(firstPageToys);

            // ページ数を取得
            double toyCount = 0;
            var match = _numbersReg.Match(firstHtml);
            if (match.Success) toyCount = Convert.ToDouble(match.Groups["numbers"].Value);
            double pageCount = Math.Ceiling(toyCount / 120D);

            // 2ページ目以降の商品を取得
            for (int i = 2; i <= pageCount; i++)
            {
                string html = Utils.GetHtml(ToToysrusSearchUrl(url, searchKeyword, i), Delay);
                List<ToyInformation> toys = GetToyInPage(html);
                ret.AddRange(toys);
            }

            return ret;
        }

        private string ToToysrusSearchUrl(string url, string searchKeyword, int page)
        {
            if (searchKeyword != "")
            {
                string escapeUriString = Uri.EscapeDataString(searchKeyword);
                return $"{url}?q={escapeUriString}&p={page}&type=03&sort=04";
            }
            else
            {
                return $"{url}?p={page}&type=03&sort=04";
            }
        }

        private List<ToyInformation> GetToyInPage(string html)
        {
            List<ToyInformation> ret = new List<ToyInformation>();

            var parser = new HtmlParser();
            var doc = parser.Parse(html);
            var amazonScraping = new AmazonScraping { Delay = AmazonDelay };

            var items = doc.GetElementsByClassName("sub-category-items");
            if (items.Count() == 0)
            {
                string toyName = "-";

                try
                {
                    var match = _toyDetailUrlReg.Match(html);
                    string url = "";
                    if (match.Success) url = match.Groups["url"].Value.Replace("href=\"", "");
                    var toy = GetToy(url);
                    if (toy == null) return ret;
                    toyName = toy.ToyName;

                    var amazonToy = amazonScraping.GetAmazonUsingScraping(toy.ToyName);

                    if (amazonToy.asin != null && toy.Price < amazonToy.price)
                    {
                        ToyInformation toyInformation = new ToyInformation();
                        toyInformation.Url = toy.Url;
                        toyInformation.ToyName = toy.ToyName;
                        toyInformation.Price = toy.Price;
                        toyInformation.OnlineStock = toy.OnlineStock;
                        toyInformation.StoreStockCount = toy.StoreStockCount;
                        toyInformation.StoreLessStockCount = toy.StoreLessStockCount;
                        toyInformation.ImageUrl = toy.ImageUrl;
                        toyInformation.Asin = amazonToy.asin;
                        toyInformation.AmazonPrice = amazonToy.price;
                        toyInformation.AmazonImageUrl = amazonToy.imageUrl;
                        ret.Add(toyInformation);
                    }

                    Notify($"[{toy.ToyName}]の取得を行いました。", NotifyStatus.Information);
                }
                catch (Exception ex)
                {
                    Notify($"[{toyName}]{ex.ToString()}", NotifyStatus.Exception);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    string toyName = "-";

                    try
                    {
                        var nameElement = item.GetElementsByClassName("item").Where(x => x.Id.StartsWith("GO_GOODS_DISP_")).FirstOrDefault();
                        var elem = nameElement as AngleSharp.Dom.Html.IHtmlAnchorElement;
                        if (elem == null) continue;

                        var toy = GetToy(elem.Href);
                        if (toy == null) continue;
                        toyName = toy.ToyName;

                        var amazonToy = amazonScraping.GetAmazonUsingScraping(toy.ToyName);

                        if (amazonToy.asin != null && toy.Price < amazonToy.price)
                        {
                            ToyInformation toyInformation = new ToyInformation();
                            toyInformation.Url = toy.Url;
                            toyInformation.ToyName = toy.ToyName;
                            toyInformation.Price = toy.Price;
                            toyInformation.OnlineStock = toy.OnlineStock;
                            toyInformation.StoreStockCount = toy.StoreStockCount;
                            toyInformation.StoreLessStockCount = toy.StoreLessStockCount;
                            toyInformation.ImageUrl = toy.ImageUrl;
                            toyInformation.Asin = amazonToy.asin;
                            toyInformation.AmazonPrice = amazonToy.price;
                            toyInformation.AmazonImageUrl = amazonToy.imageUrl;
                            ret.Add(toyInformation);
                        }

                        Notify($"[{nameElement.InnerHtml}]の取得を行いました。", NotifyStatus.Information);
                    }
                    catch (Exception ex)
                    {
                        Notify($"[{toyName}]{ex.ToString()}", NotifyStatus.Exception);
                    }
                }
            }

            return ret;
        }

        private ToyInformation GetToy(string url)
        {
            ToyInformation ret = new ToyInformation();
            ret.Url = url;

            string html = Utils.GetHtml(url, Delay);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            var productName = doc.GetElementById("DISP_GOODS_NM");
            ret.ToyName = ConvertToyName(productName.InnerHtml);

            var price = doc.GetElementsByClassName("inTax");
            if (price.Count() == 0 || (price.First() as AngleSharp.Dom.Html.IHtmlElement).IsHidden)
            {
            }
            else
            {
                ret.Price = Convert.ToInt32(price.First().InnerHtml.Substring(0, price.First().InnerHtml.IndexOf("円")).Replace(",", ""));
            }

            var image = doc.GetElementById("slideshow-01");
            if (image == null || (image as AngleSharp.Dom.Html.IHtmlAnchorElement).IsHidden)
            {
                ret.ImageUrl = "不明";
            }
            else
            {
                ret.ImageUrl = "https://www.toysrus.co.jp" + (image as AngleSharp.Dom.Html.IHtmlAnchorElement).PathName;
            }

            var isLotManegeYes = doc.GetElementById("isLotManegeYes");
            if (isLotManegeYes == null || (isLotManegeYes as AngleSharp.Dom.Html.IHtmlSpanElement).IsHidden)
            {
                var stock = doc.GetElementById("isStock");
                if (stock == null || (stock as AngleSharp.Dom.Html.IHtmlSpanElement).IsHidden)
                {
                    ret.OnlineStock = "不明";
                }
                else
                {
                    var stockStatus = stock.Children[0].Children.Where(x => (x as AngleSharp.Dom.Html.IHtmlSpanElement).IsHidden == false);
                    if (stockStatus.Count() == 0)
                    {
                        ret.OnlineStock = "不明";
                    }
                    else
                    {
                        var f = stockStatus.First().InnerHtml;
                        ret.OnlineStock = f;
                    }
                }
            }
            else
            {
                var isLot_a = doc.GetElementById("isLot_a") as AngleSharp.Dom.Html.IHtmlLabelElement;
                if (!isLot_a.IsHidden) ret.OnlineStock = "予約受付中";
                var isLot_b = doc.GetElementById("isLot_b") as AngleSharp.Dom.Html.IHtmlLabelElement;
                if (!isLot_b.IsHidden) ret.OnlineStock = "予約受付終了間近";
                var isLot_c = doc.GetElementById("isLot_b") as AngleSharp.Dom.Html.IHtmlLabelElement;
                if (!isLot_c.IsHidden) ret.OnlineStock = "予約受付終了";
                var isLot_d = doc.GetElementById("isLot_b") as AngleSharp.Dom.Html.IHtmlLabelElement;
                if (!isLot_d.IsHidden) ret.OnlineStock = "注文不可";
            }

            var sku = doc.GetElementsByName("MAIN_SKU");
            if (sku == null)
            {
                return ret;
            }

            var storeUrl = $"https://www.toysrus.co.jp/disp/CSfGoodsPageRealShop_001.jsp?sku={(sku[0] as AngleSharp.Dom.Html.IHtmlInputElement).Value}&shopCd=";
            html = Utils.GetHtml(storeUrl, Delay);
            doc = parser.Parse(html);
            var source = doc.Source.Text;

            int existCount = _exist.Matches(source).Count;
            int lessExistCount = _lessExist.Matches(source).Count;
            ret.StoreStockCount = existCount;
            ret.StoreLessStockCount = lessExistCount;

            return ret;
        }

        private string ConvertToyName(string source)
        {
            return _startExtraReg.Replace(source, "").Trim();

            //string ret = source
            //    .Replace("【送料無料】", "")
            //    .Replace("トイザらス限定", "")
            //    .Replace("トイザらス", "")
            //    .Replace("ベビーザらス限定", "")
            //    .Replace("ベビーザらス", "")
            //    .Replace("【クリアランス】", "")
            //    .Replace("【オンライン限定価格】", "").Trim();
            //ret = _startExtraReg.Replace(ret, "");

            //return ret;
        }

        #region トイザらスカテゴリー取得

        public class Store
        {
            public string StoreName { get; set; }
            public BindingList<Category> Categories { get; set; }
            public Store() => Categories = new BindingList<Category>();
        }

        public class Category
        {
            public string CategoryName { get; set; }
            public BindingList<string> Urls { get; set; }
            public Category() => Urls = new BindingList<string>();
        }

        public static BindingList<Store> GetCategories()
        {
            // トイザらスのカテゴリーのURL取得
            string html = Utils.GetHtml("https://www.toysrus.co.jp/disp/CSfALLCategoryPage_001.jsp?dispNo=001", 1000);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            Store toysrus = new Store();
            toysrus.StoreName = "トイザらス";

            var allCategoryAreaElements = doc.GetElementsByClassName("allcategory-area");
            foreach (var allCategoryAreaElement in allCategoryAreaElements)
            {
                var categoryLink = allCategoryAreaElement.GetElementsByClassName("with-link-02").FirstOrDefault();
                if (categoryLink?.InnerHtml == "特集") continue;

                Category cate = new Category();
                foreach (var elem in allCategoryAreaElement.GetElementsByClassName("with-link-02"))
                {
                    var link = elem as AngleSharp.Dom.Html.IHtmlAnchorElement;
                    var href = link.Href;
                    if (href.StartsWith("https://www.toysrus.co.jp/cat"))
                    {
                        cate.CategoryName = link.InnerHtml;
                        cate.Urls.Add(href.Replace("cat", "c"));
                    }
                    //else
                    //{
                    //    cate.Urls.Add(href);
                    //}
                }
                toysrus.Categories.Add(cate);
            }

            Category allCate = new Category
            {
                CategoryName = "すべて",
            };
            allCate.Urls.Add("https://www.toysrus.co.jp/c001/");
            toysrus.Categories.Insert(0, allCate);

            // ベビーザらスのカテゴリーのURL取得
            html = Utils.GetHtml("https://www.toysrus.co.jp/disp/CSfALLCategoryPage_001.jsp?dispNo=002", 1000);
            parser = new HtmlParser();
            doc = parser.Parse(html);

            Store babysrus = new Store();
            babysrus.StoreName = "ベビーザらス";

            allCategoryAreaElements = doc.GetElementsByClassName("allcategory-area");
            foreach (var allCategoryAreaElement in allCategoryAreaElements)
            {
                var categoryLink = allCategoryAreaElement.GetElementsByClassName("with-link-02").FirstOrDefault();
                if (categoryLink?.InnerHtml == "特集") continue;

                Category cate = new Category();
                foreach (var elem in allCategoryAreaElement.GetElementsByClassName("with-link-02"))
                {
                    var link = elem as AngleSharp.Dom.Html.IHtmlAnchorElement;
                    var href = link.Href;
                    if (href.StartsWith("https://www.toysrus.co.jp/cat"))
                    {
                        cate.CategoryName = link.InnerHtml;
                        cate.Urls.Add(href.Replace("cat", "c"));
                    }
                    //else
                    //{
                    //    cate.Urls.Add(href);
                    //}
                }
                babysrus.Categories.Add(cate);
            }

            allCate = new Category
            {
                CategoryName = "すべて",
            };
            allCate.Urls.Add("https://www.toysrus.co.jp/c002/");
            babysrus.Categories.Insert(0, allCate);

            return new BindingList<Store> { toysrus, babysrus };
        }

        #endregion
    }
}
