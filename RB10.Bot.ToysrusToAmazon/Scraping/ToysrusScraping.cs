using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class ToysrusScraping
    {
        public class ToyInformation
        {
            public string ToyName { get; set; }
            public int Price { get; set; }
        }

        public int Delay { get; set; }

        private System.Text.RegularExpressions.Regex _numbersReg = new System.Text.RegularExpressions.Regex("全(?<numbers>[0-9]+)件中");
        private System.Text.RegularExpressions.Regex _priceReg = new System.Text.RegularExpressions.Regex(@"(?<price>.*)円 \(税込\)");
        private System.Text.RegularExpressions.Regex _startExtraReg = new System.Text.RegularExpressions.Regex("【.+】");

        public delegate void ExecutingStateEventHandler(object sender, ExecutingStateEventArgs e);
        public event ExecutingStateEventHandler ExecutingStateChanged;

        public List<ToyInformation> Run()
        {
            string html = Utils.GetHtml("https://www.toysrus.co.jp/disp/CSfALLCategoryPage_001.jsp?dispNo=001", Delay);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            // カテゴリーのURL取得
            List<string> urls = new List<string>();
            var categoryLinks = doc.GetElementsByClassName("with-link-02");
            foreach (var item in categoryLinks)
            {
                var categoryLink = item as AngleSharp.Dom.Html.IHtmlAnchorElement;

                if (categoryLink != null && !categoryLink.Href.StartsWith("https://www.toysrus.co.jp/f/") && !categoryLink.Href.StartsWith("https://www.toysrus.co.jp/cat"))
                {
                    urls.Add(categoryLink.Href);
                }
            }

            // 各カテゴリー内の商品取得
            List<ToyInformation> ret = new List<ToyInformation>();
            foreach (var url in urls)
            {
                var toyInformations = GetToyCollection(url);
                ret.AddRange(toyInformations);
            }

            return ret;
        }

        private List<ToyInformation> GetToyCollection(string url)
        {
            List<ToyInformation> ret = new List<ToyInformation>();

            string firstHtml = Utils.GetHtml($"{url}?type=03&sort=04", Delay);
            List<ToyInformation>  firstPageToys = GetToyInPage(firstHtml);
            ret.AddRange(firstPageToys);

            // ページ数を取得
            double toyCount = 0;
            var match = _numbersReg.Match(firstHtml);
            if (match.Success) toyCount = Convert.ToDouble(match.Groups["numbers"].Value);
            double pageCount = Math.Ceiling(toyCount / 120D);

            // ページ毎に商品を取得
            for (int i = 2; i <= pageCount; i++)
            {
                string html = Utils.GetHtml($"{url}?p={i}type=03&sort=04", Delay);
                List<ToyInformation> toys = GetToyInPage(html);
                ret.AddRange(toys);
            }

            return ret;
        }

        private List<ToyInformation> GetToyInPage(string html)
        {
            List<ToyInformation> ret = new List<ToyInformation>();

            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            foreach (var item in doc.GetElementsByClassName("sub-category-items"))
            {

                var nameTags = item.GetElementsByClassName("item").Where(x=>x.Id.StartsWith("GO_GOODS_DISP_"));
                var priceTags = item.GetElementsByClassName("inTax");

                var toyName = ConvertToyName(nameTags.First().InnerHtml);
                var price = "0";
                var match = _priceReg.Match(priceTags.First().InnerHtml);
                if (match.Success) price = match.Groups["price"].Value;

                ret.Add(new ToyInformation { ToyName = toyName, Price = Convert.ToInt32(price.Replace(",", "")) });
            }

            return ret;
        }

        private string ConvertToyName(string source)
        {
            string ret = source.Replace("【送料無料】", "").Replace("トイザらス限定", "").Trim();

            if (ret.StartsWith("【"))
            {
                ret = _startExtraReg.Replace(ret, "");
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

        public static List<string> GetCategories(string url, int delay)
        {
            string html = Utils.GetHtml(url, delay);
            var parser = new HtmlParser();
            var doc = parser.Parse(html);

            // カテゴリーのURL取得
            List<string> urls = new List<string>();
            var categoryLinks = doc.GetElementsByClassName("with-link-02");
            foreach (var item in categoryLinks)
            {
                var categoryLink = item as AngleSharp.Dom.Html.IHtmlAnchorElement;

                if (categoryLink != null && !categoryLink.Href.StartsWith("https://www.toysrus.co.jp/f/") && !categoryLink.Href.StartsWith("https://www.toysrus.co.jp/cat"))
                {
                    urls.Add(categoryLink.Href);
                }
            }

            return urls;
        }
    }
}
