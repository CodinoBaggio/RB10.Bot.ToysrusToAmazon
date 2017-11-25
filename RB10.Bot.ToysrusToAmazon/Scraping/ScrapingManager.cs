using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;
using static RB10.Bot.ToysrusToAmazon.Scraping.AmazonScraping;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class ScrapingManager : ScrapingBase
    {
        public class Parameters
        {
            public string SaveFileName { get; set; }
            public int ToysrusDelay { get; set; }
            public int AmazonDelay { get; set; }
            public string SearchKeyword { get; set; }
            public List<string> TargetUrls { get; set; }
            public Parameters() => TargetUrls = new List<string>();
        }

        public void Start(Parameters parameters)
        {
            Task.Run(() => Run(parameters));
        }

        public void Run(Parameters parameters)
        {
            Notify("処理を開始します。", NotifyStatus.Information);

            try
            {
                // トイザらスから取得
                var toysrus = new ToysrusScraping { Delay = parameters.ToysrusDelay, AmazonDelay = parameters.AmazonDelay };
                toysrus.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                var toysrusResult = toysrus.Run(parameters.TargetUrls, parameters.SearchKeyword);

                Notify("情報取得が完了しました。", NotifyStatus.Information);

                //// Amazonから取得
                //var amazon = new AmazonScraping { Delay = parameters.AmazonDelay };
                //amazon.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                //List<ToyInformation> amazonResult = amazon.Run(toysrusResult);

                //Notify("Amazon：情報取得が完了しました。", NotifyStatus.Information);

                // ファイル出力
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("トイザらスの商品ページのURL,トイザらスの商品名,トイザらスの税込価格,トイザらスのオンライン在庫,トイザらスの店舗在庫あり,トイザらスの店舗在庫わずか,トイザらスの商品画像URL,Asin,Amazonの税込価格,Amazonの商品画像のURL");
                foreach (var result in toysrusResult)
                {
                    sb.AppendLine($"{result.Url},\"{result.ToyName}\",{result.Price},{result.OnlineStock},{result.StoreStockCount},{result.StoreLessStockCount},{result.ImageUrl},{result.Asin},{result.AmazonPrice},{result.AmazonImageUrl}");
                }

                if (0 < toysrusResult.Count())
                {
                    System.IO.File.WriteAllText(parameters.SaveFileName, sb.ToString(), Encoding.GetEncoding("shift-jis"));

                    Notify("結果ファイルの出力が完了しました。", NotifyStatus.Information);
                }
            }
            catch (Exception ex)
            {
                Notify(ex.ToString(), NotifyStatus.Exception);
            }
            finally
            {
                Notify("すべての処理が完了しました。", NotifyStatus.Information);
            }
        }
    }
}
