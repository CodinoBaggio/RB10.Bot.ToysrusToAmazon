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
        }

        public int Delay { get; set; }

        private System.Text.RegularExpressions.Regex _numbersReg = new System.Text.RegularExpressions.Regex("全(?<numbers>[0-9]+)件中");
        private System.Text.RegularExpressions.Regex _priceReg = new System.Text.RegularExpressions.Regex(@"(?<price>.*)円 \(税込\)");
        private System.Text.RegularExpressions.Regex _startExtraReg = new System.Text.RegularExpressions.Regex("^【.*?】");
        private System.Text.RegularExpressions.Regex _exist = new System.Text.RegularExpressions.Regex("<div class=\"status\">在庫あり</div>");
        private System.Text.RegularExpressions.Regex _lessExist = new System.Text.RegularExpressions.Regex("<div class=\"status\">在庫わずか</div>");

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
                string escapeUriString = Uri.EscapeUriString(searchKeyword);
                return $"{url}?q={escapeUriString}&p={page}type=03&sort=04";
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

            foreach (var item in doc.GetElementsByClassName("sub-category-items"))
            {
                try
                {
                    var nameElement = item.GetElementsByClassName("item").Where(x => x.Id.StartsWith("GO_GOODS_DISP_")).FirstOrDefault();
                    var elem = nameElement as AngleSharp.Dom.Html.IHtmlAnchorElement;
                    if (elem == null) continue;

                    var toy = GetToy(elem.Href);
                    if (toy == null) continue;

                    ret.Add(toy);

                    Notify($"トイザらス：[{nameElement.InnerHtml}]の取得を行いました。", NotifyStatus.Information);
                }
                catch (Exception ex)
                {
                    Notify($"トイザらス：{ex.ToString()}", NotifyStatus.Exception);
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
            string ret = source.Replace("【送料無料】", "")
                .Replace("トイザらス", "")
                .Replace("トイザらス限定", "")
                .Replace("ベビーザらス", "")
                .Replace("ベビーザらス限定", "")
                .Replace("【クリアランス】", "")
                .Replace("【オンライン限定価格】", "").Trim();
            ret = _startExtraReg.Replace(ret, "");

            return ret;
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
                    }
                    else
                    {
                        cate.Urls.Add(href);
                    }
                }
                toysrus.Categories.Add(cate);
            }

            Category allCate = new Category
            {
                CategoryName = "すべて",
            };
            foreach (var item in toysrus.Categories)
            {
                foreach (var item2 in item.Urls)
                {
                    allCate.Urls.Add(item2);
                }
            }
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
                    }
                    else
                    {
                        cate.Urls.Add(href);
                    }
                }
                babysrus.Categories.Add(cate);
            }

            allCate = new Category
            {
                CategoryName = "すべて",
            };
            foreach (var item in babysrus.Categories)
            {
                foreach (var item2 in item.Urls)
                {
                    allCate.Urls.Add(item2);
                }
            }
            babysrus.Categories.Insert(0, allCate);

            return new BindingList<Store> { toysrus, babysrus };
        }

        #endregion
    }
}
